using Assignmet1_Presentation.Filters;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.Learning;

[RequireLogin]
public class RecycleModel : PageModel
{
    private readonly ILearningService _learningService;

    public RecycleModel(ILearningService learningService)
    {
        _learningService = learningService;
    }

    public LearningRecycleBinDto? Data { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        Data = await _learningService.GetRecycleBinAsync(
            CurrentUserId(),
            cancellationToken);
        if (Data is not null)
            return Page();

        TempData["Error"] = "Chỉ giảng viên được phân công môn học mới có thể truy cập thùng rác Quiz.";
        return RedirectToPage("/Learning/Index");
    }

    public async Task<IActionResult> OnPostRestoreAsync(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _learningService.RestoreLearningSetAsync(
                CurrentUserId(),
                id,
                cancellationToken);
            TempData["Success"] = "Đã khôi phục Quiz về trạng thái bản nháp.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostPermanentlyDeleteAsync(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _learningService.PermanentlyDeleteLearningSetAsync(
                CurrentUserId(),
                id,
                cancellationToken);
            TempData["Success"] = "Đã xóa vĩnh viễn Quiz và lịch sử làm bài liên quan.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToPage();
    }

    private int CurrentUserId()
    {
        return HttpContext.Session.GetInt32("UserId")
            ?? throw new InvalidOperationException("Phiên đăng nhập không còn hợp lệ.");
    }
}
