import { chromium } from 'playwright';
import { mkdir } from 'fs/promises';
import path from 'path';

const baseUrl = process.env.BASE_URL || 'http://localhost:5274';
const outDir = path.resolve('scripts/site-screenshots');

const routes = [
  { path: '/', name: 'home' },
  { path: '/Catalog', name: 'catalog' },
  { path: '/Blog', name: 'blog' },
  { path: '/Page/About', name: 'about' },
  { path: '/Page/Contact', name: 'contact' }
];

async function auditViewport(label, width, height) {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width, height } });
  const consoleErrors = [];
  const pageErrors = [];

  page.on('console', msg => {
    if (msg.type() === 'error') consoleErrors.push(msg.text());
  });
  page.on('pageerror', err => pageErrors.push(err.message));

  const routeResults = [];

  for (const route of routes) {
    const res = await page.goto(`${baseUrl}${route.path}`, { waitUntil: 'networkidle', timeout: 60000 });
    const status = res?.status() ?? 0;
    const title = await page.title();
    const hasMain = await page.locator('.ms-main, main, .ms-app').first().isVisible().catch(() => false);
    routeResults.push({ route: route.name, status, title: title.slice(0, 80), hasMain });
    if (route.name === 'home') {
      await page.waitForSelector('.ms-hero-stage', { timeout: 15000 });
      await page.waitForTimeout(1200);
      await page.locator('.ms-hero').screenshot({ path: path.join(outDir, `home-hero-${label}.png`) });
      await page.locator('.ms-promo-bar').screenshot({ path: path.join(outDir, `home-promo-${label}.png`) });
    }
  }

  // Home-specific checks
  await page.goto(baseUrl, { waitUntil: 'networkidle', timeout: 60000 });
  await page.waitForSelector('.ms-hero-stage', { timeout: 15000 });
  await page.waitForTimeout(1500);

  const home = await page.evaluate(async () => {
    const grid = document.querySelector('.ms-hero-slide.is-active .ms-hero-grid');
    const copy = document.querySelector('.ms-hero-slide.is-active .ms-hero-copy');
    const product = document.querySelector('.ms-hero-slide.is-active .ms-hero-product');
    const actions = document.querySelector('.ms-hero-slide.is-active .ms-hero-actions');
    const trust = document.querySelector('.ms-hero-trust');
    const trustItems = document.querySelectorAll('.ms-hero-trust-item');
    const promoSlogan = [...document.querySelectorAll('.ms-promo-slogan')].find(el => getComputedStyle(el).display !== 'none');
    const promoCta = [...document.querySelectorAll('.ms-promo-cta-label')].find(el => getComputedStyle(el).display !== 'none');
    const promoChips = [...document.querySelectorAll('.ms-promo-chip')].filter(c => getComputedStyle(c).display !== 'none');

    const trustTrack = document.querySelector('.ms-hero-trust-marquee');
    const trustSets = document.querySelectorAll('.ms-hero-trust-set');

    const cr = copy?.getBoundingClientRect();
    const pr = product?.getBoundingClientRect();
    const ar = actions?.getBoundingClientRect();
    const tr = trust?.getBoundingClientRect();
    const trackStyle = trustTrack ? getComputedStyle(trustTrack) : null;

    return {
      heroActive: !!document.querySelector('.ms-hero-slide.is-active'),
      imageOnLeft: cr && pr ? (pr.x + pr.width) <= cr.x + 4 : false,
      heroActionsOverlapTrust: ar && tr ? ar.bottom > tr.top + 2 : false,
      trustCount: trustItems.length,
      trustSetCount: trustSets.length,
      trustMarquee: trackStyle?.animationName?.includes('msHeroTrustMarquee') ?? false,
      trustDirection: trackStyle?.direction || null,
      trustOneLine: trustItems.length ? new Set([...trustItems].map(i => Math.round(i.getBoundingClientRect().top))).size === 1 : false,
      promoSlogan: promoSlogan?.innerText?.trim() || '',
      promoCta: promoCta?.innerText?.trim() || '',
      promoChipCount: promoChips.length,
      promoLineFits: (() => {
        const line = document.querySelector('.ms-promo-line');
        return line ? line.scrollWidth <= line.clientWidth + 2 : false;
      })(),
      hasFloats: !!document.querySelector('.ms-hero-floats'),
      brokenImages: [...document.images].filter(img => !img.complete || img.naturalWidth === 0).length
    };
  });

  await browser.close();

  return {
    label,
    width,
    height,
    routes: routeResults,
    home,
    consoleErrors: [...new Set(consoleErrors)],
    pageErrors: [...new Set(pageErrors)]
  };
}

await mkdir(outDir, { recursive: true });

const results = [];
for (const cfg of [
  { label: 'mobile', width: 390, height: 844 },
  { label: 'desktop', width: 1280, height: 800 }
]) {
  results.push(await auditViewport(cfg.label, cfg.width, cfg.height));
}

console.log(JSON.stringify(results, null, 2));

const failed = [];
for (const r of results) {
  for (const route of r.routes) {
    if (route.status !== 200) failed.push(`${r.label}:${route.route} status ${route.status}`);
    if (!route.hasMain) failed.push(`${r.label}:${route.route} missing main content`);
  }
  if (r.pageErrors.length) failed.push(`${r.label}: pageErrors ${r.pageErrors.join(' | ')}`);
  if (r.home.hasFloats) failed.push(`${r.label}: hero floats visible`);
  if (!r.home.heroActive) failed.push(`${r.label}: no active hero slide`);
  if (!r.home.imageOnLeft) failed.push(`${r.label}: hero image not on left`);
  if (r.home.heroActionsOverlapTrust) failed.push(`${r.label}: hero CTAs overlap trust strip`);
  if (r.home.trustCount !== 24) failed.push(`${r.label}: trust count ${r.home.trustCount}`);
  if (r.home.trustSetCount !== 2) failed.push(`${r.label}: trust sets ${r.home.trustSetCount}`);
  if (!r.home.trustMarquee) failed.push(`${r.label}: hero trust marquee missing`);
  if (r.home.trustDirection !== 'ltr') failed.push(`${r.label}: hero trust direction ${r.home.trustDirection}`);
  if (!r.home.trustOneLine) failed.push(`${r.label}: trust not one line`);
  if (!r.home.promoLineFits) failed.push(`${r.label}: promo overflow`);
  if (r.label === 'mobile') {
    if (!r.home.promoSlogan.includes('پخش ظروف یکبار مصرف گیاهی')) failed.push(`${r.label}: promo slogan wrong`);
    if (r.home.promoChipCount !== 1) failed.push(`${r.label}: promo chips ${r.home.promoChipCount}`);
    if (!r.home.promoCta.includes('تماس')) failed.push(`${r.label}: promo CTA missing`);
  }
  if (r.label === 'desktop') {
    if (r.home.promoChipCount < 3) failed.push(`${r.label}: promo chips ${r.home.promoChipCount}`);
  }
}

if (failed.length) {
  console.error('FAILED CHECKS:\n' + failed.map(f => `- ${f}`).join('\n'));
  process.exit(1);
}

console.log('ALL CHECKS PASSED');
