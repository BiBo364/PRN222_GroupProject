using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Assignmet1_Presentation.Controllers;

public class AccountController : Controller
{
    private readonly IUserServices _userServices;

    public AccountController(IUserServices userServices)
    {
        _userServices = userServices;
    }

    // ─────────────────────────────────────────────────────────────────
    // Đăng nhập / Đăng xuất
    // ─────────────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.GetInt32("UserId") is not null)
            return RedirectToAction("Index", "Home");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var user = await _userServices.LoginAsync(model.Username, model.Password);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
            return View(model);
        }

        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetString("FullName", user.FullName ?? user.Username);
        HttpContext.Session.SetInt32("RoleId", user.RoleId);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    // ─────────────────────────────────────────────────────────────────
    // Đổi mật khẩu
    // ─────────────────────────────────────────────────────────────────

    [HttpGet]
    [RequireLogin]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireLogin]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null)
            return RedirectToAction(nameof(Login));

        var (success, error) = await _userServices.ChangePasswordAsync(
            userId.Value, model.CurrentPassword, model.NewPassword);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Đổi mật khẩu thất bại.");
            return View(model);
        }

        TempData["Success"] = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại.";
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }
}

