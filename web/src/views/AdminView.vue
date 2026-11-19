<script setup lang="ts">
import { onMounted, ref } from 'vue'
import {
  CheckCircle2,
  DollarSign,
  Plus,
  RefreshCw,
  Shield,
  ShoppingBag,
  Store,
  Tag,
  Trash2,
  Truck,
  Users,
  XCircle,
} from 'lucide-vue-next'
import Navbar from '@/components/Navbar.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import type {
  AdminDashboard,
  AuditLog,
  Category,
  CommissionSummary,
  Coupon,
  Courier,
  DeliveriesReport,
  Payout,
  Plan,
  Restaurant,
  SubscriptionsReport,
  User,
} from '@/types'

type AdminTab = 'restaurantes' | 'planos' | 'usuarios' | 'financeiro'

const activeTab = ref<AdminTab>('restaurantes')
const loading = ref(true)
const errorMessage = ref('')
const successMessage = ref('')

const stats = ref<AdminDashboard | null>(null)
const restaurants = ref<Restaurant[]>([])
const categories = ref<Category[]>([])
const plans = ref<Plan[]>([])
const coupons = ref<Coupon[]>([])
const users = ref<User[]>([])
const couriers = ref<Courier[]>([])
const payouts = ref<Payout[]>([])
const commissions = ref<CommissionSummary | null>(null)
const subscriptionsReport = ref<SubscriptionsReport | null>(null)
const deliveriesReport = ref<DeliveriesReport | null>(null)
const auditLogs = ref<AuditLog[]>([])

const restName = ref('')
const restDocument = ref('')
const restDescription = ref('')
const restOwnerId = ref('')
const restZipCode = ref('')
const restStreet = ref('')
const restNumber = ref('')
const restNeighborhood = ref('')
const restCity = ref('São Paulo')
const restState = ref('SP')
const savingRestaurant = ref(false)

const catName = ref('')
const catDisplayOrder = ref(6)
const savingCategory = ref(false)

const planName = ref('')
const planDescription = ref('')
const planMealsPerWeek = ref(5)
const planMonthlyPrice = ref<number | null>(null)
const savingPlan = ref(false)

const couponCode = ref('')
const couponDiscount = ref(10)
const couponExpiresAt = ref('')
const savingCoupon = ref(false)

const userRoleSelections = ref<Record<string, string>>({})

async function loadAllData() {
  loading.value = true
  errorMessage.value = ''

  try {
    const results = await Promise.allSettled([
      api.get<AdminDashboard>('/admin/dashboard'),
      api.get<Restaurant[]>('/restaurantes'),
      api.get<Category[]>('/categorias'),
      api.get<Plan[]>('/planos'),
      api.get<Coupon[]>('/cupons'),
      api.get<User[]>('/usuarios'),
      api.get<Courier[]>('/entregadores'),
      api.get<Payout[]>('/admin/financeiro/repasses'),
      api.get<CommissionSummary>('/admin/financeiro/comissoes'),
      api.get<SubscriptionsReport>('/admin/relatorios/assinaturas'),
      api.get<DeliveriesReport>('/admin/relatorios/entregas'),
      api.get<AuditLog[]>('/admin/auditoria'),
    ])

    if (results[0].status === 'fulfilled') stats.value = results[0].value.data
    if (results[1].status === 'fulfilled') restaurants.value = results[1].value.data
    if (results[2].status === 'fulfilled') categories.value = results[2].value.data
    if (results[3].status === 'fulfilled') plans.value = results[3].value.data
    if (results[4].status === 'fulfilled') coupons.value = results[4].value.data
    if (results[5].status === 'fulfilled') {
      users.value = results[5].value.data
      const map: Record<string, string> = {}
      for (const u of users.value) {
        map[u.id] = u.roles[0] || 'customer'
      }
      userRoleSelections.value = map
      if (!restOwnerId.value && users.value[0]) {
        restOwnerId.value = users.value[0].id
      }
    }
    if (results[6].status === 'fulfilled') couriers.value = results[6].value.data
    if (results[7].status === 'fulfilled') payouts.value = results[7].value.data
    if (results[8].status === 'fulfilled') commissions.value = results[8].value.data
    if (results[9].status === 'fulfilled') subscriptionsReport.value = results[9].value.data
    if (results[10].status === 'fulfilled') deliveriesReport.value = results[10].value.data
    if (results[11].status === 'fulfilled') auditLogs.value = results[11].value.data
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Erro ao carregar dados do painel administrativo.')
  } finally {
    loading.value = false
  }
}

