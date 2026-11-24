<script setup lang="ts">
import { computed, useId } from 'vue'

const props = withDefaults(
  defineProps<{
    modelValue?: string | number | null
    label?: string
    type?: string
    placeholder?: string
    required?: boolean
    disabled?: boolean
    readonly?: boolean
    error?: string
    hint?: string
    id?: string
    autocomplete?: string
    maxlength?: number | string
    minlength?: number | string
    min?: number | string
    max?: number | string
    step?: number | string
    inputClass?: string
  }>(),
  {
    modelValue: '',
    type: 'text',
    required: false,
    disabled: false,
    readonly: false,
  },
)

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
  (e: 'blur', event: FocusEvent): void
  (e: 'input', event: Event): void
}>()

const fallbackId = useId()
const inputId = computed(() => props.id || fallbackId)

function handleInput(event: Event) {
  const target = event.target as HTMLInputElement
  emit('update:modelValue', target.value)
  emit('input', event)
}
</script>

<template>
  <div class="space-y-1.5">
    <div v-if="label || $slots.labelRight" class="flex items-center justify-between gap-2">
      <label
        v-if="label"
        :for="inputId"
        class="block text-xs font-semibold text-ink-secondary"
      >
        {{ label }}
        <span v-if="required" class="text-red-500">*</span>
      </label>
      <slot name="labelRight" />
    </div>

    <div class="relative flex items-center">
      <div
        v-if="$slots.prefix"
        class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3.5 text-ink-muted"
      >
        <slot name="prefix" />
      </div>

      <input
        :id="inputId"
        :value="modelValue ?? ''"
        :type="type"
        :placeholder="placeholder"
        :required="required"
        :disabled="disabled"
        :readonly="readonly"
        :autocomplete="autocomplete"
        :maxlength="maxlength"
        :minlength="minlength"
        :min="min"
        :max="max"
        :step="step"
        class="block w-full rounded-xl border bg-surface px-3.5 py-2.5 text-sm text-ink placeholder:text-ink-muted/70 transition-colors focus:outline-none focus:ring-2 focus:ring-brand-500/25 disabled:cursor-not-allowed disabled:bg-surface-muted disabled:opacity-65"
        :class="[
          error
            ? 'border-red-400 focus:border-red-500 dark:border-red-500/70'
            : 'border-line-strong focus:border-brand-600',
          $slots.prefix ? 'pl-10' : '',
          $slots.suffix ? 'pr-10' : '',
          inputClass,
        ]"
        @input="handleInput"
        @blur="emit('blur', $event)"
      />

      <div
        v-if="$slots.suffix"
        class="absolute inset-y-0 right-0 flex items-center pr-3 text-ink-muted"
      >
        <slot name="suffix" />
      </div>
    </div>

    <p v-if="error" class="text-xs font-medium text-red-600 dark:text-red-400">
      {{ error }}
    </p>
    <p v-else-if="hint" class="text-xs text-ink-muted">
      {{ hint }}
    </p>
  </div>
</template>
