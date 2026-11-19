<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { Search, Star, Store } from 'lucide-vue-next'
import Navbar from '@/components/Navbar.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import type { Plan, Restaurant, ReviewItem } from '@/types'

const auth = useAuthStore()
const router = useRouter()

const isDriverOnly = computed(
  () =>
    auth.hasRole(['driver']) &&
    !auth.hasRole(['customer', 'restaurant_owner', 'admin', 'super_admin']),
)

const restaurants = ref<Restaurant[]>([])
const plans = ref<Plan[]>([])
const customerReviews = ref<ReviewItem[]>([])
const searchQuery = ref('')
const loading = ref(true)
const subscribingPlanId = ref<string | null>(null)
const error = ref('')
const successMessage = ref('')

const filteredRestaurants = computed(() => {
  const q = searchQuery.value.trim().toLowerCase()
  if (!q) return restaurants.value
  return restaurants.value.filter(
    (r) =>
      r.name.toLowerCase().includes(q) ||
      (r.description && r.description.toLowerCase().includes(q)) ||
      (r.addressNeighborhood && r.addressNeighborhood.toLowerCase().includes(q)) ||
      (r.addressCity && r.addressCity.toLowerCase().includes(q)),
  )
})

async function fetchData() {
  loading.value = true
  error.value = ''

  try {
    const [restRes, planRes, revRes] = await Promise.all([
      api.get<Restaurant[]>('/restaurantes'),
      api.get<Plan[]>('/planos'),
      api.get<ReviewItem[]>('/avaliacoes/clientes'),
    ])
    restaurants.value = restRes.data
    plans.value = planRes.data
    customerReviews.value = revRes.data
  } catch (err) {
    error.value = getErrorMessage(err, 'Não foi possível carregar os dados.')
  } finally {
    loading.value = false
  }
}

async function handleSubscribe(plan: Plan) {
  error.value = ''
  successMessage.value = ''

  if (!auth.isAuthenticated) {
    await router.push({ path: '/login', query: { redirect: '/' } })
    return
  }

  subscribingPlanId.value = plan.id
  try {
    await api.post('/assinaturas', { planId: plan.id })
    successMessage.value = `Assinatura do plano "${plan.name}" realizada com sucesso!`
  } catch (err) {
    error.value = getErrorMessage(err, 'Não foi possível assinar o plano.')
  } finally {
    subscribingPlanId.value = null
  }
}

onMounted(() => {
  void fetchData()
})
</script>

