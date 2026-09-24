using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Controllers;

public class CatalogController(
    IProductCatalogService catalog,
    IStudioContentService studio,
    ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index([FromQuery] ProductFilterQuery filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var directSlug = await catalog.TryResolveDirectProductSlugAsync(filter.Q);
            if (directSlug != null)
                return RedirectToAction("Product", "Shop", new { slug = directSlug });
        }

        ViewBag.CatalogEmpty = await studio.GetAtomAsync("catalog-empty");
        ViewBag.CatalogSearchEmpty = await studio.GetAtomAsync("catalog-search-empty");
        var (items, facets, total) = await catalog.QueryAsync(filter);
        ViewBag.Query = filter;
        ViewBag.Facets = facets;
        ViewBag.Total = total;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)ProductCatalogService.PageSizeValue);

        if (total == 0 && !string.IsNullOrWhiteSpace(filter.Q))
            ViewBag.SimilarProducts = await catalog.FindSimilarProductsAsync(filter.Q);

        await ApplyCategorySeoAsync(filter);

        return View(items);
    }

    /// <summary>لینک ادمین «مشاهده» — با روت پیش‌فرض MVC پارامتر سوم <c>id</c> است نه slug.</summary>
    public IActionResult Category(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return RedirectToAction(nameof(Index));
        return RedirectToActionPermanent(nameof(Index), new ProductFilterQuery { Category = id });
    }

    /// <summary>
    /// محتوای سئوی دسته (Meta، توضیحات، FAQ، خلاصه GEO) که موتور Auto-Fix تولید
    /// می‌کند تا این‌جا بی‌استفاده می‌ماند. بدون رندر شدن، نه گوگل آن را می‌بیند
    /// و نه بازدیدکننده — یعنی کل کار سئوی دسته‌ها هدر می‌رفت.
    /// </summary>
    async Task ApplyCategorySeoAsync(ProductFilterQuery filter)
    {
        if (!CatalogLandingRules.ShouldShowCategoryLanding(filter)) return;

        var rawKey = filter.Category!.Trim();
        var categoryKey = Uri.UnescapeDataString(rawKey);
        var categories = await db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new { c.Id, c.Name, c.Slug, c.Description, c.MetaTitle, c.MetaDescription, c.ImageUrl })
            .ToListAsync();

        static bool CategoryMatches(string slug, string name, string key) =>
            string.Equals(slug, key, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, key, StringComparison.OrdinalIgnoreCase)
            || string.Equals(slug, key, StringComparison.Ordinal);

        var category = categories.FirstOrDefault(c =>
            CategoryMatches(c.Slug, c.Name, categoryKey)
            || CategoryMatches(c.Slug, c.Name, rawKey));

        if (category is null) return;

        var profile = await db.SeoProfiles
            .AsNoTracking()
            .Where(p => p.EntityType == "category" && p.EntityId == category.Id)
            .Select(p => new { p.MetaTitle, p.MetaDescription, p.AiSummary, p.FaqJson })
            .FirstOrDefaultAsync();

        var rawDesc = category.Description;
        var snippet = SeoStorefrontDisplay.GetCategoryFeaturedSnippet(rawDesc, profile?.AiSummary);
        var bodyHtml = !string.IsNullOrWhiteSpace(rawDesc)
            ? rawDesc
            : !string.IsNullOrWhiteSpace(profile?.AiSummary)
                ? $"<p>{System.Net.WebUtility.HtmlEncode(profile.AiSummary.Trim())}</p>"
                : null;
        var metaDesc = Pick(profile?.MetaDescription, category.MetaDescription);
        var landing = new CatalogCategoryLandingViewModel
        {
            Name = category.Name,
            Slug = category.Slug,
            ImageUrl = category.ImageUrl,
            DescriptionHtml = bodyHtml,
            FeaturedSnippet = snippet,
            HeroLead = SeoStorefrontDisplay.GetCategoryHeroLead(metaDesc, snippet, rawDesc),
            Faqs = SeoFaqStore.Parse(profile?.FaqJson)
        };

        ViewBag.CategoryLanding = landing;
        ViewBag.CategoryName = landing.Name;
        ViewBag.CategorySlug = landing.Slug;
        ViewBag.CategoryDescription = landing.DescriptionHtml;
        ViewBag.CategoryAiSummary = profile?.AiSummary;
        ViewBag.CategoryFaqs = landing.Faqs;

        var title = Pick(profile?.MetaTitle, category.MetaTitle);

        if (!string.IsNullOrWhiteSpace(title)) ViewData["Title"] = title;
        if (!string.IsNullOrWhiteSpace(metaDesc)) ViewData["MetaDescription"] = metaDesc;

        var canonical = $"{Request.Scheme}://{Request.Host}/Catalog?category={Uri.EscapeDataString(category.Slug)}";
        ViewData["CanonicalUrl"] = canonical;
        ViewData["OgTitle"] = title ?? category.Name;
        ViewData["OgDescription"] = metaDesc;
        ViewData["OgType"] = "website";
        ViewData["OgUrl"] = canonical;
        if (!string.IsNullOrWhiteSpace(category.ImageUrl))
            ViewData["OgImage"] = category.ImageUrl!.StartsWith("/")
                ? $"{Request.Scheme}://{Request.Host}{category.ImageUrl}"
                : category.ImageUrl;

        static string? Pick(params string?[] candidates) =>
            candidates.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));
    }

    [HttpGet]
    public async Task<IActionResult> Suggest(string q)
    {
        var exact = await catalog.SearchSuggestionsAsync(q);
        var similar = exact.Count == 0 && !string.IsNullOrWhiteSpace(q)
            ? await catalog.FindSimilarSuggestionsAsync(q, 5)
            : [];
        return Json(new { exact, similar, query = q?.Trim() });
    }

    public IActionResult ByType(string type)
        => RedirectToAction(nameof(Index), new ProductFilterQuery { ProductType = type });

    public IActionResult ByUseCase(string useCase)
        => RedirectToAction(nameof(Index), new ProductFilterQuery { UseCase = useCase });
}

