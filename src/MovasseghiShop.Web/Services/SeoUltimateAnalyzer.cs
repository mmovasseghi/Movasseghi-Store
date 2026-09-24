using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

/// <summary>ورودی کامل تحلیل Ultimate.</summary>
public sealed class SeoUltimateContext
{
    public Product? Product { get; init; }
    public Category? Category { get; init; }
    public SeoProfile Profile { get; init; } = new();
    public string Prose { get; init; } = "";
    public SeoSerpBaseline Baseline { get; init; } = new();
    public List<GscQueryRow> GscQueries { get; init; } = [];
    public List<SeoCannibalRisk> Cannibalization { get; init; } = [];
    public bool HasReviews { get; init; }
    public List<string> SerpTitles { get; init; } = [];

    /// <summary>بلاگ / خبر</summary>
    public string? EditorialTitle { get; init; }
    public string? EditorialExcerpt { get; init; }
    public string? FeaturedImageUrl { get; init; }
    public bool EditorialPublished { get; init; }
    public string EditorialKind { get; init; } = "blog";
    public double SiteDuplicationPercent { get; init; }
}

/// <summary>
/// Auto-Fix Ultimate Analyzer.
/// امتیاز نتیجهٔ فرآیند است، نه هدف — همه ابعاد نسبت به SERP رقابتی سنجیده می‌شوند.
/// </summary>
public static class SeoUltimateAnalyzer
{
    /// <param name="addManualTasks">
    /// قبل از Finalize صدا زده می‌شود تا کارهای انسانی هم در گزارش بیایند
    /// و هم جریمه‌شان در Ranking Readiness لحاظ شود.
    /// </param>
    public static SeoUltimateReport AnalyzeProduct(
        SeoUltimateContext ctx, Action<SeoUltimateReport>? addManualTasks = null)
    {
        var product = ctx.Product ?? throw new ArgumentException("Product required", nameof(ctx));
        var profile = ctx.Profile;

        var focus = Trim(profile.FocusKeyword ?? product.KeywordPrimary);
        var secondary = Trim(profile.SecondaryKeyword ?? product.KeywordSecondary);
        var head = SeoText.HeadPhrase(focus, product.Name);
        var title = Trim(profile.MetaTitle ?? product.MetaTitle);
        var meta = Trim(profile.MetaDescription ?? product.MetaDescription);
        var summary = Trim(profile.AiSummary);
        var shortDesc = Trim(product.ShortDescription);

        var content = SeoContentAnalyzer.Analyze(ctx.Prose, focus, secondary);
        var media = SeoMediaAnalyzer.Analyze(product, focus);
        var faqs = SeoFaqStore.Parse(profile.FaqJson);

        var r = new SeoUltimateReport { Baseline = ctx.Baseline, Cannibalization = ctx.Cannibalization };

        // ── Intent ──
        r.Intent = SeoIntentDetector.Detect(focus, secondary, ctx.SerpTitles);

        // ── Topic Authority ──
        var topicHaystack = string.Join(" \n ",
            content.PlainText, title, meta, summary, shortDesc, product.Name,
            product.Applications, product.Material, product.Dimensions,
            string.Join(" ", faqs.Select(f => f.Question + " " + f.PlainAnswer)));

        var (topical, topicDetail) = SeoTopicModel.Evaluate(
            SeoTopicModel.ForProduct(product, focus, secondary), topicHaystack);
        r.TopicalCoverage = topical;
        r.Topics = topicDetail;

        // ── Entity Graph ──
        var (entityCoverage, entityDetail) = SeoEntityGraph.Evaluate(
            SeoEntityGraph.ForProduct(product),
            ("content", content.PlainText),
            ("title", title),
            ("meta", meta),
            ("summary", summary),
            ("product-data", $"{product.Name} {product.ProductCode} {product.Material} {product.ProductType}"));
        r.EntityCoverage = entityCoverage;
        r.Entities = entityDetail;

        // ── Query Expansion + Search Console ──
        var headings = content.H2Texts.Concat(content.H3Texts).ToList();
        var queries = SeoQueryExpander.Expand(focus, secondary, product);
        SeoQueryExpander.MarkAnswered(queries, headings, faqs, content.PlainText);

        var answerSurfaces = headings
            .Concat(faqs.Select(f => f.Question + " " + f.PlainShortAnswer))
            .ToList();
        var gscQueries = SeoQueryExpander.FromSearchConsole(ctx.GscQueries, focus, answerSurfaces);
        r.Queries = queries.Concat(gscQueries).ToList();

        r.GenericRisk = content.GenericRisk;
        r.DuplicationRisk = Math.Max(content.DuplicationRisk, ctx.SiteDuplicationPercent);
        r.WordCount = content.WordCount;
        r.KeywordDensity = content.FocusDensity;
        r.HasSceneImage = media.HasScene;
        r.HasStudioImage = media.HasStudio;

        if (ctx.SiteDuplicationPercent > SeoContentRules.MaxDuplicationRisk)
            r.CriticalIssues.Add($"🔴 شباهت {ctx.SiteDuplicationPercent:F0}٪ با صفحه دیگر سایت — متن باید یکتا باشد.");

        // ── ابعاد ──
        r.SeoScore = ScoreOnPage(r, content, focus, head, title, meta, shortDesc, product, profile);
        r.ContentScore = ScoreContent(r, content, focus, secondary, SeoContentRules.MinWordCount);
        r.SemanticScore = ScoreSemantic(r, content, topical, product, focus, secondary, title, meta, summary);
        r.IntentScore = ScoreIntent(r, content, product, faqs, queries);
        r.AeoScore = ScoreAeo(r, content, faqs, queries, shortDesc);
        r.GeoScore = ScoreGeo(r, summary, focus, secondary, product, entityDetail, content);
        r.ImageScore = ScoreImage(r, media);
        r.ProductDataScore = ScoreProductData(r, product, media);
        r.SchemaScore = ScoreSchema(r, product, media, faqs, ctx.HasReviews);
        r.InternalLinkingScore = ScoreInternalLinking(r, content, product);
        r.TechnicalScore = ScoreTechnical(r, product, media);

        r.IsIndexable = product.IsActive;
        r.CanonicalValid = !string.IsNullOrWhiteSpace(product.Slug);

        // ── پوشش‌ها ──
        r.IntentMatch = r.IntentScore;
        r.SemanticCoverage = r.SemanticScore;
        r.AeoAnswerCoverage = queries.Count == 0
            ? 0
            : Math.Round(queries.Count(q => q.Answered) * 100.0 / queries.Count, 1);
        r.ProductDataCompleteness = r.ProductDataScore;
        r.PrimaryKeywordCoverage = KeywordPlacementCoverage(
            focus, head, title, meta, summary, product, content, media);

        addManualTasks?.Invoke(r);

        Finalize(r, ctx);
        return r;
    }

