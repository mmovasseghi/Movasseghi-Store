import Link from 'next/link'
import { ProductCard } from '@/components/shop/ProductCard'
import { getPayloadClient } from '@/lib/payload'
import { productCardProps } from '@/lib/products'

export const dynamic = 'force-dynamic'

export default async function HomePage() {
  let featured: ReturnType<typeof productCardProps>[] = []

  try {
    const payload = await getPayloadClient()
    const { docs } = await payload.find({
      collection: 'products',
      where: { status: { equals: 'published' } },
      limit: 8,
      sort: '-updatedAt',
      depth: 1,
    })
    featured = docs.map(productCardProps)
  } catch {
    // DB may be unavailable during static export / first boot
  }

  return (
    <main>
      <section className="bg-gradient-to-b from-brand-aqua-pale to-brand-off-white px-4 py-16">
        <div className="mx-auto max-w-6xl text-center">
          <h1 className="text-3xl font-extrabold leading-tight text-brand-ink md:text-5xl">
            ظروف یکبار مصرف گیاهی آملون
          </h1>
          <p className="mx-auto mt-4 max-w-2xl text-brand-muted md:text-lg">
            تولید و توزیع ظروف پایدار برای رستوران، کافه، فست‌فود و catering — با کیفیت ثابت و ارسال
            سراسر ایران.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-3">
            <Link
              href="/shop"
              className="rounded-lg bg-brand-green px-6 py-3 font-medium text-white hover:bg-brand-green-light"
            >
              مشاهده محصولات
            </Link>
            <Link
              href="/b2b"
              className="rounded-lg border border-brand-green px-6 py-3 font-medium text-brand-green hover:bg-brand-aqua-pale"
            >
              سفارش عمده
            </Link>
          </div>
        </div>
      </section>

      <section className="mx-auto max-w-6xl px-4 py-12">
        <div className="mb-6 flex items-end justify-between">
          <h2 className="text-xl font-bold text-brand-ink">محصولات منتخب</h2>
          <Link href="/shop" className="text-sm text-brand-green hover:underline">
            همه محصولات
          </Link>
        </div>
        {featured.length > 0 ? (
          <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
            {featured.map((p) => (
              <ProductCard key={p.slug} {...p} />
            ))}
          </div>
        ) : (
          <p className="rounded-lg border border-dashed border-border bg-white p-8 text-center text-brand-muted">
            پس از راه‌اندازی دیتابیس و import محصولات، لیست اینجا نمایش داده می‌شود.
          </p>
        )}
      </section>

      <section className="border-t border-border bg-white px-4 py-12">
        <div className="mx-auto grid max-w-6xl gap-8 md:grid-cols-3">
          {[
            { title: 'جنس آملون', desc: 'ظروف گیاهی پایدار و مناسب سرو غذا' },
            { title: 'عمده و خرده', desc: 'فروش B2B با MOQ و قیمت عمده' },
            { title: 'ارسال ایران', desc: 'تحویل به سراسر کشور' },
          ].map((item) => (
            <div key={item.title} className="rounded-lg border border-border p-6">
              <h3 className="font-bold text-brand-ink">{item.title}</h3>
              <p className="mt-2 text-sm text-brand-muted">{item.desc}</p>
            </div>
          ))}
        </div>
      </section>
    </main>
  )
}
