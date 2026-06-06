using System.ComponentModel.DataAnnotations;
using Assignment1_Service.Models;

namespace Assignmet1_Presentation.Models;

public class ImportUsersViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn file (.xlsx, .csv).")]
    [Display(Name = "File danh sách người dùng")]
    public IFormFile? File { get; set; }

    [Display(Name = "Vai trò (áp dụng cho toàn bộ danh sách)")]
    [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
    public int RoleId { get; set; }

    [Display(Name = "Môn học (tuỳ chọn)")]
    public int? SubjectId { get; set; }

    public List<SubjectListItemViewModel> SubjectOptions { get; set; } = [];
    public Dictionary<int, string> TeacherBySubjectId { get; set; } = [];

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
