<script setup lang="ts">
import { computed, ref } from 'vue'
import { ArrowRight, CalendarDays, Check, Minus, Plus, Sparkles, Trash2 } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppModal from '@/components/ui/AppModal.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppEmptyState from '@/components/ui/AppEmptyState.vue'
import AppConfirmDialog from '@/components/ui/AppConfirmDialog.vue'
import OrderStatusBadge from '@/components/ui/OrderStatusBadge.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import type { Plan, Subscription, SubscriptionDelivery } from '@/types'

const props = defineProps<{
  subscriptions: Subscription[]
  availablePlans: Plan[]
  scheduledBySub: Record<string, SubscriptionDelivery[]>
}>()

const emit = defineEmits<{
  (e: 'refresh'): void
  (e: 'refresh-schedules', subscriptionId: string): void
}>()

const toast = useToastStore()

const busySubId = ref<string | null>(null)
const cancelTargetSub = ref<Subscription | null>(null)

// 14-day Meal Planner Modal
const scheduleModalOpen = ref(false)
const scheduleSubId = ref('')
const scheduleDateIso = ref('')
const scheduleDateLabel = ref('')
const scheduleWindow = ref('11:30 - 13:30')
const scheduleMealsCount = ref(1)

const windowPresets = ['11:30 - 13:30', '12:00 - 14:00', '18:30 - 20:30', '19:30 - 21:30']

// Side-by-side Plan Comparator Modal
const compareModalOpen = ref(false)
const compareSub = ref<Subscription | null>(null)
const selectedNewPlanId = ref('')

const currentPlanObj = computed(() => {
  if (!compareSub.value) return null
  return props.availablePlans.find((p) => p.id === compareSub.value?.planId) ?? null
})

const targetPlanObj = computed(() => {
  if (!selectedNewPlanId.value) return null
  return props.availablePlans.find((p) => p.id === selectedNewPlanId.value) ?? null
})

const next14Days = computed(() => {
  const days: Array<{ iso: string; dayNum: string; weekday: string; month: string; isToday: boolean }> = []
  const now = new Date()
  for (let i = 0; i < 14; i++) {
    const d = new Date(now.getFullYear(), now.getMonth(), now.getDate() + i)
    const yyyy = d.getFullYear()
    const mm = String(d.getMonth() + 1).padStart(2, '0')
    const dd = String(d.getDate()).padStart(2, '0')
    days.push({
      iso: `${yyyy}-${mm}-${dd}`,
      dayNum: dd,
      weekday: d.toLocaleDateString('pt-BR', { weekday: 'short' }).replace('.', ''),
      month: d.toLocaleDateString('pt-BR', { month: 'short' }).replace('.', ''),
      isToday: i === 0,
    })
  }
  return days
})

function getSchedulesForDay(subId: string, dayIso: string): SubscriptionDelivery[] {
  const list = props.scheduledBySub[subId] || []
  return list.filter((item) => item.scheduledDate.slice(0, 10) === dayIso)
}

function getUsagePercent(sub: Subscription): number {
  if (!sub.totalMealsInCycle || sub.totalMealsInCycle <= 0) return 0
  return Math.min(100, Math.round(((sub.usedMealsInCycle ?? 0) / sub.totalMealsInCycle) * 100))
}

function getPlanPerMeal(plan: Plan): number {
  const monthlyMeals =
    plan.mealsPerMonth && plan.mealsPerMonth > 0 ? plan.mealsPerMonth : Math.max(1, plan.mealsPerWeek * 4)
  return plan.monthlyPrice / monthlyMeals
}

function openDayScheduleModal(subId: string, day: { iso: string; dayNum: string; weekday: string; month: string }) {
  scheduleSubId.value = subId
  scheduleDateIso.value = day.iso
  scheduleDateLabel.value = `${day.weekday}, ${day.dayNum} de ${day.month}`
  scheduleWindow.value = '11:30 - 13:30'
  scheduleMealsCount.value = 1
  scheduleModalOpen.value = true
}

