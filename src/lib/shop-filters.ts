import type { ProductRecord } from '@/lib/products'

export type ShopFilterOptions = {
  materials: string[]
  packSizes: string[]
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

export function hasActiveShopFilters(params: {
  q?: string
  sale?: string
  material?: string
  pack?: string
}): boolean {
  return Boolean(params.q || params.sale === '1' || params.material || params.pack)
}
