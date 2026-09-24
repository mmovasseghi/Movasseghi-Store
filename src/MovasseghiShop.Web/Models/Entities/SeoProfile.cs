namespace MovasseghiShop.Web.Models.Entities;

/// <summary>Central SEO/AEO/GEO profile for any publishable entity.</summary>
public class SeoProfile
{
    public int Id { get; set; }
    public string EntityType { get; set; } = "product";
    public int EntityId { get; set; }

    public string? FocusKeyword { get; set; }
    public string? SecondaryKeyword { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public string? AiSummary { get; set; }
    /// <summary>JSON array of { question, shortAnswer, fullAnswer }.</summary>
    public string? FaqJson { get; set; }

    public int SeoScore { get; set; }
    public int AeoScore { get; set; }
    public int GeoScore { get; set; }
    public int TotalScore { get; set; }

    /* ── Auto-Fix Ultimate ── */
    public int ContentScore { get; set; }
    public int SemanticScore { get; set; }
    public int IntentScore { get; set; }
    public int ImageScore { get; set; }
    public int SchemaScore { get; set; }
    public int ProductDataScore { get; set; }
    public int InternalLinkingScore { get; set; }
    public int TechnicalScore { get; set; }
    /// <summary>قدرت رقابتی نسبت به میانگین SERP — ۵۰ = برابری با رقبا.</summary>
    public int CompetitiveStrength { get; set; }
    /// <summary>آمادگی واقعی رتبه‌گیری — به‌جای وعده «رتبه ۱».</summary>
    public int RankingReadiness { get; set; }
    /// <summary>گزارش کامل Ultimate به‌صورت JSON.</summary>
    public string? UltimateJson { get; set; }
    /// <summary>JSON array of issue strings for the admin UI.</summary>
    public string? ScoreIssuesJson { get; set; }

    public bool IsPublishReady { get; set; }
    /// <summary>وقتی ادمین توضیحات کامل را دستی ذخیره کرده، Auto-Fix پس‌زمینه متن را بازنویسی نمی‌کند.</summary>
    public bool LockProductDescription { get; set; }
    /// <summary>وقتی ادمین توضیح کوتاه را دستی ذخیره کرده، Auto-Fix آن و خلاصه GEO را بازنویسی نمی‌کند.</summary>
    public bool LockProductShortDescription { get; set; }
    /// <summary>دسته: توضیحات / خلاصه GEO دستی — Auto-Fix بازنویسی نمی‌کند.</summary>
    public bool LockCategoryLead { get; set; }
    /// <summary>وبلاگ/خبر: چکیده دستی.</summary>
    public bool LockArticleExcerpt { get; set; }
    /// <summary>وبلاگ/خبر: بدنهٔ مقاله دستی.</summary>
    public bool LockArticleContent { get; set; }
    public DateTime? LastAuditedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
