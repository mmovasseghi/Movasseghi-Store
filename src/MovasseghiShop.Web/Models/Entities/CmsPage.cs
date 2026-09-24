namespace MovasseghiShop.Web.Models.Entities;

public class CmsPage
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
