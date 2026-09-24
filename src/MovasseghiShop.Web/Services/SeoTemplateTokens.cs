namespace MovasseghiShop.Web.Services;

/// <summary>توکن‌های الگوی مشترک — در سنجش یکتایی سایت از intersection حذف می‌شوند (حقایق مشترک ≠ کپی).</summary>
public static class SeoTemplateTokens
{
    static readonly Lazy<HashSet<string>> Boilerplate = new(BuildBoilerplate);

    static HashSet<string> BuildBoilerplate()
    {
        const string shared =
            "خرید عمده نمایندگی رسمی آملون موثقی تهران انبار تأمین مستمر کارتن شرینک فله " +
            "رستوران کیترینگ کافه فست‌فود بیرون‌بر مایکروویو گیاهی نشاسته یکبار مصرف " +
            "پلیمر پلاستیک زیست‌محیطی پیش‌فاکتور کارشناس فروش استعلام قیمت حداقل سفارش " +
            "میلیون تومان ارسال سراسری تحویل کاری ضمانت اصالت مشاوره رایگان کاتالوگ " +
            "ظروف بسته‌بندی بهداشتی موجودی انبار عمده‌فروشی پخش wholesale";
        return SeoText.Tokenize(shared).Where(t => t.Length > 2).ToHashSet(StringComparer.Ordinal);
    }

    public static HashSet<string> DistinctiveTokenSet(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        var raw = SeoText.Tokenize(text);
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var t in raw)
        {
            if (t.Length < 2) continue;
            if (Boilerplate.Value.Contains(t) && !t.Any(char.IsDigit)) continue;
            set.Add(t);
        }
        return set;
    }

    public static double DistinctiveSimilarity(string a, string b)
    {
        var ta = DistinctiveTokenSet(a);
        var tb = DistinctiveTokenSet(b);
        if (ta.Count == 0 || tb.Count == 0) return 0;
        var inter = ta.Intersect(tb).Count();
        var union = ta.Count + tb.Count - inter;
        return union == 0 ? 0 : inter * 1.0 / union;
    }
}
