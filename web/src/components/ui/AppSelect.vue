<script setup lang="ts">
import { computed, useId } from 'vue'
import { ChevronDown } from 'lucide-vue-next'

export interface SelectOption {
  label: string
  value: string | number
  disabled?: boolean
}

const props = withDefaults(
  defineProps<{
    modelValue?: string | number | null
    label?: string
    options?: SelectOption[]
    placeholder?: string
    required?: boolean
    disabled?: boolean
    error?: string
    id?: string
  }>(),
  {
    modelValue: '',
    required: false,
    disabled: false,
  },
)

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
}>()

const fallbackId = useId()
const selectId = computed(() => props.id || fallbackId)

function handleChange(event: Event) {
  const target = event.target as HTMLSelectElement
  emit('update:modelValue', target.value)
}
</script>

<template>
  <div class="space-y-1.5">
    <label
      v-if="label"
      :for="selectId"
      class="block text-xs font-semibold text-ink-secondary"
    >
      {{ label }}
      <span v-if="required" class="text-red-500">*</span>
    </label>

    <div class="relative">
      <select
        :id="selectId"
        :value="modelValue ?? ''"
        :required="required"
        :disabled="disabled"
        class="block w-full appearance-none rounded-xl border bg-surface px-3.5 py-2.5 pr-9 text-sm text-ink transition-colors focus:outline-none focus:ring-2 focus:ring-brand-500/25 disabled:cursor-not-allowed disabled:bg-surface-muted disabled:opacity-65"
        :class="
          error
            ? 'border-red-400 focus:border-red-500'
            : 'border-line-strong focus:border-brand-600'
        "
        @change="handleChange"
      >
        <option v-if="placeholder" value="" disabled>{{ placeholder }}</option>
        <template v-if="options && options.length > 0">
          <option
            v-for="opt in options"
            :key="String(opt.value)"
            :value="opt.value"
            :disabled="opt.disabled"
          >
            {{ opt.label }}
          </option>
        </template>
        <slot v-else />
      </select>
      <ChevronDown
        class="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-muted"
      />
    </div>

    <p v-if="error" class="text-xs font-medium text-red-600 dark:text-red-400">
      {{ error }}
    </p>
  </div>
</template>
