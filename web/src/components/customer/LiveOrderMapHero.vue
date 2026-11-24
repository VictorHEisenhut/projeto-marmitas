<script setup lang="ts">
import { computed, ref } from 'vue'
import {
  Clock,
  ExternalLink,
  KeyRound,
  Maximize2,
  Navigation,
  Truck,
} from 'lucide-vue-next'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppModal from '@/components/ui/AppModal.vue'
import OrderStatusBadge from '@/components/ui/OrderStatusBadge.vue'
import type { DeliveryTracking, Order } from '@/types'

const props = defineProps<{
  order: Order
  trackingInfo: DeliveryTracking | null
}>()

const fullscreenPinOpen = ref(false)

const stepperStages = [
  { key: 'paid', label: 'Pago' },
  { key: 'confirmed', label: 'Confirmado' },
  { key: 'preparing', label: 'Preparo' },
  { key: 'ready', label: 'Pronto' },
  { key: 'transit', label: 'Em rota' },
  { key: 'delivered', label: 'Entregue' },
]

const currentStepIndex = computed(() => {
  const s = props.order.status
  if (s === 'Delivered') return 5
  if (s === 'InTransit' || s === 'PickedUp' || s === 'DriverAssigned') return 4
  if (s === 'ReadyForPickup') return 3
  if (s === 'Preparing') return 2
  if (s === 'Confirmed') return 1
  return 0
})

const mapLat = computed(() => props.trackingInfo?.latitude ?? -26.9194)
const mapLon = computed(() => props.trackingInfo?.longitude ?? -49.0661)

const mapEmbedUrl = computed(() => {
  const delta = 0.007
  const bbox = `${mapLon.value - delta},${mapLat.value - delta},${mapLon.value + delta},${mapLat.value + delta}`
  return `https://www.openstreetmap.org/export/embed.html?bbox=${encodeURIComponent(bbox)}&layer=mapnik&marker=${mapLat.value},${mapLon.value}`
})

const pinDigits = computed(() => (props.order.confirmationPin || '').slice(0, 4).split(''))

const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i
const courierDisplayName = computed(() => {
  const name = props.trackingInfo?.courierName?.trim()
  if (!name || uuidPattern.test(name)) return null
  return name
})
</script>

