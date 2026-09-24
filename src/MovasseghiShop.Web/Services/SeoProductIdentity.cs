using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

/// <summary>
/// شناسه‌های جهانی محصول (GTIN / MPN).
/// گوگل GTIN نامعتبر را نادیده می‌گیرد و در Merchant Center خطا می‌دهد،
/// پس قبل از نوشتن در Schema اعتبارسنجی می‌شود.
/// </summary>
public static class SeoProductIdentity
{
    /// <summary>GTIN معتبر: ۸، ۱۲، ۱۳ یا ۱۴ رقم با Check Digit درست.</summary>
    public static bool HasValidGtin(Product p) => IsValidGtin(p.Gtin);

    /// <summary>MPN رسمی یا کد محصول آملون — برای B2B معمولاً همان کد کارخانه است.</summary>
    public static string? ResolveMpn(Product p)
    {
        if (!string.IsNullOrWhiteSpace(p.Mpn)) return p.Mpn.Trim();
        if (!string.IsNullOrWhiteSpace(p.ProductCode)) return p.ProductCode.Trim();
        return null;
    }

    public static bool HasMpn(Product p) => ResolveMpn(p) is not null;

    public static bool IsValidGtin(string? raw)
    {
        var digits = Digits(raw);
        if (digits.Length is not (8 or 12 or 13 or 14)) return false;

        // GS1 Check Digit — از راست به چپ، وزن‌های متناوب ۳ و ۱
        var sum = 0;
        for (var i = digits.Length - 2; i >= 0; i--)
        {
            var weight = (digits.Length - i) % 2 == 0 ? 3 : 1;
            sum += (digits[i] - '0') * weight;
        }

        var check = (10 - sum % 10) % 10;
        return check == digits[^1] - '0';
    }

    /// <summary>نام فیلد Schema بر اساس طول GTIN.</summary>
    public static string? SchemaKey(string? raw) => Digits(raw).Length switch
    {
        8 => "gtin8",
        12 => "gtin12",
        13 => "gtin13",
        14 => "gtin14",
        _ => null
    };

    public static string Digits(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? ""
            : new string(raw.Where(char.IsAsciiDigit).ToArray());

    /// <summary>توضیح خطا برای نمایش در پنل — «چرا رد شد».</summary>
    public static string? Validate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var digits = Digits(raw);
        if (digits.Length is not (8 or 12 or 13 or 14))
            return $"GTIN باید ۸، ۱۲، ۱۳ یا ۱۴ رقم باشد (فعلی: {digits.Length} رقم).";

        return IsValidGtin(raw)
            ? null
            : "رقم کنترلی GTIN نامعتبر است — بارکد را از روی بسته‌بندی دوباره بخوانید.";
    }
}
