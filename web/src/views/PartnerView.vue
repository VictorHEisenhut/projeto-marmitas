<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { Plus, RefreshCw, Store, Trash2 } from 'lucide-vue-next'
import Navbar from '@/components/Navbar.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import type { Category, Product, Restaurant, RestaurantFinancials } from '@/types'

const auth = useAuthStore()

const restaurants = ref<Restaurant[]>([])
const selectedRestaurantId = ref('')
const categories = ref<Category[]>([])
const products = ref<Product[]>([])
const financials = ref<RestaurantFinancials | null>(null)

const loading = ref(true)
const loadingDetails = ref(false)
const savingRestaurant = ref(false)
const savingProduct = ref(false)
const updatingOrder = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const restaurantName = ref('')
const restaurantDocument = ref('')
const restaurantDescription = ref('')

const productName = ref('')
const productDescription = ref('')
const productPrice = ref<number | null>(null)
const productCategoryId = ref('')

const orderIdInput = ref('')
const orderNextStatus = ref('Preparing')

async function loadRestaurants() {
  loading.value = true
  errorMessage.value = ''

  try {
    const { data } = await api.get<Restaurant[]>('/restaurantes')
    const filtered = data.filter(
      (r) =>
        !auth.user?.id ||
        r.ownerId === auth.user.id ||
        auth.hasRole(['admin', 'super_admin']),
    )
    restaurants.value = filtered
    const first = filtered[0]
    if (first && !selectedRestaurantId.value) {
      selectedRestaurantId.value = first.id
    }
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Não foi possível carregar os restaurantes.')
  } finally {
    loading.value = false
  }
}

async function loadRestaurantDetails(restaurantId: string) {
  if (!restaurantId) return
  loadingDetails.value = true
  errorMessage.value = ''

  try {
    const [prodRes, catRes, finRes] = await Promise.allSettled([
      api.get<Product[]>(`/restaurantes/${restaurantId}/cardapio`),
      api.get<Category[]>('/categorias'),
      api.get<RestaurantFinancials>(`/restaurantes/${restaurantId}/financeiro`),
    ])

    if (prodRes.status === 'fulfilled') products.value = prodRes.value.data
    if (catRes.status === 'fulfilled') {
      categories.value = catRes.value.data
      const firstCat = catRes.value.data[0]
      if (firstCat) productCategoryId.value = firstCat.id
    }
    if (finRes.status === 'fulfilled') financials.value = finRes.value.data
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Erro ao carregar detalhes do restaurante.')
  } finally {
    loadingDetails.value = false
  }
}

watch(selectedRestaurantId, (newId) => {
  if (newId) {
    void loadRestaurantDetails(newId)
  }
})

async function handleCreateRestaurant() {
  errorMessage.value = ''
  successMessage.value = ''

  if (!restaurantName.value.trim()) {
    errorMessage.value = 'Informe o nome do restaurante.'
    return
  }
  if (!restaurantDocument.value.trim()) {
    errorMessage.value = 'Informe o CNPJ ou CPF do restaurante.'
    return
  }

  savingRestaurant.value = true
  try {
    const { data } = await api.post<Restaurant>('/restaurantes', {
      name: restaurantName.value.trim(),
      document: restaurantDocument.value.trim(),
      description: restaurantDescription.value.trim(),
      ownerId: auth.user?.id,
    })

    restaurantName.value = ''
    restaurantDocument.value = ''
    restaurantDescription.value = ''
    successMessage.value = 'Restaurante cadastrado com sucesso.'
    await loadRestaurants()
    selectedRestaurantId.value = data.id
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Não foi possível cadastrar o restaurante.')
  } finally {
    savingRestaurant.value = false
  }
}

