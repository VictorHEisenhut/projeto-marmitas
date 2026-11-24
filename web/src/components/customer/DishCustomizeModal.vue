<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { Minus, Plus, Sparkles, UtensilsCrossed } from 'lucide-vue-next'
import AppModal from '@/components/ui/AppModal.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppToggle from '@/components/ui/AppToggle.vue'
import { formatCurrency } from '@/services/api'
import type { Product } from '@/types'
import type { SelectedAddon } from '@/stores/cart'

const props = defineProps<{
  open: boolean
  product: Product | null
  isPlanMode: boolean
  readOnly?: boolean
}>()

const emit = defineEmits<{
  (e: 'close'): void
  (
    e: 'confirm',
    payload: {
      product: Product
      quantity: number
      selectedAddons: SelectedAddon[]
      wantsCutlery: boolean
    },
  ): void
}>()

const quantity = ref(1)
const addonQty = ref<Record<string, number>>({})
const wantsCutlery = ref(false)

watch(
  () => [props.open, props.product?.id],
  () => {
    quantity.value = 1
    addonQty.value = {}
    wantsCutlery.value = false
  },
)

const effectiveBasePrice = computed(() => {
  if (!props.product) return 0
  if (props.isPlanMode) return 0
  if (
    props.product.promotionalPrice != null &&
    props.product.promotionalPrice > 0 &&
    props.product.promotionalPrice < props.product.price
  ) {
    return props.product.promotionalPrice
  }
  return props.product.price
})

const addonsTotal = computed(() => {
  if (!props.product?.addons) return 0
  return props.product.addons.reduce((sum, addon) => {
    const q = addonQty.value[addon.name] || 0
    return sum + addon.extraPrice * q
  }, 0)
})

const cutleryTotal = computed(() => {
  if (!props.product?.allowsCutlery || !wantsCutlery.value) return 0
  return props.product.cutleryHasExtraCost ? Number(props.product.cutleryPrice || 0) : 0
})

const unitTotal = computed(() => effectiveBasePrice.value + addonsTotal.value + cutleryTotal.value)
const grandTotal = computed(() => unitTotal.value * quantity.value)

function getAddonCount(name: string): number {
  return addonQty.value[name] || 0
}

function changeAddon(name: string, delta: number, maxQty: number) {
  const next = Math.max(0, Math.min(maxQty, getAddonCount(name) + delta))
  addonQty.value = { ...addonQty.value, [name]: next }
}

function handleConfirm() {
  if (!props.product || props.readOnly) return
  const selectedAddons: SelectedAddon[] = (props.product.addons || [])
    .map((a) => ({
      name: a.name,
      extraPrice: a.extraPrice,
      quantity: getAddonCount(a.name),
    }))
    .filter((a) => a.quantity > 0)

  emit('confirm', {
    product: props.product,
    quantity: quantity.value,
    selectedAddons,
    wantsCutlery: Boolean(props.product.allowsCutlery && wantsCutlery.value),
  })
}
</script>

