'use client'

import Image from 'next/image'
import Link from 'next/link'
import { useEffect, useState } from 'react'
import { useCart } from '@/components/shop/CartProvider'
import { cn } from '@/lib/utils'

const NAV = [
  { href: '/shop', label: 'فروشگاه' },
  { href: '/pricing', label: 'لیست قیمت' },
  { href: '/b2b', label: 'عمده‌فروشی' },
  { href: '/about', label: 'درباره ما' },
  { href: '/contact', label: 'تماس' },
]

export function MobileNav() {
  const [open, setOpen] = useState(false)
  const { count } = useCart()

  useEffect(() => {
    document.body.style.overflow = open ? 'hidden' : ''
    return () => {
      document.body.style.overflow = ''
    }
  }, [open])

  return (
    <>
      <button
        type="button"
        className="flex h-11 w-11 items-center justify-center rounded-lg border border-border md:hidden"
        onClick={() => setOpen(true)}
        aria-label="منو"
      >
        <span className="flex flex-col gap-1.5">
          <span className="block h-0.5 w-5 bg-brand-ink" />
          <span className="block h-0.5 w-5 bg-brand-ink" />
          <span className="block h-0.5 w-3 bg-brand-ink" />
        </span>
      </button>

      {open && (
        <div className="fixed inset-0 z-[60] md:hidden" role="dialog" aria-modal>
          <button
            type="button"
            className="absolute inset-0 bg-brand-ink/40"
            aria-label="بستن"
            onClick={() => setOpen(false)}
          />
          <div className="absolute inset-y-0 start-0 flex w-[min(100%,320px)] flex-col bg-white shadow-xl">
            <div className="flex items-center justify-between border-b border-border p-4">
              <Image src="/brand/logo-mobile.png" alt="فروشگاه موثقی" width={120} height={40} className="h-8 w-auto" />
              <button
                type="button"
                onClick={() => setOpen(false)}
                className="flex h-10 w-10 items-center justify-center rounded-lg hover:bg-brand-aqua-pale"
                aria-label="بستن منو"
              >
                ×
              </button>
            </div>
            <nav className="flex flex-1 flex-col gap-1 p-4">
              {NAV.map((item) => (
                <Link
                  key={item.href}
                  href={item.href}
                  className="rounded-lg px-4 py-3 text-base font-medium text-brand-ink hover:bg-brand-aqua-pale"
                  onClick={() => setOpen(false)}
                >
                  {item.label}
                </Link>
              ))}
            </nav>
            <div className="border-t border-border p-4">
              <Link
                href="/cart"
                onClick={() => setOpen(false)}
                className={cn(
                  'flex items-center justify-center gap-2 rounded-xl bg-brand-green py-3 font-semibold text-white',
                )}
              >
                سبد خرید
                {count > 0 && (
                  <span className="rounded-full bg-white/20 px-2 py-0.5 text-sm">{count}</span>
                )}
              </Link>
            </div>
          </div>
        </div>
      )}
    </>
  )
}
