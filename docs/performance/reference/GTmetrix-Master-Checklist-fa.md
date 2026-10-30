# مرجع اصلی تحقیق GTmetrix: نام بررسی، شرط، حالت و منطق امتیاز

تاریخ: ۲۰۲۶-۱۰-۳۰. این مرحله تحقیق است؛ کد فروشگاه و production تغییر نکرد.

## وضعیت تکمیل و دامنهٔ شواهد

این فایل جایگزین معرفی فهرست ۵۹/۷۶تایی به‌عنوان پاسخ کامل GTmetrix می‌شود.
**فهرست و وزن قطعی همهٔ ممیزی‌های Structure نسخهٔ جاری GTmetrix هنوز از منبع اصلی تأیید نشده‌اند.**
در این سند هر شاهد، نسخه و محدودیت دارد؛ تعداد فایل‌های موتور، تعداد شروط امتیازدار GT نیست.

بررسی منابع این بار شامل همهٔ مخازن عمومی نمایش‌داده‌شده در پروفایل GTmetrix
(WordPress، GTspeed و pagespeed-library)، کد سه نسخهٔ Lighthouse، موتور Insight پین‌شده،
YSlow اصلی، کلاینت‌های API و جست‌وجوی مخازن گزارش عمومی شد.
برخی مخازنِ دارای نام «GTmetrix audit» فقط قالب گزارش AI یا launcher بودند؛
برای فهرست قواعد و امتیازدهی به آن‌ها استناد نشده است.

چهار مجموعهٔ متفاوت داریم:

1. معیارهایی که مستقیماً نمرهٔ Performance می‌سازند.
2. ممیزی‌ها، opportunities و insights که مشکل/صرفه‌جویی را گزارش می‌کنند.
3. فیلدهای اطلاعاتی، نمودارها و timingها؛ بدون امتیاز مستقل فرض‌شده.
4. قواعد PageSpeed/YSlow تاریخی؛ این‌ها نباید شروط فعال Structure امروز معرفی شوند.

## ۱. معیارهای مستقیم و منحنی امتیاز؛ Lighthouse 12.0.0

| معیار | وزن Performance | Desktop: مقدارِ نقطهٔ ۹۰ / نقطهٔ ۵۰ | Mobile: مقدارِ نقطهٔ ۹۰ / نقطهٔ ۵۰ |
| --- | ---: | --- | --- |
| FCP | ۱۰٪ | 934 / 1600 ms | 1800 / 3000 ms |
| LCP | ۲۵٪ | 1200 / 2400 ms | 2500 / 4000 ms |
| TBT | ۳۰٪ | 150 / 350 ms | 200 / 600 ms |
| CLS | ۲۵٪ | 0.1 / 0.25 | 0.1 / 0.25 |
| Speed Index | ۱۰٪ | 1311 / 2300 ms | 3387 / 5800 ms |

این‌ها نقاط کنترل منحنی log-normal همان نسخه‌اند، نه مرز همیشگی قبول/رد، نه
Core Web Vitals کاربران واقعی و نه وزن Structure GTmetrix. به‌ویژه «LCP زیر ۲٫۵ ثانیه»
به‌تنهایی Performance برابر ۹۷ در تست Desktop را تضمین نمی‌کند.

