using MovasseghiShop.Web.Data;

namespace MovasseghiShop.Web.Models.Entities;

public class UserAddress
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public ApplicationUser? User { get; set; }
    public string? Label { get; set; }
    public string Province { get; set; } = "";
    public string City { get; set; } = "";
    public string FullAddress { get; set; } = "";
    public string? PostalCode { get; set; }
    public string? Plaque { get; set; }
    public string? Unit { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
