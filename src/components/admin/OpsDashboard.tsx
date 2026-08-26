'use client'

import Link from 'next/link'
import { useEffect, useState } from 'react'

type Summary = {
  pendingOrders: number
  newQuotes: number
  publishedProducts: number
}

export function OpsDashboard() {
  const [data, setData] = useState<Summary | null>(null)
  const [error, setError] = useState(false)

  useEffect(() => {
    fetch('/api/admin/ops-summary', { credentials: 'include' })
      .then((res) => (res.ok ? res.json() : Promise.reject()))
      .then((json: Summary) => setData(json))
      .catch(() => setError(true))
  }, [])

  if (error) return null
  if (!data) {
    return (
      <div style={{ marginBottom: '1.5rem', padding: '1rem', opacity: 0.7 }}>
        در حال بارگذاری خلاصه عملیات…
      </div>
    )
  }

  const cards = [
    { label: 'سفارش در انتظار', value: data.pendingOrders, href: '/admin/collections/orders' },
    { label: 'درخواست قیمت جدید', value: data.newQuotes, href: '/admin/collections/quotes' },
    { label: 'محصول منتشرشده', value: data.publishedProducts, href: '/admin/collections/products' },
  ]

  return (
    <div style={{ marginBottom: '2rem' }}>
      <h2 style={{ marginBottom: '0.75rem', fontSize: '1.125rem', fontWeight: 600 }}>
        خلاصه عملیات
      </h2>
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(140px, 1fr))',
          gap: '0.75rem',
        }}
      >
        {cards.map((c) => (
          <Link
            key={c.href}
            href={c.href}
            style={{
              display: 'block',
              padding: '1rem',
              borderRadius: '8px',
              border: '1px solid var(--theme-elevation-150)',
              textDecoration: 'none',
              color: 'inherit',
            }}
          >
            <div style={{ fontSize: '1.75rem', fontWeight: 700 }}>{c.value}</div>
            <div style={{ marginTop: '0.25rem', fontSize: '0.875rem', opacity: 0.8 }}>{c.label}</div>
          </Link>
        ))}
      </div>
    </div>
  )
}

export default OpsDashboard
