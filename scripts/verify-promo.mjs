import { chromium } from 'playwright';
import { mkdir } from 'fs/promises';
import path from 'path';

const baseUrl = process.env.BASE_URL || 'http://localhost:5274';
const outDir = path.resolve('scripts/hero-screenshots');

async function check(label, width, height) {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width, height } });
  await page.goto(baseUrl, { waitUntil: 'networkidle', timeout: 60000 });
  await page.waitForTimeout(1200);

  const m = await page.evaluate(() => {
    const bar = document.querySelector('.ms-promo-bar');
    const line = document.querySelector('.ms-promo-line');
    const slogan = [...document.querySelectorAll('.ms-promo-slogan')].find(el => getComputedStyle(el).display !== 'none');
    const ctaLabel = [...document.querySelectorAll('.ms-promo-cta-label')].find(el => getComputedStyle(el).display !== 'none');
    const lr = line?.getBoundingClientRect();
    const sr = slogan?.getBoundingClientRect();
    const visibleChips = [...document.querySelectorAll('.ms-promo-chip')].filter(c => getComputedStyle(c).display !== 'none');

    return {
      lineFits: line ? line.scrollWidth <= line.clientWidth + 2 : false,
      barFits: bar ? bar.scrollWidth <= bar.clientWidth + 2 : false,
      sloganText: slogan?.innerText?.trim() || '',
      sloganTruncated: slogan ? slogan.scrollWidth > slogan.clientWidth + 2 : false,
      ctaText: ctaLabel?.innerText?.trim() || '',
      chipCount: visibleChips.length,
      lineHeight: lr ? Math.round(lr.height) : 0,
      oneLine: line ? line.scrollHeight <= 36 : false
    };
  });

  await page.locator('.ms-promo-bar').screenshot({ path: path.join(outDir, `promo-${label}.png`) });
  await browser.close();
  return { label, width, height, ...m };
}

await mkdir(outDir, { recursive: true });

const results = [];
for (const cfg of [
  { label: 'mobile', width: 390, height: 844 },
  { label: 'desktop', width: 1280, height: 800 }
]) {
  results.push(await check(cfg.label, cfg.width, cfg.height));
}

console.log(JSON.stringify(results, null, 2));

const failed = results.filter(r => {
  if (!r.lineFits) return true;
  if (r.sloganTruncated) return true;
  if (!r.oneLine) return true;
  if (r.label === 'mobile' && !r.sloganText.includes('پخش ظروف یکبار مصرف گیاهی')) return true;
  if (r.label === 'mobile' && r.chipCount !== 1) return true;
  if (r.label === 'mobile' && !r.ctaText.includes('تماس')) return true;
  if (r.label === 'desktop' && r.chipCount < 3) return true;
  return false;
});

if (failed.length) {
  console.error('FAILED:', JSON.stringify(failed, null, 2));
  process.exit(1);
}
