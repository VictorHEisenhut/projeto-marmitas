<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  ArrowLeft,
  CalendarDays,
  Check,
  Clock,
  Heart,
  MapPin,
  Plus,
  Search,
  Sparkles,
  Store,
  Truck,
  UtensilsCrossed,
} from 'lucide-vue-next'
import CustomerLayout from '@/layouts/CustomerLayout.vue'
import AiMarmitaAssistantModal from '@/components/AiMarmitaAssistantModal.vue'
import DishCustomizeModal from '@/components/customer/DishCustomizeModal.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppSkeleton from '@/components/ui/AppSkeleton.vue'
import AppEmptyState from '@/components/ui/AppEmptyState.vue'
import AppConfirmDialog from '@/components/ui/AppConfirmDialog.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useCartStore, type SelectedAddon } from '@/stores/cart'
import { useToastStore } from '@/stores/toast'
import type { Category, Favorite, Plan, Product, Restaurant, Subscription } from '@/types'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const cart = useCartStore()
const toast = useToastStore()

const restaurantId = computed(() => String(route.params.id || ''))
const restaurant = ref<Restaurant | null>(null)
const categories = ref<Category[]>([])
const products = ref<Product[]>([])
const restaurantPlans = ref<Plan[]>([])
const mySubscriptions = ref<Subscription[]>([])
const favoriteEntry = ref<Favorite | null>(null)
const selectedCategory = ref('')
const searchDish = ref('')
const isPlanMode = ref(cart.isPlanRedemption)
const loading = ref(true)
const error = ref('')
const togglingFav = ref(false)
const subscribingPlanId = ref<string | null>(null)
const isAiModalOpen = ref(false)

const activeRestaurantSubscription = computed(
  () =>
    mySubscriptions.value.find(
      (s) =>
        s.status === 'Active' &&
        (s.restaurantId === restaurantId.value || s.preferredRestaurantId === restaurantId.value),
    ) || null,
)

// Dish customize modal
const selectedDish = ref<Product | null>(null)
const dishModalOpen = ref(false)

// Confirm restaurant switch dialog
const pendingCartAction = ref<{
  product: Product
  quantity: number
  selectedAddons: SelectedAddon[]
  wantsCutlery: boolean
} | null>(null)

const activeCategories = computed(() => {
  const usedIds = new Set(products.value.filter((p) => p.isActive).map((p) => p.categoryId))
  return categories.value.filter((c) => usedIds.has(c.id))
})

const filteredProducts = computed(() => {
  let list = products.value.filter((p) => p.isActive)
  if (isPlanMode.value) {
    list = list.filter((p) => p.availableInPlans !== false)
  }
  if (selectedCategory.value) {
    list = list.filter((p) => p.categoryId === selectedCategory.value)
  }
  const q = searchDish.value.trim().toLowerCase()
  if (q) {
    list = list.filter(
      (p) =>
        p.name.toLowerCase().includes(q) ||
        (p.description && p.description.toLowerCase().includes(q)) ||
        (p.categoryName && p.categoryName.toLowerCase().includes(q)),
    )
  }
  return list
})

function togglePlanMode(enabled: boolean) {
  isPlanMode.value = enabled
  cart.setPlanMode(enabled)
}

function openDishModal(product: Product) {
  selectedDish.value = product
  dishModalOpen.value = true
}

function executeAddToCart(payload: {
  product: Product
  quantity: number
  selectedAddons: SelectedAddon[]
  wantsCutlery: boolean
}) {
  if (!restaurant.value) return

  cart.addItem(restaurant.value, payload.product, {
    selectedAddons: payload.selectedAddons,
    wantsCutlery: payload.wantsCutlery,
    isPlanRedemption: isPlanMode.value,
    quantity: payload.quantity,
  })

  dishModalOpen.value = false
  pendingCartAction.value = null

  toast.success(
    isPlanMode.value
      ? `"${payload.product.name}" adicionada para resgate no Plano Mensal.`
      : `"${payload.product.name}" adicionada à sua sacola.`,
  )
}

