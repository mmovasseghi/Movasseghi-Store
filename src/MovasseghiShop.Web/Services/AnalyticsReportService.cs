using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Enums;

namespace MovasseghiShop.Web.Services;

public interface IAnalyticsReportService
{
    Task<TrafficReport> GetTrafficAsync(int days, CancellationToken ct = default);
    Task<SalesReport> GetSalesAsync(int days, CancellationToken ct = default);
    Task<OverviewReport> GetOverviewAsync(int days, CancellationToken ct = default);
}

/// <summary>
/// گزارش‌های تحلیلی. اصل کار این سرویس مقایسه است نه شمارش:
/// هر عدد با دوره قبل مقایسه می‌شود و از دلِ داده «بینش» و «اقدام پیشنهادی»
/// استخراج می‌شود — چیزی که گزارش قبلی نداشت.
/// </summary>
public class AnalyticsReportService(ApplicationDbContext db) : IAnalyticsReportService
{
    public async Task<OverviewReport> GetOverviewAsync(int days, CancellationToken ct = default)
    {
        var traffic = await GetTrafficAsync(days, ct);
        var sales = await GetSalesAsync(days, ct);

        var report = new OverviewReport
        {
            Days = days,
            Traffic = traffic,
            Sales = sales
        };

        report.Insights = BuildInsights(traffic, sales);
        return report;
    }

    // ─────────────────────────── ترافیک ───────────────────────────

