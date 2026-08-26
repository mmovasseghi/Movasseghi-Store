'use client'

import { formatIrt } from '@/commerce/cart'
import { useState } from 'react'

type OrderResult = {
  orderNumber: string
  status: string
  subtotal: number
  paymentMethod: string
  shippingMethod: string
  createdAt: string
  items: { name: string; quantity: number }[]
}

const STATUS_LABEL: Record<string, string> = {
  pending: 'در انتظار تأیید',
  confirmed: 'تأیید شده',
  cancelled: 'لغو شده',
}

export function TrackOrderForm() {
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [order, setOrder] = useState<OrderResult | null>(null)

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    setLoading(true)
    setError(null)
    setOrder(null)
    const fd = new FormData(e.currentTarget)
    try {
      const res = await fetch('/api/orders/track', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          orderNumber: fd.get('orderNumber'),
          phone: fd.get('phone'),
        }),
      })
      const data = (await res.json()) as OrderResult & { error?: string }
      if (!res.ok) {
        setError(data.error ?? 'سفارش یافت نشد')
        setLoading(false)
        return
      }
      setOrder(data)
    } catch {
      setError('خطای شبکه')
    }
    setLoading(false)
  }

  return (
    <div className="mt-8">
      <form onSubmit={handleSubmit} className="space-y-4 rounded-xl border border-border bg-white p-6">
        <label className="block">
          <span className="mb-1 block text-sm text-brand-muted">کد سفارش *</span>
          <input
            name="orderNumber"
            required
            dir="ltr"
            placeholder="ORD-..."
            className="h-11 w-full rounded-lg border border-border px-3 outline-none focus:ring-2 focus:ring-brand-green/30"
          />
        </label>
        <label className="block">
          <span className="mb-1 block text-sm text-brand-muted">موبایل ثبت‌شده *</span>
          <input
            name="phone"
            required
            type="tel"
            dir="ltr"
            placeholder="09xxxxxxxxx"
            className="h-11 w-full rounded-lg border border-border px-3 outline-none focus:ring-2 focus:ring-brand-green/30"
          />
        </label>
        {error && <p className="text-sm text-red-700">{error}</p>}
        <button
          type="submit"
          disabled={loading}
          className="w-full rounded-xl bg-brand-green py-3 font-semibold text-white disabled:opacity-60"
        >
          {loading ? 'در حال جستجو…' : 'پیگیری سفارش'}
        </button>
      </form>

      {order && (
        <div className="mt-6 rounded-xl border border-brand-aqua bg-brand-aqua-pale/30 p-6">
          <p className="font-bold text-brand-ink">{order.orderNumber}</p>
          <p className="mt-1 text-sm text-brand-muted">
            وضعیت: {STATUS_LABEL[order.status] ?? order.status}
          </p>
          <p className="mt-1 text-sm text-brand-muted">جمع: {formatIrt(order.subtotal)}</p>
          <ul className="mt-4 space-y-1 text-sm text-brand-muted">
            {order.items.map((item, i) => (
              <li key={i}>
                {item.name} × {item.quantity}
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  )
}
