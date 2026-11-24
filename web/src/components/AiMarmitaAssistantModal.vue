<script setup lang="ts">
import { ref } from 'vue'
import { Dumbbell, Flame, HeartPulse, Plus, Sparkles, Wheat } from 'lucide-vue-next'
import AppModal from '@/components/ui/AppModal.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import { formatCurrency, getErrorMessage } from '@/services/api'
import { getAiMarmitaRecommendations } from '@/services/ai'
import type { AiRecommendationResponse, NutritionalEstimate, Product } from '@/types'

const props = defineProps<{
  isOpen: boolean
  restaurantId?: string
  restaurantName?: string
  availableProducts: Product[]
}>()

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'add-to-cart', product: Product): void
}>()

const query = ref('')
const selectedGoal = ref('')
const maxPrice = ref<number | null>(null)
const loading = ref(false)
const error = ref('')
const aiResult = ref<AiRecommendationResponse | null>(null)

const quickGoals = [
  {
    label: 'Alta Proteína',
    goal: 'hipertrofia',
    query: 'Opção com alto teor de proteínas para ganho de massa ou pós-treino',
  },
  {
    label: 'Low Carb & Leve',
    goal: 'low carb',
    query: 'Prato leve com baixo teor de carboidratos e boa saciedade',
  },
  {
    label: 'Vegetariano',
    goal: 'vegetariano',
    query: 'Opção nutritiva sem carne, rica em fibras e vegetais',
  },
  {
    label: 'Mais Econômica',
    goal: 'economico',
    query: 'Melhor custo-benefício mantendo refeição equilibrada',
  },
  {
    label: 'Equilíbrio & Energia',
    goal: 'energia',
    query: 'Refeição balanceada com carboidratos complexos para o dia de trabalho',
  },
]

const budgetChips = [
  { label: 'Qualquer preço', value: null },
  { label: 'Até R$ 25', value: 25 },
  { label: 'Até R$ 35', value: 35 },
  { label: 'Até R$ 45', value: 45 },
]

function applyGoal(item: (typeof quickGoals)[number]) {
  selectedGoal.value = item.goal
  query.value = item.query
  void handleSearch()
}

async function handleSearch() {
  if (!query.value.trim() && !selectedGoal.value) {
    error.value = 'Selecione um objetivo rápido ou escreva o que você procura.'
    return
  }

  loading.value = true
  error.value = ''
  aiResult.value = null

  try {
    const res = await getAiMarmitaRecommendations({
      restaurantId: props.restaurantId || null,
      query: query.value.trim() || selectedGoal.value,
      dietaryGoal: selectedGoal.value || null,
      maxPrice: maxPrice.value,
    })
    aiResult.value = res
  } catch (err) {
    error.value = getErrorMessage(err, 'Não foi possível obter a recomendação da IA no momento.')
  } finally {
    loading.value = false
  }
}

function handleAdd(productId: string) {
  const product = props.availableProducts.find((p) => p.id === productId)
  if (product) {
    emit('add-to-cart', product)
  }
}

function getDonutSegments(est: NutritionalEstimate) {
  const total = Math.max(1, est.protein + est.carbs + est.fat)
  const C = 2 * Math.PI * 18 // r = 18
  const pLen = (est.protein / total) * C
  const cLen = (est.carbs / total) * C
  const fLen = (est.fat / total) * C
  return {
    circumference: C,
    proteinDash: `${pLen.toFixed(1)} ${C.toFixed(1)}`,
    carbsDash: `${cLen.toFixed(1)} ${C.toFixed(1)}`,
    carbsOffset: (-pLen).toFixed(1),
    fatDash: `${fLen.toFixed(1)} ${C.toFixed(1)}`,
    fatOffset: (-(pLen + cLen)).toFixed(1),
  }
}
</script>

