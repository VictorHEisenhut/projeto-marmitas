<script setup lang="ts">
import type { Component } from 'vue'

export interface TabItem {
  id: string
  label: string
  count?: number
  badgeVariant?: 'neutral' | 'danger' | 'brand' | 'fresh'
  icon?: Component
}

withDefaults(
  defineProps<{
    modelValue: string
    tabs: TabItem[]
    variant?: 'pills' | 'segmented' | 'underline'
  }>(),
  {
    variant: 'pills',
  },
)

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
}>()
</script>

<template>
  <div
    role="tablist"
    class="flex items-center gap-1.5 overflow-x-auto no-scrollbar"
    :class="
      variant === 'segmented'
        ? 'rounded-xl bg-surface-muted p-1'
        : variant === 'underline'
          ? 'border-b border-line gap-4'
          : 'pb-1'
    "
  >
    <button
      v-for="tab in tabs"
      :key="tab.id"
      type="button"
      role="tab"
      :aria-selected="modelValue === tab.id"
      class="inline-flex shrink-0 items-center justify-center gap-2 whitespace-nowrap font-semibold transition-all duration-150"
      :class="[
        variant === 'segmented'
          ? [
              'flex-1 rounded-lg px-3.5 py-2 text-xs',
              modelValue === tab.id
                ? 'bg-surface text-ink shadow-sm'
                : 'text-ink-muted hover:text-ink',
            ]
          : variant === 'underline'
            ? [
                'border-b-2 px-1 pb-2.5 text-sm',
                modelValue === tab.id
                  ? 'border-brand-600 text-brand-600 dark:text-brand-400'
                  : 'border-transparent text-ink-muted hover:text-ink',
              ]
            : [
                'rounded-xl border px-3.5 py-2 text-xs',
                modelValue === tab.id
                  ? 'border-brand-600 bg-brand-600 text-white shadow-sm'
                  : 'border-line bg-surface text-ink-secondary hover:border-line-strong hover:bg-surface-muted',
              ],
      ]"
      @click="emit('update:modelValue', tab.id)"
    >
      <component :is="tab.icon" v-if="tab.icon" class="h-4 w-4 shrink-0" />
      <span>{{ tab.label }}</span>
      <span
        v-if="tab.count !== undefined"
        class="inline-flex min-w-[1.25rem] items-center justify-center rounded-full px-1.5 py-0.5 text-[10px] font-bold tabular-nums"
        :class="
          tab.badgeVariant === 'danger' && tab.count > 0
            ? 'bg-red-600 text-white'
            : modelValue === tab.id && variant === 'pills'
              ? 'bg-white/20 text-white'
              : 'bg-surface-muted text-ink-secondary'
        "
      >
        {{ tab.count }}
      </span>
    </button>
  </div>
</template>
