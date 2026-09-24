using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Models.Enums;

namespace MovasseghiShop.Web.Services;

/// <summary>نظرات و بحث نمونه برای محصول — پیش‌نویس Pending تا مدیر تأیید کند.</summary>
public static class SeoProductReviewGenerator
{
    static readonly string[] BuyerNames =
    [
        "کیترینگ آریا", "رستوران سپید", "کافه باغچه", "هتل پارسیان", "تأمین‌کننده غرب",
        "پخش مواد غذایی رضا", "سوپرمارکت زنجیره‌ای نور", "آشپزخانه صنعتی مهر"
    ];

    public static List<ProductReview> BuildSuggestedThread(Product product, string focus)
    {
        if (product.Id <= 0) return [];

        var seed = product.Id * 17 + (product.Name?.Length ?? 0);
        var name = product.Name.Trim();
        var code = product.ProductCode ?? $"P{product.Id}";
        var cap = product.CapacityCc is > 0 ? $"{product.CapacityCc} سی‌سی" : "این سایز";
        var material = string.IsNullOrWhiteSpace(product.Material) ? "گیاهی آملون" : product.Material.Trim();

        var threads = new List<(string author, int rating, string body)>
        {
            (Pick(BuyerNames, seed), 5,
                $"برای پذیرایی عمده {name} گرفتیم. بسته‌بندی سالم بود و کد {code} با فاکتور یکی بود. "
                + $"برای {focus} کیفیت لبه‌ها مهم بود — راضی بودیم."),
            (Pick(BuyerNames, seed + 1), 4,
                $"سوال: {cap} برای سرو غذای گرم مناسب است؟ مایکروویو هم تست کردید؟"),
            (Pick(BuyerNames, seed + 2), 5,
                $"حداقل سفارش عمده چقدر است و ارسال به شهرستان چطور؟ برای {name} چند کارتن پیشنهاد می‌کنید؟"),
            (Pick(BuyerNames, seed + 3), 4,
                $"جنس {material} واقعاً یکبار مصرف است؟ با ظروف پلاستیکی معمولی چه تفاوتی برای رستوران دارد؟"),
            (Pick(BuyerNames, seed + 4), 5,
                $"دفعه قبل {name} دیر رسید؛ این بار زودتر هماهنگ می‌شود؟ شماره هماهنگی بارگیری را بفرستید.")
        };

        var list = new List<ProductReview>();
        var baseDate = DateTime.UtcNow.AddDays(-14);

        for (var i = 0; i < threads.Count; i++)
        {
            var (author, rating, body) = threads[i];
            var review = new ProductReview
            {
                ProductId = product.Id,
                AuthorDisplayName = author,
                Body = body,
                Rating = rating,
                Status = ProductReviewStatus.Pending,
                IsSeoSuggested = true,
                CreatedAt = baseDate.AddDays(i * 2).AddHours(i * 3)
            };
            list.Add(review);
        }

        // پاسخ فروشگاه روی سوال مایکروویو
        var qReview = list[1];
        list.Add(new ProductReview
        {
            ProductId = product.Id,
            ParentReviewId = null, // وصل بعد از ذخیره
            AuthorDisplayName = "پشتیبانی فروشگاه موثقی",
            Body = $"سلام — {name} از خط تولید آملون است و برای سرو گرم مناسب است. "
                   + "برای مایکروویو حتماً دمای متوسط و بدون درب فلزی استفاده کنید. "
                   + $"برای سفارش عمده حداقل {WholesaleRules.FormatMinAmount()} تومان است؛ "
                   + $"هماهنگی بارگیری با {WholesaleRules.CoordinationPhone}.",
            Rating = 0,
            Status = ProductReviewStatus.Pending,
            IsStoreReply = true,
            IsSeoSuggested = true,
            CreatedAt = qReview.CreatedAt.AddHours(5)
        });

        return list;
    }

    static string Pick(string[] items, int seed) => items[Math.Abs(seed) % items.Length];
}
