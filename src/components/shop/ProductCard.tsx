import Image from 'next/image'
import Link from 'next/link'
import { Badge } from '@/components/ui/Badge'
import { cn, formatPrice } from '@/lib/utils'

type ProductCardProps = {
  slug: string
  name: string
  shortDescription?: string | null
  price: number
  salePrice?: number | null
  imageUrl?: string | null
  packSize?: string | null
  wholesalePrice?: number | null
  inStock?: boolean
  hasDiscount?: boolean
  variant?: 'card' | 'grid'
}

export function ProductCard({
  slug,
  name,
  shortDescription,
  price,
  salePrice,
  imageUrl,
  packSize,
  wholesalePrice,
  inStock = true,
  hasDiscount: hasDiscountProp,
  variant = 'grid',
}: ProductCardProps) {
  const displayPrice = salePrice && salePrice > 0 && salePrice < price ? salePrice : price
  const hasDiscount = hasDiscountProp ?? displayPrice < price
  const isGrid = variant === 'grid'

  return (
    <Link
      href={`/product/${slug}`}
      className={cn(
        'group flex flex-col bg-white transition motion-reduce:transition-none',
        isGrid ? 'hover:z-10 hover:shadow-md' : 'overflow-hidden rounded-xl border border-border shadow-sm',
      )}
    >
      <div className="relative aspect-square bg-brand-aqua-pale/50">
        {hasDiscount && (
          <Badge variant="sale" className="absolute top-2 start-2 z-10">
            ویژه
          </Badge>
        )}
        {imageUrl ? (
          <Image
            src={imageUrl}
            alt={name}
            fill
            className="object-contain p-3 transition-transform duration-300 group-hover:scale-[1.03] motion-reduce:transition-none motion-reduce:group-hover:scale-100"
            sizes="(max-width:768px) 50vw, 25vw"
          />
        ) : (
          <div className="flex h-full items-center justify-center text-sm text-brand-muted">بدون تصویر</div>
        )}
      </div>
      <div className="flex flex-1 flex-col gap-2 p-3 md:p-4">
        <h3 className="line-clamp-2 text-sm font-medium leading-snug text-brand-ink group-hover:text-brand-green md:text-base">
          {name}
        </h3>
        {shortDescription && (
          <p className="hidden text-xs leading-relaxed text-brand-muted md:line-clamp-2 md:group-hover:line-clamp-3 lg:block">
            <span className="lg:opacity-70 lg:transition-opacity lg:group-hover:opacity-100">
              {shortDescription}
            </span>
          </p>
        )}
        {(packSize || wholesalePrice) && (
          <div className="flex flex-wrap gap-1">
            {packSize && <Badge variant="default">{packSize}</Badge>}
            {wholesalePrice != null && wholesalePrice > 0 && (
              <Badge variant="b2b">قیمت عمده</Badge>
            )}
          </div>
        )}
        <div className="mt-auto flex flex-wrap items-baseline gap-2 pt-1">
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
          {!inStock && price > 0 && (
            <Badge variant="stock" className="ms-auto">
              ناموجود
            </Badge>
          )}
        </div>
      </div>
    </Link>
  )
}
