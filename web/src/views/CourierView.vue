<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import {
  AlertTriangle,
  Bike,
  CheckCircle2,
  DollarSign,
  FileText,
  Lock,
  MapPin,
  RefreshCw,
  Store,
} from 'lucide-vue-next'
import Navbar from '@/components/Navbar.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import type {
  Courier,
  CourierBillingAddress,
  CourierDocument,
  CourierEarnings,
  CourierVehicle,
  Delivery,
} from '@/types'

const earnings = ref<CourierEarnings | null>(null)
const deliveries = ref<Delivery[]>([])
const documents = ref<CourierDocument[]>([])
const vehicles = ref<CourierVehicle[]>([])

const isAvailable = ref(true)
const loading = ref(true)
const toggling = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

// Documentos obrigatórios: CPF e CNH
const cpfNumber = ref('')
const cnhNumber = ref('')
const savingDocs = ref(false)

// Veículo obrigatório
const vehicleType = ref('Moto')
const vehicleModel = ref('')
const vehiclePlate = ref('')
const savingVehicle = ref(false)

// Endereço de Cobrança obrigatório (exclusivo para faturamento do entregador)
const billingZipCode = ref('')
const billingStreet = ref('')
const billingNumber = ref('')
const billingComplement = ref('')
const billingNeighborhood = ref('')
const billingCity = ref('')
const billingState = ref('SP')
const billingSaved = ref(false)
const savingBilling = ref(false)

const hasCpf = computed(() =>
  documents.value.some((d) => d.type.toUpperCase() === 'CPF' && d.number.trim().length > 0),
)

const hasCnh = computed(() =>
  documents.value.some((d) => d.type.toUpperCase() === 'CNH' && d.number.trim().length > 0),
)

const hasVehicle = computed(() => vehicles.value.length > 0)

const hasBillingAddress = computed(() => billingSaved.value)

const isRegistrationComplete = computed(
  () => hasCpf.value && hasCnh.value && hasVehicle.value && hasBillingAddress.value,
)

function formatDeliveryStatus(status: string): string {
  const map: Record<string, string> = {
    Pending: 'Aguardando',
    Paid: 'Pago / Pronto',
    Preparing: 'Em preparo',
    ReadyForPickup: 'Pronto para coleta',
    DriverAssigned: 'Aceita por entregador',
    InTransit: 'Em rota de entrega',
    Delivered: 'Entregue',
    Cancelled: 'Cancelada',
  }
  return map[status] || status
}

async function loadCourierData() {
  loading.value = true
  errorMessage.value = ''

  try {
    const [earnRes, docRes, vehRes, billRes] = await Promise.allSettled([
      api.get<CourierEarnings>('/entregadores/me/ganhos'),
      api.get<CourierDocument[]>('/entregadores/me/documentos'),
      api.get<CourierVehicle[]>('/entregadores/me/veiculos'),
      api.get<CourierBillingAddress>('/entregadores/me/endereco-cobranca'),
    ])

    if (earnRes.status === 'fulfilled') earnings.value = earnRes.value.data
    if (docRes.status === 'fulfilled') {
      documents.value = docRes.value.data
      const existingCpf = docRes.value.data.find((d) => d.type.toUpperCase() === 'CPF')
      const existingCnh = docRes.value.data.find((d) => d.type.toUpperCase() === 'CNH')
      if (existingCpf) cpfNumber.value = existingCpf.number
      if (existingCnh) cnhNumber.value = existingCnh.number
    }
    if (vehRes.status === 'fulfilled') vehicles.value = vehRes.value.data
    if (billRes.status === 'fulfilled' && billRes.value.data) {
      const b = billRes.value.data
      billingStreet.value = b.street || ''
      billingNumber.value = b.number || ''
      billingComplement.value = b.complement || ''
      billingNeighborhood.value = b.neighborhood || ''
      billingCity.value = b.city || ''
      billingState.value = b.state || 'SP'
      billingZipCode.value = b.zipCode || ''
      billingSaved.value = Boolean(
        b.isComplete ||
          (b.street?.trim() &&
            b.number?.trim() &&
            b.neighborhood?.trim() &&
            b.city?.trim() &&
            b.state?.trim() &&
            b.zipCode?.trim()),
      )
    }

    // Só busca as entregas se o cadastro obrigatório (CPF, CNH, Veículo e Endereço de Cobrança) estiver completo
    if (hasCpf.value && hasCnh.value && hasVehicle.value && hasBillingAddress.value) {
      const delivRes = await api.get<Delivery[]>('/entregas')
      deliveries.value = delivRes.data
    } else {
      deliveries.value = []
    }
  } catch (error) {
    errorMessage.value = getErrorMessage(
      error,
      'Não foi possível carregar os dados do entregador.',
    )
  } finally {
    loading.value = false
  }
}

