'use client'

import { useRouter, useSearchParams } from 'next/navigation'
import { useCallback, useState } from 'react'

export function ShopSearch() {
  const router = useRouter()
  const params = useSearchParams()
  const [q, setQ] = useState(params.get('q') ?? '')

  const submit = useCallback(
    (e: React.FormEvent) => {
      e.preventDefault()
      const trimmed = q.trim()
      if (trimmed) {
        router.push(`/shop?q=${encodeURIComponent(trimmed)}`)
      } else {
        router.push('/shop')
      }
    },
    [q, router],
  )

  return (
    <form onSubmit={submit} className="flex w-full max-w-md gap-2">
      <input
        type="search"
        name="q"
        value={q}
        onChange={(e) => setQ(e.target.value)}
        placeholder="جستجوی محصول…"
        className="h-11 min-w-0 flex-1 rounded-lg border border-border bg-white px-3 text-sm outline-none focus:ring-2 focus:ring-brand-green/30"
        aria-label="جستجوی محصول"
      />
      <button
        type="submit"
        className="h-11 shrink-0 rounded-lg bg-brand-green px-4 text-sm font-semibold text-white hover:bg-brand-green-light"
      >
        جستجو
      </button>
    </form>
  )
}
