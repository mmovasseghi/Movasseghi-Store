using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;

namespace MovasseghiShop.Web.Services;

public sealed record ResponsiveImageData(string SrcSet, int Width, int Height);

/// <summary>Reads prebuilt derivatives only; never decodes/resizes images on a page request.</summary>
public sealed class ResponsiveImageService(IWebHostEnvironment env, IFileVersionProvider versions)
{
    public const string HeroSizes = "(min-width: 768px) 420px, (max-width: 480px) 35vw, 240px";
    public const int PackedWidth = 420;
    public static string PackedVariant(string path) => VariantStem(path) + $"-packed-v1-{PackedWidth}w.webp";
    public static readonly int[] Widths = [64, 128, 160, 192, 256, 272, 320, 384, 420, 480, 560, 640, 960, 1400];
    // Separate names make existing lossless derivatives safe to keep while
    // deploying the photographic encoding policy. Originals are never replaced.
    public static bool IsPhotographic(string path) => Path.GetExtension(path).Equals(".jpg", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(path).Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
    public static string VariantStem(string path) => path[..path.LastIndexOf('.')]
        + (IsPhotographic(path) ? "-photo-v1" : "");
    readonly ConcurrentDictionary<string, (long Stamp, long Length, int Width, int Height)> dimensions = new();

    public ResponsiveImageData? Get(string? url, HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrEmpty(env.WebRootPath)) return null;
        var clean = url.Split('?', '#')[0];
        if (clean.StartsWith("~/")) clean = clean[1..];
        var pathBase = context.Request.PathBase.Value;
        if (!string.IsNullOrEmpty(pathBase) && clean.StartsWith(pathBase + "/", StringComparison.OrdinalIgnoreCase)) clean = clean[pathBase.Length..];
        if (!clean.StartsWith("/images/", StringComparison.OrdinalIgnoreCase) || clean.Contains("..")) return null;
        var root = Path.GetFullPath(env.WebRootPath);
        string Physical(string path) => Path.Combine(root, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        var file = Physical(clean);
        if (!File.Exists(file)) return null;
        var legacyHero = clean.EndsWith("-lcp.webp", StringComparison.OrdinalIgnoreCase);
        if (legacyHero)
        {
            var legacyStem = clean[..^"-lcp.webp".Length];
            var original = new[] { ".webp", ".png", ".jpg", ".jpeg" }.Select(extension => legacyStem + extension).FirstOrDefault(path => File.Exists(Physical(path)));
            if (original is not null) { clean = original; file = Physical(clean); }
        }
        var info = new FileInfo(file);
        var stamp = info.LastWriteTimeUtc.Ticks;
        if (!dimensions.TryGetValue(file, out var size) || size.Stamp != stamp || size.Length != info.Length)
        {
            try
            {
                var identified = Image.Identify(file);
                if (identified.FrameMetadataCollection.Count > 1) return null;
                var width = identified.Width;
                var height = identified.Height;
                if (identified.Metadata.ExifProfile?.TryGetValue(ExifTag.Orientation, out var orientation) == true && orientation.Value is >= 5 and <= 8)
                    (width, height) = (height, width);
                size = (stamp, info.Length, width, height);
                dimensions[file] = size;
            }
            catch (UnknownImageFormatException) { return null; }
            catch (InvalidImageContentException) { return null; }
            catch (IOException) { return null; }
        }
        var dot = clean.LastIndexOf('.');
        if (dot < clean.LastIndexOf('/')) return null;
        var stem = VariantStem(clean);
        var candidates = new List<string>();
        string Version(string path)
        {
            var versioned = versions.AddFileVersionToPath(context.Request.PathBase, AppPath.H(path, context));
            var query = versioned.IndexOf('?');
            var resource = query < 0 ? versioned : versioned[..query];
            // srcset uses whitespace and commas as separators, unlike img src.
            return string.Join("/", resource.Split('/').Select(Uri.EscapeDataString)) + (query < 0 ? "" : versioned[query..]);
        }
        foreach (var width in Widths)
        {
            if (width >= size.Width) continue;
            var normal = $"{stem}-{width}w.webp";
            var paths = width == PackedWidth && !IsPhotographic(clean)
                ? new[] { normal, PackedVariant(clean) } : new[] { normal };
            string? best = null;
            var bestLength = info.Length;
            foreach (var candidate in paths)
            {
                try
                {
                    var candidateFile = new FileInfo(Physical(candidate));
                    // Keep the ordinary URL on ties, and reject stale files.
                    if (candidateFile.Exists && candidateFile.Length < bestLength && candidateFile.LastWriteTimeUtc.Ticks >= stamp)
                    {
                        best = candidate;
                        bestLength = candidateFile.Length;
                    }
                }
                catch (IOException) { /* An upload/cleanup raced with this render. */ }
            }
            if (best is not null) candidates.Add($"{Version(best)} {width}w");
        }
        if (!candidates.Any() && !legacyHero) return null;
        candidates.Add($"{Version(clean)} {size.Width}w");
        return new(string.Join(", ", candidates), size.Width, size.Height);
    }
}
