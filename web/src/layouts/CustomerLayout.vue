<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  Bike,
  ChevronDown,
  ClipboardList,
  Home,
  LogOut,
  MapPin,
  Moon,
  Plus,
  Search,
  Shield,
  ShoppingBag,
  Store,
  Sun,
  User as UserIcon,
  Utensils,
} from 'lucide-vue-next'
import NotificationBell from '@/components/ui/NotificationBell.vue'
import CartDrawer from '@/components/customer/CartDrawer.vue'
import AppModal from '@/components/ui/AppModal.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import { formatCurrency } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import { useThemeStore } from '@/stores/theme'
import { useAddressStore } from '@/stores/address'

const props = withDefaults(
  defineProps<{
    searchQuery?: string
    showSearch?: boolean
    hideFloatingCart?: boolean
  }>(),
  {
    searchQuery: '',
    showSearch: false,
    hideFloatingCart: false,
  },
)

const emit = defineEmits<{
  (e: 'update:searchQuery', value: string): void
}>()

const auth = useAuthStore()
const cart = useCartStore()
const theme = useThemeStore()
const addressStore = useAddressStore()
const route = useRoute()
const router = useRouter()

const userMenuOpen = ref(false)
const addressModalOpen = ref(false)
const userMenuRef = ref<HTMLElement | null>(null)
const headerSearch = ref(props.searchQuery)

const userInitials = computed(() => {
  const name = auth.user?.name || 'U'
  return name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase())
    .join('')
})

const showFloatingCartBar = computed(
  () =>
    !props.hideFloatingCart &&
    !auth.isDriverOnly &&
    cart.totalItems > 0 &&
    route.path !== '/pedidos',
)

function onHeaderSearchSubmit() {
  emit('update:searchQuery', headerSearch.value)
  if (route.path !== '/') {
    void router.push({ path: '/', query: { q: headerSearch.value.trim() || undefined } })
  }
}

async function handleSelectAddress(id: string) {
  addressStore.selectAddress(id)
  addressModalOpen.value = false
}

async function handleLogout() {
  userMenuOpen.value = false
  addressStore.reset()
  await auth.logout()
  await router.push('/login')
}

function handleClickOutside(event: MouseEvent) {
  if (userMenuRef.value && !userMenuRef.value.contains(event.target as Node)) {
    userMenuOpen.value = false
  }
}

onMounted(() => {
  theme.initTheme()
  if (auth.isAuthenticated && !auth.isDriverOnly) {
    void addressStore.fetchAddresses()
  }
  document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside)
})
</script>

