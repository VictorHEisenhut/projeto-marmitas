<script setup lang="ts">
import { computed, ref } from 'vue'
import {
  AlertCircle,
  Bike,
  CheckCircle2,
  ChevronDown,
  ChefHat,
  ClipboardList,
  Clock,
  MessageSquareReply,
  PackageCheck,
} from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
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

type QueueTab = 'new' | 'preparing' | 'ready' | 'transit' | 'history' | 'complaints'
const activeTab = ref<QueueTab>('new')

const updatingOrderId = ref<string | null>(null)
const replyingOrderId = ref<string | null>(null)
const complaintReplyText = ref('')
const sendingReply = ref(false)

// Secondary collapsible manual status update
const manualOpen = ref(false)
const manualOrderId = ref('')
const manualNextStatus = ref('Preparing')
const updatingManual = ref(false)

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

const queueTabs = computed<TabItem[]>(() => [
  { id: 'new', label: 'Novos / Pagos', count: newOrders.value.length, icon: Clock },
  { id: 'preparing', label: 'Em Preparo', count: preparingOrders.value.length, icon: ChefHat },
  { id: 'ready', label: 'Prontos', count: readyOrders.value.length, icon: PackageCheck },
  { id: 'transit', label: 'Em Rota', count: transitOrders.value.length, icon: Bike },
  { id: 'history', label: 'Histórico', count: historyOrders.value.length, icon: CheckCircle2 },
  {
    id: 'complaints',
    label: 'Reclamações',
    count: unansweredComplaints.value > 0 ? unansweredComplaints.value : complaintOrders.value.length,
    badgeVariant: unansweredComplaints.value > 0 ? 'danger' : 'neutral',
    icon: AlertCircle,
  },
])

const visibleOrders = computed(() => {
  const map: Record<QueueTab, Order[]> = {
    new: newOrders.value,
    preparing: preparingOrders.value,
    ready: readyOrders.value,
    transit: transitOrders.value,
    history: historyOrders.value,
    complaints: complaintOrders.value,
  }
  return map[activeTab.value]
})

