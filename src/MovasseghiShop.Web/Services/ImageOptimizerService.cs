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

/// <summary>Builds display derivatives without overwriting or deleting uploaded originals.</summary>
public class ImageOptimizerService(IWebHostEnvironment env, ILogger<ImageOptimizerService> logger)
    : IImageOptimizerService
{
    public const int MaxEdge = 1400;
    public const int LcpMaxEdge = 256;
    public const int WebpQuality = 82; // retained for existing callers
    public const int PhotoQuality = 94;
    static readonly string[] Extensions = [".png", ".jpg", ".jpeg", ".webp"];

    public async Task<ImageOptimizationResult> OptimizeAllAsync(bool dryRun = false, CancellationToken ct = default)
    {
        var webRoot = !string.IsNullOrEmpty(env.WebRootPath) ? env.WebRootPath : Path.Combine(env.ContentRootPath, "wwwroot");
        var root = Path.Combine(webRoot, "images");
        if (!Directory.Exists(root)) return new(0, 0, 0, 0, 0, 0);
        var files = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(f => Extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .Where(f => !System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileNameWithoutExtension(f), @"(?:-lcp|-\d+w)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            .Where(f => f.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) || !File.Exists(Path.ChangeExtension(f, ".webp")))
            .Where(f => !Path.GetFileNameWithoutExtension(f).EndsWith("-original", StringComparison.OrdinalIgnoreCase)
                || !File.Exists(Path.Combine(Path.GetDirectoryName(f)!, Path.GetFileNameWithoutExtension(f)[..^"-original".Length] + ".webp")))
            .ToArray();
        int changed = 0, skipped = 0, failed = 0;
        long before = 0, after = 0;
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var source = await Image.LoadAsync(file, ct);
                // Never flatten an animated upload into a static thumbnail.
                if (source.Frames.Count > 1) { skipped++; continue; }
                source.Mutate(x => x.AutoOrient());
                if (dryRun) { skipped++; continue; }
                var written = false;
                var display = file;
                if (!file.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                {
                    var converted = Path.ChangeExtension(file, ".webp");
                    using var buffer = new MemoryStream();
                    await source.SaveAsWebpAsync(buffer, Lossless(), ct);
                    // A new lossless format is useful only if it is smaller.
                    // Existing files are never overwritten by format conversion.
                    if (!File.Exists(converted) && buffer.Length < new FileInfo(file).Length)
                    {
                        await AtomicWriteAsync(converted, buffer.ToArray(), ct);
                        before += new FileInfo(file).Length;
                        after += buffer.Length;
                        written = true;
                        display = converted;
                    }
                }
                var stem = ResponsiveImageService.VariantStem(display);
                var photographic = ResponsiveImageService.IsPhotographic(file);
                foreach (var width in ResponsiveImageService.Widths)
                {
                    if (width >= source.Width) continue;
                    var height = Math.Max(1, (int)Math.Round((double)source.Height * width / source.Width));
                    written |= await WriteVariantAsync(source, display, $"{stem}-{width}w.webp", width, height, ct, photographic: photographic);
                }
                if (file.Contains($"{Path.DirectorySeparatorChar}products{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                {
                    var scale = Math.Min(1, (double)LcpMaxEdge / Math.Max(source.Width, source.Height));
                    written |= await WriteVariantAsync(source, display, Path.Combine(Path.GetDirectoryName(display)!, Path.GetFileNameWithoutExtension(display)) + "-lcp.webp",
                        Math.Max(1, (int)Math.Round(source.Width * scale)), Math.Max(1, (int)Math.Round(source.Height * scale)), ct, allowLarger: true);
                }
                if (written) changed++; else skipped++;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception error) { failed++; logger.LogWarning(error, "Image derivatives failed for {File}", Path.GetFileName(file)); }
        }
        logger.LogInformation("Image derivatives: {Scanned} sources, {Changed} updated, {Failed} failed; original files preserved", files.Length, changed, failed);
        return new(files.Length, changed, skipped, failed, before, after);
    }

    static WebpEncoder Lossless() => new() { FileFormat = WebpFileFormatType.Lossless, TransparentColorMode = WebpTransparentColorMode.Preserve, Method = WebpEncodingMethod.Level1 };

    static async Task<bool> WriteVariantAsync(Image source, string original, string target, int width, int height, CancellationToken ct, bool allowLarger = false, bool photographic = false)
    {
        if (File.Exists(target) && File.GetLastWriteTimeUtc(target) >= File.GetLastWriteTimeUtc(original))
        {
            var old = Image.Identify(target);
            if (old.Width == width && old.Height == height) return false;
        }
        using var output = source.Clone(x => x.Resize(width, height));
        using var buffer = new MemoryStream();
        // Transparent studio shots and branding remain lossless. JPEG photographs
        // get a high-quality display derivative; the full JPEG remains untouched.
        var encoder = photographic
            ? new WebpEncoder { FileFormat = WebpFileFormatType.Lossy, Quality = PhotoQuality, Method = WebpEncodingMethod.Level4 }
            : Lossless();
        await output.SaveAsWebpAsync(buffer, encoder, ct);
        if (!allowLarger && buffer.Length >= new FileInfo(original).Length) return false;
        await AtomicWriteAsync(target, buffer.ToArray(), ct);
        return true;
    }

    static async Task AtomicWriteAsync(string path, byte[] bytes, CancellationToken ct)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temporary, bytes, ct);
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
