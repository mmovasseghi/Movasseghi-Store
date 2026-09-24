import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

const base = process.env.MS_BASE_URL || 'http://localhost:5274';
const outDir =
  process.env.MS_SCREENSHOT_DIR ||
  path.join(process.cwd(), 'assets', 'admin-mobile-test');

await mkdir(outDir, { recursive: true });

const browser = await chromium.launch();
const context = await browser.newContext({
  locale: 'fa-IR',
  viewport: { width: 390, height: 844 },
  isMobile: true,
  hasTouch: true,
});
const page = await context.newPage();

await page.goto(`${base}/Admin/Auth/Login`, { waitUntil: 'networkidle' });
await page.fill('#username', 'admin');
await page.fill('#password', 'Admin@123456');
await page.click('button[type="submit"]');
await page.waitForTimeout(1500);
const afterLogin = page.url();
if (afterLogin.includes('/Login')) {
  console.error('Login failed, still on', afterLogin);
  await page.screenshot({ path: path.join(outDir, 'login-failed.png'), fullPage: true });
  await browser.close();
  process.exit(1);
}

const routes = [
  { name: 'dashboard', path: '/Admin/Dashboard' },
  { name: 'products', path: '/Admin/Products' },
  { name: 'orders', path: '/Admin/Orders' },
  { name: 'command-center', path: '/Admin/Studio/CommandCenter' },
  { name: 'editorial-hub', path: '/Admin/Studio/EditorialHub' },
  { name: 'blog', path: '/Admin/BlogAdmin' },
  { name: 'inquiries', path: '/Admin/Inquiries' },
];

for (const r of routes) {
  await page.goto(`${base}${r.path}`, { waitUntil: 'networkidle' });
  await page.waitForFunction(() => typeof window.AdminKit !== 'undefined', { timeout: 15000 }).catch(() => {});
  await page.waitForFunction(
    () => !document.querySelector('.kit-table-scroll .kit-table') || document.querySelector('.kit-table--stack'),
    { timeout: 8000 }
  ).catch(() => {});
  await page.waitForTimeout(250);
  await page.screenshot({ path: path.join(outDir, `${r.name}-closed.png`), fullPage: false });

  const toggle = page.locator('[data-st-menu-toggle]');
  if (await toggle.isVisible()) {
    await toggle.click({ force: true });
    await page.waitForTimeout(350);
    await page.screenshot({ path: path.join(outDir, `${r.name}-menu-open.png`), fullPage: false });
    await page.locator('[data-st-overlay]').click({ force: true });
    await page.waitForTimeout(200);
  }
}

await browser.close();
console.log('Saved to', outDir);
