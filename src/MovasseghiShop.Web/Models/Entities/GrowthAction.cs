namespace MovasseghiShop.Web.Models.Entities;

public class GrowthAction
{
    public int Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Priority { get; set; } = "medium";
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public string Status { get; set; } = "open";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
