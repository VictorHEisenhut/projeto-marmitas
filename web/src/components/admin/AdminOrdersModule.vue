<script setup lang="ts">
import { ref, watch } from 'vue'
import { ShoppingBag } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppDrawer from '@/components/ui/AppDrawer.vue'
import AppEmptyState from '@/components/ui/AppEmptyState.vue'
import AppConfirmDialog from '@/components/ui/AppConfirmDialog.vue'
import OrderStatusBadge from '@/components/ui/OrderStatusBadge.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import type { Order } from '@/types'

const props = defineProps<{
  orders: Order[]
  focusedItemId?: string | null
}>()

const emit = defineEmits<{
  (e: 'refresh'): void
  (e: 'clear-focus'): void
}>()

const toast = useToastStore()

type SupportAction = {
  type: 'force-deliver' | 'cancel' | 'refund'
  order: Order
}

const inspectorOrder = ref<Order | null>(null)
const pendingAction = ref<SupportAction | null>(null)
const executing = ref(false)

watch(
  () => props.focusedItemId,
  (id) => {
    if (!id) return
    const ord = props.orders.find((o) => o.id === id)
    if (ord) {
      inspectorOrder.value = ord
      emit('clear-focus')
    }
  },
  { immediate: true },
)

async function handleExecuteAction() {
  if (!pendingAction.value) return
  const { type, order } = pendingAction.value
  executing.value = true

  try {
    if (type === 'force-deliver') {
      await api.patch(`/entregas/${order.id}/status`, { status: 'Delivered' })
      toast.success(`Entrega #${order.id.slice(0, 8)} finalizada excepcionalmente pelo suporte.`)
    } else if (type === 'cancel') {
      await api.patch(`/pedidos/${order.id}/cancelar`)
      toast.info(`Pedido #${order.id.slice(0, 8)} cancelado.`)
    } else if (type === 'refund') {
      await api.post(`/pagamentos/${order.id}/estornar`, {
        reason: 'Estorno administrativo solicitado pelo painel',
      })
      toast.success(`Pagamento do pedido #${order.id.slice(0, 8)} estornado com sucesso.`)
    }
    pendingAction.value = null
    inspectorOrder.value = null
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível concluir a operação de suporte.'))
  } finally {
    executing.value = false
  }
}
</script>

