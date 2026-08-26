'use client'

import Link from 'next/link'

export default function Error({
  reset,
}: {
  error: Error & { digest?: string }
  reset: () => void
}) {
  return (
    <main className="mx-auto flex min-h-[50vh] max-w-lg flex-col items-center justify-center px-4 py-16 text-center">
      <h1 className="text-2xl font-bold text-brand-ink">خطایی رخ داد</h1>
      <p className="mt-3 text-brand-muted">لطفاً دوباره تلاش کنید یا با پشتیبانی تماس بگیرید.</p>
      <div className="mt-8 flex flex-wrap justify-center gap-3">
        <button
          type="button"
          onClick={reset}
          className="rounded-xl bg-brand-green px-6 py-3 font-semibold text-white hover:bg-brand-green-light"
        >
          تلاش مجدد
        </button>
        <Link href="/" className="rounded-xl border border-brand-green px-6 py-3 font-semibold text-brand-green">
          صفحه اصلی
        </Link>
        <a href="tel:09125199105" className="rounded-xl border border-border px-6 py-3 font-semibold text-brand-ink">
          تماس
        </a>
      </div>
    </main>
  )
}
