<script setup lang="ts">
import { computed, ref } from 'vue'

const props = withDefaults(
  defineProps<{
    modelValue: string
    length?: number
    disabled?: boolean
  }>(),
  {
    length: 4,
    disabled: false,
  },
)

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
}>()

const inputRefs = ref<Array<HTMLInputElement | null>>([])

const digits = computed(() => {
  const clean = (props.modelValue || '').replace(/\D/g, '').slice(0, props.length)
  return Array.from({ length: props.length }, (_, idx) => clean[idx] || '')
})

function updateAt(index: number, char: string) {
  const arr = [...digits.value]
  arr[index] = char
  emit('update:modelValue', arr.join(''))
}

function handleInput(index: number, event: Event) {
  const target = event.target as HTMLInputElement
  const onlyDigits = target.value.replace(/\D/g, '')
  if (!onlyDigits) {
    updateAt(index, '')
    return
  }

  if (onlyDigits.length > 1) {
    const pasted = onlyDigits.slice(0, props.length)
    emit('update:modelValue', pasted)
    const nextIdx = Math.min(pasted.length, props.length - 1)
    inputRefs.value[nextIdx]?.focus()
    return
  }

  updateAt(index, onlyDigits)
  if (index < props.length - 1) {
    inputRefs.value[index + 1]?.focus()
  }
}

function handleKeydown(index: number, event: KeyboardEvent) {
  if (event.key === 'Backspace' && !digits.value[index] && index > 0) {
    inputRefs.value[index - 1]?.focus()
  }
}
</script>

<template>
  <div class="inline-flex items-center gap-2" role="group" aria-label="Código PIN de entrega">
    <input
      v-for="(digit, idx) in digits"
      :key="idx"
      :ref="(el) => { inputRefs[idx] = el as HTMLInputElement | null }"
      :value="digit"
      type="text"
      inputmode="numeric"
      maxlength="4"
      :disabled="disabled"
      :aria-label="`Dígito ${idx + 1} do PIN`"
      class="h-12 w-11 rounded-xl border-2 border-line-strong bg-surface text-center font-display text-lg font-extrabold text-ink tabular-nums transition focus:border-brand-600 focus:outline-none focus:ring-2 focus:ring-brand-500/25 disabled:opacity-50"
      @input="handleInput(idx, $event)"
      @keydown="handleKeydown(idx, $event)"
    />
  </div>
</template>
