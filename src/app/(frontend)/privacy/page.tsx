import { LegalPage, legalMetadata } from '@/components/layout/LegalPage'

export const metadata = legalMetadata(
  'حریم خصوصی',
  '/privacy',
  'سیاست حریم خصوصی فروشگاه موثقی',
)

export default function PrivacyPage() {
  return (
    <LegalPage title="حریم خصوصی" canonicalPath="/privacy">
      <p>
        اطلاعات تماس (نام، شماره موبایل، توضیحات سفارش) فقط برای پردازش سفارش، پیگیری و
        هماهنگی فروش استفاده می‌شود.
      </p>
      <p>
        داده‌های سفارش در پایگاه داده امن سرور نگهداری می‌شوند. اطلاعات پرداخت آنلاین (پس از
        فعال‌سازی درگاه) مستقیماً توسط ارائه‌دهنده پرداخت پردازش می‌شود.
      </p>
      <p>
        ما اطلاعات شخصی شما را به اشخاص ثالث غیرمرتبط با انجام سفارش واگذار نمی‌کنیم، مگر با
        الزام قانونی.
      </p>
    </LegalPage>
  )
}
