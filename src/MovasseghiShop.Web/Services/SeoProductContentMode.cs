namespace MovasseghiShop.Web.Services;

/// <summary>کنترل بازنویسی توضیحات محصول در Auto-Fix — جلوگیری از خراب‌کردن محتوای دستی در نگهداری دسته‌جمعی.</summary>
public enum SeoProductContentMode
{
    /// <summary>متادیتا، Alt، FAQ و ابعاد غیرمتنی — بدون بازنویسی بدنهٔ قفل‌شده یا دستی.</summary>
    PreserveLocked = 0,

    /// <summary>بازنویسی کامل فقط وقتی LockProductDescription خاموش است (پیش‌فرض دکمه Auto-Fix تکی).</summary>
    RegenerateWhenUnlocked = 1,

    /// <summary>بازنویسی اجباری — فقط CLI / بازیابی با قصد صریح.</summary>
    ForceRegenerate = 2
}
