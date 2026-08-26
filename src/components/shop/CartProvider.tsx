'use client'

import { createContext, useCallback, useContext, useEffect, useState } from 'react'
import {
  addToCart,
  type Cart,
  type CartItem,
  cartItemCount,
  emptyCart,
  removeFromCart,
  updateQuantity,
} from '@/commerce/cart'

const STORAGE_KEY = 'movasseghi-cart'

type CartContextValue = {
  cart: Cart
  count: number
  addItem: (item: Omit<CartItem, 'quantity'>, qty?: number) => void
  removeItem: (productId: string) => void
  setQuantity: (productId: string, quantity: number) => void
}

const CartContext = createContext<CartContextValue | null>(null)

function loadCart(): Cart {
  if (typeof window === 'undefined') return emptyCart()
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return emptyCart()
    return JSON.parse(raw) as Cart
  } catch {
    return emptyCart()
  }
}

export function CartProvider({ children }: { children: React.ReactNode }) {
  const [cart, setCart] = useState<Cart>(emptyCart)
  const [ready, setReady] = useState(false)

  useEffect(() => {
    setCart(loadCart())
    setReady(true)
  }, [])

  useEffect(() => {
    if (!ready) return
    localStorage.setItem(STORAGE_KEY, JSON.stringify(cart))
  }, [cart, ready])

  const addItem = useCallback((item: Omit<CartItem, 'quantity'>, qty = 1) => {
    setCart((c) => addToCart(c, item, qty))
  }, [])

  const removeItem = useCallback((productId: string) => {
    setCart((c) => removeFromCart(c, productId))
  }, [])

  const setQuantity = useCallback((productId: string, quantity: number) => {
    setCart((c) => updateQuantity(c, productId, quantity))
  }, [])

  return (
    <CartContext.Provider
      value={{ cart, count: cartItemCount(cart), addItem, removeItem, setQuantity }}
    >
      {children}
    </CartContext.Provider>
  )
}

export function useCart() {
  const ctx = useContext(CartContext)
  if (!ctx) throw new Error('useCart must be used within CartProvider')
  return ctx
}