async function handleCreateRestaurant() {
  errorMessage.value = ''
  successMessage.value = ''

  if (!restName.value.trim() || !restDocument.value.trim()) {
    errorMessage.value = 'Informe o nome e o CNPJ do restaurante.'
    return
  }

  savingRestaurant.value = true
  try {
    await api.post('/restaurantes', {
      name: restName.value.trim(),
      document: restDocument.value.trim(),
      description: restDescription.value.trim(),
      ownerId: restOwnerId.value || undefined,
      addressZipCode: restZipCode.value.trim() || '01001-000',
      addressStreet: restStreet.value.trim() || 'Av. Paulista',
      addressNumber: restNumber.value.trim() || '1000',
      addressNeighborhood: restNeighborhood.value.trim() || 'Bela Vista',
      addressCity: restCity.value.trim() || 'São Paulo',
      addressState: restState.value.trim().toUpperCase() || 'SP',
    })

    restName.value = ''
    restDocument.value = ''
    restDescription.value = ''
    restZipCode.value = ''
    restStreet.value = ''
    restNumber.value = ''
    restNeighborhood.value = ''
    successMessage.value = 'Restaurante cadastrado e aprovado com sucesso.'
    await loadAllData()
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Não foi possível cadastrar o restaurante.')
  } finally {
    savingRestaurant.value = false
  }
}

async function handleChangeRestaurantStatus(id: string, status: string) {
  errorMessage.value = ''
  successMessage.value = ''
  try {
    await api.patch(`/restaurantes/${id}/status`, { status })
    successMessage.value = 'Status do restaurante atualizado.'
    await loadAllData()
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Não foi possível atualizar o restaurante.')
  }
}

async function handleDeleteRestaurant(id: string) {
  errorMessage.value = ''
  successMessage.value = ''
  try {
    await api.delete(`/restaurantes/${id}`)
    restaurants.value = restaurants.value.filter((r) => r.id !== id)
    successMessage.value = 'Restaurante removido.'
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Não foi possível remover o restaurante.')
  }
}

async function handleCreateCategory() {
  errorMessage.value = ''
  successMessage.value = ''
  if (!catName.value.trim()) {
    errorMessage.value = 'Informe o nome da categoria.'
    return
  }

  savingCategory.value = true
  try {
    const { data } = await api.post<Category>('/categorias', {
      name: catName.value.trim(),
      displayOrder: catDisplayOrder.value || 1,
    })
    categories.value.push(data)
    catName.value = ''
    successMessage.value = 'Categoria criada com sucesso.'
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Não foi possível criar a categoria.')
  } finally {
    savingCategory.value = false
  }
}

async function handleDeleteCategory(id: string) {
  errorMessage.value = ''
  successMessage.value = ''
  try {
    await api.delete(`/categorias/${id}`)
    categories.value = categories.value.filter((c) => c.id !== id)
    successMessage.value = 'Categoria removida.'
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Não foi possível remover a categoria.')
  }
}

async function handleCreatePlan() {
  errorMessage.value = ''
  successMessage.value = ''
  if (!planName.value.trim() || !planMonthlyPrice.value || planMonthlyPrice.value <= 0) {
    errorMessage.value = 'Informe nome e valor mensal válido para o plano.'
    return
  }

  savingPlan.value = true
  try {
    const { data } = await api.post<Plan>('/planos', {
      name: planName.value.trim(),
      description: planDescription.value.trim(),
      mealsPerWeek: planMealsPerWeek.value,
      monthlyPrice: planMonthlyPrice.value,
    })
    plans.value.push(data)
    planName.value = ''
    planDescription.value = ''
    planMonthlyPrice.value = null
    successMessage.value = 'Plano de assinatura criado com sucesso.'
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Não foi possível criar o plano.')
  } finally {
    savingPlan.value = false
  }
}

async function handleDeletePlan(id: string) {
  errorMessage.value = ''
  successMessage.value = ''
  try {
    await api.delete(`/planos/${id}`)
    plans.value = plans.value.filter((p) => p.id !== id)
    successMessage.value = 'Plano desativado com sucesso.'
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Não foi possível desativar o plano.')
  }
}

async function handleCreateCoupon() {
  errorMessage.value = ''
  successMessage.value = ''
  if (!couponCode.value.trim() || couponDiscount.value <= 0) {
    errorMessage.value = 'Informe o código e o percentual de desconto do cupom.'
    return
  }

  savingCoupon.value = true
  try {
    const { data } = await api.post<Coupon>('/cupons', {
      code: couponCode.value.trim().toUpperCase(),
      discountPercentage: couponDiscount.value,
      expiresAt: couponExpiresAt.value ? new Date(couponExpiresAt.value).toISOString() : undefined,
    })
    coupons.value.unshift(data)
    couponCode.value = ''
    couponExpiresAt.value = ''
    successMessage.value = 'Cupom promocional criado.'
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Não foi possível criar o cupom.')
  } finally {
    savingCoupon.value = false
  }
}

async function handleDeleteCoupon(id: string) {
  errorMessage.value = ''
  successMessage.value = ''
  try {
    await api.delete(`/cupons/${id}`)
    coupons.value = coupons.value.filter((c) => c.id !== id)
    successMessage.value = 'Cupom removido.'
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Não foi possível remover o cupom.')
  }
}

