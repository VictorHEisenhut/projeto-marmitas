<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  ArrowLeft,
  Clock,
  Flame,
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
import LiveCartSidebar from '@/components/customer/LiveCartSidebar.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppSkeleton from '@/components/ui/AppSkeleton.vue'
import AppEmptyState from '@/components/ui/AppEmptyState.vue'
import AppConfirmDialog from '@/components/ui/AppConfirmDialog.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useCartStore, type SelectedAddon } from '@/stores/cart'
import { useToastStore } from '@/stores/toast'
import type { Category, Favorite, Product, Restaurant } from '@/types'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const cart = useCartStore()
const toast = useToastStore()

const restaurantId = computed(() => String(route.params.id || ''))
const restaurant = ref<Restaurant | null>(null)
const categories = ref<Category[]>([])
const products = ref<Product[]>([])
const favoriteEntry = ref<Favorite | null>(null)
const selectedCategory = ref('')
const searchDish = ref('')
const isPlanMode = ref(cart.isPlanRedemption)
const loading = ref(true)
const error = ref('')
const togglingFav = ref(false)
const isAiModalOpen = ref(false)

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
      const favs = (results[3].value as { data: Favorite[] }).data
      favoriteEntry.value = favs.find((f) => f.restaurantId === restaurantId.value) ?? null
    }
  } catch (err) {
    error.value = getErrorMessage(err, 'Não foi possível carregar o cardápio do restaurante.')
  } finally {
    loading.value = false
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

onMounted(() => {
  void loadMenu()
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

        <!-- 3-Column Wolt/DoorDash Layout on Desktop (xl+) -->
        <div
          class="grid gap-6 items-start"
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
