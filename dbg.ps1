$ErrorActionPreference = "Stop"
$base = "http://localhost:5274"
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
function Get-Token($url) {
    $p = Invoke-WebRequest -Uri "$base$url" -WebSession $session -UseBasicParsing
    return ([regex]'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Match($p.Content).Groups[1].Value
}
function Strip($html) { return ($html -replace '<[^>]+>', ' ' -replace '\s+', ' ') }

$t = Get-Token "/"
Invoke-WebRequest -Uri "$base/Account/LoginWithPassword" -Method Post -WebSession $session -UseBasicParsing -Body @{
    __RequestVerificationToken = $t; phone = "09125199105"; password = "Admin@123456"; ajax = "true" } | Out-Null

function Show($label, $url, $tokenPage, $fields) {
    $fields["__RequestVerificationToken"] = (Get-Token $tokenPage)
    $r = Invoke-WebRequest -Uri "$base$url" -Method Post -WebSession $session -UseBasicParsing -Body $fields
    Write-Host "`n===== $label ====="
    Write-Host "status=$($r.StatusCode) len=$($r.Content.Length)"
    Write-Host "title: " ([regex]'<title>([\s\S]*?)</title>').Match($r.Content).Groups[1].Value
    $m = ([regex]'kit-insight-body[\s\S]{0,700}?</div>\s*</div>').Match($r.Content)
    if ($m.Success) { Write-Host "ERRORS: " (Strip $m.Value) } else { Write-Host "-- no error block --" }
    $toast = ([regex]'data-kit-flash[^>]*>([\s\S]{0,200}?)<').Match($r.Content)
    if ($toast.Success) { Write-Host "FLASH: " $toast.Groups[1].Value.Trim() }
}

# دو بار پشت‌سرهم همان کد — بار دوم باید رد شود
Show "coupon #1" "/Admin/Coupons/Save" "/Admin/Coupons/Create" @{ Id="0"; Code="DBGDUP"; Type="1"; Value="15"; UsedCount="0"; IsActive="true" }
Show "coupon #2 (dup)" "/Admin/Coupons/Save" "/Admin/Coupons/Create" @{ Id="0"; Code="DBGDUP"; Type="1"; Value="15"; UsedCount="0"; IsActive="true" }
Show "coupon 150%" "/Admin/Coupons/Save" "/Admin/Coupons/Create" @{ Id="0"; Code="DBGOVER"; Type="1"; Value="150"; UsedCount="0"; IsActive="true" }
Show "faq no answer" "/Admin/FaqAdmin/Save" "/Admin/FaqAdmin/Create" @{ Id="0"; Question="dbg no answer"; Answer=""; SortOrder="0"; IsActive="true" }
