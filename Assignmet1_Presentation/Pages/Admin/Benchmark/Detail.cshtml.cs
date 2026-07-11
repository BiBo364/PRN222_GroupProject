using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.Admin.Benchmark;

[RequireAdmin]
public class DetailModel : PageModel
{
    private readonly IBenchmarkService _benchmarkService;

    public DetailModel(IBenchmarkService benchmarkService)
    {
        _benchmarkService = benchmarkService;
    }

    public BenchmarkRunDetailViewModel ViewModel { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var runDto = await _benchmarkService.GetBenchmarkRunAsync(id);
        if (runDto == null)
        {
            TempData["Error"] = "Dot danh gia khong ton tai.";
            return RedirectToPage("/Admin/Benchmark/Index");
        }

        ViewModel = ViewModelMapper.ToViewModel(runDto);
        return Page();
    }
}