    public async Task<TrafficReport> GetTrafficAsync(int days, CancellationToken ct = default)
    {
        var (from, to) = Window(days);
        var (prevFrom, prevTo) = PreviousWindow(days);

        var report = new TrafficReport { Days = days, FromKey = from, ToKey = to };

        // ربات‌ها از آمار انسانی جدا می‌شوند — وگرنه اعداد بی‌معنا می‌شود
        var visits = db.PageVisits.AsNoTracking().Where(v => v.DateKey >= from && v.DateKey <= to);
        var human = visits.Where(v => !v.IsBot);
        var prevHuman = db.PageVisits.AsNoTracking()
            .Where(v => v.DateKey >= prevFrom && v.DateKey <= prevTo && !v.IsBot);

        report.PageViews = await human.CountAsync(ct);
        report.BotViews = await visits.CountAsync(v => v.IsBot, ct);

        if (report.PageViews == 0 && report.BotViews == 0)
        {
            report.HasData = false;
            return report;
        }

        report.HasData = true;
        report.Sessions = await human.Select(v => v.SessionKey).Distinct().CountAsync(ct);
        report.Visitors = await human.Select(v => v.VisitorKey).Distinct().CountAsync(ct);
        report.NewVisitors = await human
            .Where(v => v.IsNewVisitor)
            .Select(v => v.VisitorKey)
            .Distinct()
            .CountAsync(ct);

        report.PrevPageViews = await prevHuman.CountAsync(ct);
        report.PrevSessions = await prevHuman.Select(v => v.SessionKey).Distinct().CountAsync(ct);
        report.PrevVisitors = await prevHuman.Select(v => v.VisitorKey).Distinct().CountAsync(ct);

        // ── سری روزانه ──
        var daily = await human
            .GroupBy(v => v.DateKey)
            .Select(g => new
            {
                Key = g.Key,
                Views = g.Count(),
                Sessions = g.Select(x => x.SessionKey).Distinct().Count()
            })
            .ToListAsync(ct);

        var byDay = daily.ToDictionary(d => d.Key, d => d);
        report.Daily = EnumerateDays(days)
            .Select(k => new DailyPoint(
                k,
                FormatDayLabel(k),
                byDay.TryGetValue(k, out var d) ? d.Views : 0,
                byDay.TryGetValue(k, out var s) ? s.Sessions : 0))
            .ToList();

        // ── نشست‌های تک‌صفحه‌ای (پرش) ──
        var perSession = await human
            .GroupBy(v => v.SessionKey)
            .Select(g => g.Count())
            .ToListAsync(ct);

        if (perSession.Count > 0)
        {
            report.SinglePageSessions = perSession.Count(c => c == 1);
            report.BounceRate = Math.Round(report.SinglePageSessions * 100.0 / perSession.Count, 1);
            report.PagesPerSession = Math.Round(perSession.Average(), 2);
        }

        // ── صفحات پربازدید ──
        report.TopPages = await human
            .GroupBy(v => new { v.Path, v.PageType })
            .Select(g => new PageRow
            {
                Path = g.Key.Path,
                PageType = g.Key.PageType,
                Views = g.Count(),
                Sessions = g.Select(x => x.SessionKey).Distinct().Count(),
                AvgServerMs = (int)g.Average(x => x.ServerMs),
                Title = g.Max(x => x.Title)
            })
            .OrderByDescending(p => p.Views)
            .Take(20)
            .ToListAsync(ct);

        // ── نرخ خروج: صفحاتی که کاربر بعدشان چیزی ندید ──
        var landings = await human
            .GroupBy(v => v.SessionKey)
            .Select(g => g.OrderBy(x => x.CreatedAt).Select(x => x.Path).First())
            .ToListAsync(ct);

        report.TopLandingPages = landings
            .GroupBy(p => p)
            .Select(g => new PageRow { Path = g.Key, Views = g.Count() })
            .OrderByDescending(p => p.Views)
            .Take(10)
            .ToList();

        // نشست‌های تک‌صفحه‌ای به تفکیک صفحه ورود = نرخ خروج واقعی هر صفحه
        var singlePageLandings = await human
            .GroupBy(v => v.SessionKey)
            .Where(g => g.Count() == 1)
            .Select(g => g.Select(x => x.Path).First())
            .ToListAsync(ct);

        var exitByPath = singlePageLandings.GroupBy(p => p).ToDictionary(g => g.Key, g => g.Count());
        var landingByPath = landings.GroupBy(p => p).ToDictionary(g => g.Key, g => g.Count());

        foreach (var row in report.TopLandingPages)
        {
            var total = landingByPath.GetValueOrDefault(row.Path, 0);
            var exits = exitByPath.GetValueOrDefault(row.Path, 0);
            row.ExitRate = total > 0 ? Math.Round(exits * 100.0 / total, 1) : 0;
        }

        // ── کانال ورود ──
        report.Channels = await human
            .GroupBy(v => v.Channel)
            .Select(g => new ChannelRow
            {
                Channel = g.Key,
                Views = g.Count(),
                Sessions = g.Select(x => x.SessionKey).Distinct().Count()
            })
            .OrderByDescending(c => c.Sessions)
            .ToListAsync(ct);

        var prevChannels = await prevHuman
            .GroupBy(v => v.Channel)
            .Select(g => new { g.Key, Sessions = g.Select(x => x.SessionKey).Distinct().Count() })
            .ToListAsync(ct);

        var prevByChannel = prevChannels.ToDictionary(c => c.Key, c => c.Sessions);
        foreach (var row in report.Channels)
            row.PrevSessions = prevByChannel.GetValueOrDefault(row.Channel, 0);

        // ── ارجاع‌دهنده ──
        // EF Core نمی‌تواند record با پارامتر موقعیتی را داخل GroupBy ترجمه کند،
        // پس اول به نوع بی‌نام پروجکت می‌شود و بعد در حافظه نگاشت می‌گیرد.
        report.TopReferrers = (await human
                .Where(v => v.ReferrerHost != null && v.Channel != "internal")
                .GroupBy(v => v.ReferrerHost!)
                .Select(g => new { g.Key, Count = g.Count() })
                .OrderByDescending(r => r.Count)
                .Take(12)
                .ToListAsync(ct))
            .Select(r => new LabelCount(r.Key, r.Count))
            .ToList();

        // ── دستگاه ──
        report.Devices = (await human
                .GroupBy(v => v.DeviceType)
                .Select(g => new { g.Key, Count = g.Select(x => x.SessionKey).Distinct().Count() })
                .OrderByDescending(d => d.Count)
                .ToListAsync(ct))
            .Select(d => new LabelCount(d.Key, d.Count))
            .ToList();

        // ── جستجوی داخلی: کاربر چه می‌خواست و پیدا نکرد ──
        report.SearchTerms = (await human
                .Where(v => v.SearchTerm != null && v.SearchTerm != "")
                .GroupBy(v => v.SearchTerm!)
                .Select(g => new { g.Key, Count = g.Count() })
                .OrderByDescending(s => s.Count)
                .Take(20)
                .ToListAsync(ct))
            .Select(s => new LabelCount(s.Key, s.Count))
            .ToList();

        // ── ساعت‌های اوج (برای زمان‌بندی تماس فروش) ──
        var hourly = await human
            .GroupBy(v => v.HourOfDay)
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var byHour = hourly.ToDictionary(h => h.Hour, h => h.Count);
        report.Hourly = Enumerable.Range(0, 24)
            .Select(h => new LabelCount($"{h:00}", byHour.GetValueOrDefault(h, 0)))
            .ToList();

        // ── کندترین صفحات (اثر مستقیم روی Core Web Vitals و رتبه) ──
        report.SlowestPages = await human
            .GroupBy(v => v.Path)
            .Where(g => g.Count() >= 3)
            .Select(g => new PageRow
            {
                Path = g.Key,
                Views = g.Count(),
                AvgServerMs = (int)g.Average(x => x.ServerMs),
                MaxServerMs = g.Max(x => x.ServerMs)
            })
            .OrderByDescending(p => p.AvgServerMs)
            .Take(10)
            .ToListAsync(ct);

        report.AvgServerMs = await human.AnyAsync(ct) ? (int)await human.AverageAsync(v => v.ServerMs, ct) : 0;

        // ── خزش ربات‌ها: گوگل و ربات‌های AI چقدر سایت را می‌خوانند ──
        report.BotHits = (await visits
                .Where(v => v.IsBot)
                .GroupBy(v => v.Path)
                .Select(g => new { g.Key, Count = g.Count() })
                .OrderByDescending(b => b.Count)
                .Take(10)
                .ToListAsync(ct))
            .Select(b => new LabelCount(b.Key, b.Count))
            .ToList();

        // ── صفحات ۴۰۴ (فقط مسیرهای واقعی سایت — نه اسکنرها و ربات‌ها) ──
        // IsNoisePath در حافظه اعمال می‌شود؛ EF Core نمی‌تواند متد سفارشی را به SQL ترجمه کند.
        report.NotFoundPages = (await visits
                .Where(v => v.StatusCode == 404 && !v.IsBot)
                .GroupBy(v => v.Path)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync(ct))
            .Where(n => !IsNoisePath(n.Key))
            .OrderByDescending(n => n.Count)
            .Take(10)
            .Select(n => new LabelCount(n.Key, n.Count))
            .ToList();

        await AttachFunnelAsync(report, from, to, prevFrom, prevTo, ct);

        return report;
    }

