<script setup lang="ts">
import { DollarSign, Filter, MapPin, ShoppingBag, Store, Users } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import { formatCurrency } from '@/services/api'
import type { AdminDashboard, Restaurant } from '@/types'

defineProps<{
  stats: AdminDashboard | null
  restaurants: Restaurant[]
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
            placeholder="Ex: Vila Madalena"
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

    <!-- Executive KPI Cards -->
    <div v-if="stats" class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <AppCard padding="md">
        <div class="flex items-center justify-between text-ink-muted">
          <span class="text-xs font-bold uppercase tracking-wider">Usuários</span>
          <Users class="h-5 w-5 text-ink-muted" />
        </div>
        <p class="mt-2 font-display text-3xl font-extrabold text-ink tabular-nums">
          {{ stats.totalUsers }}
        </p>
        <p class="mt-1 text-xs text-ink-muted">
          Entregadores: <strong class="text-ink">{{ stats.totalCouriers ?? 0 }}</strong>
        </p>
      </AppCard>

      <AppCard padding="md">
        <div class="flex items-center justify-between text-ink-muted">
          <span class="text-xs font-bold uppercase tracking-wider">Restaurantes</span>
          <Store class="h-5 w-5 text-brand-600" />
        </div>
        <p class="mt-2 font-display text-3xl font-extrabold text-ink tabular-nums">
          {{ stats.totalRestaurants }}
        </p>
        <p v-if="stats.averageDeliveryMinutes" class="mt-1 text-xs text-ink-muted tabular-nums">
          Tempo médio: <strong class="text-ink">~{{ stats.averageDeliveryMinutes }} min</strong>
        </p>
      </AppCard>

      <AppCard padding="md">
        <div class="flex items-center justify-between text-ink-muted">
          <span class="text-xs font-bold uppercase tracking-wider">Pedidos</span>
          <ShoppingBag class="h-5 w-5 text-sky-500" />
        </div>
        <p class="mt-2 font-display text-3xl font-extrabold text-ink tabular-nums">
          {{ stats.totalOrders }}
        </p>
        <p class="mt-1 text-xs text-ink-muted tabular-nums">
          Ticket: <strong class="text-ink">{{ formatCurrency(stats.averageTicket || 0) }}</strong> · Cancelados: {{ stats.cancelledOrders || 0 }}
        </p>
      </AppCard>

      <AppCard padding="md" class="border-fresh-300/80 dark:border-fresh-800/60">
        <div class="flex items-center justify-between text-ink-muted">
          <span class="text-xs font-bold uppercase tracking-wider">Receita Total</span>
          <DollarSign class="h-5 w-5 text-fresh-600" />
        </div>
        <p class="mt-2 font-display text-3xl font-extrabold text-fresh-700 dark:text-fresh-400 tabular-nums">
          {{ formatCurrency(stats.totalRevenue) }}
        </p>
        <p class="mt-1 text-xs text-ink-muted tabular-nums">
          MRR: <strong class="text-ink">{{ formatCurrency(stats.monthlyRecurringRevenue || 0) }}</strong> · Churn: {{ stats.churnRatePercentage || 0 }}%
        </p>
      </AppCard>
    </div>

    <!-- Orders by Region Breakdown -->
    <AppCard v-if="stats?.ordersByRegion && stats.ordersByRegion.length > 0" padding="md" class="space-y-3">
      <div class="flex items-center gap-2">
        <MapPin class="h-4 w-4 text-brand-600" />
        <h3 class="font-display text-sm font-extrabold text-ink">Pedidos por Região</h3>
      </div>
      <div class="grid gap-2.5 sm:grid-cols-2 lg:grid-cols-4">
        <div
          v-for="reg in stats.ordersByRegion"
          :key="reg.region"
          class="flex items-center justify-between rounded-xl border border-line bg-surface-subtle px-3.5 py-2.5 text-xs"
        >
          <span class="font-bold text-ink truncate">{{ reg.region }}</span>
          <span class="shrink-0 text-ink-secondary tabular-nums">
            <strong>{{ reg.count }}</strong> · {{ formatCurrency(reg.revenue) }}
          </span>
        </div>
      </div>
    </AppCard>
  </section>
</template>
