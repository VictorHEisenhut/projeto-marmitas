<script setup lang="ts">
import { AlertCircle, AlertTriangle, CheckCircle2, Info, X } from 'lucide-vue-next'
import { useToastStore } from '@/stores/toast'

const toast = useToastStore()
</script>

<template>
  <Teleport to="body">
    <div
      aria-live="polite"
      class="pointer-events-none fixed right-4 top-4 z-[70] flex w-[calc(100%-2rem)] max-w-sm flex-col gap-2.5 sm:top-5 sm:right-5"
    >
      <div
        v-for="item in toast.toasts"
        :key="item.id"
        role="status"
        class="pointer-events-auto flex items-start gap-3 rounded-2xl border bg-surface p-3.5 shadow-elevated animate-slide-up"
        :class="{
          'border-fresh-200 dark:border-fresh-800/70': item.variant === 'success',
          'border-red-200 dark:border-red-800/70': item.variant === 'error',
          'border-amber-200 dark:border-amber-800/70': item.variant === 'warning',
          'border-sky-200 dark:border-sky-800/70': item.variant === 'info',
        }"
      >
        <div
          class="mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-xl"
          :class="{
            'bg-fresh-50 text-fresh-600 dark:bg-fresh-950/70 dark:text-fresh-400': item.variant === 'success',
            'bg-red-50 text-red-600 dark:bg-red-950/70 dark:text-red-400': item.variant === 'error',
            'bg-amber-50 text-amber-600 dark:bg-amber-950/70 dark:text-amber-400': item.variant === 'warning',
            'bg-sky-50 text-sky-600 dark:bg-sky-950/70 dark:text-sky-400': item.variant === 'info',
          }"
        >
          <CheckCircle2 v-if="item.variant === 'success'" class="h-4 w-4" />
          <AlertCircle v-else-if="item.variant === 'error'" class="h-4 w-4" />
          <AlertTriangle v-else-if="item.variant === 'warning'" class="h-4 w-4" />
          <Info v-else class="h-4 w-4" />
        </div>

        <div class="min-w-0 flex-1">
          <p v-if="item.title" class="text-xs font-bold text-ink">{{ item.title }}</p>
          <p class="text-xs leading-relaxed text-ink-secondary">{{ item.message }}</p>
        </div>

        <button
          type="button"
          aria-label="Fechar notificação"
          class="rounded-lg p-1 text-ink-muted transition hover:bg-surface-muted hover:text-ink"
          @click="toast.dismiss(item.id)"
        >
          <X class="h-3.5 w-3.5" />
        </button>
      </div>
    </div>
  </Teleport>
</template>
