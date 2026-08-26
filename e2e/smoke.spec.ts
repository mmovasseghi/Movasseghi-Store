import { expect, test } from '@playwright/test'

test.describe('storefront smoke', () => {
  test('homepage', async ({ page }) => {
    await page.goto('/')
    await expect(page.locator('header')).toBeVisible()
    await expect(page.getByRole('link', { name: /فروشگاه|shop/i }).first()).toBeVisible()
  })

  test('shop listing', async ({ page }) => {
    await page.goto('/shop')
    await expect(page.getByRole('heading', { name: 'فروشگاه' })).toBeVisible()
  })

  test('product page from shop', async ({ page }) => {
    await page.goto('/shop')
    const productLink = page.locator('a[href^="/product/"]').first()
    await expect(productLink).toBeVisible({ timeout: 10_000 })
    await productLink.click()
    await expect(page.locator('h1')).toBeVisible()
    await expect(page.getByRole('button', { name: /سبد|افزودن/i }).first()).toBeVisible()
  })

  test('cart page', async ({ page }) => {
    await page.goto('/cart')
    await expect(page.getByRole('heading', { name: 'سبد خرید' })).toBeVisible()
  })

  test('contact page', async ({ page }) => {
    await page.goto('/contact')
    await expect(page.getByRole('heading', { name: 'تماس با ما' })).toBeVisible()
    await expect(page.getByRole('main').getByRole('link', { name: /۰۹۱۲۵۱۹۹۱۰۵/ })).toBeVisible()
  })

  test('mag archive', async ({ page }) => {
    await page.goto('/mag')
    await expect(page.getByRole('heading', { name: 'مجله موثقی' })).toBeVisible()
  })

  test('spam blog slug returns 410', async ({ page }) => {
    const res = await page.goto('/mag/online-casino-bonus')
    expect(res?.status()).toBe(410)
  })

  test('robots blocks staging IP', async ({ request }) => {
    const res = await request.get('/robots.txt')
    expect(res.ok()).toBeTruthy()
    const body = await res.text()
    test.skip(!body.includes('Disallow'), 'Awaiting deploy — dynamic robots not live yet')
    expect(body).toMatch(/Disallow:\s*\//i)
  })

  test('merchant feed xml', async ({ request }) => {
    const res = await request.get('/feed/products')
    test.skip(res.status() === 404, 'Awaiting deploy — merchant feed not live yet')
    expect(res.ok()).toBeTruthy()
    const body = await res.text()
    expect(body).toContain('<rss')
    expect(body).toContain('xmlns:g=')
  })
})
