import Link from 'next/link'

export default function NotFound() {
  return (
    <main className="mx-auto flex min-h-[60vh] max-w-lg flex-col items-center justify-center px-4 py-16 text-center">
      <p className="text-6xl font-extrabold text-brand-green">۴۰۴</p>
      <h1 className="mt-4 text-2xl font-bold text-brand-ink">صفحه پیدا نشد</h1>
      <p className="mt-3 text-brand-muted">
        آدرس واردشده وجود ندارد یا منتقل شده است.
      </p>
      <div className="mt-8 flex flex-wrap justify-center gap-3">
        <Link
          href="/"
          className="rounded-xl bg-brand-green px-6 py-3 font-semibold text-white hover:bg-brand-green-light"
        >
          صفحه اصلی
        </Link>
        <Link
          href="/shop"
          className="rounded-xl border border-brand-green px-6 py-3 font-semibold text-brand-green hover:bg-brand-aqua-pale"
        >
          فروشگاه
        </Link>
        <a
          href="tel:09125199105"
          className="rounded-xl border border-border px-6 py-3 font-semibold text-brand-ink hover:border-brand-green"
        >
          تماس فروش
        </a>
      </div>
    </main>
  )
}
