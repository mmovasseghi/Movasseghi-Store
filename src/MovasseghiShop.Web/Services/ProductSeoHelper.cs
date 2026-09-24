using System.Text.Json;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

public static class ProductSeoHelper
{
    public sealed record ProductFaq(string Question, string Answer);

    public static List<ProductFaq> GetFaqs(Product product, SeoProfile? profile, string? cleanApplication)
    {
        var fromProfile = ParseFaqJson(profile?.FaqJson);
        if (fromProfile.Count >= 3)
            return fromProfile;

        return BuildFallbackFaqs(product, cleanApplication);
    }

    public static string ResolveBuyboxSummary(Product product, SeoProfile? profile) =>
        SeoStorefrontDisplay.ResolveProductBuyboxSummary(product, profile);

    static List<ProductFaq> ParseFaqJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            using var doc = JsonDocument.Parse(json);
            var list = new List<ProductFaq>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var q = item.TryGetProperty("Question", out var qEl) ? qEl.GetString()
                    : item.TryGetProperty("question", out var q2) ? q2.GetString() : null;
                var a = item.TryGetProperty("ShortAnswer", out var aEl) ? aEl.GetString()
                    : item.TryGetProperty("shortAnswer", out var a2) ? a2.GetString()
                    : item.TryGetProperty("FullAnswer", out var fEl) ? fEl.GetString()
                    : item.TryGetProperty("fullAnswer", out var f2) ? f2.GetString() : null;
                if (!string.IsNullOrWhiteSpace(q) && !string.IsNullOrWhiteSpace(a))
                    list.Add(new ProductFaq(q, a));
            }
            return list;
        }
        catch
        {
            return [];
        }
    }

    static List<ProductFaq> BuildFallbackFaqs(Product product, string? cleanApp)
    {
        var faqs = new List<ProductFaq>
        {
            new($"حداقل سفارش {product.Name} چقدر است؟",
                $"حداقل سفارش عمده از فروشگاه موثقی {WholesaleRules.FormatMinAmount()} تومان است."),
            new("آیا این محصول اصل آملون است؟",
                "بله. فروشگاه موثقی نمایندگی رسمی آملون در تهران است."),
            new("چگونه استعلام قیمت بگیرم؟",
                $"فرم استعلام همین صفحه را پر کنید یا با {WholesaleRules.CoordinationPhone} تماس بگیرید.")
        };
        if (product.MicrowaveSafe)
            faqs.Add(new("آیا در مایکروویو قابل استفاده است؟", "بله، برای مایکروویو مناسب است."));
        if (!string.IsNullOrWhiteSpace(cleanApp))
            faqs.Add(new($"{product.Name} برای چه مصرفی مناسب است؟", cleanApp));
        return faqs;
    }
}
