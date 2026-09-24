using MovasseghiShop.Web.Models.Enums;

namespace MovasseghiShop.Web.Models.Entities;

/// <summary>نظر یا پاسخ زنجیره‌ای روی محصول — فقط وضعیت Approved در سایت و Schema دیده می‌شود.</summary>
public class ProductReview
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int? ParentReviewId { get; set; }
    public ProductReview? ParentReview { get; set; }
    public ICollection<ProductReview> Replies { get; set; } = new List<ProductReview>();

    public string AuthorDisplayName { get; set; } = "";
    public string Body { get; set; } = "";
    /// <summary>۱–۵ برای نظر اصلی؛ ۰ برای پاسخ بدون امتیاز.</summary>
    public int Rating { get; set; }
    public ProductReviewStatus Status { get; set; } = ProductReviewStatus.Pending;
    public bool IsStoreReply { get; set; }
    /// <summary>تولید شده توسط Auto-Fix SEO — در پنل قابل فیلتر است.</summary>
    public bool IsSeoSuggested { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModeratedAt { get; set; }
}
