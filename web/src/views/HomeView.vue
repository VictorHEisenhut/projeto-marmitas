<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  ArrowRight,
  CalendarDays,
  Check,
  Clock,
  Flame,
  Heart,
  LayoutGrid,
  Map as MapIcon,
  MapPin,
  Navigation,
  Search,
  Sparkles,
  Star,
  Store,
  Truck,
} from 'lucide-vue-next'
import CustomerLayout from '@/layouts/CustomerLayout.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppCard from '@/components/ui/AppCard.vue'
import AppSkeleton from '@/components/ui/AppSkeleton.vue'
import AppEmptyState from '@/components/ui/AppEmptyState.vue'
import StarRating from '@/components/ui/StarRating.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useActiveOrderStore } from '@/stores/activeOrder'
import { useToastStore } from '@/stores/toast'
import type { Favorite, Plan, Restaurant, ReviewItem } from '@/types'

const auth = useAuthStore()
const activeOrderStore = useActiveOrderStore()
const toast = useToastStore()
const route = useRoute()
const router = useRouter()

const restaurants = ref<Restaurant[]>([])
const plans = ref<Plan[]>([])
const customerReviews = ref<ReviewItem[]>([])
const favorites = ref<Favorite[]>([])
const searchQuery = ref(typeof route.query.q === 'string' ? route.query.q : '')
const activeFilter = ref<'all' | 'top_rated' | 'low_fee' | 'plan_ready'>('all')
const viewMode = ref<'list' | 'map'>('list')
const mapSelectedRestaurantId = ref<string>('')

// Interactive Club Simulator
const simulatedMealsPerWeek = ref(5)
const avgSingleMealPrice = 28

const loading = ref(true)
const error = ref('')
const subscribingPlanId = ref<string | null>(null)
const togglingFavId = ref<string | null>(null)

watch(
  () => route.query.q,
  (val) => {
    if (typeof val === 'string') searchQuery.value = val
  },
)

const ratingByRestaurant = computed(() => {
  const acc: Record<string, { sum: number; count: number }> = {}
  for (const rev of customerReviews.value) {
    if (!rev.restaurantId) continue
    if (!acc[rev.restaurantId]) acc[rev.restaurantId] = { sum: 0, count: 0 }
    acc[rev.restaurantId]!.sum += rev.rating
    acc[rev.restaurantId]!.count += 1
  }
  const map: Record<string, { avg: number; count: number }> = {}
  for (const [id, data] of Object.entries(acc)) {
    map[id] = { avg: data.sum / data.count, count: data.count }
  }
  return map
})

const favoriteByRestaurantId = computed(() => {
  const map: Record<string, Favorite> = {}
  for (const fav of favorites.value) {
    if (fav.restaurantId) map[fav.restaurantId] = fav
  }
  return map
})

const quickFilters = computed(() => [
  { id: 'all' as const, label: 'Todos', count: restaurants.value.length, icon: Store },
  { id: 'top_rated' as const, label: 'Mais bem avaliados', count: restaurants.value.length, icon: Star },
  {
    id: 'low_fee' as const,
    label: 'Entrega grátis / reduzida',
    count: restaurants.value.filter((r) => (r.deliveryFee ?? 0) <= 5.99).length,
    icon: Truck,
  },
  { id: 'plan_ready' as const, label: 'Aceita Clube', count: restaurants.value.length, icon: Sparkles },
])

const filteredRestaurants = computed(() => {
  let list = [...restaurants.value]

  const q = searchQuery.value.trim().toLowerCase()
  if (q) {
    list = list.filter(
      (r) =>
        r.name.toLowerCase().includes(q) ||
        (r.description && r.description.toLowerCase().includes(q)) ||
        (r.addressNeighborhood && r.addressNeighborhood.toLowerCase().includes(q)) ||
        (r.addressCity && r.addressCity.toLowerCase().includes(q)),
    )
  }

  if (activeFilter.value === 'low_fee') {
    list = list.filter((r) => (r.deliveryFee ?? 0) <= 5.99)
  } else if (activeFilter.value === 'top_rated') {
    list.sort((a, b) => {
      const ra = ratingByRestaurant.value[a.id]?.avg ?? 4.6
      const rb = ratingByRestaurant.value[b.id]?.avg ?? 4.6
      return rb - ra
    })
  }

  return list
})

const selectedMapRestaurant = computed<Restaurant | null>(() => {
  if (filteredRestaurants.value.length === 0) return null
  return (
    filteredRestaurants.value.find((r) => r.id === mapSelectedRestaurantId.value) ??
    filteredRestaurants.value[0] ??
    null
  )
})

const selectedMapUrl = computed(() => {
  const r = selectedMapRestaurant.value
  const lat = r?.latitude ?? -26.9194
  const lon = r?.longitude ?? -49.0661
  const delta = 0.008
  const bbox = `${lon - delta},${lat - delta},${lon + delta},${lat + delta}`
  return `https://www.openstreetmap.org/export/embed.html?bbox=${encodeURIComponent(bbox)}&layer=mapnik&marker=${lat},${lon}`
})

