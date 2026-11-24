<script setup lang="ts">
import { computed, ref } from 'vue'
import {
  AlertCircle,
  CheckCircle2,
  Clock,
  ExternalLink,
  KeyRound,
  MapPin,
  MessageSquareReply,
  Navigation,
  RotateCcw,
  Star,
  Truck,
} from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppModal from '@/components/ui/AppModal.vue'
import AppConfirmDialog from '@/components/ui/AppConfirmDialog.vue'
import OrderStatusBadge from '@/components/ui/OrderStatusBadge.vue'
import StarRating from '@/components/ui/StarRating.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import { useCartStore } from '@/stores/cart'
import { useToastStore } from '@/stores/toast'
import type { DeliveryTracking, Order, Product } from '@/types'

const props = defineProps<{
  order: Order
  isActive: boolean
  trackingOpen: boolean
  trackingInfo: DeliveryTracking | null
  loadingTracking?: boolean
}>()

const emit = defineEmits<{
  (e: 'toggle-tracking', order: Order): void
  (e: 'refresh'): void
}>()

const cart = useCartStore()
const toast = useToastStore()

const confirmCancelOpen = ref(false)
const cancelling = ref(false)
const reordering = ref(false)

const reviewModalOpen = ref(false)
const reviewRating = ref(5)
const reviewComment = ref('')
const sendingReview = ref(false)

const complaintOpen = ref(false)
const complaintText = ref('')
const sendingComplaint = ref(false)

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
  if (s === 'Paid' || s === 'Created' || s === 'PaymentPending') return 0
  return -1
})

const isTerminalFailure = computed(() =>
  ['Cancelled', 'Refunded', 'Failed'].includes(props.order.status),
)

const canCancel = computed(() =>
  ['Created', 'PaymentPending', 'Paid', 'Confirmed'].includes(props.order.status),
)

const pinDigits = computed(() =>
  (props.order.confirmationPin || '').slice(0, 4).split(''),
)

const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i
const courierDisplayName = computed(() => {
  const name = props.trackingInfo?.courierName?.trim()
  if (!name || uuidPattern.test(name)) return null
  return name
})

function getOpenStreetMapEmbedUrl(lat: number, lon: number): string {
  const delta = 0.006
  const bbox = `${lon - delta},${lat - delta},${lon + delta},${lat + delta}`
  return `https://www.openstreetmap.org/export/embed.html?bbox=${encodeURIComponent(bbox)}&layer=mapnik&marker=${lat},${lon}`
}

async function handleReorder() {
  if (!props.order.items || props.order.items.length === 0) return
  reordering.value = true
  try {
    let menuProducts: Product[] = []
    try {
      const { data } = await api.get<Product[]>(
        `/restaurantes/${props.order.restaurantId}/cardapio`,
      )
      menuProducts = data
    } catch {
      // Fallback to order item snapshot prices
    }

    let added = 0
    for (const item of props.order.items) {
      const found = menuProducts.find((p) => p.id === item.productId && p.isActive)
      const fallbackPrice = item.unitPrice || item.totalPrice / Math.max(1, item.quantity)
      cart.addItem(
        { id: props.order.restaurantId, name: props.order.restaurantName },
        found ?? {
          id: item.productId,
          name: item.productName.replace(/^\[PLANO\]\s*/i, ''),
          price: fallbackPrice,
        },
        { quantity: item.quantity },
      )
      added += item.quantity
    }

    if (added > 0) {
      cart.openDrawer()
      toast.success(`${added} item(ns) de ${props.order.restaurantName} adicionados à sacola!`)
    }
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível repetir o pedido.'))
  } finally {
    reordering.value = false
  }
}

async function handleConfirmCancel() {
  cancelling.value = true
  try {
    await api.patch(`/pedidos/${props.order.id}/cancelar`)
    confirmCancelOpen.value = false
    toast.info('Pedido cancelado.')
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível cancelar o pedido.'))
  } finally {
    cancelling.value = false
  }
}

