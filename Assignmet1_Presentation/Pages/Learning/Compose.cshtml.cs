using System.ComponentModel.DataAnnotations;
using Assignmet1_Presentation.Filters;
using Assignment1_Repository.Models;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.Learning;

[RequireLogin]
public class ComposeModel : PageModel
{
    private readonly ILearningService _learningService;

    public ComposeModel(ILearningService learningService)
    {
        _learningService = learningService;
    }

    public ComposeLearningSetOptionsDto? Options { get; private set; }

    [BindProperty]
    public ComposeInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!await LoadOptionsAsync(cancellationToken))
            return RedirectToPage("/Learning/Index");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(cancellationToken);
            return Page();
        }

        try
        {
            var set = await _learningService.ComposeLearningSetAsync(
                CurrentUserId(),
                new ComposeLearningSetRequest
                {
                    ActivityType = Input.ActivityType,
                    QuestionCount = Input.QuestionCount,
                    Difficulty = Input.Difficulty,
                    Focus = Input.Focus,
                    PublishImmediately = Input.PublishImmediately
                },
                cancellationToken);
            TempData["Success"] = Input.PublishImmediately
                ? "AI đã tổng hợp và phát hành bộ ôn tập."
                : "AI đã tổng hợp bộ ôn tập ở trạng thái bản nháp.";
            return RedirectToPage("/Learning/Details", new { id = set.Id });
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
        Options = await _learningService.GetComposeOptionsAsync(CurrentUserId(), cancellationToken);
        if (Options is not null)
            return true;

        TempData["Error"] = "Chỉ giảng viên đã được phân công môn học mới có thể tạo bộ ôn tập.";
        return false;
    }

    private int CurrentUserId()
    {
        return HttpContext.Session.GetInt32("UserId")
            ?? throw new InvalidOperationException("Phiên đăng nhập không còn hợp lệ.");
    }

    public sealed class ComposeInput
    {
        [Required(ErrorMessage = "Vui lòng chọn loại hoạt động.")]
        public string ActivityType { get; set; } = LearningActivityTypes.Quiz;

        [Range(1, 50, ErrorMessage = "Số câu hỏi phải từ 1 đến 50.")]
        public int QuestionCount { get; set; } = 10;

        [Required(ErrorMessage = "Vui lòng chọn mức độ.")]
        public string Difficulty { get; set; } = LearningDifficultyLevels.Mixed;

        [StringLength(500, ErrorMessage = "Trọng tâm không được vượt quá 500 ký tự.")]
        public string? Focus { get; set; }

        public bool PublishImmediately { get; set; }
    }
}
