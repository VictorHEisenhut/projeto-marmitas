<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import {
  DollarSign,
  ExternalLink,
  LogOut,
  Menu,
  Moon,
  RefreshCw,
  Shield,
  ShoppingBag,
  Store,
  Sun,
  Tag,
  Users,
} from 'lucide-vue-next'
import NotificationBell from '@/components/ui/NotificationBell.vue'
import AppDrawer from '@/components/ui/AppDrawer.vue'
import { useAuthStore } from '@/stores/auth'
import { useThemeStore } from '@/stores/theme'

export type AdminModule = 'restaurantes' | 'planos' | 'usuarios' | 'pedidos' | 'financeiro'

defineProps<{
  activeModule: AdminModule
  loading?: boolean
}>()

const emit = defineEmits<{
  (e: 'update:activeModule', value: AdminModule): void
  (e: 'refresh'): void
}>()

const auth = useAuthStore()
const theme = useThemeStore()
const router = useRouter()
const mobileNavOpen = ref(false)

const navItems: Array<{ id: AdminModule; label: string; icon: typeof Store }> = [
  { id: 'restaurantes', label: 'Restaurantes & Categorias', icon: Store },
  { id: 'planos', label: 'Planos & Cupons', icon: Tag },
  { id: 'usuarios', label: 'Usuários & Entregadores', icon: Users },
  { id: 'pedidos', label: 'Pedidos & Estornos', icon: ShoppingBag },
  { id: 'financeiro', label: 'Financeiro & Auditoria', icon: DollarSign },
]

function selectModule(id: AdminModule) {
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
    <!-- Persistent SaaS Sidebar -->
    <aside class="hidden lg:flex lg:w-64 lg:flex-col lg:fixed lg:inset-y-0 border-r border-line bg-surface">
      <div class="flex h-16 items-center gap-2.5 border-b border-line px-5">
        <span class="flex h-9 w-9 items-center justify-center rounded-xl bg-ink text-canvas shadow-sm">
          <Shield class="h-5 w-5" />
        </span>
        <div class="min-w-0">
          <p class="font-display text-sm font-extrabold text-ink leading-tight">Marmitas Admin</p>
          <p class="text-[11px] font-medium text-ink-muted">Backoffice & Governança</p>
        </div>
      </div>

      <nav class="flex-1 space-y-1 p-3">
        <button
          v-for="item in navItems"
          :key="item.id"
          type="button"
          class="flex w-full items-center gap-2.5 rounded-xl px-3.5 py-2.5 text-sm font-semibold transition"
          :class="
            activeModule === item.id
              ? 'bg-brand-600 text-white shadow-sm shadow-brand-600/20'
              : 'text-ink-secondary hover:bg-surface-muted hover:text-ink'
          "
          @click="selectModule(item.id)"
        >
          <component :is="item.icon" class="h-4 w-4 shrink-0" />
          <span class="truncate">{{ item.label }}</span>
        </button>

        <div class="my-3 border-t border-line" />

        <p class="px-3.5 pb-1 text-[10px] font-bold uppercase tracking-wider text-ink-muted">
          Atalhos de operação
        </p>
        <RouterLink
          to="/"
          class="flex items-center gap-2.5 rounded-xl px-3.5 py-2 text-xs font-semibold text-ink-muted hover:bg-surface-muted hover:text-ink"
        >
          <ExternalLink class="h-3.5 w-3.5" />
          Vitrine Pública
        </RouterLink>
        <RouterLink
          to="/parceiro"
          class="flex items-center gap-2.5 rounded-xl px-3.5 py-2 text-xs font-semibold text-ink-muted hover:bg-surface-muted hover:text-ink"
        >
          <Store class="h-3.5 w-3.5" />
          Painel Restaurante
        </RouterLink>
        <RouterLink
          to="/entregador"
          class="flex items-center gap-2.5 rounded-xl px-3.5 py-2 text-xs font-semibold text-ink-muted hover:bg-surface-muted hover:text-ink"
        >
          <Users class="h-3.5 w-3.5" />
          Painel Entregador
        </RouterLink>
      </nav>

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
          Sair
        </button>
      </div>
    </aside>

    <!-- Main Area -->
    <div class="flex flex-1 flex-col lg:pl-64 min-w-0">
      <header class="sticky top-0 z-20 flex h-16 items-center justify-between gap-3 border-b border-line bg-surface/90 px-4 backdrop-blur-md sm:px-6">
        <div class="flex items-center gap-3 min-w-0">
          <button
            type="button"
            aria-label="Abrir menu administrativo"
            class="inline-flex h-10 w-10 items-center justify-center rounded-xl border border-line bg-surface text-ink-secondary lg:hidden"
            @click="mobileNavOpen = true"
          >
            <Menu class="h-5 w-5" />
          </button>

          <div class="min-w-0">
            <h1 class="font-display text-base sm:text-lg font-extrabold text-ink truncate">
              {{ navItems.find((i) => i.id === activeModule)?.label || 'Painel Administrativo' }}
            </h1>
            <p class="hidden sm:block text-xs text-ink-muted">
              Monitoramento executivo, homologações, pedidos e auditoria em tempo real.
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
            <span class="hidden sm:inline">Atualizar dados</span>
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

      <main class="mx-auto w-full max-w-7xl flex-1 px-4 py-6 sm:px-6">
        <slot />
      </main>
    </div>

    <!-- Mobile Drawer -->
    <AppDrawer
      :open="mobileNavOpen"
      side="left"
      title="Marmitas Admin"
      :subtitle="auth.user?.email"
      width-class="max-w-xs"
      @close="mobileNavOpen = false"
    >
      <nav class="space-y-1">
        <button
          v-for="item in navItems"
          :key="item.id"
          type="button"
          class="flex w-full items-center gap-2.5 rounded-xl px-3.5 py-2.5 text-sm font-semibold transition"
          :class="
            activeModule === item.id
              ? 'bg-brand-600 text-white'
              : 'text-ink-secondary hover:bg-surface-muted'
          "
          @click="selectModule(item.id)"
        >
          <component :is="item.icon" class="h-4 w-4" />
          {{ item.label }}
        </button>
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
