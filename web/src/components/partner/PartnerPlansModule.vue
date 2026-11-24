<script setup lang="ts">
import { ref } from 'vue'
import { CalendarDays, Clock, Edit3, Plus, Sparkles, Trash2 } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppInput from '@/components/ui/AppInput.vue'
import AppDrawer from '@/components/ui/AppDrawer.vue'
import AppEmptyState from '@/components/ui/AppEmptyState.vue'
import AppConfirmDialog from '@/components/ui/AppConfirmDialog.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import type { Plan } from '@/types'

const props = defineProps<{
  restaurantId: string
  restaurantName: string
  plans: Plan[]
}>()

const emit = defineEmits<{
  (e: 'refresh'): void
}>()

const toast = useToastStore()

const drawerOpen = ref(false)
const editingPlanId = ref<string | null>(null)
const name = ref('')
const description = ref('')
const mealsPerWeek = ref(5)
const mealsPerMonth = ref<number | null>(20)
const monthlyPrice = ref<number | null>(null)
const allowedDays = ref('Segunda a Sexta')
const deliveryWindow = ref('11:00 às 14:30')
const deliveryFee = ref<number>(0)
const cancellationPolicy = ref('Cancelamento livre até o fim do ciclo vigente.')
const customMonthlyMeals = ref(false)

const savingPlan = ref(false)
const deleteTarget = ref<Plan | null>(null)

function getMealsPerMonth(plan: Plan): number {
  return plan.mealsPerMonth && plan.mealsPerMonth > 0
    ? plan.mealsPerMonth
    : Math.max(1, plan.mealsPerWeek * 4)
}

function getPricePerMeal(plan: Plan): number {
  return plan.monthlyPrice / getMealsPerMonth(plan)
}

function onWeeklyMealsChange(val: number) {
  mealsPerWeek.value = val
  if (!customMonthlyMeals.value && val > 0) {
    mealsPerMonth.value = val * 4
  }
}

function openCreateDrawer() {
  editingPlanId.value = null
  name.value = ''
  description.value = ''
  mealsPerWeek.value = 5
  mealsPerMonth.value = 20
  monthlyPrice.value = null
  allowedDays.value = 'Segunda a Sexta'
  deliveryWindow.value = '11:00 às 14:30'
  deliveryFee.value = 0
  cancellationPolicy.value = 'Cancelamento livre até o fim do ciclo vigente.'
  customMonthlyMeals.value = false
  drawerOpen.value = true
}

function openEditDrawer(plan: Plan) {
  editingPlanId.value = plan.id
  name.value = plan.name
  description.value = plan.description || ''
  mealsPerWeek.value = plan.mealsPerWeek
  mealsPerMonth.value = getMealsPerMonth(plan)
  monthlyPrice.value = plan.monthlyPrice
  allowedDays.value = plan.allowedDays || 'Segunda a Sexta'
  deliveryWindow.value = plan.deliveryWindow || '11:00 às 14:30'
  deliveryFee.value = plan.deliveryFee ?? 0
  cancellationPolicy.value = plan.cancellationPolicy || 'Cancelamento livre até o fim do ciclo vigente.'
  customMonthlyMeals.value = true
  drawerOpen.value = true
}

async function handleSavePlan() {
  if (!name.value.trim()) {
    toast.warning('Informe o nome do plano.')
    return
  }
  if (!monthlyPrice.value || monthlyPrice.value <= 0) {
    toast.warning('Informe um valor mensal válido maior que zero.')
    return
  }
  if (!mealsPerWeek.value || mealsPerWeek.value <= 0) {
    toast.warning('Informe a quantidade de marmitas por semana.')
    return
  }

  savingPlan.value = true
  try {
    const payload = {
      restaurantId: props.restaurantId,
      name: name.value.trim(),
      description: description.value.trim(),
      mealsPerWeek: Number(mealsPerWeek.value),
      mealsPerMonth: mealsPerMonth.value && mealsPerMonth.value > 0
        ? Number(mealsPerMonth.value)
        : Number(mealsPerWeek.value) * 4,
      monthlyPrice: Number(monthlyPrice.value),
      allowedDays: allowedDays.value.trim() || 'Segunda a Sexta',
      deliveryWindow: deliveryWindow.value.trim() || '11:00 às 14:30',
      deliveryFee: Number(deliveryFee.value) || 0,
      cancellationPolicy: cancellationPolicy.value.trim() || 'Cancelamento livre até o fim do ciclo vigente.',
    }

    if (editingPlanId.value) {
      await api.put(`/planos/${editingPlanId.value}`, payload)
      toast.success('Plano da loja atualizado com sucesso!')
    } else {
      await api.post('/planos', payload)
      toast.success('Novo plano publicado para os clientes desta loja!')
    }

    drawerOpen.value = false
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível salvar o plano.'))
  } finally {
    savingPlan.value = false
  }
}

