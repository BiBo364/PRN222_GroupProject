using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Assignmet1_Presentation.Models;

public class DocumentUploadViewModel
{
    public SubjectViewModel? Subject { get; set; }

    public int? SubjectId { get; set; }

    [Required(ErrorMessage = "Please select a file.")]
    [Display(Name = "Document file (PDF, DOCX, PPTX)")]
    public IFormFile? File { get; set; }

    [Display(Name = "Chapter")]
    public int? ChapterId { get; set; }

    public List<SelectListItem> ChapterOptions { get; set; } = [];
}
