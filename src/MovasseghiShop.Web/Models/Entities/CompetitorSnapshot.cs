namespace MovasseghiShop.Web.Models.Entities;

public class CompetitorSnapshot
{
    public int Id { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public int RankPosition { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Url { get; set; }
    public int? WordCountEstimate { get; set; }
    public bool HasFaqSchema { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}
