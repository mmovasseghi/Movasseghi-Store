namespace MovasseghiShop.Web.Services;

/// <summary>JSON-LD صفحهٔ خانه — چند بلوک جدا (بدون فقط @graph) برای اسکنرهای AEO/Foglift.</summary>
public static class StorefrontHomeJsonLd
{
    public static IEnumerable<object> BuildBlocks(
        string homeUrl,
        string? metaDescription,
        string? pageTitle,
        string? logoAbsolute,
        string catalogSearchUrl)
    {
        var orgId = $"{homeUrl.TrimEnd('/')}#organization";
        var localId = $"{homeUrl.TrimEnd('/')}#localbusiness";

        yield return new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Organization",
            ["@id"] = orgId,
            ["name"] = "فروشگاه موثقی",
            ["url"] = homeUrl,
            ["logo"] = logoAbsolute,
            ["telephone"] = WholesaleRules.CoordinationPhone,
            ["description"] = metaDescription,
            ["address"] = new Dictionary<string, object?>
            {
                ["@type"] = "PostalAddress",
                ["addressCountry"] = "IR",
                ["addressLocality"] = "تهران",
                ["addressRegion"] = "تهران"
            }
        };

        yield return new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "LocalBusiness",
            ["@id"] = localId,
            ["name"] = "فروشگاه موثقی",
            ["image"] = logoAbsolute,
            ["telephone"] = WholesaleRules.CoordinationPhone,
            ["url"] = homeUrl,
            ["priceRange"] = "$$",
            ["parentOrganization"] = new Dictionary<string, object?> { ["@id"] = orgId },
            ["geo"] = new Dictionary<string, object?>
            {
                ["@type"] = "GeoCoordinates",
                ["latitude"] = StorefrontHomeSeo.TehranLatitude,
                ["longitude"] = StorefrontHomeSeo.TehranLongitude
            },
            ["areaServed"] = new Dictionary<string, object?>
            {
                ["@type"] = "Country",
                ["name"] = "Iran"
            }
        };

        yield return new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Store",
            ["name"] = "فروشگاه موثقی",
            ["url"] = homeUrl,
            ["description"] = metaDescription,
            ["telephone"] = WholesaleRules.CoordinationPhone,
            ["image"] = logoAbsolute
        };

        yield return new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "WebSite",
            ["name"] = WholesaleRules.StoreNameFa,
            ["url"] = homeUrl,
            ["publisher"] = new Dictionary<string, object?> { ["@id"] = orgId },
            ["inLanguage"] = "fa-IR",
            ["potentialAction"] = new Dictionary<string, object?>
            {
                ["@type"] = "SearchAction",
                ["target"] = catalogSearchUrl,
                ["query-input"] = "required name=search_term_string"
            }
        };

        yield return new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "WebPage",
            ["@id"] = homeUrl,
            ["url"] = homeUrl,
            ["name"] = pageTitle,
            ["description"] = metaDescription,
            ["isPartOf"] = new Dictionary<string, object?> { ["@id"] = homeUrl },
            ["about"] = new Dictionary<string, object?> { ["@id"] = orgId },
            ["speakable"] = new Dictionary<string, object?>
            {
                ["@type"] = "SpeakableSpecification",
                ["cssSelector"] = new[] { ".ms-home-faq", ".ms-hero-title" }
            }
        };

        yield return new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "FAQPage",
            ["mainEntity"] = StorefrontHomeSeo.FaqItems.Select(f => (object)new Dictionary<string, object?>
            {
                ["@type"] = "Question",
                ["name"] = f.Question,
                ["acceptedAnswer"] = new Dictionary<string, object?>
                {
                    ["@type"] = "Answer",
                    ["text"] = f.Answer
                }
            }).ToArray()
        };
    }

    /// <summary>یک بلوک JSON-LD برای parse سریع‌تر Lighthouse (به‌جای چند تگ script).</summary>
    public static object BuildGraphDocument(
        string homeUrl,
        string? metaDescription,
        string? pageTitle,
        string? logoAbsolute,
        string catalogSearchUrl)
    {
        var nodes = BuildBlocks(homeUrl, metaDescription, pageTitle, logoAbsolute, catalogSearchUrl)
            .Select(static block =>
            {
                if (block is Dictionary<string, object?> d)
                {
                    var node = new Dictionary<string, object?>(d);
                    node.Remove("@context");
                    return (object)node;
                }
                return block;
            })
            .ToArray();

        return new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = nodes
        };
    }
}
