using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.Admin.Benchmark;

[RequireAdmin]
public class QuestionsModel : PageModel
{
    private readonly IBenchmarkService _benchmarkService;
    private readonly ISubjectService _subjectService;

    public QuestionsModel(IBenchmarkService benchmarkService, ISubjectService subjectService)
    {
        _benchmarkService = benchmarkService;
        _subjectService = subjectService;
    }

    public BenchmarkQuestionsViewModel ViewModel { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? subjectId)
    {
        await LoadViewModelAsync(subjectId);
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(TestQuestionFormViewModel form)
    {
        if (string.IsNullOrWhiteSpace(form.Question))
        {
            TempData["Error"] = "Noi dung cau hoi khong duoc de trong.";
            return RedirectToPage(new { subjectId = form.SubjectId });
        }
        if (string.IsNullOrWhiteSpace(form.GroundTruth))
        {
            TempData["Error"] = "Dap an chuan khong duoc de trong.";
            return RedirectToPage(new { subjectId = form.SubjectId });
        }
        if (!form.SubjectId.HasValue)
        {
            TempData["Error"] = "Vui long chon mon hoc.";
            return RedirectToPage(new { subjectId = form.SubjectId });
        }

        var dto = new CreateTestQuestionDto
        {
            SubjectId = form.SubjectId,
            Question = form.Question.Trim(),
            GroundTruth = form.GroundTruth.Trim(),
            GroundTruthChunks = string.IsNullOrWhiteSpace(form.GroundTruthChunks) ? null : form.GroundTruthChunks.Trim(),
            Difficulty = string.IsNullOrWhiteSpace(form.Difficulty) ? "medium" : form.Difficulty.Trim(),
            Category = string.IsNullOrWhiteSpace(form.Category) ? null : form.Category.Trim()
        };

        try
        {
            if (form.Id.HasValue && form.Id.Value > 0)
            {
                await _benchmarkService.UpdateTestQuestionAsync(form.Id.Value, dto);
                TempData["Success"] = "Cap nhat cau hoi kiem thu thanh cong!";
            }
            else
            {
                var username = HttpContext.Session.GetString("FullName") ?? HttpContext.Session.GetString("Username") ?? "Human";
                await _benchmarkService.CreateTestQuestionAsync(dto, username);
                TempData["Success"] = "Them cau hoi kiem thu moi thanh cong!";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Loi khi luu cau hoi: {ex.Message}";
        }

        return RedirectToPage(new { subjectId = form.SubjectId });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, int? filterSubjectId)
    {
        try
        {
            await _benchmarkService.DeleteTestQuestionAsync(id);
            TempData["Success"] = "Xoa cau hoi kiem thu thanh cong!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Loi khi xoa cau hoi: {ex.Message}";
        }

        return RedirectToPage(new { subjectId = filterSubjectId });
    }

    private async Task LoadViewModelAsync(int? subjectId)
    {
        var subjectsDto = await _subjectService.GetSubjectsAsync();
        var subjects = subjectsDto.Select(ViewModelMapper.ToViewModel).ToList();

        var questionsDto = await _benchmarkService.GetTestQuestionsAsync(subjectId);
        var questions = questionsDto.Select(ViewModelMapper.ToViewModel).ToList();

        ViewModel = new BenchmarkQuestionsViewModel
        {
            Questions = questions,
            Subjects = subjects,
            FilterSubjectId = subjectId,
            Form = new TestQuestionFormViewModel()
        };
    }
}
