using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.Admin.Benchmark;

[RequireAdmin]
public class IndexModel : PageModel
{
    private readonly IBenchmarkService _benchmarkService;

    public IndexModel(IBenchmarkService benchmarkService)
    {
        _benchmarkService = benchmarkService;
    }

    public BenchmarkIndexViewModel ViewModel { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var runsDto = await _benchmarkService.GetBenchmarkRunsAsync();
        var runs = runsDto.Select(ViewModelMapper.ToViewModel).ToList();

        var completedRuns = runs.Where(r => string.Equals(r.Status, "completed", StringComparison.OrdinalIgnoreCase)).ToList();
        var latestCompleted = completedRuns.FirstOrDefault(r => r.Summary != null);

        var totalQuestions = await GetTotalQuestionsCountAsync();

        ViewModel = new BenchmarkIndexViewModel
        {
            Runs = runs,
            TotalRuns = runs.Count,
            CompletedRuns = completedRuns.Count,
            TotalQuestions = totalQuestions,
            LatestAvgFaithfulness = latestCompleted?.Summary?.AvgFaithfulness,
            LatestAvgAnswerCorrectness = latestCompleted?.Summary?.AvgAnswerCorrectness,
            LatestAvgLatencyMs = latestCompleted?.Summary?.AvgLatencyMs
        };

        return Page();
    }

    private async Task<int> GetTotalQuestionsCountAsync()
    {
        try
        {
            var questions = await _benchmarkService.GetTestQuestionsAsync();
            return questions.Count;
        }
        catch
        {
            return 0;
        }
    }
}
