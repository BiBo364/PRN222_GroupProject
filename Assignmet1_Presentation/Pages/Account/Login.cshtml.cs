using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.Account;

// @page "/Account/Login"
public class LoginModel : PageModel
{
    private readonly IUserServices _userServices;

    public LoginModel(IUserServices userServices)
    {
        _userServices = userServices;
    }

    [BindProperty]
    public LoginViewModel Input { get; set; } = new();

    public IActionResult OnGet(string? returnUrl = null)
    {
        if (HttpContext.Session.GetInt32("UserId") is not null)
        {
            if (IsForcedPasswordChange())
                return RedirectToPage("/Account/ChangePassword");

            return RedirectToPage("/Home/Index");
        }

        ViewData["ReturnUrl"] = returnUrl;
        Input = new LoginViewModel();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return Page();

        var user = await _userServices.LoginAsync(Input.Username, Input.Password);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
            return Page();
        }

        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetString("FullName", user.FullName ?? user.Username);
        HttpContext.Session.SetInt32("RoleId", user.RoleId);

        if (user.SubjectId.HasValue)
            HttpContext.Session.SetInt32("SubjectId", user.SubjectId.Value);
        else
            HttpContext.Session.Remove("SubjectId");

        if (user.RequirePasswordChange)
        {
            HttpContext.Session.SetString("ForcePasswordChange", "true");
            TempData["Warning"] = "Tài khoản của bạn đang sử dụng mật khẩu mặc định. Vui lòng đặt mật khẩu mới trước khi tiếp tục.";
            return RedirectToPage("/Account/ChangePassword");
        }

        HttpContext.Session.Remove("ForcePasswordChange");

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToPage("/Home/Index");
    }

    private bool IsForcedPasswordChange()
        => string.Equals(HttpContext.Session.GetString("ForcePasswordChange"), "true", StringComparison.OrdinalIgnoreCase);
}
