using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Assignment1_Service.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Assignmet1_Presentation.Pages.Admin;

// @page "/Admin/Settings"
[RequireAdmin]
public class SettingsModel : PageModel
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ChunkingSettings _defaultSettings;

    public SettingsModel(
        IDocumentRepository documentRepository,
        IOptions<ChunkingSettings> defaultSettings)
    {
        _documentRepository = documentRepository;
        _defaultSettings = defaultSettings.Value;
    }

    [BindProperty]
    public ChunkingSettingsViewModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var config = await _documentRepository.GetFirstChunkingConfigAsync();
        Input.ChunkSize = config?.ChunkSize ?? _defaultSettings.MaxWordsPerChunk;
        Input.ChunkOverlap = config?.ChunkOverlap ?? Math.Min(
            _defaultSettings.OverlapWords,
            Input.ChunkSize - 1);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var existing = await _documentRepository.GetFirstChunkingConfigAsync();
        await _documentRepository.UpsertChunkingConfigAsync(
            name: existing?.Name ?? "admin-config",
            strategy: existing?.Strategy ?? "fixed",
            chunkSize: Input.ChunkSize,
            chunkOverlap: Input.ChunkOverlap,
            description: "Cấu hình chunk size và chunk overlap được quản trị viên cập nhật trong trang Cài đặt.");

        TempData["Success"] = "Đã lưu cấu hình chunk size và chunk overlap. Thiết lập sẽ áp dụng khi index tài liệu mới.";
        return RedirectToPage();
    }
}
