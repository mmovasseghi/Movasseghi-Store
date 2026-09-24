# تست دود پنل مدیریت — با شماره موبایل و رمز وارد می‌شود، بعد همه مسیرها را می‌زند
$ErrorActionPreference = "Stop"
$base = "http://localhost:5274"
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

$landing = Invoke-WebRequest -Uri "$base/" -WebSession $session -UseBasicParsing
$token = ([regex]'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Match($landing.Content).Groups[1].Value
if (-not $token) { Write-Host "NO ANTIFORGERY TOKEN ON HOME"; exit 1 }

$body = @{
    __RequestVerificationToken = $token
    phone                      = "09125199105"
    password                   = "Admin@123456"
    ajax                       = "true"
}
try {
    $r = Invoke-WebRequest -Uri "$base/Account/LoginWithPassword" -Method Post -Body $body -WebSession $session -UseBasicParsing
    Write-Host "LOGIN: $($r.StatusCode) $($r.Content)"
} catch {
    Write-Host "LOGIN FAILED: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        Write-Host $sr.ReadToEnd()
    }
    exit 1
}

$paths = @(
    "/Admin",
    "/Admin/Reports",
    "/Admin/Reports/Traffic",
    "/Admin/Reports/Sales",
    "/Admin/Inquiries",
    "/Admin/Inquiries?status=open",
    "/Admin/Coupons",
    "/Admin/Coupons/Create",
    "/Admin/BlogAdmin",
    "/Admin/BlogAdmin/Create",
    "/Admin/NewsAdmin",
    "/Admin/NewsAdmin/Create",
    "/Admin/FaqAdmin",
    "/Admin/FaqAdmin/Create",
    "/Admin/Products",
    "/Admin/Categories",
    "/Admin/Orders",
    "/Admin/Users",
    "/Admin/Studio",
    "/Admin/Studio/RankRadar",
    "/",
    "/Catalog",
    "/Blog",
    "/News"
)

$fails = 0
foreach ($p in $paths) {
    try {
        $r = Invoke-WebRequest -Uri "$base$p" -WebSession $session -UseBasicParsing -MaximumRedirection 5
        $len = $r.Content.Length
        $flag = ""
        if ($p.StartsWith("/Admin") -and $len -lt 5000) { $flag = "  <-- SUSPICIOUSLY SMALL (auth redirect?)"; $fails++ }
        Write-Host ("OK   {0,-32} {1}  {2}b{3}" -f $p, $r.StatusCode, $len, $flag)
    } catch {
        $code = if ($_.Exception.Response) { $_.Exception.Response.StatusCode.value__ } else { "ERR" }
        Write-Host ("FAIL {0,-32} {1}" -f $p, $code)
        $fails++
    }
}
Write-Host "`nproblems: $fails"
