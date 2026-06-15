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

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.GetInt32("UserId") is not null)
        {
            if (IsForcedPasswordChange())
                return RedirectToAction(nameof(ChangePassword));

            return RedirectToAction("Index", "Home");
        }

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
            ModelState.AddModelError(string.Empty, "Ten dang nhap hoac mat khau khong dung.");
            return View(model);
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
            TempData["Warning"] = "Tai khoan cua ban dang dung mat khau mac dinh. Vui long dat mat khau moi truoc khi tiep tuc.";
            return RedirectToAction(nameof(ChangePassword));
        }

        HttpContext.Session.Remove("ForcePasswordChange");

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [RequireLogin]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel
        {
            IsForcedChange = IsForcedPasswordChange()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireLogin]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        model.IsForcedChange = IsForcedPasswordChange();

        if (!ModelState.IsValid)
            return View(model);

        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null)
            return RedirectToAction(nameof(Login));

        if (!model.IsForcedChange && string.IsNullOrWhiteSpace(model.CurrentPassword))
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Vui long nhap mat khau hien tai.");
            return View(model);
        }

        var (success, error) = model.IsForcedChange
            ? await _userServices.ChangeDefaultPasswordAsync(userId.Value, model.NewPassword)
            : await _userServices.ChangePasswordAsync(userId.Value, model.CurrentPassword, model.NewPassword);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Doi mat khau that bai.");
            return View(model);
        }

        if (model.IsForcedChange)
        {
            HttpContext.Session.Remove("ForcePasswordChange");
            TempData["Success"] = "Doi mat khau thanh cong! Ban co the tiep tuc su dung he thong.";
            return RedirectToAction("Index", "Home");
        }

        TempData["Success"] = "Doi mat khau thanh cong! Vui long dang nhap lai.";
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    private bool IsForcedPasswordChange()
        => string.Equals(HttpContext.Session.GetString("ForcePasswordChange"), "true", StringComparison.OrdinalIgnoreCase);
}
