using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using MovasseghiShop.Web.Services;
using MovasseghiShop.Web.Models.Entities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace MovasseghiShop.Web.Tests;

public sealed class ResponsiveImageTests : IDisposable
{
    readonly string root = Path.Combine(Path.GetTempPath(), "responsive-images-" + Guid.NewGuid());
    TestEnvironment Environment => new() { WebRootPath = root };
    ImageOptimizerService Optimizer => new(Environment, NullLogger<ImageOptimizerService>.Instance);
    string Folder => Path.Combine(root, "images", "products", "test");
    async Task<string> Source(int width = 800, int height = 600)
    {
        Directory.CreateDirectory(Folder);
        var path = Path.Combine(Folder, "01.webp");
        using var image = new Image<Rgba32>(width, height, Color.CornflowerBlue);
        await image.SaveAsWebpAsync(path, new WebpEncoder { FileFormat = WebpFileFormatType.Lossless });
        return path;
    }

    [Fact]
    public async Task Derivatives_preserve_originals_match_declared_widths_and_are_not_recursively_processed()
    {
        var source = await Source();
        var original = await File.ReadAllBytesAsync(source);
        var result = await Optimizer.OptimizeAllAsync();
        Assert.Equal(0, result.Failed);
        Assert.Equal(original, await File.ReadAllBytesAsync(source));
        foreach (var width in ResponsiveImageService.Widths.Where(w => w < 800))
        {
            var derivative = Path.Combine(Folder, $"01-{width}w.webp");
            if (!File.Exists(derivative)) continue; // Larger-than-source encodings are intentionally omitted.
            Assert.True(new FileInfo(derivative).Length < original.Length);
            var info = Image.Identify(derivative);
            Assert.Equal(width, info.Width);
            Assert.Equal((int)Math.Round(width * 600d / 800), info.Height);
        }
        var snapshot = Directory.GetFiles(Folder).ToDictionary(p => p, p => File.GetLastWriteTimeUtc(p));
        var repeat = await Optimizer.OptimizeAllAsync();
        Assert.Equal(1, repeat.Scanned);
        Assert.Equal(0, repeat.Rewritten);
        Assert.Equal(snapshot.Count, Directory.GetFiles(Folder).Length);
        foreach (var item in snapshot) Assert.Equal(item.Value, File.GetLastWriteTimeUtc(item.Key));
    }

