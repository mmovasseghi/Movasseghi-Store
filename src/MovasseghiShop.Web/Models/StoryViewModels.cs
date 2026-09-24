namespace MovasseghiShop.Web.Models;

public record StorySlideDto(string Image, string Title, string Link);

public record StoryItemDto(
    string Id,
    string Label,
    string IconKey,
    IReadOnlyList<StorySlideDto> Slides,
    string? CoverImageUrl = null);
