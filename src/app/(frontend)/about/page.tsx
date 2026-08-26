import Link from 'next/link'
import { LegacyProductContent } from '@/components/shop/LegacyProductContent'
import { getPayloadClient } from '@/lib/payload'
import { canonicalUrl } from '@/lib/site-url'

export const dynamic = 'force-dynamic'

export const metadata = {
  title: 'درباره ما',
  description: 'فروشگاه موثقی — تأمین‌کننده ظروف یکبار مصرف گیاهی آملون برای رستوران، کافه و فست‌فود',
  alternates: { canonical: canonicalUrl('/about') },
}

async function getAboutContent() {
  try {
    const payload = await getPayloadClient()
    const { docs } = await payload.find({
      collection: 'pages',
      where: { slug: { equals: 'about' } },
      limit: 1,
    })
    return docs[0]?.legacyContentHtml?.trim() ?? null
  } catch {
    return null
  }
}

export default async function AboutPage() {
  const legacyHtml = await getAboutContent()

  return (
    <main className="mx-auto max-w-4xl px-4 py-10">
      <h1 className="text-2xl font-bold text-brand-ink md:text-3xl">درباره فروشگاه موثقی</h1>

      <div className="mt-6 space-y-4 leading-relaxed text-brand-muted">
        <p>
          فروشگاه موثقی (ادامه‌دهنده مسیر آیریک پلاستیک ایرانیان) بزرگ‌ترین مرجع آنلاین ظروف یکبار
          مصرف گیاهی آملون در ایران است. ما تلاش می‌کنیم بدون نیاز به حضور فیزیکی در ترافیک شهری،
          امکان تهیه ظروف یکبار مصرف درجه‌یک را برای رستوران‌ها، کافه‌ها، کترینگ‌ها و مصرف‌کنندگان
          خانگی در سراسر کشور فراهم کنیم.
        </p>
        <p>
          محصولات ما از نشاسته ذرت تولید می‌شوند، سازگار با محیط‌زیست هستند و برای food service طراحی
          شده‌اند — لیوان، فنجان، کاسه، سطل، ظروف بسته‌بندی و بیشتر.
        </p>
      </div>

      <div className="mt-8 flex flex-wrap gap-3">
        <Link
          href="/shop"
          className="rounded-xl bg-brand-green px-5 py-2.5 text-sm font-semibold text-white hover:bg-brand-green-light"
        >
          مشاهده فروشگاه
        </Link>
        <Link
          href="/b2b"
          className="rounded-xl border border-border px-5 py-2.5 text-sm font-semibold text-brand-ink hover:border-brand-green"
        >
          سفارش عمده
        </Link>
        <Link
          href="/contact"
          className="rounded-xl border border-border px-5 py-2.5 text-sm font-semibold text-brand-ink hover:border-brand-green"
        >
          تماس با ما
        </Link>
      </div>

      {legacyHtml && (
        <section className="mt-12 border-t border-border pt-10">
          <h2 className="text-lg font-semibold text-brand-ink">محتوای آرشیو</h2>
          <div className="mt-4">
            <LegacyProductContent html={legacyHtml} />
          </div>
        </section>
      )}
    </main>
  )
}
