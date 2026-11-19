<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { Bell, CheckCheck, LogOut, Menu, Moon, ShoppingCart, Sun, Utensils, X } from 'lucide-vue-next'
import { api } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import { useThemeStore } from '@/stores/theme'
import type { NotificationItem } from '@/types'

const auth = useAuthStore()
const cart = useCartStore()
const theme = useThemeStore()
const router = useRouter()

const mobileMenuOpen = ref(false)
const notificationsOpen = ref(false)
const notifications = ref<NotificationItem[]>([])

const isDriverOnly = computed(
  () =>
    auth.hasRole(['driver']) &&
    !auth.hasRole(['customer', 'restaurant_owner', 'admin', 'super_admin']),
)

const unreadCount = computed(() => notifications.value.filter((n) => !n.isRead).length)

async function loadNotifications() {
  if (!auth.isAuthenticated) return
  try {
    const { data } = await api.get<NotificationItem[]>('/notificacoes')
    notifications.value = data
  } catch {
    // Silencioso caso sessão ainda esteja inicializando
  }
}

async function toggleNotifications() {
  notificationsOpen.value = !notificationsOpen.value
  if (notificationsOpen.value) {
    await loadNotifications()
  }
}

async function markAsRead(item: NotificationItem) {
  if (item.isRead) return
  try {
    await api.patch(`/notificacoes/${item.id}/lida`)
    item.isRead = true
  } catch {
    // Ignora falha pontual
  }
}

async function markAllAsRead() {
  try {
    await api.patch('/notificacoes/marcar-todas-lidas')
    notifications.value.forEach((n) => {
      n.isRead = true
    })
  } catch {
    // Ignora falha pontual
  }
}

onMounted(() => {
  theme.initTheme()
  if (auth.isAuthenticated) {
    void loadNotifications()
  }
})

async function handleLogout() {
  mobileMenuOpen.value = false
  notificationsOpen.value = false
  await auth.logout()
  await router.push('/login')
}
</script>

