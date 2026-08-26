'use client'

import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { useState } from 'react'
import { cartSubtotal, formatIrt } from '@/commerce/cart'
import { useCart } from '@/components/shop/CartProvider'
import { cn } from '@/lib/utils'

type PaymentMethod = 'online' | 'card_to_card' | 'phone'
type ShippingMethod = 'customer' | 'seller'

export function CheckoutForm() {
  const router = useRouter()
  const { cart } = useCart()
  const subtotal = cartSubtotal(cart)

  const [name, setName] = useState('')
  const [phone, setPhone] = useState('')
  const [note, setNote] = useState('')
  const [payment, setPayment] = useState<PaymentMethod>('phone')
  const [shipping, setShipping] = useState<ShippingMethod>('seller')
  const [submitting, setSubmitting] = useState(false)

  if (cart.items.length === 0) {
    return (
      <div className="mt-8 rounded-xl border border-border bg-white p-8 text-center">
        <p className="text-brand-muted">سبد خرید خالی است.</p>
        <Link href="/shop" className="mt-4 inline-block text-brand-green hover:underline">
          بازگشت به فروشگاه
        </Link>
      </div>
    )
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!name.trim() || !phone.trim()) return
    setSubmitting(true)

    const orderId = `ORD-${Date.now()}`
    const payload = {
      orderId,
      name,
      phone,
      note,
      payment,
      shipping,
      items: cart.items,
      subtotal,
      createdAt: new Date().toISOString(),
    }
    localStorage.setItem(`movasseghi-order-${orderId}`, JSON.stringify(payload))

    try {
      await fetch('/api/orders', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      })
    } catch {
      // local backup already saved
    }

    if (payment === 'phone') {
      window.location.href = `tel:09125199105`
    }

    router.push(`/checkout/success?order=${orderId}`)
  }

  return (
    <form onSubmit={handleSubmit} className="mt-8 grid gap-8 lg:grid-cols-5">
      <div className="space-y-6 lg:col-span-3">
        <section className="rounded-xl border border-border bg-white p-5">
          <h2 className="font-semibold text-brand-ink">اطلاعات تماس</h2>
          <div className="mt-4 grid gap-4 sm:grid-cols-2">
            <label className="block sm:col-span-2">
              <span className="mb-1 block text-sm text-brand-muted">نام و نام خانوادگی *</span>
              <input
                required
                value={name}
                onChange={(e) => setName(e.target.value)}
                className="h-11 w-full rounded-lg border border-border px-3 outline-none focus:ring-2 focus:ring-brand-green/30"
              />
            </label>
            <label className="block sm:col-span-2">
              <span className="mb-1 block text-sm text-brand-muted">شماره موبایل *</span>
              <input
                required
                type="tel"
                dir="ltr"
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                placeholder="09xxxxxxxxx"
                className="h-11 w-full rounded-lg border border-border px-3 text-start outline-none focus:ring-2 focus:ring-brand-green/30"
              />
            </label>
            <label className="block sm:col-span-2">
              <span className="mb-1 block text-sm text-brand-muted">توضیحات سفارش</span>
              <textarea
                value={note}
                onChange={(e) => setNote(e.target.value)}
                rows={3}
                className="w-full rounded-lg border border-border px-3 py-2 outline-none focus:ring-2 focus:ring-brand-green/30"
              />
            </label>
          </div>
        </section>

        <section className="rounded-xl border border-border bg-white p-5">
          <h2 className="font-semibold text-brand-ink">روش ارسال</h2>
          <div className="mt-3 space-y-2">
            {(
              [
                { id: 'seller' as const, label: 'ارسال توسط فروشگاه', desc: 'هماهنگی حمل در تهران و سراسر کشور' },
                { id: 'customer' as const, label: 'تحویل با خودروی مشتری', desc: 'هماهنگی زمان تحویل از انبار' },
              ] as const
            ).map((opt) => (
              <label
                key={opt.id}
                className={cn(
                  'flex cursor-pointer gap-3 rounded-lg border p-4 transition',
                  shipping === opt.id ? 'border-brand-green bg-brand-aqua-pale/50' : 'border-border',
                )}
              >
                <input
                  type="radio"
                  name="shipping"
                  checked={shipping === opt.id}
                  onChange={() => setShipping(opt.id)}
                  className="mt-1"
                />
                <span>
                  <span className="block font-medium text-brand-ink">{opt.label}</span>
                  <span className="text-sm text-brand-muted">{opt.desc}</span>
                </span>
              </label>
            ))}
          </div>
        </section>

        <section className="rounded-xl border border-border bg-white p-5">
          <h2 className="font-semibold text-brand-ink">روش پرداخت</h2>
          <div className="mt-3 space-y-2">
            {(
              [
                {
                  id: 'phone' as const,
                  label: 'سفارش تلفنی',
                  desc: 'ثبت درخواست و تماس کارشناس فروش',
                  enabled: true,
                },
                {
                  id: 'card_to_card' as const,
                  label: 'کارت به کارت',
                  desc: 'پس از ثبت سفارش، شماره حساب ارسال می‌شود',
                  enabled: true,
                },
                {
                  id: 'online' as const,
                  label: 'پرداخت آنلاین (زرین‌پال / نکست‌پی)',
                  desc: 'به‌زودی — پس از اتصال درگاه رسمی',
                  enabled: false,
                },
              ] as const
            ).map((opt) => (
              <label
                key={opt.id}
                className={cn(
                  'flex gap-3 rounded-lg border p-4 transition',
                  !opt.enabled && 'cursor-not-allowed opacity-50',
                  opt.enabled && payment === opt.id ? 'border-brand-green bg-brand-aqua-pale/50' : 'border-border',
                  opt.enabled && 'cursor-pointer',
                )}
              >
                <input
                  type="radio"
                  name="payment"
                  disabled={!opt.enabled}
                  checked={payment === opt.id}
                  onChange={() => setPayment(opt.id)}
                  className="mt-1"
                />
                <span>
                  <span className="block font-medium text-brand-ink">{opt.label}</span>
                  <span className="text-sm text-brand-muted">{opt.desc}</span>
                </span>
              </label>
            ))}
          </div>
        </section>
      </div>

      <aside className="lg:col-span-2">
        <div className="sticky top-20 rounded-xl border border-border bg-white p-5">
          <h2 className="font-semibold text-brand-ink">خلاصه سفارش</h2>
          <ul className="mt-4 max-h-64 space-y-3 overflow-y-auto text-sm">
            {cart.items.map((item) => (
              <li key={item.productId} className="flex justify-between gap-2">
                <span className="line-clamp-2 text-brand-muted">
                  {item.name} × {item.quantity}
                </span>
                <span className="shrink-0 tabular-nums">{formatIrt(item.unitPrice * item.quantity)}</span>
              </li>
            ))}
          </ul>
          <div className="mt-4 flex justify-between border-t border-border pt-4 font-bold">
            <span>جمع</span>
            <span className="tabular-nums text-brand-green">{formatIrt(subtotal)}</span>
          </div>
          <button
            type="submit"
            disabled={submitting}
            className="mt-4 w-full rounded-xl bg-brand-green py-3.5 font-semibold text-white hover:bg-brand-green-light disabled:opacity-60"
          >
            {submitting ? 'در حال ثبت…' : 'ثبت سفارش'}
          </button>
          <p className="mt-3 text-center text-xs text-brand-muted">
            B2B / فاکتور سازمانی؟{' '}
            <Link href="/b2b" className="text-brand-green hover:underline">
              درخواست عمده
            </Link>
          </p>
        </div>
      </aside>
    </form>
  )
}
