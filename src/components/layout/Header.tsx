'use client'

import Link from 'next/link'
import { useCart } from '@/components/shop/CartProvider'
import { cn } from '@/lib/utils'

export function Header() {
  const { count } = useCart()

  return (
    <header className="sticky top-0 z-50 border-b border-border bg-white/95 backdrop-blur">
      <div className="mx-auto flex h-16 max-w-6xl items-center justify-between gap-4 px-4">
        <Link href="/" className="text-lg font-bold text-brand-ink">
          فروشگاه موثقی
        </Link>
        <nav className="hidden items-center gap-6 text-sm text-brand-muted md:flex">
          <Link href="/shop" className="hover:text-brand-green">
            فروشگاه
          </Link>
          <Link href="/b2b" className="hover:text-brand-green">
            عمده‌فروشی
          </Link>
          <Link href="/about" className="hover:text-brand-green">
            درباره ما
          </Link>
        </nav>
        <Link
          href="/cart"
          className={cn(
            'relative rounded-full bg-brand-green px-4 py-2 text-sm font-medium text-white',
            'hover:bg-brand-green-light transition-colors',
          )}
        >
          سبد خرید
          {count > 0 && (
            <span className="absolute -top-2 -start-2 flex h-5 min-w-5 items-center justify-center rounded-full bg-brand-aqua px-1 text-xs font-bold text-brand-ink">
              {count}
            </span>
          )}
        </Link>
      </div>
    </header>
  )
}
