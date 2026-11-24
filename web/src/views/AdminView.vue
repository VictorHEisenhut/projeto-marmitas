<script setup lang="ts">
import { onMounted, ref } from 'vue'
import AdminLayout, { type AdminModule } from '@/layouts/AdminLayout.vue'
import AdminCommandPalette from '@/components/admin/AdminCommandPalette.vue'
import AdminGlobalFilters from '@/components/admin/AdminGlobalFilters.vue'
import AdminRestaurantsModule from '@/components/admin/AdminRestaurantsModule.vue'
import AdminPlansCouponsModule from '@/components/admin/AdminPlansCouponsModule.vue'
import AdminUsersCouriersModule from '@/components/admin/AdminUsersCouriersModule.vue'
import AdminOrdersModule from '@/components/admin/AdminOrdersModule.vue'
import AdminFinanceAuditModule from '@/components/admin/AdminFinanceAuditModule.vue'
import AppSkeleton from '@/components/ui/AppSkeleton.vue'
import { api, getErrorMessage } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import type {
  AdminDashboard,
  AuditLog,
  Category,
  CommissionSummary,
  Coupon,
  Courier,
  DeliveriesReport,
  Order,
  Payout,
  Plan,
  Restaurant,
  SubscriptionsReport,
  User,
} from '@/types'

const toast = useToastStore()

const activeModule = ref<AdminModule>('restaurantes')
const paletteOpen = ref(false)
const focusedItemId = ref<string | null>(null)
const loading = ref(true)

const stats = ref<AdminDashboard | null>(null)
const restaurants = ref<Restaurant[]>([])
const categories = ref<Category[]>([])
const plans = ref<Plan[]>([])
const coupons = ref<Coupon[]>([])
const users = ref<User[]>([])
const couriers = ref<Courier[]>([])
const adminOrders = ref<Order[]>([])
const payouts = ref<Payout[]>([])
const commissions = ref<CommissionSummary | null>(null)
const subscriptionsReport = ref<SubscriptionsReport | null>(null)
const deliveriesReport = ref<DeliveriesReport | null>(null)
const auditLogs = ref<AuditLog[]>([])

const filterRestaurantId = ref('')
const filterStatus = ref('')
const filterRegion = ref('')
const filterPaymentMethod = ref('')

function handlePaletteNavigate(payload: { module: AdminModule; itemId?: string }) {
  if (!payload.itemId && !paletteOpen.value) {
    paletteOpen.value = true
    return
  }
  activeModule.value = payload.module
  focusedItemId.value = payload.itemId || null
}

async function loadAllData() {
  loading.value = true
  try {
    const results = await Promise.allSettled([
      api.get<AdminDashboard>('/admin/dashboard', {
        params: {
          restaurantId: filterRestaurantId.value || undefined,
          status: filterStatus.value || undefined,
          region: filterRegion.value.trim() || undefined,
          paymentMethod: filterPaymentMethod.value || undefined,
        },
      }),
      api.get<Restaurant[]>('/restaurantes', { params: { includeAll: true } }),
      api.get<Category[]>('/categorias'),
      api.get<Plan[]>('/planos'),
      api.get<Coupon[]>('/cupons'),
      api.get<User[]>('/usuarios'),
      api.get<Courier[]>('/entregadores'),
      api.get<Payout[]>('/admin/financeiro/repasses'),
      api.get<CommissionSummary>('/admin/financeiro/comissoes'),
      api.get<SubscriptionsReport>('/admin/relatorios/assinaturas'),
      api.get<DeliveriesReport>('/admin/relatorios/entregas'),
      api.get<AuditLog[]>('/admin/auditoria'),
      api.get<Order[]>('/admin/pedidos'),
    ])

    if (results[0].status === 'fulfilled') stats.value = results[0].value.data
    if (results[1].status === 'fulfilled') restaurants.value = results[1].value.data
    if (results[2].status === 'fulfilled') categories.value = results[2].value.data
    if (results[3].status === 'fulfilled') plans.value = results[3].value.data
    if (results[4].status === 'fulfilled') coupons.value = results[4].value.data
    if (results[5].status === 'fulfilled') users.value = results[5].value.data
    if (results[6].status === 'fulfilled') couriers.value = results[6].value.data
    if (results[7].status === 'fulfilled') payouts.value = results[7].value.data
    if (results[8].status === 'fulfilled') commissions.value = results[8].value.data
    if (results[9].status === 'fulfilled') subscriptionsReport.value = results[9].value.data
    if (results[10].status === 'fulfilled') deliveriesReport.value = results[10].value.data
    if (results[11].status === 'fulfilled') auditLogs.value = results[11].value.data
    if (results[12].status === 'fulfilled') adminOrders.value = results[12].value.data
  } catch (err) {
    toast.error(getErrorMessage(err, 'Erro ao carregar dados do painel administrativo.'))
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  void loadAllData()
})
</script>

<template>
  <AdminLayout
    v-model:active-module="activeModule"
    :loading="loading"
    @refresh="loadAllData"
    @open-palette="paletteOpen = true"
  >
    <div class="space-y-6">
      <AdminGlobalFilters
        v-model:filter-restaurant-id="filterRestaurantId"
        v-model:filter-status="filterStatus"
        v-model:filter-payment-method="filterPaymentMethod"
        v-model:filter-region="filterRegion"
        :stats="stats"
        :restaurants="restaurants"
        :orders="adminOrders"
        :loading="loading"
        @apply="loadAllData"
      />

      <div v-if="loading && !stats" class="grid gap-4 lg:grid-cols-2">
        <AppSkeleton v-for="n in 4" :key="n" variant="card" height="180px" />
      </div>

      <AdminRestaurantsModule
        v-else-if="activeModule === 'restaurantes'"
        :restaurants="restaurants"
        :categories="categories"
        :users="users"
        :focused-item-id="focusedItemId"
        @clear-focus="focusedItemId = null"
        @refresh="loadAllData"
      />

      <AdminPlansCouponsModule
        v-else-if="activeModule === 'planos'"
        :plans="plans"
        :coupons="coupons"
        :restaurants="restaurants"
        @refresh="loadAllData"
      />

      <AdminUsersCouriersModule
        v-else-if="activeModule === 'usuarios'"
        :users="users"
        :couriers="couriers"
        :focused-item-id="focusedItemId"
        @clear-focus="focusedItemId = null"
        @refresh="loadAllData"
      />

      <AdminOrdersModule
        v-else-if="activeModule === 'pedidos'"
        :orders="adminOrders"
        :focused-item-id="focusedItemId"
        @clear-focus="focusedItemId = null"
        @refresh="loadAllData"
      />

      <AdminFinanceAuditModule
        v-else
        :commissions="commissions"
        :subscriptions-report="subscriptionsReport"
        :deliveries-report="deliveriesReport"
        :payouts="payouts"
        :audit-logs="auditLogs"
        :restaurants="restaurants"
        :couriers="couriers"
        @refresh="loadAllData"
      />
    </div>

    <AdminCommandPalette
      :open="paletteOpen"
      :restaurants="restaurants"
      :users="users"
      :couriers="couriers"
      :orders="adminOrders"
      @close="paletteOpen = false"
      @navigate="handlePaletteNavigate"
    />
  </AdminLayout>
</template>
