'use client'

import { useRouter } from 'next/navigation'
import { useState } from 'react'

export function AccountLoginForm() {
  const router = useRouter()
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    setLoading(true)
    setError(null)
    const fd = new FormData(e.currentTarget)
    try {
      const res = await fetch('/api/account/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          phone: fd.get('phone'),
          orderNumber: fd.get('orderNumber'),
        }),
      })
      const data = (await res.json()) as { error?: string }
      if (!res.ok) {
        setError(data.error ?? 'ورود ناموفق')
        setLoading(false)
        return
      }
      router.push('/account')
      router.refresh()
    } catch {
      setError('خطای شبکه')
      setLoading(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4 rounded-xl border border-border bg-white p-6">
      <p className="text-sm text-brand-muted">
        با شماره موبایل و کد یکی از سفارش‌های خود وارد شوید. ثبت‌نام عمومی فعال نیست — برای حساب
        B2B با ما تماس بگیرید.
      </p>
      <label className="block">
        <span className="mb-1 block text-sm text-brand-muted">موبایل ثبت‌شده *</span>
        <input
          name="phone"
          required
          type="tel"
          dir="ltr"
          placeholder="09xxxxxxxxx"
          className="h-11 w-full rounded-lg border border-border px-3 outline-none focus:ring-2 focus:ring-brand-green/30"
        />
      </label>
      <label className="block">
        <span className="mb-1 block text-sm text-brand-muted">کد سفارش *</span>
        <input
          name="orderNumber"
          required
          dir="ltr"
          placeholder="ORD-..."
          className="h-11 w-full rounded-lg border border-border px-3 outline-none focus:ring-2 focus:ring-brand-green/30"
        />
      </label>
      {error && <p className="text-sm text-red-700">{error}</p>}
      <button
        type="submit"
        disabled={loading}
        className="w-full rounded-xl bg-brand-green py-3 font-semibold text-white disabled:opacity-60"
      >
        {loading ? 'در حال ورود…' : 'ورود به حساب'}
      </button>
    </form>
  )
}
