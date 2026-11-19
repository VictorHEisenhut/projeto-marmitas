<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { ArrowLeft, Heart, Plus, Search, ShoppingCart, Sparkles } from 'lucide-vue-next'
import Navbar from '@/components/Navbar.vue'
import AiMarmitaAssistantModal from '@/components/AiMarmitaAssistantModal.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import type { Category, Product, Restaurant } from '@/types'

const route = useRoute()
const auth = useAuthStore()
const cart = useCartStore()

const isDriverOnly = computed(
  () =>
    auth.hasRole(['driver']) &&
    !auth.hasRole(['customer', 'restaurant_owner', 'admin', 'super_admin']),
)

const restaurantId = computed(() => String(route.params.id || ''))
const restaurant = ref<Restaurant | null>(null)
const categories = ref<Category[]>([])
const products = ref<Product[]>([])
const selectedCategory = ref('')
const searchDish = ref('')
const isPlanMode = ref(cart.isPlanRedemption)
const addonSelections = ref<Record<string, Record<string, number>>>({})
const cutlerySelections = ref<Record<string, boolean>>({})
const loading = ref(true)
const error = ref('')
const message = ref('')
const isAiModalOpen = ref(false)

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
        (p.description && p.description.toLowerCase().includes(q)),
    )
  }
  return list
})

function togglePlanMode(enabled: boolean) {
  isPlanMode.value = enabled
  cart.setPlanMode(enabled)
}

function getAddonQty(productId: string, addonName: string): number {
  return addonSelections.value[productId]?.[addonName] || 0
}

function setAddonQty(productId: string, addonName: string, qty: number, maxQty: number) {
  const clamped = Math.max(0, Math.min(maxQty, qty))
  if (!addonSelections.value[productId]) {
    addonSelections.value[productId] = {}
  }
  addonSelections.value[productId][addonName] = clamped
}

async function loadMenu() {
  loading.value = true
  error.value = ''

  try {
    const [restRes, prodRes, catRes] = await Promise.all([
      api.get<Restaurant>(`/restaurantes/${restaurantId.value}`),
      api.get<Product[]>(`/restaurantes/${restaurantId.value}/cardapio`),
      api.get<Category[]>('/categorias'),
    ])
    restaurant.value = restRes.data
    products.value = prodRes.data
    categories.value = catRes.data
  } catch (err) {
    error.value = getErrorMessage(err, 'Não foi possível carregar o cardápio do restaurante.')
  } finally {
    loading.value = false
  }
}

function handleAddToCart(product: Product) {
  if (!restaurant.value) return

  const prodAddons = product.addons || []
  const selectedAddons = prodAddons
    .map((a) => ({
      name: a.name,
      extraPrice: a.extraPrice,
      quantity: getAddonQty(product.id, a.name),
    }))
    .filter((a) => a.quantity > 0)

  const wantsCutlery = Boolean(cutlerySelections.value[product.id] && product.allowsCutlery)

  cart.addItem(restaurant.value, product, {
    selectedAddons,
    wantsCutlery,
    isPlanRedemption: isPlanMode.value,
  })

  if (addonSelections.value[product.id]) {
    addonSelections.value[product.id] = {}
  }
  cutlerySelections.value[product.id] = false

  message.value = isPlanMode.value
    ? `"${product.name}" adicionada para resgate no Plano Mensal.`
    : `"${product.name}" adicionada ao carrinho.`
}

async function handleFavoriteRestaurant() {
  if (!restaurant.value || !auth.isAuthenticated) return
  error.value = ''
  message.value = ''
  try {
    await api.post('/usuarios/me/favoritos', { restaurantId: restaurant.value.id })
    message.value = 'Restaurante salvo nos seus favoritos.'
  } catch (err) {
    error.value = getErrorMessage(err, 'Não foi possível favoritar o restaurante.')
  }
}

onMounted(() => {
  void loadMenu()
})
</script>

