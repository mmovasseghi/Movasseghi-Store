using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderVehicleInfo> OrderVehicleInfos => Set<OrderVehicleInfo>();
    public DbSet<OrderDeliveryAddress> OrderDeliveryAddresses => Set<OrderDeliveryAddress>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<NewsItem> NewsItems => Set<NewsItem>();
    public DbSet<FaqItem> FaqItems => Set<FaqItem>();
    public DbSet<CmsPage> CmsPages => Set<CmsPage>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<PurchaseInquiry> PurchaseInquiries => Set<PurchaseInquiry>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<SeoProfile> SeoProfiles => Set<SeoProfile>();
    public DbSet<PillarKeyword> PillarKeywords => Set<PillarKeyword>();
    public DbSet<GrowthAction> GrowthActions => Set<GrowthAction>();
    public DbSet<RankSnapshot> RankSnapshots => Set<RankSnapshot>();
    public DbSet<HomeSection> HomeSections => Set<HomeSection>();
    public DbSet<NavItem> NavItems => Set<NavItem>();
    public DbSet<ContentAtom> ContentAtoms => Set<ContentAtom>();
    public DbSet<CompetitorSnapshot> CompetitorSnapshots => Set<CompetitorSnapshot>();
    public DbSet<GrowthReport> GrowthReports => Set<GrowthReport>();
    public DbSet<EditorialTopicQueue> EditorialTopicQueue => Set<EditorialTopicQueue>();
    public DbSet<AuthorityCheckItem> AuthorityCheckItems => Set<AuthorityCheckItem>();
    public DbSet<PageVisit> PageVisits => Set<PageVisit>();
    public DbSet<SiteEvent> SiteEvents => Set<SiteEvent>();
    public DbSet<HomeStory> HomeStories => Set<HomeStory>();
    public DbSet<HomeStorySlide> HomeStorySlides => Set<HomeStorySlide>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Category>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
        });

        builder.Entity<Product>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => new { x.IsActive, x.IsHomeSpecialOffer, x.IsHomeFeatured, x.SortOrder });
            e.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId);
        });

        builder.Entity<ProductVariant>(e =>
        {
            e.HasIndex(x => x.Sku).IsUnique();
            e.HasOne(x => x.Product).WithMany(x => x.Variants).HasForeignKey(x => x.ProductId);
        });

        builder.Entity<ProductImage>(e =>
        {
            e.HasOne(x => x.Product).WithMany(x => x.Images).HasForeignKey(x => x.ProductId);
        });

        builder.Entity<ProductReview>(e =>
        {
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ParentReview).WithMany(x => x.Replies).HasForeignKey(x => x.ParentReviewId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ProductId, x.Status });
        });

        builder.Entity<Order>(e =>
        {
            e.HasIndex(x => x.OrderNumber).IsUnique();
            e.HasOne(x => x.VehicleInfo).WithOne(x => x.Order).HasForeignKey<OrderVehicleInfo>(x => x.OrderId);
            e.HasOne(x => x.DeliveryAddress).WithOne(x => x.Order).HasForeignKey<OrderDeliveryAddress>(x => x.OrderId);
        });

        builder.Entity<OrderItem>(e =>
        {
            e.HasOne(x => x.Order).WithMany(x => x.Items).HasForeignKey(x => x.OrderId);
        });

        builder.Entity<Coupon>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
        });

        builder.Entity<BlogPost>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
        });

        builder.Entity<NewsItem>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
        });

        builder.Entity<CmsPage>(e =>
        {
            e.HasIndex(x => x.Key).IsUnique();
        });

        builder.Entity<HomeSection>(e => e.HasIndex(x => x.Key).IsUnique());
        builder.Entity<ContentAtom>(e => e.HasIndex(x => x.Key).IsUnique());
        builder.Entity<AuthorityCheckItem>(e => e.HasIndex(x => x.Key).IsUnique());

        builder.Entity<UserAddress>(e =>
        {
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.UserId);
        });

        builder.Entity<SeoProfile>(e =>
        {
            e.HasIndex(x => new { x.EntityType, x.EntityId }).IsUnique();
        });

        builder.Entity<PillarKeyword>(e =>
        {
            e.HasIndex(x => x.Phrase).IsUnique();
        });

        // ── آنالیتیکس ──
        // ایندکس‌ها بر اساس الگوی واقعی پرس‌وجوی گزارش‌ها چیده شده‌اند:
        // همیشه بازه تاریخ + سپس گروه‌بندی بر مسیر/کانال/نشست.
        builder.Entity<PageVisit>(e =>
        {
            e.HasIndex(x => x.DateKey);
            e.HasIndex(x => new { x.DateKey, x.IsBot });
            e.HasIndex(x => new { x.DateKey, x.Path });
            e.HasIndex(x => new { x.DateKey, x.Channel });
            e.HasIndex(x => new { x.PageType, x.EntityId });
            e.HasIndex(x => x.SessionKey);
            e.HasIndex(x => x.VisitorKey);
        });

        builder.Entity<SiteEvent>(e =>
        {
            e.HasIndex(x => x.DateKey);
            e.HasIndex(x => new { x.DateKey, x.Name });
            e.HasIndex(x => new { x.Name, x.EntityId });
            e.HasIndex(x => x.SessionKey);
        });

        // ── ایندکس‌های عملکردی که وجود نداشتند ──
        // کاتالوگ و لیست ادمین روی این ستون‌ها فیلتر می‌کنند؛
        // بدون ایندکس، SQLite کل جدول را اسکن می‌کرد.
        builder.Entity<Product>(e =>
        {
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => new { x.CategoryId, x.IsActive });
            e.HasIndex(x => new { x.IsActive, x.UpdatedAt });
        });

        builder.Entity<NavItem>(e => e.HasIndex(x => new { x.Zone, x.IsActive, x.SortOrder }));

        builder.Entity<HomeStory>(e =>
        {
            e.HasIndex(x => x.StoryKey).IsUnique();
            e.HasMany(x => x.Slides).WithOne(x => x.HomeStory).HasForeignKey(x => x.HomeStoryId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<HomeStorySlide>(e => e.HasIndex(x => new { x.HomeStoryId, x.SortOrder }));

        builder.Entity<Order>(e => e.HasIndex(x => x.CreatedAt));
        builder.Entity<PurchaseInquiry>(e => e.HasIndex(x => new { x.IsHandled, x.CreatedAt }));
        builder.Entity<RankSnapshot>(e => e.HasIndex(x => new { x.Keyword, x.CheckedAt }));
    }
}
