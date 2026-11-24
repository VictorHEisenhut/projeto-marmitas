<script setup lang="ts">
import { computed, ref } from 'vue'
import { CalendarDays, Sparkles, Store, Trash2 } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppEmptyState from '@/components/ui/AppEmptyState.vue'
import AppConfirmDialog from '@/components/ui/AppConfirmDialog.vue'
import OrderStatusBadge from '@/components/ui/OrderStatusBadge.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import type { Plan, Restaurant, Subscription, SubscriptionDelivery } from '@/types'

const props = defineProps<{
  subscriptions: Subscription[]
  availablePlans: Plan[]
  restaurants?: Restaurant[]
  scheduledBySub: Record<string, SubscriptionDelivery[]>
}>()

const restaurantNameById = computed(() => {
  const map: Record<string, string> = {}
  for (const r of props.restaurants || []) {
    map[r.id] = r.name
  }
  return map
})

function getSubscriptionRestaurantId(sub: Subscription): string {
  return sub.restaurantId || sub.preferredRestaurantId || ''
}

function getSubscriptionRestaurantName(sub: Subscription): string {
  const rid = getSubscriptionRestaurantId(sub)
  return sub.restaurantName || (rid ? restaurantNameById.value[rid] : '') || 'Restaurante parceiro'
}

function getEligiblePlans(sub: Subscription): Plan[] {
  const rid = getSubscriptionRestaurantId(sub)
  if (!rid) return props.availablePlans
  return props.availablePlans.filter((p) => p.restaurantId === rid)
}

const emit = defineEmits<{
  (e: 'refresh'): void
  (e: 'refresh-schedules', subscriptionId: string): void
}>()

const toast = useToastStore()

const planSelectionBySub = ref<Record<string, string>>({})
const scheduleDateBySub = ref<Record<string, string>>({})
const scheduleWindowBySub = ref<Record<string, string>>({})
const scheduleMealsBySub = ref<Record<string, number>>({})
const busySubId = ref<string | null>(null)
const cancelTargetSub = ref<Subscription | null>(null)

function getSelectedPlan(sub: Subscription): string {
  return planSelectionBySub.value[sub.id] ?? sub.planId
}

function getWindow(subId: string): string {
  return scheduleWindowBySub.value[subId] ?? '11:30 - 13:30'
}

function getMealsCount(subId: string): number {
  return scheduleMealsBySub.value[subId] ?? 1
}

function getUsagePercent(sub: Subscription): number {
  if (!sub.totalMealsInCycle || sub.totalMealsInCycle <= 0) return 0
  return Math.min(100, Math.round(((sub.usedMealsInCycle ?? 0) / sub.totalMealsInCycle) * 100))
}

async function handleAction(sub: Subscription, action: 'pausar' | 'reativar' | 'cancelar') {
  busySubId.value = sub.id
  try {
    await api.patch(`/assinaturas/${sub.id}/${action}`)
    cancelTargetSub.value = null
    toast.success(
      action === 'pausar'
        ? 'Assinatura pausada.'
        : action === 'reativar'
          ? 'Assinatura reativada!'
          : 'Assinatura cancelada.',
    )
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível atualizar a assinatura.'))
  } finally {
    busySubId.value = null
  }
}

async function handleChangePlan(sub: Subscription) {
  const newPlanId = getSelectedPlan(sub)
  if (!newPlanId || newPlanId === sub.planId) {
    toast.info('Selecione um plano diferente do atual para alterar.')
    return
  }

  busySubId.value = sub.id
  try {
    await api.patch(`/assinaturas/${sub.id}/alterar-plano`, { newPlanId })
    toast.success('Plano alterado com cálculo proporcional (prorrata)!')
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível alterar o plano.'))
  } finally {
    busySubId.value = null
  }
}

async function handleScheduleDelivery(sub: Subscription) {
  const scheduledDate = scheduleDateBySub.value[sub.id]
  const deliveryWindow = getWindow(sub.id)
  const mealsCount = getMealsCount(sub.id)

  busySubId.value = sub.id
  try {
    await api.post(`/assinaturas/${sub.id}/agendamentos`, {
      scheduledDate: scheduledDate ? new Date(scheduledDate).toISOString() : undefined,
      deliveryWindow,
      mealsCount,
    })
    scheduleDateBySub.value[sub.id] = ''
    toast.success('Entrega da assinatura agendada!')
    emit('refresh-schedules', sub.id)
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível agendar a entrega.'))
  } finally {
    busySubId.value = null
  }
}

