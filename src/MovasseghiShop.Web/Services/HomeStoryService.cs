using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;

// SlugHelper، WholesaleRules، ProductImageHelper
using MovasseghiShop.Web;

namespace MovasseghiShop.Web.Services;

public class HomeStoryService(
    ApplicationDbContext db,
    IProductCatalogService catalog,
    IWebHostEnvironment env) : IHomeStoryService
{
    const string Logo = "/images/brand/originallogo.png";
    static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public async Task<bool> HasManagedStoriesAsync(CancellationToken ct = default)
        => await db.HomeStories.AnyAsync(s => s.IsActive && s.Slides.Any(sl => sl.IsActive), ct);

    public async Task<IReadOnlyList<StoryItemDto>> GetStoriesForHomeAsync(CancellationToken ct = default)
    {
        var managed = await db.HomeStories.AsNoTracking()
            .Include(s => s.Slides.Where(sl => sl.IsActive))
            .Where(s => s.IsActive && s.Slides.Any(sl => sl.IsActive))
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Id)
            .ToListAsync(ct);

        if (managed.Count > 0)
            return managed.Select(MapToDto).ToList();

        return await BuildAutoStoriesAsync(ct);
    }

    public async Task<List<HomeStory>> GetAllForAdminAsync(CancellationToken ct = default)
        => await db.HomeStories
            .Include(s => s.Slides.OrderBy(sl => sl.SortOrder))
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Id)
            .ToListAsync(ct);

    public async Task<HomeStory?> GetForEditAsync(int id, CancellationToken ct = default)
        => await db.HomeStories
            .Include(s => s.Slides.OrderBy(sl => sl.SortOrder))
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<HomeStory> CreateStoryAsync(HomeStory model, CancellationToken ct = default)
    {
        model.StoryKey = NormalizeStoryKey(model.StoryKey, model.Label);
        model.Label = model.Label.Trim();
        model.IconKey = string.IsNullOrWhiteSpace(model.IconKey) ? "all" : model.IconKey.Trim();
        model.UpdatedAt = DateTime.UtcNow;
        db.HomeStories.Add(model);
        await db.SaveChangesAsync(ct);
        return model;
    }

    public async Task UpdateStoryAsync(HomeStory model, CancellationToken ct = default)
    {
        var existing = await db.HomeStories.FindAsync([model.Id], ct);
        if (existing is null) throw new InvalidOperationException("استوری پیدا نشد.");

        existing.StoryKey = NormalizeStoryKey(model.StoryKey, model.Label);
        existing.Label = model.Label.Trim();
        existing.IconKey = string.IsNullOrWhiteSpace(model.IconKey) ? "all" : model.IconKey.Trim();
        existing.SortOrder = model.SortOrder;
        existing.IsActive = model.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteStoryAsync(int id, CancellationToken ct = default)
    {
        var story = await db.HomeStories.Include(s => s.Slides).FirstOrDefaultAsync(s => s.Id == id, ct);
        if (story is null) return;

        db.HomeStorySlides.RemoveRange(story.Slides);
        db.HomeStories.Remove(story);
        await db.SaveChangesAsync(ct);
        TryDeleteStoryFolder(id);
    }

    public async Task<HomeStorySlide> AddSlideAsync(int storyId, HomeStorySlide slide, CancellationToken ct = default)
    {
        slide.HomeStoryId = storyId;
        slide.Title = slide.Title.Trim();
        slide.LinkUrl = string.IsNullOrWhiteSpace(slide.LinkUrl) ? "/Catalog" : slide.LinkUrl.Trim();
        if (string.IsNullOrWhiteSpace(slide.ImageUrl))
            slide.ImageUrl = Logo;

        db.HomeStorySlides.Add(slide);
        await TouchStoryAsync(storyId, ct);
        await db.SaveChangesAsync(ct);
        return slide;
    }

    public async Task UpdateSlideAsync(HomeStorySlide slide, CancellationToken ct = default)
    {
        var existing = await db.HomeStorySlides.FindAsync([slide.Id], ct);
        if (existing is null) throw new InvalidOperationException("اسلاید پیدا نشد.");

        existing.Title = slide.Title.Trim();
        existing.LinkUrl = string.IsNullOrWhiteSpace(slide.LinkUrl) ? "/Catalog" : slide.LinkUrl.Trim();
        existing.SortOrder = slide.SortOrder;
        existing.IsActive = slide.IsActive;
        if (!string.IsNullOrWhiteSpace(slide.ImageUrl))
            existing.ImageUrl = slide.ImageUrl;

        await TouchStoryAsync(existing.HomeStoryId, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteSlideAsync(int slideId, CancellationToken ct = default)
    {
        var slide = await db.HomeStorySlides.FindAsync([slideId], ct);
        if (slide is null) return;

        db.HomeStorySlides.Remove(slide);
        await TouchStoryAsync(slide.HomeStoryId, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<string> SaveCoverImageAsync(int storyId, IFormFile file, CancellationToken ct = default)
    {
        var story = await db.HomeStories.FindAsync([storyId], ct)
            ?? throw new InvalidOperationException("استوری پیدا نشد.");

        var url = await SaveImageAsync(file, $"covers/{storyId:D4}", $"cover");
        story.CoverImageUrl = url;
        story.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return url;
    }

    public async Task<string> SaveSlideImageAsync(int storyId, int slideId, IFormFile file, CancellationToken ct = default)
    {
        var slide = await db.HomeStorySlides.FirstOrDefaultAsync(s => s.Id == slideId && s.HomeStoryId == storyId, ct)
            ?? throw new InvalidOperationException("اسلاید پیدا نشد.");

        var url = await SaveImageAsync(file, $"{storyId:D4}", $"slide-{slideId:D4}");
        slide.ImageUrl = url;
        await TouchStoryAsync(storyId, ct);
        await db.SaveChangesAsync(ct);
        return url;
    }

    public async Task SeedDefaultsIfEmptyAsync(CancellationToken ct = default)
    {
        if (await db.HomeStories.AnyAsync(ct)) return;

        var defaults = new[]
        {
            new HomeStory
            {
                StoryKey = "all",
                Label = "همه محصولات",
                IconKey = "all",
                SortOrder = 0,
                Slides =
                [
                    new HomeStorySlide { Title = "فروشگاه موثقی", LinkUrl = "/Catalog", ImageUrl = Logo, SortOrder = 0 },
                    new HomeStorySlide { Title = "پخش عمده ظروف گیاهی", LinkUrl = "/Catalog", ImageUrl = Logo, SortOrder = 1 }
                ]
            },
            new HomeStory
            {
                StoryKey = "wholesale",
                Label = "پخش عمده",
                IconKey = "wholesale",
                SortOrder = 1,
                Slides =
                [
                    new HomeStorySlide { Title = $"حداقل {WholesaleRules.FormatMinAmount()} تومان", LinkUrl = "/Catalog", ImageUrl = Logo, SortOrder = 0 },
                    new HomeStorySlide { Title = "مشاوره رایگان خرید عمده", LinkUrl = "/Page/Contact", ImageUrl = Logo, SortOrder = 1 },
                    new HomeStorySlide { Title = "ارسال سراسری", LinkUrl = "/Catalog", ImageUrl = Logo, SortOrder = 2 }
                ]
            },
            new HomeStory
            {
                StoryKey = "official",
                Label = "نمایندگی رسمی",
                IconKey = "official",
                SortOrder = 2,
                Slides =
                [
                    new HomeStorySlide { Title = "نمایندگی رسمی آملون", LinkUrl = "/Page/About", ImageUrl = Logo, SortOrder = 0 },
                    new HomeStorySlide { Title = "اصل کارخانه · ۱۰۰٪ گیاهی", LinkUrl = "/Page/About", ImageUrl = Logo, SortOrder = 1 },
                    new HomeStorySlide { Title = "درباره فروشگاه موثقی", LinkUrl = "/Page/About", ImageUrl = Logo, SortOrder = 2 }
                ]
            }
        };

        db.HomeStories.AddRange(defaults);
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> ImportFromProductsAsync(CancellationToken ct = default)
    {
        var auto = await BuildAutoStoriesAsync(ct);
        foreach (var old in await db.HomeStories.Include(s => s.Slides).ToListAsync(ct))
        {
            db.HomeStorySlides.RemoveRange(old.Slides);
            db.HomeStories.Remove(old);
        }
        await db.SaveChangesAsync(ct);

        var order = 0;
        foreach (var item in auto)
        {
            var story = new HomeStory
            {
                StoryKey = item.Id,
                Label = item.Label,
                IconKey = item.IconKey,
                CoverImageUrl = item.CoverImageUrl,
                SortOrder = order++,
                IsActive = true,
                Slides = item.Slides.Select((sl, i) => new HomeStorySlide
                {
                    ImageUrl = sl.Image,
                    Title = sl.Title,
                    LinkUrl = sl.Link,
                    SortOrder = i,
                    IsActive = true
                }).ToList()
            };
            db.HomeStories.Add(story);
        }

        await db.SaveChangesAsync(ct);
        return auto.Count;
    }

    async Task<IReadOnlyList<StoryItemDto>> BuildAutoStoriesAsync(CancellationToken ct)
    {
        var types = await catalog.GetProductTypesAsync(ct);
        var productsWithImages = await db.Products.AsNoTracking()
            .Include(p => p.Images)
            .Where(p => p.IsActive && p.Images.Any())
            .OrderByDescending(p => p.UpdatedAt)
            .Take(80)
            .ToListAsync(ct);

        var storyItems = new List<StoryItemDto>();
        var featuredSlides = ProductSlides(productsWithImages, 4);

        storyItems.Add(new StoryItemDto("all", "همه محصولات", "all",
            MergeSlides(
                new[] { new StorySlideDto(Logo, "فروشگاه موثقی", "/Catalog"), new StorySlideDto(Logo, "پخش عمده ظروف گیاهی", "/Catalog") },
                featuredSlides)));

        storyItems.Add(new StoryItemDto("wholesale", "پخش عمده", "wholesale",
            MergeSlides(
                new[]
                {
                    new StorySlideDto(Logo, $"حداقل {WholesaleRules.FormatMinAmount()} تومان", "/Catalog"),
                    new StorySlideDto(Logo, "مشاوره رایگان خرید عمده", "/Page/Contact"),
                    new StorySlideDto(Logo, "ارسال سراسری", "/Catalog")
                },
                featuredSlides)));

        storyItems.Add(new StoryItemDto("official", "نمایندگی رسمی", "official",
        [
            new(Logo, "نمایندگی رسمی آملون", "/Page/About"),
            new(Logo, "اصل کارخانه · ۱۰۰٪ گیاهی", "/Page/About"),
            new(Logo, "درباره فروشگاه موثقی", "/Page/About")
        ]));

        foreach (var t in types)
        {
            if (storyItems.Count >= 12) break;

            var url = $"/Catalog/ByType?type={Uri.EscapeDataString(t.Type)}";
            var slides = ProductSlides(productsWithImages.Where(p => p.ProductType == t.Type));
            if (slides.Count == 0)
                slides.Add(new StorySlideDto(Logo, t.Type, url));
            else
                slides = MergeSlides([new StorySlideDto(slides[0].Image, t.Type, url)], slides);

            storyItems.Add(new StoryItemDto($"type-{Uri.EscapeDataString(t.Type)}", t.Type, t.Type, slides));
        }

        return storyItems;
    }

    static StoryItemDto MapToDto(HomeStory story)
    {
        var slides = story.Slides
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .Select(s => new StorySlideDto(s.ImageUrl, s.Title, s.LinkUrl))
            .ToList();

        return new StoryItemDto(story.StoryKey, story.Label, story.IconKey, slides, story.CoverImageUrl);
    }

    List<StorySlideDto> ProductSlides(IEnumerable<Product> source, int max = 5)
        => source.Select(SlideFromProduct).Where(s => s != null).Take(max).Cast<StorySlideDto>().ToList();

    StorySlideDto? SlideFromProduct(Product p)
    {
        var img = ProductImageHelper.GetDisplayUrl(ProductImageHelper.GetPrimary(p.Images), env);
        return img == null ? null : new StorySlideDto(img, p.Name, $"/Shop/Product/{p.Slug}");
    }

    static List<StorySlideDto> MergeSlides(params IEnumerable<StorySlideDto>[] groups)
        => groups.SelectMany(g => g).Take(5).ToList();

    async Task TouchStoryAsync(int storyId, CancellationToken ct)
    {
        var story = await db.HomeStories.FindAsync([storyId], ct);
        if (story != null) story.UpdatedAt = DateTime.UtcNow;
    }

    async Task<string> SaveImageAsync(IFormFile file, string subFolder, string baseName)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext)) ext = ".jpg";

        var dir = Path.Combine(env.WebRootPath, "images", "stories", subFolder);
        Directory.CreateDirectory(dir);

        foreach (var old in Directory.GetFiles(dir, $"{baseName}.*"))
            File.Delete(old);

        var fileName = $"{baseName}{ext}";
        var physical = Path.Combine(dir, fileName);
        await using (var stream = File.Create(physical))
            await file.CopyToAsync(stream);

        return $"/images/stories/{subFolder.Replace('\\', '/')}/{fileName}";
    }

    static string NormalizeStoryKey(string? key, string label)
    {
        if (!string.IsNullOrWhiteSpace(key))
            return key.Trim().Replace(' ', '-');

        var slug = SlugHelper.Generate(label);
        return string.IsNullOrWhiteSpace(slug) ? $"story-{Guid.NewGuid():N}"[..12] : slug;
    }

    void TryDeleteStoryFolder(int storyId)
    {
        try
        {
            var dir = Path.Combine(env.WebRootPath, "images", "stories", $"{storyId:D4}");
            if (Directory.Exists(dir)) Directory.Delete(dir, true);

            var coverDir = Path.Combine(env.WebRootPath, "images", "stories", "covers", $"{storyId:D4}");
            if (Directory.Exists(coverDir)) Directory.Delete(coverDir, true);
        }
        catch
        {
            // non-critical
        }
    }
}
