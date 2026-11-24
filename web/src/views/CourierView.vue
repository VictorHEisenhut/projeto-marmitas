<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { Bike, CheckCircle2, DollarSign, Lock, Navigation, Power } from 'lucide-vue-next'
import CourierLayout from '@/layouts/CourierLayout.vue'
import CourierOnboardingChecklist from '@/components/courier/CourierOnboardingChecklist.vue'
import CourierDeliveryCard from '@/components/courier/CourierDeliveryCard.vue'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppSkeleton from '@/components/ui/AppSkeleton.vue'
import AppEmptyState from '@/components/ui/AppEmptyState.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import { OrderTrackingClient } from '@/services/realtime'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'
import type {
  Courier,
  CourierBillingAddress,
  CourierDocument,
  CourierEarnings,
  CourierVehicle,
  Delivery,
} from '@/types'

const auth = useAuthStore()
const toast = useToastStore()

const isAdminUser = computed(() => auth.hasRole(['admin', 'super_admin']))

const activeTab = ref<'queue' | 'onboarding'>('queue')
const earnings = ref<CourierEarnings | null>(null)
const deliveries = ref<Delivery[]>([])
const documents = ref<CourierDocument[]>([])
const vehicles = ref<CourierVehicle[]>([])
const billing = ref<CourierBillingAddress | null>(null)
const billingSaved = ref(false)

const isAvailable = ref(true)
const loading = ref(true)
const toggling = ref(false)
const sendingGps = ref(false)
const busyDeliveryId = ref<string | null>(null)
const lastGpsCoords = ref<{ latitude: number; longitude: number; source: string } | null>(null)

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

const realtimeClient = new OrderTrackingClient({
  onDeliveryUpdated(payload) {
    const target = deliveries.value.find(
      (d) => d.id === payload.deliveryId || d.orderId === payload.orderId,
    )
    if (target) {
      target.status = payload.status
    } else if (isRegistrationComplete.value) {
      void loadCourierData()
      toast.info('Nova corrida disponível na fila!')
    }
  },
})

function getBrowserCoordinates(): Promise<{ latitude: number; longitude: number; source: string }> {
  return new Promise((resolve) => {
    if (typeof navigator === 'undefined' || !navigator.geolocation) {
      resolve({
        latitude: -23.5614,
        longitude: -46.6559,
        source: 'padrão (navegador sem GPS)',
      })
      return
    }
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        resolve({
          latitude: pos.coords.latitude,
          longitude: pos.coords.longitude,
          source: 'GPS do dispositivo',
        })
      },
      () => {
        resolve({
          latitude: -23.5614 + (Math.random() - 0.5) * 0.006,
          longitude: -46.6559 + (Math.random() - 0.5) * 0.006,
          source: 'simulada (permissão GPS indisponível)',
        })
      },
      { enableHighAccuracy: true, timeout: 6000, maximumAge: 10000 },
    )
  })
}

async function handleSendGps() {
  sendingGps.value = true
  try {
    const coords = await getBrowserCoordinates()
    const activeDelivery = deliveries.value.find((d) =>
      ['DriverAssigned', 'PickedUp', 'InTransit'].includes(d.status),
    )

    await api.patch('/entregadores/me/localizacao', {
      latitude: coords.latitude,
      longitude: coords.longitude,
      deliveryId: activeDelivery?.id,
      orderId: activeDelivery?.orderId,
    })

    lastGpsCoords.value = coords
    toast.success(`GPS atualizado (${coords.source})!`)
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível enviar a localização GPS.'))
  } finally {
    sendingGps.value = false
  }
}

