using Microsoft.EntityFrameworkCore;
using Xunit;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Tests;

public class HomeSpecialOffersSelectorTests
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
    public async Task LoadAsync_returns_only_active_starred_products()
    {
        await using var db = CreateDb();
        var cat = SeedCategory(db);
        db.Products.AddRange(
            new Product { Name = "A", Slug = "a", CategoryId = cat.Id, IsActive = true, IsHomeSpecialOffer = true },
            new Product { Name = "B", Slug = "b", CategoryId = cat.Id, IsActive = true, IsHomeSpecialOffer = false },
            new Product { Name = "C", Slug = "c", CategoryId = cat.Id, IsActive = false, IsHomeSpecialOffer = true });
        await db.SaveChangesAsync();

        var list = await HomeSpecialOffersSelector.LoadAsync(db.Products);

        Assert.Single(list);
        Assert.Equal("A", list[0].Name);
    }

    [Fact]
    public async Task LoadAsync_respects_sort_order_then_updated_at_max_eight()
    {
        await using var db = CreateDb();
        var cat = SeedCategory(db);
        var baseTime = DateTime.UtcNow;
        for (var i = 0; i < 10; i++)
        {
            db.Products.Add(new Product
            {
                Name = $"P{i}",
                Slug = $"p{i}",
                CategoryId = cat.Id,
                IsActive = true,
                IsHomeSpecialOffer = true,
                SortOrder = i,
                UpdatedAt = baseTime.AddMinutes(i)
            });
        }
        await db.SaveChangesAsync();

        var list = await HomeSpecialOffersSelector.LoadAsync(db.Products);

        Assert.Equal(HomeSpecialOffersSelector.MaxCount, list.Count);
        Assert.Equal("P0", list[0].Name);
        Assert.Equal($"P{HomeSpecialOffersSelector.MaxCount - 1}", list[^1].Name);
    }

    [Fact]
    public async Task Toggle_flips_flag_via_entity()
    {
        await using var db = CreateDb();
        var cat = SeedCategory(db);
        var p = new Product { Name = "X", Slug = "x", CategoryId = cat.Id, IsActive = true };
        db.Products.Add(p);
        await db.SaveChangesAsync();

        p.IsHomeSpecialOffer = true;
        await db.SaveChangesAsync();

        var loaded = await db.Products.AsNoTracking().FirstAsync(x => x.Id == p.Id);
        Assert.True(loaded.IsHomeSpecialOffer);
    }
}
