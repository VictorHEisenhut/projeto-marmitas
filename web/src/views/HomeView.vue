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
  MapPin,
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
import { useToastStore } from '@/stores/toast'
import type { Favorite, Plan, Restaurant, ReviewItem } from '@/types'

const auth = useAuthStore()
const toast = useToastStore()
const route = useRoute()
const router = useRouter()

const restaurants = ref<Restaurant[]>([])
const plans = ref<Plan[]>([])
const customerReviews = ref<ReviewItem[]>([])
const favorites = ref<Favorite[]>([])
const searchQuery = ref(typeof route.query.q === 'string' ? route.query.q : '')
const activeFilter = ref<'all' | 'top_rated' | 'low_fee' | 'plan_ready'>('all')
const planRestaurantFilter = ref('')
const loading = ref(true)
const error = ref('')
const subscribingPlanId = ref<string | null>(null)
const togglingFavId = ref<string | null>(null)

const restaurantsWithPlans = computed(() => {
  const set = new Set<string>()
  for (const p of plans.value) {
    if (p.restaurantId) set.add(p.restaurantId)
  }
  return set
})

const restaurantNameById = computed(() => {
  const map: Record<string, string> = {}
  for (const r of restaurants.value) {
    map[r.id] = r.name
  }
  return map
})

function getPlanRestaurantName(plan: Plan): string {
  return plan.restaurantName || restaurantNameById.value[plan.restaurantId] || 'Restaurante Parceiro'
}

const planFilterRestaurants = computed(() => {
  const seen = new Map<string, string>()
  for (const p of plans.value) {
    if (p.restaurantId && !seen.has(p.restaurantId)) {
      seen.set(p.restaurantId, getPlanRestaurantName(p))
    }
  }
  return Array.from(seen.entries()).map(([id, name]) => ({ id, name }))
})

const filteredPlans = computed(() => {
  if (!planRestaurantFilter.value) return plans.value
  return plans.value.filter((p) => p.restaurantId === planRestaurantFilter.value)
})

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

const quickFilters = [
  { id: 'all' as const, label: 'Todos', icon: Store },
  { id: 'top_rated' as const, label: 'Mais bem avaliados', icon: Star },
  { id: 'low_fee' as const, label: 'Entrega grátis ou reduzida', icon: Truck },
  { id: 'plan_ready' as const, label: 'Clube de Planos', icon: Sparkles },
]

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
  } else if (activeFilter.value === 'plan_ready') {
    list = list.filter((r) => restaurantsWithPlans.value.has(r.id))
  } else if (activeFilter.value === 'top_rated') {
    list.sort((a, b) => {
      const ra = ratingByRestaurant.value[a.id]?.avg ?? 4.6
      const rb = ratingByRestaurant.value[b.id]?.avg ?? 4.6
      return rb - ra
    })
  }

  return list
})

function getMealsPerMonth(plan: Plan): number {
  return plan.mealsPerMonth && plan.mealsPerMonth > 0
    ? plan.mealsPerMonth
    : Math.max(1, plan.mealsPerWeek * 4)
}

function getPricePerMeal(plan: Plan): number {
  return plan.monthlyPrice / getMealsPerMonth(plan)
}

