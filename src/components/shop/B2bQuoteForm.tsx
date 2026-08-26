'use client'

import Link from 'next/link'
import { useState } from 'react'
import { trackPhoneClick } from '@/lib/analytics'

export function B2bQuoteForm() {
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [quoteNumber, setQuoteNumber] = useState<string | null>(null)

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    setSubmitting(true)
    setError(null)
    const fd = new FormData(e.currentTarget)
    const payload = {
      companyName: String(fd.get('companyName') ?? ''),
      contactName: String(fd.get('contactName') ?? ''),
      contactPhone: String(fd.get('contactPhone') ?? ''),
      city: String(fd.get('city') ?? ''),
      productsNote: String(fd.get('productsNote') ?? ''),
      shippingPreference: String(fd.get('shippingPreference') ?? 'unknown') as
        | 'seller'
        | 'customer'
        | 'unknown',
      note: String(fd.get('note') ?? ''),
    }

    try {
      const res = await fetch('/api/quotes', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      })
      const data = (await res.json()) as { quoteNumber?: string; error?: string }
      if (!res.ok) {
        setError(data.error ?? 'ثبت ناموفق')
        setSubmitting(false)
        return
      }
      setQuoteNumber(data.quoteNumber ?? null)
      trackPhoneClick('b2b_quote_success')
    } catch {
      setError('خطای شبکه — دوباره تلاش کنید')
      setSubmitting(false)
    }
  }

  if (quoteNumber) {
    return (
      <div className="rounded-xl border border-brand-aqua bg-brand-aqua-pale/40 p-6 text-center">
        <p className="text-2xl">✓</p>
        <h2 className="mt-2 text-lg font-bold text-brand-ink">درخواست ثبت شد</h2>
        <p className="mt-2 text-sm text-brand-muted">کد پیگیری: {quoteNumber}</p>
        <p className="mt-4 text-sm text-brand-muted">کارشناس فروش به‌زودی با شما تماس می‌گیرد.</p>
        <div className="mt-6 flex flex-wrap justify-center gap-3">
          <a
            href="tel:09125199105"
            onClick={() => trackPhoneClick('b2b_quote')}
            className="rounded-xl bg-brand-green px-6 py-3 font-semibold text-white"
          >
            تماس فوری
          </a>
          <Link href="/shop" className="rounded-xl border border-brand-green px-6 py-3 font-semibold text-brand-green">
            فروشگاه
          </Link>
        </div>
      </div>
    )
  }

  return (
    <form onSubmit={handleSubmit} className="mt-8 space-y-4 rounded-xl border border-border bg-white p-6">
      {error && (
        <p className="rounded-lg bg-red-50 p-3 text-sm text-red-800" role="alert">
          {error}
        </p>
      )}
      <div className="grid gap-4 sm:grid-cols-2">
        <label className="block sm:col-span-2">
          <span className="mb-1 block text-sm text-brand-muted">نام کسب‌وکار *</span>
          <input name="companyName" required className="h-11 w-full rounded-lg border border-border px-3 outline-none focus:ring-2 focus:ring-brand-green/30" />
        </label>
        <label className="block">
          <span className="mb-1 block text-sm text-brand-muted">نام تماس *</span>
          <input name="contactName" required className="h-11 w-full rounded-lg border border-border px-3 outline-none focus:ring-2 focus:ring-brand-green/30" />
        </label>
        <label className="block">
          <span className="mb-1 block text-sm text-brand-muted">موبایل *</span>
          <input name="contactPhone" required type="tel" dir="ltr" placeholder="09xxxxxxxxx" className="h-11 w-full rounded-lg border border-border px-3 outline-none focus:ring-2 focus:ring-brand-green/30" />
        </label>
        <label className="block sm:col-span-2">
          <span className="mb-1 block text-sm text-brand-muted">شهر</span>
          <input name="city" className="h-11 w-full rounded-lg border border-border px-3 outline-none focus:ring-2 focus:ring-brand-green/30" />
        </label>
        <label className="block sm:col-span-2">
          <span className="mb-1 block text-sm text-brand-muted">محصولات و تعداد تقریبی *</span>
          <textarea name="productsNote" required rows={3} className="w-full rounded-lg border border-border px-3 py-2 outline-none focus:ring-2 focus:ring-brand-green/30" />
        </label>
        <label className="block sm:col-span-2">
          <span className="mb-1 block text-sm text-brand-muted">روش تحویل</span>
          <select name="shippingPreference" className="h-11 w-full rounded-lg border border-border px-3 outline-none focus:ring-2 focus:ring-brand-green/30">
            <option value="unknown">نامشخص</option>
            <option value="seller">ارسال توسط فروشگاه</option>
            <option value="customer">تحویل با خودروی مشتری</option>
          </select>
        </label>
        <label className="block sm:col-span-2">
          <span className="mb-1 block text-sm text-brand-muted">توضیحات</span>
          <textarea name="note" rows={2} className="w-full rounded-lg border border-border px-3 py-2 outline-none focus:ring-2 focus:ring-brand-green/30" />
        </label>
      </div>
      <button
        type="submit"
        disabled={submitting}
        className="w-full rounded-xl bg-brand-green py-3.5 font-semibold text-white hover:bg-brand-green-light disabled:opacity-60"
      >
        {submitting ? 'در حال ارسال…' : 'ثبت درخواست عمده'}
      </button>
    </form>
  )
}
