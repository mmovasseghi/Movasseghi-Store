'use client'

import { useRouter } from 'next/navigation'
import { useState } from 'react'

export function HeaderSearch() {
  const router = useRouter()
  const [q, setQ] = useState('')

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault()
        const trimmed = q.trim()
        router.push(trimmed ? `/shop?q=${encodeURIComponent(trimmed)}` : '/shop')
      }}
      className="hidden min-w-0 flex-1 max-w-xs lg:flex"
    >
      <input
        type="search"
        value={q}
        onChange={(e) => setQ(e.target.value)}
        placeholder="جستجوی محصول…"
        aria-label="جستجوی محصول"
        className="h-10 w-full rounded-lg border border-border bg-brand-off-white px-3 text-sm outline-none focus:border-brand-green focus:ring-2 focus:ring-brand-green/20"
      />
    </form>
  )
}
