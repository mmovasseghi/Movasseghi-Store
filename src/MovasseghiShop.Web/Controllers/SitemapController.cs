using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;

namespace MovasseghiShop.Web.Controllers;

public class SitemapController(ApplicationDbContext db, IConfiguration config) : Controller
{
    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 3600)]
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
            SitemapUrl(ns, baseUrl + "/Blog", DateTime.UtcNow, "weekly", "0.7"),
            SitemapUrl(ns, baseUrl + "/News", DateTime.UtcNow, "weekly", "0.7"),
        };

        var products = await db.Products.Where(p => p.IsActive).Select(p => new { p.Slug, p.UpdatedAt }).ToListAsync();
        foreach (var p in products)
            urls.Add(SitemapUrl(ns, $"{baseUrl}/Shop/Product/{p.Slug}", p.UpdatedAt, "weekly", "0.8"));

        var posts = await db.BlogPosts.Where(b => b.IsPublished).Select(b => new { b.Slug, b.UpdatedAt }).ToListAsync();
        foreach (var b in posts)
            urls.Add(SitemapUrl(ns, $"{baseUrl}/Blog/Post/{Uri.EscapeDataString(b.Slug)}", b.UpdatedAt, "monthly", "0.6"));

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