[منبع تابع امتیاز](https://github.com/GoogleChrome/lighthouse/blob/v12.0.0/core/audits/audit.js#L99)
و [پیکربندی وزن‌ها](https://github.com/GoogleChrome/lighthouse/blob/v12.0.0/core/config/default-config.js).

## ۲. انواع نتیجه؛ قبل از شمردن «چک‌های امتیازدار»

- `numeric`: نتیجهٔ عددی، معمولاً پیوسته؛ لزوماً یک حد قبول/رد ثابت ندارد.
- `binary`: قبول/رد عددی.
- `metricSavings`: نتیجه همراه با تخمین اثر بر معیارها؛ همان درصد وزن GT نیست.
- `informative`: اطلاعات تشخیصی؛ نباید آن را یک شرط امتیازدار مستقل شمرد.
- `notApplicable`: در آن اجرا قابل اعمال نیست؛ نباید با نمرهٔ صفر اشتباه شود.
- `error` یا دادهٔ ناکافی: با «قبول» یکی نیست.
- بعضی ممیزی‌ها حالت خروجی را در runtime تغییر می‌دهند؛ metadata به‌تنهایی کافی نیست.

در adapter رسمی Insight نسخهٔ 12.8.2، state برابر pass به score=1، fail به score=0،
و حالت‌های اطلاعاتی به score=null تبدیل می‌شود. حضور این نتیجه در نمرهٔ Structure GT
نیازمند تطبیق مستقل با موتور GT است.

## ۳. شروط امتیاز از کد؛ نمونه‌های روشن

| مورد | شرط/محاسبهٔ مشاهده‌شده | محدودیت |
| --- | --- | --- |
| server-response-time | `responseTime < 600`؛ هدف بهبود در کد 100ms است | مقدار اندازه‌گیری‌شدهٔ همین audit؛ با کل TTFB هر ابزار یکی فرض نشود |
| ByteEfficiencyAudit | نمرهٔ صرفه‌جویی از منحنی با p10=150ms و median=935ms | مقدار صرفه‌جویی شبیه‌سازی می‌شود؛ قانون همگانی «حجم کمتر از X» نیست |
| DOM size | نقاط کنترل 818 / 1400 برای تابع عددی | نقاط منحنی‌اند؛ دستور «هر سایتی بالای 818 fail» نیست |
| JS bootup | p10=1282ms و median=3500ms؛ فیلتر ردیف منابع thresholdInMs=50 | آستانهٔ نمایش ردیف با آستانهٔ امتیاز یکی نیست |
| Main-thread work | p10=2017ms و median=4000ms | نمرهٔ همین ممیزی، نه وزن Performance اصلی |
| Total byte weight | p10=2667×1024 و median=4000×1024 bytes | این اعداد را سقف توصیه‌شدهٔ حجم سایت ننامید |
| Font display | score از خالی‌بودن فهرست موارد مشکل‌دار ساخته می‌شود | انتخاب فونت و بارگذاری باید با CLS/نمایش متن نیز آزموده شود |
| HTTP/2 | score از وجود/نبود resourceهای مشکل‌دار ساخته می‌شود | هر URL در هر اجرا قابل اعمال نیست |
| Long-cache TTL | در مسیر مشاهده‌شده، score با `results.length ? 0 : 1` ساخته می‌شود | صرف وجود defaultOptions عددی، اثبات استفادهٔ آن در شرط نهایی نیست |

تمام ثابت‌ها، defaultOptions، expressionهای score و شرط‌های if برای هر ۷۶ فایل در
`audit-scoring-evidence.json` استخراج شده‌اند. این استخراج با parser انجام شد؛ کد
ممیزی‌ها اجرا نشد. هر expression همراه شمارهٔ خط و لینک منبع است.

## ۴. جدول کامل منابع مدرن بررسی‌شده

ستون «نحوهٔ نتیجه» از همان پیاده‌سازی استخراج شده است؛ وزن عددی Structure GT برای
ردیف‌ها تأیید نشده. شمارهٔ نسخهٔ منطق امتیاز ممکن است با آخرین نسخهٔ دارای همان شناسه
متفاوت باشد؛ برای تفاوت نسخه‌ها به `expanded-audit-catalog.csv` رجوع کنید.

| شناسه و منبع | بررسی | نحوهٔ نتیجهٔ اعلام‌شده | expression امتیاز |
| --- | --- | --- | --- |
| [`first-contentful-paint`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/metrics/first-contentful-paint.js) | زمان اولین نمایش محتوایی؛ FCP | NUMERIC | `L73: Audit.computeLogNormalScore(         options.scoring,         metricResult.timing       )` |
| [`largest-contentful-paint`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/metrics/largest-contentful-paint.js) | زمان نمایش بزرگ‌ترین عنصر محتوایی؛ LCP | NUMERIC | `L82: Audit.computeLogNormalScore(         options.scoring,         metricResult.timing       )` |
| [`total-blocking-time`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/metrics/total-blocking-time.js) | زمان مسدودشدن نخ اصلی بر اثر بخشِ بیش از ۵۰ms کارهای طولانی؛ TBT | NUMERIC | `L97: 1; L106: Audit.computeLogNormalScore(         options.scoring,         metricResult.timing       )` |
| [`cumulative-layout-shift`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/metrics/cumulative-layout-shift.js) | جابه‌جایی غیرمنتظرهٔ چیدمان؛ CLS | NUMERIC | `L72: Audit.computeLogNormalScore(         scoringOptions,         cumulativeLayoutShift       )` |
| [`speed-index`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/metrics/speed-index.js) | سرعت کامل‌شدن بصری صفحه؛ Speed Index | NUMERIC | `L76: Audit.computeLogNormalScore(         options.scoring,         metricResult.timing       )` |
| [`interaction-to-next-paint`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/metrics/interaction-to-next-paint.js) | تأخیر پاسخ بصری به تعامل؛ INP، فقط Timespan در این نسخه و بدون وزن Performance | NUMERIC | `L61: null; L70: null; L76: Audit.computeLogNormalScore({p10: context.options.p10, median: context.options.median},         timing)` |
| [`interactive`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/metrics/interactive.js) | زمان تعاملی‌شدن؛ TTI، معیار قدیمی بدون وزن Performance | NUMERIC | `L81: Audit.computeLogNormalScore(         options.scoring,         timeInMs       )` |
| [`max-potential-fid`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/metrics/max-potential-fid.js) | برآورد بیشترین تأخیر بالقوهٔ ورودی اول؛ بدون وزن Performance | NUMERIC | `L133: Audit.computeLogNormalScore(         {p10: context.options.p10, median: context.options.median},         metricResult.timing       )` |
| [`render-blocking-resources`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/render-blocking-resources.js) | منابع مسدودکنندهٔ رندر اولیه | METRIC_SAVINGS | `L305: results.length ? 0 : 1` |
| [`uses-responsive-images`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/uses-responsive-images.js) | تناسب ابعاد فایل تصویر با اندازهٔ نمایش | METRIC_SAVINGS | `` |
| [`offscreen-images`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/offscreen-images.js) | بارگیری زودهنگام تصاویر خارج viewport | METRIC_SAVINGS | `` |
| [`unminified-css`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/unminified-css.js) | CSS کوچک‌سازی‌نشده | METRIC_SAVINGS | `` |
| [`unminified-javascript`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/unminified-javascript.js) | JavaScript کوچک‌سازی‌نشده | METRIC_SAVINGS | `` |
| [`unused-css-rules`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/unused-css-rules.js) | CSS استفاده‌نشده | METRIC_SAVINGS | `` |
| [`unused-javascript`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/unused-javascript.js) | JavaScript استفاده‌نشده | METRIC_SAVINGS | `` |
| [`uses-optimized-images`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/uses-optimized-images.js) | فشرده‌سازی و رمزگذاری ناکارآمد تصاویر | METRIC_SAVINGS | `` |
| [`modern-image-formats`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/modern-image-formats.js) | فرمت‌های تصویری جدید و کم‌حجم | METRIC_SAVINGS | `` |
| [`uses-text-compression`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/uses-text-compression.js) | فشرده‌سازی منابع متنی، مانند gzip/Brotli | METRIC_SAVINGS | `` |
| [`uses-rel-preconnect`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/uses-rel-preconnect.js) | اتصال زودهنگام به originهای لازم؛ preconnect | METRIC_SAVINGS | `L253: 1; L270: results.length ? 0 : 1` |
| [`server-response-time`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/server-response-time.js) | زمان پاسخ اولیهٔ سرور | METRIC_SAVINGS | `L94: Number(passed)` |
| [`redirects`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/redirects.js) | ریدایرکت‌های متعدد صفحه | METRIC_SAVINGS | `L147: tableRows.length ? 0 : 1` |
| [`uses-http2`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/dobetterweb/uses-http2.js) | استفاده از HTTP/2 برای منابع | METRIC_SAVINGS | `L231: resources.length ? 0 : 1; L271: resources.length ? 0 : 1` |
| [`efficient-animated-content`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/efficient-animated-content.js) | جایگزینی محتوای متحرک سنگین مانند GIF با ویدئوی مناسب | METRIC_SAVINGS | `` |
| [`duplicated-javascript`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/duplicated-javascript.js) | ماژول‌های JavaScript تکراری در باندل‌ها | METRIC_SAVINGS | `` |
| [`legacy-javascript`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/legacy-javascript.js) | JavaScript قدیمی، polyfill و تبدیل‌های غیرضروری برای مرورگر جدید | METRIC_SAVINGS | `` |
| [`prioritize-lcp-image`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/prioritize-lcp-image.js) | اولویت و preload تصویر LCP | METRIC_SAVINGS | `L247: null; L284: results.length ? 0 : 1` |
| [`total-byte-weight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/total-byte-weight.js) | حجم کل منابع شبکه | METRIC_SAVINGS | `L101: score` |
| [`uses-long-cache-ttl`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/uses-long-cache-ttl.js) | عمر کش منابع استاتیک | METRIC_SAVINGS | `L283: results.length ? 0 : 1` |
| [`dom-size`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/dobetterweb/dom-size.js) | تعداد، عمق و تراکم عناصر DOM | METRIC_SAVINGS | `L170: score` |
| [`critical-request-chains`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/critical-request-chains.js) | زنجیره‌های وابستگی درخواست‌های بحرانی | INFORMATIVE | `L206: Number(chainCount === 0)` |
| [`user-timings`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/user-timings.js) | علامت‌ها و اندازه‌گیری‌های Performance API؛ User Timing | INFORMATIVE | `L110: Number(userTimings.length === 0)` |
| [`bootup-time`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/bootup-time.js) | زمان parse/compile/اجرای JavaScript | METRIC_SAVINGS | `L175: score` |
| [`mainthread-work-breakdown`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/mainthread-work-breakdown.js) | تفکیک کار نخ اصلی؛ اسکریپت، استایل، layout و دیگر فعالیت‌ها | METRIC_SAVINGS | `L144: score` |
| [`font-display`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/font-display.js) | قابل‌مشاهده‌بودن متن هنگام دریافت فونت | METRIC_SAVINGS | `L187: Number(results.length === 0)` |
| [`third-party-summary`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/third-party-summary.js) | هزینهٔ منابع و اجرای سرویس‌های شخص ثالث | METRIC_SAVINGS | `L207: 1; L219: Number(passed)` |
| [`third-party-facades`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/third-party-facades.js) | جایگزین سبک برای بارگیری تنبل embedهای شخص ثالث | METRIC_SAVINGS | `L207: 1; L223: 0` |
| [`largest-contentful-paint-element`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/largest-contentful-paint-element.js) | شناسایی عنصر واقعی LCP | METRIC_SAVINGS | `L131: null; L165: lcpSavings ? 0 : 1` |
| [`lcp-lazy-loaded`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/lcp-lazy-loaded.js) | بررسی lazy نبودن تصویر LCP | METRIC_SAVINGS | `L71: null; L105: wasLazyLoaded ? 0 : 1` |
| [`layout-shifts`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/layout-shifts.js) | عناصر و رخدادهای مؤثر در جابه‌جایی چیدمان | METRIC_SAVINGS | `L124: event.args.data.weighted_score_delta; L148: passed ? 1 : 0` |
| [`uses-passive-event-listeners`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/dobetterweb/uses-passive-event-listeners.js) | listenerهای passive برای رویدادهای پیمایش/لمس | METRIC_SAVINGS | `L62: Number(results.length === 0)` |
| [`no-document-write`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/dobetterweb/no-document-write.js) | استفاده از document.write() | METRIC_SAVINGS | `L79: Number(results.length === 0)` |
| [`long-tasks`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/long-tasks.js) | کارهای طولانی روی نخ اصلی | INFORMATIVE | `L248: results.length === 0 ? 1 : 0` |
| [`non-composited-animations`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/non-composited-animations.js) | انیمیشن‌هایی که فقط با compositing اجرا نمی‌شوند | INFORMATIVE | `L127: 1; L198: results.length === 0 ? 1 : 0` |
| [`unsized-images`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/unsized-images.js) | داشتن width/height صریح و امکان رزرو فضای تصاویر | METRIC_SAVINGS | `L156: unsizedImages.length > 0 ? 0 : 1` |
| [`viewport`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/viewport.js) | وجود meta viewport مناسب | METRIC_SAVINGS | `L54: 0; L76: Number(viewportMeta.isMobileOptimized)` |
| [`uses-responsive-images-snapshot`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/uses-responsive-images-snapshot.js) | تناسب ابعاد تصویر در حالت Snapshot؛ بررسی جدا از navigation | METRIC_SAVINGS | `L99: score` |
| [`work-during-interaction`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/work-during-interaction.js) | هزینهٔ کار هنگام تعامل در حالت Timespan | METRIC_SAVINGS | `L228: null; L240: null; L272: passed ? 1 : 0` |
| [`bf-cache`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/bf-cache.js) | موانع استفاده از کش بازگشت/جلو رفتن مرورگر؛ bfcache | BINARY | `L77: null; L84: 1; L128: results.length ? 0 : 1` |
| [`network-requests`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/network-requests.js) | دادهٔ درخواست‌های شبکه | INFORMATIVE | `L130: 1` |
| [`network-rtt`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/network-rtt.js) | برآورد زمان رفت‌وبرگشت شبکه؛ RTT | INFORMATIVE | `L47: 1; L79: 1` |
| [`network-server-latency`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/network-server-latency.js) | برآورد latency بخش سرور | INFORMATIVE | `L47: 1; L79: Math.max(1 - (maxLatency / 500), 0)` |
| [`main-thread-tasks`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/main-thread-tasks.js) | فهرست وظایف نخ اصلی | INFORMATIVE | `L52: 1` |
| [`diagnostics`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/diagnostics.js) | داده‌های تشخیصی داخلی | INFORMATIVE | `L69: 1` |
| [`metrics`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/metrics.js) | مجموعهٔ دادهٔ معیارها | INFORMATIVE | `L63: 1` |
| [`screenshot-thumbnails`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/screenshot-thumbnails.js) | فریم‌های کوچک بارگیری صفحه؛ filmstrip | INFORMATIVE | `L133: 1; L160: 1` |
| [`final-screenshot`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/final-screenshot.js) | تصویر نهایی صفحه | INFORMATIVE | `L40: 1; L47: 1` |
| [`script-treemap-data`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/script-treemap-data.js) | دادهٔ نمودار اندازه و اجزای باندل‌های اسکریپت | INFORMATIVE | `L281: 1` |
| [`resource-summary`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/resource-summary.js) | خلاصهٔ تعداد و حجم منابع به تفکیک نوع | INFORMATIVE | `L82: null` |
| [`first-meaningful-paint`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/metrics/first-meaningful-paint.js) | اولین نمایش معنادار؛ معیار قدیمی/داخلی بدون وزن Performance | NUMERIC | `L77: Audit.computeLogNormalScore(         options.scoring,         metricResult.timing       )` |
| [`cache-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/cache-insight.js) | سیاست کش مؤثر، عمر منابع و فرصت صرفه‌جویی در بازدیدهای بعدی | runtime adapter / default | `state pass → 1؛ fail → 0؛ حالت دیگر → null، طبق adapter` |
| [`cls-culprits-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/cls-culprits-insight.js) | عوامل ایجاد CLS؛ تغییر ابعاد، جابه‌جایی محتوا و تغییرات چیدمان | runtime adapter / default | `state pass → 1؛ fail → 0؛ حالت دیگر → null، طبق adapter` |
| [`document-latency-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/document-latency-insight.js) | تأخیر سند اصلی؛ ریدایرکت، پاسخ سرور و انتقال/فشرده‌سازی سند | runtime adapter / default | `state pass → 1؛ fail → 0؛ حالت دیگر → null، طبق adapter` |
| [`dom-size-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/dom-size-insight.js) | اندازه و پیچیدگی DOM و اثر آن بر محاسبهٔ استایل و layout | runtime adapter / default | `state pass → 1؛ fail → 0؛ حالت دیگر → null، طبق adapter` |
| [`duplicated-javascript-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/duplicated-javascript-insight.js) | ماژول‌ها و کد JavaScript تکراری | runtime adapter / default | `state pass → 1؛ fail → 0؛ حالت دیگر → null، طبق adapter` |
| [`font-display-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/font-display-insight.js) | نمایش فونت و اثر بارگیری آن بر نمایش متن و جابه‌جایی | runtime adapter / default | `state pass → 1؛ fail → 0؛ حالت دیگر → null، طبق adapter` |
| [`forced-reflow-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/forced-reflow-insight.js) | اجرای layout هم‌زمان اجباری بر اثر خواندن هندسه پس از تغییر DOM/style | runtime adapter / default | `state pass → 1؛ fail → 0؛ حالت دیگر → null، طبق adapter` |
| [`image-delivery-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/image-delivery-insight.js) | حجم و شیوهٔ ارسال تصاویر؛ ابعاد، فرمت و فشرده‌سازی | runtime adapter / default | `state pass → 1؛ fail → 0؛ حالت دیگر → null، طبق adapter` |
| [`inp-breakdown-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/inp-breakdown-insight.js) | تفکیک تأخیر تعامل به input delay، processing و presentation در دادهٔ تعامل موجود | runtime adapter / default | `state pass → 1؛ fail → 0؛ حالت دیگر → null، طبق adapter` |
| [`lcp-breakdown-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/lcp-breakdown-insight.js) | چهار سهم LCP: TTFB، تأخیر شروع دریافت، مدت دریافت و تأخیر نمایش | runtime adapter / default | `state pass → 1؛ fail → 0؛ حالت دیگر → null، طبق adapter` |
| [`lcp-discovery-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/lcp-discovery-insight.js) | کشف زودهنگام منبع LCP؛ HTML/preload، اولویت دریافت و lazy نبودن | runtime adapter / default | `state pass → 1؛ fail → 0؛ حالت دیگر → null، طبق adapter` |
| [`legacy-javascript-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/legacy-javascript-insight.js) | کد قدیمی، polyfill و transpilation غیرضروری | runtime adapter / default | `state pass → 1؛ fail → 0؛ حالت دیگر → null، طبق adapter` |
| [`modern-http-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/modern-http-insight.js) | پروتکل‌های HTTP قدیمی و فرصت استفاده از اتصال مدرن | runtime adapter / default | `state pass → 1؛ fail → 0؛ حالت دیگر → null، طبق adapter` |
| [`network-dependency-tree-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/network-dependency-tree-insight.js) | درخت وابستگی درخواست‌ها و زنجیره‌های بحرانی شبکه | runtime adapter / default | `state pass → 1؛ fail → 0؛ حالت دیگر → null، طبق adapter` |
| [`render-blocking-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/render-blocking-insight.js) | درخواست‌های مسدودکنندهٔ رندر | runtime adapter / default | `state pass → 1؛ fail → 0؛ حالت دیگر → null، طبق adapter` |
| [`third-parties-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/third-parties-insight.js) | سهم منابع شخص ثالث در انتقال و اجرای صفحه | runtime adapter / default | `state pass → 1؛ fail → 0؛ حالت دیگر → null، طبق adapter` |
| [`viewport-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/viewport-insight.js) | تنظیم viewport و اثر آن بر نمایش و تعامل | runtime adapter / default | `state pass → 1؛ fail → 0؛ حالت دیگر → null، طبق adapter` |

## ۵. قواعد مرتبط با GT که در مخزن رسمی تاریخی پیدا شدند

این سه پیاده‌سازی در مخزن عمومی خود GTmetrix وجود دارند. **نسخهٔ تاریخی‌اند؛
وزن یا شرایط موتور Structure امروز از آن‌ها اثبات نمی‌شود.**

### Enable Keep-Alive

- `Connection: keep-alive` بدون close، نشانهٔ فعال‌بودن محسوب می‌شود.
- HTTP/2 از این آزمون header کنار گذاشته می‌شود؛ Connection در HTTP/2 منسوخ است.
- HTTP/1.1 بدون `Connection: close` اتصال پایدار دارد؛ نیاز به افزودن کورکورانهٔ header نیست.
- پروتکل نامشخص و بعضی headerهای قابل‌تجزیه‌نبودن کنار گذاشته می‌شوند.
- در این پیاده‌سازی، گزارش host به بیش از یک resource و حداقل یک مورد ناقض وابسته است.

[منبع رسمی تاریخی](https://github.com/gtmetrix/pagespeed-library/blob/HEAD/pagespeed/rules/enable_keep_alive.cc).

### Avoid CSS @import

- CSS واقعی دریافت‌شده بررسی می‌شود.
- imported URL باید به resource موجود و CSS متفاوت از خود فایل برسد.
- وجود `@import` در فایل منبع build بدون import شبکه‌ای خروجی، به‌تنهایی نقض این منطق نیست.
- صرفه‌جویی بر مبنای حذف یک گام critical path گزارش می‌شود.

[منبع رسمی تاریخی](https://github.com/gtmetrix/pagespeed-library/blob/HEAD/pagespeed/rules/avoid_css_import.cc).

### Combine images into CSS sprites

- فقط تصویرهای PNG/GIF/WebP دریافت‌شده پیش از onload در این مسیر نامزد می‌شوند.
- سقف اندازهٔ فایل در همین کد 2KiB و سقف تعداد پیکسل 96×96 است.
- تصویر 1×1، فاقد اطلاعات لازم، بزرگ یا غیرقابل کش کنار گذاشته می‌شود.
- حداقل شمار نامزدها در این کد ۵ است؛ این توصیه مربوط به همهٔ عکس‌های محصول نیست.

[منبع رسمی تاریخی](https://github.com/gtmetrix/pagespeed-library/blob/HEAD/pagespeed/rules/sprite_images.cc).

### CDN

پیاده‌سازی YSlow اصلی `ycdn` نیز بررسی شد. در ruleset پیش‌فرض تاریخی، وزن خام ۶ و
کاهش امتیاز به ازای offender برابر config.points=10 است. تشخیص به headerها، الگوهای
hostname، exceptionها و CDN سفارشی وابسته است. این فهرست تشخیص قدیمی را نباید
فهرست معتبر امروز یا الگوریتم CDN خود GTmetrix اعلام کرد.

[منبع YSlow](https://github.com/marcelduran/yslow/blob/HEAD/src/common/rules.js).

## ۶. تمام قواعد ruleset پیش‌فرض تاریخی YSlow(V2)

وزن زیر **وزن YSlow اصلی است؛ نه وزن Structure GTmetrix**. وزن‌ها درصد نیستند؛ جمع
خام آن‌ها ۱۲۳ است. در جمع نهایی فقط نتیجه‌های عددی با وزن مربوط وارد محاسبه می‌شوند؛
مخرج برای همهٔ اجراها لزوماً ثابت نیست. کل تابع lint و config هر قاعده در فایل JSON همراه آمده است.

| شناسه | نام اصلی | وزن خام تاریخی | تنظیمات پیش‌فرض قاعده |
| --- | --- | ---: | --- |
| `ynumreq` | Make fewer HTTP requests | 8 | `{"max_js":3,"points_js":4,"max_css":2,"points_css":4,"max_cssimages":6,"points_cssimages":3}` |
| `ycdn` | Use a Content Delivery Network (CDN) | 6 | `{"points":10,"types":["js","css","image","cssimage","flash","favicon"]}` |
| `yemptysrc` | Avoid empty src or href | 30 | `{"points":100}` |
| `yexpires` | Add Expires headers | 10 | `{"points":11,"howfar":172800,"types":["css","js","image","cssimage","flash","favicon"]}` |
| `ycompress` | Compress components with gzip | 8 | `{"min_filesize":500,"types":["doc","iframe","xhr","js","css"],"points":11}` |
| `ycsstop` | Put CSS at top | 4 | `{"points":10}` |
| `yjsbottom` | Put JavaScript at bottom | 4 | `{"points":5}` |
| `yexpressions` | Avoid CSS expressions | 3 | `{"points":2}` |
| `yexternal` | Make JavaScript and CSS external | 4 | `{}` |
| `ydns` | Reduce DNS lookups | 3 | `{"max_domains":4,"points":5}` |
| `yminify` | Minify JavaScript and CSS | 4 | `{"points":10,"types":["js","css"]}` |
| `yredirects` | Avoid URL redirects | 4 | `{"points":10}` |
| `ydupes` | Remove duplicate JavaScript and CSS | 4 | `{"points":5,"types":["js","css"]}` |
| `yetags` | Configure entity tags (ETags) | 2 | `{"points":11,"types":["flash","js","css","cssimage","image","favicon"]}` |
| `yxhr` | Make AJAX cacheable | 4 | `{"points":5,"min_cache_time":3600}` |
| `yxhrmethod` | Use GET for AJAX requests | 3 | `{"points":5}` |
| `ymindom` | Reduce the number of DOM elements | 3 | `{"range":250,"points":10,"maxdom":900}` |
| `yno404` | Avoid HTTP 404 (Not Found) error | 4 | `{"points":5,"types":["css","js","image","cssimage","flash","xhr","favicon"]}` |
| `ymincookie` | Reduce cookie size | 3 | `{"points":10,"max_cookie_size":1000}` |
| `ycookiefree` | Use cookie-free domains | 3 | `{"points":5,"types":["js","css","image","cssimage","flash","favicon"]}` |
| `ynofilter` | Avoid AlphaImageLoader filter | 4 | `{"points":5,"halfpoints":2}` |
| `yimgnoscale` | Do not scale images in HTML | 3 | `{"points":5}` |
| `yfavicon` | Make favicon small and cacheable | 2 | `{"points":5,"size":2000,"min_cache_time":3600}` |

[منبع تجمیع امتیاز YSlow](https://github.com/marcelduran/yslow/blob/HEAD/src/common/controller.js).

## ۷. تمام شناسه‌های rulesetهای PageSpeed در مخزن رسمی تاریخی GTmetrix

فهرست از `rule_provider.cc` استخراج شد. `core` در مجموعهٔ mobile ارجاع به یک مجموعه
است، نه audit جدید. این قواعد را به تعداد چک‌های فعلی GT اضافه نکنید.

### kCoreRules

- `avoidbadrequests`
- `avoidcharsetinmetatag`
- `avoidcssimport`
- `avoidlandingpageredirects`
- `avoidlongrunningscripts`
- `deferparsingjavascript`
- `enablegzipcompression`
- `enablekeepalive`
- `inlinesmallcss`
- `inlinesmalljavascript`
- `leveragebrowsercaching`
- `minifycss`
- `minifyjavascript`
- `minimizeredirects`
- `minimizerequestsize`
- `optimizeimages`
- `putcssinthedocumenthead`
- `serveresourcesfromaconsistenturl`
- `serverresponsetime`
- `servescaledimages`
- `specifyacachevalidator`
- `specifycharsetearly`
- `specifyimagedimensions`
- `spriteimages`

### kOldBrowserRules

- `combineexternalcss`
- `combineexternaljavascript`
- `minimizednslookups`
- `parallelizedownloadsacrosshostnames`

### kNewBrowserRules

- `preferasyncresources`

### kMobileBrowserRules

- `core` — alias مجموعه؛ نه audit مستقل
- `avoidplugins`


[منبع رسمی ثبت مجموعه‌ها](https://github.com/gtmetrix/pagespeed-library/blob/HEAD/pagespeed/rules/rule_provider.cc).

## ۸. داده‌های شبکه و فیلدهای GT؛ جدا از امتیاز

نام تمام ۲۵ فیلد شناسایی‌شده در افزونهٔ رسمی GT، داده‌های HAR/Waterfall، معیارهای قدیمی،
حالت‌های Snapshot/Timespan و تفاوت نسخه‌ها در `GTmetrix-Expanded-Inventory-fa.md` ثبت شده‌اند.
این‌ها حذف نشده‌اند؛ در جدول امتیازدار مخلوط نشده‌اند.

نشت کش بین کاربران، قیمت صحیح، استوری و ۸+۸، ورود و پرداخت همچنان تست عملکردی مستقل
می‌خواهند. اضافه‌کردن این‌ها به تعداد «قواعد قطعی امتیازدهی GT» نادرست است.

## ۹. ردگیری منابع و فایل‌های همراه

- `audit-acceptance-and-scoring.csv`: تمام ردیف‌ها با حالت، defaults، ثابت‌ها، expression و شرط‌ها.
- `audit-scoring-evidence.json`: شواهد ساختاری هر ۷۶ فایل رسمی و شمارهٔ خط.
- `insight-condition-evidence.json`: شرط‌ها و stateهای ۲۳ فایل مدل Chrome از بستهٔ پین‌شدهٔ trace_engine@0.0.59.
- `yslow-historical-scoring.json`: هر ۲۳ قاعده، config، وزن و متن کامل تابع lint تاریخی.
- `pagespeed-historical-rulesets.json`: تمام گروه‌ها و شناسه‌های ثبت‌شده در مخزن رسمی GT.
- `extended/report-sources/manifest.json`: مخازن مثال/API/گزارش بررسی‌شده؛ مثال تولیدشده با AI، launcher و گزارش فاقد inventory برای اثبات قواعد پذیرفته نشدند.

منابع اصلی:

- [Lighthouse 12.0.0](https://github.com/GoogleChrome/lighthouse/tree/v12.0.0/core/audits)
- [Lighthouse 12.8.2](https://github.com/GoogleChrome/lighthouse/tree/v12.8.2/core/audits)
- [Lighthouse 13.6.0](https://github.com/GoogleChrome/lighthouse/blob/v13.6.0/core/config/default-config.js)
- [GTmetrix WordPress و API](https://github.com/gtmetrix/gtmetrix-for-wordpress)
- [GTmetrix pagespeed-library](https://github.com/gtmetrix/pagespeed-library)
- [GTmetrix GTspeed](https://github.com/gtmetrix/GTspeed)
- [YSlow اصلی](https://github.com/marcelduran/yslow)

## چه چیزی هنوز مانع ادعای کامل‌بودن GTmetrix امروز است؟

کاتالوگ و سیاست وزن‌دهی اختصاصی Structure نسخهٔ جاری از منبع اولیه در دسترس این نشست
قرار نگرفته است. از کد Lighthouse، YSlow قدیمی یا یک گزارش نمونه نمی‌توان آن را با قطعیت ساخت.
برای بسته‌شدن این بخش، خروجی کامل passed/failed/not-applicable و نسخهٔ دقیق GT، همراه
با مستندات همان موتور لازم است. ابزار مرور عمومی وب در این نشست وجود ندارد و دسترسی
مستقیم GT همچنان محدود است؛ جست‌وجوی عمومی مخازن در GitHub جای جست‌وجوی کل وب نیست.

این پژوهش اکنون فراتر از فهرست نام‌هاست، ولی ادعای «تمام موارد امتیازدار GTmetrix
امروزی، بدون استثنا، تأیید شد» همچنان نادرست خواهد بود. وزن ناشناخته عمداً ناشناخته باقی مانده است.


## پیوست جدید: ریزشروط و بحث‌های فنی

[جزئیات تکمیلی و دامنهٔ جست‌وجو](GTmetrix-Deep-Details-fa.md)؛ شامل تفاوت نسخه‌ها، فیلترها، استثناها، ۱۴ صفحهٔ بحث و اصلاح استخراج شرط‌ها در ۷۶ فایل.
