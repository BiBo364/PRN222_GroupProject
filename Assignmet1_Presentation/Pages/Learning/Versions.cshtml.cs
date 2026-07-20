using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.Learning;

[RequireLecturer]
public class VersionsModel : PageModel
{
    private readonly ILearningService _learningService;

    public VersionsModel(ILearningService learningService)
    {
        _learningService = learningService;
    }

    public QuizVersionHistoryDto? History { get; private set; }
    public PaginationSlice<QuizVersionDto> VersionsPagination { get; private set; } =
        PaginationHelper.Paginate<QuizVersionDto>([], 1, 10);

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        History = await _learningService.GetQuizVersionHistoryAsync(
            CurrentUserId(),
            id,
            cancellationToken);
        if (History is not null)
        {
            VersionsPagination = PaginationHelper.Paginate(
                History.Versions,
                PageNumber,
                10);
            PageNumber = VersionsPagination.CurrentPage;
            return Page();
        }

        TempData["Error"] = "Không tìm thấy Quiz hoặc bạn không có quyền xem lịch sử phiên bản.";
        return RedirectToPage("/Learning/Index");
    }

    public async Task<IActionResult> OnPostRestoreAsync(
        int id,
        int versionId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _learningService.RestoreQuizVersionAsync(
                CurrentUserId(),
                id,
                versionId,
                cancellationToken);
            TempData["Success"] =
                "Đã khôi phục phiên bản Quiz. Quiz được chuyển về bản nháp để bạn kiểm tra trước khi phát hành.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToPage(new { id, pageNumber = PageNumber });
    }

    private int CurrentUserId()
    {
        return HttpContext.Session.GetInt32("UserId")
            ?? throw new InvalidOperationException("Phiên đăng nhập không còn hợp lệ.");
    }
}