<template>
  <header class="sticky top-0 z-30 border-b border-stone-200 bg-white">
    <div class="mx-auto flex h-16 max-w-6xl items-center justify-between px-4 sm:px-6">
      <div class="flex items-center gap-6">
        <RouterLink
          :to="isDriverOnly ? '/entregador' : '/'"
          class="flex items-center gap-2 font-bold text-stone-900"
          @click="mobileMenuOpen = false"
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

      <div class="flex items-center gap-2.5">
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

        <!-- Dropdown de Notificações -->
        <div v-if="auth.isAuthenticated" class="relative">
          <button
            type="button"
            aria-label="Notificações"
            class="relative inline-flex items-center justify-center rounded-lg border border-stone-200 bg-stone-50 p-2 text-stone-700 hover:bg-stone-100"
            @click="toggleNotifications"
          >
            <Bell class="h-4 w-4" />
            <span
              v-if="unreadCount > 0"
              class="absolute -right-1 -top-1 flex h-4 min-w-[1rem] items-center justify-center rounded-full bg-brand-600 px-1 text-[10px] font-bold text-white"
            >
              {{ unreadCount }}
            </span>
          </button>

          <div
            v-if="notificationsOpen"
            class="absolute right-0 mt-2 w-80 rounded-xl border border-stone-200 bg-white p-3 shadow-lg"
          >
            <div class="flex items-center justify-between border-b border-stone-100 pb-2">
              <span class="text-xs font-bold text-stone-900">Notificações</span>
              <button
                v-if="unreadCount > 0"
                type="button"
                class="inline-flex items-center gap-1 text-[11px] font-medium text-brand-600 hover:underline"
                @click="markAllAsRead"
              >
                <CheckCheck class="h-3.5 w-3.5" />
                Marcar todas lidas
              </button>
            </div>

            <div
              v-if="notifications.length === 0"
              class="py-6 text-center text-xs text-stone-500"
            >
              Nenhuma notificação no momento.
            </div>

            <ul v-else class="mt-2 max-h-64 divide-y divide-stone-100 overflow-y-auto">
              <li
                v-for="item in notifications.slice(0, 10)"
                :key="item.id"
                class="cursor-pointer py-2 text-xs transition hover:bg-stone-50"
                :class="{ 'opacity-60': item.isRead }"
                @click="markAsRead(item)"
              >
                <div class="flex items-center justify-between">
                  <span class="font-semibold text-stone-900">{{ item.title }}</span>
                  <span
                    v-if="!item.isRead"
                    class="h-2 w-2 rounded-full bg-brand-600"
                  />
                </div>
                <p class="mt-0.5 text-stone-600">{{ item.message }}</p>
              </li>
            </ul>
          </div>
        </div>

        <RouterLink
          v-if="cart.totalItems > 0 && !isDriverOnly"
          to="/pedidos"
          aria-label="Ver carrinho"
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
            class="hidden items-center gap-1.5 rounded-lg border border-stone-300 px-3 py-1.5 text-sm font-medium text-stone-700 hover:bg-stone-100 md:inline-flex"
            @click="handleLogout"
          >
            <LogOut class="h-4 w-4" />
            Sair
          </button>
        </template>

        <template v-else>
          <RouterLink
            to="/login"
            class="hidden rounded-lg px-3 py-2 text-sm font-medium text-stone-700 hover:bg-stone-100 md:inline-block"
          >
            Entrar
          </RouterLink>
          <RouterLink
            to="/cadastro"
            class="hidden rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-700 md:inline-block"
          >
            Cadastrar
          </RouterLink>
        </template>

        <!-- Botão Hambúrguer Mobile -->
        <button
          type="button"
          aria-label="Abrir menu de navegação"
          class="inline-flex items-center justify-center rounded-lg border border-stone-200 p-2 text-stone-700 hover:bg-stone-100 md:hidden"
          @click="mobileMenuOpen = !mobileMenuOpen"
        >
          <X v-if="mobileMenuOpen" class="h-5 w-5" />
          <Menu v-else class="h-5 w-5" />
        </button>
      </div>
    </div>

    <!-- Drawer Mobile -->
    <div v-if="mobileMenuOpen" class="border-t border-stone-200 bg-white px-4 py-3 md:hidden">
      <nav class="flex flex-col space-y-2 text-sm font-medium text-stone-700">
        <RouterLink
          v-if="!isDriverOnly"
          to="/"
          class="rounded-lg px-3 py-2 hover:bg-stone-50"
          @click="mobileMenuOpen = false"
        >
          Início
        </RouterLink>
        <RouterLink
          v-if="auth.hasRole(['customer', 'admin', 'super_admin']) && !isDriverOnly"
          to="/pedidos"
          class="rounded-lg px-3 py-2 hover:bg-stone-50"
          @click="mobileMenuOpen = false"
        >
          Pedidos
        </RouterLink>
        <RouterLink
          v-if="auth.hasRole(['driver', 'admin', 'super_admin'])"
          to="/entregador"
          class="rounded-lg px-3 py-2 hover:bg-stone-50"
          @click="mobileMenuOpen = false"
        >
          Entregas
        </RouterLink>
        <RouterLink
          v-if="auth.isAuthenticated"
          to="/conta"
          class="rounded-lg px-3 py-2 hover:bg-stone-50"
          @click="mobileMenuOpen = false"
        >
          Minha Conta
        </RouterLink>
        <RouterLink
          v-if="auth.hasRole(['restaurant_owner', 'restaurant_staff', 'admin', 'super_admin'])"
          to="/parceiro"
          class="rounded-lg px-3 py-2 hover:bg-stone-50"
          @click="mobileMenuOpen = false"
        >
          Restaurante
        </RouterLink>
        <RouterLink
          v-if="auth.hasRole(['admin', 'super_admin'])"
          to="/admin"
          class="rounded-lg px-3 py-2 hover:bg-stone-50"
          @click="mobileMenuOpen = false"
        >
          Admin
        </RouterLink>

        <div class="border-t border-stone-100 pt-2">
          <template v-if="auth.isAuthenticated && auth.user">
            <div class="px-3 py-1">
              <p class="text-xs font-semibold text-stone-800">{{ auth.user.name }}</p>
              <p class="text-[11px] text-stone-500">{{ auth.user.email }}</p>
            </div>
            <button
              type="button"
              class="mt-1 inline-flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm font-medium text-red-600 hover:bg-red-50"
              @click="handleLogout"
            >
              <LogOut class="h-4 w-4" />
              Sair da conta
            </button>
          </template>
          <template v-else>
            <div class="flex gap-2 pt-1">
              <RouterLink
                to="/login"
                class="flex-1 rounded-lg border border-stone-300 px-3 py-2 text-center text-sm font-medium text-stone-700"
                @click="mobileMenuOpen = false"
              >
                Entrar
              </RouterLink>
              <RouterLink
                to="/cadastro"
                class="flex-1 rounded-lg bg-brand-600 px-3 py-2 text-center text-sm font-semibold text-white"
                @click="mobileMenuOpen = false"
              >
                Cadastrar
              </RouterLink>
            </div>
          </template>
        </div>
      </nav>
    </div>
  </header>
</template>