async function handleToggleUserStatus(user: User) {
  errorMessage.value = ''
  successMessage.value = ''
  const nextStatus = user.status === 'Active' ? 'Blocked' : 'Active'
  try {
    const { data } = await api.patch<User>(`/usuarios/${user.id}/status`, { status: nextStatus })
    user.status = data.status
    successMessage.value = `Status de ${user.name} alterado para ${data.status}.`
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Erro ao alterar status do usuário.')
  }
}

async function handleAssignUserRole(user: User) {
  errorMessage.value = ''
  successMessage.value = ''
  const selectedRole = userRoleSelections.value[user.id]
  if (!selectedRole) return

  try {
    const { data } = await api.patch<User>(`/usuarios/${user.id}/roles`, {
      roles: [selectedRole],
    })
    user.roles = data.roles
    successMessage.value = `Perfil de ${user.name} atualizado para ${selectedRole}.`
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Não foi possível atualizar o perfil do usuário.')
  }
}

async function handleUpdateCourierStatus(courier: Courier, status: 'Approved' | 'Rejected') {
  errorMessage.value = ''
  successMessage.value = ''
  try {
    const { data } = await api.patch<Courier>(`/entregadores/${courier.id}/status`, { status })
    courier.status = data.status
    successMessage.value = `Entregador ${courier.name} marcado como ${status}.`
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Não foi possível atualizar o entregador.')
  }
}

async function handleProcessPayout(payout: Payout) {
  errorMessage.value = ''
  successMessage.value = ''
  try {
    const { data } = await api.post<Payout>(`/admin/financeiro/repasses/${payout.id}/processar`)
    payout.status = data.status
    payout.processedAt = data.processedAt
    successMessage.value = 'Repasse processado com sucesso.'
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Não foi possível processar o repasse.')
  }
}

function getOwnerName(ownerId: string): string {
  const found = users.value.find((u) => u.id === ownerId)
  return found ? `${found.name} (${found.email})` : ownerId
}

function getRestaurantName(restaurantId: string): string {
  const found = restaurants.value.find((r) => r.id === restaurantId)
  return found ? found.name : restaurantId
}

onMounted(() => {
  void loadAllData()
})
</script>