<template>
  <div class="min-h-screen bg-stone-50">
    <Navbar />

    <main class="mx-auto max-w-6xl space-y-6 px-4 py-8 sm:px-6">
      <div>
        <RouterLink
          to="/"
          class="inline-flex items-center gap-1.5 text-sm font-medium text-stone-600 hover:text-brand-600"
        >
          <ArrowLeft class="h-4 w-4" />
          Voltar para restaurantes
        </RouterLink>
      </div>

      <div
        v-if="error"
        class="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700"
      >
        {{ error }}
      </div>

      <div
        v-if="message"
        class="rounded-lg border border-green-200 bg-green-50 p-4 text-sm text-green-800"
      >
        {{ message }}
      </div>

      <p v-if="loading" class="text-sm text-stone-500">Carregando cardápio...</p>

      <template v-else-if="restaurant">
        <div class="flex flex-col justify-between gap-4 rounded-xl border border-stone-200 bg-white p-6 shadow-sm sm:flex-row sm:items-center">
          <div>
            <h1 class="text-2xl font-bold text-stone-900">{{ restaurant.name }}</h1>
            <p class="mt-1 text-sm text-stone-600">
              {{ restaurant.description || 'Refeições frescas preparadas diariamente.' }}
            </p>
          </div>

          <div class="flex items-center gap-2 self-start">
            <button
              type="button"
              class="inline-flex items-center gap-2 rounded-lg bg-gradient-to-r from-brand-600 to-amber-600 px-3.5 py-2 text-sm font-semibold text-white shadow-sm transition hover:from-brand-700 hover:to-amber-700"
              @click="isAiModalOpen = true"
            >
              <Sparkles class="h-4 w-4" />
              <span>Chef & Nutricionista IA</span>
            </button>

            <button
              v-if="auth.isAuthenticated"
              type="button"
              class="inline-flex items-center gap-1.5 rounded-lg border border-stone-200 px-3.5 py-2 text-sm font-medium text-stone-700 hover:bg-stone-50"
              @click="handleFavoriteRestaurant"
            >
              <Heart class="h-4 w-4 text-red-500" />
              Favoritar
            </button>
          </div>
        </div>

        <div
          v-if="cart.totalItems > 0 && !isDriverOnly"
          class="flex flex-col items-start justify-between gap-3 rounded-xl border border-orange-200 bg-orange-50 p-4 sm:flex-row sm:items-center"
        >
          <div class="flex items-center gap-2 text-sm font-medium text-stone-800">
            <ShoppingCart class="h-4 w-4 text-brand-600" />
            <span>
              Carrinho ({{ cart.restaurantName }}):
              <strong>{{ cart.totalItems }} item(ns)</strong> —
              <strong>{{ formatCurrency(cart.totalAmount) }}</strong>
            </span>
          </div>

          <RouterLink
            to="/pedidos"
            class="rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-700"
          >
            Finalizar pedido
          </RouterLink>
        </div>

        <div
          v-if="!isDriverOnly"
          class="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-stone-200 bg-white p-4 shadow-sm"
        >
          <div class="text-sm font-semibold text-stone-800">
            Modalidade de compra:
          </div>
          <div class="flex items-center gap-2">
            <button
              type="button"
              class="rounded-lg px-3.5 py-1.5 text-xs font-semibold transition"
              :class="
                !isPlanMode
                  ? 'bg-brand-600 text-white'
                  : 'border border-stone-200 bg-stone-50 text-stone-700 hover:bg-stone-100'
              "
              @click="togglePlanMode(false)"
            >
              Compra Avulsa (Individual)
            </button>
            <button
              type="button"
              class="rounded-lg px-3.5 py-1.5 text-xs font-semibold transition"
              :class="
                isPlanMode
                  ? 'bg-green-700 text-white'
                  : 'border border-stone-200 bg-stone-50 text-stone-700 hover:bg-stone-100'
              "
              @click="togglePlanMode(true)"
            >
              Plano Mensal (Resgate de Assinatura)
            </button>
          </div>
        </div>

        <div class="flex flex-col justify-between gap-3 sm:flex-row sm:items-center">
          <div v-if="categories.length > 0" class="flex flex-wrap gap-2">
            <button
              type="button"
              class="rounded-lg px-3.5 py-1.5 text-sm font-medium transition"
              :class="
                selectedCategory === ''
                  ? 'bg-stone-900 text-white'
                  : 'border border-stone-200 bg-white text-stone-700 hover:bg-stone-100'
              "
              @click="selectedCategory = ''"
            >
              Todas
            </button>
            <button
              v-for="cat in categories"
              :key="cat.id"
              type="button"
              class="rounded-lg px-3.5 py-1.5 text-sm font-medium transition"
              :class="
                selectedCategory === cat.id
                  ? 'bg-stone-900 text-white'
                  : 'border border-stone-200 bg-white text-stone-700 hover:bg-stone-100'
              "
              @click="selectedCategory = cat.id"
            >
              {{ cat.name }}
            </button>
          </div>

          <div class="relative w-full sm:w-64">
            <Search class="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-stone-400" />
            <input
              v-model="searchDish"
              type="search"
              placeholder="Buscar prato ou ingrediente..."
              class="w-full rounded-lg border border-stone-300 bg-white py-1.5 pl-9 pr-3 text-xs text-stone-900 focus:border-brand-600 focus:outline-none"
            />
          </div>
        </div>

        <div
          v-if="filteredProducts.length > 0"
          class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3"
        >
          <div
            v-for="product in filteredProducts"
            :key="product.id"
            class="flex flex-col justify-between rounded-xl border border-stone-200 bg-white p-5 shadow-sm"
          >
            <div>
              <div
                v-if="product.imageUrl"
                class="mb-3 h-36 w-full overflow-hidden rounded-lg bg-stone-100"
              >
                <img
                  :src="product.imageUrl"
                  :alt="product.name"
                  class="h-full w-full object-cover"
                />
              </div>

              <span
                v-if="product.categoryName"
                class="inline-block rounded bg-stone-100 px-2 py-0.5 text-xs font-medium text-stone-600"
              >
                {{ product.categoryName }}
              </span>
              <h2 class="mt-2 font-bold text-stone-900">{{ product.name }}</h2>
              <p class="mt-1 text-sm text-stone-600">
                {{ product.description || 'Marmita preparada no dia.' }}
              </p>

              <div
                v-if="product.addons && product.addons.length > 0"
                class="mt-3 space-y-1.5 rounded-lg border border-stone-100 bg-stone-50 p-2.5"
              >
                <p class="text-[11px] font-semibold uppercase text-stone-500">Adicionais:</p>
                <div
                  v-for="addon in product.addons"
                  :key="addon.name"
                  class="flex items-center justify-between text-xs text-stone-700"
                >
                  <span>
                    {{ addon.name }}
                    <strong class="text-stone-900">
                      ({{ addon.extraPrice > 0 ? `+${formatCurrency(addon.extraPrice)}` : 'Grátis' }})
                    </strong>
                    <span class="text-[10px] text-stone-400">máx. {{ addon.maxQuantity }}</span>
                  </span>
                  <div class="flex items-center gap-1.5">
                    <button
                      type="button"
                      class="rounded border border-stone-300 bg-white px-1.5 py-0.5 text-xs font-bold"
                      @click="setAddonQty(product.id, addon.name, getAddonQty(product.id, addon.name) - 1, addon.maxQuantity)"
                    >
                      -
                    </button>
                    <span class="w-4 text-center font-semibold">
                      {{ getAddonQty(product.id, addon.name) }}
                    </span>
                    <button
                      type="button"
                      class="rounded border border-stone-300 bg-white px-1.5 py-0.5 text-xs font-bold"
                      @click="setAddonQty(product.id, addon.name, getAddonQty(product.id, addon.name) + 1, addon.maxQuantity)"
                    >
                      +
                    </button>
                  </div>
                </div>
              </div>

              <div v-if="product.allowsCutlery" class="mt-2.5">
                <label class="flex items-center gap-2 text-xs text-stone-700 cursor-pointer">
                  <input
                    v-model="cutlerySelections[product.id]"
                    type="checkbox"
                    class="h-3.5 w-3.5 rounded border-stone-300 text-brand-600 focus:ring-brand-600"
                  />
                  <span>
                    Incluir talheres
                    <strong>
                      ({{
                        product.cutleryHasExtraCost && product.cutleryPrice
                          ? `+${formatCurrency(product.cutleryPrice)}`
                          : 'Grátis'
                      }})
                    </strong>
                  </span>
                </label>
              </div>
            </div>

            <div class="mt-4 flex items-center justify-between border-t border-stone-100 pt-4">
              <div>
                <span class="text-lg font-bold text-stone-900">
                  {{ isPlanMode ? 'R$ 0,00 (Plano)' : formatCurrency(product.price) }}
                </span>
                <p v-if="isPlanMode" class="text-[11px] text-green-700 font-medium">
                  Valor base coberto pelo plano
                </p>
              </div>

              <button
                v-if="!isDriverOnly"
                type="button"
                class="inline-flex items-center gap-1 rounded-lg bg-brand-600 px-3.5 py-2 text-xs font-semibold text-white hover:bg-brand-700"
                @click="handleAddToCart(product)"
              >
                <Plus class="h-4 w-4" />
                Adicionar
              </button>
              <span
                v-else
                class="rounded bg-stone-100 px-2.5 py-1 text-[11px] font-medium text-stone-600"
              >
                Modo Consulta
              </span>
            </div>
          </div>
        </div>

        <div
          v-else
          class="rounded-xl border border-stone-200 bg-white p-8 text-center text-sm text-stone-500"
        >
          Nenhuma marmita disponível nesta categoria.
        </div>

        <AiMarmitaAssistantModal
          :is-open="isAiModalOpen"
          :restaurant-id="restaurant.id"
          :restaurant-name="restaurant.name"
          :available-products="products"
          @close="isAiModalOpen = false"
          @add-to-cart="handleAddToCart"
        />
      </template>
    </main>
  </div>
</template>

