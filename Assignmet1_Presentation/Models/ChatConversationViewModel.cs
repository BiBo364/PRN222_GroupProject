using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Assignmet1_Presentation.Models;

public class ChatConversationViewModel
{
    [ValidateNever]
    public ChatSessionViewModel Session { get; set; } = null!;

    [ValidateNever]
    public List<SubjectViewModel> AvailableSubjects { get; set; } = [];

    public int? SelectedSubjectId { get; set; }

    [ValidateNever]
    public QuotaStatusViewModel? QuotaStatus { get; set; }

    [Required(ErrorMessage = "Please enter a question.")]
    [Display(Name = "Your question")]
    public string Question { get; set; } = string.Empty;
}
