<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  ArrowLeft,
  CalendarDays,
  Check,
  Clock,
  Flame,
  Heart,
  MapPin,
  MessageSquare,
  Plus,
  Search,
  Send,
  Sparkles,
  Star,
  Store,
  Truck,
  UtensilsCrossed,
} from 'lucide-vue-next'
import CustomerLayout from '@/layouts/CustomerLayout.vue'
import DishCustomizeModal from '@/components/customer/DishCustomizeModal.vue'
import LiveCartSidebar from '@/components/customer/LiveCartSidebar.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppCard from '@/components/ui/AppCard.vue'
import AppSkeleton from '@/components/ui/AppSkeleton.vue'
import AppEmptyState from '@/components/ui/AppEmptyState.vue'
import AppConfirmDialog from '@/components/ui/AppConfirmDialog.vue'
import StarRating from '@/components/ui/StarRating.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useCartStore, type SelectedAddon } from '@/stores/cart'
import { useToastStore } from '@/stores/toast'
import type { Category, Favorite, Plan, Product, Restaurant, ReviewItem } from '@/types'

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
const restaurantReviews = ref<ReviewItem[]>([])
const favoriteEntry = ref<Favorite | null>(null)
const selectedCategory = ref('')
const searchDish = ref('')
const isPlanMode = ref(cart.isPlanRedemption)
const loading = ref(true)
const error = ref('')
const togglingFav = ref(false)
const subscribingPlanId = ref<string | null>(null)

const newReviewRating = ref(5)
const newReviewComment = ref('')
const submittingReview = ref(false)

const selectedDish = ref<Product | null>(null)
const dishModalOpen = ref(false)

const pendingCartAction = ref<{
  product: Product
  quantity: number
  selectedAddons: SelectedAddon[]
  wantsCutlery: boolean
} | null>(null)

const activeProducts = computed(() => products.value.filter((p) => p.isActive))

const activeCategories = computed(() => {
  const counts: Record<string, number> = {}
  for (const p of activeProducts.value) {
    if (isPlanMode.value && p.availableInPlans === false) continue
    counts[p.categoryId] = (counts[p.categoryId] || 0) + 1
  }
  return categories.value
    .filter((c) => (counts[c.id] || 0) > 0)
    .map((c) => ({ ...c, count: counts[c.id] || 0 }))
})

