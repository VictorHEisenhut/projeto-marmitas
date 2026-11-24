<script setup lang="ts">
import { ref } from 'vue'
import { ShoppingBag } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppEmptyState from '@/components/ui/AppEmptyState.vue'
import AppConfirmDialog from '@/components/ui/AppConfirmDialog.vue'
import OrderStatusBadge from '@/components/ui/OrderStatusBadge.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import type { Order } from '@/types'

defineProps<{
  orders: Order[]
}>()

const emit = defineEmits<{
  (e: 'refresh'): void
}>()

const toast = useToastStore()

type SupportAction = {
  type: 'force-deliver' | 'cancel' | 'refund'
  order: Order
}

const pendingAction = ref<SupportAction | null>(null)
const executing = ref(false)

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
        Execute liberações excepcionais sem PIN, cancelamentos administrativos ou estornos com registro no Ledger.
      </p>
    </div>

    <div v-if="orders.length > 0" class="space-y-3">
      <div
        v-for="ord in orders"
        :key="ord.id"
        class="flex flex-col justify-between gap-3 rounded-2xl border border-line bg-surface-subtle p-4 lg:flex-row lg:items-center"
      >
        <div class="space-y-1 min-w-0">
          <div class="flex flex-wrap items-center gap-2">
            <span class="font-display text-sm font-extrabold text-ink">
              Pedido #{{ ord.id.slice(0, 8) }}
            </span>
            <OrderStatusBadge :status="ord.status" />
            <span class="font-display text-sm font-extrabold text-ink tabular-nums">
              {{ formatCurrency(ord.totalAmount) }}
            </span>
          </div>
          <p class="text-xs text-ink-secondary">
            <strong class="text-ink">{{ ord.restaurantName }}</strong> · Cliente: {{ ord.customerName || ord.customerId }} · {{ new Date(ord.createdAt).toLocaleString('pt-BR') }}
          </p>
          <p v-if="ord.deliveryAddress" class="text-[11px] text-ink-muted truncate">
            Entrega: {{ ord.deliveryAddress }}
          </p>
        </div>

        <div class="flex flex-wrap shrink-0 items-center gap-2">
          <AppButton
            v-if="['DriverAssigned', 'PickedUp', 'InTransit'].includes(ord.status)"
            variant="fresh"
            size="xs"
            @click="pendingAction = { type: 'force-deliver', order: ord }"
          >
            Finalizar entrega s/ PIN
          </AppButton>

          <AppButton
            v-if="!['Cancelled', 'Refunded', 'Delivered'].includes(ord.status)"
            variant="outline"
            size="xs"
            class="text-red-600 dark:text-red-400"
            @click="pendingAction = { type: 'cancel', order: ord }"
          >
            Cancelar pedido
          </AppButton>

          <AppButton
            v-if="ord.status !== 'Refunded'"
            variant="secondary"
            size="xs"
            @click="pendingAction = { type: 'refund', order: ord }"
          >
            Estornar pagamento
          </AppButton>
        </div>
      </div>
    </div>

    <AppEmptyState v-else :icon="ShoppingBag" title="Nenhum pedido registrado no momento" />

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
