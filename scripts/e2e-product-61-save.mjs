import { chromium } from 'playwright-core';

const base = 'http://localhost:5274';
const chrome = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';
const marker = `E2E_${Date.now()}`;
const slug =
  'بشقاب-بزرگ-یکبار-مصرف-گیاهی-بسته-6-عددی';

const browser = await chromium.launch({ executablePath: chrome, headless: true });
const page = await browser.newPage();
const pageErrors = [];
page.on('pageerror', (e) => pageErrors.push(e.message));

await page.goto(`${base}/Admin/Auth/Login`);
await page.fill('input[name="username"]', 'admin');
await page.fill('input[name="password"]', 'Admin@123456');
await page.click('button[type="submit"]');
await page.waitForLoadState('networkidle').catch(() => {});

await page.goto(`${base}/Admin/Products/Edit/61`);
await page.waitForTimeout(2000);
const rteState = await page.evaluate(() => ({
  url: location.href,
  hasRte: !!document.querySelector('[data-st-rte]'),
  rteId: document.querySelector('[data-st-rte]')?.dataset?.stRte,
  ready: document.querySelector('[data-st-rte]')?.dataset?.stRteReady,
  fallback: !!document.querySelector('.st-rte-fallback-warn'),
  boot: typeof window.stBootRichTextEditors,
  editors: Object.keys(window.__rteEditors || {}),
  bundle: [...document.scripts].some((s) => s.src.includes('rich-text-editor.bundle')),
}));
console.log('rteState', rteState);
if (!rteState.editors.length) {
  const dbg = await page.evaluate(async () => {
    const root = document.querySelector('[data-st-rte]');
    const source = document.getElementById('product-desc');
    const mount = document.getElementById('product-desc-mount');
    try {
      window.stBootRichTextEditors?.();
      await new Promise((r) => setTimeout(r, 500));
      return {
        source: !!source,
        mount: !!mount,
        editors: Object.keys(window.__rteEditors || {}),
        ready: root?.dataset?.stRteReady,
      };
    } catch (e) {
      return { err: String(e) };
    }
  });
  console.log('dbg', dbg, 'pageErrors', pageErrors);
}
await page.waitForSelector('.ProseMirror', { timeout: 20000 });

const shortVal = `تست-${marker}`;
await page.fill('textarea[name="ShortDescription"]', shortVal);

await page.locator('.ProseMirror').click();
await page.keyboard.press('Control+A');
await page.keyboard.type(`<p>${marker}</p>`);

await page.locator('.st-pe-save-bar button[type="submit"]').click();
await page.waitForURL(/\/Admin\/Products\/Edit\/61/, { timeout: 30000 });

const adminShort = await page.inputValue('textarea[name="ShortDescription"]');
const hasPm = (await page.locator('.ProseMirror').innerHTML()).includes(marker);

await page.goto(`${base}/Shop/Product/${encodeURIComponent(slug)}`);
const html = await page.content();
const leadOk = html.includes(shortVal);
const bodyOk = html.includes(marker);

console.log(JSON.stringify({ adminShort, hasPm, leadOk, bodyOk, marker }, null, 2));
await browser.close();
process.exit(leadOk && bodyOk && hasPm ? 0 : 1);
