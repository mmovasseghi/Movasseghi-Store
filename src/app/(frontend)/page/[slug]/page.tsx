import Link from 'next/link'
import { notFound } from 'next/navigation'
import { LegacyProductContent } from '@/components/shop/LegacyProductContent'
import { getPayloadClient } from '@/lib/payload'

export const dynamic = 'force-dynamic'

type Props = {
  params: Promise<{ slug: string }>
}

async function getPage(slug: string) {
  const payload = await getPayloadClient()
  const { docs } = await payload.find({
    collection: 'pages',
    where: {
      and: [{ slug: { equals: slug } }, { status: { equals: 'published' } }],
    },
    limit: 1,
  })
  return docs[0] ?? null
}

export async function generateMetadata({ params }: Props) {
  const { slug } = await params
  const page = await getPage(slug).catch(() => null)
  if (!page) return { title: 'صفحه' }
  return {
    title: page.seo?.title ?? page.title,
    description: page.seo?.description,
  }
}

export default async function CmsPage({ params }: Props) {
  const { slug } = await params
  const page = await getPage(slug).catch(() => null)
  if (!page) notFound()

  const html = page.legacyContentHtml?.trim()

  return (
    <main className="mx-auto max-w-4xl px-4 py-10">
      <h1 className="text-2xl font-bold text-brand-ink md:text-3xl">{page.title}</h1>
      {html ? (
        <div className="mt-8">
          <LegacyProductContent html={html} />
        </div>
      ) : (
        <p className="mt-6 text-brand-muted">محتوای این صفحه در حال آماده‌سازی است.</p>
      )}
      <p className="mt-10 text-sm">
        <Link href="/shop" className="text-brand-green hover:underline">
          بازگشت به فروشگاه
        </Link>
      </p>
    </main>
  )
}
