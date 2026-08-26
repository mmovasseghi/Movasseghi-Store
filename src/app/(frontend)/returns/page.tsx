import { LegalPage, legalMetadata } from '@/components/layout/LegalPage'
import Link from 'next/link'

export const metadata = legalMetadata(
  'مرجوعی و لغو',
  '/returns',
  'شرایط مرجوعی و لغو سفارش — فروشگاه موثقی',
)

export default function ReturnsPage() {
  return (
    <LegalPage title="مرجوعی و لغو سفارش" canonicalPath="/returns">
      <p>
        به‌دلیل ماهیت محصولات یکبار مصرف و بسته‌بندی بهداشتی، مرجوعی فقط در صورت نقص فنی یا
        مغایرت با سفارش ثبت‌شده و پس از بررسی تیم فروش امکان‌پذیر است.
      </p>
      <h2 className="text-lg font-semibold text-brand-ink">لغو سفارش</h2>
      <p>
        برای لغو سفارش قبل از ارسال، با شماره{' '}
        <a href="tel:09125199105" className="text-brand-green hover:underline">
          ۰۹۱۲۵۱۹۹۱۰۵
        </a>{' '}
        تماس بگیرید یا از{' '}
        <Link href="/track-order" className="text-brand-green hover:underline">
          پیگیری سفارش
        </Link>{' '}
        وضعیت را بررسی کنید.
      </p>
    </LegalPage>
  )
}
