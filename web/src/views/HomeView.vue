<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import Navbar from '@/components/Navbar.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import type { Plan, Restaurant } from '@/types'

const auth = useAuthStore()
const router = useRouter()

const restaurants = ref<Restaurant[]>([])
const plans = ref<Plan[]>([])
const loading = ref(true)
const subscribingPlanId = ref<string | null>(null)
const error = ref('')
const successMessage = ref('')

async function fetchData() {
  loading.value = true
  error.value = ''

  try {
    const [restRes, planRes] = await Promise.all([
      api.get<Restaurant[]>('/restaurantes'),
      api.get<Plan[]>('/planos'),
    ])
    restaurants.value = restRes.data
    plans.value = planRes.data
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
          Marmitas frescas e planos de assinatura
        </h1>
        <p class="mt-2 max-w-2xl text-sm text-stone-600 sm:text-base">
          Escolha um restaurante parceiro para pedir sua marmita de hoje ou assine um plano semanal.
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
        <h2 class="text-lg font-bold text-stone-900">Restaurantes disponíveis</h2>

        <p v-if="loading" class="text-sm text-stone-500">Carregando restaurantes...</p>

        <div
          v-else-if="restaurants.length > 0"
          class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3"
        >
          <div
            v-for="restaurant in restaurants"
            :key="restaurant.id"
            class="flex flex-col justify-between rounded-xl border border-stone-200 bg-white p-5 shadow-sm"
          >
            <div>
              <h3 class="font-bold text-stone-900">{{ restaurant.name }}</h3>
              <p class="mt-1 text-sm text-stone-600">
                {{ restaurant.description || 'Refeições preparadas diariamente.' }}
              </p>
            </div>

            <div class="mt-4 pt-2">
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
          Nenhum restaurante cadastrado no momento.
        </div>
      </section>

      <section class="space-y-4">
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
    </main>
  </div>
</template>
