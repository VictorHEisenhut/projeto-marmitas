<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { AlertCircle, Bike, Loader2, MessageSquareReply, Plus, RefreshCw, Sparkles, Store, Trash2 } from 'lucide-vue-next'
import Navbar from '@/components/Navbar.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import { generateMarmitaWithAi } from '@/services/ai'
import { useAuthStore } from '@/stores/auth'
import type { Category, NutritionalEstimate, Order, Product, Restaurant, RestaurantFinancials } from '@/types'

const auth = useAuthStore()

const restaurants = ref<Restaurant[]>([])
const selectedRestaurantId = ref('')
const categories = ref<Category[]>([])
const products = ref<Product[]>([])
const restaurantOrders = ref<Order[]>([])
const financials = ref<RestaurantFinancials | null>(null)

const loading = ref(true)
const loadingDetails = ref(false)
const savingRestaurant = ref(false)
const savingProduct = ref(false)
const generatingWithAi = ref(false)
const aiGenMessage = ref('')
const aiGenNutrition = ref<NutritionalEstimate | null>(null)
const updatingOrder = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const restaurantName = ref('')
const restaurantDocument = ref('')
const restaurantDescription = ref('')
const restaurantZipCode = ref('')
const restaurantStreet = ref('')
const restaurantNumber = ref('')
const restaurantComplement = ref('')
const restaurantNeighborhood = ref('')
const restaurantCity = ref('')
const restaurantState = ref('SP')

const editingRestaurantInfo = ref(false)
const editRestName = ref('')
const editRestDocument = ref('')
const editRestDescription = ref('')
const editRestZipCode = ref('')
const editRestStreet = ref('')
const editRestNumber = ref('')
const editRestComplement = ref('')
const editRestNeighborhood = ref('')
const editRestCity = ref('')
const editRestState = ref('SP')

function syncSelectedRestaurantForm(restaurantId: string) {
  const current = restaurants.value.find((r) => r.id === restaurantId)
  if (!current) return
  editRestName.value = current.name || ''
  editRestDocument.value = current.document || ''
  editRestDescription.value = current.description || ''
  editRestZipCode.value = current.addressZipCode || ''
  editRestStreet.value = current.addressStreet || ''
  editRestNumber.value = current.addressNumber || ''
  editRestComplement.value = current.addressComplement || ''
  editRestNeighborhood.value = current.addressNeighborhood || ''
  editRestCity.value = current.addressCity || ''
  editRestState.value = current.addressState || 'SP'

  // Se faltar CNPJ ou endereço, abre o formulário automaticamente para preenchimento obrigatório
  if (
    !current.document?.trim() ||
    !current.addressStreet?.trim() ||
    !current.addressNumber?.trim() ||
    !current.addressNeighborhood?.trim() ||
    !current.addressCity?.trim() ||
    !current.addressState?.trim() ||
    !current.addressZipCode?.trim()
  ) {
    editingRestaurantInfo.value = true
  }
}

const productName = ref('')
const productDescription = ref('')
const productPrice = ref<number | null>(null)
const productCategoryId = ref('')
const availableInPlans = ref(true)
const allowsCutlery = ref(false)
const cutleryHasExtraCost = ref(false)
const cutleryPrice = ref<number | null>(null)
const productAddons = ref<Array<{ name: string; extraPrice: number; maxQuantity: number }>>([])

function addAddonRow() {
  productAddons.value.push({
    name: '',
    extraPrice: 0,
    maxQuantity: 1,
  })
}

function removeAddonRow(index: number) {
  productAddons.value.splice(index, 1)
}

const orderIdInput = ref('')
const orderNextStatus = ref('Preparing')

const orderFilterTab = ref<'all' | 'active' | 'past' | 'complaints'>('all')
const replyingOrderId = ref<string | null>(null)
const complaintReplyText = ref('')
const sendingComplaintReply = ref(false)

const activeRestaurantOrders = computed(() =>
  restaurantOrders.value.filter(
    (o) => o.status !== 'Delivered' && o.status !== 'Cancelled',
  ),
)

const pastRestaurantOrders = computed(() =>
  restaurantOrders.value.filter(
    (o) => o.status === 'Delivered' || o.status === 'Cancelled',
  ),
)

const complaintRestaurantOrders = computed(() =>
  restaurantOrders.value.filter((o) => Boolean(o.complaintMessage)),
)

