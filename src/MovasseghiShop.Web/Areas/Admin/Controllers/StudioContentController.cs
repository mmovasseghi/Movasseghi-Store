using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class StudioContentController(
    IStudioContentService content,
    ICompetitorAnalysisService competitor,
    IAuthorityChecklistService authority,
    IGrowthReportService reports) : Controller
{
    public async Task<IActionResult> Home()
    {
        var sections = await content.GetAllHomeSectionsAsync();
        return View(sections);
    }

    public async Task<IActionResult> EditHome(int id)
    {
        var section = await content.GetHomeSectionAsync(id);
        if (section == null) return NotFound();
        return View(section);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditHome(HomeSection model)
    {
        var section = await content.GetHomeSectionAsync(model.Id);
        if (section == null) return NotFound();
        section.Title = model.Title;
        section.Subtitle = model.Subtitle;
        section.BodyHtml = model.BodyHtml;
        section.ConfigJson = model.ConfigJson;
        section.IsActive = model.IsActive;
        section.SortOrder = model.SortOrder;
        await content.SaveHomeSectionAsync(section);
        TempData["Success"] = "بخش ذخیره شد.";
        return RedirectToAction(nameof(Home));
    }

    public async Task<IActionResult> Nav()
    {
        ViewBag.Zones = (await content.GetAllNavItemsAsync()).GroupBy(n => n.Zone).Select(g => g.Key).ToList();
        return View(await content.GetAllNavItemsAsync());
    }

    public IActionResult CreateNav(string? zone) => View("EditNav", new NavItem { Zone = zone ?? "header", IsActive = true, SortOrder = 10 });

    public async Task<IActionResult> EditNav(int id)
    {
        var items = await content.GetAllNavItemsAsync();
        var item = items.FirstOrDefault(n => n.Id == id);
        if (item == null) return NotFound();
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditNav(NavItem model)
    {
        if (model.Id == 0)
            await content.SaveNavItemAsync(model);
        else
        {
            var items = await content.GetAllNavItemsAsync();
            var item = items.FirstOrDefault(n => n.Id == model.Id);
            if (item == null) return NotFound();
            item.Zone = model.Zone;
            item.Label = model.Label;
            item.Url = model.Url;
            item.Icon = model.Icon;
            item.SortOrder = model.SortOrder;
            item.IsActive = model.IsActive;
            await content.SaveNavItemAsync(item);
        }
        TempData["Success"] = "منو ذخیره شد.";
        return RedirectToAction(nameof(Nav));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteNav(int id)
    {
        await content.DeleteNavItemAsync(id);
        TempData["Success"] = "آیتم حذف شد.";
        return RedirectToAction(nameof(Nav));
    }

    public async Task<IActionResult> Atoms() => View(await content.GetAllAtomsAsync());

    public async Task<IActionResult> EditAtom(int id)
    {
        var atom = await content.GetAtomByIdAsync(id);
        if (atom == null) return NotFound();
        return View(atom);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAtom(
        ContentAtom model,
        string? promoSloganFull,
        string? promoSloganShort,
        string? promoChips)
    {
        var atom = await content.GetAtomByIdAsync(model.Id);
        if (atom == null) return NotFound();
        atom.Title = model.Title;
        atom.CtaText = model.CtaText;
        atom.CtaUrl = model.CtaUrl;
        atom.IsActive = model.IsActive;

        if (atom.Key == "promo-bar")
        {
            var chips = (promoChips ?? "")
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            atom.Body = System.Text.Json.JsonSerializer.Serialize(new
            {
                sloganFull = promoSloganFull?.Trim() ?? "",
                sloganShort = promoSloganShort?.Trim() ?? "",
                chips
            }, new System.Text.Json.JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
        else
            atom.Body = model.Body;

        await content.SaveAtomAsync(atom);
        TempData["Success"] = "متن کوتاه ذخیره شد.";
        return RedirectToAction(nameof(Atoms));
    }

    public async Task<IActionResult> Pages() => View(await content.GetAllPagesAsync());

    public async Task<IActionResult> EditPage(string key)
    {
        var page = await content.GetPageByKeyAsync(key);
        if (page == null) return NotFound();
        return View(page);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPage(CmsPage model)
    {
        var page = await content.GetPageByKeyAsync(model.Key);
        if (page == null) return NotFound();
        page.Title = model.Title;
        page.Content = model.Content;
        page.MetaTitle = model.MetaTitle;
        page.MetaDescription = model.MetaDescription;
        await content.SavePageAsync(page);
        TempData["Success"] = "صفحه ذخیره شد.";
        return RedirectToAction(nameof(Pages));
    }

    public async Task<IActionResult> Reports() => View(await reports.GetReportsAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateReport()
    {
        await reports.GenerateWeeklyReportAsync();
        TempData["Success"] = "گزارش هفتگی ساخته شد.";
        return RedirectToAction(nameof(Reports));
    }

    public async Task<IActionResult> ReportDetail(int id)
    {
        var report = await reports.GetReportAsync(id);
        if (report == null) return NotFound();
        return View(report);
    }

    public async Task<IActionResult> Authority() => View(await authority.GetAllAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAuthority(int id, bool isDone)
    {
        await authority.ToggleAsync(id, isDone);
        return RedirectToAction(nameof(Authority));
    }

    public async Task<IActionResult> Competitors(string? keyword)
    {
        ViewBag.Keyword = keyword;
        if (string.IsNullOrWhiteSpace(keyword))
            return View(new List<CompetitorSnapshot>());
        return View(await competitor.GetLatestForKeywordAsync(keyword));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncCompetitors()
    {
        var count = await competitor.SyncAllPillarsAsync();
        TempData["Success"] = count > 0 ? $"همگام‌سازی رقبا: {count} نتیجه" : "SerpAPI فعال نیست یا نتیجه‌ای نیامد.";
        return RedirectToAction(nameof(Competitors));
    }
}
