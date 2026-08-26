export const metadata = {
  title: 'عمده‌فروشی',
  description: 'سفارش عمده ظروف یکبار مصرف گیاهی — فروشگاه موثقی',
}

export default function B2BPage() {
  return (
    <main className="mx-auto max-w-3xl px-4 py-12">
      <h1 className="text-2xl font-bold text-brand-ink">سفارش عمده (B2B)</h1>
      <p className="mt-4 text-brand-muted leading-relaxed">
        برای رستوران‌ها، catering و توزیع‌کنندگان — قیمت عمده، MOQ و ارسال پallet. با ما تماس بگیرید
        یا از طریق اینستاگرام پیام دهید.
      </p>
      <ul className="mt-6 space-y-2 text-brand-ink">
        <li>
          تلفن:{' '}
          <a href="tel:09125199105" className="text-brand-green hover:underline">
            ۰۹۱۲۵۱۹۹۱۰۵
          </a>
        </li>
        <li>
          اینستاگرام:{' '}
          <a
            href="https://instagram.com/movasseghiStore"
            target="_blank"
            rel="noopener noreferrer"
            className="text-brand-green hover:underline"
          >
            @movasseghiStore
          </a>
        </li>
      </ul>
    </main>
  )
}
