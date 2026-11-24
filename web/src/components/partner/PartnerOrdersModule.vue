<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import {
  AlertCircle,
  Bike,
  CheckCircle2,
  ChevronDown,
  ChefHat,
  Clock,
  MessageSquareReply,
  PackageCheck,
  Printer,
} from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppDrawer from '@/components/ui/AppDrawer.vue'
import AppModal from '@/components/ui/AppModal.vue'
import AppTabs, { type TabItem } from '@/components/ui/AppTabs.vue'
import AppEmptyState from '@/components/ui/AppEmptyState.vue'
import OrderStatusBadge from '@/components/ui/OrderStatusBadge.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import type { Order } from '@/types'

const props = defineProps<{
  orders: Order[]
}>()

const emit = defineEmits<{
  (e: 'refresh'): void
}>()

const toast = useToastStore()

type MobileTab = 'new' | 'preparing' | 'ready' | 'transit'
const mobileTab = ref<MobileTab>('new')

const nowMs = ref(Date.now())
let slaTimer: ReturnType<typeof setInterval> | null = null

const updatingOrderId = ref<string | null>(null)
const complaintsDrawerOpen = ref(false)
const historyDrawerOpen = ref(false)
const printTicketOrder = ref<Order | null>(null)

const replyingOrderId = ref<string | null>(null)
const complaintReplyText = ref('')
const sendingReply = ref(false)

const manualOpen = ref(false)
const manualOrderCode = ref('')
const manualNextStatus = ref('Preparing')
const updatingManual = ref(false)

onMounted(() => {
  slaTimer = setInterval(() => {
    nowMs.value = Date.now()
  }, 30_000)
})

onUnmounted(() => {
  if (slaTimer) {
    clearInterval(slaTimer)
    slaTimer = null
  }
})

const newOrders = computed(() =>
  props.orders.filter((o) => ['Created', 'PaymentPending', 'Paid', 'Confirmed'].includes(o.status)),
)
const preparingOrders = computed(() => props.orders.filter((o) => o.status === 'Preparing'))
const readyOrders = computed(() => props.orders.filter((o) => o.status === 'ReadyForPickup'))
const transitOrders = computed(() =>
  props.orders.filter((o) => ['DriverAssigned', 'PickedUp', 'InTransit'].includes(o.status)),
)
const historyOrders = computed(() =>
  props.orders.filter((o) => ['Delivered', 'Cancelled', 'Refunded', 'Failed'].includes(o.status)),
)
const complaintOrders = computed(() => props.orders.filter((o) => Boolean(o.complaintMessage)))
const unansweredComplaints = computed(() =>
  complaintOrders.value.filter((o) => !o.complaintReply).length,
)

const kanbanColumns = computed(() => [
  { id: 'new' as const, title: 'Novos / Pagos', orders: newOrders.value, accent: 'brand' as const },
  { id: 'preparing' as const, title: 'Em Preparo', orders: preparingOrders.value, accent: 'warning' as const },
  { id: 'ready' as const, title: 'Prontos p/ Coleta', orders: readyOrders.value, accent: 'fresh' as const },
  { id: 'transit' as const, title: 'Em Rota', orders: transitOrders.value, accent: 'info' as const },
])

const mobileTabs = computed<TabItem[]>(() => [
  { id: 'new', label: 'Novos', count: newOrders.value.length, icon: Clock },
  { id: 'preparing', label: 'Preparo', count: preparingOrders.value.length, icon: ChefHat },
  { id: 'ready', label: 'Prontos', count: readyOrders.value.length, icon: PackageCheck },
  { id: 'transit', label: 'Em Rota', count: transitOrders.value.length, icon: Bike },
])

const mobileVisibleOrders = computed(() => {
  const map: Record<MobileTab, Order[]> = {
    new: newOrders.value,
    preparing: preparingOrders.value,
    ready: readyOrders.value,
    transit: transitOrders.value,
  }
  return map[mobileTab.value]
})

function getElapsedMinutes(createdAt: string): number {
  const diff = Math.max(0, nowMs.value - new Date(createdAt).getTime())
  return Math.floor(diff / 60_000)
}

