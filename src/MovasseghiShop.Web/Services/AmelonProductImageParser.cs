using System.Text.Json;
using System.Text.RegularExpressions;

namespace MovasseghiShop.Web.Services;

public static partial class AmelonProductImageParser
{
    /// <summary>Extract full-size product gallery images from amelon.co product HTML.</summary>
    public static List<string> ExtractImageUrls(string html)
    {
        var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match m in GalleryHrefRegex().Matches(html))
            TryAddProductImage(urls, m.Groups[1].Value);

        foreach (Match m in GalleryDataLargeRegex().Matches(html))
            TryAddProductImage(urls, m.Groups[1].Value);

        var og = OgImageRegex().Match(html).Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(og))
            TryAddProductImage(urls, og);

        TryAddFromJsonLd(html, urls);

        return urls
            .OrderBy(u => u, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void TryAddFromJsonLd(string html, HashSet<string> urls)
    {
        var jsonLd = JsonLdRegex().Match(html).Groups[1].Value;
        if (string.IsNullOrWhiteSpace(jsonLd)) return;

        try
        {
            using var doc = JsonDocument.Parse(jsonLd);
            if (!doc.RootElement.TryGetProperty("@graph", out var graph)) return;

            foreach (var node in graph.EnumerateArray())
            {
                if (node.TryGetProperty("@type", out var t) && t.GetString() != "Product") continue;
                if (!node.TryGetProperty("image", out var imgEl)) continue;

                if (imgEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in imgEl.EnumerateArray())
                        AddJsonImage(urls, item);
                }
                else
                    AddJsonImage(urls, imgEl);
            }
        }
        catch { /* ignore malformed JSON-LD */ }
    }

    private static void AddJsonImage(HashSet<string> urls, JsonElement item)
    {
        if (item.ValueKind == JsonValueKind.String)
            TryAddProductImage(urls, item.GetString());
        else if (item.TryGetProperty("url", out var urlEl))
            TryAddProductImage(urls, urlEl.GetString());
        else if (item.TryGetProperty("@id", out var idEl))
            TryAddProductImage(urls, idEl.GetString());
    }

    private static void TryAddProductImage(HashSet<string> urls, string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        url = url.Trim().Replace("&amp;", "&");
        if (!url.Contains("/wp-content/uploads/", StringComparison.OrdinalIgnoreCase)) return;
        if (url.Contains("/elementor/", StringComparison.OrdinalIgnoreCase)) return;
        if (ThumbnailRegex().IsMatch(url)) return;
        if (url.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)) return;

        urls.Add(NormalizeAmelonUrl(url));
    }

    public static string NormalizeAmelonUrl(string url)
    {
        if (url.StartsWith("//", StringComparison.Ordinal))
            url = "https:" + url;
        if (url.StartsWith("/", StringComparison.Ordinal))
            url = "https://amelon.co" + url;
        return url;
    }

    /// <summary>Build a Uri safe for HttpClient (encodes Unicode path segments from amelon.co).</summary>
    public static Uri ToRequestUri(string url)
    {
        url = NormalizeAmelonUrl(url);

        var hashIdx = url.IndexOf('#');
        if (hashIdx >= 0)
            url = url[..hashIdx];

        if (Uri.TryCreate(url, UriKind.Absolute, out var parsed))
            return EncodeUriPath(parsed);

        return EncodeRawUrl(url);
    }

    private static Uri EncodeUriPath(Uri uri)
    {
        var path = uri.AbsolutePath;
        var encodedPath = EncodePathSegments(path);
        if (encodedPath == path)
            return uri;

        var builder = new UriBuilder(uri) { Path = encodedPath };
        return builder.Uri;
    }

    private static Uri EncodeRawUrl(string url)
    {
        const string delimiter = "://";
        var schemeEnd = url.IndexOf(delimiter, StringComparison.Ordinal);
        if (schemeEnd < 0)
            throw new UriFormatException($"Invalid URI: {url}");

        var scheme = url[..schemeEnd];
        var rest = url[(schemeEnd + delimiter.Length)..];

        var pathStart = rest.IndexOf('/');
        string authority;
        string pathAndQuery;
        if (pathStart < 0)
        {
            authority = rest;
            pathAndQuery = "/";
        }
        else
        {
            authority = rest[..pathStart];
            pathAndQuery = rest[pathStart..];
        }

        var queryIdx = pathAndQuery.IndexOf('?');
        var path = queryIdx >= 0 ? pathAndQuery[..queryIdx] : pathAndQuery;
        var query = queryIdx >= 0 ? pathAndQuery[queryIdx..] : string.Empty;

        return new Uri($"{scheme}://{authority}{EncodePathSegments(path)}{query}");
    }

    private static string EncodePathSegments(string path)
    {
        if (string.IsNullOrEmpty(path) || path == "/")
            return path;

        var leadingSlash = path.StartsWith('/') ? "/" : "";
        var segments = path.TrimStart('/').Split('/');
        var encoded = segments.Select(s => Uri.EscapeDataString(Uri.UnescapeDataString(s)));
        return leadingSlash + string.Join('/', encoded);
    }

    [GeneratedRegex(@"woocommerce-product-gallery__image[\s\S]*?<a\s+href=""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex GalleryHrefRegex();

    [GeneratedRegex(@"data-large_image=""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex GalleryDataLargeRegex();

    [GeneratedRegex(@"<meta property=""og:image"" content=""(.*?)""")]
    private static partial Regex OgImageRegex();

    [GeneratedRegex(@"<script[^>]*class=""rank-math-schema-pro""[^>]*>(.*?)</script>", RegexOptions.Singleline)]
    private static partial Regex JsonLdRegex();

    [GeneratedRegex(@"-\d+x\d+\.(png|jpe?g|webp|gif)$", RegexOptions.IgnoreCase)]
    private static partial Regex ThumbnailRegex();
}
