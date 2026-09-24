using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace MovasseghiShop.Web.Services;

public sealed class HumanChallengeDto
{
    public string Token { get; set; } = "";
    public string Prompt { get; set; } = "";
    public IReadOnlyList<HumanChallengeOption> Options { get; set; } = [];
}

public sealed class HumanChallengeOption
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string Emoji { get; set; } = "";
}

public interface ICheckoutHumanVerificationService
{
    HumanChallengeDto CreateChallenge(HttpContext httpContext);
    bool ValidateAndGrant(HttpContext httpContext, string token, string selectedOptionId);
    bool IsVerified(HttpContext httpContext);
    void Revoke(HttpContext httpContext);
}

/// <summary>چالش سبک «ربات نیستم» — بدون SMS؛ پاسخ در Session با HMAC نگه‌داری می‌شود.</summary>
public class CheckoutHumanVerificationService(IConfiguration config) : ICheckoutHumanVerificationService
{
    const string SessionVerifiedKey = "checkout:human:verified";
    const string SessionPayloadKey = "checkout:human:payload";
    static readonly TimeSpan VerifiedTtl = TimeSpan.FromMinutes(45);

    static readonly (string id, string label, string emoji)[] StoreProducts =
    [
        ("bowl", "کاسه گیاهی آملون", "🥣"),
        ("carton", "کارتن ظروف عمده", "📦"),
        ("leaf", "ظرف گیاهی", "🌿")
    ];

    static readonly (string id, string label, string emoji)[] NotStore =
    [
        ("robot", "ربات", "🤖"),
        ("gear", "چرخ‌دنده", "⚙️")
    ];

    public HumanChallengeDto CreateChallenge(HttpContext httpContext)
    {
        var token = Guid.NewGuid().ToString("N");
        var correct = StoreProducts[RandomNumberGenerator.GetInt32(StoreProducts.Length)];
        var wrong = NotStore[RandomNumberGenerator.GetInt32(NotStore.Length)];

        var options = new List<HumanChallengeOption>
        {
            new() { Id = correct.id, Label = correct.label, Emoji = correct.emoji },
            new() { Id = wrong.id, Label = wrong.label, Emoji = wrong.emoji }
        };
        if (RandomNumberGenerator.GetInt32(2) == 0)
            (options[0], options[1]) = (options[1], options[0]);

        var payload = $"{token}|{correct.id}|{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        var sig = Sign(payload);
        httpContext.Session.SetString(SessionPayloadKey, $"{payload}|{sig}");

        return new HumanChallengeDto
        {
            Token = token,
            Prompt = "کدام مورد مربوط به فروشگاه موثقی است؟ (یکی را بزنید)",
            Options = options
        };
    }

    public bool ValidateAndGrant(HttpContext httpContext, string token, string selectedOptionId)
    {
        var raw = httpContext.Session.GetString(SessionPayloadKey);
        if (string.IsNullOrEmpty(raw)) return false;

        var parts = raw.Split('|');
        if (parts.Length < 4) return false;

        var storedToken = parts[0];
        var correctId = parts[1];
        if (!long.TryParse(parts[2], out var issuedUnix)) return false;
        var sig = parts[3];

        var payload = $"{storedToken}|{correctId}|{issuedUnix}";
        if (!string.Equals(Sign(payload), sig, StringComparison.Ordinal)) return false;
        if (!string.Equals(storedToken, token, StringComparison.Ordinal)) return false;

        var age = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(issuedUnix);
        if (age > TimeSpan.FromMinutes(10)) return false;

        if (!string.Equals(correctId, selectedOptionId, StringComparison.Ordinal)) return false;

        var verifiedPayload = $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}|{Sign("ok")}";
        httpContext.Session.SetString(SessionVerifiedKey, verifiedPayload);
        httpContext.Session.Remove(SessionPayloadKey);
        return true;
    }

    public bool IsVerified(HttpContext httpContext)
    {
        var raw = httpContext.Session.GetString(SessionVerifiedKey);
        if (string.IsNullOrEmpty(raw)) return false;
        var parts = raw.Split('|');
        if (parts.Length != 2 || !long.TryParse(parts[0], out var unix)) return false;
        var age = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(unix);
        return age <= VerifiedTtl;
    }

    public void Revoke(HttpContext httpContext)
    {
        httpContext.Session.Remove(SessionVerifiedKey);
        httpContext.Session.Remove(SessionPayloadKey);
    }

    string Sign(string payload)
    {
        var secret = config["Security:CheckoutHmacSecret"] ?? "movasseghi-checkout-local-dev-secret-change-in-production";
        var bytes = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }
}
