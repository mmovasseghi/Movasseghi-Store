import Link from 'next/link'
import { notFound } from 'next/navigation'
import { LegacyProductContent } from '@/components/shop/LegacyProductContent'
import { getPayloadClient } from '@/lib/payload'

export const dynamic = 'force-dynamic'

type Props = {
  params: Promise<{ slug: string }>
}

export async function generateMetadata({ params }: Props) {
  const { slug } = await params
  try {
    const payload = await getPayloadClient()
    const { docs } = await payload.find({
      collection: 'posts',
      where: { and: [{ slug: { equals: slug } }, { status: { equals: 'published' } }] },
      limit: 1,
    })
    const post = docs[0]
    if (!post) return { title: 'مقاله' }
    return {
      title: post.seo?.title ?? post.title,
      description: post.seo?.description ?? post.excerpt?.replace(/<[^>]+>/g, '').slice(0, 160),
    }
  } catch {
    return { title: 'مقاله' }
  }
}

export default async function MagPostPage({ params }: Props) {
  const { slug } = await params

  try {
    const payload = await getPayloadClient()
    const { docs } = await payload.find({
      collection: 'posts',
      where: { and: [{ slug: { equals: slug } }, { status: { equals: 'published' } }] },
      limit: 1,
    })
    const post = docs[0]
    if (!post) notFound()

    const html = post.legacyContentHtml?.trim()

    return (
      <main className="mx-auto max-w-4xl px-4 py-10">
        <nav className="mb-4 text-sm text-brand-muted">
          <Link href="/mag" className="hover:text-brand-green">
            مجله
          </Link>
          <span className="mx-2">/</span>
          <span>{post.title}</span>
        </nav>
        <h1 className="text-2xl font-bold text-brand-ink md:text-3xl">{post.title}</h1>
        {html ? (
          <div className="mt-8">
            <LegacyProductContent html={html} />
          </div>
        ) : (
          <p className="mt-6 text-brand-muted">محتوای مقاله در دسترس نیست.</p>
        )}
      </main>
    )
  } catch {
    notFound()
  }
}
