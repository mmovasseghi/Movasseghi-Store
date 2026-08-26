import Link from 'next/link'
import { cn } from '@/lib/utils'

type Props = {
  slug: string
  name: string
  productCount?: number | null
  className?: string
}

export function CategoryCard({ slug, name, productCount, className }: Props) {
  return (
    <Link
      href={`/shop/${slug}`}
      className={cn(
        'group flex min-w-[140px] shrink-0 flex-col justify-between rounded-xl border border-border bg-white p-4',
        'transition hover:border-brand-green hover:shadow-sm motion-safe:transition-shadow',
        className,
      )}
    >
      <span className="font-semibold leading-snug text-brand-ink group-hover:text-brand-green">{name}</span>
      <span className="mt-3 flex items-center justify-between text-xs text-brand-muted">
        {productCount != null && productCount > 0 ? `${productCount} محصول` : 'مشاهده'}
        <span aria-hidden className="text-brand-green transition group-hover:translate-x-[-2px]">
          ←
        </span>
      </span>
    </Link>
  )
}
