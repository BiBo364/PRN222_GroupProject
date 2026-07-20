using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.Learning;

[RequireLogin]
public class DetailsModel : PageModel
{
    private readonly ILearningService _learningService;

    public DetailsModel(ILearningService learningService)
    {
        _learningService = learningService;
    }

    public LearningSetDetailDto? LearningSet { get; private set; }
    public PaginationSlice<LearningSetQuestionDto> QuestionsPagination { get; private set; } =
        PaginationHelper.Paginate<LearningSetQuestionDto>([], 1, 10);

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        LearningSet = await _learningService.GetLearningSetAsync(
            CurrentUserId(),
            id,
            cancellationToken);

        if (LearningSet is not null)
        {
            QuestionsPagination = PaginationHelper.Paginate(
                LearningSet.Questions,
                PageNumber,
                10);
            PageNumber = QuestionsPagination.CurrentPage;
            return Page();
        }

        TempData["Error"] = "Không tìm thấy bộ ôn tập hoặc bạn không có quyền truy cập.";
        return RedirectToPage("/Learning/Index");
    }

    public async Task<IActionResult> OnPostPublishAsync(
        int id,
        bool isPublished,
        CancellationToken cancellationToken)
    {
        try
        {
            await _learningService.SetPublishedAsync(
                CurrentUserId(),
                id,
                isPublished,
                cancellationToken);
            TempData["Success"] = isPublished
                ? "Đã phát hành bộ ôn tập cho sinh viên."
                : "Đã chuyển bộ ôn tập về trạng thái bản nháp.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToPage(new { id, pageNumber = PageNumber });
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _learningService.DeleteLearningSetAsync(CurrentUserId(), id, cancellationToken);
            TempData["Success"] = "Đã chuyển Quiz vào thùng rác. Bạn có thể khôi phục Quiz nếu xóa nhầm.";
            return RedirectToPage("/Learning/Index");
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToPage(new { id, pageNumber = PageNumber });
        }
    }

    private int CurrentUserId()
    {
        return HttpContext.Session.GetInt32("UserId")
            ?? throw new InvalidOperationException("Phiên đăng nhập không còn hợp lệ.");
    }
}