function handleDishConfirm(payload: {
  product: Product
  quantity: number
  selectedAddons: SelectedAddon[]
  wantsCutlery: boolean
}) {
  if (!restaurant.value) return
  if (cart.hasDifferentRestaurant(restaurant.value.id)) {
    pendingCartAction.value = payload
    return
  }
  executeAddToCart(payload)
}

function handleAddFromAi(product: Product) {
  isAiModalOpen.value = false
  if (auth.isDriverOnly) return
  openDishModal(product)
}

async function loadMenu() {
  loading.value = true
  error.value = ''

  try {
    const requests: Promise<unknown>[] = [
      api.get<Restaurant>(`/restaurantes/${restaurantId.value}`),
      api.get<Product[]>(`/restaurantes/${restaurantId.value}/cardapio`),
      api.get<Category[]>('/categorias'),
      api.get<Plan[]>('/planos', { params: { restaurantId: restaurantId.value } }),
    ]

    if (auth.isAuthenticated && !auth.isDriverOnly) {
      requests.push(api.get<Favorite[]>('/usuarios/me/favoritos'))
      requests.push(api.get<Subscription[]>('/assinaturas'))
    }

    const results = await Promise.allSettled(requests)
    if (results[0]?.status === 'fulfilled') {
      restaurant.value = (results[0].value as { data: Restaurant }).data
    } else {
      throw (results[0] as PromiseRejectedResult)?.reason
    }

    if (results[1]?.status === 'fulfilled') {
      products.value = (results[1].value as { data: Product[] }).data
    }
    if (results[2]?.status === 'fulfilled') {
      categories.value = (results[2].value as { data: Category[] }).data
    }
    if (results[3]?.status === 'fulfilled') {
      restaurantPlans.value = (results[3].value as { data: Plan[] }).data
    }
    if (results[4]?.status === 'fulfilled') {
      const favs = (results[4].value as { data: Favorite[] }).data
      favoriteEntry.value = favs.find((f) => f.restaurantId === restaurantId.value) ?? null
    }
    if (results[5]?.status === 'fulfilled') {
      mySubscriptions.value = (results[5].value as { data: Subscription[] }).data
    }
  } catch (err) {
    error.value = getErrorMessage(err, 'Não foi possível carregar o cardápio do restaurante.')
  } finally {
    loading.value = false
  }
}

function getMealsPerMonth(plan: Plan): number {
  return plan.mealsPerMonth && plan.mealsPerMonth > 0
    ? plan.mealsPerMonth
    : Math.max(1, plan.mealsPerWeek * 4)
}

function getPricePerMeal(plan: Plan): number {
  return plan.monthlyPrice / getMealsPerMonth(plan)
}

function scrollToRestaurantPlans() {
  document.getElementById('restaurante-planos')?.scrollIntoView({ behavior: 'smooth' })
}

async function handleSubscribe(plan: Plan) {
  if (!auth.isAuthenticated) {
    await router.push({ path: '/login', query: { redirect: route.fullPath } })
    return
  }

  subscribingPlanId.value = plan.id
  try {
    const { data } = await api.post<Subscription>('/assinaturas', { planId: plan.id })
    mySubscriptions.value = [data, ...mySubscriptions.value]
    togglePlanMode(true)
    toast.success(`Assinatura do plano "${plan.name}" ativada para ${restaurant.value?.name}!`)
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível assinar o plano.'))
  } finally {
    subscribingPlanId.value = null
  }
}

watch(restaurantId, (id) => {
  if (id) {
    void loadMenu()
  }
})

async function handleToggleFavorite() {
  if (!restaurant.value) return
  if (!auth.isAuthenticated) {
    toast.info('Faça login para salvar este restaurante nos favoritos.')
    void router.push({ path: '/login', query: { redirect: route.fullPath } })
    return
  }

  togglingFav.value = true
  try {
    if (favoriteEntry.value) {
      await api.delete(`/usuarios/me/favoritos/${favoriteEntry.value.id}`)
      favoriteEntry.value = null
      toast.info('Restaurante removido dos seus favoritos.')
    } else {
      const { data } = await api.post<Favorite>('/usuarios/me/favoritos', {
        restaurantId: restaurant.value.id,
      })
      favoriteEntry.value = data
      toast.success('Restaurante salvo nos seus favoritos!')
    }
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível atualizar os favoritos.'))
  } finally {
    togglingFav.value = false
  }
}

