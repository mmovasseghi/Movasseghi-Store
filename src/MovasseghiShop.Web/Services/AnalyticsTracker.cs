using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

public interface IAnalyticsTracker
{
    void TrackEvent(
        string name,
        HttpContext http,
        string? label = null,
        int? entityId = null,
        string? entityType = null,
        decimal? value = null,
        int? quantity = null);
}

/// <summary>
/// ثبت بازدید و رویداد در یک صف در حافظه و نوشتن دسته‌ای در پس‌زمینه.
/// دلیل: نوشتن همگام در SQLite به مسیر هر درخواست تأخیر اضافه می‌کرد
/// و دقیقاً همان چیزی را خراب می‌کرد که می‌خواهیم بسنجیم (سرعت و CWV).
/// </summary>
public class AnalyticsTracker(ILogger<AnalyticsTracker> log) : IAnalyticsTracker
{
    // سقف صف — اگر مصرف‌کننده عقب بیفتد، حافظه بی‌نهایت رشد نکند
    const int MaxQueue = 5_000;

    readonly ConcurrentQueue<PageVisit> _visits = new();
    readonly ConcurrentQueue<SiteEvent> _events = new();

    public int QueuedVisits => _visits.Count;

    public void Enqueue(PageVisit visit)
    {
        if (_visits.Count >= MaxQueue)
        {
            _visits.TryDequeue(out _);
            log.LogWarning("صف آنالیتیکس پر شد — قدیمی‌ترین بازدید حذف شد.");
        }
        _visits.Enqueue(visit);
    }

    public void TrackEvent(
        string name,
        HttpContext http,
        string? label = null,
        int? entityId = null,
        string? entityType = null,
        decimal? value = null,
        int? quantity = null)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        if (_events.Count >= MaxQueue) { _events.TryDequeue(out _); return; }

        AnalyticsIdentity.Identity identity;
        try
        {
            identity = AnalyticsIdentity.Resolve(http);
        }
        catch
        {
            // پاسخ شروع شده و کوکی قابل ست شدن نیست — رویداد از دست می‌رود
            // ولی درخواست کاربر نباید بشکند.
            return;
        }

        var now = DateTime.UtcNow;

        _events.Enqueue(new SiteEvent
        {
            Name = name,
            Path = http.Request.Path.Value,
            SessionKey = identity.SessionKey,
            VisitorKey = identity.VisitorKey,
            EntityId = entityId,
            EntityType = entityType,
            Label = Truncate(label, 200),
            Value = value,
            Quantity = quantity,
            Channel = AnalyticsClassifier.Channel(http),
            DeviceType = AnalyticsClassifier.Device(http.Request.Headers.UserAgent.ToString()),
            CreatedAt = now,
            DateKey = AnalyticsClassifier.DateKey(now)
        });
    }

    /// <summary>هر دو صف را خالی و در دیتابیس می‌نویسد. توسط سرویس پس‌زمینه صدا زده می‌شود.</summary>
    public async Task<int> FlushAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var visits = Drain(_visits, 500);
        var events = Drain(_events, 500);
        if (visits.Count == 0 && events.Count == 0) return 0;

        try
        {
            if (visits.Count > 0) db.PageVisits.AddRange(visits);
            if (events.Count > 0) db.SiteEvents.AddRange(events);
            await db.SaveChangesAsync(ct);
            return visits.Count + events.Count;
        }
        catch (Exception ex)
        {
            // آنالیتیکس هرگز نباید سایت را بشکند — فقط لاگ می‌شود
            log.LogError(ex, "نوشتن دسته‌ای آنالیتیکس شکست خورد ({V} بازدید، {E} رویداد).",
                visits.Count, events.Count);
            return 0;
        }
    }

    static List<T> Drain<T>(ConcurrentQueue<T> queue, int max)
    {
        var items = new List<T>();
        while (items.Count < max && queue.TryDequeue(out var item)) items.Add(item);
        return items;
    }

    internal static string? Truncate(string? s, int max) =>
        string.IsNullOrEmpty(s) ? s : s.Length <= max ? s : s[..max];
}

/// <summary>
/// شناسه بازدیدکننده و نشست. IP خام هرگز ذخیره نمی‌شود — فقط هش نمکین.
/// </summary>
public static class AnalyticsIdentity
{
    const string VisitorCookie = "_mv";
    const string SessionItemKey = "__analytics_identity";

