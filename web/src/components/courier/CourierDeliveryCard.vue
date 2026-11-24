<script setup lang="ts">
import { computed, ref } from 'vue'
import {
  Bike,
  CheckCircle2,
  Clock,
  Delete,
  ExternalLink,
  KeyRound,
  MapPin,
  Navigation,
  PackageCheck,
  Store,
} from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppModal from '@/components/ui/AppModal.vue'
import OrderStatusBadge from '@/components/ui/OrderStatusBadge.vue'
import OtpInput from '@/components/ui/OtpInput.vue'
import { formatCurrency } from '@/services/api'
import type { Delivery } from '@/types'

const props = defineProps<{
  delivery: Delivery
  isAdminUser: boolean
  busy?: boolean
}>()

const emit = defineEmits<{
  (e: 'accept', id: string): void
  (e: 'update-status', payload: { id: string; status: string; pin?: string }): void
}>()

const pinModalOpen = ref(false)
const pinCode = ref('')

const isActiveStage = computed(() =>
  ['DriverAssigned', 'PickedUp', 'InTransit'].includes(props.delivery.status),
)

const navigationDestination = computed(() => {
  if (props.delivery.status === 'DriverAssigned') {
    return props.delivery.restaurantAddress || props.delivery.restaurantName || ''
  }
  return props.delivery.deliveryAddress || props.delivery.deliveryNeighborhood || ''
})

const navigationUrl = computed(() =>
  `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(navigationDestination.value)}`,
)

function pressKeypadDigit(d: string) {
  if (pinCode.value.length >= 4) return
  pinCode.value += d
}

function pressKeypadBackspace() {
  pinCode.value = pinCode.value.slice(0, -1)
}

function openPinSheet() {
  pinCode.value = ''
  pinModalOpen.value = true
}

function submitDelivered() {
  emit('update-status', {
    id: props.delivery.id,
    status: 'Delivered',
    pin: pinCode.value.trim() || undefined,
  })
  pinModalOpen.value = false
}
</script>

