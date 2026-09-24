using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class StoriesAdminController(IHomeStoryService stories) : Controller
{
    public static readonly (string Key, string Label)[] IconOptions =
    [
        ("all", "همه محصولات"),
        ("wholesale", "پخش عمده"),
        ("official", "نمایندگی رسمی"),
        ("کاسه", "کاسه"),
        ("ظرف چندخانه", "ظرف چندخانه"),
        ("لیوان", "لیوان"),
        ("سطل", "سطل"),
        ("ظرف", "ظرف"),
        ("قاشق", "قاشق"),
        ("نی", "نی"),
        ("بشقاب", "بشقاب"),
        ("دیس", "دیس"),
        ("چنگال", "چنگال"),
        ("کیک‌خوری", "کیک‌خوری"),
        ("سایر", "سایر")
    ];

    public async Task<IActionResult> Index()
    {
        var items = await stories.GetAllForAdminAsync();
        return View(items);
    }

    public IActionResult Create() => View("Edit", new HomeStory { IsActive = true });

    public async Task<IActionResult> Edit(int id)
    {
        var story = await stories.GetForEditAsync(id);
        return story is null ? NotFound() : View(story);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(HomeStory model, IFormFile? coverImage)
    {
        if (string.IsNullOrWhiteSpace(model.Label))
            ModelState.AddModelError(nameof(model.Label), "عنوان استوری الزامی است.");

        if (!ModelState.IsValid) return View("Edit", model);

        if (model.Id == 0)
        {
            var created = await stories.CreateStoryAsync(model);
            if (coverImage is { Length: > 0 })
                await stories.SaveCoverImageAsync(created.Id, coverImage);
            TempData["Success"] = "استوری ایجاد شد. حالا اسلایدها را اضافه کنید.";
            return RedirectToAction(nameof(Edit), new { id = created.Id });
        }

        await stories.UpdateStoryAsync(model);
        if (coverImage is { Length: > 0 })
            await stories.SaveCoverImageAsync(model.Id, coverImage);

        TempData["Success"] = "استوری ذخیره شد.";
        return RedirectToAction(nameof(Edit), new { id = model.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> AddSlide(int storyId, string title, string linkUrl, int sortOrder, IFormFile? slideImage)
    {
        var story = await stories.GetForEditAsync(storyId);
        if (story is null) return NotFound();

        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["Error"] = "عنوان اسلاید الزامی است.";
            return RedirectToAction(nameof(Edit), new { id = storyId });
        }

        var slide = new HomeStorySlide
        {
            Title = title.Trim(),
            LinkUrl = string.IsNullOrWhiteSpace(linkUrl) ? "/Catalog" : linkUrl.Trim(),
            SortOrder = sortOrder,
            IsActive = true
        };

        var created = await stories.AddSlideAsync(storyId, slide);
        if (slideImage is { Length: > 0 })
            await stories.SaveSlideImageAsync(storyId, created.Id, slideImage);

        TempData["Success"] = "اسلاید اضافه شد.";
        return RedirectToAction(nameof(Edit), new { id = storyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UpdateSlide(int storyId, int slideId, string title, string linkUrl, int sortOrder, bool isActive, IFormFile? slideImage)
    {
        var story = await stories.GetForEditAsync(storyId);
        if (story is null) return NotFound();

        var existing = story.Slides.FirstOrDefault(s => s.Id == slideId);
        if (existing is null) return NotFound();

        existing.Title = title.Trim();
        existing.LinkUrl = string.IsNullOrWhiteSpace(linkUrl) ? "/Catalog" : linkUrl.Trim();
        existing.SortOrder = sortOrder;
        existing.IsActive = isActive;

        await stories.UpdateSlideAsync(existing);
        if (slideImage is { Length: > 0 })
            await stories.SaveSlideImageAsync(storyId, slideId, slideImage);

        TempData["Success"] = "اسلاید به‌روز شد.";
        return RedirectToAction(nameof(Edit), new { id = storyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSlide(int storyId, int slideId)
    {
        await stories.DeleteSlideAsync(slideId);
        TempData["Success"] = "اسلاید حذف شد.";
        return RedirectToAction(nameof(Edit), new { id = storyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await stories.DeleteStoryAsync(id);
        TempData["Success"] = "استوری حذف شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportFromProducts()
    {
        var count = await stories.ImportFromProductsAsync();
        TempData["Success"] = $"{count} استوری از محصولات فعلی سایت ساخته شد. می‌توانید تصاویر را سفارشی کنید.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadCover(int id, IFormFile coverImage)
    {
        if (coverImage is not { Length: > 0 })
        {
            TempData["Error"] = "فایل تصویر انتخاب نشده.";
            return RedirectToAction(nameof(Index));
        }

        await stories.SaveCoverImageAsync(id, coverImage);
        TempData["Success"] = "تصویر حلقه استوری ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }
}
