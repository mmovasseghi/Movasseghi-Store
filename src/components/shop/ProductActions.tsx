'use client'

import Link from 'next/link'
import { useState } from 'react'
import { formatIrt } from '@/commerce/cart'
import { useCart } from '@/components/shop/CartProvider'
import { trackAddToCart, trackPhoneClick } from '@/lib/analytics'
import { cn, formatPrice } from '@/lib/utils'

type ProductActionsProps = {
  productId: string
  slug: string
  name: string
  price: number
  regularPrice: number
  salePrice?: number | null
  imageUrl?: string | null
  stockQuantity?: number | null
}

export function ProductActions({
  productId,
  slug,
  name,
  price,
  regularPrice,
  salePrice,
  imageUrl,
  stockQuantity,
}: ProductActionsProps) {
  const { addItem } = useCart()
  const [qty, setQty] = useState(1)
  const [added, setAdded] = useState(false)

  const hasDiscount = salePrice && salePrice > 0 && salePrice < regularPrice
  const inStock = stockQuantity == null || stockQuantity > 0

  const handleAdd = () => {
    if (price <= 0) return
    addItem({ productId, slug, name, unitPrice: price, imageUrl: imageUrl ?? undefined }, qty)
    trackAddToCart({ productId, name, price, quantity: qty })
    setAdded(true)
    setTimeout(() => setAdded(false), 2000)
  }

  return (
    <div className="space-y-5">
      <div>
        {price > 0 ? (
          <div className="flex flex-wrap items-baseline gap-3">
            <p className="text-3xl font-bold tabular-nums text-brand-green">{formatIrt(price)}</p>
            {hasDiscount && (
              <p className="text-base text-brand-muted line-through tabular-nums">
                {formatPrice(regularPrice)} تومان
              </p>
            )}
          </div>
        ) : (
          <p className="text-2xl font-bold text-brand-green">تماس برای استعلام قیمت</p>
        )}
        {stockQuantity != null && stockQuantity > 0 && (
          <p className="mt-1 text-sm text-brand-muted">
            موجودی: {stockQuantity.toLocaleString('fa-IR')} عدد
          </p>
        )}
      </div>

      {price > 0 && (
        <div className="flex items-center gap-3">
          <label htmlFor="product-qty" className="text-sm text-brand-muted">
            تعداد
          </label>
          <div className="flex items-center rounded-lg border border-border bg-white">
            <button
              type="button"
              className="flex h-11 w-11 items-center justify-center text-lg hover:bg-brand-aqua-pale"
              onClick={() => setQty((q) => Math.max(1, q - 1))}
              aria-label="کاهش"
            >
              −
            </button>
            <input
              id="product-qty"
              type="number"
              min={1}
              value={qty}
              onChange={(e) => setQty(Math.max(1, Number(e.target.value) || 1))}
              className="h-11 w-14 border-x border-border bg-transparent text-center tabular-nums outline-none"
            />
            <button
              type="button"
              className="flex h-11 w-11 items-center justify-center text-lg hover:bg-brand-aqua-pale"
              onClick={() => setQty((q) => q + 1)}
              aria-label="افزایش"
            >
              +
            </button>
          </div>
        </div>
      )}

      <div className="flex flex-col gap-3 sm:flex-row">
        <button
          type="button"
          disabled={price <= 0 || !inStock}
          onClick={handleAdd}
          className={cn(
            'min-h-11 flex-1 rounded-xl px-6 py-3 text-base font-semibold text-white transition',
            price <= 0 || !inStock
              ? 'cursor-not-allowed bg-brand-muted/50'
              : added
                ? 'bg-brand-green-light'
                : 'bg-brand-green hover:bg-brand-green-light',
          )}
        >
          {price <= 0 ? 'تماس برای خرید' : added ? '✓ به سبد اضافه شد' : 'افزودن به سبد خرید'}
        </button>
        <a
          href="tel:09125199105"
          onClick={() => trackPhoneClick('product')}
          className="flex min-h-11 flex-1 items-center justify-center rounded-xl border-2 border-brand-green px-6 py-3 text-base font-semibold text-brand-green hover:bg-brand-aqua-pale"
        >
          تماس سریع
        </a>
      </div>

      <div className="rounded-xl border border-border bg-brand-aqua-pale/40 p-4 text-sm text-brand-muted">
        <p className="font-medium text-brand-ink">سفارش عمده یا سازمانی؟</p>
        <p className="mt-1">برای رستوران، کترینگ و خرید بالک با تیم فروش تماس بگیرید.</p>
        <Link
          href="/b2b"
          className="mt-2 inline-block font-medium text-brand-green hover:underline"
        >
          درخواست عمده‌فروشی ←
        </Link>
      </div>
    </div>
  )
}
