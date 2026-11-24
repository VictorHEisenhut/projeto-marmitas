<script setup lang="ts">
import { onMounted, onUnmounted, watch } from 'vue'
import { X } from 'lucide-vue-next'

const props = withDefaults(
  defineProps<{
    open: boolean
    title?: string
    subtitle?: string
    side?: 'right' | 'left'
    widthClass?: string
  }>(),
  {
    side: 'right',
    widthClass: 'max-w-md',
  },
)

const emit = defineEmits<{
  (e: 'close'): void
}>()

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
      class="fixed inset-0 z-50 flex bg-black/55 backdrop-blur-xs animate-fade-in"
      :class="side === 'right' ? 'justify-end' : 'justify-start'"
      @click.self="emit('close')"
    >
      <div
        class="relative flex h-full w-full flex-col border-line bg-surface text-ink shadow-drawer"
        :class="[
          widthClass,
          side === 'right' ? 'border-l animate-slide-in-right' : 'border-r animate-fade-in',
        ]"
      >
        <div
          v-if="title || $slots.header"
          class="flex items-start justify-between gap-3 border-b border-line px-5 py-4"
        >
          <slot name="header">
            <div class="min-w-0">
              <h2 class="font-display text-lg font-bold text-ink truncate">
                {{ title }}
              </h2>
              <p v-if="subtitle" class="mt-0.5 text-xs text-ink-muted truncate">
                {{ subtitle }}
              </p>
            </div>
          </slot>
          <button
            type="button"
            aria-label="Fechar painel"
            class="inline-flex h-9 w-9 shrink-0 items-center justify-center rounded-full text-ink-muted transition hover:bg-surface-muted hover:text-ink"
            @click="emit('close')"
          >
            <X class="h-5 w-5" />
          </button>
        </div>

        <div class="flex-1 overflow-y-auto px-5 py-4">
          <slot />
        </div>

        <div
          v-if="$slots.footer"
          class="border-t border-line bg-surface-subtle px-5 py-4 safe-bottom"
        >
          <slot name="footer" />
        </div>
      </div>
    </div>
  </Teleport>
</template>
