namespace MovasseghiShop.Web;

/// <summary>لینک‌های ریشه‌ای (/Catalog) را با PathBase (مثلاً /MOVASSEGHISTORE) یکی می‌کند.</summary>
public static class AppPath
{
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
