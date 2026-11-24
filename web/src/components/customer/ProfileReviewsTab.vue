<script setup lang="ts">
import { ref, watch } from 'vue'
import { MessageSquare, Send } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import StarRating from '@/components/ui/StarRating.vue'
import { api, getErrorMessage } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import type { Restaurant, ReviewItem } from '@/types'

const props = defineProps<{
  restaurants: Restaurant[]
  customerReviews: ReviewItem[]
}>()

const emit = defineEmits<{
  (e: 'published', review: ReviewItem): void
}>()

const toast = useToastStore()

const restaurantId = ref(props.restaurants[0]?.id || '')
const rating = ref(5)
const comment = ref('')
const saving = ref(false)

watch(
  () => props.restaurants,
  (list) => {
    if (!restaurantId.value && list[0]) {
      restaurantId.value = list[0].id
    }
  },
)

async function handlePublish() {
  if (!comment.value.trim()) {
    toast.warning('Escreva seu comentário sobre a experiência.')
    return
  }

  saving.value = true
  try {
    const { data } = await api.post<ReviewItem>('/avaliacoes/clientes', {
      restaurantId: restaurantId.value || null,
      rating: rating.value,
      comment: comment.value.trim(),
    })
    comment.value = ''
    rating.value = 5
    toast.success('Avaliação publicada no mural público!')
    emit('published', data)
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível enviar a avaliação.'))
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="space-y-5">
    <AppCard padding="lg" class="space-y-4">
      <div class="flex flex-wrap items-center justify-between gap-2">
        <div class="flex items-center gap-2">
          <MessageSquare class="h-5 w-5 text-brand-600" />
          <div>
            <h2 class="font-display text-base font-extrabold text-ink">
              Publicar avaliação no Mural Público
            </h2>
            <p class="text-xs text-ink-muted">Visível para toda a comunidade na vitrine.</p>
          </div>
        </div>
        <AppBadge variant="fresh" size="xs">Mural Público</AppBadge>
      </div>

      <form class="space-y-4" @submit.prevent="handlePublish">
        <div class="grid gap-4 sm:grid-cols-2">
          <div class="space-y-1.5">
            <label class="block text-xs font-semibold text-ink-secondary">Restaurante</label>
            <select
              v-model="restaurantId"
              class="w-full rounded-xl border border-line-strong bg-surface px-3.5 py-2.5 text-sm text-ink focus:border-brand-600 focus:outline-none"
            >
              <option v-for="r in restaurants" :key="r.id" :value="r.id">{{ r.name }}</option>
            </select>
          </div>

          <div class="space-y-1.5">
            <label class="block text-xs font-semibold text-ink-secondary">Sua nota</label>
            <div class="flex h-10 items-center rounded-xl border border-line bg-surface-subtle px-3.5">
              <StarRating v-model="rating" size="md" show-value />
            </div>
          </div>
        </div>

        <textarea
          v-model="comment"
          rows="2"
          required
          placeholder="Compartilhe sua experiência com a marmita, sabor, temperatura e entrega..."
          class="w-full rounded-xl border border-line-strong bg-surface px-3.5 py-2.5 text-sm text-ink placeholder:text-ink-muted focus:border-brand-600 focus:outline-none"
        />

        <AppButton type="submit" variant="primary" size="sm" :loading="saving">
          <Send class="h-3.5 w-3.5" />
          Publicar avaliação
        </AppButton>
      </form>
    </AppCard>

    <div class="grid gap-4 sm:grid-cols-2">
      <AppCard v-for="rev in customerReviews" :key="rev.id" padding="md" class="space-y-2">
        <div class="flex items-center justify-between gap-2">
          <span class="font-display text-sm font-bold text-ink">
            {{ rev.authorName || 'Cliente Verificado' }}
          </span>
          <StarRating :model-value="rev.rating" readonly size="sm" />
        </div>
        <p class="text-xs font-semibold text-brand-600 dark:text-brand-400">
          {{ rev.restaurantName || 'Restaurante Parceiro' }}
        </p>
        <p class="text-sm leading-relaxed text-ink-secondary">{{ rev.comment }}</p>
      </AppCard>
    </div>
  </div>
</template>
