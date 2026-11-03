<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Eye, EyeOff, Utensils } from 'lucide-vue-next'
import { api, getErrorMessage } from '@/services/api'

const route = useRoute()
const router = useRouter()

const email = ref(typeof route.query.email === 'string' ? route.query.email : '')
const newPassword = ref('')
const confirmPassword = ref('')
const showPassword = ref(false)
const loading = ref(false)
const error = ref('')

async function handleSubmit() {
  error.value = ''

  if (!email.value.trim()) {
    error.value = 'Informe o e-mail da conta.'
    return
  }

  if (newPassword.value.length < 8) {
    error.value = 'A nova senha deve ter pelo menos 8 caracteres.'
    return
  }

  if (newPassword.value !== confirmPassword.value) {
    error.value = 'As senhas não conferem.'
    return
  }

  loading.value = true
  try {
    await api.post('/auth/redefinir-senha', {
      email: email.value.trim().toLowerCase(),
      newPassword: newPassword.value,
    })

    await router.push({
      path: '/login',
      query: { email: email.value.trim().toLowerCase() },
    })
  } catch (err) {
    error.value = getErrorMessage(err, 'Não foi possível redefinir a senha.')
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
        Redefinir senha
      </h1>
      <p class="mt-1 text-center text-sm text-stone-600">
        Cadastre uma nova senha com no mínimo 8 caracteres.
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
            <label for="email" class="block text-sm font-medium text-stone-700">
              E-mail da conta
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
            <label for="newPassword" class="block text-sm font-medium text-stone-700">
              Nova senha
            </label>
            <div class="relative mt-1">
              <input
                id="newPassword"
                v-model="newPassword"
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
              Confirmar nova senha
            </label>
            <input
              id="confirmPassword"
              v-model="confirmPassword"
              :type="showPassword ? 'text' : 'password'"
              autocomplete="new-password"
              required
              placeholder="Repita a nova senha"
              class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900 focus:border-brand-600 focus:outline-none focus:ring-1 focus:ring-brand-600"
            />
          </div>

          <button
            type="submit"
            :disabled="loading"
            class="w-full rounded-lg bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
          >
            {{ loading ? 'Salvando...' : 'Atualizar senha' }}
          </button>
        </form>

        <div class="mt-6 border-t border-stone-200 pt-4 text-center">
          <RouterLink to="/login" class="text-sm font-medium text-stone-600 hover:text-brand-600">
            Voltar para o login
          </RouterLink>
        </div>
      </div>
    </div>
  </div>
</template>

