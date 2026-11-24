<script setup lang="ts">
import { computed, ref } from 'vue'
import { Search } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import OrderStatusBadge from '@/components/ui/OrderStatusBadge.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import type {
  AuditLog,
  CommissionSummary,
  Courier,
  DeliveriesReport,
  Payout,
  Restaurant,
  SubscriptionsReport,
} from '@/types'

const props = defineProps<{
  commissions: CommissionSummary | null
  subscriptionsReport: SubscriptionsReport | null
  deliveriesReport: DeliveriesReport | null
  payouts: Payout[]
  auditLogs: AuditLog[]
  restaurants: Restaurant[]
  couriers: Courier[]
}>()

const emit = defineEmits<{
  (e: 'refresh'): void
}>()

const toast = useToastStore()
const processingPayoutId = ref<string | null>(null)
const auditSearch = ref('')

const filteredAuditLogs = computed(() => {
  const q = auditSearch.value.trim().toLowerCase()
  if (!q) return props.auditLogs.slice(0, 40)
  return props.auditLogs
    .filter(
      (log) =>
        log.action.toLowerCase().includes(q) ||
        log.details.toLowerCase().includes(q) ||
        (log.entityName && log.entityName.toLowerCase().includes(q)),
    )
    .slice(0, 40)
})

function getTargetName(targetId: string): string {
  const foundRest = props.restaurants.find((r) => r.id === targetId)
  if (foundRest) return `${foundRest.name} (Restaurante)`
  const foundCourier = props.couriers.find((c) => c.id === targetId)
  if (foundCourier) return `${foundCourier.name} (Entregador)`
  return targetId
}

async function handleProcessPayout(payout: Payout) {
  processingPayoutId.value = payout.id
  try {
    const { data } = await api.post<Payout>(`/admin/financeiro/repasses/${payout.id}/processar`)
    payout.status = data.status
    payout.processedAt = data.processedAt
    toast.success('Repasse processado com sucesso!')
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível processar o repasse.'))
  } finally {
    processingPayoutId.value = null
  }
}
</script>

<template>
  <div class="space-y-6">
    <!-- Financial Summaries -->
    <div class="grid gap-4 sm:grid-cols-3">
      <AppCard padding="md">
        <span class="text-xs font-bold uppercase tracking-wider text-ink-muted">
          Comissões ({{ commissions?.commissionRatePercentage ?? 10 }}%)
        </span>
        <p class="mt-2 font-display text-2xl font-extrabold text-fresh-700 dark:text-fresh-400 tabular-nums">
          {{ formatCurrency(commissions?.totalCommission ?? 0) }}
        </p>
        <p class="mt-1 text-xs text-ink-muted tabular-nums">
          Volume bruto: {{ formatCurrency(commissions?.grossVolume ?? 0) }}
        </p>
      </AppCard>

      <AppCard padding="md">
        <span class="text-xs font-bold uppercase tracking-wider text-ink-muted">
          Assinaturas (MRR)
        </span>
        <p class="mt-2 font-display text-2xl font-extrabold text-ink tabular-nums">
          {{ formatCurrency(subscriptionsReport?.monthlyRecurringRevenue ?? 0) }}
        </p>
        <p class="mt-1 text-xs text-ink-muted tabular-nums">
          Ativas: {{ subscriptionsReport?.totalActiveSubscriptions ?? 0 }} · Canceladas: {{ subscriptionsReport?.totalCancelledSubscriptions ?? 0 }}
        </p>
      </AppCard>

      <AppCard padding="md">
        <span class="text-xs font-bold uppercase tracking-wider text-ink-muted">
          Entregas
        </span>
        <p class="mt-2 font-display text-2xl font-extrabold text-ink tabular-nums">
          {{ deliveriesReport?.totalDeliveries ?? 0 }} concluídas
        </p>
        <p class="mt-1 text-xs text-ink-muted tabular-nums">
          Em rota: {{ deliveriesReport?.inTransitDeliveries ?? 0 }} · Canceladas: {{ deliveriesReport?.cancelledDeliveries ?? 0 }}
        </p>
      </AppCard>
    </div>

    <div class="grid gap-6 lg:grid-cols-2 items-start">
      <!-- Payouts -->
      <AppCard padding="lg" class="space-y-4">
        <div>
          <h2 class="font-display text-base font-extrabold text-ink">Repasses Financeiros (Payouts)</h2>
          <p class="text-xs text-ink-muted">Valores líquidos (90%) prontos para repasse em 1 clique.</p>
        </div>

        <div v-if="payouts.length > 0" class="space-y-3">
          <div
            v-for="payout in payouts"
            :key="payout.id"
            class="flex flex-col justify-between gap-3 rounded-2xl border border-line bg-surface-subtle p-3.5 sm:flex-row sm:items-center"
          >
            <div>
              <p class="font-display text-sm font-bold text-ink">{{ getTargetName(payout.targetId) }}</p>
              <p class="text-xs text-ink-secondary tabular-nums">
                Bruto: {{ formatCurrency(payout.grossAmount) }} · Líquido:
                <strong class="text-ink">{{ formatCurrency(payout.netAmount) }}</strong>
              </p>
            </div>

            <div>
              <AppButton
                v-if="payout.status !== 'Processed'"
                variant="primary"
                size="xs"
                :loading="processingPayoutId === payout.id"
                @click="handleProcessPayout(payout)"
              >
                Processar repasse
              </AppButton>
              <OrderStatusBadge v-else status="Processed" />
            </div>
          </div>
        </div>
        <p v-else class="py-8 text-center text-xs text-ink-muted">
          Nenhum repasse pendente ou registrado.
        </p>
      </AppCard>

      <!-- Searchable Audit Log -->
      <AppCard padding="lg" class="space-y-4">
        <div class="flex flex-col justify-between gap-2 sm:flex-row sm:items-center">
          <div>
            <h2 class="font-display text-base font-extrabold text-ink">Trilha de Auditoria</h2>
            <p class="text-xs text-ink-muted">Eventos críticos da plataforma em ordem cronológica.</p>
          </div>
          <div class="relative w-full sm:w-52">
            <Search class="pointer-events-none absolute left-3 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-ink-muted" />
            <input
              v-model="auditSearch"
              type="search"
              placeholder="Filtrar auditoria..."
              class="w-full rounded-xl border border-line-strong bg-surface py-1.5 pl-8 pr-3 text-xs text-ink"
            />
          </div>
        </div>

        <ul
          v-if="filteredAuditLogs.length > 0"
          class="max-h-96 divide-y divide-line overflow-y-auto pr-1 text-xs"
        >
          <li v-for="log in filteredAuditLogs" :key="log.id" class="py-3">
            <div class="flex items-center justify-between gap-2">
              <span class="font-bold text-ink">{{ log.action }}</span>
              <span class="shrink-0 text-[11px] text-ink-muted tabular-nums">
                {{ new Date(log.timestamp).toLocaleString('pt-BR') }}
              </span>
            </div>
            <p class="mt-1 text-ink-secondary leading-relaxed">{{ log.details }}</p>
          </li>
        </ul>
        <p v-else class="py-8 text-center text-xs text-ink-muted">
          Nenhum evento de auditoria encontrado.
        </p>
      </AppCard>
    </div>
  </div>
</template>
