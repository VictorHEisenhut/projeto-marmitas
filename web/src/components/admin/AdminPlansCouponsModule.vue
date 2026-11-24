<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { Plus, Sparkles, Store, Tag, Trash2 } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppInput from '@/components/ui/AppInput.vue'
import AppConfirmDialog from '@/components/ui/AppConfirmDialog.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import type { Coupon, Plan, Restaurant } from '@/types'

const props = defineProps<{
  plans: Plan[]
  coupons: Coupon[]
  restaurants: Restaurant[]
}>()

const emit = defineEmits<{
  (e: 'refresh'): void
}>()

const toast = useToastStore()

const planRestaurantId = ref('')
const filterRestaurantId = ref('')
const planName = ref('')
const planDescription = ref('')
const planMealsPerWeek = ref(5)
const planMonthlyPrice = ref<number | null>(null)
const savingPlan = ref(false)

const couponCode = ref('')
const couponDiscount = ref(10)
const couponExpiresAt = ref('')
const savingCoupon = ref(false)

const deletePlanTarget = ref<Plan | null>(null)
const deleteCouponTarget = ref<Coupon | null>(null)

watch(
  () => props.restaurants,
  (list) => {
    const first = list[0]
    if (!planRestaurantId.value && first) {
      planRestaurantId.value = first.id
    }
  },
  { immediate: true },
)

const restaurantNameById = computed(() => {
  const map: Record<string, string> = {}
  for (const r of props.restaurants) {
    map[r.id] = r.name
  }
  return map
})

const filteredPlans = computed(() => {
  if (!filterRestaurantId.value) return props.plans
  return props.plans.filter((p) => p.restaurantId === filterRestaurantId.value)
})

function getPlanRestaurantName(plan: Plan): string {
  return plan.restaurantName || restaurantNameById.value[plan.restaurantId] || 'Restaurante parceiro'
}

async function handleCreatePlan() {
  if (!planRestaurantId.value) {
    toast.warning('Selecione o restaurante dono do plano.')
    return
  }
  if (!planName.value.trim() || !planMonthlyPrice.value || planMonthlyPrice.value <= 0) {
    toast.warning('Informe nome e valor mensal válido.')
    return
  }
  savingPlan.value = true
  try {
    await api.post('/planos', {
      restaurantId: planRestaurantId.value,
      name: planName.value.trim(),
      description: planDescription.value.trim(),
      mealsPerWeek: planMealsPerWeek.value,
      monthlyPrice: planMonthlyPrice.value,
    })
    planName.value = ''
    planDescription.value = ''
    planMonthlyPrice.value = null
    toast.success('Plano criado com sucesso!')
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Erro ao criar plano.'))
  } finally {
    savingPlan.value = false
  }
}

async function handleConfirmDeletePlan() {
  if (!deletePlanTarget.value) return
  try {
    await api.delete(`/planos/${deletePlanTarget.value.id}`)
    deletePlanTarget.value = null
    toast.info('Plano desativado.')
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Erro ao desativar plano.'))
  }
}

async function handleCreateCoupon() {
  if (!couponCode.value.trim() || couponDiscount.value <= 0) {
    toast.warning('Informe código e desconto válidos.')
    return
  }
  savingCoupon.value = true
  try {
    await api.post('/cupons', {
      code: couponCode.value.trim().toUpperCase(),
      discountPercentage: couponDiscount.value,
      expiresAt: couponExpiresAt.value ? new Date(couponExpiresAt.value).toISOString() : undefined,
    })
    couponCode.value = ''
    couponExpiresAt.value = ''
    toast.success('Cupom promocional criado!')
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Erro ao criar cupom.'))
  } finally {
    savingCoupon.value = false
  }
}

async function handleConfirmDeleteCoupon() {
  if (!deleteCouponTarget.value) return
  try {
    await api.delete(`/cupons/${deleteCouponTarget.value.id}`)
    deleteCouponTarget.value = null
    toast.info('Cupom removido.')
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Erro ao remover cupom.'))
  }
}
</script>

