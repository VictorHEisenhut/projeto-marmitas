<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { ClipboardList, RefreshCw, ShoppingBag, Truck } from 'lucide-vue-next'
import CustomerLayout from '@/layouts/CustomerLayout.vue'
import CheckoutPanel from '@/components/customer/CheckoutPanel.vue'
import LiveOrderMapHero from '@/components/customer/LiveOrderMapHero.vue'
import OrderTrackingCard from '@/components/customer/OrderTrackingCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppTabs from '@/components/ui/AppTabs.vue'
import AppSkeleton from '@/components/ui/AppSkeleton.vue'
import AppEmptyState from '@/components/ui/AppEmptyState.vue'
import { api, getErrorMessage } from '@/services/api'
import { OrderTrackingClient } from '@/services/realtime'
import { useCartStore } from '@/stores/cart'
import { useAddressStore } from '@/stores/address'
import { useActiveOrderStore } from '@/stores/activeOrder'
import { useToastStore } from '@/stores/toast'
import type {
  Address,
  CartDeliveryEstimate,
  DeliveryTracking,
  Order,
  Plan,
  Subscription,
} from '@/types'

const cart = useCartStore()
const addressStore = useAddressStore()
const activeOrderStore = useActiveOrderStore()
const toast = useToastStore()

const orders = ref<Order[]>([])
const addresses = ref<Address[]>([])
const selectedAddressId = ref(addressStore.selectedAddress?.id || '')
const activeSubscription = ref<Subscription | null>(null)
const weeklyPlanLimit = ref<number | null>(null)
const cartEstimate = ref<CartDeliveryEstimate | null>(null)
const loading = ref(true)
const error = ref('')

const historyTab = ref<'active' | 'history'>('active')

const trackingOrderId = ref<string | null>(null)
const trackingInfo = ref<DeliveryTracking | null>(null)
const loadingTracking = ref(false)
const liveCoordsByOrder = ref<Record<string, { latitude: number; longitude: number; updatedAt: string }>>({})

