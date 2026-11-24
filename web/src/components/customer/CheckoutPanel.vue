<script setup lang="ts">
import { computed, onUnmounted, ref } from 'vue'
import {
  Check,
  Clock,
  Copy,
  CreditCard,
  MapPin,
  Minus,
  Plus,
  QrCode,
  ShieldCheck,
  Sparkles,
  Tag,
  Trash2,
  X,
} from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppModal from '@/components/ui/AppModal.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import { useCartStore } from '@/stores/cart'
import { useToastStore } from '@/stores/toast'
import type {
  Address,
  CartDeliveryEstimate,
  CouponValidationResponse,
  CreateOrderResponse,
  PaymentResponse,
  Subscription,
} from '@/types'

const props = defineProps<{
  addresses: Address[]
  selectedAddressId: string
  activeSubscription: Subscription | null
  weeklyPlanLimit: number | null
  usedPlanMealsThisWeek: number
  cartEstimate: CartDeliveryEstimate | null
}>()

const emit = defineEmits<{
  (e: 'update:selectedAddressId', value: string): void
  (e: 'order-created', orderId: string): void
}>()

const cart = useCartStore()
const toast = useToastStore()

const couponCode = ref('')
const appliedCoupon = ref<CouponValidationResponse | null>(null)
const validatingCoupon = ref(false)
const paymentMethod = ref<'PIX' | 'CreditCard' | 'DebitCard'>('PIX')
const submittingOrder = ref(false)
const checkoutError = ref('')

// PIX Confirmation Modal + 15-min countdown
const pixModalOpen = ref(false)
const pixPayment = ref<PaymentResponse | null>(null)
const pixOrderPin = ref<string | undefined>(undefined)
const copiedPix = ref(false)
const pixSecondsLeft = ref(900)
let pixTimer: ReturnType<typeof setInterval> | null = null

const pixFormattedCountdown = computed(() => {
  const m = Math.floor(pixSecondsLeft.value / 60)
  const s = pixSecondsLeft.value % 60
  return `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`
})

function startPixCountdown() {
  if (pixTimer) clearInterval(pixTimer)
  pixSecondsLeft.value = 900
  pixTimer = setInterval(() => {
    if (pixSecondsLeft.value > 0) {
      pixSecondsLeft.value -= 1
    } else if (pixTimer) {
      clearInterval(pixTimer)
      pixTimer = null
    }
  }, 1000)
}

onUnmounted(() => {
  if (pixTimer) clearInterval(pixTimer)
})

const selectedAddress = computed(
  () => props.addresses.find((a) => a.id === props.selectedAddressId) ?? null,
)

const hasRegisteredAddress = computed(
  () => props.addresses.length > 0 && Boolean(selectedAddress.value),
)

const addressMiniMapUrl = computed(() => {
  const lat = selectedAddress.value?.latitude ?? -26.9194
  const lon = selectedAddress.value?.longitude ?? -49.0661
  const delta = 0.004
  const bbox = `${lon - delta},${lat - delta},${lon + delta},${lat + delta}`
  return `https://www.openstreetmap.org/export/embed.html?bbox=${encodeURIComponent(bbox)}&layer=mapnik&marker=${lat},${lon}`
})

const remainingPlanMeals = computed(() => {
  if (props.weeklyPlanLimit === null) return null
  return Math.max(0, props.weeklyPlanLimit - props.usedPlanMealsThisWeek)
})

const planUsagePercent = computed(() => {
  if (!props.weeklyPlanLimit || props.weeklyPlanLimit <= 0) return 0
  return Math.min(100, Math.round((props.usedPlanMealsThisWeek / props.weeklyPlanLimit) * 100))
})

const planSavingsAmount = computed(() => {
  if (!cart.isPlanRedemption) return 0
  return cart.items.reduce((sum, i) => sum + (i.basePrice ?? i.price) * i.quantity, 0)
})

