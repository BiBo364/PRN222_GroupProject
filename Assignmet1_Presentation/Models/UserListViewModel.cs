namespace Assignmet1_Presentation.Models;

public class UserListViewModel
{
    public List<UserListItemViewModel> Users { get; set; } = [];
    public List<SubjectListItemViewModel> Subjects { get; set; } = [];

    // Filter state
    public string? SearchTerm { get; set; }
    public int? FilterRoleId { get; set; }
    public int? FilterSubjectId { get; set; }

    // Pagination
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public int PageSize { get; set; } = 10;
}
