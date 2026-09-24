import { chromium } from 'playwright';

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 390, height: 844 } });

const phone = '0912' + String(Date.now()).slice(-7);
const password = 'test1234';

await page.goto('http://localhost:5274/', { waitUntil: 'networkidle', timeout: 60000 });
await page.locator('[data-open-auth]').first().click();
await page.locator('[data-auth-tab="register"]').click();
await page.locator('#authPhone').fill(phone);
await page.locator('[data-auth-phone-form] [type=submit]').click();
await page.waitForSelector('[data-auth-step="otp"].is-active');
const boxes = page.locator('.ms-otp-box');
for (let i = 0; i < 6; i++) await boxes.nth(i).fill('123456'[i]);
await page.locator('[data-auth-otp-form] [type=submit]').click();
await page.waitForSelector('[data-auth-step="register"].is-active');
await page.locator('#authFirstName').fill('تست');
await page.locator('#authLastName').fill('آدرس');
await page.locator('#authPassword').fill(password);
await page.locator('#authConfirmPassword').fill(password);
await page.locator('[data-auth-register-form] [type=submit]').click();
await page.waitForURL(/\/Account/i, { timeout: 15000 });

await page.locator('[data-tab="addresses"]').click();
await page.waitForTimeout(500);

await page.context().setGeolocation({ latitude: 35.69859, longitude: 51.49657 });
await page.context().grantPermissions(['geolocation']);

const geocodePromise = page.waitForResponse(
  r => r.url().includes('/Account/ReverseGeocode') && r.status() !== 0,
  { timeout: 15000 }
);

await page.locator('[data-locate-me]').click();
await geocodePromise;
await page.waitForTimeout(800);

const overlayHidden = await page.locator('[data-map-overlay]').evaluate(el => el.hidden);
const province = await page.locator('[data-addr-province]').inputValue();
const city = await page.locator('[data-addr-city]').inputValue();

console.log(JSON.stringify({ ok: overlayHidden, overlayHidden, province, city }, null, 2));
if (!overlayHidden) process.exit(1);

await browser.close();
