using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.Admin.Benchmark;

[RequireAdmin]
public class RunModel : PageModel
{
    private readonly IBenchmarkService _benchmarkService;
    private readonly ISubjectService _subjectService;

    public RunModel(IBenchmarkService benchmarkService, ISubjectService subjectService)
    {
        _benchmarkService = benchmarkService;
        _subjectService = subjectService;
    }

    [BindProperty]
    public BenchmarkRunFormViewModel Form { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadFormOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Form.Name))
        {
            TempData["Error"] = "Vui long nhap ten dot danh gia.";
            await LoadFormOptionsAsync();
            return Page();
        }

        if (!Form.SubjectId.HasValue)
        {
            TempData["Error"] = "Vui long chon mon hoc de danh gia.";
            await LoadFormOptionsAsync();
            return Page();
        }

        // Validate that the subject has test questions before running
        var questions = await _benchmarkService.GetTestQuestionsAsync(Form.SubjectId.Value);
        if (questions.Count == 0)
        {
            TempData["Error"] = "Mon hoc duoc chon khong co cau hoi test nao. Vui long them cau hoi truoc.";
            await LoadFormOptionsAsync();
            return Page();
        }

        var runDto = new CreateBenchmarkRunDto
        {
            Name = Form.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(Form.Description) ? null : Form.Description.Trim(),
            SubjectId = Form.SubjectId,
            EmbeddingModelId = Form.EmbeddingModelId,
            ChunkingConfigId = Form.ChunkingConfigId,
            TopK = Form.TopK > 0 ? Form.TopK : 4,
            UseReranker = Form.UseReranker
        };

        try
        {
            var runId = await _benchmarkService.RunBenchmarkAsync(runDto);
            TempData["Success"] = "Chay dot danh gia benchmark hoan tat thanh cong!";
            return RedirectToPage("/Admin/Benchmark/Detail", new { id = runId });
        }
        catch (Exception ex)
        {
            var detail = ex.InnerException != null ? $" -> {ex.InnerException.Message}" : "";
            if (ex.InnerException?.InnerException != null)
            {
                detail += $" -> {ex.InnerException.InnerException.Message}";
            }
            TempData["Error"] = $"Loi khi chay benchmark: {ex.Message}{detail}";
            await LoadFormOptionsAsync();
            return Page();
        }
    }

    private async Task LoadFormOptionsAsync()
    {
        var subjectsDto = await _subjectService.GetSubjectsAsync();
        var allQuestions = await _benchmarkService.GetTestQuestionsAsync();
        var questionCountBySubject = allQuestions
            .Where(q => q.SubjectId.HasValue)
            .GroupBy(q => q.SubjectId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var subjects = subjectsDto.Select(s =>
        {
            var vm = ViewModelMapper.ToViewModel(s);
            var count = questionCountBySubject.GetValueOrDefault(s.Id, 0);
            vm.Name = $"{s.Code} - {s.Name} ({count} cau hoi test)";
            return vm;
        }).ToList();

        var embeddingDto = await _benchmarkService.GetEmbeddingModelOptionsAsync();
        var chunkingDto = await _benchmarkService.GetChunkingConfigOptionsAsync();

        Form.Subjects = subjects;
        Form.EmbeddingModels = embeddingDto.Select(ViewModelMapper.ToViewModel).ToList();
        Form.ChunkingConfigs = chunkingDto.Select(ViewModelMapper.ToViewModel).ToList();

        if (string.IsNullOrWhiteSpace(Form.Name))
        {
            Form.Name = $"Dot danh gia - {DateTime.Now:dd/MM/yyyy HH:mm}";
        }
    }
}
