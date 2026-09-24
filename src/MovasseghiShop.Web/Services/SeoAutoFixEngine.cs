using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Services.ContentPipeline;
using MovasseghiShop.Web.Services.Editorial;

namespace MovasseghiShop.Web.Services;

/// <summary>
/// Auto-Fix Ultimate Engine.
/// Keyword → SERP → Intent → Entities → Topics → Gaps → Blueprint → Generate
/// → Validate → Re-optimize → Ranking Readiness
/// </summary>
public interface ISeoAutoFixEngine
{
    Task<SeoUltimateReport> AuditProductAsync(int productId, CancellationToken ct = default);
    Task<SeoUltimateReport> AutoFixProductAsync(
        int productId,
        int maxIterations = 5,
        SeoProductContentMode contentMode = SeoProductContentMode.RegenerateWhenUnlocked,
        CancellationToken ct = default);
    Task<SeoUltimateReport> AuditCategoryAsync(int categoryId, CancellationToken ct = default);
    Task<SeoUltimateReport> AutoFixCategoryAsync(int categoryId, int maxIterations = 5, CancellationToken ct = default);
    Task<SeoUltimateReport> AuditBlogAsync(int blogId, CancellationToken ct = default);
    Task<SeoUltimateReport> AutoFixBlogAsync(int blogId, int maxIterations = 5, bool forceRegenerate = false, CancellationToken ct = default);
    Task<SeoUltimateReport> AuditNewsAsync(int newsId, CancellationToken ct = default);
    Task<SeoUltimateReport> AutoFixNewsAsync(int newsId, CancellationToken ct = default);
}

