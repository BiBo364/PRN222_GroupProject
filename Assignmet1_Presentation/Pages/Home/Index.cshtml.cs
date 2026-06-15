using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.Home;


public class IndexModel : PageModel
{
    public int? UserId { get; private set; }
    public string? DisplayName { get; private set; }

    public void OnGet()
    {
        UserId = HttpContext.Session.GetInt32("UserId");
        DisplayName = HttpContext.Session.GetString("FullName");
    }
}
