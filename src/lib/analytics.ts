'use client'

type EventParams = Record<string, string | number | boolean | undefined>

declare global {
  interface Window {
    dataLayer?: Record<string, unknown>[]
  }
}

/** GTM-compatible analytics — see docs/ANALYTICS-PLAN.md */
export function trackEvent(event: string, params?: EventParams): void {
  if (typeof window === 'undefined') return
  window.dataLayer = window.dataLayer ?? []
  window.dataLayer.push({ event, ...params })
}

export function trackPhoneClick(location: string): void {
  trackEvent('phone_click', { location })
}

export function trackAddToCart(item: {
  productId: string
  name: string
  price: number
  quantity: number
}): void {
  trackEvent('add_to_cart', {
    item_id: item.productId,
    item_name: item.name,
    price: item.price,
    quantity: item.quantity,
    value: item.price * item.quantity,
  })
}

export function trackViewItem(item: {
  productId: string
  name: string
  price: number
}): void {
  trackEvent('view_item', {
    item_id: item.productId,
    item_name: item.name,
    price: item.price,
  })
}

export function trackBeginCheckout(value: number, itemCount: number): void {
  trackEvent('begin_checkout', { value, item_count: itemCount })
}

export function trackPurchase(transactionId: string, value: number): void {
  trackEvent('purchase', { transaction_id: transactionId, value })
}

export function trackViewCart(value: number, itemCount: number): void {
  trackEvent('view_cart', { value, item_count: itemCount })
}
