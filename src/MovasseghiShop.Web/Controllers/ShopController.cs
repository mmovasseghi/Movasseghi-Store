using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Controllers;

public class ShopController(ApplicationDbContext db, IProductReviewService reviews, IAnalyticsTracker analytics) : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Catalog");

    public async Task<IActionResult> Product(string slug)
    {
        var product = await ProductStorefrontResolver.ResolveActiveBySlugAsync(db, slug);
        if (product == null)
        {
            if (!await TryHealInactiveProductAsync(slug))
                return NotFound();

            product = await ProductStorefrontResolver.ResolveActiveBySlugAsync(db, slug);
            if (product == null)
                return NotFound();
        }

        if (ShouldRedirectToCanonicalSlug(slug, product))
            return RedirectToActionPermanent(nameof(Product), new { slug = product.Slug });

        var seoProfile = await db.SeoProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.EntityType == "product" && p.EntityId == product.Id);
        ViewBag.SeoProfile = seoProfile;
        ViewBag.BuyboxSummary = SeoStorefrontDisplay.ResolveProductBuyboxSummary(product, seoProfile);

        var approved = await reviews.GetApprovedForProductAsync(product.Id);
        var (avg, count) = await reviews.GetApprovedRatingAsync(product.Id);
        ViewBag.ReviewsSection = new ProductReviewsSectionModel
        {
            ProductId = product.Id,
            Items = approved,
            Average = avg,
            Count = count
        };

        ViewBag.Related = await db.Products
            .AsNoTracking()
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Where(p => p.IsActive && p.Id != product.Id &&
                (p.ProductType == product.ProductType || p.CategoryId == product.CategoryId))
            .Take(6)
            .ToListAsync();

        // پله اول قیف — بدون این، «محصول پربازدید بی‌فروش» قابل تشخیص نیست
        analytics.TrackEvent("view_product", HttpContext,
            label: product.Name, entityId: product.Id, entityType: "product");

        // میدل‌ویر آنالیتیکس عنوان صفحه را از این‌جا می‌خواند
        HttpContext.Items["PageTitle"] = product.Name;

        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitReview(int productId, string authorName, int rating, string body, string slug)
    {
        await reviews.SubmitPublicReviewAsync(productId, authorName, rating, body);
        TempData["ReviewSubmitted"] = true;
        return RedirectToAction(nameof(Product), new { slug });
    }

    static bool ShouldRedirectToCanonicalSlug(string requestedSlug, Product product)
    {
        if (string.Equals(product.Slug, requestedSlug, StringComparison.Ordinal))
            return false;

        foreach (var candidate in ProductStorefrontResolver.SlugLookupCandidates(requestedSlug))
        {
            if (ProductStorefrontResolver.IsStrawLegacySlug(candidate)
                && string.Equals(product.ProductCode, ProductStorefrontResolver.StrawProductCode, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    async Task<bool> TryHealInactiveProductAsync(string slug)
    {
        var tracked = await ProductStorefrontResolver.FindInactiveBySlugAsync(db, slug);
        if (tracked == null)
            return false;

        if (!ProductStorefrontResolver.HasSellableVariant(tracked))
            return false;

        tracked.IsActive = true;
        tracked.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }
}
