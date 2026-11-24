<script setup lang="ts">
import { computed, ref } from 'vue'
import { Bike, CheckCircle2, Clock, KeyRound, MapPin, Navigation, PackageCheck, Store } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
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

const pinCode = ref('')

const isActiveStage = computed(() =>
  ['DriverAssigned', 'PickedUp', 'InTransit'].includes(props.delivery.status),
)

const canCompleteDelivery = computed(
  () => props.isAdminUser || pinCode.value.replace(/\D/g, '').length >= 4,
)

function submitDelivered() {
  emit('update-status', {
    id: props.delivery.id,
    status: 'Delivered',
    pin: pinCode.value.trim() || undefined,
  })
}
</script>

<template>
  <AppCard
    padding="md"
    class="space-y-4"
    :class="isActiveStage ? 'border-brand-400 ring-1 ring-brand-500/20' : ''"
  >
    <!-- Header Row -->
    <div class="flex flex-wrap items-center justify-between gap-2 border-b border-line pb-3">
      <div class="flex flex-wrap items-center gap-2">
        <span class="font-display text-base font-extrabold text-ink">
          Pedido #{{ delivery.orderId.slice(0, 8) }}
        </span>
        <OrderStatusBadge :status="delivery.status" />
      </div>

      <div class="flex items-center gap-2">
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
        <span class="font-display text-sm font-extrabold text-fresh-700 dark:text-fresh-400 tabular-nums">
          {{ formatCurrency(delivery.deliveryFee ?? 8.5) }}
        </span>
      </div>
    </div>

    <!-- Vertical Origin -> Destination Route Visual -->
    <div class="relative space-y-4 pl-6">
      <div class="absolute bottom-3 left-[9px] top-3 w-0.5 border-l-2 border-dashed border-line-strong" />

      <!-- Pickup (Origin) -->
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

      <!-- Dropoff (Destination) -->
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

    <!-- Touch-Friendly Action Controls (>= 44px height) -->
    <div
      v-if="delivery.status !== 'Delivered' && delivery.status !== 'Cancelled'"
      class="space-y-3 border-t border-line pt-4"
    >
      <!-- Stage 1: Accept -->
      <AppButton
        v-if="!delivery.courierId"
        variant="primary"
        size="lg"
        block
        :loading="busy"
        @click="emit('accept', delivery.id)"
      >
        <Bike class="h-5 w-5" />
        Aceitar entrega
      </AppButton>

      <!-- Stage 2 & 3: Pickup / Start Route -->
      <div v-else class="space-y-3">
        <div class="grid gap-2 sm:grid-cols-2">
          <AppButton
            v-if="delivery.status === 'DriverAssigned'"
            variant="outline"
            size="lg"
            block
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
            :loading="busy"
            @click="emit('update-status', { id: delivery.id, status: 'InTransit' })"
          >
            <Navigation class="h-5 w-5" />
            Iniciar rota
          </AppButton>
        </div>

        <!-- Stage 4: 4-Digit PIN Delivery Confirmation -->
        <div
          v-if="isActiveStage"
          class="flex flex-col items-center justify-between gap-3 rounded-2xl border border-fresh-200 bg-fresh-50/50 p-4 dark:border-fresh-800/60 dark:bg-fresh-950/25 sm:flex-row"
        >
          <div class="flex flex-col items-center gap-2 sm:items-start">
            <span class="inline-flex items-center gap-1.5 text-xs font-bold text-ink">
              <KeyRound class="h-4 w-4 text-fresh-600" />
              {{ isAdminUser ? 'PIN do cliente (Admin opcional)' : 'Informe o PIN de 4 dígitos do cliente' }}
            </span>
            <OtpInput v-model="pinCode" :length="4" :disabled="busy" />
          </div>

          <AppButton
            variant="fresh"
            size="lg"
            class="w-full sm:w-auto min-w-[190px]"
            :disabled="!canCompleteDelivery"
            :loading="busy"
            @click="submitDelivered"
          >
            <CheckCircle2 class="h-5 w-5" />
            Confirmar entrega
          </AppButton>
        </div>
      </div>
    </div>
  </AppCard>
</template>