async function handleCancelSchedule(subId: string, deliveryId: string) {
  try {
    await api.delete(`/assinaturas/${subId}/agendamentos/${deliveryId}`)
    toast.info('Agendamento cancelado.')
    emit('refresh-schedules', subId)
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível cancelar o agendamento.'))
  }
}
</script>

<template>
  <div class="space-y-5">
    <div v-if="subscriptions.length > 0" class="grid gap-5">
      <AppCard v-for="sub in subscriptions" :key="sub.id" padding="lg" class="space-y-5">
        <!-- Header -->
        <div class="flex flex-col justify-between gap-3 border-b border-line pb-4 sm:flex-row sm:items-start">
          <div>
            <div class="flex flex-wrap items-center gap-2">
              <h3 class="font-display text-lg font-extrabold text-ink">{{ sub.planName }}</h3>
              <OrderStatusBadge :status="sub.status" />
              <RouterLink
                v-if="getSubscriptionRestaurantId(sub)"
                :to="`/restaurantes/${getSubscriptionRestaurantId(sub)}`"
              >
                <AppBadge variant="brand" size="xs">
                  <Store class="h-3 w-3 mr-1" />
                  {{ getSubscriptionRestaurantName(sub) }}
                </AppBadge>
              </RouterLink>
            </div>
            <p class="mt-1 text-xs text-ink-muted">
              Próxima cobrança: <strong class="text-ink">{{ new Date(sub.nextBillingDate).toLocaleDateString('pt-BR') }}</strong>
            </p>
          </div>

          <div class="text-left sm:text-right">
            <p class="font-display text-2xl font-extrabold text-ink tabular-nums">
              {{ formatCurrency(sub.monthlyPrice) }}
              <span class="text-xs font-normal text-ink-muted">/mês</span>
            </p>
          </div>
        </div>

        <!-- Cycle Franchise Progress Bar -->
        <div
          v-if="sub.totalMealsInCycle !== undefined"
          class="rounded-2xl border border-line bg-surface-subtle p-4 space-y-2"
        >
          <div class="flex items-center justify-between text-xs">
            <span class="font-semibold text-ink-secondary">Franquia utilizada no ciclo</span>
            <span class="font-display font-extrabold text-ink tabular-nums">
              {{ sub.usedMealsInCycle ?? 0 }} / {{ sub.totalMealsInCycle }} refeições
            </span>
          </div>
          <div class="h-2.5 w-full overflow-hidden rounded-full bg-surface-muted">
            <div
              class="h-full rounded-full bg-fresh-600 transition-all duration-300"
              :style="{ width: `${getUsagePercent(sub)}%` }"
            />
          </div>
        </div>

        <!-- Upgrade / Downgrade + Delivery Scheduler -->
        <div v-if="sub.status === 'Active'" class="grid gap-4 lg:grid-cols-2">
          <!-- Upgrade/Downgrade -->
          <div
            v-if="getEligiblePlans(sub).length > 1"
            class="rounded-2xl border border-line bg-surface-subtle p-4 space-y-3"
          >
            <div>
              <p class="text-xs font-bold text-ink">Alterar plano em {{ getSubscriptionRestaurantName(sub) }}</p>
              <p class="text-[11px] text-ink-muted">Mudança entre planos do mesmo restaurante com cálculo prorrata.</p>
            </div>
            <div class="flex gap-2">
              <select
                :value="getSelectedPlan(sub)"
                class="flex-1 rounded-xl border border-line-strong bg-surface px-3 py-2 text-xs font-medium text-ink focus:border-brand-600 focus:outline-none"
                @change="planSelectionBySub[sub.id] = ($event.target as HTMLSelectElement).value"
              >
                <option v-for="p in getEligiblePlans(sub)" :key="p.id" :value="p.id">
                  {{ p.name }} — {{ formatCurrency(p.monthlyPrice) }}/mês
                </option>
              </select>
              <AppButton
                variant="secondary"
                size="sm"
                :loading="busySubId === sub.id"
                @click="handleChangePlan(sub)"
              >
                Trocar
              </AppButton>
            </div>
          </div>
          <div
            v-else
            class="rounded-2xl border border-line bg-surface-subtle p-4 flex items-center text-xs text-ink-muted"
          >
            Este é atualmente o único plano ativo em {{ getSubscriptionRestaurantName(sub) }}.
          </div>

          <!-- Cycle Delivery Scheduler -->
          <div class="rounded-2xl border border-line bg-surface-subtle p-4 space-y-3">
            <div class="flex items-center gap-1.5">
              <CalendarDays class="h-4 w-4 text-brand-600" />
              <p class="text-xs font-bold text-ink">Programar entregas do ciclo</p>
            </div>
            <div class="grid grid-cols-2 sm:grid-cols-4 gap-2">
              <input
                v-model="scheduleDateBySub[sub.id]"
                type="date"
                class="col-span-2 sm:col-span-1 rounded-xl border border-line-strong bg-surface px-2.5 py-2 text-xs text-ink"
              />
              <input
                :value="getWindow(sub.id)"
                type="text"
                placeholder="11:30 - 13:30"
                class="rounded-xl border border-line-strong bg-surface px-2.5 py-2 text-xs text-ink"
                @input="scheduleWindowBySub[sub.id] = ($event.target as HTMLInputElement).value"
              />
              <input
                :value="getMealsCount(sub.id)"
                type="number"
                min="1"
                max="10"
                class="rounded-xl border border-line-strong bg-surface px-2.5 py-2 text-xs text-ink"
                @input="scheduleMealsBySub[sub.id] = Number(($event.target as HTMLInputElement).value) || 1"
              />
              <AppButton
                variant="primary"
                size="sm"
                :loading="busySubId === sub.id"
                @click="handleScheduleDelivery(sub)"
              >
                Agendar
              </AppButton>
            </div>

            <ul
              v-if="scheduledBySub[sub.id]?.length"
              class="divide-y divide-line border-t border-line pt-2 text-xs"
            >
              <li
                v-for="item in scheduledBySub[sub.id]"
                :key="item.id"
                class="flex items-center justify-between py-2"
              >
                <span class="text-ink-secondary">
                  <strong class="text-ink">{{ new Date(item.scheduledDate).toLocaleDateString('pt-BR') }}</strong>
                  · {{ item.deliveryWindow }} ({{ item.mealsCount }} ref.)
                </span>
                <button
                  type="button"
                  aria-label="Cancelar agendamento"
                  class="inline-flex items-center gap-1 text-[11px] font-semibold text-red-600 hover:underline dark:text-red-400"
                  @click="handleCancelSchedule(sub.id, item.id)"
                >
                  <Trash2 class="h-3.5 w-3.5" />
                  Cancelar
                </button>
              </li>
            </ul>
          </div>
        </div>

        <!-- Subscription Controls -->
        <div class="flex flex-wrap items-center gap-2 border-t border-line pt-4">
          <AppButton
            v-if="sub.status === 'Active'"
            variant="outline"
            size="sm"
            :loading="busySubId === sub.id"
            @click="handleAction(sub, 'pausar')"
          >
            Pausar assinatura
          </AppButton>
          <AppButton
            v-if="sub.status === 'Paused'"
            variant="fresh"
            size="sm"
            :loading="busySubId === sub.id"
            @click="handleAction(sub, 'reativar')"
          >
            Reativar assinatura
          </AppButton>
          <AppButton
            v-if="sub.status !== 'Cancelled'"
            variant="ghost"
            size="sm"
            class="text-red-600 hover:bg-red-50 dark:text-red-400 dark:hover:bg-red-950/40 ml-auto"
            @click="cancelTargetSub = sub"
          >
            Cancelar assinatura
          </AppButton>
        </div>
      </AppCard>
    </div>

    <AppEmptyState
      v-else
      :icon="Sparkles"
      title="Você ainda não possui assinaturas"
      description="Assine o Clube de Marmitas e tenha refeições frescas toda semana com desconto exclusivo."
    >
      <template #action>
        <RouterLink to="/">
          <AppButton variant="fresh" size="sm">Ver planos disponíveis</AppButton>
        </RouterLink>
      </template>
    </AppEmptyState>

    <AppConfirmDialog
      :open="Boolean(cancelTargetSub)"
      title="Cancelar assinatura?"
      :message="`Tem certeza que deseja cancelar o plano '${cancelTargetSub?.planName}'? Você perderá o resgate semanal ao fim do ciclo.`"
      confirm-label="Sim, cancelar"
      variant="danger"
      :loading="Boolean(busySubId)"
      @confirm="cancelTargetSub && handleAction(cancelTargetSub, 'cancelar')"
      @cancel="cancelTargetSub = null"
    />
  </div>
</template>
