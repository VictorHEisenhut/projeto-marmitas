<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import {
  ChefHat,
  ClipboardList,
  DollarSign,
  ExternalLink,
  LogOut,
  Menu,
  Moon,
  RefreshCw,
  Settings,
  Sparkles,
  Store,
  Sun,
  Utensils,
  UtensilsCrossed,
} from 'lucide-vue-next'
import NotificationBell from '@/components/ui/NotificationBell.vue'
import AppDrawer from '@/components/ui/AppDrawer.vue'
import { useAuthStore } from '@/stores/auth'
import { useThemeStore } from '@/stores/theme'
import type { Restaurant } from '@/types'

export type PartnerModule = 'orders' | 'menu' | 'plans' | 'financials' | 'settings'

defineProps<{
  activeModule: PartnerModule
  restaurants: Restaurant[]
  selectedRestaurantId: string
  loading?: boolean
  pendingOrdersCount?: number
  pendingComplaintsCount?: number
}>()

const emit = defineEmits<{
  (e: 'update:activeModule', value: PartnerModule): void
  (e: 'update:selectedRestaurantId', value: string): void
  (e: 'refresh'): void
}>()

const auth = useAuthStore()
const theme = useThemeStore()
const router = useRouter()
const mobileNavOpen = ref(false)

const navItems: Array<{ id: PartnerModule; label: string; icon: typeof ClipboardList }> = [
  { id: 'orders', label: 'Gestor de Pedidos', icon: ClipboardList },
  { id: 'menu', label: 'Cardápio', icon: UtensilsCrossed },
  { id: 'plans', label: 'Planos da Loja', icon: Sparkles },
  { id: 'financials', label: 'Financeiro', icon: DollarSign },
  { id: 'settings', label: 'Loja, Zonas & Equipe', icon: Settings },
]

function selectModule(id: PartnerModule) {
  emit('update:activeModule', id)
  mobileNavOpen.value = false
}

async function handleLogout() {
  await auth.logout()
  await router.push('/login')
}
</script>

