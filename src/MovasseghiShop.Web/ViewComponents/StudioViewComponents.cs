using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.ViewComponents;

public class NavZoneViewComponent(IStudioContentService studio) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(string zone)
    {
        var items = await studio.GetNavItemsAsync(zone);
        // پارامتر InvokeAsync همیشه در ViewData ویوی کامپوننت در دسترس نیست — بدون zone،
        // شاخهٔ header اجرا می‌شود و لینک‌های فوتر در یک خط می‌چینند.
        ViewData["zone"] = zone;
        return View(items);
    }
}

public class ContentAtomViewComponent(IStudioContentService studio) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(string key)
    {
        var atom = await studio.GetAtomAsync(key);
        if (atom == null && key == "promo-bar")
        {
            atom = new ContentAtom
            {
                Key = "promo-bar",
                Body = """{"sloganFull":"با <strong>موثقی</strong> خیالت راحته — عمده ظروف گیاهی، اصل کارخانه","sloganShort":"با <strong>موثقی</strong> خیالت راحته","chips":["🌿 ۱۰۰٪ گیاهی","💰 تخفیف پلکانی","🛡️ ضمانت اصالت","🚚 ارسال سراسری"]}"""
            };
        }
        return View(atom);
    }
}
