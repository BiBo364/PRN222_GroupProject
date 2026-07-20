using System.ComponentModel.DataAnnotations;
using Assignment1_Service.Models;

namespace Assignmet1_Presentation.Models;

public class ImportUsersViewModel
{
    [Display(Name = "Tệp danh sách người dùng")]
    public IFormFile? File { get; set; }

    [Display(Name = "Vai trò")]
    [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
    public int RoleId { get; set; } = 3;

    [Display(Name = "Môn học")]
    public int? SubjectId { get; set; }

    public List<SubjectListItemViewModel> SubjectOptions { get; set; } = [];
    public Dictionary<int, string> TeacherBySubjectId { get; set; } = [];
    public ImportResultViewModel? Result { get; set; }
}

public sealed class ManualCreateUserViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(200, ErrorMessage = "Họ và tên không được vượt quá 200 ký tự.")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(
        100,
        MinimumLength = 3,
        ErrorMessage = "Tên đăng nhập phải có từ 3 đến 100 ký tự.")]
    [RegularExpression(
        "^[A-Za-z0-9._-]+$",
        ErrorMessage = "Tên đăng nhập chỉ được chứa chữ cái không dấu, chữ số, dấu chấm, gạch dưới hoặc gạch ngang.")]
    [Display(Name = "Tên đăng nhập")]
    public string? Username { get; set; }

    [StringLength(200, ErrorMessage = "Email không được vượt quá 200 ký tự.")]
    [EmailAddress(ErrorMessage = "Địa chỉ email không đúng định dạng.")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Range(2, 3, ErrorMessage = "Vui lòng chọn vai trò hợp lệ.")]
    [Display(Name = "Vai trò")]
    public int RoleId { get; set; } = 3;

    [Display(Name = "Môn học phụ trách")]
    public int? SubjectId { get; set; }

    public List<SubjectListItemViewModel> SubjectOptions { get; set; } = [];
    public Dictionary<int, string> TeacherBySubjectId { get; set; } = [];
}

public class ImportResultViewModel
{
    public int TotalRows { get; set; }
    public int CreatedCount { get; set; }
    public int SkippedDuplicateCount { get; set; }
    public int ErrorCount { get; set; }
    public int NotificationSentCount { get; set; }
    public int NotificationFailedCount { get; set; }
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
    public bool? NotificationSent { get; set; }
    public string? NotificationMessage { get; set; }

    public string StatusLabel => Status switch
    {
        ImportRowStatus.Created => "Thành công",
        ImportRowStatus.Duplicate => "Trùng lặp",
        ImportRowStatus.Error => "Lỗi",
        _ => "Không xác định"
    };

    public string StatusCssClass => Status switch
    {
        ImportRowStatus.Created => "badge-success",
        ImportRowStatus.Duplicate => "badge-warning",
        ImportRowStatus.Error => "badge-danger",
        _ => "badge-secondary"
    };
}
