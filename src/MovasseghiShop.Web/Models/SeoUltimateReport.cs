namespace MovasseghiShop.Web.Models;

/// <summary>
/// Auto-Fix Ultimate — گزارش کامل SEO + AEO + GEO با تحلیل رقابتی و Ranking Readiness.
/// این گزارش به‌جای «نمره ۱۰۰» می‌گوید صفحه در برابر SERP رقابتی کجا ایستاده است.
/// </summary>
public class SeoUltimateReport
{
    // ── ابعاد امتیاز (۰–۱۰۰) ──
    public int SeoScore { get; set; }
    public int AeoScore { get; set; }
    public int GeoScore { get; set; }
    public int ContentScore { get; set; }
    public int SemanticScore { get; set; }
    public int IntentScore { get; set; }
    public int ProductDataScore { get; set; }
    public int SchemaScore { get; set; }
    public int ImageScore { get; set; }
    public int InternalLinkingScore { get; set; }
    public int TechnicalScore { get; set; }

    /// <summary>قدرت رقابتی نسبت به میانگین SERP — ۵۰ = برابری با رقبا.</summary>
    public int CompetitiveStrength { get; set; }

    public int OverallScore { get; set; }

    /// <summary>میانگین وزنی ابعاد — قبل از سقف آمادگی/رقابتی.</summary>
    public int WeightedDimensionScore { get; set; }

    /// <summary>
    /// آمادگی واقعی برای رتبه گرفتن — همیشه ≤ ترکیب Overall و CompetitiveStrength.
    /// هرگز به مدیر وعده «رتبه ۱» نمی‌دهد.
    /// </summary>
    public int RankingReadiness { get; set; }

    // ── درصد پوشش‌ها (خروجی اصلی سیستم) ──
    public double TopicalCoverage { get; set; }
    public double IntentMatch { get; set; }
    public double SemanticCoverage { get; set; }
    public double EntityCoverage { get; set; }
    public double AeoAnswerCoverage { get; set; }
    public double ProductDataCompleteness { get; set; }
    public double CompetitorCoverage { get; set; }
    public double PrimaryKeywordCoverage { get; set; }

    /// <summary>ریسک محتوای کلیشه‌ای AI — هدف &lt; ۱۰٪.</summary>
    public double GenericRisk { get; set; }

    /// <summary>ریسک تکرار/کپی داخلی (پاراگراف‌های تقریباً یکسان).</summary>
    public double DuplicationRisk { get; set; }

    // ── سنجه‌های خام محتوا ──
    public int WordCount { get; set; }
    public double KeywordDensity { get; set; }
    public bool HasSceneImage { get; set; }
    public bool HasStudioImage { get; set; }

    // ── وضعیت ──
    public string Grade { get; set; } = "Weak";
    public string GradeFa { get; set; } = "ضعیف";
    public string RankingVerdict { get; set; } = "";
    public bool IsPublishReady { get; set; }
    public bool IsIndexable { get; set; } = true;
    public bool CanonicalValid { get; set; } = true;

    public SeoIntentMix Intent { get; set; } = new();
    public SeoSerpBaseline Baseline { get; set; } = new();

    public List<SeoDimensionGap> Gaps { get; set; } = [];
    public List<SeoTopicCoverage> Topics { get; set; } = [];
    public List<SeoEntityScore> Entities { get; set; } = [];
    public List<SeoQueryTarget> Queries { get; set; } = [];
    public List<SeoCannibalRisk> Cannibalization { get; set; } = [];

    public List<string> CriticalIssues { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public List<string> Opportunities { get; set; } = [];
    public List<string> PassedChecks { get; set; } = [];

    public List<SeoActionItem> ActionPlan { get; set; } = [];

    /// <summary>کارهایی که موتور نمی‌تواند انجام دهد و مدیر باید انجام دهد.</summary>
    public List<SeoManualTask> ManualTasks { get; set; } = [];

    /// <summary>لاگ حلقه بهینه‌سازی Auto-Fix.</summary>
    public List<SeoIterationStep> Iterations { get; set; } = [];

    /// <summary>ضعیف‌ترین بُعد — ورودی حلقه بعدی Auto-Fix.</summary>
    public string WeakestDimension { get; set; } = "";
    public int WeakestDimensionScore { get; set; }
}

/// <summary>ترکیب Search Intent تشخیص‌داده‌شده برای Keyword.</summary>
public class SeoIntentMix
{
    public int Transactional { get; set; }
    public int Commercial { get; set; }
    public int Informational { get; set; }
    public int Navigational { get; set; }