    async Task AttachFunnelAsync(
        TrafficReport report, int from, int to, int prevFrom, int prevTo, CancellationToken ct)
    {
        var events = db.SiteEvents.AsNoTracking().Where(e => e.DateKey >= from && e.DateKey <= to);

        async Task<int> Step(string name) =>
            await events.Where(e => e.Name == name).Select(e => e.SessionKey).Distinct().CountAsync(ct);

        var viewedProduct = await Step("view_product");
        var addedToCart = await Step("add_to_cart");
        var beganCheckout = await Step("begin_checkout");
        var purchased = await Step("purchase");
        var inquired = await Step("submit_inquiry");
        var calledPhone = await Step("click_phone");

        report.Funnel =
        [
            new FunnelStep("بازدید سایت", report.Sessions, "sessions"),
            new FunnelStep("مشاهده محصول", viewedProduct, "view_product"),
            new FunnelStep("افزودن به سبد", addedToCart, "add_to_cart"),
            new FunnelStep("شروع پرداخت", beganCheckout, "begin_checkout"),
            new FunnelStep("ثبت سفارش", purchased, "purchase")
        ];

        // درصد هر پله نسبت به پله قبلی — نشتی قیف را دقیقاً نشان می‌دهد
        for (var i = 1; i < report.Funnel.Count; i++)
        {
            var prev = report.Funnel[i - 1].Count;
            report.Funnel[i].StepRate = prev > 0 ? Math.Round(report.Funnel[i].Count * 100.0 / prev, 1) : 0;
        }

        report.InquiryCount = inquired;
        report.PhoneClickCount = calledPhone;

        // نرخ کلید تماس/استعلام — سایت B2B عمده، بخشی از تبدیل تلفنی است
        report.LeadRate = report.Sessions > 0
            ? Math.Round((inquired + calledPhone) * 100.0 / report.Sessions, 2)
            : 0;

        report.ConversionRate = report.Sessions > 0
            ? Math.Round(purchased * 100.0 / report.Sessions, 2)
            : 0;

        var prevEvents = db.SiteEvents.AsNoTracking()
            .Where(e => e.DateKey >= prevFrom && e.DateKey <= prevTo);
        var prevPurchased = await prevEvents
            .Where(e => e.Name == "purchase")
            .Select(e => e.SessionKey).Distinct().CountAsync(ct);
        report.PrevConversionRate = report.PrevSessions > 0
            ? Math.Round(prevPurchased * 100.0 / report.PrevSessions, 2)
            : 0;

        // ── محصولات پربازدید بدون فروش: بزرگ‌ترین فرصت از دست رفته ──
        var productViews = await events
            .Where(e => e.Name == "view_product" && e.EntityId != null)
            .GroupBy(e => new { e.EntityId, e.Label })
            .Select(g => new
            {
                g.Key.EntityId,
                g.Key.Label,
                Views = g.Select(x => x.SessionKey).Distinct().Count()
            })
            .OrderByDescending(p => p.Views)
            .Take(40)
            .ToListAsync(ct);

        var productCarts = await events
            .Where(e => e.Name == "add_to_cart" && e.EntityId != null)
            .GroupBy(e => e.EntityId!.Value)
            .Select(g => new { Id = g.Key, Carts = g.Select(x => x.SessionKey).Distinct().Count() })
            .ToListAsync(ct);

        var cartsById = productCarts.ToDictionary(p => p.Id, p => p.Carts);

        report.ProductOpportunities = productViews
            .Select(p => new ProductOpportunity
            {
                ProductId = p.EntityId!.Value,
                Name = p.Label ?? $"#{p.EntityId}",
                Views = p.Views,
                AddToCarts = cartsById.GetValueOrDefault(p.EntityId!.Value, 0)
            })
            .Where(p => p.Views >= 5)
            .OrderByDescending(p => p.Views)
            .ToList();

        foreach (var row in report.ProductOpportunities)
            row.CartRate = row.Views > 0 ? Math.Round(row.AddToCarts * 100.0 / row.Views, 1) : 0;
    }

