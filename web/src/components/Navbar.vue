<script setup lang="ts">
import { useRouter } from 'vue-router'
import { LogOut, Moon, ShoppingBag, Sun, Utensils } from 'lucide-vue-next'
import NotificationBell from '@/components/ui/NotificationBell.vue'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import { useThemeStore } from '@/stores/theme'

const auth = useAuthStore()
const cart = useCartStore()
const theme = useThemeStore()
const router = useRouter()

async function handleLogout() {
  await auth.logout()
  await router.push('/login')
}
</script>

<template>
  <header class="sticky top-0 z-30 border-b border-line bg-surface/95 backdrop-blur-md">
    <div class="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6">
      <RouterLink
        :to="auth.isDriverOnly ? '/entregador' : '/'"
        class="flex items-center gap-2 font-display font-extrabold text-ink"
      >
        <span class="flex h-9 w-9 items-center justify-center rounded-xl bg-brand-600 text-white">
          <Utensils class="h-5 w-5" />
        </span>
        <span class="text-lg">Marmitas</span>
      </RouterLink>

      <div class="flex items-center gap-2">
        <button
          type="button"
          :aria-label="theme.isDark ? 'Ativar modo claro' : 'Ativar modo escuro'"
          class="inline-flex h-9 w-9 items-center justify-center rounded-xl border border-line bg-surface text-ink-secondary hover:bg-surface-muted"
          @click="theme.toggleTheme()"
        >
          <Sun v-if="theme.isDark" class="h-4 w-4 text-amber-400" />
          <Moon v-else class="h-4 w-4" />
        </button>

        <NotificationBell />

        <button
          v-if="cart.totalItems > 0 && !auth.isDriverOnly"
          type="button"
          class="inline-flex h-9 items-center gap-1.5 rounded-xl bg-brand-600 px-3 text-xs font-bold text-white"
          @click="cart.openDrawer()"
        >
          <ShoppingBag class="h-4 w-4" />
          <span>{{ cart.totalItems }}</span>
        </button>

        <button
          v-if="auth.isAuthenticated"
          type="button"
          class="inline-flex h-9 items-center gap-1.5 rounded-xl border border-line px-3 text-xs font-semibold text-ink-secondary hover:bg-surface-muted"
          @click="handleLogout"
        >
          <LogOut class="h-4 w-4" />
          Sair
        </button>
      </div>
    </div>
  </header>
</template>
