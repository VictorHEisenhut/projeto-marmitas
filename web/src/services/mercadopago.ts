export interface MercadoPagoCardInput {
  cardNumber: string
  holderName: string
  expirationMonth: string
  expirationYear: string
  securityCode: string
  cpf: string
}

export interface MercadoPagoCardToken {
  token: string
  paymentMethodId: string
}

interface MercadoPagoPaymentMethod {
  id: string
  payment_type_id?: string
}

interface MercadoPagoInstance {
  createCardToken: (card: Record<string, string>) => Promise<{ id?: string }>
  getPaymentMethods: (params: { bin: string }) => Promise<{ results?: MercadoPagoPaymentMethod[] }>
}

interface MercadoPagoConstructor {
  new (publicKey: string, options?: { locale?: string }): MercadoPagoInstance
}

declare global {
  interface Window {
    MercadoPago?: MercadoPagoConstructor
  }
}

let sdkPromise: Promise<void> | null = null

export function loadMercadoPagoSdk(): Promise<void> {
  if (window.MercadoPago) return Promise.resolve()
  if (!sdkPromise) {
    sdkPromise = new Promise((resolve, reject) => {
      const script = document.createElement('script')
      script.src = 'https://sdk.mercadopago.com/js/v2'
      script.async = true
      script.onload = () => resolve()
      script.onerror = () => {
        sdkPromise = null
        reject(new Error('Não foi possível carregar o Mercado Pago. Verifique sua conexão.'))
      }
      document.head.appendChild(script)
    })
  }
  return sdkPromise
}

export async function createMercadoPagoCardToken(
  publicKey: string,
  input: MercadoPagoCardInput,
): Promise<MercadoPagoCardToken> {
  await loadMercadoPagoSdk()
  const MercadoPagoSdk = window.MercadoPago
  if (!MercadoPagoSdk) {
    throw new Error('O Mercado Pago não ficou disponível no navegador.')
  }

  const mp = new MercadoPagoSdk(publicKey, { locale: 'pt-BR' })
  const methods = await mp.getPaymentMethods({ bin: input.cardNumber.slice(0, 8) })
  const method =
    methods.results?.find((item) => item.payment_type_id === 'credit_card') ?? methods.results?.[0]

  if (!method?.id) {
    throw new Error('Não reconhecemos a bandeira deste cartão. Confira o número.')
  }

  const token = await mp.createCardToken({
    cardNumber: input.cardNumber,
    cardholderName: input.holderName,
    cardExpirationMonth: input.expirationMonth,
    cardExpirationYear: input.expirationYear,
    securityCode: input.securityCode,
    identificationType: 'CPF',
    identificationNumber: input.cpf,
  })

  if (!token?.id) {
    throw new Error('O Mercado Pago não gerou o token do cartão.')
  }

  return { token: token.id, paymentMethodId: method.id }
}

export function onlyDigits(value: string): string {
  return value.replace(/\D/g, '')
}

export function isValidCpf(value: string): boolean {
  const cpf = onlyDigits(value)
  if (cpf.length !== 11 || /^(\d)\1+$/.test(cpf)) return false

  const digit = (length: number) => {
    let sum = 0
    for (let i = 0; i < length; i += 1) sum += Number(cpf[i]) * (length + 1 - i)
    const mod = (sum * 10) % 11
    return mod === 10 ? 0 : mod
  }

  return digit(9) === Number(cpf[9]) && digit(10) === Number(cpf[10])
}