const recommendedPlan = computed<Plan | null>(() => {
  if (plans.value.length === 0) return null
  const sorted = [...plans.value].sort(
    (a, b) =>
      Math.abs(a.mealsPerWeek - simulatedMealsPerWeek.value) -
      Math.abs(b.mealsPerWeek - simulatedMealsPerWeek.value),
  )
  return sorted[0] ?? null
})

const simulatedMonthlyWithoutPlan = computed(
  () => simulatedMealsPerWeek.value * 4 * avgSingleMealPrice,
)

const simulatedSavings = computed(() => {
  if (!recommendedPlan.value) return 0
  return Math.max(0, simulatedMonthlyWithoutPlan.value - recommendedPlan.value.monthlyPrice)
})

const simulatedSavingsPercent = computed(() => {
  if (simulatedMonthlyWithoutPlan.value <= 0) return 0
  return Math.round((simulatedSavings.value / simulatedMonthlyWithoutPlan.value) * 100)
})

const platformAvgRating = computed(() => {
  if (customerReviews.value.length === 0) return 4.9
  const sum = customerReviews.value.reduce((acc, r) => acc + r.rating, 0)
  return sum / customerReviews.value.length
})

function getMealsPerMonth(plan: Plan): number {
  return plan.mealsPerMonth && plan.mealsPerMonth > 0
    ? plan.mealsPerMonth
    : Math.max(1, plan.mealsPerWeek * 4)
}

function getPricePerMeal(plan: Plan): number {
  return plan.monthlyPrice / getMealsPerMonth(plan)
}

function getInitials(name?: string): string {
  if (!name) return 'CL'
  return name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((n) => n[0]?.toUpperCase())
    .join('')
}

async function fetchData() {
  loading.value = true
  error.value = ''

  try {
    const requests: Promise<unknown>[] = [
      api.get<Restaurant[]>('/restaurantes'),
      api.get<Plan[]>('/planos'),
      api.get<ReviewItem[]>('/avaliacoes/clientes'),
    ]

    if (auth.isAuthenticated && !auth.isDriverOnly) {
      requests.push(api.get<Favorite[]>('/usuarios/me/favoritos'))
    }

    const results = await Promise.allSettled(requests)

    if (results[0]?.status === 'fulfilled') {
      restaurants.value = (results[0].value as { data: Restaurant[] }).data
      if (restaurants.value[0] && !mapSelectedRestaurantId.value) {
        mapSelectedRestaurantId.value = restaurants.value[0].id
      }
    } else {
      throw (results[0] as PromiseRejectedResult)?.reason
    }

    if (results[1]?.status === 'fulfilled') {
      plans.value = (results[1].value as { data: Plan[] }).data
    }
    if (results[2]?.status === 'fulfilled') {
      customerReviews.value = (results[2].value as { data: ReviewItem[] }).data
    }
    if (results[3]?.status === 'fulfilled') {
      favorites.value = (results[3].value as { data: Favorite[] }).data
    }
  } catch (err) {
    error.value = getErrorMessage(err, 'Não foi possível carregar os dados da vitrine.')
  } finally {
    loading.value = false
  }
}

async function handleToggleFavorite(restaurant: Restaurant) {
  if (!auth.isAuthenticated) {
    toast.info('Entre na sua conta para salvar restaurantes favoritos.')
    void router.push({ path: '/login', query: { redirect: '/' } })
    return
  }

  togglingFavId.value = restaurant.id
  const existing = favoriteByRestaurantId.value[restaurant.id]

  try {
    if (existing) {
      await api.delete(`/usuarios/me/favoritos/${existing.id}`)
      favorites.value = favorites.value.filter((f) => f.id !== existing.id)
      toast.info(`${restaurant.name} removido dos favoritos.`)
    } else {
      const { data } = await api.post<Favorite>('/usuarios/me/favoritos', {
        restaurantId: restaurant.id,
      })
      favorites.value.push(data)
      toast.success(`${restaurant.name} salvo nos seus favoritos!`)
    }
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível atualizar seus favoritos.'))
  } finally {
    togglingFavId.value = null
  }
}

async function handleSubscribe(plan: Plan) {
  if (!auth.isAuthenticated) {
    await router.push({ path: '/login', query: { redirect: '/' } })
    return
  }

  subscribingPlanId.value = plan.id
  try {
    await api.post('/assinaturas', { planId: plan.id })
    toast.success(
      `Assinatura do plano "${plan.name}" ativada com sucesso! Acesse Minha Conta para programar entregas.`,
      'Bem-vindo ao Clube!',
    )
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível assinar o plano.'))
  } finally {
    subscribingPlanId.value = null
  }
}

