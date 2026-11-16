<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { Eye, EyeOff, Utensils } from 'lucide-vue-next'
import { api, formatPhone, getErrorMessage } from '@/services/api'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const auth = useAuthStore()

const name = ref('')
const email = ref('')
const phone = ref('')
const accountRole = ref<'customer' | 'restaurant_owner' | 'driver'>('customer')
const password = ref('')
const confirmPassword = ref('')
const showPassword = ref(false)
const loading = ref(false)
const error = ref('')

function onPhoneInput(event: Event) {
  const target = event.target as HTMLInputElement
  phone.value = formatPhone(target.value)
}

async function handleSubmit() {
  error.value = ''

  if (!name.value.trim()) {
    error.value = 'O nome é obrigatório.'
    return
  }

  if (name.value.trim().length > 150) {
    error.value = 'O nome deve ter no máximo 150 caracteres.'
    return
  }

  if (!email.value.trim()) {
    error.value = 'O e-mail é obrigatório.'
    return
  }

  if (password.value.length < 8) {
    error.value = 'A senha deve ter pelo menos 8 caracteres.'
    return
  }

  if (password.value !== confirmPassword.value) {
    error.value = 'As senhas não conferem.'
    return
  }

  loading.value = true
  try {
    await auth.register({
      name: name.value,
      email: email.value,
      phone: phone.value || null,
      password: password.value,
      role: accountRole.value,
    })

    if (accountRole.value === 'driver') {
      try {
        await api.post('/entregadores/registro', {
          name: name.value.trim(),
          document: '',
          phone: phone.value.trim(),
        })
      } catch {
        // Ignora caso já registrado
      }
    }

    await router.push({
      path: '/login',
      query: { registered: 'true', email: email.value.trim().toLowerCase() },
    })
  } catch (err) {
    error.value = getErrorMessage(err, 'Não foi possível concluir o cadastro.')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="flex min-h-screen flex-col justify-center bg-stone-50 px-4 py-12 sm:px-6 lg:px-8">
    <div class="sm:mx-auto sm:w-full sm:max-w-md">
      <RouterLink to="/" class="flex items-center justify-center gap-2 text-stone-900">
        <span class="flex h-10 w-10 items-center justify-center rounded-lg bg-brand-600 text-white">
          <Utensils class="h-5 w-5" />
        </span>
        <span class="text-xl font-bold">Marmitas</span>
      </RouterLink>

      <h1 class="mt-6 text-center text-2xl font-bold text-stone-900">
        Criar nova conta
      </h1>
      <p class="mt-1 text-center text-sm text-stone-600">
        Já tem cadastro?
        <RouterLink to="/login" class="font-semibold text-brand-600 hover:underline">
          Fazer login
        </RouterLink>
      </p>
    </div>

    <div class="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
      <div class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm sm:p-8">
        <div
          v-if="error"
          role="alert"
          class="mb-4 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700"
        >
          {{ error }}
        </div>

        <form class="space-y-4" @submit.prevent="handleSubmit">
          <div>
            <label for="accountRole" class="block text-sm font-medium text-stone-700">
              Tipo de conta
            </label>
            <select
              id="accountRole"
              v-model="accountRole"
              class="mt-1 block w-full rounded-lg border border-stone-300 bg-white px-3 py-2 text-sm text-stone-900 focus:border-brand-600 focus:outline-none focus:ring-1 focus:ring-brand-600"
            >
              <option value="customer">Cliente (pedir marmitas e assinar planos)</option>
              <option value="restaurant_owner">Dono de Restaurante (vender marmitas)</option>
              <option value="driver">Entregador Parceiro (realizar entregas)</option>
            </select>
          </div>

          <div>
            <label for="name" class="block text-sm font-medium text-stone-700">
              Nome completo
            </label>
            <input
              id="name"
              v-model="name"
              type="text"
              autocomplete="name"
              maxlength="150"
              required
              placeholder="Maria Silva"
              class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900 focus:border-brand-600 focus:outline-none focus:ring-1 focus:ring-brand-600"
            />
          </div>

          <div>
            <label for="email" class="block text-sm font-medium text-stone-700">
              E-mail
            </label>
            <input
              id="email"
              v-model="email"
              type="email"
              autocomplete="email"
              maxlength="254"
              required
              placeholder="maria@email.com"
              class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900 focus:border-brand-600 focus:outline-none focus:ring-1 focus:ring-brand-600"
            />
          </div>

          <div>
            <label for="phone" class="block text-sm font-medium text-stone-700">
              Telefone <span class="text-xs font-normal text-stone-400">(opcional)</span>
            </label>
            <input
              id="phone"
              :value="phone"
              type="tel"
              autocomplete="tel"
              maxlength="20"
              placeholder="(11) 99999-9999"
              class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900 focus:border-brand-600 focus:outline-none focus:ring-1 focus:ring-brand-600"
              @input="onPhoneInput"
            />
          </div>

          <div>
            <label for="password" class="block text-sm font-medium text-stone-700">
              Senha
            </label>
            <div class="relative mt-1">
              <input
                id="password"
                v-model="password"
                :type="showPassword ? 'text' : 'password'"
                autocomplete="new-password"
                minlength="8"
                required
                placeholder="Mínimo de 8 caracteres"
                class="block w-full rounded-lg border border-stone-300 px-3 py-2 pr-10 text-sm text-stone-900 focus:border-brand-600 focus:outline-none focus:ring-1 focus:ring-brand-600"
              />
              <button
                type="button"
                class="absolute inset-y-0 right-0 flex items-center pr-3 text-stone-400 hover:text-stone-600"
                @click="showPassword = !showPassword"
              >
                <EyeOff v-if="showPassword" class="h-4 w-4" />
                <Eye v-else class="h-4 w-4" />
              </button>
            </div>
          </div>

          <div>
            <label for="confirmPassword" class="block text-sm font-medium text-stone-700">
              Confirmar senha
            </label>
            <input
              id="confirmPassword"
              v-model="confirmPassword"
              :type="showPassword ? 'text' : 'password'"
              autocomplete="new-password"
              required
              placeholder="Repita a senha"
              class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900 focus:border-brand-600 focus:outline-none focus:ring-1 focus:ring-brand-600"
            />
          </div>

          <button
            type="submit"
            :disabled="loading"
            class="w-full rounded-lg bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
          >
            {{ loading ? 'Criando conta...' : 'Criar conta' }}
          </button>
        </form>
      </div>
    </div>
  </div>
</template>

