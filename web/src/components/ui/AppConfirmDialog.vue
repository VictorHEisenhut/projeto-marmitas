<script setup lang="ts">
import { AlertTriangle } from 'lucide-vue-next'
import AppModal from './AppModal.vue'
import AppButton from './AppButton.vue'

withDefaults(
  defineProps<{
    open: boolean
    title: string
    message: string
    confirmLabel?: string
    cancelLabel?: string
    variant?: 'danger' | 'primary'
    loading?: boolean
  }>(),
  {
    confirmLabel: 'Confirmar',
    cancelLabel: 'Cancelar',
    variant: 'primary',
    loading: false,
  },
)

const emit = defineEmits<{
  (e: 'confirm'): void
  (e: 'cancel'): void
}>()
</script>

<template>
  <AppModal :open="open" max-width="sm" :sheet-on-mobile="false" @close="emit('cancel')">
    <div class="flex flex-col items-center text-center sm:items-start sm:text-left">
      <div
        class="mb-4 flex h-11 w-11 items-center justify-center rounded-2xl"
        :class="
          variant === 'danger'
            ? 'bg-red-50 text-red-600 dark:bg-red-950/60 dark:text-red-400'
            : 'bg-brand-50 text-brand-600 dark:bg-brand-950/60 dark:text-brand-400'
        "
      >
        <AlertTriangle class="h-5 w-5" />
      </div>
      <h3 class="font-display text-lg font-bold text-ink">{{ title }}</h3>
      <p class="mt-2 text-sm leading-relaxed text-ink-secondary">{{ message }}</p>
    </div>

    <template #footer>
      <div class="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <AppButton variant="outline" size="sm" :disabled="loading" @click="emit('cancel')">
          {{ cancelLabel }}
        </AppButton>
        <AppButton
          :variant="variant === 'danger' ? 'danger' : 'primary'"
          size="sm"
          :loading="loading"
          @click="emit('confirm')"
        >
          {{ confirmLabel }}
        </AppButton>
      </div>
    </template>
  </AppModal>
</template>
