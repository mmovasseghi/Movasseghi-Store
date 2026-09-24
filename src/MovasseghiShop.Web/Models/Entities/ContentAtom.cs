namespace MovasseghiShop.Web.Models.Entities;

public class ContentAtom
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public string? ImageUrl { get; set; }
    public string? CtaText { get; set; }
    public string? CtaUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
