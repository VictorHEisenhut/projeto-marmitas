<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { ArrowRight, Search, ShoppingBag, Store, Truck, Users } from 'lucide-vue-next'
import AppModal from '@/components/ui/AppModal.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import type { AdminModule } from '@/layouts/AdminLayout.vue'
import type { Courier, Order, Restaurant, User } from '@/types'

const props = defineProps<{
  open: boolean
  restaurants: Restaurant[]
  users: User[]
  couriers: Courier[]
  orders: Order[]
}>()

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'navigate', payload: { module: AdminModule; itemId?: string }): void
}>()

const query = ref('')
const inputRef = ref<HTMLInputElement | null>(null)

watch(
  () => props.open,
  (isOpen) => {
    if (isOpen) {
      query.value = ''
      setTimeout(() => {
        inputRef.value?.focus()
      }, 40)
    }
  },
)

function onGlobalKeydown(e: KeyboardEvent) {
  if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
    e.preventDefault()
    if (props.open) {
      emit('close')
    } else {
      emit('navigate', { module: 'restaurantes' })
    }
  }
}

const results = computed(() => {
  const q = query.value.trim().toLowerCase()
  const items: Array<{
    id: string
    module: AdminModule
    category: string
    title: string
    subtitle: string
    icon: typeof Store
  }> = []

  for (const r of props.restaurants) {
    if (!q || r.name.toLowerCase().includes(q) || r.document.toLowerCase().includes(q)) {
      items.push({
        id: r.id,
        module: 'restaurantes',
        category: 'Restaurante',
        title: r.name,
        subtitle: `CNPJ: ${r.document} · ${r.status}`,
        icon: Store,
      })
    }
  }

  for (const o of props.orders) {
    if (
      !q ||
      o.id.toLowerCase().includes(q) ||
      o.restaurantName.toLowerCase().includes(q) ||
      (o.customerName && o.customerName.toLowerCase().includes(q))
    ) {
      items.push({
        id: o.id,
        module: 'pedidos',
        category: 'Pedido',
        title: `Pedido #${o.id.slice(0, 8)} — ${o.restaurantName}`,
        subtitle: `${o.customerName || 'Cliente'} · ${o.status}`,
        icon: ShoppingBag,
      })
    }
  }

  for (const u of props.users) {
    if (!q || u.name.toLowerCase().includes(q) || u.email.toLowerCase().includes(q)) {
      items.push({
        id: u.id,
        module: 'usuarios',
        category: 'Usuário',
        title: u.name,
        subtitle: `${u.email} · ${u.roles.join(', ')}`,
        icon: Users,
      })
    }
  }

  for (const c of props.couriers) {
    if (!q || c.name.toLowerCase().includes(q) || c.document.toLowerCase().includes(q)) {
      items.push({
        id: c.id,
        module: 'usuarios',
        category: 'Entregador',
        title: c.name,
        subtitle: `Doc: ${c.document} · ${c.status}`,
        icon: Truck,
      })
    }
  }

  return items.slice(0, 12)
})

function selectItem(module: AdminModule, itemId: string) {
  emit('navigate', { module, itemId })
  emit('close')
}

onMounted(() => {
  window.addEventListener('keydown', onGlobalKeydown)
})

onUnmounted(() => {
  window.removeEventListener('keydown', onGlobalKeydown)
})
</script>

<template>
  <AppModal
    :open="open"
    title="Busca Rápida Administrativa"
    subtitle="Encontre pedidos por UUID, clientes por e-mail, restaurantes por CNPJ ou entregadores."
    max-width="lg"
    @close="emit('close')"
  >
    <div class="space-y-4">
      <div class="relative">
        <Search class="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-muted" />
        <input
          ref="inputRef"
          v-model="query"
          type="search"
          placeholder="Digite nome, e-mail, CNPJ ou #UUID..."
          class="w-full rounded-xl border border-line-strong bg-surface py-3 pl-10 pr-4 text-sm text-ink placeholder:text-ink-muted focus:border-brand-600 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
        />
      </div>

      <ul v-if="results.length > 0" class="max-h-80 divide-y divide-line overflow-y-auto rounded-2xl border border-line bg-surface-subtle">
        <li v-for="item in results" :key="`${item.category}-${item.id}`">
          <button
            type="button"
            class="flex w-full items-center justify-between gap-3 px-4 py-3 text-left transition hover:bg-surface-muted"
            @click="selectItem(item.module, item.id)"
          >
            <div class="flex items-center gap-3 min-w-0">
              <span class="flex h-8 w-8 shrink-0 items-center justify-center rounded-xl bg-surface text-brand-600 border border-line">
                <component :is="item.icon" class="h-4 w-4" />
              </span>
              <div class="min-w-0">
                <p class="truncate text-sm font-bold text-ink">{{ item.title }}</p>
                <p class="truncate text-xs text-ink-muted">{{ item.subtitle }}</p>
              </div>
            </div>
            <div class="flex items-center gap-2 shrink-0">
              <AppBadge variant="neutral" size="xs">{{ item.category }}</AppBadge>
              <ArrowRight class="h-3.5 w-3.5 text-ink-muted" />
            </div>
          </button>
        </li>
      </ul>

      <p v-else class="py-8 text-center text-xs text-ink-muted">
        Nenhum registro encontrado para "{{ query }}".
      </p>
    </div>
  </AppModal>
</template>
