/**
 * Improve media alt text from legacy attachment titles / product names.
 * Usage: npm run sync:media-alt
 */
import 'dotenv/config'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import config from '@payload-config'
import { getPayload } from 'payload'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const MAP_PATH = path.resolve(__dirname, '../../docs/audit/generated/product-media-map.json')

type MediaEntry = {
  legacyAttachmentId?: number
  alt?: string
  title?: string
}

type ProductRow = {
  name?: string
  primaryImage?: MediaEntry
  gallery?: MediaEntry[]
}

async function main() {
  if (!fs.existsSync(MAP_PATH)) {
    console.error('Missing product-media-map.json')
    process.exit(1)
  }

  const products = JSON.parse(fs.readFileSync(MAP_PATH, 'utf-8')) as ProductRow[]
  const altByLegacyId = new Map<number, string>()

  for (const product of products) {
    const name = product.name?.trim()
    if (!name) continue
    const featured = product.primaryImage
    if (featured?.legacyAttachmentId) {
      const alt = featured.alt?.trim() || featured.title?.trim() || name
      altByLegacyId.set(featured.legacyAttachmentId, alt)
    }
    for (const g of product.gallery ?? []) {
      if (!g.legacyAttachmentId) continue
      const alt = g.alt?.trim() || g.title?.trim() || `${name} — گالری`
      altByLegacyId.set(g.legacyAttachmentId, alt)
    }
  }

  const payload = await getPayload({ config })
  const { docs } = await payload.find({ collection: 'media', limit: 500 })
  let updated = 0

  for (const media of docs) {
    const legacyId = media.legacyAttachmentId
    if (!legacyId) continue
    const nextAlt = altByLegacyId.get(legacyId)
    if (!nextAlt || nextAlt === media.alt) continue
    if (media.alt && media.alt !== 'تصویر محصول' && media.alt.length > 10 && media.alt === nextAlt)
      continue
    await payload.update({
      collection: 'media',
      id: media.id,
      data: { alt: nextAlt.slice(0, 500) },
    })
    updated++
  }

  console.log(`Media alt sync: ${updated} updated`)
  process.exit(0)
}

main().catch((e) => {
  console.error(e)
  process.exit(1)
})
