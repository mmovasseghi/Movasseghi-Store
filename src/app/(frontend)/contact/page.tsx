import Link from 'next/link'

export const metadata = {
  title: 'تماس با ما',
  description: 'تماس با فروشگاه موثقی — سفارش تلفنی ظروف یکبار مصرف گیاهی آملون',
}

export default function ContactPage() {
  return (
    <main className="mx-auto max-w-4xl px-4 py-10">
      <h1 className="text-2xl font-bold text-brand-ink md:text-3xl">تماس با ما</h1>
      <p className="mt-3 text-brand-muted">
        برای سفارش، استعلام قیمت عمده یا پیگیری ارسال با ما در ارتباط باشید.
      </p>

      <div className="mt-8 grid gap-6 md:grid-cols-2">
        <section className="rounded-xl border border-border bg-white p-6">
          <h2 className="font-semibold text-brand-ink">تماس مستقیم</h2>
          <ul className="mt-4 space-y-3 text-sm">
            <li>
              <span className="text-brand-muted">تلفن سفارش: </span>
              <a href="tel:09125199105" className="font-medium text-brand-green hover:underline">
                ۰۹۱۲۵۱۹۹۱۰۵
              </a>
            </li>
            <li>
              <span className="text-brand-muted">اینستاگرام: </span>
              <a
                href="https://instagram.com/movasseghiStore"
                target="_blank"
                rel="noopener noreferrer"
                className="font-medium text-brand-green hover:underline"
              >
                @movasseghiStore
              </a>
            </li>
          </ul>
          <p className="mt-4 text-sm text-brand-muted">
            ساعات پاسخ‌گویی: شنبه تا پنج‌شنبه — تماس تلفنی برای ثبت سفارش سریع‌ترین روش است.
          </p>
        </section>

        <section className="rounded-xl border border-border bg-white p-6">
          <h2 className="font-semibold text-brand-ink">دسترسی سریع</h2>
          <ul className="mt-4 space-y-2 text-sm">
            <li>
              <Link href="/shop" className="text-brand-green hover:underline">
                فروشگاه آنلاین
              </Link>
            </li>
            <li>
              <Link href="/pricing" className="text-brand-green hover:underline">
                لیست قیمت محصولات
              </Link>
            </li>
            <li>
              <Link href="/b2b" className="text-brand-green hover:underline">
                سفارش عمده (B2B)
              </Link>
            </li>
          </ul>
        </section>
      </div>

      <section className="mt-8 overflow-hidden rounded-xl border border-border">
        <iframe
          title="موقعیت فروشگاه موثقی — تهران"
          src="https://maps.google.com/maps?q=%D8%AA%D9%87%D8%B1%D8%A7%D9%86&t=m&z=11&output=embed&iwloc=near"
          className="h-[280px] w-full border-0 md:h-[360px]"
          loading="lazy"
          referrerPolicy="no-referrer-when-downgrade"
        />
      </section>
    </main>
  )
}
