using System.Security.Cryptography;
using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Assignment1_Service.Helpers;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Assignment1_Service.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IDocumentRepository documentRepository,
        ISubjectRepository subjectRepository,
        IEmbeddingService embeddingService,
        ILogger<DocumentService> logger)
    {
        _documentRepository = documentRepository;
        _subjectRepository = subjectRepository;
        _embeddingService = embeddingService;
        _logger = logger;
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

    public async Task<DocumentDetailDto?> CreateDocumentEntryAsync(
        string originalName,
        string fileType,
        int subjectId,
        int? chapterId,
        int userId)
    {
        if (string.IsNullOrWhiteSpace(originalName))
            throw new ArgumentException("Original name is required.", nameof(originalName));

        fileType = fileType.Trim().ToLowerInvariant();
        if (fileType is not ("pdf" or "docx" or "pptx"))
            throw new ArgumentException("Unsupported file type.", nameof(fileType));

        var subject = await _subjectRepository.GetByIdWithDetailsAsync(subjectId)
            ?? throw new InvalidOperationException("Selected subject not found.");

        if (chapterId.HasValue && subject.Chapters.All(chapter => chapter.Id != chapterId.Value))
            throw new InvalidOperationException("Selected chapter does not belong to the selected subject.");

        var document = new Document
        {
            SubjectId = subject.Id,
            ChapterId = chapterId,
            Filename = $"manual_{Guid.NewGuid():N}.{fileType}",
            OriginalName = originalName.Trim(),
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

    public async Task<bool> DeleteDocumentAsync(int id, string storageRoot, string contentRoot, string webRoot)
    {
        var document = await _documentRepository.GetByIdWithDetailsAsync(id);
        if (document is null)
            return false;

        var filePath = DocumentPathResolver.Resolve(document, storageRoot, contentRoot, webRoot);

        if (document.FileType == "pptx")
            SlideExtractor.DeleteSlideImages(document.Id, webRoot);

        await _documentRepository.DeleteDocumentAsync(id);

        if (filePath is not null && File.Exists(filePath))
            File.Delete(filePath);

        return true;
    }

    public async Task<(DocumentUploadResultDto? Result, string? Error)> ReindexDocumentAsync(
        int id,
        string storageRoot,
        string contentRoot,
        string webRoot)
    {
        var document = await _documentRepository.GetByIdWithDetailsAsync(id);
        if (document is null)
            return (null, "Document not found.");

        var filePath = DocumentPathResolver.Resolve(document, storageRoot, contentRoot, webRoot);
        if (filePath is null)
        {
            return (null,
                "File not found on server. Documents imported directly into the database need to be uploaded again through the app.");
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
            return updated is null ? (null, "Document not found after re-index.") : (DtoMapper.ToUploadResult(updated), null);
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
            return (null, "Only PDF, DOCX, and PPTX files are supported.");

        var subject = await _subjectRepository.GetByIdWithDetailsAsync(subjectId);
        if (subject is null)
            return (null, "Selected subject not found.");

        if (chapterId.HasValue && subject.Chapters.All(chapter => chapter.Id != chapterId.Value))
            return (null, "Selected chapter does not belong to the selected subject.");

        Directory.CreateDirectory(storageRoot);

        var fileType = TextExtractor.GetFileType(originalFileName);
        var storedFileName = $"{Guid.NewGuid():N}_{Path.GetFileName(originalFileName)}";
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

        var document = new Document
        {
            SubjectId = subjectId,
            ChapterId = chapterId,
            Filename = storedFileName,
            OriginalName = originalFileName,
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
            return updated is null ? (null, "Document not found after upload.") : (DtoMapper.ToUploadResult(updated), null);
        }
        catch (Exception ex)
        {
            document.Status = "error";
            document.ErrorMsg = ex.Message;
            await _documentRepository.UpdateDocumentAsync(document);
            return (null, ex.Message);
        }
    }

    private async Task IndexDocumentContentAsync(Document document, string filePath, string webRoot)
    {
        var chunkingConfig = await _documentRepository.GetFirstChunkingConfigAsync();
        var embeddingModels = await _documentRepository.GetEmbeddingModelsAsync();
        if (embeddingModels.Count == 0)
            throw new InvalidOperationException("No embedding model configured in the database.");

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
                throw new InvalidOperationException("No text could be extracted from the file.");

            var chunkSize = chunkingConfig?.ChunkSize ?? 800;
            var overlap = chunkingConfig?.ChunkOverlap ?? 100;
            chunkEntities = new List<Chunk>();

            var nextChunkIndex = 0;
            foreach (var page in pages)
            {
                var chunks = TextChunker.Chunk(page.Content, chunkSize, overlap);
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
                throw new InvalidOperationException("Document has no content to chunk.");

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
            throw new InvalidOperationException("No slides found in the presentation.");

        return slides.Select((slide, index) =>
        {
            var metadata = new SlideChunkMetadata
            {
                Type = "slide",
                SlideNumber = slide.SlideNumber,
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
            throw new InvalidOperationException("No embedding vectors could be generated for this document.");

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
                throw new InvalidOperationException("At least one embedding vector is required for every chunk.");

            chunks[index].Embeddings = embeddings;
        }
    }
}
