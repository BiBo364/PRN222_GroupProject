using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Assignmet1_Presentation.Models;

public class DocumentCreateViewModel
{
    public SubjectViewModel? Subject { get; set; }

    public int? SubjectId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên tệp.")]
    [Display(Name = "Tên tệp")]
    public string? OriginalName { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại tệp.")]
    [Display(Name = "Loại tệp")]
    public string FileType { get; set; } = "pdf";

    [Display(Name = "Chương")]
    public int? ChapterId { get; set; }

    public List<SelectListItem> ChapterOptions { get; set; } = [];
}