<template>
  <div class="min-h-screen bg-canvas text-ink pb-20 md:pb-0">
    <!-- Sticky Marketplace Header -->
    <header class="sticky top-0 z-30 border-b border-line bg-surface/90 backdrop-blur-md">
      <div class="mx-auto flex h-16 max-w-7xl items-center justify-between gap-3 px-4 sm:px-6">
        <!-- Left: Logo + Delivery Address Selector -->
        <div class="flex min-w-0 items-center gap-3 sm:gap-5">
          <RouterLink
            :to="auth.isDriverOnly ? '/entregador' : '/'"
            class="flex shrink-0 items-center gap-2.5 group"
          >
            <span
              class="flex h-9 w-9 items-center justify-center rounded-xl bg-brand-600 text-white shadow-sm shadow-brand-600/25 transition group-hover:scale-105"
            >
              <Utensils class="h-5 w-5" />
            </span>
            <span class="font-display text-lg font-extrabold tracking-tight text-ink hidden sm:inline">
              Marmitas
            </span>
          </RouterLink>

          <!-- Address Quick Selector (Customer / Authenticated) -->
          <button
            v-if="auth.isAuthenticated && !auth.isDriverOnly"
            type="button"
            class="flex max-w-[180px] sm:max-w-[240px] items-center gap-1.5 rounded-xl border border-line bg-surface-subtle px-2.5 py-1.5 text-left transition hover:border-brand-300 hover:bg-surface-muted"
            @click="addressModalOpen = true"
          >
            <MapPin class="h-4 w-4 shrink-0 text-brand-600" />
            <div class="min-w-0">
              <p class="text-[10px] font-medium leading-none text-ink-muted">Entregar em</p>
              <p class="mt-0.5 truncate text-xs font-bold text-ink">
                {{ addressStore.shortLabel }}
              </p>
            </div>
            <ChevronDown class="h-3.5 w-3.5 shrink-0 text-ink-muted" />
          </button>

          <!-- Desktop Nav Links -->
          <nav class="hidden lg:flex items-center gap-1 text-sm font-semibold">
            <RouterLink
              v-if="!auth.isDriverOnly"
              to="/"
              class="rounded-xl px-3 py-2 transition"
              :class="
                route.path === '/'
                  ? 'bg-brand-50 text-brand-700 dark:bg-brand-950/60 dark:text-brand-300'
                  : 'text-ink-secondary hover:bg-surface-muted hover:text-ink'
              "
            >
              Início
            </RouterLink>
            <RouterLink
              v-if="auth.hasRole(['customer', 'admin', 'super_admin']) && !auth.isDriverOnly"
              to="/pedidos"
              class="rounded-xl px-3 py-2 transition"
              :class="
                route.path === '/pedidos'
                  ? 'bg-brand-50 text-brand-700 dark:bg-brand-950/60 dark:text-brand-300'
                  : 'text-ink-secondary hover:bg-surface-muted hover:text-ink'
              "
            >
              Pedidos
            </RouterLink>
            <RouterLink
              v-if="auth.hasRole(['restaurant_owner', 'restaurant_staff', 'admin', 'super_admin'])"
              to="/parceiro"
              class="rounded-xl px-3 py-2 text-ink-secondary transition hover:bg-surface-muted hover:text-ink"
            >
              Restaurante
            </RouterLink>
            <RouterLink
              v-if="auth.hasRole(['driver', 'admin', 'super_admin'])"
              to="/entregador"
              class="rounded-xl px-3 py-2 text-ink-secondary transition hover:bg-surface-muted hover:text-ink"
            >
              Entregador
            </RouterLink>
            <RouterLink
              v-if="auth.hasRole(['admin', 'super_admin'])"
              to="/admin"
              class="rounded-xl px-3 py-2 text-ink-secondary transition hover:bg-surface-muted hover:text-ink"
            >
              Admin
            </RouterLink>
          </nav>
        </div>

        <!-- Center: Optional Global Search -->
        <form
          v-if="showSearch"
          class="hidden md:flex flex-1 max-w-md items-center relative"
          @submit.prevent="onHeaderSearchSubmit"
        >
          <Search class="pointer-events-none absolute left-3.5 h-4 w-4 text-ink-muted" />
          <input
            v-model="headerSearch"
            type="search"
            placeholder="Buscar restaurante, bairro ou marmita..."
            class="w-full rounded-xl border border-line bg-surface-subtle py-2 pl-10 pr-4 text-xs text-ink placeholder:text-ink-muted focus:border-brand-600 focus:bg-surface focus:outline-none focus:ring-2 focus:ring-brand-500/20"
            @input="emit('update:searchQuery', headerSearch)"
          />
        </form>

        <!-- Right: Actions (Theme, Notifications, Cart, User) -->
        <div class="flex shrink-0 items-center gap-2">
          <button
            type="button"
            :title="theme.isDark ? 'Ativar modo claro' : 'Ativar modo escuro'"
            :aria-label="theme.isDark ? 'Ativar modo claro' : 'Ativar modo escuro'"
            class="inline-flex h-10 w-10 items-center justify-center rounded-xl border border-line bg-surface text-ink-secondary transition hover:border-line-strong hover:bg-surface-muted hover:text-ink"
            @click="theme.toggleTheme()"
          >
            <Sun v-if="theme.isDark" class="h-[18px] w-[18px] text-amber-400" />
            <Moon v-else class="h-[18px] w-[18px]" />
          </button>

          <NotificationBell />

          <!-- Cart Trigger -->
          <button
            v-if="!auth.isDriverOnly"
            type="button"
            aria-label="Abrir sacola"
            class="relative inline-flex h-10 items-center gap-2 rounded-xl px-3 text-xs font-bold transition"
            :class="
              cart.totalItems > 0
                ? 'bg-brand-600 text-white shadow-sm shadow-brand-600/25 hover:bg-brand-700'
                : 'border border-line bg-surface text-ink-secondary hover:bg-surface-muted hover:text-ink'
            "
            @click="cart.openDrawer()"
          >
            <ShoppingBag class="h-4 w-4" />
            <span v-if="cart.totalItems > 0" class="hidden sm:inline tabular-nums">
              {{ formatCurrency(cart.totalAmount) }}
            </span>
            <span
              v-if="cart.totalItems > 0"
              class="flex h-5 min-w-[1.25rem] items-center justify-center rounded-full bg-white/20 px-1 text-[11px] font-extrabold tabular-nums"
            >
              {{ cart.totalItems }}
            </span>
          </button>

          <!-- Authenticated User Menu -->
          <div v-if="auth.isAuthenticated && auth.user" ref="userMenuRef" class="relative">
            <button
              type="button"
              aria-label="Menu da conta"
              class="inline-flex h-10 items-center gap-2 rounded-xl border border-line bg-surface pl-1.5 pr-2.5 transition hover:border-line-strong hover:bg-surface-muted"
              @click="userMenuOpen = !userMenuOpen"
            >
              <span
                class="flex h-7 w-7 items-center justify-center rounded-lg bg-brand-100 font-display text-xs font-bold text-brand-800 dark:bg-brand-900/60 dark:text-brand-200"
              >
                {{ userInitials }}
              </span>
              <span class="hidden max-w-[100px] truncate text-xs font-semibold text-ink sm:inline">
                {{ auth.user.name.split(' ')[0] }}
              </span>
              <ChevronDown class="h-3.5 w-3.5 text-ink-muted hidden sm:inline" />
            </button>

            <div
              v-if="userMenuOpen"
              class="absolute right-0 z-40 mt-2 w-60 overflow-hidden rounded-2xl border border-line bg-surface shadow-elevated animate-slide-up"
            >
              <div class="border-b border-line px-4 py-3">
                <p class="truncate text-sm font-bold text-ink">{{ auth.user.name }}</p>
                <p class="truncate text-xs text-ink-muted">{{ auth.user.email }}</p>
              </div>

              <div class="p-1.5 space-y-0.5 text-xs font-semibold">
                <RouterLink
                  to="/conta"
                  class="flex items-center gap-2.5 rounded-xl px-3 py-2 text-ink-secondary transition hover:bg-surface-muted hover:text-ink"
                  @click="userMenuOpen = false"
                >
                  <UserIcon class="h-4 w-4" />
                  Minha Conta
                </RouterLink>

                <RouterLink
                  v-if="!auth.isDriverOnly && auth.hasRole(['customer', 'admin', 'super_admin'])"
                  to="/pedidos"
                  class="flex items-center gap-2.5 rounded-xl px-3 py-2 text-ink-secondary transition hover:bg-surface-muted hover:text-ink"
                  @click="userMenuOpen = false"
                >
                  <ClipboardList class="h-4 w-4" />
                  Meus Pedidos
                </RouterLink>

                <RouterLink
                  v-if="auth.hasRole(['restaurant_owner', 'restaurant_staff', 'admin', 'super_admin'])"
                  to="/parceiro"
                  class="flex items-center gap-2.5 rounded-xl px-3 py-2 text-ink-secondary transition hover:bg-surface-muted hover:text-ink"
                  @click="userMenuOpen = false"
                >
                  <Store class="h-4 w-4 text-brand-600" />
                  Gestor do Restaurante
                </RouterLink>

                <RouterLink
                  v-if="auth.hasRole(['driver', 'admin', 'super_admin'])"
                  to="/entregador"
                  class="flex items-center gap-2.5 rounded-xl px-3 py-2 text-ink-secondary transition hover:bg-surface-muted hover:text-ink"
                  @click="userMenuOpen = false"
                >
                  <Bike class="h-4 w-4 text-fresh-600" />
                  Painel do Entregador
                </RouterLink>

                <RouterLink
                  v-if="auth.hasRole(['admin', 'super_admin'])"
                  to="/admin"
                  class="flex items-center gap-2.5 rounded-xl px-3 py-2 text-ink-secondary transition hover:bg-surface-muted hover:text-ink"
                  @click="userMenuOpen = false"
                >
                  <Shield class="h-4 w-4 text-sky-600" />
                  Painel Admin
                </RouterLink>

                <div class="my-1 border-t border-line" />

                <button
                  type="button"
                  class="flex w-full items-center gap-2.5 rounded-xl px-3 py-2 text-red-600 transition hover:bg-red-50 dark:text-red-400 dark:hover:bg-red-950/40"
                  @click="handleLogout"
                >
                  <LogOut class="h-4 w-4" />
                  Sair da conta
                </button>
              </div>
            </div>
          </div>

          <!-- Guest Auth Actions -->
          <template v-else>
            <RouterLink
              to="/login"
              class="rounded-xl px-3 py-2 text-xs font-semibold text-ink-secondary transition hover:bg-surface-muted hover:text-ink"
            >
              Entrar
            </RouterLink>
            <RouterLink
              to="/cadastro"
              class="hidden sm:inline-flex items-center rounded-xl bg-brand-600 px-4 py-2 text-xs font-semibold text-white shadow-sm shadow-brand-600/20 transition hover:bg-brand-700"
            >
              Criar conta
            </RouterLink>
          </template>
        </div>
      </div>
    </header>

    <!-- Main Page Content -->
    <main>
      <slot />
    </main>

    <!-- Floating Cart Bar (Mobile) -->
    <div
      v-if="showFloatingCartBar"
      class="fixed bottom-[4.5rem] left-4 right-4 z-30 md:hidden animate-slide-up"
    >
      <button
        type="button"
        class="flex w-full items-center justify-between rounded-2xl bg-brand-600 px-4 py-3.5 text-white shadow-elevated transition active:scale-[0.99]"
        @click="cart.openDrawer()"
      >
        <div class="flex items-center gap-2.5">
          <span class="flex h-7 min-w-[1.75rem] items-center justify-center rounded-lg bg-white/20 px-1.5 text-xs font-extrabold tabular-nums">
            {{ cart.totalItems }}
          </span>
          <span class="text-sm font-bold">Ver sacola</span>
        </div>
        <span class="font-display text-sm font-extrabold tabular-nums">
          {{ formatCurrency(cart.totalAmount) }}
        </span>
      </button>
    </div>

    <!-- Mobile Bottom Navigation Bar -->
    <nav
      aria-label="Navegação principal mobile"
      class="fixed bottom-0 left-0 right-0 z-30 border-t border-line bg-surface/95 backdrop-blur-md md:hidden safe-bottom"
    >
      <div v-if="auth.isDriverOnly" class="grid grid-cols-3 h-14">
        <RouterLink
          to="/entregador"
          class="flex flex-col items-center justify-center gap-0.5 text-[11px] font-semibold"
          :class="route.path === '/entregador' ? 'text-brand-600' : 'text-ink-muted'"
        >
          <Bike class="h-5 w-5" />
          Entregas
        </RouterLink>
        <RouterLink
          to="/"
          class="flex flex-col items-center justify-center gap-0.5 text-[11px] font-semibold"
          :class="route.path === '/' ? 'text-brand-600' : 'text-ink-muted'"
        >
          <Store class="h-5 w-5" />
          Cardápios
        </RouterLink>
        <RouterLink
          to="/conta"
          class="flex flex-col items-center justify-center gap-0.5 text-[11px] font-semibold"
          :class="route.path === '/conta' ? 'text-brand-600' : 'text-ink-muted'"
        >
          <UserIcon class="h-5 w-5" />
          Conta
        </RouterLink>
      </div>

      <div v-else class="grid grid-cols-4 h-14">
        <RouterLink
          to="/"
          class="flex flex-col items-center justify-center gap-0.5 text-[11px] font-semibold"
          :class="route.path === '/' ? 'text-brand-600' : 'text-ink-muted'"
        >
          <Home class="h-5 w-5" />
          Início
        </RouterLink>
        <RouterLink
          to="/pedidos"
          class="flex flex-col items-center justify-center gap-0.5 text-[11px] font-semibold"
          :class="route.path === '/pedidos' ? 'text-brand-600' : 'text-ink-muted'"
        >
          <ClipboardList class="h-5 w-5" />
          Pedidos
        </RouterLink>
        <button
          type="button"
          class="relative flex flex-col items-center justify-center gap-0.5 text-[11px] font-semibold text-ink-muted"
          @click="cart.openDrawer()"
        >
          <div class="relative">
            <ShoppingBag class="h-5 w-5" :class="cart.totalItems > 0 ? 'text-brand-600' : ''" />
            <span
              v-if="cart.totalItems > 0"
              class="absolute -right-2 -top-1 flex h-4 min-w-[1rem] items-center justify-center rounded-full bg-brand-600 px-1 text-[9px] font-bold text-white"
            >
              {{ cart.totalItems }}
            </span>
          </div>
          <span :class="cart.totalItems > 0 ? 'text-brand-600' : ''">Sacola</span>
        </button>
        <RouterLink
          :to="auth.isAuthenticated ? '/conta' : '/login'"
          class="flex flex-col items-center justify-center gap-0.5 text-[11px] font-semibold"
          :class="route.path === '/conta' ? 'text-brand-600' : 'text-ink-muted'"
        >
          <UserIcon class="h-5 w-5" />
          {{ auth.isAuthenticated ? 'Conta' : 'Entrar' }}
        </RouterLink>
      </div>
    </nav>

    <!-- Quick Delivery Address Selector Modal -->
    <AppModal
      :open="addressModalOpen"
      title="Onde vamos entregar?"
      subtitle="Selecione seu endereço de entrega ou cadastre um novo."
      max-width="md"
      @close="addressModalOpen = false"
    >
      <div v-if="addressStore.addresses.length === 0" class="py-6 text-center space-y-3">
        <MapPin class="mx-auto h-10 w-10 text-ink-muted" />
        <p class="text-sm text-ink-secondary">Você ainda não possui endereços cadastrados.</p>
        <AppButton
          variant="primary"
          size="sm"
          @click="addressModalOpen = false; router.push({ path: '/conta', query: { tab: 'enderecos' } })"
        >
          <Plus class="h-4 w-4" />
          Cadastrar primeiro endereço
        </AppButton>
      </div>

      <div v-else class="space-y-2.5">
        <button
          v-for="addr in addressStore.addresses"
          :key="addr.id"
          type="button"
          class="flex w-full items-start justify-between gap-3 rounded-2xl border p-3.5 text-left transition"
          :class="
            addressStore.selectedAddress?.id === addr.id
              ? 'border-brand-600 bg-brand-50/50 dark:bg-brand-950/30'
              : 'border-line bg-surface hover:border-line-strong hover:bg-surface-subtle'
          "
          @click="handleSelectAddress(addr.id)"
        >
          <div class="flex items-start gap-2.5">
            <MapPin
              class="mt-0.5 h-4 w-4 shrink-0"
              :class="addressStore.selectedAddress?.id === addr.id ? 'text-brand-600' : 'text-ink-muted'"
            />
            <div>
              <p class="text-sm font-bold text-ink">
                {{ addr.street }}, {{ addr.number }}
              </p>
              <p class="text-xs text-ink-secondary">
                {{ addr.neighborhood }} — {{ addr.city }}/{{ addr.state }}
              </p>
              <p v-if="addr.complement" class="text-[11px] text-ink-muted">{{ addr.complement }}</p>
            </div>
          </div>
          <AppBadge v-if="addr.isMain" variant="brand">Principal</AppBadge>
        </button>
      </div>

      <template #footer>
        <div class="flex items-center justify-between">
          <RouterLink
            to="/conta?tab=enderecos"
            class="inline-flex items-center gap-1.5 text-xs font-semibold text-brand-600 hover:underline dark:text-brand-400"
            @click="addressModalOpen = false"
          >
            <Plus class="h-3.5 w-3.5" />
            Gerenciar ou adicionar endereço
          </RouterLink>
          <AppButton variant="outline" size="xs" @click="addressModalOpen = false">
            Fechar
          </AppButton>
        </div>
      </template>
    </AppModal>

    <!-- Global Cart Drawer -->
    <CartDrawer />
  </div>
</template>
