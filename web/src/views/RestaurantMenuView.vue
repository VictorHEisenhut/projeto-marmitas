<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { ArrowLeft, Heart, Plus, ShoppingCart } from 'lucide-vue-next'
import Navbar from '@/components/Navbar.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import type { Category, Product, Restaurant } from '@/types'

const route = useRoute()
const auth = useAuthStore()
const cart = useCartStore()

const restaurantId = computed(() => String(route.params.id || ''))
const restaurant = ref<Restaurant | null>(null)
const categories = ref<Category[]>([])
const products = ref<Product[]>([])
const selectedCategory = ref('')
const loading = ref(true)
const error = ref('')
const message = ref('')

const filteredProducts = computed(() => {
  const active = products.value.filter((p) => p.isActive)
  if (!selectedCategory.value) return active
  return active.filter((p) => p.categoryId === selectedCategory.value)
})

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
  cart.addItem(restaurant.value, product)
  message.value = `"${product.name}" adicionada ao carrinho.`
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

          <button
            v-if="auth.isAuthenticated"
            type="button"
            class="inline-flex items-center gap-1.5 self-start rounded-lg border border-stone-200 px-3.5 py-2 text-sm font-medium text-stone-700 hover:bg-stone-50"
            @click="handleFavoriteRestaurant"
          >
            <Heart class="h-4 w-4 text-red-500" />
            Favoritar
          </button>
        </div>

        <div
          v-if="cart.totalItems > 0"
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
            </div>

            <div class="mt-4 flex items-center justify-between border-t border-stone-100 pt-4">
              <span class="text-lg font-bold text-stone-900">
                {{ formatCurrency(product.price) }}
              </span>

              <button
                type="button"
                class="inline-flex items-center gap-1 rounded-lg bg-brand-600 px-3.5 py-2 text-xs font-semibold text-white hover:bg-brand-700"
                @click="handleAddToCart(product)"
              >
                <Plus class="h-4 w-4" />
                Adicionar
              </button>
            </div>
          </div>
        </div>

        <div
          v-else
          class="rounded-xl border border-stone-200 bg-white p-8 text-center text-sm text-stone-500"
        >
          Nenhuma marmita disponível nesta categoria.
        </div>
      </template>
    </main>
  </div>
</template>

