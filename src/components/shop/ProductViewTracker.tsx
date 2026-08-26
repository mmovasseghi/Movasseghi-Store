'use client'

import { useEffect } from 'react'
import { trackViewItem } from '@/lib/analytics'

type Props = {
  productId: string
  name: string
  price: number
}

export function ProductViewTracker({ productId, name, price }: Props) {
  useEffect(() => {
    trackViewItem({ productId, name, price })
  }, [productId, name, price])

  return null
}
