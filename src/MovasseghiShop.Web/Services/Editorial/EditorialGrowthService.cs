using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;
using System.Text.RegularExpressions;
using MovasseghiShop.Web.Services;
using MovasseghiShop.Web.Services.ContentPipeline;

namespace MovasseghiShop.Web.Services.Editorial;

public sealed record EditorialPublishSlot(
    int TopicId,
    string Title,
    string Status,
    DateTime? ScheduledPublishUtc,
    string ScheduledTehran,
    int? BlogPostId,
    bool? IsPublishReady);

public sealed record EditorialGrowthStatus(
    int QueuedPlanned,
    int QueuedDrafting,
    int PublishedLast7Days,
    DateTime? NextScheduledUtc,
    string? NextScheduledTehran,
    string ScheduleStrategyFa,
    IReadOnlyList<EditorialPublishSlot> Upcoming,
    string? LastRunSummary);

public interface IEditorialGrowthService
{
    Task<EditorialGrowthStatus> GetStatusAsync(CancellationToken ct = default);
    Task<IReadOnlyList<EditorialBlogInventoryRow>> GetBlogInventoryAsync(CancellationToken ct = default);
    Task<int> PurgeSubstandardBlogsAsync(CancellationToken ct = default);
    Task<int> RefreshTopicQueueAsync(CancellationToken ct = default);
    Task<EditorialGrowthRunResult> RunDailyCycleAsync(CancellationToken ct = default);
    Task<EditorialQualitySweepResult> SweepLowQualityAndProduceAsync(CancellationToken ct = default);
}

public sealed record EditorialGrowthRunResult(
    int TopicsQueued,
    int DraftsCreated,
    int PostsPublished,
    IReadOnlyList<string> Log);

