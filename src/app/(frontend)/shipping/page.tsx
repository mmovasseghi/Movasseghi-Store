import { LegalPage, legalMetadata } from '@/components/layout/LegalPage'
import Link from 'next/link'

export const metadata = legalMetadata(
  'روش ارسال',
  '/shipping',
  'شرایط ارسال ظروف یکبار مصرف گیاهی — فروشگاه موثقی',
)

export default function ShippingPage() {
  return (
    <LegalPage title="روش ارسال" canonicalPath="/shipping">
      <p>
        ارسال سفارش‌ها پس از هماهنگی تلفنی یا ثبت سفارش آنلاین انجام می‌شود. محدوده اصلی خدمات
        تهران است و ارسال به سراسر ایران با هماهنگی حمل‌ونقل امکان‌پذیر است.
      </p>
      <h2 className="text-lg font-semibold text-brand-ink">روش‌های تحویل</h2>
      <ul className="list-inside list-disc space-y-2">
        <li>
          <strong>ارسال توسط فروشگاه:</strong> هماهنگی زمان و هزینه حمل با تیم فروش
        </li>
        <li>
          <strong>تحویل با خودروی مشتری:</strong> دریافت از انبار پس از هماهنگی
        </li>
      </ul>
      <p>
        برای سفارش‌های عمده (B2B) زمان‌بندی pallet و بسته‌بندی فله یا شیرینک جداگانه هماهنگ
        می‌شود.
      </p>
      <p>
        <Link href="/contact" className="text-brand-green hover:underline">
          تماس با ما
        </Link>{' '}
        ·{' '}
        <Link href="/b2b/quote" className="text-brand-green hover:underline">
          درخواست عمده
        </Link>
      </p>
    </LegalPage>
  )
}
