<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ArrowRight, Minus, Plus, ShoppingBag, Sparkles, Trash2, UtensilsCrossed } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppConfirmDialog from '@/components/ui/AppConfirmDialog.vue'
import { formatCurrency } from '@/services/api'
import { useCartStore } from '@/stores/cart'
import { useAuthStore } from '@/stores/auth'

const props = defineProps<{
  restaurantId: string
  restaurantName: string
  deliveryFee?: number
  isPlanMode: boolean
}>()

const cart = useCartStore()
const auth = useAuthStore()
const router = useRouter()

const confirmClearOpen = ref(false)

const isCurrentStoreCart = computed(
  () => cart.items.length > 0 && cart.restaurantId === props.restaurantId,
)

const freeDeliveryTarget = 60
const freeDeliveryRemaining = computed(() =>
  Math.max(0, freeDeliveryTarget - cart.totalAmount),
)
const freeDeliveryProgress = computed(() =>
  Math.min(100, Math.round((cart.totalAmount / freeDeliveryTarget) * 100)),
)

function goToCheckout() {
  if (!auth.isAuthenticated) {
    void router.push({ path: '/login', query: { redirect: '/pedidos' } })
    return
  }
  void router.push('/pedidos')
}

function handleClear() {
  cart.clear()
  confirmClearOpen.value = false
}
</script>