async function handleAdvanceStatus(orderId: string, status: string) {
  updatingOrderId.value = orderId
  try {
    await api.post(`/pedidos/${orderId}/historico-status`, { status })
    toast.success(
      status === 'ReadyForPickup'
        ? 'Entregador chamado! Pedido liberado na fila de coleta.'
        : 'Status do pedido atualizado com sucesso.',
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
  if (!manualOrderId.value.trim()) {
    toast.warning('Informe o UUID do pedido.')
    return
  }
  updatingManual.value = true
  try {
    await api.post(`/pedidos/${manualOrderId.value.trim()}/historico-status`, {
      status: manualNextStatus.value,
    })
    manualOrderId.value = ''
    toast.success('Status atualizado manualmente.')
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível atualizar o status do pedido.'))
  } finally {
    updatingManual.value = false
  }
}
</script>

<template>
  <div class="space-y-5">
    <!-- Kanban / Queue Tabs -->
    <AppTabs v-model="activeTab" :tabs="queueTabs" variant="pills" />

    <!-- Orders Queue Grid -->
    <div v-if="visibleOrders.length > 0" class="grid gap-4 lg:grid-cols-2">
      <AppCard
        v-for="order in visibleOrders"
        :key="order.id"
        padding="md"
        class="flex flex-col justify-between space-y-4"
        :class="
          ['Paid', 'Created'].includes(order.status)
            ? 'border-brand-300 ring-1 ring-brand-500/15 dark:border-brand-800/70'
            : ''
        "
      >
        <div class="space-y-3">
          <!-- Order Top Row -->
          <div class="flex items-start justify-between gap-2 border-b border-line pb-3">
            <div>
              <div class="flex flex-wrap items-center gap-2">
                <span class="font-display text-base font-extrabold text-ink">
                  Pedido #{{ order.id.slice(0, 8) }}
                </span>
                <OrderStatusBadge :status="order.status" />
              </div>
              <p class="mt-1 text-xs text-ink-secondary">
                <strong class="text-ink">{{ order.customerName || 'Cliente' }}</strong>
                · {{ new Date(order.createdAt).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' }) }}
              </p>
            </div>

            <span class="font-display text-lg font-extrabold text-ink tabular-nums shrink-0">
              {{ formatCurrency(order.totalAmount) }}
            </span>
          </div>

          <!-- Items -->
          <ul
            v-if="order.items && order.items.length > 0"
            class="space-y-1 rounded-xl bg-surface-subtle p-3 text-xs text-ink-secondary"
          >
            <li
              v-for="item in order.items"
              :key="item.id"
              class="flex items-center justify-between gap-2"
            >
              <span>
                <strong class="text-ink">{{ item.quantity }}x</strong> {{ item.productName }}
              </span>
              <span class="font-semibold text-ink tabular-nums">
                {{ formatCurrency(item.totalPrice) }}
              </span>
            </li>
          </ul>

          <!-- Customer Complaint Panel -->
          <div
            v-if="order.complaintMessage"
            class="space-y-2.5 rounded-xl border border-red-200 bg-red-50/70 p-3.5 text-xs dark:border-red-900/60 dark:bg-red-950/30"
          >
            <div class="flex items-center justify-between gap-2">
              <span class="inline-flex items-center gap-1.5 font-bold text-red-800 dark:text-red-300">
                <AlertCircle class="h-4 w-4" />
                Reclamação do Cliente
              </span>
              <AppBadge :variant="order.complaintReply ? 'fresh' : 'warning'" size="xs">
                {{ order.complaintReply ? 'Respondida' : 'Pendente' }}
              </AppBadge>
            </div>
            <p class="text-ink leading-relaxed">{{ order.complaintMessage }}</p>

            <div
              v-if="order.complaintReply"
              class="rounded-xl border border-fresh-200 bg-fresh-50/80 p-2.5 dark:border-fresh-800/60 dark:bg-fresh-950/30"
            >
              <p class="font-bold text-fresh-800 dark:text-fresh-300">Sua resposta:</p>
              <p class="mt-0.5 text-ink">{{ order.complaintReply }}</p>
            </div>

            <div>
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

              <form v-else class="mt-2 space-y-2" @submit.prevent="handleReplyComplaint(order.id)">
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
        </div>

        <!-- 1-Click Workflow Transitions -->
        <div
          v-if="!['Delivered', 'Cancelled', 'Refunded', 'Failed'].includes(order.status)"
          class="flex flex-wrap items-center gap-2 border-t border-line pt-3"
        >
          <AppButton
            v-if="order.status === 'Created' || order.status === 'Paid'"
            variant="outline"
            size="sm"
            :loading="updatingOrderId === order.id"
            @click="handleAdvanceStatus(order.id, 'Confirmed')"
          >
            Confirmar
          </AppButton>

          <AppButton
            v-if="['Created', 'Paid', 'Confirmed'].includes(order.status)"
            variant="secondary"
            size="sm"
            :loading="updatingOrderId === order.id"
            @click="handleAdvanceStatus(order.id, 'Preparing')"
          >
            <ChefHat class="h-4 w-4" />
            Iniciar Preparo
          </AppButton>

          <AppButton
            v-if="['Created', 'Paid', 'Confirmed', 'Preparing'].includes(order.status)"
            variant="primary"
            size="sm"
            class="flex-1 sm:flex-none"
            :loading="updatingOrderId === order.id"
            @click="handleAdvanceStatus(order.id, 'ReadyForPickup')"
          >
            <Bike class="h-4 w-4" />
            Chamar Entregador
          </AppButton>
        </div>
      </AppCard>
    </div>

    <AppEmptyState
      v-else
      :icon="ClipboardList"
      title="Nenhum pedido nesta fila"
      description="Novos pedidos chegam automaticamente aqui em tempo real via SignalR."
    />

    <!-- Collapsible Secondary Tool: Manual UUID Status Update -->
    <div class="rounded-2xl border border-line bg-surface">
      <button
        type="button"
        class="flex w-full items-center justify-between px-4 py-3 text-xs font-bold text-ink-secondary hover:text-ink"
        @click="manualOpen = !manualOpen"
      >
        <span>Atualização manual por UUID do pedido (avançado)</span>
        <ChevronDown class="h-4 w-4 transition-transform" :class="{ 'rotate-180': manualOpen }" />
      </button>

      <form
        v-if="manualOpen"
        class="grid gap-3 border-t border-line p-4 sm:grid-cols-3 items-end"
        @submit.prevent="handleManualStatusUpdate"
      >
        <div class="space-y-1">
          <label class="block text-xs font-semibold text-ink-secondary">UUID do pedido</label>
          <input
            v-model="manualOrderId"
            type="text"
            placeholder="00000000-0000-..."
            class="w-full rounded-xl border border-line-strong bg-surface px-3 py-2 text-xs text-ink"
          />
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
  </div>
</template>
