<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { Trash2 } from 'lucide-vue-next'
import Navbar from '@/components/Navbar.vue'
import { api, formatCurrency, formatPhone, getErrorMessage } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import type { Address, Favorite, Restaurant, Subscription, User } from '@/types'

const auth = useAuthStore()

const name = ref(auth.user?.name || '')
const phone = ref(auth.user?.phone || '')
const savingProfile = ref(false)

const subscriptions = ref<Subscription[]>([])
const addresses = ref<Address[]>([])
const favorites = ref<Favorite[]>([])
const restaurantsMap = ref<Record<string, string>>({})
const loading = ref(true)

const street = ref('')
const number = ref('')
const complement = ref('')
const neighborhood = ref('')
const city = ref('')
const state = ref('')
const zipCode = ref('')
const isMain = ref(false)
const savingAddress = ref(false)

const error = ref('')
const successMessage = ref('')

function onPhoneInput(event: Event) {
  const target = event.target as HTMLInputElement
  phone.value = formatPhone(target.value)
}

async function loadAccountData() {
  loading.value = true
  error.value = ''

  try {
    const [subRes, addrRes, favRes, restRes] = await Promise.allSettled([
      api.get<Subscription[]>('/assinaturas'),
      api.get<Address[]>('/usuarios/me/enderecos'),
      api.get<Favorite[]>('/usuarios/me/favoritos'),
      api.get<Restaurant[]>('/restaurantes'),
    ])

    if (subRes.status === 'fulfilled') subscriptions.value = subRes.value.data
    if (addrRes.status === 'fulfilled') addresses.value = addrRes.value.data
    if (favRes.status === 'fulfilled') favorites.value = favRes.value.data
    if (restRes.status === 'fulfilled') {
      const map: Record<string, string> = {}
      for (const r of restRes.value.data) {
        map[r.id] = r.name
      }
      restaurantsMap.value = map
    }
  } catch (err) {
    error.value = getErrorMessage(err, 'Erro ao carregar dados da conta.')
  } finally {
    loading.value = false
  }
}

async function handleSaveProfile() {
  error.value = ''
  successMessage.value = ''

  if (!name.value.trim()) {
    error.value = 'O nome é obrigatório.'
    return
  }

  savingProfile.value = true
  try {
    const { data } = await api.put<User>('/usuarios/me', {
      name: name.value.trim(),
      phone: phone.value.trim() || null,
    })
    auth.user = data
    successMessage.value = 'Dados atualizados com sucesso.'
  } catch (err) {
    error.value = getErrorMessage(err, 'Não foi possível atualizar seu perfil.')
  } finally {
    savingProfile.value = false
  }
}

async function handleSubscriptionAction(id: string, action: 'pausar' | 'reativar' | 'cancelar') {
  error.value = ''
  successMessage.value = ''

  try {
    await api.patch(`/assinaturas/${id}/${action}`)
    const { data } = await api.get<Subscription[]>('/assinaturas')
    subscriptions.value = data
    successMessage.value = 'Assinatura atualizada.'
  } catch (err) {
    error.value = getErrorMessage(err, 'Não foi possível atualizar a assinatura.')
  }
}

async function handleAddAddress() {
  error.value = ''
  successMessage.value = ''

  if (!street.value.trim() || !city.value.trim()) {
    error.value = 'Informe ao menos a rua e a cidade.'
    return
  }

  savingAddress.value = true
  try {
    await api.post('/usuarios/me/enderecos', {
      street: street.value.trim(),
      number: number.value.trim() || 'S/N',
      complement: complement.value.trim() || null,
      neighborhood: neighborhood.value.trim(),
      city: city.value.trim(),
      state: state.value.trim() || 'SP',
      zipCode: zipCode.value.trim(),
      isMain: isMain.value,
    })

    street.value = ''
    number.value = ''
    complement.value = ''
    neighborhood.value = ''
    city.value = ''
    state.value = ''
    zipCode.value = ''
    isMain.value = false

    const { data } = await api.get<Address[]>('/usuarios/me/enderecos')
    addresses.value = data
    successMessage.value = 'Endereço adicionado.'
  } catch (err) {
    error.value = getErrorMessage(err, 'Erro ao cadastrar endereço.')
  } finally {
    savingAddress.value = false
  }
}

async function handleSetMainAddress(id: string) {
  error.value = ''
  try {
    await api.patch(`/usuarios/me/enderecos/${id}/principal`)
    const { data } = await api.get<Address[]>('/usuarios/me/enderecos')
    addresses.value = data
  } catch (err) {
    error.value = getErrorMessage(err, 'Erro ao definir endereço principal.')
  }
}

async function handleDeleteAddress(id: string) {
  error.value = ''
  try {
    await api.delete(`/usuarios/me/enderecos/${id}`)
    addresses.value = addresses.value.filter((a) => a.id !== id)
  } catch (err) {
    error.value = getErrorMessage(err, 'Erro ao remover endereço.')
  }
}

