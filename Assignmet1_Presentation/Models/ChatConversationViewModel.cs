using System.ComponentModel.DataAnnotations;
using Assignment1_Repository.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Assignmet1_Presentation.Models;

public class ChatConversationViewModel
{
    [ValidateNever]
    public Session Session { get; set; } = null!;

    [Required(ErrorMessage = "Please enter a question.")]
    [Display(Name = "Your question")]
    public string Question { get; set; } = string.Empty;
}
