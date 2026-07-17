using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.Account;

// @page "/Account/ChangePassword"
[RequireLogin]
public class ChangePasswordModel : PageModel
{
    private readonly IUserServices _userServices;

    public ChangePasswordModel(IUserServices userServices)
    {
        _userServices = userServices;
    }

    [BindProperty]
    public ChangePasswordViewModel Input { get; set; } = new();

    public IActionResult OnGet()
    {
        Input = new ChangePasswordViewModel
        {
            IsForcedChange = IsForcedPasswordChange()
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Input.IsForcedChange = IsForcedPasswordChange();

        if (!ModelState.IsValid)
            return Page();

        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null)
            return RedirectToPage("/Account/Login");

        if (!Input.IsForcedChange && string.IsNullOrWhiteSpace(Input.CurrentPassword))
        {
            ModelState.AddModelError(nameof(Input.CurrentPassword), "Vui lòng nhập mật khẩu hiện tại.");
            return Page();
        }

        var (success, error) = Input.IsForcedChange
            ? await _userServices.ChangeDefaultPasswordAsync(userId.Value, Input.NewPassword)
            : await _userServices.ChangePasswordAsync(userId.Value, Input.CurrentPassword, Input.NewPassword);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Đổi mật khẩu thất bại.");
            return Page();
        }

        if (Input.IsForcedChange)
        {
            HttpContext.Session.Remove("ForcePasswordChange");
            TempData["Success"] = "Đổi mật khẩu thành công. Bạn có thể tiếp tục sử dụng hệ thống.";
            return RedirectToPage("/Home/Index");
        }

        TempData["Success"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
        HttpContext.Session.Clear();
        return RedirectToPage("/Account/Login");
    }

    private bool IsForcedPasswordChange()
        => string.Equals(HttpContext.Session.GetString("ForcePasswordChange"), "true", StringComparison.OrdinalIgnoreCase);
}
