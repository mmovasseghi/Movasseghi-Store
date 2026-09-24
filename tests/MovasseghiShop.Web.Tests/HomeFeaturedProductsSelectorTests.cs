using Microsoft.EntityFrameworkCore;
using Xunit;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Tests;

public class HomeFeaturedProductsSelectorTests
{
    static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    static Category SeedCategory(ApplicationDbContext db)
    {
        var cat = new Category { Name = "Test", Slug = "test", IsActive = true };
        db.Categories.Add(cat);
        db.SaveChanges();
        return cat;
    }

    [Fact]
    public async Task LoadAsync_returns_only_active_diamond_starred_products()
    {
        await using var db = CreateDb();
        var cat = SeedCategory(db);
        db.Products.AddRange(
            new Product { Name = "A", Slug = "a", CategoryId = cat.Id, IsActive = true, IsHomeFeatured = true },
            new Product { Name = "B", Slug = "b", CategoryId = cat.Id, IsActive = true, IsHomeFeatured = false },
            new Product { Name = "C", Slug = "c", CategoryId = cat.Id, IsActive = false, IsHomeFeatured = true });
        await db.SaveChangesAsync();

        var list = await HomeFeaturedProductsSelector.LoadAsync(db.Products);

        Assert.Single(list);
        Assert.Equal("A", list[0].Name);
    }

    [Fact]
    public async Task CanEnableAsync_blocks_ninth_active_special()
    {
        await using var db = CreateDb();
        var cat = SeedCategory(db);
        for (var i = 0; i < 8; i++)
        {
            db.Products.Add(new Product
            {
                Name = $"S{i}",
                Slug = $"s{i}",
                CategoryId = cat.Id,
                IsActive = true,
                IsHomeSpecialOffer = true
            });
        }
        var extra = new Product { Name = "X", Slug = "x", CategoryId = cat.Id, IsActive = true };
        db.Products.Add(extra);
        await db.SaveChangesAsync();

        var (allowed, message) = await HomeProductHighlights.CanEnableAsync(db.Products, extra.Id, "special");

        Assert.False(allowed);
        Assert.NotNull(message);
    }
}
