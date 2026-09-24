using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoriesController(
    ApplicationDbContext db,
    ISeoEngineService seo,
    IWebHostEnvironment env,
    ISeoMaintenanceCoordinator seoMaintenance,
    Microsoft.Extensions.Options.IOptions<SeoMaintenanceOptions> seoMaintenanceOptions) : Controller
{
    public async Task<IActionResult> Index()
    {
        var cats = await db.Categories
            .Include(c => c.Products)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();

        var profiles = await db.SeoProfiles
            .Where(p => p.EntityType == "category")
            .ToListAsync();

        var seoScores = new Dictionary<int, int>();
        var seoReady = new Dictionary<int, bool>();
        var seoAudited = new Dictionary<int, bool>();
        foreach (var c in cats)
        {
            var profile = profiles.FirstOrDefault(p => p.EntityId == c.Id);
            seoScores[c.Id] = profile is null ? 0 : SeoUltimateAnalyzer.ResolveDisplayOverallFromProfile(profile);
            seoReady[c.Id] = profile?.IsPublishReady ?? false;
            seoAudited[c.Id] = profile?.LastAuditedAt != null;
        }
        ViewBag.SeoScores = seoScores;
        ViewBag.SeoReady = seoReady;
        ViewBag.SeoAudited = seoAudited;
        return View(cats);
    }

    public IActionResult Create() => View(new Category());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            TempData["Error"] = "نام دسته الزامی است.";
            return View(model);
        }

        model.Slug = string.IsNullOrWhiteSpace(model.Slug) ? SlugHelper.Generate(model.Name) : SlugHelper.Generate(model.Slug);
        model.IsActive = true;
        db.Categories.Add(model);
        await db.SaveChangesAsync();
        await seo.AutoFixCategoryAsync(model.Id);
        TempData["Success"] = "دسته ایجاد شد — SEO خودکار اعمال شد.";
        return RedirectToAction(nameof(Edit), new { id = model.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var cat = await db.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
        if (cat == null) return NotFound();

        ViewBag.SeoScore = await seo.AuditCategoryAsync(id);
        var profile = await seo.GetOrCreateCategoryProfileAsync(cat);
        ViewBag.FocusKeyword = profile.FocusKeyword;
        ViewBag.AiSummary = profile.AiSummary;
        return View(cat);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Edit(
        int id,
        Category model,
        string? focusKeyword,
        string? aiSummary,
        [FromForm(Name = "Description")] string? descriptionHtml,
        IFormFile? coverImage)
    {
        var cat = await db.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
        if (cat == null) return NotFound();

        cat.Name = model.Name.Trim();
        cat.Slug = string.IsNullOrWhiteSpace(model.Slug) ? SlugHelper.Generate(cat.Name) : SlugHelper.Generate(model.Slug);
        if (descriptionHtml != null || Request.Form.ContainsKey("Description"))
            cat.Description = descriptionHtml?.Trim();
        else if (model.Description != null)
            cat.Description = model.Description.Trim();
        cat.MetaTitle = model.MetaTitle?.Trim();
        cat.MetaDescription = model.MetaDescription?.Trim();
        cat.SortOrder = model.SortOrder;
        cat.IsActive = model.IsActive;

        if (coverImage is { Length: > 0 })
            cat.ImageUrl = await SaveCategoryImageAsync(id, coverImage);

        var profile = await seo.GetOrCreateCategoryProfileAsync(cat);
        if (!string.IsNullOrWhiteSpace(focusKeyword))
            profile.FocusKeyword = focusKeyword.Trim();
        if (Request.Form.ContainsKey("aiSummary") || Request.Form.ContainsKey("Description"))
            SeoProfileEditorialLocks.OnManualCategoryLeadSaved(profile, aiSummary, Request.Form.ContainsKey("aiSummary"));
        else if (!string.IsNullOrWhiteSpace(aiSummary))
            profile.AiSummary = aiSummary.Trim();
        profile.MetaTitle = cat.MetaTitle;
        profile.MetaDescription = cat.MetaDescription;

        await db.SaveChangesAsync();

        if (seoMaintenanceOptions.Value.AutoFixCategoryOnSave)
            await seoMaintenance.RequestCategoryFixAsync(id);

        var score = await seo.AuditCategoryAsync(id);

        TempData["Success"] = score.IsPublishReady
            ? $"دسته ذخیره شد — امتیاز SEO {score.TotalScore}/100 · {SeoPublishGateLabels.Verified}"
            : $"دسته ذخیره شد — امتیاز SEO {score.TotalScore}/100 ({SeoPublishGateLabels.GateHint})";
        if (seoMaintenanceOptions.Value.AutoFixCategoryOnSave)
            TempData["Success"] += " Auto-Fix دسته در پس‌زمینه در صف است.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    async Task<string> SaveCategoryImageAsync(int categoryId, IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            ext = ".jpg";

        var dir = Path.Combine(env.WebRootPath, "images", "categories");
        Directory.CreateDirectory(dir);

        foreach (var old in Directory.GetFiles(dir, $"{categoryId:D3}-*"))
            System.IO.File.Delete(old);

        var fileName = $"{categoryId:D3}-cover{ext}";
        var physical = Path.Combine(dir, fileName);
        await using (var stream = System.IO.File.Create(physical))
            await file.CopyToAsync(stream);

        return $"/images/categories/{fileName}";
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await db.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
        if (cat == null) return NotFound();
        if (cat.Products.Count > 0)
        {
            TempData["Error"] = $"این دسته {cat.Products.Count} محصول دارد — ابتدا محصولات را منتقل کنید.";
            return RedirectToAction(nameof(Index));
        }
        db.Categories.Remove(cat);
        await db.SaveChangesAsync();
        TempData["Success"] = "دسته حذف شد.";
        return RedirectToAction(nameof(Index));
    }
}
