using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Enums;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Controllers;

public class PaymentController(
    ApplicationDbContext db,
    IPaymentGateway paymentGateway) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Mock(string authority, string order)
    {
        var entity = await db.Orders.FirstOrDefaultAsync(o => o.OrderNumber == order);
        if (entity == null) return NotFound();
        ViewBag.Authority = authority;
        ViewBag.OrderNumber = order;
        return View(entity);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(string authority, string orderNumber)
    {
        var entity = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
        if (entity == null) return NotFound();

        var result = await paymentGateway.VerifyAsync(authority, entity.TotalAmount);
        if (!result.Success)
        {
            TempData["Error"] = result.Message ?? "پرداخت ناموفق";
            return RedirectToAction("Index", "Checkout");
        }

        entity.Status = OrderStatus.PaymentReceived;
        entity.PaymentReference = result.ReferenceId;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return RedirectToAction("Success", "Order", new { number = orderNumber });
    }
}