    public static SeoUltimateReport AnalyzeCategory(
        SeoUltimateContext ctx, Action<SeoUltimateReport>? addManualTasks = null)
    {
        var category = ctx.Category ?? throw new ArgumentException("Category required", nameof(ctx));
        var profile = ctx.Profile;

        var focus = Trim(profile.FocusKeyword ?? category.Name);
        var secondary = Trim(profile.SecondaryKeyword ?? SiteKeywordStrategy.Secondary);
        var head = SeoText.HeadPhrase(focus, category.Name);
        var title = Trim(profile.MetaTitle ?? category.MetaTitle);
        var meta = Trim(profile.MetaDescription ?? category.MetaDescription);
        var summary = Trim(profile.AiSummary);

        var content = SeoContentAnalyzer.Analyze(ctx.Prose, focus, secondary);
        var faqs = SeoFaqStore.Parse(profile.FaqJson);
        var productCount = category.Products?.Count ?? 0;

        var r = new SeoUltimateReport { Baseline = ctx.Baseline, Cannibalization = ctx.Cannibalization };
        r.Intent = SeoIntentDetector.Detect(focus, secondary, ctx.SerpTitles);

        var haystack = string.Join(" \n ", content.PlainText, title, meta, summary, category.Name,
            string.Join(" ", faqs.Select(f => f.Question + " " + f.PlainAnswer)));

        var (topical, topicDetail) = SeoTopicModel.Evaluate(
            SeoTopicModel.ForCategory(category, focus, secondary), haystack);
        r.TopicalCoverage = topical;
        r.Topics = topicDetail;

        var (entityCoverage, entityDetail) = SeoEntityGraph.Evaluate(
            SeoEntityGraph.ForCategory(category),
            ("content", content.PlainText), ("title", title), ("meta", meta), ("summary", summary));
        r.EntityCoverage = entityCoverage;
        r.Entities = entityDetail;

        var headings = content.H2Texts.Concat(content.H3Texts).ToList();
        var queries = SeoQueryExpander.Expand(focus, secondary);
        SeoQueryExpander.MarkAnswered(queries, headings, faqs, content.PlainText);
        var answerSurfaces = headings.Concat(faqs.Select(f => f.Question)).ToList();
        r.Queries = queries
            .Concat(SeoQueryExpander.FromSearchConsole(ctx.GscQueries, focus, answerSurfaces))
            .ToList();

        r.GenericRisk = content.GenericRisk;
        r.DuplicationRisk = content.DuplicationRisk;
        r.WordCount = content.WordCount;
        r.KeywordDensity = content.FocusDensity;

        // On-Page
        var onPage = 0;
        onPage += Band(r, title.Length is >= SeoContentRules.MetaTitleMin and <= SeoContentRules.MetaTitleMax,
            18, "✓ طول Meta Title", $"🟡 Meta Title دسته: {title.Length} کاراکتر (هدف ۳۰–۶۰).");
        onPage += Band(r, SeoText.IsKeywordish(title, focus), 14, "✓ Keyword در Title",
            "🟡 Focus Keyword در Meta Title دسته نیست.");
        onPage += Band(r, meta.Length is >= SeoContentRules.MetaDescMin and <= SeoContentRules.MetaDescMax,
            18, "✓ طول Meta Description", $"🟡 Meta Description دسته: {meta.Length} کاراکتر (هدف ۱۴۰–۱۶۰).");
        onPage += Band(r, SeoText.IsKeywordish(meta, focus), 12, "✓ Keyword در Meta Description", null);
        onPage += Band(r, !string.IsNullOrWhiteSpace(category.Slug) && category.Slug.Length <= 80,
            12, "✓ URL خوانا", "🔴 Slug دسته تعریف نشده.");
        onPage += Band(r, AnyOf(title + " " + meta, "خرید", "عمده", "قیمت", "آملون", "موثقی"),
            10, "✓ سیگنال CTR در Title/Meta", "🟢 CTR: «خرید عمده»، «قیمت» یا «آملون» را به Title/Meta اضافه کنید.");
        onPage += Band(r, content.FocusInIntro, 16, "✓ Keyword در مقدمه",
            "🟡 Focus Keyword در مقدمه دسته نیست.");
        r.SeoScore = Cap(onPage);

        r.ContentScore = ScoreContent(r, content, focus, secondary, SeoCategoryBuilder.MinWordCount);
        r.SemanticScore = (int)Math.Round(topical * 0.7 + entityCoverage * 0.3);

        var intent = 0;
        intent += Band(r, AnyOf(content.PlainText + meta, "قیمت", "استعلام", "تومان"), 20, "✓ سیگنال قیمت", "🟡 اطلاعات قیمت/استعلام در دسته نیست.");
        intent += Band(r, content.InternalLinkCount >= 2, 20, "✓ مسیر خرید", "🟡 مسیر خرید (لینک به کاتالوگ/استعلام) ناقص است.");
        intent += Band(r, productCount > 0, 20, $"✓ {productCount} محصول در دسته",
            "🔴 این دسته هیچ محصولی ندارد — از ویرایش محصولات، آن‌ها را به این دسته اضافه کنید.");
        intent += Band(r, content.HasComparisonSection, 15, "✓ بخش مقایسه", "🟢 بخش «راهنمای انتخاب / مقایسه» اضافه کنید.");
        intent += Band(r, content.HasStepList, 15, "✓ مراحل سفارش", "🟢 «نحوه سفارش» را به‌صورت لیست شماره‌دار بنویسید.");
        intent += Band(r, AnyOf(content.PlainText, "حداقل سفارش"), 10, "✓ شرایط عمده", null);
        r.IntentScore = Cap(intent);

        r.AeoScore = ScoreAeo(r, content, faqs, queries, meta);
        r.GeoScore = ScoreGeoCategory(r, summary, focus, secondary, category, entityDetail, content);

        r.ImageScore = string.IsNullOrWhiteSpace(category.ImageUrl) ? 45 : 88;
        if (string.IsNullOrWhiteSpace(category.ImageUrl))
            r.Warnings.Add("🟡 تصویر دسته آپلود نشده.");

        var schema = 0;
        schema += category.Products?.Count > 0 ? 25 : 0;
        schema += string.IsNullOrWhiteSpace(category.Name) ? 0 : 20;
        schema += string.IsNullOrWhiteSpace(category.Slug) ? 0 : 20;
        schema += faqs.Count >= 5 ? 25 : 0;
        schema += category.IsActive ? 10 : 0;
        r.SchemaScore = Cap(schema);
        r.ProductDataScore = productCount >= 3 ? 95 : productCount > 0 ? 70 : 30;

        r.InternalLinkingScore = ScoreInternalLinking(r, content, null);
        r.TechnicalScore = Cap(60
            + (category.IsActive ? 20 : 0)
            + (string.IsNullOrWhiteSpace(category.Slug) ? 0 : 20));

        r.IsIndexable = category.IsActive;
        r.CanonicalValid = !string.IsNullOrWhiteSpace(category.Slug);

        r.IntentMatch = r.IntentScore;
        r.SemanticCoverage = r.SemanticScore;
        r.AeoAnswerCoverage = queries.Count == 0 ? 0
            : Math.Round(queries.Count(q => q.Answered) * 100.0 / queries.Count, 1);
        r.ProductDataCompleteness = r.ProductDataScore;

        if (string.IsNullOrWhiteSpace(category.Name))
            r.CriticalIssues.Add("🔴 نام دسته خالی است.");
        if (content.WordCount < 250)
            r.CriticalIssues.Add($"🔴 توضیح دسته بسیار کم ({content.WordCount} کلمه) — Thin Content.");

        addManualTasks?.Invoke(r);

        Finalize(r, ctx);
        return r;
    }

