using Assignmet1_Presentation.Filters;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.Audit;

[RequireAuditRole]
public class IndexModel : PageModel
{
    private readonly IAuditLogService _auditLogService;

    public IndexModel(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    public AuditLogPageDto Data { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Action { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Data = await _auditLogService.GetPageAsync(
            HttpContext.Session.GetInt32("UserId")!.Value,
            HttpContext.Session.GetInt32("RoleId")!.Value,
            new AuditLogQuery
            {
                Category = Category,
                Action = Action,
                Search = Search,
                FromDate = FromDate,
                ToDate = ToDate,
                Page = PageNumber,
                PageSize = 25
            },
            cancellationToken);
    }
}
