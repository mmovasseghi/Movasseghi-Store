import { chromium } from 'playwright';
import { mkdir } from 'fs/promises';

await mkdir('scripts/site-screenshots', { recursive: true });
const phone = '0912' + String(Date.now()).slice(-7);
const password = 'test1234';
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 390, height: 844 } });
await page.goto('http://localhost:5274/', { waitUntil: 'networkidle', timeout: 60000 });
await page.locator('[data-open-auth]').first().click();
await page.waitForSelector('[data-auth-panel]', { timeout: 10000 });

// Register tab
await page.locator('[data-auth-tab="register"]').click();
await page.locator('#authPhone').fill(phone);
await page.screenshot({ path: 'scripts/site-screenshots/auth-reg-step1.png' });
await page.locator('[data-auth-phone-form] [type=submit]').click();
await page.waitForSelector('[data-auth-step="otp"].is-active', { timeout: 10000 });
await page.screenshot({ path: 'scripts/site-screenshots/auth-reg-step2.png' });
const boxes = page.locator('.ms-otp-box');
for (let i = 0; i < 6; i++) await boxes.nth(i).fill('123456'[i]);
await page.locator('[data-auth-otp-form] [type=submit]').click();
await page.waitForSelector('[data-auth-step="register"].is-active', { timeout: 10000 });
await page.screenshot({ path: 'scripts/site-screenshots/auth-reg-step3.png' });
await page.locator('#authFirstName').fill('علی');
await page.locator('#authLastName').fill('موثقی');
await page.locator('#authEmail').fill('test@example.com');
await page.locator('#authBusiness').fill('رستوران نمونه');
await page.locator('#authPassword').fill(password);
await page.locator('#authConfirmPassword').fill(password);
await page.locator('[data-auth-register-form] [type=submit]').click();
await page.waitForURL(/\/Account/i, { timeout: 15000 });
await page.screenshot({ path: 'scripts/site-screenshots/auth-dashboard.png' });
const title = await page.locator('.ms-account-title').textContent();
const name = await page.locator('.ms-account-hero-meta h2').textContent();
console.log(JSON.stringify({ ok: true, phone, password, title: title?.trim(), name: name?.trim(), url: page.url() }, null, 2));

// Logout and test password login
await page.locator('[data-logout-open]').click();
await page.locator('#logoutSheet button[type=submit]').click();
await page.waitForURL(/\//, { timeout: 10000 });
await page.locator('[data-open-auth]').first().click();
await page.waitForSelector('[data-auth-panel]', { timeout: 10000 });
await page.locator('[data-auth-login-mode="password"]').click();
await page.locator('#authLoginPhone').fill(phone);
await page.locator('#authLoginPassword').fill(password);
await page.locator('[data-auth-password-form] [type=submit]').click();
await page.waitForURL(/\/Account/i, { timeout: 15000 });
const nameAfterPwd = await page.locator('.ms-account-hero-meta h2').textContent();
console.log(JSON.stringify({ passwordLoginOk: true, name: nameAfterPwd?.trim() }, null, 2));

await browser.close();
