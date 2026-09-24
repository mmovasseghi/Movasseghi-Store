using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

public interface IOtpService
{
    Task<string> SendAsync(string phone);
    Task<bool> VerifyAsync(string phone, string code);
}

public class MockOtpService(ApplicationDbContext db, ILogger<MockOtpService> logger) : IOtpService
{
    public const string DevCode = "123456";

    public async Task<string> SendAsync(string phone)
    {
        phone = NormalizePhone(phone);
        var code = DevCode;
        db.OtpCodes.Add(new OtpCode
        {
            Phone = phone,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        });
        await db.SaveChangesAsync();
        logger.LogInformation("OTP for {Phone}: {Code}", phone, code);
        return code;
    }

    public async Task<bool> VerifyAsync(string phone, string code)
    {
        phone = NormalizePhone(phone);
        var otp = await db.OtpCodes
            .Where(x => x.Phone == phone && !x.IsUsed && x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (otp == null || otp.Code != code.Trim()) return false;
        otp.IsUsed = true;
        await db.SaveChangesAsync();
        return true;
    }

    public static string NormalizePhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "";
        var digits = new System.Text.StringBuilder(phone.Length);
        foreach (var ch in phone)
        {
            if (ch is >= '۰' and <= '۹')
                digits.Append((char)('0' + (ch - '۰')));
            else if (ch is >= '٠' and <= '٩')
                digits.Append((char)('0' + (ch - '٠')));
            else if (char.IsDigit(ch))
                digits.Append(ch);
        }

        phone = digits.ToString();
        if (phone.StartsWith("98") && phone.Length >= 12) phone = "0" + phone[2..];
        if (phone.Length == 10 && phone.StartsWith('9')) phone = "0" + phone;
        return phone;
    }
}
