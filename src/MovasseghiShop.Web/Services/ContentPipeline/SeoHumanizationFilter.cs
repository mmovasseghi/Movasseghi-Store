namespace MovasseghiShop.Web.Services.ContentPipeline;

public static class SeoHumanizationFilter
{
    static readonly string[] BannedPhrases =
    [
        "در دنیای امروز",
        "بدون شک",
        "در نهایت",
        "به طور کلی",
        "به طور حتم",
        "شاید برایتان جالب باشد",
        "در این مقاله",
        "در ادامه با",
        "همانطور که می‌دانید",
        "یکی از بهترین",
        "بی‌شک"
    ];

    public static string Clean(string html)
    {
        var result = html;
        foreach (var phrase in BannedPhrases)
            result = result.Replace(phrase, "", StringComparison.Ordinal);

        result = result.Replace("پاسخ کوتاه: برای ", "برای ", StringComparison.Ordinal);
        result = result.Replace("این بخش بر اساس الگوی صفحات برتر", "در تجربه خریداران عمده", StringComparison.Ordinal);
        return result;
    }

    public static int Score(string plain)
    {
        if (string.IsNullOrWhiteSpace(plain)) return 0;
        var penalty = BannedPhrases.Count(p => plain.Contains(p, StringComparison.Ordinal)) * 12;
        var sentences = plain.Split(['.', '!', '؟', '?'], StringSplitOptions.RemoveEmptyEntries);
        var lengths = sentences.Select(s => s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length).ToList();
        var variance = lengths.Count > 2 ? lengths.Max() - lengths.Min() : 0;
        var score = 70 + Math.Min(20, variance * 2) - penalty;
        if (plain.Contains("کد محصول", StringComparison.Ordinal) || plain.Contains("SKU", StringComparison.Ordinal))
            score += 5;
        return Math.Clamp(score, 0, 100);
    }
}
