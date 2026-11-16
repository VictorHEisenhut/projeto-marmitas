<script setup lang="ts">
import { ref } from 'vue'
import { Sparkles, X, Loader2, Plus, Flame, Dumbbell, Wheat, HeartPulse } from 'lucide-vue-next'
import { formatCurrency, getErrorMessage } from '@/services/api'
import { getAiMarmitaRecommendations } from '@/services/ai'
import type { AiRecommendationResponse, Product } from '@/types'

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
const loading = ref(false)
const error = ref('')
const aiResult = ref<AiRecommendationResponse | null>(null)

const quickGoals = [
  { label: '💪 Alta Proteína', goal: 'hipertrofia', query: 'Opção com alto teor de proteínas para ganho de massa ou pós-treino' },
  { label: '🥗 Low Carb & Leve', goal: 'low carb', query: 'Prato leve com baixo teor de carboidratos e boa saciedade' },
  { label: '🌱 Vegetariano', goal: 'vegetariano', query: 'Opção nutritiva sem carne, rica em fibras e vegetais' },
  { label: '💰 Mais Econômica', goal: 'economico', query: 'Melhor custo-benefício mantendo refeição equilibrada' },
  { label: '⚡ Equilíbrio & Energia', goal: 'energia', query: 'Refeição balanceada com carboidratos complexos para o dia de trabalho' },
]

function applyGoal(item: typeof quickGoals[0]) {
  selectedGoal.value = item.goal
  query.value = item.query
  void handleSearch()
}

