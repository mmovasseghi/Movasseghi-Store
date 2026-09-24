namespace MovasseghiShop.Web.Models;

public sealed class SeoContentPipelineConfig
{
    public int MaxCompetitorPages { get; set; } = 6;
    public int MinRelevanceScore { get; set; } = 55;
    public double MaxCompetitorSimilarityPercent { get; set; } = 28;
    public int MaxGenerationRetries { get; set; } = 4;
    public bool FetchCompetitorHtml { get; set; } = true;
    public string OurDomain { get; set; } = "movasseghi";
}

public sealed class CompetitorPageResearch
{
    public string Url { get; set; } = "";
    public string Domain { get; set; } = "";
    public string Title { get; set; } = "";
    public int SerpPosition { get; set; }
    public int WordCount { get; set; }
    public List<string> H1 { get; set; } = [];
    public List<string> H2 { get; set; } = [];
    public List<string> H3 { get; set; } = [];
    public string? MetaDescription { get; set; }
    public bool HasFaqSchema { get; set; }
    public int RelevanceScore { get; set; }
    public string? RejectReason { get; set; }
    public bool Accepted { get; set; }
}

public sealed class SeoContentResearchBundle
{
    public string SearchQuery { get; set; } = "";
    public DateTime ResearchedAt { get; set; } = DateTime.UtcNow;
    public bool HasLiveSerp { get; set; }
    public List<CompetitorPageResearch> Competitors { get; set; } = [];
    public List<string> PeopleAlsoAsk { get; set; } = [];
    public List<string> RelatedSearches { get; set; } = [];
}

public sealed class ContentGapInsight
{
    public List<string> CommonCompetitorThemes { get; set; } = [];
    public List<string> UniqueAnglesForUs { get; set; } = [];
    public List<string> SuggestedH2 { get; set; } = [];
}

public sealed class KeywordMappingEntry
{
    public int Priority { get; set; }
    public string Phrase { get; set; } = "";
    public bool UsedInContent { get; set; }
    public string Placement { get; set; } = "";
}

public sealed class SeoContentQualityGateResult
{
    public bool Passed { get; set; }
    public double CompetitorSimilarityPercent { get; set; }
    public int RelevanceScore { get; set; }
    public int HumanLikenessScore { get; set; }
    public List<string> Failures { get; set; } = [];
}

public sealed class SeoContentPipelineResult
{
    public string Html { get; set; } = "";
    public string? FaqJson { get; set; }
    public SeoContentResearchBundle Research { get; set; } = new();
    public ContentGapInsight Gap { get; set; } = new();
    public List<KeywordMappingEntry> KeywordMap { get; set; } = [];
    public SeoContentQualityGateResult Quality { get; set; } = new();
    public int Attempts { get; set; }
    public bool UsedSerpResearch { get; set; }
}
