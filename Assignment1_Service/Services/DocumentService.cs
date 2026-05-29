using System.Security.Cryptography;
using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Assignment1_Service.Helpers;
using Assignment1_Service.Services.Interfaces;

namespace Assignment1_Service.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;

    public DocumentService(IDocumentRepository documentRepository)
    {
        _documentRepository = documentRepository;
    }

    public Task<Subject?> GetDemoSubjectAsync()
    {
        return _documentRepository.GetFirstSubjectWithChaptersAsync();
    }

    public Task<List<Document>> GetDocumentsAsync()
    {
        return _documentRepository.GetDocumentsWithDetailsAsync();
    }

    public Task<Document?> GetDocumentByIdAsync(int id)
    {
        return _documentRepository.GetByIdWithDetailsAsync(id);
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

    public async Task<(Document? Document, string? Error)> ReindexDocumentAsync(
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
            return (updated, null);
        }
        catch (Exception ex)
        {
            document.Status = "failed";
            document.ErrorMsg = ex.Message;
            await _documentRepository.UpdateDocumentAsync(document);
            return (null, ex.Message);
        }
    }

    public async Task<(Document? Document, string? Error)> UploadAndProcessAsync(
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
            return (updated, null);
        }
        catch (Exception ex)
        {
            document.Status = "failed";
            document.ErrorMsg = ex.Message;
            await _documentRepository.UpdateDocumentAsync(document);
            return (null, ex.Message);
        }
    }

    private async Task IndexDocumentContentAsync(Document document, string filePath, string webRoot)
    {
        var chunkingConfig = await _documentRepository.GetFirstChunkingConfigAsync();
        var embeddingModel = await _documentRepository.GetFirstEmbeddingModelAsync()
            ?? throw new InvalidOperationException("No embedding model configured in the database.");

        List<Chunk> chunkEntities;

        if (document.FileType == "pptx")
        {
            chunkEntities = BuildSlideChunks(document, filePath, webRoot, chunkingConfig?.Id, embeddingModel);
            document.PageCount = chunkEntities.Count;
        }
        else
        {
            var text = TextExtractor.ExtractText(filePath, document.FileType);
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("No text could be extracted from the file.");

            var chunkSize = chunkingConfig?.ChunkSize ?? 500;
            var overlap = chunkingConfig?.ChunkOverlap ?? 50;
            var chunks = TextChunker.Chunk(text, chunkSize, overlap);

            if (chunks.Count == 0)
                throw new InvalidOperationException("Document has no content to chunk.");

            chunkEntities = chunks.Select(c => CreateTextChunk(
                document.Id,
                chunkingConfig?.Id,
                embeddingModel,
                c.Index,
                c.Content,
                c.CharStart,
                c.CharEnd)).ToList();

            document.PageCount = document.FileType == "pdf" ? EstimatePageCount(text) : document.PageCount;
        }

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
        int? chunkingConfigId,
        EmbeddingModel embeddingModel)
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
                CreatedAt = DateTime.Now,
                Embeddings = new List<Embedding>
                {
                    new()
                    {
                        EmbeddingModelId = embeddingModel.Id,
                        Vector = SimpleEmbedder.GenerateVector(slide.Text, embeddingModel.Dimension),
                        CreatedAt = DateTime.Now
                    }
                }
            };
        }).ToList();
    }

    private static Chunk CreateTextChunk(
        int documentId,
        int? chunkingConfigId,
        EmbeddingModel embeddingModel,
        int index,
        string content,
        int charStart,
        int charEnd)
    {
        return new Chunk
        {
            DocumentId = documentId,
            ChunkingConfigId = chunkingConfigId,
            ChunkIndex = index,
            Content = content,
            CharStart = charStart,
            CharEnd = charEnd,
            TokenCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
            CreatedAt = DateTime.Now,
            Embeddings = new List<Embedding>
            {
                new()
                {
                    EmbeddingModelId = embeddingModel.Id,
                    Vector = SimpleEmbedder.GenerateVector(content, embeddingModel.Dimension),
                    CreatedAt = DateTime.Now
                }
            }
        };
    }

    private static int EstimatePageCount(string text)
    {
        return Math.Max(1, text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length / 40);
    }
}
