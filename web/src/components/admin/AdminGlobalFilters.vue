<script setup lang="ts">
import { computed } from 'vue'
import { DollarSign, Filter, MapPin, ShoppingBag, Store, Users } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import { formatCurrency } from '@/services/api'
import type { AdminDashboard, Order, Restaurant } from '@/types'

const props = defineProps<{
  stats: AdminDashboard | null
  restaurants: Restaurant[]
  orders: Order[]
  filterRestaurantId: string
  filterStatus: string
  filterPaymentMethod: string
  filterRegion: string
  loading?: boolean
}>()

const emit = defineEmits<{
  (e: 'update:filterRestaurantId', value: string): void
  (e: 'update:filterStatus', value: string): void
  (e: 'update:filterPaymentMethod', value: string): void
  (e: 'update:filterRegion', value: string): void
  (e: 'apply'): void
}>()

const statusBreakdown = computed(() => {
  const total = props.orders.length
  if (total === 0) return null
  const delivered = props.orders.filter((o) => o.status === 'Delivered').length
  const inProgress = props.orders.filter((o) =>
    ['Created', 'PaymentPending', 'Paid', 'Confirmed', 'Preparing', 'ReadyForPickup', 'DriverAssigned', 'PickedUp', 'InTransit'].includes(o.status),
  ).length
  const cancelled = props.orders.filter((o) =>
    ['Cancelled', 'Refunded', 'Failed'].includes(o.status),
  ).length

  return {
    total,
    delivered,
    inProgress,
    cancelled,
    deliveredPct: Math.round((delivered / total) * 100),
    inProgressPct: Math.round((inProgress / total) * 100),
    cancelledPct: Math.round((cancelled / total) * 100),
  }
})

const maxRegionRevenue = computed(() => {
  const list = props.stats?.ordersByRegion || []
  return Math.max(1, ...list.map((r) => r.revenue))
})
</script>

