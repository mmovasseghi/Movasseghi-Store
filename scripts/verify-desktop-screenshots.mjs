import { chromium } from 'playwright';
import { mkdir } from 'fs/promises';
import path from 'path';

const baseUrl = process.env.BASE_URL || 'http://localhost:5274';
const outDir = path.resolve('scripts/site-screenshots/desktop-premium');

const pages = [
  { path: '/', name: 'home-full', fullPage: true },
  { path: '/', name: 'home-header', selector: '.ms-top-card' },
  { path: '/', name: 'home-hero', selector: '.ms-hero-stage' },
  { path: '/', name: 'home-offers', selector: '.ms-offers' },
  { path: '/Catalog', name: 'catalog', fullPage: true },
  { path: '/Cart', name: 'cart', fullPage: true }
];

await mkdir(outDir, { recursive: true });

for (const vp of [
  { label: '1280', width: 1280, height: 800 },
  { label: '1920', width: 1920, height: 1080 }
]) {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: vp.width, height: vp.height } });

  for (const route of pages) {
    await page.goto(`${baseUrl}${route.path}`, { waitUntil: 'networkidle', timeout: 60000 });
    if (route.path === '/') {
      await page.waitForSelector('.ms-hero-stage', { timeout: 15000 });
      await page.waitForTimeout(1500);
    }
    const file = path.join(outDir, `${route.name}-${vp.label}.png`);
    if (route.selector) {
      await page.locator(route.selector).screenshot({ path: file });
    } else {
      await page.screenshot({ path: file, fullPage: !!route.fullPage });
    }
  }

  const audit = await page.goto(`${baseUrl}/`, { waitUntil: 'networkidle' }).then(() =>
    page.evaluate(() => {
      const tabbar = document.querySelector('.ms-tabbar');
      const nav = document.querySelector('.ms-top-nav');
      const offersScroll = document.querySelector('.ms-offers-scroll');
      return {
        desktopClass: document.body.classList.contains('ms-desktop'),
        tabbarHidden: !tabbar || getComputedStyle(tabbar).display === 'none',
        navVisible: !!nav && getComputedStyle(nav).display !== 'none',
        offersGrid: offersScroll ? getComputedStyle(offersScroll).display === 'grid' : false,
        offerCount: document.querySelectorAll('.ms-offer-card').length,
        uxDesktopLoaded: [...document.styleSheets].some(s => s.href?.includes('ux-desktop'))
      };
    })
  );

  console.log(JSON.stringify({ viewport: vp.label, audit }, null, 2));
  await browser.close();
}

console.log(`Screenshots saved to ${outDir}`);
