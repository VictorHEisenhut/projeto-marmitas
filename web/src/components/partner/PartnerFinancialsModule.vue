<script setup lang="ts">
import { DollarSign, Percent, ShoppingBag, Wallet } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppEmptyState from '@/components/ui/AppEmptyState.vue'
import { formatCurrency } from '@/services/api'
import type { RestaurantFinancials } from '@/types'

defineProps<{
  financials: RestaurantFinancials | null
}>()
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
      <p
        v-if="financials.subscriptionRevenue && financials.subscriptionRevenue > 0"
        class="mt-1 text-[11px] font-medium text-fresh-700 dark:text-fresh-400"
      >
        Inclui {{ financials.activeSubscriptionsCount ?? 0 }} assinatura(s) ({{ formatCurrency(financials.subscriptionRevenue) }})
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

    <AppCard padding="lg" class="space-y-3">
      <h3 class="font-display text-base font-extrabold text-ink">Detalhamento do Repasse</h3>
      <p class="text-xs text-ink-secondary">
        A taxa da plataforma é de 10% sobre o volume bruto de pedidos válidos. O valor líquido (90%) fica disponível para repasse via chave PIX cadastrada nas configurações da loja.
      </p>
      <div class="h-3 w-full overflow-hidden rounded-full bg-surface-muted flex">
        <div class="h-full bg-fresh-600" style="width: 90%" title="Receita líquida (90%)" />
        <div class="h-full bg-amber-400" style="width: 10%" title="Comissão plataforma (10%)" />
      </div>
      <div class="flex justify-between text-xs font-semibold">
        <span class="text-fresh-700 dark:text-fresh-400">90% Seu Repasse ({{ formatCurrency(financials.netRevenue) }})</span>
        <span class="text-amber-700 dark:text-amber-400">10% Plataforma ({{ formatCurrency(financials.platformCommission) }})</span>
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
