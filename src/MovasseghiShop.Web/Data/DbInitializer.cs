using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Models.Enums;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var importer = scope.ServiceProvider.GetRequiredService<IAmelonImportService>();
        var imageLocalizer = scope.ServiceProvider.GetRequiredService<IProductImageLocalizerService>();
        var hostEnv = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        if (hostEnv.IsDevelopment())
        {
            await db.Database.EnsureCreatedAsync();
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }

        await EnsureUserProfileColumnsAsync(db, logger);
        await EnsureOrderOperatorFollowUpColumnsAsync(db, logger);
        await EnsureUserAddressesTableAsync(db, logger);
        await EnsureUserAddressColumnsAsync(db, logger);
        await EnsureNewsItemsTableAsync(db, logger);
        await EnsureProductVariantColumnsAsync(db, logger);
        await EnsureProductImageRoleColumnAsync(db, logger);
        await EnsureProductImageAltTextColumnAsync(db, logger);
        await BackfillProductImageRolesAsync(db, logger);
        await EnsureStudioTablesAsync(db, logger);
        await EnsureProductKeywordColumnsAsync(db, logger);
        await EnsureProductReviewsTableAsync(db, logger);
        await EnsureRankSnapshotsTableAsync(db, logger);
        await EnsureEditorialTopicQueueTableAsync(db, logger);
        await EnsurePillarPagesAsync(db, logger);
        await EnsureStudioContentTablesAsync(db, logger);
        await EnsureHomeStoriesTablesAsync(db, logger);
        await SeedStudioContentAsync(db, logger);

        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));

        if (await userManager.FindByNameAsync("admin") == null)
        {
            var admin = new ApplicationUser
            {
                UserName = "admin",
                PhoneNumber = "09125199105",
                PhoneNumberConfirmed = true,
                FullName = "مدیر فروشگاه"
            };
            await userManager.CreateAsync(admin, "Admin@123456");
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        if (!await db.Products.AnyAsync())
        {
            logger.LogInformation("Starting amelon.co product import...");
            try
            {
                var count = await importer.ImportAllAsync();
                logger.LogInformation("Imported {Count} products", count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Amelon import failed; app will start without products. Retry from Admin dashboard.");
                foreach (var entry in db.ChangeTracker.Entries().Where(e => e.State != EntityState.Unchanged).ToList())
                    entry.State = EntityState.Detached;
            }
        }

        if (await db.Products.AnyAsync())
            await TryLocalizeProductImagesAsync(db, imageLocalizer, logger);

        var repair = scope.ServiceProvider.GetRequiredService<IProductCatalogRepairService>();
        var repaired = await repair.RepairAsync();
        if (repaired > 0)
            logger.LogInformation("Product catalog repair: {Count} fields fixed", repaired);

        await EnsureHomeStoriesPopulatedAsync(scope.ServiceProvider, logger);

        if (!await db.CmsPages.AnyAsync())
        {
            try
            {
                db.CmsPages.Add(new Models.Entities.CmsPage
                {
                    Key = "about",
                    Title = "درباره ما",
                    Content = "<p>متن درباره ما توسط مدیریت تکمیل می‌شود.</p>"
                });
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not seed CMS about page");
            }
        }

        if (!await db.BlogPosts.AnyAsync())
        {
            var now = DateTime.UtcNow;
            db.BlogPosts.AddRange(
                CreateBlogPost(
                    "راهنمای جامع خرید عمده ظروف یکبار مصرف گیاهی",
                    "wholesale-guide-plant-based",
                    "نکات کلیدی انتخاب ظرف مناسب برای رستوران، کیترینگ و فروشگاه — حداقل سفارش ۱۰ میلیون تومان، تخفیف پلکانی تا ۱۲٪.",
                    BlogWholesaleContent(),
                    "خرید عمده ظروف گیاهی | راهنمای کامل — فروشگاه موثقی",
                    "راهنمای خرید عمده ظروف یکبار مصرف گیاهی آملون از نمایندگی رسمی موثقی در تهران. حداقل ۱۰ میلیون تومان، تخفیف پلکانی و مشاوره.",
                    "خرید عمده ظروف گیاهی, ظروف یکبار مصرف, آملون, فروشگاه موثقی, پخش عمده تهران",
                    now.AddDays(-2),
                    "/images/products/0076/01.webp"),
                CreateBlogPost(
                    "ظروف مایکروویوی آملون؛ استانداردها و نکات نگهداری",
                    "microwave-containers-guide",
                    "چطور ظروف گیاهی را برای سرو غذای داغ و گرم‌کردن در مایکروویو انتخاب کنیم؟ راهنمای عملی برای رستوران و فست‌فود.",
                    BlogMicrowaveContent(),
                    "ظروف مایکروویوی گیاهی آملون | راهنما — موثقی",
                    "راهنمای انتخاب و نگهداری ظروف مایکروویوی گیاهی آملون. استانداردها، دمای مجاز و نکات ایمنی برای رستوران و کترینگ.",
                    "ظروف مایکروویوی, ظروف گیاهی, آملون, رستوران, فست فود",
                    now.AddDays(-5),
                    "/images/products/0088/01.webp"),
                CreateBlogPost(
                    "چرا فروشگاه موثقی؟ مزیت نمایندگی رسمی آملون در تهران",
                    "why-movasseghi-official",
                    "اصالت کالا، مشاوره رایگان، ارسال سراسری و پشتیبانی تخصصی پخش عمده — تفاوت خرید از نمایندگی رسمی.",
                    BlogWhyMovasseghiContent(),
                    "نمایندگی رسمی آملون تهران | فروشگاه موثقی",
                    "فروشگاه موثقی تنها نمایندگی رسمی آملون در تهران. اصل کارخانه، پخش عمده ظروف گیاهی، مشاوره رایگان و ارسال سراسری.",
                    "نمایندگی آملون, فروشگاه موثقی, ظروف گیاهی تهران, پخش عمده",
                    now.AddDays(-9),
                    "/images/products/0046/01.webp"),
                CreateBlogPost(
                    "مقایسه ظروف گیاهی و پلاستیکی؛ کدام برای کسب‌وکار شما بهتر است؟",
                    "plant-vs-plastic-containers",
                    "مزایا و معایب ظروف نشاسته‌ای آملون در برابر پلاستیک — برای رستوران، کافه و کیترینگ.",
                    BlogPlantVsPlasticContent(),
                    "ظروف گیاهی یا پلاستیکی؟ | مقایسه — وبلاگ موثقی",
                    "مقایسه ظروف یکبار مصرف گیاهی آملون با پلاستیک. مزایای زیست‌تخریب‌پذیر، ایمنی غذایی و برندینگ برای کسب‌وکار.",
                    "ظروف گیاهی, ظروف پلاستیکی, آملون, زیست تخریب پذیر",
                    now.AddDays(-14),
                    "/images/products/0093/01.webp"));
            try
            {
                await db.SaveChangesAsync();
                logger.LogInformation("Seeded sample blog posts");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not seed blog posts");
            }
        }

        if (!await db.NewsItems.AnyAsync())
        {
            var now = DateTime.UtcNow;
            db.NewsItems.AddRange(
                CreateNews(
                    "ورود سری جدید ظروف گیاهی آملون به انبار فروشگاه موثقی",
                    "new-amelon-stock-arrival",
                    "محصولات جدید کاسه، ظرف غذا و لیوان گیاهی با بسته‌بندی شیرینگ و فله آماده تحویل عمده.",
                    NewsStockContent(),
                    true,
                    "ورود محصولات جدید آملون | اخبار موثقی",
                    "سری جدید ظروف گیاهی آملون به انبار فروشگاه موثقی رسید. خرید عمده از ۱۰ میلیون تومان با تخفیف پلکانی.",
                    "اخبار آملون, ظروف گیاهی, فروشگاه موثقی, انبار تهران",
                    now.AddDays(-1),
                    "/images/products/0071/01.webp"),
                CreateNews(
                    "فعال‌سازی تخفیف پلکانی خرید عمده در فروشگاه موثقی",
                    "fall-wholesale-discount",
                    "از ۱۰ میلیون تومان ۳٪ تا ۱۵۰ میلیون ۱۲٪ تخفیف روی کل فاکتور — سفارش‌های بزرگ با هماهنگی تلفنی.",
                    NewsDiscountContent(),
                    true,
                    "تخفیف پلکانی خرید عمده | اخبار موثقی",
                    "تخفیف پلکانی خرید عمده ظروف گیاهی آملون: از ۳٪ برای ۱۰ میلیون تا ۱۲٪ برای ۱۵۰ میلیون تومان.",
                    "تخفیف عمده, ظروف گیاهی, رستوران, کترینگ, موثقی",
                    now.AddDays(-3),
                    "/images/products/0075/01.webp"),
                CreateNews(
                    "راه‌اندازی سامانه استعلام قیمت آنلاین در فروشگاه موثقی",
                    "online-price-inquiry-launch",
                    "مشتریان عمده می‌توانند از صفحه هر محصول یا تماس مستقیم، استعلام سریع دریافت کنند.",
                    NewsInquiryContent(),
                    false,
                    "استعلام قیمت آنلاین | اخبار موثقی",
                    "سامانه استعلام قیمت آنلاین فروشگاه موثقی راه‌اندازی شد. استعلام سریع قیمت عمده ظروف گیاهی آملون.",
                    "استعلام قیمت, خرید عمده, فروشگاه موثقی",
                    now.AddDays(-6),
                    "/images/products/0072/01.webp"),
                CreateNews(
                    "تأیید نمایندگی رسمی آملون برای فروشگاه موثقی در تهران",
                    "official-amelon-dealership-tehran",
                    "فروشگاه موثقی به‌عنوان نماینده رسمی آملون در تهران فعالیت می‌کند — اصل کارخانه و پشتیبانی تخصصی.",
                    NewsDealershipContent(),
                    false,
                    "نمایندگی رسمی آملون تهران | خبر موثقی",
                    "فروشگاه موثقی نمایندگی رسمی آملون در تهران را دریافت کرد. پخش عمده ظروف گیاهی اصل کارخانه.",
                    "نمایندگی آملون, تهران, فروشگاه موثقی, ظروف گیاهی",
                    now.AddDays(-10),
                    "/images/products/0074/01.webp"),
                CreateNews(
                    "ارسال سریع‌تر سفارشات عمده در تهران و شهرستان‌ها",
                    "faster-wholesale-delivery",
                    "بهبود زنجیره توزیع برای تحویل به‌موقع سفارشات رستوران و فروشگاه.",
                    NewsDeliveryContent(),
                    false,
                    "ارسال سریع سفارش عمده | اخبار موثقی",
                    "فروشگاه موثقی زمان ارسال سفارشات عمده ظروف گیاهی را در تهران و شهرستان‌ها کاهش داد.",
                    "ارسال عمده, ظروف گیاهی, تهران, موثقی",
                    now.AddDays(-12),
                    "/images/products/0089/01.webp"));
            try
            {
                await db.SaveChangesAsync();
                logger.LogInformation("Seeded sample news items");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not seed news items");
            }
        }

        await EnsureContentFeaturedImagesAsync(db, logger);
        await EnsureWholesaleContentTextAsync(db, logger);

        try
        {
            var growth = scope.ServiceProvider.GetRequiredService<IGrowthEngineService>();
            await growth.EnsurePillarKeywordsAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Studio pillar seed skipped");
        }
    }

    private static async Task TryLocalizeProductImagesAsync(
        ApplicationDbContext db,
        IProductImageLocalizerService imageLocalizer,
        ILogger logger)
    {
        if (!await db.ProductImages.AnyAsync(i => i.Url.StartsWith("http")))
            return;

        logger.LogInformation("Localizing product images from amelon.co...");
        try
        {
            var result = await imageLocalizer.LocalizeAllAsync();
            logger.LogInformation(
                "Image localization: {Downloaded} files, {Webp} webp, {Failures} failures",
                result.ImagesDownloaded, result.WebpGenerated, result.Failures.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Product image localization failed");
        }
    }

    static async Task EnsureUserProfileColumnsAsync(ApplicationDbContext db, ILogger logger)
    {
        var columns = new Dictionary<string, string>
        {
            ["FirstName"] = "TEXT NULL",
            ["LastName"] = "TEXT NULL",
            ["FullName"] = "TEXT NULL",
            ["CompanyName"] = "TEXT NULL",
            ["CreatedAt"] = "TEXT NOT NULL DEFAULT (datetime('now'))"
        };

        foreach (var (name, ddl) in columns)
            await AddSqliteColumnIfMissingAsync(db, logger, "AspNetUsers", name, ddl);

        await db.Database.ExecuteSqlRawAsync("""
            UPDATE AspNetUsers
            SET FirstName = trim(substr(FullName, 1, instr(FullName || ' ', ' ') - 1)),
                LastName = trim(substr(FullName, instr(FullName || ' ', ' ') + 1))
            WHERE (FirstName IS NULL OR FirstName = '')
              AND FullName IS NOT NULL
              AND FullName != ''
            """);
    }

    static async Task EnsureOrderOperatorFollowUpColumnsAsync(ApplicationDbContext db, ILogger logger)
    {
        foreach (var (name, ddl) in new Dictionary<string, string>
        {
            ["OperatorFollowedUpAt"] = "TEXT NULL",
            ["OperatorFollowedUpBy"] = "TEXT NULL"
        })
            await AddSqliteColumnIfMissingAsync(db, logger, "Orders", name, ddl);
    }

    static async Task EnsureUserAddressesTableAsync(ApplicationDbContext db, ILogger logger)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS UserAddresses (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId TEXT NOT NULL,
                    Label TEXT NULL,
                    Province TEXT NOT NULL DEFAULT '',
                    City TEXT NOT NULL DEFAULT '',
                    FullAddress TEXT NOT NULL DEFAULT '',
                    PostalCode TEXT NULL,
                    Plaque TEXT NULL,
                    Unit TEXT NULL,
                    Latitude REAL NULL,
                    Longitude REAL NULL,
                    IsDefault INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
                )
                """);
            logger.LogInformation("Ensured UserAddresses table exists");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "UserAddresses table setup skipped or failed");
        }
    }

    static async Task EnsureUserAddressColumnsAsync(ApplicationDbContext db, ILogger logger)
    {
        foreach (var (name, ddl) in new Dictionary<string, string>
        {
            ["Plaque"] = "TEXT NULL",
            ["Unit"] = "TEXT NULL"
        })
            await AddSqliteColumnIfMissingAsync(db, logger, "UserAddresses", name, ddl);
    }

    static BlogPost CreateBlogPost(string title, string slug, string excerpt, string content,
        string metaTitle, string metaDesc, string metaKeywords, DateTime published, string? featuredImageUrl = null)
        => new()
        {
            Title = title,
            Slug = slug,
            Excerpt = excerpt,
            Content = content,
            MetaTitle = metaTitle,
            MetaDescription = metaDesc,
            MetaKeywords = metaKeywords,
            FeaturedImageUrl = featuredImageUrl ?? "/images/brand/originallogo.png",
            IsPublished = true,
            PublishedAt = published,
            CreatedAt = published,
            UpdatedAt = published
        };

    static NewsItem CreateNews(string title, string slug, string excerpt, string content, bool isBreaking,
        string metaTitle, string metaDesc, string metaKeywords, DateTime published, string? featuredImageUrl = null)
        => new()
        {
            Title = title,
            Slug = slug,
            Excerpt = excerpt,
            Content = content,
            MetaTitle = metaTitle,
            MetaDescription = metaDesc,
            MetaKeywords = metaKeywords,
            FeaturedImageUrl = featuredImageUrl ?? "/images/brand/originallogo.png",
            IsPublished = true,
            IsBreaking = isBreaking,
            PublishedAt = published,
            CreatedAt = published,
            UpdatedAt = published
        };

    static string BlogWholesaleContent() => """
        <p>خرید عمده ظروف یکبار مصرف گیاهی از <strong>نمایندگی رسمی آملون</strong> در تهران، مزایای مشخصی برای رستوران، فست‌فود، کافه و کیترینگ دارد: اصل کارخانه، قیمت پخش، مشاوره تخصصی و ارسال سراسری.</p>
        <h2>حداقل سفارش و تخفیف پلکانی</h2>
        <p>در فروشگاه موثقی حداقل سفارش عمده <strong>۱۰ میلیون تومان</strong> است. تخفیف روی کل فاکتور: ۳٪ از ۱۰ میلیون، ۵٪ از ۳۰ میلیون، ۷٪ از ۵۰ میلیون، ۹٪ از ۸۰ میلیون، ۱۰٪ از ۱۰۰ میلیون و ۱۲٪ از ۱۵۰ میلیون تومان.</p>
        <p>برای سفارش بالای ۷۰۰ میلیون تومان یا ۱۸۰ کارتن (خاور پر)، تخفیف بیشتر و ارسال رایگان داخل تهران با هماهنگی تلفنی.</p>
        <h2>بسته‌بندی</h2>
        <p>محصولات در بسته‌بندی شیرینگ یا فله عرضه می‌شوند — بسته به نوع ظرف و نیاز کسب‌وکار شما.</p>
        <h2>چطور ظرف مناسب انتخاب کنیم؟</h2>
        <ul>
        <li><strong>نوع غذا:</strong> غذای مرطوب، سوپ یا خشک — ظرف و درب مناسب انتخاب کنید.</li>
        <li><strong>مایکروویو:</strong> برای گرم‌کردن، ظروف مایکروویوی آملون را انتخاب کنید.</li>
        <li><strong>حجم:</strong> ظرف تک‌خانه، دوخانه یا سه‌خانه بسته به منوی شما.</li>
        </ul>
        <h2>مراحل سفارش در موثقی</h2>
        <ol>
        <li>محصول را در فروگاه پیدا کنید یا با ما تماس بگیرید.</li>
        <li>استعلام قیمت و موجودی دریافت کنید.</li>
        <li>سفارش را ثبت و زمان ارسال را هماهنگ کنید.</li>
        </ol>
        <p>برای مشاوره رایگان با کارشناسان فروش تماس بگیرید یا از فرم استعلام در صفحه محصول استفاده کنید.</p>
        """;

    static string BlogMicrowaveContent() => """
        <p>ظروف مایکروویوی <strong>آملون</strong> از نشاسته گیاهی ساخته شده‌اند و برای گرم‌کردن غذا در مایکروویو طراحی شده‌اند — گزینه‌ای ایمن‌تر و پایدارتر برای رستوران و فست‌فود.</p>
        <h2>نکات استفاده در مایکروویو</h2>
        <ul>
        <li>درب را مگر در دستورالعمل محصول، نیمه‌باز بگذارید تا بخار خارج شود.</li>
        <li>از گرم‌کردن بیش از حد دمای توصیه‌شده خودداری کنید.</li>
        <li>ظروف ترک‌خورده یا آسیب‌دیده را استفاده نکنید.</li>
        </ul>
        <h2>نگهداری در انبار</h2>
        <p>ظروف را در محیط خشک، دور از نور مستقیم و رطوبت بالا نگه دارید. پالت‌ها را روی سطح صاف چیدمان کنید تا تغییر شکل ندهند.</p>
        """;

    static string BlogWhyMovasseghiContent() => """
        <p><strong>فروشگاه موثقی</strong> نمایندگی رسمی کارخانه آملون در تهران است. تمرکز ما فقط روی ظروف یکبار مصرف گیاهی (نشاسته) اصل کارخانه است.</p>
        <h2>مزیت خرید از موثقی</h2>
        <ul>
        <li>✅ اصل کارخانه آملون — بدون واسطه غیررسمی</li>
        <li>📦 حداقل ۱۰ میلیون تومان · تخفیف تا ۱۲٪</li>
        <li>☎️ مشاوره رایگان انتخاب محصول</li>
        <li>🚚 ارسال به تهران و شهرستان‌ها</li>
        </ul>
        <p>تیم ما با انواع کسب‌وکارهای غذایی همکاری دارد و در انتخاب ظرف مناسب منوی شما راهنمایی می‌کند.</p>
        """;

    static string BlogPlantVsPlasticContent() => """
        <p>بسیاری از کسب‌وکارها بین ظروف <strong>گیاهی</strong> و <strong>پلاستیکی</strong> مردد هستند. ظروف گیاهی آملون از نشاسته renewably sourced ساخته می‌شوند.</p>
        <h2>مزایای ظروف گیاهی</h2>
        <ul>
        <li>تصویر برند سبز و مسئولانه برای مشتریان</li>
        <li>مناسب سرو غذای داغ و بسیاری از کاربردهای مایکروویو</li>
        <li>هم‌راستا با روند کاهش پلاستیک یکبار مصرف</li>
        </ul>
        <h2>کی پلاستیک منطقی‌تر است؟</h2>
        <p>برای کاربردهای بسیار خاص با نیازهای غیراستاندارد، ممکن است گزینه‌های دیگر بررسی شوند — کارشناسان موثقی در تماس رایگان راهنمایی می‌کنند.</p>
        """;

    static string NewsStockContent() => """
        <p>سری جدید محصولات <strong>آملون</strong> شامل ظروف غذا، کاسه و لیوان گیاهی به انبار فروشگاه موثقی در تهران رسید.</p>
        <p>مشتریان عمده می‌توانند از طریق فروگاه آنلاین یا تماس با واحد فروش، لیست کامل موجودی و قیمت پخش را دریافت کنند. حداقل سفارش ۱۰ میلیون تومان.</p>
        """;

    static string NewsDiscountContent() => """
        <p>فروشگاه موثقی <strong>تخفیف پلکانی عمده</strong> را برای سفارش‌های بالای ۱۰ میلیون تومان فعال کرده است — تا ۱۲٪ روی کل فاکتور.</p>
        <p>برای سفارش‌های بزرگ (۱۸۰ کارتن / ۷۰۰ میلیون+) جهت تخفیف ویژه و ارسال رایگان تهران با واحد فروش تماس بگیرید.</p>
        """;

    static string NewsInquiryContent() => """
        <p>فروشگاه موثقی <strong>سامانه استعلام قیمت آنلاین</strong> را راه‌اندازی کرد. از هر صفحه محصول می‌توانید درخواست قیمت عمده ثبت کنید.</p>
        <p>پاسخ‌گویی در ساعات کاری — برای سفارش فوری با شماره تماس درج‌شده در سایت تماس بگیرید.</p>
        """;

    static string NewsDealershipContent() => """
        <p><strong>فروشگاه موثقی</strong> به‌عنوان نماینده رسمی کارخانه آملون در تهران فعالیت می‌کند و تمامی محصولات از منبع اصل تأمین می‌شوند.</p>
        <p>این همکاری دسترسی مستقیم به کاتالوگ کامل ظروف گیاهی، پشتیبانی فنی و قیمت پخش را برای مشتریان عمده فراهم می‌کند.</p>
        """;

    static string NewsDeliveryContent() => """
        <p>زنجیره توزیع فروشگاه موثقی بهبود یافت تا سفارشات عمده در <strong>تهران و شهرستان‌ها</strong> سریع‌تر تحویل شوند.</p>
        <p>پس از ثبت سفارش، زمان تقریبی ارسال با شما هماهنگ می‌شود. برای سفارش‌های فوری با واحد هماهنگی تماس بگیرید.</p>
        """;

    static async Task EnsureWholesaleContentTextAsync(ApplicationDbContext db, ILogger logger)
    {
        foreach (var post in await db.BlogPosts.Where(b => b.Excerpt != null && b.Excerpt.Contains("۵۰ کارتن")).ToListAsync())
        {
            post.Excerpt = post.Excerpt!.Replace("۵۰ کارتن", "۱۰ میلیون تومان");
            if (post.MetaDescription?.Contains("۵۰ کارتن") == true)
                post.MetaDescription = post.MetaDescription.Replace("۵۰ کارتن", "۱۰ میلیون تومان");
        }
        foreach (var news in await db.NewsItems.Where(n => n.Excerpt != null && n.Excerpt.Contains("۵۰ کارتن")).ToListAsync())
        {
            news.Excerpt = news.Excerpt!.Replace("۵۰ کارتن", "۱۰ میلیون تومان");
            if (news.MetaDescription?.Contains("۵۰ کارتن") == true)
                news.MetaDescription = news.MetaDescription.Replace("۵۰ کارتن", "۱۰ میلیون تومان");
        }
        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Updated wholesale policy text in blog/news content");
        }
    }

    static async Task EnsureContentFeaturedImagesAsync(ApplicationDbContext db, ILogger logger)
    {
        var newsImages = new Dictionary<string, string>
        {
            ["new-amelon-stock-arrival"] = "/images/products/0071/01.webp",
            ["fall-wholesale-discount"] = "/images/products/0075/01.webp",
            ["online-price-inquiry-launch"] = "/images/products/0072/01.webp",
            ["official-amelon-dealership-tehran"] = "/images/products/0074/01.webp",
            ["faster-wholesale-delivery"] = "/images/products/0089/01.webp"
        };
        var blogImages = new Dictionary<string, string>
        {
            ["wholesale-guide-plant-based"] = "/images/products/0076/01.webp",
            ["microwave-containers-guide"] = "/images/products/0088/01.webp",
            ["why-movasseghi-official"] = "/images/products/0046/01.webp",
            ["plant-vs-plastic-containers"] = "/images/products/0093/01.webp"
        };

        var updated = 0;
        foreach (var (slug, url) in newsImages)
        {
            var item = await db.NewsItems.FirstOrDefaultAsync(n => n.Slug == slug);
            if (item == null) continue;
            if (string.IsNullOrEmpty(item.FeaturedImageUrl) || item.FeaturedImageUrl.Contains("originallogo", StringComparison.OrdinalIgnoreCase))
            {
                item.FeaturedImageUrl = url;
                updated++;
            }
        }
        foreach (var (slug, url) in blogImages)
        {
            var post = await db.BlogPosts.FirstOrDefaultAsync(b => b.Slug == slug);
            if (post == null) continue;
            if (string.IsNullOrEmpty(post.FeaturedImageUrl) || post.FeaturedImageUrl.Contains("originallogo", StringComparison.OrdinalIgnoreCase))
            {
                post.FeaturedImageUrl = url;
                updated++;
            }
        }
        if (updated > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Updated featured images on {Count} blog/news items", updated);
        }
    }

    static async Task EnsureNewsItemsTableAsync(ApplicationDbContext db, ILogger logger)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS NewsItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Slug TEXT NOT NULL,
                    Excerpt TEXT NULL,
                    Content TEXT NOT NULL,
                    FeaturedImageUrl TEXT NULL,
                    MetaTitle TEXT NULL,
                    MetaDescription TEXT NULL,
                    MetaKeywords TEXT NULL,
                    IsPublished INTEGER NOT NULL DEFAULT 0,
                    IsBreaking INTEGER NOT NULL DEFAULT 0,
                    PublishedAt TEXT NULL,
                    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                    UpdatedAt TEXT NOT NULL DEFAULT (datetime('now'))
                )
                """);
            await db.Database.ExecuteSqlRawAsync("""
                CREATE UNIQUE INDEX IF NOT EXISTS IX_NewsItems_Slug ON NewsItems (Slug)
                """);
            logger.LogInformation("Ensured NewsItems table exists");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "NewsItems table setup skipped or failed");
        }
    }

    static async Task EnsureProductVariantColumnsAsync(ApplicationDbContext db, ILogger logger)
    {
        foreach (var (name, ddl) in new[] { ("SellUnit", "INTEGER NOT NULL DEFAULT 0"), ("PacksPerCarton", "INTEGER NOT NULL DEFAULT 0") })
            await AddSqliteColumnIfMissingAsync(db, logger, "ProductVariants", name, ddl, logAdded: false);
    }

    static async Task EnsureProductImageRoleColumnAsync(ApplicationDbContext db, ILogger logger)
        => await AddSqliteColumnIfMissingAsync(db, logger, "ProductImages", "Role", "INTEGER NOT NULL DEFAULT 1");

    static async Task EnsureProductImageAltTextColumnAsync(ApplicationDbContext db, ILogger logger)
        => await AddSqliteColumnIfMissingAsync(db, logger, "ProductImages", "AltText", "TEXT NULL");

    static async Task BackfillProductImageRolesAsync(ApplicationDbContext db, ILogger logger)
    {
        var images = await db.ProductImages.ToListAsync();
        if (images.Count == 0) return;

        var changed = 0;
        foreach (var group in images.GroupBy(i => i.ProductId))
        {
            var productImages = group.OrderBy(i => i.SortOrder).ThenBy(i => i.Id).ToList();
            ProductImage? scene = null;
            ProductImage? studio = null;

            foreach (var img in productImages)
            {
                if (img.Url.Contains("hero", StringComparison.OrdinalIgnoreCase)
                    || img.Url.Contains("scene", StringComparison.OrdinalIgnoreCase))
                {
                    if (img.Role != ProductImageRole.Scene) { img.Role = ProductImageRole.Scene; changed++; }
                    scene ??= img;
                    continue;
                }

                if (img.Url.Contains("studio", StringComparison.OrdinalIgnoreCase)
                    || img.Url.Contains("-02-", StringComparison.OrdinalIgnoreCase))
                {
                    if (img.Role != ProductImageRole.Studio) { img.Role = ProductImageRole.Studio; changed++; }
                    studio ??= img;
                }
            }

            if (productImages.Count == 1)
            {
                var only = productImages[0];
                if (only.Role != ProductImageRole.Studio) { only.Role = ProductImageRole.Studio; changed++; }
                if (!only.IsPrimary) { only.IsPrimary = true; changed++; }
                only.SortOrder = 1;
                continue;
            }

            scene ??= productImages.FirstOrDefault(i => i.SortOrder == 0);
            studio ??= productImages.FirstOrDefault(i => i != scene) ?? productImages.Last();

            if (scene != null)
            {
                if (scene.Role != ProductImageRole.Scene) { scene.Role = ProductImageRole.Scene; changed++; }
                scene.SortOrder = 0;
                if (scene.IsPrimary) { scene.IsPrimary = false; changed++; }
            }

            if (studio != null)
            {
                if (studio.Role != ProductImageRole.Studio) { studio.Role = ProductImageRole.Studio; changed++; }
                studio.SortOrder = 1;
                if (!studio.IsPrimary) { studio.IsPrimary = true; changed++; }
            }

            foreach (var img in productImages.Where(i => i != scene && i != studio))
            {
                if (img.IsPrimary) { img.IsPrimary = false; changed++; }
            }
        }

        if (changed > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Backfilled ProductImages.Role on {Count} rows", changed);
        }
    }

    static async Task EnsureStudioTablesAsync(ApplicationDbContext db, ILogger logger)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS SeoProfiles (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    EntityType TEXT NOT NULL,
                    EntityId INTEGER NOT NULL,
                    FocusKeyword TEXT NULL,
                    SecondaryKeyword TEXT NULL,
                    MetaTitle TEXT NULL,
                    MetaDescription TEXT NULL,
                    MetaKeywords TEXT NULL,
                    AiSummary TEXT NULL,
                    FaqJson TEXT NULL,
                    SeoScore INTEGER NOT NULL DEFAULT 0,
                    AeoScore INTEGER NOT NULL DEFAULT 0,
                    GeoScore INTEGER NOT NULL DEFAULT 0,
                    TotalScore INTEGER NOT NULL DEFAULT 0,
                    ScoreIssuesJson TEXT NULL,
                    IsPublishReady INTEGER NOT NULL DEFAULT 0,
                    LastAuditedAt TEXT NULL,
                    UpdatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                    UNIQUE(EntityType, EntityId)
                );
                """);

            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS PillarKeywords (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Phrase TEXT NOT NULL UNIQUE,
                    Cluster TEXT NOT NULL DEFAULT 'general',
                    Priority INTEGER NOT NULL DEFAULT 10,
                    OwnerPageKey TEXT NOT NULL DEFAULT '',
                    SearchIntent TEXT NOT NULL DEFAULT 'commercial',
                    IsActive INTEGER NOT NULL DEFAULT 1
                );
                """);

            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS GrowthActions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ActionType TEXT NOT NULL,
                    Priority TEXT NOT NULL DEFAULT 'medium',
                    Title TEXT NOT NULL,
                    Description TEXT NULL,
                    EntityType TEXT NULL,
                    EntityId INTEGER NULL,
                    Status TEXT NOT NULL DEFAULT 'open',
                    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                    CompletedAt TEXT NULL
                );
                """);

            // ── Auto-Fix Ultimate — ستون‌های امتیاز ابعاد و گزارش رقابتی ──
            foreach (var (name, ddl) in new Dictionary<string, string>
            {
                ["ContentScore"] = "INTEGER NOT NULL DEFAULT 0",
                ["SemanticScore"] = "INTEGER NOT NULL DEFAULT 0",
                ["IntentScore"] = "INTEGER NOT NULL DEFAULT 0",
                ["ImageScore"] = "INTEGER NOT NULL DEFAULT 0",
                ["SchemaScore"] = "INTEGER NOT NULL DEFAULT 0",
                ["ProductDataScore"] = "INTEGER NOT NULL DEFAULT 0",
                ["InternalLinkingScore"] = "INTEGER NOT NULL DEFAULT 0",
                ["TechnicalScore"] = "INTEGER NOT NULL DEFAULT 0",
                ["CompetitiveStrength"] = "INTEGER NOT NULL DEFAULT 0",
                ["RankingReadiness"] = "INTEGER NOT NULL DEFAULT 0",
                ["UltimateJson"] = "TEXT NULL",
                ["LockProductDescription"] = "INTEGER NOT NULL DEFAULT 0",
                ["LockProductShortDescription"] = "INTEGER NOT NULL DEFAULT 0",
                ["LockCategoryLead"] = "INTEGER NOT NULL DEFAULT 0",
                ["LockArticleExcerpt"] = "INTEGER NOT NULL DEFAULT 0",
                ["LockArticleContent"] = "INTEGER NOT NULL DEFAULT 0"
            })
                await AddSqliteColumnIfMissingAsync(db, logger, "SeoProfiles", name, ddl);

            logger.LogInformation("Ensured Studio tables (SeoProfiles, PillarKeywords, GrowthActions)");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Studio tables ensure failed");
        }
    }

    static async Task EnsureProductKeywordColumnsAsync(ApplicationDbContext db, ILogger logger)
    {
        foreach (var (name, ddl) in new Dictionary<string, string>
        {
            ["KeywordPrimary"] = "TEXT NULL",
            ["KeywordSecondary"] = "TEXT NULL",
            ["Gtin"] = "TEXT NULL",
            ["Mpn"] = "TEXT NULL",
            ["IsHomeSpecialOffer"] = "INTEGER NOT NULL DEFAULT 0",
            ["IsHomeFeatured"] = "INTEGER NOT NULL DEFAULT 0",
        })
            await AddSqliteColumnIfMissingAsync(db, logger, "Products", name, ddl);
    }

    static async Task EnsureProductReviewsTableAsync(ApplicationDbContext db, ILogger logger)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS ProductReviews (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProductId INTEGER NOT NULL,
                    ParentReviewId INTEGER NULL,
                    AuthorDisplayName TEXT NOT NULL,
                    Body TEXT NOT NULL,
                    Rating INTEGER NOT NULL DEFAULT 0,
                    Status INTEGER NOT NULL DEFAULT 0,
                    IsStoreReply INTEGER NOT NULL DEFAULT 0,
                    IsSeoSuggested INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL,
                    ModeratedAt TEXT NULL,
                    FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE,
                    FOREIGN KEY (ParentReviewId) REFERENCES ProductReviews(Id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS IX_ProductReviews_ProductId_Status ON ProductReviews (ProductId, Status);
                """);
            logger.LogInformation("Ensured ProductReviews table");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ProductReviews table ensure failed");
        }
    }

    static async Task EnsureRankSnapshotsTableAsync(ApplicationDbContext db, ILogger logger)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS RankSnapshots (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Keyword TEXT NOT NULL,
                    KeywordType TEXT NOT NULL DEFAULT 'pillar',
                    EntityId INTEGER NULL,
                    Position INTEGER NOT NULL DEFAULT 0,
                    TargetUrl TEXT NULL,
                    Source TEXT NOT NULL DEFAULT 'manual',
                    CheckedAt TEXT NOT NULL DEFAULT (datetime('now'))
                );
                """);
            logger.LogInformation("Ensured RankSnapshots table exists");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "RankSnapshots table ensure failed");
        }
    }

    static async Task EnsureEditorialTopicQueueTableAsync(ApplicationDbContext db, ILogger logger)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS EditorialTopicQueue (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FocusKeyword TEXT NOT NULL,
                    SecondaryKeyword TEXT NOT NULL DEFAULT '',
                    ProposedTitle TEXT NOT NULL,
                    Angle TEXT NOT NULL DEFAULT 'guide',
                    PillarPriority INTEGER NOT NULL DEFAULT 50,
                    OpportunityScore REAL NOT NULL DEFAULT 0,
                    Source TEXT NOT NULL DEFAULT 'pillar',
                    Status TEXT NOT NULL DEFAULT 'planned',
                    BlogPostId INTEGER NULL,
                    ScheduledPublishUtc TEXT NULL,
                    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                    CompletedAt TEXT NULL,
                    LastError TEXT NULL
                );
                """);
            logger.LogInformation("Ensured EditorialTopicQueue table exists");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "EditorialTopicQueue table ensure failed");
        }
    }

    static async Task EnsurePillarPagesAsync(ApplicationDbContext db, ILogger logger)
    {
        var existingKeys = await db.CmsPages.Select(p => p.Key).ToListAsync();
        var added = 0;
        foreach (var page in PillarPageContent.GetPages())
        {
            if (existingKeys.Contains(page.Key)) continue;
            db.CmsPages.Add(page);
            added++;
        }
        if (added > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} pillar CMS pages", added);
        }
    }

    static async Task SeedStudioContentAsync(ApplicationDbContext db, ILogger logger)
    {
        if (!await db.HomeSections.AnyAsync())
        {
            db.HomeSections.AddRange(StudioSeedData.GetHomeSections());
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded home sections");
        }
        if (!await db.NavItems.AnyAsync())
        {
            db.NavItems.AddRange(StudioSeedData.GetNavItems());
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded nav items");
        }
        await EnsureFooterNavItemsAsync(db, logger);
        if (!await db.ContentAtoms.AnyAsync())
        {
            db.ContentAtoms.AddRange(StudioSeedData.GetContentAtoms());
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded content atoms");
        }
        if (!await db.AuthorityCheckItems.AnyAsync())
        {
            db.AuthorityCheckItems.AddRange(StudioSeedData.GetAuthorityItems());
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded authority checklist");
        }
    }

    static async Task EnsureFooterNavItemsAsync(ApplicationDbContext db, ILogger logger)
    {
        var footerZones = new[] { "footer-shop", "footer-support", "footer-wholesale" };
        var seed = StudioSeedData.GetNavItems().Where(n => footerZones.Contains(n.Zone)).ToList();
        foreach (var zone in footerZones)
        {
            if (await db.NavItems.AnyAsync(n => n.Zone == zone)) continue;
            var items = seed.Where(n => n.Zone == zone);
            db.NavItems.AddRange(items);
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded footer nav zone {Zone}", zone);
        }

        await FixLegacyFooterProductTypeNavAsync(db, logger);
    }

    /// <summary>نوع محصول در فیلتر «ظرف غذا» است نه «ظروف غذا» — لینک فوتر خالی نمی‌ماند.</summary>
    static async Task FixLegacyFooterProductTypeNavAsync(ApplicationDbContext db, ILogger logger)
    {
        var correct = "/Catalog/ByType?type=" + Uri.EscapeDataString("ظرف غذا");
        var broken = await db.NavItems
            .Where(n => n.Url.Contains("ظروف") && n.Url.Contains("غذا") && n.Url != correct)
            .ToListAsync();
        if (broken.Count == 0) return;
        foreach (var item in broken)
        {
            item.Label = "ظرف غذا";
            item.Url = correct;
        }
        await db.SaveChangesAsync();
        logger.LogInformation("Fixed {Count} footer nav link(s) for product type ظرف غذا", broken.Count);
    }

    static async Task EnsureStudioContentTablesAsync(ApplicationDbContext db, ILogger logger)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS HomeSections (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Key TEXT NOT NULL UNIQUE,
                    Title TEXT NOT NULL,
                    Subtitle TEXT NULL,
                    BodyHtml TEXT NULL,
                    ConfigJson TEXT NULL,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    SortOrder INTEGER NOT NULL DEFAULT 0,
                    UpdatedAt TEXT NOT NULL DEFAULT (datetime('now'))
                );
                CREATE TABLE IF NOT EXISTS NavItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Zone TEXT NOT NULL,
                    Label TEXT NOT NULL,
                    Url TEXT NOT NULL,
                    Icon TEXT NULL,
                    SortOrder INTEGER NOT NULL DEFAULT 0,
                    IsActive INTEGER NOT NULL DEFAULT 1
                );
                CREATE TABLE IF NOT EXISTS ContentAtoms (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Key TEXT NOT NULL UNIQUE,
                    Title TEXT NOT NULL,
                    Body TEXT NULL,
                    ImageUrl TEXT NULL,
                    CtaText TEXT NULL,
                    CtaUrl TEXT NULL,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    UpdatedAt TEXT NOT NULL DEFAULT (datetime('now'))
                );
                CREATE TABLE IF NOT EXISTS CompetitorSnapshots (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Keyword TEXT NOT NULL,
                    RankPosition INTEGER NOT NULL,
                    Domain TEXT NOT NULL,
                    Title TEXT NULL,
                    Url TEXT NULL,
                    WordCountEstimate INTEGER NULL,
                    HasFaqSchema INTEGER NOT NULL DEFAULT 0,
                    CheckedAt TEXT NOT NULL DEFAULT (datetime('now'))
                );
                CREATE TABLE IF NOT EXISTS GrowthReports (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    WeekStart TEXT NOT NULL,
                    Title TEXT NOT NULL,
                    BodyMarkdown TEXT NOT NULL,
                    MetricsJson TEXT NULL,
                    CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
                );
                CREATE TABLE IF NOT EXISTS AuthorityCheckItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Key TEXT NOT NULL UNIQUE,
                    Title TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    HelpUrl TEXT NULL,
                    IsDone INTEGER NOT NULL DEFAULT 0,
                    SortOrder INTEGER NOT NULL DEFAULT 0,
                    CompletedAt TEXT NULL
                );
                """);
            logger.LogInformation("Ensured Studio content tables");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Studio content tables ensure failed");
        }

        await EnsureAnalyticsTablesAsync(db, logger);
    }

    static async Task EnsureHomeStoriesTablesAsync(ApplicationDbContext db, ILogger logger)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS HomeStories (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    StoryKey TEXT NOT NULL UNIQUE,
                    Label TEXT NOT NULL,
                    CoverImageUrl TEXT NULL,
                    IconKey TEXT NOT NULL DEFAULT 'all',
                    SortOrder INTEGER NOT NULL DEFAULT 0,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    UpdatedAt TEXT NOT NULL DEFAULT (datetime('now'))
                );
                CREATE TABLE IF NOT EXISTS HomeStorySlides (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    HomeStoryId INTEGER NOT NULL,
                    ImageUrl TEXT NOT NULL DEFAULT '',
                    Title TEXT NOT NULL DEFAULT '',
                    LinkUrl TEXT NOT NULL DEFAULT '/Catalog',
                    SortOrder INTEGER NOT NULL DEFAULT 0,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    FOREIGN KEY (HomeStoryId) REFERENCES HomeStories(Id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS IX_HomeStorySlides_StoryId_Sort ON HomeStorySlides(HomeStoryId, SortOrder);
                """);
            logger.LogInformation("Ensured home stories tables");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Home stories tables ensure failed");
        }
    }

    /// <summary>
    /// استوری‌های صفحه اصلی از محصولات و دسته‌ها ساخته می‌شوند (مثل قبل).
    /// اگر فقط ۳ استوری پیش‌فرض seed شده باشد، یک‌بار به لیست کامل ارتقا می‌دهد.
    /// </summary>
    static async Task EnsureHomeStoriesPopulatedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (!await db.Products.AnyAsync()) return;

        var homeStories = scope.ServiceProvider.GetRequiredService<IHomeStoryService>();
        var count = await db.HomeStories.CountAsync();

        if (count == 0)
        {
            var imported = await homeStories.ImportFromProductsAsync();
            logger.LogInformation("Imported {Count} home stories from products", imported);
            return;
        }

        if (count != 3) return;

        var keys = await db.HomeStories.Select(s => s.StoryKey).ToListAsync();
        var defaultKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "all", "wholesale", "official" };
        if (!keys.All(k => defaultKeys.Contains(k))) return;

        var hasCustomCover = await db.HomeStories.AnyAsync(s => s.CoverImageUrl != null && s.CoverImageUrl != "");
        if (hasCustomCover) return;

        var upgraded = await homeStories.ImportFromProductsAsync();
        logger.LogInformation("Upgraded default home stories to {Count} from products", upgraded);
    }

    /// <summary>
    /// جداول آنالیتیکس + ایندکس‌های گزارش‌ها.
    /// ایندکس‌ها بر پایه بازه تاریخ ساخته شده‌اند چون هر گزارش اول بر تاریخ فیلتر می‌کند.
    /// </summary>
    static async Task EnsureAnalyticsTablesAsync(ApplicationDbContext db, ILogger logger)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS PageVisits (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Path TEXT NOT NULL,
                    Query TEXT NULL,
                    PageType TEXT NOT NULL DEFAULT 'other',
                    EntityId INTEGER NULL,
                    Title TEXT NULL,
                    SessionKey TEXT NOT NULL DEFAULT '',
                    VisitorKey TEXT NOT NULL DEFAULT '',
                    IsNewVisitor INTEGER NOT NULL DEFAULT 0,
                    IsAuthenticated INTEGER NOT NULL DEFAULT 0,
                    ReferrerHost TEXT NULL,
                    Channel TEXT NOT NULL DEFAULT 'direct',
                    SearchTerm TEXT NULL,
                    DeviceType TEXT NOT NULL DEFAULT 'desktop',
                    IsBot INTEGER NOT NULL DEFAULT 0,
                    UtmSource TEXT NULL,
                    UtmMedium TEXT NULL,
                    UtmCampaign TEXT NULL,
                    ServerMs INTEGER NOT NULL DEFAULT 0,
                    StatusCode INTEGER NOT NULL DEFAULT 200,
                    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                    DateKey INTEGER NOT NULL DEFAULT 0,
                    HourOfDay INTEGER NOT NULL DEFAULT 0
                );
                CREATE INDEX IF NOT EXISTS IX_PageVisits_DateKey ON PageVisits(DateKey);
                CREATE INDEX IF NOT EXISTS IX_PageVisits_DateKey_IsBot ON PageVisits(DateKey, IsBot);
                CREATE INDEX IF NOT EXISTS IX_PageVisits_DateKey_Path ON PageVisits(DateKey, Path);
                CREATE INDEX IF NOT EXISTS IX_PageVisits_DateKey_Channel ON PageVisits(DateKey, Channel);
                CREATE INDEX IF NOT EXISTS IX_PageVisits_PageType_EntityId ON PageVisits(PageType, EntityId);
                CREATE INDEX IF NOT EXISTS IX_PageVisits_SessionKey ON PageVisits(SessionKey);
                CREATE INDEX IF NOT EXISTS IX_PageVisits_VisitorKey ON PageVisits(VisitorKey);

                CREATE TABLE IF NOT EXISTS SiteEvents (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Path TEXT NULL,
                    SessionKey TEXT NOT NULL DEFAULT '',
                    VisitorKey TEXT NOT NULL DEFAULT '',
                    EntityId INTEGER NULL,
                    EntityType TEXT NULL,
                    Label TEXT NULL,
                    Value TEXT NULL,
                    Quantity INTEGER NULL,
                    Channel TEXT NOT NULL DEFAULT 'direct',
                    DeviceType TEXT NOT NULL DEFAULT 'desktop',
                    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                    DateKey INTEGER NOT NULL DEFAULT 0
                );
                CREATE INDEX IF NOT EXISTS IX_SiteEvents_DateKey ON SiteEvents(DateKey);
                CREATE INDEX IF NOT EXISTS IX_SiteEvents_DateKey_Name ON SiteEvents(DateKey, Name);
                CREATE INDEX IF NOT EXISTS IX_SiteEvents_Name_EntityId ON SiteEvents(Name, EntityId);
                CREATE INDEX IF NOT EXISTS IX_SiteEvents_SessionKey ON SiteEvents(SessionKey);

                -- ایندکس‌های عملکردی که وجود نداشتند: کاتالوگ و لیست ادمین
                -- روی این ستون‌ها فیلتر می‌کنند و بدون ایندکس کل جدول اسکن می‌شد.
                CREATE INDEX IF NOT EXISTS IX_Products_IsActive ON Products(IsActive);
                CREATE INDEX IF NOT EXISTS IX_Products_CategoryId_IsActive ON Products(CategoryId, IsActive);
                CREATE INDEX IF NOT EXISTS IX_Products_IsActive_UpdatedAt ON Products(IsActive, UpdatedAt);
                CREATE INDEX IF NOT EXISTS IX_NavItems_Zone_IsActive ON NavItems(Zone, IsActive, SortOrder);
                CREATE INDEX IF NOT EXISTS IX_Orders_CreatedAt ON Orders(CreatedAt);
                CREATE INDEX IF NOT EXISTS IX_PurchaseInquiries_IsHandled ON PurchaseInquiries(IsHandled, CreatedAt);
                CREATE INDEX IF NOT EXISTS IX_RankSnapshots_Keyword ON RankSnapshots(Keyword, CheckedAt);
                """);

            // WAL نوشتن آنالیتیکس در پس‌زمینه را از خواندن صفحات جدا می‌کند
            // تا ثبت بازدید، بارگذاری صفحه را قفل نکند.
            await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
            await db.Database.ExecuteSqlRawAsync("PRAGMA synchronous=NORMAL;");

            logger.LogInformation("Ensured analytics tables and performance indexes");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Analytics tables ensure failed");
        }
    }

    static async Task<bool> SqliteColumnExistsAsync(ApplicationDbContext db, string table, string column)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({table})";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    static async Task AddSqliteColumnIfMissingAsync(
        ApplicationDbContext db,
        ILogger logger,
        string table,
        string column,
        string ddl,
        bool logAdded = true)
    {
        if (await SqliteColumnExistsAsync(db, table, column))
            return;

        await db.Database.ExecuteSqlRawAsync($"ALTER TABLE {table} ADD COLUMN {column} {ddl}");
        if (logAdded)
            logger.LogInformation("Added {Table}.{Column}", table, column);
    }
}
