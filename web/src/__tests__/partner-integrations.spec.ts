import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import PartnerLayout from '@/layouts/PartnerLayout.vue'
import PartnerIntegrationsModule from '@/components/partner/PartnerIntegrationsModule.vue'
import { api } from '@/services/api'

vi.mock('@/services/api', () => ({
  api: {
    get: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
  getErrorMessage: (_error: unknown, fallback = 'erro') => fallback,
}))

function installPinia() {
  const pinia = createPinia()
  setActivePinia(pinia)
  return pinia
}

describe('Portal do parceiro — Integrações', () => {
  beforeEach(() => {
    vi.mocked(api.get).mockReset()
    vi.mocked(api.put).mockReset()
  })

  it('mostra a aba Integrações no menu e seleciona o módulo', async () => {
    const pinia = installPinia()
    const router = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: '/', component: { template: '<div />' } },
        { path: '/login', component: { template: '<div />' } },
        { path: '/admin', component: { template: '<div />' } },
      ],
    })

    const wrapper = mount(PartnerLayout, {
      props: {
        activeModule: 'orders',
        restaurants: [
          {
            id: 'loja-1',
            name: 'Marmitaria Centro',
            description: 'Caseira',
            document: '12345678000199',
            status: 'Approved',
            ownerId: 'dono-1',
          },
        ],
        selectedRestaurantId: 'loja-1',
      },
      global: { plugins: [pinia, router] },
    })

    const tab = wrapper.findAll('button').find((button) => button.text().includes('Integrações'))
    expect(tab).toBeDefined()
    await tab!.trigger('click')
    expect(wrapper.emitted('update:activeModule')?.[0]).toEqual(['integrations'])
  })

  it('renderiza o Mercado Pago para a loja informar o token', async () => {
    installPinia()
    vi.mocked(api.get).mockResolvedValue({
      data: {
        provider: 'MercadoPago',
        enabled: false,
        connected: false,
      },
    })

    const wrapper = mount(PartnerIntegrationsModule, {
      props: {
        restaurantId: 'loja-1',
        restaurantName: 'Marmitaria Centro',
      },
    })

    await flushPromises()

    expect(wrapper.text()).toContain('Mercado Pago')
    expect(wrapper.text()).toContain('Access Token')
    expect(wrapper.text()).toContain('Public Key')
    expect(wrapper.text()).toContain('Salvar e validar')
    expect(api.get).toHaveBeenCalledWith('/restaurantes/loja-1/integracoes/mercadopago')
  })
})
