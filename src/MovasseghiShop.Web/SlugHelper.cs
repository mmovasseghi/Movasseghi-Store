using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MovasseghiShop.Web;

public static partial class SlugHelper
{
    public static string Generate(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "item";
        var normalized = text.Trim().ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("–", "-")
            .Replace("—", "-");
        normalized = InvalidCharsRegex().Replace(normalized, "");
        normalized = MultiDashRegex().Replace(normalized, "-").Trim('-');
        return string.IsNullOrEmpty(normalized) ? "item" : normalized;
    }

    public static string FormatToman(decimal amount)
        => amount.ToString("N0", CultureInfo.GetCultureInfo("fa-IR"));

    [GeneratedRegex(@"[^a-z0-9\u0600-\u06FF\-]")]
    private static partial Regex InvalidCharsRegex();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex MultiDashRegex();
}
