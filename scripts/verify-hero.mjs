import { chromium } from 'playwright';
import { mkdir } from 'fs/promises';
import path from 'path';

const baseUrl = process.env.BASE_URL || 'http://localhost:5274';
const outDir = path.resolve('scripts/hero-screenshots');

async function checkSlide(page, label, slideIndex) {
  const dots = page.locator('.ms-hero-dots button');
  if (slideIndex > 0) {
    await dots.nth(slideIndex).click();
    await page.waitForTimeout(900);
  }

  const m = await page.evaluate(() => {
    const actions = document.querySelector('.ms-hero-slide.is-active .ms-hero-actions');
    const trust = document.querySelector('.ms-hero-trust');
    const items = document.querySelectorAll('.ms-hero-trust-item');
    const track = document.querySelector('.ms-hero-trust-marquee');
    const sets = document.querySelectorAll('.ms-hero-trust-set');
    const ar = actions?.getBoundingClientRect();
    const tr = trust?.getBoundingClientRect();
    const trackStyle = track ? getComputedStyle(track) : null;
    const actionsStyle = actions ? getComputedStyle(actions) : null;
    const viewport = document.querySelector('.ms-hero-viewport');
    const desc = document.querySelector('.ms-hero-slide.is-active .ms-hero-desc');
    const visibleItems = [...items].filter(i => {
      const set = i.closest('.ms-hero-trust-set');
      return set && getComputedStyle(set).display !== 'none';
    });
    const itemTops = visibleItems.map(i => Math.round(i.getBoundingClientRect().top));
    const oneLine = itemTops.length <= 1 || new Set(itemTops).size === 1;

    return {
      viewportHeight: Math.round(viewport?.getBoundingClientRect().height ?? 0),
      descHeight: Math.round(desc?.getBoundingClientRect().height ?? 0),
      actionsWrap: actionsStyle?.flexWrap || null,
      actionsOneRow: actions ? actions.scrollHeight <= Math.ceil(parseFloat(actionsStyle?.lineHeight || '20') * 1.8) : false,
      trustCount: items.length,
      trustSetCount: sets.length,
      trustMarquee: trackStyle?.animationName?.includes('msHeroTrustMarquee') ?? false,
      trustDirection: trackStyle?.direction || null,
      oneLine,
      overlap: ar && tr ? ar.bottom > tr.top + 2 : false,
      gap: ar && tr ? Math.round(tr.top - ar.bottom) : null,
      title: document.querySelector('.ms-hero-slide.is-active .ms-hero-title')?.textContent?.trim() || ''
    };
  });

  await page.locator('.ms-hero-trust').screenshot({
    path: path.join(outDir, `hero-trust-${label}-slide${slideIndex + 1}.png`)
  });

  return { slide: slideIndex + 1, ...m };
}

async function capture(label, width, height) {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width, height } });
  await page.goto(baseUrl, { waitUntil: 'networkidle', timeout: 60000 });
  await page.waitForSelector('.ms-hero-stage', { timeout: 15000 });
  await page.waitForTimeout(1200);

  const slides = [];
  for (let i = 0; i < 3; i++) {
    slides.push(await checkSlide(page, label, i));
  }

  await page.locator('.ms-hero').screenshot({ path: path.join(outDir, `hero-${label}.png`) });
  await browser.close();
  return { label, width, height, slides };
}

await mkdir(outDir, { recursive: true });

const results = [];
for (const cfg of [
  { label: 'mobile', width: 390, height: 844 },
  { label: 'desktop', width: 1280, height: 800 }
]) {
  results.push(await capture(cfg.label, cfg.width, cfg.height));
}

console.log(JSON.stringify(results, null, 2));

const failed = results.flatMap(r => {
  const heights = r.slides.map(s => s.viewportHeight);
  const heightDelta = heights.length ? Math.max(...heights) - Math.min(...heights) : 0;
  const issues = r.slides.map(s => ({ ...s, label: r.label, heightDelta })).filter(s =>
    s.trustCount !== 24 ||
    s.trustSetCount !== 2 ||
    !s.trustMarquee ||
    s.trustDirection !== 'ltr' ||
    !s.oneLine ||
    s.overlap ||
    s.actionsWrap !== 'nowrap' ||
    s.heightDelta > 2
  );
  return issues;
});

if (failed.length) {
  console.error('FAILED:', JSON.stringify(failed, null, 2));
  process.exit(1);
}
