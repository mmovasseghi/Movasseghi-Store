using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Models;

public class SeoScoreResult
{
    public int SeoScore { get; set; }
    public int AeoScore { get; set; }
    public int GeoScore { get; set; }
    public int ContentScore { get; set; }
    public int MediaScore { get; set; }
    public int TotalScore { get; set; }
    public bool IsPublishReady { get; set; }
    public int WordCount { get; set; }
    public double KeywordDensity { get; set; }
    public bool HasSceneImage { get; set; }
    public bool HasStudioImage { get; set; }
    public List<string> Issues { get; set; } = [];
    public List<string> Suggestions { get; set; } = [];
    public List<string> Checklist { get; set; } = [];

    /// <summary>Professional multi-dimension SEO report.</summary>
    public SeoProReport? Pro { get; set; }

    /// <summary>Auto-Fix Ultimate — تحلیل رقابتی، Gap و Ranking Readiness.</summary>
    public SeoUltimateReport? Ultimate { get; set; }

    public int RankingReadiness => Ultimate?.RankingReadiness ?? 0;
    public int CompetitiveStrength => Ultimate?.CompetitiveStrength ?? 0;

    /// <summary>امتیاز کل یکسان با پنل SEO — از Ultimate در صورت وجود.</summary>
    public int DisplayTotal =>
        Ultimate is not null ? SeoUltimateAnalyzer.ResolveDisplayOverall(Ultimate) : TotalScore;
}