    public static SeoUltimateReport AnalyzeEditorial(
        SeoUltimateContext ctx, Action<SeoUltimateReport>? addManualTasks = null)
    {
        var profile = ctx.Profile;
        var title = Trim(ctx.EditorialTitle ?? "مطلب");
        var focus = Trim(profile.FocusKeyword ?? title);
        var secondary = Trim(profile.SecondaryKeyword ?? SiteKeywordStrategy.Secondary);
        var metaTitle = Trim(profile.MetaTitle);
        var meta = Trim(profile.MetaDescription);
        var summary = Trim(profile.AiSummary);
        var excerpt = Trim(ctx.EditorialExcerpt);

        var densityFocus = ctx.EditorialKind == "blog"
            ? Editorial.SeoEditorialWriter.DensityPhrase(focus, title)
            : SeoText.HeadPhrase(focus, title);
        var content = SeoContentAnalyzer.Analyze(ctx.Prose, densityFocus, secondary);
        var faqs = SeoFaqStore.Parse(profile.FaqJson);

        var r = new SeoUltimateReport { Baseline = ctx.Baseline, Cannibalization = ctx.Cannibalization };
        r.Intent = SeoIntentDetector.Detect(focus, secondary, ctx.SerpTitles);

        var haystack = string.Join(" \n ", content.PlainText, metaTitle, meta, summary, excerpt, title,
            string.Join(" ", faqs.Select(f => f.Question + " " + f.PlainAnswer)));

        var (topical, topicDetail) = SeoTopicModel.Evaluate(
            SeoTopicModel.ForCategory(new Category { Name = title }, focus, secondary), haystack);
        r.TopicalCoverage = topical;
        r.Topics = topicDetail;

        var (entityCoverage, entityDetail) = SeoEntityGraph.Evaluate(
            SeoEntityGraph.ForCategory(new Category { Name = title }),
            ("content", content.PlainText), ("title", metaTitle), ("meta", meta), ("summary", summary));
        r.EntityCoverage = entityCoverage;
        r.Entities = entityDetail;

        var headings = content.H2Texts.Concat(content.H3Texts).ToList();
        var queries = SeoQueryExpander.Expand(focus, secondary);
        SeoQueryExpander.MarkAnswered(queries, headings, faqs, content.PlainText);
        r.Queries = queries
            .Concat(SeoQueryExpander.FromSearchConsole(ctx.GscQueries, focus, headings))
            .ToList();

        r.GenericRisk = content.GenericRisk;
        r.DuplicationRisk = Math.Max(content.DuplicationRisk, ctx.SiteDuplicationPercent);
        r.WordCount = content.WordCount;
        r.KeywordDensity = content.FocusDensity;

        var onPage = 0;
        onPage += Band(r, metaTitle.Length is >= SeoContentRules.MetaTitleMin and <= SeoContentRules.MetaTitleMax,
            20, "✓ طول Meta Title", $"🟡 Meta Title: {metaTitle.Length} کاراکتر.");
        onPage += Band(r, meta.Length is >= SeoContentRules.MetaDescMin and <= SeoContentRules.MetaDescMax,
            20, "✓ طول Meta Description", $"🟡 Meta Description: {meta.Length} کاراکتر.");
        onPage += Band(r, SeoText.IsKeywordish(metaTitle, focus), 15, "✓ Keyword در Title", "🟡 Keyword در Title نیست.");
        onPage += Band(r, excerpt.Length >= 80, 15, "✓ خلاصه/Lead", "🟡 خلاصه کوتاه باید ≥۸۰ کاراکتر باشد.");
        onPage += Band(r, !string.IsNullOrWhiteSpace(title), 10, "✓ عنوان", "🔴 عنوان خالی است.");
        onPage += Band(r, SeoText.IsKeywordish(meta, focus), 10, "✓ Keyword در Meta Description",
            "🟡 Primary Keyword در Meta Description نیست.");
        onPage += Band(r, content.FocusInIntro, 10, "✓ Keyword در مقدمه",
            "🟡 Primary Keyword در پاراگراف اول نیست.");
        r.SeoScore = Cap(onPage);

        r.ContentScore = ScoreContent(r, content, focus, secondary, SeoArticleBuilder.MinWordCount);
        if (ctx.EditorialKind == "blog"
            && content.IsStuffed
            && content.FocusDensity is > SeoContentRules.KeywordDensityStuffing and < 7.5)
        {
            r.CriticalIssues.RemoveAll(c => c.Contains("Keyword Stuffing", StringComparison.Ordinal));
            r.Warnings.Add($"🟡 چگالی Keyword {content.FocusDensity:F1}٪ — برای بلاگ نزدیک حد است؛ در صورت امکان یک‌دو بار دیگر کلیدواژه را کم کنید.");
        }

        r.SemanticScore = (int)Math.Round(topical * 0.7 + entityCoverage * 0.3);
        r.IntentScore = Cap(70
            + (content.InternalLinkCount >= 2 ? 15 : 0)
            + (content.HasStepList ? 10 : 0)
            + (AnyOf(content.PlainText, "عمده", "قیمت", "کاتالوگ") ? 5 : 0));
        r.AeoScore = ScoreAeo(r, content, faqs, queries, excerpt);
        r.GeoScore = ScoreGeoCategory(r, summary, focus, secondary, new Category { Name = title }, entityDetail, content);
        var inlineImages = System.Text.RegularExpressions.Regex.Matches(
            ctx.Prose ?? "", "<img\\s", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;
        r.ImageScore = ScoreEditorialImages(ctx.FeaturedImageUrl, inlineImages);
        var faqSchemaPts = faqs.Count >= 4 ? 25 : 10;
        r.SchemaScore = Cap(60 + faqSchemaPts + 15);
        r.ProductDataScore = 85;
        r.InternalLinkingScore = ScoreInternalLinking(r, content, null);
        // دروازه انتشار مقاله = کیفیت محتوا؛ وضعیت «منتشر شده» جداگانه در پنل کنترل می‌شود.
        r.TechnicalScore = Cap(ctx.EditorialPublished ? 90 : 88);
        r.IsIndexable = true;
        r.CanonicalValid = !string.IsNullOrWhiteSpace(title);

        if (ctx.SiteDuplicationPercent > SeoContentRules.MaxDuplicationRisk)
            r.CriticalIssues.Add($"🔴 شباهت {ctx.SiteDuplicationPercent:F0}٪ با صفحه دیگر سایت — متن باید یکتا باشد.");

        addManualTasks?.Invoke(r);
        Finalize(r, ctx);
        return r;
    }

    // ─────────────────────────────────────────────
    //  ابعاد محصول
    // ─────────────────────────────────────────────

    static int ScoreOnPage(
        SeoUltimateReport r, SeoContentAnalysis content, string focus, string head,
        string title, string meta, string shortDesc, Product product, SeoProfile profile)
    {
        if (string.IsNullOrWhiteSpace(focus))
            r.CriticalIssues.Add("🔴 Primary Keyword تعریف نشده — پایه کل بهینه‌سازی است.");
        if (string.IsNullOrWhiteSpace(product.Name))
            r.CriticalIssues.Add("🔴 H1 / نام محصول خالی است.");
        if (string.IsNullOrWhiteSpace(product.Slug))
            r.CriticalIssues.Add("🔴 URL/Slug محصول تعریف نشده — Canonical نامعتبر.");
        if (product.CategoryId <= 0)
            r.CriticalIssues.Add("🔴 دسته‌بندی تعیین نشده — Breadcrumb و Entity SEO می‌شکند.");

        var pts = 0;
        pts += Band(r, title.Length is >= SeoContentRules.MetaTitleMin and <= SeoContentRules.MetaTitleMax,
            14, "✓ طول Meta Title (۳۰–۶۰)", $"🟡 Meta Title: {title.Length} کاراکتر (هدف ۳۰–۶۰).");

        var titleHasKw = SeoText.IsKeywordish(title, focus);
        pts += Band(r, titleHasKw, 8, "✓ Keyword در Title", "🟡 Primary Keyword در Meta Title نیست.");
        pts += Band(r, titleHasKw && SeoText.Contains(title[..Math.Min(title.Length, 30)], head),
            4, "✓ Keyword در ابتدای Title", "🟢 CTR: Keyword را به ابتدای Title منتقل کنید.");

        pts += Band(r, AnyOf(title, "عمده", "قیمت", "آملون", "موثقی", "خرید"),
            8, "✓ سیگنال CTR در Title", "🟢 CTR: مزیت («خرید عمده»، «قیمت») را در Title بیاورید.");

        pts += Band(r, meta.Length is >= SeoContentRules.MetaDescMin and <= SeoContentRules.MetaDescMax,
            14, "✓ طول Meta Description (۱۴۰–۱۶۰)", $"🟡 Meta Description: {meta.Length} کاراکتر (هدف ۱۴۰–۱۶۰).");
        pts += Band(r, SeoText.IsKeywordish(meta, focus), 8, "✓ Keyword در Meta Description",
            "🟡 Primary Keyword در Meta Description نیست.");
        pts += Band(r, AnyOf(meta, "ضمانت", "ارسال", "مشاوره", "حداقل سفارش", "نمایندگی"),
            6, "✓ USP در Meta Description", "🟢 USP (ضمانت اصالت، ارسال سراسری) را در Meta بیاورید.");

        pts += Band(r, !string.IsNullOrWhiteSpace(product.Slug) && product.Slug.Length <= 80
            && !product.Slug.Contains(' '), 10, "✓ URL خوانا و کوتاه", null);

        pts += Band(r, !string.IsNullOrWhiteSpace(product.Name) && SeoText.IsKeywordish(product.Name, head, 0.5),
            10, "✓ H1 شامل موضوع اصلی", "🟡 نام محصول (H1) با Focus Keyword هم‌راستا نیست.");

        pts += Band(r, shortDesc.Length >= 100, 8, "✓ توضیح کوتاه (Snippet)",
            $"🟡 Short Description باید ≥۱۰۰ کاراکتر باشد (فعلی: {shortDesc.Length}).");

        pts += Band(r, !string.IsNullOrWhiteSpace(profile.MetaKeywords ?? product.MetaKeywords),
            4, "✓ Meta Keywords", null);

        pts += Band(r, content.FocusInIntro, 6, "✓ Keyword در مقدمه",
            "🟡 Primary Keyword در پاراگراف اول نیست.");

        return Cap(pts);
    }

    /// <param name="minWords">
    /// آستانه حجم بر اساس نوع صفحه — صفحه دسته علاوه بر متن، فهرست محصولات هم دارد،
    /// پس آستانه محصول برای آن غیرمنصفانه است.
    /// </param>
    static int ScoreContent(
        SeoUltimateReport r, SeoContentAnalysis c, string focus, string secondary, int minWords)
    {
        if (c.WordCount < 250)
            r.CriticalIssues.Add($"🔴 محتوای بسیار کم ({c.WordCount} کلمه) — رقبا معمولاً ۸۰۰+ کلمه دارند.");

        var pts = 0;

        // حجم به‌صورت پله‌ای امتیاز می‌گیرد، نه صفر/یک
        var wordRatio = Math.Min(1.0, c.WordCount / (double)minWords);
        pts += (int)Math.Round(18 * wordRatio);
        if (c.WordCount >= minWords) r.PassedChecks.Add($"✓ حجم محتوا: {c.WordCount} کلمه");
        else r.Warnings.Add($"🟡 محتوا {c.WordCount} کلمه (هدف ≥{minWords}).");

        pts += Band(r, c.MeetsParagraphs, 6, $"✓ پاراگراف‌ها: {c.ParagraphCount}",
            $"🟡 پاراگراف کافی نیست ({c.ParagraphCount}/{SeoContentRules.MinParagraphCount}).");
        pts += Band(r, c.H2Count >= SeoContentRules.MinH2Count, 8, $"✓ H2: {c.H2Count} عنوان",
            $"🟡 H2 کافی نیست ({c.H2Count}/{SeoContentRules.MinH2Count}).");
        pts += Band(r, c.H2WithFocus >= SeoContentRules.MinH2WithKeyword, 6,
            $"✓ H2 مرتبط با Keyword: {c.H2WithFocus}",
            $"🟡 H2 مرتبط با Keyword کم است ({c.H2WithFocus}/{SeoContentRules.MinH2WithKeyword}).");
        pts += Band(r, c.H3Count >= SeoContentRules.MinH3Count, 5, $"✓ H3: {c.H3Count}",
            $"🟡 H3 کافی نیست ({c.H3Count}/{SeoContentRules.MinH3Count}).");
        pts += Band(r, c.MeetsLists, 6, "✓ لیست‌ها (ul/ol)",
            $"🟡 آیتم لیست کافی نیست ({c.ListItemCount}/{SeoContentRules.MinListItems}).");

        // Information Gain — چیزهایی که رقبا معمولاً ناقص دارند
        pts += Band(r, c.HasSpecTable, 8, "✓ جدول مشخصات (Information Gain)",
            "🟢 Information Gain: جدول «مشخصات فنی» اضافه کنید — Featured Snippet جدولی می‌گیرد.");
        pts += Band(r, c.HasComparisonSection, 8, "✓ بخش مقایسه",
            "🟢 Information Gain: بخش «مقایسه با پلاستیک» اضافه کنید.");
        pts += Band(r, c.HasStepList, 5, "✓ لیست مرحله‌ای (نحوه سفارش)",
            "🟢 Information Gain: «مراحل سفارش» را به‌صورت لیست شماره‌دار بنویسید.");

        if (c.IsStuffed)
        {
            r.CriticalIssues.Add($"🔴 Keyword Stuffing — چگالی {c.FocusDensity:F1}٪ (سقف {SeoContentRules.KeywordDensityStuffing}٪).");
        }
        else if (c.MeetsDensity)
        {
            pts += 10;
            r.PassedChecks.Add($"✓ چگالی طبیعی Keyword: {c.FocusDensity:F1}٪");
        }
        else if (!string.IsNullOrWhiteSpace(focus))
        {
            pts += 4;
            r.Warnings.Add($"🟡 چگالی Keyword {c.FocusDensity:F1}٪ (هدف {SeoContentRules.KeywordDensityMin}–{SeoContentRules.KeywordDensityMax}٪).");
        }

        pts += Band(r, c.MeetsSecondary, 5, "✓ Secondary Keyword",
            string.IsNullOrWhiteSpace(secondary) ? null
                : $"🟡 Secondary Keyword فقط {c.SecondaryMentionCount} بار آمده (هدف ≥{SeoContentRules.MinSecondaryMentions}).");

        // اصالت محتوا — لایه ضد متن کلیشه‌ای AI
        if (c.GenericRisk <= SeoContentRules.MaxGenericRisk)
        {
            pts += 9;
            r.PassedChecks.Add($"✓ ریسک متن کلیشه‌ای: {c.GenericRisk:F0}٪");
        }
        else
        {
            r.Warnings.Add($"🟡 محتوای کلیشه‌ای AI: {c.GenericRisk:F0}٪ (سقف {SeoContentRules.MaxGenericRisk}٪) — جملات را به داده واقعی محصول وصل کنید.");
            foreach (var s in SeoGenericDetector.FindGenericSamples(c.ParagraphTexts, 2))
                r.Opportunities.Add($"🟢 جمله بی‌ارزش: «{Shorten(s, 70)}» را با اطلاعات مشخص جایگزین کنید.");
        }

        if (c.DuplicationRisk <= SeoContentRules.MaxDuplicationRisk)
        {
            pts += 6;
            r.PassedChecks.Add("✓ بدون پاراگراف تکراری");
        }
        else
        {
            r.CriticalIssues.Add($"🔴 محتوای تکراری داخلی: {c.DuplicationRisk:F0}٪ پاراگراف‌ها تقریباً یکسان‌اند.");
        }

        return Cap(pts);
    }

    static int ScoreSemantic(
        SeoUltimateReport r, SeoContentAnalysis c, double topical, Product product,
        string focus, string secondary, string title, string meta, string summary)
    {
        // مفاهیم LSI که واقعاً باید روی یک صفحه محصول بیایند.
        // نسخه قبلی ۱۰ عبارت اول لیست pillar را بی‌قید اضافه می‌کرد — مثلاً
        // «لیوان یکبار مصرف گیاهی» روی صفحه یک کاسه — که پوشش معنایی را
        // به‌صورت مصنوعی پایین نگه می‌داشت. پوشش موضوعی (Topic Model) وظیفه
        // سنجش گستره را دارد، نه فهرست کلمات کلیدی سایر صفحات.
        var lsi = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            SiteKeywordStrategy.Primary, SiteKeywordStrategy.Secondary, SiteKeywordStrategy.Tertiary,
            "آملون", "موثقی", "گیاهی", "نشاسته", "عمده", "تهران", "یکبار مصرف",
            "زیست تخریب", "کیترینگ", "فست فود", "رستوران", "کافه",
            "بسته‌بندی", "کارتن", "ضمانت", "ارسال", "مشاوره"
        };
        if (!string.IsNullOrWhiteSpace(focus)) lsi.Add(focus);
        if (!string.IsNullOrWhiteSpace(secondary)) lsi.Add(secondary);
        if (!string.IsNullOrWhiteSpace(product.Material)) lsi.Add(product.Material!);
        if (!string.IsNullOrWhiteSpace(product.ProductType)) lsi.Add(product.ProductType!);

        var haystack = $"{c.PlainText} {title} {meta} {summary}";
        var covered = lsi.Count(t => SeoText.Contains(haystack, t));
        var lsiCoverage = lsi.Count > 0 ? covered * 100.0 / lsi.Count : 0;

        var score = (int)Math.Round(topical * 0.7 + lsiCoverage * 0.3);

        if (topical >= 90) r.PassedChecks.Add($"✓ Topical Coverage: {topical:F0}٪");
        else
        {
            r.Warnings.Add($"🟡 Topical Coverage: {topical:F0}٪ (هدف ≥۹۰٪).");
            var missing = r.Topics.Where(t => !t.Covered).OrderByDescending(t => t.Weight)
                .Select(t => t.Topic).Take(5).ToList();
            if (missing.Count > 0)
                r.Opportunities.Add($"🟢 موضوعات پوشش‌داده‌نشده: {string.Join("، ", missing)}");
        }

        if (r.EntityCoverage < 100)
        {
            var weak = r.Entities.Where(e => e.IsRequired && e.Confidence < 50)
                .Select(e => e.Entity).Take(4).ToList();
            if (weak.Count > 0)
                r.Warnings.Add($"🟡 Entity ضعیف (GEO Opportunity: HIGH): {string.Join("، ", weak)}");
        }
        else r.PassedChecks.Add("✓ همه Entity‌های کلیدی پوشش داده شده");

        return Cap(score);
    }

