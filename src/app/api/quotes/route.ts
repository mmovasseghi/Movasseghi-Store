import { getPayloadClient } from '@/lib/payload'
import { isValidMobile, normalizePhone } from '@/lib/phone'
import { NextResponse } from 'next/server'

type QuoteBody = {
  companyName: string
  contactName: string
  contactPhone: string
  city?: string
  productsNote: string
  shippingPreference?: 'seller' | 'customer' | 'unknown'
  note?: string
}

function validate(body: unknown): body is QuoteBody {
  if (!body || typeof body !== 'object') return false
  const b = body as Record<string, unknown>
  return (
    typeof b.companyName === 'string' &&
    b.companyName.trim().length >= 2 &&
    typeof b.contactName === 'string' &&
    b.contactName.trim().length >= 2 &&
    typeof b.contactPhone === 'string' &&
    isValidMobile(b.contactPhone) &&
    typeof b.productsNote === 'string' &&
    b.productsNote.trim().length >= 3
  )
}

export async function POST(request: Request) {
  let body: unknown
  try {
    body = await request.json()
  } catch {
    return NextResponse.json({ error: 'Invalid JSON' }, { status: 400 })
  }

  if (!validate(body)) {
    return NextResponse.json({ error: 'Invalid quote data' }, { status: 400 })
  }

  const quoteNumber = `QT-${Date.now()}`

  try {
    const payload = await getPayloadClient()
    await payload.create({
      collection: 'quotes',
      data: {
        quoteNumber,
        companyName: body.companyName.trim(),
        contactName: body.contactName.trim(),
        contactPhone: normalizePhone(body.contactPhone),
        city: body.city?.trim() || undefined,
        productsNote: body.productsNote.trim(),
        shippingPreference: body.shippingPreference ?? 'unknown',
        note: body.note?.trim() || undefined,
        status: 'new',
      },
    })
    return NextResponse.json({ quoteNumber, saved: true })
  } catch (err) {
    console.error('Quote save failed:', err)
    return NextResponse.json({ error: 'Server error' }, { status: 500 })
  }
}
