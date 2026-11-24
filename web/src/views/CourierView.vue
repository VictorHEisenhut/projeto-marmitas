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

const mapEmbedUrl = computed(() => {
  const lat = lastGpsCoords.value?.latitude ?? -26.9194
  const lon = lastGpsCoords.value?.longitude ?? -49.0661
  const delta = 0.008
  const bbox = `${lon - delta},${lat - delta},${lon + delta},${lat + delta}`
  return `https://www.openstreetmap.org/export/embed.html?bbox=${encodeURIComponent(bbox)}&layer=mapnik&marker=${lat},${lon}`
})

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
        latitude: -26.9194,
        longitude: -49.0661,
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
          latitude: -26.9194 + (Math.random() - 0.5) * 0.006,
          longitude: -49.0661 + (Math.random() - 0.5) * 0.006,
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
      <!-- Map-First Full-Bleed Header with Floating HUD when Registered -->
      <section
        v-if="isRegistrationComplete"
        class="relative overflow-hidden rounded-3xl border border-line bg-surface shadow-elevated"
      >
        <div class="relative h-[240px] sm:h-[300px] w-full bg-surface-muted">
          <iframe
            :key="`${lastGpsCoords?.latitude ?? 0}-${lastGpsCoords?.longitude ?? 0}`"
            :src="mapEmbedUrl"
            title="Mapa operacional do entregador"
            class="h-full w-full border-0"
            loading="lazy"
          />

          <!-- Top Floating HUD Pills -->
          <div class="absolute inset-x-3 top-3 z-10 flex flex-wrap items-center justify-between gap-2">
            <!-- Earnings Pill -->
            <div
              v-if="earnings"
              class="inline-flex items-center gap-3 rounded-2xl border border-line bg-surface/95 px-3.5 py-2 shadow-md backdrop-blur-md"
            >
              <span class="inline-flex items-center gap-1 font-display text-sm font-extrabold text-fresh-700 dark:text-fresh-400 tabular-nums">
                <DollarSign class="h-4 w-4" />
                {{ formatCurrency(earnings.totalEarnings) }}
              </span>
              <span class="h-4 w-px bg-line" />
              <span class="inline-flex items-center gap-1 text-xs font-bold text-ink tabular-nums">
                <CheckCircle2 class="h-3.5 w-3.5 text-fresh-600" />
                {{ earnings.completedDeliveries }} entr.
              </span>
            </div>

            <!-- Online / Offline + GPS Floating Controls -->
            <div class="flex flex-wrap items-center gap-2">
              <button
                type="button"
                :disabled="toggling"
                class="inline-flex h-10 items-center gap-2 rounded-2xl px-4 text-xs font-extrabold shadow-md transition"
                :class="
                  isAvailable
                    ? 'bg-fresh-600 text-white hover:bg-fresh-700'
                    : 'border border-line bg-surface/95 text-ink hover:bg-surface'
                "
                @click="handleToggleAvailability"
              >
                <Power class="h-4 w-4" />
                <span>{{ isAvailable ? 'ONLINE' : 'OFFLINE' }}</span>
              </button>

              <button
                type="button"
                :disabled="sendingGps"
                class="inline-flex h-10 items-center gap-1.5 rounded-2xl border border-line bg-surface/95 px-3.5 text-xs font-bold text-ink shadow-md backdrop-blur-md transition hover:bg-surface disabled:opacity-60"
                @click="handleSendGps"
              >
                <Navigation class="h-4 w-4 text-brand-600" :class="{ 'animate-spin': sendingGps }" />
                <span>{{ sendingGps ? 'Enviando...' : 'GPS ao vivo' }}</span>
              </button>
            </div>
          </div>

          <!-- Bottom Floating Coordinates & Desktop Mode Switch -->
          <div class="absolute inset-x-3 bottom-3 z-10 flex items-center justify-between gap-2">
            <span
              v-if="lastGpsCoords"
              class="rounded-full bg-surface/90 px-3 py-1 text-[11px] font-semibold text-ink shadow-sm backdrop-blur-md tabular-nums"
            >
              {{ lastGpsCoords.source }} · {{ lastGpsCoords.latitude.toFixed(4) }}, {{ lastGpsCoords.longitude.toFixed(4) }}
            </span>
            <span v-else />

            <AppButton
              variant="secondary"
              size="xs"
              class="hidden md:inline-flex shadow-md"
              @click="activeTab = activeTab === 'queue' ? 'onboarding' : 'queue'"
            >
              {{ activeTab === 'queue' ? 'Meus Documentos' : 'Voltar às Corridas' }}
            </AppButton>
          </div>
        </div>
      </section>

      <!-- Loading Skeletons -->
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
