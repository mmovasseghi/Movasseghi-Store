namespace MovasseghiShop.Web.Models;

public class SeoProReport
{
    public int TechnicalScore { get; set; }
    public int OnPageScore { get; set; }
    public int ContentQualityScore { get; set; }
    public int SearchIntentScore { get; set; }
    public int SemanticCoverageScore { get; set; }
    public int ImageSeoScore { get; set; }
    public int StructuredDataScore { get; set; }
    public int InternalLinkingScore { get; set; }
    public int EcommerceSeoScore { get; set; }
    public int PerformanceScore { get; set; }
    public int UxScore { get; set; }
    public int AeoScore { get; set; }
    public int GeoScore { get; set; }
    public int OverallScore { get; set; }

    public double PrimaryKeywordCoverage { get; set; }
    public double SemanticCoverage { get; set; }
    public string CompetitiveStrength { get; set; } = "ضعیف";

    public List<string> CriticalIssues { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public List<string> Opportunities { get; set; } = [];
    public List<string> PassedChecks { get; set; } = [];
    public List<SeoActionItem> ActionPlan { get; set; } = [];
}

public class SeoActionItem
{
    public int Priority { get; set; }
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public bool AutoFixable { get; set; }
}
