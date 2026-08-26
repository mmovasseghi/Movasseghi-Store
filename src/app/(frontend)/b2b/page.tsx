import Link from 'next/link'
import { canonicalUrl } from '@/lib/site-url'

export const metadata = {
  title: 'عمده‌فروشی',
  description: 'سفارش عمده ظروف یکبار مصرف گیاهی — فروشگاه موثقی',
  alternates: { canonical: canonicalUrl('/b2b') },
}

export default function B2BPage() {
  return (
    <main className="mx-auto max-w-3xl px-4 py-12">
      <p className="text-sm font-medium text-brand-green">B2B · عمده‌فروشی</p>
      <h1 className="mt-2 text-2xl font-bold text-brand-ink md:text-3xl">سفارش عمده و سازمانی</h1>
      <p className="mt-4 leading-relaxed text-brand-muted">
        برای رستوران‌ها، catering، هتل‌ها و توزیع‌کنندگان — قیمت عمده، حداقل سفارش (MOQ) و
        ارسال pallet. تیم فروش موثقی آماده هماهنگی است.
      </p>

      <div className="mt-8 space-y-4 rounded-xl border border-border bg-white p-6">
        <h2 className="font-semibold text-brand-ink">راه‌های تماس</h2>
        <ul className="space-y-3 text-brand-ink">
          <li className="flex items-center gap-3">
            <span className="flex h-10 w-10 items-center justify-center rounded-full bg-brand-aqua-pale text-lg">
              📞
            </span>
            <span>
              تلفن:{' '}
              <a href="tel:09125199105" className="font-semibold text-brand-green hover:underline">
                ۰۹۱۲۵۱۹۹۱۰۵
              </a>
            </span>
          </li>
          <li className="flex items-center gap-3">
            <span className="flex h-10 w-10 items-center justify-center rounded-full bg-brand-aqua-pale text-lg">
              📷
            </span>
            <span>
              اینستاگرام:{' '}
              <a
                href="https://instagram.com/movasseghiStore"
                target="_blank"
                rel="noopener noreferrer"
                className="font-semibold text-brand-green hover:underline"
              >
                @movasseghiStore
              </a>
            </span>
          </li>
        </ul>
      </div>

      <div className="mt-6 rounded-xl border border-brand-aqua bg-brand-aqua-pale/30 p-6">
        <h2 className="font-semibold text-brand-ink">چه اطلاعاتی آماده کنید؟</h2>
        <ul className="mt-3 list-inside list-disc space-y-1 text-sm text-brand-muted">
          <li>نام کسب‌وکار و شهر</li>
          <li>لیست محصولات و تعداد تقریبی</li>
          <li>نوع بسته‌بندی (فله / شیرینک)</li>
          <li>روش تحویل (ارسال فروشگاه / خودروی مشتری)</li>
        </ul>
      </div>

      <div className="mt-8 flex flex-wrap gap-3">
        <Link
          href="/b2b/quote"
          className="rounded-xl bg-brand-green px-6 py-3 font-semibold text-white hover:bg-brand-green-light"
        >
          فرم درخواست عمده
        </Link>
        <a
          href="tel:09125199105"
          className="rounded-xl border-2 border-brand-green px-6 py-3 font-semibold text-brand-green hover:bg-brand-aqua-pale"
        >
          تماس فوری
        </a>
        <Link
          href="/shop"
          className="rounded-xl border border-brand-green px-6 py-3 font-semibold text-brand-green hover:bg-brand-aqua-pale"
        >
          مشاهده کاتالوگ
        </Link>
      </div>
    </main>
  )
}
