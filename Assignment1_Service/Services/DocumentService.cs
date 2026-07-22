using System.Security.Cryptography;
using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Assignment1_Service.Helpers;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Assignment1_Service.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IUserReposity _userRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<DocumentService> _logger;
    private readonly ChunkingSettings _chunkingSettings;
    private readonly IAccountNotificationService _notificationService;

    public DocumentService(
        IDocumentRepository documentRepository,
        ISubjectRepository subjectRepository,
        IUserReposity userRepository,
        IEmbeddingService embeddingService,
        ILogger<DocumentService> logger,
        IOptions<ChunkingSettings> chunkingSettings,
        IAccountNotificationService notificationService)
    {
        _documentRepository = documentRepository;
        _subjectRepository = subjectRepository;
        _userRepository = userRepository;
        _embeddingService = embeddingService;
        _logger = logger;
        _chunkingSettings = chunkingSettings.Value;
        _notificationService = notificationService;
    }

    public async Task<List<DocumentListItemDto>> GetDocumentsAsync()
    {
        var documents = await _documentRepository.GetDocumentsWithDetailsAsync();
        return documents.Select(DtoMapper.ToListItemDto).ToList();
    }

    public async Task<DocumentDetailDto?> GetDocumentByIdAsync(int id)
    {
        var document = await _documentRepository.GetByIdWithDetailsAsync(id);
        return document is null ? null : DtoMapper.ToDetailDto(document);
    }

    public async Task<DocumentDetailDto?> GetDeletedDocumentByIdAsync(int id)
    {
        var document = await _documentRepository.GetDeletedByIdWithDetailsAsync(id);
        return document is null ? null : DtoMapper.ToDetailDto(document);
    }

    public async Task<DocumentDetailDto?> CreateDocumentEntryAsync(
        string originalName,
        string fileType,
        int subjectId,
        int? chapterId,
        int userId)
    {
        if (string.IsNullOrWhiteSpace(originalName))
            throw new ArgumentException("Tên tài liệu là bắt buộc.", nameof(originalName));

        var normalizedOriginalName = Path.GetFileName(originalName).Trim();
        fileType = fileType.Trim().ToLowerInvariant();
        if (fileType is not ("pdf" or "docx" or "pptx"))
            throw new ArgumentException("Loại tệp không được hỗ trợ.", nameof(fileType));

        var (allowed, accessError) = await ValidateUploaderSubjectAccessAsync(userId, subjectId);
        if (!allowed)
            throw new InvalidOperationException(accessError);

        var subject = await _subjectRepository.GetByIdWithDetailsAsync(subjectId)
            ?? throw new InvalidOperationException("Không tìm thấy môn học đã chọn.");

        if (chapterId.HasValue && subject.Chapters.All(chapter => chapter.Id != chapterId.Value))
            throw new InvalidOperationException("Chương đã chọn không thuộc môn học.");

        if (await _documentRepository.ExistsActiveDocumentNameAsync(subject.Id, normalizedOriginalName))
        {
            await SendDuplicateNotificationAsync(userId, normalizedOriginalName, subject, "Tên tài liệu trùng lặp với tài liệu đã tồn tại trong môn học.");
            throw new InvalidOperationException("Tài liệu này đã tồn tại trong môn học.");
        }

        var document = new Document
        {
            SubjectId = subject.Id,
            ChapterId = chapterId,
            Filename = $"manual_{Guid.NewGuid():N}.{fileType}",
            OriginalName = normalizedOriginalName,
            FileType = fileType,
            FileSize = null,
            FileHash = null,
            StoragePath = $"manual://{Guid.NewGuid():N}",
            PageCount = null,
            Status = "pending",
            ErrorMsg = null,
            UploadedBy = userId,
            CreatedAt = DateTime.Now,
            IndexedAt = null
        };

        document = await _documentRepository.AddDocumentAsync(document);
        var created = await _documentRepository.GetByIdWithDetailsAsync(document.Id);
        return created is null ? null : DtoMapper.ToDetailDto(created);
    }

    public async Task<(DocumentDetailDto? Document, string? Error)> UpdateDocumentMetadataAsync(
        int id,
        string originalName,
        int? chapterId,
        int userId)
    {
        var document = await _documentRepository.GetByIdWithDetailsAsync(id);
        if (document is null)
            return (null, "Không tìm thấy tài liệu.");

        if (!document.SubjectId.HasValue)
            return (null, "Tài liệu này chưa được gắn với môn học.");

        var (allowed, accessError) = await ValidateUploaderSubjectAccessAsync(userId, document.SubjectId.Value);
        if (!allowed)
            return (null, accessError);

        var subject = await _subjectRepository.GetByIdWithDetailsAsync(document.SubjectId.Value);
        if (subject is null)
            return (null, "Môn học của tài liệu không tồn tại.");

        if (chapterId.HasValue && subject.Chapters.All(chapter => chapter.Id != chapterId.Value))
            return (null, "Chương đã chọn không thuộc môn học này.");

        var normalizedOriginalName = Path.GetFileName(originalName).Trim();
        if (string.IsNullOrWhiteSpace(normalizedOriginalName))
            return (null, "Tên tài liệu không được để trống.");

        if (await _documentRepository.ExistsActiveDocumentNameAsync(
                document.SubjectId.Value,
                normalizedOriginalName,
                document.Id))
        {
            await SendDuplicateNotificationAsync(userId, normalizedOriginalName, subject, "Tên tài liệu trùng lặp với tài liệu khác trong môn học khi cập nhật.");
            return (null, "Đã có tài liệu cùng tên trong môn học này.");
        }

        document.OriginalName = normalizedOriginalName;
        document.ChapterId = chapterId;
        await _documentRepository.UpdateDocumentAsync(document);

        var updated = await _documentRepository.GetByIdWithDetailsAsync(document.Id);
        return updated is null ? (null, "Không tìm thấy tài liệu sau khi cập nhật.") : (DtoMapper.ToDetailDto(updated), null);
    }

    public async Task<bool> DeleteDocumentAsync(int id, string storageRoot, string contentRoot, string webRoot, int? deletedByUserId = null)
    {
        var document = await _documentRepository.GetByIdWithDetailsAsync(id);
        if (document is null)
            return false;

        document.IsDeleted = true;
        document.DeletedAt = DateTime.Now;
        document.DeletedBy = deletedByUserId;
        await _documentRepository.UpdateDocumentAsync(document);

        return true;
    }

    public async Task<List<DocumentListItemDto>> GetDeletedDocumentsAsync()
    {
        var docs = await _documentRepository.GetDeletedDocumentsAsync();
        return docs.Select(DtoMapper.ToListItemDto).ToList();
    }

    public async Task<(bool Success, string? Error)> RestoreDocumentAsync(int id)
    {
        var document = await _documentRepository.GetDeletedByIdWithDetailsAsync(id);
        if (document is null)
            return (false, "Không tìm thấy tài liệu trong thùng rác.");

        if (!document.SubjectId.HasValue)
            return (false, "Tài liệu này chưa được gắn với môn học.");

        if (await _documentRepository.ExistsActiveDocumentNameAsync(document.SubjectId.Value, document.OriginalName))
            return (false, "Đã có tài liệu cùng tên trong môn học này.");

        if (!string.IsNullOrWhiteSpace(document.FileHash)
            && await _documentRepository.ExistsActiveDocumentHashAsync(document.SubjectId.Value, document.FileHash))
        {
            return (false, "Nội dung tài liệu này đã tồn tại trong môn học.");
        }

        var restored = await _documentRepository.RestoreDocumentAsync(id);
        return restored ? (true, null) : (false, "Không thể khôi phục tài liệu.");
    }

    public async Task<(DocumentUploadResultDto? Result, string? Error)> ReindexDocumentAsync(
        int id,
        int userId,
        string storageRoot,
        string contentRoot,
        string webRoot)
    {
        var document = await _documentRepository.GetByIdWithDetailsAsync(id);
        if (document is null)
            return (null, "Không tìm thấy tài liệu.");

        if (document.SubjectId.HasValue)
        {
            var (allowed, accessError) = await ValidateUploaderSubjectAccessAsync(userId, document.SubjectId.Value);
            if (!allowed)
                return (null, accessError);
        }

        var filePath = DocumentPathResolver.Resolve(document, storageRoot, contentRoot, webRoot);
        if (filePath is null)
        {
            return (null,
                "Không tìm thấy tệp trên máy chủ. Tài liệu được nhập trực tiếp vào cơ sở dữ liệu cần được tải lại qua ứng dụng.");
        }

        document.Status = "processing";
        document.ErrorMsg = null;
        await _documentRepository.UpdateDocumentAsync(document);

        try
        {
            if (document.Chunks.Count > 0)
            {
                if (document.FileType == "pptx")
                    SlideExtractor.DeleteSlideImages(document.Id, webRoot);
                await _documentRepository.ClearChunksAsync(document.Id);
            }

            await IndexDocumentContentAsync(document, filePath, webRoot);
            var updated = await _documentRepository.GetByIdWithDetailsAsync(id);
            return updated is null ? (null, "Không tìm thấy tài liệu sau khi lập lại chỉ mục.") : (DtoMapper.ToUploadResult(updated), null);
        }
        catch (Exception ex)
        {
            document.Status = "error";
            document.ErrorMsg = ex.Message;
            await _documentRepository.UpdateDocumentAsync(document);
            return (null, ex.Message);
        }
    }

    public async Task<(DocumentUploadResultDto? Result, string? Error)> UploadAndProcessAsync(
        Stream fileStream,
        string originalFileName,
        long fileSize,
        int subjectId,
        int? chapterId,
        int userId,
        string storageRoot,
        string webRoot)
    {
        if (!TextExtractor.IsAllowedExtension(originalFileName))
            return (null, "Chỉ hỗ trợ tệp PDF, DOCX và PPTX.");

        var normalizedOriginalName = Path.GetFileName(originalFileName).Trim();

        var (allowed, accessError) = await ValidateUploaderSubjectAccessAsync(userId, subjectId);
        if (!allowed)
            return (null, accessError);

        var subject = await _subjectRepository.GetByIdWithDetailsAsync(subjectId);
        if (subject is null)
            return (null, "Không tìm thấy môn học đã chọn.");

        if (chapterId.HasValue && subject.Chapters.All(chapter => chapter.Id != chapterId.Value))
            return (null, "Chương đã chọn không thuộc môn học.");

        if (await _documentRepository.ExistsActiveDocumentNameAsync(subjectId, normalizedOriginalName))
        {
            await SendDuplicateNotificationAsync(userId, normalizedOriginalName, subject, "Tên tài liệu trùng lặp với tài liệu đã tồn tại trong môn học.");
            return (null, "Tài liệu này đã tồn tại trong môn học.");
        }

        Directory.CreateDirectory(storageRoot);

        var fileType = TextExtractor.GetFileType(normalizedOriginalName);
        var storedFileName = $"{Guid.NewGuid():N}_{normalizedOriginalName}";
        var storagePath = Path.Combine(storageRoot, storedFileName);

        string fileHash;
        await using (var output = File.Create(storagePath))
        {
            using var sha256 = SHA256.Create();
            await using var cryptoStream = new CryptoStream(output, sha256, CryptoStreamMode.Write);
            await fileStream.CopyToAsync(cryptoStream);
            await cryptoStream.FlushFinalBlockAsync();
            fileHash = Convert.ToHexString(sha256.Hash!);
        }

        if (await _documentRepository.ExistsActiveDocumentHashAsync(subjectId, fileHash))
        {
            DeleteStoredFileQuietly(storagePath);
            await SendDuplicateNotificationAsync(userId, normalizedOriginalName, subject, "Nội dung tài liệu trùng lặp với tài liệu đã tồn tại trong môn học (Trùng mã Hash SHA-256).");
            return (null, "Nội dung tài liệu này đã tồn tại trong môn học.");
        }

        var document = new Document
        {
            SubjectId = subjectId,
            ChapterId = chapterId,
            Filename = storedFileName,
            OriginalName = normalizedOriginalName,
            FileType = fileType,
            FileSize = fileSize,
            FileHash = fileHash,
            StoragePath = storagePath,
            Status = "processing",
            UploadedBy = userId,
            CreatedAt = DateTime.Now
        };

        document = await _documentRepository.AddDocumentAsync(document);

        try
        {
            await IndexDocumentContentAsync(document, storagePath, webRoot);
            var updated = await _documentRepository.GetByIdWithDetailsAsync(document.Id);
            return updated is null ? (null, "Không tìm thấy tài liệu sau khi tải lên.") : (DtoMapper.ToUploadResult(updated), null);
        }
        catch (Exception ex)
        {
            document.Status = "error";
            document.ErrorMsg = ex.Message;
            await _documentRepository.UpdateDocumentAsync(document);
            return (null, ex.Message);
        }
    }

    private void DeleteStoredFileQuietly(string storagePath)
    {
        try
        {
            if (File.Exists(storagePath))
                File.Delete(storagePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete duplicate upload file {StoragePath}.", storagePath);
        }
    }

    private async Task<(bool Allowed, string? Error)> ValidateUploaderSubjectAccessAsync(int userId, int subjectId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return (false, "Không tìm thấy người dùng.");

        if (user.RoleId != 2)
            return (false, "Bạn không có quyền upload tài liệu.");

        if (!user.AssignedSubjects.Any(subject => subject.IsDeleted != true && subject.Id == subjectId))
            return (false, "Bạn chỉ được phép tải tài liệu lên môn học được phân công.");

        return (true, null);
    }

    private async Task IndexDocumentContentAsync(Document document, string filePath, string webRoot)
    {
        var embeddingModels = await _documentRepository.GetEmbeddingModelsAsync();
        if (embeddingModels.Count == 0)
            throw new InvalidOperationException("Chưa cấu hình mô hình biểu diễn ngữ nghĩa trong cơ sở dữ liệu.");

        // Đọc cấu hình chunk từ DB — bản ghi này đã được sync từ appsettings.json lúc khởi động.
        // Muốn thay đổi: sửa section "Chunking" trong appsettings.json rồi restart app.
        var chunkingConfig = await _documentRepository.GetFirstChunkingConfigAsync();
        var maxWordsPerChunk = chunkingConfig?.ChunkSize   ?? _chunkingSettings.MaxWordsPerChunk;
        var overlapWords     = chunkingConfig?.ChunkOverlap ?? _chunkingSettings.OverlapWords;

        List<Chunk> chunkEntities;

        if (document.FileType == "pptx")
        {
            chunkEntities = BuildSlideChunks(document, filePath, webRoot, chunkingConfig?.Id);
            document.PageCount = chunkEntities.Count;
        }
        else
        {
            var pages = TextExtractor.ExtractPages(filePath, document.FileType);
            if (pages.Count == 0)
                throw new InvalidOperationException("Không thể trích xuất văn bản từ tệp.");

            chunkEntities = new List<Chunk>();
            var nextChunkIndex = 0;

            foreach (var page in pages)
            {
                var chunks = TextChunker.Chunk(page.Content, maxWordsPerChunk, overlapWords);
                foreach (var chunk in chunks)
                {
                    chunkEntities.Add(CreateTextChunk(
                        document.Id,
                        chunkingConfig?.Id,
                        nextChunkIndex++,
                        chunk.Content,
                        chunk.CharStart,
                        chunk.CharEnd,
                        page.PageNumber));
                }
            }

            if (chunkEntities.Count == 0)
                throw new InvalidOperationException("Tài liệu không có nội dung để phân đoạn.");

            document.PageCount = pages.Count;
        }

        _logger.LogInformation(
            "Indexed document {DocumentId} ({OriginalName}) into {ChunkCount} chunk(s) across {PageCount} page(s).",
            document.Id,
            document.OriginalName,
            chunkEntities.Count,
            document.PageCount ?? 0);

        await AttachEmbeddingsAsync(chunkEntities, embeddingModels);
        await _documentRepository.AddChunksAsync(chunkEntities);

        document.Status = "indexed";
        document.IndexedAt = DateTime.Now;
        document.ErrorMsg = null;
        document.StoragePath = filePath;
        await _documentRepository.UpdateDocumentAsync(document);
    }

    private static List<Chunk> BuildSlideChunks(
        Document document,
        string filePath,
        string webRoot,
        int? chunkingConfigId)
    {
        SlideExtractor.DeleteSlideImages(document.Id, webRoot);
        var slides = SlideExtractor.ExtractSlides(filePath, document.Id, webRoot);

        if (slides.Count == 0)
            throw new InvalidOperationException("Không tìm thấy trang chiếu trong tệp trình chiếu.");

        return slides.Select((slide, index) =>
        {
            var metadata = new SlideChunkMetadata
            {
                Type = "slide",
                SlideNumber = slide.SlideNumber,
                PageNumber = slide.SlideNumber,
                ImageUrls = slide.ImageUrls
            };

            return new Chunk
            {
                DocumentId = document.Id,
                ChunkingConfigId = chunkingConfigId,
                ChunkIndex = index,
                Content = slide.Text,
                PageNumber = slide.SlideNumber,
                TokenCount = slide.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
                Metadata = SlideChunkMetadata.ToJson(metadata),
                CreatedAt = DateTime.Now
            };
        }).ToList();
    }

    private static Chunk CreateTextChunk(
        int documentId,
        int? chunkingConfigId,
        int index,
        string content,
        int charStart,
        int charEnd,
        int pageNumber)
    {
        return new Chunk
        {
            DocumentId = documentId,
            ChunkingConfigId = chunkingConfigId,
            ChunkIndex = index,
            Content = content,
            CharStart = charStart,
            CharEnd = charEnd,
            PageNumber = pageNumber,
            Metadata = TextChunkMetadata.ToJson(new TextChunkMetadata { PageNumber = pageNumber }),
            TokenCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
            CreatedAt = DateTime.Now
        };
    }

    private async Task AttachEmbeddingsAsync(
        List<Chunk> chunks,
        IReadOnlyCollection<EmbeddingModel> embeddingModels)
    {
        var texts = chunks.Select(chunk => chunk.Content).ToList();
        var vectorsByModelId = await _embeddingService.GenerateDocumentEmbeddingsAsync(texts, embeddingModels);

        if (vectorsByModelId.Count == 0)
            throw new InvalidOperationException("Không thể tạo véc-tơ biểu diễn ngữ nghĩa cho tài liệu này.");

        _logger.LogInformation(
            "Generated embeddings for {ChunkCount} chunk(s) using {ModelCount} embedding model(s).",
            chunks.Count,
            embeddingModels.Count);

        for (var index = 0; index < chunks.Count; index++)
        {
            var embeddings = new List<Embedding>();

            foreach (var model in embeddingModels.OrderBy(model => model.Id))
            {
                if (!vectorsByModelId.TryGetValue(model.Id, out var vectors) || index >= vectors.Count)
                    continue;

                embeddings.Add(new Embedding
                {
                    EmbeddingModelId = model.Id,
                    Vector = VectorMath.SerializeVector(vectors[index]),
                    CreatedAt = DateTime.Now
                });
            }

            if (embeddings.Count == 0)
                throw new InvalidOperationException("Mỗi đoạn nội dung cần có ít nhất một véc-tơ biểu diễn ngữ nghĩa.");

            chunks[index].Embeddings = embeddings;
        }
    }

    private async Task SendDuplicateNotificationAsync(int userId, string documentName, Subject subject, string reason)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user is not null && !string.IsNullOrWhiteSpace(user.Email))
            {
                await _notificationService.SendDuplicateDocumentNotificationEmailAsync(
                    user.Email,
                    user.FullName ?? user.Username,
                    documentName,
                    subject.Code,
                    subject.Name,
                    reason
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi email thông báo trùng lặp tài liệu cho giảng viên.");
        }
    }
}