    // ─────────────────────────── فروش ───────────────────────────

    public async Task<SalesReport> GetSalesAsync(int days, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var from = now.AddDays(-days);
        var prevFrom = now.AddDays(-days * 2);

        var report = new SalesReport { Days = days };

        var orders = db.Orders.AsNoTracking().Where(o => o.CreatedAt >= from);
        var prevOrders = db.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= prevFrom && o.CreatedAt < from);

        // سفارش لغوشده جزو درآمد و شمارش فروش نیست
        var paid = orders.Where(o => o.Status != OrderStatus.Cancelled);
        var prevPaid = prevOrders.Where(o => o.Status != OrderStatus.Cancelled);

        report.OrderCount = await paid.CountAsync(ct);
        report.PrevOrderCount = await prevPaid.CountAsync(ct);

        report.Revenue = await paid.AnyAsync(ct) ? await paid.SumAsync(o => o.TotalAmount, ct) : 0;
        report.PrevRevenue = await prevPaid.AnyAsync(ct) ? await prevPaid.SumAsync(o => o.TotalAmount, ct) : 0;

        var paidCount = await paid.CountAsync(ct);
        report.AvgOrderValue = paidCount > 0 ? report.Revenue / paidCount : 0;

        var prevPaidCount = await prevPaid.CountAsync(ct);
        report.PrevAvgOrderValue = prevPaidCount > 0 ? report.PrevRevenue / prevPaidCount : 0;

        report.CancelledCount = await orders.CountAsync(o => o.Status == OrderStatus.Cancelled, ct);
        // «در انتظار» = هنوز پرداخت نشده یا پرداخت شده ولی تأیید نشده؛
        // این‌ها سفارش‌هایی هستند که منتظر اقدام مدیر فروشگاه‌اند.
        report.PendingCount = await orders.CountAsync(
            o => o.Status == OrderStatus.PendingPayment || o.Status == OrderStatus.PaymentReceived, ct);

        report.ByStatus = await orders
            .GroupBy(o => o.Status)
            .Select(g => new StatusRow
            {
                Status = g.Key,
                Count = g.Count(),
                Amount = g.Sum(o => o.TotalAmount)
            })
            .ToListAsync(ct);

        // ── درآمد روزانه ──
        var dailyOrders = await paid
            .Select(o => new { o.CreatedAt, o.TotalAmount })
            .ToListAsync(ct);

        var byDay = dailyOrders
            .GroupBy(o => AnalyticsClassifier.DateKey(o.CreatedAt))
            .ToDictionary(g => g.Key, g => new { Count = g.Count(), Sum = g.Sum(x => x.TotalAmount) });

        report.Daily = EnumerateDays(days)
            .Select(k => new RevenuePoint(
                k,
                FormatDayLabel(k),
                byDay.TryGetValue(k, out var d) ? d.Count : 0,
                byDay.TryGetValue(k, out var s) ? s.Sum : 0))
            .ToList();

        // ── پرفروش‌ترین محصولات ──
        // OrderItem به Variant وصل است نه Product، پس گروه‌بندی روی نام محصول
        // انجام می‌شود (نام در لحظه سفارش ذخیره شده) و شناسه محصول از Variant می‌آید.
        var variantToProduct = await db.ProductVariants
            .AsNoTracking()
            .Select(v => new { v.Id, v.ProductId })
            .ToDictionaryAsync(v => v.Id, v => v.ProductId, ct);

