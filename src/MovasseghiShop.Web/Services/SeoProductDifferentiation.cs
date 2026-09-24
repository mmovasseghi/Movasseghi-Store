using System.Text;
using System.Text.RegularExpressions;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

/// <summary>تفکیک واقعی محتوای محصول — فقط از داده تأیید‌شده، بدون حدس.</summary>
public static class SeoProductDifferentiation
{
    static readonly Regex PackPieces = new(@"(?<n>\d+)\s*قط", RegexOptions.CultureInvariant);
    static readonly Regex PackCount = new(@"بسته\s*(?<n>\d+)\s*عدد", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    static readonly Regex SizeHint = new(@"\b(کوچک|متوسط|بزرگ|مینی|جامبو)\b", RegexOptions.CultureInvariant);

    public static int Seed(Product product, int attempt = 0) =>
        unchecked(product.Id * 31 + attempt * 17 + (product.ProductCode?.GetHashCode(StringComparison.Ordinal) ?? 0));

    public static ProductDifferentiators Extract(Product product)
    {
        var name = product.Name.Trim();
        var packPieces = TryMatchInt(PackPieces, name);
        var packUnits = TryMatchInt(PackCount, name);
        var size = SizeHint.Match(name).Success ? SizeHint.Match(name).Value : null;

        return new ProductDifferentiators
        {
            Name = name,
            Sku = product.ProductCode?.Trim(),
            SizeHint = size,
            PackPieceCount = packPieces,
            PackUnitCount = packUnits,
            CapacityCc = product.CapacityCc,
            Compartments = product.CompartmentCount,
            Dimensions = product.Dimensions?.Trim(),
            Material = product.Material?.Trim(),
            Applications = product.Applications?.Trim(),
            ProductType = product.ProductType?.Trim(),
            MicrowaveSafe = product.MicrowaveSafe,
            VariantLabels = product.Variants
                .Where(v => !string.IsNullOrWhiteSpace(v.PackLabel) || !string.IsNullOrWhiteSpace(v.Sku))
                .Select(v => string.IsNullOrWhiteSpace(v.PackLabel)
                    ? $"SKU {v.Sku.Trim()}"
                    : v.PackLabel!.Trim())
                .Distinct()
                .Take(6)
                .ToList()
        };
    }

    /// <summary>پاراگراف یکتای ابتدای متن — حداکثر توکن متمایز در ۵۵ واژه اول.</summary>
    public static void AppendIdentityLead(StringBuilder sb, Product product, string marker, int attempt)
    {
        var d = Extract(product);
        var seed = Seed(product, attempt);
        var sku = string.IsNullOrWhiteSpace(d.Sku) ? "—" : H(d.Sku!);
        var name = H(d.Name);

        sb.Append("<h2>شناسنامه مدل ").Append(name).Append("</h2>");

        switch (seed % 6)
        {
            case 0:
                sb.Append("<p><strong>").Append(name).Append("</strong> با کد ")
                  .Append(sku).Append(" در خط ")
                  .Append(FormatType(d)).Append(" قرار دارد");
                AppendCapacityClause(sb, d);
                AppendPackClause(sb, d);
                sb.Append(". این صفحه فقط برای همین SKU نوشته شده تا با مدل‌های نزدیک در کاتالوگ اشتباه گرفته نشود.</p>");
                break;
            case 1:
                sb.Append("<p>تیم خرید عمده وقتی ").Append(marker)
                  .Append(" را جستجو می‌کند، معمولاً به دنبال مدل مشخصی مثل ")
                  .Append(name).Append(" (").Append(sku).Append(") است");
                AppendPackClause(sb, d);
                sb.Append(". تفاوت این SKU با سایر بشقاب‌ها/ظروف هم‌نام در جدول مشخصات و بخش «تفاوت با مدل‌های نزدیک» آمده است.</p>");
                break;
            case 2:
                sb.Append("<p>کد ").Append(sku).Append(" روی فاکتور موثقی برای ")
                  .Append(name).Append(" درج می‌شود");
                AppendCapacityClause(sb, d);
                AppendCompartmentClause(sb, d);
                sb.Append(". اگر به دنبال ظرفیت یا تعداد قطعه دیگری هستید، از لینک دسته محصولات مشابه را مقایسه کنید.</p>");
                break;
            case 3:
                sb.Append("<p>").Append(name).Append(" — ")
                  .Append(FormatMaterial(d)).Append(" — برای ")
                  .Append(FormatApps(d));
                AppendPackClause(sb, d);
                sb.Append(" عرضه می‌شود. شناسه انبار: ").Append(sku).Append(".</p>");
                break;
            case 4:
                sb.Append("<p>در سبد خرید عمده، ").Append(name)
                  .Append(" را با کد ").Append(sku).Append(" ثبت کنید");
                AppendSizeClause(sb, d);
                AppendCapacityClause(sb, d);
                sb.Append(" تا ارسال با همان بسته‌بندی تأییدشده انجام شود.</p>");
                break;
            default:
                sb.Append("<p>این صفحه مخصوص ").Append(name).Append(" است (SKU ")
                  .Append(sku).Append(")");
                AppendPackClause(sb, d);
                AppendVariantClause(sb, d);
                sb.Append(". متن زیر بر اساس همین مشخصات فنی تنظیم شده، نه الگوی عمومی سایر محصولات.</p>");
                break;
        }
    }

    public static void AppendDifferentiationSection(StringBuilder sb, Product product, string shortLabel, int attempt)
    {
        var d = Extract(product);
        var peer = H(shortLabel);
        sb.Append("<h2>تفاوت ").Append(H(d.Name))
          .Append(" با مدل‌های نزدیک ").Append(peer).Append("</h2>");
        sb.Append("<ul>");

        if (d.PackPieceCount is > 0)
            sb.Append("<li><strong>تعداد قطعه در بسته:</strong> ").Append(d.PackPieceCount.Value)
              .Append(" قط — در نام محصول و SKU قابل ردیابی است.</li>");
        if (d.PackUnitCount is > 0)
            sb.Append("<li><strong>بسته‌بندی فروش:</strong> ").Append(d.PackUnitCount.Value)
              .Append(" عدد در هر بسته عمده.</li>");
        if (!string.IsNullOrWhiteSpace(d.SizeHint))
            sb.Append("<li><strong>کلاس سایز:</strong> ").Append(H(d.SizeHint))
              .Append(" — برای تفکیک از مدل‌های کوچک‌تر/بزرگ‌تر هم‌خانواده.</li>");
        if (d.CapacityCc is > 0)
            sb.Append("<li><strong>ظرفیت حجمی:</strong> ").Append(d.CapacityCc.Value)
              .Append(" سی‌سی — معیار اصلی انتخاب برای پرس و کنترل هزینه.</li>");
        if (d.Compartments is > 0)
            sb.Append("<li><strong>چیدمان خانه:</strong> ").Append(d.Compartments.Value)
              .Append(" خانه — برای منوهای چند‌ماده متفاوت از مدل تک‌خانه است.</li>");
        if (!string.IsNullOrWhiteSpace(d.Dimensions))
            sb.Append("<li><strong>ابعاد ثبت‌شده:</strong> ").Append(H(d.Dimensions!))
              .Append("</li>");
        if (d.MicrowaveSafe)
            sb.Append("<li><strong>مایکروویو:</strong> برای این SKU سازگاری گرم‌کردن اعلام شده است.</li>");

        foreach (var v in d.VariantLabels.Take(3))
            sb.Append("<li><strong>گونه موجود:</strong> ").Append(H(v)).Append("</li>");

        if (d.PackPieceCount is null && d.PackUnitCount is null && d.CapacityCc is null
            && string.IsNullOrWhiteSpace(d.Dimensions) && d.Compartments is null)
        {
            sb.Append("<li>برای تفکیک از محصولات مشابه، کد ")
              .Append(H(d.Sku ?? "—"))
              .Append(" و نام کامل ").Append(H(d.Name))
              .Append(" مرجع رسمی سفارش است.</li>");
        }

        sb.Append("</ul>");

        var scenario = (Seed(product, attempt) % 4) switch
        {
            0 => $"برای خط سرو {FormatApps(d)}، انتخاب {d.Name} وقتی منطقی است که پرس و بسته‌بندی با ظرفیت ثبت‌شده هم‌خوان باشد.",
            1 => $"اگر چند مدل {peer} در کاتالوگ دارید، ابتدا مصرف ماهانه و سپس {FormatMaterial(d)} و کد {d.Sku ?? "SKU"} را فیلتر کنید.",
            2 => $"در مذاکره عمده، ذکر کد {d.Sku ?? "محصول"} از اشتباه در ارسال نسخه کوچک‌تر/بزرگ‌تر جلوگیری می‌کند.",
            _ => $"صفحه‌های دیگر ممکن است {peer} مشابه داشته باشند؛ این متن فقط سناریوی خرید {d.Name} را پوشش می‌دهد."
        };
        sb.Append("<p>").Append(H(scenario)).Append("</p>");
    }

    static void AppendCapacityClause(StringBuilder sb, ProductDifferentiators d)
    {
        if (d.CapacityCc is > 0)
            sb.Append("؛ ظرفیت ").Append(d.CapacityCc.Value).Append(" سی‌سی");
    }

    static void AppendCompartmentClause(StringBuilder sb, ProductDifferentiators d)
    {
        if (d.Compartments is > 0)
            sb.Append("؛ ").Append(d.Compartments.Value).Append(" خانه");
    }

    static void AppendPackClause(StringBuilder sb, ProductDifferentiators d)
    {
        if (d.PackPieceCount is > 0)
            sb.Append("؛ ").Append(d.PackPieceCount.Value).Append(" قطعه در بسته");
        else if (d.PackUnitCount is > 0)
            sb.Append("؛ بسته ").Append(d.PackUnitCount.Value).Append(" عددی");
    }

    static void AppendSizeClause(StringBuilder sb, ProductDifferentiators d)
    {
        if (!string.IsNullOrWhiteSpace(d.SizeHint))
            sb.Append(" (سایز ").Append(H(d.SizeHint!)).Append(')');
    }

    static void AppendVariantClause(StringBuilder sb, ProductDifferentiators d)
    {
        if (d.VariantLabels.Count == 0) return;
        sb.Append("؛ گونه‌های ").Append(H(string.Join("، ", d.VariantLabels.Take(2))));
    }

    static string H(string value) => value
        .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    static string FormatType(ProductDifferentiators d) =>
        string.IsNullOrWhiteSpace(d.ProductType) ? "ظروف یکبار مصرف" : d.ProductType!;

    static string FormatMaterial(ProductDifferentiators d) =>
        string.IsNullOrWhiteSpace(d.Material) ? "جنس ثبت‌شده در مشخصات" : d.Material!;

    static string FormatApps(ProductDifferentiators d) =>
        string.IsNullOrWhiteSpace(d.Applications) ? "رستوران و کیترینگ" : d.Applications!;

    static int? TryMatchInt(Regex rx, string text)
    {
        var m = rx.Match(text);
        return m.Success && int.TryParse(m.Groups["n"].Value, out var n) ? n : null;
    }

    public sealed class ProductDifferentiators
    {
        public string Name { get; init; } = "";
        public string? Sku { get; init; }
        public string? SizeHint { get; init; }
        public int? PackPieceCount { get; init; }
        public int? PackUnitCount { get; init; }
        public int? CapacityCc { get; init; }
        public int? Compartments { get; init; }
        public string? Dimensions { get; init; }
        public string? Material { get; init; }
        public string? Applications { get; init; }
        public string? ProductType { get; init; }
        public bool MicrowaveSafe { get; init; }
        public IReadOnlyList<string> VariantLabels { get; init; } = [];
    }
}
