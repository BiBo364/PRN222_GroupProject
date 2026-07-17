using Assignmet1_Presentation.Filters;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.Learning;

[RequireLogin]
public class QuestionBankModel : PageModel
{
    private readonly ILearningService _learningService;

    public QuestionBankModel(ILearningService learningService)
    {
        _learningService = learningService;
    }

    public QuestionBankPageDto? Data { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Difficulty { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? QuestionType { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool IncludeInactive { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!await LoadAsync(cancellationToken))
            return RedirectToPage("/Learning/Index");

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync(
        int id,
        string questionType,
        string prompt,
        string? optionsText,
        string correctAnswer,
        string? explanation,
        string difficulty,
        string? topic,
        string? learningObjective,
        CancellationToken cancellationToken)
    {
        try
        {
            await _learningService.UpdateQuestionAsync(
                CurrentUserId(),
                new UpdateQuestionBankItemRequest
                {
                    Id = id,
                    QuestionType = questionType,
                    Prompt = prompt,
                    Options = SplitOptions(optionsText),
                    CorrectAnswer = correctAnswer,
                    Explanation = explanation,
                    Difficulty = difficulty,
                    Topic = topic,
                    LearningObjective = learningObjective
                },
                cancellationToken);
            TempData["Success"] = "Đã cập nhật câu hỏi trong ngân hàng đề.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        try
        {
            await _learningService.SetQuestionActiveAsync(
                CurrentUserId(),
                id,
                isActive,
                cancellationToken);
            TempData["Success"] = isActive
                ? "Đã khôi phục câu hỏi vào ngân hàng đề."
                : "Đã lưu trữ câu hỏi. Các bộ ôn tập cũ vẫn giữ nguyên nội dung.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToPage(new
        {
            Search,
            Difficulty,
            QuestionType,
            IncludeInactive
        });
    }

    private async Task<bool> LoadAsync(CancellationToken cancellationToken)
    {
        Data = await _learningService.GetQuestionBankAsync(
            CurrentUserId(),
            Search,
            Difficulty,
            QuestionType,
            IncludeInactive,
            cancellationToken);

        if (Data is not null)
            return true;

        TempData["Error"] = "Bạn không có quyền quản lý ngân hàng câu hỏi.";
        return false;
    }

    private int CurrentUserId()
    {
        return HttpContext.Session.GetInt32("UserId")
            ?? throw new InvalidOperationException("Phiên đăng nhập không còn hợp lệ.");
    }

    private static IReadOnlyCollection<string> SplitOptions(string? optionsText)
    {
        return string.IsNullOrWhiteSpace(optionsText)
            ? []
            : optionsText.Split(
                    ['\r', '\n'],
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
    }
}
