namespace MovasseghiShop.Web.Models.Entities;

public class HomeSection
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? BodyHtml { get; set; }
    public string? ConfigJson { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
