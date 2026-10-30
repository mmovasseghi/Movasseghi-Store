using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using MovasseghiShop.Web.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace MovasseghiShop.Web.Tests;

public class ImageOptimizerTests : IDisposable
{
    readonly string root = Path.Combine(Path.GetTempPath(), "movasseghi-image-" + Guid.NewGuid());

    ImageOptimizerService CreateOptimizer() => new(new TestEnvironment { WebRootPath = root },
        NullLogger<ImageOptimizerService>.Instance);

    [Theory]
    [InlineData(".webp")]
    [InlineData(".png")]
    public async Task Small_compressed_product_image_gets_a_hero_variant_without_changing_the_source(string extension)
    {
        var folder = Path.Combine(root, "images", "products", "test");
        Directory.CreateDirectory(folder);
        var source = Path.Combine(folder, "01" + extension);
        using (var image = new Image<Rgba32>(512, 384, Color.ForestGreen)) await image.SaveAsync(source);
        var original = await File.ReadAllBytesAsync(source);
        Assert.True(original.Length < 120 * 1024);
        var service = CreateOptimizer();

        var result = await service.OptimizeAllAsync();
        var hero = Path.Combine(folder, "01-lcp.webp");
        Assert.Equal(0, result.Failed);
        using (var image = await Image.LoadAsync(hero))
        {
            Assert.Equal(256, image.Width);
            Assert.Equal(192, image.Height);
        }
        Assert.Equal(original, await File.ReadAllBytesAsync(source));
        var heroBytes = await File.ReadAllBytesAsync(hero);
        var repeat = await service.OptimizeAllAsync();
        Assert.Equal(1, repeat.Scanned);
        Assert.Equal(heroBytes, await File.ReadAllBytesAsync(hero));
        Assert.False(File.Exists(Path.Combine(folder, "01-lcp-lcp.webp")));
    }

    [Fact]
    public async Task Existing_display_webp_is_preferred_over_the_original_png_for_the_hero()
    {
        var folder = Path.Combine(root, "images", "products", "test");
        Directory.CreateDirectory(folder);
        using (var image = new Image<Rgba32>(512, 384, Color.Red)) await image.SaveAsync(Path.Combine(folder, "01.png"));
        using (var image = new Image<Rgba32>(512, 384, Color.Blue)) await image.SaveAsync(Path.Combine(folder, "01.webp"));
        var result = await CreateOptimizer().OptimizeAllAsync();
        Assert.Equal(1, result.Scanned);
        using var hero = await Image.LoadAsync<Rgba32>(Path.Combine(folder, "01-lcp.webp"));
        Assert.True(hero[0, 0].B > hero[0, 0].R);
    }

    [Fact]
    public async Task Changed_source_replaces_an_outdated_hero_even_if_the_old_hero_was_smaller()
    {
        var folder = Path.Combine(root, "images", "products", "test");
        Directory.CreateDirectory(folder);
        var source = Path.Combine(folder, "01.webp");
        var hero = Path.Combine(folder, "01-lcp.webp");
        using (var image = new Image<Rgba32>(512, 384, Color.Red)) await image.SaveAsync(source);
        var service = CreateOptimizer();
        await service.OptimizeAllAsync();
        using (var image = new Image<Rgba32>(512, 384, Color.Blue)) await image.SaveAsync(source);
        File.SetLastWriteTimeUtc(hero, DateTime.UtcNow.AddMinutes(-2));

        await service.OptimizeAllAsync();
        using var updated = await Image.LoadAsync<Rgba32>(hero);
        Assert.True(updated[0, 0].B > updated[0, 0].R);
    }

    [Fact]
    public async Task Dry_run_preserves_files_and_corrupt_images_are_reported_as_failures()
    {
        var folder = Path.Combine(root, "images", "products", "test");
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "01.webp"), "invalid image");
        var service = CreateOptimizer();
        await service.OptimizeAllAsync(dryRun: true);
        Assert.Single(Directory.GetFiles(folder));
        var result = await service.OptimizeAllAsync();
        Assert.Equal(1, result.Failed);
        Assert.Single(Directory.GetFiles(folder));
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }

    sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "ImageTests";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
