using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Enums;

namespace MovasseghiShop.Web.Areas.Admin;

public interface IAdminNavBadgeService
{
    Task<int> GetOpenInquiryCountAsync(CancellationToken ct = default);
    Task<int> GetOpenOrderCountAsync(CancellationToken ct = default);
    void Invalidate();
}

/// <summary>شمارش نشان‌های منوی ادمین — با کش کوتاه و پاک‌سازی پس از تغییر سفارش/استعلام.</summary>
public class AdminNavBadgeService(ApplicationDbContext db, IMemoryCacheAccessor cache) : IAdminNavBadgeService
{
    const string InquiryKey = "admin:nav:open-inquiries";
    const string OrderKey = "admin:nav:open-orders";
    static readonly TimeSpan Ttl = TimeSpan.FromSeconds(15);

    public Task<int> GetOpenInquiryCountAsync(CancellationToken ct = default) =>
        cache.GetOrCreateAsync(InquiryKey, Ttl, () =>
            db.PurchaseInquiries.CountAsync(i => !i.IsHandled, ct));

    public Task<int> GetOpenOrderCountAsync(CancellationToken ct = default) =>
        cache.GetOrCreateAsync(OrderKey, Ttl, () =>
            db.Orders.CountAsync(o =>
                o.Status != OrderStatus.Cancelled
                && o.OperatorFollowedUpAt == null, ct));

    public void Invalidate()
    {
        cache.Remove(InquiryKey);
        cache.Remove(OrderKey);
    }
}
