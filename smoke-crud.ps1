# تست CRUD واقعی — چیزی می‌سازد، تغییرش می‌دهد و حذف می‌کند تا مطمئن شویم فرم‌ها واقعاً کار می‌کنند
$ErrorActionPreference = "Stop"
$base = "http://localhost:5274"
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

function Get-Token($url) {
    $p = Invoke-WebRequest -Uri "$base$url" -WebSession $session -UseBasicParsing
    return ([regex]'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Match($p.Content).Groups[1].Value
}

$t = Get-Token "/"
Invoke-WebRequest -Uri "$base/Account/LoginWithPassword" -Method Post -WebSession $session -UseBasicParsing -Body @{
    __RequestVerificationToken = $t; phone = "09125199105"; password = "Admin@123456"; ajax = "true"
} | Out-Null

function Test-Post($label, $url, $fields, $tokenPage) {
    try {
        $tok = Get-Token $tokenPage
        $fields["__RequestVerificationToken"] = $tok
        $r = Invoke-WebRequest -Uri "$base$url" -Method Post -Body $fields -WebSession $session -UseBasicParsing
        Write-Host ("OK   {0,-28} {1}" -f $label, $r.StatusCode)
        return $r
    } catch {
        $code = if ($_.Exception.Response) { $_.Exception.Response.StatusCode.value__ } else { "ERR" }
        Write-Host ("FAIL {0,-28} {1}  {2}" -f $label, $code, $_.Exception.Message)
        return $null
    }
}

# ── FAQ ──
Test-Post "faq create" "/Admin/FaqAdmin/Save" @{
    Id = "0"; Question = "تست خودکار: حداقل سفارش چند کارتن است؟"
    Answer = "حداقل سفارش عمده در فروشگاه موثقی ده کارتن است. برای سفارش‌های کمتر از این مقدار می‌توانید از طریق فرم استعلام قیمت درخواست بدهید تا شرایط ویژه بررسی شود."
    SortOrder = "99"; IsActive = "true"
} "/Admin/FaqAdmin/Create" | Out-Null

$faqPage = Invoke-WebRequest -Uri "$base/Admin/FaqAdmin" -WebSession $session -UseBasicParsing
$faqId = ([regex]'/Admin/FaqAdmin/Edit/(\d+)"[^>]*>ویرایش').Matches($faqPage.Content) | Select-Object -Last 1
$faqId = $faqId.Groups[1].Value
Write-Host "     faq id = $faqId"

# ── اعتبارسنجی: پاسخ خالی باید رد شود ──
$bad = Test-Post "faq invalid rejected" "/Admin/FaqAdmin/Save" @{
    Id = "0"; Question = "بدون پاسخ؟"; Answer = ""; SortOrder = "0"; IsActive = "true"
} "/Admin/FaqAdmin/Create"
if ($bad -and $bad.Content -match "الزامی است") { Write-Host "     validation message shown OK" }
else { Write-Host "     !! validation message MISSING" }

# ── کوپن ──
Test-Post "coupon create" "/Admin/Coupons/Save" @{
    Id = "0"; Code = "SMOKETEST1"; Type = "1"; Value = "15"; MinCartons = "10"; UsedCount = "0"; IsActive = "true"
} "/Admin/Coupons/Create" | Out-Null

$cPage = Invoke-WebRequest -Uri "$base/Admin/Coupons" -WebSession $session -UseBasicParsing
if ($cPage.Content -match "SMOKETEST1") { Write-Host "     coupon appears in list OK" } else { Write-Host "     !! coupon NOT in list" }
$cId = (([regex]'/Admin/Coupons/Edit/(\d+)').Matches($cPage.Content) | Select-Object -Last 1).Groups[1].Value

# ── کوپن تکراری باید رد شود ──
$dup = Test-Post "coupon dup rejected" "/Admin/Coupons/Save" @{
    Id = "0"; Code = "SMOKETEST1"; Type = "1"; Value = "10"; UsedCount = "0"; IsActive = "true"
} "/Admin/Coupons/Create"
if ($dup -and $dup.Content -match "قبلاً ثبت شده") { Write-Host "     duplicate blocked OK" } else { Write-Host "     !! duplicate NOT blocked" }

# ── درصد بالای ۱۰۰ باید رد شود ──
$over = Test-Post "coupon >100% rejected" "/Admin/Coupons/Save" @{
    Id = "0"; Code = "SMOKETEST2"; Type = "1"; Value = "150"; UsedCount = "0"; IsActive = "true"
} "/Admin/Coupons/Create"
if ($over -and $over.Content -match "بیش از ۱۰۰") { Write-Host "     >100% blocked OK" } else { Write-Host "     !! >100% NOT blocked" }

# ── مقاله بلاگ ──
Test-Post "blog create" "/Admin/BlogAdmin/Save" @{
    Id = "0"; Title = "تست خودکار: راهنمای انتخاب ظرف گیاهی"
    Content = "<h2>ظرف گیاهی چیست؟</h2><p>ظرف گیاهی از نشاسته ذرت و نیشکر ساخته می‌شود و در طبیعت تجزیه می‌شود.</p>"
    Excerpt = "راهنمای کوتاه انتخاب ظرف گیاهی مناسب رستوران"
    MetaTitle = "راهنمای انتخاب ظرف گیاهی"; MetaDescription = "چطور ظرف گیاهی مناسب کسب‌وکارتان را انتخاب کنید"
    IsPublished = "true"
} "/Admin/BlogAdmin/Create" | Out-Null

$bPage = Invoke-WebRequest -Uri "$base/Admin/BlogAdmin" -WebSession $session -UseBasicParsing
$bId = (([regex]'/Admin/BlogAdmin/Edit/(\d+)').Matches($bPage.Content) | Select-Object -Last 1).Groups[1].Value
Write-Host "     blog id = $bId"

# ── بررسی نمایش مقاله در سایت عمومی ──
try {
    $pub = Invoke-WebRequest -Uri "$base/Blog" -WebSession $session -UseBasicParsing
    if ($pub.Content -match "راهنمای انتخاب ظرف گیاهی") { Write-Host "     blog visible on public site OK" }
    else { Write-Host "     !! blog NOT visible on /Blog" }
} catch { Write-Host "     !! /Blog error" }

# ── toggle ──
Test-Post "blog toggle publish" "/Admin/BlogAdmin/TogglePublish/$bId" @{} "/Admin/BlogAdmin" | Out-Null

# ── پاک‌سازی ──
Test-Post "blog delete"   "/Admin/BlogAdmin/Delete/$bId" @{} "/Admin/BlogAdmin" | Out-Null
Test-Post "coupon delete" "/Admin/Coupons/Delete/$cId"   @{} "/Admin/Coupons"   | Out-Null
Test-Post "faq delete"    "/Admin/FaqAdmin/Delete/$faqId" @{} "/Admin/FaqAdmin"  | Out-Null
