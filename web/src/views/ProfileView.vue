<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import {
  Bell,
  Heart,
  MapPin,
  MessageSquare,
  Shield,
  Sparkles,
  User as UserIcon,
} from 'lucide-vue-next'
import CustomerLayout from '@/layouts/CustomerLayout.vue'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppInput from '@/components/ui/AppInput.vue'
import AppToggle from '@/components/ui/AppToggle.vue'
import AppSkeleton from '@/components/ui/AppSkeleton.vue'
import ProfileSubscriptionsTab from '@/components/customer/ProfileSubscriptionsTab.vue'
import ProfileAddressesTab from '@/components/customer/ProfileAddressesTab.vue'
import ProfileFavoritesTab from '@/components/customer/ProfileFavoritesTab.vue'
import ProfileSecurityLgpdTab from '@/components/customer/ProfileSecurityLgpdTab.vue'
import ProfileReviewsTab from '@/components/customer/ProfileReviewsTab.vue'
import CourierAccountPanel from '@/components/customer/CourierAccountPanel.vue'
import { api, formatPhone, getErrorMessage } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'
import type {
  Address,
  CourierEarnings,
  Delivery,
  Favorite,
  NotificationPreferences,
  Plan,
  Restaurant,
  ReviewItem,
  Subscription,
  SubscriptionDelivery,
  User,
} from '@/types'

type CustomerSection = 'perfil' | 'assinaturas' | 'enderecos' | 'favoritos' | 'seguranca' | 'avaliacoes'

const auth = useAuthStore()
const toast = useToastStore()
const route = useRoute()

const validTabs: CustomerSection[] = [
  'perfil',
  'assinaturas',
  'enderecos',
  'favoritos',
  'seguranca',
  'avaliacoes',
]

function resolveInitialTab(): CustomerSection {
  const q = typeof route.query.tab === 'string' ? route.query.tab : ''
  return validTabs.includes(q as CustomerSection) ? (q as CustomerSection) : 'perfil'
}

const activeSection = ref<CustomerSection>(resolveInitialTab())

watch(
  () => route.query.tab,
  (val) => {
    if (typeof val === 'string' && validTabs.includes(val as CustomerSection)) {
      activeSection.value = val as CustomerSection
    }
  },
)

const canViewCourierReviews = computed(() =>
  auth.hasRole(['driver', 'admin', 'super_admin']),
)

const userInitials = computed(() => {
  const n = auth.user?.name || 'U'
  return n
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase())
    .join('')
})

const name = ref(auth.user?.name || '')
const phone = ref(auth.user?.phone || '')
const savingProfile = ref(false)

const notificationPrefs = ref<NotificationPreferences>({
  emailEnabled: true,
  pushEnabled: true,
  smsEnabled: false,
})
const savingPrefs = ref(false)

const subscriptions = ref<Subscription[]>([])
const availablePlans = ref<Plan[]>([])
const scheduledBySub = ref<Record<string, SubscriptionDelivery[]>>({})
const addresses = ref<Address[]>([])
const favorites = ref<Favorite[]>([])
const restaurants = ref<Restaurant[]>([])
const customerReviews = ref<ReviewItem[]>([])
const courierReviews = ref<ReviewItem[]>([])
const courierEarnings = ref<CourierEarnings | null>(null)
const myDeliveries = ref<Delivery[]>([])
const loading = ref(true)

const customerNavItems: Array<{ id: CustomerSection; label: string; icon: typeof UserIcon }> = [
  { id: 'perfil', label: 'Perfil & Notificações', icon: UserIcon },
  { id: 'assinaturas', label: 'Assinaturas', icon: Sparkles },
  { id: 'enderecos', label: 'Endereços', icon: MapPin },
  { id: 'favoritos', label: 'Favoritos', icon: Heart },
  { id: 'seguranca', label: 'Segurança & LGPD', icon: Shield },
  { id: 'avaliacoes', label: 'Mural de Avaliações', icon: MessageSquare },
]

function onPhoneInput(event: Event) {
  const target = event.target as HTMLInputElement
  phone.value = formatPhone(target.value)
}

async function loadSubscriptionSchedules(subscriptionId: string) {
  try {
    const { data } = await api.get<SubscriptionDelivery[]>(
      `/assinaturas/${subscriptionId}/agendamentos`,
    )
    scheduledBySub.value[subscriptionId] = data
  } catch {
    scheduledBySub.value[subscriptionId] = []
  }
}

