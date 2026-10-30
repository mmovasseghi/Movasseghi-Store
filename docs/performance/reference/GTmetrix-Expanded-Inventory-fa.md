# موجودی گسترش‌یافتهٔ بررسی‌ها و امتیازدهی مرتبط با GTmetrix

تاریخ تحقیق: ۲۰۲۶-۱۰-۳۰. این مرحله فقط تحقیق است و تغییری در برنامه/production انجام نشد.

## اصلاح صریح پاسخ قبلی

فهرست قبلیِ ۵۹ مورد فقط دستهٔ Performance نسخهٔ Lighthouse 12.0.0 بود. آن را نباید
«تمام چک‌های GTmetrix» معرفی می‌کردم. وجود audit در Lighthouse نیز حضور یا وزن آن در
Structure GTmetrix را اثبات نمی‌کند. این سند آن خلط را اصلاح می‌کند.

**هنوز تأیید فهرست کامل اختصاصی GTmetrix و وزن عددی تک‌تک auditهای Structure ممکن نشده است.**
صفحات GTmetrix از شبکهٔ محیط مسدودند؛ از بررسی چند مخزن نمی‌توان نتیجه گرفت همهٔ
مستندات اینترنت یا کد موتور اختصاصی GTmetrix دیده شده است. هیچ وزن GT حدس زده نشده است.

## چه منابعی واقعاً بررسی شدند؟

۱. فایل پیکربندی رسمی Lighthouse در نسخه‌های ثابت 12.0.0، 12.8.2 و 13.6.0.
۲. ۵۹ پیاده‌سازی audit نسخهٔ 12.0.0 و ۱۷ پیاده‌سازی Insight اضافه‌شده در 12.8.2.
۳. افزونهٔ رسمی GTmetrix WordPress و کلاینت رسمی API 2.0 آن.
۴. کلاینت‌های عمومی npm: @jackbrown9375/gtmetrix و @pipedream/gtmetrix؛ فقط متن منبع
   خوانده شد، کد بسته‌ها اجرا نشد و integrity آرشیوها تأیید شد. این‌ها موتور امتیازدهی نیستند.
۵. جست‌وجوی مخازن عمومی GitHub دربارهٔ Structure، Keep-Alive و لینک‌های راهنمای اختصاصی؛
   چک‌لیست‌های monkeyscript، flowforfrank و matheuscostadesign خوانده شدند. منابع ثانویه
   و مخلوطی از توصیه‌های جدید/قدیمی‌اند، نه مشخصات رسمی نمره‌دهی GTmetrix.
۶. مشخصات HAR 1.2 برای تفکیک فیلدهای شبکه از auditهای امتیازدار.

## چه چیزی استخراج شد؟

| مجموعه | تعداد | قطعیت |
| --- | ---: | --- |
| مراجع Performance در Lighthouse 12.0.0 | ۵۹ | استخراج مستقیم از منبع رسمی |
| مراجع Performance در Lighthouse 12.8.2 | ۷۶ | استخراج مستقیم از منبع رسمی |
| مراجع Performance در Lighthouse 13.6.0 | ۵۰ | استخراج مستقیم از منبع رسمی |
| اتحاد شناسه‌های سه نسخه | ۷۶ | شامل معیار، تشخیص، insight و خروجی پنهان؛ نه ۷۶ شرط امتیازدار GT |
| معیارهای وزن‌دار مستقیم Performance | ۵ | وزن‌ها در هر سه نسخه مشخص‌اند |
| مورد اختصاصی/تاریخی GT برای تطبیق | ۴ | نامزد مبتنی بر منابع ثانویه؛ وضعیت و وزن جاری GT تأیید نشده |
| فیلدهای گزارش در افزونهٔ رسمی GT | ۲۵ | دو مورد PageSpeed/YSlow تاریخی؛ فیلد با audit یکی نیست |

## ۱. پنج معیار امتیازدار Performance

