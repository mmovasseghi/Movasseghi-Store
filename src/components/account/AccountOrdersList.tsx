'use client'

import { formatIrt } from '@/commerce/cart'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { useEffect, useState } from 'react'

type OrderSummary = {
  orderNumber: string
  status: string
  subtotal: number
  createdAt: string
}

const STATUS_LABEL: Record<string, string> = {
  pending: 'در انتظار',
  confirmed: 'تأیید شده',
  cancelled: 'لغو شده',
}

export function AccountOrdersList() {
  const router = useRouter()
  const [orders, setOrders] = useState<OrderSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetch('/api/account/orders')
      .then(async (res) => {
        if (res.status === 401) {
          router.replace('/account/login')
          return null
        }
        return res.json() as Promise<{ orders?: OrderSummary[]; error?: string }>
      })
      .then((data) => {
        if (!data) return
        if (!data.orders) {
          setError(data.error ?? 'خطا در بارگذاری')
          return
        }
        setOrders(data.orders)
      })
      .catch(() => setError('خطای شبکه'))
      .finally(() => setLoading(false))
  }, [router])

  const logout = async () => {
    await fetch('/api/account/logout', { method: 'POST' })
    router.push('/account/login')
    router.refresh()
  }

  if (loading) {
    return <p className="text-sm text-brand-muted">در حال بارگذاری سفارش‌ها…</p>
  }

  if (error) {
    return <p className="text-sm text-red-700">{error}</p>
  }

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-brand-muted">{orders.length} سفارش</p>
        <button
          type="button"
          onClick={logout}
          className="text-sm text-brand-muted underline hover:text-brand-green"
        >
          خروج
        </button>
      </div>
      {orders.length === 0 ? (
        <p className="mt-6 rounded-xl border border-dashed border-border bg-white p-8 text-center text-brand-muted">
          هنوز سفارشی ثبت نشده است.{' '}
          <Link href="/shop" className="text-brand-green hover:underline">
            مشاهده فروشگاه
          </Link>
        </p>
      ) : (
        <ul className="mt-4 divide-y divide-border overflow-hidden rounded-xl border border-border bg-white">
          {orders.map((o) => (
            <li key={o.orderNumber}>
              <Link
                href={`/account/orders/${encodeURIComponent(o.orderNumber)}`}
                className="flex flex-col gap-1 px-4 py-4 hover:bg-brand-aqua-pale/40 sm:flex-row sm:items-center sm:justify-between"
              >
                <div>
                  <p className="font-semibold text-brand-ink">{o.orderNumber}</p>
                  <p className="text-sm text-brand-muted">
                    {STATUS_LABEL[o.status] ?? o.status}
                  </p>
                </div>
                <div className="text-sm text-brand-muted sm:text-end">
                  <p>{formatIrt(o.subtotal)}</p>
                  <p>{new Date(o.createdAt).toLocaleDateString('fa-IR')}</p>
                </div>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
