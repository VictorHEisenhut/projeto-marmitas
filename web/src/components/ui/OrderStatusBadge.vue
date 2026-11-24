<script setup lang="ts">
import { computed } from 'vue'
import AppBadge, { type BadgeVariant } from './AppBadge.vue'

const props = defineProps<{
  status: string
}>()

const meta = computed<{ label: string; variant: BadgeVariant; dot?: boolean }>(() => {
  const map: Record<string, { label: string; variant: BadgeVariant; dot?: boolean }> = {
    Created: { label: 'Criado', variant: 'neutral' },
    Pending: { label: 'Pendente', variant: 'warning', dot: true },
    PaymentPending: { label: 'Aguardando pagamento', variant: 'warning', dot: true },
    Paid: { label: 'Pago', variant: 'info', dot: true },
    Confirmed: { label: 'Confirmado', variant: 'info', dot: true },
    Preparing: { label: 'Em preparo', variant: 'brand', dot: true },
    ReadyForPickup: { label: 'Pronto p/ coleta', variant: 'warning', dot: true },
    DriverAssigned: { label: 'Entregador alocado', variant: 'brand', dot: true },
    PickedUp: { label: 'Coletado', variant: 'brand', dot: true },
    InTransit: { label: 'Em rota', variant: 'brand', dot: true },
    Delivered: { label: 'Entregue', variant: 'fresh', dot: true },
    Cancelled: { label: 'Cancelado', variant: 'danger' },
    Refunded: { label: 'Estornado', variant: 'danger' },
    Failed: { label: 'Falha no pagamento', variant: 'danger' },
    Active: { label: 'Ativa', variant: 'fresh', dot: true },
    Paused: { label: 'Pausada', variant: 'warning', dot: true },
    GracePeriod: { label: 'Carência', variant: 'warning', dot: true },
    Approved: { label: 'Aprovado', variant: 'fresh', dot: true },
    Rejected: { label: 'Pausado / Rejeitado', variant: 'danger' },
    Processed: { label: 'Processado', variant: 'fresh' },
  }
  return map[props.status] || { label: props.status, variant: 'neutral' }
})
</script>

<template>
  <AppBadge :variant="meta.variant" :dot="meta.dot">
    {{ meta.label }}
  </AppBadge>
</template>