public class SeoAutoFixEngine(
    ApplicationDbContext db,
    ISeoSerpIntelligence serp,
    IProductReviewService reviews,
    ISeoContentPipelineService contentPipeline,
    IEditorialContentPipelineService editorialPipeline,
    ILogger<SeoAutoFixEngine> logger) : ISeoAutoFixEngine
{
    /// <summary>حداقل بهبود معنادار در هر تکرار — کمتر از این، تغییر متن ارزش ندارد.</summary>
    const double MeaningfulGain = 1.0;

    static readonly JsonSerializerOptions Json = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // ─────────────────────────────────────────────
    //  محصول
    // ─────────────────────────────────────────────

    public async Task<SeoUltimateReport> AuditProductAsync(int productId, CancellationToken ct = default)
    {
        var product = await LoadProductAsync(productId, ct);
        var profile = await GetOrCreateProfileAsync("product", productId, ct);
        var report = await AnalyzeProductAsync(product, profile, ct);
        await PersistAsync(profile, report, ct);
        return report;
    }

    public async Task<SeoUltimateReport> AutoFixProductAsync(
        int productId,
        int maxIterations = 5,
        SeoProductContentMode contentMode = SeoProductContentMode.RegenerateWhenUnlocked,
        CancellationToken ct = default)
    {
        var product = await LoadProductAsync(productId, ct);
        var profile = await GetOrCreateProfileAsync("product", productId, ct);

        if (contentMode == SeoProductContentMode.ForceRegenerate)
        {
            profile.LockProductDescription = false;
            profile.LockProductShortDescription = false;
        }

        var canRegenerateContent = contentMode switch
        {
            SeoProductContentMode.ForceRegenerate => true,
            SeoProductContentMode.RegenerateWhenUnlocked => !profile.LockProductDescription,
            _ => false
        };

        var before = await AnalyzeProductAsync(product, profile, ct);
        var iterations = new List<SeoIterationStep>();

        if (canRegenerateContent)
            await ApplyFoundationAsync(product, profile, ct);
        else
            await ApplyProductLightMaintenanceAsync(product, profile, ct);

        await db.SaveChangesAsync(ct);

        var report = await AnalyzeProductAsync(product, profile, ct);
        iterations.Add(new SeoIterationStep
        {
            Iteration = 0,
            TargetDimension = "foundation",
            Action = canRegenerateContent
                ? "بازسازی پایه: Keyword، Meta، Snippet، Alt تصاویر، FAQ، خلاصه GEO و محتوا"
                : "نگهداری امن: Meta، Alt، MPN، FAQ و تصویر Scene — بدون بازنویسی توضیحات قفل‌شده",
            ScoreBefore = before.OverallScore,
            ScoreAfter = report.OverallScore
        });

        // ── حلقه: ضعیف‌ترین بُعد را پیدا کن، فقط همان را درست کن ──
        var appliedActions = new HashSet<string>();

        if (canRegenerateContent)
            report = await ReconcileSiteDuplicationAsync(product, profile, report, iterations, ct);

        for (var i = 1; i <= maxIterations; i++)
        {
            var weakest = SeoUltimateAnalyzer.Dimensions(report)
                .Where(d => d.Score < d.Target)
                .Where(d => CanAutoFix(d.Key) && !appliedActions.Contains(d.Key))
                .OrderByDescending(d => d.Target - d.Score)
                .FirstOrDefault();

            if (weakest.Key is null)
            {
                iterations.Add(new SeoIterationStep
                {
                    Iteration = i,
                    TargetDimension = report.WeakestDimension,
                    Action = "بهینه‌سازی خودکار بیشتری ممکن نیست",
                    ScoreBefore = report.OverallScore,
                    ScoreAfter = report.OverallScore,
                    Stopped = true,
                    StopReason = HasRemainingGate(report)
                        ? "ابعاد باقی‌مانده نیازمند اقدام انسانی است (تصویر، تأیید نظرات، سرعت)."
                        : "همه ابعاد قابل‌اصلاح به حداقل هدف رسیدند."
                });
                break;
            }

            var scoreBefore = report.OverallScore;
            var action = ApplyDimensionFix(weakest.Key, product, profile, report);
            appliedActions.Add(weakest.Key);
            await db.SaveChangesAsync(ct);

            report = await AnalyzeProductAsync(product, profile, ct);
            if (report.CriticalIssues.Any(i => i.Contains("شباهت", StringComparison.Ordinal)))
                report = await ReconcileSiteDuplicationAsync(product, profile, report, iterations, ct);

            var step = new SeoIterationStep
            {
                Iteration = i,
                TargetDimension = weakest.Fa,
                Action = action,
                ScoreBefore = scoreBefore,
                ScoreAfter = report.OverallScore
            };

            // ── محافظ بازده نزولی ──
            if (step.Delta < MeaningfulGain && i >= 2)
            {
                step.Stopped = true;
                step.StopReason = $"بازده نزولی: تکرار {i} فقط {step.Delta} امتیاز آورد — تغییر بیشتر متن ارزش مورد انتظار پایینی دارد.";
                iterations.Add(step);
                break;
            }

            iterations.Add(step);

            if (report.IsPublishReady && report.OverallScore >= SeoContentRules.PublishThreshold)
            {
                iterations.Add(new SeoIterationStep
                {
                    Iteration = i,
                    TargetDimension = "—",
                    Action = "هدف برآورده شد",
                    ScoreBefore = report.OverallScore,
                    ScoreAfter = report.OverallScore,
                    Stopped = true,
                    StopReason = "تمام حداقل‌های Publish Gate برآورده شد."
                });
                break;
            }
        }

        if (report.CriticalIssues.Any(i => i.Contains("Keyword Stuffing", StringComparison.Ordinal)))
        {
            ApplyKeywordDensityGuard(product, profile);
            await db.SaveChangesAsync(ct);
            report = await AnalyzeProductAsync(product, profile, ct);
            iterations.Add(new SeoIterationStep
            {
                Iteration = iterations.Count,
                TargetDimension = "content",
                Action = "کاهش چگالی عبارت کلیدی (حذف تکرار literal بیش از سقف)",
                ScoreBefore = iterations.LastOrDefault()?.ScoreAfter ?? before.OverallScore,
                ScoreAfter = report.OverallScore
            });
        }

        report.Iterations = iterations;
        await PersistAsync(profile, report, ct);

        logger.LogInformation(
            "Auto-Fix Ultimate product {Id}: {Before} → {After} (readiness {Readiness}, {Steps} steps)",
            productId, before.OverallScore, report.OverallScore, report.RankingReadiness, iterations.Count);

        return report;
    }

    /// <summary>همه اصلاح‌های پایه که مستقل از نمره باید انجام شوند.</summary>
    async Task ApplyFoundationAsync(Product product, SeoProfile profile, CancellationToken ct)
    {
        if (profile.LockProductDescription)
        {
            var lockedProse = ProductDescriptionHelper.GetProseForEditor(product.Description);
            product.Description = ProductDescriptionHelper.MergeDescription(lockedProse, product);
            product.UpdatedAt = DateTime.UtcNow;
            return;
        }

        var gsc = await serp.GetSearchConsoleQueriesAsync(ct);
        var (primary, secondary) = SeoKeywordResearch.ResolveProductKeywords(product, gsc);

        if (string.IsNullOrWhiteSpace(product.Mpn) && !string.IsNullOrWhiteSpace(product.ProductCode))
            product.Mpn = product.ProductCode.Trim();

        product.KeywordPrimary = primary;
        product.KeywordSecondary = secondary;
        profile.FocusKeyword = primary;
        profile.SecondaryKeyword = secondary;

        profile.MetaTitle = SeoUltimateWriter.BuildMetaTitle(primary, product.Name);
        profile.MetaDescription = SeoUltimateWriter.BuildMetaDescription(product, primary, secondary);
        profile.MetaKeywords = SiteKeywordStrategy.SuggestProductMetaKeywords(product, primary, secondary);
        if (!profile.LockProductShortDescription)
        {
            profile.AiSummary = SeoUltimateWriter.BuildAiSummary(product, primary, secondary);
            product.ShortDescription = SeoUltimateWriter.BuildShortDescription(product, primary, secondary);
        }

        product.MetaTitle = profile.MetaTitle;
        product.MetaDescription = profile.MetaDescription;
        product.MetaKeywords = profile.MetaKeywords;

        SeoProductDataHeuristics.ApplyDefaults(product);
        SeoMediaAnalyzer.EnsureImageRoles(product);
        SeoMediaAnalyzer.ApplySeoAltTexts(product, primary, secondary);
        await reviews.EnsureSeoSuggestedReviewsAsync(product, primary, ct);

        if (string.IsNullOrWhiteSpace(product.Slug))
            product.Slug = SlugHelper.Generate(product.Name);

        var pipeline = await contentPipeline.GenerateProductProseAsync(
            product, profile, primary, secondary, ct);
        var prose = pipeline.Html;
        if (!string.IsNullOrWhiteSpace(pipeline.FaqJson))
            profile.FaqJson = pipeline.FaqJson;

        if (!pipeline.Quality.Passed)
        {
            logger.LogWarning(
                "Content pipeline quality gate soft-fail product {Id}: {Issues}",
                product.Id, string.Join("; ", pipeline.Quality.Failures));
        }

        prose = await EnsureUniqueProductProseAsync(product, profile, primary, secondary, prose, ct);
        prose = SeoMediaAnalyzer.EnsureSceneFigureInProse(product, primary, prose);

        product.Description = ProductDescriptionHelper.MergeDescription(prose, product);
        ApplyKeywordDensityGuard(product, profile);
        product.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>اصلاح‌های غیرمخرب — برای نگهداری دسته‌جمعی و محصولاتی که توضیحات دستی قفل شده.</summary>
    async Task ApplyProductLightMaintenanceAsync(Product product, SeoProfile profile, CancellationToken ct)
    {
        var gsc = await serp.GetSearchConsoleQueriesAsync(ct);
        var (primary, secondary) = SeoKeywordResearch.ResolveProductKeywords(product, gsc);

        if (string.IsNullOrWhiteSpace(product.Mpn) && !string.IsNullOrWhiteSpace(product.ProductCode))
            product.Mpn = product.ProductCode.Trim();

        product.KeywordPrimary = primary;
        product.KeywordSecondary = secondary;
        profile.FocusKeyword = primary;
        profile.SecondaryKeyword = secondary;

        profile.MetaTitle = SeoUltimateWriter.BuildMetaTitle(primary, product.Name);
        profile.MetaDescription = SeoUltimateWriter.BuildMetaDescription(product, primary, secondary);
        profile.MetaKeywords = SiteKeywordStrategy.SuggestProductMetaKeywords(product, primary, secondary);
        if (!profile.LockProductShortDescription)
        {
            profile.AiSummary = SeoUltimateWriter.BuildAiSummary(product, primary, secondary);
            product.ShortDescription = SeoUltimateWriter.BuildShortDescription(product, primary, secondary);
        }

        product.MetaTitle = profile.MetaTitle;
        product.MetaDescription = profile.MetaDescription;
        product.MetaKeywords = profile.MetaKeywords;

        SeoProductDataHeuristics.ApplyDefaults(product);
        SeoMediaAnalyzer.EnsureImageRoles(product);
        SeoMediaAnalyzer.ApplySeoAltTexts(product, primary, secondary);
        await reviews.EnsureSeoSuggestedReviewsAsync(product, primary, ct);

        if (string.IsNullOrWhiteSpace(product.Slug))
            product.Slug = SlugHelper.Generate(product.Name);

        var prose = ProductDescriptionHelper.GetProseForEditor(product.Description);
        prose = SeoMediaAnalyzer.EnsureSceneFigureInProse(product, primary, prose);
        product.Description = ProductDescriptionHelper.MergeDescription(prose, product);
        ApplyKeywordDensityGuard(product, profile);
        product.UpdatedAt = DateTime.UtcNow;
    }

    void ApplyKeywordDensityGuard(Product product, SeoProfile profile)
    {
        if (profile.LockProductDescription) return;

        var focus = profile.FocusKeyword ?? product.KeywordPrimary ?? product.Name;
        var shortLabel = SeoText.ShortForm(focus, product.Name);
        var prose = ProductDescriptionHelper.GetProseForEditor(product.Description);
        prose = SeoKeywordPlacer.EnforceDensityCap(prose, focus, shortLabel);
        product.Description = ProductDescriptionHelper.MergeDescription(prose, product);
    }

    const int MaxUniquenessAttempts = 6;

    async Task<string> EnsureUniqueProductProseAsync(
        Product product,
        SeoProfile profile,
        string primary,
        string secondary,
        string prose,
        CancellationToken ct)
    {
        var sig = SeoSiteUniqueness.ProductSignature(product);
        var plain = SeoText.StripHtml(prose);
        var (siteDup, conflict) = await SeoSiteUniqueness.CheckAsync(db, "product", product.Id, plain, sig, ct);
        if (!SeoSiteUniqueness.ShouldRewrite(siteDup))
            return prose;

        var modes = new[]
        {
            SeoContentMode.Unique,
            SeoContentMode.Intent,
            SeoContentMode.Semantic,
            SeoContentMode.Aeo,
            SeoContentMode.Unique,
            SeoContentMode.Full
        };

        for (var attempt = 0; attempt < MaxUniquenessAttempts; attempt++)
        {
            var mode = modes[Math.Min(attempt, modes.Length - 1)];
            var topics = new List<string> { conflict ?? "یکتاسازی محتوا" };
            if (attempt > 1 && !string.IsNullOrWhiteSpace(conflict))
                topics.Add($"تفکیک از {conflict}");

            prose = SeoUltimateWriter.BuildContent(
                product, primary, secondary, product.Category?.Slug,
                new SeoContentBuildOptions
                {
                    Mode = mode,
                    DifferentiationAttempt = attempt + 1,
                    MissingTopics = topics,
                    Intent = SeoIntentDetector.Detect(primary, secondary)
                });

            if (!profile.LockProductShortDescription)
                product.ShortDescription = SeoUltimateWriter.BuildShortDescription(product, primary, secondary);
            plain = SeoText.StripHtml(prose);
            (siteDup, conflict) = await SeoSiteUniqueness.CheckAsync(db, "product", product.Id, plain, sig, ct);

            logger.LogInformation(
                "Uniqueness pass product {Id} attempt {Attempt} mode {Mode}: similarity {Dup}% vs {Conflict}",
                product.Id, attempt + 1, mode, siteDup, conflict);

            if (!SeoSiteUniqueness.ShouldRewrite(siteDup))
                break;
        }

        return prose;
    }

    async Task<SeoUltimateReport> ReconcileSiteDuplicationAsync(
        Product product,
        SeoProfile profile,
        SeoUltimateReport report,
        List<SeoIterationStep> iterations,
        CancellationToken ct)
    {
        var focus = profile.FocusKeyword ?? product.KeywordPrimary ?? product.Name;
        var secondary = profile.SecondaryKeyword ?? product.KeywordSecondary ?? SiteKeywordStrategy.Secondary;
        var prose = ProductDescriptionHelper.GetProseForEditor(product.Description);
        var plain = SeoText.StripHtml(prose);
        var sig = SeoSiteUniqueness.ProductSignature(product);
        var (dup, _) = await SeoSiteUniqueness.CheckAsync(db, "product", product.Id, plain, sig, ct);

        if (!SeoSiteUniqueness.ShouldRewrite(dup) || profile.LockProductDescription)
            return report;

        var scoreBefore = report.OverallScore;
        var rewritten = await EnsureUniqueProductProseAsync(product, profile, focus, secondary, prose, ct);
        product.Description = ProductDescriptionHelper.MergeDescription(rewritten, product);
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        report = await AnalyzeProductAsync(product, profile, ct);
        iterations.Add(new SeoIterationStep
        {
            Iteration = 0,
            TargetDimension = "یکتاسازی",
            Action = $"بازنویسی محتوا برای کاهش شباهت سایت (از {dup:F0}٪ به ≤{SeoSiteUniqueness.RewriteThresholdPercent:F0}٪)",
            ScoreBefore = scoreBefore,
            ScoreAfter = report.OverallScore
        });

        return report;
    }

    /// <summary>اصلاح هدفمند یک بُعد — فقط همان بخش بازسازی می‌شود.</summary>
    string ApplyDimensionFix(string dimension, Product product, SeoProfile profile, SeoUltimateReport report)
    {
        var focus = profile.FocusKeyword ?? product.KeywordPrimary ?? product.Name;
        var secondary = profile.SecondaryKeyword ?? product.KeywordSecondary ?? SiteKeywordStrategy.Secondary;
        var buildOptions = BuildContentOptions(dimension, report, focus, secondary);

        switch (dimension)
        {
            case "onpage":
                profile.MetaTitle = SeoUltimateWriter.BuildMetaTitle(focus, product.Name);
                profile.MetaDescription = SeoUltimateWriter.BuildMetaDescription(product, focus, secondary);
                profile.MetaKeywords = SiteKeywordStrategy.SuggestProductMetaKeywords(product, focus, secondary);
                product.MetaTitle = profile.MetaTitle;
                product.MetaDescription = profile.MetaDescription;
                product.MetaKeywords = profile.MetaKeywords;
                if (!profile.LockProductShortDescription)
                    product.ShortDescription = SeoUltimateWriter.BuildShortDescription(product, focus, secondary);
                return "بازسازی Meta Title/Description با سیگنال CTR و USP + Snippet کوتاه";

            case "content":
            case "semantic":
            case "intent":
            case "internal":
                buildOptions = buildOptions with
                {
                    Mode = buildOptions.Mode == SeoContentMode.Full
                        ? SeoContentMode.Unique
                        : buildOptions.Mode,
                    DifferentiationAttempt = Math.Max(1, buildOptions.DifferentiationAttempt)
                };
                if (!profile.LockProductDescription)
                {
                    var prose = SeoUltimateWriter.BuildContent(
                        product, focus, secondary, product.Category?.Slug, buildOptions);
                    product.Description = ProductDescriptionHelper.MergeDescription(prose, product);
                }
                return dimension switch
                {
                    "semantic" => "بازتولید محتوا بر اساس Blueprint برای پوشش موضوعات و Entity‌های جامانده",
                    "intent" => "بازتولید محتوا با تمرکز روی Intent تراکنشی: قیمت، مراحل سفارش، شرایط عمده",
                    "internal" => "بازتولید محتوا با لینک داخلی به کاتالوگ، عمده، قیمت و برند با Anchor متنوع",
                    _ => "بازتولید محتوا با جدول مشخصات، جدول مقایسه، لیست مرحله‌ای و بلوک‌های پاسخ مستقیم"
                };

            case "aeo":
                profile.FaqJson = SeoFaqStore.Serialize(
                    SeoUltimateWriter.BuildFaqs(product, focus, secondary, aeoMode: true));
                if (!profile.LockProductDescription)
                {
                    var aeoProse = SeoUltimateWriter.BuildContent(
                        product, focus, secondary, product.Category?.Slug, buildOptions);
                    product.Description = ProductDescriptionHelper.MergeDescription(aeoProse, product);
                }
                return "بازسازی بلوک‌های پاسخ AEO: ۸ سوال با پاسخ قابل استخراج + عناوین سوالی";

            case "geo":
                if (!profile.LockProductShortDescription)
                    profile.AiSummary = SeoUltimateWriter.BuildAiSummary(product, focus, secondary);
                return "بازنویسی خلاصه GEO با Entity کامل، داده عددی قابل استناد و کد محصول";

            case "image":
                SeoMediaAnalyzer.EnsureImageRoles(product);
                SeoMediaAnalyzer.ApplySeoAltTexts(product, focus, secondary);
                var imgProse = ProductDescriptionHelper.GetProseForEditor(product.Description);
                imgProse = SeoMediaAnalyzer.EnsureSceneFigureInProse(product, focus, imgProse);
                product.Description = ProductDescriptionHelper.MergeDescription(imgProse, product);
                return "Alt یکتا برای گالری + درج تصویر Scene در مقاله با Alt متفاوت";

            default:
                return "بدون تغییر";
        }
    }

    static SeoContentBuildOptions BuildContentOptions(
        string dimension, SeoUltimateReport report, string focus, string secondary)
    {
        var intent = report.Intent ?? SeoIntentDetector.Detect(focus, secondary);
        var missingTopics = report.Topics
            .Where(t => !t.Covered)
            .Select(t => t.Topic)
            .Distinct()
            .ToList();
        var unanswered = report.Queries
            .Where(q => !q.Answered && !string.IsNullOrWhiteSpace(q.Query))
            .Select(q => q.Query)
            .Distinct()
            .Take(6)
            .ToList();

        return dimension switch
        {
            "semantic" => new SeoContentBuildOptions
            {
                Mode = SeoContentMode.Semantic,
                Intent = intent,
                MissingTopics = missingTopics
            },
            "intent" => new SeoContentBuildOptions
            {
                Mode = SeoContentMode.Intent,
                Intent = intent,
                MissingTopics = missingTopics
            },
            "internal" => new SeoContentBuildOptions
            {
                Mode = SeoContentMode.Internal,
                Intent = intent
            },
            "aeo" => new SeoContentBuildOptions
            {
                Mode = SeoContentMode.Aeo,
                Intent = intent,
                UnansweredQueries = unanswered
            },
            _ => new SeoContentBuildOptions
            {
                Mode = SeoContentMode.Full,
                Intent = intent,
                MissingTopics = missingTopics
            }
        };
    }

    /// <summary>ابعادی که موتور می‌تواند خودش اصلاح کند.</summary>
    static bool CanAutoFix(string dimension) =>
        dimension is "onpage" or "content" or "semantic" or "intent" or "aeo" or "geo" or "image" or "internal";

    static bool HasRemainingGate(SeoUltimateReport r) =>
        r.ImageScore < SeoContentRules.MinImageScore
        || r.ProductDataScore < SeoContentRules.MinProductDataScore
        || r.SchemaScore < SeoContentRules.MinSchemaScore
        || r.TechnicalScore < SeoContentRules.MinTechnicalScore;

    /// <summary>«من دیگر چه کاری نمی‌توانم بکنم و شما چه کاری باید بکنید.»</summary>
    static void AddRemainingManualTasks(SeoUltimateReport r, Product product)
    {
        var media = SeoMediaAnalyzer.Analyze(product, product.KeywordPrimary ?? "");

        if (!media.HasScene)
            r.ManualTasks.Insert(0, new SeoManualTask
            {
                Title = "تصویر Scene (استفاده واقعی محصول) آپلود کنید",
                Why = "موتور نمی‌تواند عکس بگیرد؛ تصویر محیط واقعی برای Image Search و نرخ کلیک لازم است.",
                ScoreImpact = 20,
                BlocksPublish = false
            });

        if (!media.HasStudio && media.ImageCount == 0)
            r.ManualTasks.Insert(0, new SeoManualTask
            {
                Title = "تصویر Studio (پس‌زمینه سفید) آپلود کنید",
                Why = "تصویر اصلی محصول برای Product Schema و Google Shopping الزامی است.",
                ScoreImpact = 20,
                BlocksPublish = false
            });
        else if (!media.HasStudio)
            r.ManualTasks.Insert(0, new SeoManualTask
            {
                Title = "نقش Studio را در گالری برای تصویر اصلی تنظیم کنید",
                Why = "حداقل یک تصویر با نقش Studio باید به‌عنوان تصویر اصلی Schema استفاده شود.",
                ScoreImpact = 8,
                BlocksPublish = false
            });

        if (!product.Variants.Any(v => v.Price > 0))
            r.ManualTasks.Insert(0, new SeoManualTask
            {
                Title = "قیمت حداقل یک variant را ثبت کنید",
                Why = "بدون قیمت، Offer Schema ناقص می‌ماند و Rich Result محصول نمایش داده نمی‌شود.",
                ScoreImpact = 14,
                BlocksPublish = r.SchemaScore < SeoContentRules.MinSchemaScore
            });

        if (string.IsNullOrWhiteSpace(product.ProductCode))
            r.ManualTasks.Insert(0, new SeoManualTask
            {
                Title = "کد محصول (SKU) را وارد کنید",
                Why = "SKU در Schema، خلاصه GEO و پیگیری اصالت استفاده می‌شود.",
                ScoreImpact = 12,
                BlocksPublish = r.ProductDataScore < SeoContentRules.MinProductDataScore
            });

        // مشخصات فنی را موتور از خودش نمی‌سازد — حدس زدن جنس یا ابعاد
        // یعنی نوشتن داده نادرست در Schema.
        if (string.IsNullOrWhiteSpace(product.Material))
            r.ManualTasks.Insert(0, new SeoManualTask
            {
                Title = "جنس محصول را وارد کنید (مثلاً گیاهی / PET / کاغذی)",
                Why = "جنس در جدول مشخصات، Schema و Query‌های «جنس چیست» استفاده می‌شود و "
                    + "موتور حق حدس زدن آن را ندارد.",
                ScoreImpact = 6,
                BlocksPublish = r.ProductDataScore < SeoContentRules.MinProductDataScore
            });

        if (string.IsNullOrWhiteSpace(product.Dimensions) && product.CapacityCc is not > 0)
            r.ManualTasks.Insert(0, new SeoManualTask
            {
                Title = "ابعاد یا ظرفیت (سی‌سی) محصول را وارد کنید",
                Why = "ظرفیت و ابعاد پرتکرارترین سوال خریدار عمده است و در Featured Snippet "
                    + "و مقایسه با رقبا نقش مستقیم دارد.",
                ScoreImpact = 6,
                BlocksPublish = r.ProductDataScore < SeoContentRules.MinProductDataScore
            });

        if (!r.Baseline.HasLiveData)
            r.ManualTasks.Add(new SeoManualTask
            {
                Title = "همگام‌سازی داده SERP رقبا را فعال کنید (SerpApi:ApiKey)",
                Why = "بدون داده واقعی Top 10، قدرت رقابتی فقط تخمین است و ادعای رتبه معتبر نیست.",
                ScoreImpact = 6,
                BlocksPublish = false
            });

        r.ManualTasks = r.ManualTasks
            .GroupBy(t => t.Title)
            .Select(g => g.First())
            .ToList();
    }

    // ─────────────────────────────────────────────
    //  دسته
    // ─────────────────────────────────────────────

    public async Task<SeoUltimateReport> AuditCategoryAsync(int categoryId, CancellationToken ct = default)
    {
        var category = await LoadCategoryAsync(categoryId, ct);
        var profile = await GetOrCreateProfileAsync("category", categoryId, ct);
        var report = await AnalyzeCategoryAsync(category, profile, ct);
        await PersistAsync(profile, report, ct);
        return report;
    }

    public async Task<SeoUltimateReport> AutoFixCategoryAsync(
        int categoryId, int maxIterations = 5, CancellationToken ct = default)
    {
        var category = await LoadCategoryAsync(categoryId, ct);
        var profile = await GetOrCreateProfileAsync("category", categoryId, ct);

        var before = await AnalyzeCategoryAsync(category, profile, ct);
        var iterations = new List<SeoIterationStep>();

        await EnsureCategoryCoverImageAsync(category, ct);
        ApplyCategoryFoundation(category, profile);
        await db.SaveChangesAsync(ct);

        var report = await AnalyzeCategoryAsync(category, profile, ct);
        iterations.Add(new SeoIterationStep
        {
            Iteration = 0,
            TargetDimension = "foundation",
            Action = "بازسازی Keyword، Meta، خلاصه GEO، FAQ و توضیحات دسته (با سقف چگالی)",
            ScoreBefore = before.OverallScore,
            ScoreAfter = report.OverallScore
        });

        if (profile.LockCategoryLead)
        {
            report.Iterations = iterations;
            await PersistAsync(profile, report, ct);
            return report;
        }

        var appliedActions = new HashSet<string>();
        for (var i = 1; i <= maxIterations; i++)
        {
            var weakest = SeoUltimateAnalyzer.Dimensions(report)
                .Where(d => d.Score < d.Target)
                .Where(d => CanAutoFix(d.Key) && !appliedActions.Contains(d.Key))
                .OrderByDescending(d => d.Target - d.Score)
                .FirstOrDefault();

            if (weakest.Key is null)
            {
                iterations.Add(new SeoIterationStep
                {
                    Iteration = i,
                    TargetDimension = report.WeakestDimension,
                    Action = "بهینه‌سازی خودکار بیشتری ممکن نیست",
                    ScoreBefore = report.OverallScore,
                    ScoreAfter = report.OverallScore,
                    Stopped = true,
                    StopReason = "ابعاد باقی‌مانده نیازمند اقدام انسانی است (تصویر دسته، محصولات خالی)."
                });
                break;
            }

            var scoreBefore = report.OverallScore;
            var action = ApplyCategoryDimensionFix(weakest.Key, category, profile, report);
            appliedActions.Add(weakest.Key);
            ApplyCategoryKeywordDensityGuard(category, profile);
            await db.SaveChangesAsync(ct);

            report = await AnalyzeCategoryAsync(category, profile, ct);
            var step = new SeoIterationStep
            {
                Iteration = i,
                TargetDimension = weakest.Fa,
                Action = action,
                ScoreBefore = scoreBefore,
                ScoreAfter = report.OverallScore
            };

            if (step.Delta < MeaningfulGain && i >= 2)
            {
                step.Stopped = true;
                step.StopReason =
                    $"بازده نزولی: تکرار {i} فقط {step.Delta} امتیاز آورد — تغییر بیشتر متن ارزش مورد انتظار پایینی دارد.";
                iterations.Add(step);
                break;
            }

            iterations.Add(step);

            if (report.IsPublishReady && report.OverallScore >= SeoContentRules.PublishThreshold)
            {
                iterations.Add(new SeoIterationStep
                {
                    Iteration = i,
                    TargetDimension = "—",
                    Action = "هدف برآورده شد",
                    ScoreBefore = report.OverallScore,
                    ScoreAfter = report.OverallScore,
                    Stopped = true,
                    StopReason = "تمام حداقل‌های Publish Gate برآورده شد."
                });
                break;
            }
        }

        if (report.CriticalIssues.Any(i => i.Contains("Keyword Stuffing", StringComparison.Ordinal)))
        {
            ApplyCategoryKeywordDensityGuard(category, profile);
            await db.SaveChangesAsync(ct);
            report = await AnalyzeCategoryAsync(category, profile, ct);
            iterations.Add(new SeoIterationStep
            {
                Iteration = iterations.Count,
                TargetDimension = "content",
                Action = "کاهش چگالی عبارت کلیدی در توضیحات دسته",
                ScoreBefore = iterations.LastOrDefault()?.ScoreAfter ?? before.OverallScore,
                ScoreAfter = report.OverallScore
            });
        }

        report.Iterations = iterations;
        await PersistAsync(profile, report, ct);

        logger.LogInformation(
            "Auto-Fix Ultimate category {Id}: {Before} → {After} (readiness {Readiness}, {Steps} steps)",
            categoryId, before.OverallScore, report.OverallScore, report.RankingReadiness, iterations.Count);

        return report;
    }

    void ApplyCategoryFoundation(Category category, SeoProfile profile)
    {
        var (primary, secondary) = SeoCategoryBuilder.SuggestKeywords(category);
        profile.FocusKeyword = primary;
        profile.SecondaryKeyword = secondary;
        profile.MetaTitle = SeoCategoryBuilder.BuildMetaTitle(category, primary);
        profile.MetaDescription = SeoCategoryBuilder.BuildMetaDescription(category, primary, secondary);
        profile.MetaKeywords = SiteKeywordStrategy.MetaKeywordsTop(10);

        if (!profile.LockCategoryLead)
        {
            profile.AiSummary = SeoCategoryBuilder.BuildAiSummary(category, primary, secondary);
            category.Description = SeoCategoryBuilder.BuildDescription(category, primary, secondary);
            ApplyCategoryKeywordDensityGuard(category, profile);
        }

        profile.FaqJson = SeoFaqStore.Serialize(SeoCategoryBuilder.BuildFaqs(category, primary, secondary));
        category.MetaTitle = profile.MetaTitle;
        category.MetaDescription = profile.MetaDescription;

        var activeCount = category.Products?.Count(p => p.IsActive) ?? 0;
        if (activeCount == 0 && category.IsActive
            && (category.Name?.Contains("محصولات آملون", StringComparison.Ordinal) == true
                || string.Equals(category.Slug, "amelon-products", StringComparison.OrdinalIgnoreCase)))
        {
            category.IsActive = false;
            logger.LogInformation(
                "Category {Id} «{Name}» غیرفعال شد — دسته خالی و فقط برای واردسازی بود.",
                category.Id, category.Name);
        }
    }

    void ApplyCategoryKeywordDensityGuard(Category category, SeoProfile profile)
    {
        if (profile.LockCategoryLead) return;

        var focus = profile.FocusKeyword ?? category.Name ?? "";
        var shortLabel = SeoText.ShortForm(focus, category.Name);
        var html = category.Description ?? "";
        for (var pass = 0; pass < 4; pass++)
        {
            var next = SeoKeywordPlacer.EnforceDensityCap(html, focus, shortLabel);
            if (next == html) break;
            html = next;
        }

        category.Description = html;
    }

    Task EnsureCategoryCoverImageAsync(Category category, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(category.ImageUrl))
            return Task.CompletedTask;

        var product = category.Products?
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Id)
            .FirstOrDefault(p => p.Images.Any());

        var url = product?.Images
            .OrderBy(i => i.SortOrder)
            .Select(i => i.Url)
            .FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));

        category.ImageUrl = !string.IsNullOrWhiteSpace(url)
            ? url
            : "/images/brand/originallogo.png";

        return Task.CompletedTask;
    }

    string ApplyCategoryDimensionFix(
        string dimension, Category category, SeoProfile profile, SeoUltimateReport report)
    {
        var focus = profile.FocusKeyword ?? category.Name ?? "";
        var secondary = profile.SecondaryKeyword ?? SiteKeywordStrategy.Secondary;

        switch (dimension)
        {
            case "onpage":
                profile.MetaTitle = SeoCategoryBuilder.BuildMetaTitle(category, focus);
                profile.MetaDescription = SeoCategoryBuilder.BuildMetaDescription(category, focus, secondary);
                category.MetaTitle = profile.MetaTitle;
                category.MetaDescription = profile.MetaDescription;
                return "بازسازی Meta Title/Description دسته با سیگنال CTR";

            case "geo":
                profile.AiSummary = SeoCategoryBuilder.BuildAiSummary(category, focus, secondary);
                return "بازنویسی خلاصه GEO دسته با داده عددی و Entity";

            case "aeo":
                profile.FaqJson = SeoFaqStore.Serialize(
                    SeoCategoryBuilder.BuildFaqs(category, focus, secondary));
                var unanswered = report.Queries
                    .Where(q => !q.Answered && !string.IsNullOrWhiteSpace(q.Query))
                    .Select(q => q.Query)
                    .Distinct()
                    .Take(4)
                    .ToList();
                category.Description = SeoCategoryBuilder.BuildDescription(
                    category, focus, secondary, unanswered);
                return "بازسازی FAQ + بلوک‌های پاسخ AEO برای Queryهای بی‌پاسخ";

            case "content":
            case "semantic":
            case "intent":
            case "internal":
                category.Description = SeoCategoryBuilder.BuildDescription(category, focus, secondary);
                return dimension switch
                {
                    "semantic" => "بازسازی محتوای دسته برای پوشش موضوعات و Entity",
                    "intent" => "بازسازی محتوا با تمرکز قیمت، عمده و مراحل سفارش",
                    "internal" => "بازسازی محتوا با لینک داخلی به کاتالوگ، قیمت و عمده",
                    _ => "بازسازی محتوای دسته: حجم، جدول مقایسه و لیست مرحله‌ای"
                };

            default:
                return "بدون تغییر";
        }
    }

    public async Task<SeoUltimateReport> AuditBlogAsync(int blogId, CancellationToken ct = default)
    {
        var post = await db.BlogPosts.FindAsync([blogId], ct)
            ?? throw new InvalidOperationException($"Blog {blogId} not found");
        var profile = await GetOrCreateProfileAsync("blog", blogId, ct);
        var report = await AnalyzeBlogAsync(post, profile, ct);
        await PersistAsync(profile, report, ct);
        return report;
    }

    public async Task<SeoUltimateReport> AutoFixBlogAsync(
        int blogId, int maxIterations = 5, bool forceRegenerate = false, CancellationToken ct = default)
    {
        var post = await db.BlogPosts.FindAsync([blogId], ct)
            ?? throw new InvalidOperationException($"Blog {blogId} not found");
        var profile = await GetOrCreateProfileAsync("blog", blogId, ct);
        if (forceRegenerate)
        {
            profile.LockArticleContent = false;
            profile.LockArticleExcerpt = false;
        }

        var before = await AnalyzeBlogAsync(post, profile, ct);
        var iterations = new List<SeoIterationStep>();

        await ApplyEditorialLightFoundationAsync(post, profile, ct);
        if (forceRegenerate && !profile.LockArticleContent)
        {
            await RebuildBlogBodyForGateAsync(post, profile, ct);
            profile.FaqJson = SeoFaqStore.Serialize(
                SeoEditorialWriter.BuildFaqs(profile.FocusKeyword ?? post.Title, post.Title,
                    profile.SecondaryKeyword ?? SiteKeywordStrategy.Secondary));
        }
        else if (!profile.LockArticleContent)
            await ApplyEditorialFoundationAsync(post, profile, "blog", ct);
        else
            await EnsureBlogImagesAsync(post, profile, ct);
        ApplyEditorialKeywordDensityGuard(post, profile);
        await db.SaveChangesAsync(ct);

        var report = await AnalyzeBlogAsync(post, profile, ct);
        iterations.Add(new SeoIterationStep
        {
            Iteration = 0,
            TargetDimension = "foundation",
            Action = "بازسازی از GSC + SERP Gap + GEO/AEO + تصویر کاتالوگ",
            ScoreBefore = before.OverallScore,
            ScoreAfter = report.OverallScore
        });

        if (!profile.LockArticleContent)
        {
            var applied = new HashSet<string>();
            for (var i = 1; i <= maxIterations; i++)
            {
                var weakest = SeoUltimateAnalyzer.Dimensions(report)
                    .Where(d => d.Score < d.Target)
                    .Where(d => CanAutoFix(d.Key) && !applied.Contains(d.Key))
                    .OrderByDescending(d => d.Target - d.Score)
                    .FirstOrDefault();

                if (weakest.Key is null) break;

                var scoreBefore = report.OverallScore;
                var action = await ApplyEditorialDimensionFixAsync(weakest.Key, post, profile, ct);
                applied.Add(weakest.Key);
                await db.SaveChangesAsync(ct);
                report = await AnalyzeBlogAsync(post, profile, ct);

                var step = new SeoIterationStep
                {
                    Iteration = i,
                    TargetDimension = weakest.Fa,
                    Action = action,
                    ScoreBefore = scoreBefore,
                    ScoreAfter = report.OverallScore
                };
                if (step.Delta < MeaningfulGain && i >= 2)
                {
                    step.Stopped = true;
                    step.StopReason = "بازده نزولی در تکرار مقاله";
                    iterations.Add(step);
                    break;
                }

                iterations.Add(step);
                if (report.IsPublishReady && report.OverallScore >= SeoContentRules.PublishThreshold)
                    break;
            }
        }

        if (!profile.LockArticleContent)
        {
            if (report.CriticalIssues.Any(i => i.Contains("Keyword Stuffing", StringComparison.Ordinal)))
            {
                ApplyEditorialKeywordDensityGuard(post, profile);
                await db.SaveChangesAsync(ct);
                report = await AnalyzeBlogAsync(post, profile, ct);
                iterations.Add(new SeoIterationStep
                {
                    Iteration = iterations.Count,
                    TargetDimension = "محتوا",
                    Action = "کاهش چگالی کلیدواژه مقاله",
                    ScoreBefore = iterations.LastOrDefault()?.ScoreAfter ?? before.OverallScore,
                    ScoreAfter = report.OverallScore
                });
            }

            for (var uniqPass = 0; uniqPass < 3; uniqPass++)
            {
                if (!report.CriticalIssues.Any(i =>
                        i.Contains("تکراری داخلی", StringComparison.Ordinal)
                        || i.Contains("شباهت", StringComparison.Ordinal)))
                    break;

                var focus = profile.FocusKeyword ?? post.Title;
                var secondary = profile.SecondaryKeyword ?? SiteKeywordStrategy.Secondary;
                var angle = InferEditorialAngle(post.Title);
                var products = await ResolveEditorialProductLinksAsync(focus, secondary, ct);
                post.Content = await EnsureUniqueBlogHtmlAsync(
                    post, focus, secondary, angle, products, post.Content ?? "", ct);
                post.Content = SeoEditorialWriter.EnsureAeoSectionInHtml(
                    post.Content, focus, post.Title, secondary, post.Id);
                await EnsureBlogImagesAsync(post, profile, ct);
                ApplyEditorialKeywordDensityGuard(post, profile);
                await db.SaveChangesAsync(ct);
                report = await AnalyzeBlogAsync(post, profile, ct);
                iterations.Add(new SeoIterationStep
                {
                    Iteration = iterations.Count,
                    TargetDimension = "محتوا",
                    Action = $"یکتاسازی متن (گذر {uniqPass + 1}) + AEO + تصویر",
                    ScoreBefore = iterations.LastOrDefault()?.ScoreAfter ?? before.OverallScore,
                    ScoreAfter = report.OverallScore
                });
            }

            if (report.SeoScore < SeoContentRules.MinOnPageScore)
            {
                await ApplyEditorialDimensionFixAsync("onpage", post, profile, ct);
                EditorialKeywordSync.ApplyToPost(post, profile);
                await db.SaveChangesAsync(ct);
                report = await AnalyzeBlogAsync(post, profile, ct);
            }

            ApplyEditorialKeywordDensityGuard(post, profile);
            await db.SaveChangesAsync(ct);
            report = await AnalyzeBlogAsync(post, profile, ct);
        }

        if (!report.IsPublishReady && report.ContentScore < SeoContentRules.MinContentScore && !profile.LockArticleContent)
        {
            post.Content += "<h2>چک‌لیست خرید عمده</h2><ul>"
                + "<li>تست نمونه با غذای اصلی منو</li>"
                + "<li>محاسبه مصرف ماهانه و نوع بسته‌بندی</li>"
                + "<li>مقایسه دو SKU نزدیک قبل از پیش‌فاکتور</li>"
                + "<li>ثبت بچ و تاریخ ورود در انبار</li></ul>";
            post.Content = SeoEditorialWriter.FinalizePublishHtml(
                post.Content, profile.FocusKeyword ?? post.Title,
                SeoText.ShortForm(profile.FocusKeyword ?? post.Title, post.Title), post.Title);
            await db.SaveChangesAsync(ct);
            report = await AnalyzeBlogAsync(post, profile, ct);
        }

        if (!report.IsPublishReady && report.SeoScore < SeoContentRules.MinOnPageScore)
        {
            await ApplyEditorialDimensionFixAsync("onpage", post, profile, ct);
            EditorialKeywordSync.ApplyToPost(post, profile);
            await db.SaveChangesAsync(ct);
            report = await AnalyzeBlogAsync(post, profile, ct);
        }

        for (var rebuild = 0; rebuild < 3 && !report.IsPublishReady; rebuild++)
        {
            if (profile.LockArticleContent) break;
            if (rebuild > 0 || report.CriticalIssues.Count > 0)
                await RebuildBlogBodyForGateAsync(post, profile, ct);
            else
                break;
            await db.SaveChangesAsync(ct);
            report = await AnalyzeBlogAsync(post, profile, ct);
            iterations.Add(new SeoIterationStep
            {
                Iteration = iterations.Count,
                TargetDimension = "محتوا",
                Action = $"بازسازی کامل بدنه مقاله (تلاش {rebuild + 1})",
                ScoreBefore = iterations.LastOrDefault()?.ScoreAfter ?? before.OverallScore,
                ScoreAfter = report.OverallScore
            });
        }

        report.Iterations = iterations;
        await PersistAsync(profile, report, ct);
        return report;
    }

    async Task RebuildBlogBodyForGateAsync(BlogPost post, SeoProfile profile, CancellationToken ct)
    {
        var primary = profile.FocusKeyword ?? post.Title;
        var secondary = profile.SecondaryKeyword ?? SiteKeywordStrategy.Secondary;
        var angle = InferEditorialAngle(post.Title);
        var products = await ResolveEditorialProductLinksAsync(primary, secondary, ct);
        var seed = post.Id * 19 + post.Title.Length;

        post.Content = SeoEditorialWriter.BuildContent(
            post.Title, primary, secondary, angle, post.Id, products, seed);
        var intro = SeoEditorialWriter.BuildUniqueLeadParagraph(post.Title, primary, post.Id);
        if (!post.Content.Contains(intro, StringComparison.Ordinal))
            post.Content = "<p><strong>" + SeoText.HeadPhrase(primary, post.Title) + "</strong> " + intro + "</p>" + post.Content;

        if (!post.Content.Contains("<table", StringComparison.OrdinalIgnoreCase) && angle == "compare")
        {
            post.Content += "<h2>مقایسه سریع</h2><table><tr><th>معیار</th><th>گیاهی</th><th>پلاستیک</th></tr>"
                            + "<tr><td>برند</td><td>قوی</td><td>متوسط</td></tr></table>";
        }

        if (!post.Content.Contains("<ol", StringComparison.OrdinalIgnoreCase))
        {
            post.Content += "<h2>مراحل سفارش</h2><ol><li>انتخاب SKU</li><li>استعلام</li><li>تسویه</li><li>تحویل</li></ol>";
        }

        post.Content = await EnsureUniqueBlogHtmlAsync(
            post, primary, secondary, angle, products, post.Content, ct);
        post.Content = SeoEditorialWriter.EnsureAeoSectionInHtml(
            post.Content, primary, post.Title, secondary, post.Id);
        await EnsureBlogImagesAsync(post, profile, ct);
        ApplyEditorialKeywordDensityGuard(post, profile);

        if (!profile.LockArticleExcerpt)
            post.Excerpt = SeoEditorialWriter.BuildExcerpt(primary, post.Title, angle);

        for (var extra = 0; extra < 6; extra++)
        {
            var words = SeoText.CountWords(SeoText.StripHtml(post.Content ?? ""));
            if (words >= SeoEditorialWriter.MinWordCount + 40) break;
            post.Content += "<p>" + SeoEditorialWriter.BuildUniqueExpansionSnippet(
                primary, secondary, post.Title, post.Id * 31 + extra) + "</p>";
        }

        post.Content = SeoEditorialWriter.FinalizePublishHtml(
            post.Content ?? "", primary, SeoText.ShortForm(primary, post.Title), post.Title);
    }

    void ApplyEditorialKeywordDensityGuard(BlogPost post, SeoProfile profile)
    {
        if (profile.LockArticleContent) return;
        var focus = profile.FocusKeyword ?? post.Title;
        var shortForm = SeoText.ShortForm(focus, post.Title);
        var densityPhrase = shortForm.Length >= 4 ? shortForm : SeoText.HeadPhrase(focus, post.Title);
        post.Content = SeoEditorialWriter.FinalizePublishHtml(
            post.Content ?? "", focus, shortForm, post.Title);
    }

    async Task<string> ApplyEditorialDimensionFixAsync(
        string dimension, BlogPost post, SeoProfile profile, CancellationToken ct)
    {
        var focus = profile.FocusKeyword ?? post.Title;
        var secondary = profile.SecondaryKeyword ?? SiteKeywordStrategy.Secondary;
        var angle = InferEditorialAngle(post.Title);
        var products = await ResolveEditorialProductLinksAsync(focus, secondary, ct);

        switch (dimension)
        {
            case "onpage":
                post.MetaTitle = SeoArticleBuilder.BuildMetaTitle(focus, post.Title);
                post.MetaDescription = SeoArticleBuilder.BuildMetaDescription(focus, secondary, post.Title, post.Excerpt ?? "");
                profile.MetaTitle = post.MetaTitle;
                profile.MetaDescription = post.MetaDescription;
                EditorialKeywordSync.ApplyToPost(post, profile);
                if (!profile.LockArticleExcerpt)
                    post.Excerpt = SeoEditorialWriter.BuildExcerpt(focus, post.Title, angle);
                return "بازسازی Meta مقاله";
            case "geo":
                profile.AiSummary = SeoEditorialWriter.BuildGeoSummary(focus, secondary, post.Title, products);
                return "بازنویسی خلاصه GEO";
            case "aeo":
                profile.FaqJson = SeoFaqStore.Serialize(SeoEditorialWriter.BuildFaqs(focus, post.Title, secondary));
                if (!profile.LockArticleContent)
                {
                    post.Content = SeoEditorialWriter.EnsureAeoSectionInHtml(
                        post.Content ?? "", focus, post.Title, secondary, post.Id);
                    ApplyEditorialKeywordDensityGuard(post, profile);
                }
                return "بازسازی FAQ AEO + بلوک پاسخ مستقیم";
            case "image":
                await EnsureBlogImagesAsync(post, profile, ct);
                return "تصویر شاخص و عکس‌های درون مقاله از کاتالوگ";
            default:
                if (!profile.LockArticleContent)
                {
                    var piped = await editorialPipeline.GenerateBlogProseAsync(
                        post.Title, focus, secondary, angle, post.Id, products, ct);
                    post.Content = await EnsureUniqueBlogHtmlAsync(
                        post, focus, secondary, angle, products, piped.Html, ct);
                    post.Content = SeoEditorialWriter.EnsureAeoSectionInHtml(
                        post.Content, focus, post.Title, secondary, post.Id);
                    if (!string.IsNullOrWhiteSpace(piped.FaqJson))
                        profile.FaqJson = piped.FaqJson;
                    if (!string.IsNullOrWhiteSpace(piped.GeoSummary) && !profile.LockArticleExcerpt)
                        profile.AiSummary = piped.GeoSummary;
                }
                return "بازتولید محتوا با تحقیق SERP و شکاف رقبا";
        }
    }

    public async Task<SeoUltimateReport> AuditNewsAsync(int newsId, CancellationToken ct = default)
    {
        var item = await db.NewsItems.FindAsync([newsId], ct)
            ?? throw new InvalidOperationException($"News {newsId} not found");
        var profile = await GetOrCreateProfileAsync("news", newsId, ct);
        var report = await AnalyzeNewsAsync(item, profile, ct);
        await PersistAsync(profile, report, ct);
        return report;
    }

    public async Task<SeoUltimateReport> AutoFixNewsAsync(int newsId, CancellationToken ct = default)
    {
        var item = await db.NewsItems.FindAsync([newsId], ct)
            ?? throw new InvalidOperationException($"News {newsId} not found");
        var profile = await GetOrCreateProfileAsync("news", newsId, ct);
        var before = await AnalyzeNewsAsync(item, profile, ct);
        await ApplyEditorialFoundationAsync(item, profile, "news", ct);
        await db.SaveChangesAsync(ct);
        var report = await AnalyzeNewsAsync(item, profile, ct);
        report.Iterations =
        [
            new SeoIterationStep
            {
                Iteration = 0,
                TargetDimension = "foundation",
                Action = "بازسازی Keyword از GSC، Meta، خلاصه، محتوا و یکتاسازی متن",
                ScoreBefore = before.OverallScore,
                ScoreAfter = report.OverallScore
            }
        ];
        await PersistAsync(profile, report, ct);
        return report;
    }

    async Task ApplyEditorialLightFoundationAsync(BlogPost post, SeoProfile profile, CancellationToken ct)
    {
        var gsc = await serp.GetSearchConsoleQueriesAsync(ct);
        var (primary, secondary) = SeoKeywordResearch.ResolveEditorialKeywords(post.Title, gsc);
        profile.FocusKeyword = primary;
        profile.SecondaryKeyword = secondary;
        post.MetaTitle = SeoArticleBuilder.BuildMetaTitle(primary, post.Title);
        post.MetaDescription = SeoArticleBuilder.BuildMetaDescription(primary, secondary, post.Title, post.Excerpt ?? "");
        profile.MetaTitle = post.MetaTitle;
        profile.MetaDescription = post.MetaDescription;
        EditorialKeywordSync.ApplyToPost(post, profile);

        if (EditorialImageResolver.IsPlaceholderFeatured(post.FeaturedImageUrl))
            post.FeaturedImageUrl = await EditorialImageResolver.ResolveFeaturedImageAsync(
                db, primary, secondary, ct);

        if ((post.Excerpt ?? "").Trim().Length < 80 && !profile.LockArticleExcerpt)
        {
            var angle = InferEditorialAngle(post.Title);
            post.Excerpt = SeoEditorialWriter.BuildExcerpt(primary, post.Title, angle);
        }

        if (!profile.LockArticleContent && !string.IsNullOrWhiteSpace(post.Content))
        {
            var density = SeoEditorialWriter.DensityPhrase(primary, post.Title);
            var plain = SeoText.StripHtml(post.Content);
            var intro = plain.Length > 200 ? plain[..200] : plain;
            if (!SeoText.IsKeywordish(intro, density, 0.5))
            {
                var lead = SeoEditorialWriter.BuildUniqueLeadParagraph(post.Title, primary, post.Id);
                post.Content = "<p>" + lead + "</p>" + post.Content;
            }

            post.Content = SeoEditorialWriter.FinalizePublishHtml(
                post.Content, primary, SeoText.ShortForm(primary, post.Title), post.Title);
        }
    }

    async Task ApplyEditorialFoundationAsync(
        BlogPost post, SeoProfile profile, string kind, CancellationToken ct)
    {
        var primary = profile.FocusKeyword ?? post.Title;
        var secondary = profile.SecondaryKeyword ?? SiteKeywordStrategy.Secondary;

        var products = await ResolveEditorialProductLinksAsync(primary, secondary, ct);
        var angle = InferEditorialAngle(post.Title);

        if (!profile.LockArticleExcerpt)
        {
            post.Excerpt = SeoEditorialWriter.BuildExcerpt(primary, post.Title, angle);
            profile.AiSummary = SeoEditorialWriter.BuildGeoSummary(primary, secondary, post.Title, products);
        }

        profile.FaqJson = SeoFaqStore.Serialize(
            SeoEditorialWriter.BuildFaqs(primary, post.Title, secondary));

        if (EditorialImageResolver.IsPlaceholderFeatured(post.FeaturedImageUrl))
            post.FeaturedImageUrl = await EditorialImageResolver.ResolveFeaturedImageAsync(
                db, primary, secondary, ct);

        if (!profile.LockArticleContent && kind == "blog")
        {
            var piped = await editorialPipeline.GenerateBlogProseAsync(
                post.Title, primary, secondary, angle, post.Id, products, ct);
            post.Content = await EnsureUniqueBlogHtmlAsync(
                post, primary, secondary, angle, products, piped.Html, ct);
            post.Content = SeoEditorialWriter.EnsureAeoSectionInHtml(
                post.Content, primary, post.Title, secondary);
            if (!string.IsNullOrWhiteSpace(piped.FaqJson))
                profile.FaqJson = piped.FaqJson;
            if (!string.IsNullOrWhiteSpace(piped.GeoSummary) && !profile.LockArticleExcerpt)
                profile.AiSummary = piped.GeoSummary;
        }
        else if (!profile.LockArticleContent)
        {
            post.Content = SeoEditorialWriter.BuildContent(
                post.Title, primary, secondary, angle, post.Id, products);
        }

        if (kind == "blog")
            await EnsureBlogImagesAsync(post, profile, ct);

        post.UpdatedAt = DateTime.UtcNow;
    }

    async Task EnsureBlogImagesAsync(BlogPost post, SeoProfile profile, CancellationToken ct)
    {
        var focus = profile.FocusKeyword ?? post.Title;
        var secondary = profile.SecondaryKeyword ?? SiteKeywordStrategy.Secondary;

        var (featured, _, inlinePicks) = await EditorialImageResolver.ResolveArticleImagesAsync(
            db, focus, secondary, ct);
        if (EditorialImageResolver.IsPlaceholderFeatured(post.FeaturedImageUrl))
            post.FeaturedImageUrl = featured;

        if (profile.LockArticleContent) return;

        post.Content = SeoEditorialWriter.EnsureCatalogImagesInHtml(
            post.Content ?? "", post.FeaturedImageUrl, focus, post.Title, post.Id, inlinePicks);
    }

    async Task<string> EnsureUniqueBlogHtmlAsync(
        BlogPost post,
        string primary,
        string secondary,
        string angle,
        IReadOnlyList<ProductLink> products,
        string initialHtml,
        CancellationToken ct)
    {
        var html = initialHtml;
        var signature = SeoSiteUniqueness.BlogSignature(post, primary);

        for (var attempt = 0; attempt < 8; attempt++)
        {
            var plain = SeoText.StripHtml(html);
            var (siteDup, _) = await SeoSiteUniqueness.CheckAsync(db, "blog", post.Id, plain, signature, ct);
            var analysis = SeoContentAnalyzer.Analyze(html, primary, secondary);
            html = EditorialParagraphDiversifier.ReduceInternalDuplication(
                html, primary, secondary, post.Title, post.Id + attempt * 13);

            plain = SeoText.StripHtml(html);
            analysis = SeoContentAnalyzer.Analyze(html, primary, secondary);
            (siteDup, _) = await SeoSiteUniqueness.CheckAsync(db, "blog", post.Id, plain, signature, ct);

            if (!SeoSiteUniqueness.ShouldRewrite(siteDup)
                && analysis.DuplicationRisk <= SeoContentRules.MaxDuplicationRisk)
                return html;

            html = attempt < 3
                ? (await editorialPipeline.GenerateBlogProseAsync(
                    post.Title, primary, secondary, angle, post.Id, products, ct)).Html
                : SeoEditorialWriter.BuildContent(
                    post.Title, primary, secondary, angle, post.Id, products, attempt);
        }

        var finalPlain = SeoText.StripHtml(html);
        var (finalDup, _) = await SeoSiteUniqueness.CheckAsync(db, "blog", post.Id, finalPlain, signature, ct);
        if (finalDup > SeoContentRules.MaxDuplicationRisk
            && !html.Contains("مرجع #", StringComparison.Ordinal))
        {
            var lead = SeoEditorialWriter.BuildUniqueLeadParagraph(post.Title, primary, post.Id);
            html = "<p>" + lead + "</p>" + html;
        }

        return SeoEditorialWriter.FinalizePublishHtml(html, primary, SeoText.ShortForm(primary, post.Title), post.Title);
    }

    static string EditorialPlainForUniquenessCheck(string? html)
    {
        var plain = SeoText.StripHtml(html ?? "");
        var aeo = plain.IndexOf("پاسخ‌های کوتاه برای جستجو", StringComparison.Ordinal);
        if (aeo > 250) plain = plain[..aeo].Trim();
        return plain;
    }

    static string InferEditorialAngle(string title)
    {
        if (title.Contains("مقایسه", StringComparison.Ordinal)) return "compare";
        if (title.Contains("خرید عمده", StringComparison.Ordinal)) return "buying";
        if (title.Contains("کاربرد", StringComparison.Ordinal)) return "usecase";
        if (title.Contains("سوالات", StringComparison.Ordinal)) return "faq";
        return "guide";
    }

    async Task<List<ProductLink>> ResolveEditorialProductLinksAsync(
        string focus, string secondary, CancellationToken ct)
    {
        var tokens = SeoText.Tokenize(focus).Concat(SeoText.Tokenize(secondary))
            .Where(t => t.Length > 2).Distinct().ToList();
        var products = await db.Products.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .Take(80)
            .ToListAsync(ct);
        return products
            .Select(p => new { p, score = tokens.Sum(t => ($"{p.Name} {p.ProductType}").Contains(t, StringComparison.OrdinalIgnoreCase) ? 1 : 0) })
            .OrderByDescending(x => x.score)
            .Where(x => x.score > 0)
            .Take(4)
            .Select(x => new ProductLink(x.p.Name, x.p.Slug, x.p.ProductCode))
            .ToList();
    }

    async Task ApplyEditorialFoundationAsync(
        NewsItem item, SeoProfile profile, string kind, CancellationToken ct)
    {
        var gsc = await serp.GetSearchConsoleQueriesAsync(ct);
        var (primary, secondary) = SeoKeywordResearch.ResolveEditorialKeywords(item.Title, gsc);
        profile.FocusKeyword = primary;
        profile.SecondaryKeyword = secondary;
        item.MetaTitle = SeoArticleBuilder.BuildMetaTitle(primary, item.Title);
        item.MetaDescription = SeoArticleBuilder.BuildMetaDescription(primary, secondary, item.Title, item.Excerpt ?? "");
        profile.MetaTitle = item.MetaTitle;
        profile.MetaDescription = item.MetaDescription;
        profile.MetaKeywords = $"{primary}، {secondary}";
        if (!profile.LockArticleExcerpt)
            profile.AiSummary = item.Excerpt = SeoArticleBuilder.BuildExcerpt(primary, item.Title, item.Id);
        if (!profile.LockArticleContent)
        {
            item.Content = SeoArticleBuilder.BuildContent(item.Title, primary, secondary, item.Id, kind);
            for (var attempt = 0; attempt < 5; attempt++)
            {
                var (dup, _) = await SeoSiteUniqueness.CheckAsync(db, kind, item.Id, SeoText.StripHtml(item.Content), item.Title, ct);
                if (!SeoSiteUniqueness.ShouldRewrite(dup)) break;
                item.Content = SeoArticleBuilder.BuildContent(item.Title, primary, secondary, item.Id, kind, attempt + 1);
            }
        }
        item.UpdatedAt = DateTime.UtcNow;
    }

    // ─────────────────────────────────────────────
    //  تحلیل
    // ─────────────────────────────────────────────

    async Task<SeoUltimateReport> AnalyzeProductAsync(
        Product product, SeoProfile profile, CancellationToken ct)
    {
        SeoGeoSummaryMaintainer.EnsureProduct(profile, product);

        var focus = profile.FocusKeyword ?? product.KeywordPrimary ?? product.Name;

        var prose = ProductDescriptionHelper.GetProseForEditor(product.Description);
        var siteDup = (await SeoSiteUniqueness.CheckAsync(
            db, "product", product.Id, SeoText.StripHtml(prose),
            SeoSiteUniqueness.ProductSignature(product), ct)).MaxSimilarity;

        var ctx = new SeoUltimateContext
        {
            Product = product,
            Profile = profile,
            Prose = prose,
            Baseline = await serp.GetBaselineAsync(focus, ct),
            SerpTitles = await serp.GetSerpTitlesAsync(focus, ct),
            GscQueries = await serp.GetSearchConsoleQueriesAsync(ct),
            Cannibalization = await DetectCannibalizationAsync("product", product.Id, focus, ct),
            HasReviews = await reviews.CountApprovedAsync(product.Id, ct) >= 1,
            SiteDuplicationPercent = siteDup
        };

        // کارهای انسانی قبل از Finalize اضافه می‌شوند تا هم در Audit دیده شوند
        // و هم جریمه‌شان در Ranking Readiness لحاظ شود.
        return SeoUltimateAnalyzer.AnalyzeProduct(ctx, r => AddRemainingManualTasks(r, product));
    }

    async Task<SeoUltimateReport> AnalyzeCategoryAsync(
        Category category, SeoProfile profile, CancellationToken ct)
    {
        SeoGeoSummaryMaintainer.EnsureCategory(profile, category, category.Products?.Count ?? 0);

        var focus = profile.FocusKeyword ?? category.Name;

        var ctx = new SeoUltimateContext
        {
            Category = category,
            Profile = profile,
            Prose = category.Description ?? "",
            Baseline = await serp.GetBaselineAsync(focus, ct),
            SerpTitles = await serp.GetSerpTitlesAsync(focus, ct),
            GscQueries = await serp.GetSearchConsoleQueriesAsync(ct),
            Cannibalization = await DetectCannibalizationAsync("category", category.Id, focus, ct)
        };

        return SeoUltimateAnalyzer.AnalyzeCategory(ctx, r => AddCategoryManualTasks(r, category));
    }

    async Task<SeoUltimateReport> AnalyzeBlogAsync(BlogPost post, SeoProfile profile, CancellationToken ct)
    {
        var focus = profile.FocusKeyword ?? post.Title;
        SeoGeoSummaryMaintainer.EnsureEditorial(profile, focus, post.Title, post.Id);
        var plain = EditorialPlainForUniquenessCheck(post.Content);
        var siteDup = (await SeoSiteUniqueness.CheckAsync(
            db, "blog", post.Id, plain, SeoSiteUniqueness.BlogSignature(post, focus), ct)).MaxSimilarity;
        var ctx = new SeoUltimateContext
        {
            Profile = profile,
            Prose = post.Content ?? "",
            EditorialTitle = post.Title,
            EditorialExcerpt = post.Excerpt,
            FeaturedImageUrl = post.FeaturedImageUrl,
            EditorialPublished = post.IsPublished,
            EditorialKind = "blog",
            SiteDuplicationPercent = siteDup,
            Baseline = await serp.GetBaselineAsync(focus, ct),
            SerpTitles = await serp.GetSerpTitlesAsync(focus, ct),
            GscQueries = await serp.GetSearchConsoleQueriesAsync(ct),
            Cannibalization = await DetectCannibalizationAsync("blog", post.Id, focus, ct)
        };
        return SeoUltimateAnalyzer.AnalyzeEditorial(ctx, r => AddEditorialManualTasks(r, post.FeaturedImageUrl));
    }

    async Task<SeoUltimateReport> AnalyzeNewsAsync(NewsItem item, SeoProfile profile, CancellationToken ct)
    {
        var focus = profile.FocusKeyword ?? item.Title;
        SeoGeoSummaryMaintainer.EnsureEditorial(profile, focus, item.Title, item.Id);
        var plain = SeoText.StripHtml(item.Content ?? "");
        var siteDup = (await SeoSiteUniqueness.CheckAsync(db, "news", item.Id, plain, item.Title, ct)).MaxSimilarity;
        var ctx = new SeoUltimateContext
        {
            Profile = profile,
            Prose = item.Content ?? "",
            EditorialTitle = item.Title,
            EditorialExcerpt = item.Excerpt,
            FeaturedImageUrl = item.FeaturedImageUrl,
            EditorialPublished = item.IsPublished,
            EditorialKind = "news",
            SiteDuplicationPercent = siteDup,
            Baseline = await serp.GetBaselineAsync(focus, ct),
            SerpTitles = await serp.GetSerpTitlesAsync(focus, ct),
            GscQueries = await serp.GetSearchConsoleQueriesAsync(ct),
            Cannibalization = await DetectCannibalizationAsync("news", item.Id, focus, ct)
        };
        return SeoUltimateAnalyzer.AnalyzeEditorial(ctx, r => AddEditorialManualTasks(r, item.FeaturedImageUrl));
    }

    static void AddEditorialManualTasks(SeoUltimateReport r, string? featuredImageUrl)
    {
        if (EditorialImageResolver.IsPlaceholderFeatured(featuredImageUrl))
            r.ManualTasks.Add(new SeoManualTask
            {
                Title = "تصویر شاخص واقعی (غیر از لوگو) برای مقاله تنظیم کنید",
                Why = "بدون تصویر شاخص از کاتالوگ، CTR و Image SEO ضعیف می‌ماند.",
                ScoreImpact = 12,
                BlocksPublish = r.ImageScore < SeoContentRules.MinImageScore
            });
    }

    /// <summary>کارهای دسته که موتور نمی‌تواند انجام دهد.</summary>
    static void AddCategoryManualTasks(SeoUltimateReport r, Category category)
    {
        if (category.Products is { Count: 0 })
            r.ManualTasks.Insert(0, new SeoManualTask
            {
                Title = "حداقل ۳ محصول به این دسته اضافه کنید",
                Why = "دسته خالی Thin Content است و موتور نمی‌تواند محصول بسازد.",
                ScoreImpact = 20,
                BlocksPublish = true
            });

        if (string.IsNullOrWhiteSpace(category.ImageUrl))
            r.ManualTasks.Add(new SeoManualTask
            {
                Title = "تصویر شاخص دسته را آپلود کنید",
                Why = "تصویر دسته در Image Search و کارت‌های اشتراک‌گذاری استفاده می‌شود.",
                ScoreImpact = 8,
                BlocksPublish = false
            });

        if (!r.Baseline.HasLiveData)
            r.ManualTasks.Add(new SeoManualTask
            {
                Title = "همگام‌سازی داده SERP رقبا را فعال کنید (SerpApi:ApiKey)",
                Why = "بدون داده واقعی Top 10، قدرت رقابتی فقط تخمین است و ادعای رتبه معتبر نیست.",
                ScoreImpact = 6,
                BlocksPublish = false
            });

        r.ManualTasks = r.ManualTasks.GroupBy(t => t.Title).Select(g => g.First()).ToList();
    }

    /// <summary>
    /// Keyword Cannibalization — دو صفحه که برای یک Keyword رقابت می‌کنند
    /// اعتبار را تقسیم می‌کنند و هیچ‌کدام رتبه نمی‌گیرد.
    /// </summary>
    async Task<List<SeoCannibalRisk>> DetectCannibalizationAsync(
        string entityType, int entityId, string focus, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(focus)) return [];

        var others = await db.SeoProfiles
            .Where(p => p.FocusKeyword != null && p.FocusKeyword != ""
                && !(p.EntityType == entityType && p.EntityId == entityId))
            .Select(p => new { p.EntityType, p.EntityId, p.FocusKeyword })
            .ToListAsync(ct);

        var clashes = others
            .Where(o => SeoText.Similarity(o.FocusKeyword!, focus) >= 0.8)
            .Take(5)
            .ToList();

        if (clashes.Count == 0) return [];

        var productIds = clashes.Where(c => c.EntityType == "product").Select(c => c.EntityId).ToList();
        var categoryIds = clashes.Where(c => c.EntityType == "category").Select(c => c.EntityId).ToList();

        var productNames = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var categoryNames = await db.Categories
            .Where(c => categoryIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        return clashes.Select(c => new SeoCannibalRisk
        {
            Keyword = c.FocusKeyword!,
            EntityType = c.EntityType,
            EntityId = c.EntityId,
            EntityName = c.EntityType == "product"
                ? productNames.GetValueOrDefault(c.EntityId, $"محصول #{c.EntityId}")
                : categoryNames.GetValueOrDefault(c.EntityId, $"دسته #{c.EntityId}"),
            Recommendation = c.EntityType == "category"
                ? "دسته باید Keyword عمومی‌تر (سطح مجموعه) بگیرد و محصول Keyword دقیق‌تر مدل."
                : "Keyword یکی از دو محصول را با ویژگی متمایزکننده (ظرفیت، تعداد خانه یا کاربرد) تخصصی کنید."
        }).ToList();
    }

    // ─────────────────────────────────────────────
    //  ذخیره‌سازی و بارگذاری
    // ─────────────────────────────────────────────

    async Task PersistAsync(SeoProfile profile, SeoUltimateReport r, CancellationToken ct)
    {
        profile.SeoScore = r.SeoScore;
        profile.AeoScore = r.AeoScore;
        profile.GeoScore = r.GeoScore;
        profile.ContentScore = r.ContentScore;
        profile.SemanticScore = r.SemanticScore;
        profile.IntentScore = r.IntentScore;
        profile.ImageScore = r.ImageScore;
        profile.SchemaScore = r.SchemaScore;
        profile.ProductDataScore = r.ProductDataScore;
        profile.InternalLinkingScore = r.InternalLinkingScore;
        profile.TechnicalScore = r.TechnicalScore;
        profile.CompetitiveStrength = r.CompetitiveStrength;
        profile.RankingReadiness = r.RankingReadiness;
        profile.TotalScore = r.OverallScore;
        profile.IsPublishReady = r.IsPublishReady;
        profile.ScoreIssuesJson = JsonSerializer.Serialize(
            r.CriticalIssues.Concat(r.Warnings).Take(25).ToList(), Json);
        profile.LastAuditedAt = DateTime.UtcNow;
        profile.UpdatedAt = DateTime.UtcNow;

        try { profile.UltimateJson = JsonSerializer.Serialize(r, Json); }
        catch (Exception ex) { logger.LogWarning(ex, "Ultimate report serialization skipped"); }

        await db.SaveChangesAsync(ct);
    }

    async Task<Product> LoadProductAsync(int id, CancellationToken ct) =>
        await db.Products
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
        ?? throw new InvalidOperationException($"Product {id} not found");

    async Task<Category> LoadCategoryAsync(int id, CancellationToken ct) =>
        await db.Categories
            .Include(c => c.Products)
            .ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
        ?? throw new InvalidOperationException($"Category {id} not found");

    async Task<SeoProfile> GetOrCreateProfileAsync(string type, int id, CancellationToken ct)
    {
        var profile = await db.SeoProfiles
            .FirstOrDefaultAsync(p => p.EntityType == type && p.EntityId == id, ct);
        if (profile != null) return profile;

        profile = new SeoProfile { EntityType = type, EntityId = id };
        db.SeoProfiles.Add(profile);
        await db.SaveChangesAsync(ct);
        return profile;
    }
}
