using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Html;

namespace MovasseghiShop.Web.Services;

/// <summary>Razor نوع <c>application/ld+json</c> را به &#x2B;json تبدیل می‌کند — اسکنرها schema نمی‌بینند.</summary>
public static class StorefrontJsonLd
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    public static IHtmlContent Script(object graph)
    {
        var json = JsonSerializer.Serialize(graph, JsonOptions);
        return new HtmlString("<script type=\"application/ld+json\">" + json + "</script>");
    }
}
