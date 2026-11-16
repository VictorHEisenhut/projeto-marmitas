<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { LogOut, Moon, ShoppingCart, Sun, Utensils } from 'lucide-vue-next'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import { useThemeStore } from '@/stores/theme'

const auth = useAuthStore()
const cart = useCartStore()
const theme = useThemeStore()
const router = useRouter()

const isDriverOnly = computed(
  () =>
    auth.hasRole(['driver']) &&
    !auth.hasRole(['customer', 'restaurant_owner', 'admin', 'super_admin']),
)

onMounted(() => {
  theme.initTheme()
})

async function handleLogout() {
  await auth.logout()
  await router.push('/login')
}
</script>

<template>
  <header class="border-b border-stone-200 bg-white">
    <div class="mx-auto flex h-16 max-w-6xl items-center justify-between px-4 sm:px-6">
      <div class="flex items-center gap-6">
        <RouterLink
          :to="isDriverOnly ? '/entregador' : '/'"
          class="flex items-center gap-2 font-bold text-stone-900"
        >
          <span class="flex h-9 w-9 items-center justify-center rounded-lg bg-brand-600 text-white">
            <Utensils class="h-5 w-5" />
          </span>
          <span class="text-lg">Marmitas</span>
        </RouterLink>

        <nav class="hidden items-center gap-4 text-sm font-medium text-stone-600 md:flex">
          <RouterLink v-if="!isDriverOnly" to="/" class="hover:text-brand-600">
            Início
          </RouterLink>
          <RouterLink
            v-if="auth.hasRole(['customer', 'admin', 'super_admin']) && !isDriverOnly"
            to="/pedidos"
            class="hover:text-brand-600"
          >
            Pedidos
          </RouterLink>
          <RouterLink
            v-if="auth.hasRole(['driver', 'admin', 'super_admin'])"
            to="/entregador"
            class="hover:text-brand-600"
          >
            Entregas
          </RouterLink>
          <RouterLink
            v-if="auth.isAuthenticated"
            to="/conta"
            class="hover:text-brand-600"
          >
            Minha Conta
          </RouterLink>
          <RouterLink
            v-if="auth.hasRole(['restaurant_owner', 'restaurant_staff', 'admin', 'super_admin'])"
            to="/parceiro"
            class="hover:text-brand-600"
          >
            Restaurante
          </RouterLink>
          <RouterLink
            v-if="auth.hasRole(['admin', 'super_admin'])"
            to="/admin"
            class="hover:text-brand-600"
          >
            Admin
          </RouterLink>
        </nav>
      </div>

      <div class="flex items-center gap-3">
        <button
          type="button"
          :title="theme.isDark ? 'Ativar modo claro' : 'Ativar modo noturno'"
          :aria-label="theme.isDark ? 'Ativar modo claro' : 'Ativar modo noturno'"
          class="inline-flex items-center gap-1.5 rounded-lg border border-stone-200 bg-stone-50 px-2.5 py-1.5 text-xs font-medium text-stone-700 transition hover:bg-stone-100"
          @click="theme.toggleTheme()"
        >
          <Sun v-if="theme.isDark" class="h-4 w-4 text-amber-400" />
          <Moon v-else class="h-4 w-4 text-stone-600" />
          <span class="hidden sm:inline">{{ theme.isDark ? 'Modo Claro' : 'Modo Noturno' }}</span>
        </button>

        <RouterLink
          v-if="cart.totalItems > 0 && !isDriverOnly"
          to="/pedidos"
          class="inline-flex items-center gap-1.5 rounded-lg bg-orange-50 px-3 py-1.5 text-sm font-semibold text-brand-700 hover:bg-orange-100"
        >
          <ShoppingCart class="h-4 w-4" />
          <span>{{ cart.totalItems }}</span>
        </RouterLink>

        <template v-if="auth.isAuthenticated && auth.user">
          <div class="hidden text-right sm:block">
            <p class="text-sm font-semibold text-stone-800">{{ auth.user.name }}</p>
            <p class="text-xs text-stone-500">{{ auth.user.email }}</p>
          </div>

          <button
            type="button"
            class="inline-flex items-center gap-1.5 rounded-lg border border-stone-300 px-3 py-1.5 text-sm font-medium text-stone-700 hover:bg-stone-100"
            @click="handleLogout"
          >
            <LogOut class="h-4 w-4" />
            Sair
          </button>
        </template>

        <template v-else>
          <RouterLink
            to="/login"
            class="rounded-lg px-3 py-2 text-sm font-medium text-stone-700 hover:bg-stone-100"
          >
            Entrar
          </RouterLink>
          <RouterLink
            to="/cadastro"
            class="rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-700"
          >
            Cadastrar
          </RouterLink>
        </template>
      </div>
    </div>
  </header>
</template>
