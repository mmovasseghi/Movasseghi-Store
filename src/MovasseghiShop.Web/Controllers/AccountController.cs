using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Controllers;

public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext db,
    IOtpService otpService,
    IGeocodingService geocodingService) : Controller
{
    public IActionResult Login() => Redirect("/?auth=1");

    [HttpGet]
    public IActionResult Modal() => PartialView("_AuthModal");

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Redirect("/?auth=1");

        var userId = user.Id;
        var orders = await db.Orders.AsNoTracking()
            .Include(o => o.DeliveryAddress)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var addresses = orders
            .Where(o => o.DeliveryAddress != null)
            .Select(o => o.DeliveryAddress!)
            .GroupBy(a => $"{a.Province}|{a.City}|{a.FullAddress}")
            .Select(g => g.First())
            .ToList();

        var userAddresses = await db.UserAddresses.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();

        var (first, last) = ResolveNames(user);
        var vm = new AccountDashboardViewModel
        {
            User = user,
            Orders = orders,
            SavedAddresses = addresses,
            UserAddresses = userAddresses,
            FirstName = first,
            LastName = last,
            DisplayName = BuildDisplayName(user),
            Initials = BuildInitials(first, last, user.PhoneNumber),
            RoleBadge = User.IsInRole("Admin") ? "مدیر" : "مشتری عمده",
            HasPassword = await userManager.HasPasswordAsync(user)
        };
        ViewBag.AccountHasPassword = vm.HasPassword;
        return View(vm);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(
        string? currentPassword,
        string newPassword,
        string confirmPassword,
        bool ajax = false)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            if (ajax) return BadRequest(new { error = "رمز عبور جدید را وارد کنید." });
            return RedirectToAction(nameof(Index));
        }

        if (newPassword != confirmPassword)
        {
            if (ajax) return BadRequest(new { error = "رمز عبور و تکرار آن یکسان نیست." });
            return RedirectToAction(nameof(Index));
        }

        IdentityResult result;
        var hasPwd = await userManager.HasPasswordAsync(user);
        if (hasPwd)
        {
            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                if (ajax) return BadRequest(new { error = "رمز عبور فعلی را وارد کنید." });
                return RedirectToAction(nameof(Index));
            }
            result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        }
        else
        {
            result = await userManager.AddPasswordAsync(user, newPassword);
        }

        if (!result.Succeeded)
        {
            var error = IdentityErrorMessage(result);
            if (result.Errors.Any(e => e.Code == "PasswordMismatch"))
                error = "رمز عبور فعلی اشتباه است.";
            if (ajax) return BadRequest(new { error });
            TempData["Error"] = error;
            return RedirectToAction(nameof(Index));
        }

        await signInManager.RefreshSignInAsync(user);
        if (ajax) return Json(new { ok = true, message = "رمز عبور با موفقیت به‌روزرسانی شد." });
        TempData["Success"] = "رمز عبور با موفقیت به‌روزرسانی شد.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string firstName, string lastName, string? email, string? businessName, bool ajax = false)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            if (ajax) return BadRequest(new { error = "نام و نام خانوادگی الزامی است." });
            return RedirectToAction(nameof(Index));
        }

        user.FirstName = firstName.Trim();
        user.LastName = lastName.Trim();
        user.SyncFullName();
        user.Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        user.CompanyName = string.IsNullOrWhiteSpace(businessName) ? null : businessName.Trim();

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var error = IdentityErrorMessage(result);
            if (ajax) return BadRequest(new { error });
            TempData["Error"] = error;
            return RedirectToAction(nameof(Index));
        }

        if (ajax) return Json(new
        {
            ok = true,
            fullName = user.FullName,
            displayName = BuildDisplayName(user),
            email = user.Email,
            companyName = user.CompanyName,
            firstName = user.FirstName,
            lastName = user.LastName
        });
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ReverseGeocode(double lat, double lng, CancellationToken ct)
    {
        try
        {
            var result = await geocodingService.ReverseAsync(lat, lng, ct);
            if (result == null)
                return NotFound(new { error = "آدرسی برای این موقعیت یافت نشد." });

            return Json(new
            {
                ok = true,
                province = result.Province,
                city = result.City,
                fullAddress = result.FullAddress,
                displayName = result.DisplayName
            });
        }
        catch (Exception)
        {
            return StatusCode(503, new { error = "سرویس آدرس‌یابی موقتاً در دسترس نیست." });
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAddress(
        int? id,
        string? label,
        string province,
        string city,
        string fullAddress,
        string? postalCode,
        string? plaque,
        string? unit,
        double? latitude,
        double? longitude,
        bool isDefault = false,
        bool ajax = false)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        province = (province ?? "").Trim();
        city = (city ?? "").Trim();
        fullAddress = (fullAddress ?? "").Trim();

        if (string.IsNullOrWhiteSpace(province) || string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(fullAddress))
        {
            if (ajax) return BadRequest(new { error = "استان، شهر و آدرس کامل الزامی است." });
            return RedirectToAction(nameof(Index));
        }

        UserAddress entity;
        if (id.HasValue && id.Value > 0)
        {
            entity = await db.UserAddresses.FirstOrDefaultAsync(a => a.Id == id.Value && a.UserId == user.Id);
            if (entity == null)
            {
                if (ajax) return BadRequest(new { error = "آدرس یافت نشد." });
                return RedirectToAction(nameof(Index));
            }
        }
        else
        {
            entity = new UserAddress { UserId = user.Id };
            db.UserAddresses.Add(entity);
        }

        entity.Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
        entity.Province = province;
        entity.City = city;
        entity.FullAddress = fullAddress;
        entity.PostalCode = string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim();
        entity.Plaque = string.IsNullOrWhiteSpace(plaque) ? null : plaque.Trim();
        entity.Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
        entity.Latitude = latitude;
        entity.Longitude = longitude;
        entity.IsDefault = isDefault;

        if (isDefault)
        {
            var others = await db.UserAddresses.Where(a => a.UserId == user.Id && a.Id != entity.Id).ToListAsync();
            others.ForEach(a => a.IsDefault = false);
        }

        await db.SaveChangesAsync();

        if (ajax) return Json(new { ok = true, address = MapAddressDto(entity) });
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress(int id, bool ajax = false)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var entity = await db.UserAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);
        if (entity == null)
        {
            if (ajax) return BadRequest(new { error = "آدرس یافت نشد." });
            return RedirectToAction(nameof(Index));
        }

        db.UserAddresses.Remove(entity);
        await db.SaveChangesAsync();

        if (ajax) return Json(new { ok = true });
        return RedirectToAction(nameof(Index));
    }

    static object MapAddressDto(UserAddress a) => new
    {
        a.Id,
        a.Label,
        a.Province,
        a.City,
        a.FullAddress,
        a.PostalCode,
        a.Plaque,
        a.Unit,
        a.Latitude,
        a.Longitude,
        a.IsDefault
    };

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendLoginOtp(string phone, bool ajax = false)
    {
        phone = MockOtpService.NormalizePhone(phone ?? "");
        if (string.IsNullOrWhiteSpace(phone) || phone.Length < 10)
        {
            if (ajax) return BadRequest(new { error = "شماره موبایل معتبر وارد کنید." });
            return Redirect("/?auth=1");
        }

        var exists = await userManager.Users.AnyAsync(u => u.PhoneNumber == phone);
        await otpService.SendAsync(phone);

        if (ajax)
            return Json(new { ok = true, devOtp = MockOtpService.DevCode, phone, isExistingUser = exists });

        TempData["Phone"] = phone;
        TempData["DevOtp"] = MockOtpService.DevCode;
        return Redirect("/?auth=1");
    }

    /// <summary>OTP login — existing users only; new users must complete registration.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp(string phone, string code, bool ajax = false)
    {
        phone = MockOtpService.NormalizePhone(phone ?? "");
        if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(code))
        {
            if (ajax) return BadRequest(new { error = "شماره و کد تأیید الزامی است." });
            return Redirect("/?auth=1");
        }

        if (!await otpService.VerifyAsync(phone, code))
        {
            if (ajax) return BadRequest(new { error = "کد نامعتبر یا منقضی شده است." });
            return Redirect("/?auth=1");
        }

        var user = await userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);

        if (user == null)
        {
            HttpContext.Session.SetString("PendingRegPhone", phone);
            await HttpContext.Session.CommitAsync();
            if (ajax) return Json(new { ok = true, needsRegistration = true, phone });
            return Redirect("/?auth=1");
        }

        HttpContext.Session.Remove("PendingRegPhone");
        await signInManager.SignInAsync(user, isPersistent: true);

        if (ajax) return Json(new { ok = true, redirect = Url.Action(nameof(Index)) });
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Verify OTP during registration (before profile form).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyRegisterOtp(string phone, string code, bool ajax = false)
    {
        phone = MockOtpService.NormalizePhone(phone ?? "");
        if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(code))
        {
            if (ajax) return BadRequest(new { error = "شماره و کد تأیید الزامی است." });
            return Redirect("/?auth=1");
        }

        if (!await otpService.VerifyAsync(phone, code))
        {
            if (ajax) return BadRequest(new { error = "کد نامعتبر یا منقضی شده است." });
            return Redirect("/?auth=1");
        }

        if (await userManager.Users.AnyAsync(u => u.PhoneNumber == phone))
        {
            if (ajax) return BadRequest(new { error = "این شماره قبلاً ثبت شده. وارد شوید." });
            return Redirect("/?auth=1");
        }

        HttpContext.Session.SetString("PendingRegPhone", phone);
        await HttpContext.Session.CommitAsync();

        if (ajax) return Json(new { ok = true, phone });
        return Redirect("/?auth=1");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWithPassword(string phone, string password, bool ajax = false)
    {
        phone = MockOtpService.NormalizePhone(phone ?? "");
        if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(password))
        {
            if (ajax) return BadRequest(new { error = "شماره و رمز عبور را وارد کنید." });
            return Redirect("/?auth=1");
        }

        var user = await userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);
        if (user == null)
        {
            if (ajax) return BadRequest(new { error = "کاربری با این شماره یافت نشد. ابتدا ثبت‌نام کنید." });
            return Redirect("/?auth=1");
        }

        if (!await userManager.HasPasswordAsync(user))
        {
            if (ajax) return BadRequest(new { error = "رمز عبور برای این حساب تنظیم نشده. با پیامک وارد شوید." });
            return Redirect("/?auth=1");
        }

        var result = await signInManager.PasswordSignInAsync(user.UserName!, password, isPersistent: true, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            if (ajax) return BadRequest(new { error = "رمز عبور اشتباه است." });
            return Redirect("/?auth=1");
        }

        if (ajax) return Json(new { ok = true, redirect = Url.Action(nameof(Index)) });
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteRegistration(
        string firstName,
        string lastName,
        string password,
        string confirmPassword,
        string? email,
        string? businessName,
        bool ajax = false)
    {
        var phone = HttpContext.Session.GetString("PendingRegPhone");
        if (string.IsNullOrWhiteSpace(phone))
        {
            if (ajax) return BadRequest(new { error = "نشست ثبت‌نام منقضی شده. دوباره از اول شروع کنید." });
            return Redirect("/?auth=1");
        }

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            if (ajax) return BadRequest(new { error = "نام و نام خانوادگی الزامی است." });
            return Redirect("/?auth=1");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            if (ajax) return BadRequest(new { error = "رمز عبور را وارد کنید." });
            return Redirect("/?auth=1");
        }

        if (password != confirmPassword)
        {
            if (ajax) return BadRequest(new { error = "رمز عبور و تکرار آن یکسان نیست." });
            return Redirect("/?auth=1");
        }

        if (await userManager.Users.AnyAsync(u => u.PhoneNumber == phone))
        {
            HttpContext.Session.Remove("PendingRegPhone");
            if (ajax) return BadRequest(new { error = "این شماره قبلاً ثبت شده. وارد شوید." });
            return Redirect("/?auth=1");
        }

        var created = await CreateCustomerAsync(phone, firstName, lastName, email, businessName, password);
        if (created.Error != null)
        {
            if (ajax) return BadRequest(new { error = created.Error });
            TempData["Error"] = created.Error;
            return Redirect("/?auth=1");
        }

        HttpContext.Session.Remove("PendingRegPhone");
        await signInManager.SignInAsync(created.User!, isPersistent: true);

        if (ajax) return Json(new { ok = true, redirect = Url.Action(nameof(Index)) });
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();

    async Task<(ApplicationUser? User, string? Error)> CreateCustomerAsync(
        string phone, string firstName, string lastName, string? email, string? businessName, string password)
    {
        var user = new ApplicationUser
        {
            UserName = phone,
            PhoneNumber = phone,
            PhoneNumberConfirmed = true,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            CompanyName = string.IsNullOrWhiteSpace(businessName) ? null : businessName.Trim()
        };
        user.SyncFullName();

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded) return (user, null);

        return (null, IdentityErrorMessage(result));
    }

    static string IdentityErrorMessage(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(e => e.Code switch
        {
            "DuplicateUserName" => "این شماره قبلاً ثبت شده است.",
            "DuplicateEmail" => "این ایمیل قبلاً استفاده شده است.",
            "PasswordTooShort" => "رمز عبور باید حداقل ۸ کاراکتر باشد.",
            "PasswordRequiresDigit" => "رمز عبور باید حداقل یک عدد داشته باشد.",
            "PasswordRequiresNonAlphanumeric" => "رمز عبور باید حداقل یک نماد داشته باشد.",
            "PasswordRequiresUpper" => "رمز عبور باید حداقل یک حرف بزرگ داشته باشد.",
            "PasswordRequiresLower" => "رمز عبور باید حداقل یک حرف کوچک داشته باشد.",
            _ => "خطا در ثبت اطلاعات. لطفاً دوباره تلاش کنید."
        }));

    static string BuildDisplayName(ApplicationUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.FullName)) return user.FullName.Trim();
        var first = user.FirstName?.Trim() ?? "";
        var last = user.LastName?.Trim() ?? "";
        var combined = $"{first} {last}".Trim();
        return string.IsNullOrWhiteSpace(combined) ? (user.PhoneNumber ?? "کاربر") : combined;
    }

    static (string First, string Last) ResolveNames(ApplicationUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.FirstName) || !string.IsNullOrWhiteSpace(user.LastName))
            return (user.FirstName ?? "", user.LastName ?? "");
        return SplitName(user.FullName);
    }

    static (string First, string Last) SplitName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return ("", "");
        var parts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => ("", ""),
            1 => (parts[0], ""),
            _ => (parts[0], parts[1])
        };
    }

    static string BuildInitials(string first, string last, string? phone)
    {
        if (!string.IsNullOrWhiteSpace(first))
            return (first[..1] + (last.Length > 0 ? last[..1] : "")).ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(phone) && phone.Length >= 2)
            return phone[^2..];
        return "م";
    }
}

public class ProfileController : Controller
{
    [Authorize]
    public IActionResult Index() => RedirectToAction("Index", "Account");
}