const filteredProducts = computed(() => {
  let list = [...activeProducts.value]
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

const highlightProducts = computed(() => {
  if (selectedCategory.value || searchDish.value.trim()) return []
  return filteredProducts.value.slice(0, 4)
})

const restaurantAvgRating = computed<number | null>(() => {
  if (restaurantReviews.value.length === 0) return null
  const sum = restaurantReviews.value.reduce((acc, r) => acc + r.rating, 0)
  return sum / restaurantReviews.value.length
})

const ratingDistribution = computed(() => {
  const total = restaurantReviews.value.length
  return [5, 4, 3, 2, 1].map((stars) => {
    const count = restaurantReviews.value.filter((r) => Math.round(r.rating) === stars).length
    const percentage = total > 0 ? Math.round((count / total) * 100) : 0
    return { stars, count, percentage }
  })
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

function scrollToSection(sectionId: string) {
  document.getElementById(sectionId)?.scrollIntoView({ behavior: 'smooth' })
}

function hasRequiredCustomization(product: Product): boolean {
  return Boolean((product.addons && product.addons.length > 0) || product.allowsCutlery)
}

function togglePlanMode(enabled: boolean) {
  isPlanMode.value = enabled
  cart.setPlanMode(enabled)
}

function openDishModal(product: Product) {
  selectedDish.value = product
  dishModalOpen.value = true
}

function handleQuickOrModalAdd(product: Product) {
  if (auth.isDriverOnly || hasRequiredCustomization(product)) {
    openDishModal(product)
    return
  }
  handleDishConfirm({
    product,
    quantity: 1,
    selectedAddons: [],
    wantsCutlery: false,
  })
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
      : `"${payload.product.name}" adicionada à sacola.`,
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

async function loadMenu() {
  loading.value = true
  error.value = ''

  try {
    const requests: Promise<unknown>[] = [
      api.get<Restaurant>(`/restaurantes/${restaurantId.value}`),
      api.get<Product[]>(`/restaurantes/${restaurantId.value}/cardapio`),
      api.get<Category[]>('/categorias'),
      api.get<Plan[]>(`/restaurantes/${restaurantId.value}/planos`),
      api.get<ReviewItem[]>(`/restaurantes/${restaurantId.value}/avaliacoes`),
    ]

    if (auth.isAuthenticated && !auth.isDriverOnly) {
      requests.push(api.get<Favorite[]>('/usuarios/me/favoritos'))
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
    } else {
      restaurantPlans.value = []
    }
    if (results[4]?.status === 'fulfilled') {
      restaurantReviews.value = (results[4].value as { data: ReviewItem[] }).data
    } else {
      restaurantReviews.value = []
    }
    if (results[5]?.status === 'fulfilled') {
      const favs = (results[5].value as { data: Favorite[] }).data
      favoriteEntry.value = favs.find((f) => f.restaurantId === restaurantId.value) ?? null
    }
  } catch (err) {
    error.value = getErrorMessage(err, 'Não foi possível carregar o cardápio do restaurante.')
  } finally {
    loading.value = false
  }
}

async function handleSubscribePlan(plan: Plan) {
  if (!auth.isAuthenticated) {
    await router.push({ path: '/login', query: { redirect: route.fullPath } })
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

async function handleSubmitRestaurantReview() {
  if (!restaurant.value) return
  if (!auth.isAuthenticated) {
    await router.push({ path: '/login', query: { redirect: route.fullPath } })
    return
  }
  if (!newReviewComment.value.trim()) {
    toast.warning('Escreva seu comentário sobre a experiência no restaurante.')
    return
  }

  submittingReview.value = true
  try {
    const { data } = await api.post<ReviewItem>('/avaliacoes/clientes', {
      restaurantId: restaurant.value.id,
      rating: newReviewRating.value,
      comment: newReviewComment.value.trim(),
    })
    restaurantReviews.value.unshift(data)
    newReviewComment.value = ''
    newReviewRating.value = 5
    toast.success('Avaliação publicada com sucesso!')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível enviar a avaliação.'))
  } finally {
    submittingReview.value = false
  }
}

async function scrollToHashTarget() {
  if (route.hash === '#planos' || route.hash === '#avaliacoes' || route.hash === '#cardapio') {
    await nextTick()
    scrollToSection(route.hash.slice(1))
  }
}

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

watch(restaurantId, async () => {
  await loadMenu()
  await scrollToHashTarget()
})

watch(
  () => route.hash,
  () => {
    void scrollToHashTarget()
  },
)

onMounted(async () => {
  await loadMenu()
  await scrollToHashTarget()
})
</script>

<template>
  <CustomerLayout hide-desktop-cart-trigger>
    <div class="mx-auto max-w-7xl px-4 py-5 sm:px-6 space-y-6">
      <div>
        <RouterLink
          to="/"
          class="inline-flex items-center gap-1.5 text-xs font-bold text-ink-secondary transition hover:text-brand-600"
        >
          <ArrowLeft class="h-4 w-4" />
          Voltar para restaurantes
        </RouterLink>
      </div>

      <div v-if="loading" class="space-y-6">
        <AppSkeleton variant="card" height="220px" />
        <div class="grid gap-4 md:grid-cols-2">
          <AppSkeleton v-for="n in 4" :key="n" variant="card" height="132px" />
        </div>
      </div>

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
        <!-- Storefront Hero -->
        <section class="overflow-hidden rounded-3xl border border-line bg-surface shadow-card">
          <div class="relative h-44 sm:h-52 w-full bg-gradient-to-br from-brand-700 via-brand-600 to-amber-500">
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
                  <button
                    v-if="restaurantAvgRating !== null"
                    type="button"
                    class="inline-flex items-center gap-1 rounded-full bg-white/20 px-2.5 py-0.5 text-[11px] font-bold backdrop-blur-md transition hover:bg-white/30"
                    @click="scrollToSection('avaliacoes')"
                  >
                    <Star class="h-3 w-3 fill-amber-300 text-amber-300" />
                    <span class="tabular-nums">{{ restaurantAvgRating.toFixed(1) }}</span>
                    <span class="opacity-85">({{ restaurantReviews.length }})</span>
                  </button>
                  <button
                    v-if="restaurantPlans.length > 0"
                    type="button"
                    class="inline-flex items-center gap-1 rounded-full bg-fresh-600/90 px-2.5 py-0.5 text-[11px] font-bold text-white backdrop-blur-md transition hover:bg-fresh-600"
                    @click="scrollToSection('planos')"
                  >
                    <Sparkles class="h-3 w-3" />
                    <span>{{ restaurantPlans.length }} {{ restaurantPlans.length === 1 ? 'plano' : 'planos' }}</span>
                  </button>
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

        <!-- Section Navigation Bar -->
        <div class="flex flex-wrap items-center gap-2">
          <button
            type="button"
            class="inline-flex items-center gap-1.5 rounded-full border border-line bg-surface px-4 py-2 text-xs font-bold text-ink transition hover:border-brand-400 hover:text-brand-600"
            @click="scrollToSection('cardapio')"
          >
            <UtensilsCrossed class="h-3.5 w-3.5 text-brand-600" />
            <span>Cardápio ({{ activeProducts.length }})</span>
          </button>
          <button
            type="button"
            class="inline-flex items-center gap-1.5 rounded-full border border-line bg-surface px-4 py-2 text-xs font-bold text-ink transition hover:border-fresh-500 hover:text-fresh-700 dark:hover:text-fresh-400"
            @click="scrollToSection('planos')"
          >
            <Sparkles class="h-3.5 w-3.5 text-fresh-600" />
            <span>Planos do Restaurante ({{ restaurantPlans.length }})</span>
          </button>
          <button
            type="button"
            class="inline-flex items-center gap-1.5 rounded-full border border-line bg-surface px-4 py-2 text-xs font-bold text-ink transition hover:border-amber-400 hover:text-amber-600 dark:hover:text-amber-400"
            @click="scrollToSection('avaliacoes')"
          >
            <Star class="h-3.5 w-3.5 fill-amber-400 text-amber-400" />
            <span>Avaliações ({{ restaurantReviews.length }})</span>
          </button>
        </div>

        <!-- 3-Column Wolt/DoorDash Layout on Desktop (xl+) -->
        <div
          id="cardapio"
          class="grid gap-6 items-start scroll-mt-20"
          :class="!auth.isDriverOnly ? 'xl:grid-cols-[200px_1fr_340px]' : 'xl:grid-cols-[200px_1fr]'"
        >
          <!-- Column 1 (Left Sticky): Vertical Category Index -->
          <aside class="hidden xl:block sticky top-20 space-y-2">
            <p class="px-3 text-[11px] font-bold uppercase tracking-wider text-ink-muted">
              Categorias
            </p>
            <nav class="space-y-1">
              <button
                type="button"
                class="flex w-full items-center justify-between rounded-xl px-3 py-2 text-xs font-bold transition"
                :class="
                  selectedCategory === ''
                    ? 'bg-brand-600 text-white shadow-xs'
                    : 'text-ink-secondary hover:bg-surface-muted hover:text-ink'
                "
                @click="selectedCategory = ''"
              >
                <span>Todas</span>
                <span class="tabular-nums opacity-80">{{ activeProducts.length }}</span>
              </button>
              <button
                v-for="cat in activeCategories"
                :key="cat.id"
                type="button"
                class="flex w-full items-center justify-between rounded-xl px-3 py-2 text-xs font-bold transition"
                :class="
                  selectedCategory === cat.id
                    ? 'bg-brand-600 text-white shadow-xs'
                    : 'text-ink-secondary hover:bg-surface-muted hover:text-ink'
                "
                @click="selectedCategory = cat.id"
              >
                <span class="truncate">{{ cat.name }}</span>
                <span class="tabular-nums opacity-80">{{ cat.count }}</span>
              </button>
            </nav>
          </aside>

          <!-- Column 2 (Center): Mode Banner, Search, Highlights & Dish Grid -->
          <div class="min-w-0 space-y-6">
            <!-- Contextual Mode Switch + Search -->
            <div class="flex flex-col gap-3 rounded-2xl border border-line bg-surface p-4 shadow-card sm:flex-row sm:items-center sm:justify-between">
              <div v-if="!auth.isDriverOnly" class="inline-flex rounded-xl bg-surface-muted p-1 self-start">
                <button
                  type="button"
                  class="rounded-lg px-3.5 py-1.5 text-xs font-bold transition"
                  :class="!isPlanMode ? 'bg-surface text-ink shadow-xs' : 'text-ink-muted hover:text-ink'"
                  @click="togglePlanMode(false)"
                >
                  Compra Avulsa
                </button>
                <button
                  type="button"
                  class="inline-flex items-center gap-1.5 rounded-lg px-3.5 py-1.5 text-xs font-bold transition"
                  :class="isPlanMode ? 'bg-fresh-600 text-white shadow-xs' : 'text-ink-muted hover:text-ink'"
                  @click="togglePlanMode(true)"
                >
                  <Sparkles class="h-3.5 w-3.5" />
                  Resgate do Plano
                </button>
              </div>

              <div class="relative flex-1 sm:max-w-xs">
                <Search class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-muted" />
                <input
                  v-model="searchDish"
                  type="search"
                  placeholder="Buscar prato no cardápio..."
                  class="w-full rounded-xl border border-line bg-surface-subtle py-2 pl-9 pr-3 text-xs text-ink placeholder:text-ink-muted focus:border-brand-600 focus:bg-surface focus:outline-none"
                />
              </div>
            </div>

            <!-- Mobile / Tablet Horizontal Category Chips -->
            <div class="flex xl:hidden items-center gap-2 overflow-x-auto no-scrollbar pb-1">
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
                {{ cat.name }} ({{ cat.count }})
              </button>
            </div>

            <!-- Destaques Carousel (4:3 photos) -->
            <section v-if="highlightProducts.length > 0" class="space-y-3">
              <div class="flex items-center gap-2">
                <Flame class="h-4 w-4 text-brand-600" />
                <h2 class="font-display text-base font-extrabold text-ink">Destaques do dia</h2>
              </div>

              <div class="flex gap-3.5 overflow-x-auto no-scrollbar pb-1 snap-x">
                <div
                  v-for="dish in highlightProducts"
                  :key="`hl-${dish.id}`"
                  role="button"
                  tabindex="0"
                  class="group w-56 shrink-0 snap-start overflow-hidden rounded-2xl border border-line bg-surface shadow-card transition hover:border-brand-300 cursor-pointer"
                  @click="openDishModal(dish)"
                  @keyup.enter="openDishModal(dish)"
                >
                  <div class="relative aspect-[4/3] w-full overflow-hidden bg-surface-muted flex items-center justify-center">
                    <img
                      v-if="dish.imageUrl"
                      :src="dish.imageUrl"
                      :alt="dish.name"
                      class="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
                    />
                    <UtensilsCrossed v-else class="h-8 w-8 text-ink-muted/50" />
                    <button
                      v-if="!auth.isDriverOnly"
                      type="button"
                      :aria-label="`Adicionar ${dish.name}`"
                      class="absolute bottom-2 right-2 flex h-8 w-8 items-center justify-center rounded-xl bg-brand-600 text-white shadow-md transition hover:scale-105"
                      @click.stop="handleQuickOrModalAdd(dish)"
                    >
                      <Plus class="h-4 w-4" />
                    </button>
                  </div>
                  <div class="p-3 space-y-1">
                    <p class="truncate font-display text-xs font-extrabold text-ink">{{ dish.name }}</p>
                    <p class="font-display text-sm font-extrabold tabular-nums" :class="isPlanMode ? 'text-fresh-700 dark:text-fresh-400' : 'text-brand-600 dark:text-brand-400'">
                      {{ isPlanMode ? 'R$ 0,00 (Plano)' : formatCurrency(dish.promotionalPrice || dish.price) }}
                    </p>
                  </div>
                </div>
              </div>
            </section>

            <!-- Full Menu Grid -->
            <section class="space-y-3">
              <h2 class="font-display text-base font-extrabold text-ink">
                Cardápio completo ({{ filteredProducts.length }})
              </h2>

              <div v-if="filteredProducts.length > 0" class="grid gap-3.5 md:grid-cols-2">
                <div
                  v-for="product in filteredProducts"
                  :key="product.id"
                  role="button"
                  tabindex="0"
                  class="group flex justify-between gap-3.5 rounded-2xl border border-line bg-surface p-4 shadow-card transition-all duration-150 hover:border-brand-300 hover:shadow-card-hover dark:hover:border-brand-700/70 cursor-pointer"
                  @click="openDishModal(product)"
                  @keyup.enter="openDishModal(product)"
                >
                  <div class="flex min-w-0 flex-1 flex-col justify-between">
                    <div>
                      <div class="flex flex-wrap items-center gap-1.5">
                        <span
                          v-if="product.categoryName"
                          class="text-[10px] font-bold uppercase tracking-wide text-ink-muted"
                        >
                          {{ product.categoryName }}
                        </span>
                        <AppBadge v-if="isPlanMode" variant="fresh" size="xs">
                          No Plano
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

                      <h3 class="mt-1 font-display text-sm font-extrabold text-ink group-hover:text-brand-600 transition-colors">
                        {{ product.name }}
                      </h3>
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
                        v-if="hasRequiredCustomization(product)"
                        class="text-[11px] font-medium text-ink-muted"
                      >
                        Personalizável
                      </span>
                    </div>
                  </div>

                  <div class="relative h-24 w-24 shrink-0 sm:h-28 sm:w-28">
                    <div class="h-full w-full overflow-hidden rounded-2xl border border-line bg-surface-muted flex items-center justify-center">
                      <img
                        v-if="product.imageUrl"
                        :src="product.imageUrl"
                        :alt="product.name"
                        class="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
                      />
                      <UtensilsCrossed v-else class="h-7 w-7 text-ink-muted/60" />
                    </div>

                    <button
                      v-if="!auth.isDriverOnly"
                      type="button"
                      :aria-label="`Adicionar ${product.name}`"
                      class="absolute -bottom-2 -right-2 flex h-9 w-9 items-center justify-center rounded-xl bg-brand-600 text-white shadow-md transition hover:scale-105 hover:bg-brand-700"
                      @click.stop="handleQuickOrModalAdd(product)"
                    >
                      <Plus class="h-5 w-5" />
                    </button>
                  </div>
                </div>
              </div>

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
            </section>
          </div>

          <!-- Column 3 (Right Sticky): Live Cart Sidebar (Desktop xl+) -->
          <div v-if="!auth.isDriverOnly" class="hidden xl:block">
            <LiveCartSidebar
              :restaurant-id="restaurant.id"
              :restaurant-name="restaurant.name"
              :delivery-fee="restaurant.deliveryFee"
              :is-plan-mode="isPlanMode"
            />
          </div>
        </div>

        <!-- Restaurant Plans Section (All Plans of This Restaurant) -->
        <section id="planos" class="space-y-5 border-t border-line pt-8 scroll-mt-20">
          <div class="flex flex-col justify-between gap-2 sm:flex-row sm:items-end">
            <div>
              <AppBadge variant="fresh" size="sm" dot class="mb-2">
                Assinaturas da Loja
              </AppBadge>
              <h2 class="font-display text-xl font-extrabold text-ink sm:text-2xl">
                Planos de {{ restaurant.name }}
              </h2>
              <p class="mt-1 text-xs text-ink-muted sm:text-sm">
                Todos os planos ativos cadastrados por este restaurante para resgate direto no cardápio.
              </p>
            </div>
            <RouterLink
              v-if="auth.isAuthenticated && !auth.isDriverOnly"
              to="/conta?tab=assinaturas"
              class="text-xs font-bold text-fresh-700 hover:underline dark:text-fresh-400"
            >
              Gerenciar minhas assinaturas →
            </RouterLink>
          </div>

          <div
            v-if="restaurantPlans.length > 0"
            class="grid gap-5 sm:grid-cols-2 lg:grid-cols-3"
          >
            <AppCard
              v-for="plan in restaurantPlans"
              :key="plan.id"
              padding="lg"
              class="flex flex-col justify-between"
            >
              <div>
                <div class="flex flex-wrap items-center justify-between gap-2">
                  <AppBadge variant="fresh" size="sm">
                    {{ plan.mealsPerWeek }} marmitas / semana
                  </AppBadge>
                  <AppBadge variant="neutral" size="xs">
                    {{ getMealsPerMonth(plan) }}/mês
                  </AppBadge>
                </div>

                <h3 class="mt-4 font-display text-lg font-extrabold text-ink">
                  {{ plan.name }}
                </h3>
                <p class="mt-1.5 text-xs leading-relaxed text-ink-secondary">
                  {{ plan.description || 'Refeições balanceadas entregues na janela ideal para sua rotina.' }}
                </p>

                <div class="mt-4 rounded-2xl border border-line bg-surface-subtle p-4">
                  <div class="flex items-baseline gap-1">
                    <span class="font-display text-2xl font-extrabold text-ink tabular-nums">
                      {{ formatCurrency(plan.monthlyPrice) }}
                    </span>
                    <span class="text-xs font-medium text-ink-muted">/mês</span>
                  </div>
                  <p class="mt-1 text-xs font-bold text-fresh-700 dark:text-fresh-400 tabular-nums">
                    Apenas {{ formatCurrency(getPricePerMeal(plan)) }} por marmita
                  </p>
                </div>

                <ul class="mt-4 space-y-2 text-xs text-ink-secondary">
                  <li class="flex items-center gap-2">
                    <Check class="h-4 w-4 shrink-0 text-fresh-600" />
                    <span>Resgate direto no cardápio desta loja</span>
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
                    <Truck class="h-4 w-4 shrink-0 text-fresh-600" />
                    <span>
                      {{ plan.deliveryFee && plan.deliveryFee > 0 ? `Taxa de entrega: ${formatCurrency(plan.deliveryFee)}` : 'Entrega inclusa no plano' }}
                    </span>
                  </li>
                </ul>
                <p v-if="plan.cancellationPolicy" class="mt-3 text-[11px] text-ink-muted">
                  {{ plan.cancellationPolicy }}
                </p>
              </div>

              <div v-if="!auth.isDriverOnly" class="mt-5 pt-2">
                <AppButton
                  variant="fresh"
                  size="md"
                  block
                  :loading="subscribingPlanId === plan.id"
                  @click="handleSubscribePlan(plan)"
                >
                  Assinar plano
                </AppButton>
              </div>
            </AppCard>
          </div>

          <AppEmptyState
            v-else
            :icon="Sparkles"
            title="Nenhum plano cadastrado neste restaurante"
            description="Este restaurante ainda não publicou pacotes de assinatura recorrente."
          />
        </section>

        <!-- Restaurant Customer Reviews Section (All Reviews of This Restaurant) -->
        <section id="avaliacoes" class="space-y-6 border-t border-line pt-8 pb-4 scroll-mt-20">
          <div class="flex flex-wrap items-center justify-between gap-3">
            <div>
              <h2 class="font-display text-xl font-extrabold text-ink sm:text-2xl">
                Avaliações de {{ restaurant.name }}
              </h2>
              <p class="text-xs text-ink-muted sm:text-sm">
                Todas as avaliações publicadas por clientes deste restaurante.
              </p>
            </div>
            <AppBadge variant="brand" size="sm">
              {{ restaurantReviews.length }} {{ restaurantReviews.length === 1 ? 'avaliação' : 'avaliações' }}
            </AppBadge>
          </div>

          <div class="grid gap-5 lg:grid-cols-12 items-start">
            <!-- Rating Summary + New Review Form -->
            <div class="space-y-4 lg:col-span-5">
              <AppCard padding="lg" class="space-y-4">
                <div class="flex items-center gap-4">
                  <div class="text-center">
                    <p class="font-display text-4xl font-extrabold text-ink tabular-nums">
                      {{ restaurantAvgRating !== null ? restaurantAvgRating.toFixed(1) : '—' }}
                    </p>
                    <p class="text-[11px] font-semibold text-ink-muted">de 5,0</p>
                  </div>
                  <div class="space-y-1">
                    <StarRating
                      :model-value="Math.round(restaurantAvgRating ?? 0)"
                      readonly
                      size="md"
                    />
                    <p class="text-xs text-ink-secondary">
                      Baseado em {{ restaurantReviews.length }} {{ restaurantReviews.length === 1 ? 'avaliação' : 'avaliações' }}
                    </p>
                  </div>
                </div>

                <div class="space-y-1.5 border-t border-line pt-3">
                  <div
                    v-for="row in ratingDistribution"
                    :key="row.stars"
                    class="flex items-center gap-2 text-xs"
                  >
                    <span class="w-8 font-bold text-ink-secondary tabular-nums">{{ row.stars }} ★</span>
                    <div class="h-2 flex-1 overflow-hidden rounded-full bg-surface-muted">
                      <div
                        class="h-full rounded-full bg-amber-400 transition-all duration-300"
                        :style="{ width: `${row.percentage}%` }"
                      />
                    </div>
                    <span class="w-8 text-right text-[11px] text-ink-muted tabular-nums">{{ row.count }}</span>
                  </div>
                </div>
              </AppCard>

              <AppCard v-if="!auth.isDriverOnly" padding="lg" class="space-y-3">
                <div class="flex items-center gap-2">
                  <MessageSquare class="h-4 w-4 text-brand-600" />
                  <h3 class="font-display text-sm font-extrabold text-ink">
                    Avaliar {{ restaurant.name }}
                  </h3>
                </div>

                <form v-if="auth.isAuthenticated" class="space-y-3" @submit.prevent="handleSubmitRestaurantReview">
                  <div class="flex items-center justify-between rounded-xl border border-line bg-surface-subtle px-3 py-2">
                    <span class="text-xs font-semibold text-ink-secondary">Sua nota</span>
                    <StarRating v-model="newReviewRating" size="md" show-value />
                  </div>
                  <textarea
                    v-model="newReviewComment"
                    rows="3"
                    required
                    placeholder="Conte como foi o sabor, tempero, embalagem e entrega..."
                    class="w-full rounded-xl border border-line-strong bg-surface px-3.5 py-2.5 text-sm text-ink placeholder:text-ink-muted focus:border-brand-600 focus:outline-none"
                  />
                  <AppButton type="submit" variant="primary" size="sm" block :loading="submittingReview">
                    <Send class="h-3.5 w-3.5" />
                    Publicar avaliação
                  </AppButton>
                </form>

                <div v-else class="space-y-2 text-xs text-ink-secondary">
                  <p>Entre na sua conta para avaliar sua experiência com este restaurante.</p>
                  <RouterLink
                    :to="{ path: '/login', query: { redirect: `${route.path}#avaliacoes` } }"
                    class="inline-flex items-center gap-1 font-bold text-brand-600 hover:underline dark:text-brand-400"
                  >
                    Entrar para avaliar →
                  </RouterLink>
                </div>
              </AppCard>
            </div>

            <!-- All Restaurant Reviews List -->
            <div class="lg:col-span-7">
              <div v-if="restaurantReviews.length > 0" class="grid gap-4 sm:grid-cols-2">
                <AppCard
                  v-for="rev in restaurantReviews"
                  :key="rev.id"
                  padding="md"
                  class="space-y-3"
                >
                  <div class="flex items-start justify-between gap-2">
                    <div class="flex items-center gap-2.5 min-w-0">
                      <span class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-brand-100 font-display text-xs font-extrabold text-brand-800 dark:bg-brand-900/60 dark:text-brand-200">
                        {{ getInitials(rev.authorName) }}
                      </span>
                      <div class="min-w-0">
                        <p class="truncate text-sm font-bold text-ink">
                          {{ rev.authorName || 'Cliente Verificado' }}
                        </p>
                        <p v-if="rev.createdAt" class="text-[11px] text-ink-muted tabular-nums">
                          {{ new Date(rev.createdAt).toLocaleDateString('pt-BR') }}
                        </p>
                      </div>
                    </div>
                    <StarRating :model-value="rev.rating" readonly size="sm" />
                  </div>
                  <p class="text-sm leading-relaxed text-ink-secondary">
                    “{{ rev.comment }}”
                  </p>
                </AppCard>
              </div>

              <AppEmptyState
                v-else
                :icon="Star"
                title="Ainda sem avaliações para este restaurante"
                description="Seja o primeiro a avaliar o cardápio e a entrega desta loja."
              />
            </div>
          </div>
        </section>

        <DishCustomizeModal
          :open="dishModalOpen"
          :product="selectedDish"
          :is-plan-mode="isPlanMode"
          :read-only="auth.isDriverOnly"
          @close="dishModalOpen = false"
          @confirm="handleDishConfirm"
        />

        <AppConfirmDialog
          :open="Boolean(pendingCartAction)"
          title="Trocar de restaurante?"
          :message="`Sua sacola atual possui itens de '${cart.restaurantName}'. Para adicionar itens de '${restaurant.name}', a sacola anterior será substituída.`"
          confirm-label="Sim, trocar restaurante"
          variant="primary"
          @confirm="pendingCartAction && executeAddToCart(pendingCartAction)"
          @cancel="pendingCartAction = null"
        />
      </template>
    </div>
  </CustomerLayout>
</template>
