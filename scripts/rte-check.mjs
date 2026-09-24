import { chromium } from 'playwright-core';

const base = 'http://localhost:5274';
const chrome = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';

const browser = await chromium.launch({ executablePath: chrome, headless: true });
const page = await browser.newPage();
const errors = [];
page.on('pageerror', (e) => errors.push('pageerror: ' + e.message));
page.on('console', (msg) => {
  if (msg.type() === 'error') errors.push('console: ' + msg.text());
});

await page.goto(`${base}/Admin/Auth/Login`);
await page.fill('input[name="username"]', 'admin');
await page.fill('input[name="password"]', 'Admin@123456');
await page.click('button[type="submit"]');
await page.waitForLoadState('networkidle', { timeout: 20000 }).catch(() => {});
if (page.url().includes('/Login')) {
  console.log('login url:', page.url());
  const err = await page.locator('.validation-summary-errors, [data-kit-flash="error"]').first().textContent().catch(() => '');
  console.log('login err:', err);
  await browser.close();
  process.exit(2);
}

await page.goto(`${base}/Admin/Products/Edit/23`);
await page.evaluate(() => {
  try {
    localStorage.setItem('st-pe-tab', 'specs');
  } catch {}
});
await page.reload();
await page.waitForTimeout(3000);

const state = await page.evaluate(() => ({
  hasEditor: !!window.__rteEditors?.['product-desc'],
  hasProseMirror: !!document.querySelector('.ProseMirror'),
  hasFallback: !!document.querySelector('.st-rte-fallback-warn'),
  fallbackText: document.querySelector('.st-rte-fallback-warn')?.textContent || null,
  bundleScript: [...document.scripts].some((s) => s.src.includes('rich-text-editor.bundle')),
  oldModule: [...document.scripts].some((s) => s.src.includes('rich-text-editor.js')),
}));

console.log(JSON.stringify(state, null, 2));
if (errors.length) console.log('errors:\n' + errors.join('\n'));

await browser.close();
process.exit(state.hasEditor && !state.hasFallback ? 0 : 1);
