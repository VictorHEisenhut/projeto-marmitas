<script setup lang="ts">
import { ref } from 'vue'
import { Star } from 'lucide-vue-next'

const props = withDefaults(
  defineProps<{
    modelValue: number
    readonly?: boolean
    size?: 'sm' | 'md' | 'lg'
    showValue?: boolean
  }>(),
  {
    readonly: false,
    size: 'md',
    showValue: false,
  },
)

const emit = defineEmits<{
  (e: 'update:modelValue', value: number): void
}>()

const hovered = ref(0)

const sizeClass: Record<string, string> = {
  sm: 'h-3.5 w-3.5',
  md: 'h-5 w-5',
  lg: 'h-7 w-7',
}

function selectStar(star: number) {
  if (props.readonly) return
  emit('update:modelValue', star)
}
</script>

<template>
  <div class="inline-flex items-center gap-1" :role="readonly ? undefined : 'radiogroup'" aria-label="Avaliação de 1 a 5 estrelas">
    <button
      v-for="star in 5"
      :key="star"
      type="button"
      :disabled="readonly"
      :aria-label="`${star} estrela${star > 1 ? 's' : ''}`"
      class="transition-transform duration-100 focus-visible:outline-none"
      :class="readonly ? 'cursor-default' : 'cursor-pointer hover:scale-110'"
      @mouseenter="!readonly && (hovered = star)"
      @mouseleave="!readonly && (hovered = 0)"
      @click="selectStar(star)"
    >
      <Star
        :class="[
          sizeClass[size],
          (hovered || modelValue) >= star
            ? 'fill-amber-400 text-amber-400'
            : 'fill-transparent text-line-strong',
        ]"
      />
    </button>
    <span v-if="showValue" class="ml-1 text-xs font-bold text-ink-secondary tabular-nums">
      {{ modelValue.toFixed(1) }}
    </span>
  </div>
</template>
