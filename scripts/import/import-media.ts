/**
 * Import legacy product images from media-master into Payload Media collection.
 * Links featuredImage + gallery on products by legacyId.
 *
 * Usage: npm run import:media
 */
import 'dotenv/config'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { getPayload } from 'payload'
import config from '@payload-config'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const MAP_PATH = path.resolve(__dirname, '../../docs/audit/generated/product-media-map.json')
const MASTER_DIR = path.resolve(__dirname, '../../media-master/legacy-uploads')

type LegacyAttachment = {
  legacyAttachmentId: number
  attachedFile: string
  alt?: string
  title?: string
  fileOnDisk?: boolean
}

type ProductMap = {
  legacyProductId: number
  slug: string
  primaryImage?: LegacyAttachment | null
  primaryImageId?: number | null
  gallery?: LegacyAttachment[]
}

const uploadedCache = new Map<number, string | number>()

function resolveMasterPath(att: LegacyAttachment): string | null {
  const rel = att.attachedFile?.replace(/\\/g, '/')
  if (!rel) return null
  const direct = path.join(MASTER_DIR, rel)
  if (fs.existsSync(direct)) return direct
  const base = path.basename(rel)
  const walk = (dir: string): string | null => {
    if (!fs.existsSync(dir)) return null
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
      const full = path.join(dir, entry.name)
      if (entry.isFile() && entry.name === base) return full
      if (entry.isDirectory()) {
        const found = walk(full)
        if (found) return found
      }
    }
    return null
  }
  return walk(MASTER_DIR)
}

async function uploadAttachment(
  payload: Awaited<ReturnType<typeof getPayload>>,
  att: LegacyAttachment,
): Promise<string | number | null> {
  if (uploadedCache.has(att.legacyAttachmentId)) {
    return uploadedCache.get(att.legacyAttachmentId)!
  }

  const filePath = resolveMasterPath(att)
  if (!filePath) return null

  const existing = await payload.find({
    collection: 'media',
    where: { legacyAttachmentId: { equals: att.legacyAttachmentId } },
    limit: 1,
  })
  if (existing.docs[0]) {
    uploadedCache.set(att.legacyAttachmentId, existing.docs[0].id)
    return existing.docs[0].id
  }

  const buffer = fs.readFileSync(filePath)
  const filename = path.basename(filePath)
  const doc = await payload.create({
    collection: 'media',
    data: {
      alt: att.alt?.trim() || att.title || `تصویر محصول`,
      legacyAttachmentId: att.legacyAttachmentId,
      legacyPath: att.attachedFile,
    },
    file: {
      data: buffer,
      mimetype: filename.endsWith('.webp')
        ? 'image/webp'
        : filename.endsWith('.png')
          ? 'image/png'
          : 'image/jpeg',
      name: filename,
      size: buffer.length,
    },
  })
  uploadedCache.set(att.legacyAttachmentId, doc.id)
  return doc.id
}

async function main() {
  if (!fs.existsSync(MAP_PATH)) {
    console.error('Missing product-media-map.json — run extract-media-inventory.py')
    process.exit(1)
  }

  const products: ProductMap[] = JSON.parse(fs.readFileSync(MAP_PATH, 'utf-8'))
  const payload = await getPayload({ config })

  let linked = 0
  let uploaded = 0
  let missing = 0

  for (const p of products) {
    const productRes = await payload.find({
      collection: 'products',
      where: { legacyId: { equals: p.legacyProductId } },
      limit: 1,
    })
    const product = productRes.docs[0]
    if (!product) continue

    let featuredId: string | number | undefined
    if (p.primaryImage && 'attachedFile' in p.primaryImage) {
      const id = await uploadAttachment(payload, p.primaryImage)
      if (id) {
        featuredId = id
        uploaded++
      } else missing++
    }

    const galleryItems: { image: string | number }[] = []
    for (const g of p.gallery ?? []) {
      if (!('attachedFile' in g)) continue
      const id = await uploadAttachment(payload, g)
      if (id) {
        galleryItems.push({ image: id })
        uploaded++
      } else missing++
    }

    if (featuredId || galleryItems.length) {
      await payload.update({
        collection: 'products',
        id: product.id,
        data: {
          ...(featuredId ? { featuredImage: featuredId } : {}),
          ...(galleryItems.length ? { gallery: galleryItems } : {}),
        },
      })
      linked++
    }
  }

  console.log(`Media import: ${linked} products linked, ${uploaded} files uploaded, ${missing} missing masters`)
  process.exit(0)
}

main().catch((e) => {
  console.error(e)
  process.exit(1)
})
