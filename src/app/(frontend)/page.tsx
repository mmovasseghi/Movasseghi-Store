import Image from 'next/image'
import Link from 'next/link'
import { CategoryCard } from '@/components/shop/CategoryCard'
import { ProductCard } from '@/components/shop/ProductCard'
import { getPayloadClient } from '@/lib/payload'
import { productCardProps } from '@/lib/products'

export const dynamic = 'force-dynamic'

const TRUST = [
  { title: '۱۰۰٪ گیاهی', desc: 'ظروف آملون بر پایه نشاسته ذرت — دوستدار محیط زیست' },
  { title: 'عمده و خرده', desc: 'فروش B2B برای رستوران، کترینگ و سازمان‌ها' },
  { title: 'ارسال سراسری', desc: 'تحویل در تهران و ارسال به سراسر ایران' },
  { title: 'مشاوره خرید', desc: 'تماس مستقیم با تیم فروش برای انتخاب محصول' },
]

export default async function HomePage() {
  let featured: ReturnType<typeof productCardProps>[] = []
  let categories: { slug: string; name: string; productCount?: number | null }[] = []

  try {
    const payload = await getPayloadClient()
    const [productResult, categoryResult] = await Promise.all([
      payload.find({
        collection: 'products',
        where: { status: { equals: 'published' } },
        limit: 8,
        sort: '-updatedAt',
        depth: 1,
      }),
      payload.find({
        collection: 'categories',
        limit: 30,
        sort: 'sortOrder',
        depth: 0,
      }),
    ])
    featured = productResult.docs.map(productCardProps)
    categories = categoryResult.docs
      .filter((c) => !c.parent)
      .map((c) => ({
        slug: c.slug,
        name: c.name,
        productCount: c.productCount,
      }))
  } catch {
    // graceful empty
  }

  const mainCategory = categories.find((c) => c.slug.includes('آملون')) ?? categories[0]

  return (
    <main>
      {/* Hero — legacy picMain-1 + commercial SEO H1 */}
      <section className="relative overflow-hidden bg-brand-ink">
        <div className="absolute inset-0">
          <Image
            src="/brand/hero.png"
            alt="ظروف یکبار مصرف گیاهی آملون"
            fill
            className="object-cover opacity-40"
            priority
            sizes="100vw"
          />
          <div className="absolute inset-0 bg-gradient-to-l from-brand-ink/95 via-brand-ink/70 to-brand-ink/30" />
        </div>
        <div className="relative mx-auto max-w-6xl px-4 py-16 md:py-24">
          <p className="text-sm font-medium text-brand-aqua">فروشگاه موثقی</p>
          <h1 className="mt-3 max-w-2xl text-3xl font-extrabold leading-tight text-white md:text-5xl">
            ظروف یکبار مصرف گیاهی آملون
          </h1>
          <p className="mt-4 max-w-xl text-base leading-relaxed text-white/85 md:text-lg">
            تولید و توزیع ظروف پایدار برای رستوران، کافه، فست‌فود و catering — کیفیت ثابت، ارسال
            سراسر ایران.
          </p>
          <div className="mt-8 flex flex-wrap gap-3">
            <Link
              href={mainCategory ? `/shop/${mainCategory.slug}` : '/shop'}
              className="rounded-xl bg-brand-green px-6 py-3.5 text-base font-semibold text-white hover:bg-brand-green-light"
            >
              {mainCategory ? mainCategory.name : 'مشاهده محصولات'}
            </Link>
            <Link
              href="/b2b"
              className="rounded-xl border-2 border-white/30 px-6 py-3.5 text-base font-semibold text-white hover:bg-white/10"
            >
              سفارش عمده
            </Link>
            <a
              href="tel:09125199105"
              className="rounded-xl bg-white/10 px-6 py-3.5 text-base font-semibold text-white backdrop-blur hover:bg-white/20"
            >
              ۰۹۱۲۵۱۹۹۱۰۵
            </a>
          </div>
        </div>
      </section>

      {/* Categories */}
      {categories.length > 0 && (
        <section className="border-b border-border bg-white px-4 py-8 motion-reveal">
          <div className="mx-auto max-w-6xl">
            <h2 className="text-lg font-bold text-brand-ink">دسته‌بندی محصولات</h2>
            <div className="mt-4 flex gap-3 overflow-x-auto pb-2 [-ms-overflow-style:none] [scrollbar-width:none] [&::-webkit-scrollbar]:hidden">
              {categories.map((cat) => (
                <CategoryCard key={cat.slug} {...cat} />
              ))}
            </div>
          </div>
        </section>
      )}

      {/* Featured products — bordered grid DNA */}
      <section className="mx-auto max-w-6xl px-4 py-12">
        <div className="mb-6 flex items-end justify-between gap-4">
          <div>
            <h2 className="text-xl font-bold text-brand-ink md:text-2xl">محصولات منتخب</h2>
            <p className="mt-1 text-sm text-brand-muted">ظرف یکبار مصرف گیاهی — آماده سفارش</p>
          </div>
          <Link href="/shop" className="shrink-0 text-sm font-medium text-brand-green hover:underline">
            همه محصولات ←
          </Link>
        </div>
        {featured.length > 0 ? (
          <div className="grid grid-cols-2 divide-x divide-y divide-border overflow-hidden rounded-xl border border-border md:grid-cols-4">
            {featured.map((p) => (
              <ProductCard key={p.slug} {...p} variant="grid" />
            ))}
          </div>
        ) : (
          <p className="rounded-xl border border-dashed border-border bg-white p-8 text-center text-brand-muted">
            محصولات پس از import نمایش داده می‌شوند.
          </p>
        )}
      </section>

      {/* Trust */}
      <section className="border-t border-border bg-white px-4 py-12">
        <div className="mx-auto grid max-w-6xl gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {TRUST.map((item) => (
            <div
              key={item.title}
              className="rounded-xl border border-border bg-brand-off-white p-5 transition hover:border-brand-aqua"
            >
              <h3 className="font-bold text-brand-ink">{item.title}</h3>
              <p className="mt-2 text-sm leading-relaxed text-brand-muted">{item.desc}</p>
            </div>
          ))}
        </div>
      </section>

      {/* CTA band */}
      <section className="bg-brand-green px-4 py-12">
        <div className="mx-auto flex max-w-6xl flex-col items-center gap-6 text-center md:flex-row md:text-start">
          <div className="flex-1">
            <h2 className="text-2xl font-bold text-white">سفارش عمده یا فاکتور سازمانی؟</h2>
            <p className="mt-2 text-white/85">
              برای رستوران‌ها، catering و توزیع‌کنندگان — MOQ و قیمت عمده با هماهنگی مستقیم.
            </p>
          </div>
          <div className="flex shrink-0 flex-wrap justify-center gap-3">
            <Link
              href="/b2b"
              className="rounded-xl bg-white px-6 py-3 font-semibold text-brand-green hover:bg-brand-aqua-pale"
            >
              درخواست B2B
            </Link>
            <a
              href="tel:09125199105"
              className="rounded-xl border-2 border-white px-6 py-3 font-semibold text-white hover:bg-white/10"
            >
              تماس فروش
            </a>
          </div>
        </div>
      </section>
    </main>
  )
}
