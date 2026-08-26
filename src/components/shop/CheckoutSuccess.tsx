'use client'

import Link from 'next/link'
import { useSearchParams } from 'next/navigation'
import { Suspense, useEffect, useRef } from 'react'
import { useCart } from '@/components/shop/CartProvider'
import { trackPhoneClick, trackPurchase } from '@/lib/analytics'

function SuccessContent() {
  const params = useSearchParams()
  const orderId = params.get('order')
  const { clearCart } = useCart()
  const cleared = useRef(false)

  useEffect(() => {
    if (!orderId || cleared.current) return
    cleared.current = true
    clearCart()

    try {
      const raw = localStorage.getItem(`movasseghi-order-${orderId}`)
      if (raw) {
        const order = JSON.parse(raw) as { subtotal?: number }
        if (typeof order.subtotal === 'number') {
          trackPurchase(orderId, order.subtotal)
        }
      }
    } catch {
      // ignore
    }
  }, [orderId, clearCart])

  return (
    <div className="mt-8 rounded-xl border border-brand-aqua bg-brand-aqua-pale/40 p-8 text-center">
      <p className="text-4xl">✓</p>
      <h2 className="mt-4 text-xl font-bold text-brand-ink">سفارش شما ثبت شد</h2>
      {orderId && <p className="mt-2 text-sm text-brand-muted">کد پیگیری: {orderId}</p>}
      <p className="mx-auto mt-4 max-w-md text-brand-muted">
        کارشناسان فروش در اسرع وقت با شما تماس می‌گیرند. برای پیگیری فوری می‌توانید تماس بگیرید.
      </p>
      <div className="mt-6 flex flex-wrap justify-center gap-3">
        <a
          href="tel:09125199105"
          onClick={() => trackPhoneClick('checkout_success')}
          className="rounded-xl bg-brand-green px-6 py-3 font-semibold text-white hover:bg-brand-green-light"
        >
          تماس ۰۹۱۲۵۱۹۹۱۰۵
        </a>
        <Link
          href="/shop"
          className="rounded-xl border border-brand-green px-6 py-3 font-semibold text-brand-green hover:bg-white"
        >
          ادامه خرید
        </Link>
      </div>
    </div>
  )
}

export function CheckoutSuccess() {
  return (
    <Suspense fallback={<p className="mt-8 text-center text-brand-muted">…</p>}>
      <SuccessContent />
    </Suspense>
  )
}
