using System.ComponentModel.DataAnnotations;

namespace Assignmet1_Presentation.Models;

public class SubjectEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã môn học.")]
    [Display(Name = "Mã môn học")]
    public string? Code { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên môn học.")]
    [Display(Name = "Tên môn học")]
    public string? Name { get; set; }

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }
}
