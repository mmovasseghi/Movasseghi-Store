namespace MovasseghiShop.Web.Services;

public static class CmsPreviewUrls
{
    public static string? ForPageKey(string key) => key switch
    {
        "about" => "/Page/About",
        "wholesale" => "/Page/Wholesale",
        "pricing" => "/Page/Pricing",
        "amelon" => "/Page/Amelon",
        "terms" => "/Page/Terms",
        "privacy" => "/Page/Privacy",
        _ => null
    };

    public static string ForHomeSection(string sectionKey) => sectionKey switch
    {
        "hero" => "/",
        "features" => "/#ms-features",
        "wholesale" => "/#ms-segments",
        "featured" => "/#ms-featured",
        "offers" => "/#ms-offers",
        _ => "/"
    };
}