    // نمک در حافظه فرایند تولید می‌شود تا هش‌ها بین ری‌استارت‌ها
    // قابل ارتباط‌دادن با شخص نباشند، ولی در طول عمر فرایند پایدار بمانند.
    static readonly string Salt = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

    public record Identity(string VisitorKey, string SessionKey, bool IsNew);

    public static Identity Resolve(HttpContext http)
    {
        if (http.Items.TryGetValue(SessionItemKey, out var cached) && cached is Identity hit)
            return hit;

        var isNew = false;
        var visitorId = http.Request.Cookies[VisitorCookie];

        if (string.IsNullOrWhiteSpace(visitorId))
        {
            visitorId = Guid.NewGuid().ToString("N");
            isNew = true;
            // کوکی first-party و ضروری برای آمار خودِ سایت — بدون داده شخصی
            http.Response.Cookies.Append(VisitorCookie, visitorId, new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = http.Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddDays(365)
            });
        }

        var sessionId = SafeSessionId(http);

        var identity = new Identity(
            Hash(visitorId),
            Hash(sessionId ?? visitorId + ":" + DateTime.UtcNow.ToString("yyyyMMddHH")),
            isNew);

        http.Items[SessionItemKey] = identity;
        return identity;
    }

    /// <summary>
    /// Session روی همه مسیرها فعال نیست (مثلاً فایل استاتیک یا API)؛
    /// دسترسی بی‌محافظه استثنا می‌دهد و درخواست را می‌شکند.
    /// </summary>
    static string? SafeSessionId(HttpContext http)
    {
        try { return http.Session.IsAvailable ? http.Session.Id : null; }
        catch { return null; }
    }

    static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(Salt + value));
        return Convert.ToHexString(bytes)[..24];
    }
}

/// <summary>دسته‌بندی مسیر، منبع ورود و دستگاه.</summary>
public static class AnalyticsClassifier
{
    static readonly string[] BotMarkers =
    [
        "bot", "crawl", "spider", "slurp", "bingpreview", "facebookexternalhit",
        "headlesschrome", "lighthouse", "pagespeed", "gtmetrix", "pingdom",
        "ahrefs", "semrush", "mj12", "dotbot", "petalbot", "yandex", "applebot",
        "gptbot", "claudebot", "perplexity", "ccbot", "bytespider", "amazonbot"
    ];

    static readonly string[] SearchHosts =
    [
        "google.", "bing.", "yahoo.", "duckduckgo.", "yandex.", "baidu.",
        "ecosia.", "brave.", "ask.", "torob.", "emalls.", "chatgpt.", "openai.",
        "perplexity.", "claude."
    ];

    static readonly string[] SocialHosts =
    [
        "instagram.", "t.me", "telegram.", "whatsapp.", "wa.me", "facebook.",
        "fb.", "twitter.", "x.com", "linkedin.", "pinterest.", "youtube.",
        "aparat.", "rubika.", "eitaa.", "bale."
    ];

    public static int DateKey(DateTime utc)
    {
        var local = ToLocal(utc);
        return local.Year * 10000 + local.Month * 100 + local.Day;
    }

    public static int HourOfDay(DateTime utc) => ToLocal(utc).Hour;

    /// <summary>گزارش باید به وقت تهران باشد؛ گروه‌بندی UTC روز را جابه‌جا می‌کند.</summary>
    public static DateTime ToLocal(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).AddMinutes(210);

    public static bool IsBot(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return true;
        var ua = userAgent.ToLowerInvariant();
        return BotMarkers.Any(m => ua.Contains(m, StringComparison.Ordinal));
    }

    public static string Device(string userAgent)
    {
        if (IsBot(userAgent)) return "bot";
        var ua = userAgent.ToLowerInvariant();
        if (ua.Contains("ipad") || (ua.Contains("android") && !ua.Contains("mobile"))) return "tablet";
        if (ua.Contains("mobi") || ua.Contains("iphone") || ua.Contains("android")) return "mobile";
        return "desktop";
    }

    public static string? ReferrerHost(HttpContext http)
    {
        var referrer = http.Request.Headers.Referer.ToString();
        if (string.IsNullOrWhiteSpace(referrer)) return null;
        return Uri.TryCreate(referrer, UriKind.Absolute, out var uri)
            ? uri.Host.ToLowerInvariant()
            : null;
    }

