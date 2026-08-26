'use client'

import Image from 'next/image'
import { useState } from 'react'
import { cn } from '@/lib/utils'

type GalleryItem = {
  url: string
  alt: string
}

type ProductGalleryProps = {
  name: string
  images: GalleryItem[]
}

export function ProductGallery({ name, images }: ProductGalleryProps) {
  const [active, setActive] = useState(0)
  if (!images.length) return null

  const current = images[active] ?? images[0]

  return (
    <div className="space-y-3">
      <div className="relative aspect-square overflow-hidden rounded-lg border border-border bg-brand-aqua-pale">
        <Image
          src={current.url}
          alt={current.alt || name}
          fill
          className="object-cover"
          sizes="(max-width:768px) 100vw, 50vw"
          priority
        />
      </div>
      {images.length > 1 && (
        <div className="flex gap-2 overflow-x-auto pb-1">
          {images.map((img, i) => (
            <button
              key={img.url}
              type="button"
              onClick={() => setActive(i)}
              className={cn(
                'relative h-16 w-16 shrink-0 overflow-hidden rounded-md border-2 transition',
                i === active ? 'border-brand-green' : 'border-border opacity-80',
              )}
            >
              <Image src={img.url} alt={img.alt || name} fill className="object-cover" sizes="64px" />
            </button>
          ))}
        </div>
      )}
    </div>
  )
}
