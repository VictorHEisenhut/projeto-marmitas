import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { api } from '@/services/api'
import type { Order } from '@/types'

const ACTIVE_STATUSES = new Set([
  'Created',
  'Pending',
  'PaymentPending',
  'Paid',
  'Confirmed',
  'Preparing',
  'ReadyForPickup',
  'DriverAssigned',
  'PickedUp',
  'InTransit',
])

export const useActiveOrderStore = defineStore('activeOrder', () => {
  const activeOrders = ref<Order[]>([])
  const loaded = ref(false)

  const primaryActiveOrder = computed<Order | null>(() => activeOrders.value[0] ?? null)

  const statusLabel = computed(() => {
    const s = primaryActiveOrder.value?.status
    if (!s) return ''
    const map: Record<string, string> = {
      Created: 'Pedido criado',
      Pending: 'Pendente',
      PaymentPending: 'Aguardando pagamento',
      Paid: 'Pedido pago',
      Confirmed: 'Confirmado',
      Preparing: 'Em preparo',
      ReadyForPickup: 'Pronto p/ coleta',
      DriverAssigned: 'Entregador a caminho',
      PickedUp: 'Coletado',
      InTransit: 'Em rota até você',
    }
    return map[s] || s
  })

  async function refresh() {
    try {
      const { data } = await api.get<Order[]>('/pedidos')
      activeOrders.value = data.filter((o) => ACTIVE_STATUSES.has(o.status))
      loaded.value = true
    } catch {
      // Silent for unauthenticated or non-customer roles
    }
  }

  function reset() {
    activeOrders.value = []
    loaded.value = false
  }

  return {
    activeOrders,
    primaryActiveOrder,
    statusLabel,
    loaded,
    refresh,
    reset,
  }
})
