export type CartItem = {
  productId: string
  slug: string
  name: string
  unitPrice: number
  quantity: number
  imageUrl?: string
}

export type Cart = {
  items: CartItem[]
  currency: 'IRT'
  updatedAt: string
}

export const emptyCart = (): Cart => ({
  items: [],
  currency: 'IRT',
  updatedAt: new Date().toISOString(),
})

export function cartSubtotal(cart: Cart): number {
  return cart.items.reduce((sum, item) => sum + item.unitPrice * item.quantity, 0)
}

export function formatIrt(amount: number): string {
  return new Intl.NumberFormat('fa-IR').format(amount) + ' تومان'
}

export function addToCart(cart: Cart, item: Omit<CartItem, 'quantity'>, qty = 1): Cart {
  const existing = cart.items.find((i) => i.productId === item.productId)
  const items = existing
    ? cart.items.map((i) =>
        i.productId === item.productId ? { ...i, quantity: i.quantity + qty } : i,
      )
    : [...cart.items, { ...item, quantity: qty }]

  return { ...cart, items, updatedAt: new Date().toISOString() }
}

export function removeFromCart(cart: Cart, productId: string): Cart {
  return {
    ...cart,
    items: cart.items.filter((i) => i.productId !== productId),
    updatedAt: new Date().toISOString(),
  }
}

export function updateQuantity(cart: Cart, productId: string, quantity: number): Cart {
  if (quantity <= 0) return removeFromCart(cart, productId)
  return {
    ...cart,
    items: cart.items.map((i) => (i.productId === productId ? { ...i, quantity } : i)),
    updatedAt: new Date().toISOString(),
  }
}

export function cartItemCount(cart: Cart): number {
  return cart.items.reduce((sum, i) => sum + i.quantity, 0)
}
