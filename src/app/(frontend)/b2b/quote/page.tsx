import { B2bQuoteForm } from '@/components/shop/B2bQuoteForm'
import Link from 'next/link'
import { canonicalUrl } from '@/lib/site-url'

export const metadata = {
  title: 'درخواست قیمت عمده',
  description: 'فرم درخواست قیمت عمده و سازمانی — ظروف یکبار مصرف گیاهی موثقی',
  alternates: { canonical: canonicalUrl('/b2b/quote') },
}

export default function B2bQuotePage() {
  return (
    <main className="mx-auto max-w-2xl px-4 py-10">
      <nav className="mb-4 text-sm text-brand-muted">
        <Link href="/b2b" className="hover:text-brand-green">
          عمده‌فروشی
        </Link>
        <span className="mx-2">/</span>
        <span>درخواست قیمت</span>
      </nav>
      <h1 className="text-2xl font-bold text-brand-ink">درخواست قیمت عمده</h1>
      <p className="mt-2 text-brand-muted">
        فرم زیر را تکمیل کنید — کارشناس فروش با شما تماس می‌گیرد.
      </p>
      <B2bQuoteForm />
    </main>
  )
}
