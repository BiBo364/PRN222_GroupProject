using System.ComponentModel.DataAnnotations;

namespace Assignmet1_Presentation.Models;

public class SubjectCreateViewModel
{
    [Required(ErrorMessage = "Please provide a subject code.")]
    [Display(Name = "Subject code")]
    public string? Code { get; set; }

    [Required(ErrorMessage = "Please provide a subject name.")]
    [Display(Name = "Subject name")]
    public string? Name { get; set; }

    [Display(Name = "Description")]
    public string? Description { get; set; }
}