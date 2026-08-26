'use client'

import Image from 'next/image'
import Link from 'next/link'
import { cartSubtotal, formatIrt } from '@/commerce/cart'
import { useCart } from '@/components/shop/CartProvider'

export function CartView() {
  const { cart, removeItem, setQuantity } = useCart()
  const subtotal = cartSubtotal(cart)

  if (cart.items.length === 0) {
    return (
      <div className="mt-8 rounded-xl border border-border bg-white p-8 text-center">
        <p className="text-brand-muted">سبد خرید شما خالی است.</p>
        <Link
          href="/shop"
          className="mt-4 inline-block rounded-xl bg-brand-green px-6 py-3 font-medium text-white hover:bg-brand-green-light"
        >
          رفتن به فروشگاه
        </Link>
      </div>
    )
  }

  return (
    <div className="mt-8 space-y-4">
      <ul className="divide-y divide-border rounded-xl border border-border bg-white">
        {cart.items.map((item) => (
          <li key={item.productId} className="flex gap-4 p-4">
            <div className="relative h-20 w-20 shrink-0 overflow-hidden rounded-lg bg-brand-aqua-pale">
              {item.imageUrl ? (
                <Image
                  src={item.imageUrl}
                  alt={item.name}
                  fill
                  className="object-cover"
                  sizes="80px"
                />
              ) : (
                <div className="flex h-full items-center justify-center text-xs text-brand-muted">
                  —
                </div>
              )}
            </div>
            <div className="min-w-0 flex-1">
              <Link
                href={`/product/${item.slug}`}
                className="font-medium text-brand-ink hover:text-brand-green"
              >
                {item.name}
              </Link>
              <p className="mt-1 tabular-nums text-brand-green">{formatIrt(item.unitPrice)}</p>
              <div className="mt-2 flex items-center gap-3">
                <input
                  type="number"
                  min={1}
                  value={item.quantity}
                  onChange={(e) => setQuantity(item.productId, Number(e.target.value) || 1)}
                  className="h-9 w-16 rounded border border-border text-center text-sm"
                  aria-label="تعداد"
                />
                <button
                  type="button"
                  onClick={() => removeItem(item.productId)}
                  className="text-sm text-brand-muted hover:text-red-600"
                >
                  حذف
                </button>
              </div>
            </div>
            <p className="shrink-0 tabular-nums font-semibold text-brand-ink">
              {formatIrt(item.unitPrice * item.quantity)}
            </p>
          </li>
        ))}
      </ul>
      <div className="flex items-center justify-between rounded-xl border border-border bg-white p-4">
        <span className="font-semibold text-brand-ink">جمع کل</span>
        <span className="text-xl font-bold tabular-nums text-brand-green">
          {formatIrt(subtotal)}
        </span>
      </div>
      <Link
        href="/checkout"
        className="block rounded-xl bg-brand-green py-3.5 text-center font-semibold text-white hover:bg-brand-green-light"
      >
        ادامه به تسویه حساب
      </Link>
    </div>
  )
}
