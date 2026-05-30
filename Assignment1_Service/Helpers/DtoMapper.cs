using Assignment1_Repository.Models;
using Assignment1_Service.Models;

namespace Assignment1_Service.Helpers;

public static class DtoMapper
{
    public static SubscriptionPlanDto ToDto(SubscriptionPlan plan) => new()
    {
        Id = plan.Id,
        Name = plan.Name,
        Description = plan.Description,
        Price = plan.Price,
        DurationDays = plan.DurationDays
    };

    public static UserSubscriptionDto ToDto(UserSubscription subscription) => new()
    {
        Id = subscription.Id,
        PlanName = subscription.Plan.Name,
        StartAt = subscription.StartAt,
        EndAt = subscription.EndAt
    };

    public static PaymentTicketDto ToDto(PaymentTicket ticket) => new()
    {
        Id = ticket.Id,
        UserId = ticket.UserId,
        Username = ticket.User.Username,
        Email = ticket.User.Email,
        FullName = ticket.User.FullName,
        PlanName = ticket.Plan.Name,
        Amount = ticket.Amount,
        TransferReference = ticket.TransferReference,
        Status = ticket.Status,
        AdminNote = ticket.AdminNote,
        ReviewerName = ticket.ReviewedByNavigation?.FullName ?? ticket.ReviewedByNavigation?.Username,
        ReviewedAt = ticket.ReviewedAt,
        CreatedAt = ticket.CreatedAt
    };

    public static SubjectDto ToDto(Subject subject) => new()
    {
        Id = subject.Id,
        Code = subject.Code,
        Name = subject.Name,
        Description = subject.Description,
        Chapters = subject.Chapters
            .OrderBy(c => c.Number)
            .Select(c => new ChapterDto { Id = c.Id, Number = c.Number, Title = c.Title })
            .ToList()
    };

    public static DocumentListItemDto ToListItemDto(Document document) => new()
    {
        Id = document.Id,
        OriginalName = document.OriginalName,
        FileType = document.FileType,
        Status = document.Status,
        ErrorMsg = document.ErrorMsg,
        ChunkCount = document.Chunks.Count,
        SubjectId = document.SubjectId,
        ChapterNumber = document.Chapter?.Number,
        ChapterTitle = document.Chapter?.Title,
        CreatedAt = document.CreatedAt,
        IndexedAt = document.IndexedAt
    };

    public static DocumentDetailDto ToDetailDto(Document document) => new()
    {
        Id = document.Id,
        OriginalName = document.OriginalName,
        FileType = document.FileType,
        Status = document.Status,
        ChapterNumber = document.Chapter?.Number,
        ChapterTitle = document.Chapter?.Title,
        UploadedByName = document.UploadedByNavigation?.FullName ?? document.UploadedByNavigation?.Username,
        IndexedAt = document.IndexedAt,
        Chunks = document.Chunks
            .OrderBy(c => c.ChunkIndex)
            .Select(ToDto)
            .ToList()
    };

    public static ChunkDto ToDto(Chunk chunk) => new()
    {
        Id = chunk.Id,
        ChunkIndex = chunk.ChunkIndex,
        Content = chunk.Content,
        Metadata = chunk.Metadata,
        PageNumber = chunk.PageNumber,
        TokenCount = chunk.TokenCount
    };

    public static DocumentUploadResultDto ToUploadResult(Document document) => new()
    {
        OriginalName = document.OriginalName,
        ChunkCount = document.Chunks.Count
    };

    public static ChatSessionDto ToDto(Session session) => new()
    {
        Id = session.Id,
        Title = session.Title,
        SubjectCode = session.Subject?.Code,
        SubjectName = session.Subject?.Name,
        Messages = session.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(ToDto)
            .ToList()
    };

    public static ChatMessageDto ToDto(Message message) => new()
    {
        Role = message.Role,
        Content = message.Content,
        CreatedAt = message.CreatedAt,
        Citations = message.MessageCitations
            .OrderBy(c => c.RankOrder)
            .Select(ToCitationDto)
            .ToList()
    };

    public static ChatCitationDto ToCitationDto(MessageCitation citation)
    {
        var slideMeta = SlideChunkMetadata.FromJson(citation.Chunk.Metadata);
        return new ChatCitationDto
        {
            ChunkId = citation.ChunkId,
            DocumentName = citation.Chunk.Document.OriginalName,
            SlideNumber = slideMeta?.SlideNumber ?? citation.Chunk.PageNumber,
            Excerpt = citation.Chunk.Content.Length > 120
                ? citation.Chunk.Content[..120] + "…"
                : citation.Chunk.Content,
            Score = citation.SimilarityScore ?? 0
        };
    }

    public static ChatSessionListItemDto ToListItemDto(Session session) => new()
    {
        Id = session.Id,
        Title = session.Title,
        UpdatedAt = session.UpdatedAt
    };
}
