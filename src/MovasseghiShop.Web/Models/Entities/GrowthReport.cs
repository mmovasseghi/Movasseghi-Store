namespace MovasseghiShop.Web.Models.Entities;

public class GrowthReport
{
    public int Id { get; set; }
    public DateTime WeekStart { get; set; }
    public string Title { get; set; } = string.Empty;
    public string BodyMarkdown { get; set; } = string.Empty;
    public string? MetricsJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
