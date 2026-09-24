using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Controllers;

public class HomeController(
    ApplicationDbContext db,
    IProductCatalogService catalog,
    IStudioContentService studio,
    IHomeStoryService homeStories) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = SiteKeywordStrategy.SiteTitle;
        ViewData["MetaDescription"] = SiteKeywordStrategy.SiteDescription;
        ViewData["MetaKeywords"] = SiteKeywordStrategy.MetaKeywords;

        ViewBag.HomeSections = await studio.GetHomeSectionsAsync();
        var types = await catalog.GetProductTypesAsync();
        ViewBag.ProductTypes = types;
        ViewBag.Categories = await db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        var featured = await HomeFeaturedProductsSelector.LoadAsync(
            db.Products.AsNoTracking().Include(p => p.Images).Include(p => p.Variants));
        ViewBag.FeaturedProducts = featured;
        ViewBag.ProductCount = await db.Products.CountAsync(p => p.IsActive);

        var storyItems = await homeStories.GetStoriesForHomeAsync();
        ViewBag.StoryItems = storyItems;
        ViewBag.CategoryTypes = types.Select(t => (t.Type, t.Count)).ToList();

        var specialOffers = await HomeSpecialOffersSelector.LoadAsync(
            db.Products.AsNoTracking().Include(p => p.Images).Include(p => p.Variants));
        ViewBag.SpecialOffers = specialOffers;

        var wholesaleFromDb = HomeSectionConfigParser.ParseWholesaleSegments(
            ViewBag.HomeSections is Dictionary<string, HomeSection> hs && hs.TryGetValue("wholesale", out var ws) ? ws.ConfigJson : null);
        ViewBag.WholesaleSegments = wholesaleFromDb.Count > 0
            ? wholesaleFromDb.Select(s => (s.Title, s.Desc, s.Icon, s.Url)).ToList()
            : new List<(string Title, string Desc, string Icon, string Url)>
            {
                ("رستوران و فست‌فود", "ظروف غذای داغ، مایکروویوی و پذیرایی", "restaurant", "/Catalog?q=غذا"),
                ("کافه و قنادی", "لیوان، دسر و سرو نوشیدنی", "cafe", "/Catalog/ByType?type=" + Uri.EscapeDataString("لیوان")),
                ("کیترینگ و مهمانی", "حجم بالا · بسته‌بندی یکبار مصرف", "catering", "/Catalog"),
                ("فروشگاه و پخش", "خرید عمده از " + WholesaleRules.FormatMinAmount() + " تومان", "store", "/Catalog")
            };

        ViewBag.LatestBlogPosts = await db.BlogPosts.AsNoTracking()
            .Where(b => b.IsPublished)
            .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
            .Take(4)
            .ToListAsync();

        ViewBag.LatestNews = await db.NewsItems.AsNoTracking()
            .Where(n => n.IsPublished)
            .OrderByDescending(n => n.PublishedAt ?? n.CreatedAt)
            .Take(4)
            .ToListAsync();

        return View();
    }

    /// <summary>لینک‌های قدیمی /Search → کاتالوگ.</summary>
    public IActionResult Search(string? q)
        => RedirectToAction("Index", "Catalog", new ProductFilterQuery { Q = q });

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
