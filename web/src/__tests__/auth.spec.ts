import { beforeEach, describe, expect, it } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { formatPhone, getErrorMessage } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'

describe('services/api helpers', () => {
  it('formata telefone brasileiro com 10 e 11 dígitos', () => {
    expect(formatPhone('11988887777')).toBe('(11) 98888-7777')
    expect(formatPhone('1133334444')).toBe('(11) 3333-4444')
  })

  it('retorna mensagem padrão quando erro é desconhecido', () => {
    expect(getErrorMessage(null, 'Falha padrão')).toBe('Falha padrão')
    expect(getErrorMessage(new Error('Erro específico'))).toBe('Erro específico')
  })
})

describe('stores/auth e stores/cart', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
  })

  it('inicia desautenticado e rota padrão aponta para vitrine', () => {
    const auth = useAuthStore()
    expect(auth.isAuthenticated).toBe(false)
    expect(auth.defaultRoute).toBe('/')
  })

  it('adiciona itens ao carrinho e calcula total corretamente', () => {
    const cart = useCartStore()
    cart.addItem(
      { id: 'rest-1', name: 'Cozinha da Maria' },
      { id: 'prod-1', name: 'Frango Grelhado', price: 25 },
    )
    cart.addItem(
      { id: 'rest-1', name: 'Cozinha da Maria' },
      { id: 'prod-1', name: 'Frango Grelhado', price: 25 },
    )

    expect(cart.totalItems).toBe(2)
    expect(cart.totalAmount).toBe(50)
  })
})