<template>
  <AppCard padding="lg" class="space-y-4">
    <div>
      <h2 class="font-display text-base font-extrabold text-ink">
        Monitoramento Global de Pedidos & Estornos ({{ orders.length }})
      </h2>
      <p class="text-xs text-ink-muted">
        Clique em qualquer pedido na tabela para abrir o Inspector com ações de suporte, cancelamento e estorno.
      </p>
    </div>

    <div v-if="orders.length > 0" class="overflow-x-auto rounded-2xl border border-line">
      <table class="w-full text-left text-xs">
        <thead class="border-b border-line bg-surface-subtle text-[11px] font-bold uppercase tracking-wider text-ink-muted">
          <tr>
            <th class="px-4 py-3">Pedido</th>
            <th class="px-4 py-3">Restaurante</th>
            <th class="px-4 py-3 hidden md:table-cell">Cliente</th>
            <th class="px-4 py-3">Status</th>
            <th class="px-4 py-3 text-right">Total</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-line">
          <tr
            v-for="ord in orders"
            :key="ord.id"
            class="cursor-pointer transition hover:bg-surface-muted/60"
            @click="inspectorOrder = ord"
          >
            <td class="px-4 py-3 font-mono font-bold text-ink">#{{ ord.id.slice(0, 8) }}</td>
            <td class="px-4 py-3 font-semibold text-ink">{{ ord.restaurantName }}</td>
            <td class="px-4 py-3 hidden md:table-cell text-ink-secondary">{{ ord.customerName || ord.customerId.slice(0, 8) }}</td>
            <td class="px-4 py-3"><OrderStatusBadge :status="ord.status" /></td>
            <td class="px-4 py-3 text-right font-display font-extrabold text-ink tabular-nums">
              {{ formatCurrency(ord.totalAmount) }}
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <AppEmptyState v-else :icon="ShoppingBag" title="Nenhum pedido registrado no momento" />

    <!-- Order Inspector Drawer -->
    <AppDrawer
      :open="Boolean(inspectorOrder)"
      :title="`Pedido #${inspectorOrder?.id.slice(0, 8)}`"
      :subtitle="inspectorOrder?.restaurantName"
      width-class="max-w-md"
      @close="inspectorOrder = null"
    >
      <div v-if="inspectorOrder" class="space-y-5 text-xs">
        <div class="flex items-center justify-between rounded-2xl border border-line bg-surface-subtle p-4">
          <OrderStatusBadge :status="inspectorOrder.status" />
          <span class="font-display text-lg font-extrabold text-ink tabular-nums">
            {{ formatCurrency(inspectorOrder.totalAmount) }}
          </span>
        </div>

        <div class="space-y-2 rounded-2xl border border-line bg-surface-subtle p-4">
          <p><strong class="text-ink">Cliente:</strong> {{ inspectorOrder.customerName || inspectorOrder.customerId }}</p>
          <p><strong class="text-ink">Criado em:</strong> {{ new Date(inspectorOrder.createdAt).toLocaleString('pt-BR') }}</p>
          <p v-if="inspectorOrder.deliveryAddress"><strong class="text-ink">Entrega:</strong> {{ inspectorOrder.deliveryAddress }}</p>
          <p v-if="inspectorOrder.confirmationPin"><strong class="text-ink">PIN:</strong> <span class="font-mono font-bold">{{ inspectorOrder.confirmationPin }}</span></p>
        </div>

        <ul v-if="inspectorOrder.items?.length" class="divide-y divide-line rounded-2xl border border-line bg-surface-subtle px-4">
          <li v-for="item in inspectorOrder.items" :key="item.id" class="flex justify-between py-2.5">
            <span><strong>{{ item.quantity }}x</strong> {{ item.productName }}</span>
            <span class="font-bold tabular-nums">{{ formatCurrency(item.totalPrice) }}</span>
          </li>
        </ul>

        <div class="space-y-2 pt-2">
          <AppButton
            v-if="['DriverAssigned', 'PickedUp', 'InTransit'].includes(inspectorOrder.status)"
            variant="fresh"
            size="md"
            block
            @click="pendingAction = { type: 'force-deliver', order: inspectorOrder }"
          >
            Finalizar entrega (Suporte s/ PIN)
          </AppButton>

          <AppButton
            v-if="!['Cancelled', 'Refunded', 'Delivered'].includes(inspectorOrder.status)"
            variant="outline"
            size="md"
            block
            class="text-red-600 dark:text-red-400"
            @click="pendingAction = { type: 'cancel', order: inspectorOrder }"
          >
            Cancelar pedido
          </AppButton>

          <AppButton
            v-if="inspectorOrder.status !== 'Refunded'"
            variant="danger"
            size="md"
            block
            @click="pendingAction = { type: 'refund', order: inspectorOrder }"
          >
            Estornar pagamento
          </AppButton>
        </div>
      </div>
    </AppDrawer>

    <AppConfirmDialog
      :open="Boolean(pendingAction)"
      :title="
        pendingAction?.type === 'force-deliver'
          ? 'Finalizar entrega sem PIN?'
          : pendingAction?.type === 'cancel'
            ? 'Cancelar pedido administrativamente?'
            : 'Estornar pagamento do pedido?'
      "
      :message="
        pendingAction?.type === 'force-deliver'
          ? `Confirmar liberação excepcional da entrega do pedido #${pendingAction?.order.id.slice(0, 8)} sem exigir o PIN do cliente?`
          : pendingAction?.type === 'cancel'
            ? `Tem certeza que deseja cancelar o pedido #${pendingAction?.order.id.slice(0, 8)}?`
            : `Esta ação marcará o pedido #${pendingAction?.order.id.slice(0, 8)} como Refunded e lançará um débito compensatório no Ledger financeiro.`
      "
      :confirm-label="
        pendingAction?.type === 'force-deliver'
          ? 'Finalizar entrega'
          : pendingAction?.type === 'cancel'
            ? 'Cancelar pedido'
            : 'Confirmar estorno'
      "
      :variant="pendingAction?.type === 'force-deliver' ? 'primary' : 'danger'"
      :loading="executing"
      @confirm="handleExecuteAction"
      @cancel="pendingAction = null"
    />
  </AppCard>
</template>
