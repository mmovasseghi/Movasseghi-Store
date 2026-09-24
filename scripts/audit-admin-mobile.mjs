import { chromium } from 'playwright';

const base = process.env.MS_BASE_URL || 'http://localhost:5274';
const routes = [
  '/Admin/Dashboard',
  '/Admin/Products',
  '/Admin/Orders',
  '/Admin/Categories',
  '/Admin/BlogAdmin',
  '/Admin/NewsAdmin',
  '/Admin/FaqAdmin',
  '/Admin/Inquiries',
  '/Admin/Coupons',
  '/Admin/Reports',
  '/Admin/Reports/Traffic',
  '/Admin/Studio/CommandCenter',
  '/Admin/Studio/EditorialHub',
  '/Admin/Studio/RankRadar',
  '/Admin/StudioContent/Home',
  '/Admin/Users',
  '/Admin/ProductReviewsAdmin',
];

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 390, height: 844 } });
await page.goto(`${base}/Admin/Auth/Login`, { waitUntil: 'domcontentloaded' });
await page.fill('#username', 'admin');
await page.fill('#password', 'Admin@123456');
await page.click('button[type=submit]');
await page.waitForTimeout(1200);

const issues = [];
for (const path of routes) {
  await page.goto(`${base}${path}`, { waitUntil: 'networkidle', timeout: 60000 }).catch(() => {});
  await page.waitForTimeout(500);
  const r = await page.evaluate(() => {
    window.AdminKit?.refreshMobileTables?.();
    const w = window.innerWidth;
    const sw = document.documentElement.scrollWidth;
    const hasMobileCss = !!document.querySelector('link[href*="admin-mobile"]');
    const wideTables = [...document.querySelectorAll('table')].filter((t) => {
      const rect = t.getBoundingClientRect();
      return rect.width > w + 8;
    }).length;
    const overflowEls = [];
    document.querySelectorAll('.st-main *').forEach((el) => {
      const rect = el.getBoundingClientRect();
      if (rect.right > w + 4 && rect.width > 40) {
        overflowEls.push({ tag: el.tagName, c: String(el.className).slice(0, 60), w: Math.round(rect.width) });
      }
    });
    overflowEls.sort((a, b) => b.w - a.w);
    return { sw, w, hasMobileCss, wideTables, top: overflowEls.slice(0, 3) };
  });
  if (r.sw > r.w + 2 || r.wideTables > 0) {
    issues.push({ path, ...r });
  }
}
console.log(JSON.stringify(issues, null, 2));
await browser.close();
