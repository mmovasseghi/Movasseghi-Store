using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace MovasseghiShop.Web.Services;

public sealed record ImageOptimizationResult(
    int Scanned,
    int Rewritten,
    int Skipped,
    int Failed,
    long BytesBefore,
    long BytesAfter)
{
    public long BytesSaved => BytesBefore - BytesAfter;
    public double PercentSaved => BytesBefore == 0 ? 0 : Math.Round(100.0 * BytesSaved / BytesBefore, 1);
}

public interface IImageOptimizerService
{
    Task<ImageOptimizationResult> OptimizeAllAsync(bool dryRun = false, CancellationToken ct = default);
}

/// <summary>
/// تصاویر محصولات مستقیم از منبع دانلود می‌شدند و بدون تغییر ابعاد به WebP تبدیل
/// می‌شدند. نتیجه: عکس‌های ۲۱ مگاپیکسلی (چند مگابایت) برای کارتی که ۲۲۰ پیکسل
/// نمایش داده می‌شود. این سرویس هر تصویر بزرگ‌تر از سقف را دوباره با ابعاد
/// منطقی رمزگذاری می‌کند — بزرگ‌ترین عامل کندی LCP در سایت.
/// </summary>
public class ImageOptimizerService(IWebHostEnvironment env, ILogger<ImageOptimizerService> logger)
    : IImageOptimizerService
{
    /// <summary>بزرگ‌ترین ضلع مجاز. صفحه محصول حداکثر ~۷۰۰ پیکسل نشان می‌دهد؛ ۱۴۰۰ برای نمایشگر Retina و زوم کافی است.</summary>
    public const int MaxEdge = 1400;

    public const int WebpQuality = 82;

    /// <summary>زیر این حجم، سود بازفشرده‌سازی ناچیز است و ارزش ریسک افت کیفیت را ندارد.</summary>
    private const long MinBytesToBother = 120 * 1024;

    private static readonly string[] Extensions = [".png", ".jpg", ".jpeg", ".webp"];

    public async Task<ImageOptimizationResult> OptimizeAllAsync(bool dryRun = false, CancellationToken ct = default)
    {
        var root = Path.Combine(env.WebRootPath, "images");
        if (!Directory.Exists(root))
            return new ImageOptimizationResult(0, 0, 0, 0, 0, 0);

        int scanned = 0, rewritten = 0, skipped = 0, failed = 0;
        long before = 0, after = 0;

        var files = Directory
            .EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(f => Extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            scanned++;

            var info = new FileInfo(file);
            try
            {
                if (info.Length < MinBytesToBother)
                {
                    skipped++;
                    continue;
                }

                using var image = await Image.LoadAsync(file, ct);
                var longest = Math.Max(image.Width, image.Height);
                var oversized = longest > MaxEdge;

                // فایل کوچک‌مقیاس ولی حجیم هم ارزش بازفشرده‌سازی دارد (مثلاً PNG بدون فشرده‌سازی)
                if (!oversized && info.Length < 250 * 1024)
                {
                    skipped++;
                    continue;
                }

                if (oversized)
                {
                    var scale = (double)MaxEdge / longest;
                    image.Mutate(x => x.Resize(
                        (int)Math.Round(image.Width * scale),
                        (int)Math.Round(image.Height * scale)));
                }

                // متادیتای دوربین (EXIF/ICC) گاهی ده‌ها کیلوبایت است و در وب مصرفی ندارد
                image.Metadata.ExifProfile = null;
                image.Metadata.IptcProfile = null;
                image.Metadata.XmpProfile = null;

                var target = Path.ChangeExtension(file, ".webp");

                using var buffer = new MemoryStream();
                await image.SaveAsWebpAsync(buffer, new WebpEncoder { Quality = WebpQuality }, ct);

                var existingTargetLength = File.Exists(target) ? new FileInfo(target).Length : long.MaxValue;

                // اگر خروجی جدید از فایل موجود بزرگ‌تر شد، دست نزن
                if (buffer.Length >= existingTargetLength && File.Exists(target))
                {
                    skipped++;
                    continue;
                }

                before += info.Length;
                after += buffer.Length;
                rewritten++;

                // فایل اصلی عمداً حذف نمی‌شود: آدرس ذخیره‌شده در دیتابیس هنوز به آن
                // اشاره می‌کند و PreferWebp فقط هنگام نمایش، مسیر را به WebP برمی‌گرداند.
                // حذف اصل یعنی اگر جایی از این مسیر رد شود، تصویر ۴۰۴ می‌دهد.
                if (!dryRun)
                    await File.WriteAllBytesAsync(target, buffer.ToArray(), ct);
            }
            catch (Exception ex)
            {
                failed++;
                logger.LogWarning(ex, "بهینه‌سازی «{File}» شکست خورد.", info.Name);
            }
        }

        var result = new ImageOptimizationResult(scanned, rewritten, skipped, failed, before, after);
        logger.LogInformation(
            "بهینه‌سازی تصاویر: {Scanned} فایل بررسی شد، {Rewritten} بازنویسی، {Saved} کیلوبایت صرفه‌جویی ({Percent}٪).",
            result.Scanned, result.Rewritten, result.BytesSaved / 1024, result.PercentSaved);

        return result;
    }
}
