import { beforeEach, describe, expect, it } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { mount } from '@vue/test-utils'
import { useCartStore } from '@/stores/cart'
import { useToastStore } from '@/stores/toast'
import OrderStatusBadge from '@/components/ui/OrderStatusBadge.vue'
import StarRating from '@/components/ui/StarRating.vue'
import OtpInput from '@/components/ui/OtpInput.vue'

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
})