async function toggleAvailability() {
  toggling.value = true
  errorMessage.value = ''

  try {
    const nextState = !isAvailable.value
    const { data } = await api.patch<Courier>('/entregadores/me/disponibilidade', {
      isAvailable: nextState,
    })
    isAvailable.value = data.isAvailable
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Não foi possível alterar sua disponibilidade.')
  } finally {
    toggling.value = false
  }
}

async function handleAcceptDelivery(id: string) {
  errorMessage.value = ''
  successMessage.value = ''

  if (!isRegistrationComplete.value) {
    errorMessage.value =
      'Complete seu cadastro obrigatório (CPF, CNH, Veículo e Endereço de Cobrança) antes de aceitar entregas.'
    return
  }

  try {
    await api.patch(`/entregas/${id}/aceitar`)
    successMessage.value = 'Entrega aceita com sucesso.'
    await loadCourierData()
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Erro ao aceitar entrega.')
  }
}

async function handleUpdateDeliveryStatus(id: string, status: string) {
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await api.patch(`/entregas/${id}/status`, { status })
    successMessage.value = 'Status da entrega atualizado.'
    await loadCourierData()
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Erro ao atualizar status da entrega.')
  }
}

async function handleSaveMandatoryDocuments() {
  errorMessage.value = ''
  successMessage.value = ''

  if (!cpfNumber.value.trim() || !cnhNumber.value.trim()) {
    errorMessage.value = 'O preenchimento tanto do CPF quanto da CNH é obrigatório.'
    return
  }

  savingDocs.value = true
  try {
    await api.post<CourierDocument>('/entregadores/me/documentos', {
      type: 'CPF',
      number: cpfNumber.value.trim(),
    })
    await api.post<CourierDocument>('/entregadores/me/documentos', {
      type: 'CNH',
      number: cnhNumber.value.trim(),
    })
    successMessage.value = 'Documentos obrigatórios (CPF e CNH) registrados com sucesso!'
    await loadCourierData()
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Erro ao salvar CPF e CNH.')
  } finally {
    savingDocs.value = false
  }
}

async function handleAddVehicle() {
  errorMessage.value = ''
  successMessage.value = ''

  if (!vehicleModel.value.trim() || !vehiclePlate.value.trim()) {
    errorMessage.value = 'Informe o modelo e a placa do veículo obrigatoriamente.'
    return
  }

  savingVehicle.value = true
  try {
    await api.post<CourierVehicle>('/entregadores/me/veiculos', {
      type: vehicleType.value,
      model: vehicleModel.value.trim(),
      licensePlate: vehiclePlate.value.trim().toUpperCase(),
    })
    vehicleModel.value = ''
    vehiclePlate.value = ''
    successMessage.value = 'Veículo cadastrado com sucesso!'
    await loadCourierData()
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Erro ao cadastrar veículo.')
  } finally {
    savingVehicle.value = false
  }
}

async function handleSaveBillingAddress() {
  errorMessage.value = ''
  successMessage.value = ''

  if (
    !billingZipCode.value.trim() ||
    !billingStreet.value.trim() ||
    !billingNumber.value.trim() ||
    !billingNeighborhood.value.trim() ||
    !billingCity.value.trim() ||
    !billingState.value.trim()
  ) {
    errorMessage.value =
      'Preencha todos os campos obrigatórios do endereço de cobrança (CEP, Rua, Número, Bairro, Cidade e UF).'
    return
  }

  savingBilling.value = true
  try {
    await api.put<CourierBillingAddress>('/entregadores/me/endereco-cobranca', {
      zipCode: billingZipCode.value.trim(),
      street: billingStreet.value.trim(),
      number: billingNumber.value.trim(),
      complement: billingComplement.value.trim() || null,
      neighborhood: billingNeighborhood.value.trim(),
      city: billingCity.value.trim(),
      state: billingState.value.trim().toUpperCase(),
    })
    successMessage.value = 'Endereço de cobrança do entregador salvo com sucesso!'
    await loadCourierData()
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Erro ao salvar endereço de cobrança.')
  } finally {
    savingBilling.value = false
  }
}