<template>
  <AppCard
    padding="md"
    class="space-y-4"
    :class="isActiveStage ? 'border-brand-400 ring-1 ring-brand-500/20' : ''"
  >
    <!-- Header Row with Giant Payout Value -->
    <div class="flex items-start justify-between gap-3 border-b border-line pb-3">
      <div class="space-y-1">
        <div class="flex flex-wrap items-center gap-2">
          <span class="font-display text-base font-extrabold text-ink">
            Pedido #{{ delivery.orderId.slice(0, 8) }}
          </span>
          <OrderStatusBadge :status="delivery.status" />
        </div>
        <AppBadge
          v-if="delivery.estimatedMinutes && delivery.status !== 'Delivered' && delivery.status !== 'Cancelled'"
          variant="brand"
          size="xs"
        >
          <Clock class="h-3 w-3" />
          ~{{ delivery.estimatedMinutes }} min
          <span v-if="delivery.distanceKm"> · {{ delivery.distanceKm.toFixed(1) }} km</span>
          <span v-if="delivery.stopsAhead"> · +{{ delivery.stopsAhead }} parada(s)</span>
        </AppBadge>
      </div>

      <div class="text-right shrink-0">
        <p class="text-[10px] font-bold uppercase tracking-wider text-ink-muted">Repasse</p>
        <p class="font-display text-2xl font-extrabold text-fresh-700 dark:text-fresh-400 tabular-nums leading-none">
          {{ formatCurrency(delivery.deliveryFee ?? 8.5) }}
        </p>
      </div>
    </div>

    <!-- Vertical Origin -> Destination Route Visual -->
    <div class="relative space-y-4 pl-6">
      <div class="absolute bottom-3 left-[9px] top-3 w-0.5 border-l-2 border-dashed border-line-strong" />

      <div class="relative">
        <span class="absolute -left-6 top-0.5 flex h-5 w-5 items-center justify-center rounded-full bg-brand-600 text-white shadow-xs">
          <Store class="h-3 w-3" />
        </span>
        <p class="text-[10px] font-bold uppercase tracking-wider text-brand-600 dark:text-brand-400">
          Coleta · Restaurante
        </p>
        <p class="text-sm font-extrabold text-ink">
          {{ delivery.restaurantName || 'Restaurante Parceiro' }}
        </p>
        <p v-if="delivery.restaurantAddress" class="text-xs text-ink-secondary">
          {{ delivery.restaurantAddress }}
        </p>
      </div>

      <div class="relative">
        <span class="absolute -left-6 top-0.5 flex h-5 w-5 items-center justify-center rounded-full bg-fresh-600 text-white shadow-xs">
          <MapPin class="h-3 w-3" />
        </span>
        <p class="text-[10px] font-bold uppercase tracking-wider text-fresh-700 dark:text-fresh-400">
          Entrega · Destino
        </p>
        <p class="text-sm font-extrabold text-ink">
          Bairro {{ delivery.deliveryNeighborhood || 'Centro' }}
        </p>
        <p v-if="delivery.deliveryAddress" class="text-xs text-ink-secondary">
          {{ delivery.deliveryAddress }}
        </p>
      </div>
    </div>

    <!-- 1-Hand Ergonomic Action Zone (>= 52px height) -->
    <div
      v-if="delivery.status !== 'Delivered' && delivery.status !== 'Cancelled'"
      class="space-y-2.5 border-t border-line pt-4"
    >
      <AppButton
        v-if="!delivery.courierId"
        variant="primary"
        size="lg"
        block
        class="min-h-[52px] text-base"
        :loading="busy"
        @click="emit('accept', delivery.id)"
      >
        <Bike class="h-5 w-5" />
        Aceitar corrida
      </AppButton>

      <template v-else>
        <!-- 1-Tap External GPS Navigation -->
        <a
          v-if="navigationDestination"
          :href="navigationUrl"
          target="_blank"
          rel="noopener noreferrer"
          class="flex h-12 w-full items-center justify-center gap-2 rounded-xl border border-brand-300 bg-brand-50/70 text-xs font-extrabold text-brand-800 transition hover:bg-brand-100 dark:border-brand-800 dark:bg-brand-950/40 dark:text-brand-200"
        >
          <Navigation class="h-4 w-4" />
          <span>
            {{ delivery.status === 'DriverAssigned' ? 'Abrir rota até Restaurante (Maps)' : 'Abrir rota até Cliente (Maps)' }}
          </span>
          <ExternalLink class="h-3.5 w-3.5" />
        </a>

        <div class="grid gap-2.5 sm:grid-cols-2">
          <AppButton
            v-if="delivery.status === 'DriverAssigned'"
            variant="outline"
            size="lg"
            block
            class="min-h-[52px]"
            :loading="busy"
            @click="emit('update-status', { id: delivery.id, status: 'PickedUp' })"
          >
            <PackageCheck class="h-5 w-5 text-brand-600" />
            Confirmar coleta
          </AppButton>

          <AppButton
            v-if="delivery.status === 'DriverAssigned' || delivery.status === 'PickedUp'"
            variant="secondary"
            size="lg"
            block
            class="min-h-[52px]"
            :loading="busy"
            @click="emit('update-status', { id: delivery.id, status: 'InTransit' })"
          >
            <Navigation class="h-5 w-5" />
            Iniciar rota
          </AppButton>
        </div>

        <AppButton
          v-if="isActiveStage"
          variant="fresh"
          size="lg"
          block
          class="min-h-[52px] text-base"
          :loading="busy"
          @click="openPinSheet"
        >
          <KeyRound class="h-5 w-5" />
          Finalizar Entrega (PIN 4 dígitos)
        </AppButton>
      </template>
    </div>

    <!-- Dedicated High-Contrast PIN Confirmation Sheet with Custom Large Numeric Keypad -->
    <AppModal
      :open="pinModalOpen"
      title="Confirmar Entrega com PIN"
      :subtitle="`Pedido #${delivery.orderId.slice(0, 8)} · ${delivery.deliveryNeighborhood || 'Cliente'}`"
      max-width="sm"
      sheet-on-mobile
      @close="pinModalOpen = false"
    >
      <div class="space-y-5 text-center">
        <p class="text-xs text-ink-secondary">
          Solicite o código de 4 dígitos ao cliente ou à portaria para concluir a entrega.
        </p>

        <div class="flex justify-center">
          <OtpInput v-model="pinCode" :length="4" :disabled="busy" />
        </div>

        <!-- Custom Large Touch Keypad (1-hand friendly) -->
        <div class="mx-auto grid max-w-[280px] grid-cols-3 gap-2">
          <button
            v-for="digit in ['1', '2', '3', '4', '5', '6', '7', '8', '9']"
            :key="digit"
            type="button"
            class="flex h-14 items-center justify-center rounded-2xl border border-line bg-surface-subtle font-display text-xl font-extrabold text-ink transition active:scale-95 hover:bg-surface-muted"
            @click="pressKeypadDigit(digit)"
          >
            {{ digit }}
          </button>
          <button
            type="button"
            class="flex h-14 items-center justify-center rounded-2xl border border-line bg-surface-subtle text-xs font-bold text-ink-muted hover:bg-surface-muted"
            @click="pinCode = ''"
          >
            Limpar
          </button>
          <button
            type="button"
            class="flex h-14 items-center justify-center rounded-2xl border border-line bg-surface-subtle font-display text-xl font-extrabold text-ink transition active:scale-95 hover:bg-surface-muted"
            @click="pressKeypadDigit('0')"
          >
            0
          </button>
          <button
            type="button"
            aria-label="Apagar dígito"
            class="flex h-14 items-center justify-center rounded-2xl border border-line bg-surface-subtle text-ink-secondary transition active:scale-95 hover:bg-surface-muted"
            @click="pressKeypadBackspace"
          >
            <Delete class="h-5 w-5" />
          </button>
        </div>
      </div>

      <template #footer>
        <div class="flex flex-col gap-2">
          <AppButton
            variant="fresh"
            size="lg"
            block
            class="min-h-[52px]"
            :disabled="!isAdminUser && pinCode.replace(/\D/g, '').length < 4"
            :loading="busy"
            @click="submitDelivered"
          >
            <CheckCircle2 class="h-5 w-5" />
            {{ isAdminUser && pinCode.length < 4 ? 'Finalizar (Admin s/ PIN)' : 'Confirmar entrega' }}
          </AppButton>
        </div>
      </template>
    </AppModal>
  </AppCard>
</template>
