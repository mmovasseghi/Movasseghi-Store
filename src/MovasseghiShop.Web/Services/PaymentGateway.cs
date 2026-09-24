namespace MovasseghiShop.Web.Services;

public interface IPaymentGateway
{
    Task<PaymentResult> InitiateAsync(decimal amountToman, string orderNumber, string callbackUrl);
    Task<PaymentVerifyResult> VerifyAsync(string authority, decimal expectedAmount);
}

public record PaymentResult(bool Success, string? Authority, string? PaymentUrl, string? Message);
public record PaymentVerifyResult(bool Success, string? ReferenceId, string? Message);

public class MockPaymentGateway : IPaymentGateway
{
    public Task<PaymentResult> InitiateAsync(decimal amountToman, string orderNumber, string callbackUrl)
    {
        var authority = Guid.NewGuid().ToString("N")[..16];
        var url = $"/payment/mock?authority={authority}&order={orderNumber}";
        return Task.FromResult(new PaymentResult(true, authority, url, null));
    }

    public Task<PaymentVerifyResult> VerifyAsync(string authority, decimal expectedAmount)
        => Task.FromResult(new PaymentVerifyResult(true, $"MOCK-{authority[..8].ToUpperInvariant()}", null));
}
