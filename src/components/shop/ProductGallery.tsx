'use client'

import Image from 'next/image'
import { useCallback, useEffect, useState } from 'react'
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
  const [lightbox, setLightbox] = useState(false)
  const [touchStart, setTouchStart] = useState<number | null>(null)

  const go = useCallback(
    (dir: 1 | -1) => {
      setActive((i) => (i + dir + images.length) % images.length)
    },
    [images.length],
  )

  useEffect(() => {
    if (!lightbox) return
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setLightbox(false)
      if (e.key === 'ArrowLeft') go(1)
      if (e.key === 'ArrowRight') go(-1)
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [lightbox, go])

  if (!images.length) return null

  const current = images[active] ?? images[0]

  return (
    <>
      <div className="space-y-3">
        <button
          type="button"
          onClick={() => setLightbox(true)}
          className="group relative aspect-square w-full overflow-hidden rounded-xl border border-border bg-white"
          aria-label="بزرگ‌نمایی تصویر"
        >
          <Image
            src={current.url}
            alt={current.alt || name}
            fill
            className="object-contain p-2 transition-transform duration-300 group-hover:scale-[1.02] motion-reduce:transition-none"
            sizes="(max-width:768px) 100vw, 50vw"
            priority
          />
          {images.length > 1 && (
            <span className="absolute bottom-3 start-3 rounded-full bg-brand-ink/75 px-2.5 py-0.5 text-xs text-white">
              {active + 1} / {images.length}
            </span>
          )}
          <span className="absolute bottom-3 end-3 rounded-full bg-white/90 px-2 py-1 text-xs text-brand-muted shadow-sm">
            🔍 بزرگ‌نمایی
          </span>
        </button>

        {images.length > 1 && (
          <div
            className="flex gap-2 overflow-x-auto pb-1"
            onTouchStart={(e) => setTouchStart(e.touches[0]?.clientX ?? null)}
            onTouchEnd={(e) => {
              if (touchStart === null) return
              const diff = touchStart - (e.changedTouches[0]?.clientX ?? touchStart)
              if (Math.abs(diff) > 40) go(diff > 0 ? 1 : -1)
              setTouchStart(null)
            }}
          >
            {images.map((img, i) => (
              <button
                key={img.url}
                type="button"
                onClick={() => setActive(i)}
                className={cn(
                  'relative h-[4.5rem] w-[4.5rem] shrink-0 overflow-hidden rounded-lg border-2 transition',
                  i === active
                    ? 'border-brand-green ring-2 ring-brand-aqua'
                    : 'border-border opacity-85',
                )}
              >
                <Image
                  src={img.url}
                  alt={img.alt || name}
                  fill
                  className="object-cover"
                  sizes="72px"
                />
              </button>
            ))}
          </div>
        )}
      </div>

      {lightbox && (
        <div
          className="fixed inset-0 z-[100] flex items-center justify-center bg-black/90 p-4"
          role="dialog"
          aria-modal
          aria-label="گالری تصویر"
          onClick={() => setLightbox(false)}
        >
          <button
            type="button"
            className="absolute top-4 end-4 flex h-11 w-11 items-center justify-center rounded-full bg-white/10 text-2xl text-white hover:bg-white/20"
            onClick={() => setLightbox(false)}
            aria-label="بستن"
          >
            ×
          </button>
          {images.length > 1 && (
            <>
              <button
                type="button"
                className="absolute start-2 top-1/2 flex h-11 w-11 -translate-y-1/2 items-center justify-center rounded-full bg-white/10 text-white hover:bg-white/20 md:start-6"
                onClick={(e) => {
                  e.stopPropagation()
                  go(-1)
                }}
                aria-label="تصویر قبلی"
              >
                ‹
              </button>
              <button
                type="button"
                className="absolute end-2 top-1/2 flex h-11 w-11 -translate-y-1/2 items-center justify-center rounded-full bg-white/10 text-white hover:bg-white/20 md:end-6"
                onClick={(e) => {
                  e.stopPropagation()
                  go(1)
                }}
                aria-label="تصویر بعدی"
              >
                ›
              </button>
            </>
          )}
          <div
            className="relative h-[min(85vh,720px)] w-full max-w-3xl"
            onClick={(e) => e.stopPropagation()}
          >
            <Image
              src={current.url}
              alt={current.alt || name}
              fill
              className="object-contain"
              sizes="100vw"
            />
          </div>
        </div>
      )}
    </>
  )
}
