namespace MovasseghiShop.Web.Services.Editorial;

public sealed record EditorialImagePick(string Url, string RoleLabelFa, string? ProductName);

public sealed record EditorialBlogInventoryRow(
    int Id,
    string Title,
    bool IsPublished,
    int DisplayScore,
    bool IsPublishReady,
    string? FocusKeyword,
    string? ScheduledTehran,
    string? PublishedTehran,
    string FeaturedHint,
    int InlineImageCount);

public sealed record EditorialTimelineStep(
    int Step,
    string Title,
    string Detail,
    string? Badge);

public sealed record EditorialQualitySweepResult(
    int Purged,
    int ProducedReady,
    int TargetCount,
    IReadOnlyList<string> Log);