function scrollToPlans() {
  document.getElementById('planos-section')?.scrollIntoView({ behavior: 'smooth' })
}

onMounted(() => {
  void fetchData()
})
</script>

<template>
  <CustomerLayout>
    <div class="mx-auto max-w-7xl space-y-10 px-4 py-6 sm:px-6 sm:py-8">
      <!-- Asymmetric Bento Hero -->
      <section class="grid grid-cols-1 gap-4 lg:grid-cols-12">
        <!-- Main Left Bento Block (8 cols) -->
        <div
          class="relative overflow-hidden rounded-3xl bg-gradient-to-br from-brand-700 via-brand-600 to-amber-500 p-6 text-white shadow-elevated sm:p-9 lg:col-span-8 flex flex-col justify-between"
        >
          <div class="pointer-events-none absolute -right-20 -top-20 h-72 w-72 rounded-full bg-white/15 blur-3xl" />
          <div class="pointer-events-none absolute -bottom-24 right-1/3 h-64 w-64 rounded-full bg-amber-300/20 blur-3xl" />

          <div class="relative z-10 space-y-4">
            <div class="inline-flex items-center gap-2 rounded-full bg-white/15 px-3.5 py-1 text-xs font-bold backdrop-blur-md">
              <Flame class="h-3.5 w-3.5 text-amber-200" />
              <span>
                {{ auth.isDriverOnly ? 'Modo Consulta para Entregadores' : 'Cozinhas locais · Preparo diário' }}
              </span>
            </div>

            <h1 class="font-display text-3xl font-extrabold leading-tight tracking-tight sm:text-4xl">
              {{
                auth.isDriverOnly
                  ? 'Cardápios dos Restaurantes Parceiros'
                  : 'Marmitas caseiras frescas no ritmo da sua semana'
              }}
            </h1>

            <p class="max-w-xl text-sm leading-relaxed text-white/90 sm:text-base">
              {{
                auth.isDriverOnly
                  ? 'Consulte estabelecimentos, cardápios e avaliações públicas da comunidade.'
                  : 'Peça avulso com rastreamento GPS ao vivo ou economize até 35% assinando o Clube de Marmitas.'
              }}
            </p>
          </div>

          <div class="relative z-10 mt-6 flex flex-col gap-3 sm:flex-row sm:items-center">
            <div class="relative flex-1">
              <Search class="pointer-events-none absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-stone-500" />
              <input
                v-model="searchQuery"
                type="search"
                placeholder="Buscar restaurante, bairro, cidade ou culinária..."
                class="h-12 w-full rounded-2xl border-0 bg-white pl-11 pr-4 text-sm font-medium text-stone-900 placeholder:text-stone-500 shadow-lg focus:outline-none focus:ring-2 focus:ring-amber-300"
              />
            </div>

            <button
              v-if="!auth.isDriverOnly"
              type="button"
              class="inline-flex h-12 shrink-0 items-center justify-center gap-2 rounded-2xl bg-stone-950/90 px-5 text-sm font-bold text-white shadow-lg transition hover:bg-stone-900"
              @click="scrollToPlans"
            >
              <Sparkles class="h-4 w-4 text-fresh-400" />
              <span>Simular Plano</span>
            </button>
          </div>
        </div>

        <!-- Right Bento Column (4 cols: 2 stacked interactive blocks) -->
        <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-1 lg:col-span-4">
          <!-- Top Bento Card: Live Activity / Quick Access -->
          <div class="flex flex-col justify-between rounded-3xl border border-line bg-surface p-5 shadow-card">
            <template v-if="activeOrderStore.primaryActiveOrder && !auth.isDriverOnly">
              <div class="flex items-center justify-between gap-2">
                <AppBadge variant="fresh" size="xs" dot>Pedido em andamento</AppBadge>
                <span
                  v-if="activeOrderStore.primaryActiveOrder.confirmationPin"
                  class="font-mono text-xs font-extrabold text-brand-700 dark:text-brand-400"
                >
                  PIN {{ activeOrderStore.primaryActiveOrder.confirmationPin }}
                </span>
              </div>
              <div class="my-2">
                <p class="font-display text-base font-extrabold text-ink truncate">
                  {{ activeOrderStore.primaryActiveOrder.restaurantName }}
                </p>
                <p class="text-xs text-ink-secondary">{{ activeOrderStore.statusLabel }}</p>
              </div>
              <RouterLink to="/pedidos" class="inline-flex items-center gap-1.5 text-xs font-bold text-brand-600 hover:underline dark:text-brand-400">
                <span>Abrir mapa ao vivo</span>
                <ArrowRight class="h-3.5 w-3.5" />
              </RouterLink>
            </template>

            <template v-else>
              <div class="flex items-center justify-between">
                <AppBadge variant="brand" size="xs">Entrega Segura</AppBadge>
                <Navigation class="h-4 w-4 text-brand-600" />
              </div>
              <div class="my-2">
                <p class="font-display text-base font-extrabold text-ink">
                  GPS ao vivo + PIN 4 dígitos
                </p>
                <p class="mt-0.5 text-xs text-ink-secondary">
                  Acompanhe cada etapa do preparo até a portaria com código exclusivo.
                </p>
              </div>
              <RouterLink
                :to="auth.isAuthenticated ? '/pedidos' : '/login'"
                class="inline-flex items-center gap-1.5 text-xs font-bold text-brand-600 hover:underline dark:text-brand-400"
              >
                <span>{{ auth.isAuthenticated ? 'Ver meus pedidos' : 'Entrar para acompanhar' }}</span>
                <ArrowRight class="h-3.5 w-3.5" />
              </RouterLink>
            </template>
          </div>

          <!-- Bottom Bento Card: Clube de Marmitas Highlight -->
          <div class="flex flex-col justify-between rounded-3xl border border-fresh-200 bg-fresh-50/70 p-5 shadow-card dark:border-fresh-800/60 dark:bg-fresh-950/30">
            <div class="flex items-center justify-between">
              <AppBadge variant="fresh" size="xs">Clube de Marmitas</AppBadge>
              <span class="font-display text-xs font-extrabold text-fresh-700 dark:text-fresh-300 tabular-nums">
                {{ plans.length }} planos ativos
              </span>
            </div>
            <div class="my-2">
              <p class="font-display text-base font-extrabold text-ink">
                Marmitas a partir de
                <span v-if="plans[0]" class="text-fresh-700 dark:text-fresh-400 tabular-nums">
                  {{ formatCurrency(getPricePerMeal(plans[0])) }}/un.
                </span>
                <span v-else class="text-fresh-700 dark:text-fresh-400">R$ 0,00 base</span>
              </p>
              <p class="mt-0.5 text-xs text-ink-secondary">
                Agende sua semana no calendário e troque de plano quando quiser.
              </p>
            </div>
            <button
              v-if="!auth.isDriverOnly"
              type="button"
              class="inline-flex items-center gap-1.5 text-left text-xs font-bold text-fresh-700 hover:underline dark:text-fresh-300"
              @click="scrollToPlans"
            >
              <span>Calcular minha economia</span>
              <ArrowRight class="h-3.5 w-3.5" />
            </button>
          </div>
        </div>
      </section>

      <!-- Error State -->
      <AppCard v-if="error" class="border-red-200 bg-red-50/70 dark:border-red-900/60 dark:bg-red-950/30">
        <div class="flex flex-col items-start justify-between gap-3 sm:flex-row sm:items-center">
          <p class="text-sm font-medium text-red-700 dark:text-red-300">{{ error }}</p>
          <AppButton variant="outline" size="sm" @click="fetchData">
            Tentar novamente
          </AppButton>
        </div>
      </AppCard>

      <!-- Restaurants Discovery Section (List / Map Switch) -->
      <section class="space-y-5">
        <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
          <div>
            <h2 class="font-display text-xl font-extrabold text-ink sm:text-2xl">
              Restaurantes disponíveis
            </h2>
            <p class="text-xs text-ink-muted sm:text-sm">
              Escolha por proximidade, menor taxa ou melhor avaliação
            </p>
          </div>

          <div class="flex flex-wrap items-center gap-2">
            <!-- List / Map View Toggle -->
            <div class="inline-flex rounded-xl bg-surface-muted p-1">
              <button
                type="button"
                class="inline-flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-xs font-bold transition"
                :class="viewMode === 'list' ? 'bg-surface text-ink shadow-xs' : 'text-ink-muted hover:text-ink'"
                @click="viewMode = 'list'"
              >
                <LayoutGrid class="h-3.5 w-3.5" />
                Lista
              </button>
              <button
                type="button"
                class="inline-flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-xs font-bold transition"
                :class="viewMode === 'map' ? 'bg-surface text-ink shadow-xs' : 'text-ink-muted hover:text-ink'"
                @click="viewMode = 'map'"
              >
                <MapIcon class="h-3.5 w-3.5" />
                Mapa
              </button>
            </div>
          </div>
        </div>

        <!-- Horizontal Filter Chips -->
        <div class="flex items-center gap-2 overflow-x-auto no-scrollbar pb-1">
          <button
            v-for="chip in quickFilters"
            :key="chip.id"
            type="button"
            class="inline-flex shrink-0 items-center gap-1.5 rounded-full border px-3.5 py-2 text-xs font-semibold transition"
            :class="
              activeFilter === chip.id
                ? 'border-brand-600 bg-brand-600 text-white shadow-sm'
                : 'border-line bg-surface text-ink-secondary hover:border-line-strong hover:bg-surface-muted'
            "
            @click="activeFilter = chip.id"
          >
            <component :is="chip.icon" class="h-3.5 w-3.5" />
            <span>{{ chip.label }}</span>
            <span
              class="rounded-full px-1.5 py-0.5 text-[10px] font-bold tabular-nums"
              :class="activeFilter === chip.id ? 'bg-white/20 text-white' : 'bg-surface-muted text-ink-muted'"
            >
              {{ chip.count }}
            </span>
          </button>
        </div>

        <!-- Skeletons -->
        <div v-if="loading" class="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
          <div
            v-for="n in 6"
            :key="n"
            class="overflow-hidden rounded-2xl border border-line bg-surface"
          >
            <AppSkeleton variant="rect" height="160px" class="rounded-none" />
            <div class="space-y-3 p-5">
              <AppSkeleton width="65%" height="20px" />
              <AppSkeleton width="90%" height="14px" />
              <AppSkeleton width="45%" height="14px" />
            </div>
          </div>
        </div>

        <!-- Interactive Split Map Mode -->
        <div
          v-else-if="viewMode === 'map' && filteredRestaurants.length > 0"
          class="grid grid-cols-1 gap-4 lg:grid-cols-12 items-start"
        >
          <div class="lg:col-span-7 overflow-hidden rounded-3xl border border-line bg-surface shadow-card">
            <div class="relative h-[360px] lg:h-[480px] w-full bg-surface-muted">
              <iframe
                :key="selectedMapRestaurant?.id || 'default-map'"
                :src="selectedMapUrl"
                title="Mapa de restaurantes parceiros"
                class="h-full w-full border-0"
                loading="lazy"
              />
              <div
                v-if="selectedMapRestaurant"
                class="absolute bottom-3 left-3 right-3 rounded-2xl border border-line bg-surface/95 p-4 shadow-elevated backdrop-blur-md flex flex-col justify-between gap-3 sm:flex-row sm:items-center"
              >
                <div class="min-w-0">
                  <p class="font-display text-base font-extrabold text-ink truncate">
                    {{ selectedMapRestaurant.name }}
                  </p>
                  <p class="text-xs text-ink-secondary truncate">
                    {{ selectedMapRestaurant.addressNeighborhood || 'Região Central' }}
                    · {{ !selectedMapRestaurant.deliveryFee ? 'Entrega Grátis' : formatCurrency(selectedMapRestaurant.deliveryFee) }}
                  </p>
                </div>
                <RouterLink :to="`/restaurantes/${selectedMapRestaurant.id}`" class="shrink-0">
                  <AppButton variant="primary" size="sm">Abrir cardápio</AppButton>
                </RouterLink>
              </div>
            </div>
          </div>

          <div class="lg:col-span-5 space-y-2.5 max-h-[480px] overflow-y-auto pr-1">
            <button
              v-for="r in filteredRestaurants"
              :key="r.id"
              type="button"
              class="flex w-full items-center justify-between gap-3 rounded-2xl border p-3.5 text-left transition"
              :class="
                selectedMapRestaurant?.id === r.id
                  ? 'border-brand-600 bg-brand-50/40 ring-1 ring-brand-600/20 dark:bg-brand-950/30'
                  : 'border-line bg-surface hover:border-line-strong'
              "
              @click="mapSelectedRestaurantId = r.id"
            >
              <div class="min-w-0">
                <p class="font-display text-sm font-extrabold text-ink truncate">{{ r.name }}</p>
                <p class="text-xs text-ink-muted truncate">
                  {{ r.addressNeighborhood }} · {{ r.addressCity }}/{{ r.addressState }}
                </p>
              </div>
              <div class="shrink-0 text-right">
                <span class="inline-flex items-center gap-1 text-xs font-bold text-ink">
                  <Star class="h-3.5 w-3.5 fill-amber-400 text-amber-400" />
                  {{ (ratingByRestaurant[r.id]?.avg ?? 4.8).toFixed(1) }}
                </span>
                <p class="text-[11px] font-semibold text-fresh-700 dark:text-fresh-400">
                  {{ !r.deliveryFee ? 'Grátis' : formatCurrency(r.deliveryFee) }}
                </p>
              </div>
            </button>
          </div>
        </div>

        <!-- Restaurant Cards Grid -->
        <div
          v-else-if="filteredRestaurants.length > 0"
          class="grid gap-5 sm:grid-cols-2 lg:grid-cols-3"
        >
          <article
            v-for="restaurant in filteredRestaurants"
            :key="restaurant.id"
            class="group relative flex flex-col justify-between overflow-hidden rounded-2xl border border-line bg-surface shadow-card transition-all duration-200 hover:-translate-y-0.5 hover:border-brand-300/80 hover:shadow-card-hover dark:hover:border-brand-700/70"
          >
            <div>
              <div class="relative aspect-[16/9] w-full overflow-hidden bg-gradient-to-br from-brand-100 via-amber-50 to-surface-muted dark:from-brand-950/60 dark:via-surface-muted dark:to-surface-subtle">
                <img
                  v-if="restaurant.imageUrl"
                  :src="restaurant.imageUrl"
                  :alt="restaurant.name"
                  class="h-full w-full object-cover transition-transform duration-500 group-hover:scale-105"
                />
                <div v-else class="flex h-full w-full items-center justify-center">
                  <div class="flex h-14 w-14 items-center justify-center rounded-2xl bg-surface/80 text-brand-600 shadow-sm backdrop-blur-sm">
                    <Store class="h-7 w-7" />
                  </div>
                </div>

                <div class="absolute inset-x-3 top-3 flex items-center justify-between gap-2">
                  <span class="inline-flex items-center gap-1 rounded-full bg-surface/95 px-2.5 py-1 text-[11px] font-bold text-ink shadow-sm backdrop-blur-md">
                    <Star class="h-3 w-3 fill-amber-400 text-amber-400" />
                    <span class="tabular-nums">
                      {{ (ratingByRestaurant[restaurant.id]?.avg ?? 4.8).toFixed(1) }}
                    </span>
                  </span>

                  <button
                    v-if="!auth.isDriverOnly"
                    type="button"
                    :disabled="togglingFavId === restaurant.id"
                    :aria-label="favoriteByRestaurantId[restaurant.id] ? 'Remover dos favoritos' : 'Favoritar restaurante'"
                    class="flex h-8 w-8 items-center justify-center rounded-full bg-surface/95 text-ink-secondary shadow-sm backdrop-blur-md transition hover:scale-110"
                    @click.prevent="handleToggleFavorite(restaurant)"
                  >
                    <Heart
                      class="h-4 w-4 transition-colors"
                      :class="
                        favoriteByRestaurantId[restaurant.id]
                          ? 'fill-red-500 text-red-500'
                          : 'text-ink-muted hover:text-red-500'
                      "
                    />
                  </button>
                </div>

                <div class="absolute bottom-3 left-3 flex items-center gap-1.5">
                  <span
                    class="inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-[11px] font-bold shadow-sm backdrop-blur-md"
                    :class="
                      !restaurant.deliveryFee || restaurant.deliveryFee === 0
                        ? 'bg-fresh-600 text-white'
                        : 'bg-surface/95 text-ink'
                    "
                  >
                    <Truck class="h-3 w-3" />
                    {{
                      !restaurant.deliveryFee || restaurant.deliveryFee === 0
                        ? 'Entrega Grátis'
                        : formatCurrency(restaurant.deliveryFee)
                    }}
                  </span>
                  <span
                    v-if="restaurant.deliveryRadiusKm"
                    class="rounded-full bg-surface/90 px-2.5 py-1 text-[11px] font-semibold text-ink-secondary shadow-sm backdrop-blur-md tabular-nums"
                  >
                    Raio {{ restaurant.deliveryRadiusKm }} km
                  </span>
                </div>
              </div>

              <div class="p-5 pb-3">
                <h3 class="font-display text-base font-extrabold text-ink group-hover:text-brand-600 transition-colors">
                  {{ restaurant.name }}
                </h3>
                <p class="mt-1 line-clamp-2 text-xs leading-relaxed text-ink-secondary">
                  {{ restaurant.description || 'Marmitas caseiras frescas preparadas diariamente com ingredientes selecionados.' }}
                </p>

                <div class="mt-3 space-y-1 text-[11px] text-ink-muted">
                  <p v-if="restaurant.addressNeighborhood" class="flex items-center gap-1.5 truncate">
                    <MapPin class="h-3.5 w-3.5 shrink-0 text-brand-500" />
                    <span class="truncate">
                      {{ restaurant.addressNeighborhood }} — {{ restaurant.addressCity }}/{{ restaurant.addressState }}
                    </span>
                  </p>
                  <p v-if="restaurant.openingHours" class="flex items-center gap-1.5 truncate">
                    <Clock class="h-3.5 w-3.5 shrink-0 text-ink-muted" />
                    <span class="truncate">{{ restaurant.openingHours }}</span>
                  </p>
                </div>
              </div>
            </div>

            <div class="px-5 pb-5 pt-2">
              <RouterLink
                :to="`/restaurantes/${restaurant.id}`"
                class="flex w-full items-center justify-center gap-2 rounded-xl bg-brand-50 px-4 py-2.5 text-xs font-bold text-brand-700 transition hover:bg-brand-600 hover:text-white dark:bg-brand-950/60 dark:text-brand-300 dark:hover:bg-brand-600 dark:hover:text-white"
              >
                <span>Ver cardápio</span>
                <ArrowRight class="h-3.5 w-3.5" />
              </RouterLink>
            </div>
          </article>
        </div>

        <AppEmptyState
          v-else
          :icon="Store"
          :title="searchQuery ? 'Nenhum restaurante encontrado' : 'Nenhum restaurante disponível'"
          :description="
            searchQuery
              ? 'Tente buscar por outro bairro, nome ou limpe os filtros aplicados.'
              : 'Em breve novos restaurantes parceiros estarão disponíveis na sua região.'
          "
        >
          <template v-if="searchQuery || activeFilter !== 'all'" #action>
            <AppButton
              variant="outline"
              size="sm"
              @click="searchQuery = ''; activeFilter = 'all'"
            >
              Limpar filtros
            </AppButton>
          </template>
        </AppEmptyState>
      </section>

      <!-- Subscription Plans + Interactive Savings Simulator -->
      <section v-if="!auth.isDriverOnly" id="planos-section" class="space-y-6 scroll-mt-20">
        <div class="flex flex-col justify-between gap-2 sm:flex-row sm:items-end">
          <div>
            <AppBadge variant="fresh" size="sm" dot class="mb-2">
              Clube de Marmitas · Assinatura Recorrente
            </AppBadge>
            <h2 class="font-display text-xl font-extrabold text-ink sm:text-2xl">
              Assine e economize toda semana
            </h2>
            <p class="mt-1 text-xs text-ink-muted sm:text-sm">
              Simule quantas marmitas você pede por semana e descubra o plano ideal.
            </p>
          </div>
          <RouterLink
            v-if="auth.isAuthenticated"
            to="/conta?tab=assinaturas"
            class="text-xs font-bold text-fresh-700 hover:underline dark:text-fresh-400"
          >
            Gerenciar minhas assinaturas →
          </RouterLink>
        </div>

        <!-- Interactive Club Savings Simulator -->
        <AppCard
          v-if="plans.length > 0 && recommendedPlan"
          padding="md"
          class="border-fresh-200 bg-gradient-to-r from-fresh-50/80 via-surface to-brand-50/40 dark:border-fresh-900/60 dark:from-fresh-950/30 dark:via-surface dark:to-brand-950/20"
        >
          <div class="grid grid-cols-1 gap-5 lg:grid-cols-12 lg:items-center">
            <div class="lg:col-span-7 space-y-3">
              <div class="flex items-center justify-between">
                <label for="sim-meals" class="font-display text-sm font-extrabold text-ink">
                  Quantas marmitas você pede por semana?
                </label>
                <span class="rounded-xl bg-fresh-600 px-3 py-1 font-display text-sm font-extrabold text-white tabular-nums">
                  {{ simulatedMealsPerWeek }} / semana
                </span>
              </div>
              <input
                id="sim-meals"
                v-model.number="simulatedMealsPerWeek"
                type="range"
                min="2"
                max="10"
                step="1"
                class="w-full accent-fresh-600 cursor-pointer"
              />
              <div class="flex justify-between text-[11px] font-semibold text-ink-muted tabular-nums">
                <span>2 ref./sem</span>
                <span>5 ref./sem (Seg–Sex)</span>
                <span>10 ref./sem</span>
              </div>
            </div>

            <div class="lg:col-span-5 flex items-center justify-between gap-4 rounded-2xl border border-fresh-200 bg-surface p-4 dark:border-fresh-800/60">
              <div>
                <p class="text-[11px] font-bold uppercase tracking-wider text-ink-muted">
                  Plano recomendado: <span class="text-fresh-700 dark:text-fresh-400">{{ recommendedPlan.name }}</span>
                </p>
                <p class="mt-1 font-display text-xl font-extrabold text-fresh-700 dark:text-fresh-400 tabular-nums">
                  Economize {{ formatCurrency(simulatedSavings) }}/mês
                </p>
                <p class="text-[11px] text-ink-muted tabular-nums">
                  vs. avulso médio ({{ formatCurrency(avgSingleMealPrice) }}/un.)
                </p>
              </div>
              <AppBadge v-if="simulatedSavingsPercent > 0" variant="fresh" size="sm">
                -{{ simulatedSavingsPercent }}%
              </AppBadge>
            </div>
          </div>
        </AppCard>

        <div v-if="loading" class="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
          <AppSkeleton v-for="n in 3" :key="n" variant="card" height="280px" />
        </div>

        <div
          v-else-if="plans.length > 0"
          class="grid gap-5 sm:grid-cols-2 lg:grid-cols-3"
        >
          <div
            v-for="plan in plans"
            :key="plan.id"
            class="relative flex flex-col justify-between rounded-2xl border p-6 transition-all duration-200"
            :class="
              recommendedPlan?.id === plan.id
                ? 'border-fresh-500 bg-surface shadow-elevated ring-2 ring-fresh-500/30'
                : 'border-line bg-surface shadow-card hover:border-line-strong'
            "
          >
            <div>
              <div class="flex items-center justify-between gap-2">
                <AppBadge variant="fresh" size="sm">
                  {{ plan.mealsPerWeek }} marmitas / semana
                </AppBadge>
                <AppBadge v-if="recommendedPlan?.id === plan.id" variant="brand" size="xs">
                  Recomendado p/ você
                </AppBadge>
              </div>

              <h3 class="mt-4 font-display text-xl font-extrabold text-ink">
                {{ plan.name }}
              </h3>
              <p class="mt-1.5 text-sm leading-relaxed text-ink-secondary">
                {{ plan.description || 'Refeições balanceadas entregues na janela ideal para sua rotina.' }}
              </p>

              <div class="mt-5 rounded-2xl bg-surface-subtle p-4 border border-line">
                <div class="flex items-baseline gap-1">
                  <span class="font-display text-3xl font-extrabold text-ink tabular-nums">
                    {{ formatCurrency(plan.monthlyPrice) }}
                  </span>
                  <span class="text-xs font-medium text-ink-muted">/mês</span>
                </div>
                <p class="mt-1 text-xs font-bold text-fresh-700 dark:text-fresh-400 tabular-nums">
                  Apenas {{ formatCurrency(getPricePerMeal(plan)) }} por marmita
                  <span class="font-normal text-ink-muted">({{ getMealsPerMonth(plan) }} ref./mês)</span>
                </p>
              </div>

              <ul class="mt-4 space-y-2 text-xs text-ink-secondary">
                <li class="flex items-center gap-2">
                  <Check class="h-4 w-4 shrink-0 text-fresh-600" />
                  <span>Resgate direto no cardápio com valor base R$ 0,00</span>
                </li>
                <li v-if="plan.deliveryWindow" class="flex items-center gap-2">
                  <Clock class="h-4 w-4 shrink-0 text-fresh-600" />
                  <span>Janela: {{ plan.deliveryWindow }}</span>
                </li>
                <li v-if="plan.allowedDays" class="flex items-center gap-2">
                  <CalendarDays class="h-4 w-4 shrink-0 text-fresh-600" />
                  <span>Dias: {{ plan.allowedDays }}</span>
                </li>
                <li class="flex items-center gap-2">
                  <Check class="h-4 w-4 shrink-0 text-fresh-600" />
                  <span>Pausa, upgrade ou cancelamento flexível</span>
                </li>
              </ul>
            </div>

            <div class="mt-6 pt-2">
              <AppButton
                :variant="recommendedPlan?.id === plan.id ? 'fresh' : 'secondary'"
                size="lg"
                block
                :loading="subscribingPlanId === plan.id"
                @click="handleSubscribe(plan)"
              >
                Assinar plano
              </AppButton>
            </div>
          </div>
        </div>

        <AppEmptyState
          v-else
          :icon="Sparkles"
          title="Nenhum plano ativo no momento"
          description="Novos planos de assinatura recorrente estarão disponíveis em breve."
        />
      </section>

      <!-- Social Proof Editorial Strip (Reviews) -->
      <section class="space-y-5 pb-4">
        <div class="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 class="font-display text-xl font-extrabold text-ink sm:text-2xl">
              O que nossos clientes dizem
            </h2>
            <p class="text-xs text-ink-muted sm:text-sm">
              Nota média da comunidade:
              <strong class="text-ink tabular-nums">{{ platformAvgRating.toFixed(1) }}/5.0</strong>
              ({{ customerReviews.length }} avaliações verificadas)
            </p>
          </div>
          <AppBadge variant="fresh" size="sm">Mural Público</AppBadge>
        </div>

        <div v-if="loading" class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <AppSkeleton v-for="n in 3" :key="n" variant="card" height="140px" />
        </div>

        <div
          v-else-if="customerReviews.length > 0"
          class="flex gap-4 overflow-x-auto no-scrollbar pb-2 snap-x"
        >
          <AppCard
            v-for="rev in customerReviews.slice(0, 12)"
            :key="rev.id"
            padding="md"
            class="w-[300px] sm:w-[340px] shrink-0 snap-start"
          >
            <div class="flex items-start justify-between gap-3">
              <div class="flex items-center gap-3 min-w-0">
                <span class="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-brand-100 font-display text-xs font-extrabold text-brand-800 dark:bg-brand-900/60 dark:text-brand-200">
                  {{ getInitials(rev.authorName) }}
                </span>
                <div class="min-w-0">
                  <p class="truncate text-sm font-bold text-ink">
                    {{ rev.authorName || 'Cliente Verificado' }}
                  </p>
                  <p class="truncate text-xs font-semibold text-brand-600 dark:text-brand-400">
                    {{ rev.restaurantName || 'Restaurante Parceiro' }}
                  </p>
                </div>
              </div>
              <StarRating :model-value="rev.rating" readonly size="sm" />
            </div>

            <p class="mt-3 text-sm leading-relaxed text-ink-secondary">
              “{{ rev.comment }}”
            </p>
          </AppCard>
        </div>

        <AppEmptyState
          v-else
          :icon="Star"
          title="Ainda sem avaliações públicas"
          description="Faça seu primeiro pedido e compartilhe sua experiência com a comunidade."
        />
      </section>
    </div>
  </CustomerLayout>
</template>
