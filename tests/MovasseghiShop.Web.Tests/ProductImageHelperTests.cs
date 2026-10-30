using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using MovasseghiShop.Web;
using Xunit;

namespace MovasseghiShop.Web.Tests;

public class ProductImageHelperTests : IDisposable
{
    readonly string root = Path.Combine(Path.GetTempPath(), "movasseghi-helper-" + Guid.NewGuid());

    public ProductImageHelperTests()
    {
        Directory.CreateDirectory(Path.Combine(root, "images", "products", "0079"));
    }

    [Fact]
    public void PreferLcpVariant_returns_lcp_file_when_present()
    {
        File.WriteAllText(Path.Combine(root, "images", "products", "0079", "01.webp"), "full");
        File.WriteAllText(Path.Combine(root, "images", "products", "0079", "01-lcp.webp"), "small");
        var env = new TestEnvironment { WebRootPath = root };

        var sized = ProductImageHelper.PreferLcpVariant("/images/products/0079/01.webp", env);

        Assert.Equal("/images/products/0079/01-lcp.webp", sized);
    }

    [Fact]
    public void PreferLcpVariant_keeps_display_url_when_lcp_missing()
    {
        File.WriteAllText(Path.Combine(root, "images", "products", "0079", "01.webp"), "full");
        var env = new TestEnvironment { WebRootPath = root };

        var sized = ProductImageHelper.PreferLcpVariant("/images/products/0079/01.webp", env);

        Assert.Equal("/images/products/0079/01.webp", sized);
    }

    [Fact]
    public void PreferLcpVariant_does_not_double_suffix()
    {
        File.WriteAllText(Path.Combine(root, "images", "products", "0079", "01-lcp.webp"), "small");
        var env = new TestEnvironment { WebRootPath = root };

        var sized = ProductImageHelper.PreferLcpVariant("/images/products/0079/01-lcp.webp", env);

        Assert.Equal("/images/products/0079/01-lcp.webp", sized);
    }

    [Fact]
    public void ResolveCardStageUrls_keeps_dual_when_studio_and_scene_differ()
    {
        var dir = Path.Combine(root, "images", "products", "0079");
        File.WriteAllText(Path.Combine(dir, "01.webp"), "studio");
        File.WriteAllText(Path.Combine(dir, "01-lcp.webp"), "studio-lcp");
        File.WriteAllText(Path.Combine(dir, "01-hero.webp"), "scene");
        File.WriteAllText(Path.Combine(dir, "01-hero-lcp.webp"), "scene-lcp");
        var env = new TestEnvironment { WebRootPath = root };
        var slides = new ProductDualCardSlides(
            "/images/products/0079/01.webp",
            "/images/products/0079/01-hero.webp",
            "studio",
            "scene",
            true);

        var (studio, scene, canDual) = ProductImageHelper.ResolveCardStageUrls(slides, env, preferCardSized: true);

        Assert.True(canDual);
        Assert.Equal("/images/products/0079/01-lcp.webp", studio);
        Assert.Equal("/images/products/0079/01-hero-lcp.webp", scene);
    }

    [Fact]
    public void ResolveCardStageUrls_does_not_collapse_scene_onto_studio_lcp()
    {
        var dir = Path.Combine(root, "images", "products", "0079");
        File.WriteAllText(Path.Combine(dir, "01.webp"), "studio");
        File.WriteAllText(Path.Combine(dir, "01-lcp.webp"), "studio-lcp");
        // Scene has no dedicated LCP; PreferLcpVariant would wrongly map a same-stem collision
        // only if URLs equal — keep original scene when sized equals studio.
        var env = new TestEnvironment { WebRootPath = root };
        var slides = new ProductDualCardSlides(
            "/images/products/0079/01.webp",
            "/images/products/0079/01.webp",
            "studio",
            "scene",
            true);

        var (studio, scene, canDual) = ProductImageHelper.ResolveCardStageUrls(slides, env, preferCardSized: true);

        Assert.False(canDual);
        Assert.Equal("/images/products/0079/01-lcp.webp", studio);
        Assert.Null(scene);
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }

    sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "HelperTests";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