async function handleRemoveFavorite(id: string) {
  error.value = ''
  try {
    await api.delete(`/usuarios/me/favoritos/${id}`)
    favorites.value = favorites.value.filter((f) => f.id !== id)
  } catch (err) {
    error.value = getErrorMessage(err, 'Erro ao remover favorito.')
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
  <div class="min-h-screen bg-stone-50">
    <Navbar />

    <main class="mx-auto max-w-5xl space-y-8 px-4 py-8 sm:px-6">
      <div>
        <h1 class="text-2xl font-bold text-stone-900">Minha Conta</h1>
        <p class="mt-1 text-sm text-stone-600">
          Gerencie seus dados pessoais, assinaturas e endereços de entrega.
        </p>
      </div>

      <div
        v-if="error"
        class="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700"
      >
        {{ error }}
      </div>

      <div
        v-if="successMessage"
        class="rounded-lg border border-green-200 bg-green-50 p-4 text-sm text-green-800"
      >
        {{ successMessage }}
      </div>

      <section class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm">
        <h2 class="text-lg font-bold text-stone-900">Dados pessoais</h2>

        <form class="mt-4 grid gap-4 sm:grid-cols-2" @submit.prevent="handleSaveProfile">
          <div>
            <label for="profile-name" class="block text-sm font-medium text-stone-700">
              Nome completo
            </label>
            <input
              id="profile-name"
              v-model="name"
              type="text"
              required
              class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
            />
          </div>

          <div>
            <label for="profile-phone" class="block text-sm font-medium text-stone-700">
              Telefone
            </label>
            <input
              id="profile-phone"
              :value="phone"
              type="tel"
              placeholder="(11) 99999-9999"
              class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
              @input="onPhoneInput"
            />
          </div>

          <div class="sm:col-span-2">
            <button
              type="submit"
              :disabled="savingProfile"
              class="rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-50"
            >
              {{ savingProfile ? 'Salvando...' : 'Salvar alterações' }}
            </button>
          </div>
        </form>
      </section>

      <section class="space-y-4">
        <h2 class="text-lg font-bold text-stone-900">Minhas assinaturas</h2>

        <p v-if="loading" class="text-sm text-stone-500">Carregando assinaturas...</p>

        <div v-else-if="subscriptions.length > 0" class="grid gap-4 sm:grid-cols-2">
          <div
            v-for="sub in subscriptions"
            :key="sub.id"
            class="flex flex-col justify-between rounded-xl border border-stone-200 bg-white p-5 shadow-sm"
          >
            <div>
              <div class="flex items-center justify-between">
                <h3 class="font-bold text-stone-900">{{ sub.planName }}</h3>
                <span
                  class="rounded-full px-2.5 py-0.5 text-xs font-semibold"
                  :class="
                    sub.status === 'Active'
                      ? 'bg-green-50 text-green-700'
                      : sub.status === 'Paused'
                        ? 'bg-amber-50 text-amber-800'
                        : 'bg-stone-100 text-stone-600'
                  "
                >
                  {{
                    sub.status === 'Active'
                      ? 'Ativa'
                      : sub.status === 'Paused'
                        ? 'Pausada'
                        : 'Cancelada'
                  }}
                </span>
              </div>

              <p class="mt-2 text-lg font-bold text-stone-900">
                {{ formatCurrency(sub.monthlyPrice) }}
                <span class="text-xs font-normal text-stone-500">/mês</span>
              </p>
              <p class="mt-1 text-xs text-stone-500">
                Próxima cobrança: {{ new Date(sub.nextBillingDate).toLocaleDateString('pt-BR') }}
              </p>
            </div>

            <div class="mt-4 flex flex-wrap gap-2 border-t border-stone-100 pt-3">
              <button
                v-if="sub.status === 'Active'"
                type="button"
                class="rounded-lg border border-stone-300 px-3 py-1.5 text-xs font-medium text-stone-700 hover:bg-stone-50"
                @click="handleSubscriptionAction(sub.id, 'pausar')"
              >
                Pausar
              </button>
              <button
                v-if="sub.status === 'Paused'"
                type="button"
                class="rounded-lg bg-green-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-green-700"
                @click="handleSubscriptionAction(sub.id, 'reativar')"
              >
                Reativar
              </button>
              <button
                v-if="sub.status !== 'Cancelled'"
                type="button"
                class="rounded-lg border border-red-200 px-3 py-1.5 text-xs font-medium text-red-600 hover:bg-red-50"
                @click="handleSubscriptionAction(sub.id, 'cancelar')"
              >
                Cancelar
              </button>
            </div>
          </div>
        </div>

        <div
          v-else
          class="rounded-xl border border-stone-200 bg-white p-6 text-center text-sm text-stone-500"
        >
          Você ainda não possui assinaturas ativas.
        </div>
      </section>

      <section class="space-y-4">
        <h2 class="text-lg font-bold text-stone-900">Endereços de entrega</h2>

        <div v-if="addresses.length > 0" class="grid gap-4 sm:grid-cols-2">
          <div
            v-for="addr in addresses"
            :key="addr.id"
            class="flex flex-col justify-between rounded-xl border border-stone-200 bg-white p-5 shadow-sm"
          >
            <div>
              <div class="flex items-center justify-between">
                <p class="font-semibold text-stone-900">
                  {{ addr.street }}, {{ addr.number }}
                </p>
                <span
                  v-if="addr.isMain"
                  class="rounded bg-orange-100 px-2 py-0.5 text-xs font-semibold text-brand-700"
                >
                  Principal
                </span>
              </div>
              <p v-if="addr.complement" class="text-xs text-stone-500">{{ addr.complement }}</p>
              <p class="mt-1 text-sm text-stone-600">
                {{ addr.neighborhood }} — {{ addr.city }}/{{ addr.state }}
              </p>
              <p class="text-xs text-stone-400">CEP: {{ addr.zipCode }}</p>
            </div>

            <div class="mt-4 flex items-center justify-between border-t border-stone-100 pt-3">
              <button
                v-if="!addr.isMain"
                type="button"
                class="text-xs font-medium text-brand-700 hover:underline"
                @click="handleSetMainAddress(addr.id)"
              >
                Definir como principal
              </button>
              <span v-else />

              <button
                type="button"
                class="text-stone-400 hover:text-red-600"
                @click="handleDeleteAddress(addr.id)"
              >
                <Trash2 class="h-4 w-4" />
              </button>
            </div>
          </div>
        </div>

        <form
          class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm"
          @submit.prevent="handleAddAddress"
        >
          <h3 class="text-sm font-bold text-stone-900">Adicionar novo endereço</h3>

          <div class="mt-4 grid gap-3 sm:grid-cols-3">
            <div class="sm:col-span-2">
              <label class="block text-xs font-medium text-stone-600">Rua / Logradouro</label>
              <input
                v-model="street"
                type="text"
                required
                class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
              />
            </div>

            <div>
              <label class="block text-xs font-medium text-stone-600">Número</label>
              <input
                v-model="number"
                type="text"
                required
                class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
              />
            </div>

            <div>
              <label class="block text-xs font-medium text-stone-600">Complemento</label>
              <input
                v-model="complement"
                type="text"
                class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
              />
            </div>

            <div>
              <label class="block text-xs font-medium text-stone-600">Bairro</label>
              <input
                v-model="neighborhood"
                type="text"
                class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
              />
            </div>

            <div>
              <label class="block text-xs font-medium text-stone-600">CEP</label>
              <input
                v-model="zipCode"
                type="text"
                placeholder="00000-000"
                class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
              />
            </div>

            <div class="sm:col-span-2">
              <label class="block text-xs font-medium text-stone-600">Cidade</label>
              <input
                v-model="city"
                type="text"
                required
                class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none"
              />
            </div>

            <div>
              <label class="block text-xs font-medium text-stone-600">UF</label>
              <input
                v-model="state"
                type="text"
                maxlength="2"
                placeholder="SP"
                class="mt-1 w-full rounded-lg border border-stone-300 px-3 py-2 text-sm uppercase focus:border-brand-600 focus:outline-none"
              />
            </div>
          </div>

          <div class="mt-4 flex items-center justify-between">
            <label class="inline-flex items-center gap-2 text-sm text-stone-700">
              <input v-model="isMain" type="checkbox" class="rounded border-stone-300" />
              Definir como endereço principal
            </label>

            <button
              type="submit"
              :disabled="savingAddress"
              class="rounded-lg bg-stone-900 px-4 py-2 text-sm font-semibold text-white hover:bg-stone-800 disabled:opacity-50"
            >
              {{ savingAddress ? 'Adicionando...' : 'Adicionar endereço' }}
            </button>
          </div>
        </form>
      </section>

      <section v-if="favorites.length > 0" class="space-y-4">
        <h2 class="text-lg font-bold text-stone-900">Restaurantes favoritos</h2>

        <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          <div
            v-for="fav in favorites"
            :key="fav.id"
            class="flex items-center justify-between rounded-xl border border-stone-200 bg-white p-4 shadow-sm"
          >
            <RouterLink
              v-if="fav.restaurantId"
              :to="`/restaurantes/${fav.restaurantId}`"
              class="font-medium text-stone-900 hover:text-brand-600"
            >
              {{ restaurantsMap[fav.restaurantId] || 'Ver restaurante' }}
            </RouterLink>
            <span v-else class="text-sm text-stone-700">Item favorito</span>

            <button
              type="button"
              class="text-stone-400 hover:text-red-600"
              @click="handleRemoveFavorite(fav.id)"
            >
              <Trash2 class="h-4 w-4" />
            </button>
          </div>
        </div>
      </section>
    </main>
  </div>
</template>