async function handleCreateProduct() {
  errorMessage.value = ''
  successMessage.value = ''

  if (!selectedRestaurantId.value) return
  if (!productName.value.trim()) {
    errorMessage.value = 'Informe o nome da marmita.'
    return
  }
  if (!productCategoryId.value) {
    errorMessage.value = 'Selecione uma categoria para a marmita.'
    return
  }
  if (!productPrice.value || productPrice.value <= 0) {
    errorMessage.value = 'Informe um preço válido maior que zero.'
    return
  }

  savingProduct.value = true
  try {
    await api.post(`/restaurantes/${selectedRestaurantId.value}/marmitas`, {
      categoryId: productCategoryId.value,
      name: productName.value.trim(),
      description: productDescription.value.trim(),
      price: productPrice.value,
    })

    productName.value = ''
    productDescription.value = ''
    productPrice.value = null
    successMessage.value = 'Marmita adicionada ao cardápio.'
    await loadRestaurantDetails(selectedRestaurantId.value)
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Não foi possível cadastrar a marmita.')
  } finally {
    savingProduct.value = false
  }
}

async function handleToggleAvailability(product: Product) {
  errorMessage.value = ''
  try {
    const { data } = await api.patch<Product>(`/marmitas/${product.id}/disponibilidade`, {
      isActive: !product.isActive,
    })
    product.isActive = data.isActive
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Erro ao alterar disponibilidade.')
  }
}

async function handleDeleteProduct(productId: string) {
  errorMessage.value = ''
  try {
    await api.delete(`/marmitas/${productId}`)
    products.value = products.value.filter((p) => p.id !== productId)
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Erro ao remover marmita.')
  }
}

async function handleUpdateOrderStatus() {
  errorMessage.value = ''
  successMessage.value = ''

  if (!orderIdInput.value.trim()) {
    errorMessage.value = 'Informe o ID do pedido.'
    return
  }

  updatingOrder.value = true
  try {
    await api.post(`/pedidos/${orderIdInput.value.trim()}/historico-status`, {
      status: orderNextStatus.value,
    })
    orderIdInput.value = ''
    successMessage.value = 'Status do pedido atualizado com sucesso.'
    if (selectedRestaurantId.value) {
      await loadRestaurantDetails(selectedRestaurantId.value)
    }
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Não foi possível atualizar o status do pedido.')
  } finally {
    updatingOrder.value = false
  }
}

onMounted(() => {
  void loadRestaurants()
})
</script>

