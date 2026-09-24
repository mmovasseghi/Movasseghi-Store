namespace MovasseghiShop.Web.Models;

public class ContentPageHeader
{
    public string Page { get; set; } = "blog";
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public string CountLabel { get; set; } = "مطلب";
}
