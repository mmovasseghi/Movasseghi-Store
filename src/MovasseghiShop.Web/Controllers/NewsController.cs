using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;

namespace MovasseghiShop.Web.Controllers;

public class NewsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(int page = 1)
    {
        const int pageSize = 9;
        var query = db.NewsItems.Where(n => n.IsPublished).OrderByDescending(n => n.PublishedAt ?? n.CreatedAt);
        var total = await query.CountAsync();
        ViewBag.Page = page;
        ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        ViewBag.Total = total;
        ViewBag.Breaking = await query.Where(n => n.IsBreaking).Take(3).ToListAsync();
        return View(await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync());
    }

    public async Task<IActionResult> Article(string slug)
    {
        var item = await db.NewsItems.FirstOrDefaultAsync(n => n.Slug == slug && n.IsPublished);
        if (item == null) return NotFound();

        ViewBag.Related = await db.NewsItems.AsNoTracking()
            .Where(n => n.IsPublished && n.Id != item.Id)
            .OrderByDescending(n => n.PublishedAt ?? n.CreatedAt)
            .Take(4)
            .ToListAsync();

        return View(item);
    }
}
