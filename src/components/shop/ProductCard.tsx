import Image from 'next/image'
import Link from 'next/link'
import { formatPrice } from '@/lib/utils'

type ProductCardProps = {
  slug: string
  name: string
  price: number
  salePrice?: number | null
  imageUrl?: string | null
}

export function ProductCard({ slug, name, price, salePrice, imageUrl }: ProductCardProps) {
  const displayPrice = salePrice && salePrice > 0 && salePrice < price ? salePrice : price
  const hasDiscount = displayPrice < price

  return (
    <Link
      href={`/product/${slug}`}
      className="group flex flex-col overflow-hidden rounded-lg border border-border bg-white shadow-sm transition hover:shadow-md"
    >
      <div className="relative aspect-square bg-brand-aqua-pale">
        {imageUrl ? (
          <Image
            src={imageUrl}
            alt={name}
            fill
            className="object-cover"
            sizes="(max-width:768px) 50vw, 25vw"
          />
        ) : (
          <div className="flex h-full items-center justify-center text-brand-muted text-sm">
            بدون تصویر
          </div>
        )}
      </div>
      <div className="flex flex-1 flex-col gap-2 p-4">
        <h3 className="line-clamp-2 text-sm font-medium text-brand-ink group-hover:text-brand-green">
          {name}
        </h3>
        <div className="mt-auto flex items-baseline gap-2">
          {price > 0 ? (
            <>
              <span className="font-bold tabular-nums text-brand-ink">
                {formatPrice(displayPrice)} تومان
              </span>
              {hasDiscount && (
                <span className="text-xs text-brand-muted line-through tabular-nums">
                  {formatPrice(price)}
                </span>
              )}
            </>
          ) : (
            <span className="text-sm font-medium text-brand-green">تماس برای قیمت</span>
          )}
        </div>
      </div>
    </Link>
  )
}
