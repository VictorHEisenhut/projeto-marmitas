<script setup lang="ts">
withDefaults(
  defineProps<{
    modelValue: boolean
    label?: string
    description?: string
    disabled?: boolean
    activeColor?: 'brand' | 'fresh'
    size?: 'sm' | 'md'
  }>(),
  {
    disabled: false,
    activeColor: 'brand',
    size: 'md',
  },
)

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
}>()
</script>

<template>
  <div class="flex items-center justify-between gap-3">
    <div v-if="label || description" class="min-w-0">
      <p v-if="label" class="text-sm font-semibold text-ink">{{ label }}</p>
      <p v-if="description" class="text-xs text-ink-muted">{{ description }}</p>
    </div>

    <button
      type="button"
      role="switch"
      :aria-checked="modelValue"
      :aria-label="label || 'Alternar opção'"
      :disabled="disabled"
      class="relative inline-flex shrink-0 cursor-pointer items-center rounded-full transition-colors duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 focus-visible:ring-offset-2 focus-visible:ring-offset-canvas disabled:cursor-not-allowed disabled:opacity-50"
      :class="[
        size === 'sm' ? 'h-5 w-9' : 'h-6 w-11',
        modelValue
          ? activeColor === 'fresh'
            ? 'bg-fresh-600'
            : 'bg-brand-600'
          : 'bg-line-strong',
      ]"
      @click="emit('update:modelValue', !modelValue)"
    >
      <span
        class="pointer-events-none inline-block rounded-full bg-white shadow-sm transition-transform duration-200"
        :class="[
          size === 'sm' ? 'h-4 w-4' : 'h-5 w-5',
          modelValue
            ? size === 'sm'
              ? 'translate-x-4'
              : 'translate-x-5'
            : 'translate-x-0.5',
        ]"
      />
    </button>
  </div>
</template>
