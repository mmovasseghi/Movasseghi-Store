namespace MovasseghiShop.Web.Models.Entities;

public class HomeStory
{
    public int Id { get; set; }
    /// <summary>شناسه یکتا برای data-story-id (مثلاً all، wholesale)</summary>
    public string StoryKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    /// <summary>کلید آیکون SVG وقتی تصویر کاور نداریم</summary>
    public string IconKey { get; set; } = "all";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<HomeStorySlide> Slides { get; set; } = [];
}