<template>
  <aside class="sticky top-20">
    <AppCard padding="none" class="overflow-hidden">
      <!-- Header -->
      <div class="flex items-center justify-between border-b border-line bg-surface-subtle px-5 py-4">
        <div class="flex items-center gap-2.5">
          <span class="flex h-9 w-9 items-center justify-center rounded-xl bg-brand-600 text-white shadow-xs">
            <ShoppingBag class="h-4 w-4" />
          </span>
          <div>
            <h2 class="font-display text-sm font-extrabold text-ink">Sacola Viva</h2>
            <p class="text-[11px] text-ink-muted truncate max-w-[165px]">
              {{ cart.items.length > 0 ? cart.restaurantName : restaurantName }}
            </p>
          </div>
        </div>
        <button
          v-if="cart.items.length > 0"
          type="button"
          class="text-[11px] font-bold text-red-600 hover:underline dark:text-red-400"
          @click="confirmClearOpen = true"
        >
          Limpar
        </button>
      </div>

      <!-- Empty State -->
      <div v-if="cart.items.length === 0" class="px-5 py-10 text-center space-y-2.5">
        <div class="mx-auto flex h-12 w-12 items-center justify-center rounded-2xl bg-surface-muted text-ink-muted">
          <UtensilsCrossed class="h-6 w-6" />
        </div>
        <p class="font-display text-sm font-bold text-ink">Sua sacola está vazia</p>
        <p class="text-xs leading-relaxed text-ink-muted">
          Clique em qualquer marmita ao lado para montar seu pedido sem sair do cardápio.
        </p>
      </div>

      <!-- Populated Cart -->
      <div v-else class="divide-y divide-line">
        <!-- Contextual Progress Banner -->
        <div
          v-if="cart.isPlanRedemption"
          class="flex items-start gap-2 bg-fresh-50/70 px-4 py-3 text-[11px] text-fresh-800 dark:bg-fresh-950/35 dark:text-fresh-300"
        >
          <Sparkles class="mt-0.5 h-3.5 w-3.5 shrink-0 text-fresh-600" />
          <span>
            <strong>Resgate de Plano ativo:</strong> base R$ 0,00 · apenas adicionais e talheres pagos somam no subtotal.
          </span>
        </div>
        <div
          v-else-if="deliveryFee && deliveryFee > 0"
          class="bg-surface-subtle px-4 py-3 space-y-1.5"
        >
          <div class="flex items-center justify-between text-[11px]">
            <span class="font-semibold text-ink-secondary">
              {{
                freeDeliveryRemaining > 0
                  ? `Faltam ${formatCurrency(freeDeliveryRemaining)} p/ pedido ideal`
                  : 'Ótimo pedido! Pronto para finalizar'
              }}
            </span>
            <span class="font-bold text-brand-600 tabular-nums">{{ freeDeliveryProgress }}%</span>
          </div>
          <div class="h-1.5 w-full overflow-hidden rounded-full bg-surface-muted">
            <div
              class="h-full rounded-full bg-brand-600 transition-all duration-300"
              :style="{ width: `${freeDeliveryProgress}%` }"
            />
          </div>
        </div>

        <!-- Warning if cart belongs to another store -->
        <div
          v-if="!isCurrentStoreCart"
          class="bg-amber-50/80 px-4 py-2.5 text-[11px] font-medium text-amber-900 dark:bg-amber-950/35 dark:text-amber-300"
        >
          Itens de <strong>{{ cart.restaurantName }}</strong> na sacola. Adicionar daqui substituirá a sacola atual.
        </div>

        <!-- Items List -->
        <ul class="max-h-[340px] divide-y divide-line overflow-y-auto px-4">
          <li
            v-for="(item, idx) in cart.items"
            :key="`${item.productId}-${idx}`"
            class="py-3 space-y-2"
          >
            <div class="flex items-start justify-between gap-2">
              <div class="min-w-0">
                <p class="text-xs font-bold text-ink leading-snug">{{ item.name }}</p>
                <AppBadge v-if="cart.isPlanRedemption" variant="fresh" size="xs" class="mt-1">
                  Plano R$ 0,00
                </AppBadge>
                <p
                  v-if="(item.selectedAddons && item.selectedAddons.length > 0) || item.wantsCutlery"
                  class="mt-0.5 text-[10px] text-ink-muted leading-snug"
                >
                  <span v-if="item.selectedAddons?.length">
                    {{ item.selectedAddons.map((a) => `${a.quantity}x ${a.name}`).join(', ') }}
                  </span>
                  <span v-if="item.selectedAddons?.length && item.wantsCutlery"> · </span>
                  <span v-if="item.wantsCutlery">Com talheres</span>
                </p>
              </div>
              <button
                type="button"
                aria-label="Remover item"
                class="rounded-lg p-1 text-ink-muted hover:bg-red-50 hover:text-red-600 dark:hover:bg-red-950/40"
                @click="cart.removeItemByIndex(idx)"
              >
                <Trash2 class="h-3.5 w-3.5" />
              </button>
            </div>

            <div class="flex items-center justify-between">
              <span class="font-display text-xs font-extrabold text-ink tabular-nums">
                {{ formatCurrency(cart.getItemEffectivePrice(item) * item.quantity) }}
              </span>

              <div class="inline-flex items-center gap-1 rounded-lg border border-line bg-surface-subtle p-0.5">
                <button
                  type="button"
                  aria-label="Diminuir quantidade"
                  class="flex h-6 w-6 items-center justify-center rounded-md text-ink-secondary hover:bg-surface"
                  @click="cart.updateItemQuantityByIndex(idx, item.quantity - 1)"
                >
                  <Minus class="h-3 w-3" />
                </button>
                <span class="min-w-[1.25rem] text-center text-xs font-bold text-ink tabular-nums">
                  {{ item.quantity }}
                </span>
                <button
                  type="button"
                  aria-label="Aumentar quantidade"
                  class="flex h-6 w-6 items-center justify-center rounded-md text-brand-600 hover:bg-surface"
                  @click="cart.updateItemQuantityByIndex(idx, item.quantity + 1)"
                >
                  <Plus class="h-3 w-3" />
                </button>
              </div>
            </div>
          </li>
        </ul>

        <!-- Footer CTA -->
        <div class="bg-surface-subtle p-4 space-y-3">
          <div class="flex items-center justify-between text-xs">
            <span class="text-ink-secondary">
              Subtotal ({{ cart.totalItems }} {{ cart.totalItems === 1 ? 'item' : 'itens' }})
            </span>
            <span class="font-display text-lg font-extrabold text-ink tabular-nums">
              {{ formatCurrency(cart.totalAmount) }}
            </span>
          </div>

          <AppButton variant="primary" size="lg" block @click="goToCheckout">
            <span>{{ auth.isAuthenticated ? 'Ir para pagamento' : 'Entrar e finalizar' }}</span>
            <ArrowRight class="h-4 w-4" />
          </AppButton>
        </div>
      </div>
    </AppCard>
  </aside>

  <AppConfirmDialog
    :open="confirmClearOpen"
    title="Limpar sacola?"
    message="Todos os itens adicionados serão removidos da sua sacola."
    confirm-label="Sim, limpar"
    variant="danger"
    @confirm="handleClear"
    @cancel="confirmClearOpen = false"
  />
</template>
