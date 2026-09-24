using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

/// <summary>جلوگیری از تکرار متن بین صفحات داخلی سایت.</summary>
public static class SeoSiteUniqueness
{
    const int SnippetWords = 55;

    public const double RewriteThresholdPercent = SeoContentRules.MaxDuplicationRisk;

    public static async Task<(double MaxSimilarity, string? ConflictLabel)> CheckAsync(
        ApplicationDbContext db,
        string entityType,
        int entityId,
        string plainText,
        string? entitySignature = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(plainText)) return (0, null);

        var anchored = Anchor(entitySignature, plainText);
        var windows = ExtractComparisonWindows(anchored);
        if (windows.Count == 0) return (0, null);

        var corpus = await LoadCorpusAsync(db, entityType, entityId, ct);
        var max = 0.0;
        string? label = null;

        foreach (var target in windows)
        {
            foreach (var entry in corpus)
            {
                var sim = SeoTemplateTokens.DistinctiveSimilarity(target, entry.Snippet);
                sim = ApplyCatalogIdentityDiscount(entityType, entitySignature, entry.Signature, sim);
                sim = ApplyEditorialIdentityDiscount(entityType, entitySignature, entry.Kind, entry.Signature, sim);
                sim = ApplyBlogProductCatalogDiscount(entityType, entry.Kind, sim);
                sim = ApplyBlogToBlogTemplateDiscount(entityType, entry.Kind, sim);

                if (sim <= max) continue;
                max = sim;
                label = $"{entry.Kind}: {entry.Label}";
            }
        }

        return (Math.Round(max * 100, 1), label);
    }

    public static bool ShouldRewrite(double similarityPercent) =>
        similarityPercent > RewriteThresholdPercent;

    static string Anchor(string? signature, string plain)
    {
        if (string.IsNullOrWhiteSpace(signature)) return plain;
        return $"{signature.Trim()} {plain}";
    }

    static List<string> ExtractComparisonWindows(string plainText)
    {
        var words = SeoText.StripHtml(plainText).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 12) return [];

        var list = new List<string> { TakeWords(words, 0) };
        if (words.Length > SnippetWords + 20)
            list.Add(TakeWords(words, words.Length / 3));
        if (words.Length > SnippetWords * 2)
            list.Add(TakeWords(words, Math.Max(0, words.Length - SnippetWords)));

        return list.Where(w => w.Length >= 40).Distinct().ToList();
    }

    static string TakeWords(string[] words, int start) =>
        string.Join(' ', words.Skip(start).Take(SnippetWords));

    sealed record CorpusEntry(string Kind, string Label, string Signature, string Snippet);

    static async Task<List<CorpusEntry>> LoadCorpusAsync(
        ApplicationDbContext db, string entityType, int entityId, CancellationToken ct)
    {
        var list = new List<CorpusEntry>();

        var products = await db.Products.AsNoTracking()
            .Where(p => entityType != "product" || p.Id != entityId)
            .Select(p => new { p.Name, p.ProductCode, p.ShortDescription, p.Description })
            .Take(200)
            .ToListAsync(ct);
        foreach (var p in products)
        {
            var plain = SeoText.StripHtml(p.Description ?? "");
            var signature = $"{p.Name} {p.ProductCode}".Trim();
            var mix = Anchor(signature, string.Join(" ", p.ShortDescription ?? "", plain));
            AddCorpusSnippets(list, "محصول", p.Name, signature, mix);
        }

        var blogs = await db.BlogPosts.AsNoTracking()
            .Where(b => entityType != "blog" || b.Id != entityId)
            .Select(b => new { b.Id, b.Title, b.Excerpt, b.Content })
            .Take(100)
            .ToListAsync(ct);
        foreach (var b in blogs)
        {
            var plain = SeoText.StripHtml(b.Content ?? "");
            var signature = BlogSignature(new BlogPost { Id = b.Id, Title = b.Title }, null);
            var mix = Anchor(signature, string.Join(" ", b.Excerpt ?? "", plain));
            AddCorpusSnippets(list, "بلاگ", b.Title, signature, mix);
        }

        var news = await db.NewsItems.AsNoTracking()
            .Where(n => entityType != "news" || n.Id != entityId)
            .Select(n => new { n.Title, n.Excerpt, n.Content })
            .Take(100)
            .ToListAsync(ct);
        foreach (var n in news)
        {
            var plain = SeoText.StripHtml(n.Content ?? "");
            var mix = Anchor(n.Title, string.Join(" ", n.Excerpt ?? "", plain));
            AddCorpusSnippets(list, "خبر", n.Title, n.Title, mix);
        }

        return list.Where(x => x.Snippet.Length >= 30).ToList();
    }

    static void AddCorpusSnippets(
        List<CorpusEntry> list, string kind, string label, string signature, string text)
    {
        var words = SeoText.StripHtml(text).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 12) return;

        list.Add(new CorpusEntry(kind, label, signature, TakeWords(words, 0)));
        if (words.Length > SnippetWords + 20)
            list.Add(new CorpusEntry(kind, label, signature, TakeWords(words, words.Length / 3)));
    }

    public static string ProductSignature(Product product) =>
        $"{product.Name} {product.ProductCode} #{product.Id}".Trim();

    public static string BlogSignature(BlogPost post, string? focusKeyword = null) =>
        $"{post.Title} #{post.Id} {focusKeyword}".Trim();

    /// <summary>اشاره به کاتالوگ/محصول در مقاله ≠ کپی توضیح محصول.</summary>
    static double ApplyBlogProductCatalogDiscount(string entityType, string corpusKind, double bodySimilarity)
    {
        if (entityType == "blog" && corpusKind == "محصول")
            return bodySimilarity * 0.22;
        return bodySimilarity;
    }

    /// <summary>بخش‌های مشترک راهنما (AEO/عمده) بین مقالات ≠ کپی محتوا.</summary>
    static double ApplyBlogToBlogTemplateDiscount(string entityType, string corpusKind, double bodySimilarity)
    {
        if (entityType == "blog" && corpusKind == "بلاگ")
            return bodySimilarity * 0.32;
        return bodySimilarity;
    }

    static double ApplyEditorialIdentityDiscount(
        string entityType, string? entitySignature, string corpusKind, string corpusSignature, double bodySimilarity)
    {
        if (entityType is not ("blog" or "news") || corpusKind is not ("بلاگ" or "خبر"))
            return bodySimilarity;
        if (string.IsNullOrWhiteSpace(entitySignature))
            return bodySimilarity;

        var identitySim = SeoTemplateTokens.DistinctiveSimilarity(
            Anchor(entitySignature, ""),
            Anchor(corpusSignature, ""));
        if (identitySim >= 0.45)
            return bodySimilarity;

        var factor = 0.18 + identitySim * 0.4;
        return bodySimilarity * factor;
    }

    static double ApplyCatalogIdentityDiscount(
        string entityType, string? entitySignature, string corpusSignature, double bodySimilarity)
    {
        if (entityType != "product" || string.IsNullOrWhiteSpace(entitySignature))
            return bodySimilarity;

        var identitySim = SeoTemplateTokens.DistinctiveSimilarity(
            Anchor(entitySignature, ""),
            Anchor(corpusSignature, ""));

        if (identitySim >= 0.55)
            return bodySimilarity;

        var factor = 0.12 + identitySim * 0.35;
        return bodySimilarity * factor;
    }
}
