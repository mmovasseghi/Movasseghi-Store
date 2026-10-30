using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace MovasseghiShop.Web.Controllers;

/// <summary>تصاویر کوچک‌شده برای LCP کارت/هیرو — بدون نیاز به فایل از پیش ساخته روی دیسک.</summary>
[Route("media")]
public sealed class MediaController(IWebHostEnvironment env, ILogger<MediaController> log) : Controller
{
    const int DefaultQuality = 82;

    [HttpGet("{maxEdge:int}/{**assetPath}")]
    public async Task<IActionResult> Resize(int maxEdge, string assetPath, CancellationToken ct)
    {
        maxEdge = Math.Clamp(maxEdge, 64, 960);
        if (string.IsNullOrWhiteSpace(assetPath)
            || assetPath.Contains("..", StringComparison.Ordinal)
            || !assetPath.StartsWith("images/", StringComparison.OrdinalIgnoreCase))
            return NotFound();

        if (string.IsNullOrEmpty(env.WebRootPath))
            return NotFound();

        var relative = assetPath.Replace('/', Path.DirectorySeparatorChar);
        var physical = Path.Combine(env.WebRootPath, relative);
        if (!System.IO.File.Exists(physical))
            return NotFound();

        try
        {
            using var image = await Image.LoadAsync(physical, ct);
            var longest = Math.Max(image.Width, image.Height);
            if (longest > maxEdge)
            {
                var scale = (double)maxEdge / longest;
                image.Mutate(x => x.Resize(
                    (int)Math.Round(image.Width * scale),
                    (int)Math.Round(image.Height * scale)));
            }

            image.Metadata.ExifProfile = null;
            image.Metadata.IptcProfile = null;
            image.Metadata.XmpProfile = null;

            using var ms = new MemoryStream();
            await image.SaveAsWebpAsync(ms, new WebpEncoder { Quality = DefaultQuality }, ct);
            Response.Headers.CacheControl = "public, max-age=2592000, immutable";
            return File(ms.ToArray(), "image/webp");
        }
        catch (Exception ex)
        {
            log.LogDebug(ex, "Resize failed for {Path}", assetPath);
            return NotFound();
        }
    }
}