        var soldItems = await db.OrderItems
            .AsNoTracking()
            .Where(i => i.Order.CreatedAt >= from && i.Order.Status != OrderStatus.Cancelled)
            .Select(i => new { i.ProductVariantId, i.ProductName, i.CartonQuantity, i.LineTotal, i.OrderId })
            .ToListAsync(ct);

        report.TopProducts = soldItems
            .GroupBy(i => i.ProductName)
            .Select(g => new ProductSalesRow
            {
                ProductId = variantToProduct.GetValueOrDefault(g.First().ProductVariantId),
                Name = g.Key,
                Quantity = g.Sum(i => i.CartonQuantity),
                Revenue = g.Sum(i => i.LineTotal),
                OrderCount = g.Select(i => i.OrderId).Distinct().Count()
            })
            .OrderByDescending(p => p.Revenue)
            .Take(15)
            .ToList();

        // ── استعلام قیمت (کانال فروش عمده) ──
        report.InquiryTotal = await db.PurchaseInquiries.CountAsync(i => i.CreatedAt >= from, ct);
        report.InquiryOpen = await db.PurchaseInquiries
            .CountAsync(i => i.CreatedAt >= from && !i.IsHandled, ct);

        report.TopInquiredProducts = (await db.PurchaseInquiries
                .AsNoTracking()
                .Where(i => i.CreatedAt >= from)
                .GroupBy(i => i.Product.Name)
                .Select(g => new { g.Key, Count = g.Count() })
                .OrderByDescending(p => p.Count)
                .Take(10)
                .ToListAsync(ct))
            .Select(p => new LabelCount(p.Key, p.Count))
            .ToList();

