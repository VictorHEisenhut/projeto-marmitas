<script setup lang="ts">
import { computed } from 'vue'

export type BadgeVariant = 'neutral' | 'brand' | 'fresh' | 'warning' | 'danger' | 'info' | 'dark'

const props = withDefaults(
  defineProps<{
    variant?: BadgeVariant
    size?: 'xs' | 'sm'
    dot?: boolean
  }>(),
  {
    variant: 'neutral',
    size: 'xs',
    dot: false,
  },
)

const variantClasses = computed(() => {
  const map: Record<BadgeVariant, string> = {
    neutral:
      'bg-surface-muted text-ink-secondary border-line',
    brand:
      'bg-brand-50 text-brand-700 border-brand-200/80 dark:bg-brand-950/60 dark:text-brand-300 dark:border-brand-800/60',
    fresh:
      'bg-fresh-50 text-fresh-700 border-fresh-200/80 dark:bg-fresh-950/60 dark:text-fresh-300 dark:border-fresh-800/60',
    warning:
      'bg-amber-50 text-amber-800 border-amber-200/80 dark:bg-amber-950/50 dark:text-amber-300 dark:border-amber-800/60',
    danger:
      'bg-red-50 text-red-700 border-red-200/80 dark:bg-red-950/50 dark:text-red-300 dark:border-red-800/60',
    info:
      'bg-sky-50 text-sky-700 border-sky-200/80 dark:bg-sky-950/50 dark:text-sky-300 dark:border-sky-800/60',
    dark:
      'bg-ink text-canvas border-transparent',
  }
  return map[props.variant]
})

const dotColor = computed(() => {
  const map: Record<BadgeVariant, string> = {
    neutral: 'bg-ink-muted',
    brand: 'bg-brand-500',
    fresh: 'bg-fresh-500',
    warning: 'bg-amber-500',
    danger: 'bg-red-500',
    info: 'bg-sky-500',
    dark: 'bg-canvas',
  }
  return map[props.variant]
})
</script>

<template>
  <span
    class="inline-flex items-center gap-1.5 rounded-full border font-semibold whitespace-nowrap"
    :class="[
      variantClasses,
      size === 'xs' ? 'px-2.5 py-0.5 text-[11px] leading-4' : 'px-3 py-1 text-xs leading-4',
    ]"
  >
    <span v-if="dot" class="h-1.5 w-1.5 rounded-full shrink-0" :class="dotColor" />
    <slot />
  </span>
</template>