const filteredRestaurantOrders = computed(() => {
  if (orderFilterTab.value === 'active') return activeRestaurantOrders.value
  if (orderFilterTab.value === 'past') return pastRestaurantOrders.value
  if (orderFilterTab.value === 'complaints') return complaintRestaurantOrders.value
  return restaurantOrders.value
})

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
    if (selectedRestaurantId.value) {
      syncSelectedRestaurantForm(selectedRestaurantId.value)
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
    const [prodRes, catRes, finRes, ordRes] = await Promise.allSettled([
      api.get<Product[]>(`/restaurantes/${restaurantId}/cardapio`),
      api.get<Category[]>('/categorias'),
      api.get<RestaurantFinancials>(`/restaurantes/${restaurantId}/financeiro`),
      api.get<Order[]>('/pedidos', { params: { restaurantId } }),
    ])

    if (prodRes.status === 'fulfilled') products.value = prodRes.value.data
    if (catRes.status === 'fulfilled') {
      categories.value = catRes.value.data
      const firstCat = catRes.value.data[0]
      if (firstCat) productCategoryId.value = firstCat.id
    }
    if (finRes.status === 'fulfilled') financials.value = finRes.value.data
    if (ordRes.status === 'fulfilled') restaurantOrders.value = ordRes.value.data
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Erro ao carregar detalhes do restaurante.')
  } finally {
    loadingDetails.value = false
  }
}

