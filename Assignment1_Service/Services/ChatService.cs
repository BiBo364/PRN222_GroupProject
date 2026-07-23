using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Assignment1_Service.Helpers;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Assignment1_Service.Services;

public class ChatService : IChatService
{
    private const int RetrievalTopK = 4;
    private const int RetrievalBatchSize = 120;
    private const int RetrievalCandidateBuffer = 24;

    private readonly IChatRepository _chatRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IGeminiService _geminiService;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IChatRepository chatRepository,
        ISubjectRepository subjectRepository,
        IEmbeddingService embeddingService,
        IGeminiService geminiService,
        ILogger<ChatService> logger)
    {
        _chatRepository = chatRepository;
        _subjectRepository = subjectRepository;
        _embeddingService = embeddingService;
        _geminiService = geminiService;
        _logger = logger;
    }

    public async Task<List<SubjectDto>> GetAvailableSubjectsAsync()
    {
        var subjects = await _subjectRepository.GetSubjectsWithDetailsAsync();
        return subjects.Select(DtoMapper.ToDto).ToList();
    }

    public async Task<List<ChatSessionListItemDto>> GetSessionsAsync(string userId, int? subjectId = null)
    {
        var sessions = await _chatRepository.GetUserSessionsAsync(userId, subjectId);
        return sessions.Select(DtoMapper.ToListItemDto).ToList();
    }

    public async Task<ChatSessionDto> CreateSessionAsync(string userId, int subjectId, string? title = null)
    {
        var subject = await GetAvailableSubjectAsync(subjectId);

        var session = new Session
        {
            Id = Guid.NewGuid().ToString("D"),
            SubjectId = subject.Id,
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(title) ? $"Chat {DateTime.Now:dd/MM HH:mm}" : title.Trim(),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            IsArchived = false
        };

        session = await _chatRepository.CreateSessionAsync(session);
        session.Subject = new Subject
        {
            Id = subject.Id,
            Code = subject.Code,
            Name = subject.Name,
            Description = subject.Description
        };

        _logger.LogInformation(
            "Created chat session {SessionId} for subject {SubjectId} ({SubjectCode}).",
            session.Id,
            subject.Id,
            subject.Code);
        return DtoMapper.ToDto(session);
    }

    public async Task<ChatSessionDto?> GetSessionAsync(string sessionId, string userId)
    {
        var session = await _chatRepository.GetSessionForUserAsync(sessionId, userId);
        return session is null ? null : DtoMapper.ToDto(session);
    }

    public async Task<ChatReplyDto> AskAsync(string sessionId, string userId, string question)
    {
        var totalTimer = Stopwatch.StartNew();
        var session = await _chatRepository.GetSessionForUserAsync(sessionId, userId)
            ?? throw new InvalidOperationException("Không tìm thấy cuộc trò chuyện.");

        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("Câu hỏi là bắt buộc.");

        var subjectId = session.SubjectId
            ?? throw new InvalidOperationException("Cuộc trò chuyện chưa được gắn với môn học.");

        var subject = await GetAvailableSubjectAsync(subjectId);
        var sessionLoadElapsed = totalTimer.Elapsed;

        var isFirstQuestion = !session.Messages.Any(m => m.Role == "user");
        var recentHistory = session.Messages
            .OrderBy(m => m.CreatedAt)
            .TakeLast(6)
            .Select(DtoMapper.ToDto)
            .ToList();

        await _chatRepository.AddMessageAsync(new Message
        {
            SessionId = sessionId,
            Role = "user",
            Content = question.Trim(),
            CreatedAt = DateTime.Now
        });

        var embeddingTimer = Stopwatch.StartNew();
        var configuredEmbeddingModels = await _chatRepository.GetEmbeddingModelsAsync();
        var embeddingModels = EmbeddingModelSelector.SelectForQueryAndRetrieval(configuredEmbeddingModels);
        if (embeddingModels.Count == 0)
            throw new InvalidOperationException("Chưa cấu hình mô hình biểu diễn ngữ nghĩa.");

        if (embeddingModels.Count < configuredEmbeddingModels.Count)
        {
            _logger.LogWarning(
                "Using one local fallback embedding model ({ModelId}) instead of {ConfiguredModelCount} unsupported configured models for chat retrieval.",
                embeddingModels[0].Id,
                configuredEmbeddingModels.Count);
        }

        var queryVectors = await _embeddingService.GenerateQueryEmbeddingsAsync(question, embeddingModels);
        embeddingTimer.Stop();
        if (queryVectors.Count == 0)
            throw new InvalidOperationException("Không thể tạo biểu diễn ngữ nghĩa cho câu hỏi này.");

        var retrievalTimer = Stopwatch.StartNew();
        var retrieved = await RetrieveChunksAsync(question, subject.Id, queryVectors, embeddingModels);
        retrievalTimer.Stop();
        if (retrieved.Count == 0)
            throw new InvalidOperationException("Môn học đã chọn chưa có tài liệu được lập chỉ mục.");

        _logger.LogInformation(
            "Retrieved {RetrievedCount} chunk(s) for session {SessionId} and subject {SubjectId} after batch-scanning indexed content.",
            retrieved.Count,
            sessionId,
            subject.Id);
        var answerTimer = Stopwatch.StartNew();
        var answer = retrieved.Count == 0
            ? "Mình không tìm thấy nội dung phù hợp trong tài liệu đã index để trả lời câu hỏi này."
            : await _geminiService.GenerateAnswerAsync(question, retrieved, recentHistory);
        answerTimer.Stop();

        var reply = new ChatReplyDto
        {
            Answer = answer,
            FoundInDocuments = retrieved.Count > 0,
            Citations = BuildCitations(retrieved)
        };

        var assistantMessage = await _chatRepository.AddMessageAsync(new Message
        {
            SessionId = sessionId,
            Role = "assistant",
            Content = reply.Answer,
            CreatedAt = DateTime.Now
        });

        if (reply.Citations.Count > 0)
        {
            var citations = reply.Citations.Select((c, index) => new MessageCitation
            {
                MessageId = assistantMessage.Id,
                ChunkId = c.ChunkId,
                SimilarityScore = c.Score,
                RankOrder = index + 1,
                WasUsed = true
            });
            await _chatRepository.AddCitationsAsync(citations);
        }

        session.UpdatedAt = DateTime.Now;
        if (isFirstQuestion && session.Title?.StartsWith("Chat ") == true)
            session.Title = TruncateTitle(question);

        await _chatRepository.UpdateSessionAsync(session);

        totalTimer.Stop();
        _logger.LogInformation(
            "Chat latency for session {SessionId}: session load {SessionLoadMs} ms, query embedding {EmbeddingMs} ms, retrieval {RetrievalMs} ms, answer generation {AnswerMs} ms, total {TotalMs} ms.",
            sessionId,
            sessionLoadElapsed.TotalMilliseconds,
            embeddingTimer.Elapsed.TotalMilliseconds,
            retrievalTimer.Elapsed.TotalMilliseconds,
            answerTimer.Elapsed.TotalMilliseconds,
            totalTimer.Elapsed.TotalMilliseconds);

        return reply;
    }

    private async Task<List<RetrievedChunk>> RetrieveChunksAsync(
        string question,
        int subjectId,
        IReadOnlyDictionary<int, float[]> queryVectors,
        IReadOnlyCollection<EmbeddingModel> embeddingModels)
    {
        var merged = new Dictionary<int, RetrievedChunk>();
        var modelIds = embeddingModels.Select(model => model.Id).ToArray();
        var scannedChunks = 0;
        int? lastChunkId = null;

        while (true)
        {
            var batch = await _chatRepository.GetIndexedChunkBatchBySubjectAsync(
                subjectId,
                modelIds,
                lastChunkId,
                RetrievalBatchSize);

            if (batch.Count == 0)
                break;

            scannedChunks += batch.Count;
            lastChunkId = batch[^1].Id;

            var candidates = ChunkRetriever.Retrieve(
                question,
                batch,
                queryVectors,
                RetrievalCandidateBuffer,
                useHybridRerank: true);

            foreach (var candidate in candidates)
            {
                if (!merged.TryGetValue(candidate.Chunk.Id, out var current)
                    || candidate.Score > current.Score)
                {
                    merged[candidate.Chunk.Id] = candidate;
                }
            }

            TrimRetrievedChunks(merged, RetrievalCandidateBuffer);

            if (batch.Count < RetrievalBatchSize)
                break;
        }

        _logger.LogInformation(
            "Scanned {ChunkCount} indexed chunk(s) for subject {SubjectId} across batch retrieval.",
            scannedChunks,
            subjectId);

        return merged.Values
            .OrderByDescending(chunk => chunk.Score)
            .ThenByDescending(chunk => chunk.VectorScore)
            .Take(RetrievalTopK)
            .ToList();
    }

    private static void TrimRetrievedChunks(
        IDictionary<int, RetrievedChunk> candidates,
        int keepCount)
    {
        if (candidates.Count <= keepCount)
            return;

        var retained = candidates.Values
            .OrderByDescending(chunk => chunk.Score)
            .ThenByDescending(chunk => chunk.VectorScore)
            .Take(keepCount)
            .ToDictionary(chunk => chunk.Chunk.Id);

        candidates.Clear();
        foreach (var entry in retained)
            candidates[entry.Key] = entry.Value;
    }

    private async Task<SubjectDto> GetAvailableSubjectAsync(int subjectId)
    {
        var subjects = await GetAvailableSubjectsAsync();
        return subjects.FirstOrDefault(subject => subject.Id == subjectId)
            ?? throw new InvalidOperationException("Môn học đã chọn hiện không khả dụng cho Chat AI.");
    }

    private static List<ChatCitationDto> BuildCitations(List<RetrievedChunk> chunks)
    {
        return chunks.Select(chunk =>
        {
            var slideMeta = SlideChunkMetadata.FromJson(chunk.Chunk.Metadata);
            var pageNumber = chunk.Chunk.PageNumber.GetValueOrDefault() > 0
                ? chunk.Chunk.PageNumber
                : null;

            return new ChatCitationDto
            {
                ChunkId = chunk.Chunk.Id,
                DocumentName = chunk.Document.OriginalName,
                SlideNumber = slideMeta?.EffectiveSlideNumber ?? pageNumber,
                Excerpt = Truncate(chunk.Chunk.Content, 200),
                Score = Math.Round(chunk.Score, 3)
            };
        }).ToList();
    }

    private static string TruncateTitle(string question)
    {
        var title = question.Trim();
        return title.Length <= 80 ? title : title[..77] + "...";
    }

    private static string Truncate(string text, int max)
    {
        if (text.Length <= max)
            return text;

        return text[..max] + "...";
    }
}
