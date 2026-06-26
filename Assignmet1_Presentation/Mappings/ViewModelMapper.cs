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

    public static QuotaStatusViewModel ToViewModel(ChatQuotaStatusDto dto) => new()
    {
        SubjectId = dto.SubjectId,
        IsPlus = dto.IsPlus,
        IsAllowed = dto.IsAllowed,
        QuestionLimit = dto.QuestionLimit,
        QuestionsUsed = dto.QuestionsUsed,
        QuestionsRemaining = dto.QuestionsRemaining,
        WindowStartAt = dto.WindowStartAt,
        WindowEndAt = dto.WindowEndAt,
        CurrentPlanName = dto.CurrentPlanName,
        CurrentPackageName = dto.CurrentPackageName,
        Message = dto.Message
    };

    public static SubjectViewModel ToViewModel(SubjectDto dto) => new()
    {
        Id = dto.Id,
        Code = dto.Code,
        Name = dto.Name,
        Description = dto.Description,
        Chapters = dto.Chapters.Select(ToViewModel).ToList()
    };

    public static SubjectListItemViewModel ToViewModel(SubjectListItemDto dto) => new()
    {
        Id = dto.Id,
        Code = dto.Code,
        Name = dto.Name,
        Description = dto.Description,
        ChapterCount = dto.ChapterCount,
        DocumentCount = dto.DocumentCount,
        IndexedDocumentCount = dto.IndexedDocumentCount,
        DeletedAt = dto.DeletedAt,
        DeletedByName = dto.DeletedByName
    };

    public static SubjectDetailViewModel ToViewModel(SubjectDetailDto dto) => new()
    {
        Subject = ToViewModel(dto.Subject),
        Documents = dto.Documents.Select(ToViewModel).ToList()
    };

    public static SubjectListItemViewModel ToListItemViewModel(SubjectDetailDto dto) => new()
    {
        Id = dto.Subject.Id,
        Code = dto.Subject.Code,
        Name = dto.Subject.Name,
        Description = dto.Subject.Description,
        ChapterCount = dto.Subject.Chapters.Count,
        DocumentCount = dto.Documents.Count,
        IndexedDocumentCount = dto.Documents.Count(document => document.Status == "indexed")
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
        SubjectCode = dto.SubjectCode,
        ChapterNumber = dto.ChapterNumber,
        ChapterTitle = dto.ChapterTitle,
        CreatedAt = dto.CreatedAt,
        IndexedAt = dto.IndexedAt,
        DeletedAt = dto.DeletedAt,
        DeletedByName = dto.DeletedByName
    };

    public static DocumentListItemViewModel ToListItemViewModel(DocumentDetailDto dto) => new()
    {
        Id = dto.Id,
        OriginalName = dto.OriginalName,
        FileType = dto.FileType,
        Status = dto.Status,
        ErrorMsg = dto.ErrorMsg,
        ChunkCount = dto.Chunks.Count,
        SubjectId = dto.SubjectId,
        ChapterNumber = dto.ChapterNumber,
        ChapterTitle = dto.ChapterTitle,
        CreatedAt = dto.CreatedAt,
        IndexedAt = dto.IndexedAt
    };

    public static DocumentDetailViewModel ToDocumentDetailPage(DocumentDetailDto dto)
    {
        var chunks = dto.Chunks.Select(ToViewModel).ToList();
        var isSlideDeck = dto.FileType == "pptx";

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
            ChunkItems = ChunkDisplayItem.Build(chunks, isSlideDeck),
            IsSlideDeck = isSlideDeck
        };
    }

    public static ChunkViewModel ToViewModel(ChunkDto dto) => new()
    {
        Id = dto.Id,
        ChunkIndex = dto.ChunkIndex,
        Content = dto.Content,
        Metadata = dto.Metadata,
        PageNumber = dto.PageNumber,
        CharStart = dto.CharStart,
        CharEnd = dto.CharEnd,
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
        SubjectId = dto.SubjectId,
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
