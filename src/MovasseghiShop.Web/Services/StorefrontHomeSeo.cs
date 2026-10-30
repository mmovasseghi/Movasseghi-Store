namespace MovasseghiShop.Web.Services;

/// <summary>متن‌های ثابت FAQ/AEO صفحهٔ خانه — هم HTML هم JSON-LD.</summary>
public static class StorefrontHomeSeo
{
    public const double TehranLatitude = 35.6892;
    public const double TehranLongitude = 51.3890;

    public static readonly (string Question, string Answer)[] FaqItems =
    [
        (
            "حداقل مبلغ خرید عمده از موثقی چقدر است؟",
            $"حداقل سفارش عمده {WholesaleRules.FormatMinAmount()} تومان است؛ تخفیف پلکانی تا {WholesaleRules.MaxAutoDiscountPercent}٪ روی جمع فاکتور اعمال می‌شود."
        ),
        (
            "آیا محصولات اصل آملون هستند؟",
            "فروشگاه موثقی نمایندگی رسمی آملون در تهران است و ظروف یکبار مصرف گیاهی اصل عرضه می‌کند."
        ),
        (
            "ارسال به سراسر کشور انجام می‌شود؟",
            "بله؛ برای سفارش‌های عمده ارسال سراسری انجام می‌شود. هماهنگی و مشاوره از طریق تماس با " +
            WholesaleRules.CoordinationPhone + "."
        ),
        (
            "برای رستوران، کافه و کیترینگ چه محصولاتی دارید؟",
            "ظروف غذای گیاهی، لیوان و دسر، بسته‌بندی مهمانی و پذیرایی — با جستجو در کاتالوگ یا تماس با کارشناس."
        )
    ];
}
