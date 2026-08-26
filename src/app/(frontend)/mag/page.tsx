import Link from 'next/link'
import { getPayloadClient } from '@/lib/payload'
import { canonicalUrl } from '@/lib/site-url'

export const dynamic = 'force-dynamic'

export const metadata = {
  title: 'مجله',
  description: 'مقالات فروشگاه موثقی — ظروف یکبار مصرف گیاهی آملون',
  alternates: { canonical: canonicalUrl('/mag') },
}

export default async function MagArchivePage() {
  let posts: { slug: string; title: string; excerpt?: string | null; publishedAt?: string | null }[] = []

  try {
    const payload = await getPayloadClient()
    const { docs } = await payload.find({
      collection: 'posts',
      where: { status: { equals: 'published' } },
      sort: '-publishedAt',
      limit: 50,
    })
    posts = docs.map((p) => ({
      slug: p.slug,
      title: p.title,
      excerpt: p.excerpt,
      publishedAt: p.publishedAt,
    }))
  } catch {
    // empty
  }

  return (
    <main className="mx-auto max-w-3xl px-4 py-10">
      <h1 className="text-2xl font-bold text-brand-ink md:text-3xl">مجله موثقی</h1>
      <p className="mt-2 text-brand-muted">مقالات آموزشی درباره ظروف گیاهی و food service</p>

      <ul className="mt-8 space-y-6">
        {posts.map((post) => (
          <li key={post.slug} className="rounded-xl border border-border bg-white p-5">
            <Link href={`/mag/${post.slug}`} className="block">
              <h2 className="text-lg font-semibold text-brand-ink hover:text-brand-green">{post.title}</h2>
              {post.excerpt && (
                <p className="mt-2 line-clamp-2 text-sm text-brand-muted">{post.excerpt.replace(/<[^>]+>/g, '')}</p>
              )}
            </Link>
          </li>
        ))}
      </ul>

      {posts.length === 0 && (
        <p className="mt-8 text-center text-brand-muted">مقاله‌ای منتشر نشده است.</p>
      )}

      <p className="mt-10 text-sm">
        <Link href="/shop" className="text-brand-green hover:underline">
          بازگشت به فروشگاه
        </Link>
      </p>
    </main>
  )
}
