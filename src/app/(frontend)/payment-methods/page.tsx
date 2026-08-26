import { LegalPage, legalMetadata } from '@/components/layout/LegalPage'
import Link from 'next/link'

export const metadata = legalMetadata(
  'روش‌های پرداخت',
  '/payment-methods',
  'پرداخت آنلاین، کارت به کارت و سفارش تلفنی — فروشگاه موثقی',
)

export default function PaymentMethodsPage() {
  return (
    <LegalPage title="روش‌های پرداخت" canonicalPath="/payment-methods">
      <p>فروشگاه موثقی روش‌های پرداخت زیر را پشتیبانی می‌کند:</p>
      <ul className="list-inside list-disc space-y-2">
        <li>
          <strong>سفارش تلفنی:</strong> ثبت درخواست و هماهنگی با کارشناس فروش
        </li>
        <li>
          <strong>کارت به کارت:</strong> پس از تأیید سفارش، اطلاعات حساب ارسال می‌شود
        </li>
        <li>
          <strong>پرداخت آنلاین:</strong> پس از اتصال درگاه رسمی (زرین‌پال / نکست‌پی)
        </li>
        <li>
          <strong>فاکتور سازمانی:</strong> برای مشتریان B2B با هماهنگی قبلی
        </li>
      </ul>
      <p className="text-brand-muted">
        تأیید نهایی پرداخت آنلاین فقط پس از verify سمت سرور انجام می‌شود.
      </p>
      <p>
        <Link href="/checkout" className="text-brand-green hover:underline">
          تسویه حساب
        </Link>
      </p>
    </LegalPage>
  )
}