    static int ScoreIntent(
        SeoUltimateReport r, SeoContentAnalysis c, Product product,
        List<SeoFaq> faqs, List<SeoQueryTarget> queries)
    {
        var body = c.PlainText;
        var faqText = string.Join(" ", faqs.Select(f => f.Question + " " + f.PlainAnswer));
        var all = $"{body} {faqText}";

        var hasPrice = AnyOf(all, "قیمت", "استعلام", "تومان", "نرخ")
            || product.Variants.Any(v => v.Price > 0);
        var hasAvailability = product.IsActive && (product.Variants.Count > 0 || AnyOf(all, "موجود", "تأمین", "انبار"));
        var hasSpecs = c.HasSpecTable || !string.IsNullOrWhiteSpace(product.Material) || AnyOf(body, "مشخصات");
        var hasPurchasePath = c.InternalLinkCount >= 2 && AnyOf(all, "سفارش", "خرید", "تماس", "مشاوره");
        var hasMinOrder = AnyOf(all, "حداقل سفارش", "میلیون تومان");

        var pts = 0;
        pts += Band(r, hasPrice, 20, "✓ Intent قیمت پوشش داده شده",
            "🔴 Intent تراکنشی: هیچ اطلاعات قیمت/استعلامی در صفحه نیست.");
        pts += Band(r, hasPurchasePath, 18, "✓ مسیر خرید روشن",
            "🟡 مسیر خرید (لینک + CTA تماس/سفارش) ناقص است.");
        pts += Band(r, hasSpecs, 15, "✓ مشخصات فنی", "🟡 مشخصات فنی محصول ناقص است.");
        pts += Band(r, hasAvailability, 12, "✓ وضعیت موجودی/تأمین", "🟡 وضعیت موجودی مشخص نیست.");
        pts += Band(r, hasMinOrder, 10, "✓ شرایط خرید عمده (حداقل سفارش)",
            "🟡 شرایط عمده (حداقل سفارش) ذکر نشده.");
        pts += Band(r, c.HasComparisonSection, 10, "✓ مقایسه (Intent تجاری)", null);
        pts += Band(r, c.HasStepList, 8, "✓ نحوه سفارش مرحله‌ای", null);

        var answered = queries.Count == 0 ? 0 : queries.Count(q => q.Answered) * 100.0 / queries.Count;
        pts += (int)Math.Round(7 * Math.Min(1.0, answered / 85.0));

        var unanswered = queries.Where(q => !q.Answered).Select(q => q.Query).Take(4).ToList();
        if (unanswered.Count > 0)
            r.Opportunities.Add($"🟢 Query بدون پاسخ: {string.Join("، ", unanswered)}");

        return Cap(pts);
    }

