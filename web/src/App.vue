<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { Moon, Sun } from 'lucide-vue-next'
import { useThemeStore } from '@/stores/theme'

const theme = useThemeStore()
const route = useRoute()

const isStandaloneAuthRoute = computed(() => {
  const paths = ['/login', '/cadastro', '/entregador/cadastro', '/esqueci-senha', '/redefinir-senha', '/acesso-negado']
  return paths.includes(route.path)
})

onMounted(() => {
  theme.initTheme()
})
</script>

<template>
  <div class="relative min-h-screen">
    <button
      v-if="isStandaloneAuthRoute"
      type="button"
      :title="theme.isDark ? 'Ativar modo claro' : 'Ativar modo noturno'"
      :aria-label="theme.isDark ? 'Ativar modo claro' : 'Ativar modo noturno'"
      class="fixed right-4 top-4 z-50 inline-flex items-center gap-1.5 rounded-lg border border-stone-200 bg-white px-3 py-1.5 text-xs font-medium text-stone-700 shadow-sm transition hover:bg-stone-100"
      @click="theme.toggleTheme()"
    >
      <Sun v-if="theme.isDark" class="h-4 w-4 text-amber-400" />
      <Moon v-else class="h-4 w-4 text-stone-600" />
      <span>{{ theme.isDark ? 'Modo Claro' : 'Modo Noturno' }}</span>
    </button>

    <RouterView />
  </div>
</template>
