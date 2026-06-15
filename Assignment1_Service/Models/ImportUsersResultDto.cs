namespace Assignment1_Service.Models;

/// <summary>
/// Ket qua import nguoi dung tu file Excel/CSV.
/// </summary>
public class ImportUsersResultDto
{
    /// <summary>Tong so dong du lieu doc duoc tu file, bo qua dong header.</summary>
    public int TotalRows { get; set; }

    /// <summary>So tai khoan tao thanh cong.</summary>
    public int CreatedCount { get; set; }

    /// <summary>So dong bi bo qua vi email da ton tai.</summary>
    public int SkippedDuplicateCount { get; set; }

    /// <summary>So dong loi.</summary>
    public int ErrorCount { get; set; }

    /// <summary>So email thong bao gui thanh cong.</summary>
    public int NotificationSentCount { get; set; }

    /// <summary>So email thong bao chua gui duoc.</summary>
    public int NotificationFailedCount { get; set; }

    /// <summary>Chi tiet tung dong trong ket qua import.</summary>
    public List<ImportRowResultDto> Rows { get; set; } = [];
}

public class ImportRowResultDto
{
    public int RowNumber { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Username { get; set; }
    public ImportRowStatus Status { get; set; }
    public string? Message { get; set; }
    public bool? NotificationSent { get; set; }
    public string? NotificationMessage { get; set; }
}

public enum ImportRowStatus
{
    Created,
    Duplicate,
    Error
}
