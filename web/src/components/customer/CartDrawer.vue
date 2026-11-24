<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { ArrowRight, Minus, Plus, ShoppingBag, Sparkles, Store, Trash2, UtensilsCrossed } from 'lucide-vue-next'
import AppDrawer from '@/components/ui/AppDrawer.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppConfirmDialog from '@/components/ui/AppConfirmDialog.vue'
import { formatCurrency } from '@/services/api'
import { useCartStore } from '@/stores/cart'
import { useAuthStore } from '@/stores/auth'

const cart = useCartStore()
const auth = useAuthStore()
const router = useRouter()

const confirmClearOpen = ref(false)

function goToCheckout() {
  cart.closeDrawer()
  if (!auth.isAuthenticated) {
    void router.push({ path: '/login', query: { redirect: '/pedidos' } })
    return
  }
  void router.push('/pedidos')
}

function goToRestaurant() {
  if (!cart.restaurantId) return
  cart.closeDrawer()
  void router.push(`/restaurantes/${cart.restaurantId}`)
}

function confirmClearCart() {
  cart.clear()
  confirmClearOpen.value = false
}
</script>

<template>
  <AppDrawer
    :open="cart.drawerOpen"
    title="Sua Sacola"
    :subtitle="cart.restaurantName || 'Adicione marmitas para começar'"
    @close="cart.closeDrawer()"
  >
    <div v-if="cart.items.length === 0" class="flex h-full flex-col items-center justify-center py-12 text-center">
      <div class="mb-4 flex h-16 w-16 items-center justify-center rounded-3xl bg-brand-50 text-brand-600 dark:bg-brand-950/60 dark:text-brand-400">
        <ShoppingBag class="h-8 w-8" />
      </div>
      <h3 class="font-display text-base font-bold text-ink">Sua sacola está vazia</h3>
      <p class="mt-1.5 max-w-xs text-sm text-ink-muted">
        Explore os restaurantes parceiros e adicione marmitas fresquinhas para fazer seu pedido.
      </p>
      <AppButton
        variant="primary"
        size="sm"
        class="mt-5"
        @click="cart.closeDrawer(); router.push('/')"
      >
        Explorar restaurantes
      </AppButton>
    </div>

    <div v-else class="space-y-4">
      <div class="flex items-center justify-between rounded-xl border border-line bg-surface-subtle p-3">
        <button
          type="button"
          class="flex items-center gap-2.5 text-left hover:text-brand-600 transition"
          @click="goToRestaurant"
        >
          <span class="flex h-9 w-9 items-center justify-center rounded-xl bg-brand-50 text-brand-600 dark:bg-brand-950/60 dark:text-brand-400">
            <Store class="h-4 w-4" />
          </span>
          <div class="min-w-0">
            <p class="text-xs font-bold text-ink truncate">{{ cart.restaurantName }}</p>
            <p class="text-[11px] text-ink-muted">Ver cardápio completo</p>
          </div>
        </button>

        <button
          type="button"
          class="text-xs font-semibold text-red-600 hover:underline dark:text-red-400"
          @click="confirmClearOpen = true"
        >
          Limpar
        </button>
      </div>

      <div
        v-if="cart.isPlanRedemption"
        class="flex items-center gap-2 rounded-xl border border-fresh-200 bg-fresh-50 px-3.5 py-2.5 text-xs text-fresh-800 dark:border-fresh-800/60 dark:bg-fresh-950/40 dark:text-fresh-300"
      >
        <Sparkles class="h-4 w-4 shrink-0 text-fresh-600 dark:text-fresh-400" />
        <span>
          <strong>Modo Plano Mensal:</strong> marmitas base cobertas pela assinatura (R$ 0,00). Apenas extras e talheres pagos são cobrados.
        </span>
      </div>

      <ul class="divide-y divide-line">
        <li
          v-for="(item, idx) in cart.items"
          :key="`${item.productId}-${idx}`"
          class="flex gap-3 py-3.5"
        >
          <div class="h-14 w-14 shrink-0 overflow-hidden rounded-xl border border-line bg-surface-muted flex items-center justify-center">
            <img
              v-if="item.imageUrl"
              :src="item.imageUrl"
              :alt="item.name"
              class="h-full w-full object-cover"
            />
            <UtensilsCrossed v-else class="h-5 w-5 text-ink-muted" />
          </div>

          <div class="min-w-0 flex-1">
            <div class="flex items-start justify-between gap-2">
              <p class="text-sm font-bold text-ink leading-snug">{{ item.name }}</p>
              <button
                type="button"
                aria-label="Remover item"
                class="rounded-lg p-1 text-ink-muted hover:bg-red-50 hover:text-red-600 dark:hover:bg-red-950/40"
                @click="cart.removeItemByIndex(idx)"
              >
                <Trash2 class="h-3.5 w-3.5" />
              </button>
            </div>

            <div v-if="cart.isPlanRedemption" class="mt-1">
              <AppBadge variant="fresh" size="xs">Incluso no Plano</AppBadge>
            </div>

            <p
              v-if="(item.selectedAddons && item.selectedAddons.length > 0) || item.wantsCutlery"
              class="mt-1 text-[11px] text-ink-muted leading-snug"
            >
              <span v-if="item.selectedAddons && item.selectedAddons.length > 0">
                {{ item.selectedAddons.map((a) => `${a.quantity}x ${a.name}`).join(', ') }}
              </span>
              <span v-if="item.selectedAddons?.length && item.wantsCutlery"> • </span>
              <span v-if="item.wantsCutlery">Com talheres</span>
            </p>

            <div class="mt-2.5 flex items-center justify-between">
              <span class="font-display text-sm font-bold text-ink tabular-nums">
                {{ formatCurrency(cart.getItemEffectivePrice(item) * item.quantity) }}
              </span>

              <div class="inline-flex items-center gap-1 rounded-xl border border-line bg-surface-subtle p-0.5">
                <button
                  type="button"
                  aria-label="Diminuir quantidade"
                  class="flex h-7 w-7 items-center justify-center rounded-lg text-ink-secondary transition hover:bg-surface hover:text-ink"
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
                  class="flex h-7 w-7 items-center justify-center rounded-lg text-brand-600 transition hover:bg-surface"
                  @click="cart.updateItemQuantityByIndex(idx, item.quantity + 1)"
                >
                  <Plus class="h-3.5 w-3.5" />
                </button>
              </div>
            </div>
          </div>
        </li>
      </ul>
    </div>

    <template v-if="cart.items.length > 0" #footer>
      <div class="space-y-3">
        <div class="flex items-center justify-between text-sm">
          <span class="text-ink-secondary">
            Subtotal ({{ cart.totalItems }} {{ cart.totalItems === 1 ? 'item' : 'itens' }})
          </span>
          <span class="font-display text-lg font-extrabold text-ink tabular-nums">
            {{ formatCurrency(cart.totalAmount) }}
          </span>
        </div>

        <AppButton variant="primary" size="lg" block @click="goToCheckout">
          <span>{{ auth.isAuthenticated ? 'Finalizar pedido' : 'Entrar para finalizar' }}</span>
          <ArrowRight class="h-4 w-4" />
        </AppButton>
      </div>
    </template>
  </AppDrawer>

  <AppConfirmDialog
    :open="confirmClearOpen"
    title="Limpar sacola?"
    message="Todos os itens adicionados serão removidos da sua sacola atual."
    confirm-label="Sim, limpar sacola"
    variant="danger"
    @confirm="confirmClearCart"
    @cancel="confirmClearOpen = false"
  />
</template>
