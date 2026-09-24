namespace MovasseghiShop.Web.Models.Entities;

public class RankSnapshot
{
    public int Id { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public string KeywordType { get; set; } = "pillar";
    public int? EntityId { get; set; }
    /// <summary>Google position 1-100, 0 = not in top 100 / unknown.</summary>
    public int Position { get; set; }
    public string? TargetUrl { get; set; }
    public string Source { get; set; } = "manual";
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}
