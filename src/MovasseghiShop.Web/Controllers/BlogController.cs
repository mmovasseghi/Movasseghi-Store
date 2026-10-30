using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Controllers;

public class BlogController(ApplicationDbContext db) : Controller
{
    [OutputCache(PolicyName = "PublicPage")]
    public async Task<IActionResult> Index(int page = 1)
    {
        const int pageSize = 9;
        var ordered = db.BlogPosts.Where(b => b.IsPublished).OrderByDescending(b => b.PublishedAt ?? b.CreatedAt);
        var total = await ordered.CountAsync();
        var featured = page == 1 ? await ordered.FirstOrDefaultAsync() : null;
        var listQuery = featured != null ? ordered.Where(b => b.Id != featured.Id) : ordered;
        var listTotal = featured != null ? Math.Max(0, total - 1) : total;

        ViewBag.Page = page;
        ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(listTotal / (double)pageSize));
        ViewBag.Total = total;
        ViewBag.Featured = featured;
        return View(await listQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync());
    }

    [OutputCache(PolicyName = "PublicPage")]
    public async Task<IActionResult> Post(string slug)
    {
        var post = await db.BlogPosts.FirstOrDefaultAsync(b => b.Slug == slug && b.IsPublished);
        if (post == null) return NotFound();

        ViewBag.Related = await db.BlogPosts.AsNoTracking()
            .Where(b => b.IsPublished && b.Id != post.Id)
            .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
            .Take(4)
            .ToListAsync();

        var profile = await db.SeoProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.EntityType == "blog" && p.EntityId == post.Id);
        ViewBag.ArticleFaqs = profile?.FaqJson;

        return View(post);
    }
}

public class PageController(ApplicationDbContext db, IConfiguration config) : Controller
{
    [OutputCache(PolicyName = "PublicPage")]
    public async Task<IActionResult> About()
    {
        var page = await db.CmsPages.FirstOrDefaultAsync(p => p.Key == "about");
        if (page == null) return NotFound();
        return RenderCmsPageView(page);
    }

    [OutputCache(PolicyName = "PublicPage")]
    public Task<IActionResult> Wholesale() => RenderCmsPage("wholesale");

    [OutputCache(PolicyName = "PublicPage")]
    public Task<IActionResult> Pricing() => RenderCmsPage("pricing");

    [OutputCache(PolicyName = "PublicPage")]
    public Task<IActionResult> Amelon() => RenderCmsPage("amelon");

    [OutputCache(PolicyName = "PublicPage")]
    public async Task<IActionResult> Terms()
    {
        var page = await db.CmsPages.FirstOrDefaultAsync(p => p.Key == "terms");
        if (page == null) return NotFound();
        return RenderCmsPageView(page);
    }

    [OutputCache(PolicyName = "PublicPage")]
    public async Task<IActionResult> Privacy()
    {
        var page = await db.CmsPages.FirstOrDefaultAsync(p => p.Key == "privacy");
        if (page == null) return NotFound();
        return RenderCmsPageView(page);
    }

    [OutputCache(PolicyName = "PublicPage")]
    public async Task<IActionResult> Faq()
    {
        var items = await db.FaqItems.Where(f => f.IsActive).OrderBy(f => f.SortOrder).ToListAsync();
        return View(items);
    }

    [OutputCache(PolicyName = "PublicPage")]
    public IActionResult Contact()
    {
        ViewBag.Phone = config["SiteSettings:Phone"];
        ViewBag.Instagram = config["SiteSettings:Instagram"];
        ViewBag.Address = config["SiteSettings:Address"];
        var pageUrl = StorefrontUrls.PublicBase(HttpContext, config) + AppPath.H("/Page/Contact", HttpContext);
        ViewData["Title"] = "تماس با فروشگاه موثقی | مشاوره خرید عمده";
        ViewData["MetaDescription"] = "تماس با فروشگاه موثقی — مشاوره رایگان خرید عمده ظروف گیاهی آملون، آدرس و شبکه‌های اجتماعی.";
        ViewData["CanonicalUrl"] = pageUrl;
        ViewData["OgTitle"] = ViewData["Title"];
        ViewData["OgDescription"] = ViewData["MetaDescription"];
        ViewData["OgUrl"] = pageUrl;
        ViewData["OgImage"] = AppPath.Absolute("/images/brand/originallogo.png", HttpContext);
        return View();
    }

    async Task<IActionResult> RenderCmsPage(string key)
    {
        var page = await db.CmsPages.FirstOrDefaultAsync(p => p.Key == key);
        if (page == null) return NotFound();
        return RenderCmsPageView(page);
    }

    IActionResult RenderCmsPageView(CmsPage page)
    {
        ViewData["Title"] = page.MetaTitle ?? page.Title;
        ViewData["MetaDescription"] = page.MetaDescription;
        var path = Request.Path.Value ?? "/";
        var pageUrl = StorefrontUrls.PublicBase(HttpContext, config) + AppPath.H(path, HttpContext);
        ViewData["CanonicalUrl"] = pageUrl;
        ViewData["OgTitle"] = ViewData["Title"];
        ViewData["OgDescription"] = ViewData["MetaDescription"];
        ViewData["OgType"] = "website";
        ViewData["OgUrl"] = pageUrl;
        ViewData["OgImage"] = AppPath.Absolute("/images/brand/originallogo.png", HttpContext);
        return View("Pillar", page);
    }
}
