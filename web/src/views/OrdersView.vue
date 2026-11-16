<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { AlertCircle, MessageSquareReply, Minus, Plus, RefreshCw, Star, Trash2 } from 'lucide-vue-next'
import Navbar from '@/components/Navbar.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import { useCartStore } from '@/stores/cart'
import type { CouponValidationResponse, CreateOrderResponse, Order } from '@/types'

const cart = useCartStore()

const orders = ref<Order[]>([])
const loading = ref(true)
const submittingOrder = ref(false)
const validatingCoupon = ref(false)
const error = ref('')
const successMessage = ref('')

const couponCode = ref('')
const appliedCoupon = ref<CouponValidationResponse | null>(null)
const paymentMethod = ref('PIX')

const reviewOrderId = ref<string | null>(null)
const reviewRating = ref(5)
const reviewComment = ref('')
const sendingReview = ref(false)

const complaintOrderId = ref<string | null>(null)
const complaintText = ref('')
const sendingComplaint = ref(false)

const finalCartTotal = computed(() => {
  if (appliedCoupon.value && appliedCoupon.value.valid) {
    return appliedCoupon.value.finalAmount
  }
  return cart.totalAmount
})

function formatStatus(status: string): string {
  const map: Record<string, string> = {
    Pending: 'Pendente',
    Created: 'Criado',
    Paid: 'Pago',
    Preparing: 'Em preparo',
    ReadyForPickup: 'Pronto para retirada',
    DriverAssigned: 'Entregador alocado',
    InTransit: 'Em rota de entrega',
    Delivered: 'Entregue',
    Cancelled: 'Cancelado',
    Refunded: 'Estornado',
  }
  return map[status] || status
}

async function loadOrders() {
  loading.value = true
  error.value = ''

  try {
    const { data } = await api.get<Order[]>('/pedidos')
    orders.value = data
  } catch (err) {
    error.value = getErrorMessage(err, 'Não foi possível carregar seus pedidos.')
  } finally {
    loading.value = false
  }
}

async function handleValidateCoupon() {
  error.value = ''
  if (!couponCode.value.trim()) {
    error.value = 'Informe o código do cupom.'
    return
  }

  validatingCoupon.value = true
  try {
    const { data } = await api.post<CouponValidationResponse>('/cupons/validar', {
      code: couponCode.value.trim().toUpperCase(),
      orderAmount: cart.totalAmount,
    })
    appliedCoupon.value = data
  } catch (err) {
    appliedCoupon.value = null
    error.value = getErrorMessage(err, 'Cupom inválido ou expirado.')
  } finally {
    validatingCoupon.value = false
  }
}

async function handleCheckout() {
  if (!cart.restaurantId || cart.items.length === 0) return
  error.value = ''
  successMessage.value = ''
  submittingOrder.value = true

  try {
    const { data: created } = await api.post<CreateOrderResponse>('/pedidos', {
      restaurantId: cart.restaurantId,
      isPlanRedemption: cart.isPlanRedemption,
      items: cart.items.map((item) => ({
        productId: item.productId,
        quantity: item.quantity,
        wantsCutlery: Boolean(item.wantsCutlery),
        selectedAddons: (item.selectedAddons || []).map((a) => ({
          name: a.name,
          quantity: a.quantity,
        })),
      })),
    })

    await api.post(`/pedidos/${created.orderId}/pagamento`, {
      method: paymentMethod.value,
    })

    cart.clear()
    appliedCoupon.value = null
    couponCode.value = ''
    successMessage.value = 'Pedido realizado e pagamento aprovado com sucesso!'
    await loadOrders()
  } catch (err) {
    error.value = getErrorMessage(err, 'Não foi possível concluir o pedido.')
  } finally {
    submittingOrder.value = false
  }
}

