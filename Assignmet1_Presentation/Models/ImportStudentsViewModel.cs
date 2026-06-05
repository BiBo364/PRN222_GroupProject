using System.ComponentModel.DataAnnotations;
using Assignment1_Service.Models;

namespace Assignmet1_Presentation.Models;

public class ImportStudentsViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn file Excel (.xlsx).")]
    [Display(Name = "File danh sách sinh viên (.xlsx)")]
    public IFormFile? File { get; set; }

    [Display(Name = "Môn học (gán cho toàn bộ sinh viên trong file)")]
    public int? SubjectId { get; set; }

    public List<SubjectListItemViewModel> SubjectOptions { get; set; } = [];

    // Kết quả import (sau khi POST)
    public ImportResultViewModel? Result { get; set; }
}

public class ImportResultViewModel
{
    public int TotalRows { get; set; }
    public int CreatedCount { get; set; }
    public int SkippedDuplicateCount { get; set; }
    public int ErrorCount { get; set; }
    public List<ImportRowResultViewModel> Rows { get; set; } = [];
}

public class ImportRowResultViewModel
{
    public int RowNumber { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Username { get; set; }
    public ImportRowStatus Status { get; set; }
    public string? Message { get; set; }

    public string StatusLabel => Status switch
    {
        ImportRowStatus.Created   => "Thành công",
        ImportRowStatus.Duplicate => "Trùng lặp",
        ImportRowStatus.Error     => "Lỗi",
        _                         => "Không xác định"
    };

    public string StatusCssClass => Status switch
    {
        ImportRowStatus.Created   => "badge-success",
        ImportRowStatus.Duplicate => "badge-warning",
        ImportRowStatus.Error     => "badge-danger",
        _                         => "badge-secondary"
    };
}
