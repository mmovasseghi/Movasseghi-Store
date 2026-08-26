import Image from 'next/image'
import Link from 'next/link'
import { cn, formatPrice } from '@/lib/utils'

type ProductCardProps = {
  slug: string
  name: string
  price: number
  salePrice?: number | null
  imageUrl?: string | null
  variant?: 'card' | 'grid'
}

export function ProductCard({
  slug,
  name,
  price,
  salePrice,
  imageUrl,
  variant = 'grid',
}: ProductCardProps) {
  const displayPrice = salePrice && salePrice > 0 && salePrice < price ? salePrice : price
  const hasDiscount = displayPrice < price
  const isGrid = variant === 'grid'

  return (
    <Link
      href={`/product/${slug}`}
      className={cn(
        'group flex flex-col bg-white transition hover:z-10 hover:shadow-md',
        isGrid ? 'bg-white' : 'overflow-hidden rounded-xl border border-border shadow-sm',
      )}
    >
      <div className="relative aspect-square bg-brand-aqua-pale/50">
        {imageUrl ? (
          <Image
            src={imageUrl}
            alt={name}
            fill
            className="object-contain p-3 transition-transform duration-300 group-hover:scale-105 motion-reduce:transition-none"
            sizes="(max-width:768px) 50vw, 25vw"
          />
        ) : (
          <div className="flex h-full items-center justify-center text-brand-muted text-sm">
            بدون تصویر
          </div>
        )}
      </div>
      <div className="flex flex-1 flex-col gap-2 p-3 md:p-4">
        <h3 className="line-clamp-2 text-sm font-medium leading-snug text-brand-ink group-hover:text-brand-green md:text-base">
          {name}
        </h3>
        <div className="mt-auto flex items-baseline gap-2 pt-1">
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
            <span className="text-sm font-semibold text-brand-green">تماس برای قیمت</span>
          )}
        </div>
      </div>
    </Link>
  )
}
