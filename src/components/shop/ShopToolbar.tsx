'use client'

import { useRouter, useSearchParams } from 'next/navigation'
import { useCallback } from 'react'

const SORT_OPTIONS = [
  { value: 'name', label: 'نام (الفبا)' },
  { value: 'price-asc', label: 'قیمت: کم به زیاد' },
  { value: 'price-desc', label: 'قیمت: زیاد به کم' },
] as const

export function ShopToolbar() {
  const router = useRouter()
  const params = useSearchParams()
  const sort = params.get('sort') ?? 'name'
  const saleOnly = params.get('sale') === '1'
  const q = params.get('q') ?? ''

  const update = useCallback(
    (next: { sort?: string; sale?: boolean }) => {
      const sp = new URLSearchParams()
      if (q) sp.set('q', q)
      sp.set('sort', next.sort ?? sort)
      if (next.sale ?? saleOnly) sp.set('sale', '1')
      router.push(`/shop?${sp.toString()}`)
    },
    [q, router, saleOnly, sort],
  )

  return (
    <div className="mt-4 flex flex-wrap items-center gap-3">
      <label className="flex items-center gap-2 text-sm text-brand-muted">
        <span>مرتب‌سازی</span>
        <select
          value={sort}
          onChange={(e) => update({ sort: e.target.value })}
          className="h-10 rounded-lg border border-border bg-white px-2 text-sm text-brand-ink outline-none focus:ring-2 focus:ring-brand-green/30"
          aria-label="مرتب‌سازی محصولات"
        >
          {SORT_OPTIONS.map((o) => (
            <option key={o.value} value={o.value}>
              {o.label}
            </option>
          ))}
        </select>
      </label>
      <label className="flex min-h-10 cursor-pointer items-center gap-2 rounded-lg border border-border bg-white px-3 text-sm">
        <input
          type="checkbox"
          checked={saleOnly}
          onChange={(e) => update({ sale: e.target.checked })}
          className="h-4 w-4 accent-brand-green"
        />
        <span className="text-brand-ink">فقط تخفیف‌دار</span>
      </label>
    </div>
  )
}
