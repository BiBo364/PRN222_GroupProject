using Assignmet1_Presentation.Models;
using Assignment1_Service.Models;

namespace Assignmet1_Presentation.Mappings;

public static class ViewModelMapper
{
    public static SubscriptionPlanViewModel ToViewModel(SubscriptionPlanDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Description = dto.Description,
        Price = dto.Price,
        DurationDays = dto.DurationDays
    };

    public static UserSubscriptionViewModel ToViewModel(UserSubscriptionDto dto) => new()
    {
        Id = dto.Id,
        PlanName = dto.PlanName,
        StartAt = dto.StartAt,
        EndAt = dto.EndAt
    };

    public static PaymentTicketViewModel ToViewModel(PaymentTicketDto dto) => new()
    {
        Id = dto.Id,
        Username = dto.Username,
        Email = dto.Email,
        FullName = dto.FullName,
        PlanName = dto.PlanName,
        Amount = dto.Amount,
        TransferReference = dto.TransferReference,
        Status = dto.Status,
        AdminNote = dto.AdminNote,
        ReviewerName = dto.ReviewerName,
        ReviewedAt = dto.ReviewedAt,
        CreatedAt = dto.CreatedAt
    };

    public static SubjectViewModel ToViewModel(SubjectDto dto) => new()
    {
        Id = dto.Id,
        Code = dto.Code,
        Name = dto.Name,
        Description = dto.Description,
        Chapters = dto.Chapters.Select(ToViewModel).ToList()
    };

    public static ChapterViewModel ToViewModel(ChapterDto dto) => new()
    {
        Id = dto.Id,
        Number = dto.Number,
        Title = dto.Title
    };

    public static DocumentListItemViewModel ToViewModel(DocumentListItemDto dto) => new()
    {
        Id = dto.Id,
        OriginalName = dto.OriginalName,
        FileType = dto.FileType,
        Status = dto.Status,
        ErrorMsg = dto.ErrorMsg,
        ChunkCount = dto.ChunkCount,
        SubjectId = dto.SubjectId,
        ChapterNumber = dto.ChapterNumber,
        ChapterTitle = dto.ChapterTitle,
        CreatedAt = dto.CreatedAt,
        IndexedAt = dto.IndexedAt
    };

    public static DocumentDetailViewModel ToDocumentDetailPage(DocumentDetailDto dto)
    {
        var chunks = dto.Chunks.Select(ToViewModel).ToList();
        return new DocumentDetailViewModel
        {
            Id = dto.Id,
            OriginalName = dto.OriginalName,
            FileType = dto.FileType,
            Status = dto.Status,
            ChapterNumber = dto.ChapterNumber,
            ChapterTitle = dto.ChapterTitle,
            UploadedByName = dto.UploadedByName,
            IndexedAt = dto.IndexedAt,
            Chunks = chunks,
            ChunkItems = chunks.Select(ChunkDisplayItem.FromChunk).ToList(),
            IsSlideDeck = dto.FileType == "pptx"
        };
    }

    public static ChunkViewModel ToViewModel(ChunkDto dto) => new()
    {
        Id = dto.Id,
        ChunkIndex = dto.ChunkIndex,
        Content = dto.Content,
        Metadata = dto.Metadata,
        PageNumber = dto.PageNumber,
        TokenCount = dto.TokenCount
    };

    public static ChatSessionListItemViewModel ToViewModel(ChatSessionListItemDto dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title,
        UpdatedAt = dto.UpdatedAt
    };

    public static ChatSessionViewModel ToViewModel(ChatSessionDto dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title,
        SubjectCode = dto.SubjectCode,
        SubjectName = dto.SubjectName,
        Messages = dto.Messages.Select(ToViewModel).ToList()
    };

    public static ChatMessageViewModel ToViewModel(ChatMessageDto dto) => new()
    {
        Role = dto.Role,
        Content = dto.Content,
        CreatedAt = dto.CreatedAt,
        Citations = dto.Citations.Select(ToViewModel).ToList()
    };

    public static ChatCitationViewModel ToViewModel(ChatCitationDto dto) => new()
    {
        DocumentName = dto.DocumentName,
        SlideNumber = dto.SlideNumber,
        Score = dto.Score
    };
}
