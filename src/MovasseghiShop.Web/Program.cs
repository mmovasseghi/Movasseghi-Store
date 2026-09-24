using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.WebEncoders;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// HtmlEncoder پیش‌فرض هر حرف غیرلاتین را به entity عددی تبدیل می‌کند؛ یعنی «ا» به‌جای
// ۲ بایت UTF-8 هفت بایت («&#x627;») می‌شود. روی یک سایت تماماً فارسی این یعنی
// حجم HTML چند برابر — که مستقیم به سرعت لود و Core Web Vitals ضربه می‌زند.
// با اجازه‌دادن به کل بازه یونیکد، فارسی خام نوشته می‌شود و escape امنیتی
// (< > & ") همچنان انجام می‌شود.
builder.Services.Configure<WebEncoderOptions>(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = false;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.AccessDeniedPath = "/account/access-denied";
    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = context =>
        {
            var path = context.Request.Path.Value ?? "";
            var pathBase = context.Request.PathBase.Value ?? "";
            if (path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/Admin/Auth", StringComparison.OrdinalIgnoreCase))
            {
                var returnUrl = path + context.Request.QueryString;
                var loginUrl = pathBase + "/Admin/Auth/Login";
                if (!string.IsNullOrEmpty(returnUrl) && returnUrl != "/")
                    loginUrl += "?returnUrl=" + Uri.EscapeDataString(returnUrl);
                context.Response.Redirect(loginUrl);
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = context =>
        {
            var path = context.Request.Path.Value ?? "";
            var pathBase = context.Request.PathBase.Value ?? "";
            if (path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Redirect(pathBase + "/Admin/Auth/Login");
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<IProductVariantAdminService, ProductVariantAdminService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOtpService, MockOtpService>();
builder.Services.AddScoped<ICheckoutHumanVerificationService, CheckoutHumanVerificationService>();
builder.Services.AddScoped<IProductReviewService, ProductReviewService>();
builder.Services.AddScoped<IPaymentGateway, MockPaymentGateway>();
builder.Services.AddScoped<IProductImageLocalizerService, ProductImageLocalizerService>();
builder.Services.AddScoped<IImageOptimizerService, ImageOptimizerService>();
builder.Services.AddScoped<IAmelonImportService, AmelonImportService>();
builder.Services.AddScoped<IAmelonPriceSyncService, AmelonPriceSyncService>();
builder.Services.AddScoped<IProductCatalogService, ProductCatalogService>();
builder.Services.AddScoped<IGoogleSearchConsoleService, GoogleSearchConsoleService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ISeoSerpIntelligence, SeoSerpIntelligence>();
builder.Services.AddScoped<ISeoAutoFixEngine, SeoAutoFixEngine>();
builder.Services.Configure<MovasseghiShop.Web.Models.SeoContentPipelineConfig>(
    builder.Configuration.GetSection("Seo:ContentPipeline"));
builder.Services.AddSingleton<MovasseghiShop.Web.Services.ContentPipeline.ISerpOrganicSearchService,
    MovasseghiShop.Web.Services.ContentPipeline.SerpOrganicSearchService>();
builder.Services.AddScoped<MovasseghiShop.Web.Services.ContentPipeline.ISeoContentPipelineService,
    MovasseghiShop.Web.Services.ContentPipeline.SeoContentPipelineService>();
builder.Services.AddScoped<MovasseghiShop.Web.Services.ContentPipeline.IEditorialContentPipelineService,
    MovasseghiShop.Web.Services.ContentPipeline.EditorialContentPipelineService>();
builder.Services.AddHttpClient("competitor-fetch", c => c.Timeout = TimeSpan.FromSeconds(18));
builder.Services.AddScoped<ISeoEngineService, SeoEngineService>();
builder.Services.Configure<SeoMaintenanceOptions>(builder.Configuration.GetSection(SeoMaintenanceOptions.SectionName));
builder.Services.AddSingleton<SeoMaintenanceCoordinator>();
builder.Services.AddSingleton<ISeoMaintenanceCoordinator>(sp => sp.GetRequiredService<SeoMaintenanceCoordinator>());
builder.Services.AddHostedService<SeoMaintenanceBackgroundService>();
builder.Services.AddHostedService<SeoStartupMaintenanceService>();
builder.Services.AddScoped<IProductCatalogRepairService, ProductCatalogRepairService>();
builder.Services.AddScoped<IGrowthEngineService, GrowthEngineService>();
builder.Services.Configure<MovasseghiShop.Web.Services.Editorial.EditorialGrowthOptions>(
    builder.Configuration.GetSection(MovasseghiShop.Web.Services.Editorial.EditorialGrowthOptions.SectionName));
builder.Services.AddScoped<MovasseghiShop.Web.Services.Editorial.IEditorialGrowthService,
    MovasseghiShop.Web.Services.Editorial.EditorialGrowthService>();
builder.Services.AddScoped<MovasseghiShop.Web.Services.Editorial.IBlogEditorialAdminService,
    MovasseghiShop.Web.Services.Editorial.BlogEditorialAdminService>();
builder.Services.AddHostedService<MovasseghiShop.Web.Services.Editorial.EditorialGrowthBackgroundService>();
builder.Services.AddScoped<IRankTrackerService, RankTrackerService>();
builder.Services.AddScoped<IStudioContentService, StudioContentService>();
builder.Services.AddScoped<IHomeStoryService, HomeStoryService>();
builder.Services.AddScoped<ICompetitorAnalysisService, CompetitorAnalysisService>();
builder.Services.AddScoped<IAuthorityChecklistService, AuthorityChecklistService>();
builder.Services.AddScoped<IGrowthReportService, GrowthReportService>();

// ── آنالیتیکس ──
// Tracker یک Singleton است چون صف در حافظه را نگه می‌دارد؛
// نوشتن در دیتابیس در سرویس پس‌زمینه با scope مجزا انجام می‌شود.
builder.Services.AddSingleton<AnalyticsTracker>();
builder.Services.AddSingleton<IAnalyticsTracker>(sp => sp.GetRequiredService<AnalyticsTracker>());
builder.Services.AddHostedService<AnalyticsFlushService>();
builder.Services.AddScoped<IAnalyticsReportService, AnalyticsReportService>();
builder.Services.AddSingleton<MovasseghiShop.Web.Areas.Admin.IMemoryCacheAccessor,
    MovasseghiShop.Web.Areas.Admin.MemoryCacheAccessor>();
builder.Services.AddScoped<MovasseghiShop.Web.Areas.Admin.IAdminNavBadgeService,
    MovasseghiShop.Web.Areas.Admin.AdminNavBadgeService>();
builder.Services.AddHttpClient("serpapi", c => c.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddSingleton<ISerpRankProvider, SerpApiRankProvider>();
builder.Services.AddHostedService<RankSyncBackgroundService>();
builder.Services.AddHostedService<WeeklyReportBackgroundService>();
builder.Services.AddHostedService<GscStartupSyncService>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("checkout-human", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});
builder.Services.AddScoped<IGeocodingService, NominatimGeocodingService>();
builder.Services.AddHttpClient("Nominatim", c =>
{
    c.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
    c.DefaultRequestHeaders.UserAgent.ParseAdd("MovasseghiShop/1.0 (address-geocoding)");
    c.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient("Photon", c =>
{
    c.BaseAddress = new Uri("https://photon.komoot.io/");
    c.DefaultRequestHeaders.UserAgent.ParseAdd("MovasseghiShop/1.0 (address-geocoding)");
    c.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient("amelon", c =>
{
    c.BaseAddress = new Uri("https://amelon.co/");
    c.DefaultRequestHeaders.UserAgent.ParseAdd("MovasseghiShop/1.0 (catalog-import)");
    c.Timeout = TimeSpan.FromSeconds(90);
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    UseProxy = false,
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
    AutomaticDecompression = System.Net.DecompressionMethods.All
});
builder.Services.AddHttpContextAccessor();

// ═══ عملکرد: فشرده‌سازی پاسخ ═══
// Brotli حجم HTML/CSS/JS را ۷۰–۸۰٪ کم می‌کند. مستقیم روی LCP و FCP اثر دارد
// و LCP یکی از سه شاخص رتبه‌بندی گوگل است.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
    options.MimeTypes =
    [
        "text/html", "text/css", "text/javascript", "text/plain", "text/xml",
        "application/javascript", "application/json", "application/xml",
        "application/ld+json", "image/svg+xml", "application/manifest+json"
    ];
});
builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(
    o => o.Level = System.IO.Compression.CompressionLevel.Optimal);
builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(
    o => o.Level = System.IO.Compression.CompressionLevel.Optimal);

// ═══ عملکرد: Output Cache ═══
// صفحات عمومی برای مهمان‌ها از کش سرور پاسخ می‌گیرند تا EF و Razor
// در هر بازدید دوباره کار نکنند. برای کاربر لاگین‌شده یا سبد پرشده
// کش نمی‌شود، وگرنه داده یک کاربر به دیگری نشان داده می‌شود.
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(policy => policy.NoCache());

    options.AddPolicy("PublicPage", policy => policy
        .Expire(TimeSpan.FromMinutes(5))
        .SetVaryByQuery("category", "productType", "packaging", "sort",
                        "capacity", "compartments", "microwave", "page", "q")
        .SetVaryByHeader("Accept-Encoding"));

    options.AddPolicy("StaticContent", policy => policy
        .Expire(TimeSpan.FromMinutes(30))
        .SetVaryByHeader("Accept-Encoding"));
});

var mvcBuilder = builder.Services.AddControllersWithViews(options =>
{
    // نشان‌های منوی ادمین در هر صفحه لازم است — به‌جای تکرار در هر کنترلر
    options.Filters.Add<MovasseghiShop.Web.Areas.Admin.AdminNavBadgeFilter>();

    // پیام‌های پیش‌فرض model binding انگلیسی‌اند و تا امروز همان‌ها به کاربر
    // نشان داده می‌شد («The Answer field is required.»). این‌جا سراسری فارسی می‌شوند
    // تا هیچ فرمی در سایت یا پنل، خطای انگلیسی نشان ندهد.
    // پراپرتی‌های string غیرnullable به‌طور ضمنی Required می‌شوند و پیام انگلیسی
    // تولید می‌کنند. کنترلرها خودشان اعتبارسنجی فارسی دارند، پس این لایه اضافه است.
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;

    var m = options.ModelBindingMessageProvider;
    m.SetValueIsInvalidAccessor(v => $"مقدار «{v}» معتبر نیست.");
    m.SetValueMustNotBeNullAccessor(_ => "این فیلد را خالی نگذارید.");
    m.SetMissingBindRequiredValueAccessor(f => $"مقدار «{f}» ارسال نشد.");
    m.SetMissingKeyOrValueAccessor(() => "این فیلد را خالی نگذارید.");
    m.SetMissingRequestBodyRequiredValueAccessor(() => "محتوای درخواست خالی است.");
    m.SetAttemptedValueIsInvalidAccessor((v, f) => $"مقدار «{v}» برای «{f}» معتبر نیست.");
    m.SetUnknownValueIsInvalidAccessor(f => $"مقدار واردشده برای «{f}» معتبر نیست.");
    m.SetNonPropertyAttemptedValueIsInvalidAccessor(v => $"مقدار «{v}» معتبر نیست.");
    m.SetNonPropertyUnknownValueIsInvalidAccessor(() => "مقدار واردشده معتبر نیست.");
    m.SetValueMustBeANumberAccessor(f => $"«{f}» باید عدد باشد.");
    m.SetNonPropertyValueMustBeANumberAccessor(() => "این مقدار باید عدد باشد.");
});
if (builder.Environment.IsDevelopment())
    mvcBuilder.AddRazorRuntimeCompilation();
builder.Services.AddRazorPages();

var app = builder.Build();

if (args.Contains("--optimize-images", StringComparer.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var optimizer = scope.ServiceProvider.GetRequiredService<IImageOptimizerService>();
    var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("OptimizeImages");
    var dryRun = args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase);

    var r = await optimizer.OptimizeAllAsync(dryRun);
    log.LogInformation(
        "{Mode} — بررسی {Scanned} · بازنویسی {Rewritten} · رد {Skipped} · خطا {Failed} · "
        + "{Before} KB → {After} KB (صرفه‌جویی {Percent}٪)",
        dryRun ? "آزمایشی" : "اعمال شد",
        r.Scanned, r.Rewritten, r.Skipped, r.Failed,
        r.BytesBefore / 1024, r.BytesAfter / 1024, r.PercentSaved);
    return;
}

if (args.Contains("--repair-catalog", StringComparer.OrdinalIgnoreCase))
{
    await DbInitializer.InitializeAsync(app.Services);
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("RepairCatalog");
    var microwaveUpdated = await db.Products.Where(p => !p.MicrowaveSafe)
        .ExecuteUpdateAsync(s => s.SetProperty(p => p.MicrowaveSafe, true));
    log.LogInformation("MicrowaveSafe=true for {Count} products", microwaveUpdated);

    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    var products = await db.Products.Include(p => p.Images).ToListAsync();
    var galleryLinks = 0;
    foreach (var product in products)
    {
        galleryLinks += MovasseghiShop.Web.ProductImageHelper.RepairGalleriesFromDisk(product, env.WebRootPath);
        MovasseghiShop.Web.ProductImageHelper.ApplyGalleryRolesFromUrls(product);
    }
    await db.SaveChangesAsync();
    log.LogInformation("Gallery roles normalized for {Count} products; {Links} image links restored from disk",
        products.Count, galleryLinks);
    return;
}

if (args.Contains("--sync-amelon-prices", StringComparer.OrdinalIgnoreCase))
{
    await DbInitializer.InitializeAsync(app.Services);
    using var scope = app.Services.CreateScope();
    var importer = scope.ServiceProvider.GetRequiredService<IAmelonImportService>();
    var sync = scope.ServiceProvider.GetRequiredService<IAmelonPriceSyncService>();
    var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SyncAmelon");
    log.LogInformation("Importing missing amelon products...");
    var added = await importer.ImportMissingByWebCodesAsync(AmelonPriceSyncService.MissingWebCodes);
    log.LogInformation("Imported {Count} missing products", added);
    log.LogInformation("Syncing prices from PDF lists...");
    var priceResult = await sync.SyncFromPriceListsAsync();
    log.LogInformation(
        "Prices: matched={M} updated={U} created={C} skipped={S} deactivated={D}",
        priceResult.ProductsMatched, priceResult.VariantsUpdated, priceResult.VariantsCreated, priceResult.Skipped,
        priceResult.ProductsDeactivated);
    if (priceResult.Audit != null)
    {
        log.LogInformation("PDF codes: {Pdf}, mapped: {Map}, matched products: {Match}",
            priceResult.Audit.PdfWholesaleCodes, priceResult.Audit.MappedToSite, priceResult.Audit.MatchedProducts);
        log.LogInformation("Audit written to App_Data/price-sync-audit.json");
    }
    log.LogInformation("Enriching SEO text from amelon.co (gallery images are not re-localized)...");
    var enriched = await sync.EnrichSeoFromAmelonAsync();
    log.LogInformation("Enriched {Count} products from amelon", enriched);
    var microwaveUpdated = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
        .Products.Where(p => !p.MicrowaveSafe)
        .ExecuteUpdateAsync(s => s.SetProperty(p => p.MicrowaveSafe, true));
    if (microwaveUpdated > 0)
        log.LogInformation("MicrowaveSafe=true for {Count} products", microwaveUpdated);
    return;
}

if (args.Contains("--localize-images", StringComparer.OrdinalIgnoreCase))
{
    await DbInitializer.InitializeAsync(app.Services);
    using var scope = app.Services.CreateScope();
    var localizer = scope.ServiceProvider.GetRequiredService<IProductImageLocalizerService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("LocalizeImages");
    logger.LogInformation("Starting one-time product image localization...");
    var result = await localizer.LocalizeAllAsync();
    logger.LogInformation(
        "Done: {Products} products, {Downloaded} images, {Webp} webp, {Failures} failures",
        result.ProductsProcessed, result.ImagesDownloaded, result.WebpGenerated, result.Failures.Count);
    foreach (var f in result.Failures.Take(20))
        logger.LogWarning("{Failure}", f);
    return;
}

var auditProductArg = args.FirstOrDefault(a =>
    a.StartsWith("--seo-audit-product=", StringComparison.OrdinalIgnoreCase));
if (auditProductArg != null
    && int.TryParse(auditProductArg.Split('=', 2)[1], out var auditProductId))
{
    await DbInitializer.InitializeAsync(app.Services);
    using var scope = app.Services.CreateScope();
    var engine = scope.ServiceProvider.GetRequiredService<ISeoAutoFixEngine>();
    var report = await engine.AuditProductAsync(auditProductId);
    var display = SeoUltimateAnalyzer.ResolveDisplayOverall(report);
    Console.WriteLine(
        $"Product {auditProductId}: display={display} overall={report.OverallScore} weighted={report.WeightedDimensionScore} readiness={report.RankingReadiness} competitive={report.CompetitiveStrength} image={report.ImageScore}");
    foreach (var c in report.CriticalIssues) Console.WriteLine($"CRITICAL: {c}");
    foreach (var w in report.Warnings) Console.WriteLine($"WARN: {w}");
    return;
}

var fixProductArg = args.FirstOrDefault(a =>
    a.StartsWith("--seo-fix-product=", StringComparison.OrdinalIgnoreCase));
if (fixProductArg != null
    && int.TryParse(fixProductArg.Split('=', 2)[1], out var fixProductId))
{
    await DbInitializer.InitializeAsync(app.Services);
    using var scope = app.Services.CreateScope();
    var engine = scope.ServiceProvider.GetRequiredService<ISeoAutoFixEngine>();
    var report = await engine.AutoFixProductAsync(fixProductId, 5);
    var display = SeoUltimateAnalyzer.ResolveDisplayOverall(report);
    Console.WriteLine(
        $"Product {fixProductId}: display={display} overall={report.OverallScore} ready={report.IsPublishReady} density={report.KeywordDensity:F1}%");
    foreach (var c in report.CriticalIssues) Console.WriteLine($"CRITICAL: {c}");
    foreach (var w in report.Warnings) Console.WriteLine($"WARN: {w}");
    return;
}

if (args.Contains("--editorial-growth-run", StringComparer.OrdinalIgnoreCase))
{
    await DbInitializer.InitializeAsync(app.Services);
    using var scope = app.Services.CreateScope();
    var editorial = scope.ServiceProvider.GetRequiredService<MovasseghiShop.Web.Services.Editorial.IEditorialGrowthService>();
    var result = await editorial.RunDailyCycleAsync();
    Console.WriteLine($"Editorial: queued={result.TopicsQueued} drafts={result.DraftsCreated} published={result.PostsPublished}");
    foreach (var line in result.Log) Console.WriteLine($"  {line}");
    return;
}

if (args.Contains("--recover-stuck-blogs", StringComparer.OrdinalIgnoreCase))
{
    await DbInitializer.InitializeAsync(app.Services);
    using var scope = app.Services.CreateScope();
    var blogEd = scope.ServiceProvider.GetRequiredService<MovasseghiShop.Web.Services.Editorial.IBlogEditorialAdminService>();
    var r = await blogEd.RecoverStuckBlogsAsync();
    Console.WriteLine($"Recover: {r.NowReady}/{r.Total} ready, {r.StillStuck} still stuck");
    return;
}

if (args.Contains("--backfill-blog-keywords", StringComparer.OrdinalIgnoreCase))
{
    await DbInitializer.InitializeAsync(app.Services);
    using var scope = app.Services.CreateScope();
    var blogEd = scope.ServiceProvider.GetRequiredService<MovasseghiShop.Web.Services.Editorial.IBlogEditorialAdminService>();
    var n = await blogEd.BackfillAllBlogKeywordsAsync();
    Console.WriteLine($"Backfill blog keywords: {n} posts");
    return;
}

if (args.Contains("--editorial-quality-sweep", StringComparer.OrdinalIgnoreCase))
{
    await DbInitializer.InitializeAsync(app.Services);
    using var scope = app.Services.CreateScope();
    var editorial = scope.ServiceProvider.GetRequiredService<MovasseghiShop.Web.Services.Editorial.IEditorialGrowthService>();
    var sweep = await editorial.SweepLowQualityAndProduceAsync();
    Console.WriteLine($"Sweep: purged={sweep.Purged} produced={sweep.ProducedReady}/{sweep.TargetCount}");
    foreach (var line in sweep.Log) Console.WriteLine($"  {line}");
    var cycle = await editorial.RunDailyCycleAsync();
    Console.WriteLine($"Cycle: drafts={cycle.DraftsCreated} pub={cycle.PostsPublished}");
    foreach (var line in cycle.Log) Console.WriteLine($"  {line}");
    return;
}

if (args.Contains("--editorial-full-maint", StringComparer.OrdinalIgnoreCase))
{
    await DbInitializer.InitializeAsync(app.Services);
    using var scope = app.Services.CreateScope();
    var blogEd = scope.ServiceProvider.GetRequiredService<MovasseghiShop.Web.Services.Editorial.IBlogEditorialAdminService>();
    var editorial = scope.ServiceProvider.GetRequiredService<MovasseghiShop.Web.Services.Editorial.IEditorialGrowthService>();

    var kw = await blogEd.BackfillAllBlogKeywordsAsync();
    Console.WriteLine($"Keywords: {kw} posts");

    var sweep = await editorial.SweepLowQualityAndProduceAsync();
    Console.WriteLine($"Sweep: purged={sweep.Purged} produced={sweep.ProducedReady}/{sweep.TargetCount}");

    var cycle = await editorial.RunDailyCycleAsync();
    Console.WriteLine($"Growth: drafts={cycle.DraftsCreated} pub={cycle.PostsPublished} queued={cycle.TopicsQueued}");
    foreach (var line in cycle.Log) Console.WriteLine($"  {line}");
    return;
}

if (args.Contains("--seo-fix-categories", StringComparer.OrdinalIgnoreCase))
{
    await DbInitializer.InitializeAsync(app.Services);
    using var scope = app.Services.CreateScope();
    var engine = scope.ServiceProvider.GetRequiredService<ISeoAutoFixEngine>();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var categoryIds = await db.Categories.OrderBy(c => c.Id).Select(c => c.Id).ToListAsync();
    foreach (var c in await db.Categories.OrderBy(c => c.Id).Select(c => new { c.Id, c.Name, Count = c.Products.Count(p => p.IsActive) }).ToListAsync())
        Console.WriteLine($"  cat {c.Id}: {c.Count} active — {c.Name}");
    foreach (var id in categoryIds)
    {
        var report = await engine.AutoFixCategoryAsync(id, 5);
        var display = SeoUltimateAnalyzer.ResolveDisplayOverall(report);
        Console.WriteLine(
            $"Category {id}: display={display} overall={report.OverallScore} ready={report.IsPublishReady} aeo={report.AeoAnswerCoverage:F0}% words={report.WordCount}");
        foreach (var c in report.CriticalIssues) Console.WriteLine($"  CRITICAL: {c}");
        if (!report.IsPublishReady)
            Console.WriteLine($"  VERDICT: {report.RankingVerdict}");
    }

    return;
}

if (args.Contains("--seo-fix-blogs", StringComparer.OrdinalIgnoreCase))
{
    await DbInitializer.InitializeAsync(app.Services);
    using var scope = app.Services.CreateScope();
    var engine = scope.ServiceProvider.GetRequiredService<ISeoAutoFixEngine>();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var blogIds = await db.BlogPosts.OrderBy(b => b.Id).Select(b => b.Id).ToListAsync();
    foreach (var id in blogIds)
    {
        var audit = await engine.AuditBlogAsync(id);
        var report = await engine.AutoFixBlogAsync(id, 6, forceRegenerate: !audit.IsPublishReady);
        var display = SeoUltimateAnalyzer.ResolveDisplayOverall(report);
        Console.WriteLine(
            $"Blog {id}: display={display} overall={report.OverallScore} ready={report.IsPublishReady} words={report.WordCount}");
        foreach (var c in report.CriticalIssues) Console.WriteLine($"  CRITICAL: {c}");
        if (!report.IsPublishReady)
            Console.WriteLine($"  VERDICT: {report.RankingVerdict}");
    }

    return;
}

if (args.Contains("--bulk-seo-fix", StringComparer.OrdinalIgnoreCase))
{
    await DbInitializer.InitializeAsync(app.Services);
    using var scope = app.Services.CreateScope();
    var engine = scope.ServiceProvider.GetRequiredService<ISeoAutoFixEngine>();
    var growth = scope.ServiceProvider.GetRequiredService<IGrowthEngineService>();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("BulkSeoFix");

    var productIds = await db.Products.Select(p => p.Id).ToListAsync();
    var productScores = new List<int>();
    var productReadiness = new List<int>();
    var productReady = 0;
    var productFailed = 0;

    log.LogInformation("Auto-Fix Ultimate — {Count} محصول (۵ تکرار هر کدام)", productIds.Count);
    foreach (var id in productIds)
    {
        try
        {
            var report = await engine.AutoFixProductAsync(id, 5);
            productScores.Add(report.OverallScore);
            productReadiness.Add(report.RankingReadiness);
            if (report.IsPublishReady) productReady++;
            log.LogInformation(
                "محصول {Id}: امتیاز {Score}/100 · آمادگی {Readiness}/100 · {Gate}",
                id, report.OverallScore, report.RankingReadiness,
                report.IsPublishReady ? "آماده" : "نیاز به کار");
        }
        catch (Exception ex)
        {
            productFailed++;
            log.LogWarning(ex, "محصول {Id} خطا داد", id);
        }
    }

    var categoryIds = await db.Categories.Select(c => c.Id).ToListAsync();
    var categoryScores = new List<int>();
    var categoryFailed = 0;

    log.LogInformation("Auto-Fix دسته‌ها — {Count} دسته", categoryIds.Count);
    foreach (var id in categoryIds)
    {
        try
        {
            var report = await engine.AutoFixCategoryAsync(id, 5);
            categoryScores.Add(report.OverallScore);
            log.LogInformation("دسته {Id}: امتیاز {Score}/100", id, report.OverallScore);
        }
        catch (Exception ex)
        {
            categoryFailed++;
            log.LogWarning(ex, "دسته {Id} خطا داد", id);
        }
    }

    await growth.RefreshActionQueueAsync();

    var avgScore = productScores.Count > 0 ? Math.Round(productScores.Average(), 1) : 0;
    var avgReadiness = productReadiness.Count > 0 ? Math.Round(productReadiness.Average(), 1) : 0;
    var avgCategory = categoryScores.Count > 0 ? Math.Round(categoryScores.Average(), 1) : 0;

    Console.WriteLine();
    Console.WriteLine("═══ نتیجه Bulk Auto-Fix ═══");
    Console.WriteLine($"محصولات: {productIds.Count - productFailed}/{productIds.Count} موفق · {productReady} آماده انتشار");
    Console.WriteLine($"میانگین امتیاز محصول: {avgScore}/100 · میانگین آمادگی: {avgReadiness}/100");
    Console.WriteLine($"دسته‌ها: {categoryIds.Count - categoryFailed}/{categoryIds.Count} موفق · میانگین: {avgCategory}/100");
    if (productFailed + categoryFailed > 0)
        Console.WriteLine($"خطا: {productFailed} محصول · {categoryFailed} دسته");
    Console.WriteLine();

    return;
}

await DbInitializer.InitializeAsync(app.Services);

var configuredPathBase = app.Configuration["PathBase"]?.Trim();
if (!string.IsNullOrEmpty(configuredPathBase))
{
    if (!configuredPathBase.StartsWith('/'))
        configuredPathBase = "/" + configuredPathBase;
    configuredPathBase = configuredPathBase.TrimEnd('/');
    app.UsePathBase(configuredPathBase);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// فشرده‌سازی باید قبل از هر چیزی باشد که پاسخ می‌نویسد
app.UseResponseCompression();

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

// ═══ فایل‌های استاتیک با کش طولانی ═══
// asp-append-version=true به هر فایل هش محتوا می‌دهد، پس کش یک‌ساله
// امن است: تغییر فایل ⇒ تغییر URL ⇒ دانلود دوباره.
// بدون این هدر، مرورگر در هر بازدید CSS/JS/فونت را دوباره اعتبارسنجی می‌کرد.
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.File.Name;
        var headers = ctx.Context.Response.Headers;

        var isFingerprinted = ctx.Context.Request.Query.ContainsKey("v");
        var isImmutableType =
            path.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".woff", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".avif", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase);

        headers.CacheControl = isFingerprinted
            ? "public,max-age=31536000,immutable"
            : isImmutableType
                ? "public,max-age=31536000"
                : "public,max-age=604800";
    }
});

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseOutputCache();

app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.TryAdd("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
    await next();
});

// ثبت بازدید بعد از احراز هویت تا وضعیت لاگین کاربر معلوم باشد،
// و بعد از Session تا شناسه نشست در دسترس باشد.
app.UseMiddleware<AnalyticsMiddleware>();

app.MapControllerRoute(
    name: "product",
    pattern: "Shop/Product/{slug}",
    defaults: new { controller = "Shop", action = "Product" });

app.MapControllerRoute(
    name: "blog-post",
    pattern: "Blog/Post/{slug}",
    defaults: new { controller = "Blog", action = "Post" });

app.MapControllerRoute(
    name: "news-article",
    pattern: "News/Article/{slug}",
    defaults: new { controller = "News", action = "Article" });

app.MapControllerRoute(
    name: "legacy-search",
    pattern: "Search",
    defaults: new { controller = "Home", action = "Search" });

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