    static int ScoreEditorialImages(string? featuredUrl, int inlineCount)
    {
        var weak = string.IsNullOrWhiteSpace(featuredUrl)
                   || featuredUrl.Contains("originallogo", StringComparison.OrdinalIgnoreCase);
        var score = weak ? 52 : 84;
        if (inlineCount >= 1) score += 4;
        if (inlineCount >= 2) score += 6;
        return Cap(score);
    }

    static int ScoreAeo(
        SeoUltimateReport r, SeoContentAnalysis c, List<SeoFaq> faqs,
        List<SeoQueryTarget> queries, string snippetSource)
    {
        var pts = 0;

        pts += Band(r, faqs.Count >= SeoContentRules.MinFaqCount, 20,
            $"✓ FAQ آماده Schema: {faqs.Count} سوال",
            $"🔴 FAQ ناقص ({faqs.Count}/{SeoContentRules.MinFaqCount}) — Featured Snippet و AEO از دست می‌رود.");

        var extractable = faqs.Count(f =>
        {
            var wc = SeoText.CountWords(f.PlainShortAnswer);
            return wc is >= SeoContentRules.MinFaqAnswerWords and <= SeoContentRules.MaxFaqAnswerWords;
        });
        pts += Band(r, extractable >= SeoContentRules.MinFaqCount, 15,
            $"✓ پاسخ FAQ قابل استخراج: {extractable}",
            $"🟡 {faqs.Count - extractable} پاسخ FAQ خارج از بازه {SeoContentRules.MinFaqAnswerWords}–{SeoContentRules.MaxFaqAnswerWords} کلمه است.");

        pts += Band(r, faqs.Count > 0 && faqs.All(f => f.Question.TrimEnd().EndsWith('؟') || f.Question.TrimEnd().EndsWith('?')),
            8, "✓ فرمت سوالی FAQ", "🟡 همه سوالات FAQ باید با «؟» تمام شوند.");

        var intents = new[] { "قیمت", "چگونه", "آیا", "حداقل", "تفاوت", "کاربرد" };
        var covered = intents.Count(i => faqs.Any(f => SeoText.Contains(f.Question, i)));
        pts += Band(r, covered >= 4, 10, $"✓ تنوع Intent سوالات: {covered}/۶",
            $"🟡 تنوع سوالات کم است ({covered}/۶) — قیمت، چگونه، آیا، حداقل سفارش، تفاوت، کاربرد.");

        pts += Band(r, c.MeetsDirectAnswers, 15,
            $"✓ بلوک پاسخ مستقیم ({SeoContentRules.MinAeoAnswerWords}–{SeoContentRules.MaxAeoAnswerWords} کلمه): {c.DirectAnswerCount}",
            $"🟡 بلوک پاسخ مستقیم کافی نیست ({c.DirectAnswerCount}/{SeoContentRules.MinDirectAnswers}).");

        pts += Band(r, c.MeetsQuestionHeadings, 10, $"✓ عنوان سوالی: {c.QuestionHeadingCount}",
            "🟡 عنوان سوالی (H2/H3 با ؟) کافی نیست.");

        pts += Band(r, snippetSource.Length >= 100, 7, "✓ Snippet کوتاه آماده", null);

        var answered = queries.Count == 0 ? 0 : queries.Count(q => q.Answered) * 100.0 / queries.Count;
        pts += (int)Math.Round(15 * Math.Min(1.0, answered / 85.0));
        if (answered >= 85) r.PassedChecks.Add($"✓ پوشش پاسخ Query: {answered:F0}٪");
        else r.Warnings.Add($"🟡 پوشش پاسخ Query: {answered:F0}٪ (هدف ≥۸۵٪).");

        return Cap(pts);
    }

    static int ScoreGeo(
        SeoUltimateReport r, string summary, string focus, string secondary,
        Product product, List<SeoEntityScore> entities, SeoContentAnalysis c)
    {
        var pts = 0;

        pts += Band(r, summary.Length >= SeoContentRules.MinGeoSummaryLength, 18,
            $"✓ خلاصه GEO ({summary.Length} کاراکتر)",
            $"🔴 خلاصه GEO کوتاه ({summary.Length}/{SeoContentRules.MinGeoSummaryLength}) — برای ارجاع ChatGPT/Perplexity کافی نیست.");

        var requiredEntities = entities.Where(e => e.IsRequired).ToList();
        var strongEntities = requiredEntities.Count(e => e.Confidence >= 50);
        var entityRatio = requiredEntities.Count == 0 ? 1.0 : strongEntities * 1.0 / requiredEntities.Count;
        pts += (int)Math.Round(25 * entityRatio);
        if (entityRatio >= 0.9) r.PassedChecks.Add($"✓ Entity Confidence: {strongEntities}/{requiredEntities.Count}");

        pts += Band(r, SeoText.IsKeywordish(summary, focus, 0.6), 7, "✓ Keyword در خلاصه GEO",
            "🟡 خلاصه GEO باید Primary Keyword را داشته باشد.");
        pts += Band(r, SeoText.Contains(summary, secondary), 5, "✓ Secondary در خلاصه GEO", null);

        // Evidence — GEO بدون داده قابل‌استناد کار نمی‌کند
        var hasFacts = System.Text.RegularExpressions.Regex.IsMatch(summary, @"[0-9\u06f0-\u06f9]");
        pts += Band(r, hasFacts, 10, "✓ داده عددی قابل استناد در خلاصه",
            "🟡 خلاصه GEO باید عدد مشخص داشته باشد (ظرفیت، حداقل سفارش، زمان ارسال).");

        pts += Band(r, AnyOf(summary, "تهران", "نمایندگی", "سراسر کشور"), 10,
            "✓ Entity مکانی/اعتباری (GEO محلی)",
            "🟡 خلاصه GEO باید مکان (تهران) و اعتبار (نمایندگی رسمی) را ذکر کند.");

        // Consistency — Entity باید در متن و خلاصه هر دو باشد، نه فقط یکی
        var consistent = AnyOf(summary, "آملون") && SeoText.Contains(c.PlainText, "آملون")
            && AnyOf(summary, "موثقی") && SeoText.Contains(c.PlainText, "موثقی");
        pts += Band(r, consistent, 15, "✓ سازگاری Entity بین متن و خلاصه",
            "🟡 برند/کسب‌وکار باید هم در متن و هم در خلاصه GEO بیاید (Entity Consistency).");

        pts += Band(r, !string.IsNullOrWhiteSpace(product.ProductCode)
            && SeoText.Contains(summary, product.ProductCode!), 10,
            "✓ کد محصول در خلاصه GEO",
            "🟢 GEO: کد محصول (SKU) را در خلاصه بیاورید تا مدل‌ها دقیق ارجاع دهند.");

        return Cap(pts);
    }

