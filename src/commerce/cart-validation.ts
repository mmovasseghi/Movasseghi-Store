import type { CartItem } from '@/commerce/cart'
import { productDisplayPrice } from '@/lib/products'
import type { Product } from '@/payload-types'

export type ValidatedLine = {
  productId: string
  slug: string
  name: string
  quantity: number
  unitPrice: number
  lineTotal: number
  imageUrl?: string
  inStock: boolean
  availableQty: number
}

export type CartValidationResult =
  | {
      ok: true
      items: ValidatedLine[]
      subtotal: number
      warnings: string[]
    }
  | { ok: false; error: string; invalidProductIds?: string[] }

export function validateCartAgainstProducts(
  cartItems: Pick<CartItem, 'productId' | 'quantity'>[],
  products: Product[],
): CartValidationResult {
  if (cartItems.length === 0) {
    return { ok: false, error: 'سبد خرید خالی است' }
  }

  const byId = new Map(products.map((p) => [String(p.id), p]))
  const validated: ValidatedLine[] = []
  const warnings: string[] = []
  const invalid: string[] = []

  for (const line of cartItems) {
    const product = byId.get(line.productId)
    if (!product || product.status !== 'published') {
      invalid.push(line.productId)
      continue
    }

    const qty = Math.max(1, Math.floor(line.quantity))
    const unitPrice = productDisplayPrice(product)
    if (unitPrice <= 0) {
      warnings.push(`${product.name}: قیمت نامعتبر — تماس بگیرید`)
      continue
    }

    const stock = product.stockQuantity ?? 0
    const manageStock = product.manageStock !== false
    const inStock = !manageStock || stock > 0
    let finalQty = qty

    if (manageStock && stock > 0 && qty > stock) {
      finalQty = stock
      warnings.push(`${product.name}: حداکثر ${stock} عدد موجود است`)
    } else if (manageStock && stock <= 0) {
      warnings.push(`${product.name}: ناموجود`)
      continue
    }

    const imageUrl =
      typeof product.featuredImage === 'object' && product.featuredImage?.url
        ? product.featuredImage.url
        : undefined

    validated.push({
      productId: String(product.id),
      slug: product.slug,
      name: product.name,
      quantity: finalQty,
      unitPrice,
      lineTotal: unitPrice * finalQty,
      imageUrl: imageUrl ?? undefined,
      inStock,
      availableQty: manageStock ? stock : 9999,
    })
  }

  if (invalid.length > 0) {
    return {
      ok: false,
      error: 'برخی محصولات در سبد دیگر موجود نیستند',
      invalidProductIds: invalid,
    }
  }

  if (validated.length === 0) {
    return { ok: false, error: 'هیچ قلم معتبری در سبد نیست' }
  }

  const subtotal = validated.reduce((s, i) => s + i.lineTotal, 0)
  return { ok: true, items: validated, subtotal, warnings }
}

export function validatedToCartItems(lines: ValidatedLine[]): CartItem[] {
  return lines.map((l) => ({
    productId: l.productId,
    slug: l.slug,
    name: l.name,
    unitPrice: l.unitPrice,
    quantity: l.quantity,
    imageUrl: l.imageUrl,
  }))
}