onMounted(() => {
  void loadMenu()
})
</script>

<template>
  <CustomerLayout>
    <div class="mx-auto max-w-7xl px-4 py-5 sm:px-6 space-y-6">
      <!-- Back link -->
      <div>
        <RouterLink
          to="/"
          class="inline-flex items-center gap-1.5 text-xs font-bold text-ink-secondary transition hover:text-brand-600"
        >
          <ArrowLeft class="h-4 w-4" />
          Voltar para restaurantes
        </RouterLink>
      </div>

      <!-- Loading Skeleton -->
      <div v-if="loading" class="space-y-6">
        <AppSkeleton variant="card" height="220px" />
        <div class="grid gap-4 md:grid-cols-2">
          <AppSkeleton v-for="n in 4" :key="n" variant="card" height="132px" />
        </div>
      </div>

      <!-- Error -->
      <AppEmptyState
        v-else-if="error"
        :icon="Store"
        title="Não foi possível abrir este cardápio"
        :description="error"
      >
        <template #action>
          <AppButton variant="primary" size="sm" @click="loadMenu">
            Tentar novamente
          </AppButton>
        </template>
      </AppEmptyState>

      <template v-else-if="restaurant">
        <!-- Storefront Hero Header -->
        <section class="overflow-hidden rounded-3xl border border-line bg-surface shadow-card">
          <div class="relative h-44 sm:h-56 w-full bg-gradient-to-br from-brand-700 via-brand-600 to-amber-500">
            <img
              v-if="restaurant.imageUrl"
              :src="restaurant.imageUrl"
              :alt="restaurant.name"
              class="h-full w-full object-cover"
            />
            <div class="absolute inset-0 bg-gradient-to-t from-black/75 via-black/30 to-transparent" />

            <div class="absolute inset-x-0 bottom-0 flex flex-col justify-between gap-4 p-5 sm:flex-row sm:items-end sm:p-6">
              <div class="space-y-1.5 text-white min-w-0">
                <div class="flex flex-wrap items-center gap-2">
                  <span class="inline-flex items-center gap-1 rounded-full bg-white/20 px-2.5 py-0.5 text-[11px] font-bold backdrop-blur-md">
                    <Truck class="h-3 w-3" />
                    {{
                      !restaurant.deliveryFee || restaurant.deliveryFee === 0
                        ? 'Entrega Grátis'
                        : `Taxa base ${formatCurrency(restaurant.deliveryFee)}`
                    }}
                  </span>
                  <span
                    v-if="restaurant.deliveryRadiusKm"
                    class="rounded-full bg-white/20 px-2.5 py-0.5 text-[11px] font-semibold backdrop-blur-md"
                  >
                    Raio {{ restaurant.deliveryRadiusKm }} km
                  </span>
                </div>
                <h1 class="font-display text-2xl font-extrabold tracking-tight sm:text-3xl">
                  {{ restaurant.name }}
                </h1>
                <p class="max-w-2xl text-xs text-white/85 sm:text-sm">
                  {{ restaurant.description || 'Refeições caseiras preparadas diariamente.' }}
                </p>
              </div>

              <div class="flex flex-wrap items-center gap-2 shrink-0">
                <button
                  type="button"
                  class="inline-flex h-10 items-center gap-2 rounded-xl bg-gradient-to-r from-brand-500 to-amber-500 px-4 text-xs font-bold text-white shadow-lg transition hover:brightness-110"
                  @click="isAiModalOpen = true"
                >
                  <Sparkles class="h-4 w-4" />
                  <span>Chef & Nutricionista IA</span>
                </button>

                <button
                  v-if="!auth.isDriverOnly"
                  type="button"
                  :disabled="togglingFav"
                  :aria-label="favoriteEntry ? 'Remover dos favoritos' : 'Favoritar restaurante'"
                  class="inline-flex h-10 items-center gap-1.5 rounded-xl bg-white/95 px-3.5 text-xs font-bold text-stone-900 shadow-sm transition hover:bg-white"
                  @click="handleToggleFavorite"
                >
                  <Heart
                    class="h-4 w-4"
                    :class="favoriteEntry ? 'fill-red-500 text-red-500' : 'text-stone-600'"
                  />
                  <span>{{ favoriteEntry ? 'Favoritado' : 'Favoritar' }}</span>
                </button>
              </div>
            </div>
          </div>

          <!-- Store Details Bar -->
          <div class="flex flex-wrap items-center gap-x-6 gap-y-2 border-t border-line bg-surface-subtle px-5 py-3 text-xs text-ink-secondary sm:px-6">
            <span v-if="restaurant.addressStreet" class="inline-flex items-center gap-1.5">
              <MapPin class="h-3.5 w-3.5 text-brand-600" />
              {{ restaurant.addressStreet }}, {{ restaurant.addressNumber }} — {{ restaurant.addressNeighborhood }}, {{ restaurant.addressCity }}/{{ restaurant.addressState }}
            </span>
            <span v-if="restaurant.openingHours" class="inline-flex items-center gap-1.5">
              <Clock class="h-3.5 w-3.5 text-ink-muted" />
              {{ restaurant.openingHours }}
            </span>
          </div>
        </section>

        <!-- Purchase Mode Switch (Individual vs Monthly Plan) -->
        <section
          v-if="!auth.isDriverOnly"
          class="space-y-3 rounded-2xl border border-line bg-surface p-4 shadow-card"
        >
          <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
            <div class="space-y-1">
              <div class="flex flex-wrap items-center gap-2">
                <p class="font-display text-sm font-bold text-ink">Modalidade do pedido</p>
                <AppBadge v-if="activeRestaurantSubscription" variant="fresh" size="xs">
                  Assinatura ativa: {{ activeRestaurantSubscription.planName }}
                </AppBadge>
              </div>
              <p class="text-xs text-ink-muted">
                {{
                  isPlanMode
                    ? 'Exibindo apenas marmitas elegíveis ao plano deste restaurante (base R$ 0,00 — apenas extras pagos).'
                    : 'Compre avulso ou alterne para resgatar refeições de um plano ativo nesta loja.'
                }}
              </p>
            </div>

            <div class="inline-flex rounded-xl bg-surface-muted p-1 self-start sm:self-auto">
              <button
                type="button"
                class="rounded-lg px-4 py-2 text-xs font-bold transition"
                :class="
                  !isPlanMode
                    ? 'bg-surface text-ink shadow-sm'
                    : 'text-ink-muted hover:text-ink'
                "
                @click="togglePlanMode(false)"
              >
                Compra Avulsa
              </button>
              <button
                type="button"
                class="inline-flex items-center gap-1.5 rounded-lg px-4 py-2 text-xs font-bold transition"
                :class="
                  isPlanMode
                    ? 'bg-fresh-600 text-white shadow-sm'
                    : 'text-ink-muted hover:text-ink'
                "
                @click="togglePlanMode(true)"
              >
                <Sparkles class="h-3.5 w-3.5" />
                Resgate do Plano
              </button>
            </div>
          </div>

          <div
            v-if="isPlanMode && !activeRestaurantSubscription"
            class="flex flex-wrap items-center justify-between gap-2 rounded-xl border border-amber-200 bg-amber-50/70 px-3.5 py-2.5 text-xs text-amber-900 dark:border-amber-800/60 dark:bg-amber-950/30 dark:text-amber-200"
          >
            <span>
              {{
                restaurantPlans.length > 0
                  ? `Você ainda não possui uma assinatura ativa em ${restaurant.name}.`
                  : `${restaurant.name} ainda não disponibilizou planos de assinatura.`
              }}
            </span>
            <button
              v-if="restaurantPlans.length > 0"
              type="button"
              class="font-bold underline"
              @click="scrollToRestaurantPlans"
            >
              Ver planos desta loja
            </button>
          </div>
        </section>

        <!-- Sticky Category Bar + Dish Search -->
        <div
          class="sticky top-16 z-20 -mx-4 border-y border-line bg-canvas/95 px-4 py-3 backdrop-blur-md sm:-mx-6 sm:px-6"
        >
          <div class="flex flex-col justify-between gap-3 sm:flex-row sm:items-center">
            <div class="flex items-center gap-2 overflow-x-auto no-scrollbar">
              <button
                type="button"
                class="shrink-0 rounded-full border px-3.5 py-1.5 text-xs font-bold transition"
                :class="
                  selectedCategory === ''
                    ? 'border-ink bg-ink text-canvas'
                    : 'border-line bg-surface text-ink-secondary hover:bg-surface-muted'
                "
                @click="selectedCategory = ''"
              >
                Todas
              </button>
              <button
                v-for="cat in activeCategories"
                :key="cat.id"
                type="button"
                class="shrink-0 rounded-full border px-3.5 py-1.5 text-xs font-bold transition"
                :class="
                  selectedCategory === cat.id
                    ? 'border-ink bg-ink text-canvas'
                    : 'border-line bg-surface text-ink-secondary hover:bg-surface-muted'
                "
                @click="selectedCategory = cat.id"
              >
                {{ cat.name }}
              </button>
            </div>

            <div class="relative w-full sm:w-64 shrink-0">
              <Search class="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-muted" />
              <input
                v-model="searchDish"
                type="search"
                placeholder="Buscar no cardápio..."
                class="w-full rounded-xl border border-line bg-surface py-2 pl-9 pr-3 text-xs text-ink placeholder:text-ink-muted focus:border-brand-600 focus:outline-none"
              />
            </div>
          </div>
        </div>

        <!-- iFood-style Horizontal Dish Cards Grid -->
        <section v-if="filteredProducts.length > 0" class="grid gap-4 md:grid-cols-2">
          <div
            v-for="product in filteredProducts"
            :key="product.id"
            role="button"
            tabindex="0"
            class="group flex justify-between gap-4 rounded-2xl border border-line bg-surface p-4 shadow-card transition-all duration-150 hover:border-brand-300 hover:shadow-card-hover dark:hover:border-brand-700/70 cursor-pointer"
            @click="openDishModal(product)"
            @keyup.enter="openDishModal(product)"
          >
            <!-- Left Text Info -->
            <div class="flex min-w-0 flex-1 flex-col justify-between">
              <div>
                <div class="flex flex-wrap items-center gap-1.5">
                  <span
                    v-if="product.categoryName"
                    class="text-[11px] font-bold uppercase tracking-wide text-ink-muted"
                  >
                    {{ product.categoryName }}
                  </span>
                  <AppBadge
                    v-if="isPlanMode"
                    variant="fresh"
                    size="xs"
                  >
                    Incluso no Plano
                  </AppBadge>
                  <AppBadge
                    v-else-if="product.availableInPlans !== false"
                    variant="fresh"
                    size="xs"
                  >
                    Aceita Plano
                  </AppBadge>
                  <AppBadge
                    v-if="product.dailyStock != null && product.dailyStock <= 10"
                    variant="warning"
                    size="xs"
                  >
                    {{ product.dailyStock }} un.
                  </AppBadge>
                </div>

                <h2 class="mt-1.5 font-display text-base font-extrabold text-ink group-hover:text-brand-600 transition-colors">
                  {{ product.name }}
                </h2>
                <p class="mt-1 line-clamp-2 text-xs leading-relaxed text-ink-secondary">
                  {{ product.description || 'Marmita fresca preparada no dia.' }}
                </p>
              </div>

              <div class="mt-3 flex flex-wrap items-center justify-between gap-2 pt-1">
                <div class="flex items-baseline gap-2">
                  <template v-if="isPlanMode">
                    <span class="font-display text-base font-extrabold text-fresh-700 dark:text-fresh-400 tabular-nums">
                      R$ 0,00
                    </span>
                    <span class="text-[11px] text-ink-muted">base plano</span>
                  </template>
                  <template v-else-if="product.promotionalPrice && product.promotionalPrice < product.price">
                    <span class="font-display text-base font-extrabold text-brand-600 tabular-nums">
                      {{ formatCurrency(product.promotionalPrice) }}
                    </span>
                    <span class="text-xs text-ink-muted line-through tabular-nums">
                      {{ formatCurrency(product.price) }}
                    </span>
                  </template>
                  <template v-else>
                    <span class="font-display text-base font-extrabold text-ink tabular-nums">
                      {{ formatCurrency(product.price) }}
                    </span>
                  </template>
                </div>

                <span
                  v-if="product.addons && product.addons.length > 0"
                  class="text-[11px] font-medium text-ink-muted"
                >
                  + {{ product.addons.length }} adicional(is)
                </span>
              </div>
            </div>

            <!-- Right Square Rounded Thumbnail + Floating Add Button -->
            <div class="relative h-28 w-28 shrink-0 sm:h-32 sm:w-32">
              <div class="h-full w-full overflow-hidden rounded-2xl border border-line bg-surface-muted flex items-center justify-center">
                <img
                  v-if="product.imageUrl"
                  :src="product.imageUrl"
                  :alt="product.name"
                  class="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
                />
                <UtensilsCrossed v-else class="h-8 w-8 text-ink-muted/60" />
              </div>

              <button
                v-if="!auth.isDriverOnly"
                type="button"
                :aria-label="`Adicionar ${product.name}`"
                class="absolute -bottom-2 -right-2 flex h-9 w-9 items-center justify-center rounded-xl bg-brand-600 text-white shadow-md transition hover:scale-105 hover:bg-brand-700"
                @click.stop="openDishModal(product)"
              >
                <Plus class="h-5 w-5" />
              </button>
            </div>
          </div>
        </section>

        <AppEmptyState
          v-else
          :icon="UtensilsCrossed"
          title="Nenhuma marmita encontrada"
          :description="
            isPlanMode
              ? 'Este restaurante não possui marmitas elegíveis ao plano com este filtro.'
              : 'Tente alterar a categoria selecionada ou limpar a busca.'
          "
        >
          <template #action>
            <AppButton
              variant="outline"
              size="sm"
              @click="selectedCategory = ''; searchDish = ''; if (isPlanMode) togglePlanMode(false)"
            >
              Ver todo o cardápio
            </AppButton>
          </template>
        </AppEmptyState>

        <!-- Restaurant Individual Subscription Plans -->
        <section
          v-if="!auth.isDriverOnly && restaurantPlans.length > 0"
          id="restaurante-planos"
          class="space-y-4 border-t border-line pt-6 scroll-mt-20"
        >
          <div class="flex flex-col justify-between gap-2 sm:flex-row sm:items-end">
            <div>
              <AppBadge variant="fresh" size="xs" dot class="mb-1.5">
                Planos exclusivos desta loja
              </AppBadge>
              <h2 class="font-display text-lg font-extrabold text-ink sm:text-xl">
                Planos de assinatura de {{ restaurant.name }}
              </h2>
              <p class="text-xs text-ink-muted">
                Assine para resgatar marmitas diretamente no cardápio deste restaurante com valor base R$ 0,00.
              </p>
            </div>
            <RouterLink
              v-if="auth.isAuthenticated"
              to="/conta?tab=assinaturas"
              class="text-xs font-bold text-fresh-700 hover:underline dark:text-fresh-400"
            >
              Gerenciar assinaturas →
            </RouterLink>
          </div>

          <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            <div
              v-for="plan in restaurantPlans"
              :key="plan.id"
              class="flex flex-col justify-between rounded-2xl border p-5 transition"
              :class="
                activeRestaurantSubscription?.planId === plan.id
                  ? 'border-fresh-500 bg-fresh-50/40 ring-1 ring-fresh-500/30 dark:bg-fresh-950/20'
                  : 'border-line bg-surface shadow-card'
              "
            >
              <div>
                <div class="flex items-center justify-between gap-2">
                  <AppBadge variant="fresh" size="xs">{{ plan.mealsPerWeek }} marmitas / semana</AppBadge>
                  <AppBadge v-if="activeRestaurantSubscription?.planId === plan.id" variant="brand" size="xs">
                    Seu plano ativo
                  </AppBadge>
                </div>

                <h3 class="mt-3 font-display text-base font-extrabold text-ink">{{ plan.name }}</h3>
                <p class="mt-1 text-xs leading-relaxed text-ink-secondary">
                  {{ plan.description || 'Assinatura mensal válida no cardápio desta loja.' }}
                </p>

                <div class="mt-4 rounded-xl border border-line bg-surface-subtle p-3">
                  <div class="flex items-baseline gap-1">
                    <span class="font-display text-2xl font-extrabold text-ink tabular-nums">
                      {{ formatCurrency(plan.monthlyPrice) }}
                    </span>
                    <span class="text-xs text-ink-muted">/mês</span>
                  </div>
                  <p class="mt-0.5 text-[11px] font-bold text-fresh-700 dark:text-fresh-400 tabular-nums">
                    {{ formatCurrency(getPricePerMeal(plan)) }} por marmita ({{ getMealsPerMonth(plan) }} ref./mês)
                  </p>
                </div>

                <ul class="mt-3 space-y-1.5 text-[11px] text-ink-secondary">
                  <li v-if="plan.allowedDays" class="flex items-center gap-1.5">
                    <CalendarDays class="h-3.5 w-3.5 shrink-0 text-fresh-600" />
                    <span>{{ plan.allowedDays }}</span>
                  </li>
                  <li v-if="plan.deliveryWindow" class="flex items-center gap-1.5">
                    <Clock class="h-3.5 w-3.5 shrink-0 text-fresh-600" />
                    <span>Janela: {{ plan.deliveryWindow }}</span>
                  </li>
                  <li class="flex items-center gap-1.5">
                    <Check class="h-3.5 w-3.5 shrink-0 text-fresh-600" />
                    <span>Uso exclusivo em {{ restaurant.name }}</span>
                  </li>
                </ul>
              </div>

              <div class="mt-4 pt-2">
                <RouterLink
                  v-if="activeRestaurantSubscription"
                  to="/conta?tab=assinaturas"
                  class="flex w-full items-center justify-center rounded-xl border border-line-strong bg-surface-subtle px-4 py-2 text-xs font-bold text-ink transition hover:bg-surface-muted"
                >
                  {{
                    activeRestaurantSubscription.planId === plan.id
                      ? 'Gerenciar assinatura'
                      : 'Trocar para este plano'
                  }}
                </RouterLink>
                <AppButton
                  v-else
                  variant="fresh"
                  size="md"
                  block
                  :loading="subscribingPlanId === plan.id"
                  @click="handleSubscribe(plan)"
                >
                  Assinar plano
                </AppButton>
              </div>
            </div>
          </div>
        </section>

        <!-- Dish Customization Modal / Bottom Sheet -->
        <DishCustomizeModal
          :open="dishModalOpen"
          :product="selectedDish"
          :is-plan-mode="isPlanMode"
          :read-only="auth.isDriverOnly"
          @close="dishModalOpen = false"
          @confirm="handleDishConfirm"
        />

        <!-- Restaurant Switch Confirmation -->
        <AppConfirmDialog
          :open="Boolean(pendingCartAction)"
          title="Trocar de restaurante?"
          :message="`Sua sacola atual possui itens de '${cart.restaurantName}'. Para adicionar itens de '${restaurant.name}', a sacola anterior será substituída.`"
          confirm-label="Sim, trocar restaurante"
          variant="primary"
          @confirm="pendingCartAction && executeAddToCart(pendingCartAction)"
          @cancel="pendingCartAction = null"
        />

        <!-- Chef & Nutricionista IA Modal -->
        <AiMarmitaAssistantModal
          :is-open="isAiModalOpen"
          :restaurant-id="restaurant.id"
          :restaurant-name="restaurant.name"
          :available-products="products"
          @close="isAiModalOpen = false"
          @add-to-cart="handleAddFromAi"
        />
      </template>
    </div>
  </CustomerLayout>
</template>
