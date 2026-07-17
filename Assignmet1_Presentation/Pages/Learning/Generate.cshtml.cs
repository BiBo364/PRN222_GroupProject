using System.ComponentModel.DataAnnotations;
using Assignmet1_Presentation.Filters;
using Assignment1_Repository.Models;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.Learning;

[RequireLogin]
[EnableRateLimiting("ai-generation")]
public class GenerateModel : PageModel
{
    private readonly ILearningService _learningService;

    public GenerateModel(ILearningService learningService)
    {
        _learningService = learningService;
    }

    public LearningGenerationOptionsDto? Options { get; private set; }

    [BindProperty]
    public GenerateInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!await LoadOptionsAsync(cancellationToken))
            return RedirectToPage("/Learning/Index");

        Input.DocumentIds = Options!.Documents.Select(document => document.Id).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(cancellationToken);
            return Page();
        }

        var questionTypes = new List<string>();
        if (Input.MultipleChoice)
            questionTypes.Add(LearningQuestionTypes.MultipleChoice);
        if (Input.TrueFalse)
            questionTypes.Add(LearningQuestionTypes.TrueFalse);
        if (Input.ShortAnswer)
            questionTypes.Add(LearningQuestionTypes.ShortAnswer);

        if (questionTypes.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Vui lòng chọn ít nhất một loại câu hỏi.");
            await LoadOptionsAsync(cancellationToken);
            return Page();
        }

        try
        {
            var result = await _learningService.GenerateQuestionsAsync(
                HttpContext.Session.GetInt32("UserId")!.Value,
                new GenerateQuestionBankRequest
                {
                    DocumentIds = Input.DocumentIds,
                    ChapterId = Input.ChapterId,
                    QuestionCount = Input.QuestionCount,
                    QuestionTypes = questionTypes,
                    Difficulty = Input.Difficulty,
                    Focus = Input.Focus
                },
                cancellationToken);

            TempData["Success"] = result.CreatedCount == result.RequestedCount
                ? $"AI đã tạo và lưu {result.CreatedCount} câu hỏi vào ngân hàng đề."
                : $"Đã lưu {result.CreatedCount}/{result.RequestedCount} câu hỏi đạt kiểm tra chất lượng. Bạn có thể tạo bổ sung phần còn lại.";
            return RedirectToPage("/Learning/QuestionBank");
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await LoadOptionsAsync(cancellationToken);
            return Page();
        }
    }

    private async Task<bool> LoadOptionsAsync(CancellationToken cancellationToken)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
            return false;

        Options = await _learningService.GetGenerationOptionsAsync(userId.Value, cancellationToken);
        if (Options is not null)
            return true;

        TempData["Error"] = "Chỉ giảng viên đã được phân công môn học mới có thể tạo câu hỏi bằng AI.";
        return false;
    }

    public sealed class GenerateInput
    {
        public List<int> DocumentIds { get; set; } = [];
        public int? ChapterId { get; set; }

        [Range(1, 30, ErrorMessage = "Số câu hỏi phải từ 1 đến 30.")]
        public int QuestionCount { get; set; } = 10;

        public bool MultipleChoice { get; set; } = true;
        public bool TrueFalse { get; set; } = true;
        public bool ShortAnswer { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn mức độ.")]
        public string Difficulty { get; set; } = LearningDifficultyLevels.Mixed;

        [StringLength(500, ErrorMessage = "Trọng tâm không được vượt quá 500 ký tự.")]
        public string? Focus { get; set; }
    }
}