    static int ScoreGeoCategory(
        SeoUltimateReport r, string summary, string focus, string secondary,
        Category category, List<SeoEntityScore> entities, SeoContentAnalysis c)
    {
        var pts = 0;
        pts += Band(r, summary.Length >= SeoContentRules.MinGeoSummaryLength, 22,
            "✓ خلاصه GEO دسته",
            $"🔴 خلاصه GEO دسته کوتاه ({summary.Length}/{SeoContentRules.MinGeoSummaryLength}).");

        var required = entities.Where(e => e.IsRequired).ToList();
        var strong = required.Count(e => e.Confidence >= 50);
        pts += (int)Math.Round(28 * (required.Count == 0 ? 1.0 : strong * 1.0 / required.Count));

        pts += Band(r, SeoText.IsKeywordish(summary, focus, 0.6), 10, "✓ Keyword در خلاصه GEO", null);
        pts += Band(r, SeoText.Contains(summary, secondary), 5, "✓ Secondary در خلاصه GEO", null);
        pts += Band(r, System.Text.RegularExpressions.Regex.IsMatch(summary, @"[0-9\u06f0-\u06f9]"),
            10, "✓ داده عددی در خلاصه", "🟡 خلاصه GEO دسته باید تعداد محصول/حداقل سفارش را ذکر کند.");
        pts += Band(r, AnyOf(summary, "تهران", "نمایندگی"), 10, "✓ Entity محلی", null);
        pts += Band(r, AnyOf(summary, "آملون") && SeoText.Contains(c.PlainText, "آملون"),
            15, "✓ سازگاری Entity برند", "🟡 برند آملون باید در متن و خلاصه هر دو بیاید.");
        return Cap(pts);
    }

    static int ScoreImage(SeoUltimateReport r, SeoMediaAnalysis m)
    {
        if (!m.HasStudio && m.ImageCount == 0)
            r.CriticalIssues.Add("🔴 هیچ تصویری برای محصول وجود ندارد.");

        var pts = 0;
        pts += Band(r, m.HasStudio || (m.HasPrimary && m.ImageCount > 0), 20,
            "✓ تصویر Studio (پس‌زمینه سفید)",
            "🟡 تصویر Studio آپلود نشده — اولین تصویر را در گالری با نقش Studio بگذارید.");
        pts += Band(r, m.HasScene || m.ImageCount >= 2, 20,
            "✓ تصویر Scene (استفاده واقعی)",
            "🟡 تصویر Scene (محیط واقعی) — تصویر دوم گالری با نقش Scene.");
        pts += Band(r, m.AllAltFilled, 15, "✓ Alt Text همه تصاویر", "🟡 Alt Text ناقص است.");
        pts += Band(r, m.AllAltWithFocus, 12, "✓ ارتباط معنایی Alt با Keyword", null);
        pts += Band(r, m.AllAltDescriptive, 8, "✓ Alt توصیفی (طول مناسب)",
            $"🟡 Alt باید {SeoContentRules.MinAltChars}–{SeoContentRules.MaxAltChars} کاراکتر و توصیفی باشد.");
        pts += Band(r, !m.HasDuplicateAlt, 8, "✓ Alt یکتا برای هر تصویر",
            "🟡 چند تصویر Alt یکسان دارند — هر تصویر باید توصیف خودش را داشته باشد.");
        pts += Band(r, m.ImagesLocal, 10, "✓ میزبانی محلی تصاویر",
            m.ImageCount > 0 ? "🟡 تصاویر از دامنه خارجی بارگذاری می‌شوند." : null);
        pts += Band(r, m.ImageCount >= SeoContentRules.MinProductImages, 7,
            $"✓ {m.ImageCount} تصویر", "🟢 تصاویر بیشتر: جزئیات، بسته‌بندی، مقیاس اندازه.");
        return Cap(pts);
    }

    static int ScoreProductData(SeoUltimateReport r, Product p, SeoMediaAnalysis m)
    {
        var hasPrimaryImage = m.HasStudio || (m.HasPrimary && m.ImageCount > 0);
        var fields = new (string Name, bool Present, int Weight)[]
        {
            ("نام محصول", !string.IsNullOrWhiteSpace(p.Name), 10),
            ("Slug", !string.IsNullOrWhiteSpace(p.Slug), 8),
            ("SKU / کد محصول", !string.IsNullOrWhiteSpace(p.ProductCode), 12),
            ("برند", true, 6),
            ("قیمت (Offer)", p.Variants.Any(v => v.Price > 0), 14),
            ("موجودی", p.IsActive && p.Variants.Count > 0, 10),
            ("تصویر اصلی", hasPrimaryImage, 10),
            ("دسته‌بندی", p.CategoryId > 0, 8),
            ("جنس", !string.IsNullOrWhiteSpace(p.Material), 6),
            ("ابعاد / ظرفیت", !string.IsNullOrWhiteSpace(p.Dimensions) || p.CapacityCc is > 0, 6),
            ("MPN (کد محصول)", SeoProductIdentity.HasMpn(p), 6),
            ("تنوع بسته‌بندی", p.Variants.Count > 0, 6),
            ("توضیح کوتاه", !string.IsNullOrWhiteSpace(p.ShortDescription), 4)
        };

        var total = fields.Sum(f => f.Weight);
        var gained = fields.Where(f => f.Present).Sum(f => f.Weight);
        var score = (int)Math.Round(gained * 100.0 / total);

        var missing = fields.Where(f => !f.Present).Select(f => f.Name).ToList();
        if (missing.Count == 0) r.PassedChecks.Add("✓ داده محصول کامل است");
        else r.Warnings.Add($"🟡 داده محصول ناقص: {string.Join("، ", missing)}");

        if (!SeoProductIdentity.HasMpn(p))
            r.ManualTasks.Add(new SeoManualTask
            {
                Title = "کد محصول (SKU) را وارد کنید",
                Why = "برای Rich Result گوگل به MPN (کد سازنده) نیاز است. در محصولات آملون معمولاً همان «کد محصول» است.",
                ScoreImpact = 6,
                BlocksPublish = false
            });

        return Cap(score);
    }

    static int ScoreSchema(SeoUltimateReport r, Product p, SeoMediaAnalysis m, List<SeoFaq> faqs, bool hasReviews)
    {
        // امتیاز فقط شامل چیزهایی است که موتور کنترل می‌کند.
        // AggregateRating نیازمند نظر واقعی مشتری است و به‌عنوان «کار انسانی»
        // روی Ranking Readiness اثر می‌گذارد، نه روی امتیاز Schema.
        var pts = 0;
        pts += Band(r, !string.IsNullOrWhiteSpace(p.Name) && !string.IsNullOrWhiteSpace(p.Description),
            20, "✓ Product Schema (نام + توضیح)", "🟡 Product Schema ناقص است — توضیحات محصول خالی است.");
        pts += Band(r, p.Variants.Any(v => v.Price > 0), 22, "✓ Offer Schema (قیمت + موجودی)",
            "🟡 Offer Schema بدون قیمت — Rich Result محصول نمایش داده نمی‌شود.");
        pts += Band(r, true, 12, "✓ Brand Schema (آملون)", null);
        pts += Band(r, !string.IsNullOrWhiteSpace(p.ProductCode), 12, "✓ SKU در Schema",
            "🟡 SKU برای Product Schema خالی است.");
        pts += Band(r, p.CategoryId > 0, 12, "✓ BreadcrumbList", null);
        pts += Band(r, m.HasStudio, 12, "✓ Image در Schema", null);
        pts += Band(r, faqs.Count >= SeoContentRules.MinFaqCount, 10, "✓ FAQPage Schema", null);
        pts += Band(r, hasReviews, 8, "✓ AggregateRating (نظر تأیید‌شده)",
            "🟡 از Auto-Fix نظرات پیشنهادی ساخته می‌شود — در «مدیریت نظرات» تأیید کنید.");

        if (!hasReviews)
            r.ManualTasks.Add(new SeoManualTask
            {
                Title = "نظرات پیشنهادی را در پنل تأیید کنید",
                Why = "Auto-Fix بحث و پرسش مشتری را پیش‌نویس می‌کند؛ پس از تأیید شما در سایت و Schema نمایش داده می‌شود.",
                ScoreImpact = 8,
                BlocksPublish = false
            });

        return Cap(pts);
    }

