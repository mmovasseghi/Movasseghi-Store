using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Models;

public class ProformaInvoiceViewModel
{
    public Order Order { get; set; } = null!;
    public CartSummary? Analytics { get; set; }
}
