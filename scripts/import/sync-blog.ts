/**
 * Sync sanitized blog posts into Payload.
 * Usage: npm run sync:blog
 */
import 'dotenv/config'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import config from '@payload-config'
import { getPayload } from 'payload'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const DATA_PATH = path.resolve(__dirname, 'blog-data.json')

type BlogRow = {
  legacyId: number
  title: string
  slug: string
  excerpt?: string | null
  legacyContentHtml?: string
  status: 'published' | 'draft'
  publishedAt?: string | null
}

async function main() {
  if (!fs.existsSync(DATA_PATH)) {
    console.error('Missing blog-data.json — run: python scripts/legacy/export-blog-posts.py')
    process.exit(1)
  }

  const data = JSON.parse(fs.readFileSync(DATA_PATH, 'utf-8')) as { posts: BlogRow[] }
  const payload = await getPayload({ config })

  let created = 0
  let updated = 0

  for (const post of data.posts) {
    const existing = await payload.find({
      collection: 'posts',
      where: { slug: { equals: post.slug } },
      limit: 1,
    })

    const doc = existing.docs[0]
    const payloadData = {
      title: post.title,
      slug: post.slug,
      status: post.status,
      legacyId: post.legacyId,
      excerpt: post.excerpt ?? undefined,
      legacyContentHtml: post.legacyContentHtml,
      publishedAt: post.publishedAt ?? undefined,
    }

    if (doc) {
      await payload.update({ collection: 'posts', id: doc.id, data: payloadData })
      updated++
    } else {
      await payload.create({ collection: 'posts', data: payloadData })
      created++
    }
  }

  console.log(`Blog sync: ${created} created, ${updated} updated`)
  process.exit(0)
}

main().catch((e) => {
  console.error(e)
  process.exit(1)
})
