using MovasseghiShop.Web.Models.Enums;

namespace MovasseghiShop.Web.Models;

// ─────────────────────── بلوک‌های مشترک ───────────────────────

public record LabelCount(string Label, int Count);

public record DailyPoint(int DateKey, string Label, int Views, int Sessions);

public record RevenuePoint(int DateKey, string Label, int Orders, decimal Revenue);

public enum InsightKind { Win, Risk, Action }

/// <summary>یک یافته تحلیلی: چه دیده شد، چرا مهم است، چه باید کرد.</summary>
public record Insight(InsightKind Kind, string Title, string Detail, string? Action);

public class PageRow
{
    public string Path { get; set; } = "";
    public string? Title { get; set; }
    public string PageType { get; set; } = "other";
    public int Views { get; set; }
    public int Sessions { get; set; }
    public int AvgServerMs { get; set; }
    public int MaxServerMs { get; set; }
    public double ExitRate { get; set; }

    public string FaPageType => PageType switch
    {
        "home" => "صفحه اصلی",
        "catalog" => "کاتالوگ",
        "category" => "دسته‌بندی",
        "product" => "محصول",
        "blog" => "بلاگ",
        "blog-post" => "مقاله",
        "news" => "اخبار",
        "news-article" => "خبر",
        "page" => "صفحه",
        "cart" => "سبد خرید",
        "checkout" => "پرداخت",
        "order" => "سفارش",
        "account" => "حساب کاربری",
        _ => "سایر"
    };
}

public class ChannelRow
{
    public string Channel { get; set; } = "";
    public int Views { get; set; }
    public int Sessions { get; set; }
    public int PrevSessions { get; set; }

    public string Fa => Channel switch
    {
        "organic" => "جستجوی گوگل",
        "social" => "شبکه اجتماعی",
        "direct" => "ورود مستقیم",
        "referral" => "لینک از سایت دیگر",
        "internal" => "ناوبری داخلی",
        "paid" => "تبلیغات پرداختی",
        "campaign" => "کمپین",
        _ => Channel
    };

    public string Icon => Channel switch
    {
        "organic" => "🔍",
        "social" => "📱",
        "direct" => "🔗",
        "referral" => "↗️",
        "internal" => "🏠",
        "paid" => "💳",
        "campaign" => "📣",
        _ => "•"
    };

    public double Delta => PrevSessions > 0
        ? Math.Round((Sessions - PrevSessions) * 100.0 / PrevSessions, 1)
        : Sessions > 0 ? 100 : 0;
}

public class FunnelStep(string label, int count, string key)
{
    public string Label { get; } = label;
    public string Key { get; } = key;
    public int Count { get; } = count;

    /// <summary>درصد عبور از پله قبلی — نقطه نشتی قیف را نشان می‌دهد.</summary>
    public double StepRate { get; set; }
}

public class ProductOpportunity
{
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public int Views { get; set; }
    public int AddToCarts { get; set; }
    public double CartRate { get; set; }
}

public class ProductSalesRow
{
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

public class StatusRow
{
    public OrderStatus Status { get; set; }
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

// ─────────────────────── گزارش ترافیک ───────────────────────

public class TrafficReport
{
    public int Days { get; set; }
    public int FromKey { get; set; }
    public int ToKey { get; set; }
    public bool HasData { get; set; }

    public int PageViews { get; set; }
    public int Sessions { get; set; }
    public int Visitors { get; set; }
    public int NewVisitors { get; set; }
    public int BotViews { get; set; }

    public int PrevPageViews { get; set; }
    public int PrevSessions { get; set; }
    public int PrevVisitors { get; set; }

    public int SinglePageSessions { get; set; }
    public double BounceRate { get; set; }
    public double PagesPerSession { get; set; }
    public int AvgServerMs { get; set; }

    public double ConversionRate { get; set; }
    public double PrevConversionRate { get; set; }
    public double LeadRate { get; set; }
    public int InquiryCount { get; set; }
    public int PhoneClickCount { get; set; }

