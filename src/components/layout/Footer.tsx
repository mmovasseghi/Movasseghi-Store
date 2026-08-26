import Link from 'next/link'

export function Footer() {
  return (
    <footer className="mt-auto border-t border-border bg-brand-off-white">
      <div className="mx-auto grid max-w-6xl gap-8 px-4 py-10 md:grid-cols-3">
        <div>
          <p className="font-bold text-brand-ink">فروشگاه موثقی</p>
          <p className="mt-2 text-sm text-brand-muted">
            ظروف یکبار مصرف گیاهی آملون — مناسب رستوران، کافه و فست‌فود
          </p>
        </div>
        <div>
          <p className="font-semibold text-brand-ink">تماس</p>
          <ul className="mt-2 space-y-1 text-sm text-brand-muted">
            <li>
              <a href="tel:09125199105" className="hover:text-brand-green">
                ۰۹۱۲۵۱۹۹۱۰۵
              </a>
            </li>
            <li>
              <a
                href="https://instagram.com/movasseghiStore"
                target="_blank"
                rel="noopener noreferrer"
                className="hover:text-brand-green"
              >
                @movasseghiStore
              </a>
            </li>
          </ul>
        </div>
        <div>
          <p className="font-semibold text-brand-ink">دسترسی سریع</p>
          <ul className="mt-2 space-y-1 text-sm text-brand-muted">
            <li>
              <Link href="/shop" className="hover:text-brand-green">
                فروشگاه
              </Link>
            </li>
            <li>
              <Link href="/b2b" className="hover:text-brand-green">
                سفارش عمده
              </Link>
            </li>
          </ul>
        </div>
      </div>
      <div className="border-t border-border py-4 text-center text-xs text-brand-muted">
        © {new Date().getFullYear()} فروشگاه موثقی
      </div>
    </footer>
  )
}
