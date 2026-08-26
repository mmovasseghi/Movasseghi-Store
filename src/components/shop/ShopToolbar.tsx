'use client'

import type { ShopFilterOptions } from '@/lib/shop-filters'
import { useRouter, useSearchParams } from 'next/navigation'
import { useCallback } from 'react'

const SORT_OPTIONS = [
  { value: 'name', label: 'نام (الفبا)' },
  { value: 'price-asc', label: 'قیمت: کم به زیاد' },
  { value: 'price-desc', label: 'قیمت: زیاد به کم' },
] as const

type Props = {
  filterOptions?: ShopFilterOptions
  basePath?: string
}

export function ShopToolbar({ filterOptions, basePath = '/shop' }: Props) {
  const router = useRouter()
  const params = useSearchParams()
  const sort = params.get('sort') ?? 'name'
  const saleOnly = params.get('sale') === '1'
  const q = params.get('q') ?? ''
  const material = params.get('material') ?? ''
  const pack = params.get('pack') ?? ''

  const update = useCallback(
    (next: { sort?: string; sale?: boolean; material?: string; pack?: string }) => {
      const sp = new URLSearchParams()
      if (q) sp.set('q', q)
      sp.set('sort', next.sort ?? sort)
      if (next.sale ?? saleOnly) sp.set('sale', '1')
      const mat = next.material !== undefined ? next.material : material
      const pk = next.pack !== undefined ? next.pack : pack
      if (mat) sp.set('material', mat)
      if (pk) sp.set('pack', pk)
      router.push(`${basePath}?${sp.toString()}`)
    },
    [q, router, saleOnly, sort, material, pack, basePath],
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

      {filterOptions && filterOptions.materials.length > 0 && (
        <label className="flex items-center gap-2 text-sm text-brand-muted">
          <span>جنس</span>
          <select
            value={material}
            onChange={(e) => update({ material: e.target.value })}
            className="h-10 max-w-[10rem] rounded-lg border border-border bg-white px-2 text-sm text-brand-ink outline-none focus:ring-2 focus:ring-brand-green/30"
            aria-label="فیلتر جنس"
          >
            <option value="">همه</option>
            {filterOptions.materials.map((m) => (
              <option key={m} value={m}>
                {m}
              </option>
            ))}
          </select>
        </label>
      )}

      {filterOptions && filterOptions.packSizes.length > 0 && (
        <label className="flex items-center gap-2 text-sm text-brand-muted">
          <span>بسته</span>
          <select
            value={pack}
            onChange={(e) => update({ pack: e.target.value })}
            className="h-10 max-w-[10rem] rounded-lg border border-border bg-white px-2 text-sm text-brand-ink outline-none focus:ring-2 focus:ring-brand-green/30"
            aria-label="فیلتر بسته‌بندی"
          >
            <option value="">همه</option>
            {filterOptions.packSizes.map((p) => (
              <option key={p} value={p}>
                {p}
              </option>
            ))}
          </select>
        </label>
      )}

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