async function handleCancelOrder(orderId: string) {
  error.value = ''
  successMessage.value = ''

  try {
    await api.patch(`/pedidos/${orderId}/cancelar`)
    successMessage.value = 'Pedido cancelado.'
    await loadOrders()
  } catch (err) {
    error.value = getErrorMessage(err, 'Não foi possível cancelar o pedido.')
  }
}

async function handleSubmitReview(orderId: string) {
  error.value = ''
  successMessage.value = ''
  sendingReview.value = true

  try {
    await api.post(`/pedidos/${orderId}/avaliacao`, {
      rating: reviewRating.value,
      comment: reviewComment.value.trim(),
    })
    reviewOrderId.value = null
    reviewComment.value = ''
    reviewRating.value = 5
    successMessage.value = 'Avaliação enviada com sucesso!'
  } catch (err) {
    error.value = getErrorMessage(err, 'Não foi possível enviar a avaliação.')
  } finally {
    sendingReview.value = false
  }
}

async function handleSubmitComplaint(orderId: string) {
  error.value = ''
  successMessage.value = ''

  if (!complaintText.value.trim()) {
    error.value = 'Descreva o motivo da sua reclamação.'
    return
  }

  sendingComplaint.value = true
  try {
    await api.post(`/pedidos/${orderId}/reclamacao`, {
      message: complaintText.value.trim(),
    })
    complaintOrderId.value = null
    complaintText.value = ''
    successMessage.value = 'Reclamação enviada ao restaurante! Acompanhe a resposta abaixo.'
    await loadOrders()
  } catch (err) {
    error.value = getErrorMessage(err, 'Não foi possível registrar a reclamação.')
  } finally {
    sendingComplaint.value = false
  }
}

onMounted(() => {
  void loadOrders()
})
</script>

