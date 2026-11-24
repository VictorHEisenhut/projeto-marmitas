<script setup lang="ts">
import { computed } from 'vue'
import { Loader2 } from 'lucide-vue-next'

export type ButtonVariant = 'primary' | 'secondary' | 'outline' | 'ghost' | 'danger' | 'fresh'
export type ButtonSize = 'xs' | 'sm' | 'md' | 'lg'

const props = withDefaults(
  defineProps<{
    variant?: ButtonVariant
    size?: ButtonSize
    type?: 'button' | 'submit' | 'reset'
    loading?: boolean
    disabled?: boolean
    block?: boolean
  }>(),
  {
    variant: 'primary',
    size: 'md',
    type: 'button',
    loading: false,
    disabled: false,
    block: false,
  },
)

const variantClasses = computed(() => {
  const map: Record<ButtonVariant, string> = {
    primary:
      'bg-brand-600 text-white hover:bg-brand-700 active:bg-brand-800 shadow-sm shadow-brand-600/20',
    secondary:
      'bg-ink text-canvas hover:opacity-90 active:opacity-85 shadow-sm',
    fresh:
      'bg-fresh-600 text-white hover:bg-fresh-700 active:bg-fresh-800 shadow-sm shadow-fresh-600/20',
    outline:
      'border border-line-strong bg-surface text-ink hover:bg-surface-muted active:bg-surface-subtle',
    ghost:
      'bg-transparent text-ink-secondary hover:bg-surface-muted hover:text-ink',
    danger:
      'bg-red-600 text-white hover:bg-red-700 active:bg-red-800 shadow-sm shadow-red-600/20',
  }
  return map[props.variant]
})

const sizeClasses = computed(() => {
  const map: Record<ButtonSize, string> = {
    xs: 'h-7 px-2.5 text-xs gap-1.5 rounded-lg',
    sm: 'h-9 px-3.5 text-xs gap-1.5 rounded-xl',
    md: 'h-10 px-4 text-sm gap-2 rounded-xl',
    lg: 'h-12 px-5 text-sm gap-2 rounded-xl',
  }
  return map[props.size]
})
</script>

<template>
  <button
    :type="type"
    :disabled="disabled || loading"
    class="inline-flex items-center justify-center font-semibold transition-all duration-150 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 focus-visible:ring-offset-2 focus-visible:ring-offset-canvas disabled:pointer-events-none disabled:opacity-50 select-none"
    :class="[variantClasses, sizeClasses, block ? 'w-full' : '']"
  >
    <Loader2 v-if="loading" class="h-4 w-4 shrink-0 animate-spin" />
    <slot />
  </button>
</template>
