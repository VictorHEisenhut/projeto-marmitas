<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import {
  Bike,
  CheckCircle2,
  DollarSign,
  Lock,
  MessageSquare,
  Star,
  Store,
  Trash2,
  UtensilsCrossed,
} from 'lucide-vue-next'
import Navbar from '@/components/Navbar.vue'
import { api, formatCurrency, formatPhone, getErrorMessage } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import type {
  Address,
  CourierEarnings,
  Delivery,
  Favorite,
  Product,
  Restaurant,
  ReviewItem,
  Subscription,
  User,
} from '@/types'

const auth = useAuthStore()

const isDriverOnly = computed(
  () =>
    auth.hasRole(['driver']) &&
    !auth.hasRole(['customer', 'restaurant_owner', 'admin', 'super_admin']),
)

const canViewCourierReviews = computed(() =>
  auth.hasRole(['driver', 'admin', 'super_admin']),
)

const name = ref(auth.user?.name || '')
const phone = ref(auth.user?.phone || '')
const savingProfile = ref(false)

// Estados de Cliente
const subscriptions = ref<Subscription[]>([])
const addresses = ref<Address[]>([])
const favorites = ref<Favorite[]>([])
const restaurants = ref<Restaurant[]>([])
const restaurantsMap = ref<Record<string, string>>({})
const loading = ref(true)

const street = ref('')
const number = ref('')
const complement = ref('')
const neighborhood = ref('')
const city = ref('')
const state = ref('')
const zipCode = ref('')
const isMain = ref(false)
const savingAddress = ref(false)

// Estados exclusivos da Conta do Entregador
const activeCourierTab = ref<'dashboard' | 'menus' | 'courier-reviews' | 'customer-reviews'>(
  'dashboard',
)
const courierEarnings = ref<CourierEarnings | null>(null)
const myDeliveries = ref<Delivery[]>([])
const selectedMenuRestaurantId = ref('')
const selectedRestaurantProducts = ref<Product[]>([])
const loadingMenuProducts = ref(false)

// Avaliações (Entregadores = restrito | Clientes = público para todos)
const courierReviews = ref<ReviewItem[]>([])
const customerReviews = ref<ReviewItem[]>([])

const newCourierReviewRestaurantId = ref('')
const newCourierReviewRating = ref(5)
const newCourierReviewComment = ref('')
const savingCourierReview = ref(false)

const newCustomerReviewRestaurantId = ref('')
const newCustomerReviewRating = ref(5)
const newCustomerReviewComment = ref('')
const savingCustomerReview = ref(false)

const error = ref('')
const successMessage = ref('')

const completedDeliveriesList = computed(() =>
  myDeliveries.value.filter((d) => d.status === 'Delivered'),
)

const activeDeliveriesList = computed(() =>
  myDeliveries.value.filter(
    (d) =>
      (d.courierId && d.courierId === auth.user?.id && d.status !== 'Delivered') ||
      d.status === 'DriverAssigned' ||
      d.status === 'InTransit',
  ),
)

function onPhoneInput(event: Event) {
  const target = event.target as HTMLInputElement
  phone.value = formatPhone(target.value)
}

function formatDeliveryStatus(status: string): string {
  const map: Record<string, string> = {
    Pending: 'Aguardando',
    Paid: 'Pago / Pronto',
    Preparing: 'Em preparo',
    ReadyForPickup: 'Pronto para coleta',
    DriverAssigned: 'Aceita pelo entregador',
    InTransit: 'Em rota de entrega',
    Delivered: 'Entregue (Concluída)',
    Cancelled: 'Cancelada',
  }
  return map[status] || status
}

async function loadRestaurantMenuPreview(restaurantId: string) {
  selectedMenuRestaurantId.value = restaurantId
  loadingMenuProducts.value = true
  try {
    const { data } = await api.get<Product[]>(`/restaurantes/${restaurantId}/cardapio`)
    selectedRestaurantProducts.value = data
  } catch {
    selectedRestaurantProducts.value = []
  } finally {
    loadingMenuProducts.value = false
  }
}

