/**
 * Sync legacy product HTML descriptions from migration-data.json into Payload.
 * Usage: npm run sync:content
 */
import 'dotenv/config'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import config from '@payload-config'
import { getPayload } from 'payload'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const DATA_PATH = path.resolve(__dirname, 'migration-data.json')

type MigrationProduct = {
  legacyId: number
  name: string
  descriptionHtml?: string
  shortDescription?: string
  seo?: { title?: string; description?: string; focusKeyword?: string }
}

async function main() {
  if (!fs.existsSync(DATA_PATH)) {
    console.error('Missing migration-data.json — run: npm run export:legacy')
    process.exit(1)
  }

  const data = JSON.parse(fs.readFileSync(DATA_PATH, 'utf-8')) as { products: MigrationProduct[] }
  const payload = await getPayload({ config })

  let updated = 0
  let skipped = 0

  for (const p of data.products) {
    const res = await payload.find({
      collection: 'products',
      where: { legacyId: { equals: p.legacyId } },
      limit: 1,
    })
    const product = res.docs[0]
    if (!product) {
      skipped++
      continue
    }

    const html = p.descriptionHtml?.trim()
    if (!html) continue

    await payload.update({
      collection: 'products',
      id: product.id,
      data: {
        legacyDescriptionHtml: html,
        ...(p.shortDescription ? { shortDescription: p.shortDescription } : {}),
        ...(p.seo
          ? {
              seo: {
                title: p.seo.title || product.name,
                description: p.seo.description,
                focusKeyword: p.seo.focusKeyword,
              },
            }
          : {}),
      },
    })
    updated++
  }

  console.log(`Content sync: ${updated} products updated, ${skipped} not found in Payload`)
  process.exit(0)
}

main().catch((e) => {
  console.error(e)
  process.exit(1)
})