<template>
  <AppModal
    :open="open && Boolean(product)"
    :title="product?.name"
    :subtitle="product?.categoryName || 'Marmita do dia'"
    max-width="lg"
    sheet-on-mobile
    @close="emit('close')"
  >
    <div v-if="product" class="space-y-5">
      <!-- Dish Hero Photo -->
      <div class="relative aspect-[16/10] w-full overflow-hidden rounded-2xl bg-gradient-to-br from-brand-50 via-amber-50 to-surface-muted dark:from-brand-950/50 dark:via-surface-muted dark:to-surface-subtle">
        <img
          v-if="product.imageUrl"
          :src="product.imageUrl"
          :alt="product.name"
          class="h-full w-full object-cover"
        />
        <div v-else class="flex h-full w-full items-center justify-center">
          <UtensilsCrossed class="h-12 w-12 text-brand-500/50" />
        </div>

        <div class="absolute left-3 top-3 flex flex-wrap gap-1.5">
          <AppBadge v-if="isPlanMode" variant="fresh" size="sm">
            <Sparkles class="h-3 w-3" />
            Incluso no Plano — Base R$ 0,00
          </AppBadge>
          <AppBadge
            v-else-if="product.availableInPlans !== false"
            variant="fresh"
            size="xs"
          >
            Elegível em Planos
          </AppBadge>
          <AppBadge
            v-if="product.dailyStock != null && product.dailyStock <= 10"
            variant="warning"
            size="xs"
          >
            Restam {{ product.dailyStock }} hoje
          </AppBadge>
        </div>
      </div>

      <!-- Description & Base Price -->
      <div class="space-y-2">
        <p class="text-sm leading-relaxed text-ink-secondary">
          {{ product.description || 'Marmita fresca preparada no dia com ingredientes selecionados.' }}
        </p>

        <div class="flex items-baseline gap-2.5 pt-1">
          <template v-if="isPlanMode">
            <span class="font-display text-xl font-extrabold text-fresh-700 dark:text-fresh-400 tabular-nums">
              R$ 0,00
            </span>
            <span class="text-xs font-medium text-ink-muted">
              Valor base coberto pela assinatura · extras cobrados à parte
            </span>
          </template>
          <template v-else-if="product.promotionalPrice && product.promotionalPrice < product.price">
            <span class="font-display text-xl font-extrabold text-brand-600 tabular-nums">
              {{ formatCurrency(product.promotionalPrice) }}
            </span>
            <span class="text-sm text-ink-muted line-through tabular-nums">
              {{ formatCurrency(product.price) }}
            </span>
          </template>
          <template v-else>
            <span class="font-display text-xl font-extrabold text-ink tabular-nums">
              {{ formatCurrency(product.price) }}
            </span>
          </template>
        </div>
      </div>

      <!-- Addons Section -->
      <div
        v-if="!readOnly && product.addons && product.addons.length > 0"
        class="space-y-3 rounded-2xl border border-line bg-surface-subtle p-4"
      >
        <div class="flex items-center justify-between">
          <h4 class="font-display text-sm font-bold text-ink">Turbine sua marmita</h4>
          <AppBadge variant="neutral" size="xs">Opcional</AppBadge>
        </div>

        <div class="divide-y divide-line">
          <div
            v-for="addon in product.addons"
            :key="addon.name"
            class="flex items-center justify-between gap-3 py-2.5 first:pt-0 last:pb-0"
          >
            <div class="min-w-0">
              <p class="text-sm font-semibold text-ink">{{ addon.name }}</p>
              <p class="text-xs text-ink-muted">
                <span class="font-semibold text-brand-600 dark:text-brand-400 tabular-nums">
                  {{ addon.extraPrice > 0 ? `+ ${formatCurrency(addon.extraPrice)}` : 'Grátis' }}
                </span>
                <span> · máx. {{ addon.maxQuantity }}</span>
              </p>
            </div>

            <div class="inline-flex items-center gap-1.5 rounded-xl border border-line bg-surface p-0.5">
              <button
                type="button"
                aria-label="Remover adicional"
                :disabled="getAddonCount(addon.name) === 0"
                class="flex h-8 w-8 items-center justify-center rounded-lg text-ink-secondary transition hover:bg-surface-muted disabled:opacity-35"
                @click="changeAddon(addon.name, -1, addon.maxQuantity)"
              >
                <Minus class="h-3.5 w-3.5" />
              </button>
              <span class="min-w-[1.5rem] text-center text-xs font-bold text-ink tabular-nums">
                {{ getAddonCount(addon.name) }}
              </span>
              <button
                type="button"
                aria-label="Adicionar extra"
                :disabled="getAddonCount(addon.name) >= addon.maxQuantity"
                class="flex h-8 w-8 items-center justify-center rounded-lg text-brand-600 transition hover:bg-brand-50 dark:hover:bg-brand-950/40 disabled:opacity-35"
                @click="changeAddon(addon.name, 1, addon.maxQuantity)"
              >
                <Plus class="h-3.5 w-3.5" />
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Cutlery Option -->
      <div
        v-if="!readOnly && product.allowsCutlery"
        class="rounded-2xl border border-line bg-surface-subtle p-4"
      >
        <AppToggle
          v-model="wantsCutlery"
          label="Enviar talheres descartáveis?"
          :description="
            product.cutleryHasExtraCost && product.cutleryPrice
              ? `Acréscimo de ${formatCurrency(product.cutleryPrice)} por marmita`
              : 'Gratuito — marque apenas se precisar'
          "
        />
      </div>
    </div>

    <template #footer>
      <div v-if="readOnly" class="flex justify-end">
        <AppButton variant="outline" size="md" @click="emit('close')">
          Fechar visualização
        </AppButton>
      </div>

      <div v-else class="flex items-center gap-3">
        <!-- Quantity Stepper -->
        <div class="inline-flex h-12 items-center gap-1 rounded-xl border border-line-strong bg-surface px-1.5">
          <button
            type="button"
            aria-label="Diminuir quantidade"
            :disabled="quantity <= 1"
            class="flex h-9 w-9 items-center justify-center rounded-lg text-ink-secondary transition hover:bg-surface-muted disabled:opacity-35"
            @click="quantity = Math.max(1, quantity - 1)"
          >
            <Minus class="h-4 w-4" />
          </button>
          <span class="min-w-[1.75rem] text-center font-display text-sm font-extrabold text-ink tabular-nums">
            {{ quantity }}
          </span>
          <button
            type="button"
            aria-label="Aumentar quantidade"
            class="flex h-9 w-9 items-center justify-center rounded-lg text-brand-600 transition hover:bg-brand-50 dark:hover:bg-brand-950/40"
            @click="quantity += 1"
          >
            <Plus class="h-4 w-4" />
          </button>
        </div>

        <AppButton
          :variant="isPlanMode ? 'fresh' : 'primary'"
          size="lg"
          block
          class="flex-1 justify-between"
          @click="handleConfirm"
        >
          <span>Adicionar</span>
          <span class="font-display font-extrabold tabular-nums">
            {{ formatCurrency(grandTotal) }}
          </span>
        </AppButton>
      </div>
    </template>
  </AppModal>
</template>
