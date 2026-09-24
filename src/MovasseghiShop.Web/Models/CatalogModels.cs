using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Models;

public class ProductFilterQuery
{
    public string? Q { get; set; }
    public string? Category { get; set; }
    public string? ProductType { get; set; }
    public string? UseCase { get; set; }
    public string? Packaging { get; set; }
    public int? CapacityMin { get; set; }
    public int? CapacityMax { get; set; }
    /// <summary>بازه ظرفیت: lt300 | 300-500 | 500-800 | gt800</summary>
    public string? Capacity { get; set; }
    public int? Compartments { get; set; }
    public bool? Microwave { get; set; }
    public string? Sort { get; set; }
    public int Page { get; set; } = 1;
}

public record FilterOption(string Value, string Label, int Count);

public class CatalogFilterModel
{
    public List<FilterOption> Categories { get; set; } = [];
    public List<FilterOption> ProductTypes { get; set; } = [];
    public List<FilterOption> UseCases { get; set; } = [];
    public List<FilterOption> Packaging { get; set; } = [];
    public List<FilterOption> CapacityBands { get; set; } = [];
    public List<FilterOption> Compartments { get; set; } = [];
    public int MicrowaveCount { get; set; }
    public int TotalResults { get; set; }
}

public class CatalogFiltersViewModel
{
    public CatalogFilterModel Facets { get; set; } = new();
    public ProductFilterQuery Query { get; set; } = new();
    public bool Mobile { get; set; }
}

/// <summary>صفحهٔ تمیز یک دسته در کاتالوگ — محتوای SEO ادمین / Auto-Fix.</summary>
public sealed class CatalogCategoryLandingViewModel
{
    public string Name { get; init; } = "";
    public string Slug { get; init; } = "";
    public string? ImageUrl { get; init; }
    public string? DescriptionHtml { get; init; }
    public string? FeaturedSnippet { get; init; }
    public string? HeroLead { get; init; }
    public IReadOnlyList<SeoFaq> Faqs { get; init; } = [];

    public bool HasProse => !string.IsNullOrWhiteSpace(DescriptionHtml);
    public bool HasSnippet => !string.IsNullOrWhiteSpace(FeaturedSnippet);
    public bool HasFaqs => Faqs.Count > 0;
    public bool HasAnyContent =>
        HasProse || HasSnippet || HasFaqs || !string.IsNullOrWhiteSpace(HeroLead);

    public bool HasHero => !string.IsNullOrWhiteSpace(Name);
}
