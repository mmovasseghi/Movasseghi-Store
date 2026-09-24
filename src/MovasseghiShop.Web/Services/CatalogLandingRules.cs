using MovasseghiShop.Web.Models;

namespace MovasseghiShop.Web.Services;

public static class CatalogLandingRules
{
    /// <summary>
    /// صفحهٔ اول یک دسته — فیلترهای جانبی (نوع، بسته‌بندی، مرتب‌سازی) مجاز؛ جستجو و صفحه‌بندی بعدی خیر.
    /// </summary>
    public static bool ShouldShowCategoryLanding(ProductFilterQuery q) =>
        !string.IsNullOrWhiteSpace(q.Category)
        && string.IsNullOrWhiteSpace(q.Q)
        && q.Page <= 1;
}