<template>
  <div class="grid gap-6 lg:grid-cols-2 items-start">
    <!-- Subscription Plans -->
    <AppCard padding="lg" class="space-y-5">
      <div class="flex items-center gap-2">
        <Sparkles class="h-5 w-5 text-fresh-600" />
        <h2 class="font-display text-base font-extrabold text-ink">Planos de Assinatura</h2>
      </div>

      <form class="space-y-3 rounded-2xl border border-line bg-surface-subtle p-4" @submit.prevent="handleCreatePlan">
        <div class="space-y-1.5">
          <label class="block text-xs font-semibold text-ink-secondary">Restaurante *</label>
          <select
            v-model="planRestaurantId"
            required
            class="w-full rounded-xl border border-line-strong bg-surface px-3 py-2 text-xs font-semibold text-ink focus:border-brand-600 focus:outline-none"
          >
            <option value="" disabled>Selecione um restaurante...</option>
            <option v-for="r in restaurants" :key="r.id" :value="r.id">{{ r.name }}</option>
          </select>
        </div>
        <div class="grid gap-3 sm:grid-cols-2">
          <AppInput v-model="planName" label="Nome do plano" required placeholder="Semanal 5 Refeições" />
          <AppInput v-model.number="planMealsPerWeek" label="Refeições / semana" type="number" min="1" required />
        </div>
        <div class="grid gap-3 sm:grid-cols-2">
          <AppInput v-model.number="planMonthlyPrice" label="Valor mensal (R$)" type="number" step="0.01" min="1" required placeholder="249.90" />
          <AppInput v-model="planDescription" label="Descrição" placeholder="Almoço garantido seg a sex" />
        </div>
        <AppButton type="submit" variant="fresh" size="sm" :loading="savingPlan">
          <Plus class="h-4 w-4" />
          Criar plano
        </AppButton>
      </form>

      <div v-if="restaurants.length > 0" class="flex items-center justify-between gap-2">
        <span class="text-xs font-semibold text-ink-muted">
          {{ filteredPlans.length }} {{ filteredPlans.length === 1 ? 'plano' : 'planos' }}
        </span>
        <select
          v-model="filterRestaurantId"
          class="rounded-xl border border-line-strong bg-surface px-3 py-1.5 text-xs font-medium text-ink"
        >
          <option value="">Todos restaurantes</option>
          <option v-for="r in restaurants" :key="r.id" :value="r.id">{{ r.name }}</option>
        </select>
      </div>

      <div v-if="filteredPlans.length > 0" class="divide-y divide-line">
        <div
          v-for="plan in filteredPlans"
          :key="plan.id"
          class="flex items-center justify-between gap-3 py-3"
        >
          <div class="min-w-0">
            <div class="flex flex-wrap items-center gap-2">
              <p class="font-display text-sm font-extrabold text-ink">{{ plan.name }}</p>
              <AppBadge variant="fresh" size="xs">{{ plan.mealsPerWeek }}/sem</AppBadge>
              <AppBadge variant="brand" size="xs">
                <Store class="h-3 w-3 mr-1" />
                {{ getPlanRestaurantName(plan) }}
              </AppBadge>
            </div>
            <p class="text-xs text-ink-secondary">
              <strong class="text-ink tabular-nums">{{ formatCurrency(plan.monthlyPrice) }}/mês</strong>
              <span v-if="plan.description"> · {{ plan.description }}</span>
            </p>
          </div>
          <button
            type="button"
            aria-label="Desativar plano"
            class="rounded-lg p-1.5 text-ink-muted hover:bg-red-50 hover:text-red-600"
            @click="deletePlanTarget = plan"
          >
            <Trash2 class="h-4 w-4" />
          </button>
        </div>
      </div>
      <p v-else class="py-6 text-center text-xs text-ink-muted">Nenhum plano cadastrado para o filtro selecionado.</p>
    </AppCard>

    <!-- Promo Coupons -->
    <AppCard padding="lg" class="space-y-5">
      <div class="flex items-center gap-2">
        <Tag class="h-5 w-5 text-brand-600" />
        <h2 class="font-display text-base font-extrabold text-ink">Cupons Promocionais</h2>
      </div>

      <form class="space-y-3 rounded-2xl border border-line bg-surface-subtle p-4" @submit.prevent="handleCreateCoupon">
        <div class="grid gap-3 sm:grid-cols-3">
          <AppInput v-model="couponCode" label="Código" required placeholder="MARMITA10" input-class="uppercase font-mono" />
          <AppInput v-model.number="couponDiscount" label="Desconto (%)" type="number" min="1" max="100" required />
          <AppInput v-model="couponExpiresAt" label="Validade" type="date" />
        </div>
        <AppButton type="submit" variant="primary" size="sm" :loading="savingCoupon">
          <Plus class="h-4 w-4" />
          Criar cupom
        </AppButton>
      </form>

      <div v-if="coupons.length > 0" class="divide-y divide-line">
        <div
          v-for="coupon in coupons"
          :key="coupon.id"
          class="flex items-center justify-between gap-3 py-3"
        >
          <div>
            <div class="flex items-center gap-2">
              <span class="rounded-lg bg-brand-100 px-2.5 py-0.5 font-mono text-xs font-extrabold text-brand-800 dark:bg-brand-950/60 dark:text-brand-300">
                {{ coupon.code }}
              </span>
              <AppBadge variant="fresh" size="xs">{{ coupon.discountPercentage }}% OFF</AppBadge>
            </div>
            <p class="mt-1 text-xs text-ink-muted">
              Expira em {{ new Date(coupon.expiresAt).toLocaleDateString('pt-BR') }}
            </p>
          </div>
          <button
            type="button"
            aria-label="Remover cupom"
            class="rounded-lg p-1.5 text-ink-muted hover:bg-red-50 hover:text-red-600"
            @click="deleteCouponTarget = coupon"
          >
            <Trash2 class="h-4 w-4" />
          </button>
        </div>
      </div>
      <p v-else class="py-6 text-center text-xs text-ink-muted">Nenhum cupom cadastrado.</p>
    </AppCard>

    <AppConfirmDialog
      :open="Boolean(deletePlanTarget)"
      title="Desativar plano?"
      :message="`Deseja desativar o plano '${deletePlanTarget?.name}'?`"
      confirm-label="Sim, desativar"
      variant="danger"
      @confirm="handleConfirmDeletePlan"
      @cancel="deletePlanTarget = null"
    />

    <AppConfirmDialog
      :open="Boolean(deleteCouponTarget)"
      title="Remover cupom?"
      :message="`Deseja remover o cupom '${deleteCouponTarget?.code}'?`"
      confirm-label="Sim, remover"
      variant="danger"
      @confirm="handleConfirmDeleteCoupon"
      @cancel="deleteCouponTarget = null"
    />
  </div>
</template>
