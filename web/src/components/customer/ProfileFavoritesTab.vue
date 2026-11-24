<script setup lang="ts">
import { computed } from 'vue'
import { ArrowRight, Heart, MapPin, Store, Trash2 } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppEmptyState from '@/components/ui/AppEmptyState.vue'
import { api, getErrorMessage } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import type { Favorite, Restaurant } from '@/types'

const props = defineProps<{
  favorites: Favorite[]
  restaurants: Restaurant[]
}>()

const emit = defineEmits<{
  (e: 'removed', id: string): void
}>()

const toast = useToastStore()

const restaurantMap = computed(() => {
  const map: Record<string, Restaurant> = {}
  for (const r of props.restaurants) map[r.id] = r
  return map
})

async function handleRemove(id: string) {
  try {
    await api.delete(`/usuarios/me/favoritos/${id}`)
    toast.info('Removido dos favoritos.')
    emit('removed', id)
  } catch (err) {
    toast.error(getErrorMessage(err, 'Erro ao remover favorito.'))
  }
}
</script>

<template>
  <div class="space-y-5">
    <div>
      <h2 class="font-display text-lg font-extrabold text-ink">Restaurantes favoritos</h2>
      <p class="text-xs text-ink-muted">Acesse rapidamente suas cozinhas preferidas.</p>
    </div>

    <div v-if="favorites.length > 0" class="grid gap-4 sm:grid-cols-2">
      <AppCard
        v-for="fav in favorites"
        :key="fav.id"
        padding="none"
        class="overflow-hidden flex flex-col justify-between"
      >
        <div class="flex items-center gap-4 p-4">
          <div class="h-16 w-16 shrink-0 overflow-hidden rounded-2xl border border-line bg-brand-50 dark:bg-brand-950/50 flex items-center justify-center">
            <img
              v-if="fav.restaurantId && restaurantMap[fav.restaurantId]?.imageUrl"
              :src="restaurantMap[fav.restaurantId]?.imageUrl || ''"
              :alt="restaurantMap[fav.restaurantId]?.name"
              class="h-full w-full object-cover"
            />
            <Store v-else class="h-7 w-7 text-brand-600" />
          </div>

          <div class="min-w-0 flex-1">
            <p class="truncate font-display text-sm font-extrabold text-ink">
              {{ (fav.restaurantId && restaurantMap[fav.restaurantId]?.name) || 'Restaurante Parceiro' }}
            </p>
            <p
              v-if="fav.restaurantId && restaurantMap[fav.restaurantId]?.addressNeighborhood"
              class="mt-0.5 inline-flex items-center gap-1 text-xs text-ink-muted"
            >
              <MapPin class="h-3 w-3 text-brand-500" />
              {{ restaurantMap[fav.restaurantId]?.addressNeighborhood }}
            </p>
          </div>

          <button
            type="button"
            aria-label="Remover dos favoritos"
            class="rounded-xl p-2 text-ink-muted transition hover:bg-red-50 hover:text-red-600 dark:hover:bg-red-950/40"
            @click="handleRemove(fav.id)"
          >
            <Trash2 class="h-4 w-4" />
          </button>
        </div>

        <div v-if="fav.restaurantId" class="border-t border-line bg-surface-subtle px-4 py-2.5">
          <RouterLink
            :to="`/restaurantes/${fav.restaurantId}`"
            class="inline-flex items-center gap-1.5 text-xs font-bold text-brand-600 hover:underline dark:text-brand-400"
          >
            <span>Abrir cardápio</span>
            <ArrowRight class="h-3.5 w-3.5" />
          </RouterLink>
        </div>
      </AppCard>
    </div>

    <AppEmptyState
      v-else
      :icon="Heart"
      title="Nenhum restaurante favoritado"
      description="Clique no coração em qualquer restaurante da vitrine para salvá-lo aqui."
    >
      <template #action>
        <RouterLink to="/">
          <AppButton variant="primary" size="sm">Explorar restaurantes</AppButton>
        </RouterLink>
      </template>
    </AppEmptyState>
  </div>
</template>
