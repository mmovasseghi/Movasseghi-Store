import { NextResponse } from 'next/server'
import type { NextRequest } from 'next/server'

const SPAM_SLUG =
  /casino|betting|bonus|bitcoin|btc|gambl|slot|poker|forex|kraken|binance|huobi|bybit|mexc|bitfinex|free.?spin|jackpot|roulette|wagering/i

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl

  if (pathname.startsWith('/mag/') && pathname.length > 5) {
    const slug = decodeURIComponent(pathname.slice(5))
    if (SPAM_SLUG.test(slug)) {
      return new NextResponse('این محتوا حذف شده است.', {
        status: 410,
        headers: { 'Content-Type': 'text/plain; charset=utf-8' },
      })
    }
  }

  return NextResponse.next()
}

export const config = {
  matcher: ['/mag/:path*'],
}
