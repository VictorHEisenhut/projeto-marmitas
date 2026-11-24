<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import PartnerLayout, { type PartnerModule } from '@/layouts/PartnerLayout.vue'
import PartnerOnboarding from '@/components/partner/PartnerOnboarding.vue'
import PartnerOrdersModule from '@/components/partner/PartnerOrdersModule.vue'
import PartnerMenuModule from '@/components/partner/PartnerMenuModule.vue'
import PartnerPlansModule from '@/components/partner/PartnerPlansModule.vue'
import PartnerFinancialsModule from '@/components/partner/PartnerFinancialsModule.vue'
import PartnerSettingsModule from '@/components/partner/PartnerSettingsModule.vue'
import AppSkeleton from '@/components/ui/AppSkeleton.vue'
import { api, getErrorMessage } from '@/services/api'
import { OrderTrackingClient } from '@/services/realtime'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'
import type {
  Category,
  DeliveryZone,
  Order,
  Plan,
  Product,
  Restaurant,
  RestaurantFinancials,
  RestaurantTeamMember,
} from '@/types'

const auth = useAuthStore()
const toast = useToastStore()

const activeModule = ref<PartnerModule>('orders')
const restaurants = ref<Restaurant[]>([])
const selectedRestaurantId = ref('')
const categories = ref<Category[]>([])
const products = ref<Product[]>([])
const plans = ref<Plan[]>([])
const orders = ref<Order[]>([])
const financials = ref<RestaurantFinancials | null>(null)
const deliveryZones = ref<DeliveryZone[]>([])
const teamMembers = ref<RestaurantTeamMember[]>([])

const loading = ref(true)
const loadingDetails = ref(false)

const currentRestaurant = computed(
  () => restaurants.value.find((r) => r.id === selectedRestaurantId.value) ?? null,
)

const pendingOrdersCount = computed(
  () =>
    orders.value.filter((o) =>
      ['Created', 'PaymentPending', 'Paid', 'Confirmed', 'Preparing'].includes(o.status),
    ).length,
)

const pendingComplaintsCount = computed(
  () => orders.value.filter((o) => Boolean(o.complaintMessage) && !o.complaintReply).length,
)

const realtimeClient = new OrderTrackingClient({
  onOrderStatusUpdated(payload) {
    if (payload.restaurantId && payload.restaurantId === selectedRestaurantId.value) {
      const target = orders.value.find((o) => o.id === payload.orderId)
      if (target) {
        target.status = payload.status
        target.updatedAt = payload.updatedAt
      } else {
        void loadRestaurantDetails(selectedRestaurantId.value)
        toast.info('Novo pedido recebido em tempo real!')
      }
    }
  },
})

async function loadRestaurants() {
  loading.value = true
  try {
    const { data } = await api.get<Restaurant[]>('/restaurantes')
    const filtered = data.filter(
      (r) =>
        !auth.user?.id ||
        r.ownerId === auth.user.id ||
        auth.hasRole(['admin', 'super_admin', 'restaurant_staff']),
    )
    restaurants.value = filtered
    const first = filtered[0]
    if (first && !selectedRestaurantId.value) {
      selectedRestaurantId.value = first.id
    } else if (selectedRestaurantId.value) {
      await loadRestaurantDetails(selectedRestaurantId.value)
    }
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível carregar seus restaurantes.'))
  } finally {
    loading.value = false
  }
}

async function loadRestaurantDetails(restaurantId: string) {
  if (!restaurantId) return
  loadingDetails.value = true
  try {
    const [prodRes, catRes, finRes, ordRes, zoneRes, teamRes, planRes] = await Promise.allSettled([
      api.get<Product[]>(`/restaurantes/${restaurantId}/cardapio`, {
        params: { includeInactive: true },
      }),
      api.get<Category[]>('/categorias'),
      api.get<RestaurantFinancials>(`/restaurantes/${restaurantId}/financeiro`),
      api.get<Order[]>('/pedidos', { params: { restaurantId } }),
      api.get<DeliveryZone[]>(`/restaurantes/${restaurantId}/zonas-entrega`),
      api.get<RestaurantTeamMember[]>(`/restaurantes/${restaurantId}/equipe`),
      api.get<Plan[]>('/planos', { params: { restaurantId } }),
    ])

    if (prodRes.status === 'fulfilled') products.value = prodRes.value.data
    if (catRes.status === 'fulfilled') categories.value = catRes.value.data
    if (finRes.status === 'fulfilled') financials.value = finRes.value.data
    if (ordRes.status === 'fulfilled') orders.value = ordRes.value.data
    if (zoneRes.status === 'fulfilled') deliveryZones.value = zoneRes.value.data
    if (teamRes.status === 'fulfilled') teamMembers.value = teamRes.value.data
    if (planRes.status === 'fulfilled') plans.value = planRes.value.data
  } catch (err) {
    toast.error(getErrorMessage(err, 'Erro ao carregar dados do restaurante.'))
  } finally {
    loadingDetails.value = false
  }
}

watch(selectedRestaurantId, (newId) => {
  if (newId) {
    realtimeClient.joinRestaurantGroup(newId)
    void loadRestaurantDetails(newId)
  }
})

async function handleRestaurantCreated(created: Restaurant) {
  await loadRestaurants()
  selectedRestaurantId.value = created.id
}

async function handleRefreshAll() {
  if (selectedRestaurantId.value) {
    await loadRestaurantDetails(selectedRestaurantId.value)
  } else {
    await loadRestaurants()
  }
}

onMounted(() => {
  void realtimeClient.connect()
  void loadRestaurants()
})

onUnmounted(() => {
  realtimeClient.disconnect()
})
</script>

<template>
  <PartnerLayout
    v-model:active-module="activeModule"
    v-model:selected-restaurant-id="selectedRestaurantId"
    :restaurants="restaurants"
    :loading="loading || loadingDetails"
    :pending-orders-count="pendingOrdersCount"
    :pending-complaints-count="pendingComplaintsCount"
    @refresh="handleRefreshAll"
  >
    <div v-if="loading" class="space-y-4">
      <AppSkeleton variant="card" height="120px" />
      <div class="grid gap-4 md:grid-cols-2">
        <AppSkeleton v-for="n in 4" :key="n" variant="card" height="160px" />
      </div>
    </div>

    <!-- Guided Onboarding if partner has no store yet -->
    <PartnerOnboarding
      v-else-if="restaurants.length === 0"
      @created="handleRestaurantCreated"
    />

    <!-- Modular Operational Workspace -->
    <template v-else-if="currentRestaurant">
      <PartnerOrdersModule
        v-if="activeModule === 'orders'"
        :orders="orders"
        @refresh="loadRestaurantDetails(currentRestaurant.id)"
      />

      <PartnerMenuModule
        v-else-if="activeModule === 'menu'"
        :restaurant-id="currentRestaurant.id"
        :products="products"
        :categories="categories"
        @refresh="loadRestaurantDetails(currentRestaurant.id)"
      />

      <PartnerPlansModule
        v-else-if="activeModule === 'plans'"
        :restaurant-id="currentRestaurant.id"
        :restaurant-name="currentRestaurant.name"
        :plans="plans"
        @refresh="loadRestaurantDetails(currentRestaurant.id)"
      />

      <PartnerFinancialsModule
        v-else-if="activeModule === 'financials'"
        :financials="financials"
      />

      <PartnerSettingsModule
        v-else
        :restaurant="currentRestaurant"
        :delivery-zones="deliveryZones"
        :team-members="teamMembers"
        @refresh="loadRestaurants"
      />
    </template>
  </PartnerLayout>
</template>
