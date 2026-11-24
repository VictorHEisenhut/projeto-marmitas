<script setup lang="ts">
withDefaults(
  defineProps<{
    variant?: 'line' | 'card' | 'circle' | 'rect'
    height?: string
    width?: string
  }>(),
  {
    variant: 'line',
  },
)
</script>

<template>
  <div
    aria-hidden="true"
    class="animate-pulse bg-surface-muted"
    :class="
      variant === 'circle'
        ? 'rounded-full'
        : variant === 'card'
          ? 'rounded-2xl border border-line'
          : variant === 'rect'
            ? 'rounded-xl'
            : 'rounded-md h-4'
    "
    :style="{ height, width }"
  />
</template>
