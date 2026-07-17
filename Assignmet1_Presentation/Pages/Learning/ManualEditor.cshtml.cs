using Assignmet1_Presentation.Filters;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.Learning;

[RequireLogin]
public class ManualEditorModel : PageModel
{
    private readonly ILearningService _learningService;

    public ManualEditorModel(ILearningService learningService)
    {
        _learningService = learningService;
    }

    public ManualQuizEditorDto? Editor { get; private set; }
    public string DraftStorageKey { get; private set; } = string.Empty;

    [BindProperty]
    public ManualQuizInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(
        int? id,
        CancellationToken cancellationToken)
    {
        Editor = await _learningService.GetManualQuizEditorAsync(
            CurrentUserId(),
            id,
            cancellationToken);
        if (Editor is null)
        {
            TempData["Error"] = "Chỉ giảng viên được phân công môn học mới có thể soạn Quiz thủ công.";
            return RedirectToPage("/Learning/Index");
        }

        Input = ManualQuizInput.FromEditor(Editor);
        DraftStorageKey = BuildDraftStorageKey(Editor.Id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
        int? id,
        string? submitAction,
        CancellationToken cancellationToken)
    {
        Input.Id = id ?? Input.Id;
        var publish = string.Equals(submitAction, "publish", StringComparison.OrdinalIgnoreCase)
            || (string.Equals(submitAction, "save", StringComparison.OrdinalIgnoreCase)
                && Input.IsPublished);

        try
        {
            var result = await _learningService.SaveManualQuizAsync(
                CurrentUserId(),
                Input.ToRequest(publish),
                isAutosave: false,
                cancellationToken);
            TempData["Success"] = publish
                ? "Đã lưu và phát hành Quiz cho sinh viên."
                : "Đã lưu Quiz ở trạng thái bản nháp.";
            return RedirectToPage("/Learning/Details", new { id = result.Id });
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToPage("/Learning/ManualEditor", new { id = Input.Id });
        }
    }

    public async Task<IActionResult> OnPostAutosaveAsync(
        [FromBody] SaveManualQuizRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _learningService.SaveManualQuizAsync(
                CurrentUserId(),
                request,
                isAutosave: true,
                cancellationToken);
            return new JsonResult(new
            {
                success = true,
                quizId = result.Id,
                savedAt = result.SavedAt,
                isPublished = result.IsPublished,
                questionIds = result.QuestionIds,
                editUrl = Url.Page("/Learning/ManualEditor", new { id = result.Id })
            });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new
            {
                success = false,
                message = exception.Message
            });
        }
    }

    private int CurrentUserId()
    {
        return HttpContext.Session.GetInt32("UserId")
            ?? throw new InvalidOperationException("Phiên đăng nhập không còn hợp lệ.");
    }

    private string BuildDraftStorageKey(int? quizId)
    {
        var suffix = quizId?.ToString() ?? "new";
        return $"rag-edu:manual-quiz:{CurrentUserId()}:{suffix}";
    }

    public sealed class ManualQuizInput
    {
        public int? Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Instructions { get; set; }
        public int DurationMinutes { get; set; } = 15;
        public bool IsPublished { get; set; }
        public bool ShuffleQuestions { get; set; } = true;
        public bool ShuffleOptions { get; set; } = true;
        public List<ManualQuestionInput> Questions { get; set; } = [];

        public SaveManualQuizRequest ToRequest(bool isPublished)
        {
            return new SaveManualQuizRequest
            {
                Id = Id,
                Title = Title,
                Description = Description,
                Instructions = Instructions,
                DurationMinutes = DurationMinutes,
                IsPublished = isPublished,
                ShuffleQuestions = ShuffleQuestions,
                ShuffleOptions = ShuffleOptions,
                Questions = Questions.Select(question => question.ToRequest()).ToList()
            };
        }

        public static ManualQuizInput FromEditor(ManualQuizEditorDto editor)
        {
            return new ManualQuizInput
            {
                Id = editor.Id,
                Title = editor.Title,
                Description = editor.Description,
                Instructions = editor.Instructions,
                DurationMinutes = editor.DurationMinutes,
                IsPublished = editor.IsPublished,
                ShuffleQuestions = editor.ShuffleQuestions,
                ShuffleOptions = editor.ShuffleOptions,
                Questions = editor.Questions
                    .Select(ManualQuestionInput.FromDto)
                    .ToList()
            };
        }
    }

    public sealed class ManualQuestionInput
    {
        public int? Id { get; set; }
        public string ClientKey { get; set; } = Guid.NewGuid().ToString("N");
        public string QuestionType { get; set; } = "multiple_choice";
        public string Prompt { get; set; } = string.Empty;
        public List<string> Options { get; set; } = [];
        public string CorrectAnswer { get; set; } = string.Empty;
        public string? Explanation { get; set; }
        public string Difficulty { get; set; } = "medium";
        public string? Topic { get; set; }
        public decimal Points { get; set; } = 1m;

        public SaveManualQuizQuestionRequest ToRequest()
        {
            return new SaveManualQuizQuestionRequest
            {
                Id = Id,
                ClientKey = ClientKey,
                QuestionType = QuestionType,
                Prompt = Prompt,
                Options = Options,
                CorrectAnswer = CorrectAnswer,
                Explanation = Explanation,
                Difficulty = Difficulty,
                Topic = Topic,
                Points = Points
            };
        }

        public static ManualQuestionInput FromDto(ManualQuizQuestionDto question)
        {
            return new ManualQuestionInput
            {
                Id = question.Id,
                ClientKey = question.ClientKey,
                QuestionType = question.QuestionType,
                Prompt = question.Prompt,
                Options = question.Options.ToList(),
                CorrectAnswer = question.CorrectAnswer,
                Explanation = question.Explanation,
                Difficulty = question.Difficulty,
                Topic = question.Topic,
                Points = question.Points
            };
        }
    }
}