    public List<DailyPoint> Daily { get; set; } = [];
    public List<PageRow> TopPages { get; set; } = [];
    public List<PageRow> TopLandingPages { get; set; } = [];
    public List<PageRow> SlowestPages { get; set; } = [];
    public List<ChannelRow> Channels { get; set; } = [];
    public List<LabelCount> TopReferrers { get; set; } = [];
    public List<LabelCount> Devices { get; set; } = [];
    public List<LabelCount> SearchTerms { get; set; } = [];
    public List<LabelCount> Hourly { get; set; } = [];
    public List<LabelCount> BotHits { get; set; } = [];
    public List<LabelCount> NotFoundPages { get; set; } = [];
    public List<FunnelStep> Funnel { get; set; } = [];
    public List<ProductOpportunity> ProductOpportunities { get; set; } = [];

    public double SessionDelta => PrevSessions > 0
        ? Math.Round((Sessions - PrevSessions) * 100.0 / PrevSessions, 1)
        : Sessions > 0 ? 100 : 0;

    public double ViewDelta => PrevPageViews > 0
        ? Math.Round((PageViews - PrevPageViews) * 100.0 / PrevPageViews, 1)
        : PageViews > 0 ? 100 : 0;

    public double VisitorDelta => PrevVisitors > 0
        ? Math.Round((Visitors - PrevVisitors) * 100.0 / PrevVisitors, 1)
        : Visitors > 0 ? 100 : 0;

    public double ReturningShare => Visitors > 0
        ? Math.Round((Visitors - NewVisitors) * 100.0 / Visitors, 1)
        : 0;

    /// <summary>ساعت اوج ترافیک — برای زمان‌بندی تماس فروش و انتشار محتوا.</summary>
    public string PeakHour
    {
        get
        {
            var peak = Hourly.OrderByDescending(h => h.Count).FirstOrDefault();
            return peak is null || peak.Count == 0 ? "—" : $"{peak.Label}:00";
        }
    }
}

// ─────────────────────── گزارش فروش ───────────────────────

public class SalesReport
{
    public int Days { get; set; }
    public bool HasData { get; set; }

    public int OrderCount { get; set; }
    public int PrevOrderCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal PrevRevenue { get; set; }
    public decimal AvgOrderValue { get; set; }
    public decimal PrevAvgOrderValue { get; set; }
    public int CancelledCount { get; set; }
    public int PendingCount { get; set; }

    public int InquiryTotal { get; set; }
    public int InquiryOpen { get; set; }

    public List<RevenuePoint> Daily { get; set; } = [];
    public List<ProductSalesRow> TopProducts { get; set; } = [];
    public List<StatusRow> ByStatus { get; set; } = [];
    public List<LabelCount> TopInquiredProducts { get; set; } = [];

    public double RevenueDelta => PrevRevenue > 0
        ? Math.Round((double)((Revenue - PrevRevenue) * 100 / PrevRevenue), 1)
        : Revenue > 0 ? 100 : 0;

    public double OrderDelta => PrevOrderCount > 0
        ? Math.Round((OrderCount - PrevOrderCount) * 100.0 / PrevOrderCount, 1)
        : OrderCount > 0 ? 100 : 0;

    public double AovDelta => PrevAvgOrderValue > 0
        ? Math.Round((double)((AvgOrderValue - PrevAvgOrderValue) * 100 / PrevAvgOrderValue), 1)
        : AvgOrderValue > 0 ? 100 : 0;
}

// ─────────────────────── نمای کلی ───────────────────────

public class OverviewReport
{
    public int Days { get; set; }
    public TrafficReport Traffic { get; set; } = new();
    public SalesReport Sales { get; set; } = new();
    public List<Insight> Insights { get; set; } = [];

    public int RiskCount => Insights.Count(i => i.Kind == InsightKind.Risk);
    public int ActionCount => Insights.Count(i => i.Kind == InsightKind.Action);
    public int WinCount => Insights.Count(i => i.Kind == InsightKind.Win);
}
