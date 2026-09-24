using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Models;

public sealed class ProductReviewsSectionModel
{
    public int ProductId { get; init; }
    public IReadOnlyList<ProductReview> Items { get; init; } = [];
    public double Average { get; init; }
    public int Count { get; init; }
}

/// <summary>ویرایش نظر + پاسخ فروشگاه در ادمین.</summary>
public sealed class AdminProductReviewEditViewModel
{
    public const string StoreReplyDisplayName = "پشتیبانی موثقی";

    public ProductReview Review { get; set; } = null!;
    public ProductReview? ParentReview { get; set; }
    public IReadOnlyList<ProductReview> StoreReplies { get; set; } = [];

    public bool IsRootReview => Review.ParentReviewId == null && !Review.IsStoreReply;
}
