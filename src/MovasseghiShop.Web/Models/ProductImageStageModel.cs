namespace MovasseghiShop.Web.Models;

/// <summary>Shared product image stage — cards, offers, hero, search, gallery product slides.</summary>
public sealed class ProductImageStageModel
{
    public string? ImageUrl { get; init; }
    public string Alt { get; init; } = "";
    /// <summary>card | offer | featured | search</summary>
    public string Variant { get; init; } = "card";
    public int Width { get; init; } = 220;
    public int Height { get; init; } = 220;
    public string Loading { get; init; } = "lazy";
    public bool FetchPriority { get; init; }
    /// <summary>Three-layer sage/mint frame for studio product shots.</summary>
    public bool Framed { get; init; } = true;
    public string? ExtraClass { get; init; }
    public string? AlternateImageUrl { get; init; }
    public string AlternateAlt { get; init; } = "";
    public bool DualCycle { get; init; }
}
