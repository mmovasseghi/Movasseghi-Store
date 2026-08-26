'use client'

import Image from 'next/image'
import Link from 'next/link'
import { useEffect, useRef } from 'react'
import { cartSubtotal, formatIrt } from '@/commerce/cart'
import { Button } from '@/components/ui/Button'
import { Sheet } from '@/components/ui/Sheet'
import { useCart } from '@/components/shop/CartProvider'
import { trackViewCart } from '@/lib/analytics'

type Props = {
  open: boolean
  onClose: () => void
}

export function CartSheet({ open, onClose }: Props) {
  const { cart, removeItem, setQuantity } = useCart()
  const subtotal = cartSubtotal(cart)
  const trackedOpen = useRef(false)

  useEffect(() => {
    if (open && cart.items.length > 0 && !trackedOpen.current) {
      trackedOpen.current = true
      trackViewCart(subtotal, cart.items.length)
    }
    if (!open) {
      trackedOpen.current = false
    }
  }, [open, cart.items.length, subtotal])

  return (
    <Sheet open={open} onClose={onClose} title="سبد خرید">
      {cart.items.length === 0 ? (
        <div className="p-6 text-center">
          <p className="text-brand-muted">سبد خرید خالی است.</p>
          <Link href="/shop" onClick={onClose} className="mt-4 inline-block">
            <Button variant="primary">رفتن به فروشگاه</Button>
          </Link>
        </div>
      ) : (
        <>
          <ul className="divide-y divide-border">
            {cart.items.map((item) => (
              <li key={item.productId} className="flex gap-3 p-4">
                <div className="relative h-16 w-16 shrink-0 overflow-hidden rounded-lg bg-brand-aqua-pale">
                  {item.imageUrl ? (
                    <Image src={item.imageUrl} alt={item.name} fill className="object-contain p-1" sizes="64px" />
                  ) : null}
                </div>
                <div className="min-w-0 flex-1">
                  <Link
                    href={`/product/${item.slug}`}
                    onClick={onClose}
                    className="line-clamp-2 text-sm font-medium text-brand-ink hover:text-brand-green"
                  >
                    {item.name}
                  </Link>
                  <p className="mt-0.5 text-sm tabular-nums text-brand-green">{formatIrt(item.unitPrice)}</p>
                  <div className="mt-2 flex items-center gap-2">
                    <button
                      type="button"
                      aria-label="کاهش"
                      className="flex h-9 w-9 items-center justify-center rounded-lg border border-border"
                      onClick={() => setQuantity(item.productId, item.quantity - 1)}
                    >
                      −
                    </button>
                    <span className="min-w-8 text-center text-sm tabular-nums">{item.quantity}</span>
                    <button
                      type="button"
                      aria-label="افزایش"
                      className="flex h-9 w-9 items-center justify-center rounded-lg border border-border"
                      onClick={() => setQuantity(item.productId, item.quantity + 1)}
                    >
                      +
                    </button>
                    <button
                      type="button"
                      onClick={() => removeItem(item.productId)}
                      className="ms-auto text-xs text-brand-muted hover:text-red-600"
                    >
                      حذف
                    </button>
                  </div>
                </div>
              </li>
            ))}
          </ul>
          <div className="border-t border-border p-4">
            <div className="flex items-center justify-between">
              <span className="font-semibold text-brand-ink">جمع</span>
              <span className="text-lg font-bold tabular-nums text-brand-green">{formatIrt(subtotal)}</span>
            </div>
            <div className="mt-4 grid gap-2">
              <Link href="/checkout" onClick={onClose}>
                <Button fullWidth>تسویه حساب</Button>
              </Link>
              <Link href="/cart" onClick={onClose}>
                <Button variant="secondary" fullWidth>
                  مشاهده سبد کامل
                </Button>
              </Link>
            </div>
          </div>
        </>
      )}
    </Sheet>
  )
}
