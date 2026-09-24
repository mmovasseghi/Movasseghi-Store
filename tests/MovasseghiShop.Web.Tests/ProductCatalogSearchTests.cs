using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Services;
using Xunit;

namespace MovasseghiShop.Web.Tests;

/// <summary>Relevance ordering for short Persian queries like «نی».</summary>
public class ProductCatalogSearchTests
{
    [Fact]
    public void Short_query_prefers_exact_slug_and_name_start()
    {
        var straw = new Product
        {
            Name = "نی یکبار مصرف گیاهی",
            Slug = "نی",
            ProductCode = "000832",
            Category = new Category { Name = "لوازم جانبی" }
        };
        var other = new Product
        {
            Name = "سیب زمینی خوری یکبار مصرف گیاهی",
            Slug = "سیب-زمینی-خوري",
            ProductCode = "000402",
            Category = new Category { Name = "ظروف" }
        };

        var scoreStraw = InvokeScoreSuggestion(straw, "نی");
        var scoreOther = InvokeScoreSuggestion(other, "نی");

        Assert.True(scoreStraw > scoreOther);
    }

    static int InvokeScoreSuggestion(Product p, string term)
    {
        var method = typeof(ProductCatalogService).GetMethod(
            "ScoreSuggestion",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);
        var tokens = new[] { term };
        return (int)method.Invoke(null, [p, term, tokens])!;
    }
}
