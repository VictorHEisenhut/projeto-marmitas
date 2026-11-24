import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { mount } from '@vue/test-utils'
import { api } from '@/services/api'
import { useActiveOrderStore } from '@/stores/activeOrder'
import { useCartStore } from '@/stores/cart'
import { useToastStore } from '@/stores/toast'
import OrderStatusBadge from '@/components/ui/OrderStatusBadge.vue'
import StarRating from '@/components/ui/StarRating.vue'
import OtpInput from '@/components/ui/OtpInput.vue'
import CheckoutPanel from '@/components/customer/CheckoutPanel.vue'

if (typeof globalThis.localStorage === 'undefined' || typeof globalThis.localStorage?.clear !== 'function') {
  const store = new Map<string, string>()
  Object.defineProperty(globalThis, 'localStorage', {
    value: {
      getItem: (key: string) => store.get(key) ?? null,
      setItem: (key: string, value: string) => store.set(key, String(value)),
      removeItem: (key: string) => store.delete(key),
      clear: () => store.clear(),
    },
    configurable: true,
    writable: true,
  })
}

describe('stores/cart e stores/toast', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
  })

  it('detecta restaurante diferente e calcula preço promocional e resgate de plano', () => {
    const cart = useCartStore()
    cart.addItem(
      { id: 'rest-1', name: 'Cozinha Caseira' },
      { id: 'p-1', name: 'Marmita Fit', price: 30, promotionalPrice: 24, allowsCutlery: true, cutleryHasExtraCost: true, cutleryPrice: 2 },
      {
        wantsCutlery: true,
        selectedAddons: [{ name: 'Ovo', extraPrice: 3, quantity: 1 }],
        quantity: 2,
      },
    )

    expect(cart.hasDifferentRestaurant('rest-2')).toBe(true)
    expect(cart.hasDifferentRestaurant('rest-1')).toBe(false)
    // Unit price: 24 (promo) + 3 (addon) + 2 (cutlery) = 29 * 2 = 58
    expect(cart.totalAmount).toBe(58)

    // In plan mode, base price becomes 0, only addons (3) + cutlery (2) = 5 * 2 = 10
    cart.setPlanMode(true)
    expect(cart.totalAmount).toBe(10)

    cart.openDrawer()
    expect(cart.drawerOpen).toBe(true)
    cart.closeDrawer()
    expect(cart.drawerOpen).toBe(false)
  })

  it('gerencia notificações toast corretamente', () => {
    const toast = useToastStore()
    const id = toast.success('Pedido confirmado', 'Sucesso')
    expect(toast.toasts).toHaveLength(1)
    expect(toast.toasts[0]?.variant).toBe('success')
    toast.dismiss(id)
    expect(toast.toasts).toHaveLength(0)
  })
})

describe('componentes UI base', () => {
  it('renderiza rótulo semântico em OrderStatusBadge', () => {
    const wrapper = mount(OrderStatusBadge, {
      props: { status: 'InTransit' },
    })
    expect(wrapper.text()).toContain('Em rota')
  })

  it('emite nova nota ao clicar nas estrelas de StarRating', async () => {
    const wrapper = mount(StarRating, {
      props: { modelValue: 3 },
    })
    const buttons = wrapper.findAll('button')
    expect(buttons).toHaveLength(5)
    await buttons[4]!.trigger('click')
    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual([5])
  })

  it('aceita colagem de 4 dígitos no OtpInput', async () => {
    const wrapper = mount(OtpInput, {
      props: { modelValue: '', length: 4 },
    })
    const inputs = wrapper.findAll('input')
    expect(inputs).toHaveLength(4)
    await inputs[0]!.setValue('4829')
    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual(['4829'])
  })

  it('aceita colagem de 6 dígitos no OtpInput para redefinição de senha', async () => {
    const wrapper = mount(OtpInput, {
      props: { modelValue: '', length: 6 },
    })
    const inputs = wrapper.findAll('input')
    expect(inputs).toHaveLength(6)
    await inputs[0]!.setValue('194827')
    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual(['194827'])
  })
})

describe('useActiveOrderStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.restoreAllMocks()
  })

  it('filtra apenas pedidos em andamento, prioriza o principal e limpa no reset()', async () => {
    const getSpy = vi.spyOn(api, 'get').mockResolvedValue({
      data: [
        { id: 'ord-done', status: 'Delivered', total: 35, createdAt: '2026-01-01T10:00:00Z' },
        { id: 'ord-cancel', status: 'Cancelled', total: 30, createdAt: '2026-01-01T11:00:00Z' },
        { id: 'ord-active', status: 'InTransit', total: 42, createdAt: '2026-01-01T12:00:00Z' },
      ],
    })

    const store = useActiveOrderStore()
    await store.refresh()

    expect(getSpy).toHaveBeenCalledWith('/pedidos')
    expect(store.loaded).toBe(true)
    expect(store.activeOrders).toHaveLength(1)
    expect(store.primaryActiveOrder?.id).toBe('ord-active')
    expect(store.statusLabel).toBe('Em rota até você')

    store.reset()
    expect(store.activeOrders).toHaveLength(0)
    expect(store.primaryActiveOrder).toBeNull()
    expect(store.loaded).toBe(false)
  })
})

describe('CheckoutPanel — endereço obrigatório', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
    vi.restoreAllMocks()
  })

  it('bloqueia a finalização do pedido quando não há endereço cadastrado', () => {
    const cart = useCartStore()
    cart.addItem(
      { id: 'rest-1', name: 'Sabores de Blumenau' },
      { id: 'prod-1', name: 'Marmita Fit', price: 29.9 },
    )

    const wrapper = mount(CheckoutPanel, {
      props: {
        addresses: [],
        selectedAddressId: '',
        activeSubscription: null,
        weeklyPlanLimit: null,
        usedPlanMealsThisWeek: 0,
        cartEstimate: null,
      },
      global: {
        stubs: {
          RouterLink: { template: '<a><slot /></a>' },
        },
      },
    })

    const submitBtn = wrapper.findAll('button').find((b) => b.text().includes('Confirmar e pagar pedido'))
    expect(submitBtn).toBeDefined()
    expect(submitBtn!.attributes('disabled')).toBeDefined()
    expect(wrapper.text()).toContain('Endereço obrigatório para fazer pedidos')
  })

  it('habilita a finalização quando existe endereço cadastrado e selecionado (Blumenau/SC)', () => {
    const cart = useCartStore()
    cart.addItem(
      { id: 'rest-1', name: 'Sabores de Blumenau' },
      { id: 'prod-1', name: 'Marmita Fit', price: 29.9 },
    )

    const wrapper = mount(CheckoutPanel, {
      props: {
        addresses: [
          {
            id: 'addr-bnu-1',
            userId: 'user-1',
            street: 'Rua XV de Novembro',
            number: '450',
            neighborhood: 'Centro',
            city: 'Blumenau',
            state: 'SC',
            zipCode: '89010-001',
            isMain: true,
            latitude: -26.9194,
            longitude: -49.0661,
          },
        ],
        selectedAddressId: 'addr-bnu-1',
        activeSubscription: null,
        weeklyPlanLimit: null,
        usedPlanMealsThisWeek: 0,
        cartEstimate: null,
      },
      global: {
        stubs: {
          RouterLink: { template: '<a><slot /></a>' },
        },
      },
    })

    const submitBtn = wrapper.findAll('button').find((b) => b.text().includes('Confirmar e pagar pedido'))
    expect(submitBtn).toBeDefined()
    expect(submitBtn!.attributes('disabled')).toBeUndefined()
  })
})