const totalSavingsAmount = computed(
  () => planSavingsAmount.value + (appliedCoupon.value?.valid ? appliedCoupon.value.discountAmount : 0),
)

const finalCartTotal = computed(() => {
  if (appliedCoupon.value && appliedCoupon.value.valid) {
    return appliedCoupon.value.finalAmount
  }
  return cart.totalAmount
})

const paymentOptions = [
  {
    id: 'PIX' as const,
    label: 'PIX Instantâneo',
    subtitle: 'Aprovação imediata via QR Code ou Copia-e-Cola',
    icon: QrCode,
  },
  {
    id: 'CreditCard' as const,
    label: 'Cartão de Crédito',
    subtitle: 'Pagamento rápido e seguro',
    icon: CreditCard,
  },
  {
    id: 'DebitCard' as const,
    label: 'Cartão de Débito',
    subtitle: 'Débito online instantâneo',
    icon: CreditCard,
  },
]

function generateIdempotencyKey(prefix: string): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return `${prefix}-${crypto.randomUUID()}`
  }
  return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`
}

async function handleValidateCoupon() {
  checkoutError.value = ''
  if (!couponCode.value.trim()) {
    toast.warning('Informe o código do cupom para aplicar.')
    return
  }

  validatingCoupon.value = true
  try {
    const { data } = await api.post<CouponValidationResponse>('/cupons/validar', {
      code: couponCode.value.trim().toUpperCase(),
      orderAmount: cart.totalAmount,
    })
    appliedCoupon.value = data
    toast.success(`Cupom ${data.code} aplicado! Economia de ${formatCurrency(data.discountAmount)}.`)
  } catch (err) {
    appliedCoupon.value = null
    const msg = getErrorMessage(err, 'Cupom inválido ou expirado.')
    checkoutError.value = msg
    toast.error(msg)
  } finally {
    validatingCoupon.value = false
  }
}

function removeCoupon() {
  appliedCoupon.value = null
  couponCode.value = ''
}

async function copyPixCode() {
  const code = pixPayment.value?.pixCopyPaste || pixPayment.value?.pixQrCode
  if (!code) return
  try {
    await navigator.clipboard.writeText(code)
    copiedPix.value = true
    toast.success('Código PIX Copia-e-Cola copiado!')
    setTimeout(() => {
      copiedPix.value = false
    }, 2500)
  } catch {
    toast.error('Não foi possível copiar automaticamente. Selecione e copie manualmente.')
  }
}

async function handleCheckout() {
  if (!cart.restaurantId || cart.items.length === 0) return
  checkoutError.value = ''

  if (props.addresses.length === 0) {
    checkoutError.value = 'É obrigatório cadastrar um endereço de entrega antes de fazer um pedido.'
    toast.warning(checkoutError.value)
    return
  }

  if (!props.selectedAddressId || !selectedAddress.value) {
    checkoutError.value = 'Selecione um endereço de entrega cadastrado para continuar.'
    toast.warning(checkoutError.value)
    return
  }

  submittingOrder.value = true
  const orderIdempotencyKey = generateIdempotencyKey('order')
  const paymentIdempotencyKey = generateIdempotencyKey('pay')

  try {
    const { data: created } = await api.post<CreateOrderResponse>(
      '/pedidos',
      {
        restaurantId: cart.restaurantId,
        isPlanRedemption: cart.isPlanRedemption,
        addressId: props.selectedAddressId || undefined,
        couponCode: appliedCoupon.value?.valid ? appliedCoupon.value.code : undefined,
        items: cart.items.map((item) => ({
          productId: item.productId,
          quantity: item.quantity,
          wantsCutlery: Boolean(item.wantsCutlery),
          selectedAddons: (item.selectedAddons || []).map((a) => ({
            name: a.name,
            quantity: a.quantity,
          })),
        })),
      },
      {
        headers: {
          'Idempotency-Key': orderIdempotencyKey,
        },
      },
    )

    const { data: paymentRes } = await api.post<PaymentResponse>(
      `/pedidos/${created.orderId}/pagamento`,
      {
        method: paymentMethod.value,
      },
      {
        headers: {
          'Idempotency-Key': paymentIdempotencyKey,
        },
      },
    )

    cart.clear()
    appliedCoupon.value = null
    couponCode.value = ''

    if (paymentMethod.value === 'PIX' && (paymentRes?.pixCopyPaste || paymentRes?.pixQrCode)) {
      pixPayment.value = paymentRes
      pixOrderPin.value = created.confirmationPin
      pixModalOpen.value = true
      startPixCountdown()
    }

    toast.success('Pedido confirmado e pagamento aprovado!', 'Pedido realizado')
    emit('order-created', created.orderId)
  } catch (err) {
    const msg = getErrorMessage(err, 'Não foi possível concluir o pedido.')
    checkoutError.value = msg
    toast.error(msg)
  } finally {
    submittingOrder.value = false
  }
}
</script>

<template>
  <section class="grid grid-cols-1 gap-6 lg:grid-cols-12 items-start">
    <!-- Left Column -->
    <div class="space-y-5 lg:col-span-7 xl:col-span-8">
      <div
        v-if="activeSubscription && weeklyPlanLimit !== null"
        class="rounded-2xl border border-fresh-200 bg-fresh-50/70 p-4 dark:border-fresh-800/60 dark:bg-fresh-950/30"
      >
        <div class="flex flex-wrap items-center justify-between gap-2 text-xs">
          <div class="flex items-center gap-2 font-bold text-fresh-800 dark:text-fresh-300">
            <Sparkles class="h-4 w-4 text-fresh-600" />
            <span>{{ activeSubscription.planName }} · Cota Semanal</span>
          </div>
          <span class="font-display font-extrabold text-fresh-700 dark:text-fresh-300 tabular-nums">
            {{ remainingPlanMeals }} disponível(is) de {{ weeklyPlanLimit }}
          </span>
        </div>
        <div class="mt-2.5 h-2 w-full overflow-hidden rounded-full bg-fresh-200/70 dark:bg-fresh-900/60">
          <div
            class="h-full rounded-full bg-fresh-600 transition-all duration-300"
            :style="{ width: `${planUsagePercent}%` }"
          />
        </div>
        <p class="mt-1.5 text-[11px] text-fresh-800/80 dark:text-fresh-300/80">
          Você utilizou {{ usedPlanMealsThisWeek }} de {{ weeklyPlanLimit }} marmitas do plano nos últimos 7 dias.
        </p>
      </div>

      <div
        v-if="checkoutError"
        role="alert"
        class="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-medium text-red-700 dark:border-red-900/60 dark:bg-red-950/40 dark:text-red-300"
      >
        {{ checkoutError }}
      </div>

      <!-- Step 1: Address Cards + Mini-Map Confirmation -->
      <AppCard>
        <div class="flex items-center justify-between gap-2 mb-4">
          <div class="flex items-center gap-2">
            <span class="flex h-7 w-7 items-center justify-center rounded-lg bg-brand-50 font-display text-xs font-extrabold text-brand-700 dark:bg-brand-950/60 dark:text-brand-300">
              1
            </span>
            <h2 class="font-display text-base font-extrabold text-ink">Endereço de entrega</h2>
          </div>
          <RouterLink
            to="/conta?tab=enderecos"
            class="text-xs font-bold text-brand-600 hover:underline dark:text-brand-400"
          >
            + Novo endereço
          </RouterLink>
        </div>

        <div v-if="addresses.length > 0" class="space-y-4">
          <div class="grid gap-3 sm:grid-cols-2">
            <button
              v-for="addr in addresses"
              :key="addr.id"
              type="button"
              class="flex items-start gap-3 rounded-2xl border p-3.5 text-left transition"
              :class="
                selectedAddressId === addr.id
                  ? 'border-brand-600 bg-brand-50/40 ring-1 ring-brand-600/30 dark:bg-brand-950/30'
                  : 'border-line bg-surface-subtle hover:border-line-strong'
              "
              @click="emit('update:selectedAddressId', addr.id)"
            >
              <MapPin
                class="mt-0.5 h-4 w-4 shrink-0"
                :class="selectedAddressId === addr.id ? 'text-brand-600' : 'text-ink-muted'"
              />
              <div class="min-w-0 flex-1">
                <div class="flex items-center justify-between gap-1">
                  <p class="truncate text-sm font-bold text-ink">
                    {{ addr.street }}, {{ addr.number }}
                  </p>
                  <AppBadge v-if="addr.isMain" variant="brand" size="xs">Principal</AppBadge>
                </div>
                <p class="text-xs text-ink-secondary truncate">
                  {{ addr.neighborhood }} — {{ addr.city }}/{{ addr.state }}
                </p>
                <p v-if="addr.complement" class="text-[11px] text-ink-muted truncate">
                  {{ addr.complement }}
                </p>
              </div>
            </button>
          </div>

          <!-- Address Confirmation Mini-Map -->
          <div
            v-if="selectedAddress"
            class="overflow-hidden rounded-2xl border border-line bg-surface-muted"
          >
            <iframe
              :key="selectedAddress.id"
              :src="addressMiniMapUrl"
              title="Confirmação visual do endereço"
              class="h-32 w-full border-0"
              loading="lazy"
            />
          </div>
        </div>

        <div
          v-else
          role="alert"
          class="flex flex-col items-start justify-between gap-3 rounded-2xl border border-red-200 bg-red-50/80 p-4 text-xs text-red-800 dark:border-red-900/60 dark:bg-red-950/35 dark:text-red-300 sm:flex-row sm:items-center"
        >
          <div class="space-y-0.5">
            <p class="font-bold">Endereço obrigatório para fazer pedidos</p>
            <p class="opacity-90">
              Você ainda não possui um endereço cadastrado. Cadastre seu endereço de entrega para liberar o checkout.
            </p>
          </div>
          <RouterLink
            to="/conta?tab=enderecos"
            class="shrink-0 rounded-xl bg-brand-600 px-3.5 py-2 text-xs font-bold text-white transition hover:bg-brand-700"
          >
            Cadastrar endereço
          </RouterLink>
        </div>

        <div
          v-if="cartEstimate"
          class="mt-4 rounded-2xl border border-brand-200/80 bg-brand-50/40 p-4 dark:border-brand-800/50 dark:bg-brand-950/25"
        >
          <div class="flex flex-wrap items-center justify-between gap-2">
            <div class="flex items-center gap-2">
              <Clock class="h-4 w-4 text-brand-600" />
              <span class="font-display text-sm font-extrabold text-ink tabular-nums">
                Entrega estimada: ~{{ cartEstimate.estimatedMinutes }} min
              </span>
              <span class="text-xs text-ink-secondary tabular-nums">
                (previsto às {{ new Date(cartEstimate.estimatedArrivalAt).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' }) }})
              </span>
            </div>
            <AppBadge variant="brand" size="xs">
              {{ cartEstimate.totalDistanceKm.toFixed(1) }} km · {{ cartEstimate.routeSummary }}
            </AppBadge>
          </div>

          <div class="mt-3 grid grid-cols-3 gap-2 text-center text-[11px]">
            <div class="rounded-xl bg-surface p-2 border border-line">
              <p class="text-ink-muted">Preparo</p>
              <p class="font-display font-bold text-ink tabular-nums">~{{ cartEstimate.preparationMinutes }} min</p>
            </div>
            <div class="rounded-xl bg-surface p-2 border border-line">
              <p class="text-ink-muted">Deslocamento</p>
              <p class="font-display font-bold text-ink tabular-nums">~{{ cartEstimate.travelMinutes }} min</p>
            </div>
            <div class="rounded-xl bg-surface p-2 border border-line">
              <p class="text-ink-muted">Paradas antes</p>
              <p class="font-display font-bold text-ink tabular-nums">
                {{ cartEstimate.stopsAhead > 0 ? `${cartEstimate.stopsAhead} (~${cartEstimate.stopsDelayMinutes}m)` : 'Rota direta' }}
              </p>
            </div>
          </div>
        </div>
      </AppCard>

      <!-- Step 2: Items Review -->
      <AppCard>
        <div class="flex items-center justify-between gap-2 mb-3">
          <div class="flex items-center gap-2">
            <span class="flex h-7 w-7 items-center justify-center rounded-lg bg-brand-50 font-display text-xs font-extrabold text-brand-700 dark:bg-brand-950/60 dark:text-brand-300">
              2
            </span>
            <div>
              <h2 class="font-display text-base font-extrabold text-ink">Itens do pedido</h2>
              <p class="text-xs text-ink-muted">{{ cart.restaurantName }}</p>
            </div>
          </div>
          <button
            type="button"
            class="text-xs font-semibold text-red-600 hover:underline dark:text-red-400"
            @click="cart.clear()"
          >
            Limpar sacola
          </button>
        </div>

        <div class="divide-y divide-line">
          <div
            v-for="(item, idx) in cart.items"
            :key="`${item.productId}-${idx}`"
            class="flex flex-col justify-between gap-3 py-3 sm:flex-row sm:items-center"
          >
            <div class="min-w-0">
              <div class="flex flex-wrap items-center gap-2">
                <p class="text-sm font-bold text-ink">{{ item.name }}</p>
                <AppBadge v-if="cart.isPlanRedemption" variant="fresh" size="xs">
                  Resgate do Plano (Base R$ 0,00)
                </AppBadge>
              </div>
              <p class="mt-0.5 text-xs text-ink-muted">
                {{ formatCurrency(cart.getItemEffectivePrice(item)) }} un.
                <span v-if="item.selectedAddons && item.selectedAddons.length > 0">
                  · Extras: {{ item.selectedAddons.map((a) => `${a.quantity}x ${a.name}`).join(', ') }}
                </span>
                <span v-if="item.wantsCutlery"> · Com talheres</span>
              </p>
            </div>

            <div class="flex items-center justify-between gap-3 sm:justify-end">
              <div class="inline-flex items-center gap-1 rounded-xl border border-line bg-surface-subtle p-0.5">
                <button
                  type="button"
                  aria-label="Diminuir quantidade"
                  class="flex h-7 w-7 items-center justify-center rounded-lg text-ink-secondary hover:bg-surface"
                  @click="cart.updateItemQuantityByIndex(idx, item.quantity - 1)"
                >
                  <Minus class="h-3.5 w-3.5" />
                </button>
                <span class="min-w-[1.5rem] text-center text-xs font-bold text-ink tabular-nums">
                  {{ item.quantity }}
                </span>
                <button
                  type="button"
                  aria-label="Aumentar quantidade"
                  class="flex h-7 w-7 items-center justify-center rounded-lg text-brand-600 hover:bg-surface"
                  @click="cart.updateItemQuantityByIndex(idx, item.quantity + 1)"
                >
                  <Plus class="h-3.5 w-3.5" />
                </button>
              </div>

              <span class="w-20 text-right font-display text-sm font-bold text-ink tabular-nums">
                {{ formatCurrency(cart.getItemEffectivePrice(item) * item.quantity) }}
              </span>

              <button
                type="button"
                aria-label="Remover item"
                class="rounded-lg p-1.5 text-ink-muted hover:bg-red-50 hover:text-red-600 dark:hover:bg-red-950/40"
                @click="cart.removeItemByIndex(idx)"
              >
                <Trash2 class="h-4 w-4" />
              </button>
            </div>
          </div>
        </div>
      </AppCard>

      <!-- Step 3: Payment Method -->
      <AppCard>
        <div class="flex items-center gap-2 mb-4">
          <span class="flex h-7 w-7 items-center justify-center rounded-lg bg-brand-50 font-display text-xs font-extrabold text-brand-700 dark:bg-brand-950/60 dark:text-brand-300">
            3
          </span>
          <h2 class="font-display text-base font-extrabold text-ink">Forma de pagamento</h2>
        </div>

        <div class="grid gap-3 sm:grid-cols-3">
          <button
            v-for="opt in paymentOptions"
            :key="opt.id"
            type="button"
            class="flex flex-col items-start gap-2 rounded-2xl border p-4 text-left transition"
            :class="
              paymentMethod === opt.id
                ? 'border-brand-600 bg-brand-50/40 ring-1 ring-brand-600/30 dark:bg-brand-950/30'
                : 'border-line bg-surface-subtle hover:border-line-strong'
            "
            @click="paymentMethod = opt.id"
          >
            <div class="flex w-full items-center justify-between">
              <component
                :is="opt.icon"
                class="h-5 w-5"
                :class="paymentMethod === opt.id ? 'text-brand-600' : 'text-ink-muted'"
              />
              <span
                class="flex h-4 w-4 items-center justify-center rounded-full border"
                :class="
                  paymentMethod === opt.id
                    ? 'border-brand-600 bg-brand-600 text-white'
                    : 'border-line-strong'
                "
              >
                <Check v-if="paymentMethod === opt.id" class="h-2.5 w-2.5" />
              </span>
            </div>
            <div>
              <p class="text-sm font-bold text-ink">{{ opt.label }}</p>
              <p class="mt-0.5 text-[11px] leading-snug text-ink-muted">{{ opt.subtitle }}</p>
            </div>
          </button>
        </div>
      </AppCard>
    </div>

    <!-- Right Column: Sticky Financial Summary -->
    <aside class="lg:col-span-5 xl:col-span-4 lg:sticky lg:top-24 space-y-4">
      <AppCard padding="lg" class="space-y-5">
        <h2 class="font-display text-lg font-extrabold text-ink">Resumo do pedido</h2>

        <!-- Coupon -->
        <div class="space-y-2">
          <label class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-ink-muted">
            <Tag class="h-3.5 w-3.5 text-brand-600" />
            Cupom promocional
          </label>

          <div v-if="appliedCoupon && appliedCoupon.valid" class="flex items-center justify-between rounded-xl border border-fresh-200 bg-fresh-50 px-3 py-2.5 text-xs dark:border-fresh-800/60 dark:bg-fresh-950/40">
            <div>
              <span class="font-mono font-extrabold text-fresh-800 dark:text-fresh-300">
                {{ appliedCoupon.code }}
              </span>
              <span class="ml-1.5 text-fresh-700 dark:text-fresh-400">
                (-{{ appliedCoupon.discountPercentage }}%)
              </span>
            </div>
            <button
              type="button"
              aria-label="Remover cupom"
              class="rounded-lg p-1 text-fresh-700 hover:bg-fresh-100 dark:text-fresh-300"
              @click="removeCoupon"
            >
              <X class="h-3.5 w-3.5" />
            </button>
          </div>

          <div v-else class="flex gap-2">
            <input
              v-model="couponCode"
              type="text"
              placeholder="Ex: BEMVINDO10"
              class="flex-1 rounded-xl border border-line-strong bg-surface px-3 py-2 text-xs font-semibold uppercase text-ink placeholder:normal-case placeholder:text-ink-muted focus:border-brand-600 focus:outline-none"
              @keyup.enter="handleValidateCoupon"
            />
            <AppButton
              variant="outline"
              size="sm"
              :loading="validatingCoupon"
              @click="handleValidateCoupon"
            >
              Aplicar
            </AppButton>
          </div>
        </div>

        <!-- Totals & Total Savings Highlight -->
        <div class="space-y-2.5 border-t border-line pt-4 text-sm">
          <div class="flex items-center justify-between text-ink-secondary">
            <span>Subtotal ({{ cart.totalItems }} {{ cart.totalItems === 1 ? 'item' : 'itens' }})</span>
            <span class="font-semibold tabular-nums">{{ formatCurrency(cart.totalAmount) }}</span>
          </div>

          <div
            v-if="planSavingsAmount > 0"
            class="flex items-center justify-between text-fresh-700 dark:text-fresh-400"
          >
            <span>Coberto pelo Plano</span>
            <span class="font-bold tabular-nums">-{{ formatCurrency(planSavingsAmount) }}</span>
          </div>

          <div
            v-if="appliedCoupon && appliedCoupon.valid"
            class="flex items-center justify-between text-fresh-700 dark:text-fresh-400"
          >
            <span>Desconto ({{ appliedCoupon.code }})</span>
            <span class="font-bold tabular-nums">-{{ formatCurrency(appliedCoupon.discountAmount) }}</span>
          </div>

          <div
            v-if="totalSavingsAmount > 0"
            class="rounded-xl bg-fresh-50 px-3 py-2 text-xs font-bold text-fresh-800 dark:bg-fresh-950/40 dark:text-fresh-300 flex items-center justify-between"
          >
            <span>Sua economia neste pedido</span>
            <span class="tabular-nums">{{ formatCurrency(totalSavingsAmount) }}</span>
          </div>

          <div class="flex items-center justify-between border-t border-line pt-3">
            <span class="font-display text-base font-extrabold text-ink">Total a pagar</span>
            <span class="font-display text-2xl font-extrabold text-ink tabular-nums">
              {{ formatCurrency(finalCartTotal) }}
            </span>
          </div>
        </div>

        <div
          v-if="!hasRegisteredAddress"
          class="rounded-xl border border-amber-200 bg-amber-50/80 p-3 text-xs text-amber-900 dark:border-amber-800/60 dark:bg-amber-950/35 dark:text-amber-300"
        >
          <p class="font-bold">
            {{ addresses.length === 0 ? 'Cadastre um endereço para finalizar' : 'Selecione um endereço de entrega' }}
          </p>
          <RouterLink
            v-if="addresses.length === 0"
            to="/conta?tab=enderecos"
            class="mt-1 inline-block font-bold text-brand-600 underline dark:text-brand-400"
          >
            Ir para Meus Endereços
          </RouterLink>
        </div>

        <AppButton
          variant="primary"
          size="lg"
          block
          :disabled="!hasRegisteredAddress"
          :loading="submittingOrder"
          @click="handleCheckout"
        >
          Confirmar e pagar pedido
        </AppButton>

        <div class="flex items-center justify-center gap-1.5 text-[11px] text-ink-muted">
          <ShieldCheck class="h-3.5 w-3.5 text-fresh-600" />
          <span>Transação protegida com chave de idempotência</span>
        </div>
      </AppCard>
    </aside>
  </section>

  <!-- PIX Confirmation Modal with Visual QR SVG + 15-min Countdown -->
  <AppModal
    :open="pixModalOpen"
    title="Pagamento PIX Confirmado"
    subtitle="Comprovante e código PIX gerados pelo gateway de pagamento."
    max-width="md"
    @close="pixModalOpen = false"
  >
    <div v-if="pixPayment" class="space-y-5 text-center">
      <!-- Visual QR Code Matrix Frame + Countdown -->
      <div class="mx-auto inline-flex flex-col items-center gap-2 rounded-3xl border border-line bg-white p-4 shadow-card">
        <svg viewBox="0 0 120 120" class="h-36 w-36 text-stone-900" aria-label="QR Code PIX">
          <rect width="120" height="120" fill="#ffffff" />
          <!-- Top-left finder -->
          <rect x="8" y="8" width="28" height="28" rx="4" fill="none" stroke="currentColor" stroke-width="5" />
          <rect x="16" y="16" width="12" height="12" rx="2" fill="currentColor" />
          <!-- Top-right finder -->
          <rect x="84" y="8" width="28" height="28" rx="4" fill="none" stroke="currentColor" stroke-width="5" />
          <rect x="92" y="16" width="12" height="12" rx="2" fill="currentColor" />
          <!-- Bottom-left finder -->
          <rect x="8" y="84" width="28" height="28" rx="4" fill="none" stroke="currentColor" stroke-width="5" />
          <rect x="16" y="92" width="12" height="12" rx="2" fill="currentColor" />
          <!-- Matrix blocks -->
          <rect x="46" y="12" width="8" height="8" fill="currentColor" />
          <rect x="62" y="12" width="12" height="8" fill="currentColor" />
          <rect x="46" y="28" width="16" height="8" fill="currentColor" />
          <rect x="12" y="46" width="10" height="8" fill="currentColor" />
          <rect x="30" y="46" width="14" height="8" fill="currentColor" />
          <rect x="52" y="46" width="16" height="16" rx="3" fill="#EA580C" />
          <rect x="76" y="46" width="10" height="8" fill="currentColor" />
          <rect x="96" y="46" width="12" height="14" fill="currentColor" />
          <rect x="12" y="64" width="18" height="8" fill="currentColor" />
          <rect x="44" y="70" width="14" height="8" fill="currentColor" />
          <rect x="66" y="68" width="18" height="10" fill="currentColor" />
          <rect x="48" y="88" width="12" height="12" fill="currentColor" />
          <rect x="70" y="88" width="16" height="8" fill="currentColor" />
          <rect x="94" y="86" width="14" height="14" fill="currentColor" />
          <rect x="72" y="104" width="24" height="8" fill="currentColor" />
        </svg>
        <span class="text-[11px] font-bold text-stone-600 tabular-nums">
          Validade do código: {{ pixFormattedCountdown }}
        </span>
      </div>

      <div>
        <AppBadge variant="fresh" size="sm" dot>
          Status: {{ pixPayment.status }}
        </AppBadge>
        <p class="mt-2 font-display text-2xl font-extrabold text-ink tabular-nums">
          {{ formatCurrency(pixPayment.amount) }}
        </p>
        <p v-if="pixPayment.gateway" class="text-xs text-ink-muted">
          Processado via {{ pixPayment.gateway }}
        </p>
      </div>

      <div
        v-if="pixOrderPin"
        class="rounded-2xl border border-amber-200 bg-amber-50/70 p-4 dark:border-amber-800/60 dark:bg-amber-950/30"
      >
        <p class="text-xs font-bold text-amber-900 dark:text-amber-300">
          Seu PIN de entrega (4 dígitos)
        </p>
        <p class="mt-1 font-mono text-2xl font-extrabold tracking-[0.35em] text-amber-900 dark:text-amber-200">
          {{ pixOrderPin }}
        </p>
      </div>

      <div v-if="pixPayment.pixCopyPaste || pixPayment.pixQrCode" class="space-y-2 text-left">
        <label class="block text-xs font-bold text-ink-secondary">
          PIX Copia-e-Cola
        </label>
        <div class="rounded-xl border border-line bg-surface-subtle p-3 font-mono text-[11px] break-all text-ink-secondary max-h-24 overflow-y-auto">
          {{ pixPayment.pixCopyPaste || pixPayment.pixQrCode }}
        </div>
        <AppButton variant="outline" size="md" block @click="copyPixCode">
          <Check v-if="copiedPix" class="h-4 w-4 text-fresh-600" />
          <Copy v-else class="h-4 w-4" />
          <span>{{ copiedPix ? 'Código PIX copiado!' : 'Copiar código Copia-e-Cola' }}</span>
        </AppButton>
      </div>
    </div>

    <template #footer>
      <AppButton variant="primary" size="md" block @click="pixModalOpen = false">
        Acompanhar meu pedido
      </AppButton>
    </template>
  </AppModal>
</template>
