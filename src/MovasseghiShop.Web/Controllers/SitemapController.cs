using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;

namespace MovasseghiShop.Web.Controllers;

public class SitemapController(ApplicationDbContext db, IConfiguration config) : Controller
{
    [HttpGet("/sitemap.xml")]
    [OutputCache(PolicyName = "StaticContent")]
    public async Task<ContentResult> Index()
    {
        var baseUrl = GetBaseUrl();
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urls = new List<XElement>
        {
            SitemapUrl(ns, baseUrl + "/", DateTime.UtcNow, "daily", "1.0"),
            SitemapUrl(ns, baseUrl + "/Catalog", DateTime.UtcNow, "daily", "0.9"),
            SitemapUrl(ns, baseUrl + "/Page/Wholesale", DateTime.UtcNow, "weekly", "0.9"),
            SitemapUrl(ns, baseUrl + "/Page/Pricing", DateTime.UtcNow, "weekly", "0.9"),
            SitemapUrl(ns, baseUrl + "/Page/Amelon", DateTime.UtcNow, "weekly", "0.9"),
            SitemapUrl(ns, baseUrl + "/Page/About", DateTime.UtcNow, "monthly", "0.7"),
            SitemapUrl(ns, baseUrl + "/Page/Contact", DateTime.UtcNow, "monthly", "0.6"),
            SitemapUrl(ns, baseUrl + "/Page/Faq", DateTime.UtcNow, "monthly", "0.6"),
            SitemapUrl(ns, baseUrl + "/Blog", DateTime.UtcNow, "weekly", "0.7"),
            SitemapUrl(ns, baseUrl + "/News", DateTime.UtcNow, "weekly", "0.7"),
        };

        var categories = await db.Categories.AsNoTracking()
            .Where(c => c.IsActive && c.Slug != "")
            .Select(c => c.Slug)
            .ToListAsync();
        foreach (var slug in categories)
        {
            var loc = $"{baseUrl}/Catalog?category={Uri.EscapeDataString(slug)}";
            urls.Add(SitemapUrl(ns, loc, DateTime.UtcNow, "weekly", "0.85"));
        }

        var products = await db.Products.Where(p => p.IsActive).Select(p => new { p.Slug, p.UpdatedAt }).ToListAsync();
        foreach (var p in products)
            urls.Add(SitemapUrl(ns, $"{baseUrl}/Shop/Product/{p.Slug}", p.UpdatedAt, "weekly", "0.8"));

        var posts = await db.BlogPosts.Where(b => b.IsPublished).Select(b => new { b.Slug, b.UpdatedAt }).ToListAsync();
        foreach (var b in posts)
            urls.Add(SitemapUrl(ns, $"{baseUrl}/Blog/Post/{Uri.EscapeDataString(b.Slug)}", b.UpdatedAt, "monthly", "0.6"));

        var news = await db.NewsItems.Where(n => n.IsPublished).Select(n => new { n.Slug, n.UpdatedAt, n.PublishedAt }).ToListAsync();
        foreach (var n in news)
        {
            var last = n.UpdatedAt;
            if (n.PublishedAt.HasValue && n.PublishedAt.Value > last)
                last = n.PublishedAt.Value;
            urls.Add(SitemapUrl(ns, $"{baseUrl}/News/Article/{Uri.EscapeDataString(n.Slug)}", last, "weekly", "0.65"));
        }

        var doc = new XDocument(new XElement(ns + "urlset", urls));
        return Content(doc.ToString(), "application/xml", Encoding.UTF8);
    }

    string GetBaseUrl()
    {
        var configured = config["SiteSettings:PublicBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.TrimEnd('/');
        return $"{Request.Scheme}://{Request.Host}";
    }

    static XElement SitemapUrl(XNamespace ns, string loc, DateTime lastMod, string changefreq, string priority) =>
        new(ns + "url",
            new XElement(ns + "loc", loc),
            new XElement(ns + "lastmod", lastMod.ToString("yyyy-MM-dd")),
            new XElement(ns + "changefreq", changefreq),
            new XElement(ns + "priority", priority));
}
