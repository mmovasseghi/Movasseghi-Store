import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

const outDir = path.join(process.cwd(), 'assets', 'admin-mobile-test');
await mkdir(outDir, { recursive: true });

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 390, height: 844 }, isMobile: true });
await page.goto('http://localhost:5274/Admin/Auth/Login');
await page.fill('#username', 'admin');
await page.fill('#password', 'Admin@123456');
await page.click('button[type=submit]');
await page.waitForTimeout(1200);

await page.goto('http://localhost:5274/Admin/Dashboard', { waitUntil: 'networkidle' });
await page.waitForTimeout(400);
await page.screenshot({ path: path.join(outDir, 'dashboard-closed-v2.png') });

await page.click('[data-st-menu-toggle]');
await page.waitForTimeout(450);
const openMetrics = await page.evaluate(() => {
  const sb = document.querySelector('.st-sidebar');
  const r = sb?.getBoundingClientRect();
  const main = document.querySelector('.st-main-wrap');
  const mainStyle = main ? getComputedStyle(main) : null;
  return {
    sidebarW: Math.round(r?.width || 0),
    viewportW: window.innerWidth,
    mainHidden: mainStyle?.visibility === 'hidden' || mainStyle?.width === '0px',
    overlayShown: getComputedStyle(document.querySelector('.st-overlay')).display !== 'none'
      && document.querySelector('.st-overlay.is-visible'),
  };
});
await page.screenshot({ path: path.join(outDir, 'dashboard-menu-open-v2.png') });
console.log('openMetrics', openMetrics);

await page.click('[data-st-nav-close]');
await page.waitForTimeout(300);
const closed = await page.evaluate(() => !document.querySelector('.st-sidebar.is-open'));
console.log('closedAfterX', closed);

await browser.close();
