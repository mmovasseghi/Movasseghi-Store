using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MovasseghiShop.Web.Data;

namespace MovasseghiShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class AuthController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager) : Controller
{
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl) => View(model: returnUrl);

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password, string? returnUrl)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user == null || !await userManager.IsInRoleAsync(user, "Admin"))
        {
            TempData["Error"] = "نام کاربری یا رمز عبور اشتباه است.";
            return View(model: returnUrl);
        }

        var result = await signInManager.PasswordSignInAsync(user, password, true, false);
        if (!result.Succeeded)
        {
            TempData["Error"] = "نام کاربری یا رمز عبور اشتباه است.";
            return View(model: returnUrl);
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Dashboard");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }
}