    public static string Channel(HttpContext http)
    {
        var medium = http.Request.Query["utm_medium"].ToString().ToLowerInvariant();
        if (medium is "cpc" or "ppc" or "paid") return "paid";
        if (!string.IsNullOrWhiteSpace(http.Request.Query["utm_source"])) return "campaign";

        var host = ReferrerHost(http);
        if (host is null) return "direct";

        var self = http.Request.Host.Host.ToLowerInvariant();
        if (host == self || host.EndsWith("." + self, StringComparison.Ordinal)) return "internal";
        if (SearchHosts.Any(s => host.Contains(s, StringComparison.Ordinal))) return "organic";
        if (SocialHosts.Any(s => host.Contains(s, StringComparison.Ordinal))) return "social";
        return "referral";
    }

    /// <summary>نوع صفحه از مسیر — گزارش‌ها بدون تجزیه دوباره URL ساخته می‌شوند.</summary>
    public static (string Type, int? EntityId) Classify(HttpContext http)
    {
        var route = http.GetRouteData()?.Values;
        var controller = route?["controller"]?.ToString() ?? "";
        var action = route?["action"]?.ToString() ?? "";
        var path = http.Request.Path.Value ?? "/";

        var type = controller switch
        {
            "Home" => "home",
            "Catalog" => http.Request.Query.ContainsKey("category") ? "category" : "catalog",
            "Shop" when action == "Product" => "product",
            "Blog" when action == "Post" => "blog-post",
            "Blog" => "blog",
            "News" when action == "Article" => "news-article",
            "News" => "news",
            "Page" => "page",
            "Cart" => "cart",
            "Checkout" => "checkout",
            "Order" => "order",
            "Account" => "account",
            _ => path == "/" ? "home" : "other"
        };

        var id = route?["id"]?.ToString();
        return (type, int.TryParse(id, out var parsed) ? parsed : null);
    }

    /// <summary>Query را از پارامترهای حساس/بی‌ارزش پاک می‌کند.</summary>
    public static string? SafeQuery(HttpContext http)
    {
        if (!http.Request.QueryString.HasValue) return null;

        var keep = http.Request.Query
            .Where(kv => kv.Key is "category" or "productType" or "packaging" or "sort"
                or "capacity" or "compartments" or "microwave" or "page" or "q"
                or "utm_source" or "utm_medium" or "utm_campaign")
            .Select(kv => $"{kv.Key}={kv.Value}")
            .ToList();

        return keep.Count == 0 ? null : AnalyticsTracker.Truncate(string.Join("&", keep), 400);
    }
}

/// <summary>
/// میدل‌ویر ثبت بازدید. بعد از پاسخ اجرا می‌شود تا زمان سرور واقعی ثبت شود
/// و درخواست کاربر معطل نوشتن آمار نماند.
/// </summary>
public class AnalyticsMiddleware(RequestDelegate next, AnalyticsTracker tracker, ILogger<AnalyticsMiddleware> log)
{
    static readonly string[] IgnoredPrefixes =
    [
        "/css/", "/js/", "/lib/", "/images/", "/img/", "/uploads/", "/fonts/", "/media/",
        "/favicon", "/robots.txt", "/sitemap", "/.well-known/", "/Admin/", "/api/analytics"
    ];

    static readonly string[] IgnoredExtensions =
    [
        ".css", ".js", ".map", ".png", ".jpg", ".jpeg", ".webp", ".avif", ".gif",
        ".svg", ".ico", ".woff", ".woff2", ".ttf", ".xml", ".txt", ".json", ".webmanifest"
    ];

    public async Task InvokeAsync(HttpContext http)
    {
        var path = http.Request.Path.Value ?? "/";

        if (!ShouldTrack(http, path))
        {
            await next(http);
            return;
        }

        // شناسه بازدیدکننده باید قبل از شروع پاسخ حل شود، چون ممکن است
        // کوکی ست کند و بعد از نوشتن اولین بایت پاسخ، هدرها قابل تغییر نیستند.
        AnalyticsIdentity.Identity identity;
        try
        {
            identity = AnalyticsIdentity.Resolve(http);
        }
        catch (Exception ex)
        {
            log.LogDebug(ex, "تعیین شناسه بازدیدکننده ممکن نشد؛ این درخواست ثبت نمی‌شود.");
            await next(http);
            return;
        }

        var sw = Stopwatch.StartNew();
        await next(http);
        sw.Stop();

        try
        {
            Record(http, path, (int)sw.ElapsedMilliseconds, identity);
        }
        catch (Exception ex)
        {
            // ثبت آمار هرگز نباید به کاربر خطا برگرداند — ولی سکوت کامل هم
            // اشکال‌یابی را غیرممکن می‌کند، پس در سطح Debug لاگ می‌شود.
            log.LogDebug(ex, "ثبت بازدید برای «{Path}» شکست خورد.", path);
        }
    }