<template>
  <div class="min-h-screen bg-stone-50">
    <Navbar />

    <main class="mx-auto max-w-6xl space-y-8 px-4 py-8 sm:px-6">
      <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <div>
          <h1 class="text-2xl font-bold text-stone-900">Painel do Restaurante</h1>
          <p class="mt-1 text-sm text-stone-600">
            Gerencie seu cardápio de marmitas, pedidos e faturamento.
          </p>
        </div>

        <div class="flex items-center gap-3">
          <select
            v-if="restaurants.length > 1"
            v-model="selectedRestaurantId"
            class="rounded-lg border border-stone-300 bg-white px-3 py-2 text-sm font-medium text-stone-800 focus:border-brand-600 focus:outline-none"
          >
            <option v-for="r in restaurants" :key="r.id" :value="r.id">
              {{ r.name }}
            </option>
          </select>

          <button
            type="button"
            :disabled="loading"
            class="inline-flex items-center gap-2 rounded-lg border border-stone-200 bg-white px-4 py-2 text-sm font-medium text-stone-700 hover:bg-stone-50 disabled:opacity-50"
            @click="loadRestaurants"
          >
            <RefreshCw class="h-4 w-4" :class="{ 'animate-spin': loading }" />
            Atualizar
          </button>
        </div>
      </div>

      <div
        v-if="errorMessage"
        class="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700"
      >
        {{ errorMessage }}
      </div>

      <div
        v-if="successMessage"
        class="rounded-xl border border-green-200 bg-green-50 p-4 text-sm text-green-800"
      >
        {{ successMessage }}
      </div>

      <p v-if="loading" class="text-sm text-stone-500">Carregando dados...</p>

      <div
        v-else-if="restaurants.length === 0"
        class="mx-auto max-w-lg rounded-xl border border-stone-200 bg-white p-6 shadow-sm"
      >
        <div class="text-center">
          <Store class="mx-auto h-10 w-10 text-brand-600" />
          <h2 class="mt-3 text-base font-semibold text-stone-900">
            Cadastrar seu restaurante
          </h2>
          <p class="mt-1 text-sm text-stone-500">
            Vincule seu restaurante à sua conta para gerenciar o cardápio e os pedidos.
          </p>
        </div>

        <form class="mt-6 space-y-4" @submit.prevent="handleCreateRestaurant">
          <div>
            <label class="block text-xs font-medium text-stone-700">Nome do restaurante</label>
            <input
              v-model="restaurantName"
              type="text"
              required
              placeholder="Ex: Cozinha Caseira da Maria"
              class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900 focus:border-brand-600 focus:outline-none"
            />
          </div>

          <div>
            <label class="block text-xs font-medium text-stone-700">CNPJ / CPF</label>
            <input
              v-model="restaurantDocument"
              type="text"
              required
              placeholder="00.000.000/0001-00"
              class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900 focus:border-brand-600 focus:outline-none"
            />
          </div>

          <div>
            <label class="block text-xs font-medium text-stone-700">Descrição</label>
            <textarea
              v-model="restaurantDescription"
              rows="2"
              placeholder="Especialidades e horário de atendimento"
              class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900 focus:border-brand-600 focus:outline-none"
            />
          </div>

          <button
            type="submit"
            :disabled="savingRestaurant"
            class="w-full rounded-lg bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
          >
            {{ savingRestaurant ? 'Cadastrando...' : 'Criar e vincular restaurante' }}
          </button>
        </form>
      </div>

      <template v-else>
        <section v-if="financials" class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <div class="rounded-xl border border-stone-200 bg-white p-5 shadow-sm">
            <span class="text-xs font-medium text-stone-500">Pedidos válidos</span>
            <p class="mt-2 text-2xl font-bold text-stone-900">{{ financials.totalOrders }}</p>
          </div>

          <div class="rounded-xl border border-stone-200 bg-white p-5 shadow-sm">
            <span class="text-xs font-medium text-stone-500">Receita bruta</span>
            <p class="mt-2 text-2xl font-bold text-stone-900">
              {{ formatCurrency(financials.grossRevenue) }}
            </p>
          </div>

          <div class="rounded-xl border border-stone-200 bg-white p-5 shadow-sm">
            <span class="text-xs font-medium text-stone-500">Comissão (10%)</span>
            <p class="mt-2 text-2xl font-bold text-stone-700">
              {{ formatCurrency(financials.platformCommission) }}
            </p>
          </div>

          <div class="rounded-xl border border-stone-200 bg-white p-5 shadow-sm">
            <span class="text-xs font-medium text-stone-500">Valor líquido</span>
            <p class="mt-2 text-2xl font-bold text-green-700">
              {{ formatCurrency(financials.netRevenue) }}
            </p>
          </div>
        </section>

        <div class="grid gap-6 lg:grid-cols-3">
          <section class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm lg:col-span-1">
            <h2 class="text-base font-bold text-stone-900">Cadastrar marmita</h2>

            <form class="mt-4 space-y-3" @submit.prevent="handleCreateProduct">
              <div>
                <label class="block text-xs font-medium text-stone-600">Categoria</label>
                <select
                  v-model="productCategoryId"
                  required
                  class="mt-1 w-full rounded-lg border border-stone-300 bg-white px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
                >
                  <option value="" disabled>Selecione...</option>
                  <option v-for="cat in categories" :key="cat.id" :value="cat.id">
                    {{ cat.name }}
                  </option>
                </select>
              </div>

              <div>
                <label class="block text-xs font-medium text-stone-600">Nome do prato</label>
                <input
                  v-model="productName"
                  type="text"
                  required
                  placeholder="Ex: Frango grelhado com batata doce"
                  class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
                />
              </div>

              <div>
                <label class="block text-xs font-medium text-stone-600">Descrição</label>
                <textarea
                  v-model="productDescription"
                  rows="2"
                  placeholder="Ingredientes e acompanhamentos"
                  class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
                />
              </div>

              <div>
                <label class="block text-xs font-medium text-stone-600">Preço (R$)</label>
                <input
                  v-model.number="productPrice"
                  type="number"
                  step="0.01"
                  min="0.01"
                  required
                  placeholder="24.90"
                  class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
                />
              </div>

              <button
                type="submit"
                :disabled="savingProduct"
                class="inline-flex w-full items-center justify-center gap-1.5 rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-50"
              >
                <Plus class="h-4 w-4" />
                {{ savingProduct ? 'Salvando...' : 'Adicionar ao cardápio' }}
              </button>
            </form>

            <div class="mt-8 border-t border-stone-100 pt-6">
              <h3 class="text-sm font-bold text-stone-900">Atualizar status de pedido</h3>
              <form class="mt-3 space-y-3" @submit.prevent="handleUpdateOrderStatus">
                <div>
                  <label class="block text-xs font-medium text-stone-600">ID do pedido</label>
                  <input
                    v-model="orderIdInput"
                    type="text"
                    placeholder="UUID do pedido"
                    class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
                  />
                </div>

                <div>
                  <label class="block text-xs font-medium text-stone-600">Novo status</label>
                  <select
                    v-model="orderNextStatus"
                    class="mt-1 w-full rounded-lg border border-stone-300 bg-white px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
                  >
                    <option value="Preparing">Em preparo</option>
                    <option value="ReadyForPickup">Pronto para retirada</option>
                    <option value="Delivered">Entregue</option>
                    <option value="Cancelled">Cancelado</option>
                  </select>
                </div>

                <button
                  type="submit"
                  :disabled="updatingOrder"
                  class="w-full rounded-lg bg-stone-900 px-4 py-2 text-sm font-semibold text-white hover:bg-stone-800 disabled:opacity-50"
                >
                  {{ updatingOrder ? 'Atualizando...' : 'Atualizar pedido' }}
                </button>
              </form>
            </div>
          </section>

          <section class="space-y-4 lg:col-span-2">
            <h2 class="text-base font-bold text-stone-900">Itens do cardápio</h2>

            <p v-if="loadingDetails" class="text-sm text-stone-500">Carregando cardápio...</p>

            <div v-else-if="products.length > 0" class="space-y-3">
              <div
                v-for="product in products"
                :key="product.id"
                class="flex flex-col justify-between gap-3 rounded-xl border border-stone-200 bg-white p-4 shadow-sm sm:flex-row sm:items-center"
              >
                <div>
                  <div class="flex items-center gap-2">
                    <h3 class="font-semibold text-stone-900">{{ product.name }}</h3>
                    <span
                      class="rounded px-2 py-0.5 text-xs font-medium"
                      :class="
                        product.isActive
                          ? 'bg-green-50 text-green-700'
                          : 'bg-stone-100 text-stone-500'
                      "
                    >
                      {{ product.isActive ? 'Ativo' : 'Pausado' }}
                    </span>
                  </div>
                  <p class="mt-0.5 text-xs text-stone-500">
                    {{ product.categoryName || 'Sem categoria' }} •
                    {{ formatCurrency(product.price) }}
                  </p>
                  <p v-if="product.description" class="mt-1 text-xs text-stone-600">
                    {{ product.description }}
                  </p>
                </div>

                <div class="flex items-center gap-2">
                  <button
                    type="button"
                    class="rounded-lg border border-stone-200 px-3 py-1.5 text-xs font-medium text-stone-700 hover:bg-stone-50"
                    @click="handleToggleAvailability(product)"
                  >
                    {{ product.isActive ? 'Pausar' : 'Ativar' }}
                  </button>
                  <button
                    type="button"
                    class="rounded-lg border border-red-200 p-1.5 text-red-600 hover:bg-red-50"
                    @click="handleDeleteProduct(product.id)"
                  >
                    <Trash2 class="h-4 w-4" />
                  </button>
                </div>
              </div>
            </div>

            <div
              v-else
              class="rounded-xl border border-stone-200 bg-white p-8 text-center text-sm text-stone-500"
            >
              Nenhuma marmita cadastrada neste restaurante.
            </div>
          </section>
        </div>
      </template>
    </main>
  </div>
</template>
