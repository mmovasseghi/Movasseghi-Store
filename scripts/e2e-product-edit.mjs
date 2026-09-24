import { chromium } from 'playwright';

const base = 'http://localhost:5274';
const marker = `PW_${Date.now()}`;

const browser = await chromium.launch({ headless: true });
const page = await browser.newPage();

await page.goto(`${base}/Admin/Auth/Login`);
await page.fill('input[name="username"]', 'admin');
await page.fill('input[name="password"]', 'Admin@123456');
await page.click('button[type="submit"]');
await page.waitForURL(/\/Admin\//, { timeout: 15000 });

await page.goto(`${base}/Admin/Products/Edit/23`);
await page.waitForSelector('.ProseMirror', { timeout: 60000 });

const editorOk = await page.evaluate(async () => {
  for (let i = 0; i < 40; i++) {
    if (window.__rteEditors?.['product-desc']) return true;
    await new Promise((r) => setTimeout(r, 250));
  }
  return false;
});
console.log('rte editor registered:', editorOk);

await page.locator('.ProseMirror').click();
await page.keyboard.press('Control+A');
await page.keyboard.type(marker);

await page.locator('.st-pe-save-bar button[type="submit"]').click();
await page.waitForURL(/\/Admin\/Products\/Edit\/23/, { timeout: 30000 });

const textarea = await page.locator('#product-desc').inputValue();
const proseMirror = await page.locator('.ProseMirror').innerText();
console.log('marker:', marker);
console.log('textarea has marker:', textarea.includes(marker));
console.log('prosemirror has marker:', proseMirror.includes(marker));

await browser.close();
process.exit(textarea.includes(marker) ? 0 : 1);