async function loadAccountData() {
  loading.value = true
  error.value = ''

  try {
    const [restRes, custRevRes] = await Promise.allSettled([
      api.get<Restaurant[]>('/restaurantes'),
      api.get<ReviewItem[]>('/avaliacoes/clientes'),
    ])

    if (restRes.status === 'fulfilled') {
      restaurants.value = restRes.value.data
      const map: Record<string, string> = {}
      for (const r of restRes.value.data) {
        map[r.id] = r.name
      }
      restaurantsMap.value = map
      const firstRest = restRes.value.data[0]
      if (firstRest) {
        if (!newCourierReviewRestaurantId.value) newCourierReviewRestaurantId.value = firstRest.id
        if (!newCustomerReviewRestaurantId.value) newCustomerReviewRestaurantId.value = firstRest.id
        if (isDriverOnly.value && !selectedMenuRestaurantId.value) {
          await loadRestaurantMenuPreview(firstRest.id)
        }
      }
    }

    if (custRevRes.status === 'fulfilled') {
      customerReviews.value = custRevRes.value.data
    }

    if (canViewCourierReviews.value) {
      const [earnRes, delivRes, courRevRes] = await Promise.allSettled([
        api.get<CourierEarnings>('/entregadores/me/ganhos'),
        api.get<Delivery[]>('/entregas'),
        api.get<ReviewItem[]>('/avaliacoes/entregadores'),
      ])

      if (earnRes.status === 'fulfilled') courierEarnings.value = earnRes.value.data
      if (delivRes.status === 'fulfilled') myDeliveries.value = delivRes.value.data
      if (courRevRes.status === 'fulfilled') courierReviews.value = courRevRes.value.data
    }

    if (!isDriverOnly.value) {
      const [subRes, addrRes, favRes] = await Promise.allSettled([
        api.get<Subscription[]>('/assinaturas'),
        api.get<Address[]>('/usuarios/me/enderecos'),
        api.get<Favorite[]>('/usuarios/me/favoritos'),
      ])

      if (subRes.status === 'fulfilled') subscriptions.value = subRes.value.data
      if (addrRes.status === 'fulfilled') addresses.value = addrRes.value.data
      if (favRes.status === 'fulfilled') favorites.value = favRes.value.data
    }
  } catch (err) {
    error.value = getErrorMessage(err, 'Erro ao carregar dados da conta.')
  } finally {
    loading.value = false
  }
}

async function handleSaveProfile() {
  error.value = ''
  successMessage.value = ''

  if (!name.value.trim()) {
    error.value = 'O nome é obrigatório.'
    return
  }

  savingProfile.value = true
  try {
    const { data } = await api.put<User>('/usuarios/me', {
      name: name.value.trim(),
      phone: phone.value.trim() || null,
    })
    auth.user = data
    successMessage.value = 'Dados atualizados com sucesso.'
  } catch (err) {
    error.value = getErrorMessage(err, 'Não foi possível atualizar seu perfil.')
  } finally {
    savingProfile.value = false
  }
}

async function handleSubmitCourierReview() {
  error.value = ''
  successMessage.value = ''

  if (!newCourierReviewComment.value.trim()) {
    error.value = 'Escreva seu comentário na avaliação de entregador.'
    return
  }

  savingCourierReview.value = true
  try {
    const { data } = await api.post<ReviewItem>('/avaliacoes/entregadores', {
      restaurantId: newCourierReviewRestaurantId.value || null,
      rating: newCourierReviewRating.value,
      comment: newCourierReviewComment.value.trim(),
    })
    courierReviews.value.unshift(data)
    newCourierReviewComment.value = ''
    newCourierReviewRating.value = 5
    successMessage.value = 'Avaliação publicada na aba exclusiva de entregadores!'
  } catch (err) {
    error.value = getErrorMessage(err, 'Não foi possível publicar a avaliação.')
  } finally {
    savingCourierReview.value = false
  }
}

async function handleSubmitCustomerReview() {
  error.value = ''
  successMessage.value = ''

  if (!newCustomerReviewComment.value.trim()) {
    error.value = 'Escreva seu comentário sobre a experiência.'
    return
  }

  savingCustomerReview.value = true
  try {
    const { data } = await api.post<ReviewItem>('/avaliacoes/clientes', {
      restaurantId: newCustomerReviewRestaurantId.value || null,
      rating: newCustomerReviewRating.value,
      comment: newCustomerReviewComment.value.trim(),
    })
    customerReviews.value.unshift(data)
    newCustomerReviewComment.value = ''
    newCustomerReviewRating.value = 5
    successMessage.value = 'Avaliação de cliente publicada para todos os usuários!'
  } catch (err) {
    error.value = getErrorMessage(err, 'Não foi possível enviar a avaliação.')
  } finally {
    savingCustomerReview.value = false
  }
}

async function handleSubscriptionAction(id: string, action: 'pausar' | 'reativar' | 'cancelar') {
  error.value = ''
  successMessage.value = ''

  try {
    await api.patch(`/assinaturas/${id}/${action}`)
    const { data } = await api.get<Subscription[]>('/assinaturas')
    subscriptions.value = data
    successMessage.value = 'Assinatura atualizada.'
  } catch (err) {
    error.value = getErrorMessage(err, 'Não foi possível atualizar a assinatura.')
  }
}

async function handleAddAddress() {
  error.value = ''
  successMessage.value = ''

  if (!street.value.trim() || !city.value.trim()) {
    error.value = 'Informe ao menos a rua e a cidade.'
    return
  }

  savingAddress.value = true
  try {
    await api.post('/usuarios/me/enderecos', {
      street: street.value.trim(),
      number: number.value.trim() || 'S/N',
      complement: complement.value.trim() || null,
      neighborhood: neighborhood.value.trim(),
      city: city.value.trim(),
      state: state.value.trim() || 'SP',
      zipCode: zipCode.value.trim(),
      isMain: isMain.value,
    })

    street.value = ''
    number.value = ''
    complement.value = ''
    neighborhood.value = ''
    city.value = ''
    state.value = ''
    zipCode.value = ''
    isMain.value = false

    const { data } = await api.get<Address[]>('/usuarios/me/enderecos')
    addresses.value = data
    successMessage.value = 'Endereço adicionado.'
  } catch (err) {
    error.value = getErrorMessage(err, 'Erro ao cadastrar endereço.')
  } finally {
    savingAddress.value = false
  }
}