<template>
  <div class="min-h-screen bg-stone-50">
    <Navbar />

    <main class="mx-auto max-w-6xl space-y-10 px-4 py-8 sm:px-6">
      <div class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm sm:p-8">
        <h1 class="text-2xl font-bold text-stone-900 sm:text-3xl">
          {{ isDriverOnly ? 'Cardápios dos Restaurantes Parceiros' : 'Marmitas frescas e planos de assinatura' }}
        </h1>
        <p class="mt-2 max-w-2xl text-sm text-stone-600 sm:text-base">
          {{
            isDriverOnly
              ? 'Consulte os cardápios dos restaurantes parceiros e acompanhe as avaliações dos clientes.'
              : 'Escolha um restaurante parceiro para pedir sua marmita de hoje ou assine um plano semanal.'
          }}
        </p>
      </div>

      <div
        v-if="error"
        class="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700"
      >
        {{ error }}
      </div>

      <div
        v-if="successMessage"
        class="flex items-center justify-between rounded-lg border border-green-200 bg-green-50 p-4 text-sm text-green-800"
      >
        <span>{{ successMessage }}</span>
        <RouterLink to="/conta" class="font-semibold underline">
          Ver minhas assinaturas
        </RouterLink>
      </div>

      <section class="space-y-4">
        <div class="flex flex-col justify-between gap-3 sm:flex-row sm:items-center">
          <h2 class="text-lg font-bold text-stone-900">Restaurantes disponíveis</h2>

          <div class="relative w-full sm:w-72">
            <Search class="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-stone-400" />
            <input
              v-model="searchQuery"
              type="search"
              placeholder="Buscar restaurante, bairro ou culinária..."
              class="w-full rounded-lg border border-stone-300 bg-white py-2 pl-9 pr-3 text-xs text-stone-900 focus:border-brand-600 focus:outline-none"
            />
          </div>
        </div>

        <p v-if="loading" class="text-sm text-stone-500">Carregando restaurantes...</p>

        <div
          v-else-if="filteredRestaurants.length > 0"
          class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3"
        >
          <div
            v-for="restaurant in filteredRestaurants"
            :key="restaurant.id"
            class="flex flex-col justify-between overflow-hidden rounded-xl border border-stone-200 bg-white shadow-sm"
          >
            <div>
              <div class="relative flex h-32 items-center justify-center bg-gradient-to-br from-orange-100 via-amber-50 to-stone-100">
                <img
                  v-if="restaurant.imageUrl"
                  :src="restaurant.imageUrl"
                  :alt="restaurant.name"
                  class="h-full w-full object-cover"
                />
                <Store v-else class="h-10 w-10 text-brand-600/70" />
              </div>

              <div class="p-5 pb-2">
                <h3 class="font-bold text-stone-900">{{ restaurant.name }}</h3>
                <p class="mt-1 text-sm text-stone-600">
                  {{ restaurant.description || 'Refeições preparadas diariamente.' }}
                </p>
                <p v-if="restaurant.addressNeighborhood" class="mt-2 text-xs text-stone-500">
                  📍 Bairro {{ restaurant.addressNeighborhood }} — {{ restaurant.addressCity }}/{{ restaurant.addressState }}
                </p>
              </div>
            </div>

            <div class="p-5 pt-3">
              <RouterLink
                :to="`/restaurantes/${restaurant.id}`"
                class="inline-block rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-700"
              >
                Ver cardápio
              </RouterLink>
            </div>
          </div>
        </div>

        <div
          v-else
          class="rounded-xl border border-stone-200 bg-white p-6 text-center text-sm text-stone-500"
        >
          {{
            searchQuery
              ? 'Nenhum restaurante encontrado para a sua busca.'
              : 'Nenhum restaurante cadastrado no momento.'
          }}
        </div>
      </section>

      <section v-if="!isDriverOnly" class="space-y-4">
        <h2 class="text-lg font-bold text-stone-900">Planos de assinatura</h2>

        <p v-if="loading" class="text-sm text-stone-500">Carregando planos...</p>

        <div
          v-else-if="plans.length > 0"
          class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3"
        >
          <div
            v-for="plan in plans"
            :key="plan.id"
            class="flex flex-col justify-between rounded-xl border border-stone-200 bg-white p-5 shadow-sm"
          >
            <div>
              <span class="inline-block rounded bg-green-100 px-2 py-0.5 text-xs font-semibold text-green-800">
                {{ plan.mealsPerWeek }} marmitas / semana
              </span>
              <h3 class="mt-2 font-bold text-stone-900">{{ plan.name }}</h3>
              <p class="mt-1 text-sm text-stone-600">{{ plan.description }}</p>
            </div>

            <div class="mt-4 flex items-center justify-between border-t border-stone-100 pt-4">
              <p class="text-lg font-bold text-stone-900">
                {{ formatCurrency(plan.monthlyPrice) }}
                <span class="text-xs font-normal text-stone-500">/mês</span>
              </p>

              <button
                type="button"
                :disabled="subscribingPlanId === plan.id"
                class="rounded-lg bg-stone-900 px-3.5 py-2 text-xs font-semibold text-white hover:bg-stone-800 disabled:opacity-50"
                @click="handleSubscribe(plan)"
              >
                {{ subscribingPlanId === plan.id ? 'Assinando...' : 'Assinar plano' }}
              </button>
            </div>
          </div>
        </div>

        <div
          v-else
          class="rounded-xl border border-stone-200 bg-white p-6 text-center text-sm text-stone-500"
        >
          Nenhum plano ativo no momento.
        </div>
      </section>

      <!-- Área de Avaliação de Clientes (Visível para TODOS os usuários) -->
      <section class="space-y-4">
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-2">
            <Star class="h-5 w-5 text-amber-500" />
            <h2 class="text-lg font-bold text-stone-900">
              Avaliações de Clientes
            </h2>
          </div>
          <span class="rounded-full bg-green-100 px-3 py-1 text-xs font-semibold text-green-800">
            Mural Público
          </span>
        </div>

        <div v-if="customerReviews.length > 0" class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <div
            v-for="rev in customerReviews"
            :key="rev.id"
            class="rounded-xl border border-stone-200 bg-white p-5 shadow-sm space-y-2"
          >
            <div class="flex items-center justify-between">
              <span class="font-bold text-sm text-stone-900">
                {{ rev.authorName || 'Cliente Verificado' }}
              </span>
              <span class="text-xs font-bold text-amber-600">
                {{ '★'.repeat(rev.rating) }}{{ '☆'.repeat(5 - rev.rating) }}
              </span>
            </div>
            <p class="text-xs font-semibold text-brand-700">
              {{ rev.restaurantName || 'Restaurante Parceiro' }}
            </p>
            <p class="text-sm text-stone-600">{{ rev.comment }}</p>
          </div>
        </div>
      </section>
    </main>
  </div>
</template>