<template>
  <div class="min-h-screen bg-stone-50">
    <Navbar />

    <main class="mx-auto max-w-5xl space-y-8 px-4 py-8 sm:px-6">
      <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <div>
          <h1 class="text-2xl font-bold text-stone-900">Carrinho e Pedidos</h1>
          <p class="mt-1 text-sm text-stone-600">
            Finalize seu pedido atual ou acompanhe o histórico de compras.
          </p>
        </div>

        <button
          type="button"
          :disabled="loading"
          class="inline-flex items-center gap-2 self-start rounded-lg border border-stone-200 bg-white px-4 py-2 text-sm font-medium text-stone-700 hover:bg-stone-50 disabled:opacity-50"
          @click="loadOrders"
        >
          <RefreshCw class="h-4 w-4" :class="{ 'animate-spin': loading }" />
          Atualizar
        </button>
      </div>

      <div
        v-if="error"
        class="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700"
      >
        {{ error }}
      </div>

      <div
        v-if="successMessage"
        class="rounded-lg border border-green-200 bg-green-50 p-4 text-sm text-green-800"
      >
        {{ successMessage }}
      </div>

      <section
        v-if="cart.items.length > 0"
        class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm"
      >
        <div class="flex items-center justify-between border-b border-stone-100 pb-4">
          <div>
            <h2 class="text-lg font-bold text-stone-900">Seu carrinho</h2>
            <p class="text-xs text-stone-500">Restaurante: {{ cart.restaurantName }}</p>
          </div>
          <button
            type="button"
            class="text-xs font-medium text-red-600 hover:underline"
            @click="cart.clear()"
          >
            Limpar carrinho
          </button>
        </div>

        <div class="divide-y divide-stone-100 py-2">
          <div
            v-for="item in cart.items"
            :key="item.productId"
            class="flex items-center justify-between py-3"
          >
            <div>
              <p class="font-medium text-stone-900">
                {{ item.name }}
                <span
                  v-if="cart.isPlanRedemption"
                  class="ml-1.5 rounded bg-green-100 px-2 py-0.5 text-[10px] font-semibold text-green-800"
                >
                  Resgate do Plano (Base R$ 0,00)
                </span>
              </p>
              <p class="text-xs text-stone-500">
                {{ formatCurrency(cart.getItemEffectivePrice(item)) }} cada
                <span v-if="item.selectedAddons && item.selectedAddons.length > 0">
                  • Extras:
                  {{ item.selectedAddons.map((a) => `${a.quantity}x ${a.name}`).join(', ') }}
                </span>
                <span v-if="item.wantsCutlery"> • Com talheres</span>
              </p>
            </div>

            <div class="flex items-center gap-3">
              <div class="flex items-center gap-2 rounded-lg border border-stone-200 px-2 py-1">
                <button
                  type="button"
                  class="text-stone-600 hover:text-stone-900"
                  @click="cart.updateQuantity(item.productId, item.quantity - 1)"
                >
                  <Minus class="h-3.5 w-3.5" />
                </button>
                <span class="min-w-[1.5rem] text-center text-sm font-semibold">
                  {{ item.quantity }}
                </span>
                <button
                  type="button"
                  class="text-stone-600 hover:text-stone-900"
                  @click="cart.updateQuantity(item.productId, item.quantity + 1)"
                >
                  <Plus class="h-3.5 w-3.5" />
                </button>
              </div>

              <span class="w-20 text-right text-sm font-semibold text-stone-900">
                {{ formatCurrency(cart.getItemEffectivePrice(item) * item.quantity) }}
              </span>

              <button
                type="button"
                class="text-stone-400 hover:text-red-600"
                @click="cart.removeItem(item.productId)"
              >
                <Trash2 class="h-4 w-4" />
              </button>
            </div>
          </div>
        </div>

        <div class="mt-4 grid gap-4 border-t border-stone-100 pt-4 sm:grid-cols-2">
          <div class="space-y-2">
            <label class="block text-xs font-semibold uppercase text-stone-500">
              Cupom de desconto
            </label>
            <div class="flex gap-2">
              <input
                v-model="couponCode"
                type="text"
                placeholder="Ex: BEMVINDO10"
                class="w-full rounded-lg border border-stone-300 px-3 py-2 text-sm uppercase focus:border-brand-600 focus:outline-none"
              />
              <button
                type="button"
                :disabled="validatingCoupon"
                class="rounded-lg border border-stone-300 bg-stone-50 px-4 py-2 text-sm font-medium text-stone-700 hover:bg-stone-100 disabled:opacity-50"
                @click="handleValidateCoupon"
              >
                Aplicar
              </button>
            </div>
            <p v-if="appliedCoupon" class="text-xs font-medium text-green-700">
              Cupom {{ appliedCoupon.code }} aplicado (-{{ appliedCoupon.discountPercentage }}%:
              -{{ formatCurrency(appliedCoupon.discountAmount) }})
            </p>
          </div>

          <div class="space-y-2">
            <label class="block text-xs font-semibold uppercase text-stone-500">
              Forma de pagamento
            </label>
            <select
              v-model="paymentMethod"
              class="w-full rounded-lg border border-stone-300 bg-white px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
            >
              <option value="PIX">PIX</option>
              <option value="CreditCard">Cartão de Crédito</option>
              <option value="DebitCard">Cartão de Débito</option>
            </select>
          </div>
        </div>

        <div class="mt-6 flex flex-col items-start justify-between gap-4 border-t border-stone-100 pt-4 sm:flex-row sm:items-center">
          <div>
            <p class="text-xs text-stone-500">Total a pagar</p>
            <p class="text-2xl font-bold text-stone-900">
              {{ formatCurrency(finalCartTotal) }}
            </p>
          </div>

          <button
            type="button"
            :disabled="submittingOrder"
            class="w-full rounded-lg bg-brand-600 px-6 py-2.5 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-50 sm:w-auto"
            @click="handleCheckout"
          >
            {{ submittingOrder ? 'Processando...' : 'Confirmar e pagar pedido' }}
          </button>
        </div>
      </section>

      <section class="space-y-4">
        <h2 class="text-lg font-bold text-stone-900">Meus pedidos</h2>

        <p v-if="loading" class="text-sm text-stone-500">Carregando pedidos...</p>

        <div v-else-if="orders.length > 0" class="space-y-4">
          <div
            v-for="order in orders"
            :key="order.id"
            class="rounded-xl border border-stone-200 bg-white p-5 shadow-sm"
          >
            <div class="flex flex-col justify-between gap-2 border-b border-stone-100 pb-3 sm:flex-row sm:items-center">
              <div>
                <h3 class="font-bold text-stone-900">
                  {{ order.restaurantName || 'Restaurante Parceiro' }}
                </h3>
                <p class="text-xs text-stone-500">
                  Pedido #{{ order.id.slice(0, 8) }} •
                  {{ new Date(order.createdAt).toLocaleString('pt-BR') }}
                </p>
              </div>

              <div class="flex items-center gap-3">
                <span
                  class="rounded-full px-2.5 py-0.5 text-xs font-semibold"
                  :class="
                    order.status === 'Cancelled'
                      ? 'bg-red-50 text-red-700'
                      : order.status === 'Delivered' || order.status === 'Paid'
                        ? 'bg-green-50 text-green-700'
                        : 'bg-amber-50 text-amber-800'
                  "
                >
                  {{ formatStatus(order.status) }}
                </span>

                <span class="font-bold text-stone-900">
                  {{ formatCurrency(order.totalAmount) }}
                </span>
              </div>
            </div>

            <ul v-if="order.items && order.items.length > 0" class="mt-3 space-y-1 text-sm text-stone-600">
              <li v-for="item in order.items" :key="item.id" class="flex justify-between">
                <span>{{ item.quantity }}x {{ item.productName }}</span>
                <span>{{ formatCurrency(item.totalPrice) }}</span>
              </li>
            </ul>

            <div class="mt-4 flex flex-wrap items-center gap-3 pt-2">
              <button
                v-if="order.status !== 'Cancelled' && order.status !== 'Delivered'"
                type="button"
                class="rounded-lg border border-red-200 px-3 py-1.5 text-xs font-medium text-red-600 hover:bg-red-50"
                @click="handleCancelOrder(order.id)"
              >
                Cancelar pedido
              </button>

              <button
                v-if="order.status !== 'Cancelled'"
                type="button"
                class="inline-flex items-center gap-1 rounded-lg border border-stone-200 px-3 py-1.5 text-xs font-medium text-stone-700 hover:bg-stone-50"
                @click="reviewOrderId = reviewOrderId === order.id ? null : order.id"
              >
                <Star class="h-3.5 w-3.5 text-amber-500" />
                Avaliar pedido
              </button>

              <button
                type="button"
                class="inline-flex items-center gap-1 rounded-lg border border-red-200 bg-red-50 px-3 py-1.5 text-xs font-semibold text-red-700 hover:bg-red-100"
                @click="
                  complaintOrderId = complaintOrderId === order.id ? null : order.id;
                  complaintText = order.complaintMessage || '';
                "
              >
                <AlertCircle class="h-3.5 w-3.5" />
                {{ order.complaintMessage ? 'Atualizar reclamação' : 'Abrir reclamação' }}
              </button>
            </div>

            <!-- Exibição da Reclamação do Cliente e Resposta do Restaurante -->
            <div
              v-if="order.complaintMessage"
              class="mt-4 space-y-2 rounded-lg border border-red-200 bg-red-50 p-3.5 text-xs"
            >
              <div class="flex items-center justify-between">
                <span class="font-bold text-red-800">
                  Sua Reclamação Registrada:
                </span>
                <span
                  class="rounded-full px-2 py-0.5 text-[11px] font-semibold"
                  :class="
                    order.complaintReply
                      ? 'bg-green-100 text-green-800'
                      : 'bg-amber-100 text-amber-800'
                  "
                >
                  {{ order.complaintReply ? 'Respondida pelo restaurante' : 'Aguardando resposta do restaurante' }}
                </span>
              </div>
              <p class="text-stone-800">{{ order.complaintMessage }}</p>

              <div
                v-if="order.complaintReply"
                class="mt-2 rounded border border-green-200 bg-green-50 p-3 text-xs"
              >
                <div class="flex items-center gap-1.5 font-bold text-green-900">
                  <MessageSquareReply class="h-3.5 w-3.5" />
                  <span>Resposta do Restaurante ({{ order.restaurantName }}):</span>
                </div>
                <p class="mt-1 text-stone-800">{{ order.complaintReply }}</p>
              </div>
            </div>

            <!-- Formulário para Abrir / Atualizar Reclamação -->
            <form
              v-if="complaintOrderId === order.id"
              class="mt-4 space-y-3 rounded-lg border border-red-200 bg-stone-50 p-4"
              @submit.prevent="handleSubmitComplaint(order.id)"
            >
              <label class="block text-xs font-bold text-stone-800">
                Relatar problema ou reclamação ao restaurante ({{ order.restaurantName }}):
              </label>
              <textarea
                v-model="complaintText"
                rows="2"
                required
                placeholder="Descreva o problema ocorrido com seu pedido (ex: item faltando, atraso, embalagem)..."
                class="w-full rounded-lg border border-stone-300 bg-white px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
              />
              <div class="flex gap-2">
                <button
                  type="submit"
                  :disabled="sendingComplaint"
                  class="rounded-lg bg-red-600 px-4 py-1.5 text-xs font-semibold text-white hover:bg-red-700 disabled:opacity-50"
                >
                  {{ sendingComplaint ? 'Enviando...' : 'Enviar reclamação ao restaurante' }}
                </button>
                <button
                  type="button"
                  class="rounded-lg border border-stone-300 bg-white px-3 py-1.5 text-xs text-stone-600"
                  @click="complaintOrderId = null"
                >
                  Cancelar
                </button>
              </div>
            </form>

            <form
              v-if="reviewOrderId === order.id"
              class="mt-4 space-y-3 rounded-lg border border-stone-200 bg-stone-50 p-4"
              @submit.prevent="handleSubmitReview(order.id)"
            >
              <div class="flex items-center gap-3">
                <label class="text-xs font-semibold text-stone-700">Nota (1 a 5):</label>
                <select
                  v-model.number="reviewRating"
                  class="rounded border border-stone-300 bg-white px-2.5 py-1 text-sm"
                >
                  <option :value="5">5 - Excelente</option>
                  <option :value="4">4 - Muito bom</option>
                  <option :value="3">3 - Regular</option>
                  <option :value="2">2 - Ruim</option>
                  <option :value="1">1 - Péssimo</option>
                </select>
              </div>

              <div>
                <input
                  v-model="reviewComment"
                  type="text"
                  placeholder="Deixe um comentário sobre sua refeição (opcional)"
                  class="w-full rounded-lg border border-stone-300 bg-white px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
                />
              </div>

              <div class="flex gap-2">
                <button
                  type="submit"
                  :disabled="sendingReview"
                  class="rounded-lg bg-brand-600 px-4 py-1.5 text-xs font-semibold text-white hover:bg-brand-700 disabled:opacity-50"
                >
                  {{ sendingReview ? 'Enviando...' : 'Enviar avaliação' }}
                </button>
                <button
                  type="button"
                  class="rounded-lg border border-stone-300 bg-white px-3 py-1.5 text-xs text-stone-600"
                  @click="reviewOrderId = null"
                >
                  Cancelar
                </button>
              </div>
            </form>
          </div>
        </div>

        <div
          v-else
          class="rounded-xl border border-stone-200 bg-white p-8 text-center text-sm text-stone-500"
        >
          Você ainda não realizou nenhum pedido.
        </div>
      </section>
    </main>
  </div>
</template>

