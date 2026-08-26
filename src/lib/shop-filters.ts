import type { ProductRecord } from '@/lib/products'
import { isOnSale } from '@/lib/products'
import { persianSearchMatch } from '@/lib/persian-search'

export type ShopFilterOptions = {
  materials: string[]
  packSizes: string[]
}

export type ShopFilterParams = {
  q?: string
  sale?: string
  material?: string
  pack?: string
}

export function extractFilterOptions(products: ProductRecord[]): ShopFilterOptions {
  const materials = new Set<string>()
  const packSizes = new Set<string>()
  for (const p of products) {
    const m = p.attributes?.material?.trim()
    const pack = p.attributes?.packSize?.trim()
    if (m) materials.add(m)
    if (pack) packSizes.add(pack)
  }
  return {
    materials: [...materials].sort((a, b) => a.localeCompare(b, 'fa')),
    packSizes: [...packSizes].sort((a, b) => a.localeCompare(b, 'fa')),
  }
}

export function matchesShopFilters(
  product: ProductRecord,
  material?: string,
  pack?: string,
): boolean {
  if (material) {
    const value = product.attributes?.material?.trim()
    if (!value || value !== material) return false
  }
  if (pack) {
    const value = product.attributes?.packSize?.trim()
    if (!value || value !== pack) return false
  }
  return true
}

export function hasActiveShopFilters(params: ShopFilterParams): boolean {
  return Boolean(params.q || params.sale === '1' || params.material || params.pack)
}

export function filterPublishedProducts(
  products: ProductRecord[],
  params: ShopFilterParams,
): ProductRecord[] {
  const query = params.q?.trim() ?? ''
  let filtered = products
  if (query) {
    filtered = filtered.filter(
      (p) =>
        persianSearchMatch(p.name, query) ||
        (p.sku ? persianSearchMatch(p.sku, query) : false),
    )
  }
  if (params.sale === '1') {
    filtered = filtered.filter((p) => isOnSale(p))
  }
  const material = params.material?.trim()
  const pack = params.pack?.trim()
  if (material || pack) {
    filtered = filtered.filter((p) => matchesShopFilters(p, material || undefined, pack || undefined))
  }
  return filtered
}
