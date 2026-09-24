namespace MovasseghiShop.Web.Models.Entities;

/// <summary>صف موضوعات بلاگ — از ستون‌های کلیدی، GSC و شکاف محتوا.</summary>
public class EditorialTopicQueue
{
    public int Id { get; set; }
    public string FocusKeyword { get; set; } = string.Empty;
    public string SecondaryKeyword { get; set; } = string.Empty;
    public string ProposedTitle { get; set; } = string.Empty;
    public string Angle { get; set; } = "guide";
    public int PillarPriority { get; set; }
    /// <summary>امتیاز فرصت (۰–۱۰۰) — مانورپذیری + اولویت + نبود محتوای تکراری.</summary>
    public double OpportunityScore { get; set; }
    public string Source { get; set; } = "pillar";
    public string Status { get; set; } = "planned";
    public int? BlogPostId { get; set; }
    public DateTime? ScheduledPublishUtc { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? LastError { get; set; }
}
