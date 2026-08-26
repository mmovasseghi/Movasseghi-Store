'use client'

import { formatIrt } from '@/commerce/cart'
import { useCart } from '@/components/shop/CartProvider'

type ProductStickyBarProps = {
  name: string
  price: number
  productId: string
  slug: string
  imageUrl?: string | null
}

export function ProductStickyBar({
  name,
  price,
  productId,
  slug,
  imageUrl,
}: ProductStickyBarProps) {
  const { addItem } = useCart()

  return (
    <div className="fixed inset-x-0 bottom-0 z-40 border-t border-border bg-white/95 p-3 shadow-[0_-4px_24px_rgba(0,0,0,0.08)] backdrop-blur md:hidden">
      <div className="mx-auto flex max-w-6xl items-center gap-3">
        <div className="min-w-0 flex-1">
          <p className="truncate text-sm font-medium text-brand-ink">{name}</p>
          <p className="text-base font-bold tabular-nums text-brand-green">
            {price > 0 ? formatIrt(price) : 'تماس برای قیمت'}
          </p>
        </div>
        {price > 0 ? (
          <button
            type="button"
            onClick={() =>
              addItem({ productId, slug, name, unitPrice: price, imageUrl: imageUrl ?? undefined })
            }
            className="shrink-0 rounded-xl bg-brand-green px-5 py-3 text-sm font-semibold text-white"
          >
            افزودن به سبد
          </button>
        ) : (
          <a
            href="tel:09125199105"
            className="shrink-0 rounded-xl bg-brand-green px-5 py-3 text-sm font-semibold text-white"
          >
            تماس
          </a>
        )}
      </div>
    </div>
  )
}
