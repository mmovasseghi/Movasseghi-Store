using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

public interface IHomeStoryService
{
    Task<IReadOnlyList<StoryItemDto>> GetStoriesForHomeAsync(CancellationToken ct = default);
    Task<bool> HasManagedStoriesAsync(CancellationToken ct = default);

    Task<List<HomeStory>> GetAllForAdminAsync(CancellationToken ct = default);
    Task<HomeStory?> GetForEditAsync(int id, CancellationToken ct = default);
    Task<HomeStory> CreateStoryAsync(HomeStory model, CancellationToken ct = default);
    Task UpdateStoryAsync(HomeStory model, CancellationToken ct = default);
    Task DeleteStoryAsync(int id, CancellationToken ct = default);

    Task<HomeStorySlide> AddSlideAsync(int storyId, HomeStorySlide slide, CancellationToken ct = default);
    Task UpdateSlideAsync(HomeStorySlide slide, CancellationToken ct = default);
    Task DeleteSlideAsync(int slideId, CancellationToken ct = default);

    Task<string> SaveCoverImageAsync(int storyId, IFormFile file, CancellationToken ct = default);
    Task<string> SaveSlideImageAsync(int storyId, int slideId, IFormFile file, CancellationToken ct = default);

    Task SeedDefaultsIfEmptyAsync(CancellationToken ct = default);
    Task<int> ImportFromProductsAsync(CancellationToken ct = default);
}
