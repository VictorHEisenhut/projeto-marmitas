<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Eye, EyeOff, Utensils } from 'lucide-vue-next'
import { getErrorMessage } from '@/services/api'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const email = ref(typeof route.query.email === 'string' ? route.query.email : '')
const password = ref('')
const showPassword = ref(false)
const loading = ref(false)
const error = ref('')

const registeredSuccess = route.query.registered === 'true'

async function handleSubmit() {
  error.value = ''

  if (!email.value.trim() || !password.value) {
    error.value = 'Preencha seu e-mail e senha.'
    return
  }

  loading.value = true
  try {
    await auth.login({
      email: email.value,
      password: password.value,
    })

    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : ''
    if (redirect && redirect.startsWith('/')) {
      await router.replace(redirect)
    } else {
      await router.replace(auth.defaultRoute)
    }
  } catch (err) {
    error.value = getErrorMessage(err, 'E-mail ou senha inválidos.')
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
        Entrar na sua conta
      </h1>
      <p class="mt-1 text-center text-sm text-stone-600">
        Ou
        <RouterLink to="/cadastro" class="font-semibold text-brand-600 hover:underline">
          crie uma conta gratuitamente
        </RouterLink>
      </p>
    </div>

    <div class="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
      <div class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm sm:p-8">
        <div
          v-if="registeredSuccess && !error"
          class="mb-4 rounded-lg border border-green-200 bg-green-50 p-3 text-sm text-green-800"
        >
          Conta criada com sucesso! Faça login para continuar.
        </div>

        <div
          v-if="error"
          role="alert"
          class="mb-4 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700"
        >
          {{ error }}
        </div>

        <form class="space-y-4" @submit.prevent="handleSubmit">
          <div>
            <label for="email" class="block text-sm font-medium text-stone-700">
              E-mail
            </label>
            <input
              id="email"
              v-model="email"
              type="email"
              autocomplete="email"
              required
              placeholder="seu@email.com"
              class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900 focus:border-brand-600 focus:outline-none focus:ring-1 focus:ring-brand-600"
            />
          </div>

          <div>
            <div class="flex items-center justify-between">
              <label for="password" class="block text-sm font-medium text-stone-700">
                Senha
              </label>
              <RouterLink
                to="/esqueci-senha"
                class="text-xs font-medium text-brand-600 hover:underline"
              >
                Esqueceu a senha?
              </RouterLink>
            </div>

            <div class="relative mt-1">
              <input
                id="password"
                v-model="password"
                :type="showPassword ? 'text' : 'password'"
                autocomplete="current-password"
                required
                placeholder="••••••••"
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

          <button
            type="submit"
            :disabled="loading"
            class="w-full rounded-lg bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
          >
            {{ loading ? 'Entrando...' : 'Entrar' }}
          </button>
        </form>

        <div class="mt-6 border-t border-stone-200 pt-4 text-center">
          <RouterLink
            to="/entregador/cadastro"
            class="text-xs font-medium text-stone-500 hover:text-brand-600"
          >
            Quer trabalhar com entregas? Cadastre-se como entregador
          </RouterLink>
        </div>
      </div>
    </div>
  </div>
</template>

