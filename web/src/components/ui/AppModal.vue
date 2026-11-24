<script setup lang="ts">
import { onMounted, onUnmounted, watch } from 'vue'
import { X } from 'lucide-vue-next'

const props = withDefaults(
  defineProps<{
    open: boolean
    title?: string
    subtitle?: string
    maxWidth?: 'sm' | 'md' | 'lg' | 'xl' | '2xl'
    sheetOnMobile?: boolean
    hideClose?: boolean
  }>(),
  {
    maxWidth: 'lg',
    sheetOnMobile: true,
    hideClose: false,
  },
)

const emit = defineEmits<{
  (e: 'close'): void
}>()

const maxWidthClass: Record<string, string> = {
  sm: 'sm:max-w-sm',
  md: 'sm:max-w-md',
  lg: 'sm:max-w-lg',
  xl: 'sm:max-w-xl',
  '2xl': 'sm:max-w-2xl',
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape' && props.open) {
    emit('close')
  }
}

watch(
  () => props.open,
  (isOpen) => {
    if (typeof document === 'undefined') return
    document.body.style.overflow = isOpen ? 'hidden' : ''
  },
)

onMounted(() => {
  window.addEventListener('keydown', handleKeydown)
})

onUnmounted(() => {
  window.removeEventListener('keydown', handleKeydown)
  if (typeof document !== 'undefined') {
    document.body.style.overflow = ''
  }
})
</script>

<template>
  <Teleport to="body">
    <div
      v-if="open"
      role="dialog"
      aria-modal="true"
      class="fixed inset-0 z-50 flex justify-center bg-black/55 backdrop-blur-xs animate-fade-in"
      :class="
        sheetOnMobile
          ? 'items-end sm:items-center sm:p-4'
          : 'items-center p-4'
      "
      @click.self="emit('close')"
    >
      <div
        class="relative flex max-h-[92vh] w-full flex-col overflow-hidden border border-line bg-surface text-ink shadow-elevated animate-slide-up"
        :class="[
          maxWidthClass[maxWidth],
          sheetOnMobile ? 'rounded-t-3xl sm:rounded-2xl' : 'rounded-2xl',
        ]"
      >
        <div
          v-if="sheetOnMobile"
          class="mx-auto mt-2.5 h-1 w-10 rounded-full bg-line-strong sm:hidden"
        />

        <div
          v-if="title || $slots.header"
          class="flex items-start justify-between gap-4 border-b border-line px-5 py-4 sm:px-6"
        >
          <slot name="header">
            <div class="min-w-0">
              <h3 class="font-display text-lg font-bold text-ink leading-tight">
                {{ title }}
              </h3>
              <p v-if="subtitle" class="mt-0.5 text-xs text-ink-muted">
                {{ subtitle }}
              </p>
            </div>
          </slot>

          <button
            v-if="!hideClose"
            type="button"
            aria-label="Fechar janela"
            class="inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-ink-muted transition hover:bg-surface-muted hover:text-ink"
            @click="emit('close')"
          >
            <X class="h-4 w-4" />
          </button>
        </div>

        <div class="flex-1 overflow-y-auto px-5 py-4 sm:px-6 sm:py-5">
          <slot />
        </div>

        <div
          v-if="$slots.footer"
          class="border-t border-line bg-surface-subtle px-5 py-3.5 sm:px-6 safe-bottom"
        >
          <slot name="footer" />
        </div>
      </div>
    </div>
  </Teleport>
</template>