    static bool ShouldTrack(HttpContext http, string path)
    {
        if (!HttpMethods.IsGet(http.Request.Method)) return false;
        if (IgnoredPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase))) return false;
        if (IgnoredExtensions.Any(e => path.EndsWith(e, StringComparison.OrdinalIgnoreCase))) return false;
        return true;
    }

    void Record(HttpContext http, string path, int ms, AnalyticsIdentity.Identity identity)
    {
        // صفحه HTML واقعی — نه ریدایرکت، JSON یا فایل.
        // استثنا: ۴۰۴ که ممکن است بدنه نداشته باشد ولی برای گزارش لینک شکسته لازم است.
        var contentType = http.Response.ContentType ?? "";
        var isHtml = contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase);
        if (!isHtml && http.Response.StatusCode != StatusCodes.Status404NotFound) return;

        var userAgent = http.Request.Headers.UserAgent.ToString();
        var isBot = AnalyticsClassifier.IsBot(userAgent);
        var (pageType, entityId) = AnalyticsClassifier.Classify(http);
        var now = DateTime.UtcNow;

        tracker.Enqueue(new PageVisit
        {
            Path = AnalyticsTracker.Truncate(path, 300) ?? "/",
            Query = AnalyticsClassifier.SafeQuery(http),
            PageType = pageType,
            EntityId = entityId,
            Title = AnalyticsTracker.Truncate(http.Items["PageTitle"] as string, 200),
            SessionKey = identity.SessionKey,
            VisitorKey = identity.VisitorKey,
            IsNewVisitor = identity.IsNew,
            IsAuthenticated = http.User.Identity?.IsAuthenticated == true,
            ReferrerHost = AnalyticsClassifier.ReferrerHost(http),
            Channel = AnalyticsClassifier.Channel(http),
            SearchTerm = AnalyticsTracker.Truncate(http.Request.Query["q"].ToString(), 150) is { Length: > 0 } q
                ? q
                : null,
            DeviceType = AnalyticsClassifier.Device(userAgent),
            IsBot = isBot,
            UtmSource = AnalyticsTracker.Truncate(http.Request.Query["utm_source"].ToString(), 80),
            UtmMedium = AnalyticsTracker.Truncate(http.Request.Query["utm_medium"].ToString(), 80),
            UtmCampaign = AnalyticsTracker.Truncate(http.Request.Query["utm_campaign"].ToString(), 120),
            ServerMs = ms,
            StatusCode = http.Response.StatusCode,
            CreatedAt = now,
            DateKey = AnalyticsClassifier.DateKey(now),
            HourOfDay = AnalyticsClassifier.HourOfDay(now)
        });
    }
}

/// <summary>
/// سرویس پس‌زمینه‌ای که صف آنالیتیکس را هر ۵ ثانیه در دیتابیس می‌نویسد
/// و داده‌های قدیمی‌تر از یک سال را پاک می‌کند.
/// </summary>
public class AnalyticsFlushService(
    AnalyticsTracker tracker,
    IServiceScopeFactory scopeFactory,
    ILogger<AnalyticsFlushService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var lastPrune = DateTime.UtcNow;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);

                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await tracker.FlushAsync(db, ct);

                if (DateTime.UtcNow - lastPrune > TimeSpan.FromHours(12))
                {
                    lastPrune = DateTime.UtcNow;
                    await PruneAsync(db, ct);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "چرخه نوشتن آنالیتیکس خطا داد.");
            }
        }

        // خروج تمیز — آخرین بازدیدهای صف از دست نروند
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await tracker.FlushAsync(db, CancellationToken.None);
        }
        catch { /* در حال خاموش شدن */ }
    }

    static async Task PruneAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var cutoff = AnalyticsClassifier.DateKey(DateTime.UtcNow.AddDays(-400));
        await db.PageVisits.Where(v => v.DateKey < cutoff).ExecuteDeleteAsync(ct);
        await db.SiteEvents.Where(e => e.DateKey < cutoff).ExecuteDeleteAsync(ct);
    }
}