<template>
  <div class="flex min-h-screen bg-canvas text-ink">
    <!-- Desktop Persistent Sidebar -->
    <aside class="hidden lg:flex lg:w-64 lg:flex-col lg:fixed lg:inset-y-0 border-r border-line bg-surface">
      <div class="flex h-16 items-center gap-2.5 border-b border-line px-5">
        <span class="flex h-9 w-9 items-center justify-center rounded-xl bg-brand-600 text-white shadow-sm">
          <ChefHat class="h-5 w-5" />
        </span>
        <div class="min-w-0">
          <p class="font-display text-sm font-extrabold text-ink leading-tight">Gestor Marmitas</p>
          <p class="text-[11px] font-medium text-ink-muted">Portal do Restaurante</p>
        </div>
      </div>

      <!-- Store Selector -->
      <div v-if="restaurants.length > 0" class="border-b border-line p-4">
        <label class="block text-[10px] font-bold uppercase tracking-wider text-ink-muted mb-1.5">
          Loja ativa
        </label>
        <div class="relative">
          <Store class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-brand-600" />
          <select
            :value="selectedRestaurantId"
            class="w-full rounded-xl border border-line-strong bg-surface-subtle py-2 pl-9 pr-3 text-xs font-bold text-ink focus:border-brand-600 focus:outline-none"
            @change="emit('update:selectedRestaurantId', ($event.target as HTMLSelectElement).value)"
          >
            <option v-for="r in restaurants" :key="r.id" :value="r.id">
              {{ r.name }}
            </option>
          </select>
        </div>
      </div>

      <!-- Navigation -->
      <nav class="flex-1 space-y-1 p-3">
        <button
          v-for="item in navItems"
          :key="item.id"
          type="button"
          class="flex w-full items-center justify-between rounded-xl px-3.5 py-2.5 text-sm font-semibold transition"
          :class="
            activeModule === item.id
              ? 'bg-brand-600 text-white shadow-sm shadow-brand-600/20'
              : 'text-ink-secondary hover:bg-surface-muted hover:text-ink'
          "
          @click="selectModule(item.id)"
        >
          <span class="flex items-center gap-2.5">
            <component :is="item.icon" class="h-4 w-4 shrink-0" />
            <span>{{ item.label }}</span>
          </span>
          <span
            v-if="item.id === 'orders' && (pendingOrdersCount || 0) > 0"
            class="rounded-full px-2 py-0.5 text-[10px] font-bold tabular-nums"
            :class="
              activeModule === item.id
                ? 'bg-white/20 text-white'
                : 'bg-brand-100 text-brand-700 dark:bg-brand-950/60 dark:text-brand-300'
            "
          >
            {{ pendingOrdersCount }}
          </span>
        </button>

        <div class="my-3 border-t border-line" />

        <RouterLink
          to="/"
          class="flex items-center gap-2.5 rounded-xl px-3.5 py-2 text-xs font-semibold text-ink-muted transition hover:bg-surface-muted hover:text-ink"
        >
          <ExternalLink class="h-4 w-4" />
          Ver vitrine pública
        </RouterLink>
        <RouterLink
          v-if="auth.hasRole(['admin', 'super_admin'])"
          to="/admin"
          class="flex items-center gap-2.5 rounded-xl px-3.5 py-2 text-xs font-semibold text-ink-muted transition hover:bg-surface-muted hover:text-ink"
        >
          <Utensils class="h-4 w-4" />
          Ir para Admin
        </RouterLink>
      </nav>

      <!-- Sidebar Footer -->
      <div class="border-t border-line p-4 space-y-3">
        <div class="min-w-0">
          <p class="truncate text-xs font-bold text-ink">{{ auth.user?.name }}</p>
          <p class="truncate text-[11px] text-ink-muted">{{ auth.user?.email }}</p>
        </div>
        <button
          type="button"
          class="flex w-full items-center justify-center gap-2 rounded-xl border border-line px-3 py-2 text-xs font-semibold text-ink-secondary transition hover:bg-red-50 hover:text-red-600 dark:hover:bg-red-950/40 dark:hover:text-red-400"
          @click="handleLogout"
        >
          <LogOut class="h-3.5 w-3.5" />
          Sair da conta
        </button>
      </div>
    </aside>

    <!-- Main Content Area -->
    <div class="flex flex-1 flex-col lg:pl-64 min-w-0">
      <header class="sticky top-0 z-20 flex h-16 items-center justify-between gap-3 border-b border-line bg-surface/90 px-4 backdrop-blur-md sm:px-6">
        <div class="flex items-center gap-3 min-w-0">
          <button
            type="button"
            aria-label="Abrir menu do restaurante"
            class="inline-flex h-10 w-10 items-center justify-center rounded-xl border border-line bg-surface text-ink-secondary lg:hidden"
            @click="mobileNavOpen = true"
          >
            <Menu class="h-5 w-5" />
          </button>

          <div class="min-w-0">
            <h1 class="font-display text-base sm:text-lg font-extrabold text-ink truncate">
              {{ navItems.find((i) => i.id === activeModule)?.label || 'Painel do Restaurante' }}
            </h1>
            <p
              v-if="(pendingComplaintsCount || 0) > 0"
              class="text-[11px] font-semibold text-red-600 dark:text-red-400"
            >
              {{ pendingComplaintsCount }} reclamação(ões) aguardando atenção
            </p>
          </div>
        </div>

        <div class="flex items-center gap-2">
          <button
            type="button"
            :disabled="loading"
            class="inline-flex h-10 items-center gap-2 rounded-xl border border-line bg-surface px-3 text-xs font-semibold text-ink-secondary transition hover:bg-surface-muted hover:text-ink disabled:opacity-50"
            @click="emit('refresh')"
          >
            <RefreshCw class="h-4 w-4" :class="{ 'animate-spin': loading }" />
            <span class="hidden sm:inline">Atualizar</span>
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
        </div>
      </header>

      <main class="mx-auto w-full max-w-6xl flex-1 px-4 py-6 sm:px-6">
        <slot />
      </main>
    </div>

    <!-- Mobile Navigation Drawer -->
    <AppDrawer
      :open="mobileNavOpen"
      side="left"
      title="Gestor do Restaurante"
      :subtitle="auth.user?.name"
      width-class="max-w-xs"
      @close="mobileNavOpen = false"
    >
      <div v-if="restaurants.length > 0" class="mb-4">
        <label class="block text-[10px] font-bold uppercase tracking-wider text-ink-muted mb-1.5">
          Loja ativa
        </label>
        <select
          :value="selectedRestaurantId"
          class="w-full rounded-xl border border-line-strong bg-surface-subtle px-3 py-2 text-xs font-bold text-ink"
          @change="emit('update:selectedRestaurantId', ($event.target as HTMLSelectElement).value)"
        >
          <option v-for="r in restaurants" :key="r.id" :value="r.id">
            {{ r.name }}
          </option>
        </select>
      </div>

      <nav class="space-y-1">
        <button
          v-for="item in navItems"
          :key="item.id"
          type="button"
          class="flex w-full items-center justify-between rounded-xl px-3.5 py-2.5 text-sm font-semibold transition"
          :class="
            activeModule === item.id
              ? 'bg-brand-600 text-white'
              : 'text-ink-secondary hover:bg-surface-muted'
          "
          @click="selectModule(item.id)"
        >
          <span class="flex items-center gap-2.5">
            <component :is="item.icon" class="h-4 w-4" />
            {{ item.label }}
          </span>
        </button>

        <div class="my-3 border-t border-line" />

        <RouterLink
          to="/"
          class="flex items-center gap-2.5 rounded-xl px-3.5 py-2.5 text-xs font-semibold text-ink-secondary hover:bg-surface-muted"
          @click="mobileNavOpen = false"
        >
          <ExternalLink class="h-4 w-4" />
          Vitrine pública
        </RouterLink>
      </nav>

      <template #footer>
        <button
          type="button"
          class="flex w-full items-center justify-center gap-2 rounded-xl border border-line px-3 py-2.5 text-xs font-semibold text-red-600 hover:bg-red-50 dark:text-red-400"
          @click="handleLogout"
        >
          <LogOut class="h-4 w-4" />
          Sair da conta
        </button>
      </template>
    </AppDrawer>
  </div>
</template>
