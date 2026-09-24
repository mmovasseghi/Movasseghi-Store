import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

const base = process.env.MS_BASE_URL || 'http://localhost:5274';
const outDir =
  process.env.MS_SCREENSHOT_DIR ||
  path.join(process.cwd(), 'assets');

await mkdir(outDir, { recursive: true });

const browser = await chromium.launch();
const context = await browser.newContext({ locale: 'fa-IR' });
const page = await context.newPage();

async function passHumanGate() {
  await page.goto(`${base}/Cart`, { waitUntil: 'networkidle' });
  const verified = await page.locator('#humanGate[data-human-verified="1"]').count();
  if (verified) return;
  const leaf = page.locator('[data-human-option="leaf"]');
  if (await leaf.count()) {
    await leaf.first().click();
    await page.waitForSelector('#humanGate[data-human-verified="1"]', { timeout: 15000 });
  }
}

await passHumanGate();

const count = await page.evaluate(async () => {
  const r = await fetch('/Cart/Count');
  const j = await r.json();
  return j.count ?? 0;
});

if (count < 1) {
  await page.goto(`${base}/`, { waitUntil: 'domcontentloaded' });
  const productLink = page.locator('a[href*="/Shop/"]').first();
  if (await productLink.count()) {
    await productLink.click();
    await page.waitForLoadState('networkidle');
    const form = page.locator('[data-pd-add-form]').first();
    const qty = form.locator('input[name="cartons"]');
    if (await qty.count()) await qty.fill('5000');
    const addSubmit = form.locator('button[type="submit"]');
    if (await addSubmit.count()) await addSubmit.click();
    await page.waitForTimeout(1200);
  }
  await passHumanGate();
}

await page.goto(`${base}/Cart`, { waitUntil: 'domcontentloaded' });
await passHumanGate();
const checkoutLink = page.locator('a.ms-cart-checkout:not(.is-disabled)');
if (await checkoutLink.count()) {
  await checkoutLink.first().click();
  await page.waitForLoadState('networkidle');
} else {
  await page.goto(`${base}/Checkout`, { waitUntil: 'domcontentloaded' });
}
const url = page.url();
if (!url.includes('/Checkout')) {
  await page.screenshot({
    path: path.join(outDir, 'checkout-delivery-debug.png'),
    fullPage: true,
  });
  throw new Error(`Checkout not reached (at ${url})`);
}

const delivery = page.locator('#checkoutDelivery');
if (!(await delivery.count())) {
  await page.screenshot({
    path: path.join(outDir, 'checkout-delivery-debug.png'),
    fullPage: true,
  });
  const title = await page.title();
  throw new Error(`#checkoutDelivery missing on ${url} (title: ${title})`);
}
await delivery.waitFor({ state: 'visible', timeout: 20000 });

const premium = await delivery.getAttribute('data-premium-delivery');
const shotPath = path.join(
  outDir,
  premium === '1' ? 'checkout-delivery-premium.png' : 'checkout-delivery-below-premium.png'
);

await delivery.screenshot({ path: shotPath });
console.log('Saved', shotPath, 'premium=', premium);

await browser.close();