async function handleSearch() {
  if (!query.value.trim() && !selectedGoal.value) {
    error.value = 'Selecione uma opção rápida ou escreva o que você deseja.'
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

function handleClose() {
  emit('close')
}
</script>

<template>
  <div
    v-if="isOpen"
    class="fixed inset-0 z-50 flex items-center justify-center overflow-y-auto bg-stone-900/60 p-4 backdrop-blur-sm"
    @click.self="handleClose"
  >
    <div
      class="relative w-full max-w-2xl rounded-2xl border border-stone-200 bg-white shadow-2xl transition-all"
    >
      <!-- Header -->
      <div class="flex items-center justify-between border-b border-stone-100 p-5 sm:p-6 bg-gradient-to-r from-brand-50 to-amber-50 rounded-t-2xl">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-brand-600 text-white shadow-sm">
            <Sparkles class="h-5 w-5" />
          </div>
          <div>
            <h3 class="text-lg font-bold text-stone-900 flex items-center gap-2">
              Chef & Nutricionista IA
              <span class="rounded-full bg-brand-100 px-2 py-0.5 text-xs font-semibold text-brand-700">
                Gemini 3.8 Flash
              </span>
            </h3>
            <p class="text-xs text-stone-600 sm:text-sm">
              {{ restaurantName ? `Cardápio de ${restaurantName}` : 'Descubra a marmita ideal para suas metas' }}
            </p>
          </div>
        </div>
        <button
          type="button"
          class="rounded-lg p-1.5 text-stone-400 hover:bg-stone-200/60 hover:text-stone-700"
          @click="handleClose"
        >
          <X class="h-5 w-5" />
        </button>
      </div>

      <!-- Body -->
      <div class="max-h-[75vh] overflow-y-auto p-5 sm:p-6 space-y-5">
        <!-- Quick goal chips -->
        <div>
          <label class="block text-xs font-semibold uppercase tracking-wider text-stone-500 mb-2">
            Objetivos rápidos
          </label>
          <div class="flex flex-wrap gap-2">
            <button
              v-for="item in quickGoals"
              :key="item.goal"
              type="button"
              class="rounded-full border px-3 py-1.5 text-xs font-medium transition"
              :class="
                selectedGoal === item.goal
                  ? 'border-brand-600 bg-brand-600 text-white shadow-sm'
                  : 'border-stone-200 bg-stone-50 text-stone-700 hover:border-brand-300 hover:bg-white'
              "
              @click="applyGoal(item)"
            >
              {{ item.label }}
            </button>
          </div>
        </div>

        <!-- Custom prompt input -->
        <div class="space-y-2">
          <label class="block text-xs font-semibold uppercase tracking-wider text-stone-500">
            Ou descreva o que você procura
          </label>
          <div class="flex gap-2">
            <input
              v-model="query"
              type="text"
              placeholder="Ex: Treinei perna hoje, quero algo com arroz integral e muita proteína..."
              class="flex-1 rounded-xl border border-stone-300 px-3.5 py-2.5 text-sm focus:border-brand-500 focus:outline-none focus:ring-1 focus:ring-brand-500"
              @keyup.enter="handleSearch"
            />
            <button
              type="button"
              :disabled="loading"
              class="inline-flex items-center gap-2 rounded-xl bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-brand-700 disabled:opacity-50"
              @click="handleSearch"
            >
              <Loader2 v-if="loading" class="h-4 w-4 animate-spin" />
              <Sparkles v-else class="h-4 w-4" />
              <span>Consultar</span>
            </button>
          </div>
        </div>

        <!-- Error Message -->
        <div
          v-if="error"
          class="rounded-xl border border-red-200 bg-red-50 p-3.5 text-sm text-red-700"
        >
          {{ error }}
        </div>

        <!-- Loading state -->
        <div v-if="loading" class="py-10 text-center space-y-3">
          <div class="inline-flex h-12 w-12 items-center justify-center rounded-2xl bg-brand-100 text-brand-600 animate-pulse">
            <Sparkles class="h-6 w-6 animate-spin" />
          </div>
          <p class="text-sm font-medium text-stone-700">O Gemini está analisando os pratos e valores nutricionais...</p>
          <p class="text-xs text-stone-400">Calculando calorias, proteínas e equilíbrio dos ingredientes</p>
        </div>

        <!-- AI Results -->
        <div v-else-if="aiResult" class="space-y-4">
          <!-- Advice Banner -->
          <div class="rounded-xl border border-amber-200/80 bg-amber-50/70 p-4 text-sm text-stone-800">
            <div class="flex items-start gap-2.5">
              <HeartPulse class="h-5 w-5 text-brand-600 shrink-0 mt-0.5" />
              <div class="space-y-1">
                <span class="font-bold text-stone-900">Avaliação do Nutricionista IA:</span>
                <p class="text-stone-700 leading-relaxed">{{ aiResult.generalAdvice }}</p>
              </div>
            </div>
          </div>

          <!-- Recommendations List -->
          <div class="space-y-3">
            <h4 class="text-xs font-semibold uppercase tracking-wider text-stone-500">
              Pratos recomendados para você ({{ aiResult.recommendations.length }})
            </h4>

            <div
              v-for="item in aiResult.recommendations"
              :key="item.productId"
              class="rounded-xl border border-stone-200 bg-white p-4 transition hover:border-brand-400 hover:shadow-sm"
            >
              <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
                <div class="space-y-1">
                  <div class="flex items-center gap-2">
                    <span class="text-base font-bold text-stone-900">{{ item.productName }}</span>
                    <span class="text-sm font-semibold text-brand-600">{{ formatCurrency(item.price) }}</span>
                  </div>
                  <p class="text-xs text-stone-600">{{ item.whyRecommended }}</p>
                </div>

                <button
                  type="button"
                  class="inline-flex items-center justify-center gap-1.5 rounded-lg bg-stone-900 px-3.5 py-2 text-xs font-semibold text-white shadow-sm transition hover:bg-brand-600 shrink-0"
                  @click="handleAdd(item.productId)"
                >
                  <Plus class="h-3.5 w-3.5" />
                  <span>Adicionar</span>
                </button>
              </div>

              <!-- Nutrition Estimate Badges -->
              <div class="mt-3 pt-3 border-t border-stone-100 flex flex-wrap items-center gap-3 text-xs text-stone-600">
                <div class="flex items-center gap-1 font-medium text-amber-700">
                  <Flame class="h-3.5 w-3.5" />
                  <span>~{{ item.nutritionalEstimate.calories }} kcal</span>
                </div>
                <div class="flex items-center gap-1 font-medium text-emerald-700">
                  <Dumbbell class="h-3.5 w-3.5" />
                  <span>{{ item.nutritionalEstimate.protein }}g proteína</span>
                </div>
                <div class="flex items-center gap-1 text-stone-600">
                  <Wheat class="h-3.5 w-3.5" />
                  <span>{{ item.nutritionalEstimate.carbs }}g carb</span>
                </div>
                <div class="flex items-center gap-1 text-stone-500">
                  <span>{{ item.nutritionalEstimate.fat }}g gordura</span>
                </div>

                <div class="flex flex-wrap gap-1 ml-auto">
                  <span
                    v-for="tag in item.tags"
                    :key="tag"
                    class="rounded-full bg-stone-100 px-2 py-0.5 text-[10px] font-medium text-stone-600"
                  >
                    {{ tag }}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Footer -->
      <div class="border-t border-stone-100 px-6 py-3.5 bg-stone-50/60 rounded-b-2xl flex justify-between items-center text-xs text-stone-500">
        <span>As informações nutricionais são estimativas orientativas.</span>
        <button
          type="button"
          class="font-medium text-stone-600 hover:text-stone-900"
          @click="handleClose"
        >
          Fechar
        </button>
      </div>
    </div>
  </div>
</template>