function isPopularPlan(plan: Plan, index: number): boolean {
  if (plans.value.length >= 3) return index === 1
  if (plans.value.length === 2) return index === 1
  return plan.mealsPerWeek >= 5
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
      `Assinatura do plano "${plan.name}" ativada com sucesso! Acesse Minha Conta para gerenciar.`,
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
      <!-- Hero Banner -->
      <section
        class="relative overflow-hidden rounded-3xl bg-gradient-to-br from-brand-700 via-brand-600 to-amber-500 p-6 text-white shadow-elevated sm:p-10"
      >
        <div
          class="pointer-events-none absolute -right-20 -top-20 h-72 w-72 rounded-full bg-white/15 blur-3xl"
        />
        <div
          class="pointer-events-none absolute -bottom-24 right-1/3 h-64 w-64 rounded-full bg-amber-300/20 blur-3xl"
        />

        <div class="relative z-10 max-w-3xl space-y-5">
          <div class="inline-flex items-center gap-2 rounded-full bg-white/15 px-3.5 py-1 text-xs font-bold backdrop-blur-md">
            <Flame class="h-3.5 w-3.5 text-amber-200" />
            <span>
              {{ auth.isDriverOnly ? 'Modo Consulta para Entregadores' : 'Comida fresca preparada hoje' }}
            </span>
          </div>

          <h1 class="font-display text-3xl font-extrabold leading-tight tracking-tight sm:text-4xl lg:text-5xl">
            {{
              auth.isDriverOnly
                ? 'Cardápios dos Restaurantes Parceiros'
                : 'Marmitas caseiras frescas e planos que cabem na sua rotina'
            }}
          </h1>

          <p class="max-w-2xl text-sm leading-relaxed text-white/90 sm:text-base">
            {{
              auth.isDriverOnly
                ? 'Consulte os estabelecimentos ativos, pratos preparados e o mural público de avaliações dos clientes.'
                : 'Peça sua marmita avulsa com entrega rastreada ao vivo ou assine o Clube de Marmitas para economizar toda semana.'
            }}
          </p>

          <!-- Integrated Search + Plans CTA -->
          <div class="flex flex-col gap-3 pt-1 sm:flex-row sm:items-center">
            <div class="relative flex-1 max-w-xl">
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
              <span>Conhecer Planos</span>
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

      <!-- Discovery Filters & Restaurants Grid -->
      <section class="space-y-5">
        <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
          <div>
            <h2 class="font-display text-xl font-extrabold text-ink sm:text-2xl">
              Restaurantes disponíveis
            </h2>
            <p class="text-xs text-ink-muted sm:text-sm">
              Cozinhas parceiras selecionadas com preparo diário
            </p>
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
            </button>
          </div>
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
              <!-- 16:9 Cover Image -->
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

                <!-- Overlay Badges -->
                <div class="absolute inset-x-3 top-3 flex items-center justify-between gap-2">
                  <span
                    class="inline-flex items-center gap-1 rounded-full bg-surface/95 px-2.5 py-1 text-[11px] font-bold text-ink shadow-sm backdrop-blur-md"
                  >
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

                <!-- Fee & Delivery Time Pill -->
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
                  <span
                    v-if="restaurantsWithPlans.has(restaurant.id)"
                    class="rounded-full bg-brand-600/95 px-2.5 py-1 text-[11px] font-bold text-white shadow-sm backdrop-blur-md"
                  >
                    Tem planos
                  </span>
                </div>
              </div>

              <!-- Info Body -->
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

      <!-- Subscription Plans Showcase (Per-Restaurant Plans) -->
      <section v-if="!auth.isDriverOnly" id="planos-section" class="space-y-6 scroll-mt-20">
        <div class="flex flex-col justify-between gap-3 sm:flex-row sm:items-end">
          <div>
            <AppBadge variant="fresh" size="sm" dot class="mb-2">
              Planos por Restaurante · Assinatura Recorrente
            </AppBadge>
            <h2 class="font-display text-xl font-extrabold text-ink sm:text-2xl">
              Escolha o restaurante e garanta almoços toda semana
            </h2>
            <p class="mt-1 text-xs text-ink-muted sm:text-sm">
              Cada loja define sua própria franquia, valores e cardápio elegível para resgate com base R$ 0,00.
            </p>
          </div>
          <div class="flex flex-wrap items-center gap-3">
            <select
              v-if="planFilterRestaurants.length > 0"
              v-model="planRestaurantFilter"
              aria-label="Filtrar planos por restaurante"
              class="rounded-xl border border-line-strong bg-surface px-3 py-2 text-xs font-semibold text-ink"
            >
              <option value="">Todos os restaurantes</option>
              <option v-for="r in planFilterRestaurants" :key="r.id" :value="r.id">
                {{ r.name }}
              </option>
            </select>
            <RouterLink
              v-if="auth.isAuthenticated"
              to="/conta?tab=assinaturas"
              class="text-xs font-bold text-fresh-700 hover:underline dark:text-fresh-400"
            >
              Gerenciar minhas assinaturas →
            </RouterLink>
          </div>
        </div>

        <div v-if="loading" class="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
          <AppSkeleton v-for="n in 3" :key="n" variant="card" height="280px" />
        </div>

        <div
          v-else-if="filteredPlans.length > 0"
          class="grid gap-5 sm:grid-cols-2 lg:grid-cols-3"
        >
          <div
            v-for="(plan, idx) in filteredPlans"
            :key="plan.id"
            class="relative flex flex-col justify-between rounded-2xl border p-6 transition-all duration-200"
            :class="
              isPopularPlan(plan, idx)
                ? 'border-fresh-500 bg-surface shadow-elevated ring-1 ring-fresh-500/30'
                : 'border-line bg-surface shadow-card hover:border-line-strong'
            "
          >
            <div>
              <div class="flex items-center justify-between gap-2">
                <AppBadge variant="fresh" size="sm">
                  {{ plan.mealsPerWeek }} marmitas / semana
                </AppBadge>
                <AppBadge v-if="isPopularPlan(plan, idx)" variant="brand" size="xs">
                  Mais popular
                </AppBadge>
              </div>

              <div v-if="plan.restaurantId" class="mt-3">
                <RouterLink
                  :to="`/restaurantes/${plan.restaurantId}`"
                  class="inline-flex items-center gap-1.5 rounded-full border border-brand-200 bg-brand-50/70 px-2.5 py-1 text-[11px] font-bold text-brand-700 transition hover:bg-brand-100 dark:border-brand-800/60 dark:bg-brand-950/40 dark:text-brand-300"
                >
                  <Store class="h-3 w-3" />
                  <span>{{ getPlanRestaurantName(plan) }}</span>
                </RouterLink>
              </div>

              <h3 class="mt-3 font-display text-xl font-extrabold text-ink">
                {{ plan.name }}
              </h3>
              <p class="mt-1.5 text-sm leading-relaxed text-ink-secondary">
                {{ plan.description || 'Refeições balanceadas entregues na janela ideal para sua rotina.' }}
              </p>

              <!-- Pricing Highlight -->
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

              <!-- Plan Benefits -->
              <ul class="mt-4 space-y-2 text-xs text-ink-secondary">
                <li class="flex items-center gap-2">
                  <Check class="h-4 w-4 shrink-0 text-fresh-600" />
                  <span>Resgate direto no cardápio de {{ getPlanRestaurantName(plan) }}</span>
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
                :variant="isPopularPlan(plan, idx) ? 'fresh' : 'secondary'"
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

      <!-- Public Customer Reviews Wall -->
      <section class="space-y-5 pb-4">
        <div class="flex flex-wrap items-center justify-between gap-2">
          <div>
            <h2 class="font-display text-xl font-extrabold text-ink sm:text-2xl">
              O que nossos clientes dizem
            </h2>
            <p class="text-xs text-ink-muted sm:text-sm">
              Avaliações reais de pedidos e entregas realizadas na plataforma
            </p>
          </div>
          <AppBadge variant="fresh" size="sm">Mural Público</AppBadge>
        </div>

        <div v-if="loading" class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <AppSkeleton v-for="n in 3" :key="n" variant="card" height="140px" />
        </div>

        <div
          v-else-if="customerReviews.length > 0"
          class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3"
        >
          <AppCard v-for="rev in customerReviews.slice(0, 9)" :key="rev.id" padding="md">
            <div class="flex items-start justify-between gap-3">
              <div class="flex items-center gap-3 min-w-0">
                <span
                  class="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-brand-100 font-display text-xs font-extrabold text-brand-800 dark:bg-brand-900/60 dark:text-brand-200"
                >
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