| معیار | وزن Lighthouse در هر سه نسخه |
| --- | ---: |
| First Contentful Paint / FCP | ۱۰٪ |
| Largest Contentful Paint / LCP | ۲۵٪ |
| Total Blocking Time / TBT | ۳۰٪ |
| Cumulative Layout Shift / CLS | ۲۵٪ |
| Speed Index | ۱۰٪ |

صفر بودن وزن audit تشخیصی در Lighthouse، به معنی صفر بودن اثر اصلاح آن بر سرعت یا
وزن صفر آن در Structure GTmetrix نیست. این دو نوع وزن با هم یکی نیستند.

## ۲. فهرست واحد ۷۶ شناسه با نسخه و منبع

اختصار نسخه‌ها: 12.0 = v12.0.0، 12.8 = v12.8.2، 13.6 = v13.6.0.
شناسه‌ای که در نسخهٔ جدید حذف شده ممکن است با یک Insight جایگزین شده باشد؛ نباید
هر دو را شرط مستقل و هم‌زمان یک گزارش شمرد. حالت‌های Snapshot/Timespan نیز ممکن است
در Navigation معمولی قابل اعمال نباشند. وزن Structure GT برای تمام این ردیف‌ها **نامشخص** است.

| شناسه و منبع | بررسی | نسخه‌ها | وزن مستقیم Performance |
| --- | --- | --- | ---: |
| [`bf-cache`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/bf-cache.js) | موانع استفاده از کش بازگشت/جلو رفتن مرورگر؛ bfcache | 12.0, 12.8, 13.6 | 0 |
| [`bootup-time`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/bootup-time.js) | زمان parse/compile/اجرای JavaScript | 12.0, 12.8, 13.6 | 0 |
| [`cache-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/cache-insight.js) | سیاست کش مؤثر، عمر منابع و فرصت صرفه‌جویی در بازدیدهای بعدی | 12.8, 13.6 | 0 |
| [`cls-culprits-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/cls-culprits-insight.js) | عوامل ایجاد CLS؛ تغییر ابعاد، جابه‌جایی محتوا و تغییرات چیدمان | 12.8, 13.6 | 0 |
| [`critical-request-chains`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/critical-request-chains.js) | زنجیره‌های وابستگی درخواست‌های بحرانی | 12.0, 12.8 | 0 |
| [`cumulative-layout-shift`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/metrics/cumulative-layout-shift.js) | جابه‌جایی غیرمنتظرهٔ چیدمان؛ CLS | 12.0, 12.8, 13.6 | 25 |
| [`diagnostics`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/diagnostics.js) | داده‌های تشخیصی داخلی | 12.0, 12.8, 13.6 | 0 |
| [`document-latency-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/document-latency-insight.js) | تأخیر سند اصلی؛ ریدایرکت، پاسخ سرور و انتقال/فشرده‌سازی سند | 12.8, 13.6 | 0 |
| [`dom-size`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/dobetterweb/dom-size.js) | تعداد، عمق و تراکم عناصر DOM | 12.0, 12.8 | 0 |
| [`dom-size-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/dom-size-insight.js) | اندازه و پیچیدگی DOM و اثر آن بر محاسبهٔ استایل و layout | 12.8, 13.6 | 0 |
| [`duplicated-javascript`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/duplicated-javascript.js) | ماژول‌های JavaScript تکراری در باندل‌ها | 12.0, 12.8 | 0 |
| [`duplicated-javascript-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/duplicated-javascript-insight.js) | ماژول‌ها و کد JavaScript تکراری | 12.8, 13.6 | 0 |
| [`efficient-animated-content`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/efficient-animated-content.js) | جایگزینی محتوای متحرک سنگین مانند GIF با ویدئوی مناسب | 12.0, 12.8 | 0 |
| [`final-screenshot`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/final-screenshot.js) | تصویر نهایی صفحه | 12.0, 12.8, 13.6 | 0 |
| [`first-contentful-paint`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/metrics/first-contentful-paint.js) | زمان اولین نمایش محتوایی؛ FCP | 12.0, 12.8, 13.6 | 10 |
| [`first-meaningful-paint`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/metrics/first-meaningful-paint.js) | اولین نمایش معنادار؛ معیار قدیمی/داخلی بدون وزن Performance | 12.0, 12.8 | 0 |
| [`font-display`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/font-display.js) | قابل‌مشاهده‌بودن متن هنگام دریافت فونت | 12.0, 12.8 | 0 |
| [`font-display-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/font-display-insight.js) | نمایش فونت و اثر بارگیری آن بر نمایش متن و جابه‌جایی | 12.8, 13.6 | 0 |
| [`forced-reflow-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/forced-reflow-insight.js) | اجرای layout هم‌زمان اجباری بر اثر خواندن هندسه پس از تغییر DOM/style | 12.8, 13.6 | 0 |
| [`image-delivery-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/image-delivery-insight.js) | حجم و شیوهٔ ارسال تصاویر؛ ابعاد، فرمت و فشرده‌سازی | 12.8, 13.6 | 0 |
| [`inp-breakdown-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/inp-breakdown-insight.js) | تفکیک تأخیر تعامل به input delay، processing و presentation در دادهٔ تعامل موجود | 12.8, 13.6 | 0 |
| [`interaction-to-next-paint`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/metrics/interaction-to-next-paint.js) | تأخیر پاسخ بصری به تعامل؛ INP، فقط Timespan در این نسخه و بدون وزن Performance | 12.0, 12.8, 13.6 | 0 |
| [`interactive`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/metrics/interactive.js) | زمان تعاملی‌شدن؛ TTI، معیار قدیمی بدون وزن Performance | 12.0, 12.8, 13.6 | 0 |
| [`largest-contentful-paint`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/metrics/largest-contentful-paint.js) | زمان نمایش بزرگ‌ترین عنصر محتوایی؛ LCP | 12.0, 12.8, 13.6 | 25 |
| [`largest-contentful-paint-element`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/largest-contentful-paint-element.js) | شناسایی عنصر واقعی LCP | 12.0, 12.8 | 0 |
| [`layout-shifts`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/layout-shifts.js) | عناصر و رخدادهای مؤثر در جابه‌جایی چیدمان | 12.0, 12.8, 13.6 | 0 |
| [`lcp-breakdown-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/lcp-breakdown-insight.js) | چهار سهم LCP: TTFB، تأخیر شروع دریافت، مدت دریافت و تأخیر نمایش | 12.8, 13.6 | 0 |
| [`lcp-discovery-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/lcp-discovery-insight.js) | کشف زودهنگام منبع LCP؛ HTML/preload، اولویت دریافت و lazy نبودن | 12.8, 13.6 | 0 |
| [`lcp-lazy-loaded`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/lcp-lazy-loaded.js) | بررسی lazy نبودن تصویر LCP | 12.0, 12.8 | 0 |
| [`legacy-javascript`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/legacy-javascript.js) | JavaScript قدیمی، polyfill و تبدیل‌های غیرضروری برای مرورگر جدید | 12.0, 12.8 | 0 |
| [`legacy-javascript-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/legacy-javascript-insight.js) | کد قدیمی، polyfill و transpilation غیرضروری | 12.8, 13.6 | 0 |
| [`long-tasks`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/long-tasks.js) | کارهای طولانی روی نخ اصلی | 12.0, 12.8, 13.6 | 0 |
| [`main-thread-tasks`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/main-thread-tasks.js) | فهرست وظایف نخ اصلی | 12.0, 12.8, 13.6 | 0 |
| [`mainthread-work-breakdown`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/mainthread-work-breakdown.js) | تفکیک کار نخ اصلی؛ اسکریپت، استایل، layout و دیگر فعالیت‌ها | 12.0, 12.8, 13.6 | 0 |
| [`max-potential-fid`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/metrics/max-potential-fid.js) | برآورد بیشترین تأخیر بالقوهٔ ورودی اول؛ بدون وزن Performance | 12.0, 12.8, 13.6 | 0 |
| [`metrics`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/metrics.js) | مجموعهٔ دادهٔ معیارها | 12.0, 12.8, 13.6 | 0 |
| [`modern-http-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/modern-http-insight.js) | پروتکل‌های HTTP قدیمی و فرصت استفاده از اتصال مدرن | 12.8, 13.6 | 0 |
| [`modern-image-formats`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/modern-image-formats.js) | فرمت‌های تصویری جدید و کم‌حجم | 12.0, 12.8 | 0 |
| [`network-dependency-tree-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/network-dependency-tree-insight.js) | درخت وابستگی درخواست‌ها و زنجیره‌های بحرانی شبکه | 12.8, 13.6 | 0 |
| [`network-requests`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/network-requests.js) | دادهٔ درخواست‌های شبکه | 12.0, 12.8, 13.6 | 0 |
| [`network-rtt`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/network-rtt.js) | برآورد زمان رفت‌وبرگشت شبکه؛ RTT | 12.0, 12.8, 13.6 | 0 |
| [`network-server-latency`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/network-server-latency.js) | برآورد latency بخش سرور | 12.0, 12.8, 13.6 | 0 |
| [`no-document-write`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/dobetterweb/no-document-write.js) | استفاده از document.write() | 12.0, 12.8 | 0 |
| [`non-composited-animations`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/non-composited-animations.js) | انیمیشن‌هایی که فقط با compositing اجرا نمی‌شوند | 12.0, 12.8, 13.6 | 0 |
| [`offscreen-images`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/offscreen-images.js) | بارگیری زودهنگام تصاویر خارج viewport | 12.0, 12.8 | 0 |
| [`prioritize-lcp-image`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/prioritize-lcp-image.js) | اولویت و preload تصویر LCP | 12.0, 12.8 | 0 |
| [`redirects`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/redirects.js) | ریدایرکت‌های متعدد صفحه | 12.0, 12.8, 13.6 | 0 |
| [`render-blocking-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/render-blocking-insight.js) | درخواست‌های مسدودکنندهٔ رندر | 12.8, 13.6 | 0 |
| [`render-blocking-resources`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/render-blocking-resources.js) | منابع مسدودکنندهٔ رندر اولیه | 12.0, 12.8 | 0 |
| [`resource-summary`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/resource-summary.js) | خلاصهٔ تعداد و حجم منابع به تفکیک نوع | 12.0, 12.8, 13.6 | 0 |
| [`screenshot-thumbnails`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/screenshot-thumbnails.js) | فریم‌های کوچک بارگیری صفحه؛ filmstrip | 12.0, 12.8, 13.6 | 0 |
| [`script-treemap-data`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/script-treemap-data.js) | دادهٔ نمودار اندازه و اجزای باندل‌های اسکریپت | 12.0, 12.8, 13.6 | 0 |
| [`server-response-time`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/server-response-time.js) | زمان پاسخ اولیهٔ سرور | 12.0, 12.8, 13.6 | 0 |
| [`speed-index`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/metrics/speed-index.js) | سرعت کامل‌شدن بصری صفحه؛ Speed Index | 12.0, 12.8, 13.6 | 10 |
| [`third-parties-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/third-parties-insight.js) | سهم منابع شخص ثالث در انتقال و اجرای صفحه | 12.8, 13.6 | 0 |
| [`third-party-facades`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/third-party-facades.js) | جایگزین سبک برای بارگیری تنبل embedهای شخص ثالث | 12.0, 12.8 | 0 |
| [`third-party-summary`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/third-party-summary.js) | هزینهٔ منابع و اجرای سرویس‌های شخص ثالث | 12.0, 12.8 | 0 |
| [`total-blocking-time`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/metrics/total-blocking-time.js) | زمان مسدودشدن نخ اصلی بر اثر بخشِ بیش از ۵۰ms کارهای طولانی؛ TBT | 12.0, 12.8, 13.6 | 30 |
| [`total-byte-weight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/total-byte-weight.js) | حجم کل منابع شبکه | 12.0, 12.8, 13.6 | 0 |
| [`unminified-css`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/unminified-css.js) | CSS کوچک‌سازی‌نشده | 12.0, 12.8, 13.6 | 0 |
| [`unminified-javascript`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/unminified-javascript.js) | JavaScript کوچک‌سازی‌نشده | 12.0, 12.8, 13.6 | 0 |
| [`unsized-images`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/unsized-images.js) | داشتن width/height صریح و امکان رزرو فضای تصاویر | 12.0, 12.8, 13.6 | 0 |
| [`unused-css-rules`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/unused-css-rules.js) | CSS استفاده‌نشده | 12.0, 12.8, 13.6 | 0 |
| [`unused-javascript`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/unused-javascript.js) | JavaScript استفاده‌نشده | 12.0, 12.8, 13.6 | 0 |
| [`user-timings`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/user-timings.js) | علامت‌ها و اندازه‌گیری‌های Performance API؛ User Timing | 12.0, 12.8, 13.6 | 0 |
| [`uses-http2`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/dobetterweb/uses-http2.js) | استفاده از HTTP/2 برای منابع | 12.0, 12.8 | 0 |
| [`uses-long-cache-ttl`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/uses-long-cache-ttl.js) | عمر کش منابع استاتیک | 12.0, 12.8 | 0 |
| [`uses-optimized-images`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/uses-optimized-images.js) | فشرده‌سازی و رمزگذاری ناکارآمد تصاویر | 12.0, 12.8 | 0 |
| [`uses-passive-event-listeners`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/dobetterweb/uses-passive-event-listeners.js) | listenerهای passive برای رویدادهای پیمایش/لمس | 12.0, 12.8 | 0 |
| [`uses-rel-preconnect`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/uses-rel-preconnect.js) | اتصال زودهنگام به originهای لازم؛ preconnect | 12.0, 12.8 | 0 |
| [`uses-responsive-images`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/uses-responsive-images.js) | تناسب ابعاد فایل تصویر با اندازهٔ نمایش | 12.0, 12.8 | 0 |
| [`uses-responsive-images-snapshot`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/uses-responsive-images-snapshot.js) | تناسب ابعاد تصویر در حالت Snapshot؛ بررسی جدا از navigation | 12.0, 12.8 | 0 |
| [`uses-text-compression`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/byte-efficiency/uses-text-compression.js) | فشرده‌سازی منابع متنی، مانند gzip/Brotli | 12.0, 12.8 | 0 |
| [`viewport`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/viewport.js) | وجود meta viewport مناسب | 12.0, 12.8 | 0 |
| [`viewport-insight`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.8.2/core/audits/insights/viewport-insight.js) | تنظیم viewport و اثر آن بر نمایش و تعامل | 12.8, 13.6 | 0 |
| [`work-during-interaction`](https://raw.githubusercontent.com/GoogleChrome/lighthouse/v12.0.0/core/audits/work-during-interaction.js) | هزینهٔ کار هنگام تعامل در حالت Timespan | 12.0, 12.8 | 0 |

### ۱۷ Insight جاافتاده از فهرست قبلی

- `cache-insight`: سیاست کش مؤثر، عمر منابع و فرصت صرفه‌جویی در بازدیدهای بعدی.
- `cls-culprits-insight`: عوامل ایجاد CLS؛ تغییر ابعاد، جابه‌جایی محتوا و تغییرات چیدمان.
- `document-latency-insight`: تأخیر سند اصلی؛ ریدایرکت، پاسخ سرور و انتقال/فشرده‌سازی سند.
- `dom-size-insight`: اندازه و پیچیدگی DOM و اثر آن بر محاسبهٔ استایل و layout.
- `duplicated-javascript-insight`: ماژول‌ها و کد JavaScript تکراری.
- `font-display-insight`: نمایش فونت و اثر بارگیری آن بر نمایش متن و جابه‌جایی.
- `forced-reflow-insight`: اجرای layout هم‌زمان اجباری بر اثر خواندن هندسه پس از تغییر DOM/style.
- `image-delivery-insight`: حجم و شیوهٔ ارسال تصاویر؛ ابعاد، فرمت و فشرده‌سازی.
- `inp-breakdown-insight`: تفکیک تأخیر تعامل به input delay، processing و presentation در دادهٔ تعامل موجود.
- `lcp-breakdown-insight`: چهار سهم LCP: TTFB، تأخیر شروع دریافت، مدت دریافت و تأخیر نمایش.
- `lcp-discovery-insight`: کشف زودهنگام منبع LCP؛ HTML/preload، اولویت دریافت و lazy نبودن.
- `legacy-javascript-insight`: کد قدیمی، polyfill و transpilation غیرضروری.
- `modern-http-insight`: پروتکل‌های HTTP قدیمی و فرصت استفاده از اتصال مدرن.
- `network-dependency-tree-insight`: درخت وابستگی درخواست‌ها و زنجیره‌های بحرانی شبکه.
- `render-blocking-insight`: درخواست‌های مسدودکنندهٔ رندر.
- `third-parties-insight`: سهم منابع شخص ثالث در انتقال و اجرای صفحه.
- `viewport-insight`: تنظیم viewport و اثر آن بر نمایش و تعامل.

## ۳. بررسی‌های اختصاصی GTmetrix که نباید با Lighthouse خلط شوند

چهار مورد زیر در راهنماها و چک‌لیست‌های مرتبط با GTmetrix مطرح‌اند. **آن‌ها را فعلاً
نامزد تطبیق با مستندات اصلی/خروجی واقعی GT می‌دانم، نه چهار audit تأییدشده برای نسخهٔ جاری.**
شناسهٔ داخلی، وزن، شرط اجرا و معتبر بودن در گزارش جاری از منبع اصلی تأیید نشده است.

| نام | چه چیزی باید بررسی شود؟ | خطای رایج در برداشت |
| --- | --- | --- |
| Use a Content Delivery Network (CDN) | تشخیص تحویل منابع استاتیک از CDN | وجود CDN تضمین A یا سرعت بهتر برای هر منطقه نیست |
| Enable Keep-Alive | استفادهٔ مجدد از اتصال‌ها در موارد قابل اعمال | قواعد HTTP/1.1 را نباید کورکورانه به HTTP/2 یا HTTP/3 تعمیم داد |
| Avoid CSS @import | وابستگی سریالی دریافت stylesheetها | هر import در منبع build به معنی import شبکه‌ای در خروجی نیست |
| Combine images using CSS sprites | کاهش درخواست‌های تصاویر کوچک مناسب | ادغام همهٔ تصاویر محصول یا حذف lazy loading راه‌حل درست نیست |

توصیه‌هایی مثل «همهٔ CSS/JS را ادغام کن»، «هرگز inline ننویس»، «cookie-free domain بساز»
یا «از CSS expression پرهیز کن» در چک‌لیست‌های قدیمی دیده شدند. بدون تطبیق نسخه نباید
به‌عنوان شرط امتیازدهی فعلی GT یا دستور تغییر سایت ارائه شوند. برای مثال ممنوعیت مطلق
inline با CSS بحرانی صحیح تعارض دارد. صرف وجود توصیه در اینترنت، اثبات وزن داشتنش نیست.

## ۴. تمام فیلدهای گزارش شناسایی‌شده در افزونهٔ رسمی GTmetrix

این‌ها نام فیلدهای اعلان/گزارش آن افزونه‌اند، نه جدول وزن نمره. وجود یک فیلد در کلاینت
قدیمی/جدید مشترک، نمایش آن در هر گزارش یا پلن را تضمین نمی‌کند.

| کلید | معنی/عنوان |
| --- | --- |
| `pagespeed_score` | PageSpeed score |
| `yslow_score` | YSlow score |
| `fully_loaded_time` | Fully Loaded |
| `time_to_first_byte` | Time to First Byte |
| `redirect_duration` | Redirect Duration |
| `connect_duration` | Connect Duration |
| `backend_duration` | Backend Duration |
| `onload_time` | Onload |
| `onload_duration` | Onload Duration |
| `page_bytes` | Total Size |
| `page_requests` | Total Requests |
| `html_bytes` | HTML Size |
| `first_paint_time` | First Paint |
| `dom_interactive_time` | DOM Interactive |
| `dom_content_loaded_time` | DOM Content Loaded |
| `dom_content_loaded_duration` | DOM Content Loaded Duration |
| `gtmetrix_grade` | GTmetrix Grade |
| `performance_score` | Performance Score |
| `structure_score` | Structure Score |
| `largest_contentful_paint` | Largest Contentful Paint |
| `first_contentful_paint` | First Contentful Paint |
| `total_blocking_time` | Total Blocking Time |
| `cumulative_layout_shift` | Cumulative Layout Shift |
| `time_to_interactive` | Time to Interactive |
| `speed_index` | Speed Index |

## ۵. داده‌های Waterfall و HAR؛ بدون ادعای امتیاز مستقل

مشخصات HAR 1.2 و کلاینت GT وجود خروجی HAR را مستند می‌کنند. جدول زیر محورهای دادهٔ
قابل بررسی است؛ نام هر header، cookie یا query parameter وابسته به همان سایت است و
فهرست ثابت جهانی ندارد. نمایش تمام فیلدهای HAR در UI هر پلن GT تضمین نشده است.

| بخش | فیلدها/موارد |
| --- | --- |
| هویت تست | browser/creator و نسخه، زمان شروع، page id/title، ارجاع هر درخواست به page |
| درخواست | URL، method، HTTP version، headers، cookies، queryString، postData، حجم header/body |
| پاسخ | status/statusText، HTTP version، headers، cookies، redirectURL، حجم header/body |
| محتوا | MIME type، اندازهٔ محتوا، compression، encoding و متن در صورت موجود بودن |
| زمان‌ها | blocked، DNS، connect، send، wait، receive، SSL |
| اتصال | serverIPAddress و connection identifier در صورت موجود بودن |
| کش | وضعیت beforeRequest/afterRequest، expires، lastAccess، eTag و hitCount در صورت موجود بودن |
| رویدادهای صفحه | onContentLoad و onLoad؛ زمان‌های اضافی GT در جدول فیلدهای افزونه آمده‌اند |
| نمودارهای Lighthouse | network requests، RTT، server latency، main-thread tasks، filmstrip، final screenshot، treemap و resource summary |

`timings.ssl` در HAR زیرمجموعهٔ connect است؛ نباید دوباره برای محاسبهٔ کل زمان جمع شود.
مقدار منفی بعضی فیلدهای timing می‌تواند یعنی اندازه‌گیری موجود نیست، نه سرعت منفی.
خطاهای 4xx/5xx، درخواست‌های ریدایرکت‌شده و cache headers از دادهٔ شبکه قابل تشخیص‌اند؛
وجود هر مورد لزوماً یک audit وزن‌دار مستقل نیست.

## ۶. شرایط اجرای تست که باید در کنار هر نتیجه ثبت شوند

URL نهایی، تاریخ تست، محل جغرافیایی، browser/Lighthouse، viewport و DPR، دستگاه و CPU،
نوع و محدودیت اتصال، cache سرد/گرم، ورود و cookies، ad blocking و سایر تنظیمات قابل دسترس.
همهٔ این‌ها الزاماً گزینهٔ قابل انتخاب در همهٔ پلن‌های GT نیستند. مقایسهٔ تست‌ها بدون ثبت
شرایط یکسان، اثر تغییر کد را از اثر شبکه و محیط جدا نمی‌کند.

## ۷. چیزهایی که نباید برای زیاد کردن تعداد «چک‌های GT» وارد فهرست شوند

- تست صحت قیمت، سبد، ورود، استوری، پرداخت یا مجوز مشاهدهٔ سفارش؛ این‌ها تست عملکردی مستقل‌اند.
- SEO/AEO/GEO، رتبهٔ گوگل و AI citations؛ از Grade عملکرد استنتاج نمی‌شوند.
- همهٔ auditهای SEO/Accessibility/Best Practices Lighthouse؛ این‌ها دسته‌های جداگانه‌اند
  و بدون مدرک نباید وزنشان را وارد Grade GT کرد.
- PageSpeed/YSlow تاریخی؛ صرف پشتیبانی کلاینت از گزارش قدیمی، آن‌ها را معیار جاری نمی‌کند.
- هر فیلد نمودار، waterfall یا HAR؛ «داده‌ای که ابزار ثبت می‌کند» با «شرطی که وزن امتیاز دارد» متفاوت است.

## ۸. بخش باقی‌مانده برای ادعای «تمام موارد امتیازدهی GTmetrix»

برای تکمیل واقعی هنوز این شواهد لازم است:

۱. نسخهٔ دقیق GT/Lighthouse و تنظیمات گزارش مورد نظر.
۲. خروجی کامل auditهای Structure، شامل passed، failed، not-applicable و خطاها؛ صرف Top Issues کافی نیست.
۳. شناسه، score، scoreDisplayMode و جزئیات هر audit و، در صورت انتشار، وزن/نحوهٔ تجمیع GT.
۴. تطبیق کاتالوگ اختصاصی GT با مستندات قابل دسترس همان نسخه.

در منابع قابل دسترس این پژوهش، جدول رسمی وزن عددی همهٔ موارد Structure پیدا نشد.
این به معنی اثبات «هیچ‌جا منتشر نشده» نیست. دسترسی مستقیم به صفحات GT در محیط مسدود
است و ابزار جست‌وجوی عمومی وب در این نشست وجود ندارد؛ جست‌وجوی مخازن عمومی جای کل وب را نمی‌گیرد.
تا رفع این کمبود، استفاده از عنوان «همهٔ چک‌های قطعی GTmetrix استخراج شد» نادرست است.

## منابع و داده‌های قابل بازتولید

- [Google Lighthouse v12.0.0 config](https://github.com/GoogleChrome/lighthouse/blob/v12.0.0/core/config/default-config.js)
- [Google Lighthouse v12.8.2 config](https://github.com/GoogleChrome/lighthouse/blob/v12.8.2/core/config/default-config.js)
- [Google Lighthouse v13.6.0 config](https://github.com/GoogleChrome/lighthouse/blob/v13.6.0/core/config/default-config.js)
- [افزونهٔ رسمی GTmetrix](https://github.com/gtmetrix/gtmetrix-for-wordpress)
- [کلاینت رسمی API 2.0](https://github.com/gtmetrix/gtmetrix-for-wordpress/blob/HEAD/lib/Services_WTF_Test_2.php)
- [HAR 1.2 specification](https://github.com/ahmadnassri/har-spec/blob/master/versions/1.2.md)
- [چک‌لیست ثانویه monkeyscript](https://github.com/monkeyscript/website-perf-checklist)
- [چک‌لیست ثانویه flowforfrank](https://github.com/flowforfrank/performance-checklist)
- [چک‌لیست ثانویه matheuscostadesign](https://github.com/matheuscostadesign/guia-seo)
- [کلاینت Pipedream](https://www.npmjs.com/package/@pipedream/gtmetrix)
- [کلاینت jackbrown9375](https://www.npmjs.com/package/@jackbrown9375/gtmetrix)

فایل‌های همراه: `expanded-audit-catalog.csv`، `expanded-audit-catalog.json` و
`extended/source-checksums.json`. CSV برای تک‌تک ردیف‌ها حضور و وزن Structure را
صریحاً UNKNOWN ثبت می‌کند؛ ستون وزن Lighthouse نباید جای آن استفاده شود.
