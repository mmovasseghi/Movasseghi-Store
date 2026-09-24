# اندازه واقعی روی شبکه (با برتلی) برای HTML و همه دارایی‌های سراسری
$base = "http://localhost:5274"

function Measure-Url($url) {
    try {
        $r = Invoke-WebRequest -Uri $url -UseBasicParsing -Headers @{ "Accept-Encoding" = "br, gzip" }
        $len = $r.RawContentLength
        if (-not $len -or $len -eq 0) { $len = $r.Content.Length }
        return @{ ok = $true; bytes = $len; enc = ($r.Headers["Content-Encoding"] -join ",") }
    } catch { return @{ ok = $false; bytes = 0; enc = "" } }
}

Write-Host "── HTML (over the wire, brotli) ──"
$total = 0
foreach ($p in @("/", "/Catalog", "/Blog", "/News")) {
    $m = Measure-Url "$base$p"
    Write-Host ("  {0,-12} {1,8} B   {2}" -f $p, $m.bytes, $m.enc)
}

Write-Host "`n── assets loaded on EVERY page ──"
$assets = @(
    "/fonts/Vazirmatn-var.woff2",
    "/css/site.css", "/css/prose.css", "/css/ux-cart.css", "/css/ux-mobile.css", "/css/ux-desktop.css",
    "/js/vendor/gsap.min.js", "/js/vendor/ScrollTrigger.min.js",
    "/js/auth.js", "/js/site.js", "/js/desktop.js",
    "/images/brand/originallogo.png"
)
foreach ($a in $assets) {
    $m = Measure-Url "$base$a"
    if ($m.ok) { $total += $m.bytes; Write-Host ("  {0,-34} {1,8} B   {2}" -f $a, $m.bytes, $m.enc) }
    else { Write-Host ("  {0,-34}   MISSING" -f $a) }
}
Write-Host ("`n  TOTAL per-page assets: {0} B  ({1} KB)" -f $total, [math]::Round($total/1KB,1))

Write-Host "`n── external requests still in HTML ──"
$html = (Invoke-WebRequest -Uri "$base/" -UseBasicParsing).Content
[regex]::Matches($html, '(?:src|href)="(https?://[^"]+)"') | ForEach-Object { $_.Groups[1].Value } |
    ForEach-Object { ([uri]$_).Host } | Sort-Object -Unique | ForEach-Object { Write-Host "  $_" }