public class InquiryController(ApplicationDbContext db, IAnalyticsTracker analytics) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int productId, int? variantId, string customerName, string phone, int? quantityCartons, string? notes)
    {
        var product = await db.Products.FindAsync(productId);
        if (product == null) return NotFound();

        db.PurchaseInquiries.Add(new PurchaseInquiry
        {
            ProductId = productId,
            VariantId = variantId,
            CustomerName = customerName.Trim(),
            Phone = MockOtpService.NormalizePhone(phone),
            QuantityCartons = quantityCartons,
            Notes = notes
        });
        await db.SaveChangesAsync();

        // در فروش عمده، استعلام یک تبدیل کامل است — نه یک تعامل ساده
        analytics.TrackEvent("submit_inquiry", HttpContext,
            label: product.Name,
            entityId: productId,
            entityType: "product",
            quantity: quantityCartons);

        TempData["InquirySuccess"] = true;
        return RedirectToAction("Product", "Shop", new { slug = product.Slug });
    }
}

/// <summary>
/// نقطه ثبت رویدادهای سمت کلاینت (کلیک تماس، جستجو، استفاده از فیلتر)
/// که سرور نمی‌تواند ببیند.
/// </summary>
[Route("api/analytics")]
public class AnalyticsApiController(IAnalyticsTracker analytics) : Controller
{
    // فقط رویدادهای شناخته‌شده پذیرفته می‌شوند تا کسی جدول را با داده بی‌ربط پر نکند
    static readonly HashSet<string> Allowed =
    [
        "click_phone", "click_whatsapp", "click_instagram", "click_telegram",
        "search", "filter_use", "view_faq", "copy_phone", "share_product"
    ];

    [HttpPost("track")]
    [IgnoreAntiforgeryToken]
    public IActionResult Track([FromBody] TrackRequest? body)
    {
        if (body is null || !Allowed.Contains(body.Name)) return BadRequest();

        analytics.TrackEvent(body.Name, HttpContext,
            label: body.Label,
            entityId: body.EntityId,
            entityType: body.EntityType);

        return NoContent();
    }

    public record TrackRequest(string Name, string? Label, int? EntityId, string? EntityType);
}