function getSlaVariant(minutes: number): 'fresh' | 'warning' | 'danger' {
  if (minutes >= 20) return 'danger'
  if (minutes >= 10) return 'warning'
  return 'fresh'
}

async function handleAdvanceStatus(orderId: string, status: string) {
  updatingOrderId.value = orderId
  try {
    await api.post(`/pedidos/${orderId}/historico-status`, { status })
    toast.success(
      status === 'ReadyForPickup'
        ? 'Entregador chamado! Pedido liberado na fila de coleta.'
        : 'Status do pedido atualizado.',
    )
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível atualizar o status do pedido.'))
  } finally {
    updatingOrderId.value = null
  }
}

async function handleReplyComplaint(orderId: string) {
  if (!complaintReplyText.value.trim()) {
    toast.warning('Digite uma resposta para o cliente.')
    return
  }
  sendingReply.value = true
  try {
    await api.post(`/pedidos/${orderId}/reclamacao/resposta`, {
      reply: complaintReplyText.value.trim(),
    })
    replyingOrderId.value = null
    complaintReplyText.value = ''
    toast.success('Resposta enviada ao cliente!')
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível enviar a resposta.'))
  } finally {
    sendingReply.value = false
  }
}

async function handleManualStatusUpdate() {
  const normalized = manualOrderCode.value.replace(/^#+/, '').trim().toLowerCase()
  if (!normalized) {
    toast.warning('Informe o código do pedido (ex: #ab6cd09d).')
    return
  }

  let resolvedOrderId: string | null = null
  const exactMatch = props.orders.find((o) => o.id.toLowerCase() === normalized)
  if (exactMatch) {
    resolvedOrderId = exactMatch.id
  } else {
    const matches = props.orders.filter((o) => o.id.toLowerCase().startsWith(normalized))
    const uniqueMatch = matches.length === 1 ? matches[0] : undefined
    if (uniqueMatch) {
      resolvedOrderId = uniqueMatch.id
    } else if (matches.length > 1) {
      toast.warning('Mais de um pedido corresponde a esse código. Informe os 8 caracteres do código do pedido.')
      return
    } else if (/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(normalized)) {
      resolvedOrderId = normalized
    } else {
      toast.error(`Pedido #${normalized} não encontrado nesta loja.`)
      return
    }
  }

  updatingManual.value = true
  try {
    await api.post(`/pedidos/${resolvedOrderId}/historico-status`, {
      status: manualNextStatus.value,
    })
    manualOrderCode.value = ''
    toast.success(`Status do pedido #${resolvedOrderId.slice(0, 8)} atualizado manualmente.`)
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível atualizar o status do pedido.'))
  } finally {
    updatingManual.value = false
  }
}

function triggerBrowserPrint() {
  if (typeof window !== 'undefined' && typeof window.print === 'function') {
    window.print()
  }
}
</script>

<template>
  <div class="space-y-5">
    <!-- Top KDS Control Strip -->
    <div class="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-line bg-surface p-3.5 shadow-card">
      <div class="flex items-center gap-2 text-xs">
        <span class="h-2.5 w-2.5 rounded-full bg-fresh-500 animate-pulse" />
        <span class="font-display font-extrabold text-ink">KDS em Tempo Real</span>
        <span class="hidden sm:inline text-ink-muted">
          · {{ newOrders.length + preparingOrders.length + readyOrders.length + transitOrders.length }} pedido(s) em operação
        </span>
      </div>

      <div class="flex flex-wrap items-center gap-2">
        <AppButton variant="outline" size="xs" @click="historyDrawerOpen = true">
          <CheckCircle2 class="h-3.5 w-3.5 text-fresh-600" />
          Histórico ({{ historyOrders.length }})
        </AppButton>

        <AppButton
          :variant="unansweredComplaints > 0 ? 'danger' : 'outline'"
          size="xs"
          @click="complaintsDrawerOpen = true"
        >
          <AlertCircle class="h-3.5 w-3.5" />
          Reclamações ({{ unansweredComplaints > 0 ? `${unansweredComplaints} pendente(s)` : complaintOrders.length }})
        </AppButton>
      </div>
    </div>

    <!-- Mobile/Tablet Tabs (< xl) -->
    <div class="xl:hidden space-y-4">
      <AppTabs v-model="mobileTab" :tabs="mobileTabs" variant="pills" />

      <div v-if="mobileVisibleOrders.length > 0" class="grid gap-3.5 md:grid-cols-2">
        <AppCard
          v-for="order in mobileVisibleOrders"
          :key="`mob-${order.id}`"
          padding="md"
          class="space-y-3"
        >
          <div class="flex items-start justify-between gap-2 border-b border-line pb-2.5">
            <div>
              <div class="flex items-center gap-2">
                <span class="font-display text-sm font-extrabold text-ink">#{{ order.id.slice(0, 8) }}</span>
                <AppBadge :variant="getSlaVariant(getElapsedMinutes(order.createdAt))" size="xs">
                  <Clock class="h-3 w-3" />
                  {{ getElapsedMinutes(order.createdAt) }}m
                </AppBadge>
              </div>
              <p class="mt-0.5 text-xs font-semibold text-ink-secondary">
                {{ order.customerName || 'Cliente' }}
              </p>
            </div>
            <div class="text-right">
              <OrderStatusBadge :status="order.status" />
              <p class="mt-1 font-display text-sm font-extrabold text-ink tabular-nums">
                {{ formatCurrency(order.totalAmount) }}
              </p>
            </div>
          </div>

          <ul v-if="order.items?.length" class="space-y-1 text-xs text-ink-secondary">
            <li v-for="item in order.items" :key="item.id">
              <strong class="text-ink">{{ item.quantity }}x</strong> {{ item.productName }}
            </li>
          </ul>

          <div class="flex flex-wrap items-center gap-2 border-t border-line pt-2.5">
            <AppButton
              v-if="order.status === 'Created' || order.status === 'Paid'"
              variant="outline"
              size="xs"
              :loading="updatingOrderId === order.id"
              @click="handleAdvanceStatus(order.id, 'Confirmed')"
            >
              Confirmar
            </AppButton>
            <AppButton
              v-if="['Created', 'Paid', 'Confirmed'].includes(order.status)"
              variant="secondary"
              size="xs"
              :loading="updatingOrderId === order.id"
              @click="handleAdvanceStatus(order.id, 'Preparing')"
            >
              Preparar
            </AppButton>
            <AppButton
              v-if="['Created', 'Paid', 'Confirmed', 'Preparing'].includes(order.status)"
              variant="primary"
              size="xs"
              :loading="updatingOrderId === order.id"
              @click="handleAdvanceStatus(order.id, 'ReadyForPickup')"
            >
              Chamar Entregador
            </AppButton>
            <button
              type="button"
              aria-label="Imprimir comanda"
              class="ml-auto rounded-lg border border-line p-1.5 text-ink-muted hover:text-ink"
              @click="printTicketOrder = order"
            >
              <Printer class="h-3.5 w-3.5" />
            </button>
          </div>
        </AppCard>
      </div>

      <AppEmptyState
        v-else
        :icon="ChefHat"
        title="Nenhum pedido nesta coluna"
        description="Novos pedidos chegam automaticamente em tempo real."
      />
    </div>

    <!-- Desktop 4-Column Kanban KDS (xl+) -->
    <div class="hidden xl:grid xl:grid-cols-4 gap-4 items-start">
      <div
        v-for="col in kanbanColumns"
        :key="col.id"
        class="rounded-2xl border border-line bg-surface-subtle p-3 space-y-3 min-h-[420px]"
      >
        <div class="flex items-center justify-between px-1 py-1">
          <h3 class="font-display text-xs font-extrabold uppercase tracking-wider text-ink">
            {{ col.title }}
          </h3>
          <AppBadge :variant="col.accent" size="xs">{{ col.orders.length }}</AppBadge>
        </div>

        <div v-if="col.orders.length === 0" class="py-12 text-center text-xs text-ink-muted">
          Sem pedidos aqui
        </div>

        <div v-else class="space-y-3">
          <div
            v-for="order in col.orders"
            :key="order.id"
            class="rounded-2xl border bg-surface p-3.5 shadow-card space-y-3 transition"
            :class="
              col.id === 'new' && ['Paid', 'Created'].includes(order.status)
                ? 'border-brand-400 ring-1 ring-brand-500/20'
                : 'border-line'
            "
          >
            <!-- Ticket Header + Live SLA Badge -->
            <div class="flex items-start justify-between gap-2">
              <div class="min-w-0">
                <div class="flex items-center gap-1.5">
                  <span class="font-display text-sm font-extrabold text-ink">
                    #{{ order.id.slice(0, 8) }}
                  </span>
                  <AppBadge :variant="getSlaVariant(getElapsedMinutes(order.createdAt))" size="xs">
                    {{ getElapsedMinutes(order.createdAt) }}m
                  </AppBadge>
                </div>
                <p class="mt-0.5 truncate text-xs font-semibold text-ink-secondary">
                  {{ order.customerName || 'Cliente' }}
                </p>
              </div>

              <button
                type="button"
                aria-label="Imprimir comanda"
                title="Imprimir comanda"
                class="rounded-lg border border-line p-1.5 text-ink-muted transition hover:bg-surface-muted hover:text-ink"
                @click="printTicketOrder = order"
              >
                <Printer class="h-3.5 w-3.5" />
              </button>
            </div>

            <!-- Items -->
            <ul v-if="order.items?.length" class="space-y-1 rounded-xl bg-surface-subtle p-2.5 text-xs text-ink-secondary">
              <li v-for="item in order.items" :key="item.id" class="leading-snug">
                <strong class="text-ink">{{ item.quantity }}x</strong> {{ item.productName }}
              </li>
            </ul>

            <!-- Total + Status -->
            <div class="flex items-center justify-between text-xs">
              <OrderStatusBadge :status="order.status" />
              <span class="font-display font-extrabold text-ink tabular-nums">
                {{ formatCurrency(order.totalAmount) }}
              </span>
            </div>

            <!-- Complaint Alert inside Ticket -->
            <button
              v-if="order.complaintMessage"
              type="button"
              class="flex w-full items-center justify-between rounded-xl border border-red-200 bg-red-50/80 px-2.5 py-1.5 text-[11px] font-bold text-red-700 dark:border-red-900/60 dark:bg-red-950/35 dark:text-red-300"
              @click="complaintsDrawerOpen = true"
            >
              <span class="inline-flex items-center gap-1">
                <AlertCircle class="h-3.5 w-3.5" />
                {{ order.complaintReply ? 'Reclamação respondida' : 'Reclamação pendente!' }}
              </span>
              <span>Ver</span>
            </button>

            <!-- 1-Click Primary Action -->
            <div class="pt-1 space-y-1.5">
              <AppButton
                v-if="order.status === 'Created' || order.status === 'Paid'"
                variant="outline"
                size="sm"
                block
                :loading="updatingOrderId === order.id"
                @click="handleAdvanceStatus(order.id, 'Confirmed')"
              >
                Confirmar pedido
              </AppButton>

              <AppButton
                v-if="order.status === 'Confirmed'"
                variant="secondary"
                size="sm"
                block
                :loading="updatingOrderId === order.id"
                @click="handleAdvanceStatus(order.id, 'Preparing')"
              >
                <ChefHat class="h-4 w-4" />
                Iniciar Preparo
              </AppButton>

              <AppButton
                v-if="order.status === 'Preparing'"
                variant="primary"
                size="sm"
                block
                :loading="updatingOrderId === order.id"
                @click="handleAdvanceStatus(order.id, 'ReadyForPickup')"
              >
                <Bike class="h-4 w-4" />
                Chamar Entregador
              </AppButton>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Collapsible Manual Order Code Fallback -->
    <div class="rounded-2xl border border-line bg-surface">
      <button
        type="button"
        class="flex w-full items-center justify-between px-4 py-3 text-xs font-bold text-ink-secondary hover:text-ink"
        @click="manualOpen = !manualOpen"
      >
        <span>Atualização manual por código do pedido</span>
        <ChevronDown class="h-4 w-4 transition-transform" :class="{ 'rotate-180': manualOpen }" />
      </button>

      <form
        v-if="manualOpen"
        class="grid gap-3 border-t border-line p-4 sm:grid-cols-3 items-end"
        @submit.prevent="handleManualStatusUpdate"
      >
        <div class="space-y-1">
          <label class="block text-xs font-semibold text-ink-secondary">Código do pedido</label>
          <input
            v-model="manualOrderCode"
            type="text"
            placeholder="Ex: #ab6cd09d"
            list="partner-order-codes"
            class="w-full rounded-xl border border-line-strong bg-surface px-3 py-2 text-xs text-ink"
          />
          <datalist id="partner-order-codes">
            <option
              v-for="order in orders"
              :key="order.id"
              :value="`#${order.id.slice(0, 8)}`"
            >
              #{{ order.id.slice(0, 8) }} — {{ order.customerName || 'Cliente' }}
            </option>
          </datalist>
        </div>
        <div class="space-y-1">
          <label class="block text-xs font-semibold text-ink-secondary">Novo status</label>
          <select
            v-model="manualNextStatus"
            class="w-full rounded-xl border border-line-strong bg-surface px-3 py-2 text-xs text-ink"
          >
            <option value="Confirmed">Confirmado</option>
            <option value="Preparing">Em preparo</option>
            <option value="ReadyForPickup">Pronto para retirada</option>
            <option value="Delivered">Entregue</option>
            <option value="Cancelled">Cancelado</option>
          </select>
        </div>
        <AppButton type="submit" variant="secondary" size="sm" :loading="updatingManual">
          Atualizar status
        </AppButton>
      </form>
    </div>

    <!-- Complaints Lateral Drawer -->
    <AppDrawer
      :open="complaintsDrawerOpen"
      title="Reclamações & Atendimento"
      subtitle="Responda rapidamente sem perder a fila da cozinha de vista."
      width-class="max-w-lg"
      @close="complaintsDrawerOpen = false"
    >
      <div v-if="complaintOrders.length > 0" class="space-y-4">
        <div
          v-for="order in complaintOrders"
          :key="`comp-${order.id}`"
          class="rounded-2xl border border-line bg-surface-subtle p-4 space-y-3 text-xs"
        >
          <div class="flex items-center justify-between gap-2">
            <span class="font-display text-sm font-extrabold text-ink">
              Pedido #{{ order.id.slice(0, 8) }} · {{ order.customerName || 'Cliente' }}
            </span>
            <AppBadge :variant="order.complaintReply ? 'fresh' : 'warning'" size="xs">
              {{ order.complaintReply ? 'Respondida' : 'Pendente' }}
            </AppBadge>
          </div>

          <div class="rounded-xl border border-red-200 bg-red-50/70 p-3 dark:border-red-900/60 dark:bg-red-950/30">
            <p class="font-bold text-red-800 dark:text-red-300">Relato do cliente:</p>
            <p class="mt-1 text-ink">{{ order.complaintMessage }}</p>
          </div>

          <div
            v-if="order.complaintReply"
            class="rounded-xl border border-fresh-200 bg-fresh-50/80 p-3 dark:border-fresh-800/60 dark:bg-fresh-950/30"
          >
            <p class="font-bold text-fresh-800 dark:text-fresh-300">Sua resposta:</p>
            <p class="mt-1 text-ink">{{ order.complaintReply }}</p>
          </div>

          <AppButton
            v-if="replyingOrderId !== order.id"
            variant="secondary"
            size="xs"
            @click="
              replyingOrderId = order.id;
              complaintReplyText = order.complaintReply || '';
            "
          >
            <MessageSquareReply class="h-3.5 w-3.5" />
            {{ order.complaintReply ? 'Editar resposta' : 'Responder cliente' }}
          </AppButton>

          <form v-else class="space-y-2" @submit.prevent="handleReplyComplaint(order.id)">
            <textarea
              v-model="complaintReplyText"
              rows="2"
              required
              placeholder="Escreva a resposta do restaurante..."
              class="w-full rounded-xl border border-line-strong bg-surface px-3 py-2 text-xs text-ink"
            />
            <div class="flex gap-2">
              <AppButton type="submit" variant="primary" size="xs" :loading="sendingReply">
                Enviar resposta
              </AppButton>
              <AppButton type="button" variant="ghost" size="xs" @click="replyingOrderId = null">
                Cancelar
              </AppButton>
            </div>
          </form>
        </div>
      </div>

      <AppEmptyState
        v-else
        :icon="CheckCircle2"
        title="Nenhuma reclamação registrada"
        description="Tudo tranquilo no atendimento aos seus clientes."
      />
    </AppDrawer>

    <!-- Past Orders History Drawer -->
    <AppDrawer
      :open="historyDrawerOpen"
      title="Histórico de Pedidos"
      subtitle="Pedidos entregues, cancelados ou estornados."
      width-class="max-w-lg"
      @close="historyDrawerOpen = false"
    >
      <div v-if="historyOrders.length > 0" class="space-y-3">
        <div
          v-for="ord in historyOrders"
          :key="`hist-${ord.id}`"
          class="rounded-2xl border border-line bg-surface-subtle p-3.5 space-y-2 text-xs"
        >
          <div class="flex items-center justify-between gap-2">
            <span class="font-display font-extrabold text-ink">#{{ ord.id.slice(0, 8) }}</span>
            <OrderStatusBadge :status="ord.status" />
          </div>
          <p class="text-ink-secondary">
            {{ ord.customerName || 'Cliente' }} · {{ new Date(ord.createdAt).toLocaleString('pt-BR') }}
          </p>
          <div class="flex items-center justify-between border-t border-line pt-2">
            <span class="font-bold text-ink tabular-nums">{{ formatCurrency(ord.totalAmount) }}</span>
            <button
              type="button"
              class="inline-flex items-center gap-1 font-semibold text-brand-600 hover:underline"
              @click="printTicketOrder = ord"
            >
              <Printer class="h-3.5 w-3.5" />
              Comanda
            </button>
          </div>
        </div>
      </div>
      <AppEmptyState v-else :icon="CheckCircle2" title="Sem histórico ainda" />
    </AppDrawer>

    <!-- Thermal Kitchen Ticket Print Modal -->
    <AppModal
      :open="Boolean(printTicketOrder)"
      title="Comanda de Cozinha"
      subtitle="Visualização rápida para impressão térmica."
      max-width="sm"
      @close="printTicketOrder = null"
    >
      <div
        v-if="printTicketOrder"
        class="rounded-xl border border-dashed border-line-strong bg-white p-4 font-mono text-xs text-stone-900 space-y-3"
      >
        <div class="border-b border-dashed border-stone-300 pb-2 text-center">
          <p class="text-sm font-extrabold uppercase">{{ printTicketOrder.restaurantName }}</p>
          <p>COMANDA #{{ printTicketOrder.id.slice(0, 8).toUpperCase() }}</p>
          <p class="text-[11px] text-stone-500">
            {{ new Date(printTicketOrder.createdAt).toLocaleString('pt-BR') }}
          </p>
        </div>

        <div class="space-y-1">
          <p><strong>CLIENTE:</strong> {{ printTicketOrder.customerName || 'Cliente' }}</p>
          <p v-if="printTicketOrder.deliveryAddress">
            <strong>ENDEREÇO:</strong> {{ printTicketOrder.deliveryAddress }}
          </p>
        </div>

        <div class="border-y border-dashed border-stone-300 py-2 space-y-1">
          <div
            v-for="item in printTicketOrder.items"
            :key="item.id"
            class="flex justify-between gap-2"
          >
            <span>{{ item.quantity }}x {{ item.productName }}</span>
            <span>{{ formatCurrency(item.totalPrice) }}</span>
          </div>
        </div>

        <div class="flex justify-between text-sm font-extrabold">
          <span>TOTAL</span>
          <span>{{ formatCurrency(printTicketOrder.totalAmount) }}</span>
        </div>
      </div>

      <template #footer>
        <div class="flex justify-end gap-2">
          <AppButton variant="outline" size="sm" @click="printTicketOrder = null">Fechar</AppButton>
          <AppButton variant="primary" size="sm" @click="triggerBrowserPrint">
            <Printer class="h-4 w-4" />
            Imprimir comanda
          </AppButton>
        </div>
      </template>
    </AppModal>
  </div>
</template>