public class EditorialGrowthService(
    ApplicationDbContext db,
    ISeoSerpIntelligence serp,
    ISeoAutoFixEngine seo,
    IEditorialContentPipelineService editorialPipeline,
    IRankTrackerService rankTracker,
    IOptions<EditorialGrowthOptions> options,
    ILogger<EditorialGrowthService> log) : IEditorialGrowthService
{
    readonly EditorialGrowthOptions _opt = options.Value;

    public async Task<EditorialGrowthStatus> GetStatusAsync(CancellationToken ct = default)
    {
        var planned = await db.EditorialTopicQueue.CountAsync(t => t.Status == "planned", ct);
        var drafting = await db.EditorialTopicQueue.CountAsync(t => t.Status == "drafting", ct);
        var week = DateTime.UtcNow.AddDays(-7);
        var pub7 = await db.BlogPosts.CountAsync(b => b.IsPublished && b.PublishedAt >= week, ct);
        var upcomingRows = await db.EditorialTopicQueue.AsNoTracking()
            .Where(t => t.Status == "planned" || t.Status == "drafting")
            .OrderBy(t => t.ScheduledPublishUtc ?? DateTime.MaxValue)
            .ThenByDescending(t => t.OpportunityScore)
            .Take(12)
            .ToListAsync(ct);

        var upcoming = new List<EditorialPublishSlot>();
        foreach (var t in upcomingRows)
        {
            bool? ready = null;
            if (t.BlogPostId is int bid)
            {
                ready = await db.SeoProfiles.AsNoTracking()
                    .Where(p => p.EntityType == "blog" && p.EntityId == bid)
                    .Select(p => p.IsPublishReady)
                    .FirstOrDefaultAsync(ct);
            }

            upcoming.Add(new EditorialPublishSlot(
                t.Id,
                t.ProposedTitle,
                t.Status,
                t.ScheduledPublishUtc,
                t.ScheduledPublishUtc.HasValue
                    ? EditorialHumanSchedule.FormatTehran(t.ScheduledPublishUtc.Value)
                    : "—",
                t.BlogPostId,
                ready));
        }

        var next = upcomingRows
            .Select(t => t.ScheduledPublishUtc)
            .FirstOrDefault(u => u.HasValue);

        var strategy =
            $"هر {_opt.CheckIntervalMinutes} دقیقه سرویس پس‌زمینه چک می‌کند. "
            + $"حداکثر {_opt.MaxPostsPerDay} انتشار در روز (تهران)، حداقل {_opt.MinHoursBetweenPosts} ساعت بین دو پست. "
            + $"پنجره انتشار: {_opt.PublishHourStartTehran}:۰۰ تا {_opt.PublishHourEndTehran}:۰۰ "
            + $"{(_opt.IgnorePublishWindow ? "(در Development پنجره نادیده گرفته می‌شود)" : "به‌وقت تهران")}. "
            + $"روزهای بدون انتشار: {EditorialHumanSchedule.DescribeSkipWeekdays(_opt.SkipWeekdays)}. "
            + $"پیش‌نویس ساخته می‌شود → Auto-Fix تا باز شدن دروازه SEO → "
            + $"{(_opt.AutoPublishWhenReady ? "انتشار خودکار در زمان Scheduled" : "انتشار دستی")}.";

        return new EditorialGrowthStatus(
            planned,
            drafting,
            pub7,
            next,
            next.HasValue ? EditorialHumanSchedule.FormatTehran(next.Value) : null,
            strategy,
            upcoming,
            null);
    }

    static readonly Regex ImgTag = new("<img\\s", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<IReadOnlyList<EditorialBlogInventoryRow>> GetBlogInventoryAsync(CancellationToken ct = default)
    {
        var posts = await db.BlogPosts.AsNoTracking().OrderByDescending(b => b.UpdatedAt).ToListAsync(ct);
        var profiles = await db.SeoProfiles.AsNoTracking()
            .Where(p => p.EntityType == "blog")
            .ToDictionaryAsync(p => p.EntityId, ct);
        var queueByPost = await db.EditorialTopicQueue.AsNoTracking()
            .Where(t => t.BlogPostId != null)
            .ToDictionaryAsync(t => t.BlogPostId!.Value, ct);

        var rows = new List<EditorialBlogInventoryRow>();
        foreach (var b in posts)
        {
            profiles.TryGetValue(b.Id, out var profile);
            var display = profile != null
                ? SeoUltimateAnalyzer.ResolveDisplayOverallFromProfile(profile)
                : 0;
            queueByPost.TryGetValue(b.Id, out var topic);
            var scheduled = topic?.ScheduledPublishUtc.HasValue == true
                ? EditorialHumanSchedule.FormatTehran(topic.ScheduledPublishUtc!.Value)
                : null;
            var published = b.IsPublished && b.PublishedAt.HasValue
                ? EditorialHumanSchedule.FormatTehran(b.PublishedAt.Value)
                : null;
            var feat = string.IsNullOrWhiteSpace(b.FeaturedImageUrl) ? "—"
                : b.FeaturedImageUrl.Contains("scene", StringComparison.OrdinalIgnoreCase) ? "🌅 Scene"
                : b.FeaturedImageUrl.Contains("studio", StringComparison.OrdinalIgnoreCase) ? "📦 Studio"
                : "تصویر";
            rows.Add(new EditorialBlogInventoryRow(
                b.Id, b.Title, b.IsPublished, display,
                profile?.IsPublishReady == true,
                profile?.FocusKeyword,
                scheduled, published, feat,
                ImgTag.Matches(b.Content ?? "").Count));
        }

        return rows;
    }

    public async Task<int> PurgeSubstandardBlogsAsync(CancellationToken ct = default)
    {
        var min = _opt.MinPublishDisplayScore;
        var purged = 0;
        var ids = await db.BlogPosts.Select(p => p.Id).ToListAsync(ct);
        foreach (var id in ids)
        {
            if (await MeetsStoredBlogQualityAsync(id, ct)) continue;
            await PurgeBlogCompletelyAsync(id, ct);
            purged++;
        }

        return purged;
    }

    async Task<bool> MeetsStoredBlogQualityAsync(int blogId, CancellationToken ct)
    {
        var profile = await db.SeoProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.EntityType == "blog" && p.EntityId == blogId, ct);
        if (profile is null) return false;
        var display = SeoUltimateAnalyzer.ResolveDisplayOverallFromProfile(profile);
        return profile.IsPublishReady && display >= _opt.MinPublishDisplayScore;
    }

    public async Task<EditorialQualitySweepResult> SweepLowQualityAndProduceAsync(CancellationToken ct = default)
    {
        var lines = new List<string>();
        var target = Math.Clamp(_opt.TargetArticlesPerQualitySweep, 1, 20);
        var purged = await PurgeSubstandardBlogsAsync(ct);
        if (purged > 0)
            lines.Add($"حذف {purged} مقاله زیر {_opt.MinPublishDisplayScore} یا بدون دروازه انتشار");

        await RefreshTopicQueueAsync(ct);
        var produced = 0;
        var maxRounds = target * 5;
        for (var round = 0; round < maxRounds && produced < target; round++)
        {
            var topic = await db.EditorialTopicQueue
                .Where(t => t.Status == "planned")
                .OrderByDescending(t => t.OpportunityScore)
                .ThenBy(t => t.PillarPriority)
                .FirstOrDefaultAsync(ct);
            if (topic is null)
            {
                var added = await RefreshTopicQueueAsync(ct);
                if (added == 0) break;
                continue;
            }

            var outcome = await TryCreatePublishableDraftAsync(topic, ct);
            if (outcome is not { } ok || !MeetsEditorialQuality(ok.Report))
            {
                topic.Status = "skipped";
                topic.LastError = "به کیفیت هدف نرسید";
                await db.SaveChangesAsync(ct);
                continue;
            }

            topic.Status = "drafting";
            topic.BlogPostId = ok.PostId;
            topic.ScheduledPublishUtc = EditorialHumanSchedule.SuggestNextPublishUtc(_opt);
            await db.SaveChangesAsync(ct);
            produced++;
            lines.Add(
                $"✓ مقاله #{ok.PostId} «{topic.ProposedTitle}» — امتیاز {SeoUltimateAnalyzer.ResolveDisplayOverall(ok.Report)}، انتشار {EditorialHumanSchedule.FormatTehran(topic.ScheduledPublishUtc.Value)}");
        }

        lines.Add($"پاکسازی: {purged} · تولید آماده: {produced}/{target}");
        return new EditorialQualitySweepResult(purged, produced, target, lines);
    }

    bool MeetsEditorialQuality(SeoUltimateReport report) =>
        report.IsPublishReady
        && SeoUltimateAnalyzer.ResolveDisplayOverall(report) >= _opt.MinPublishDisplayScore;

    public async Task<int> RefreshTopicQueueAsync(CancellationToken ct = default)
    {
        var candidates = await EditorialTopicPlanner.PlanAsync(db, serp, _opt.MaxQueuedTopics, ct);
        var existing = await db.EditorialTopicQueue
            .Where(t => t.Status == "planned" || t.Status == "drafting")
            .ToListAsync(ct);
        var activeKeywords = await db.EditorialTopicQueue
            .Where(t => t.Status != "published" && t.Status != "skipped")
            .Select(t => t.FocusKeyword)
            .ToListAsync(ct);

        var added = 0;
        foreach (var c in candidates)
        {
            if (existing.Any(e => SeoText.IsKeywordish(e.FocusKeyword, c.FocusKeyword, 0.9)))
                continue;
            if (activeKeywords.Any(k => SeoText.IsKeywordish(k, c.FocusKeyword, 0.9)))
                continue;

            db.EditorialTopicQueue.Add(new EditorialTopicQueue
            {
                FocusKeyword = c.FocusKeyword,
                SecondaryKeyword = c.SecondaryKeyword,
                ProposedTitle = c.ProposedTitle,
                Angle = c.Angle,
                PillarPriority = c.PillarPriority,
                OpportunityScore = c.OpportunityScore,
                Source = c.Source,
                Status = "planned"
            });
            added++;
        }

        await db.SaveChangesAsync(ct);
        return added;
    }

    public async Task<EditorialGrowthRunResult> RunDailyCycleAsync(CancellationToken ct = default)
    {
        var lines = new List<string>();
        if (!_opt.Enabled)
        {
            lines.Add("EditorialGrowth غیرفعال است.");
            return new EditorialGrowthRunResult(0, 0, 0, lines);
        }

        if (EditorialHumanSchedule.IsSkippedWeekday(_opt))
        {
            lines.Add("امروز در تقویم SkipWeekdays است — انتشار انجام نشد.");
            return new EditorialGrowthRunResult(0, 0, 0, lines);
        }

        var purged = await PurgeSubstandardBlogsAsync(ct);
        if (purged > 0)
            lines.Add($"پاکسازی: {purged} مقاله زیر {_opt.MinPublishDisplayScore} حذف شد");

        var boosted = await BoostUnderperformingPostsAsync(ct, lines);
        var queued = await RefreshTopicQueueAsync(ct);
        lines.Add($"موضوع جدید در صف: {queued}");
        if (boosted > 0) lines.Add($"مقالات به‌روزرسانی‌شده (SERP): {boosted}");

        if (!_opt.IgnorePublishWindow && !EditorialHumanSchedule.IsWithinPublishWindow(_opt))
        {
            lines.Add("خارج از پنجره انتشار تهران — فقط انتشار‌های سررسید.");
            var latePub = await PublishDueDraftsAsync(ct, lines);
            await db.SaveChangesAsync(ct);
            return new EditorialGrowthRunResult(queued, 0, latePub, lines);
        }

        var drafts = 0;
        var published = await PublishDueDraftsAsync(ct, lines);

        var maxCreates = Math.Max(_opt.MaxPostsPerDay * 8, 12);
        for (var attempt = 0; attempt < maxCreates; attempt++)
        {
            var publishedToday = await EditorialHumanSchedule.CountPublishedTodayTehranAsync(db, ct);
            if (publishedToday >= _opt.MaxPostsPerDay)
            {
                lines.Add($"سقف انتشار روز ({_opt.MaxPostsPerDay}) پر شد.");
                break;
            }

            if (!await EditorialHumanSchedule.CanPublishAnotherTodayAsync(db, _opt, ct)) break;

            if (published > 0 && !await EditorialHumanSchedule.RespectsCooldownAsync(db, _opt, ct))
            {
                lines.Add("فاصلهٔ حداقل بین انتشار‌ها — در چرخه بعدی ادامه می‌دهد.");
                break;
            }

            var topic = await db.EditorialTopicQueue
                .Where(t => t.Status == "planned")
                .OrderByDescending(t => t.OpportunityScore)
                .ThenBy(t => t.PillarPriority)
                .FirstOrDefaultAsync(ct);

            if (topic is null)
            {
                var added = await RefreshTopicQueueAsync(ct);
                if (added == 0) break;
                continue;
            }

            try
            {
                var outcome = await TryCreatePublishableDraftAsync(topic, ct);
                if (outcome is null)
                {
                    topic.Status = "skipped";
                    topic.LastError = "به کیفیت هدف نرسید — حذف و موضوع بعدی";
                    await db.SaveChangesAsync(ct);
                    lines.Add($"رد شد: «{topic.ProposedTitle}»");
                    continue;
                }

                var (postId, report) = outcome.Value;
                var display = SeoUltimateAnalyzer.ResolveDisplayOverall(report);
                topic.Status = "drafting";
                topic.BlogPostId = postId;
                topic.ScheduledPublishUtc = DateTime.UtcNow.AddMinutes(-3);
                await db.SaveChangesAsync(ct);
                drafts++;

                if (_opt.AutoPublishWhenReady && MeetsEditorialQuality(report))
                {
                    if (await TryPublishPostAsync(postId, topic.ScheduledPublishUtc.Value, ct))
                    {
                        published++;
                        topic.Status = "published";
                        topic.CompletedAt = DateTime.UtcNow;
                        lines.Add($"منتشر شد #{postId} «{topic.ProposedTitle}» — امتیاز {display}");
                        continue;
                    }
                }

                lines.Add($"پیش‌نویس #{postId} آماده (امتیاز {display}) — در صف انتشار");
            }
            catch (Exception ex)
            {
                topic.LastError = ex.Message;
                topic.Status = "planned";
                log.LogWarning(ex, "Editorial draft failed for topic {Id}", topic.Id);
                lines.Add($"خطا: {ex.Message}");
            }
        }

        published += await PublishDueDraftsAsync(ct, lines);
        await db.SaveChangesAsync(ct);
        return new EditorialGrowthRunResult(queued, drafts, published, lines);
    }

    async Task<int> PublishDueDraftsAsync(CancellationToken ct, List<string> lines)
    {
        if (!_opt.AutoPublishWhenReady) return 0;

        var extra = 0;
        var due = await db.EditorialTopicQueue
            .Where(t => t.Status == "drafting" && t.BlogPostId != null && t.ScheduledPublishUtc <= DateTime.UtcNow)
            .ToListAsync(ct);

        foreach (var t in due)
        {
            if (!await EditorialHumanSchedule.CanPublishAnotherTodayAsync(db, _opt, ct)) break;
            if (!await EditorialHumanSchedule.RespectsCooldownAsync(db, _opt, ct)) break;

            var report = await seo.AuditBlogAsync(t.BlogPostId!.Value, ct);
            if (!MeetsEditorialQuality(report))
                report = await seo.AutoFixBlogAsync(t.BlogPostId!.Value, 9, forceRegenerate: true, ct);
            if (!MeetsEditorialQuality(report))
            {
                var failedId = t.BlogPostId!.Value;
                await PurgeBlogCompletelyAsync(failedId, ct);
                t.BlogPostId = null;
                t.Status = "planned";
                t.LastError = "در زمان انتشار به دروازه SEO نرسید — پیش‌نویس حذف شد.";
                lines.Add($"پیش‌نویس #{failedId} حذف شد (آماده انتشار نیست)");
                continue;
            }

            if (await TryPublishPostAsync(t.BlogPostId!.Value, t.ScheduledPublishUtc ?? DateTime.UtcNow, ct))
            {
                extra++;
                t.Status = "published";
                t.CompletedAt = DateTime.UtcNow;
                lines.Add($"منتشر شد: پست #{t.BlogPostId}");
            }
        }

        return extra;
    }

    async Task<(int PostId, SeoUltimateReport Report)?> TryCreatePublishableDraftAsync(
        EditorialTopicQueue topic, CancellationToken ct)
    {
        var attempts = Math.Clamp(_opt.MaxDraftAttemptsPerTopic, 3, 10);
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            var postId = await CreateDraftFromTopicAsync(topic, attempt, ct);
            var report = await seo.AutoFixBlogAsync(postId, 9, forceRegenerate: true, ct);
            if (MeetsEditorialQuality(report))
                return (postId, report);

            log.LogWarning(
                "Editorial draft #{PostId} attempt {Attempt} — display {Display}, {Verdict}",
                postId, attempt + 1, SeoUltimateAnalyzer.ResolveDisplayOverall(report), report.RankingVerdict);
            await PurgeBlogCompletelyAsync(postId, ct);
        }

        return null;
    }

    async Task PurgeBlogCompletelyAsync(int postId, CancellationToken ct)
    {
        var profile = await db.SeoProfiles.FirstOrDefaultAsync(
            p => p.EntityType == "blog" && p.EntityId == postId, ct);
        if (profile != null) db.SeoProfiles.Remove(profile);

        var post = await db.BlogPosts.FindAsync([postId], ct);
        if (post != null) db.BlogPosts.Remove(post);

        var queueLinks = await db.EditorialTopicQueue
            .Where(t => t.BlogPostId == postId)
            .ToListAsync(ct);
        foreach (var t in queueLinks)
        {
            t.BlogPostId = null;
            if (t.Status == "drafting") t.Status = "planned";
        }

        await db.SaveChangesAsync(ct);
    }

    async Task<int> CreateDraftFromTopicAsync(EditorialTopicQueue topic, int differentiationAttempt, CancellationToken ct)
    {
        var title = topic.ProposedTitle.Trim();
        var slug = await UniqueSlugAsync(title, ct);
        var post = new BlogPost
        {
            Title = title,
            Slug = slug,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.BlogPosts.Add(post);
        await db.SaveChangesAsync(ct);

        var products = await ResolveProductLinksAsync(topic.FocusKeyword, topic.SecondaryKeyword, ct);
        post.Excerpt = SeoEditorialWriter.BuildExcerpt(topic.FocusKeyword, title, topic.Angle);
        var piped = await editorialPipeline.GenerateBlogProseAsync(
            title, topic.FocusKeyword, topic.SecondaryKeyword, topic.Angle, post.Id + differentiationAttempt * 31, products, ct);
        var (featuredUrl, _, inlinePicks) = await EditorialImageResolver.ResolveArticleImagesAsync(
            db, topic.FocusKeyword, topic.SecondaryKeyword, ct);
        post.FeaturedImageUrl = featuredUrl;
        post.Content = SeoEditorialWriter.EnsureCatalogImagesInHtml(
            piped.Html, post.FeaturedImageUrl, topic.FocusKeyword, title, post.Id, inlinePicks);
        post.UpdatedAt = DateTime.UtcNow;

        var profile = await db.SeoProfiles.FirstOrDefaultAsync(
            p => p.EntityType == "blog" && p.EntityId == post.Id, ct);
        if (profile == null)
        {
            profile = new SeoProfile { EntityType = "blog", EntityId = post.Id };
            db.SeoProfiles.Add(profile);
        }

        profile.FocusKeyword = topic.FocusKeyword;
        profile.SecondaryKeyword = topic.SecondaryKeyword;
        profile.FaqJson = piped.FaqJson ?? SeoFaqStore.Serialize(
            SeoEditorialWriter.BuildFaqs(topic.FocusKeyword, title, topic.SecondaryKeyword));
        profile.AiSummary = piped.GeoSummary ?? SeoEditorialWriter.BuildGeoSummary(
            topic.FocusKeyword, topic.SecondaryKeyword, title, products);

        EditorialKeywordSync.ApplyToPost(post, profile);

        await db.SaveChangesAsync(ct);
        return post.Id;
    }

    async Task<List<ProductLink>> ResolveProductLinksAsync(string focus, string secondary, CancellationToken ct)
    {
        var tokens = SeoText.Tokenize(focus).Concat(SeoText.Tokenize(secondary))
            .Where(t => t.Length > 2).Distinct().ToList();

        var products = await db.Products.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .Take(80)
            .ToListAsync(ct);

        return products
            .Select(p => new { p, score = ScoreProduct(p, tokens) })
            .OrderByDescending(x => x.score)
            .Where(x => x.score > 0)
            .Take(4)
            .Select(x => new ProductLink(x.p.Name, x.p.Slug, x.p.ProductCode))
            .ToList();
    }

    static int ScoreProduct(Product p, List<string> tokens)
    {
        var hay = $"{p.Name} {p.ProductType} {p.Applications}";
        return tokens.Sum(t => hay.Contains(t, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
    }

    async Task<bool> TryPublishPostAsync(int postId, DateTime publishUtc, CancellationToken ct)
    {
        var post = await db.BlogPosts.FindAsync([postId], ct);
        if (post == null) return false;
        post.IsPublished = true;
        post.PublishedAt = publishUtc;
        post.UpdatedAt = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(5, 40));
        if (_opt.SyncRankAfterPublish)
            await SyncRankForPostAsync(postId, ct);
        return true;
    }

    async Task SyncRankForPostAsync(int postId, CancellationToken ct)
    {
        var profile = await db.SeoProfiles.FirstOrDefaultAsync(
            p => p.EntityType == "blog" && p.EntityId == postId, ct);
        var post = await db.BlogPosts.FindAsync([postId], ct);
        if (profile == null || post == null || string.IsNullOrWhiteSpace(profile.FocusKeyword)) return;

        var sync = await rankTracker.SyncBlogKeywordAsync(postId, ct);
        if (!sync.ApiConfigured)
            log.LogDebug("Rank sync skipped — SerpAPI not configured");
        else if (sync.PositionsFound > 0)
            log.LogInformation("Rank after publish blog {Id}: {Message}", postId, sync.Message);
    }

    async Task<int> BoostUnderperformingPostsAsync(CancellationToken ct, List<string> lines)
    {
        var threshold = _opt.RepublishBoostScoreThreshold;
        var candidates = await db.BlogPosts
            .Where(b => b.IsPublished)
            .Join(db.SeoProfiles.Where(p => p.EntityType == "blog"),
                b => b.Id, p => p.EntityId, (b, p) => new { b.Id, p.TotalScore, p.IsPublishReady })
            .Where(x => x.TotalScore < threshold || !x.IsPublishReady)
            .OrderBy(x => x.TotalScore)
            .Take(2)
            .Select(x => x.Id)
            .ToListAsync(ct);

        var count = 0;
        foreach (var id in candidates)
        {
            try
            {
                var report = await seo.AutoFixBlogAsync(id, 3, forceRegenerate: false, ct);
                count++;
                lines.Add($"به‌روزرسانی مقاله #{id} → امتیاز {report.OverallScore}");
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Boost failed for blog {Id}", id);
            }
        }

        return count;
    }

    async Task<string> UniqueSlugAsync(string title, CancellationToken ct)
    {
        var baseSlug = SlugHelper.Generate(title);
        var slug = baseSlug;
        var n = 0;
        while (await db.BlogPosts.AnyAsync(b => b.Slug == slug, ct))
            slug = $"{baseSlug}-{++n}";
        return slug;
    }
}
