/**
 * Seed / sync legacy static pages into Payload.
 * Usage: npm run sync:pages
 */
import 'dotenv/config'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import config from '@payload-config'
import { getPayload } from 'payload'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const DATA_PATH = path.resolve(__dirname, 'pages-data.json')

type PageRow = {
  legacyId: number
  title: string
  slug: string
  legacyContentHtml?: string
  status: 'published' | 'draft'
  seo?: { title?: string; description?: string }
}

async function main() {
  if (!fs.existsSync(DATA_PATH)) {
    console.error('Missing pages-data.json')
    process.exit(1)
  }

  const data = JSON.parse(fs.readFileSync(DATA_PATH, 'utf-8')) as { pages: PageRow[] }
  const payload = await getPayload({ config })

  let created = 0
  let updated = 0

  for (const page of data.pages) {
    const existing = await payload.find({
      collection: 'pages',
      where: { slug: { equals: page.slug } },
      limit: 1,
    })

    const doc = existing.docs[0]
    const payloadData = {
      title: page.title,
      slug: page.slug,
      status: page.status,
      legacyId: page.legacyId,
      ...(page.slug !== 'pricing' && page.legacyContentHtml
        ? { legacyContentHtml: page.legacyContentHtml }
        : {}),
      ...(page.seo ? { seo: page.seo } : {}),
    }

    if (doc) {
      await payload.update({ collection: 'pages', id: doc.id, data: payloadData })
      updated++
    } else {
      await payload.create({ collection: 'pages', data: payloadData })
      created++
    }
  }

  console.log(`Pages sync: ${created} created, ${updated} updated`)
  process.exit(0)
}

main().catch((e) => {
  console.error(e)
  process.exit(1)
})