function openCompareModal(sub: Subscription) {
  compareSub.value = sub
  const alternate = props.availablePlans.find((p) => p.id !== sub.planId) || props.availablePlans[0]
  selectedNewPlanId.value = alternate?.id || sub.planId
  compareModalOpen.value = true
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

async function handleConfirmChangePlan() {
  if (!compareSub.value || !selectedNewPlanId.value) return
  if (selectedNewPlanId.value === compareSub.value.planId) {
    toast.info('Selecione um plano diferente do atual para alterar.')
    return
  }

  busySubId.value = compareSub.value.id
  try {
    await api.patch(`/assinaturas/${compareSub.value.id}/alterar-plano`, {
      newPlanId: selectedNewPlanId.value,
    })
    compareModalOpen.value = false
    toast.success('Plano alterado com cálculo proporcional (prorrata)!')
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível alterar o plano.'))
  } finally {
    busySubId.value = null
  }
}

async function handleConfirmSchedule() {
  if (!scheduleSubId.value || !scheduleDateIso.value) return
  busySubId.value = scheduleSubId.value
  try {
    await api.post(`/assinaturas/${scheduleSubId.value}/agendamentos`, {
      scheduledDate: new Date(`${scheduleDateIso.value}T12:00:00`).toISOString(),
      deliveryWindow: scheduleWindow.value,
      mealsCount: scheduleMealsCount.value,
    })
    scheduleModalOpen.value = false
    toast.success('Entrega agendada no seu Meal Planner!')
    emit('refresh-schedules', scheduleSubId.value)
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
  <div class="space-y-6">
    <div v-if="subscriptions.length > 0" class="space-y-6">
      <AppCard v-for="sub in subscriptions" :key="sub.id" padding="lg" class="space-y-6">
        <!-- Header -->
        <div class="flex flex-col justify-between gap-3 border-b border-line pb-4 sm:flex-row sm:items-center">
          <div>
            <div class="flex flex-wrap items-center gap-2">
              <h3 class="font-display text-lg font-extrabold text-ink">{{ sub.planName }}</h3>
              <OrderStatusBadge :status="sub.status" />
            </div>
            <p class="mt-1 text-xs text-ink-muted">
              Próxima cobrança: <strong class="text-ink">{{ new Date(sub.nextBillingDate).toLocaleDateString('pt-BR') }}</strong>
            </p>
          </div>

          <div class="flex items-center gap-3">
            <div class="text-left sm:text-right">
              <p class="font-display text-2xl font-extrabold text-ink tabular-nums">
                {{ formatCurrency(sub.monthlyPrice) }}
                <span class="text-xs font-normal text-ink-muted">/mês</span>
              </p>
            </div>
            <AppButton
              v-if="sub.status === 'Active' && availablePlans.length > 1"
              variant="outline"
              size="sm"
              @click="openCompareModal(sub)"
            >
              Trocar plano
            </AppButton>
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

        <!-- Visual 14-Day Weekly Meal Planner Calendar -->
        <div v-if="sub.status === 'Active'" class="space-y-3">
          <div class="flex flex-wrap items-center justify-between gap-2">
            <div class="flex items-center gap-2">
              <CalendarDays class="h-4 w-4 text-brand-600" />
              <h4 class="font-display text-sm font-extrabold text-ink">
                Meal Planner — Próximos 14 dias
              </h4>
            </div>
            <span class="text-[11px] text-ink-muted">
              Clique em qualquer dia para agendar sua entrega
            </span>
          </div>

          <div class="grid grid-cols-2 sm:grid-cols-4 md:grid-cols-7 gap-2">
            <div
              v-for="day in next14Days"
              :key="day.iso"
              class="flex flex-col justify-between rounded-2xl border p-2.5 min-h-[104px] transition"
              :class="
                getSchedulesForDay(sub.id, day.iso).length > 0
                  ? 'border-fresh-300 bg-fresh-50/70 dark:border-fresh-800/60 dark:bg-fresh-950/30'
                  : day.isToday
                    ? 'border-brand-300 bg-brand-50/30 dark:border-brand-800/50 dark:bg-brand-950/20'
                    : 'border-line bg-surface-subtle hover:border-line-strong'
              "
            >
              <div class="flex items-center justify-between text-[11px]">
                <span class="font-bold uppercase text-ink-muted">{{ day.weekday }}</span>
                <span
                  class="flex h-6 w-6 items-center justify-center rounded-lg font-display font-extrabold tabular-nums"
                  :class="day.isToday ? 'bg-brand-600 text-white' : 'text-ink'"
                >
                  {{ day.dayNum }}
                </span>
              </div>

              <!-- Scheduled Items on Day -->
              <div v-if="getSchedulesForDay(sub.id, day.iso).length > 0" class="mt-2 space-y-1">
                <div
                  v-for="sched in getSchedulesForDay(sub.id, day.iso)"
                  :key="sched.id"
                  class="rounded-xl bg-surface p-1.5 border border-fresh-200 text-[10px] dark:border-fresh-800/50"
                >
                  <div class="flex items-center justify-between font-bold text-fresh-700 dark:text-fresh-400">
                    <span>{{ sched.mealsCount }} ref.</span>
                    <button
                      type="button"
                      aria-label="Cancelar agendamento"
                      class="text-red-500 hover:text-red-700"
                      @click="handleCancelSchedule(sub.id, sched.id)"
                    >
                      <Trash2 class="h-3 w-3" />
                    </button>
                  </div>
                  <p class="truncate text-[9px] text-ink-muted">{{ sched.deliveryWindow }}</p>
                </div>
              </div>

              <!-- Empty Day Quick Add -->
              <button
                v-else
                type="button"
                class="mt-2 flex items-center justify-center gap-1 rounded-xl border border-dashed border-line-strong py-1.5 text-[11px] font-semibold text-ink-secondary transition hover:border-brand-500 hover:bg-surface hover:text-brand-600"
                @click="openDayScheduleModal(sub.id, day)"
              >
                <Plus class="h-3 w-3" />
                Agendar
              </button>
            </div>
          </div>
        </div>

        <!-- Subscription Lifecycle Actions -->
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

    <!-- Schedule Delivery Day Modal -->
    <AppModal
      :open="scheduleModalOpen"
      title="Agendar entrega do ciclo"
      :subtitle="scheduleDateLabel"
      max-width="sm"
      @close="scheduleModalOpen = false"
    >
      <div class="space-y-4">
        <div class="space-y-2">
          <label class="block text-xs font-bold text-ink-secondary">Janela de entrega</label>
          <div class="grid grid-cols-2 gap-2">
            <button
              v-for="win in windowPresets"
              :key="win"
              type="button"
              class="rounded-xl border px-3 py-2 text-xs font-bold transition"
              :class="
                scheduleWindow === win
                  ? 'border-brand-600 bg-brand-600 text-white'
                  : 'border-line bg-surface-subtle text-ink-secondary hover:bg-surface-muted'
              "
              @click="scheduleWindow = win"
            >
              {{ win }}
            </button>
          </div>
        </div>

        <div class="flex items-center justify-between rounded-2xl border border-line bg-surface-subtle p-3.5">
          <div>
            <p class="text-xs font-bold text-ink">Quantidade de marmitas</p>
            <p class="text-[11px] text-ink-muted">Descontado da franquia do ciclo</p>
          </div>
          <div class="inline-flex items-center gap-2 rounded-xl border border-line bg-surface p-1">
            <button
              type="button"
              class="flex h-8 w-8 items-center justify-center rounded-lg text-ink-secondary hover:bg-surface-muted"
              @click="scheduleMealsCount = Math.max(1, scheduleMealsCount - 1)"
            >
              <Minus class="h-3.5 w-3.5" />
            </button>
            <span class="min-w-[1.5rem] text-center font-display text-sm font-extrabold text-ink tabular-nums">
              {{ scheduleMealsCount }}
            </span>
            <button
              type="button"
              class="flex h-8 w-8 items-center justify-center rounded-lg text-brand-600 hover:bg-brand-50"
              @click="scheduleMealsCount = Math.min(5, scheduleMealsCount + 1)"
            >
              <Plus class="h-3.5 w-3.5" />
            </button>
          </div>
        </div>
      </div>

      <template #footer>
        <div class="flex justify-end gap-2">
          <AppButton variant="outline" size="sm" @click="scheduleModalOpen = false">Cancelar</AppButton>
          <AppButton
            variant="fresh"
            size="sm"
            :loading="Boolean(busySubId)"
            @click="handleConfirmSchedule"
          >
            Confirmar agendamento
          </AppButton>
        </div>
      </template>
    </AppModal>

    <!-- Side-by-Side Upgrade / Downgrade Plan Comparator Modal -->
    <AppModal
      :open="compareModalOpen"
      title="Comparar & Alterar Plano"
      subtitle="A alteração aplica cálculo proporcional (prorrata) automático no ciclo atual."
      max-width="xl"
      @close="compareModalOpen = false"
    >
      <div class="space-y-4">
        <div class="space-y-1.5">
          <label class="block text-xs font-bold text-ink-secondary">Escolha o novo plano</label>
          <select
            v-model="selectedNewPlanId"
            class="w-full rounded-xl border border-line-strong bg-surface px-3.5 py-2.5 text-sm font-semibold text-ink"
          >
            <option
              v-for="p in availablePlans"
              :key="p.id"
              :value="p.id"
              :disabled="p.id === compareSub?.planId"
            >
              {{ p.name }} — {{ formatCurrency(p.monthlyPrice) }}/mês ({{ p.mealsPerWeek }}/sem)
            </option>
          </select>
        </div>

        <div v-if="currentPlanObj && targetPlanObj" class="grid gap-4 sm:grid-cols-2">
          <!-- Current Plan -->
          <div class="rounded-2xl border border-line bg-surface-subtle p-4 space-y-2">
            <AppBadge variant="neutral" size="xs">Plano Atual</AppBadge>
            <h4 class="font-display text-base font-extrabold text-ink">{{ currentPlanObj.name }}</h4>
            <p class="font-display text-2xl font-extrabold text-ink tabular-nums">
              {{ formatCurrency(currentPlanObj.monthlyPrice) }}<span class="text-xs font-normal text-ink-muted">/mês</span>
            </p>
            <ul class="space-y-1 text-xs text-ink-secondary pt-1">
              <li class="flex items-center gap-1.5">
                <Check class="h-3.5 w-3.5 text-ink-muted" />
                {{ currentPlanObj.mealsPerWeek }} marmitas/semana
              </li>
              <li class="flex items-center gap-1.5">
                <Check class="h-3.5 w-3.5 text-ink-muted" />
                {{ formatCurrency(getPlanPerMeal(currentPlanObj)) }} por marmita
              </li>
            </ul>
          </div>

          <!-- New Plan -->
          <div class="rounded-2xl border-2 border-fresh-500 bg-fresh-50/50 p-4 space-y-2 dark:bg-fresh-950/30">
            <AppBadge variant="fresh" size="xs">Novo Plano</AppBadge>
            <h4 class="font-display text-base font-extrabold text-ink">{{ targetPlanObj.name }}</h4>
            <p class="font-display text-2xl font-extrabold text-fresh-700 dark:text-fresh-400 tabular-nums">
              {{ formatCurrency(targetPlanObj.monthlyPrice) }}<span class="text-xs font-normal text-ink-muted">/mês</span>
            </p>
            <ul class="space-y-1 text-xs text-ink-secondary pt-1">
              <li class="flex items-center gap-1.5">
                <Check class="h-3.5 w-3.5 text-fresh-600" />
                {{ targetPlanObj.mealsPerWeek }} marmitas/semana
              </li>
              <li class="flex items-center gap-1.5">
                <Check class="h-3.5 w-3.5 text-fresh-600" />
                {{ formatCurrency(getPlanPerMeal(targetPlanObj)) }} por marmita
              </li>
            </ul>
          </div>
        </div>
      </div>

      <template #footer>
        <div class="flex justify-end gap-2">
          <AppButton variant="outline" size="sm" @click="compareModalOpen = false">Cancelar</AppButton>
          <AppButton
            variant="fresh"
            size="sm"
            :disabled="selectedNewPlanId === compareSub?.planId"
            :loading="Boolean(busySubId)"
            @click="handleConfirmChangePlan"
          >
            <span>Confirmar troca de plano</span>
            <ArrowRight class="h-4 w-4" />
          </AppButton>
        </div>
      </template>
    </AppModal>

    <AppConfirmDialog
      :open="Boolean(cancelTargetSub)"
      title="Cancelar assinatura?"
      :message="`Tem certeza que deseja cancelar o plano '${cancelTargetSub?.planName}'?`"
      confirm-label="Sim, cancelar"
      variant="danger"
      :loading="Boolean(busySubId)"
      @confirm="cancelTargetSub && handleAction(cancelTargetSub, 'cancelar')"
      @cancel="cancelTargetSub = null"
    />
  </div>
</template>
