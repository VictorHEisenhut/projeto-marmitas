<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { Bike, CheckCircle2, DollarSign, Lock, Send, Star, Store, UtensilsCrossed } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppTabs from '@/components/ui/AppTabs.vue'
import AppSkeleton from '@/components/ui/AppSkeleton.vue'
import AppEmptyState from '@/components/ui/AppEmptyState.vue'
import OrderStatusBadge from '@/components/ui/OrderStatusBadge.vue'
import StarRating from '@/components/ui/StarRating.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'
import type { CourierEarnings, Delivery, Product, Restaurant, ReviewItem } from '@/types'

const props = defineProps<{
  earnings: CourierEarnings | null
  deliveries: Delivery[]
  restaurants: Restaurant[]
  courierReviews: ReviewItem[]
  customerReviews: ReviewItem[]
}>()

const emit = defineEmits<{
  (e: 'courier-review-added', review: ReviewItem): void
}>()

const auth = useAuthStore()
const toast = useToastStore()

const activeTab = ref<'dashboard' | 'menus' | 'courier-reviews' | 'customer-reviews'>('dashboard')

const selectedMenuRestaurantId = ref(props.restaurants[0]?.id || '')
const selectedProducts = ref<Product[]>([])
const loadingMenu = ref(false)

const reviewRestaurantId = ref(props.restaurants[0]?.id || '')
const reviewRating = ref(5)
const reviewComment = ref('')
const savingReview = ref(false)

const completedDeliveries = computed(() => props.deliveries.filter((d) => d.status === 'Delivered'))
const activeDeliveries = computed(() =>
  props.deliveries.filter(
    (d) =>
      (d.courierId && d.courierId === auth.user?.id && d.status !== 'Delivered') ||
      d.status === 'DriverAssigned' ||
      d.status === 'PickedUp' ||
      d.status === 'InTransit',
  ),
)

const tabs = [
  { id: 'dashboard', label: 'Ganhos & Entregas', icon: Bike },
  { id: 'menus', label: 'Cardápios (Consulta)', icon: UtensilsCrossed },
  { id: 'courier-reviews', label: 'Mural Entregadores', icon: Lock },
  { id: 'customer-reviews', label: 'Mural Clientes', icon: Star },
]

async function loadMenuPreview(restaurantId: string) {
  if (!restaurantId) return
  selectedMenuRestaurantId.value = restaurantId
  loadingMenu.value = true
  try {
    const { data } = await api.get<Product[]>(`/restaurantes/${restaurantId}/cardapio`)
    selectedProducts.value = data
  } catch {
    selectedProducts.value = []
  } finally {
    loadingMenu.value = false
  }
}

watch(
  () => props.restaurants,
  (list) => {
    const first = list[0]
    if (first) {
      if (!reviewRestaurantId.value) reviewRestaurantId.value = first.id
      if (!selectedMenuRestaurantId.value) {
        void loadMenuPreview(first.id)
      }
    }
  },
  { immediate: true },
)

async function handleSubmitCourierReview() {
  if (!reviewComment.value.trim()) {
    toast.warning('Escreva seu relato sobre o tempo de coleta ou embalagem.')
    return
  }

  savingReview.value = true
  try {
    const { data } = await api.post<ReviewItem>('/avaliacoes/entregadores', {
      restaurantId: reviewRestaurantId.value || null,
      rating: reviewRating.value,
      comment: reviewComment.value.trim(),
    })
    reviewComment.value = ''
    reviewRating.value = 5
    toast.success('Avaliação publicada no mural exclusivo de entregadores!')
    emit('courier-review-added', data)
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível publicar a avaliação.'))
  } finally {
    savingReview.value = false
  }
}
</script>