<template>
  <section class="space-y-5">
    <!-- Global Filter Bar -->
    <AppCard padding="sm">
      <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-5 items-end">
        <div class="space-y-1">
          <label class="block text-[11px] font-bold uppercase tracking-wider text-ink-muted">
            Restaurante
          </label>
          <select
            :value="filterRestaurantId"
            class="w-full rounded-xl border border-line-strong bg-surface px-3 py-2 text-xs font-medium text-ink"
            @change="emit('update:filterRestaurantId', ($event.target as HTMLSelectElement).value)"
          >
            <option value="">Todos os restaurantes</option>
            <option v-for="r in restaurants" :key="r.id" :value="r.id">{{ r.name }}</option>
          </select>
        </div>

        <div class="space-y-1">
          <label class="block text-[11px] font-bold uppercase tracking-wider text-ink-muted">
            Status do Pedido
          </label>
          <select
            :value="filterStatus"
            class="w-full rounded-xl border border-line-strong bg-surface px-3 py-2 text-xs font-medium text-ink"
            @change="emit('update:filterStatus', ($event.target as HTMLSelectElement).value)"
          >
            <option value="">Todos os status</option>
            <option value="Paid">Pago</option>
            <option value="Confirmed">Confirmado</option>
            <option value="Preparing">Em preparo</option>
            <option value="ReadyForPickup">Pronto p/ coleta</option>
            <option value="InTransit">Em rota</option>
            <option value="Delivered">Entregue</option>
            <option value="Cancelled">Cancelado</option>
            <option value="Refunded">Estornado</option>
          </select>
        </div>

        <div class="space-y-1">
          <label class="block text-[11px] font-bold uppercase tracking-wider text-ink-muted">
            Pagamento
          </label>
          <select
            :value="filterPaymentMethod"
            class="w-full rounded-xl border border-line-strong bg-surface px-3 py-2 text-xs font-medium text-ink"
            @change="emit('update:filterPaymentMethod', ($event.target as HTMLSelectElement).value)"
          >
            <option value="">Todas as formas</option>
            <option value="PIX">PIX</option>
            <option value="CreditCard">Cartão de Crédito</option>
            <option value="DebitCard">Cartão de Débito</option>
          </select>
        </div>

        <div class="space-y-1">
          <label class="block text-[11px] font-bold uppercase tracking-wider text-ink-muted">
            Região / Bairro
          </label>
          <input
            :value="filterRegion"
            type="text"
            placeholder="Ex: Victor Konder"
            class="w-full rounded-xl border border-line-strong bg-surface px-3 py-2 text-xs text-ink"
            @input="emit('update:filterRegion', ($event.target as HTMLInputElement).value)"
            @keyup.enter="emit('apply')"
          />
        </div>

        <AppButton variant="secondary" size="sm" block :loading="loading" @click="emit('apply')">
          <Filter class="h-3.5 w-3.5" />
          Aplicar filtros
        </AppButton>
      </div>
    </AppCard>

    <!-- Executive KPI Cards with Inline SVG Sparklines -->
    <div v-if="stats" class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <AppCard padding="md" class="space-y-2">
        <div class="flex items-center justify-between text-ink-muted">
          <span class="text-xs font-bold uppercase tracking-wider">Usuários</span>
          <Users class="h-4 w-4" />
        </div>
        <div class="flex items-end justify-between gap-2">
          <div>
            <p class="font-display text-3xl font-extrabold text-ink tabular-nums">{{ stats.totalUsers }}</p>
            <p class="text-[11px] text-ink-muted">Entregadores: <strong class="text-ink">{{ stats.totalCouriers ?? 0 }}</strong></p>
          </div>
          <svg viewBox="0 0 64 24" class="h-6 w-16 text-sky-500" aria-hidden="true">
            <polyline fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" points="2,20 14,17 26,18 38,11 50,8 62,4" />
          </svg>
        </div>
      </AppCard>

      <AppCard padding="md" class="space-y-2">
        <div class="flex items-center justify-between text-ink-muted">
          <span class="text-xs font-bold uppercase tracking-wider">Restaurantes</span>
          <Store class="h-4 w-4 text-brand-600" />
        </div>
        <div class="flex items-end justify-between gap-2">
          <div>
            <p class="font-display text-3xl font-extrabold text-ink tabular-nums">{{ stats.totalRestaurants }}</p>
            <p v-if="stats.averageDeliveryMinutes" class="text-[11px] text-ink-muted tabular-nums">
              Tempo médio: <strong class="text-ink">~{{ stats.averageDeliveryMinutes }} min</strong>
            </p>
          </div>
          <svg viewBox="0 0 64 24" class="h-6 w-16 text-brand-500" aria-hidden="true">
            <polyline fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" points="2,18 15,16 28,12 40,14 52,7 62,6" />
          </svg>
        </div>
      </AppCard>

      <AppCard padding="md" class="space-y-2">
        <div class="flex items-center justify-between text-ink-muted">
          <span class="text-xs font-bold uppercase tracking-wider">Pedidos</span>
          <ShoppingBag class="h-4 w-4 text-sky-500" />
        </div>
        <div class="flex items-end justify-between gap-2">
          <div>
            <p class="font-display text-3xl font-extrabold text-ink tabular-nums">{{ stats.totalOrders }}</p>
            <p class="text-[11px] text-ink-muted tabular-nums">
              Ticket: <strong class="text-ink">{{ formatCurrency(stats.averageTicket || 0) }}</strong>
            </p>
          </div>
          <svg viewBox="0 0 64 24" class="h-6 w-16 text-amber-500" aria-hidden="true">
            <polyline fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" points="2,19 14,13 26,15 38,9 50,10 62,3" />
          </svg>
        </div>
      </AppCard>

      <AppCard padding="md" class="border-fresh-300/80 space-y-2 dark:border-fresh-800/60">
        <div class="flex items-center justify-between text-ink-muted">
          <span class="text-xs font-bold uppercase tracking-wider">Receita Total</span>
          <DollarSign class="h-4 w-4 text-fresh-600" />
        </div>
        <div class="flex items-end justify-between gap-2">
          <div class="min-w-0">
            <p class="font-display text-2xl font-extrabold text-fresh-700 dark:text-fresh-400 tabular-nums truncate">
              {{ formatCurrency(stats.totalRevenue) }}
            </p>
            <p class="text-[11px] text-ink-muted tabular-nums truncate">
              MRR: <strong class="text-ink">{{ formatCurrency(stats.monthlyRecurringRevenue || 0) }}</strong> · Churn: {{ stats.churnRatePercentage || 0 }}%
            </p>
          </div>
          <svg viewBox="0 0 64 24" class="h-6 w-16 shrink-0 text-fresh-600" aria-hidden="true">
            <polyline fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" points="2,21 15,18 27,13 39,11 51,6 62,2" />
          </svg>
        </div>
      </AppCard>
    </div>

    <!-- Visual Charts Row: Order Status Stacked Bar + Regional Revenue Horizontal Bars -->
    <div v-if="statusBreakdown || (stats?.ordersByRegion && stats.ordersByRegion.length > 0)" class="grid gap-4 lg:grid-cols-2">
      <!-- Order Distribution Stacked Bar -->
      <AppCard v-if="statusBreakdown" padding="md" class="space-y-3">
        <div class="flex items-center justify-between text-xs">
          <h3 class="font-display text-sm font-extrabold text-ink">Distribuição de Pedidos por Status</h3>
          <span class="font-bold text-ink-muted tabular-nums">{{ statusBreakdown.total }} total</span>
        </div>
        <div class="flex h-3 w-full overflow-hidden rounded-full bg-surface-muted">
          <div class="bg-fresh-600" :style="{ width: `${statusBreakdown.deliveredPct}%` }" title="Entregues" />
          <div class="bg-brand-500" :style="{ width: `${statusBreakdown.inProgressPct}%` }" title="Em andamento" />
          <div class="bg-red-500" :style="{ width: `${statusBreakdown.cancelledPct}%` }" title="Cancelados/Estornados" />
        </div>
        <div class="flex flex-wrap gap-4 text-[11px] font-semibold">
          <span class="inline-flex items-center gap-1.5 text-ink-secondary">
            <span class="h-2.5 w-2.5 rounded-full bg-fresh-600" />
            Entregues: <strong class="text-ink tabular-nums">{{ statusBreakdown.delivered }} ({{ statusBreakdown.deliveredPct }}%)</strong>
          </span>
          <span class="inline-flex items-center gap-1.5 text-ink-secondary">
            <span class="h-2.5 w-2.5 rounded-full bg-brand-500" />
            Em andamento: <strong class="text-ink tabular-nums">{{ statusBreakdown.inProgress }} ({{ statusBreakdown.inProgressPct }}%)</strong>
          </span>
          <span class="inline-flex items-center gap-1.5 text-ink-secondary">
            <span class="h-2.5 w-2.5 rounded-full bg-red-500" />
            Cancelados/Estornados: <strong class="text-ink tabular-nums">{{ statusBreakdown.cancelled }} ({{ statusBreakdown.cancelledPct }}%)</strong>
          </span>
        </div>
      </AppCard>

      <!-- Regional Performance Horizontal Bars -->
      <AppCard v-if="stats?.ordersByRegion && stats.ordersByRegion.length > 0" padding="md" class="space-y-3">
        <div class="flex items-center gap-2">
          <MapPin class="h-4 w-4 text-brand-600" />
          <h3 class="font-display text-sm font-extrabold text-ink">Desempenho por Região</h3>
        </div>
        <div class="space-y-2.5">
          <div v-for="reg in stats.ordersByRegion.slice(0, 5)" :key="reg.region" class="space-y-1 text-xs">
            <div class="flex items-center justify-between">
              <span class="font-bold text-ink truncate">{{ reg.region }}</span>
              <span class="text-ink-secondary tabular-nums">
                <strong>{{ reg.count }} ped.</strong> · {{ formatCurrency(reg.revenue) }}
              </span>
            </div>
            <div class="h-2 w-full overflow-hidden rounded-full bg-surface-muted">
              <div
                class="h-full rounded-full bg-brand-600"
                :style="{ width: `${Math.max(6, Math.round((reg.revenue / maxRegionRevenue) * 100))}%` }"
              />
            </div>
          </div>
        </div>
      </AppCard>
    </div>
  </section>
</template>