async function loadAccountData() {
  loading.value = true
  try {
    const [restRes, custRevRes, prefsRes] = await Promise.allSettled([
      api.get<Restaurant[]>('/restaurantes'),
      api.get<ReviewItem[]>('/avaliacoes/clientes'),
      api.get<NotificationPreferences>('/usuarios/me/preferencias-notificacao'),
    ])

    if (prefsRes.status === 'fulfilled' && prefsRes.value.data) {
      notificationPrefs.value = prefsRes.value.data
    }
    if (restRes.status === 'fulfilled') restaurants.value = restRes.value.data
    if (custRevRes.status === 'fulfilled') customerReviews.value = custRevRes.value.data

    if (canViewCourierReviews.value) {
      const [earnRes, delivRes, courRevRes] = await Promise.allSettled([
        api.get<CourierEarnings>('/entregadores/me/ganhos'),
        api.get<Delivery[]>('/entregas'),
        api.get<ReviewItem[]>('/avaliacoes/entregadores'),
      ])
      if (earnRes.status === 'fulfilled') courierEarnings.value = earnRes.value.data
      if (delivRes.status === 'fulfilled') myDeliveries.value = delivRes.value.data
      if (courRevRes.status === 'fulfilled') courierReviews.value = courRevRes.value.data
    }

    if (!auth.isDriverOnly) {
      const [subRes, addrRes, favRes, planRes] = await Promise.allSettled([
        api.get<Subscription[]>('/assinaturas'),
        api.get<Address[]>('/usuarios/me/enderecos'),
        api.get<Favorite[]>('/usuarios/me/favoritos'),
        api.get<Plan[]>('/planos'),
      ])
      if (planRes.status === 'fulfilled') availablePlans.value = planRes.value.data
      if (subRes.status === 'fulfilled') {
        subscriptions.value = subRes.value.data
        for (const sub of subRes.value.data) {
          void loadSubscriptionSchedules(sub.id)
        }
      }
      if (addrRes.status === 'fulfilled') addresses.value = addrRes.value.data
      if (favRes.status === 'fulfilled') favorites.value = favRes.value.data
    }
  } catch (err) {
    toast.error(getErrorMessage(err, 'Erro ao carregar dados da conta.'))
  } finally {
    loading.value = false
  }
}

async function handleSaveProfile() {
  if (!name.value.trim()) {
    toast.warning('O nome completo é obrigatório.')
    return
  }

  savingProfile.value = true
  try {
    const { data } = await api.put<User>('/usuarios/me', {
      name: name.value.trim(),
      phone: phone.value.trim() || null,
    })
    auth.user = data
    toast.success('Dados pessoais atualizados com sucesso!')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível atualizar seu perfil.'))
  } finally {
    savingProfile.value = false
  }
}

async function handleSavePrefs() {
  savingPrefs.value = true
  try {
    const { data } = await api.put<NotificationPreferences>(
      '/usuarios/me/preferencias-notificacao',
      notificationPrefs.value,
    )
    notificationPrefs.value = data
    toast.success('Preferências de notificação salvas!')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível salvar as preferências.'))
  } finally {
    savingPrefs.value = false
  }
}

onMounted(() => {
  if (auth.user) {
    name.value = auth.user.name
    phone.value = auth.user.phone || ''
  }
  void loadAccountData()
})
</script>