<template>
  <div class="min-h-screen bg-stone-50">
    <Navbar />

    <main class="mx-auto max-w-6xl space-y-8 px-4 py-8 sm:px-6">
      <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <div>
          <h1 class="text-2xl font-bold text-stone-900">Painel Administrativo</h1>
          <p class="mt-1 text-sm text-stone-600">
            Gestão de restaurantes, planos, cupons, usuários, entregadores e financeiro.
          </p>
        </div>

        <button
          type="button"
          :disabled="loading"
          class="inline-flex items-center gap-2 self-start rounded-lg border border-stone-200 bg-white px-4 py-2 text-sm font-medium text-stone-700 hover:bg-stone-50 disabled:opacity-50"
          @click="loadAllData"
        >
          <RefreshCw class="h-4 w-4" :class="{ 'animate-spin': loading }" />
          Atualizar dados
        </button>
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

      <!-- Indicadores Gerais -->
      <section v-if="stats" class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <div class="rounded-xl border border-stone-200 bg-white p-5 shadow-sm">
          <div class="flex items-center justify-between text-stone-500">
            <span class="text-sm font-medium">Usuários</span>
            <Users class="h-5 w-5 text-stone-400" />
          </div>
          <p class="mt-3 text-2xl font-bold text-stone-900">{{ stats.totalUsers }}</p>
        </div>

        <div class="rounded-xl border border-stone-200 bg-white p-5 shadow-sm">
          <div class="flex items-center justify-between text-stone-500">
            <span class="text-sm font-medium">Restaurantes</span>
            <Store class="h-5 w-5 text-orange-600" />
          </div>
          <p class="mt-3 text-2xl font-bold text-stone-900">{{ stats.totalRestaurants }}</p>
        </div>

        <div class="rounded-xl border border-stone-200 bg-white p-5 shadow-sm">
          <div class="flex items-center justify-between text-stone-500">
            <span class="text-sm font-medium">Pedidos</span>
            <ShoppingBag class="h-5 w-5 text-blue-600" />
          </div>
          <p class="mt-3 text-2xl font-bold text-stone-900">{{ stats.totalOrders }}</p>
        </div>

        <div class="rounded-xl border border-stone-200 bg-white p-5 shadow-sm">
          <div class="flex items-center justify-between text-stone-500">
            <span class="text-sm font-medium">Receita Total</span>
            <DollarSign class="h-5 w-5 text-green-600" />
          </div>
          <p class="mt-3 text-2xl font-bold text-stone-900">
            {{ formatCurrency(stats.totalRevenue) }}
          </p>
        </div>
      </section>

      <!-- Navegação por Abas -->
      <div class="flex flex-wrap gap-2 border-b border-stone-200 pb-3">
        <button
          type="button"
          class="inline-flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-medium transition-colors"
          :class="
            activeTab === 'restaurantes'
              ? 'bg-brand-600 text-white'
              : 'bg-white text-stone-700 hover:bg-stone-100'
          "
          @click="activeTab = 'restaurantes'"
        >
          <Store class="h-4 w-4" />
          Restaurantes & Categorias
        </button>

        <button
          type="button"
          class="inline-flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-medium transition-colors"
          :class="
            activeTab === 'planos'
              ? 'bg-brand-600 text-white'
              : 'bg-white text-stone-700 hover:bg-stone-100'
          "
          @click="activeTab = 'planos'"
        >
          <Tag class="h-4 w-4" />
          Planos & Cupons
        </button>

        <button
          type="button"
          class="inline-flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-medium transition-colors"
          :class="
            activeTab === 'usuarios'
              ? 'bg-brand-600 text-white'
              : 'bg-white text-stone-700 hover:bg-stone-100'
          "
          @click="activeTab = 'usuarios'"
        >
          <Users class="h-4 w-4" />
          Usuários & Entregadores
        </button>

        <button
          type="button"
          class="inline-flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-medium transition-colors"
          :class="
            activeTab === 'financeiro'
              ? 'bg-brand-600 text-white'
              : 'bg-white text-stone-700 hover:bg-stone-100'
          "
          @click="activeTab = 'financeiro'"
        >
          <DollarSign class="h-4 w-4" />
          Financeiro & Auditoria
        </button>
      </div>

      <!-- ABA 1: RESTAURANTES & CATEGORIAS -->
      <section v-if="activeTab === 'restaurantes'" class="grid gap-6 lg:grid-cols-3">
        <div class="space-y-6">
          <!-- Novo Restaurante -->
          <div class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm">
            <h2 class="text-base font-semibold text-stone-900">Cadastrar restaurante</h2>
            <form class="mt-4 space-y-3" @submit.prevent="handleCreateRestaurant">
              <div>
                <label class="block text-xs font-medium text-stone-700">Nome</label>
                <input
                  v-model="restName"
                  type="text"
                  required
                  placeholder="Ex: Marmitaria Central"
                  class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900"
                />
              </div>

              <div>
                <label class="block text-xs font-medium text-stone-700">CNPJ</label>
                <input
                  v-model="restDocument"
                  type="text"
                  required
                  placeholder="00.000.000/0001-00"
                  class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900"
                />
              </div>

              <div>
                <label class="block text-xs font-medium text-stone-700">Proprietário (Usuário)</label>
                <select
                  v-model="restOwnerId"
                  class="mt-1 block w-full rounded-lg border border-stone-300 bg-white px-3 py-2 text-sm text-stone-900"
                >
                  <option v-for="u in users" :key="u.id" :value="u.id">
                    {{ u.name }} ({{ u.email }})
                  </option>
                </select>
              </div>

              <div>
                <label class="block text-xs font-medium text-stone-700">Descrição</label>
                <textarea
                  v-model="restDescription"
                  rows="2"
                  placeholder="Especialidade e atendimento"
                  class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900"
                />
              </div>

              <div class="grid grid-cols-2 gap-2">
                <div>
                  <label class="block text-xs font-medium text-stone-700">CEP</label>
                  <input
                    v-model="restZipCode"
                    type="text"
                    placeholder="01001-000"
                    class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900"
                  />
                </div>
                <div>
                  <label class="block text-xs font-medium text-stone-700">Número</label>
                  <input
                    v-model="restNumber"
                    type="text"
                    placeholder="100"
                    class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900"
                  />
                </div>
              </div>

              <div>
                <label class="block text-xs font-medium text-stone-700">Rua / Logradouro</label>
                <input
                  v-model="restStreet"
                  type="text"
                  placeholder="Rua das Flores"
                  class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900"
                />
              </div>

              <div class="grid grid-cols-3 gap-2">
                <div>
                  <label class="block text-xs font-medium text-stone-700">Bairro</label>
                  <input
                    v-model="restNeighborhood"
                    type="text"
                    placeholder="Centro"
                    class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900"
                  />
                </div>
                <div>
                  <label class="block text-xs font-medium text-stone-700">Cidade</label>
                  <input
                    v-model="restCity"
                    type="text"
                    class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900"
                  />
                </div>
                <div>
                  <label class="block text-xs font-medium text-stone-700">UF</label>
                  <input
                    v-model="restState"
                    type="text"
                    maxlength="2"
                    class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900"
                  />
                </div>
              </div>

              <button
                type="submit"
                :disabled="savingRestaurant"
                class="inline-flex w-full items-center justify-center gap-2 rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-50"
              >
                <Plus class="h-4 w-4" />
                {{ savingRestaurant ? 'Salvando...' : 'Cadastrar restaurante' }}
              </button>
            </form>
          </div>

          <!-- Categorias -->
          <div class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm">
            <h2 class="text-base font-semibold text-stone-900">Categorias de Cardápio</h2>
            <form class="mt-3 flex gap-2" @submit.prevent="handleCreateCategory">
              <input
                v-model="catName"
                type="text"
                placeholder="Nova categoria"
                class="block flex-1 rounded-lg border border-stone-300 px-3 py-1.5 text-sm text-stone-900"
              />
              <button
                type="submit"
                :disabled="savingCategory"
                class="rounded-lg bg-stone-900 px-3 py-1.5 text-xs font-semibold text-white hover:bg-stone-800"
              >
                Adicionar
              </button>
            </form>

            <ul class="mt-4 divide-y divide-stone-100 text-sm">
              <li
                v-for="cat in categories"
                :key="cat.id"
                class="flex items-center justify-between py-2"
              >
                <span class="font-medium text-stone-800">{{ cat.name }}</span>
                <button
                  type="button"
                  class="text-stone-400 hover:text-red-600"
                  title="Remover categoria"
                  @click="handleDeleteCategory(cat.id)"
                >
                  <Trash2 class="h-4 w-4" />
                </button>
              </li>
            </ul>
          </div>
        </div>

        <!-- Lista de Restaurantes -->
        <div class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm lg:col-span-2">
          <h2 class="text-base font-semibold text-stone-900">
            Restaurantes cadastrados ({{ restaurants.length }})
          </h2>

          <div v-if="restaurants.length === 0" class="py-8 text-center text-sm text-stone-500">
            Nenhum restaurante encontrado.
          </div>

          <div v-else class="mt-4 space-y-3">
            <div
              v-for="rest in restaurants"
              :key="rest.id"
              class="flex flex-col justify-between gap-3 rounded-lg border border-stone-200 p-4 sm:flex-row sm:items-center"
            >
              <div>
                <div class="flex items-center gap-2">
                  <h3 class="font-semibold text-stone-900">{{ rest.name }}</h3>
                  <span
                    class="rounded-full px-2 py-0.5 text-[11px] font-medium"
                    :class="
                      rest.status === 'Approved'
                        ? 'bg-green-100 text-green-800'
                        : 'bg-amber-100 text-amber-800'
                    "
                  >
                    {{ rest.status }}
                  </span>
                </div>
                <p class="mt-0.5 text-xs text-stone-500">
                  CNPJ: {{ rest.document }} · Dono: {{ getOwnerName(rest.ownerId) }}
                </p>
                <p class="mt-1 text-xs text-stone-600">{{ rest.description }}</p>
              </div>

              <div class="flex items-center gap-2">
                <button
                  v-if="rest.status !== 'Approved'"
                  type="button"
                  class="rounded-lg border border-green-200 bg-green-50 px-3 py-1.5 text-xs font-medium text-green-700 hover:bg-green-100"
                  @click="handleChangeRestaurantStatus(rest.id, 'Approved')"
                >
                  Aprovar
                </button>
                <button
                  v-if="rest.status === 'Approved'"
                  type="button"
                  class="rounded-lg border border-amber-200 bg-amber-50 px-3 py-1.5 text-xs font-medium text-amber-700 hover:bg-amber-100"
                  @click="handleChangeRestaurantStatus(rest.id, 'Rejected')"
                >
                  Pausar
                </button>
                <button
                  type="button"
                  class="rounded-lg border border-red-200 px-2.5 py-1.5 text-xs text-red-600 hover:bg-red-50"
                  @click="handleDeleteRestaurant(rest.id)"
                >
                  <Trash2 class="h-4 w-4" />
                </button>
              </div>
            </div>
          </div>
        </div>
      </section>

      <!-- ABA 2: PLANOS & CUPONS -->
      <section v-else-if="activeTab === 'planos'" class="grid gap-6 lg:grid-cols-2">
        <!-- Planos de Assinatura -->
        <div class="space-y-4 rounded-xl border border-stone-200 bg-white p-6 shadow-sm">
          <h2 class="text-base font-semibold text-stone-900">Planos de Assinatura</h2>

          <form class="space-y-3 rounded-lg bg-stone-50 p-4" @submit.prevent="handleCreatePlan">
            <div class="grid gap-3 sm:grid-cols-2">
              <div>
                <label class="block text-xs font-medium text-stone-700">Nome do plano</label>
                <input
                  v-model="planName"
                  type="text"
                  required
                  placeholder="Ex: Plano Semanal 5 Refeições"
                  class="mt-1 block w-full rounded-lg border border-stone-300 bg-white px-3 py-1.5 text-sm text-stone-900"
                />
              </div>
              <div>
                <label class="block text-xs font-medium text-stone-700">Refeições / semana</label>
                <input
                  v-model.number="planMealsPerWeek"
                  type="number"
                  min="1"
                  required
                  class="mt-1 block w-full rounded-lg border border-stone-300 bg-white px-3 py-1.5 text-sm text-stone-900"
                />
              </div>
            </div>

            <div class="grid gap-3 sm:grid-cols-2">
              <div>
                <label class="block text-xs font-medium text-stone-700">Valor mensal (R$)</label>
                <input
                  v-model.number="planMonthlyPrice"
                  type="number"
                  step="0.01"
                  min="1"
                  required
                  placeholder="249.90"
                  class="mt-1 block w-full rounded-lg border border-stone-300 bg-white px-3 py-1.5 text-sm text-stone-900"
                />
              </div>
              <div>
                <label class="block text-xs font-medium text-stone-700">Descrição</label>
                <input
                  v-model="planDescription"
                  type="text"
                  placeholder="Almoço garantido de seg a sex"
                  class="mt-1 block w-full rounded-lg border border-stone-300 bg-white px-3 py-1.5 text-sm text-stone-900"
                />
              </div>
            </div>

            <button
              type="submit"
              :disabled="savingPlan"
              class="inline-flex items-center gap-1.5 rounded-lg bg-brand-600 px-4 py-2 text-xs font-semibold text-white hover:bg-brand-700"
            >
              <Plus class="h-4 w-4" />
              Criar plano
            </button>
          </form>

          <div class="divide-y divide-stone-100">
            <div
              v-for="plan in plans"
              :key="plan.id"
              class="flex items-center justify-between py-3"
            >
              <div>
                <p class="font-medium text-stone-900">
                  {{ plan.name }} · {{ formatCurrency(plan.monthlyPrice) }}/mês
                </p>
                <p class="text-xs text-stone-500">
                  {{ plan.mealsPerWeek }} marmitas/semana · {{ plan.description }}
                </p>
              </div>
              <button
                type="button"
                class="text-stone-400 hover:text-red-600"
                @click="handleDeletePlan(plan.id)"
              >
                <Trash2 class="h-4 w-4" />
              </button>
            </div>
          </div>
        </div>

        <!-- Cupons de Desconto -->
        <div class="space-y-4 rounded-xl border border-stone-200 bg-white p-6 shadow-sm">
          <h2 class="text-base font-semibold text-stone-900">Cupons Promocionais</h2>

          <form class="space-y-3 rounded-lg bg-stone-50 p-4" @submit.prevent="handleCreateCoupon">
            <div class="grid gap-3 sm:grid-cols-3">
              <div>
                <label class="block text-xs font-medium text-stone-700">Código</label>
                <input
                  v-model="couponCode"
                  type="text"
                  required
                  placeholder="MARMITA10"
                  class="mt-1 block w-full rounded-lg border border-stone-300 bg-white px-3 py-1.5 text-sm uppercase text-stone-900"
                />
              </div>
              <div>
                <label class="block text-xs font-medium text-stone-700">Desconto (%)</label>
                <input
                  v-model.number="couponDiscount"
                  type="number"
                  min="1"
                  max="100"
                  required
                  class="mt-1 block w-full rounded-lg border border-stone-300 bg-white px-3 py-1.5 text-sm text-stone-900"
                />
              </div>
              <div>
                <label class="block text-xs font-medium text-stone-700">Validade</label>
                <input
                  v-model="couponExpiresAt"
                  type="date"
                  class="mt-1 block w-full rounded-lg border border-stone-300 bg-white px-3 py-1.5 text-sm text-stone-900"
                />
              </div>
            </div>

            <button
              type="submit"
              :disabled="savingCoupon"
              class="inline-flex items-center gap-1.5 rounded-lg bg-brand-600 px-4 py-2 text-xs font-semibold text-white hover:bg-brand-700"
            >
              <Plus class="h-4 w-4" />
              Criar cupom
            </button>
          </form>

          <div v-if="coupons.length === 0" class="py-6 text-center text-sm text-stone-500">
            Nenhum cupom cadastrado.
          </div>

          <div v-else class="divide-y divide-stone-100">
            <div
              v-for="coupon in coupons"
              :key="coupon.id"
              class="flex items-center justify-between py-3"
            >
              <div>
                <span class="rounded bg-orange-100 px-2 py-0.5 font-mono text-xs font-bold text-orange-800">
                  {{ coupon.code }}
                </span>
                <span class="ml-2 text-sm font-medium text-stone-900">
                  {{ coupon.discountPercentage }}% OFF
                </span>
                <p class="mt-0.5 text-xs text-stone-500">
                  Expira em {{ new Date(coupon.expiresAt).toLocaleDateString('pt-BR') }}
                </p>
              </div>
              <button
                type="button"
                class="text-stone-400 hover:text-red-600"
                @click="handleDeleteCoupon(coupon.id)"
              >
                <Trash2 class="h-4 w-4" />
              </button>
            </div>
          </div>
        </div>
      </section>

      <!-- ABA 3: USUÁRIOS & ENTREGADORES -->
      <section v-else-if="activeTab === 'usuarios'" class="grid gap-6 lg:grid-cols-2">
        <!-- Usuários e Roles -->
        <div class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm">
          <div class="flex items-center gap-2">
            <Shield class="h-5 w-5 text-brand-600" />
            <h2 class="text-base font-semibold text-stone-900">
              Usuários e Perfis ({{ users.length }})
            </h2>
          </div>

          <div class="mt-4 space-y-3">
            <div
              v-for="u in users"
              :key="u.id"
              class="rounded-lg border border-stone-200 p-3 text-sm"
            >
              <div class="flex items-start justify-between gap-2">
                <div>
                  <p class="font-semibold text-stone-900">{{ u.name }}</p>
                  <p class="text-xs text-stone-500">{{ u.email }}</p>
                  <div class="mt-1 flex flex-wrap gap-1">
                    <span
                      v-for="role in u.roles"
                      :key="role"
                      class="rounded bg-stone-100 px-2 py-0.5 text-[11px] font-medium text-stone-700"
                    >
                      {{ role }}
                    </span>
                  </div>
                </div>

                <button
                  type="button"
                  class="rounded-lg border px-2.5 py-1 text-xs font-medium"
                  :class="
                    u.status === 'Active'
                      ? 'border-stone-200 text-stone-700 hover:bg-stone-50'
                      : 'border-red-200 bg-red-50 text-red-700'
                  "
                  @click="handleToggleUserStatus(u)"
                >
                  {{ u.status === 'Active' ? 'Bloquear' : 'Ativar' }}
                </button>
              </div>

              <div class="mt-3 flex items-center gap-2 border-t border-stone-100 pt-2">
                <select
                  v-model="userRoleSelections[u.id]"
                  class="block flex-1 rounded-lg border border-stone-300 bg-white px-2.5 py-1 text-xs text-stone-800"
                >
                  <option value="customer">customer (Cliente)</option>
                  <option value="restaurant_owner">restaurant_owner (Dono de Restaurante)</option>
                  <option value="driver">driver (Entregador)</option>
                  <option value="admin">admin (Administrador)</option>
                </select>
                <button
                  type="button"
                  class="rounded-lg bg-stone-900 px-3 py-1 text-xs font-medium text-white hover:bg-stone-800"
                  @click="handleAssignUserRole(u)"
                >
                  Salvar perfil
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Homologação de Entregadores -->
        <div class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm">
          <div class="flex items-center gap-2">
            <Truck class="h-5 w-5 text-brand-600" />
            <h2 class="text-base font-semibold text-stone-900">
              Entregadores Parceiros ({{ couriers.length }})
            </h2>
          </div>

          <div v-if="couriers.length === 0" class="py-8 text-center text-sm text-stone-500">
            Nenhum entregador cadastrado no momento.
          </div>

          <div v-else class="mt-4 space-y-3">
            <div
              v-for="courier in couriers"
              :key="courier.id"
              class="flex items-center justify-between rounded-lg border border-stone-200 p-3 text-sm"
            >
              <div>
                <div class="flex items-center gap-2">
                  <p class="font-semibold text-stone-900">{{ courier.name }}</p>
                  <span
                    class="rounded-full px-2 py-0.5 text-[11px] font-medium"
                    :class="
                      courier.status === 'Approved'
                        ? 'bg-green-100 text-green-800'
                        : 'bg-amber-100 text-amber-800'
                    "
                  >
                    {{ courier.status }}
                  </span>
                </div>
                <p class="text-xs text-stone-500">
                  CPF/Doc: {{ courier.document || 'N/A' }} · Tel: {{ courier.phone || 'N/A' }}
                </p>
              </div>

              <div class="flex items-center gap-1.5">
                <button
                  v-if="courier.status !== 'Approved'"
                  type="button"
                  class="inline-flex items-center gap-1 rounded-lg bg-green-600 px-2.5 py-1 text-xs font-medium text-white hover:bg-green-700"
                  @click="handleUpdateCourierStatus(courier, 'Approved')"
                >
                  <CheckCircle2 class="h-3.5 w-3.5" />
                  Aprovar
                </button>
                <button
                  v-if="courier.status !== 'Rejected'"
                  type="button"
                  class="inline-flex items-center gap-1 rounded-lg border border-red-200 px-2.5 py-1 text-xs font-medium text-red-600 hover:bg-red-50"
                  @click="handleUpdateCourierStatus(courier, 'Rejected')"
                >
                  <XCircle class="h-3.5 w-3.5" />
                  Rejeitar
                </button>
              </div>
            </div>
          </div>
        </div>
      </section>

      <!-- ABA 4: FINANCEIRO, RELATÓRIOS & AUDITORIA -->
      <section v-else-if="activeTab === 'financeiro'" class="space-y-6">
        <div class="grid gap-4 sm:grid-cols-3">
          <div class="rounded-xl border border-stone-200 bg-white p-5 shadow-sm">
            <span class="text-xs font-medium text-stone-500">
              Comissão da Plataforma ({{ commissions?.commissionRatePercentage ?? 10 }}%)
            </span>
            <p class="mt-2 text-2xl font-bold text-green-700">
              {{ formatCurrency(commissions?.totalCommission ?? 0) }}
            </p>
            <p class="mt-1 text-xs text-stone-500">
              Volume bruto: {{ formatCurrency(commissions?.grossVolume ?? 0) }}
            </p>
          </div>

          <div class="rounded-xl border border-stone-200 bg-white p-5 shadow-sm">
            <span class="text-xs font-medium text-stone-500">Receita Recorrente (Assinaturas)</span>
            <p class="mt-2 text-2xl font-bold text-stone-900">
              {{ formatCurrency(subscriptionsReport?.monthlyRecurringRevenue ?? 0) }}
            </p>
            <p class="mt-1 text-xs text-stone-500">
              Ativas: {{ subscriptionsReport?.totalActiveSubscriptions ?? 0 }} · Canceladas:
              {{ subscriptionsReport?.totalCancelledSubscriptions ?? 0 }}
            </p>
          </div>

          <div class="rounded-xl border border-stone-200 bg-white p-5 shadow-sm">
            <span class="text-xs font-medium text-stone-500">Relatório de Entregas</span>
            <p class="mt-2 text-2xl font-bold text-stone-900">
              {{ deliveriesReport?.totalDeliveries ?? 0 }} concluídas
            </p>
            <p class="mt-1 text-xs text-stone-500">
              Em rota: {{ deliveriesReport?.inTransitDeliveries ?? 0 }} · Canceladas:
              {{ deliveriesReport?.cancelledDeliveries ?? 0 }}
            </p>
          </div>
        </div>

        <div class="grid gap-6 lg:grid-cols-2">
          <!-- Repasses aos Restaurantes -->
          <div class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm">
            <h2 class="text-base font-semibold text-stone-900">Repasses Financeiros</h2>
            <p class="mt-0.5 text-xs text-stone-500">
              Valores líquidos (90%) disponíveis para transferência aos restaurantes parceiros.
            </p>

            <div v-if="payouts.length === 0" class="py-8 text-center text-sm text-stone-500">
              Nenhum repasse pendente ou registrado.
            </div>

            <div v-else class="mt-4 space-y-3">
              <div
                v-for="payout in payouts"
                :key="payout.id"
                class="flex items-center justify-between rounded-lg border border-stone-200 p-3 text-sm"
              >
                <div>
                  <p class="font-semibold text-stone-900">
                    {{ getRestaurantName(payout.targetId) }}
                  </p>
                  <p class="text-xs text-stone-500">
                    Bruto: {{ formatCurrency(payout.grossAmount) }} · Líquido:
                    <strong>{{ formatCurrency(payout.netAmount) }}</strong>
                  </p>
                </div>

                <div>
                  <button
                    v-if="payout.status !== 'Processed'"
                    type="button"
                    class="rounded-lg bg-brand-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-brand-700"
                    @click="handleProcessPayout(payout)"
                  >
                    Processar repasse
                  </button>
                  <span
                    v-else
                    class="rounded-full bg-green-100 px-2.5 py-1 text-xs font-medium text-green-800"
                  >
                    Processado
                  </span>
                </div>
              </div>
            </div>
          </div>

          <!-- Trilha de Auditoria -->
          <div class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm">
            <h2 class="text-base font-semibold text-stone-900">Auditoria do Sistema</h2>
            <p class="mt-0.5 text-xs text-stone-500">
              Últimos eventos registrados na operação da plataforma.
            </p>

            <div v-if="auditLogs.length === 0" class="py-8 text-center text-sm text-stone-500">
              Nenhum evento de auditoria registrado.
            </div>

            <ul v-else class="mt-4 max-h-80 divide-y divide-stone-100 overflow-y-auto text-xs">
              <li v-for="log in auditLogs.slice(0, 20)" :key="log.id" class="py-2.5">
                <div class="flex items-center justify-between">
                  <span class="font-semibold text-stone-800">{{ log.action }}</span>
                  <span class="text-stone-400">
                    {{ new Date(log.timestamp).toLocaleString('pt-BR') }}
                  </span>
                </div>
                <p class="mt-0.5 text-stone-600">{{ log.details }}</p>
              </li>
            </ul>
          </div>
        </div>
      </section>
    </main>
  </div>
</template>
