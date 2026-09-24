using Microsoft.AspNetCore.Identity;

namespace MovasseghiShop.Web.Data;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string? CompanyName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public void SyncFullName()
    {
        var first = FirstName?.Trim() ?? "";
        var last = LastName?.Trim() ?? "";
        FullName = string.IsNullOrWhiteSpace(first) && string.IsNullOrWhiteSpace(last)
            ? null
            : $"{first} {last}".Trim();
    }
}
