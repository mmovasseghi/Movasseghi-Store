using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services.Editorial;

/// <summary>کلیدواژه‌های مقاله — Focus از تحقیق + pillarهای سایت.</summary>
public static class EditorialKeywordSync
{
    public static string BuildMetaKeywords(string focus, string secondary, string? title = null)
    {
        var keywords = new List<string>();
        void Add(string? phrase)
        {
            if (string.IsNullOrWhiteSpace(phrase)) return;
            var p = phrase.Trim();
            if (!keywords.Contains(p, StringComparer.OrdinalIgnoreCase))
                keywords.Add(p);
        }

        Add(focus);
        Add(secondary);
        Add(SiteKeywordStrategy.Primary);
        Add(SiteKeywordStrategy.Tertiary);

        foreach (var pillar in SiteKeywordStrategy.Pillars.OrderBy(p => p.Priority).Take(8))
            Add(pillar.Phrase);

        if (!string.IsNullOrWhiteSpace(title))
        {
            foreach (var token in SeoText.Tokenize(title).Where(t => t.Length > 3).Take(4))
                Add(token);
        }

        return string.Join("، ", keywords.Take(14));
    }

    public static void ApplyToPost(BlogPost post, SeoProfile profile)
    {
        var focus = profile.FocusKeyword;
        if (string.IsNullOrWhiteSpace(focus))
            focus = SeoKeywordResearch.ResolveEditorialKeywords(post.Title, []).Primary;

        var secondary = profile.SecondaryKeyword ?? SiteKeywordStrategy.Secondary;
        profile.FocusKeyword = focus.Trim();
        profile.SecondaryKeyword = secondary.Trim();

        var meta = BuildMetaKeywords(profile.FocusKeyword, profile.SecondaryKeyword, post.Title);
        post.MetaKeywords = meta;
        profile.MetaKeywords = meta;
    }
}