<template>
  <section class="relative overflow-hidden rounded-3xl border border-line bg-surface shadow-elevated">
    <!-- Full-Bleed Map Background Layer -->
    <div class="relative h-[320px] sm:h-[420px] w-full bg-surface-muted">
      <iframe
        :key="`${mapLat}-${mapLon}`"
        :src="mapEmbedUrl"
        title="Mapa de rastreamento em tempo real"
        class="h-full w-full border-0"
        loading="lazy"
      />

      <!-- Top Right Live Signal + Google Maps Floating Pills -->
      <div class="absolute right-3 top-3 z-10 flex flex-wrap items-center gap-2">
        <span class="inline-flex items-center gap-1.5 rounded-full bg-surface/95 px-3 py-1.5 text-xs font-bold text-ink shadow-md backdrop-blur-md">
          <span class="h-2 w-2 rounded-full bg-fresh-500 animate-pulse" />
          Ao vivo
        </span>
        <a
          :href="`https://www.google.com/maps?q=${mapLat},${mapLon}`"
          target="_blank"
          rel="noopener noreferrer"
          class="inline-flex items-center gap-1.5 rounded-full bg-surface/95 px-3 py-1.5 text-xs font-bold text-brand-600 shadow-md backdrop-blur-md transition hover:bg-surface dark:text-brand-400"
        >
          <Navigation class="h-3.5 w-3.5" />
          <span>Google Maps</span>
          <ExternalLink class="h-3 w-3" />
        </a>
      </div>

      <!-- Desktop Floating Uber Eats-style Activity Card -->
      <div class="hidden sm:block absolute bottom-4 left-4 z-10 w-[400px] max-w-[calc(100%-2rem)] rounded-2xl border border-line bg-surface/95 p-5 shadow-elevated backdrop-blur-md space-y-4">
        <div class="flex items-start justify-between gap-3">
          <div>
            <div class="flex items-center gap-2">
              <OrderStatusBadge :status="order.status" />
              <span class="text-[11px] font-semibold text-ink-muted">
                #{{ order.id.slice(0, 8) }}
              </span>
            </div>
            <h2 class="mt-1.5 font-display text-xl font-extrabold text-ink leading-tight">
              <template v-if="trackingInfo?.estimatedMinutes && trackingInfo.estimatedMinutes > 0">
                Chega em ~{{ trackingInfo.estimatedMinutes }} min
              </template>
              <template v-else>
                {{ order.restaurantName }}
              </template>
            </h2>
            <p class="mt-0.5 text-xs text-ink-secondary">
              <template v-if="trackingInfo?.estimatedArrivalAt">
                Previsto às
                <strong class="text-ink tabular-nums">
                  {{ new Date(trackingInfo.estimatedArrivalAt).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' }) }}
                </strong>
                ·
              </template>
              {{ order.restaurantName }}
            </p>
          </div>

          <span class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-brand-600 text-white shadow-sm">
            <Truck class="h-5 w-5" />
          </span>
        </div>

        <!-- Animated Progress Bar -->
        <div class="grid grid-cols-6 gap-1">
          <div
            v-for="(stage, idx) in stepperStages"
            :key="stage.key"
            class="space-y-1"
          >
            <div
              class="h-1.5 rounded-full transition-all"
              :class="
                idx < currentStepIndex
                  ? 'bg-fresh-500'
                  : idx === currentStepIndex
                    ? 'bg-brand-600 animate-pulse'
                    : 'bg-surface-muted'
              "
            />
            <p
              class="truncate text-center text-[9px] font-bold"
              :class="idx <= currentStepIndex ? 'text-ink' : 'text-ink-muted'"
            >
              {{ stage.label }}
            </p>
          </div>
        </div>

        <!-- Courier + PIN Strip -->
        <div class="flex items-center justify-between gap-3 rounded-xl border border-amber-300/80 bg-amber-50/80 px-3.5 py-2.5 dark:border-amber-700/60 dark:bg-amber-950/35">
          <div class="min-w-0">
            <p class="text-[10px] font-bold uppercase tracking-wider text-amber-900 dark:text-amber-300">
              PIN de entrega
            </p>
            <p v-if="courierDisplayName" class="truncate text-xs font-semibold text-ink">
              Entregador: {{ courierDisplayName }}
            </p>
            <p v-else class="text-[11px] text-amber-900/80 dark:text-amber-300/80">
              Informe ao receber
            </p>
          </div>

          <div v-if="order.confirmationPin" class="flex items-center gap-2 shrink-0">
            <span class="font-mono text-lg font-extrabold tracking-[0.2em] text-ink">
              {{ order.confirmationPin }}
            </span>
            <button
              type="button"
              aria-label="Ampliar PIN em tela cheia"
              title="Ampliar PIN"
              class="flex h-8 w-8 items-center justify-center rounded-lg border border-amber-300 bg-surface text-ink transition hover:bg-amber-100 dark:border-amber-700"
              @click="fullscreenPinOpen = true"
            >
              <Maximize2 class="h-3.5 w-3.5" />
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Mobile Sheet Summary Below Map -->
    <div class="sm:hidden border-t border-line bg-surface p-4 space-y-4">
      <div class="flex items-start justify-between gap-2">
        <div>
          <div class="flex items-center gap-2">
            <OrderStatusBadge :status="order.status" />
            <span class="text-xs text-ink-muted">#{{ order.id.slice(0, 8) }}</span>
          </div>
          <h2 class="mt-1 font-display text-lg font-extrabold text-ink">
            <template v-if="trackingInfo?.estimatedMinutes">
              Chega em ~{{ trackingInfo.estimatedMinutes }} min
            </template>
            <template v-else>
              {{ order.restaurantName }}
            </template>
          </h2>
        </div>

        <AppButton
          v-if="order.confirmationPin"
          variant="outline"
          size="xs"
          @click="fullscreenPinOpen = true"
        >
          <KeyRound class="h-3.5 w-3.5 text-amber-600" />
          PIN {{ order.confirmationPin }}
        </AppButton>
      </div>

      <div class="grid grid-cols-6 gap-1">
        <div
          v-for="(stage, idx) in stepperStages"
          :key="stage.key"
          class="h-1.5 rounded-full"
          :class="
            idx < currentStepIndex
              ? 'bg-fresh-500'
              : idx === currentStepIndex
                ? 'bg-brand-600 animate-pulse'
                : 'bg-surface-muted'
          "
        />
      </div>
    </div>

    <!-- Route Stops Strip -->
    <div
      v-if="trackingInfo?.routeStops && trackingInfo.routeStops.length > 0"
      class="flex flex-wrap items-center gap-3 border-t border-line bg-surface-subtle px-5 py-3 text-xs"
    >
      <span class="inline-flex items-center gap-1.5 font-bold text-ink">
        <Clock class="h-3.5 w-3.5 text-brand-600" />
        Rota:
      </span>
      <div
        v-for="(stop, idx) in trackingInfo.routeStops"
        :key="idx"
        class="inline-flex items-center gap-1.5 text-ink-secondary"
      >
        <AppBadge
          :variant="stop.type === 'Destination' ? 'fresh' : stop.type === 'IntermediateStop' ? 'warning' : 'brand'"
          size="xs"
        >
          {{ idx + 1 }}. {{ stop.label }}
        </AppBadge>
        <span class="truncate max-w-[180px]">{{ stop.address }}</span>
      </div>
    </div>

    <!-- Fullscreen High-Contrast PIN Modal -->
    <AppModal
      :open="fullscreenPinOpen"
      title="Código PIN de Entrega"
      subtitle="Mostre este código ao entregador ou à portaria ao receber sua marmita."
      max-width="sm"
      :sheet-on-mobile="false"
      @close="fullscreenPinOpen = false"
    >
      <div class="py-6 text-center space-y-5">
        <p class="text-xs font-bold uppercase tracking-wider text-ink-muted">
          {{ order.restaurantName }} · Pedido #{{ order.id.slice(0, 8) }}
        </p>

        <div class="flex items-center justify-center gap-3">
          <span
            v-for="(digit, dIdx) in pinDigits"
            :key="dIdx"
            class="flex h-20 w-16 items-center justify-center rounded-2xl border-2 border-brand-600 bg-brand-50 font-mono text-4xl font-extrabold text-brand-900 shadow-md dark:bg-brand-950/60 dark:text-brand-200"
          >
            {{ digit }}
          </span>
        </div>

        <p class="text-xs text-ink-secondary">
          Não compartilhe o código antes de confirmar o recebimento do pedido.
        </p>
      </div>

      <template #footer>
        <AppButton variant="primary" size="lg" block @click="fullscreenPinOpen = false">
          Fechar tela do PIN
        </AppButton>
      </template>
    </AppModal>
  </section>
</template>
