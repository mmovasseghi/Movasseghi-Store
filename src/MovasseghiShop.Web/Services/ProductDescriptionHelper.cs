using System.Text;
using System.Text.RegularExpressions;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

public static class ProductDescriptionHelper
{
    static readonly Regex SpecsSectionRegex = new(
        @"<section\s+class=""product-specs""[^>]*>.*?</section>",
        RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Body HTML for the visual editor (without auto-generated specs block).</summary>
    public static string GetProseForEditor(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return "";
        return StripSpecsSections(description);
    }

    public static string BuildSpecsSectionHtml(Product p)
    {
        var sb = new StringBuilder();
        sb.Append("<section class=\"product-specs\">");
        sb.Append("<h2>مشخصات فنی</h2><ul>");
        if (!string.IsNullOrWhiteSpace(p.ProductCode))
            sb.Append($"<li><strong>کد محصول آملون:</strong> {Escape(p.ProductCode)}</li>");
        if (!string.IsNullOrWhiteSpace(p.Dimensions))
            sb.Append($"<li><strong>ابعاد:</strong> {Escape(p.Dimensions)}</li>");
        if (!string.IsNullOrWhiteSpace(p.Material))
            sb.Append($"<li><strong>جنس:</strong> {Escape(p.Material)}</li>");
        if (p.CapacityCc.HasValue)
            sb.Append($"<li><strong>ظرفیت:</strong> {p.CapacityCc} سی‌سی</li>");
        if (p.CompartmentCount.HasValue)
            sb.Append($"<li><strong>تعداد خانه:</strong> {p.CompartmentCount}</li>");
        if (p.MicrowaveSafe)
            sb.Append("<li><strong>مایکروویو:</strong> قابل استفاده</li>");
        if (!string.IsNullOrWhiteSpace(p.Applications))
            sb.Append($"<li><strong>کاربرد:</strong> {Escape(p.Applications)}</li>");
        sb.Append("</ul></section>");
        return sb.ToString();
    }

    public static string MergeDescription(string? proseHtml, Product p)
    {
        var body = string.IsNullOrWhiteSpace(proseHtml) ? "" : proseHtml.Trim();
        body = StripSpecsSections(body);
        var specs = BuildSpecsSectionHtml(p);
        if (string.IsNullOrWhiteSpace(body)) return specs;
        return body + specs;
    }

    static string StripSpecsSections(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return "";
        var prev = html;
        while (true)
        {
            var next = SpecsSectionRegex.Replace(prev, "").Trim();
            if (next.Length == prev.Length) return next;
            prev = next;
        }
    }

    static string Escape(string value) =>
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
