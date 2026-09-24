using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UsersController(UserManager<ApplicationUser> userManager, ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q)
    {
        var query = userManager.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(u =>
                (u.FullName != null && u.FullName.Contains(term)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(term)) ||
                (u.UserName != null && u.UserName.Contains(term)) ||
                (u.Email != null && u.Email.Contains(term)) ||
                (u.CompanyName != null && u.CompanyName.Contains(term)));
        }

        var users = await query.OrderByDescending(u => u.CreatedAt).Take(300).ToListAsync();
        var roles = new Dictionary<string, IList<string>>();
        foreach (var u in users)
            roles[u.Id] = await userManager.GetRolesAsync(u);

        var orderCounts = await db.Orders
            .GroupBy(o => o.UserId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key ?? "", x => x.Count);

        ViewBag.Roles = roles;
        ViewBag.OrderCount = orderCounts;
        ViewBag.Search = q;
        ViewBag.TotalUsers = await userManager.Users.CountAsync();
        ViewBag.AdminCount = (await userManager.GetUsersInRoleAsync("Admin")).Count;
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string id, string role, bool grant)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (grant)
        {
            if (!await userManager.IsInRoleAsync(user, role))
                await userManager.AddToRoleAsync(user, role);
            TempData["Success"] = $"نقش {role} به {user.FullName ?? user.UserName} اضافه شد.";
        }
        else
        {
            if (user.Id == userManager.GetUserId(User) && role == "Admin")
            {
                TempData["Error"] = "نمی‌توانید نقش Admin خودتان را حذف کنید.";
                return RedirectToAction(nameof(Index));
            }
            await userManager.RemoveFromRoleAsync(user, role);
            TempData["Success"] = $"نقش {role} از {user.FullName ?? user.UserName} حذف شد.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (user.Id == userManager.GetUserId(User))
        {
            TempData["Error"] = "نمی‌توانید حساب خودتان را قفل کنید.";
            return RedirectToAction(nameof(Index));
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            await userManager.SetLockoutEndDateAsync(user, null);
            TempData["Success"] = $"حساب {user.FullName ?? user.UserName} فعال شد.";
        }
        else
        {
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            TempData["Success"] = $"حساب {user.FullName ?? user.UserName} قفل شد.";
        }

        return RedirectToAction(nameof(Index));
    }
}
