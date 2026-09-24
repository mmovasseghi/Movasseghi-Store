using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Areas.Admin;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Admin;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Models.Enums;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductsController(
    ApplicationDbContext db,
    IWebHostEnvironment env,
    ISeoEngineService seo,
    IProductVariantAdminService variants) : Controller
{
    public async Task<IActionResult> Index(string? q, int? categoryId, string? status)
    {
        ViewBag.Categories = await db.Categories.OrderBy(c => c.SortOrder).ToListAsync();
        ViewBag.Search = q;
        ViewBag.CategoryId = categoryId;
        ViewBag.Status = NormalizeStatus(status);
        await SetProductSeoViewBagsAsync();
        await SetHomeHighlightViewBagsAsync();
        var products = await QueryProductsAsync(q, categoryId, status);
        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> ListPartial(string? q, int? categoryId, string? status)
    {
        await SetProductSeoViewBagsAsync();
        await SetHomeHighlightViewBagsAsync();
        var products = await QueryProductsAsync(q, categoryId, status);
        return PartialView("_ProductsList", products);
    }

    async Task SetHomeHighlightViewBagsAsync()
    {
        ViewBag.HomeSpecialTotal = await HomeProductHighlights.CountSpecialAsync(db.Products);
        ViewBag.HomeFeaturedTotal = await HomeProductHighlights.CountFeaturedAsync(db.Products);
    }

    async Task<List<Product>> QueryProductsAsync(string? q, int? categoryId, string? status)
    {
        var query = db.Products
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p =>
                p.Name.Contains(term) ||
                p.Slug.Contains(term) ||
                (p.ProductCode != null && p.ProductCode.Contains(term)));
        }

        if (categoryId is > 0)
            query = query.Where(p => p.CategoryId == categoryId);

        status = NormalizeStatus(status);
        if (status == "active") query = query.Where(p => p.IsActive);
        else if (status == "inactive") query = query.Where(p => !p.IsActive);
        else if (status == "special") query = query.Where(p => p.IsHomeSpecialOffer);
        else if (status == "featured") query = query.Where(p => p.IsHomeFeatured);

        return await query
            .OrderByDescending(p => p.IsHomeSpecialOffer)
            .ThenByDescending(p => p.IsHomeFeatured)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetHomeHighlight(int id, [FromForm] string role, [FromForm] bool enabled)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        role = (role ?? "").Trim().ToLowerInvariant();
        if (role is not ("special" or "featured"))
            return BadRequest(new { ok = false, message = "نقش نامعتبر است." });

        if (enabled)
        {
            var (allowed, message) = await HomeProductHighlights.CanEnableAsync(db.Products, id, role);
            if (!allowed)
                return Json(new { ok = false, message });
        }

        if (role == "special")
            product.IsHomeSpecialOffer = enabled;
        else
            product.IsHomeFeatured = enabled;

        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var specialTotal = await HomeProductHighlights.CountSpecialAsync(db.Products);
        var featuredTotal = await HomeProductHighlights.CountFeaturedAsync(db.Products);

        return Json(new
        {
            ok = true,
            id = product.Id,
            isHomeSpecialOffer = product.IsHomeSpecialOffer,
            isHomeFeatured = product.IsHomeFeatured,
            specialTotal,
            featuredTotal,
            max = HomeProductHighlights.MaxPerSlot
        });
    }

    async Task SetProductSeoViewBagsAsync()
    {
        var seoProfiles = await db.SeoProfiles
            .Where(s => s.EntityType == "product")
            .ToListAsync();
        ViewBag.SeoScores = seoProfiles.ToDictionary(
            s => s.EntityId,
            s => SeoUltimateAnalyzer.ResolveDisplayOverallFromProfile(s));
        ViewBag.SeoReadiness = seoProfiles.ToDictionary(s => s.EntityId, s => s.RankingReadiness);
        ViewBag.SeoReady = seoProfiles.ToDictionary(s => s.EntityId, s => s.IsPublishReady);
        ViewBag.SeoAudited = seoProfiles.ToDictionary(s => s.EntityId, s => s.LastAuditedAt != null);
    }

    static string? NormalizeStatus(string? status)
    {
        status = status?.Trim().ToLowerInvariant();
        return status is "active" or "inactive" or "special" or "featured" ? status : null;
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await db.Categories.OrderBy(c => c.SortOrder).ToListAsync();
        return View(new Product { IsActive = false });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, int categoryId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "نام محصول الزامی است.";
            return RedirectToAction(nameof(Create));
        }

        var slug = SlugHelper.Generate(name);
        if (await db.Products.AnyAsync(p => p.Slug == slug))
            slug += "-" + DateTime.UtcNow.Ticks.ToString()[^6..];

        var product = new Product
        {
            Name = name.Trim(),
            Slug = slug,
            CategoryId = categoryId > 0 ? categoryId : await db.Categories.Select(c => c.Id).FirstAsync(),
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        await seo.AutoFixProductAsync(product.Id);
        TempData["Success"] = "محصول ایجاد شد — SEO اولیه اعمال شد.";
        return RedirectToAction(nameof(Edit), new { id = product.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        db.ProductVariants.RemoveRange(product.Variants);
        db.ProductImages.RemoveRange(product.Images);
        var profiles = await db.SeoProfiles.Where(s => s.EntityType == "product" && s.EntityId == id).ToListAsync();
        db.SeoProfiles.RemoveRange(profiles);
        db.Products.Remove(product);
        await db.SaveChangesAsync();
        TempData["Success"] = $"محصول «{product.Name}» حذف شد.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var product = await db.Products
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();
        ViewBag.Categories = await db.Categories.OrderBy(c => c.SortOrder).ToListAsync();
        ViewBag.SeoScore = await seo.AuditProductAsync(id);
        var (suggestedPrimary, suggestedSecondary) = seo.SuggestKeywords(product);
        ViewBag.SuggestedPrimary = suggestedPrimary;
        ViewBag.SuggestedSecondary = suggestedSecondary;
        ViewBag.ProseContent = ProductDescriptionHelper.GetProseForEditor(product.Description);
        ViewBag.Packaging = variants.BuildEditorModel(product);
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> Edit(
        int id,
        Product model,
        [FromForm(Name = "Description")] string? descriptionHtml,
        [Bind(Prefix = "packaging")] ProductPackagingInput packaging,
        IFormFile? sceneImage,
        IFormFile? studioImage,
        bool forcePublish = false)
    {
        try
        {
            return await EditCoreAsync(id, model, descriptionHtml, packaging, sceneImage, studioImage, forcePublish);
        }
        catch (Exception ex) when (IsSkuConflict(ex))
        {
            TempData["Error"] =
                "ذخیره انجام نشد: تداخل SKU (کد آملون «000217» هم برای دیس متوسط #81 و هم دیس بزرگ #90 ثبت شده). پس از به‌روزرسانی سرور، ذخیره با SKUهای AM-P90-* انجام می‌شود.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (DbUpdateException ex)
        {
            TempData["Error"] = "ذخیره در دیتابیس ناموفق بود. صفحه را رفرش کنید؛ تصویر Scene معمولاً از قبل ذخیره شده است.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"خطا در ذخیره: {ex.Message}";
            return RedirectToAction(nameof(Edit), new { id });
        }
    }

    static bool IsSkuConflict(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e.Message.Contains("UNIQUE constraint failed: ProductVariants.Sku", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    async Task<IActionResult> EditCoreAsync(
        int id,
        Product model,
        string? descriptionHtml,
        ProductPackagingInput packaging,
        IFormFile? sceneImage,
        IFormFile? studioImage,
        bool forcePublish)
    {
        var product = await db.Products
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        product.Name = model.Name;
        if (Request.Form.ContainsKey("ShortDescription"))
        {
            var previousShort = product.ShortDescription;
            product.ShortDescription = model.ShortDescription?.Trim();
            var shortProfile = await SeoProfileEditorialLocks.GetOrCreateAsync(db, "product", id);
            SeoProfileEditorialLocks.OnManualProductShortSaved(shortProfile, product.ShortDescription, previousShort);
        }
        product.ProductCode = model.ProductCode?.Trim();
        if (model.CategoryId > 0)
            product.CategoryId = model.CategoryId;
        product.Gtin = SeoProductIdentity.Digits(model.Gtin) is { Length: > 0 } g ? g : null;
        product.Mpn = string.IsNullOrWhiteSpace(model.Mpn) ? null : model.Mpn.Trim();
        product.Dimensions = model.Dimensions?.Trim();
        product.Material = model.Material?.Trim();
        product.Applications = model.Applications?.Trim();
        product.MicrowaveSafe = model.MicrowaveSafe;
        product.CapacityCc = model.CapacityCc;
        product.CompartmentCount = model.CompartmentCount;
        if (descriptionHtml != null || Request.Form.ContainsKey("Description"))
        {
            var prose = (descriptionHtml ?? Request.Form["Description"].ToString() ?? model.Description ?? "").Trim();
            var existingProse = ProductDescriptionHelper.GetProseForEditor(product.Description);
            if (string.IsNullOrWhiteSpace(prose) && !string.IsNullOrWhiteSpace(existingProse))
            {
                TempData["Warn"] = "متن توضیحات کامل از فرم نرسید — نسخهٔ قبلی در دیتابیس نگه داشته شد. صفحه را با Ctrl+F5 رفرش کنید و دوباره ذخیره کنید.";
            }
            else
            {
                product.Description = ProductDescriptionHelper.MergeDescription(prose, product);

                var seoProfile = await db.SeoProfiles
                    .FirstOrDefaultAsync(s => s.EntityType == "product" && s.EntityId == id);
                if (seoProfile is null)
                {
                    seoProfile = new SeoProfile { EntityType = "product", EntityId = id };
                    db.SeoProfiles.Add(seoProfile);
                }
                seoProfile.LockProductDescription = true;
                seoProfile.UpdatedAt = DateTime.UtcNow;
            }
        }
        product.KeywordPrimary = model.KeywordPrimary?.Trim();
        product.KeywordSecondary = model.KeywordSecondary?.Trim();
        product.MetaTitle = model.MetaTitle;
        product.MetaDescription = model.MetaDescription;
        product.UpdatedAt = DateTime.UtcNow;

        await variants.ApplyAsync(product, packaging);

        var gallerySaved = false;
        if (sceneImage is { Length: > 0 })
        {
            await SaveGalleryImageAsync(product, sceneImage, ProductImageRole.Scene);
            gallerySaved = true;
        }
        if (studioImage is { Length: > 0 })
        {
            await SaveGalleryImageAsync(product, studioImage, ProductImageRole.Studio);
            gallerySaved = true;
        }

        ProductImageHelper.ApplyGalleryRolesFromUrls(product);

        var wasActive = product.IsActive;
        var formWantsActive = Request.Form["IsActive"].Contains("true");

        await db.SaveChangesAsync();

        SeoScoreResult score;
        try
        {
            // پس از ذخیره دستی: فقط نگهداری امن (بدون بازنویسی توضیحات قفل‌شده) + ارزیابی مجدد
            score = await seo.AutoFixProductAsync(id);
        }
        catch
        {
            score = new SeoScoreResult { TotalScore = 0, IsPublishReady = product.IsActive };
            TempData["Warn"] = "محصول ذخیره شد؛ ارزیابی SEO بلافاصله بعد از ذخیره کامل نشد — می‌توانید Auto-Fix را جداگانه بزنید.";
        }

        if (!formWantsActive)
        {
            product.IsActive = false;
        }
        else if (wasActive || forcePublish)
        {
            // محصولی که قبلاً در فروشگاه بوده با ذخیرهٔ عادی از کاتالوگ حذف نمی‌شود — فقط خاموش کردن دستی.
            product.IsActive = true;
            if (!forcePublish && !score.IsPublishReady)
                TempData["Warn"] = SeoPublishGateLabels.ActiveDespiteLowScoreMessage(score.DisplayTotal);
        }
        else
        {
            product.IsActive = score.IsPublishReady;
            if (!score.IsPublishReady)
                TempData["Error"] = SeoPublishGateLabels.ActivationBlockedMessage(score.DisplayTotal);
        }

        await db.SaveChangesAsync();

        TempData["Success"] = gallerySaved
            ? "تصویر گالری ذخیره شد."
            : product.IsActive
                ? "محصول ذخیره و آماده نمایش است."
                : wasActive && !product.IsActive
                    ? "محصول ذخیره شد — برای نمایش در فروشگاه Auto-Fix بزنید یا انتشار اجباری."
                    : "محصول ذخیره شد.";
        return RedirectToAction(nameof(Edit), new { id, tab = gallerySaved ? "gallery" : null });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> UploadGallery(int id, ProductImageRole role, IFormFile file)
    {
        var product = await db.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        if (file is not { Length: > 0 })
            return BadRequest(new { ok = false, message = "فایل تصویر انتخاب نشده است." });

        await SaveGalleryImageAsync(product, file, role);
        ProductImageHelper.ApplyGalleryRolesFromUrls(product);
        ApplyProductImageAlts(product);
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var image = FindGalleryImage(product, role);
        var label = role == ProductImageRole.Scene ? "Scene" : "Studio";
        return Json(new
        {
            ok = true,
            message = $"تصویر {label} ذخیره شد.",
            url = image?.Url + "?v=" + product.UpdatedAt.Ticks,
            imageId = image?.Id,
            altText = image?.AltText
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateImageAlt(int id, [FromForm] int imageId, [FromForm] string? altText)
    {
        if (imageId <= 0)
            return BadRequest(new { ok = false, message = "شناسه تصویر نامعتبر است." });

        var image = await db.ProductImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == id);
        if (image is null) return NotFound(new { ok = false, message = "تصویر پیدا نشد. صفحه را رفرش کنید." });

        image.AltText = (altText ?? "").Trim();
        await db.SaveChangesAsync();
        return Json(new { ok = true, altText = image.AltText });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateImageAlts(int id)
    {
        var product = await db.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return NotFound(new { ok = false, message = "محصول پیدا نشد." });

        ApplyProductImageAlts(product);
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Json(new
        {
            ok = true,
            alts = product.Images.Select(i => new { i.Id, i.Role, altText = i.AltText }).ToList()
        });
    }

    static void ApplyProductImageAlts(Product product)
    {
        var primary = product.KeywordPrimary ?? product.Name;
        var secondary = product.KeywordSecondary ?? SiteKeywordStrategy.Secondary;
        SeoMediaAnalyzer.ApplySeoAltTexts(product, primary, secondary);
    }

    async Task SaveGalleryImageAsync(Product product, IFormFile file, ProductImageRole role)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            ext = ".jpg";

        var stem = role == ProductImageRole.Scene ? "01-hero" : "02-studio";
        var dir = Path.Combine(env.WebRootPath, "images", "products", product.Id.ToString("D4"));
        Directory.CreateDirectory(dir);

        foreach (var old in Directory.GetFiles(dir, stem + ".*"))
            System.IO.File.Delete(old);

        var fileName = stem + ext;
        var physical = Path.Combine(dir, fileName);
        await using (var stream = System.IO.File.Create(physical))
            await file.CopyToAsync(stream);

        var url = $"/images/products/{product.Id:D4}/{fileName}";
        var image = FindGalleryImage(product, role);
        if (image == null)
        {
            product.Images.Add(new ProductImage
            {
                ProductId = product.Id,
                Url = url,
                Role = role,
                SortOrder = role == ProductImageRole.Scene ? 0 : 1,
                IsPrimary = role == ProductImageRole.Studio,
            });
        }
        else
        {
            image.Url = url;
            image.Role = role;
            image.SortOrder = role == ProductImageRole.Scene ? 0 : 1;
            if (role == ProductImageRole.Studio) image.IsPrimary = true;
            if (role == ProductImageRole.Scene) image.IsPrimary = false;
        }
    }

    static ProductImage? FindGalleryImage(Product product, ProductImageRole role)
    {
        var image = product.Images.FirstOrDefault(i => i.Role == role);
        if (image != null) return image;

        return role switch
        {
            ProductImageRole.Scene => product.Images.FirstOrDefault(i =>
                i.Url.Contains("01-hero", StringComparison.OrdinalIgnoreCase)
                || i.Url.Contains("hero", StringComparison.OrdinalIgnoreCase)),
            ProductImageRole.Studio => product.Images.FirstOrDefault(i =>
                i.Url.Contains("02-studio", StringComparison.OrdinalIgnoreCase)
                || i.Url.Contains("studio", StringComparison.OrdinalIgnoreCase)),
            _ => null
        };
    }

}

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrdersController(ApplicationDbContext db, IAdminNavBadgeService adminNavBadges) : Controller
{
    public async Task<IActionResult> Index(string? q, string? status, string? queue)
    {
        var query = db.Orders.AsNoTracking().Include(o => o.Items).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(o =>
                o.OrderNumber.Contains(term) ||
                o.CustomerName.Contains(term) ||
                o.Phone.Contains(term) ||
                (o.CouponCode != null && o.CouponCode.Contains(term)));
        }

        if (Enum.TryParse<OrderStatus>(status, out var parsed))
            query = query.Where(o => o.Status == parsed);

        if (string.Equals(queue, "followup", StringComparison.OrdinalIgnoreCase))
            query = query.Where(o => o.Status != OrderStatus.Cancelled && o.OperatorFollowedUpAt == null);

        var orders = await query.OrderByDescending(o => o.CreatedAt).Take(400).ToListAsync();
        ViewBag.Q = q;
        ViewBag.Status = status;
        ViewBag.Queue = queue;
        ViewBag.NeedsFollowUpCount = await db.Orders.CountAsync(o =>
            o.Status != OrderStatus.Cancelled && o.OperatorFollowedUpAt == null);
        ViewBag.Counts = (await db.Orders.AsNoTracking()
                .GroupBy(o => o.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync())
            .ToDictionary(x => x.Key, x => x.Count);
        return View(orders);
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .Include(o => o.VehicleInfo)
            .Include(o => o.DeliveryAddress)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();

        if (!string.IsNullOrEmpty(order.UserId))
        {
            var account = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == order.UserId);
            ViewBag.OrderAccount = account;
            var userOrders = db.Orders.AsNoTracking().Where(o => o.UserId == order.UserId);
            ViewBag.OrderAccountOrderCount = await userOrders.CountAsync();
            ViewBag.OrderAccountCompletedCount = await userOrders.CountAsync(o => o.Status != OrderStatus.Cancelled);
            ViewBag.OrderAccountTotalSpent = await userOrders
                .Where(o => o.Status != OrderStatus.Cancelled)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
        }

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
    {
        var order = await db.Orders.FindAsync(id);
        if (order == null) return NotFound();
        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        adminNavBadges.Invalidate();
        TempData["Success"] = $"وضعیت سفارش {order.OrderNumber} به «{OrderStatusLabels.Fa(status)}» تغییر کرد.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkFollowedUp(int id, string? returnUrl = null)
    {
        var order = await db.Orders.FindAsync(id);
        if (order == null) return NotFound();
        if (order.Status == OrderStatus.Cancelled)
        {
            TempData["Error"] = "سفارش لغو شده — نیازی به پیگیری اپراتور نیست.";
            return RedirectBack(id, returnUrl);
        }

        if (order.OperatorFollowedUpAt is null)
        {
            order.OperatorFollowedUpAt = DateTime.UtcNow;
            order.OperatorFollowedUpBy = User.Identity?.Name
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                ?? "admin";
            order.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            adminNavBadges.Invalidate();
            TempData["Success"] = $"سفارش {order.OrderNumber} به‌عنوان «پیگیری شد» ثبت شد.";
        }

        return RedirectBack(id, returnUrl);
    }

    IActionResult RedirectBack(int id, string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Details), new { id });
}

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CouponsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var coupons = await db.Coupons.AsNoTracking().OrderByDescending(c => c.Id).ToListAsync();
        ViewBag.ActiveCount = coupons.Count(c => c.IsActive && !IsExpired(c));
        ViewBag.ExpiredCount = coupons.Count(IsExpired);
        ViewBag.TotalUses = coupons.Sum(c => c.UsedCount);
        return View(coupons);
    }

    public IActionResult Create() => View("Edit", new Coupon { IsActive = true });

    public async Task<IActionResult> Edit(int id)
    {
        var coupon = await db.Coupons.FindAsync(id);
        return coupon is null ? NotFound() : View(coupon);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Coupon model)
    {
        var code = (model.Code ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
            ModelState.AddModelError(nameof(model.Code), "کد تخفیف الزامی است.");
        else if (await db.Coupons.AnyAsync(c => c.Code == code && c.Id != model.Id))
            ModelState.AddModelError(nameof(model.Code), "این کد قبلاً ثبت شده است.");

        if (model.Value <= 0)
            ModelState.AddModelError(nameof(model.Value), "مقدار تخفیف باید بزرگ‌تر از صفر باشد.");
        if (model.Type == CouponType.Percentage && model.Value > 100)
            ModelState.AddModelError(nameof(model.Value), "درصد تخفیف نمی‌تواند بیش از ۱۰۰ باشد.");
        if (model.StartsAt.HasValue && model.ExpiresAt.HasValue && model.ExpiresAt <= model.StartsAt)
            ModelState.AddModelError(nameof(model.ExpiresAt), "تاریخ انقضا باید بعد از تاریخ شروع باشد.");

        // فرم را با داده‌های کاربر برگردان — اطلاعات وارد‌شده از دست نرود
        if (!ModelState.IsValid) return View("Edit", model);

        model.Code = code;

        if (model.Id == 0)
        {
            db.Coupons.Add(model);
            TempData["Success"] = $"کد تخفیف «{code}» ساخته شد.";
        }
        else
        {
            var existing = await db.Coupons.FindAsync(model.Id);
            if (existing is null) return NotFound();

            existing.Code = code;
            existing.Type = model.Type;
            existing.Value = model.Value;
            existing.MinOrderAmount = model.MinOrderAmount;
            existing.MinCartons = model.MinCartons;
            existing.MaxUses = model.MaxUses;
            existing.StartsAt = model.StartsAt;
            existing.ExpiresAt = model.ExpiresAt;
            existing.IsActive = model.IsActive;
            TempData["Success"] = $"کد تخفیف «{code}» به‌روز شد.";
        }

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var coupon = await db.Coupons.FindAsync(id);
        if (coupon is null) return NotFound();
        coupon.IsActive = !coupon.IsActive;
        await db.SaveChangesAsync();
        TempData["Success"] = coupon.IsActive
            ? $"کد «{coupon.Code}» فعال شد."
            : $"کد «{coupon.Code}» غیرفعال شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var coupon = await db.Coupons.FindAsync(id);
        if (coupon is null) return NotFound();

        if (coupon.UsedCount > 0)
        {
            // حذف کد استفاده‌شده تاریخچه سفارش را می‌شکند — غیرفعال می‌شود
            coupon.IsActive = false;
            await db.SaveChangesAsync();
            TempData["Error"] =
                $"کد «{coupon.Code}» {coupon.UsedCount} بار استفاده شده و حذف نشد (تاریخچه سفارش می‌شکند). فقط غیرفعال شد.";
            return RedirectToAction(nameof(Index));
        }

        db.Coupons.Remove(coupon);
        await db.SaveChangesAsync();
        TempData["Success"] = $"کد «{coupon.Code}» حذف شد.";
        return RedirectToAction(nameof(Index));
    }

    static bool IsExpired(Coupon c) => c.ExpiresAt.HasValue && c.ExpiresAt < DateTime.UtcNow;
}

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class BlogAdminController(
    ApplicationDbContext db,
    ISeoEngineService seo,
    ISeoAutoFixEngine autoFix,
    MovasseghiShop.Web.Services.Editorial.IBlogEditorialAdminService blogEditorial) : Controller
{
    public async Task<IActionResult> Index(string? q, string? status)
    {
        var query = db.BlogPosts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(b => b.Title.Contains(term) || b.Slug.Contains(term));
        }

        if (status == "published") query = query.Where(b => b.IsPublished);
        else if (status == "draft") query = query.Where(b => !b.IsPublished);

        var posts = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
        ViewBag.Q = q;
        ViewBag.Status = status;
        ViewBag.PublishedCount = await db.BlogPosts.CountAsync(b => b.IsPublished);
        ViewBag.DraftCount = await db.BlogPosts.CountAsync(b => !b.IsPublished);
        await AdminSeoListBags.LoadAsync(db, this, "blog");
        return View(posts);
    }

    public IActionResult Create() => View("Edit", new BlogPost());

    public async Task<IActionResult> Edit(int id)
    {
        var post = await db.BlogPosts.FindAsync(id);
        if (post is null) return NotFound();
        if (string.IsNullOrWhiteSpace(post.MetaKeywords))
            await blogEditorial.EnsureKeywordsAsync(post);

        var profile = await db.SeoProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.EntityType == "blog" && p.EntityId == id);
        ViewBag.FocusKeyword = profile?.FocusKeyword;
        ViewBag.SecondaryKeyword = profile?.SecondaryKeyword;
        ViewBag.InlineImages = MovasseghiShop.Web.Services.Editorial.EditorialHtmlImages.Parse(post.Content);
        ViewBag.SeoScore = await seo.AuditBlogAsync(id);
        return View(post);
    }

    [HttpGet]
    public async Task<IActionResult> CatalogImages(string? q, int take = 48)
    {
        take = Math.Clamp(take, 8, 120);
        var query = db.ProductImages.AsNoTracking()
            .Where(i => i.Url != "" && !i.Url.Contains("originallogo"));

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(i =>
                i.Product.Name.Contains(term)
                || (i.AltText != null && i.AltText.Contains(term)));
        }

        var items = await query
            .OrderByDescending(i => i.IsPrimary)
            .ThenBy(i => i.SortOrder)
            .Take(take)
            .Select(i => new
            {
                url = i.Url,
                alt = string.IsNullOrWhiteSpace(i.AltText) ? i.Product.Name : i.AltText,
                product = i.Product.Name
            })
            .ToListAsync();

        return Json(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AutoFillSeo(int id)
    {
        var post = await db.BlogPosts.FindAsync(id);
        if (post is null) return NotFound();

        await blogEditorial.EnsureKeywordsAsync(post);
        var report = await autoFix.AutoFixBlogAsync(id, 6, forceRegenerate: true);
        var display = SeoUltimateAnalyzer.ResolveDisplayOverall(report);
        TempData["Success"] = report.IsPublishReady
            ? $"مقاله آماده انتشار است — امتیاز نمایشی {display}/100."
            : $"بهبود اعمال شد (امتیاز {display}) — هنوز یک مورد در پنل SEO باید رفع شود.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(BlogPost model)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
            ModelState.AddModelError(nameof(model.Title), "عنوان الزامی است.");
        if (string.IsNullOrWhiteSpace(model.Content))
            ModelState.AddModelError(nameof(model.Content), "متن مقاله الزامی است.");

        if (!ModelState.IsValid) return View("Edit", model);

        var slug = string.IsNullOrWhiteSpace(model.Slug)
            ? SlugHelper.Generate(model.Title)
            : SlugHelper.Generate(model.Slug);
        slug = await UniqueSlugAsync(slug, model.Id);

        if (model.Id == 0)
        {
            model.Slug = slug;
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;
            if (model.IsPublished) model.PublishedAt ??= DateTime.UtcNow;
            db.BlogPosts.Add(model);
            TempData["Success"] = "مقاله ساخته شد.";
        }
        else
        {
            var existing = await db.BlogPosts.FindAsync(model.Id);
            if (existing is null) return NotFound();

            var previousExcerpt = existing.Excerpt;
            var previousContent = existing.Content;
            existing.Title = model.Title.Trim();
            existing.Slug = slug;
            existing.Excerpt = model.Excerpt?.Trim();
            existing.Content = model.Content;
            var blogProfile = await SeoProfileEditorialLocks.GetOrCreateAsync(db, "blog", existing.Id);
            SeoProfileEditorialLocks.OnManualArticleSaved(
                blogProfile, previousExcerpt, previousContent, existing.Excerpt, existing.Content);
            existing.FeaturedImageUrl = model.FeaturedImageUrl?.Trim();
            existing.MetaTitle = model.MetaTitle?.Trim();
            existing.MetaDescription = model.MetaDescription?.Trim();
            existing.MetaKeywords = model.MetaKeywords?.Trim();
            existing.UpdatedAt = DateTime.UtcNow;

            // اولین انتشار زمان می‌گیرد؛ ویرایش بعدی تاریخ انتشار را عقب نمی‌برد
            if (model.IsPublished && !existing.IsPublished) existing.PublishedAt ??= DateTime.UtcNow;
            existing.IsPublished = model.IsPublished;

            TempData["Success"] = "مقاله به‌روز شد.";
            await db.SaveChangesAsync();
            if (existing.IsPublished)
            {
                var score = await seo.AuditBlogAsync(existing.Id);
                if (!score.IsPublishReady)
                {
                    existing.IsPublished = false;
                    await db.SaveChangesAsync();
                    TempData["Error"] = SeoPublishGateLabels.ActivationBlockedMessage(score.DisplayTotal);
                    return RedirectToAction(nameof(Edit), new { id = existing.Id });
                }
            }
            return RedirectToAction(nameof(Index));
        }

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePublish(int id)
    {
        var post = await db.BlogPosts.FindAsync(id);
        if (post is null) return NotFound();
        var wantPublish = !post.IsPublished;
        if (wantPublish)
        {
            var score = await seo.AuditBlogAsync(id);
            if (!score.IsPublishReady)
            {
                TempData["Error"] = SeoPublishGateLabels.ActivationBlockedMessage(score.DisplayTotal);
                return RedirectToAction(nameof(Edit), new { id });
            }
        }
        post.IsPublished = wantPublish;
        if (post.IsPublished) post.PublishedAt ??= DateTime.UtcNow;
        post.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = post.IsPublished ? "مقاله منتشر شد." : "مقاله به پیش‌نویس برگشت.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var post = await db.BlogPosts.FindAsync(id);
        if (post is null) return NotFound();
        db.BlogPosts.Remove(post);
        await db.SaveChangesAsync();
        TempData["Success"] = "مقاله حذف شد.";
        return RedirectToAction(nameof(Index));
    }

    async Task<string> UniqueSlugAsync(string slug, int selfId)
    {
        if (string.IsNullOrWhiteSpace(slug)) slug = "post";
        var candidate = slug;
        var n = 2;
        while (await db.BlogPosts.AnyAsync(b => b.Slug == candidate && b.Id != selfId))
            candidate = $"{slug}-{n++}";
        return candidate;
    }
}

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class NewsAdminController(ApplicationDbContext db, ISeoEngineService seo) : Controller
{
    public async Task<IActionResult> Index(string? q, string? status)
    {
        var query = db.NewsItems.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(n => n.Title.Contains(term) || n.Slug.Contains(term));
        }

        if (status == "published") query = query.Where(n => n.IsPublished);
        else if (status == "draft") query = query.Where(n => !n.IsPublished);
        else if (status == "breaking") query = query.Where(n => n.IsBreaking);

        var items = await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
        ViewBag.Q = q;
        ViewBag.Status = status;
        ViewBag.PublishedCount = await db.NewsItems.CountAsync(n => n.IsPublished);
        ViewBag.DraftCount = await db.NewsItems.CountAsync(n => !n.IsPublished);
        ViewBag.BreakingCount = await db.NewsItems.CountAsync(n => n.IsBreaking && n.IsPublished);
        await AdminSeoListBags.LoadAsync(db, this, "news");
        return View(items);
    }

    public IActionResult Create() => View("Edit", new NewsItem());

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.NewsItems.FindAsync(id);
        if (item is null) return NotFound();
        ViewBag.SeoScore = await seo.AuditNewsAsync(id);
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(NewsItem model)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
            ModelState.AddModelError(nameof(model.Title), "عنوان الزامی است.");
        if (string.IsNullOrWhiteSpace(model.Content))
            ModelState.AddModelError(nameof(model.Content), "متن خبر الزامی است.");

        if (!ModelState.IsValid) return View("Edit", model);

        var slug = string.IsNullOrWhiteSpace(model.Slug)
            ? SlugHelper.Generate(model.Title)
            : SlugHelper.Generate(model.Slug);
        slug = await UniqueSlugAsync(slug, model.Id);

        if (model.Id == 0)
        {
            model.Slug = slug;
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;
            if (model.IsPublished) model.PublishedAt ??= DateTime.UtcNow;
            db.NewsItems.Add(model);
            TempData["Success"] = "خبر ساخته شد.";
        }
        else
        {
            var existing = await db.NewsItems.FindAsync(model.Id);
            if (existing is null) return NotFound();

            var previousNewsExcerpt = existing.Excerpt;
            var previousNewsContent = existing.Content;
            existing.Title = model.Title.Trim();
            existing.Slug = slug;
            existing.Excerpt = model.Excerpt?.Trim();
            existing.Content = model.Content;
            var newsProfile = await SeoProfileEditorialLocks.GetOrCreateAsync(db, "news", existing.Id);
            SeoProfileEditorialLocks.OnManualArticleSaved(
                newsProfile, previousNewsExcerpt, previousNewsContent, existing.Excerpt, existing.Content);
            existing.FeaturedImageUrl = model.FeaturedImageUrl?.Trim();
            existing.MetaTitle = model.MetaTitle?.Trim();
            existing.MetaDescription = model.MetaDescription?.Trim();
            existing.MetaKeywords = model.MetaKeywords?.Trim();
            existing.IsBreaking = model.IsBreaking;
            existing.UpdatedAt = DateTime.UtcNow;

            if (model.IsPublished && !existing.IsPublished) existing.PublishedAt ??= DateTime.UtcNow;
            existing.IsPublished = model.IsPublished;

            TempData["Success"] = "خبر به‌روز شد.";
            await db.SaveChangesAsync();
            if (existing.IsPublished)
            {
                var score = await seo.AuditNewsAsync(existing.Id);
                if (!score.IsPublishReady)
                {
                    existing.IsPublished = false;
                    await db.SaveChangesAsync();
                    TempData["Error"] = SeoPublishGateLabels.ActivationBlockedMessage(score.DisplayTotal);
                    return RedirectToAction(nameof(Edit), new { id = existing.Id });
                }
            }
            return RedirectToAction(nameof(Index));
        }

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePublish(int id)
    {
        var item = await db.NewsItems.FindAsync(id);
        if (item is null) return NotFound();
        var wantPublish = !item.IsPublished;
        if (wantPublish)
        {
            var score = await seo.AuditNewsAsync(id);
            if (!score.IsPublishReady)
            {
                TempData["Error"] = SeoPublishGateLabels.ActivationBlockedMessage(score.DisplayTotal);
                return RedirectToAction(nameof(Edit), new { id });
            }
        }
        item.IsPublished = wantPublish;
        if (item.IsPublished) item.PublishedAt ??= DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = item.IsPublished ? "خبر منتشر شد." : "خبر به پیش‌نویس برگشت.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleBreaking(int id)
    {
        var item = await db.NewsItems.FindAsync(id);
        if (item is null) return NotFound();
        item.IsBreaking = !item.IsBreaking;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = item.IsBreaking ? "خبر فوری شد." : "برچسب فوری برداشته شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.NewsItems.FindAsync(id);
        if (item is null) return NotFound();
        db.NewsItems.Remove(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "خبر حذف شد.";
        return RedirectToAction(nameof(Index));
    }

    async Task<string> UniqueSlugAsync(string slug, int selfId)
    {
        if (string.IsNullOrWhiteSpace(slug)) slug = "news";
        var candidate = slug;
        var n = 2;
        while (await db.NewsItems.AnyAsync(x => x.Slug == candidate && x.Id != selfId))
            candidate = $"{slug}-{n++}";
        return candidate;
    }
}

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class InquiriesController(ApplicationDbContext db, IAdminNavBadgeService adminNavBadges) : Controller
{
    /// <summary>
    /// صندوق استعلام قیمت — تا امروز فقط تعدادش در داشبورد دیده می‌شد
    /// و هیچ راهی برای دیدن یا پیگیری خودِ درخواست‌ها نبود؛ یعنی سرنخ فروش گم می‌شد.
    /// </summary>
    public async Task<IActionResult> Index(string? status, string? q)
    {
        var query = db.PurchaseInquiries
            .AsNoTracking()
            .Include(i => i.Product)
            .AsQueryable();

        if (status == "open") query = query.Where(i => !i.IsHandled);
        else if (status == "handled") query = query.Where(i => i.IsHandled);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(i =>
                i.CustomerName.Contains(term)
                || i.Phone.Contains(term)
                || i.Product.Name.Contains(term));
        }

        var items = await query.OrderByDescending(i => i.CreatedAt).Take(500).ToListAsync();

        ViewBag.Q = q;
        ViewBag.Status = status;
        ViewBag.OpenCount = await db.PurchaseInquiries.CountAsync(i => !i.IsHandled);
        ViewBag.HandledCount = await db.PurchaseInquiries.CountAsync(i => i.IsHandled);
        ViewBag.TotalCartons = await db.PurchaseInquiries
            .Where(i => !i.IsHandled && i.QuantityCartons != null)
            .SumAsync(i => i.QuantityCartons!.Value);

        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleHandled(int id, string? returnUrl = null)
    {
        var inquiry = await db.PurchaseInquiries.FindAsync(id);
        if (inquiry is null) return NotFound();
        inquiry.IsHandled = !inquiry.IsHandled;
        await db.SaveChangesAsync();
        adminNavBadges.Invalidate();
        TempData["Success"] = inquiry.IsHandled
            ? $"استعلام «{inquiry.CustomerName}» پیگیری‌شده علامت خورد."
            : $"استعلام «{inquiry.CustomerName}» به صندوق باز برگشت.";
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var inquiry = await db.PurchaseInquiries.FindAsync(id);
        if (inquiry is null) return NotFound();
        db.PurchaseInquiries.Remove(inquiry);
        await db.SaveChangesAsync();
        TempData["Success"] = "استعلام حذف شد.";
        return RedirectToAction(nameof(Index));
    }
}

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class FaqAdminController(ApplicationDbContext db) : Controller
{
    /// <summary>
    /// FAQ سایت — موجودیتش وجود داشت ولی هیچ رابط مدیریتی نداشت.
    /// این محتوا مستقیم روی AEO و Featured Snippet اثر دارد.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var items = await db.FaqItems
            .AsNoTracking()
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Id)
            .ToListAsync();
        return View(items);
    }

    public IActionResult Create() => View("Edit", new FaqItem { IsActive = true });

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.FaqItems.FindAsync(id);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(FaqItem model)
    {
        if (string.IsNullOrWhiteSpace(model.Question))
            ModelState.AddModelError(nameof(model.Question), "سوال الزامی است.");
        if (string.IsNullOrWhiteSpace(model.Answer))
            ModelState.AddModelError(nameof(model.Answer), "پاسخ الزامی است.");

        if (!ModelState.IsValid) return View("Edit", model);

        var question = model.Question.Trim();
        // AEO: سوال باید علامت پرسش داشته باشد تا گوگل آن را سوال بشناسد
        if (!question.EndsWith('؟') && !question.EndsWith('?')) question += "؟";

        if (model.Id == 0)
        {
            model.Question = question;
            db.FaqItems.Add(model);
            TempData["Success"] = "سوال اضافه شد.";
        }
        else
        {
            var existing = await db.FaqItems.FindAsync(model.Id);
            if (existing is null) return NotFound();
            existing.Question = question;
            existing.Answer = model.Answer.Trim();
            existing.SortOrder = model.SortOrder;
            existing.IsActive = model.IsActive;
            TempData["Success"] = "سوال به‌روز شد.";
        }

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.FaqItems.FindAsync(id);
        if (item is null) return NotFound();
        db.FaqItems.Remove(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "سوال حذف شد.";
        return RedirectToAction(nameof(Index));
    }
}

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductReviewsAdminController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(int? productId, string? status, string? q)
    {
        ViewBag.ProductId = productId;
        ViewBag.Status = status;
        ViewBag.Search = q;
        ViewBag.PendingCount = await db.ProductReviews.CountAsync(r =>
            r.Status == ProductReviewStatus.Pending && r.ParentReviewId == null);
        var items = await QueryReviewsAsync(productId, status, q);
        await AttachStoreReplyMetaAsync(items);
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> ListPartial(int? productId, string? status, string? q)
    {
        ViewBag.ProductId = productId;
        ViewBag.Status = status;
        ViewBag.Search = q;
        var items = await QueryReviewsAsync(productId, status, q);
        await AttachStoreReplyMetaAsync(items);
        return PartialView("_ProductReviewsList", items);
    }

    IActionResult RedirectToReviewsIndex(int? productId, string? status, string? q) =>
        RedirectToAction(nameof(Index), new
        {
            productId = productId is > 0 ? productId : null,
            status = string.IsNullOrEmpty(status) ? null : status,
            q = string.IsNullOrWhiteSpace(q) ? null : q.Trim()
        });

    async Task AttachStoreReplyMetaAsync(List<ProductReview> roots)
    {
        var ids = roots.Select(r => r.Id).ToList();
        if (ids.Count == 0)
        {
            ViewBag.StoreReplyCounts = new Dictionary<int, (int Total, int Approved)>();
            return;
        }

        var rows = await db.ProductReviews.AsNoTracking()
            .Where(r => r.ParentReviewId != null && ids.Contains(r.ParentReviewId.Value) && r.IsStoreReply)
            .GroupBy(r => r.ParentReviewId!.Value)
            .Select(g => new
            {
                Id = g.Key,
                Total = g.Count(),
                Approved = g.Count(x => x.Status == ProductReviewStatus.Approved)
            })
            .ToListAsync();
        ViewBag.StoreReplyCounts = rows.ToDictionary(x => x.Id, x => (x.Total, x.Approved));
    }

    async Task<List<ProductReview>> QueryReviewsAsync(int? productId, string? status, string? q)
    {
        var query = db.ProductReviews.AsNoTracking().Include(r => r.Product)
            .Where(r => r.ParentReviewId == null && !r.IsStoreReply);
        if (productId is > 0) query = query.Where(r => r.ProductId == productId);
        if (status == "pending") query = query.Where(r => r.Status == ProductReviewStatus.Pending);
        else if (status == "approved") query = query.Where(r => r.Status == ProductReviewStatus.Approved);
        else if (status == "rejected") query = query.Where(r => r.Status == ProductReviewStatus.Rejected);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(r =>
                r.AuthorDisplayName.Contains(term) ||
                r.Body.Contains(term) ||
                (r.Product != null && (
                    r.Product.Name.Contains(term) ||
                    r.Product.Slug.Contains(term) ||
                    (r.Product.ProductCode != null && r.Product.ProductCode.Contains(term)))));
        }

        return await query.OrderByDescending(r => r.CreatedAt).Take(500).ToListAsync();
    }

    public async Task<IActionResult> Edit(int id)
    {
        var vm = await BuildEditViewModelAsync(id);
        return vm is null ? NotFound() : View(vm);
    }

    async Task<AdminProductReviewEditViewModel?> BuildEditViewModelAsync(int id)
    {
        var item = await db.ProductReviews.Include(r => r.Product).FirstOrDefaultAsync(r => r.Id == id);
        if (item is null) return null;

        ProductReview? parent = null;
        List<ProductReview> storeReplies = [];
        if (item.ParentReviewId is int pid)
        {
            parent = await db.ProductReviews.AsNoTracking().Include(r => r.Product)
                .FirstOrDefaultAsync(r => r.Id == pid);
        }
        else if (!item.IsStoreReply)
        {
            storeReplies = await db.ProductReviews
                .Where(r => r.ParentReviewId == item.Id && r.IsStoreReply)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();
        }

        return new AdminProductReviewEditViewModel
        {
            Review = item,
            ParentReview = parent,
            StoreReplies = storeReplies
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ProductReview model)
    {
        if (string.IsNullOrWhiteSpace(model.Body))
            ModelState.AddModelError(nameof(model.Body), "متن نظر الزامی است.");
        if (string.IsNullOrWhiteSpace(model.AuthorDisplayName))
            ModelState.AddModelError(nameof(model.AuthorDisplayName), "نام نمایشی الزامی است.");
        if (!ModelState.IsValid)
        {
            var invalidVm = await BuildEditViewModelAsync(model.Id);
            if (invalidVm is null) return NotFound();
            invalidVm.Review.AuthorDisplayName = model.AuthorDisplayName;
            invalidVm.Review.Body = model.Body;
            invalidVm.Review.Rating = model.Rating;
            invalidVm.Review.Status = model.Status;
            return View("Edit", invalidVm);
        }

        var existing = await db.ProductReviews.FindAsync(model.Id);
        if (existing is null) return NotFound();
        existing.AuthorDisplayName = model.AuthorDisplayName.Trim();
        existing.Body = model.Body.Trim();
        existing.Rating = Math.Clamp(model.Rating, 0, 5);
        existing.Status = model.Status;
        existing.ModeratedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = "نظر ذخیره شد.";
        return RedirectToReviewsIndex(existing.ProductId, Request.Form["listStatus"], Request.Form["listQ"]);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveStoreReply(int parentReviewId, int? replyId, string body)
    {
        var publish = string.Equals(Request.Form["publish"], "true", StringComparison.OrdinalIgnoreCase);
        var text = (body ?? "").Trim();
        if (string.IsNullOrEmpty(text))
        {
            TempData["Error"] = "متن پاسخ فروشگاه را وارد کنید.";
            return RedirectToAction(nameof(Edit), new { id = parentReviewId });
        }
        if (text.Length > 2000) text = text[..2000];

        var parent = await db.ProductReviews.FindAsync(parentReviewId);
        if (parent is null || parent.ParentReviewId != null || parent.IsStoreReply)
            return NotFound();

        ProductReview? reply;
        if (replyId is > 0)
        {
            reply = await db.ProductReviews.FirstOrDefaultAsync(r =>
                r.Id == replyId && r.ParentReviewId == parentReviewId && r.IsStoreReply);
            if (reply is null) return NotFound();
            reply.Body = text;
            reply.Status = publish ? ProductReviewStatus.Approved : ProductReviewStatus.Pending;
            reply.ModeratedAt = DateTime.UtcNow;
        }
        else
        {
            reply = new ProductReview
            {
                ProductId = parent.ProductId,
                ParentReviewId = parentReviewId,
                IsStoreReply = true,
                AuthorDisplayName = AdminProductReviewEditViewModel.StoreReplyDisplayName,
                Body = text,
                Rating = 0,
                Status = publish ? ProductReviewStatus.Approved : ProductReviewStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ModeratedAt = DateTime.UtcNow
            };
            db.ProductReviews.Add(reply);
        }

        await db.SaveChangesAsync();
        TempData["Success"] = replyId is > 0 ? "پاسخ فروشگاه به‌روزرسانی شد." : "پاسخ فروشگاه ثبت شد.";
        return RedirectToAction(nameof(Edit), new { id = parentReviewId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteStoreReply(int replyId, int parentReviewId)
    {
        var reply = await db.ProductReviews.FirstOrDefaultAsync(r =>
            r.Id == replyId && r.ParentReviewId == parentReviewId && r.IsStoreReply);
        if (reply is null) return NotFound();
        db.ProductReviews.Remove(reply);
        await db.SaveChangesAsync();
        TempData["Success"] = "پاسخ حذف شد.";
        return RedirectToAction(nameof(Edit), new { id = parentReviewId });
    }

    static bool IsReviewsAjaxRequest(HttpRequest request) =>
        string.Equals(request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
        || string.Equals(request.Form["ajax"], "true", StringComparison.OrdinalIgnoreCase);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, int? productId, string? status, string? q)
    {
        var item = await db.ProductReviews.FindAsync(id);
        if (item is null) return NotFound();
        item.Status = ProductReviewStatus.Approved;
        item.ModeratedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        if (IsReviewsAjaxRequest(Request))
            return new JsonResult(new { ok = true, message = "نظر تأیید شد." });
        TempData["Success"] = "نظر تأیید شد.";
        return RedirectToReviewsIndex(productId, status, q);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, int? productId, string? status, string? q)
    {
        var item = await db.ProductReviews.FindAsync(id);
        if (item is null) return NotFound();
        item.Status = ProductReviewStatus.Rejected;
        item.ModeratedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        if (IsReviewsAjaxRequest(Request))
            return new JsonResult(new { ok = true, message = "نظر رد شد." });
        TempData["Success"] = "نظر رد شد.";
        return RedirectToReviewsIndex(productId, status, q);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveAllSuggested(int productId, string? status, string? q)
    {
        var pending = await db.ProductReviews
            .Where(r => r.ProductId == productId && r.IsSeoSuggested && r.Status == ProductReviewStatus.Pending)
            .ToListAsync();
        foreach (var r in pending)
        {
            r.Status = ProductReviewStatus.Approved;
            r.ModeratedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
        TempData["Success"] = $"{pending.Count} نظر پیشنهادی تأیید شد.";
        return RedirectToReviewsIndex(productId, status, q);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int? productId, string? status, string? q)
    {
        var item = await db.ProductReviews.FindAsync(id);
        if (item is null) return NotFound();
        db.ProductReviews.Remove(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "نظر حذف شد.";
        return RedirectToReviewsIndex(productId, status, q);
    }
}

/// <summary>امتیاز SEO لیست ادمین برای بلاگ/خبر (مثل محصول و دسته).</summary>
internal static class AdminSeoListBags
{
    public static async Task LoadAsync(ApplicationDbContext db, Controller view, string entityType)
    {
        var profiles = await db.SeoProfiles.AsNoTracking()
            .Where(p => p.EntityType == entityType)
            .ToListAsync();
        view.ViewBag.SeoScores = profiles.ToDictionary(
            p => p.EntityId,
            SeoUltimateAnalyzer.ResolveDisplayOverallFromProfile);
        view.ViewBag.SeoAudited = profiles.ToDictionary(p => p.EntityId, p => p.LastAuditedAt != null);
        view.ViewBag.SeoReady = profiles.ToDictionary(p => p.EntityId, p => p.IsPublishReady);
    }
}