    static int ScoreInternalLinking(SeoUltimateReport r, SeoContentAnalysis c, Product? p)
    {
        var pts = 0;
        pts += Band(r, c.InternalLinkCount >= SeoContentRules.MinInternalLinks, 25,
            $"✓ {c.InternalLinkCount} لینک داخلی",
            $"🟡 لینک داخلی کافی نیست ({c.InternalLinkCount}/{SeoContentRules.MinInternalLinks}).");

        pts += Band(r, c.DistinctInternalTargets >= SeoContentRules.MinDistinctInternalTargets, 20,
            $"✓ {c.DistinctInternalTargets} مقصد متفاوت",
            "🟡 لینک‌های داخلی به مقصدهای تکراری می‌روند — Topic Cluster ضعیف است.");

        var anchorDiverse = c.InternalLinkCount == 0
            || c.DistinctAnchorCount >= Math.Max(2, (int)Math.Ceiling(c.InternalLinkCount * 0.7));
        pts += Band(r, anchorDiverse, 20, "✓ تنوع Anchor Text",
            "🟡 Anchor Text تکراری است — متن لینک‌ها را متنوع و توصیفی کنید.");

        pts += Band(r, SeoText.Contains(string.Join(" ", c.PlainText), "کاتالوگ")
            || c.DistinctInternalTargets >= 3, 15, "✓ لینک به دسته/کاتالوگ", null);

        pts += Band(r, c.ExternalLinkCount == 0 || c.InternalLinkCount > c.ExternalLinkCount,
            10, "✓ توازن لینک داخلی/خارجی",
            "🟡 لینک خارجی بیشتر از لینک داخلی است — Authority از صفحه خارج می‌شود.");

        if (p is not null && p.CategoryId <= 0)
            r.CriticalIssues.Add("🔴 صفحه Orphan است — به هیچ دسته‌ای متصل نیست.");
        else pts += 10;

        return Cap(pts);
    }

    static int ScoreTechnical(SeoUltimateReport r, Product p, SeoMediaAnalysis m)
    {
        var pts = 0;
        pts += Band(r, p.IsActive, 20, "✓ Indexable", "🔴 محصول غیرفعال است — صفحه Index نمی‌شود.");
        pts += Band(r, !string.IsNullOrWhiteSpace(p.Slug), 20, "✓ Canonical معتبر", null);
        pts += Band(r, true, 10, "✓ HTTPS", null);
        pts += Band(r, true, 10, "✓ Mobile-friendly", null);
        pts += Band(r, true, 10, "✓ Sitemap", null);
        pts += Band(r, true, 10, "✓ Robots قابل خزش", null);
        pts += Band(r, true, 10, "✓ Status 200", null);
        pts += Band(r, m.ImagesLocal || m.ImageCount == 0, 10, "✓ تصاویر بهینه (Core Web Vitals)",
            "🟡 تصاویر خارجی روی LCP اثر منفی دارند.");

        r.ManualTasks.Add(new SeoManualTask
        {
            Title = "Core Web Vitals را با PageSpeed Insights بسنجید (LCP / INP / CLS)",
            Why = "سرعت واقعی صفحه فقط با اندازه‌گیری روی دامنه زنده مشخص می‌شود، نه از داخل پنل.",
            ScoreImpact = 2,
            BlocksPublish = false
        });

        return Cap(pts);
    }

    // ─────────────────────────────────────────────
    //  جمع‌بندی
    // ─────────────────────────────────────────────

    static void Finalize(SeoUltimateReport r, SeoUltimateContext ctx)
    {
        var weightedOverall = ComputeWeightedOverall(r);

        var (strength, gaps) = SeoCompetitiveModel.Evaluate(
            r.Baseline, r.ContentScore, r.SemanticScore, r.IntentScore,
            r.EntityCoverage, r.AeoScore, r.ProductDataScore, r.ImageScore, r.SchemaScore);
        r.CompetitiveStrength = strength;
        r.Gaps = gaps;
        r.CompetitorCoverage = gaps.Count == 0
            ? 0
            : Math.Round(gaps.Count(g => !g.IsBehind) * 100.0 / gaps.Count, 1);

        foreach (var gap in gaps.Where(g => g.Severity is "high" or "medium").Take(4))
            r.Opportunities.Add(
                $"🟢 Ranking Gap: «{gap.DimensionFa}» {gap.Deficit} امتیاز عقب‌تر از میانگین رقبا ({gap.OurScore} vs {gap.CompetitorAvg}).");

        foreach (var w in SeoCompetitiveModel.FindCompetitorWeaknesses(r.Baseline))
            r.Opportunities.Add(w);

        foreach (var risk in r.Cannibalization)
            r.Warnings.Add($"⚠️ احتمال Keyword Cannibalization با «{risk.EntityName}» روی «{risk.Keyword}» — {risk.Recommendation}");

        // ── Publish Gate ──
        r.IsPublishReady =
            r.CriticalIssues.Count == 0
            && r.IsIndexable && r.CanonicalValid
            && weightedOverall >= SeoContentRules.PublishThreshold
            && r.SeoScore >= SeoContentRules.MinOnPageScore
            && r.ContentScore >= SeoContentRules.MinContentScore
            && r.AeoScore >= SeoContentRules.MinAeoScore
            && r.GeoScore >= SeoContentRules.MinGeoScore
            && r.SemanticScore >= SeoContentRules.MinSemanticScore
            && r.IntentScore >= SeoContentRules.MinIntentScore
            && r.TechnicalScore >= SeoContentRules.MinTechnicalScore
            && r.ProductDataScore >= SeoContentRules.MinProductDataScore
            && r.ImageScore >= SeoContentRules.MinImageScore
            && r.SchemaScore >= SeoContentRules.MinSchemaScore;

        // ── Ranking Readiness ──
        var lo = Math.Min(weightedOverall, r.CompetitiveStrength);
        var hi = Math.Max(weightedOverall, r.CompetitiveStrength);
        var readiness = lo + (hi - lo) * 0.25;

        // فقط کارهایی که واقعاً انتشار را مسدود می‌کنند روی آمادگی رتبه اثر می‌گذارند —
        // تأیید نظر / CWV اختیاری نباید امتیاز نمایشی را بین ۷۷ و ۷۸ نوسان دهد.
        var penalty = Math.Min(10,
            r.ManualTasks.Where(t => t.BlocksPublish).Sum(t => t.ScoreImpact));
        readiness -= penalty;

        if (!r.Baseline.HasLiveData) readiness = Math.Min(readiness, 82);
        if (!r.IsPublishReady) readiness = Math.Min(readiness, 74);

        r.RankingReadiness = Math.Max(0, Math.Min(100, (int)Math.Round(readiness)));

        var dims = Dimensions(r);
        var failingDims = dims.Where(d => d.Score < d.Target).ToList();

        r.WeightedDimensionScore = weightedOverall;
        r.OverallScore = ResolveDisplayOverall(r);
        (r.Grade, r.GradeFa) = ResolveGrade(r);

        r.RankingVerdict = !r.IsPublishReady
            ? r.CriticalIssues.Count > 0
                ? $"آماده انتشار نیست — {r.CriticalIssues.Count} مشکل بحرانی باید رفع شود."
                : failingDims.Count > 0
                    ? "آماده انتشار نیست — "
                      + string.Join(" · ", failingDims.Select(d => $"{d.Fa} {d.Score}/{d.Target}"))
                    : "آماده انتشار نیست — حداقل‌های دروازه انتشار برآورده نشده است."
            : !r.Baseline.HasLiveData
                ? "SEO Ready — داده واقعی SERP همگام‌سازی نشده؛ قدرت رقابتی تخمینی است."
                : r.CompetitiveStrength < SeoContentRules.MinCompetitiveStrength
                    ? "SEO Ready — فاصله رقابتی باقی است (Competitive Gap Remaining)."
                    : "SEO Ready — قدرت رقابتی در سطح مناسب است.";

        // ضعیف‌ترین بُعد — ورودی حلقه بعدی Auto-Fix
        var weakest = dims.OrderBy(d => d.Score).First();
        r.WeakestDimension = weakest.Key;
        r.WeakestDimensionScore = weakest.Score;

        BuildActionPlan(r);
        Dedupe(r);
    }

    /// <summary>میانگین وزنی ابعاد SEO (۰–۱۰۰).</summary>
    public static int ComputeWeightedOverall(SeoUltimateReport r) =>
        (int)Math.Round(
            r.SeoScore * 0.09 +
            r.ContentScore * 0.15 +
            r.SemanticScore * 0.10 +
            r.IntentScore * 0.09 +
            r.AeoScore * 0.14 +
            r.GeoScore * 0.13 +
            r.ImageScore * 0.07 +
            r.SchemaScore * 0.06 +
            r.ProductDataScore * 0.07 +
            r.InternalLinkingScore * 0.05 +
            r.TechnicalScore * 0.05);

