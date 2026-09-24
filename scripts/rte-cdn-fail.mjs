import { chromium } from 'playwright-core';

const base = 'http://localhost:5274';
const chrome = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';
const browser = await chromium.launch({ executablePath: chrome, headless: true });
const page = await browser.newPage();
const errors = [];
page.on('pageerror', (e) => errors.push(e.message));

await page.goto(`${base}/Admin/Auth/Login`);
await page.fill('input[name="username"]', 'admin');
await page.fill('input[name="password"]', 'Admin@123456');
await page.click('button[type="submit"]');
await page.waitForLoadState('networkidle').catch(() => {});

await page.goto(`${base}/Admin/Products/Edit/23`);
await page.evaluate(() => {
  const s = document.createElement('script');
  s.type = 'module';
  s.src = '/js/rich-text-editor.js';
  document.body.appendChild(s);
});
await page.waitForTimeout(5000);

const state = await page.evaluate(() => ({
  hasEditor: !!window.__rteEditors?.['product-desc'],
  hasFallback: !!document.querySelector('.st-rte-fallback-warn'),
}));
console.log(state, errors.slice(0, 5));
await browser.close();
