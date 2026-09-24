namespace MovasseghiShop.Web.Models.Entities;

public class HomeStorySlide
{
    public int Id { get; set; }
    public int HomeStoryId { get; set; }
    public HomeStory HomeStory { get; set; } = null!;
    public string ImageUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string LinkUrl { get; set; } = "/Catalog";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