<template>
  <AppModal
    :open="isOpen"
    max-width="2xl"
    sheet-on-mobile
    @close="emit('close')"
  >
    <template #header>
      <div class="flex items-center gap-3">
        <div class="flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl bg-gradient-to-br from-brand-600 to-amber-500 text-white shadow-sm">
          <Sparkles class="h-5 w-5" />
        </div>
        <div class="min-w-0">
          <div class="flex flex-wrap items-center gap-2">
            <h3 class="font-display text-lg font-extrabold text-ink">
              Chef & Nutricionista IA
            </h3>
            <AppBadge variant="brand" size="xs">Gemini IA</AppBadge>
          </div>
          <p class="truncate text-xs text-ink-muted">
            {{ restaurantName ? `Analisando o cardápio de ${restaurantName}` : 'Recomendações personalizadas para suas metas' }}
          </p>
        </div>
      </div>
    </template>

    <div class="space-y-5">
      <!-- Quick Goal Chips -->
      <div>
        <p class="mb-2 text-[11px] font-bold uppercase tracking-wider text-ink-muted">
          Objetivos nutricionais
        </p>
        <div class="flex flex-wrap gap-2">
          <button
            v-for="item in quickGoals"
            :key="item.goal"
            type="button"
            class="rounded-full border px-3.5 py-1.5 text-xs font-semibold transition"
            :class="
              selectedGoal === item.goal
                ? 'border-brand-600 bg-brand-600 text-white shadow-sm'
                : 'border-line bg-surface-subtle text-ink-secondary hover:border-brand-400 hover:text-ink'
            "
            @click="applyGoal(item)"
          >
            {{ item.label }}
          </button>
        </div>
      </div>

      <!-- Max Price Quick Filter -->
      <div>
        <p class="mb-2 text-[11px] font-bold uppercase tracking-wider text-ink-muted">
          Orçamento máximo por marmita
        </p>
        <div class="flex flex-wrap gap-1.5">
          <button
            v-for="b in budgetChips"
            :key="String(b.value)"
            type="button"
            class="rounded-xl border px-3 py-1 text-xs font-semibold transition"
            :class="
              maxPrice === b.value
                ? 'border-fresh-600 bg-fresh-600 text-white'
                : 'border-line bg-surface text-ink-secondary hover:bg-surface-muted'
            "
            @click="maxPrice = b.value"
          >
            {{ b.label }}
          </button>
        </div>
      </div>

      <!-- Conversational Prompt Input -->
      <div class="space-y-2">
        <label class="block text-[11px] font-bold uppercase tracking-wider text-ink-muted">
          Ou descreva o que você procura hoje
        </label>
        <div class="flex flex-col gap-2 sm:flex-row">
          <input
            v-model="query"
            type="text"
            placeholder="Ex: Treinei hoje cedo, quero algo com bastante proteína e carboidrato leve..."
            class="flex-1 rounded-xl border border-line-strong bg-surface px-4 py-2.5 text-sm text-ink placeholder:text-ink-muted/70 focus:border-brand-600 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
            @keyup.enter="handleSearch"
          />
          <AppButton variant="primary" size="md" :loading="loading" @click="handleSearch">
            <Sparkles v-if="!loading" class="h-4 w-4" />
            <span>Consultar</span>
          </AppButton>
        </div>
      </div>

      <div
        v-if="error"
        class="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-xs font-medium text-red-700 dark:border-red-900/60 dark:bg-red-950/40 dark:text-red-300"
      >
        {{ error }}
      </div>

      <div
        v-if="loading"
        class="flex flex-col items-center justify-center rounded-2xl border border-dashed border-brand-200 bg-brand-50/40 py-10 text-center dark:border-brand-800/60 dark:bg-brand-950/20"
      >
        <div class="mb-3 flex h-12 w-12 items-center justify-center rounded-2xl bg-brand-600 text-white shadow-md animate-pulse">
          <Sparkles class="h-6 w-6" />
        </div>
        <p class="font-display text-sm font-bold text-ink">
          O Chef IA está montando sua curadoria nutricional...
        </p>
        <p class="mt-1 text-xs text-ink-muted">
          Calculando calorias, proteínas, carboidratos e equilíbrio dos pratos
        </p>
      </div>

      <div v-else-if="aiResult" class="space-y-4">
        <div class="rounded-2xl border border-amber-200 bg-amber-50/70 p-4 dark:border-amber-800/60 dark:bg-amber-950/30">
          <div class="flex items-start gap-3">
            <HeartPulse class="mt-0.5 h-5 w-5 shrink-0 text-brand-600 dark:text-brand-400" />
            <div class="space-y-1">
              <p class="text-xs font-extrabold uppercase tracking-wide text-ink">
                Avaliação Nutricional
              </p>
              <p class="text-sm leading-relaxed text-ink-secondary">
                {{ aiResult.generalAdvice }}
              </p>
            </div>
          </div>
        </div>

        <div class="space-y-3">
          <h4 class="text-xs font-bold uppercase tracking-wider text-ink-muted">
            Pratos sugeridos ({{ aiResult.recommendations.length }})
          </h4>

          <div
            v-for="item in aiResult.recommendations"
            :key="item.productId"
            class="rounded-2xl border border-line bg-surface p-4 shadow-card transition hover:border-brand-400"
          >
            <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
              <!-- Mini Donut SVG for Macros -->
              <div class="flex items-center gap-3.5 min-w-0 flex-1">
                <div class="relative flex h-14 w-14 shrink-0 items-center justify-center">
                  <svg class="h-14 w-14 -rotate-90" viewBox="0 0 44 44" aria-hidden="true">
                    <circle cx="22" cy="22" r="18" fill="none" stroke="currentColor" stroke-width="5" class="text-surface-muted" />
                    <circle
                      cx="22"
                      cy="22"
                      r="18"
                      fill="none"
                      stroke="#16A34A"
                      stroke-width="5"
                      :stroke-dasharray="getDonutSegments(item.nutritionalEstimate).proteinDash"
                    />
                    <circle
                      cx="22"
                      cy="22"
                      r="18"
                      fill="none"
                      stroke="#0EA5E9"
                      stroke-width="5"
                      :stroke-dasharray="getDonutSegments(item.nutritionalEstimate).carbsDash"
                      :stroke-dashoffset="getDonutSegments(item.nutritionalEstimate).carbsOffset"
                    />
                    <circle
                      cx="22"
                      cy="22"
                      r="18"
                      fill="none"
                      stroke="#F59E0B"
                      stroke-width="5"
                      :stroke-dasharray="getDonutSegments(item.nutritionalEstimate).fatDash"
                      :stroke-dashoffset="getDonutSegments(item.nutritionalEstimate).fatOffset"
                    />
                  </svg>
                  <div class="absolute inset-0 flex flex-col items-center justify-center text-center leading-none">
                    <span class="font-display text-[11px] font-extrabold text-ink tabular-nums">
                      {{ item.nutritionalEstimate.calories }}
                    </span>
                    <span class="text-[8px] font-bold text-ink-muted">kcal</span>
                  </div>
                </div>

                <div class="min-w-0 space-y-1">
                  <div class="flex flex-wrap items-center gap-2">
                    <span class="font-display text-base font-extrabold text-ink">
                      {{ item.productName }}
                    </span>
                    <span class="font-display text-sm font-bold text-brand-600 dark:text-brand-400 tabular-nums">
                      {{ formatCurrency(item.price) }}
                    </span>
                  </div>
                  <p class="text-xs leading-relaxed text-ink-secondary">
                    {{ item.whyRecommended }}
                  </p>
                </div>
              </div>

              <AppButton variant="primary" size="sm" class="shrink-0" @click="handleAdd(item.productId)">
                <Plus class="h-3.5 w-3.5" />
                <span>Adicionar</span>
              </AppButton>
            </div>

            <div class="mt-3 flex flex-wrap items-center gap-3 border-t border-line pt-2.5 text-xs">
              <span class="inline-flex items-center gap-1 font-bold text-amber-700 dark:text-amber-400 tabular-nums">
                <Flame class="h-3.5 w-3.5" />
                ~{{ item.nutritionalEstimate.calories }} kcal
              </span>
              <span class="inline-flex items-center gap-1 font-bold text-fresh-700 dark:text-fresh-400 tabular-nums">
                <Dumbbell class="h-3.5 w-3.5" />
                {{ item.nutritionalEstimate.protein }}g prot
              </span>
              <span class="inline-flex items-center gap-1 font-semibold text-sky-700 dark:text-sky-400 tabular-nums">
                <Wheat class="h-3.5 w-3.5" />
                {{ item.nutritionalEstimate.carbs }}g carb
              </span>
              <span class="text-ink-muted font-medium tabular-nums">
                {{ item.nutritionalEstimate.fat }}g gord
              </span>

              <div v-if="item.tags?.length" class="ml-auto flex flex-wrap gap-1">
                <AppBadge v-for="tag in item.tags" :key="tag" variant="neutral" size="xs">
                  {{ tag }}
                </AppBadge>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <template #footer>
      <div class="flex items-center justify-between text-xs text-ink-muted">
        <span>Estimativas nutricionais orientativas geradas por IA.</span>
        <AppButton variant="ghost" size="xs" @click="emit('close')">Fechar</AppButton>
      </div>
    </template>
  </AppModal>
</template>