async function handleSetMainAddress(id: string) {
  error.value = ''
  try {
    await api.patch(`/usuarios/me/enderecos/${id}/principal`)
    const { data } = await api.get<Address[]>('/usuarios/me/enderecos')
    addresses.value = data
  } catch (err) {
    error.value = getErrorMessage(err, 'Erro ao definir endereço principal.')
  }
}

async function handleDeleteAddress(id: string) {
  error.value = ''
  try {
    await api.delete(`/usuarios/me/enderecos/${id}`)
    addresses.value = addresses.value.filter((a) => a.id !== id)
  } catch (err) {
    error.value = getErrorMessage(err, 'Erro ao remover endereço.')
  }
}

async function handleRemoveFavorite(id: string) {
  error.value = ''
  try {
    await api.delete(`/usuarios/me/favoritos/${id}`)
    favorites.value = favorites.value.filter((f) => f.id !== id)
  } catch (err) {
    error.value = getErrorMessage(err, 'Erro ao remover favorito.')
  }
}

onMounted(() => {
  if (auth.user) {
    name.value = auth.user.name
    phone.value = auth.user.phone || ''
  }
  void loadAccountData()
})
</script>

<template>
  <div class="min-h-screen bg-stone-50">
    <Navbar />

    <main class="mx-auto max-w-5xl space-y-8 px-4 py-8 sm:px-6">
      <div>
        <h1 class="text-2xl font-bold text-stone-900">
          {{ isDriverOnly ? 'Minha Conta — Central do Entregador' : 'Minha Conta' }}
        </h1>
        <p class="mt-1 text-sm text-stone-600">
          {{
            isDriverOnly
              ? 'Acompanhe seu dashboard de entregas feitas, consulte os cardápios dos restaurantes e acesse as avaliações.'
              : 'Gerencie seus dados pessoais, assinaturas, endereços de entrega e avaliações.'
          }}
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
        class="rounded-lg border border-green-200 bg-green-50 p-4 text-sm text-green-800"
      >
        {{ successMessage }}
      </div>

      <section class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm">
        <h2 class="text-lg font-bold text-stone-900">Dados pessoais</h2>

        <form class="mt-4 grid gap-4 sm:grid-cols-2" @submit.prevent="handleSaveProfile">
          <div>
            <label for="profile-name" class="block text-sm font-medium text-stone-700">
              Nome completo
            </label>
            <input
              id="profile-name"
              v-model="name"
              type="text"
              required
              class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
            />
          </div>

          <div>
            <label for="profile-phone" class="block text-sm font-medium text-stone-700">
              Telefone
            </label>
            <input
              id="profile-phone"
              :value="phone"
              type="tel"
              placeholder="(11) 99999-9999"
              class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
              @input="onPhoneInput"
            />
          </div>

          <div class="sm:col-span-2">
            <button
              type="submit"
              :disabled="savingProfile"
              class="rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-50"
            >
              {{ savingProfile ? 'Salvando...' : 'Salvar alterações' }}
            </button>
          </div>
        </form>
      </section>

      <!-- ========================================================================= -->
      <!-- VISÃO EXCLUSIVA DA CONTA DO ENTREGADOR (Sem compras, planos ou endereços) -->
      <!-- ========================================================================= -->
      <template v-if="isDriverOnly">
        <!-- Abas de Navegação do Entregador -->
        <div class="flex flex-wrap gap-2 border-b border-stone-200 pb-3">
          <button
            type="button"
            class="inline-flex items-center gap-2 rounded-lg px-4 py-2 text-xs font-semibold transition"
            :class="
              activeCourierTab === 'dashboard'
                ? 'bg-brand-600 text-white'
                : 'border border-stone-200 bg-white text-stone-700 hover:bg-stone-50'
            "
            @click="activeCourierTab = 'dashboard'"
          >
            <Bike class="h-4 w-4" />
            Dashboard de Entregas Feitas
          </button>

          <button
            type="button"
            class="inline-flex items-center gap-2 rounded-lg px-4 py-2 text-xs font-semibold transition"
            :class="
              activeCourierTab === 'menus'
                ? 'bg-brand-600 text-white'
                : 'border border-stone-200 bg-white text-stone-700 hover:bg-stone-50'
            "
            @click="activeCourierTab = 'menus'"
          >
            <UtensilsCrossed class="h-4 w-4" />
            Cardápios dos Restaurantes
          </button>

          <button
            type="button"
            class="inline-flex items-center gap-2 rounded-lg px-4 py-2 text-xs font-semibold transition"
            :class="
              activeCourierTab === 'courier-reviews'
                ? 'bg-brand-600 text-white'
                : 'border border-stone-200 bg-white text-stone-700 hover:bg-stone-50'
            "
            @click="activeCourierTab = 'courier-reviews'"
          >
            <Lock class="h-3.5 w-3.5" />
            Avaliações de Entregadores (Exclusivo)
          </button>

          <button
            type="button"
            class="inline-flex items-center gap-2 rounded-lg px-4 py-2 text-xs font-semibold transition"
            :class="
              activeCourierTab === 'customer-reviews'
                ? 'bg-brand-600 text-white'
                : 'border border-stone-200 bg-white text-stone-700 hover:bg-stone-50'
            "
            @click="activeCourierTab = 'customer-reviews'"
          >
            <Star class="h-4 w-4" />
            Avaliações de Clientes (Público)
          </button>
        </div>

        <!-- ABA 1: Dashboard de Entregas Feitas -->
        <section v-if="activeCourierTab === 'dashboard'" class="space-y-6">
          <div class="grid gap-4 sm:grid-cols-3">
            <div class="rounded-xl border border-stone-200 bg-white p-5 shadow-sm">
              <div class="flex items-center gap-2 text-stone-500">
                <CheckCircle2 class="h-5 w-5 text-green-600" />
                <span class="text-xs font-semibold uppercase">Entregas Concluídas</span>
              </div>
              <p class="mt-2 text-3xl font-bold text-stone-900">
                {{ courierEarnings?.completedDeliveries ?? completedDeliveriesList.length }}
              </p>
            </div>

            <div class="rounded-xl border border-stone-200 bg-white p-5 shadow-sm">
              <div class="flex items-center gap-2 text-stone-500">
                <Bike class="h-5 w-5 text-brand-600" />
                <span class="text-xs font-semibold uppercase">Entregas em Rota / Aceitas</span>
              </div>
              <p class="mt-2 text-3xl font-bold text-stone-900">
                {{ activeDeliveriesList.length }}
              </p>
            </div>

            <div class="rounded-xl border border-stone-200 bg-white p-5 shadow-sm">
              <div class="flex items-center gap-2 text-stone-500">
                <DollarSign class="h-5 w-5 text-green-600" />
                <span class="text-xs font-semibold uppercase">Ganhos Totais</span>
              </div>
              <p class="mt-2 text-3xl font-bold text-green-700">
                {{ formatCurrency(courierEarnings?.totalEarnings ?? completedDeliveriesList.length * 8.5) }}
              </p>
            </div>
          </div>

          <div class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm space-y-4">
            <div class="flex items-center justify-between">
              <div>
                <h2 class="text-base font-bold text-stone-900">
                  Histórico e Dashboard de Entregas
                </h2>
                <p class="text-xs text-stone-500">
                  Relatório das entregas concluídas e em andamento com restaurante de coleta e bairro de destino.
                </p>
              </div>
              <RouterLink
                to="/entregador"
                class="rounded-lg bg-brand-600 px-3.5 py-2 text-xs font-semibold text-white hover:bg-brand-700"
              >
                Ir para Fila de Coletas
              </RouterLink>
            </div>

            <div v-if="myDeliveries.length > 0" class="space-y-3">
              <div
                v-for="deliv in myDeliveries"
                :key="deliv.id"
                class="flex flex-col justify-between gap-3 rounded-lg border border-stone-200 bg-stone-50 p-4 sm:flex-row sm:items-center"
              >
                <div class="space-y-1">
                  <div class="flex items-center gap-2">
                    <span class="font-bold text-stone-900">
                      Pedido #{{ deliv.orderId.slice(0, 8) }}
                    </span>
                    <span
                      class="rounded-full px-2.5 py-0.5 text-xs font-semibold"
                      :class="
                        deliv.status === 'Delivered'
                          ? 'bg-green-100 text-green-800'
                          : 'bg-amber-100 text-amber-800'
                      "
                    >
                      {{ formatDeliveryStatus(deliv.status) }}
                    </span>
                  </div>
                  <p class="text-xs text-stone-700">
                    <strong>Restaurante (Coleta):</strong>
                    {{ deliv.restaurantName || 'Restaurante Parceiro' }}
                    <span v-if="deliv.restaurantAddress" class="text-stone-500">
                      ({{ deliv.restaurantAddress }})
                    </span>
                  </p>
                  <p class="text-xs text-stone-700">
                    <strong>Região da Entrega:</strong>
                    Bairro {{ deliv.deliveryNeighborhood || 'Centro' }}
                    <span v-if="deliv.deliveryAddress" class="text-stone-500">
                      — {{ deliv.deliveryAddress }}
                    </span>
                  </p>
                </div>

                <div class="text-right">
                  <span class="text-xs text-stone-500">Repasse da entrega</span>
                  <p class="text-sm font-bold text-green-700">
                    {{ deliv.status === 'Delivered' ? 'R$ 8,50 (Pago)' : 'R$ 8,50 (Previsto)' }}
                  </p>
                </div>
              </div>
            </div>

            <div
              v-else
              class="rounded-lg border border-stone-200 bg-stone-50 p-6 text-center text-sm text-stone-500"
            >
              Nenhuma entrega registrada ainda. Complete seu cadastro na aba Entregas para visualizar e aceitar corridas!
            </div>
          </div>
        </section>

        <!-- ABA 2: Cardápios dos Restaurantes (Consulta pelo Entregador, sem compra) -->
        <section v-else-if="activeCourierTab === 'menus'" class="space-y-6">
          <div class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm space-y-4">
            <div>
              <h2 class="text-base font-bold text-stone-900">
                Cardápios dos Restaurantes Parceiros (Modo Consulta)
              </h2>
              <p class="text-xs text-stone-500">
                Selecione um restaurante para visualizar os pratos e itens preparados por cada estabelecimento.
              </p>
            </div>

            <div class="flex flex-wrap gap-2">
              <button
                v-for="r in restaurants"
                :key="r.id"
                type="button"
                class="inline-flex items-center gap-2 rounded-lg border px-3.5 py-2 text-xs font-semibold transition"
                :class="
                  selectedMenuRestaurantId === r.id
                    ? 'border-brand-600 bg-brand-600 text-white'
                    : 'border-stone-200 bg-stone-50 text-stone-700 hover:bg-stone-100'
                "
                @click="loadRestaurantMenuPreview(r.id)"
              >
                <Store class="h-3.5 w-3.5" />
                {{ r.name }}
              </button>
            </div>

            <p v-if="loadingMenuProducts" class="text-xs text-stone-500">
              Carregando cardápio do restaurante...
            </p>

            <div
              v-else-if="selectedRestaurantProducts.length > 0"
              class="grid gap-3 sm:grid-cols-2"
            >
              <div
                v-for="prod in selectedRestaurantProducts"
                :key="prod.id"
                class="rounded-lg border border-stone-200 bg-stone-50 p-4 space-y-1"
              >
                <div class="flex items-center justify-between gap-2">
                  <span class="rounded bg-stone-200 px-2 py-0.5 text-[10px] font-semibold text-stone-700">
                    {{ prod.categoryName || 'Marmita' }}
                  </span>
                  <span class="text-xs font-bold text-stone-900">
                    {{ formatCurrency(prod.price) }}
                  </span>
                </div>
                <h3 class="font-bold text-stone-900 text-sm pt-1">{{ prod.name }}</h3>
                <p class="text-xs text-stone-600">{{ prod.description }}</p>
              </div>
            </div>

            <p v-else class="text-xs text-stone-500">
              Nenhum item encontrado neste cardápio.
            </p>
          </div>
        </section>

        <!-- ABA 3: Avaliações de Entregadores (Visível SOMENTE para Entregadores) -->
        <section v-else-if="activeCourierTab === 'courier-reviews'" class="space-y-6">
          <div class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm space-y-4">
            <div class="flex items-center justify-between">
              <div>
                <div class="flex items-center gap-2">
                  <Lock class="h-4 w-4 text-brand-600" />
                  <h2 class="text-base font-bold text-stone-900">
                    Mural de Avaliações de Entregadores (Visível Somente para Entregadores)
                  </h2>
                </div>
                <p class="mt-0.5 text-xs text-stone-500">
                  Espaço exclusivo para entregadores avaliarem tempo de coleta, embalagem e apoio dos restaurantes.
                </p>
              </div>
              <span class="rounded-full bg-amber-100 px-3 py-1 text-xs font-semibold text-amber-800">
                Restrito a Entregadores
              </span>
            </div>

            <form
              class="rounded-lg border border-stone-200 bg-stone-50 p-4 space-y-3"
              @submit.prevent="handleSubmitCourierReview"
            >
              <h3 class="text-xs font-bold uppercase text-stone-700">
                Registrar nova avaliação de entregador
              </h3>
              <div class="grid gap-3 sm:grid-cols-2">
                <div>
                  <label class="block text-xs font-medium text-stone-700">Restaurante avaliado</label>
                  <select
                    v-model="newCourierReviewRestaurantId"
                    class="mt-1 w-full rounded-lg border border-stone-300 bg-white px-3 py-2 text-sm"
                  >
                    <option v-for="r in restaurants" :key="r.id" :value="r.id">
                      {{ r.name }}
                    </option>
                  </select>
                </div>

                <div>
                  <label class="block text-xs font-medium text-stone-700">Nota da Coleta (1 a 5)</label>
                  <select
                    v-model.number="newCourierReviewRating"
                    class="mt-1 w-full rounded-lg border border-stone-300 bg-white px-3 py-2 text-sm"
                  >
                    <option :value="5">⭐⭐⭐⭐⭐ (5 - Coleta rápida e organizada)</option>
                    <option :value="4">⭐⭐⭐⭐ (4 - Boa coleta)</option>
                    <option :value="3">⭐⭐⭐ (3 - Tempo médio de espera)</option>
                    <option :value="2">⭐⭐ (2 - Demora na liberação)</option>
                    <option :value="1">⭐ (1 - Muito demorado)</option>
                  </select>
                </div>
              </div>

              <div>
                <label class="block text-xs font-medium text-stone-700">
                  Relato para outros entregadores
                </label>
                <textarea
                  v-model="newCourierReviewComment"
                  rows="2"
                  required
                  placeholder="Conte como é a retirada, lacre das marmitas e local de parada..."
                  class="mt-1 w-full rounded-lg border border-stone-300 bg-white px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
                />
              </div>

              <button
                type="submit"
                :disabled="savingCourierReview"
                class="rounded-lg bg-brand-600 px-4 py-2 text-xs font-semibold text-white hover:bg-brand-700 disabled:opacity-50"
              >
                {{ savingCourierReview ? 'Publicando...' : 'Publicar avaliação de entregador' }}
              </button>
            </form>

            <div class="space-y-3">
              <div
                v-for="rev in courierReviews"
                :key="rev.id"
                class="rounded-lg border border-stone-200 bg-stone-50 p-4 space-y-1.5"
              >
                <div class="flex items-center justify-between">
                  <div class="flex items-center gap-2">
                    <span class="font-bold text-sm text-stone-900">
                      {{ rev.authorName || 'Entregador Parceiro' }}
                    </span>
                    <span class="rounded bg-stone-200 px-2 py-0.5 text-[11px] font-medium text-stone-700">
                      {{ rev.restaurantName || 'Coleta' }}
                    </span>
                  </div>
                  <span class="text-xs font-bold text-amber-600">
                    {{ '★'.repeat(rev.rating) }}{{ '☆'.repeat(5 - rev.rating) }} ({{ rev.rating }}/5)
                  </span>
                </div>
                <p class="text-sm text-stone-700">{{ rev.comment }}</p>
              </div>
            </div>
          </div>
        </section>

        <!-- ABA 4: Avaliações de Clientes (Visível para TODOS os usuários) -->
        <section v-else-if="activeCourierTab === 'customer-reviews'" class="space-y-6">
          <div class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm space-y-4">
            <div class="flex items-center justify-between">
              <div>
                <h2 class="text-base font-bold text-stone-900">
                  Avaliações de Clientes (Visível para Todos os Usuários)
                </h2>
                <p class="text-xs text-stone-500">
                  Opiniões públicas registradas pelos clientes sobre as marmitas e entregas.
                </p>
              </div>
              <span class="rounded-full bg-green-100 px-3 py-1 text-xs font-semibold text-green-800">
                Público para Todos
              </span>
            </div>

            <div class="space-y-3">
              <div
                v-for="rev in customerReviews"
                :key="rev.id"
                class="rounded-lg border border-stone-200 bg-stone-50 p-4 space-y-1.5"
              >
                <div class="flex items-center justify-between">
                  <div class="flex items-center gap-2">
                    <span class="font-bold text-sm text-stone-900">
                      {{ rev.authorName || 'Cliente Verificado' }}
                    </span>
                    <span class="rounded bg-orange-100 px-2 py-0.5 text-[11px] font-semibold text-brand-700">
                      {{ rev.restaurantName || 'Restaurante Parceiro' }}
                    </span>
                  </div>
                  <span class="text-xs font-bold text-amber-600">
                    {{ '★'.repeat(rev.rating) }}{{ '☆'.repeat(5 - rev.rating) }} ({{ rev.rating }}/5)
                  </span>
                </div>
                <p class="text-sm text-stone-700">{{ rev.comment }}</p>
              </div>
            </div>
          </div>
        </section>
      </template>

      <!-- ========================================================================= -->
      <!-- VISÃO DE CLIENTE / OUTROS USUÁRIOS                                        -->
      <!-- ========================================================================= -->
      <template v-else>
        <section class="space-y-4">
          <h2 class="text-lg font-bold text-stone-900">Minhas assinaturas</h2>

          <p v-if="loading" class="text-sm text-stone-500">Carregando assinaturas...</p>

          <div v-else-if="subscriptions.length > 0" class="grid gap-4 sm:grid-cols-2">
            <div
              v-for="sub in subscriptions"
              :key="sub.id"
              class="flex flex-col justify-between rounded-xl border border-stone-200 bg-white p-5 shadow-sm"
            >
              <div>
                <div class="flex items-center justify-between">
                  <h3 class="font-bold text-stone-900">{{ sub.planName }}</h3>
                  <span
                    class="rounded-full px-2.5 py-0.5 text-xs font-semibold"
                    :class="
                      sub.status === 'Active'
                        ? 'bg-green-50 text-green-700'
                        : sub.status === 'Paused'
                          ? 'bg-amber-50 text-amber-800'
                          : 'bg-stone-100 text-stone-600'
                    "
                  >
                    {{
                      sub.status === 'Active'
                        ? 'Ativa'
                        : sub.status === 'Paused'
                          ? 'Pausada'
                          : 'Cancelada'
                    }}
                  </span>
                </div>

                <p class="mt-2 text-lg font-bold text-stone-900">
                  {{ formatCurrency(sub.monthlyPrice) }}
                  <span class="text-xs font-normal text-stone-500">/mês</span>
                </p>
                <p class="mt-1 text-xs text-stone-500">
                  Próxima cobrança: {{ new Date(sub.nextBillingDate).toLocaleDateString('pt-BR') }}
                </p>
              </div>

              <div class="mt-4 flex flex-wrap gap-2 border-t border-stone-100 pt-3">
                <button
                  v-if="sub.status === 'Active'"
                  type="button"
                  class="rounded-lg border border-stone-300 px-3 py-1.5 text-xs font-medium text-stone-700 hover:bg-stone-50"
                  @click="handleSubscriptionAction(sub.id, 'pausar')"
                >
                  Pausar
                </button>
                <button
                  v-if="sub.status === 'Paused'"
                  type="button"
                  class="rounded-lg bg-green-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-green-700"
                  @click="handleSubscriptionAction(sub.id, 'reativar')"
                >
                  Reativar
                </button>
                <button
                  v-if="sub.status !== 'Cancelled'"
                  type="button"
                  class="rounded-lg border border-red-200 px-3 py-1.5 text-xs font-medium text-red-600 hover:bg-red-50"
                  @click="handleSubscriptionAction(sub.id, 'cancelar')"
                >
                  Cancelar
                </button>
              </div>
            </div>
          </div>

          <div
            v-else
            class="rounded-xl border border-stone-200 bg-white p-6 text-center text-sm text-stone-500"
          >
            Você ainda não possui assinaturas ativas.
          </div>
        </section>

        <section class="space-y-4">
          <h2 class="text-lg font-bold text-stone-900">Endereços de entrega</h2>

          <div v-if="addresses.length > 0" class="grid gap-4 sm:grid-cols-2">
            <div
              v-for="addr in addresses"
              :key="addr.id"
              class="flex flex-col justify-between rounded-xl border border-stone-200 bg-white p-5 shadow-sm"
            >
              <div>
                <div class="flex items-center justify-between">
                  <p class="font-semibold text-stone-900">
                    {{ addr.street }}, {{ addr.number }}
                  </p>
                  <span
                    v-if="addr.isMain"
                    class="rounded bg-orange-100 px-2 py-0.5 text-xs font-semibold text-brand-700"
                  >
                    Principal
                  </span>
                </div>
                <p v-if="addr.complement" class="text-xs text-stone-500">{{ addr.complement }}</p>
                <p class="mt-1 text-sm text-stone-600">
                  {{ addr.neighborhood }} — {{ addr.city }}/{{ addr.state }}
                </p>
                <p class="text-xs text-stone-400">CEP: {{ addr.zipCode }}</p>
              </div>

              <div class="mt-4 flex items-center justify-between border-t border-stone-100 pt-3">
                <button
                  v-if="!addr.isMain"
                  type="button"
                  class="text-xs font-medium text-brand-700 hover:underline"
                  @click="handleSetMainAddress(addr.id)"
                >
                  Definir como principal
                </button>
                <span v-else />

                <button
                  type="button"
                  class="text-stone-400 hover:text-red-600"
                  @click="handleDeleteAddress(addr.id)"
                >
                  <Trash2 class="h-4 w-4" />
                </button>
              </div>
            </div>
          </div>

          <form
            class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm"
            @submit.prevent="handleAddAddress"
          >
            <h3 class="text-sm font-bold text-stone-900">Adicionar novo endereço</h3>

            <div class="mt-4 grid gap-3 sm:grid-cols-3">
              <div class="sm:col-span-2">
                <label class="block text-xs font-medium text-stone-600">Rua / Logradouro</label>
                <input
                  v-model="street"
                  type="text"
                  required
                  class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
                />
              </div>

              <div>
                <label class="block text-xs font-medium text-stone-600">Número</label>
                <input
                  v-model="number"
                  type="text"
                  required
                  class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
                />
              </div>

              <div>
                <label class="block text-xs font-medium text-stone-600">Complemento</label>
                <input
                  v-model="complement"
                  type="text"
                  class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
                />
              </div>

              <div>
                <label class="block text-xs font-medium text-stone-600">Bairro</label>
                <input
                  v-model="neighborhood"
                  type="text"
                  class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
                />
              </div>

              <div>
                <label class="block text-xs font-medium text-stone-600">CEP</label>
                <input
                  v-model="zipCode"
                  type="text"
                  placeholder="00000-000"
                  class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
                />
              </div>

              <div class="sm:col-span-2">
                <label class="block text-xs font-medium text-stone-600">Cidade</label>
                <input
                  v-model="city"
                  type="text"
                  required
                  class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
                />
              </div>

              <div>
                <label class="block text-xs font-medium text-stone-600">UF</label>
                <input
                  v-model="state"
                  type="text"
                  maxlength="2"
                  placeholder="SP"
                  class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm uppercase focus:border-brand-600 focus:outline-none"
                />
              </div>
            </div>

            <div class="mt-4 flex items-center justify-between">
              <label class="inline-flex items-center gap-2 text-sm text-stone-700">
                <input v-model="isMain" type="checkbox" class="rounded border-stone-300" />
                Definir como endereço principal
              </label>

              <button
                type="submit"
                :disabled="savingAddress"
                class="rounded-lg bg-stone-900 px-4 py-2 text-sm font-semibold text-white hover:bg-stone-800 disabled:opacity-50"
              >
                {{ savingAddress ? 'Adicionando...' : 'Adicionar endereço' }}
              </button>
            </div>
          </form>
        </section>

        <section v-if="favorites.length > 0" class="space-y-4">
          <h2 class="text-lg font-bold text-stone-900">Restaurantes favoritos</h2>

          <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            <div
              v-for="fav in favorites"
              :key="fav.id"
              class="flex items-center justify-between rounded-xl border border-stone-200 bg-white p-4 shadow-sm"
            >
              <RouterLink
                v-if="fav.restaurantId"
                :to="`/restaurantes/${fav.restaurantId}`"
                class="font-medium text-stone-900 hover:text-brand-600"
              >
                {{ restaurantsMap[fav.restaurantId] || 'Ver restaurante' }}
              </RouterLink>
              <span v-else class="text-sm text-stone-700">Item favorito</span>

              <button
                type="button"
                class="text-stone-400 hover:text-red-600"
                @click="handleRemoveFavorite(fav.id)"
              >
                <Trash2 class="h-4 w-4" />
              </button>
            </div>
          </div>
        </section>

        <!-- Área de Avaliação de Clientes (Visível para TODOS os usuários) -->
        <section class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm space-y-4">
          <div class="flex flex-col justify-between gap-2 sm:flex-row sm:items-center">
            <div>
              <div class="flex items-center gap-2">
                <MessageSquare class="h-5 w-5 text-brand-600" />
                <h2 class="text-lg font-bold text-stone-900">
                  Avaliações de Clientes (Mural Público)
                </h2>
              </div>
              <p class="text-xs text-stone-500">
                Esta área de avaliações aparece para todos os usuários da plataforma.
              </p>
            </div>
            <span class="self-start rounded-full bg-green-100 px-3 py-1 text-xs font-semibold text-green-800">
              Visível para Todos os Usuários
            </span>
          </div>

          <form
            class="rounded-lg border border-stone-200 bg-stone-50 p-4 space-y-3"
            @submit.prevent="handleSubmitCustomerReview"
          >
            <h3 class="text-xs font-bold uppercase text-stone-700">
              Deixar uma avaliação pública de cliente
            </h3>
            <div class="grid gap-3 sm:grid-cols-2">
              <div>
                <label class="block text-xs font-medium text-stone-700">Restaurante</label>
                <select
                  v-model="newCustomerReviewRestaurantId"
                  class="mt-1 w-full rounded-lg border border-stone-300 bg-white px-3 py-2 text-sm"
                >
                  <option v-for="r in restaurants" :key="r.id" :value="r.id">
                    {{ r.name }}
                  </option>
                </select>
              </div>
              <div>
                <label class="block text-xs font-medium text-stone-700">Nota (1 a 5)</label>
                <select
                  v-model.number="newCustomerReviewRating"
                  class="mt-1 w-full rounded-lg border border-stone-300 bg-white px-3 py-2 text-sm"
                >
                  <option :value="5">⭐⭐⭐⭐⭐ (5 - Excelente)</option>
                  <option :value="4">⭐⭐⭐⭐ (4 - Muito bom)</option>
                  <option :value="3">⭐⭐⭐ (3 - Regular)</option>
                  <option :value="2">⭐⭐ (2 - Ruim)</option>
                  <option :value="1">⭐ (1 - Péssimo)</option>
                </select>
              </div>
            </div>
            <div>
              <input
                v-model="newCustomerReviewComment"
                type="text"
                required
                placeholder="Compartilhe sua experiência com a marmita e entrega..."
                class="w-full rounded-lg border border-stone-300 bg-white px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
              />
            </div>
            <button
              type="submit"
              :disabled="savingCustomerReview"
              class="rounded-lg bg-brand-600 px-4 py-2 text-xs font-semibold text-white hover:bg-brand-700 disabled:opacity-50"
            >
              {{ savingCustomerReview ? 'Enviando...' : 'Publicar avaliação de cliente' }}
            </button>
          </form>

          <div class="grid gap-3 sm:grid-cols-2">
            <div
              v-for="rev in customerReviews"
              :key="rev.id"
              class="rounded-lg border border-stone-200 bg-stone-50 p-4 space-y-1.5"
            >
              <div class="flex items-center justify-between">
                <span class="font-bold text-sm text-stone-900">
                  {{ rev.authorName || 'Cliente Verificado' }}
                </span>
                <span class="text-xs font-bold text-amber-600">
                  {{ '★'.repeat(rev.rating) }}{{ '☆'.repeat(5 - rev.rating) }}
                </span>
              </div>
              <p class="text-xs font-semibold text-brand-700">
                {{ rev.restaurantName || 'Restaurante Parceiro' }}
              </p>
              <p class="text-sm text-stone-700">{{ rev.comment }}</p>
            </div>
          </div>
        </section>
      </template>
    </main>
  </div>
</template>
