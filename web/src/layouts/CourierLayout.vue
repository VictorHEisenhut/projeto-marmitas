<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router'
import { Bike, FileCheck2, LogOut, Moon, RefreshCw, Sun, User as UserIcon } from 'lucide-vue-next'
import NotificationBell from '@/components/ui/NotificationBell.vue'
import { useAuthStore } from '@/stores/auth'
import { useThemeStore } from '@/stores/theme'

defineProps<{
  isOnline?: boolean
  loading?: boolean
  activeTab?: 'queue' | 'onboarding'
}>()

const emit = defineEmits<{
  (e: 'update:activeTab', value: 'queue' | 'onboarding'): void
  (e: 'refresh'): void
}>()

const auth = useAuthStore()
const theme = useThemeStore()
const route = useRoute()
const router = useRouter()

async function handleLogout() {
  await auth.logout()
  await router.push('/login')
}
</script>

<template>
  <div class="min-h-screen bg-canvas text-ink pb-20 md:pb-8">
    <header class="sticky top-0 z-30 border-b border-line bg-surface/95 backdrop-blur-md">
      <div class="mx-auto flex h-16 max-w-5xl items-center justify-between gap-3 px-4 sm:px-6">
        <div class="flex items-center gap-3">
          <RouterLink to="/entregador" class="flex items-center gap-2.5">
            <span
              class="flex h-10 w-10 items-center justify-center rounded-xl text-white shadow-sm transition-colors"
              :class="isOnline ? 'bg-fresh-600' : 'bg-brand-600'"
            >
              <Bike class="h-5 w-5" />
            </span>
            <div>
              <p class="font-display text-sm font-extrabold text-ink leading-tight">
                Marmitas Entregador
              </p>
              <div class="flex items-center gap-1.5 mt-0.5">
                <span
                  class="h-2 w-2 rounded-full"
                  :class="isOnline ? 'bg-fresh-500 animate-pulse' : 'bg-ink-muted'"
                />
                <span class="text-[11px] font-semibold" :class="isOnline ? 'text-fresh-600 dark:text-fresh-400' : 'text-ink-muted'">
                  {{ isOnline ? 'Online · Disponível' : 'Offline' }}
                </span>
              </div>
            </div>
          </RouterLink>
        </div>

        <div class="flex items-center gap-2">
          <button
            type="button"
            aria-label="Atualizar corridas"
            :disabled="loading"
            class="inline-flex h-10 w-10 items-center justify-center rounded-xl border border-line bg-surface text-ink-secondary transition hover:bg-surface-muted disabled:opacity-50"
            @click="emit('refresh')"
          >
            <RefreshCw class="h-4 w-4" :class="{ 'animate-spin': loading }" />
          </button>

          <button
            type="button"
            :aria-label="theme.isDark ? 'Modo claro' : 'Modo escuro'"
            class="inline-flex h-10 w-10 items-center justify-center rounded-xl border border-line bg-surface text-ink-secondary transition hover:bg-surface-muted"
            @click="theme.toggleTheme()"
          >
            <Sun v-if="theme.isDark" class="h-4 w-4 text-amber-400" />
            <Moon v-else class="h-4 w-4" />
          </button>

          <NotificationBell />

          <RouterLink
            to="/conta"
            class="hidden md:inline-flex h-10 items-center gap-2 rounded-xl border border-line bg-surface px-3 text-xs font-semibold text-ink-secondary hover:bg-surface-muted hover:text-ink"
          >
            <UserIcon class="h-4 w-4" />
            Central & Ganhos
          </RouterLink>

          <button
            type="button"
            aria-label="Sair"
            class="inline-flex h-10 w-10 items-center justify-center rounded-xl border border-line bg-surface text-ink-secondary transition hover:bg-red-50 hover:text-red-600 dark:hover:bg-red-950/40"
            @click="handleLogout"
          >
            <LogOut class="h-4 w-4" />
          </button>
        </div>
      </div>
    </header>

    <main class="mx-auto max-w-5xl px-4 py-5 sm:px-6">
      <slot />
    </main>

    <!-- Mobile Bottom Bar for Field Operations -->
    <nav
      aria-label="Navegação entregador"
      class="fixed bottom-0 left-0 right-0 z-30 border-t border-line bg-surface/95 backdrop-blur-md md:hidden safe-bottom"
    >
      <div class="grid grid-cols-3 h-14">
        <button
          type="button"
          class="flex flex-col items-center justify-center gap-0.5 text-[11px] font-semibold"
          :class="activeTab === 'queue' ? 'text-brand-600' : 'text-ink-muted'"
          @click="emit('update:activeTab', 'queue')"
        >
          <Bike class="h-5 w-5" />
          Corridas
        </button>
        <button
          type="button"
          class="flex flex-col items-center justify-center gap-0.5 text-[11px] font-semibold"
          :class="activeTab === 'onboarding' ? 'text-brand-600' : 'text-ink-muted'"
          @click="emit('update:activeTab', 'onboarding')"
        >
          <FileCheck2 class="h-5 w-5" />
          Homologação
        </button>
        <RouterLink
          to="/conta"
          class="flex flex-col items-center justify-center gap-0.5 text-[11px] font-semibold"
          :class="route.path === '/conta' ? 'text-brand-600' : 'text-ink-muted'"
        >
          <UserIcon class="h-5 w-5" />
          Minha Conta
        </RouterLink>
      </div>
    </nav>
  </div>
</template>