    [Fact]
    public async Task New_lossless_logo_format_preserves_decoded_pixels_including_transparency()
    {
        var folder = Path.Combine(root, "images", "brand");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "logo.png");
        using var original = new Image<Rgba32>(310, 114, new Rgba32(20, 70, 90, 128));
        original[0, 0] = new Rgba32(50, 70, 90, 0);
        await original.SaveAsPngAsync(path, new SixLabors.ImageSharp.Formats.Png.PngEncoder { TransparentColorMode = SixLabors.ImageSharp.Formats.Png.PngTransparentColorMode.Preserve });
        var bytes = await File.ReadAllBytesAsync(path);
        Assert.Equal(0, (await Optimizer.OptimizeAllAsync()).Failed);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(path));
        using var converted = await Image.LoadAsync<Rgba32>(Path.ChangeExtension(path, ".webp"));
        Assert.Equal(original.Size, converted.Size);
        for (var y = 0; y < original.Height; y++) for (var x = 0; x < original.Width; x++) Assert.Equal(original[x, y], converted[x, y]);
    }

    [Fact]
    public async Task Newly_uploaded_original_never_uses_stale_derivatives()
    {
        var path = await Source();
        await Optimizer.OptimizeAllAsync();
        var service = new ResponsiveImageService(Environment, new Versions());
        Assert.Contains("64w", service.Get("/images/products/test/01.webp", new DefaultHttpContext())!.SrcSet);
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddHours(1));
        Assert.Null(service.Get("/images/products/test/01.webp", new DefaultHttpContext()));
        Assert.Null(service.Get("https://outside.example/image.webp", new DefaultHttpContext()));
        Assert.Null(service.Get("/images/../secret.webp", new DefaultHttpContext()));
    }

    [Fact]
    public async Task Legacy_hero_preload_resolves_same_full_quality_candidates_with_pathbase()
    {
        await Source();
        await Optimizer.OptimizeAllAsync();
        var context = new DefaultHttpContext();
        context.Request.PathBase = "/store";
        var service = new ResponsiveImageService(Environment, new Versions());
        var image = service.Get("/store/images/products/test/01-lcp.webp", context)!;
        Assert.Equal(800, image.Width);
        Assert.Equal(600, image.Height);
        Assert.Contains("/store/images/products/test/01-64w.webp?v=test 64w", image.SrcSet);
        Assert.Contains("/store/images/products/test/01.webp?v=test 800w", image.SrcSet);
    }

    [Fact]
    public async Task Hero_notices_new_derivatives_and_never_reuses_them_after_an_upload()
    {
        var path = await Source();
        var image = new ProductImage { Url = "/images/products/test/01.webp" };
        Assert.Equal(image.Url, ProductImageHelper.GetLcpDisplayUrl(image, Environment));
        await Optimizer.OptimizeAllAsync();
        Assert.EndsWith("01-lcp.webp", ProductImageHelper.GetLcpDisplayUrl(image, Environment));
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddHours(1));
        Assert.Equal(image.Url, ProductImageHelper.GetLcpDisplayUrl(image, Environment));
    }

    [Fact]
    public async Task Animated_webp_upload_is_not_flattened_or_changed()
    {
        Directory.CreateDirectory(Folder);
        var path = Path.Combine(Folder, "01.webp");
        using (var image = new Image<Rgba32>(128, 128, Color.Red))
        {
            image.Frames.AddFrame(image.Frames.RootFrame);
            await image.SaveAsWebpAsync(path, new WebpEncoder { FileFormat = WebpFileFormatType.Lossless });
        }
        var original = await File.ReadAllBytesAsync(path);
        var result = await Optimizer.OptimizeAllAsync();
        Assert.Equal(0, result.Failed);
        Assert.Equal(original, await File.ReadAllBytesAsync(path));
        Assert.Single(Directory.GetFiles(Folder));
    }

    [Fact]
    public async Task Archived_original_is_preserved_but_not_duplicated_when_the_display_webp_exists()
    {
        await Source();
        var archive = Path.Combine(Folder, "01-original.png");
        using (var image = new Image<Rgba32>(800, 600, Color.Red)) await image.SaveAsPngAsync(archive);
        var original = await File.ReadAllBytesAsync(archive);
        var result = await Optimizer.OptimizeAllAsync();
        Assert.Equal(1, result.Scanned);
        Assert.Equal(original, await File.ReadAllBytesAsync(archive));
        Assert.Empty(Directory.GetFiles(Folder, "01-original-*w.webp"));
    }

    [Fact]
    public async Task Uploaded_names_with_spaces_and_commas_remain_valid_srcset_urls()
    {
        var source = await Source();
        var renamed = Path.Combine(Folder, "blue photo,1.webp");
        File.Move(source, renamed);
        await Optimizer.OptimizeAllAsync();
        var data = new ResponsiveImageService(Environment, new Versions()).Get("/images/products/test/blue photo,1.webp", new DefaultHttpContext())!;
        Assert.Contains("blue%20photo%2C1-64w.webp?v=test 64w", data.SrcSet);
        Assert.DoesNotContain("blue photo", data.SrcSet);
    }

    [Fact]
    public async Task Camera_orientation_uses_the_displayed_width_in_the_candidate_list()
    {
        Directory.CreateDirectory(Folder);
        var file = Path.Combine(Folder, "camera.jpg");
        using (var image = new Image<Rgba32>(128, 256, Color.Green))
        {
            var random = new Random(42);
            for (var y = 0; y < image.Height; y++) for (var x = 0; x < image.Width; x++)
                image[x, y] = new Rgba32((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));
            image.Metadata.ExifProfile = new ExifProfile();
            image.Metadata.ExifProfile.SetValue(ExifTag.Orientation, (ushort)6);
            await image.SaveAsJpegAsync(file);
        }
        await Optimizer.OptimizeAllAsync();
        var data = new ResponsiveImageService(Environment, new Versions()).Get("/images/products/test/camera.jpg", new DefaultHttpContext())!;
        Assert.Equal(256, data.Width);
        Assert.Equal(128, data.Height);
    }

    [Fact]
    public async Task Very_wide_image_does_not_produce_zero_height_derivatives()
    {
        await Source(900, 1);
        Assert.Equal(0, (await Optimizer.OptimizeAllAsync()).Failed);
        Assert.Equal(1, Image.Identify(Path.Combine(Folder, "01-lcp.webp")).Height);
    }

    [Fact]
    public async Task Photograph_candidates_are_smaller_modern_files_without_replacing_the_jpeg()
    {
        Directory.CreateDirectory(Folder);
        var path = Path.Combine(Folder, "01-hero.jpg");
        using (var image = new Image<Rgba32>(800, 600))
        {
            var random = new Random(17);
            for (var y = 0; y < image.Height; y++) for (var x = 0; x < image.Width; x++)
                image[x, y] = new Rgba32((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));
            await image.SaveAsJpegAsync(path);
        }
        var original = await File.ReadAllBytesAsync(path);
        Assert.Equal(0, (await Optimizer.OptimizeAllAsync()).Failed);
        Assert.Equal(original, await File.ReadAllBytesAsync(path));
        var candidate = Path.Combine(Folder, "01-hero-photo-v1-256w.webp");
        Assert.True(new FileInfo(candidate).Length < original.Length);
        Assert.Equal(256, Image.Identify(candidate).Width);
        var data = new ResponsiveImageService(Environment, new Versions()).Get("/images/products/test/01-hero.jpg", new DefaultHttpContext())!;
        Assert.Contains("01-hero-photo-v1-256w.webp", data.SrcSet);
        Assert.Contains("01-hero.jpg?v=test 800w", data.SrcSet);
    }

    [Fact]
    public async Task Scene_keeps_its_own_format_alias_and_sees_new_files_after_a_negative_lookup()
    {
        await Source();
        var path = Path.Combine(Folder, "01-hero.png");
        using (var image = new Image<Rgba32>(128, 128, Color.Blue)) await image.SaveAsPngAsync(path);
        var url = "/images/products/test/01-hero.png";
        Assert.Equal(url, ProductImageHelper.PreferWebp(url, Environment));
        var alias = Path.ChangeExtension(path, ".webp");
        using (var image = new Image<Rgba32>(128, 128, Color.Blue)) await image.SaveAsWebpAsync(alias);
        Assert.Equal("/images/products/test/01-hero.webp", ProductImageHelper.PreferWebp(url, Environment));
        var slides = ProductImageHelper.ResolveDualCardSlides(new[] {
            new ProductImage { Id = 1, Url = "/images/products/test/01.webp", IsPrimary = true, Role = MovasseghiShop.Web.Models.Enums.ProductImageRole.Studio },
            new ProductImage { Id = 2, Url = url, Role = MovasseghiShop.Web.Models.Enums.ProductImageRole.Scene }
        }, Environment, "محصول");
        Assert.True(slides.CanCycle);
        Assert.Equal("/images/products/test/01-hero.webp", slides.SceneUrl);
        File.Delete(alias);
        Assert.Equal(url, ProductImageHelper.PreferWebp(url, Environment));
    }

    [Fact]
    public async Task Packed_variant_preserves_every_pixel_and_chooses_the_smallest_fresh_file()
    {
        Directory.CreateDirectory(Folder);
        var path = Path.Combine(Folder, "plate.webp");
        using (var image = new Image<Rgba32>(800, 600))
        {
            var random = new Random(23);
            for (var y = 0; y < image.Height; y++) for (var x = 0; x < image.Width; x++)
                image[x, y] = new Rgba32((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));
            await image.SaveAsWebpAsync(path, new WebpEncoder { FileFormat = WebpFileFormatType.Lossless, TransparentColorMode = WebpTransparentColorMode.Preserve });
        }
        var original = await File.ReadAllBytesAsync(path);
        Assert.Equal(0, (await Optimizer.OptimizeAllAsync()).Failed);
        Assert.Equal(original, await File.ReadAllBytesAsync(path));
        var normal = Path.Combine(Folder, "plate-420w.webp");
        var packed = Path.Combine(Folder, "plate-packed-v1-420w.webp");
        using (var a = await Image.LoadAsync<Rgba32>(normal))
        using (var b = await Image.LoadAsync<Rgba32>(packed))
        {
            Assert.Equal(a.Size, b.Size);
            var pixelsA = new Rgba32[a.Width * a.Height];
            var pixelsB = new Rgba32[pixelsA.Length];
            a.CopyPixelDataTo(pixelsA); b.CopyPixelDataTo(pixelsB);
            Assert.Equal(pixelsA, pixelsB); // Includes RGB under transparent pixels.
            using var rejectedDisplay = await Image.LoadAsync<Rgba32>(Path.Combine(Folder, "plate-display-v2-420w.webp"));
            var fallback = new Rgba32[pixelsA.Length];
            rejectedDisplay.CopyPixelDataTo(fallback);
            Assert.Equal(pixelsA, fallback); // Noisy lossy output must fall back to exact lossless pixels.
        }
        var service = new ResponsiveImageService(Environment, new Versions());
        var context = new DefaultHttpContext();
        var smaller = new FileInfo(packed).Length < new FileInfo(normal).Length ? "plate-packed-v1-420w.webp" : "plate-420w.webp";
        var data = service.Get("/images/products/test/plate.webp", context)!;
        Assert.Contains(smaller + "?v=test 420w", data.SrcSet);
        Assert.Single(data.SrcSet.Split(", ").Where(item => item.EndsWith(" 420w")));
        File.Delete(packed);
        Assert.Contains("plate-420w.webp?v=test 420w", service.Get("/images/products/test/plate.webp", context)!.SrcSet);
        var repeat = await Optimizer.OptimizeAllAsync();
        Assert.Equal(0, repeat.Failed);
        var stamp = File.GetLastWriteTimeUtc(packed);
        Assert.Equal(0, (await Optimizer.OptimizeAllAsync()).Rewritten);
        Assert.Equal(stamp, File.GetLastWriteTimeUtc(packed));
    }

    [Fact]
    public async Task Studio_display_preserves_alpha_and_bounds_visible_error_without_replacing_originals()
    {
        Directory.CreateDirectory(Folder);
        var path = Path.Combine(Folder, "studio.webp");
        using (var source = new Image<Rgba32>(800, 800))
        {
            var random = new Random(17);
            for (var y = 0; y < 800; y++) for (var x = 0; x < 800; x++)
            {
                var radius = Math.Sqrt(Math.Pow((x - 400) / 330d, 2) + Math.Pow((y - 400) / 280d, 2));
                var alpha = (byte)Math.Clamp((1.01 - radius) * 25500, 0, 255);
                var gray = (byte)(220 + x / 40 + random.Next(12));
                source[x, y] = new Rgba32(gray, gray, gray, alpha);
            }
            await source.SaveAsWebpAsync(path, new WebpEncoder { FileFormat = WebpFileFormatType.Lossless, TransparentColorMode = WebpTransparentColorMode.Preserve });
        }
        var original = await File.ReadAllBytesAsync(path);
        Assert.Equal(0, (await Optimizer.OptimizeAllAsync()).Failed);
        Assert.Equal(original, await File.ReadAllBytesAsync(path));
        var packed = Path.Combine(Folder, "studio-packed-v1-420w.webp");
        var display = Path.Combine(Folder, "studio-display-v2-420w.webp");
        Assert.True(new FileInfo(display).Length < new FileInfo(packed).Length);
        using var reference = await Image.LoadAsync<Rgba32>(packed);
        using var actual = await Image.LoadAsync<Rgba32>(display);
        Assert.Equal(reference.Size, actual.Size);
        long error = 0, opacity = 0;
        for (var y = 0; y < reference.Height; y++) for (var x = 0; x < reference.Width; x++)
        {
            var a = reference[x, y]; var b = actual[x, y];
            Assert.Equal(a.A, b.A);
            foreach (var difference in new[] { Math.Abs(a.R - b.R), Math.Abs(a.G - b.G), Math.Abs(a.B - b.B) })
            {
                Assert.True(difference * a.A <= 16 * 255);
                error += difference * a.A;
            }
            opacity += a.A;
        }
        Assert.True(error > 0); // A real quality-100 display derivative, not the fallback.
        Assert.True(error <= opacity * 3);
        var selected = new ResponsiveImageService(Environment, new Versions()).Get("/images/products/test/studio.webp", new DefaultHttpContext())!;
        Assert.Contains("studio-display-v2-420w.webp?v=test 420w", selected.SrcSet);
    }

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
    sealed class Versions : IFileVersionProvider { public string AddFileVersionToPath(PathString pathBase, string path) => path + "?v=test"; }
    sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
