export type PaymentProvider = 'nextpay' | 'zarinpal' | 'balepay'

export type PaymentRequest = {
  orderId: string
  amount: number
  currency: 'IRT'
  callbackUrl: string
  customerMobile?: string
  description?: string
}

export type PaymentInitResult =
  | { ok: true; redirectUrl: string; providerRef: string }
  | { ok: false; error: string }

export type PaymentVerifyResult =
  | { ok: true; orderId: string; paidAmount: number; refId: string }
  | { ok: false; error: string }

export interface PaymentAdapter {
  readonly provider: PaymentProvider
  initPayment(request: PaymentRequest): Promise<PaymentInitResult>
  verifyPayment(params: Record<string, string>): Promise<PaymentVerifyResult>
}

class StubPaymentAdapter implements PaymentAdapter {
  readonly provider: PaymentProvider

  constructor(provider: PaymentProvider) {
    this.provider = provider
  }

  async initPayment(request: PaymentRequest): Promise<PaymentInitResult> {
    return {
      ok: true,
      redirectUrl: `/checkout/pending?order=${request.orderId}&provider=${this.provider}`,
      providerRef: `stub-${this.provider}-${Date.now()}`,
    }
  }

  async verifyPayment(params: Record<string, string>): Promise<PaymentVerifyResult> {
    const orderId = params.order ?? 'unknown'
    return { ok: true, orderId, paidAmount: 0, refId: `stub-verify-${orderId}` }
  }
}

export class PaymentService {
  private adapters: Map<PaymentProvider, PaymentAdapter>

  constructor(adapters?: PaymentAdapter[]) {
    this.adapters = new Map(
      (
        adapters ?? [
          new StubPaymentAdapter('nextpay'),
          new StubPaymentAdapter('zarinpal'),
          new StubPaymentAdapter('balepay'),
        ]
      ).map((a) => [a.provider, a]),
    )
  }

  getAdapter(provider: PaymentProvider): PaymentAdapter | undefined {
    return this.adapters.get(provider)
  }

  async init(provider: PaymentProvider, request: PaymentRequest): Promise<PaymentInitResult> {
    const adapter = this.adapters.get(provider)
    if (!adapter) return { ok: false, error: `Unknown provider: ${provider}` }
    return adapter.initPayment(request)
  }

  async verify(
    provider: PaymentProvider,
    params: Record<string, string>,
  ): Promise<PaymentVerifyResult> {
    const adapter = this.adapters.get(provider)
    if (!adapter) return { ok: false, error: `Unknown provider: ${provider}` }
    return adapter.verifyPayment(params)
  }
}

export const paymentService = new PaymentService()
