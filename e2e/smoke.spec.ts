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
})