onMounted(() => {
  void loadCourierData()
})
</script>

<template>
  <div class="min-h-screen bg-stone-50">
    <Navbar />

    <main class="mx-auto max-w-5xl space-y-8 px-4 py-8 sm:px-6">
      <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <div>
          <h1 class="text-2xl font-bold text-stone-900">Painel do Entregador</h1>
          <p class="mt-1 text-sm text-stone-600">
            Consulte seus ganhos, aceite entregas e gerencie seus dados obrigatórios.
          </p>
        </div>

        <div class="flex items-center gap-3">
          <button
            type="button"
            :disabled="toggling || !isRegistrationComplete"
            class="inline-flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-semibold transition disabled:opacity-50"
            :class="
              isAvailable
                ? 'bg-green-600 text-white hover:bg-green-700'
                : 'border border-stone-300 bg-white text-stone-700 hover:bg-stone-50'
            "
            @click="toggleAvailability"
          >
            <Bike class="h-4 w-4" />
            {{ isAvailable ? 'Disponível para entregas' : 'Ficar disponível' }}
          </button>

          <button
            type="button"
            :disabled="loading"
            class="inline-flex items-center gap-2 rounded-lg border border-stone-200 bg-white px-3.5 py-2 text-sm font-medium text-stone-700 hover:bg-stone-50 disabled:opacity-50"
            @click="loadCourierData"
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

      <!-- Checklist de Cadastro Obrigatório do Entregador -->
      <section class="rounded-xl border border-stone-200 bg-white p-5 shadow-sm">
        <div class="flex flex-col justify-between gap-2 sm:flex-row sm:items-center">
          <div>
            <h2 class="text-base font-bold text-stone-900">
              Requisitos Obrigatórios para Liberar Entregas
            </h2>
            <p class="text-xs text-stone-500">
              As entregas disponíveis só aparecem após o cadastro completo de CPF, CNH, Veículo e Endereço de Cobrança.
            </p>
          </div>
          <span
            class="inline-flex items-center gap-1.5 self-start rounded-full px-3 py-1 text-xs font-semibold sm:self-auto"
            :class="
              isRegistrationComplete
                ? 'bg-green-100 text-green-800'
                : 'bg-amber-100 text-amber-800'
            "
          >
            <CheckCircle2 v-if="isRegistrationComplete" class="h-3.5 w-3.5" />
            <AlertTriangle v-else class="h-3.5 w-3.5" />
            {{ isRegistrationComplete ? 'Cadastro Completo (Liberado)' : 'Cadastro Pendente' }}
          </span>
        </div>

        <div class="mt-4 grid gap-3 sm:grid-cols-4">
          <div
            class="flex items-center justify-between rounded-lg border px-3 py-2.5 text-xs font-semibold"
            :class="
              hasCpf
                ? 'border-green-200 bg-green-50 text-green-800'
                : 'border-amber-200 bg-amber-50 text-amber-800'
            "
          >
            <span>1. CPF</span>
            <span>{{ hasCpf ? 'Cadastrado' : 'Pendente' }}</span>
          </div>

          <div
            class="flex items-center justify-between rounded-lg border px-3 py-2.5 text-xs font-semibold"
            :class="
              hasCnh
                ? 'border-green-200 bg-green-50 text-green-800'
                : 'border-amber-200 bg-amber-50 text-amber-800'
            "
          >
            <span>2. CNH</span>
            <span>{{ hasCnh ? 'Cadastrada' : 'Pendente' }}</span>
          </div>

          <div
            class="flex items-center justify-between rounded-lg border px-3 py-2.5 text-xs font-semibold"
            :class="
              hasVehicle
                ? 'border-green-200 bg-green-50 text-green-800'
                : 'border-amber-200 bg-amber-50 text-amber-800'
            "
          >
            <span>3. Veículo</span>
            <span>{{ hasVehicle ? 'Cadastrado' : 'Pendente' }}</span>
          </div>

          <div
            class="flex items-center justify-between rounded-lg border px-3 py-2.5 text-xs font-semibold"
            :class="
              hasBillingAddress
                ? 'border-green-200 bg-green-50 text-green-800'
                : 'border-amber-200 bg-amber-50 text-amber-800'
            "
          >
            <span>4. End. de Cobrança</span>
            <span>{{ hasBillingAddress ? 'Cadastrado' : 'Pendente' }}</span>
          </div>
        </div>
      </section>

      <div v-if="earnings" class="grid gap-4 sm:grid-cols-2">
        <div class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm">
          <div class="flex items-center gap-3 text-stone-500">
            <CheckCircle2 class="h-5 w-5 text-green-600" />
            <span class="text-sm font-medium">Entregas concluídas</span>
          </div>
          <p class="mt-3 text-3xl font-bold text-stone-900">
            {{ earnings.completedDeliveries }}
          </p>
        </div>

        <div class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm">
          <div class="flex items-center gap-3 text-stone-500">
            <DollarSign class="h-5 w-5 text-orange-600" />
            <span class="text-sm font-medium">Total acumulado</span>
          </div>
          <p class="mt-3 text-3xl font-bold text-stone-900">
            {{ formatCurrency(earnings.totalEarnings) }}
          </p>
        </div>
      </div>

      <!-- Fila de entregas (Somente visível após cadastrar CPF, CNH, Veículo e Endereço de Cobrança) -->
      <section class="space-y-4">
        <h2 class="text-lg font-bold text-stone-900">Fila de entregas</h2>

        <p v-if="loading" class="text-sm text-stone-500">Carregando dados...</p>

        <div
          v-else-if="!isRegistrationComplete"
          class="rounded-xl border border-amber-200 bg-amber-50 p-6 text-sm text-amber-900 shadow-sm"
        >
          <div class="flex items-start gap-3">
            <Lock class="mt-0.5 h-5 w-5 shrink-0 text-amber-700" />
            <div class="space-y-1">
              <p class="font-bold">
                Fila de entregas bloqueada — Conclua seu cadastro obrigatório abaixo
              </p>
              <p class="text-xs text-amber-800">
                Para visualizar os pedidos disponíveis para coleta, cadastre obrigatoriamente seu
                <strong>CPF</strong>, <strong>CNH</strong>, <strong>Veículo</strong> e
                <strong>Endereço de Cobrança</strong> nos formulários abaixo.
              </p>
            </div>
          </div>
        </div>

        <div v-else-if="deliveries.length > 0" class="space-y-3">
          <div
            v-for="delivery in deliveries"
            :key="delivery.id"
            class="flex flex-col justify-between gap-4 rounded-xl border border-stone-200 bg-white p-5 shadow-sm sm:flex-row sm:items-center"
          >
            <div class="space-y-2">
              <div class="flex flex-wrap items-center gap-2">
                <span class="font-bold text-stone-900">
                  Pedido #{{ delivery.orderId.slice(0, 8) }}
                </span>
                <span
                  class="rounded-full px-2.5 py-0.5 text-xs font-semibold"
                  :class="
                    delivery.status === 'Delivered'
                      ? 'bg-green-50 text-green-700'
                      : delivery.status === 'Cancelled'
                        ? 'bg-red-50 text-red-700'
                        : 'bg-amber-50 text-amber-800'
                  "
                >
                  {{ formatDeliveryStatus(delivery.status) }}
                </span>
              </div>

              <div class="grid gap-1.5 text-xs text-stone-700 sm:grid-cols-2 sm:gap-4">
                <div class="flex items-start gap-1.5">
                  <Store class="mt-0.5 h-4 w-4 shrink-0 text-brand-600" />
                  <div>
                    <span class="font-semibold text-stone-900">Coleta (Restaurante):</span>
                    <p class="font-medium text-brand-700">
                      {{ delivery.restaurantName || 'Restaurante Parceiro' }}
                    </p>
                    <p v-if="delivery.restaurantAddress" class="text-[11px] text-stone-500">
                      {{ delivery.restaurantAddress }}
                    </p>
                  </div>
                </div>

                <div class="flex items-start gap-1.5">
                  <MapPin class="mt-0.5 h-4 w-4 shrink-0 text-green-600" />
                  <div>
                    <span class="font-semibold text-stone-900">Região da Entrega (Bairro):</span>
                    <p class="font-medium text-green-700">
                      Bairro: {{ delivery.deliveryNeighborhood || 'Centro' }}
                    </p>
                    <p v-if="delivery.deliveryAddress" class="text-[11px] text-stone-500">
                      {{ delivery.deliveryAddress }}
                    </p>
                  </div>
                </div>
              </div>

              <p class="text-[11px] text-stone-400">ID da entrega: {{ delivery.id }}</p>
            </div>

            <div class="flex flex-wrap items-center gap-2">
              <button
                v-if="!delivery.courierId && delivery.status !== 'Delivered' && delivery.status !== 'Cancelled'"
                type="button"
                class="rounded-lg bg-brand-600 px-3.5 py-1.5 text-xs font-semibold text-white hover:bg-brand-700"
                @click="handleAcceptDelivery(delivery.id)"
              >
                Aceitar entrega
              </button>

              <button
                v-if="delivery.status === 'DriverAssigned'"
                type="button"
                class="rounded-lg bg-stone-900 px-3.5 py-1.5 text-xs font-semibold text-white hover:bg-stone-800"
                @click="handleUpdateDeliveryStatus(delivery.id, 'InTransit')"
              >
                Iniciar rota
              </button>

              <button
                v-if="delivery.status === 'DriverAssigned' || delivery.status === 'InTransit'"
                type="button"
                class="rounded-lg bg-green-600 px-3.5 py-1.5 text-xs font-semibold text-white hover:bg-green-700"
                @click="handleUpdateDeliveryStatus(delivery.id, 'Delivered')"
              >
                Confirmar entrega
              </button>
            </div>
          </div>
        </div>

        <div
          v-else
          class="rounded-xl border border-stone-200 bg-white p-6 text-center text-sm text-stone-500"
        >
          Nenhuma entrega disponível no momento.
        </div>
      </section>

      <!-- Formulários de Cadastro Obrigatório: CPF + CNH e Veículo -->
      <div class="grid gap-6 sm:grid-cols-2">
        <section class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm">
          <div class="flex items-center gap-2">
            <FileText class="h-5 w-5 text-brand-600" />
            <h2 class="text-base font-bold text-stone-900">
              Documentos Obrigatórios (CPF e CNH)
            </h2>
          </div>
          <p class="mt-1 text-xs text-stone-500">
            Cadastre obrigatoriamente seu CPF e sua CNH para habilitar sua conta.
          </p>

          <ul v-if="documents.length > 0" class="mt-3 space-y-2 text-sm text-stone-700">
            <li
              v-for="doc in documents"
              :key="doc.id"
              class="flex items-center justify-between rounded-lg bg-stone-50 px-3 py-2"
            >
              <span class="font-medium">{{ doc.type }}: {{ doc.number }}</span>
              <span class="text-xs font-semibold text-green-700">Verificado</span>
            </li>
          </ul>

          <form class="mt-4 space-y-3" @submit.prevent="handleSaveMandatoryDocuments">
            <div>
              <label class="block text-xs font-medium text-stone-700">
                CPF <span class="text-red-500">*</span>
              </label>
              <input
                v-model="cpfNumber"
                type="text"
                required
                placeholder="000.000.000-00"
                class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
              />
            </div>

            <div>
              <label class="block text-xs font-medium text-stone-700">
                Número da CNH <span class="text-red-500">*</span>
              </label>
              <input
                v-model="cnhNumber"
                type="text"
                required
                placeholder="00000000000"
                class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
              />
            </div>

            <button
              type="submit"
              :disabled="savingDocs"
              class="w-full rounded-lg bg-stone-900 px-4 py-2 text-xs font-semibold text-white hover:bg-stone-800 disabled:opacity-50"
            >
              {{ savingDocs ? 'Salvando...' : 'Salvar CPF e CNH' }}
            </button>
          </form>
        </section>

        <section class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm">
          <div class="flex items-center gap-2">
            <Bike class="h-5 w-5 text-brand-600" />
            <h2 class="text-base font-bold text-stone-900">
              Meu Veículo (Obrigatório)
            </h2>
          </div>
          <p class="mt-1 text-xs text-stone-500">
            Cadastre o veículo que você utilizará para realizar as entregas.
          </p>

          <ul v-if="vehicles.length > 0" class="mt-3 space-y-2 text-sm text-stone-700">
            <li
              v-for="v in vehicles"
              :key="v.id"
              class="flex items-center justify-between rounded-lg bg-stone-50 px-3 py-2"
            >
              <span>{{ v.type }} — {{ v.model }}</span>
              <span class="font-mono text-xs font-semibold text-stone-600">
                {{ v.licensePlate }}
              </span>
            </li>
          </ul>

          <form class="mt-4 space-y-3" @submit.prevent="handleAddVehicle">
            <div class="grid grid-cols-3 gap-2">
              <div>
                <label class="block text-xs font-medium text-stone-700">Tipo *</label>
                <select
                  v-model="vehicleType"
                  class="mt-1 w-full rounded-lg border border-stone-300 bg-white px-2.5 py-2 text-sm"
                >
                  <option value="Moto">Moto</option>
                  <option value="Bicicleta">Bicicleta</option>
                  <option value="Carro">Carro</option>
                </select>
              </div>
              <div class="col-span-2">
                <label class="block text-xs font-medium text-stone-700">
                  Modelo <span class="text-red-500">*</span>
                </label>
                <input
                  v-model="vehicleModel"
                  type="text"
                  required
                  placeholder="Ex: Honda CG 160"
                  class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
                />
              </div>
            </div>

            <div>
              <label class="block text-xs font-medium text-stone-700">
                Placa do Veículo <span class="text-red-500">*</span>
              </label>
              <input
                v-model="vehiclePlate"
                type="text"
                required
                placeholder="Ex: ABC1D23"
                class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm uppercase focus:border-brand-600 focus:outline-none"
              />
            </div>

            <button
              type="submit"
              :disabled="savingVehicle"
              class="w-full rounded-lg bg-stone-900 px-4 py-2 text-xs font-semibold text-white hover:bg-stone-800 disabled:opacity-50"
            >
              {{ savingVehicle ? 'Salvando...' : 'Cadastrar veículo' }}
            </button>
          </form>
        </section>
      </div>

      <!-- Endereço de Cobrança do Entregador (Obrigatório, sem ser endereço de entrega) -->
      <section class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm">
        <div class="flex flex-col justify-between gap-2 sm:flex-row sm:items-center">
          <div>
            <div class="flex items-center gap-2">
              <MapPin class="h-5 w-5 text-brand-600" />
              <h2 class="text-base font-bold text-stone-900">
                Endereço de Cobrança / Faturamento do Entregador (Obrigatório)
              </h2>
            </div>
            <p class="mt-1 text-xs text-stone-500">
              Este endereço é de uso exclusivo para cobrança fiscal e repasse financeiro do entregador (não é endereço de entrega de pedidos).
            </p>
          </div>
          <span
            v-if="hasBillingAddress"
            class="inline-flex items-center gap-1 self-start rounded-full bg-green-100 px-2.5 py-0.5 text-xs font-semibold text-green-800"
          >
            <CheckCircle2 class="h-3.5 w-3.5" />
            Endereço de Cobrança Salvo
          </span>
        </div>

        <form class="mt-4 grid gap-4 sm:grid-cols-6" @submit.prevent="handleSaveBillingAddress">
          <div class="sm:col-span-2">
            <label class="block text-xs font-medium text-stone-700">
              CEP <span class="text-red-500">*</span>
            </label>
            <input
              v-model="billingZipCode"
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
              v-model="billingStreet"
              type="text"
              required
              placeholder="Rua Exemplo"
              class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
            />
          </div>

          <div class="sm:col-span-1">
            <label class="block text-xs font-medium text-stone-700">
              Número <span class="text-red-500">*</span>
            </label>
            <input
              v-model="billingNumber"
              type="text"
              required
              placeholder="123"
              class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
            />
          </div>

          <div class="sm:col-span-2">
            <label class="block text-xs font-medium text-stone-700">
              Bairro <span class="text-red-500">*</span>
            </label>
            <input
              v-model="billingNeighborhood"
              type="text"
              required
              placeholder="Ex: Centro"
              class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
            />
          </div>

          <div class="sm:col-span-2">
            <label class="block text-xs font-medium text-stone-700">
              Cidade <span class="text-red-500">*</span>
            </label>
            <input
              v-model="billingCity"
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
              v-model="billingState"
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
              v-model="billingComplement"
              type="text"
              placeholder="Apto / Sala"
              class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
            />
          </div>

          <div class="sm:col-span-6">
            <button
              type="submit"
              :disabled="savingBilling"
              class="w-full rounded-lg bg-brand-600 px-4 py-2.5 text-xs font-semibold text-white hover:bg-brand-700 disabled:opacity-50 sm:w-auto"
            >
              {{ savingBilling ? 'Salvando endereço de cobrança...' : 'Salvar endereço de cobrança' }}
            </button>
          </div>
        </form>
      </section>
    </main>
  </div>
</template>
