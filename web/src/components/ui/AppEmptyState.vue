<script setup lang="ts">
import type { Component } from 'vue'

defineProps<{
  icon?: Component
  title: string
  description?: string
}>()
</script>

<template>
  <div
    class="flex flex-col items-center justify-center rounded-2xl border border-dashed border-line-strong bg-surface px-6 py-12 text-center"
  >
    <div
      v-if="icon"
      class="mb-4 flex h-12 w-12 items-center justify-center rounded-2xl bg-brand-50 text-brand-600 dark:bg-brand-950/60 dark:text-brand-400"
    >
      <component :is="icon" class="h-6 w-6" />
    </div>
    <h3 class="font-display text-base font-bold text-ink">{{ title }}</h3>
    <p v-if="description" class="mt-1 max-w-sm text-sm text-ink-muted">
      {{ description }}
    </p>
    <div v-if="$slots.action" class="mt-5">
      <slot name="action" />
    </div>
  </div>
</template>