watch(selectedRestaurantId, (newId) => {
  if (newId) {
    syncSelectedRestaurantForm(newId)
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
    errorMessage.value = 'O CNPJ do restaurante é obrigatório.'
    return
  }
  if (
    !restaurantZipCode.value.trim() ||
    !restaurantStreet.value.trim() ||
    !restaurantNumber.value.trim() ||
    !restaurantNeighborhood.value.trim() ||
    !restaurantCity.value.trim() ||
    !restaurantState.value.trim()
  ) {
    errorMessage.value =
      'O endereço completo do restaurante (CEP, Rua, Número, Bairro, Cidade e UF) é obrigatório.'
    return
  }

  savingRestaurant.value = true
  try {
    const { data } = await api.post<Restaurant>('/restaurantes', {
      name: restaurantName.value.trim(),
      document: restaurantDocument.value.trim(),
      description: restaurantDescription.value.trim(),
      ownerId: auth.user?.id,
      addressZipCode: restaurantZipCode.value.trim(),
      addressStreet: restaurantStreet.value.trim(),
      addressNumber: restaurantNumber.value.trim(),
      addressComplement: restaurantComplement.value.trim() || null,
      addressNeighborhood: restaurantNeighborhood.value.trim(),
      addressCity: restaurantCity.value.trim(),
      addressState: restaurantState.value.trim().toUpperCase(),
    })

    restaurantName.value = ''
    restaurantDocument.value = ''
    restaurantDescription.value = ''
    restaurantZipCode.value = ''
    restaurantStreet.value = ''
    restaurantNumber.value = ''
    restaurantComplement.value = ''
    restaurantNeighborhood.value = ''
    restaurantCity.value = ''
    restaurantState.value = 'SP'
    successMessage.value = 'Restaurante cadastrado com CNPJ e endereço completo!'
    await loadRestaurants()
    selectedRestaurantId.value = data.id
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Não foi possível cadastrar o restaurante.')
  } finally {
    savingRestaurant.value = false
  }
}

async function handleUpdateRestaurantInfo() {
  if (!selectedRestaurantId.value) return
  errorMessage.value = ''
  successMessage.value = ''

  if (!editRestName.value.trim()) {
    errorMessage.value = 'O nome do restaurante é obrigatório.'
    return
  }
  if (!editRestDocument.value.trim()) {
    errorMessage.value = 'O CNPJ do restaurante é obrigatório.'
    return
  }
  if (
    !editRestZipCode.value.trim() ||
    !editRestStreet.value.trim() ||
    !editRestNumber.value.trim() ||
    !editRestNeighborhood.value.trim() ||
    !editRestCity.value.trim() ||
    !editRestState.value.trim()
  ) {
    errorMessage.value =
      'O endereço completo do restaurante (CEP, Rua, Número, Bairro, Cidade e UF) é obrigatório.'
    return
  }

  savingRestaurant.value = true
  try {
    await api.put<Restaurant>(`/restaurantes/${selectedRestaurantId.value}`, {
      name: editRestName.value.trim(),
      document: editRestDocument.value.trim(),
      description: editRestDescription.value.trim(),
      ownerId: auth.user?.id,
      addressZipCode: editRestZipCode.value.trim(),
      addressStreet: editRestStreet.value.trim(),
      addressNumber: editRestNumber.value.trim(),
      addressComplement: editRestComplement.value.trim() || null,
      addressNeighborhood: editRestNeighborhood.value.trim(),
      addressCity: editRestCity.value.trim(),
      addressState: editRestState.value.trim().toUpperCase(),
    })
    editingRestaurantInfo.value = false
    successMessage.value = 'Dados cadastrais do restaurante (CNPJ e Endereço) atualizados com sucesso!'
    await loadRestaurants()
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Não foi possível atualizar os dados do restaurante.')
  } finally {
    savingRestaurant.value = false
  }
}

async function handleGenerateWithAi() {
  if (!productName.value.trim()) {
    errorMessage.value = 'Digite o nome base ou ingredientes do prato para a IA criar a descrição.'
    return
  }

  generatingWithAi.value = true
  errorMessage.value = ''
  aiGenMessage.value = ''
  aiGenNutrition.value = null

  try {
    const selectedCat = categories.value.find((c) => c.id === productCategoryId.value)?.name
    const res = await generateMarmitaWithAi({
      baseNameOrIngredients: productName.value.trim(),
      targetCategory: selectedCat || null,
      price: productPrice.value || null,
    })

    if (res.suggestedName) productName.value = res.suggestedName
    if (res.gourmetDescription) productDescription.value = res.gourmetDescription
    aiGenNutrition.value = res.nutritionalEstimate

    if (res.suggestedCategory && categories.value.length > 0) {
      const match = categories.value.find(
        (c) =>
          c.name.toLowerCase().includes(res.suggestedCategory.toLowerCase()) ||
          res.suggestedCategory.toLowerCase().includes(c.name.toLowerCase()),
      )
      if (match) productCategoryId.value = match.id
    }

    aiGenMessage.value = 'Prato otimizado pela IA do Gemini!'
  } catch (err) {
    errorMessage.value = getErrorMessage(err, 'Não foi possível gerar detalhes com IA no momento.')
  } finally {
    generatingWithAi.value = false
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
  if (!productDescription.value.trim()) {
    errorMessage.value = 'Informe a descrição da marmita.'
    return
  }
  if (!productCategoryId.value) {
    errorMessage.value = 'Selecione uma categoria para a marmita.'
    return
  }
  if (!productPrice.value || productPrice.value <= 0) {
    errorMessage.value = 'Informe um valor avulso válido maior que zero.'
    return
  }
  if (allowsCutlery.value && cutleryHasExtraCost.value && (!cutleryPrice.value || cutleryPrice.value <= 0)) {
    errorMessage.value = 'Informe um valor maior que zero para o custo extra de talheres.'
    return
  }

  for (const addon of productAddons.value) {
    if (!addon.name.trim()) {
      errorMessage.value = 'Preencha o nome de todos os adicionais adicionados.'
      return
    }
    if (addon.extraPrice < 0) {
      errorMessage.value = `O valor extra do adicional "${addon.name}" não pode ser negativo.`
      return
    }
    if (!addon.maxQuantity || addon.maxQuantity < 1) {
      errorMessage.value = `O limite máximo do adicional "${addon.name}" deve ser pelo menos 1.`
      return
    }
  }

  savingProduct.value = true
  try {
    await api.post(`/restaurantes/${selectedRestaurantId.value}/marmitas`, {
      categoryId: productCategoryId.value,
      name: productName.value.trim(),
      description: productDescription.value.trim(),
      price: productPrice.value,
      availableInPlans: availableInPlans.value,
      allowsCutlery: allowsCutlery.value,
      cutleryHasExtraCost: allowsCutlery.value && cutleryHasExtraCost.value,
      cutleryPrice: allowsCutlery.value && cutleryHasExtraCost.value ? cutleryPrice.value : 0,
      addons: productAddons.value.map((a) => ({
        name: a.name.trim(),
        extraPrice: Number(a.extraPrice) || 0,
        maxQuantity: Number(a.maxQuantity) || 1,
      })),
    })

    productName.value = ''
    productDescription.value = ''
    productPrice.value = null
    availableInPlans.value = true
    allowsCutlery.value = false
    cutleryHasExtraCost.value = false
    cutleryPrice.value = null
    productAddons.value = []
    aiGenNutrition.value = null
    aiGenMessage.value = ''
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

async function handleQuickOrderStatus(orderId: string, status: string) {
  errorMessage.value = ''
  successMessage.value = ''
  updatingOrder.value = true

  try {
    await api.post(`/pedidos/${orderId}/historico-status`, { status })
    successMessage.value =
      status === 'ReadyForPickup'
        ? 'Serviço de entrega solicitado! Pedido enviado para a fila dos entregadores.'
        : 'Status do pedido atualizado com sucesso.'
    if (selectedRestaurantId.value) {
      await loadRestaurantDetails(selectedRestaurantId.value)
    }
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Não foi possível atualizar o status do pedido.')
  } finally {
    updatingOrder.value = false
  }
}

async function handleReplyComplaint(orderId: string) {
  errorMessage.value = ''
  successMessage.value = ''

  if (!complaintReplyText.value.trim()) {
    errorMessage.value = 'Digite uma resposta para a reclamação do cliente.'
    return
  }

  sendingComplaintReply.value = true
  try {
    await api.post(`/pedidos/${orderId}/reclamacao/resposta`, {
      reply: complaintReplyText.value.trim(),
    })
    replyingOrderId.value = null
    complaintReplyText.value = ''
    successMessage.value = 'Resposta enviada ao cliente com sucesso!'
    if (selectedRestaurantId.value) {
      await loadRestaurantDetails(selectedRestaurantId.value)
    }
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Não foi possível responder à reclamação.')
  } finally {
    sendingComplaintReply.value = false
  }
}

function formatOrderStatus(status: string): string {
  const map: Record<string, string> = {
    Created: 'Criado',
    Paid: 'Pago',
    Preparing: 'Em preparo',
    ReadyForPickup: 'Pronto (Aguardando Entregador)',
    DriverAssigned: 'Entregador a caminho',
    InTransit: 'Em rota de entrega',
    Delivered: 'Entregue',
    Cancelled: 'Cancelado',
  }
  return map[status] || status
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
            <label class="block text-xs font-medium text-stone-700">
              Nome do restaurante <span class="text-red-500">*</span>
            </label>
            <input
              v-model="restaurantName"
              type="text"
              required
              placeholder="Ex: Cozinha Caseira da Maria"
              class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900 focus:border-brand-600 focus:outline-none"
            />
          </div>

          <div>
            <label class="block text-xs font-medium text-stone-700">
              CNPJ do Restaurante <span class="text-red-500">*</span>
            </label>
            <input
              v-model="restaurantDocument"
              type="text"
              required
              placeholder="00.000.000/0001-00"
              class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900 focus:border-brand-600 focus:outline-none"
            />
          </div>

          <div class="rounded-lg border border-stone-200 bg-stone-50 p-3.5 space-y-3">
            <h3 class="text-xs font-bold uppercase tracking-wide text-stone-700">
              Endereço do Restaurante (Obrigatório)
            </h3>

            <div class="grid gap-3 sm:grid-cols-3">
              <div>
                <label class="block text-xs font-medium text-stone-700">
                  CEP <span class="text-red-500">*</span>
                </label>
                <input
                  v-model="restaurantZipCode"
                  type="text"
                  required
                  placeholder="00000-000"
                  class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900 focus:border-brand-600 focus:outline-none"
                />
              </div>

              <div class="sm:col-span-2">
                <label class="block text-xs font-medium text-stone-700">
                  Rua / Logradouro <span class="text-red-500">*</span>
                </label>
                <input
                  v-model="restaurantStreet"
                  type="text"
                  required
                  placeholder="Ex: Rua Harmonia"
                  class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900 focus:border-brand-600 focus:outline-none"
                />
              </div>

              <div>
                <label class="block text-xs font-medium text-stone-700">
                  Número <span class="text-red-500">*</span>
                </label>
                <input
                  v-model="restaurantNumber"
                  type="text"
                  required
                  placeholder="450"
                  class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900 focus:border-brand-600 focus:outline-none"
                />
              </div>

              <div>
                <label class="block text-xs font-medium text-stone-700">
                  Bairro <span class="text-red-500">*</span>
                </label>
                <input
                  v-model="restaurantNeighborhood"
                  type="text"
                  required
                  placeholder="Ex: Vila Madalena"
                  class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900 focus:border-brand-600 focus:outline-none"
                />
              </div>

              <div>
                <label class="block text-xs font-medium text-stone-700">
                  Cidade / UF <span class="text-red-500">*</span>
                </label>
                <div class="mt-1 flex gap-1.5">
                  <input
                    v-model="restaurantCity"
                    type="text"
                    required
                    placeholder="São Paulo"
                    class="block w-full rounded-lg border border-stone-300 px-2.5 py-2 text-sm text-stone-900 focus:border-brand-600 focus:outline-none"
                  />
                  <input
                    v-model="restaurantState"
                    type="text"
                    maxlength="2"
                    required
                    placeholder="SP"
                    class="block w-14 rounded-lg border border-stone-300 px-2 py-2 text-center text-sm uppercase text-stone-900 focus:border-brand-600 focus:outline-none"
                  />
                </div>
              </div>
            </div>
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
        <!-- Dados Cadastrais do Restaurante (CNPJ e Endereço Obrigatório) -->
        <section class="rounded-xl border border-stone-200 bg-white p-5 shadow-sm">
          <div class="flex flex-col justify-between gap-3 sm:flex-row sm:items-center">
            <div>
              <h2 class="text-base font-bold text-stone-900">
                Dados Cadastrais do Restaurante (CNPJ e Endereço Obrigatório)
              </h2>
              <p class="mt-0.5 text-xs text-stone-600">
                <strong>CNPJ:</strong> {{ editRestDocument || 'Não informado' }} •
                <strong>Endereço:</strong>
                <span v-if="editRestStreet && editRestNeighborhood">
                  {{ editRestStreet }}, {{ editRestNumber }} — Bairro {{ editRestNeighborhood }}, {{ editRestCity }}/{{ editRestState }} (CEP {{ editRestZipCode }})
                </span>
                <span v-else class="text-red-600 font-semibold">
                  Endereço pendente (preenchimento obrigatório)
                </span>
              </p>
            </div>

            <button
              type="button"
              class="self-start rounded-lg border border-stone-200 px-3.5 py-1.5 text-xs font-semibold text-stone-700 hover:bg-stone-50 sm:self-auto"
              @click="editingRestaurantInfo = !editingRestaurantInfo"
            >
              {{ editingRestaurantInfo ? 'Fechar edição' : 'Editar CNPJ e Endereço' }}
            </button>
          </div>

          <form
            v-if="editingRestaurantInfo"
            class="mt-4 grid gap-3 border-t border-stone-100 pt-4 sm:grid-cols-6"
            @submit.prevent="handleUpdateRestaurantInfo"
          >
            <div class="sm:col-span-3">
              <label class="block text-xs font-medium text-stone-700">
                Nome do Restaurante <span class="text-red-500">*</span>
              </label>
              <input
                v-model="editRestName"
                type="text"
                required
                class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
              />
            </div>

            <div class="sm:col-span-3">
              <label class="block text-xs font-medium text-stone-700">
                CNPJ <span class="text-red-500">*</span>
              </label>
              <input
                v-model="editRestDocument"
                type="text"
                required
                placeholder="00.000.000/0001-00"
                class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
              />
            </div>

            <div class="sm:col-span-2">
              <label class="block text-xs font-medium text-stone-700">
                CEP <span class="text-red-500">*</span>
              </label>
              <input
                v-model="editRestZipCode"
                type="text"
                required
                placeholder="00000-000"
                class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
              />
            </div>

            <div class="sm:col-span-3">
              <label class="block text-xs font-medium text-stone-700">
                Rua / Logradouro <span class="text-red-500">*</span>
              </label>
              <input
                v-model="editRestStreet"
                type="text"
                required
                placeholder="Ex: Rua Harmonia"
                class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
              />
            </div>

            <div class="sm:col-span-1">
              <label class="block text-xs font-medium text-stone-700">
                Número <span class="text-red-500">*</span>
              </label>
              <input
                v-model="editRestNumber"
                type="text"
                required
                placeholder="450"
                class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
              />
            </div>

            <div class="sm:col-span-2">
              <label class="block text-xs font-medium text-stone-700">
                Bairro <span class="text-red-500">*</span>
              </label>
              <input
                v-model="editRestNeighborhood"
                type="text"
                required
                placeholder="Vila Madalena"
                class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
              />
            </div>

            <div class="sm:col-span-2">
              <label class="block text-xs font-medium text-stone-700">
                Cidade <span class="text-red-500">*</span>
              </label>
              <input
                v-model="editRestCity"
                type="text"
                required
                placeholder="São Paulo"
                class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
              />
            </div>

            <div class="sm:col-span-1">
              <label class="block text-xs font-medium text-stone-700">
                UF <span class="text-red-500">*</span>
              </label>
              <input
                v-model="editRestState"
                type="text"
                maxlength="2"
                required
                placeholder="SP"
                class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm uppercase focus:border-brand-600 focus:outline-none"
              />
            </div>

            <div class="sm:col-span-1">
              <label class="block text-xs font-medium text-stone-700">Complemento</label>
              <input
                v-model="editRestComplement"
                type="text"
                placeholder="Loja 1"
                class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
              />
            </div>

            <div class="sm:col-span-6">
              <button
                type="submit"
                :disabled="savingRestaurant"
                class="rounded-lg bg-brand-600 px-4 py-2 text-xs font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
              >
                {{ savingRestaurant ? 'Salvando...' : 'Salvar CNPJ e Endereço do Restaurante' }}
              </button>
            </div>
          </form>
        </section>

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
                <div class="flex items-center justify-between">
                  <label class="block text-xs font-medium text-stone-600">Nome do prato ou ingredientes</label>
                  <button
                    type="button"
                    :disabled="generatingWithAi || !productName.trim()"
                    class="inline-flex items-center gap-1 text-xs font-semibold text-brand-600 hover:text-brand-700 disabled:opacity-40"
                    title="Gera nome apetitoso, descrição gourmet e estimativa nutricional com Gemini 3.8 Flash"
                    @click="handleGenerateWithAi"
                  >
                    <Loader2 v-if="generatingWithAi" class="h-3.5 w-3.5 animate-spin" />
                    <Sparkles v-else class="h-3.5 w-3.5" />
                    <span>{{ generatingWithAi ? 'Otimizando...' : '✨ Otimizar com IA' }}</span>
                  </button>
                </div>
                <input
                  v-model="productName"
                  type="text"
                  required
                  placeholder="Ex: Frango grelhado com batata doce e brócolis"
                  class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
                />
              </div>

              <div>
                <label class="block text-xs font-medium text-stone-600">Descrição</label>
                <textarea
                  v-model="productDescription"
                  rows="3"
                  placeholder="Ingredientes e acompanhamentos"
                  class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
                />
              </div>

              <div
                v-if="aiGenNutrition"
                class="rounded-lg border border-amber-200 bg-amber-50/70 p-3 text-xs text-stone-700 space-y-1"
              >
                <div class="flex items-center gap-1.5 font-semibold text-amber-900">
                  <Sparkles class="h-3.5 w-3.5 text-brand-600" />
                  <span>Estimativa Nutricional (Gemini IA):</span>
                </div>
                <div class="flex flex-wrap items-center gap-2 text-stone-600 pt-1">
                  <span class="font-medium text-amber-700">~{{ aiGenNutrition.calories }} kcal</span>
                  <span>•</span>
                  <span class="font-medium text-emerald-700">{{ aiGenNutrition.protein }}g proteína</span>
                  <span>•</span>
                  <span>{{ aiGenNutrition.carbs }}g carboidratos</span>
                  <span>•</span>
                  <span>{{ aiGenNutrition.fat }}g gorduras</span>
                </div>
              </div>

              <div>
                <label class="block text-xs font-medium text-stone-600">Valor Avulso (R$)</label>
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

              <div class="rounded-lg border border-stone-200 bg-stone-50 p-3 space-y-2">
                <label class="flex items-center gap-2 text-xs font-semibold text-stone-800 cursor-pointer">
                  <input
                    v-model="availableInPlans"
                    type="checkbox"
                    class="h-4 w-4 rounded border-stone-300 text-brand-600 focus:ring-brand-600"
                  />
                  <span>Disponível em Planos Mensais (Assinatura)</span>
                </label>
                <p class="text-[11px] text-stone-500">
                  Desmarque caso esta marmita seja exclusiva para compra avulsa.
                </p>
              </div>

              <div class="rounded-lg border border-stone-200 bg-stone-50 p-3 space-y-2">
                <label class="flex items-center gap-2 text-xs font-semibold text-stone-800 cursor-pointer">
                  <input
                    v-model="allowsCutlery"
                    type="checkbox"
                    class="h-4 w-4 rounded border-stone-300 text-brand-600 focus:ring-brand-600"
                  />
                  <span>Prato permite opção de talheres?</span>
                </label>

                <div v-if="allowsCutlery" class="space-y-2 pt-1 pl-1">
                  <div class="flex items-center gap-4 text-xs text-stone-700">
                    <label class="flex items-center gap-1.5 cursor-pointer">
                      <input
                        v-model="cutleryHasExtraCost"
                        type="radio"
                        :value="false"
                        class="text-brand-600 focus:ring-brand-600"
                      />
                      <span>Gratuito</span>
                    </label>
                    <label class="flex items-center gap-1.5 cursor-pointer">
                      <input
                        v-model="cutleryHasExtraCost"
                        type="radio"
                        :value="true"
                        class="text-brand-600 focus:ring-brand-600"
                      />
                      <span>Com custo extra</span>
                    </label>
                  </div>

                  <div v-if="cutleryHasExtraCost">
                    <label class="block text-[11px] font-medium text-stone-600">
                      Valor do talher (R$)
                    </label>
                    <input
                      v-model.number="cutleryPrice"
                      type="number"
                      step="0.01"
                      min="0.01"
                      required
                      placeholder="1.50"
                      class="mt-1 w-full rounded-lg border border-stone-300 bg-white px-2.5 py-1.5 text-xs focus:border-brand-600 focus:outline-none"
                    />
                  </div>
                </div>
              </div>

              <div class="rounded-lg border border-stone-200 bg-stone-50 p-3 space-y-2.5">
                <div class="flex items-center justify-between">
                  <span class="text-xs font-semibold text-stone-800">Adicionais (Extras)</span>
                  <button
                    type="button"
                    class="inline-flex items-center gap-1 rounded border border-brand-200 bg-white px-2 py-1 text-[11px] font-semibold text-brand-700 hover:bg-brand-50"
                    @click="addAddonRow"
                  >
                    <Plus class="h-3 w-3" />
                    Adicionar novo extra
                  </button>
                </div>

                <div
                  v-for="(addon, idx) in productAddons"
                  :key="idx"
                  class="rounded border border-stone-200 bg-white p-2.5 space-y-2"
                >
                  <div class="flex items-center justify-between gap-2">
                    <input
                      v-model="addon.name"
                      type="text"
                      required
                      placeholder="Nome (ex: Ovo frito, Mais proteína)"
                      class="w-full rounded border border-stone-300 px-2 py-1 text-xs focus:border-brand-600 focus:outline-none"
                    />
                    <button
                      type="button"
                      class="text-red-500 hover:text-red-700"
                      title="Remover adicional"
                      @click="removeAddonRow(idx)"
                    >
                      <Trash2 class="h-3.5 w-3.5" />
                    </button>
                  </div>

                  <div class="grid grid-cols-2 gap-2">
                    <div>
                      <label class="block text-[10px] font-medium text-stone-500">
                        Valor Extra (R$)
                      </label>
                      <input
                        v-model.number="addon.extraPrice"
                        type="number"
                        step="0.01"
                        min="0"
                        required
                        class="mt-0.5 w-full rounded border border-stone-300 px-2 py-1 text-xs focus:border-brand-600 focus:outline-none"
                      />
                    </div>
                    <div>
                      <label class="block text-[10px] font-medium text-stone-500">
                        Limite Máx.
                      </label>
                      <input
                        v-model.number="addon.maxQuantity"
                        type="number"
                        min="1"
                        step="1"
                        required
                        class="mt-0.5 w-full rounded border border-stone-300 px-2 py-1 text-xs focus:border-brand-600 focus:outline-none"
                      />
                    </div>
                  </div>
                </div>
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

            <div class="pt-4 space-y-4">
              <div class="flex flex-col justify-between gap-3 sm:flex-row sm:items-center">
                <div>
                  <h2 class="text-base font-bold text-stone-900">
                    Dashboard de Pedidos, Histórico Anterior e Reclamações
                  </h2>
                  <p class="text-xs text-stone-500">
                    Gerencie pedidos ativos, consulte pedidos anteriores e responda reclamações de clientes.
                  </p>
                </div>
              </div>

              <!-- Abas de Filtro do Dashboard de Pedidos do Restaurante -->
              <div class="flex flex-wrap gap-2">
                <button
                  type="button"
                  class="rounded-lg px-3 py-1.5 text-xs font-semibold transition"
                  :class="
                    orderFilterTab === 'all'
                      ? 'bg-stone-900 text-white'
                      : 'border border-stone-200 bg-white text-stone-700 hover:bg-stone-50'
                  "
                  @click="orderFilterTab = 'all'"
                >
                  Todos ({{ restaurantOrders.length }})
                </button>
                <button
                  type="button"
                  class="rounded-lg px-3 py-1.5 text-xs font-semibold transition"
                  :class="
                    orderFilterTab === 'active'
                      ? 'bg-brand-600 text-white'
                      : 'border border-stone-200 bg-white text-stone-700 hover:bg-stone-50'
                  "
                  @click="orderFilterTab = 'active'"
                >
                  Em Andamento ({{ activeRestaurantOrders.length }})
                </button>
                <button
                  type="button"
                  class="rounded-lg px-3 py-1.5 text-xs font-semibold transition"
                  :class="
                    orderFilterTab === 'past'
                      ? 'bg-green-700 text-white'
                      : 'border border-stone-200 bg-white text-stone-700 hover:bg-stone-50'
                  "
                  @click="orderFilterTab = 'past'"
                >
                  Pedidos Anteriores ({{ pastRestaurantOrders.length }})
                </button>
                <button
                  type="button"
                  class="inline-flex items-center gap-1 rounded-lg px-3 py-1.5 text-xs font-semibold transition"
                  :class="
                    orderFilterTab === 'complaints'
                      ? 'bg-red-600 text-white'
                      : 'border border-red-200 bg-red-50 text-red-700 hover:bg-red-100'
                  "
                  @click="orderFilterTab = 'complaints'"
                >
                  <AlertCircle class="h-3.5 w-3.5" />
                  Reclamações ({{ complaintRestaurantOrders.length }})
                </button>
              </div>

              <div v-if="filteredRestaurantOrders.length > 0" class="space-y-3">
                <div
                  v-for="order in filteredRestaurantOrders"
                  :key="order.id"
                  class="rounded-xl border border-stone-200 bg-white p-4 shadow-sm space-y-3"
                >
                  <div class="flex flex-col justify-between gap-2 sm:flex-row sm:items-center">
                    <div>
                      <span class="font-bold text-stone-900">
                        Pedido #{{ order.id.slice(0, 8) }}
                      </span>
                      <span v-if="order.customerName" class="ml-2 text-xs font-semibold text-stone-700">
                        • Cliente: {{ order.customerName }}
                      </span>
                      <span class="ml-2 text-xs text-stone-500">
                        • {{ new Date(order.createdAt).toLocaleString('pt-BR') }}
                      </span>
                    </div>

                    <div class="flex items-center gap-2">
                      <span
                        class="rounded-full px-2.5 py-0.5 text-xs font-semibold"
                        :class="
                          order.status === 'Delivered'
                            ? 'bg-green-50 text-green-700'
                            : order.status === 'Cancelled'
                              ? 'bg-red-50 text-red-700'
                              : 'bg-amber-50 text-amber-800'
                        "
                      >
                        {{ formatOrderStatus(order.status) }}
                      </span>
                      <span class="font-bold text-stone-900">
                        {{ formatCurrency(order.totalAmount) }}
                      </span>
                    </div>
                  </div>

                  <ul v-if="order.items && order.items.length > 0" class="text-xs text-stone-600 space-y-1">
                    <li v-for="item in order.items" :key="item.id">
                      {{ item.quantity }}x {{ item.productName }} ({{ formatCurrency(item.totalPrice) }})
                    </li>
                  </ul>

                  <!-- Seção de Reclamação do Cliente e Resposta do Restaurante -->
                  <div
                    v-if="order.complaintMessage"
                    class="rounded-lg border border-red-200 bg-red-50 p-3.5 text-xs space-y-2"
                  >
                    <div class="flex items-center justify-between">
                      <div class="flex items-center gap-1.5 font-bold text-red-800">
                        <AlertCircle class="h-4 w-4" />
                        <span>Reclamação do Cliente ({{ order.customerName || 'Cliente' }}):</span>
                      </div>
                      <span
                        class="rounded-full px-2 py-0.5 text-[11px] font-semibold"
                        :class="
                          order.complaintReply
                            ? 'bg-green-100 text-green-800'
                            : 'bg-amber-100 text-amber-800'
                        "
                      >
                        {{ order.complaintReply ? 'Respondida' : 'Aguardando resposta' }}
                      </span>
                    </div>

                    <p class="text-stone-800">{{ order.complaintMessage }}</p>

                    <div
                      v-if="order.complaintReply"
                      class="rounded border border-green-200 bg-green-50 p-2.5 text-xs"
                    >
                      <div class="flex items-center gap-1.5 font-bold text-green-900">
                        <MessageSquareReply class="h-3.5 w-3.5" />
                        <span>Sua Resposta ao Cliente:</span>
                      </div>
                      <p class="mt-1 text-stone-800">{{ order.complaintReply }}</p>
                    </div>

                    <div class="pt-1">
                      <button
                        v-if="replyingOrderId !== order.id"
                        type="button"
                        class="inline-flex items-center gap-1 rounded-lg bg-stone-900 px-3 py-1.5 text-xs font-semibold text-white hover:bg-stone-800"
                        @click="
                          replyingOrderId = order.id;
                          complaintReplyText = order.complaintReply || '';
                        "
                      >
                        <MessageSquareReply class="h-3.5 w-3.5" />
                        {{ order.complaintReply ? 'Editar resposta' : 'Responder reclamação' }}
                      </button>

                      <form
                        v-else
                        class="mt-2 space-y-2"
                        @submit.prevent="handleReplyComplaint(order.id)"
                      >
                        <textarea
                          v-model="complaintReplyText"
                          rows="2"
                          required
                          placeholder="Escreva a resposta do restaurante para o cliente..."
                          class="w-full rounded-lg border border-stone-300 bg-white px-3 py-2 text-xs text-stone-900 focus:border-brand-600 focus:outline-none"
                        />
                        <div class="flex gap-2">
                          <button
                            type="submit"
                            :disabled="sendingComplaintReply"
                            class="rounded-lg bg-brand-600 px-3.5 py-1.5 text-xs font-semibold text-white hover:bg-brand-700 disabled:opacity-50"
                          >
                            {{ sendingComplaintReply ? 'Enviando...' : 'Enviar resposta ao cliente' }}
                          </button>
                          <button
                            type="button"
                            class="rounded-lg border border-stone-300 bg-white px-3 py-1.5 text-xs text-stone-600"
                            @click="replyingOrderId = null"
                          >
                            Cancelar
                          </button>
                        </div>
                      </form>
                    </div>
                  </div>

                  <div
                    v-if="order.status !== 'Delivered' && order.status !== 'Cancelled'"
                    class="flex flex-wrap items-center gap-2 border-t border-stone-100 pt-3"
                  >
                    <button
                      v-if="order.status === 'Created' || order.status === 'Paid'"
                      type="button"
                      :disabled="updatingOrder"
                      class="rounded-lg border border-stone-300 bg-white px-3 py-1.5 text-xs font-semibold text-stone-800 hover:bg-stone-50"
                      @click="handleQuickOrderStatus(order.id, 'Preparing')"
                    >
                      Iniciar preparo
                    </button>

                    <button
                      v-if="order.status === 'Created' || order.status === 'Paid' || order.status === 'Preparing'"
                      type="button"
                      :disabled="updatingOrder"
                      class="inline-flex items-center gap-1.5 rounded-lg bg-brand-600 px-3.5 py-1.5 text-xs font-semibold text-white hover:bg-brand-700"
                      @click="handleQuickOrderStatus(order.id, 'ReadyForPickup')"
                    >
                      <Bike class="h-3.5 w-3.5" />
                      Solicitar entrega (Chamar entregador)
                    </button>
                  </div>
                </div>
              </div>

              <div
                v-else
                class="rounded-xl border border-stone-200 bg-white p-6 text-center text-sm text-stone-500"
              >
                Nenhum pedido encontrado neste filtro.
              </div>
            </div>
          </section>
        </div>
      </template>
    </main>
  </div>
</template>
