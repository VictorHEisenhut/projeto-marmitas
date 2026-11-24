<script setup lang="ts">
withDefaults(
  defineProps<{
    padding?: 'none' | 'sm' | 'md' | 'lg'
    interactive?: boolean
  }>(),
  {
    padding: 'md',
    interactive: false,
  },
)
</script>

<template>
  <div
    class="rounded-2xl border border-line bg-surface shadow-card transition-all duration-200"
    :class="[
      padding === 'none' ? '' : padding === 'sm' ? 'p-4' : padding === 'lg' ? 'p-6 sm:p-8' : 'p-5',
      interactive ? 'cursor-pointer hover:-translate-y-0.5 hover:border-brand-300/80 hover:shadow-card-hover dark:hover:border-brand-700/70' : '',
    ]"
  >
    <slot />
  </div>
</template>
