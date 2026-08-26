/**
 * Seed Payload from scripts/import/migration-data.json
 * Usage: npm run seed:legacy
 */
import 'dotenv/config'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { getPayload } from 'payload'
import config from '@payload-config'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const DATA_PATH = path.resolve(__dirname, 'migration-data.json')

type MigrationCategory = {
  legacyTermId: number
  name: string
  slug: string
  description?: string
  parentLegacyTermId?: number
  productCount?: number
}

type MigrationProduct = {
  legacyId: number
  name: string
  slug: string
  shortDescription?: string
  descriptionHtml?: string
  regularPrice: number
  salePrice?: number
  sku?: string
  stockQuantity?: number
  manageStock?: boolean
  legacyCategoryIds?: number[]
  seo?: { title?: string; description?: string; focusKeyword?: string }
  status: string
}

type MigrationBundle = {
  categories: MigrationCategory[]
  products: MigrationProduct[]
}

async function main() {
  if (!fs.existsSync(DATA_PATH)) {
    console.error(`Missing ${DATA_PATH} — run: python scripts/legacy/export-migration-bundle.py`)
    process.exit(1)
  }

  const data: MigrationBundle = JSON.parse(fs.readFileSync(DATA_PATH, 'utf-8'))
  const payload = await getPayload({ config })

  const catIdByLegacy = new Map<number, string | number>()

  // Pass 1: categories without parent
  for (const cat of data.categories.filter((c) => !c.parentLegacyTermId)) {
    const doc = await payload.create({
      collection: 'categories',
      data: {
        name: cat.name,
        slug: cat.slug,
        description: cat.description?.replace(/rn/g, '\n'),
        sortOrder: 0,
        productCount: cat.productCount ?? 0,
        seo: { focusKeyword: cat.name },
      },
    })
    catIdByLegacy.set(cat.legacyTermId, doc.id)
  }

  // Pass 2: child categories
  let remaining = data.categories.filter((c) => c.parentLegacyTermId)
  while (remaining.length) {
    const next: MigrationCategory[] = []
    for (const cat of remaining) {
      const parentId = catIdByLegacy.get(cat.parentLegacyTermId ?? 0)
      if (!parentId) {
        next.push(cat)
        continue
      }
      const doc = await payload.create({
        collection: 'categories',
        data: {
          name: cat.name,
          slug: cat.slug,
          description: cat.description?.replace(/rn/g, '\n'),
          parent: parentId,
          sortOrder: 0,
          productCount: cat.productCount ?? 0,
        },
      })
      catIdByLegacy.set(cat.legacyTermId, doc.id)
    }
    if (next.length === remaining.length) break
    remaining = next
  }

  let created = 0
  for (const p of data.products) {
    const existing = await payload.find({
      collection: 'products',
      where: { legacyId: { equals: p.legacyId } },
      limit: 1,
    })
    if (existing.docs.length) continue

    const categoryIds = (p.legacyCategoryIds ?? [])
      .map((id) => catIdByLegacy.get(id))
      .filter(Boolean)

    await payload.create({
      collection: 'products',
      data: {
        name: p.name,
        slug: p.slug,
        status: 'published',
        sku: p.sku,
        regularPrice: p.regularPrice,
        salePrice: p.salePrice,
        stockQuantity: p.stockQuantity ?? 0,
        manageStock: p.manageStock ?? true,
        shortDescription: p.shortDescription,
        legacyDescriptionHtml: p.descriptionHtml,
        categories: categoryIds,
        legacyId: p.legacyId,
        attributes: { material: 'آملون' },
        seo: p.seo
          ? {
              title: p.seo.title || p.name,
              description: p.seo.description,
              focusKeyword: p.seo.focusKeyword,
            }
          : undefined,
      },
    })
    created++
  }

  console.log(`Seed complete: ${catIdByLegacy.size} categories, ${created} products created`)

  const users = await payload.find({ collection: 'users', limit: 1 })
  if (!users.docs.length && process.env.ADMIN_EMAIL && process.env.ADMIN_PASSWORD) {
    await payload.create({
      collection: 'users',
      data: {
        email: process.env.ADMIN_EMAIL,
        password: process.env.ADMIN_PASSWORD,
      },
    })
    console.log(`Admin user created: ${process.env.ADMIN_EMAIL}`)
  }

  process.exit(0)
}

main().catch((err) => {
  console.error(err)
  process.exit(1)
})
