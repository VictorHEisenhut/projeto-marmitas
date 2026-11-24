<script setup lang="ts">
import { computed } from 'vue'
import { DollarSign, Percent, ShoppingBag, Wallet } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppEmptyState from '@/components/ui/AppEmptyState.vue'
import { formatCurrency } from '@/services/api'
import type { RestaurantFinancials } from '@/types'

const props = defineProps<{
  financials: RestaurantFinancials | null
}>()

const chartBars = computed(() => {
  if (!props.financials) return []
  const max = Math.max(1, props.financials.grossRevenue)
  return [
    {
      label: 'Receita Bruta',
      value: props.financials.grossRevenue,
      pct: Math.round((props.financials.grossRevenue / max) * 100),
      colorClass: 'bg-brand-500',
    },
    {
      label: 'Seu Repasse (90%)',
      value: props.financials.netRevenue,
      pct: Math.round((props.financials.netRevenue / max) * 100),
      colorClass: 'bg-fresh-600',
    },
    {
      label: 'Comissão (10%)',
      value: props.financials.platformCommission,
      pct: Math.max(4, Math.round((props.financials.platformCommission / max) * 100)),
      colorClass: 'bg-amber-400',
    },
  ]
})
</script>

<template>
  <div v-if="financials" class="space-y-6">
    <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <AppCard padding="md">
        <div class="flex items-center justify-between text-ink-muted">
          <span class="text-xs font-bold uppercase tracking-wider">Pedidos Válidos</span>
          <ShoppingBag class="h-5 w-5 text-sky-500" />
        </div>
        <p class="mt-3 font-display text-3xl font-extrabold text-ink tabular-nums">
          {{ financials.totalOrders }}
        </p>
      </AppCard>

      <AppCard padding="md">
        <div class="flex items-center justify-between text-ink-muted">
          <span class="text-xs font-bold uppercase tracking-wider">Receita Bruta</span>
          <DollarSign class="h-5 w-5 text-brand-600" />
        </div>
        <p class="mt-3 font-display text-3xl font-extrabold text-ink tabular-nums">
          {{ formatCurrency(financials.grossRevenue) }}
        </p>
      </AppCard>

      <AppCard padding="md">
        <div class="flex items-center justify-between text-ink-muted">
          <span class="text-xs font-bold uppercase tracking-wider">Comissão (10%)</span>
          <Percent class="h-5 w-5 text-amber-500" />
        </div>
        <p class="mt-3 font-display text-3xl font-extrabold text-ink-secondary tabular-nums">
          {{ formatCurrency(financials.platformCommission) }}
        </p>
      </AppCard>

      <AppCard padding="md" class="border-fresh-300/80 bg-fresh-50/40 dark:border-fresh-800/60 dark:bg-fresh-950/25">
        <div class="flex items-center justify-between text-fresh-700 dark:text-fresh-400">
          <span class="text-xs font-bold uppercase tracking-wider">Receita Líquida</span>
          <Wallet class="h-5 w-5" />
        </div>
        <p class="mt-3 font-display text-3xl font-extrabold text-fresh-700 dark:text-fresh-300 tabular-nums">
          {{ formatCurrency(financials.netRevenue) }}
        </p>
      </AppCard>
    </div>

    <!-- Visual Comparative Chart + Payout Breakdown -->
    <AppCard padding="lg" class="space-y-5">
      <div>
        <h3 class="font-display text-base font-extrabold text-ink">Composição Financeira & Repasse</h3>
        <p class="text-xs text-ink-muted">
          Comparativo proporcional entre volume bruto de pedidos, comissão de 10% e repasse líquido via PIX.
        </p>
      </div>

      <div class="space-y-3.5">
        <div v-for="bar in chartBars" :key="bar.label" class="space-y-1.5">
          <div class="flex items-center justify-between text-xs">
            <span class="font-bold text-ink">{{ bar.label }}</span>
            <span class="font-display font-extrabold text-ink tabular-nums">
              {{ formatCurrency(bar.value) }}
            </span>
          </div>
          <div class="h-3 w-full overflow-hidden rounded-full bg-surface-muted">
            <div
              class="h-full rounded-full transition-all duration-500"
              :class="bar.colorClass"
              :style="{ width: `${bar.pct}%` }"
            />
          </div>
        </div>
      </div>
    </AppCard>
  </div>

  <AppEmptyState
    v-else
    :icon="DollarSign"
    title="Sem movimentação financeira ainda"
    description="Assim que seus primeiros pedidos forem pagos, o resumo de receita bruta, comissão e líquido aparecerá aqui."
  />
</template>
