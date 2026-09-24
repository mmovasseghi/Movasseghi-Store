using System.Text.RegularExpressions;

namespace MovasseghiShop.Web;

/// <summary>لینک‌های ریشه‌ای (/Catalog) را با PathBase (مثلاً /MOVASSEGHISTORE) یکی می‌کند.</summary>
public static class AppPath
{
    static readonly Regex HtmlRootAttrRegex = new(
        @"(?<attr>src|href|poster)\s*=\s*(?<q>['""])(/(?!//))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>URL مطلق برای og:image و JSON-LD (شامل PathBase).</summary>
    public static string? Absolute(string? path, HttpContext ctx)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return path;

        var local = H(path, ctx);
        return $"{ctx.Request.Scheme}://{ctx.Request.Host}{local}";
    }

    /// <summary>محتوای HTML ذخیره‌شده (ادیتور) — src/href ریشه‌ای را با PathBase می‌چسباند.</summary>
    public static string RewriteHtmlRootUrls(string? html, HttpContext ctx)
    {
        if (string.IsNullOrEmpty(html)) return html ?? "";
        var pathBase = ctx.Request.PathBase.Value ?? "";
        if (string.IsNullOrEmpty(pathBase)) return html;

        return HtmlRootAttrRegex.Replace(html, m =>
        {
            var url = m.Groups[2].Value;
            if (url.StartsWith(pathBase, StringComparison.OrdinalIgnoreCase))
                return m.Value;
            return $"{m.Groups["attr"].Value}={m.Groups["q"].Value}{pathBase}{url}";
        });
    }

    public static string H(string? path, HttpContext ctx)
    {
        if (string.IsNullOrWhiteSpace(path))
            return (ctx.Request.PathBase.Value ?? "") + "/";

        path = path.Trim();
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith('#'))
            return path;

        var pathBase = ctx.Request.PathBase.Value ?? "";
        if (path.StartsWith('~'))
            path = path[1..];
        if (!path.StartsWith('/'))
            path = "/" + path;

        if (!string.IsNullOrEmpty(pathBase)
            && path.StartsWith(pathBase, StringComparison.OrdinalIgnoreCase))
            return path;

        return pathBase + path;
    }
}