async function handleSubmitReview() {
  sendingReview.value = true
  try {
    await api.post(`/pedidos/${props.order.id}/avaliacao`, {
      rating: reviewRating.value,
      comment: reviewComment.value.trim(),
    })
    reviewModalOpen.value = false
    reviewComment.value = ''
    reviewRating.value = 5
    toast.success('Avaliação enviada! Obrigado pelo seu feedback.')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível enviar a avaliação.'))
  } finally {
    sendingReview.value = false
  }
}

function openComplaintForm() {
  complaintText.value = props.order.complaintMessage || ''
  complaintOpen.value = !complaintOpen.value
}

async function handleSubmitComplaint() {
  if (!complaintText.value.trim()) {
    toast.warning('Descreva o problema ocorrido com seu pedido.')
    return
  }

  sendingComplaint.value = true
  try {
    await api.post(`/pedidos/${props.order.id}/reclamacao`, {
      message: complaintText.value.trim(),
    })
    complaintOpen.value = false
    toast.success('Reclamação enviada ao restaurante! Acompanhe a resposta no pedido.')
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível registrar a reclamação.'))
  } finally {
    sendingComplaint.value = false
  }
}
</script>

<template>
  <AppCard
    padding="md"
    :class="isActive ? 'border-brand-300/90 ring-1 ring-brand-500/15 dark:border-brand-800/70' : ''"
  >
    <div class="flex flex-col justify-between gap-3 border-b border-line pb-4 sm:flex-row sm:items-start">
      <div class="space-y-1 min-w-0">
        <div class="flex flex-wrap items-center gap-2">
          <h3 class="font-display text-base font-extrabold text-ink">
            {{ order.restaurantName || 'Restaurante Parceiro' }}
          </h3>
          <OrderStatusBadge :status="order.status" />
          <AppBadge v-if="order.couponCode" variant="fresh" size="xs">
            Cupom {{ order.couponCode }} (-{{ formatCurrency(order.discountAmount || 0) }})
          </AppBadge>
        </div>
        <p class="text-xs text-ink-muted tabular-nums">
          Pedido #{{ order.id.slice(0, 8) }} · {{ new Date(order.createdAt).toLocaleString('pt-BR') }}
        </p>
        <p v-if="order.deliveryAddress" class="inline-flex items-center gap-1.5 text-xs text-ink-secondary">
          <MapPin class="h-3.5 w-3.5 shrink-0 text-brand-500" />
          <span>{{ order.deliveryAddress }}</span>
        </p>
      </div>

      <div class="text-left sm:text-right shrink-0">
        <p class="text-[11px] font-medium text-ink-muted">Total</p>
        <p class="font-display text-xl font-extrabold text-ink tabular-nums">
          {{ formatCurrency(order.totalAmount) }}
        </p>
      </div>
    </div>

    <div v-if="!isTerminalFailure" class="mt-4">
      <div class="grid grid-cols-6 gap-1.5">
        <div v-for="(stage, idx) in stepperStages" :key="stage.key" class="space-y-1.5">
          <div
            class="h-1.5 rounded-full transition-all duration-300"
            :class="
              idx < currentStepIndex
                ? 'bg-fresh-500'
                : idx === currentStepIndex
                  ? isActive
                    ? 'bg-brand-600 animate-pulse'
                    : 'bg-fresh-600'
                  : 'bg-surface-muted'
            "
          />
          <p class="truncate text-center text-[10px] font-bold" :class="idx <= currentStepIndex ? 'text-ink' : 'text-ink-muted'">
            {{ stage.label }}
          </p>
        </div>
      </div>
    </div>

    <div
      v-if="order.confirmationPin && isActive && !isTerminalFailure"
      class="mt-4 flex flex-col justify-between gap-3 rounded-2xl border border-amber-300/80 bg-amber-50/80 p-4 dark:border-amber-700/60 dark:bg-amber-950/35 sm:flex-row sm:items-center"
    >
      <div class="flex items-start gap-3">
        <span class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-amber-500/20 text-amber-800 dark:text-amber-300">
          <KeyRound class="h-5 w-5" />
        </span>
        <div>
          <p class="font-display text-sm font-extrabold text-amber-950 dark:text-amber-200">
            Código PIN de entrega
          </p>
          <p class="text-xs text-amber-900/80 dark:text-amber-300/80">
            Informe ao entregador ou à portaria somente ao receber o pedido.
          </p>
        </div>
      </div>

      <div class="flex items-center gap-1.5 self-start sm:self-auto">
        <span
          v-for="(digit, dIdx) in pinDigits"
          :key="dIdx"
          class="flex h-11 w-10 items-center justify-center rounded-xl border border-amber-300 bg-surface font-mono text-lg font-extrabold text-ink shadow-xs dark:border-amber-700"
        >
          {{ digit }}
        </span>
      </div>
    </div>

    <ul
      v-if="order.items && order.items.length > 0"
      class="mt-4 space-y-1.5 rounded-xl bg-surface-subtle p-3.5 text-xs text-ink-secondary"
    >
      <li v-for="item in order.items" :key="item.id" class="flex items-center justify-between gap-2">
        <span>
          <strong class="text-ink">{{ item.quantity }}x</strong> {{ item.productName }}
        </span>
        <span class="font-semibold text-ink tabular-nums">{{ formatCurrency(item.totalPrice) }}</span>
      </li>
    </ul>

    <div class="mt-4 flex flex-wrap items-center gap-2 pt-1">
      <AppButton
        v-if="!isActive && order.items && order.items.length > 0"
        variant="primary"
        size="xs"
        :loading="reordering"
        @click="handleReorder"
      >
        <RotateCcw class="h-3.5 w-3.5" />
        Pedir novamente
      </AppButton>

      <AppButton
        v-if="!isTerminalFailure"
        :variant="trackingOpen ? 'secondary' : 'outline'"
        size="xs"
        @click="emit('toggle-tracking', order)"
      >
        <Truck class="h-3.5 w-3.5" />
        {{ trackingOpen ? 'Ocultar detalhes' : 'Detalhes do rastreio' }}
      </AppButton>

      <AppButton v-if="!isTerminalFailure" variant="outline" size="xs" @click="reviewModalOpen = true">
        <Star class="h-3.5 w-3.5 text-amber-500" />
        Avaliar pedido
      </AppButton>

      <AppButton variant="outline" size="xs" @click="openComplaintForm">
        <AlertCircle class="h-3.5 w-3.5 text-red-500" />
        {{ order.complaintMessage ? 'Atualizar suporte' : 'Preciso de ajuda' }}
      </AppButton>

      <AppButton
        v-if="canCancel"
        variant="ghost"
        size="xs"
        class="text-red-600 hover:bg-red-50 dark:text-red-400 dark:hover:bg-red-950/40 ml-auto"
        @click="confirmCancelOpen = true"
      >
        Cancelar pedido
      </AppButton>
    </div>

    <div
      v-if="trackingOpen"
      class="mt-4 space-y-4 rounded-2xl border border-line bg-surface-subtle p-4"
    >
      <div class="flex flex-wrap items-center justify-between gap-2">
        <div class="flex items-center gap-2">
          <span class="h-2.5 w-2.5 rounded-full bg-fresh-500 animate-pulse" />
          <span class="font-display text-sm font-extrabold text-ink">Acompanhamento Detalhado</span>
        </div>
        <span v-if="trackingInfo" class="text-[11px] text-ink-muted tabular-nums">
          Atualizado às {{ new Date(trackingInfo.updatedAt).toLocaleTimeString('pt-BR') }}
        </span>
      </div>

      <div
        v-if="trackingInfo && trackingInfo.estimatedMinutes && trackingInfo.estimatedMinutes > 0"
        class="space-y-3 rounded-xl border border-brand-200/70 bg-brand-50/40 p-3.5 dark:border-brand-800/50 dark:bg-brand-950/25"
      >
        <div class="flex flex-wrap items-center justify-between gap-2">
          <div class="flex items-center gap-2 text-xs font-bold text-ink">
            <Clock class="h-4 w-4 text-brand-600" />
            <span class="tabular-nums">Chegada estimada em ~{{ trackingInfo.estimatedMinutes }} min</span>
          </div>
          <AppBadge variant="brand" size="xs">
            {{ (trackingInfo.totalDistanceKm ?? 0).toFixed(1) }} km ·
            {{ trackingInfo.stopsAhead ? `${trackingInfo.stopsAhead} parada(s) antes` : 'Rota direta' }}
          </AppBadge>
        </div>

        <ol
          v-if="trackingInfo.routeStops && trackingInfo.routeStops.length > 0"
          class="space-y-2 border-t border-brand-200/60 pt-2.5 text-xs dark:border-brand-800/40"
        >
          <li v-for="(stop, sIdx) in trackingInfo.routeStops" :key="sIdx" class="flex items-start gap-2.5">
            <span
              class="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full text-[10px] font-extrabold text-white"
              :class="
                stop.type === 'Destination'
                  ? 'bg-fresh-600'
                  : stop.type === 'IntermediateStop'
                    ? 'bg-amber-500'
                    : 'bg-brand-600'
              "
            >
              {{ sIdx + 1 }}
            </span>
            <span class="text-ink-secondary">
              <strong class="text-ink">{{ stop.label }}:</strong> {{ stop.address }}
            </span>
          </li>
        </ol>
      </div>

      <div v-if="trackingInfo && (trackingInfo.latitude || courierDisplayName)" class="space-y-3">
        <div class="flex flex-wrap items-center justify-between gap-2 rounded-xl border border-line bg-surface px-3.5 py-2.5 text-xs">
          <div class="flex flex-wrap items-center gap-3">
            <span v-if="courierDisplayName" class="text-ink">
              <strong>Entregador:</strong> {{ courierDisplayName }}
            </span>
            <span v-if="trackingInfo.latitude && trackingInfo.longitude" class="inline-flex items-center gap-1 text-ink-muted tabular-nums">
              <Navigation class="h-3.5 w-3.5 text-brand-600" />
              {{ trackingInfo.latitude.toFixed(5) }}, {{ trackingInfo.longitude.toFixed(5) }}
            </span>
          </div>

          <a
            v-if="trackingInfo.latitude && trackingInfo.longitude"
            :href="`https://www.google.com/maps?q=${trackingInfo.latitude},${trackingInfo.longitude}`"
            target="_blank"
            rel="noopener noreferrer"
            class="inline-flex items-center gap-1 font-bold text-brand-600 hover:underline dark:text-brand-400"
          >
            <span>Abrir no Google Maps</span>
            <ExternalLink class="h-3.5 w-3.5" />
          </a>
        </div>

        <div v-if="trackingInfo.latitude && trackingInfo.longitude" class="overflow-hidden rounded-2xl border border-line bg-surface">
          <iframe
            :key="`${trackingInfo.latitude}-${trackingInfo.longitude}`"
            :src="getOpenStreetMapEmbedUrl(trackingInfo.latitude, trackingInfo.longitude)"
            title="Mapa de rastreamento ao vivo"
            class="h-56 w-full border-0"
            loading="lazy"
          />
        </div>
      </div>
    </div>

    <div v-if="order.complaintMessage" class="mt-4 space-y-2.5 rounded-2xl border border-line bg-surface-subtle p-4 text-xs">
      <div class="flex items-center justify-between gap-2">
        <span class="font-display font-bold text-ink">Atendimento do Pedido</span>
        <AppBadge :variant="order.complaintReply ? 'fresh' : 'warning'" size="xs">
          {{ order.complaintReply ? 'Respondida' : 'Aguardando restaurante' }}
        </AppBadge>
      </div>
      <div class="rounded-2xl rounded-tl-sm border border-red-200/80 bg-red-50/70 p-3 dark:border-red-900/60 dark:bg-red-950/30">
        <p class="text-[11px] font-bold text-red-800 dark:text-red-300">Você relatou:</p>
        <p class="mt-1 text-ink leading-relaxed">{{ order.complaintMessage }}</p>
      </div>
      <div v-if="order.complaintReply" class="ml-4 rounded-2xl rounded-tr-sm border border-fresh-200 bg-fresh-50/80 p-3 dark:border-fresh-800/60 dark:bg-fresh-950/30">
        <div class="flex items-center gap-1.5 text-[11px] font-bold text-fresh-800 dark:text-fresh-300">
          <MessageSquareReply class="h-3.5 w-3.5" />
          <span>{{ order.restaurantName }} respondeu:</span>
        </div>
        <p class="mt-1 text-ink leading-relaxed">{{ order.complaintReply }}</p>
      </div>
    </div>

    <form
      v-if="complaintOpen"
      class="mt-4 space-y-3 rounded-2xl border border-red-200/80 bg-surface-subtle p-4 dark:border-red-900/50"
      @submit.prevent="handleSubmitComplaint"
    >
      <label class="block text-xs font-bold text-ink">
        Relatar problema ao restaurante ({{ order.restaurantName }})
      </label>
      <textarea
        v-model="complaintText"
        rows="2"
        required
        placeholder="Descreva o ocorrido (ex: item faltando, atraso, embalagem)..."
        class="w-full rounded-xl border border-line-strong bg-surface px-3.5 py-2.5 text-sm text-ink placeholder:text-ink-muted focus:border-brand-600 focus:outline-none"
      />
      <div class="flex items-center gap-2">
        <AppButton type="submit" variant="danger" size="xs" :loading="sendingComplaint">Enviar relato</AppButton>
        <AppButton type="button" variant="ghost" size="xs" @click="complaintOpen = false">Cancelar</AppButton>
      </div>
    </form>
  </AppCard>

  <AppConfirmDialog
    :open="confirmCancelOpen"
    title="Cancelar este pedido?"
    :message="`Tem certeza que deseja cancelar o pedido #${order.id.slice(0, 8)} de ${order.restaurantName}?`"
    confirm-label="Sim, cancelar pedido"
    variant="danger"
    :loading="cancelling"
    @confirm="handleConfirmCancel"
    @cancel="confirmCancelOpen = false"
  />

  <AppModal
    :open="reviewModalOpen"
    title="Avaliar seu pedido"
    :subtitle="`${order.restaurantName} · Pedido #${order.id.slice(0, 8)}`"
    max-width="md"
    @close="reviewModalOpen = false"
  >
    <form id="review-order-form" class="space-y-4" @submit.prevent="handleSubmitReview">
      <div class="flex flex-col items-center rounded-2xl border border-line bg-surface-subtle py-5">
        <p class="mb-2 text-xs font-semibold text-ink-secondary">Como foi a sua experiência?</p>
        <StarRating v-model="reviewRating" size="lg" />
      </div>
      <div class="space-y-1.5">
        <label class="block text-xs font-semibold text-ink-secondary">Comentário (opcional)</label>
        <textarea
          v-model="reviewComment"
          rows="3"
          placeholder="Conte sobre o sabor, temperatura, embalagem ou entrega..."
          class="w-full rounded-xl border border-line-strong bg-surface px-3.5 py-2.5 text-sm text-ink placeholder:text-ink-muted focus:border-brand-600 focus:outline-none"
        />
      </div>
    </form>

    <template #footer>
      <div class="flex justify-end gap-2">
        <AppButton variant="outline" size="sm" @click="reviewModalOpen = false">Cancelar</AppButton>
        <AppButton type="submit" form="review-order-form" variant="primary" size="sm" :loading="sendingReview">
          <CheckCircle2 class="h-4 w-4" />
          Enviar avaliação
        </AppButton>
      </div>
    </template>
  </AppModal>
</template>
