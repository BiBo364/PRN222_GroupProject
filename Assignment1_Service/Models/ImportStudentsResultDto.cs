namespace Assignment1_Service.Models;

/// <summary>
/// Kết quả import sinh viên từ file Excel.
/// </summary>
public class ImportStudentsResultDto
{
    /// <summary>Tổng số dòng dữ liệu đọc được từ Excel (bỏ qua dòng header).</summary>
    public int TotalRows { get; set; }

    /// <summary>Số tài khoản tạo thành công.</summary>
    public int CreatedCount { get; set; }

    /// <summary>Số dòng bị bỏ qua vì email đã tồn tại.</summary>
    public int SkippedDuplicateCount { get; set; }

    /// <summary>Số dòng lỗi (email không hợp lệ, thiếu thông tin, ...).</summary>
    public int ErrorCount { get; set; }

    /// <summary>Chi tiết từng dòng trong kết quả import.</summary>
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
}

public enum ImportRowStatus
{
    Created,
    Duplicate,
    Error
}
