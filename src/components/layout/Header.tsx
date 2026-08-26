'use client'

import Image from 'next/image'
import Link from 'next/link'
import { useState } from 'react'
import { CartSheet } from '@/components/shop/CartSheet'
import { MobileNav } from '@/components/layout/MobileNav'
import { useCart } from '@/components/shop/CartProvider'
import { cn } from '@/lib/utils'

export function Header() {
  const { count } = useCart()
  const [cartOpen, setCartOpen] = useState(false)

  return (
    <>
      <header className="sticky top-0 z-50 border-b border-border bg-white/95 backdrop-blur supports-[backdrop-filter]:bg-white/80">
        <div className="mx-auto flex h-16 max-w-6xl items-center justify-between gap-3 px-4">
          <div className="flex items-center gap-2">
            <MobileNav />
            <Link href="/" className="flex items-center gap-2">
              <Image
                src="/brand/logo.png"
                alt="فروشگاه موثقی"
                width={140}
                height={44}
                className="hidden h-9 w-auto md:block"
                priority
              />
              <Image
                src="/brand/logo-mobile.png"
                alt="فروشگاه موثقی"
                width={100}
                height={32}
                className="h-8 w-auto md:hidden"
                priority
              />
            </Link>
          </div>

          <nav className="hidden items-center gap-6 text-sm font-medium text-brand-muted md:flex">
            <Link href="/shop" className="hover:text-brand-green">
              فروشگاه
            </Link>
            <Link href="/b2b" className="hover:text-brand-green">
              عمده‌فروشی
            </Link>
            <Link href="/about" className="hover:text-brand-green">
              درباره ما
            </Link>
            <Link href="/contact" className="hover:text-brand-green">
              تماس
            </Link>
          </nav>

          <div className="flex items-center gap-2">
            <Link
              href="/shop"
              className="hidden h-11 items-center rounded-xl border border-border px-3 text-sm text-brand-muted hover:border-brand-green sm:flex"
              aria-label="جستجو"
            >
              جستجو…
            </Link>
            <button
              type="button"
              onClick={() => setCartOpen(true)}
              className={cn(
                'relative flex h-11 min-w-11 items-center justify-center rounded-xl bg-brand-green px-4 text-sm font-semibold text-white',
                'hover:bg-brand-green-light transition-colors',
              )}
              aria-label={`سبد خرید${count > 0 ? `، ${count} قلم` : ''}`}
            >
              <span className="hidden sm:inline">سبد خرید</span>
              <span className="sm:hidden" aria-hidden>
                🛒
              </span>
              {count > 0 && (
                <span className="absolute -top-1.5 -start-1.5 flex h-5 min-w-5 items-center justify-center rounded-full bg-brand-aqua px-1 text-xs font-bold text-brand-ink">
                  {count}
                </span>
              )}
            </button>
          </div>
        </div>
      </header>
      <CartSheet open={cartOpen} onClose={() => setCartOpen(false)} />
    </>
  )
}