<template>
  <CustomerLayout>
    <div class="mx-auto max-w-6xl space-y-6 px-4 py-6 sm:px-6 sm:py-8">
      <!-- Account Header Card -->
      <div class="flex flex-col justify-between gap-4 rounded-3xl border border-line bg-surface p-5 shadow-card sm:flex-row sm:items-center sm:p-6">
        <div class="flex items-center gap-4 min-w-0">
          <div class="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl bg-gradient-to-br from-brand-600 to-amber-500 font-display text-lg font-extrabold text-white shadow-sm">
            {{ userInitials }}
          </div>
          <div class="min-w-0">
            <h1 class="font-display text-xl font-extrabold text-ink truncate sm:text-2xl">
              {{ auth.isDriverOnly ? 'Central do Entregador' : auth.user?.name || 'Minha Conta' }}
            </h1>
            <p class="truncate text-xs text-ink-muted sm:text-sm">
              {{ auth.user?.email }}
            </p>
          </div>
        </div>
      </div>

      <!-- DRIVER-ONLY VIEW -->
      <template v-if="auth.isDriverOnly">
        <!-- Quick Profile Card for Driver -->
        <AppCard padding="lg">
          <h2 class="font-display text-base font-extrabold text-ink mb-4">Dados do entregador</h2>
          <form class="grid gap-4 sm:grid-cols-3 items-end" @submit.prevent="handleSaveProfile">
            <AppInput v-model="name" label="Nome completo" required />
            <AppInput
              :model-value="phone"
              label="Telefone"
              type="tel"
              placeholder="(47) 99999-9999"
              @input="onPhoneInput"
            />
            <AppButton type="submit" variant="primary" size="md" :loading="savingProfile">
              Salvar dados
            </AppButton>
          </form>
        </AppCard>

        <CourierAccountPanel
          :earnings="courierEarnings"
          :deliveries="myDeliveries"
          :restaurants="restaurants"
          :courier-reviews="courierReviews"
          :customer-reviews="customerReviews"
          @courier-review-added="(rev) => courierReviews.unshift(rev)"
        />
      </template>

      <!-- CUSTOMER / STANDARD VIEW (Clean Side Navigation + Active Panel) -->
      <div v-else class="grid grid-cols-1 gap-6 lg:grid-cols-12 items-start">
        <!-- Sidebar Navigation (Horizontal Scroll on Mobile, Vertical Sticky on Desktop) -->
        <aside class="min-w-0 w-full lg:col-span-3 lg:sticky lg:top-24">
          <nav
            aria-label="Seções da conta"
            class="flex max-w-full gap-1.5 overflow-x-auto no-scrollbar pb-1 lg:flex-col lg:rounded-2xl lg:border lg:border-line lg:bg-surface lg:p-2 lg:shadow-card"
          >
            <button
              v-for="item in customerNavItems"
              :key="item.id"
              type="button"
              class="flex shrink-0 items-center gap-2.5 rounded-xl px-3.5 py-2.5 text-xs font-bold transition lg:w-full lg:text-sm"
              :class="
                activeSection === item.id
                  ? 'bg-brand-600 text-white shadow-sm'
                  : 'border border-line bg-surface text-ink-secondary hover:bg-surface-muted hover:text-ink lg:border-0 lg:bg-transparent'
              "
              @click="activeSection = item.id"
            >
              <component :is="item.icon" class="h-4 w-4 shrink-0" />
              <span>{{ item.label }}</span>
            </button>
          </nav>
        </aside>

        <!-- Active Tab Content -->
        <div class="min-w-0 w-full lg:col-span-9 space-y-6">
          <div v-if="loading" class="space-y-4">
            <AppSkeleton variant="card" height="220px" />
            <AppSkeleton variant="card" height="160px" />
          </div>

          <!-- Section 1: Profile & Notification Preferences -->
          <template v-else-if="activeSection === 'perfil'">
            <AppCard padding="lg" class="space-y-5">
              <div>
                <h2 class="font-display text-lg font-extrabold text-ink">Dados pessoais</h2>
                <p class="text-xs text-ink-muted">Mantenha seu nome e telefone atualizados para contato na entrega.</p>
              </div>

              <form class="grid gap-4 sm:grid-cols-2" @submit.prevent="handleSaveProfile">
                <AppInput v-model="name" label="Nome completo" required />
                <AppInput
                  :model-value="phone"
                  label="Telefone / WhatsApp"
                  type="tel"
                  placeholder="(47) 99999-9999"
                  @input="onPhoneInput"
                />
                <div class="sm:col-span-2 pt-1">
                  <AppButton type="submit" variant="primary" size="md" :loading="savingProfile">
                    Salvar alterações
                  </AppButton>
                </div>
              </form>
            </AppCard>

            <AppCard padding="lg" class="space-y-5">
              <div class="flex items-center gap-2">
                <Bell class="h-5 w-5 text-brand-600" />
                <div>
                  <h2 class="font-display text-lg font-extrabold text-ink">Preferências de notificação</h2>
                  <p class="text-xs text-ink-muted">Escolha como deseja receber avisos sobre pedidos e assinaturas.</p>
                </div>
              </div>

              <form class="space-y-4" @submit.prevent="handleSavePrefs">
                <div class="space-y-4 rounded-2xl border border-line bg-surface-subtle p-4">
                  <AppToggle
                    v-model="notificationPrefs.emailEnabled"
                    label="Notificações por E-mail"
                    description="Recibos, renovação de planos e resumo de pedidos."
                  />
                  <div class="border-t border-line" />
                  <AppToggle
                    v-model="notificationPrefs.pushEnabled"
                    label="Notificações Push"
                    description="Alertas instantâneos quando o entregador sair para entrega."
                  />
                  <div class="border-t border-line" />
                  <AppToggle
                    v-model="notificationPrefs.smsEnabled"
                    label="Alertas por SMS"
                    description="Avisos críticos caso esteja sem internet no celular."
                  />
                </div>

                <AppButton type="submit" variant="secondary" size="sm" :loading="savingPrefs">
                  Salvar preferências
                </AppButton>
              </form>
            </AppCard>
          </template>

          <!-- Section 2: Subscriptions -->
          <ProfileSubscriptionsTab
            v-else-if="activeSection === 'assinaturas'"
            :subscriptions="subscriptions"
            :available-plans="availablePlans"
            :restaurants="restaurants"
            :scheduled-by-sub="scheduledBySub"
            @refresh="loadAccountData"
            @refresh-schedules="loadSubscriptionSchedules"
          />

          <!-- Section 3: Addresses -->
          <ProfileAddressesTab
            v-else-if="activeSection === 'enderecos'"
            :addresses="addresses"
            @refresh="loadAccountData"
          />

          <!-- Section 4: Favorites -->
          <ProfileFavoritesTab
            v-else-if="activeSection === 'favoritos'"
            :favorites="favorites"
            :restaurants="restaurants"
            @removed="(id) => (favorites = favorites.filter((f) => f.id !== id))"
          />

          <!-- Section 5: Security (MFA) & LGPD -->
          <ProfileSecurityLgpdTab v-else-if="activeSection === 'seguranca'" />

          <!-- Section 6: Public Reviews Wall -->
          <ProfileReviewsTab
            v-else-if="activeSection === 'avaliacoes'"
            :restaurants="restaurants"
            :customer-reviews="customerReviews"
            @published="(rev) => customerReviews.unshift(rev)"
          />
        </div>
      </div>
    </div>
  </CustomerLayout>
</template>