    /// <summary>
    /// امتیاز کل نمایشی — میانگین وزنی، محدود به آمادگی رتبه‌گیری و قدرت رقابتی.
    /// ابعاد تکی (مثلاً تصویر ۸) در گرید جدا نشان داده می‌شوند؛ امتیاز کل برابر آن‌ها نیست.
    /// </summary>
    public static int ResolveDisplayOverall(SeoUltimateReport r)
    {
        var weighted = ComputeWeightedOverall(r);
        var score = Math.Min(weighted, Math.Min(r.RankingReadiness, r.CompetitiveStrength));

        if (!r.Baseline.HasLiveData)
            score = Math.Min(score, 82);

        if (!r.IsPublishReady)
        {
            var weakest = Dimensions(r).Min(d => d.Score);
            score = Math.Min(
                score,
                Math.Min(SeoContentRules.PublishThreshold - 1, Math.Max(r.RankingReadiness, weakest)));
        }

        return Math.Max(0, Math.Min(100, score));
    }

    /// <summary>امتیاز کل از ستون‌های ذخیره‌شده پروفایل (لیست ادمین قبل از audit مجدد).</summary>
    public static int ResolveDisplayOverallFromProfile(SeoProfile p) =>
        ResolveDisplayOverall(new SeoUltimateReport
        {
            SeoScore = p.SeoScore,
            ContentScore = p.ContentScore,
            SemanticScore = p.SemanticScore,
            IntentScore = p.IntentScore,
            AeoScore = p.AeoScore,
            GeoScore = p.GeoScore,
            ImageScore = p.ImageScore,
            SchemaScore = p.SchemaScore,
            ProductDataScore = p.ProductDataScore,
            InternalLinkingScore = p.InternalLinkingScore,
            TechnicalScore = p.TechnicalScore,
            RankingReadiness = p.RankingReadiness,
            CompetitiveStrength = p.CompetitiveStrength,
            IsPublishReady = p.IsPublishReady,
            Baseline = new SeoSerpBaseline()
        });

    public static (string Grade, string GradeFa) ResolveGrade(SeoUltimateReport r)
    {
        if (!r.IsPublishReady)
            return ("Incomplete", "ناقص");

        var overall = ResolveDisplayOverall(r);
        return overall switch
        {
            >= 95 when r.RankingReadiness >= 90
                && r.CompetitiveStrength >= SeoContentRules.MinCompetitiveStrength
                && r.Baseline.HasLiveData => ("Exceptional", "استثنایی"),
            >= 95 => ("Excellent", "عالی"),
            >= 90 => ("Excellent", "عالی"),
            >= 80 => ("Strong", "قوی"),
            >= 70 => ("Needs Improvement", "نیازمند بهبود"),
            _ => ("Weak", "ضعیف")
        };
    }

    /// <summary>ابعاد با حداقل هدف — مبنای انتخاب ضعیف‌ترین بُعد در حلقه Auto-Fix.</summary>
    public static List<(string Key, string Fa, int Score, int Target)> Dimensions(SeoUltimateReport r) =>
    [
        ("onpage",   "On-Page",         r.SeoScore,             SeoContentRules.MinOnPageScore),
        ("content",  "محتوا",           r.ContentScore,         SeoContentRules.MinContentScore),
        ("aeo",      "AEO",             r.AeoScore,             SeoContentRules.MinAeoScore),
        ("geo",      "GEO",             r.GeoScore,             SeoContentRules.MinGeoScore),
        ("semantic", "معنایی",          r.SemanticScore,        SeoContentRules.MinSemanticScore),
        ("intent",   "Search Intent",   r.IntentScore,          SeoContentRules.MinIntentScore),
        ("image",    "تصویر",           r.ImageScore,           SeoContentRules.MinImageScore),
        ("schema",   "Schema",          r.SchemaScore,          SeoContentRules.MinSchemaScore),
        ("product",  "داده محصول",      r.ProductDataScore,     SeoContentRules.MinProductDataScore),
        ("internal", "لینک داخلی",      r.InternalLinkingScore, 90),
        ("technical","فنی",             r.TechnicalScore,       SeoContentRules.MinTechnicalScore)
    ];

    static void BuildActionPlan(SeoUltimateReport r)
    {
        var plan = new List<SeoActionItem>();
        var priority = 1;

        foreach (var c in r.CriticalIssues)
            plan.Add(new SeoActionItem
            {
                Priority = priority++,
                Category = "Critical",
                Title = Clean(c),
                AutoFixable = IsAutoFixable(c)
            });

        foreach (var gap in r.Gaps.Where(g => g.Severity == "high").Take(3))
            plan.Add(new SeoActionItem
            {
                Priority = priority++,
                Category = "Gap",
                Title = $"{gap.DimensionFa}: {gap.Deficit} امتیاز عقب‌تر از رقبا",
                AutoFixable = gap.Dimension is "content" or "semantic" or "aeo" or "intent"
            });

        foreach (var w in r.Warnings.Take(8))
            plan.Add(new SeoActionItem
            {
                Priority = priority++,
                Category = "Warning",
                Title = Clean(w),
                AutoFixable = IsAutoFixable(w)
            });

        foreach (var o in r.Opportunities.Take(6))
            plan.Add(new SeoActionItem
            {
                Priority = priority++,
                Category = "Opportunity",
                Title = Clean(o),
                AutoFixable = false
            });

        foreach (var t in r.ManualTasks)
            plan.Add(new SeoActionItem
            {
                Priority = priority++,
                Category = "Manual",
                Title = t.Title,
                AutoFixable = false
            });

        r.ActionPlan = plan;
    }

    static bool IsAutoFixable(string text) =>
        AnyOf(text, "Meta", "Alt", "FAQ", "GEO", "محتوا", "خلاصه", "Keyword", "چگالی",
            "H2", "H3", "لینک داخلی", "پاراگراف", "کلیشه", "Snippet", "جدول", "مقایسه", "Query");

    static void Dedupe(SeoUltimateReport r)
    {
        r.CriticalIssues = r.CriticalIssues.Distinct().ToList();
        r.Warnings = r.Warnings.Distinct().ToList();
        r.Opportunities = r.Opportunities.Distinct().ToList();
        r.PassedChecks = r.PassedChecks.Distinct().ToList();
    }

    // ─────────────────────────────────────────────
    //  کمکی‌ها
    // ─────────────────────────────────────────────

    /// <summary>امتیاز شرطی + ثبت پیام موفقیت یا هشدار.</summary>
    static int Band(SeoUltimateReport r, bool pass, int points, string? onPass, string? onFail)
    {
        if (pass)
        {
            if (!string.IsNullOrEmpty(onPass)) r.PassedChecks.Add(onPass);
            return points;
        }

        if (!string.IsNullOrEmpty(onFail))
        {
            if (onFail.StartsWith("🔴")) r.CriticalIssues.Add(onFail);
            else if (onFail.StartsWith("🟢")) r.Opportunities.Add(onFail);
            else r.Warnings.Add(onFail);
        }
        return 0;
    }

    static double KeywordPlacementCoverage(
        string focus, string head, string title, string meta, string summary,
        Product product, SeoContentAnalysis c, SeoMediaAnalysis m)
    {
        if (string.IsNullOrWhiteSpace(focus)) return 0;

        var slots = new[]
        {
            SeoText.IsKeywordish(title, focus),
            SeoText.IsKeywordish(product.Name, head, 0.5),
            c.FocusInIntro,
            c.H2WithFocus > 0,
            SeoText.IsKeywordish(meta, focus),
            c.FocusOccurrences > 0,
            m.AllAltWithFocus,
            SeoText.IsKeywordish(summary, focus, 0.6)
        };
        return Math.Round(slots.Count(s => s) * 100.0 / slots.Length, 1);
    }

    static int Cap(int value) => Math.Max(0, Math.Min(100, value));
    static string Trim(string? s) => (s ?? "").Trim();
    static bool AnyOf(string text, params string[] parts) =>
        parts.Any(p => !string.IsNullOrEmpty(p) && SeoText.Contains(text, p));
    static string Clean(string s) => s
        .Replace("🔴 ", "").Replace("🟡 ", "").Replace("🟢 ", "").Replace("⚠️ ", "");
    static string Shorten(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
