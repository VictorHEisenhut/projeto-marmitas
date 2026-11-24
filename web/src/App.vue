<script setup lang="ts">
import { onMounted } from 'vue'
import { useThemeStore } from '@/stores/theme'
import AppToast from '@/components/ui/AppToast.vue'

const theme = useThemeStore()

onMounted(() => {
  theme.initTheme()
})
</script>

<template>
  <div class="relative min-h-screen bg-canvas text-ink">
    <RouterView />
    <AppToast />
  </div>
</template>
