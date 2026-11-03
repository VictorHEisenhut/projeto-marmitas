<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { Bike, CheckCircle2, DollarSign, RefreshCw } from 'lucide-vue-next'
import Navbar from '@/components/Navbar.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import type {
  Courier,
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

const docType = ref('CNH')
const docNumber = ref('')
const savingDoc = ref(false)

const vehicleType = ref('Moto')
const vehicleModel = ref('')
const vehiclePlate = ref('')
const savingVehicle = ref(false)

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
    const [earnRes, delivRes, docRes, vehRes] = await Promise.allSettled([
      api.get<CourierEarnings>('/entregadores/me/ganhos'),
      api.get<Delivery[]>('/entregas'),
      api.get<CourierDocument[]>('/entregadores/me/documentos'),
      api.get<CourierVehicle[]>('/entregadores/me/veiculos'),
    ])

    if (earnRes.status === 'fulfilled') earnings.value = earnRes.value.data
    if (delivRes.status === 'fulfilled') deliveries.value = delivRes.value.data
    if (docRes.status === 'fulfilled') documents.value = docRes.value.data
    if (vehRes.status === 'fulfilled') vehicles.value = vehRes.value.data
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

async function handleAddDocument() {
  errorMessage.value = ''
  successMessage.value = ''

  if (!docNumber.value.trim()) {
    errorMessage.value = 'Informe o número do documento.'
    return
  }

  savingDoc.value = true
  try {
    const { data } = await api.post<CourierDocument>('/entregadores/me/documentos', {
      type: docType.value,
      number: docNumber.value.trim(),
    })
    documents.value.push(data)
    docNumber.value = ''
    successMessage.value = 'Documento registrado.'
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Erro ao salvar documento.')
  } finally {
    savingDoc.value = false
  }
}

async function handleAddVehicle() {
  errorMessage.value = ''
  successMessage.value = ''

  if (!vehicleModel.value.trim() || !vehiclePlate.value.trim()) {
    errorMessage.value = 'Informe o modelo e a placa do veículo.'
    return
  }

  savingVehicle.value = true
  try {
    const { data } = await api.post<CourierVehicle>('/entregadores/me/veiculos', {
      type: vehicleType.value,
      model: vehicleModel.value.trim(),
      licensePlate: vehiclePlate.value.trim().toUpperCase(),
    })
    vehicles.value.push(data)
    vehicleModel.value = ''
    vehiclePlate.value = ''
    successMessage.value = 'Veículo cadastrado.'
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Erro ao cadastrar veículo.')
  } finally {
    savingVehicle.value = false
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
            Consulte seus ganhos, aceite entregas e gerencie seus dados.
          </p>
        </div>

        <div class="flex items-center gap-3">
          <button
            type="button"
            :disabled="toggling"
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

      <section class="space-y-4">
        <h2 class="text-lg font-bold text-stone-900">Fila de entregas</h2>

        <p v-if="loading" class="text-sm text-stone-500">Carregando entregas...</p>

        <div v-else-if="deliveries.length > 0" class="space-y-3">
          <div
            v-for="delivery in deliveries"
            :key="delivery.id"
            class="flex flex-col justify-between gap-4 rounded-xl border border-stone-200 bg-white p-5 shadow-sm sm:flex-row sm:items-center"
          >
            <div>
              <div class="flex items-center gap-2">
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
              <p class="mt-1 text-xs text-stone-500">ID da entrega: {{ delivery.id }}</p>
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

      <div class="grid gap-6 sm:grid-cols-2">
        <section class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm">
          <h2 class="text-base font-bold text-stone-900">Meus documentos</h2>

          <ul v-if="documents.length > 0" class="mt-3 space-y-2 text-sm text-stone-700">
            <li
              v-for="doc in documents"
              :key="doc.id"
              class="flex items-center justify-between rounded-lg bg-stone-50 px-3 py-2"
            >
              <span>{{ doc.type }}: {{ doc.number }}</span>
              <span class="text-xs font-medium text-green-700">{{ doc.status }}</span>
            </li>
          </ul>

          <form class="mt-4 space-y-3" @submit.prevent="handleAddDocument">
            <div class="grid grid-cols-3 gap-2">
              <select
                v-model="docType"
                class="rounded-lg border border-stone-300 bg-white px-2.5 py-2 text-sm"
              >
                <option value="CNH">CNH</option>
                <option value="CPF">CPF</option>
                <option value="RG">RG</option>
              </select>
              <input
                v-model="docNumber"
                type="text"
                placeholder="Número do documento"
                class="col-span-2 rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
              />
            </div>

            <button
              type="submit"
              :disabled="savingDoc"
              class="w-full rounded-lg bg-stone-900 px-4 py-2 text-xs font-semibold text-white hover:bg-stone-800 disabled:opacity-50"
            >
              {{ savingDoc ? 'Salvando...' : 'Adicionar documento' }}
            </button>
          </form>
        </section>

        <section class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm">
          <h2 class="text-base font-bold text-stone-900">Meus veículos</h2>

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
              <select
                v-model="vehicleType"
                class="rounded-lg border border-stone-300 bg-white px-2.5 py-2 text-sm"
              >
                <option value="Moto">Moto</option>
                <option value="Bicicleta">Bicicleta</option>
                <option value="Carro">Carro</option>
              </select>
              <input
                v-model="vehicleModel"
                type="text"
                placeholder="Modelo (ex: CG 160)"
                class="col-span-2 rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
              />
            </div>

            <input
              v-model="vehiclePlate"
              type="text"
              placeholder="Placa (ex: ABC1D23)"
              class="w-full rounded-lg border border-stone-300 px-3 py-2 text-sm uppercase focus:border-brand-600 focus:outline-none"
            />

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
    </main>
  </div>
</template>
