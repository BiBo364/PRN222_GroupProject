using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Assignmet1_Presentation.Models;

public class DocumentCreateViewModel
{
    public SubjectViewModel? Subject { get; set; }

    public int? SubjectId { get; set; }

    [Required(ErrorMessage = "Please provide a file name.")]
    [Display(Name = "File name")]
    public string? OriginalName { get; set; }

    [Required]
    [Display(Name = "Type")]
    public string FileType { get; set; } = "pdf";

    [Display(Name = "Chapter")]
    public int? ChapterId { get; set; }

    public List<SelectListItem> ChapterOptions { get; set; } = [];
}