async function handleConfirmDelete() {
  if (!deleteTarget.value) return
  try {
    await api.delete(`/planos/${deleteTarget.value.id}`)
    toast.info(`Plano "${deleteTarget.value.name}" desativado.`)
    deleteTarget.value = null
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível desativar o plano.'))
  }
}
</script>

<template>
  <div class="space-y-5">
    <!-- Top Bar -->
    <div class="flex flex-col justify-between gap-3 sm:flex-row sm:items-center">
      <div>
        <h2 class="font-display text-base font-extrabold text-ink">
          Planos de assinatura de {{ restaurantName }}
        </h2>
        <p class="text-xs text-ink-muted">
          {{ plans.length }} {{ plans.length === 1 ? 'plano ativo' : 'planos ativos' }} disponíveis exclusivamente para pedidos nesta loja.
        </p>
      </div>

      <AppButton variant="fresh" size="md" @click="openCreateDrawer">
        <Plus class="h-4 w-4" />
        Novo plano
      </AppButton>
    </div>

    <!-- Plans Grid -->
    <div v-if="plans.length > 0" class="grid gap-4 md:grid-cols-2">
      <AppCard
        v-for="plan in plans"
        :key="plan.id"
        padding="lg"
        class="flex flex-col justify-between space-y-4"
      >
        <div class="space-y-3">
          <div class="flex items-start justify-between gap-3">
            <div class="min-w-0">
              <div class="flex flex-wrap items-center gap-1.5">
                <AppBadge variant="fresh" size="xs">
                  {{ plan.mealsPerWeek }}/semana
                </AppBadge>
                <AppBadge variant="neutral" size="xs">
                  {{ getMealsPerMonth(plan) }}/mês
                </AppBadge>
              </div>
              <h3 class="mt-2 font-display text-base font-extrabold text-ink">
                {{ plan.name }}
              </h3>
            </div>

            <div class="text-right shrink-0">
              <p class="font-display text-xl font-extrabold text-ink tabular-nums">
                {{ formatCurrency(plan.monthlyPrice) }}
                <span class="text-xs font-normal text-ink-muted">/mês</span>
              </p>
              <p class="text-[11px] font-bold text-fresh-700 dark:text-fresh-400 tabular-nums">
                {{ formatCurrency(getPricePerMeal(plan)) }}/marmita
              </p>
            </div>
          </div>

          <p class="text-xs leading-relaxed text-ink-secondary">
            {{ plan.description || 'Assinatura mensal recorrente com resgate direto no cardápio da loja.' }}
          </p>

          <div class="flex flex-wrap gap-x-4 gap-y-1.5 rounded-xl border border-line bg-surface-subtle px-3 py-2.5 text-[11px] text-ink-secondary">
            <span v-if="plan.allowedDays" class="inline-flex items-center gap-1.5">
              <CalendarDays class="h-3.5 w-3.5 text-fresh-600" />
              {{ plan.allowedDays }}
            </span>
            <span v-if="plan.deliveryWindow" class="inline-flex items-center gap-1.5">
              <Clock class="h-3.5 w-3.5 text-fresh-600" />
              {{ plan.deliveryWindow }}
            </span>
          </div>
        </div>

        <div class="flex items-center justify-between gap-2 border-t border-line pt-3">
          <span class="text-[11px] text-ink-muted truncate">
            {{ plan.deliveryFee && plan.deliveryFee > 0 ? `Taxa: ${formatCurrency(plan.deliveryFee)}` : 'Entrega inclusa no plano' }}
          </span>

          <div class="flex items-center gap-1.5">
            <AppButton variant="outline" size="xs" @click="openEditDrawer(plan)">
              <Edit3 class="h-3.5 w-3.5" />
              Editar
            </AppButton>
            <button
              type="button"
              aria-label="Desativar plano"
              class="rounded-lg border border-red-200 p-1.5 text-red-600 transition hover:bg-red-50 dark:border-red-900/60 dark:text-red-400 dark:hover:bg-red-950/40"
              @click="deleteTarget = plan"
            >
              <Trash2 class="h-3.5 w-3.5" />
            </button>
          </div>
        </div>
      </AppCard>
    </div>

    <AppEmptyState
      v-else
      :icon="Sparkles"
      title="Nenhum plano ativo nesta loja"
      description="Crie pacotes semanais ou mensais para fidelizar clientes e garantir receita recorrente no seu restaurante."
    >
      <template #action>
        <AppButton variant="fresh" size="sm" @click="openCreateDrawer">
          <Plus class="h-4 w-4" />
          Criar primeiro plano
        </AppButton>
      </template>
    </AppEmptyState>

    <!-- Create / Edit Plan Drawer -->
    <AppDrawer
      :open="drawerOpen"
      :title="editingPlanId ? 'Editar Plano da Loja' : 'Novo Plano de Assinatura'"
      :subtitle="`Os resgates deste plano serão válidos exclusivamente no cardápio de ${restaurantName}.`"
      width-class="max-w-lg"
      @close="drawerOpen = false"
    >
      <form id="partner-plan-form" class="space-y-4" @submit.prevent="handleSavePlan">
        <AppInput
          v-model="name"
          label="Nome do plano"
          required
          placeholder="Ex: Executivo 5x Semana"
        />

        <div class="space-y-1.5">
          <label class="block text-xs font-semibold text-ink-secondary">Descrição</label>
          <textarea
            v-model="description"
            rows="3"
            placeholder="Explique quais pratos e benefícios estão incluídos..."
            class="w-full rounded-xl border border-line-strong bg-surface px-3.5 py-2.5 text-sm text-ink focus:border-brand-600 focus:outline-none"
          />
        </div>

        <div class="grid gap-3 sm:grid-cols-3">
          <AppInput
            :model-value="mealsPerWeek"
            label="Marmitas / semana"
            type="number"
            min="1"
            required
            @update:model-value="(v) => onWeeklyMealsChange(Number(v) || 1)"
          />
          <AppInput
            :model-value="mealsPerMonth"
            label="Franquia mensal"
            type="number"
            min="1"
            required
            @update:model-value="(v) => { customMonthlyMeals = true; mealsPerMonth = Number(v) || 1 }"
          />
          <AppInput
            v-model.number="monthlyPrice"
            label="Valor mensal (R$)"
            type="number"
            step="0.01"
            min="1"
            required
            placeholder="289.90"
          />
        </div>

        <div class="grid gap-3 sm:grid-cols-2">
          <AppInput
            v-model="allowedDays"
            label="Dias permitidos"
            placeholder="Segunda a Sexta"
          />
          <AppInput
            v-model="deliveryWindow"
            label="Janela de entrega"
            placeholder="11:00 às 14:30"
          />
        </div>

        <div class="grid gap-3 sm:grid-cols-2">
          <AppInput
            v-model.number="deliveryFee"
            label="Taxa extra por entrega (R$)"
            type="number"
            step="0.01"
            min="0"
            placeholder="0.00"
          />
          <AppInput
            v-model="cancellationPolicy"
            label="Política de cancelamento"
            placeholder="Cancelamento livre até o fim do ciclo."
          />
        </div>
      </form>

      <template #footer>
        <div class="flex justify-end gap-2">
          <AppButton variant="outline" size="md" @click="drawerOpen = false">
            Cancelar
          </AppButton>
          <AppButton
            type="submit"
            form="partner-plan-form"
            variant="fresh"
            size="md"
            :loading="savingPlan"
          >
            {{ editingPlanId ? 'Salvar alterações' : 'Publicar plano' }}
          </AppButton>
        </div>
      </template>
    </AppDrawer>

    <AppConfirmDialog
      :open="Boolean(deleteTarget)"
      title="Desativar plano da loja?"
      :message="`O plano '${deleteTarget?.name}' deixará de aceitar novas assinaturas.`"
      confirm-label="Sim, desativar"
      variant="danger"
      @confirm="handleConfirmDelete"
      @cancel="deleteTarget = null"
    />
  </div>
</template>