        report.HasData = report.OrderCount > 0 || report.InquiryTotal > 0;
        return report;
    }

    // ─────────────────────── تولید بینش ───────────────────────

    /// <summary>
    /// لایه «هوش» گزارش: الگوهای معنادار را از داده بیرون می‌کشد و
    /// برای هرکدام اقدام مشخص پیشنهاد می‌دهد. عدد خالی بدون تفسیر
    /// همان چیزی بود که گزارش قبلی را بی‌فایده کرده بود.
    /// </summary>
    static List<Insight> BuildInsights(TrafficReport t, SalesReport s)
    {
        var insights = new List<Insight>();

        if (!t.HasData)
        {
            insights.Add(new Insight(
                InsightKind.Action,
                "داده ترافیک هنوز جمع نشده",
                "ردیابی بازدید تازه فعال شده است. برای اینکه گزارش‌ها معنا پیدا کنند "
                + "دست‌کم چند روز داده لازم است.",
                "چند روز صبر کنید، سپس بازه ۷ روزه را ببینید."));
            return insights;
        }

        // ── رشد یا افت ترافیک ──
        if (t.PrevSessions > 0)
        {
            var delta = (t.Sessions - t.PrevSessions) * 100.0 / t.PrevSessions;
            if (delta >= 15)
                insights.Add(new Insight(
                    InsightKind.Win,
                    $"ترافیک {Math.Abs(delta):0}٪ رشد کرد",
                    $"نشست‌ها از {t.PrevSessions:N0} به {t.Sessions:N0} رسید.",
                    "کانالی که بیشترین رشد را داشته پیدا کنید و همان را تقویت کنید."));
            else if (delta <= -15)
                insights.Add(new Insight(
                    InsightKind.Risk,
                    $"ترافیک {Math.Abs(delta):0}٪ افت کرد",
                    $"نشست‌ها از {t.PrevSessions:N0} به {t.Sessions:N0} کاهش یافت.",
                    "کانال‌های ورود را با دوره قبل مقایسه کنید و در Rank Radar افت رتبه را بررسی کنید."));
        }

        // ── سهم جستجوی ارگانیک: شاخص اصلی موفقیت سئو ──
        var organic = t.Channels.FirstOrDefault(c => c.Channel == "organic");
        if (organic is not null && t.Sessions > 0)
        {
            var share = organic.Sessions * 100.0 / t.Sessions;
            if (share < 25)
                insights.Add(new Insight(
                    InsightKind.Risk,
                    $"فقط {share:0}٪ ترافیک از جستجوی ارگانیک است",
                    "سایتی که برای عمده‌فروشی ساخته شده باید بیشتر ترافیکش از گوگل بیاید؛ "
                    + "وابستگی به لینک مستقیم و شبکه اجتماعی شکننده است.",
                    "در مرکز SEO کلمات هدف را بررسی و Auto-Fix را روی صفحات ضعیف اجرا کنید."));
            else if (share >= 55)
                insights.Add(new Insight(
                    InsightKind.Win,
                    $"{share:0}٪ ترافیک ارگانیک است",
                    "سرمایه‌گذاری سئو دارد جواب می‌دهد و ترافیک پایدار می‌سازد.",
                    "کلمات صفحه دوم گوگل را هدف بگیرید — سریع‌ترین رشد آن‌جاست."));

            if (organic.PrevSessions > 0)
            {
                var od = (organic.Sessions - organic.PrevSessions) * 100.0 / organic.PrevSessions;
                if (od <= -20)
                    insights.Add(new Insight(
                        InsightKind.Risk,
                        $"ترافیک ارگانیک {Math.Abs(od):0}٪ افت کرد",
                        "افت ناگهانی ارگانیک معمولاً یعنی افت رتبه یا مشکل ایندکس.",
                        "Rank Radar را همگام‌سازی و Search Console را برای خطای پوشش چک کنید."));
            }
        }
        else if (t.Sessions > 20)
        {
            insights.Add(new Insight(
                InsightKind.Risk,
                "هیچ ترافیک ارگانیکی ثبت نشده",
                "در این بازه هیچ بازدیدی از گوگل یا موتور جستجو نیامده است.",
                "ثبت سایت در Search Console و ارسال Sitemap را بررسی کنید."));
        }

        // ── نرخ پرش (با حجم کم داده هشدار اغراق‌آمیز است) ──
        if (t.BounceRate >= 80 && t.Sessions >= 80 && t.PagesPerSession < 1.3)
            insights.Add(new Insight(
                InsightKind.Risk,
                $"نرخ پرش {t.BounceRate:0}٪ است",
                $"{t.SinglePageSessions:N0} نشست فقط یک صفحه دیدند و رفتند — "
                + "یعنی صفحه ورود انتظار کاربر را برآورده نمی‌کند.",
                "روی صفحات ورود پربازدید، لینک داخلی و محصول مرتبط اضافه کنید."));

        // ── ترافیک موبایل ──
        var mobile = t.Devices.FirstOrDefault(d => d.Label == "mobile");
        if (mobile is not null && t.Sessions > 0)
        {
            var share = mobile.Count * 100.0 / t.Sessions;
            if (share >= 60)
                insights.Add(new Insight(
                    InsightKind.Action,
                    $"{share:0}٪ بازدیدکننده با موبایل می‌آید",
                    "رتبه گوگل بر اساس نسخه موبایل تعیین می‌شود (Mobile-First Indexing).",
                    "سرعت و چیدمان موبایل را در اولویت هر تغییری قرار دهید."));
        }

        // ── سرعت سرور: ورودی مستقیم Core Web Vitals ──
        if (t.AvgServerMs > 600)
            insights.Add(new Insight(
                InsightKind.Risk,
                $"میانگین زمان پاسخ سرور {t.AvgServerMs:N0} میلی‌ثانیه است",
                "پاسخ کند سرور مستقیماً LCP را بالا می‌برد و LCP یکی از سه شاخص رتبه‌بندی گوگل است.",
                "فهرست کندترین صفحات را ببینید و همان‌ها را بهینه یا کش کنید."));
        else if (t.AvgServerMs > 0 && t.AvgServerMs <= 200)
            insights.Add(new Insight(
                InsightKind.Win,
                $"پاسخ سرور {t.AvgServerMs:N0} میلی‌ثانیه — عالی",
                "زیر ۲۰۰ میلی‌ثانیه پایه محکمی برای گرفتن نمره سبز Core Web Vitals است.",
                null));

        var slow = t.SlowestPages.FirstOrDefault(p => p.AvgServerMs > 900);
        if (slow is not null)
            insights.Add(new Insight(
                InsightKind.Action,
                "یک صفحه به‌طور مشخص کند است",
                $"مسیر «{slow.Path}» به‌طور میانگین {slow.AvgServerMs:N0} میلی‌ثانیه طول می‌کشد "
                + $"({slow.Views:N0} بازدید).",
                "پرس‌وجوهای این صفحه را بررسی یا خروجی‌اش را کش کنید."));

        // ── ۴۰۴ ──
        if (t.NotFoundPages.Count > 0)
        {
            var top = t.NotFoundPages[0];
            insights.Add(new Insight(
                InsightKind.Risk,
                $"{t.NotFoundPages.Sum(n => n.Count):N0} بازدید به صفحه ۴۰۴ خورد",
                $"بیشترین مورد: «{top.Label}» با {top.Count:N0} بار. "
                + "لینک شکسته هم کاربر را فراری می‌دهد هم اعتبار سئو را هدر می‌کند.",
                "برای این مسیرها ریدایرکت ۳۰۱ بگذارید یا لینک‌دهنده را اصلاح کنید."));
        }

        // ── خزش ربات‌های AI: پایه AEO و GEO ──
        if (t.BotViews > 0)
        {
            insights.Add(new Insight(
                InsightKind.Win,
                $"{t.BotViews:N0} بازدید ربات ثبت شد",
                "ربات‌های جستجو و هوش مصنوعی سایت را می‌خوانند؛ این پیش‌شرط دیده‌شدن "
                + "در نتایج گوگل و پاسخ‌های AI است.",
                "مطمئن شوید صفحات مهم بیشترین خزش را می‌گیرند."));
        }
        else if (t.PageViews > 50)
        {
            insights.Add(new Insight(
                InsightKind.Risk,
                "هیچ خزش رباتی ثبت نشده",
                "اگر ربات گوگل سایت را نخزد، هیچ صفحه‌ای ایندکس نمی‌شود.",
                "robots.txt و Sitemap را بررسی و سایت را در Search Console ثبت کنید."));
        }

        // ── جستجوی داخلی بدون نتیجه = تقاضای بی‌پاسخ ──
        if (t.SearchTerms.Count > 0 && t.SearchTerms[0].Count >= 3)
        {
            var top = t.SearchTerms[0];
            insights.Add(new Insight(
                InsightKind.Action,
                $"پرجستجوترین عبارت داخلی: «{top.Label}»",
                $"{top.Count:N0} بار در سایت جستجو شده. جستجوی داخلی دقیق‌ترین سیگنال "
                + "تقاضای واقعی مشتری است.",
                "اگر محصول یا صفحه متناظرش را ندارید، بسازید — تقاضا از قبل موجود است."));
        }

        // ── محصول پربازدید با نرخ سبد پایین ──
        var leak = t.ProductOpportunities
            .Where(p => p.Views >= 10 && p.CartRate < 5)
            .OrderByDescending(p => p.Views)
            .FirstOrDefault();
        if (leak is not null)
            insights.Add(new Insight(
                InsightKind.Action,
                "یک محصول بازدید می‌گیرد ولی به سبد نمی‌رود",
                $"«{leak.Name}» {leak.Views:N0} بازدید داشت ولی فقط {leak.CartRate:0.#}٪ به سبد رفت. "
                + "معمولاً یعنی قیمت، تصویر یا مشخصات کافی نیست.",
                "تصویر، جدول مشخصات و شرط حداقل سفارش این محصول را بازبینی کنید."));

        // ── نشتی قیف ──
        var worstStep = t.Funnel
            .Skip(1)
            .Where(f => f.Count > 0 || f.StepRate > 0)
            .OrderBy(f => f.StepRate)
            .FirstOrDefault();
        if (worstStep is not null && worstStep.StepRate < 30 && t.Sessions >= 20)
            insights.Add(new Insight(
                InsightKind.Risk,
                $"بیشترین ریزش در پله «{worstStep.Label}»",
                $"فقط {worstStep.StepRate:0.#}٪ از پله قبل به این مرحله رسیدند.",
                worstStep.Key switch
                {
                    "add_to_cart" => "دکمه افزودن به سبد و نمایش قیمت را واضح‌تر کنید.",
                    "begin_checkout" => "هزینه ارسال و حداقل سفارش را قبل از پرداخت شفاف کنید.",
                    "purchase" => "تعداد فیلدهای فرم پرداخت را کم و روش‌های پرداخت را بیشتر کنید.",
                    _ => "این مرحله را روی موبایل قدم‌به‌قدم تست کنید."
                }));

        // ── استعلام بی‌پاسخ = پول روی میز ──
        if (s.InquiryOpen > 0)
            insights.Add(new Insight(
                InsightKind.Risk,
                $"{s.InquiryOpen:N0} استعلام قیمت بی‌پاسخ مانده",
                "در فروش عمده، استعلام گرم‌ترین سرنخ است و با گذر زمان سرد می‌شود.",
                "به صندوق استعلام قیمت بروید و همین امروز تماس بگیرید."));

        // ── درآمد ──
        if (s.PrevRevenue > 0)
        {
            var delta = (double)((s.Revenue - s.PrevRevenue) * 100 / s.PrevRevenue);
            if (delta >= 15)
                insights.Add(new Insight(
                    InsightKind.Win,
                    $"درآمد {delta:0}٪ رشد کرد",
                    $"از {s.PrevRevenue:N0} به {s.Revenue:N0} تومان رسید.",
                    null));
            else if (delta <= -15)
                insights.Add(new Insight(
                    InsightKind.Risk,
                    $"درآمد {Math.Abs(delta):0}٪ افت کرد",
                    $"از {s.PrevRevenue:N0} به {s.Revenue:N0} تومان کاهش یافت.",
                    "ترافیک و نرخ تبدیل را جدا بررسی کنید تا بفهمید کدام‌یک افت کرده."));
        }

        if (s.CancelledCount > 0 && s.OrderCount > 0)
        {
            var rate = s.CancelledCount * 100.0 / s.OrderCount;
            if (rate >= 20)
                insights.Add(new Insight(
                    InsightKind.Risk,
                    $"{rate:0}٪ سفارش‌ها لغو شدند",
                    $"{s.CancelledCount:N0} از {s.OrderCount:N0} سفارش لغو شد.",
                    "علت لغو را از مشتری بپرسید — معمولاً هزینه ارسال یا موجودی است."));
        }

        if (s.PendingCount >= 3)
            insights.Add(new Insight(
                InsightKind.Action,
                $"{s.PendingCount:N0} سفارش در انتظار بررسی است",
                "سفارش معلق یعنی مشتری منتظر است.",
                "به بخش سفارش‌ها بروید و وضعیتشان را به‌روز کنید."));

        // ── نرخ تبدیل ──
        if (t.Sessions >= 50)
        {
            if (t.ConversionRate < 0.5 && t.LeadRate < 1)
                insights.Add(new Insight(
                    InsightKind.Risk,
                    $"نرخ تبدیل {t.ConversionRate:0.##}٪ است",
                    "ترافیک می‌آید ولی به سفارش یا استعلام تبدیل نمی‌شود.",
                    "شفافیت قیمت، حداقل سفارش و راه تماس را در صفحه محصول بهتر کنید."));
            else if (t.ConversionRate >= 2)
                insights.Add(new Insight(
                    InsightKind.Win,
                    $"نرخ تبدیل {t.ConversionRate:0.##}٪ — قوی",
                    "برای فروشگاه عمده این عدد بالای میانگین است.",
                    "الان بهترین زمان افزایش ترافیک است، چون تبدیلش تضمین‌شده‌تر است."));
        }

        // مهم‌ترین اول: ریسک، بعد اقدام، بعد موفقیت
        return insights
            .OrderBy(i => i.Kind switch
            {
                InsightKind.Risk => 0,
                InsightKind.Action => 1,
                _ => 2
            })
            .ToList();
    }

    // ─────────────────────────── کمکی ───────────────────────────

    static bool IsNoisePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return true;
        var p = path.Trim().ToLowerInvariant();
        return p.StartsWith("/json/")
            || p.StartsWith("/.well-known/")
            || p.Contains("/wp-")
            || p.Contains("/phpmyadmin")
            || p is "/favicon.ico" or "/robots.txt" or "/sitemap.xml";
    }

    static (int From, int To) Window(int days)
    {
        var now = DateTime.UtcNow;
        return (AnalyticsClassifier.DateKey(now.AddDays(-(days - 1))), AnalyticsClassifier.DateKey(now));
    }

    static (int From, int To) PreviousWindow(int days)
    {
        var now = DateTime.UtcNow;
        return (
            AnalyticsClassifier.DateKey(now.AddDays(-(days * 2 - 1))),
            AnalyticsClassifier.DateKey(now.AddDays(-days)));
    }

    static IEnumerable<int> EnumerateDays(int days)
    {
        var now = AnalyticsClassifier.ToLocal(DateTime.UtcNow).Date;
        for (var i = days - 1; i >= 0; i--)
        {
            var d = now.AddDays(-i);
            yield return d.Year * 10000 + d.Month * 100 + d.Day;
        }
    }

    static string FormatDayLabel(int dateKey)
    {
        var year = dateKey / 10000;
        var month = dateKey / 100 % 100;
        var day = dateKey % 100;
        try
        {
            var pc = new System.Globalization.PersianCalendar();
            var g = new DateTime(year, month, day);
            return $"{pc.GetMonth(g):00}/{pc.GetDayOfMonth(g):00}";
        }
        catch
        {
            return $"{month:00}/{day:00}";
        }
    }
}