async function loadCourierData() {
  loading.value = true
  try {
    const [earnRes, docRes, vehRes, billRes] = await Promise.allSettled([
      api.get<CourierEarnings>('/entregadores/me/ganhos'),
      api.get<CourierDocument[]>('/entregadores/me/documentos'),
      api.get<CourierVehicle[]>('/entregadores/me/veiculos'),
      api.get<CourierBillingAddress>('/entregadores/me/endereco-cobranca'),
    ])

    if (earnRes.status === 'fulfilled') earnings.value = earnRes.value.data
    if (docRes.status === 'fulfilled') documents.value = docRes.value.data
    if (vehRes.status === 'fulfilled') vehicles.value = vehRes.value.data
    if (billRes.status === 'fulfilled' && billRes.value.data) {
      const b = billRes.value.data
      billing.value = b
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

    if (hasCpf.value && hasCnh.value && hasVehicle.value && hasBillingAddress.value) {
      const { data } = await api.get<Delivery[]>('/entregas')
      deliveries.value = data
    } else {
      deliveries.value = []
    }
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível carregar os dados do entregador.'))
  } finally {
    loading.value = false
  }
}

async function handleToggleAvailability() {
  toggling.value = true
  try {
    const { data } = await api.patch<Courier>('/entregadores/me/disponibilidade', {
      isAvailable: !isAvailable.value,
    })
    isAvailable.value = data.isAvailable
    toast.info(data.isAvailable ? 'Você está ONLINE para receber corridas.' : 'Você ficou OFFLINE.')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível alterar disponibilidade.'))
  } finally {
    toggling.value = false
  }
}

async function handleAcceptDelivery(id: string) {
  if (!isRegistrationComplete.value) {
    toast.warning('Complete CPF, CNH, Veículo e Endereço de Cobrança antes de aceitar entregas.')
    activeTab.value = 'onboarding'
    return
  }

  busyDeliveryId.value = id
  try {
    await api.patch(`/entregas/${id}/aceitar`)
    toast.success('Entrega aceita! Dirija-se ao restaurante para coleta.')
    await loadCourierData()
  } catch (err) {
    toast.error(getErrorMessage(err, 'Erro ao aceitar entrega.'))
  } finally {
    busyDeliveryId.value = null
  }
}

async function handleUpdateDeliveryStatus(payload: { id: string; status: string; pin?: string }) {
  if (payload.status === 'Delivered' && !payload.pin && !isAdminUser.value) {
    toast.warning('Informe o código PIN de 4 dígitos do cliente para concluir a entrega.')
    return
  }

  busyDeliveryId.value = payload.id
  try {
    await api.patch(`/entregas/${payload.id}/status`, {
      status: payload.status,
      confirmationPin: payload.status === 'Delivered' ? payload.pin : undefined,
    })
    toast.success(
      payload.status === 'Delivered'
        ? 'Entrega concluída com sucesso! Repasse creditado.'
        : 'Status da corrida atualizado!',
    )
    await loadCourierData()
  } catch (err) {
    toast.error(getErrorMessage(err, 'Erro ao atualizar status da entrega.'))
  } finally {
    busyDeliveryId.value = null
  }
}

onMounted(() => {
  void realtimeClient.connect()
  void loadCourierData()
})

onUnmounted(() => {
  realtimeClient.disconnect()
})
</script>

<template>
  <CourierLayout
    v-model:active-tab="activeTab"
    :is-online="isAvailable && isRegistrationComplete"
    :loading="loading"
    @refresh="loadCourierData"
  >
    <div class="space-y-5">
      <!-- Operational Status Bar (Online/Offline + Live GPS + Earnings Summary) -->
      <AppCard padding="md" class="space-y-4">
        <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <!-- High-Contrast Online / Offline Toggle Button (>= 48px) -->
          <button
            type="button"
            :disabled="toggling || !isRegistrationComplete"
            class="inline-flex h-12 items-center justify-center gap-2.5 rounded-2xl px-6 text-sm font-extrabold transition shadow-sm disabled:opacity-50"
            :class="
              isAvailable && isRegistrationComplete
                ? 'bg-fresh-600 text-white hover:bg-fresh-700'
                : 'border-2 border-line-strong bg-surface-subtle text-ink hover:bg-surface-muted'
            "
            @click="handleToggleAvailability"
          >
            <Power class="h-5 w-5" />
            <span>
              {{
                !isRegistrationComplete
                  ? 'Homologação Pendente'
                  : isAvailable
                    ? 'ONLINE · Recebendo corridas'
                    : 'OFFLINE · Ficar disponível'
              }}
            </span>
          </button>

          <div class="flex flex-wrap items-center gap-2">
            <AppButton
              variant="outline"
              size="lg"
              class="flex-1 sm:flex-none"
              :disabled="!isRegistrationComplete"
              :loading="sendingGps"
              @click="handleSendGps"
            >
              <Navigation class="h-4 w-4 text-brand-600" />
              Atualizar GPS ao vivo
            </AppButton>

            <AppButton
              variant="ghost"
              size="lg"
              class="hidden md:inline-flex"
              @click="activeTab = activeTab === 'queue' ? 'onboarding' : 'queue'"
            >
              {{ activeTab === 'queue' ? 'Ver Homologação' : 'Voltar às Corridas' }}
            </AppButton>
          </div>
        </div>

        <!-- Last GPS Feedback -->
        <p v-if="lastGpsCoords" class="text-xs text-ink-muted tabular-nums">
          Última posição enviada ({{ lastGpsCoords.source }}):
          <strong class="text-ink">{{ lastGpsCoords.latitude.toFixed(5) }}, {{ lastGpsCoords.longitude.toFixed(5) }}</strong>
        </p>

        <!-- Earnings & Completed Deliveries Bar -->
        <div v-if="earnings" class="grid grid-cols-2 gap-3 border-t border-line pt-4">
          <div class="rounded-2xl bg-surface-subtle p-3.5 border border-line">
            <div class="flex items-center gap-1.5 text-[11px] font-bold uppercase text-ink-muted">
              <CheckCircle2 class="h-4 w-4 text-fresh-600" />
              <span>Entregas concluídas</span>
            </div>
            <p class="mt-1 font-display text-2xl font-extrabold text-ink tabular-nums">
              {{ earnings.completedDeliveries }}
            </p>
          </div>
          <div class="rounded-2xl bg-surface-subtle p-3.5 border border-line">
            <div class="flex items-center gap-1.5 text-[11px] font-bold uppercase text-ink-muted">
              <DollarSign class="h-4 w-4 text-fresh-600" />
              <span>Total acumulado</span>
            </div>
            <p class="mt-1 font-display text-2xl font-extrabold text-fresh-700 dark:text-fresh-400 tabular-nums">
              {{ formatCurrency(earnings.totalEarnings) }}
            </p>
          </div>
        </div>
      </AppCard>

      <!-- Desktop/Mobile View Switch: Queue vs Onboarding -->
      <div v-if="loading" class="space-y-4">
        <AppSkeleton variant="card" height="140px" />
        <AppSkeleton variant="card" height="200px" />
      </div>

      <!-- Onboarding / Homologation View -->
      <CourierOnboardingChecklist
        v-else-if="activeTab === 'onboarding'"
        :documents="documents"
        :vehicles="vehicles"
        :billing="billing"
        :has-cpf="hasCpf"
        :has-cnh="hasCnh"
        :has-vehicle="hasVehicle"
        :has-billing-address="hasBillingAddress"
        @refresh="loadCourierData"
      />

      <!-- Queue & Active Deliveries View -->
      <section v-else class="space-y-4">
        <!-- Blocked Queue Notice if Incomplete -->
        <template v-if="!isRegistrationComplete">
          <AppCard
            padding="lg"
            class="border-amber-300 bg-amber-50/70 dark:border-amber-700/60 dark:bg-amber-950/30"
          >
            <div class="flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-center">
              <div class="flex items-start gap-3">
                <Lock class="mt-0.5 h-6 w-6 shrink-0 text-amber-700 dark:text-amber-400" />
                <div class="space-y-1">
                  <h2 class="font-display text-base font-extrabold text-amber-950 dark:text-amber-200">
                    Fila de entregas bloqueada
                  </h2>
                  <p class="text-xs leading-relaxed text-amber-900/90 dark:text-amber-300/90">
                    Para visualizar e aceitar corridas, finalize o cadastro obrigatório de
                    <strong>CPF, CNH, Veículo e Endereço de Cobrança</strong>.
                  </p>
                </div>
              </div>
              <AppButton variant="primary" size="md" @click="activeTab = 'onboarding'">
                Completar homologação
              </AppButton>
            </div>
          </AppCard>

          <CourierOnboardingChecklist
            :documents="documents"
            :vehicles="vehicles"
            :billing="billing"
            :has-cpf="hasCpf"
            :has-cnh="hasCnh"
            :has-vehicle="hasVehicle"
            :has-billing-address="hasBillingAddress"
            @refresh="loadCourierData"
          />
        </template>

        <!-- Unlocked Delivery Queue -->
        <template v-else>
          <div class="flex items-center justify-between">
            <h2 class="font-display text-lg font-extrabold text-ink">
              Fila de Entregas & Corridas ({{ deliveries.length }})
            </h2>
          </div>

          <div v-if="deliveries.length > 0" class="space-y-4">
            <CourierDeliveryCard
              v-for="delivery in deliveries"
              :key="delivery.id"
              :delivery="delivery"
              :is-admin-user="isAdminUser"
              :busy="busyDeliveryId === delivery.id"
              @accept="handleAcceptDelivery"
              @update-status="handleUpdateDeliveryStatus"
            />
          </div>

          <AppEmptyState
            v-else
            :icon="Bike"
            title="Nenhuma corrida disponível agora"
            description="Mantenha-se online e com o GPS atualizado. Novas corridas aparecerão aqui automaticamente."
          />
        </template>
      </section>
    </div>
  </CourierLayout>
</template>