    public string Dominant { get; set; } = "commercial";
    public string DominantFa { get; set; } = "تجاری";

    public override string ToString() =>
        $"Transactional {Transactional}% · Commercial {Commercial}% · Informational {Informational}%";
}

/// <summary>میانگین رقبای Top 10 — پایه مقایسه.</summary>
public class SeoSerpBaseline
{
    /// <summary>آیا از داده واقعی SERP ساخته شده یا از baseline صنعتی.</summary>
    public bool HasLiveData { get; set; }
    public int CompetitorsAnalyzed { get; set; }
    public string Keyword { get; set; } = "";
    public int? OurPosition { get; set; }
    public int AvgCompetitorWordCount { get; set; }
    public double FaqSchemaShare { get; set; }
    public DateTime? DataDate { get; set; }

    public int ContentBenchmark { get; set; } = 82;
    public int SemanticBenchmark { get; set; } = 88;
    public int IntentBenchmark { get; set; } = 91;
    public int EntityBenchmark { get; set; } = 79;
    public int AeoBenchmark { get; set; } = 71;
    public int ProductBenchmark { get; set; } = 86;
    public int ImageBenchmark { get; set; } = 74;
    public int SchemaBenchmark { get; set; } = 80;

    public string SourceLabel => HasLiveData
        ? $"داده واقعی SERP — {CompetitorsAnalyzed} رقیب"
        : "Baseline صنعتی (داده SERP همگام‌سازی نشده)";
}

/// <summary>فاصله یک بُعد با میانگین رقبا.</summary>
public class SeoDimensionGap
{
    public string Dimension { get; set; } = "";
    public string DimensionFa { get; set; } = "";
    public int OurScore { get; set; }
    public int CompetitorAvg { get; set; }
    public int Deficit => Math.Max(0, CompetitorAvg - OurScore);
    public bool IsBehind => OurScore < CompetitorAvg;
    public string Severity => Deficit switch
    {
        >= 25 => "high",
        >= 10 => "medium",
        > 0 => "low",
        _ => "none"
    };
}

/// <summary>پوشش یک موضوع از Topic Model.</summary>
public class SeoTopicCoverage
{
    public string Topic { get; set; } = "";
    public bool Covered { get; set; }
    public int Weight { get; set; } = 1;
    public int Mentions { get; set; }
}

/// <summary>اعتبار و سازگاری یک Entity.</summary>
public class SeoEntityScore
{
    public string Entity { get; set; } = "";
    public string Kind { get; set; } = "";
    public int Confidence { get; set; }
    public int Consistency { get; set; }
    public string Evidence { get; set; } = "Low";
    public bool IsRequired { get; set; }
}

/// <summary>Query گسترش‌یافته و وضعیت پاسخ در صفحه.</summary>
public class SeoQueryTarget
{
    public string Query { get; set; } = "";
    public string Kind { get; set; } = "";
    public bool Answered { get; set; }
    public bool FromSearchConsole { get; set; }
    public double Impressions { get; set; }
    public double Clicks { get; set; }
    public double? Position { get; set; }
}

/// <summary>هشدار Keyword Cannibalization.</summary>
public class SeoCannibalRisk
{
    public string Keyword { get; set; } = "";
    public string EntityType { get; set; } = "product";
    public int EntityId { get; set; }
    public string EntityName { get; set; } = "";
    public string Recommendation { get; set; } = "";
}

/// <summary>کاری که فقط انسان می‌تواند انجام دهد.</summary>
public class SeoManualTask
{
    public string Title { get; set; } = "";
    public string Why { get; set; } = "";
    public int ScoreImpact { get; set; }
    public bool BlocksPublish { get; set; }
}

/// <summary>یک قدم از حلقه Auto-Fix.</summary>
public class SeoIterationStep
{
    public int Iteration { get; set; }
    public string TargetDimension { get; set; } = "";
    public string Action { get; set; } = "";
    public int ScoreBefore { get; set; }
    public int ScoreAfter { get; set; }
    public int Delta => ScoreAfter - ScoreBefore;
    public bool Stopped { get; set; }
    public string? StopReason { get; set; }
}
