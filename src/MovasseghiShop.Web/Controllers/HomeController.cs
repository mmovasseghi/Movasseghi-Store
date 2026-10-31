using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.OutputCaching;
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
    IHomeStoryService homeStories,
    IConfiguration config,
    IWebHostEnvironment env,
    IFileVersionProvider fileVersions,
    ResponsiveImageService responsiveImages) : Controller
{
    [OutputCache(PolicyName = "HomePage")]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = SiteKeywordStrategy.SiteTitle;
        ViewData["MetaDescription"] = SiteKeywordStrategy.SiteDescription;
        ViewData["MetaKeywords"] = null;
        var homeUrl = StorefrontUrls.PublicBase(HttpContext, config) + AppPath.H("/", HttpContext);
        ViewData["CanonicalUrl"] = homeUrl;
        ViewData["OgTitle"] = SiteKeywordStrategy.SiteTitle;
        ViewData["OgDescription"] = SiteKeywordStrategy.SiteDescription;
        ViewData["OgType"] = "website";
        ViewData["OgUrl"] = homeUrl;
        ViewData["OgImage"] = AppPath.Absolute("/images/brand/originallogo.png", HttpContext);

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
        ViewBag.CategoryTypes = types.Select(t => (t.Type, t.Count)).Take(12).ToList();

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

        // Match the view: skip products without an available image within the
        // same four hero candidates, otherwise the preload targets the wrong URL.
        var lcpProduct = featured.Take(4).FirstOrDefault(p =>
            ProductImageHelper.GetDisplayUrl(ProductImageHelper.GetPrimary(p.Images), env) != null);
        if (lcpProduct is not null)
        {
            var lcpPath = ProductImageHelper.GetLcpDisplayUrl(ProductImageHelper.GetPrimary(lcpProduct.Images), env);
            if (!string.IsNullOrWhiteSpace(lcpPath))
            {
                var lcpUrl = fileVersions.AddFileVersionToPath(Request.PathBase, AppPath.H(lcpPath, HttpContext));
                ViewData["LcpPreload"] = lcpUrl;
                var sources = responsiveImages.Get(lcpPath, HttpContext);
                ViewData["LcpSrcSet"] = sources?.SrcSet;
                // Link response headers cannot reliably choose a responsive image.
                // The early HTML preload and img share srcset/sizes instead.
                if (sources is null) Response.Headers.Append("Link", $"<{lcpUrl}>; rel=preload; as=image; fetchpriority=high");
            }
        }

        return View();
    }

    /// <summary>داده استوری برای fetch سمت کلاینت — HTML خانه سبک‌تر (GTmetrix/LCP).</summary>
    [HttpGet("/Home/Stories.json")]
    [OutputCache(PolicyName = "StaticContent")]
    public async Task<IActionResult> StoriesJson(CancellationToken ct)
    {
        var stories = await homeStories.GetStoriesForHomeAsync(ct);
        return Json(stories);
    }

    /// <summary>لینک‌های قدیمی /Search → کاتالوگ.</summary>
    public IActionResult Search(string? q)
        => RedirectToAction("Index", "Catalog", new ProductFilterQuery { Q = q });

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