<template>
  <div class="space-y-6">
    <AppTabs v-model="activeTab" :tabs="tabs" variant="pills" />

    <!-- Tab 1: Earnings & Deliveries Dashboard -->
    <section v-if="activeTab === 'dashboard'" class="space-y-6">
      <div class="grid gap-4 sm:grid-cols-3">
        <AppCard padding="md">
          <div class="flex items-center gap-2 text-ink-muted">
            <CheckCircle2 class="h-5 w-5 text-fresh-600" />
            <span class="text-xs font-bold uppercase tracking-wider">Entregas Concluídas</span>
          </div>
          <p class="mt-2 font-display text-3xl font-extrabold text-ink tabular-nums">
            {{ earnings?.completedDeliveries ?? completedDeliveries.length }}
          </p>
        </AppCard>

        <AppCard padding="md">
          <div class="flex items-center gap-2 text-ink-muted">
            <Bike class="h-5 w-5 text-brand-600" />
            <span class="text-xs font-bold uppercase tracking-wider">Em Rota / Aceitas</span>
          </div>
          <p class="mt-2 font-display text-3xl font-extrabold text-ink tabular-nums">
            {{ activeDeliveries.length }}
          </p>
        </AppCard>

        <AppCard padding="md">
          <div class="flex items-center gap-2 text-ink-muted">
            <DollarSign class="h-5 w-5 text-fresh-600" />
            <span class="text-xs font-bold uppercase tracking-wider">Ganhos Totais</span>
          </div>
          <p class="mt-2 font-display text-3xl font-extrabold text-fresh-700 dark:text-fresh-400 tabular-nums">
            {{ formatCurrency(earnings?.totalEarnings ?? completedDeliveries.length * 8.5) }}
          </p>
        </AppCard>
      </div>

      <AppCard padding="lg" class="space-y-4">
        <div class="flex flex-col justify-between gap-3 sm:flex-row sm:items-center">
          <div>
            <h3 class="font-display text-base font-extrabold text-ink">Histórico de Entregas</h3>
            <p class="text-xs text-ink-muted">Corridas concluídas e em andamento com origem e destino.</p>
          </div>
          <RouterLink to="/entregador">
            <AppButton variant="primary" size="sm">Ir para Fila de Coletas</AppButton>
          </RouterLink>
        </div>

        <div v-if="deliveries.length > 0" class="space-y-3">
          <div
            v-for="deliv in deliveries"
            :key="deliv.id"
            class="flex flex-col justify-between gap-3 rounded-2xl border border-line bg-surface-subtle p-4 sm:flex-row sm:items-center"
          >
            <div class="space-y-1 text-xs">
              <div class="flex flex-wrap items-center gap-2">
                <span class="font-display text-sm font-extrabold text-ink">
                  Pedido #{{ deliv.orderId.slice(0, 8) }}
                </span>
                <OrderStatusBadge :status="deliv.status" />
              </div>
              <p class="text-ink-secondary">
                <strong class="text-ink">Coleta:</strong> {{ deliv.restaurantName || 'Restaurante Parceiro' }}
                <span v-if="deliv.restaurantAddress" class="text-ink-muted"> ({{ deliv.restaurantAddress }})</span>
              </p>
              <p class="text-ink-secondary">
                <strong class="text-ink">Destino:</strong> Bairro {{ deliv.deliveryNeighborhood || 'Centro' }}
                <span v-if="deliv.deliveryAddress" class="text-ink-muted"> — {{ deliv.deliveryAddress }}</span>
              </p>
            </div>

            <div class="text-left sm:text-right shrink-0">
              <span class="text-[11px] text-ink-muted">Repasse</span>
              <p class="font-display text-sm font-extrabold text-fresh-700 dark:text-fresh-400 tabular-nums">
                {{ formatCurrency(deliv.deliveryFee ?? 8.5) }}
                ({{ deliv.status === 'Delivered' ? 'Pago' : 'Previsto' }})
              </p>
            </div>
          </div>
        </div>

        <AppEmptyState
          v-else
          :icon="Bike"
          title="Nenhuma entrega registrada ainda"
          description="Complete sua homologação no Painel do Entregador para visualizar e aceitar corridas."
        />
      </AppCard>
    </section>

    <!-- Tab 2: Restaurant Menus (Read-only consultation) -->
    <section v-else-if="activeTab === 'menus'" class="space-y-4">
      <AppCard padding="lg" class="space-y-4">
        <div class="flex flex-wrap items-center justify-between gap-2">
          <div>
            <h3 class="font-display text-base font-extrabold text-ink">
              Cardápios dos Restaurantes Parceiros
            </h3>
            <p class="text-xs text-ink-muted">Modo consulta para conhecer os pratos e embalagens de cada loja.</p>
          </div>
          <AppBadge variant="neutral" size="xs">Somente Consulta</AppBadge>
        </div>

        <div class="flex flex-wrap gap-2">
          <button
            v-for="r in restaurants"
            :key="r.id"
            type="button"
            class="inline-flex items-center gap-1.5 rounded-xl border px-3.5 py-2 text-xs font-bold transition"
            :class="
              selectedMenuRestaurantId === r.id
                ? 'border-brand-600 bg-brand-600 text-white'
                : 'border-line bg-surface-subtle text-ink-secondary hover:bg-surface-muted'
            "
            @click="loadMenuPreview(r.id)"
          >
            <Store class="h-3.5 w-3.5" />
            {{ r.name }}
          </button>
        </div>

        <div v-if="loadingMenu" class="grid gap-3 sm:grid-cols-2">
          <AppSkeleton v-for="n in 4" :key="n" variant="card" height="90px" />
        </div>

        <div v-else-if="selectedProducts.length > 0" class="grid gap-3 sm:grid-cols-2">
          <div
            v-for="prod in selectedProducts"
            :key="prod.id"
            class="rounded-2xl border border-line bg-surface-subtle p-4 space-y-1"
          >
            <div class="flex items-center justify-between gap-2">
              <AppBadge variant="neutral" size="xs">{{ prod.categoryName || 'Marmita' }}</AppBadge>
              <span class="font-display text-xs font-extrabold text-ink tabular-nums">
                {{ formatCurrency(prod.price) }}
              </span>
            </div>
            <h4 class="pt-1 font-display text-sm font-bold text-ink">{{ prod.name }}</h4>
            <p class="text-xs text-ink-secondary">{{ prod.description }}</p>
          </div>
        </div>

        <AppEmptyState v-else :icon="UtensilsCrossed" title="Nenhum prato neste cardápio" />
      </AppCard>
    </section>

    <!-- Tab 3: Exclusive Courier Reviews -->
    <section v-else-if="activeTab === 'courier-reviews'" class="space-y-5">
      <AppCard padding="lg" class="space-y-4">
        <div class="flex flex-wrap items-center justify-between gap-2">
          <div class="flex items-center gap-2">
            <Lock class="h-5 w-5 text-brand-600" />
            <div>
              <h3 class="font-display text-base font-extrabold text-ink">
                Mural de Avaliações de Entregadores
              </h3>
              <p class="text-xs text-ink-muted">
                Espaço exclusivo para avaliar agilidade de coleta, embalagem e ponto de apoio.
              </p>
            </div>
          </div>
          <AppBadge variant="warning" size="sm">Restrito a Entregadores</AppBadge>
        </div>

        <form class="space-y-4 rounded-2xl border border-line bg-surface-subtle p-4" @submit.prevent="handleSubmitCourierReview">
          <div class="grid gap-4 sm:grid-cols-2">
            <div class="space-y-1.5">
              <label class="block text-xs font-semibold text-ink-secondary">Restaurante avaliado</label>
              <select
                v-model="reviewRestaurantId"
                class="w-full rounded-xl border border-line-strong bg-surface px-3 py-2 text-sm text-ink"
              >
                <option v-for="r in restaurants" :key="r.id" :value="r.id">{{ r.name }}</option>
              </select>
            </div>
            <div class="space-y-1.5">
              <label class="block text-xs font-semibold text-ink-secondary">Nota da Coleta</label>
              <div class="flex h-10 items-center rounded-xl border border-line bg-surface px-3">
                <StarRating v-model="reviewRating" size="md" show-value />
              </div>
            </div>
          </div>

          <textarea
            v-model="reviewComment"
            rows="2"
            required
            placeholder="Conte como é a retirada, lacre das marmitas e estacionamento..."
            class="w-full rounded-xl border border-line-strong bg-surface px-3.5 py-2.5 text-sm text-ink"
          />

          <AppButton type="submit" variant="primary" size="sm" :loading="savingReview">
            <Send class="h-3.5 w-3.5" />
            Publicar avaliação exclusiva
          </AppButton>
        </form>

        <div class="space-y-3">
          <div
            v-for="rev in courierReviews"
            :key="rev.id"
            class="rounded-2xl border border-line bg-surface-subtle p-4 space-y-1.5"
          >
            <div class="flex flex-wrap items-center justify-between gap-2">
              <div class="flex items-center gap-2">
                <span class="font-display text-sm font-bold text-ink">
                  {{ rev.authorName || 'Entregador Parceiro' }}
                </span>
                <AppBadge variant="neutral" size="xs">{{ rev.restaurantName || 'Coleta' }}</AppBadge>
              </div>
              <StarRating :model-value="rev.rating" readonly size="sm" />
            </div>
            <p class="text-sm text-ink-secondary">{{ rev.comment }}</p>
          </div>
        </div>
      </AppCard>
    </section>

    <!-- Tab 4: Public Customer Reviews -->
    <section v-else class="space-y-4">
      <AppCard padding="lg" class="space-y-4">
        <div class="flex flex-wrap items-center justify-between gap-2">
          <div>
            <h3 class="font-display text-base font-extrabold text-ink">Avaliações de Clientes</h3>
            <p class="text-xs text-ink-muted">Opiniões públicas registradas pelos clientes.</p>
          </div>
          <AppBadge variant="fresh" size="sm">Público</AppBadge>
        </div>

        <div class="grid gap-3 sm:grid-cols-2">
          <div
            v-for="rev in customerReviews"
            :key="rev.id"
            class="rounded-2xl border border-line bg-surface-subtle p-4 space-y-1.5"
          >
            <div class="flex items-center justify-between gap-2">
              <span class="font-display text-sm font-bold text-ink">
                {{ rev.authorName || 'Cliente Verificado' }}
              </span>
              <StarRating :model-value="rev.rating" readonly size="sm" />
            </div>
            <p class="text-xs font-semibold text-brand-600 dark:text-brand-400">
              {{ rev.restaurantName || 'Restaurante Parceiro' }}
            </p>
            <p class="text-sm text-ink-secondary">{{ rev.comment }}</p>
          </div>
        </div>
      </AppCard>
    </section>
  </div>
</template>