const activeStatuses = new Set([
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

const activeOrders = computed(() => orders.value.filter((o) => activeStatuses.has(o.status)))
const pastOrders = computed(() => orders.value.filter((o) => !activeStatuses.has(o.status)))

const heroOrder = computed<Order | null>(() => {
  if (trackingOrderId.value) {
    const found = activeOrders.value.find((o) => o.id === trackingOrderId.value)
    if (found) return found
  }
  return activeOrders.value[0] ?? null
})

const usedPlanMealsThisWeek = computed(() => {
  const cutoff = Date.now() - 7 * 24 * 60 * 60 * 1000
  return orders.value
    .filter((o) => o.status !== 'Cancelled' && new Date(o.createdAt).getTime() >= cutoff)
    .flatMap((o) => o.items || [])
    .filter((i) => i.productName.toUpperCase().startsWith('[PLANO]'))
    .reduce((acc, i) => acc + i.quantity, 0)
})

const orderTabs = computed(() => [
  { id: 'active', label: 'Em Andamento', count: activeOrders.value.length, icon: Truck },
  { id: 'history', label: 'Histórico', count: pastOrders.value.length, icon: ClipboardList },
])

async function loadCartDeliveryEstimate() {
  if (!cart.restaurantId) {
    cartEstimate.value = null
    return
  }
  try {
    const { data } = await api.get<CartDeliveryEstimate>('/geolocalizacao/estimativa-entrega', {
      params: {
        restaurantId: cart.restaurantId,
        addressId: selectedAddressId.value || undefined,
      },
    })
    cartEstimate.value = data
  } catch {
    cartEstimate.value = null
  }
}

watch([() => cart.restaurantId, selectedAddressId], () => {
  void loadCartDeliveryEstimate()
})

const realtimeClient = new OrderTrackingClient({
  onOrderStatusUpdated(payload) {
    const target = orders.value.find((o) => o.id === payload.orderId)
    if (target) {
      target.status = payload.status
      target.updatedAt = payload.updatedAt
      toast.info(`Pedido #${target.id.slice(0, 8)} atualizado para: ${payload.status}`)
    }
    void activeOrderStore.refresh()
    if ((trackingOrderId.value === payload.orderId || heroOrder.value?.id === payload.orderId) && target) {
      void openTrackingForOrder(target)
    }
  },
  onDriverLocationUpdated(payload) {
    liveCoordsByOrder.value[payload.orderId] = {
      latitude: payload.latitude,
      longitude: payload.longitude,
      updatedAt: payload.updatedAt,
    }
    if (trackingOrderId.value === payload.orderId || heroOrder.value?.id === payload.orderId) {
      const target = orders.value.find((o) => o.id === payload.orderId)
      if (target) {
        void openTrackingForOrder(target)
      }
    }
  },
})

async function openTrackingForOrder(order: Order) {
  trackingOrderId.value = order.id
  loadingTracking.value = true
  try {
    const { data } = await api.get<DeliveryTracking>(`/entregas/${order.id}/rastreamento`)
    const live = liveCoordsByOrder.value[order.id]
    if (live) {
      data.latitude = live.latitude
      data.longitude = live.longitude
      data.updatedAt = live.updatedAt
    }
    trackingInfo.value = data
  } catch {
    const live = liveCoordsByOrder.value[order.id]
    trackingInfo.value = {
      deliveryId: order.id,
      orderId: order.id,
      status: order.status,
      latitude: live?.latitude ?? -26.9194,
      longitude: live?.longitude ?? -49.0661,
      updatedAt: live?.updatedAt ?? order.updatedAt,
    }
  } finally {
    loadingTracking.value = false
  }
}

async function handleToggleTracking(order: Order) {
  if (trackingOrderId.value === order.id) {
    trackingOrderId.value = null
    trackingInfo.value = null
    return
  }
  await openTrackingForOrder(order)
}

async function loadOrders() {
  loading.value = true
  error.value = ''

  try {
    const [ordersRes, addrRes, subsRes, plansRes] = await Promise.allSettled([
      api.get<Order[]>('/pedidos'),
      api.get<Address[]>('/usuarios/me/enderecos'),
      api.get<Subscription[]>('/assinaturas'),
      api.get<Plan[]>('/planos'),
    ])

    if (ordersRes.status === 'fulfilled') {
      orders.value = ordersRes.value.data
      for (const ord of orders.value) {
        realtimeClient.joinOrderGroup(ord.id)
      }
      if (activeOrders.value.length === 0 && pastOrders.value.length > 0) {
        historyTab.value = 'history'
      } else if (activeOrders.value.length > 0) {
        historyTab.value = 'active'
      }
      const firstActive = activeOrders.value[0]
      if (firstActive) {
        void openTrackingForOrder(firstActive)
      }
    }

    if (addrRes.status === 'fulfilled') {
      addresses.value = addrRes.value.data
      const mainAddr =
        addresses.value.find((a) => a.id === addressStore.selectedAddressId) ||
        addresses.value.find((a) => a.isMain) ||
        addresses.value[0]
      if (mainAddr && !selectedAddressId.value) {
        selectedAddressId.value = mainAddr.id
      }
      if (cart.restaurantId) {
        void loadCartDeliveryEstimate()
      }
    }

    if (subsRes.status === 'fulfilled') {
      const active = subsRes.value.data.find((s) => s.status === 'Active') || null
      activeSubscription.value = active
      if (active && plansRes.status === 'fulfilled') {
        const plan = plansRes.value.data.find((p) => p.id === active.planId)
        weeklyPlanLimit.value = plan?.mealsPerWeek ?? 5
      } else {
        weeklyPlanLimit.value = null
      }
    }
  } catch (err) {
    error.value = getErrorMessage(err, 'Não foi possível carregar seus pedidos.')
  } finally {
    loading.value = false
    void activeOrderStore.refresh()
  }
}

async function handleOrderCreated() {
  historyTab.value = 'active'
  await loadOrders()
}

onMounted(() => {
  void realtimeClient.connect()
  void loadOrders()
})

onUnmounted(() => {
  realtimeClient.disconnect()
})
</script>

<template>
  <CustomerLayout hide-floating-cart>
    <div class="mx-auto max-w-6xl space-y-8 px-4 py-6 sm:px-6 sm:py-8">
      <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <div>
          <h1 class="font-display text-2xl font-extrabold tracking-tight text-ink sm:text-3xl">
            {{ cart.items.length > 0 ? 'Finalizar Pedido & Acompanhamento' : 'Meus Pedidos' }}
          </h1>
          <p class="mt-1 text-sm text-ink-secondary">
            Acompanhe o preparo, mapa GPS ao vivo, PIN de entrega e histórico completo.
          </p>
        </div>

        <AppButton variant="outline" size="sm" :disabled="loading" @click="loadOrders">
          <RefreshCw class="h-4 w-4" :class="{ 'animate-spin': loading }" />
          Atualizar
        </AppButton>
      </div>

      <div
        v-if="error"
        class="rounded-2xl border border-red-200 bg-red-50 p-4 text-sm text-red-700 dark:border-red-900/60 dark:bg-red-950/40 dark:text-red-300"
      >
        {{ error }}
      </div>

      <!-- 2-Column Checkout Flow -->
      <CheckoutPanel
        v-if="cart.items.length > 0"
        v-model:selected-address-id="selectedAddressId"
        :addresses="addresses"
        :active-subscription="activeSubscription"
        :weekly-plan-limit="weeklyPlanLimit"
        :used-plan-meals-this-week="usedPlanMealsThisWeek"
        :cart-estimate="cartEstimate"
        @order-created="handleOrderCreated"
      />

      <!-- Full-Bleed Live Map Hero when there is an active order and cart is empty -->
      <LiveOrderMapHero
        v-if="cart.items.length === 0 && heroOrder"
        :order="heroOrder"
        :tracking-info="heroOrder.id === trackingOrderId ? trackingInfo : null"
      />

      <!-- Live Orders & History Tabs -->
      <section class="space-y-5">
        <div class="flex flex-col justify-between gap-3 sm:flex-row sm:items-center">
          <h2 class="font-display text-lg font-extrabold text-ink">
            Acompanhamento de Pedidos
          </h2>

          <AppTabs
            v-model="historyTab"
            :tabs="orderTabs"
            variant="segmented"
            class="w-full sm:w-auto sm:min-w-[300px]"
          />
        </div>

        <div v-if="loading" class="space-y-4">
          <AppSkeleton v-for="n in 2" :key="n" variant="card" height="190px" />
        </div>

        <div v-else-if="historyTab === 'active'" class="space-y-4">
          <template v-if="activeOrders.length > 0">
            <OrderTrackingCard
              v-for="order in activeOrders"
              :key="order.id"
              :order="order"
              :is-active="true"
              :tracking-open="trackingOrderId === order.id"
              :tracking-info="trackingOrderId === order.id ? trackingInfo : null"
              :loading-tracking="loadingTracking"
              @toggle-tracking="handleToggleTracking"
              @refresh="loadOrders"
            />
          </template>

          <AppEmptyState
            v-else
            :icon="ShoppingBag"
            title="Nenhum pedido em andamento agora"
            description="Que tal escolher uma marmita fresquinha para o almoço de hoje?"
          >
            <template #action>
              <RouterLink to="/">
                <AppButton variant="primary" size="sm">Explorar restaurantes</AppButton>
              </RouterLink>
            </template>
          </AppEmptyState>
        </div>

        <div v-else class="space-y-4">
          <template v-if="pastOrders.length > 0">
            <OrderTrackingCard
              v-for="order in pastOrders"
              :key="order.id"
              :order="order"
              :is-active="false"
              :tracking-open="trackingOrderId === order.id"
              :tracking-info="trackingOrderId === order.id ? trackingInfo : null"
              :loading-tracking="loadingTracking"
              @toggle-tracking="handleToggleTracking"
              @refresh="loadOrders"
            />
          </template>

          <AppEmptyState
            v-else
            :icon="ClipboardList"
            title="Seu histórico está vazio"
            description="Seus pedidos entregues ou cancelados aparecerão organizados aqui."
          />
        </div>
      </section>
    </div>
  </CustomerLayout>
</template>
