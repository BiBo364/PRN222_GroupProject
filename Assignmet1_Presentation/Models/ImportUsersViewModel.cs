using System.ComponentModel.DataAnnotations;
using Assignment1_Service.Models;

namespace Assignmet1_Presentation.Models;

public class ImportUsersViewModel
{
    [Required(ErrorMessage = "Vui long chon file (.xlsx, .csv).")]
    [Display(Name = "File danh sach nguoi dung")]
    public IFormFile? File { get; set; }

    [Display(Name = "Vai tro")]
    [Required(ErrorMessage = "Vui long chon vai tro.")]
    public int RoleId { get; set; }

    [Display(Name = "Mon hoc")]
    public int? SubjectId { get; set; }

    public List<SubjectListItemViewModel> SubjectOptions { get; set; } = [];
    public Dictionary<int, string> TeacherBySubjectId { get; set; } = [];
    public ImportResultViewModel? Result { get; set; }
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
        ImportRowStatus.Created => "Thanh cong",
        ImportRowStatus.Duplicate => "Trung lap",
        ImportRowStatus.Error => "Loi",
        _ => "Khong xac dinh"
    };

    public string StatusCssClass => Status switch
    {
        ImportRowStatus.Created => "badge-success",
        ImportRowStatus.Duplicate => "badge-warning",
        ImportRowStatus.Error => "badge-danger",
        _ => "badge-secondary"
    };
}
