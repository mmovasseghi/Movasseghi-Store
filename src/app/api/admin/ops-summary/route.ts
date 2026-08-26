import config from '@payload-config'
import { getPayload } from 'payload'
import { headers } from 'next/headers'
import { NextResponse } from 'next/server'

export async function GET() {
  try {
    const payload = await getPayload({ config })
    const { user } = await payload.auth({ headers: await headers() })
    if (!user) {
      return NextResponse.json({ error: 'Unauthorized' }, { status: 401 })
    }

    const [pendingOrders, newQuotes, publishedProducts] = await Promise.all([
      payload.count({
        collection: 'orders',
        where: { status: { equals: 'pending' } },
      }),
      payload.count({
        collection: 'quotes',
        where: { status: { equals: 'new' } },
      }),
      payload.count({
        collection: 'products',
        where: { status: { equals: 'published' } },
      }),
    ])

    return NextResponse.json({
      pendingOrders: pendingOrders.totalDocs,
      newQuotes: newQuotes.totalDocs,
      publishedProducts: publishedProducts.totalDocs,
    })
  } catch (err) {
    console.error('Ops summary failed:', err)
    return NextResponse.json({ error: 'Server error' }, { status: 500 })
  }
}
