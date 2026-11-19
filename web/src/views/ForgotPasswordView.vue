<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { Utensils } from 'lucide-vue-next'
import { api, getErrorMessage } from '@/services/api'
import type { ForgotPasswordResponse } from '@/types'

const router = useRouter()

const email = ref('')
const loading = ref(false)
const error = ref('')
const result = ref<ForgotPasswordResponse | null>(null)

async function handleSubmit() {
  error.value = ''

  if (!email.value.trim()) {
    error.value = 'Informe o e-mail da conta.'
    return
  }

  loading.value = true
  try {
    const { data } = await api.post<ForgotPasswordResponse>('/auth/esqueci-senha', {
      email: email.value.trim().toLowerCase(),
    })
    result.value = data
  } catch (err) {
    error.value = getErrorMessage(err, 'Não foi possível localizar a conta.')
  } finally {
    loading.value = false
  }
}

function goToReset() {
  if (!result.value) return
  void router.push({
    path: '/redefinir-senha',
    query: {
      email: result.value.email,
      code: result.value.resetCode || undefined,
    },
  })
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
        Recuperar senha
      </h1>
      <p class="mt-1 text-center text-sm text-stone-600">
        Informe seu e-mail cadastrado para redefinir sua senha.
      </p>
    </div>

    <div class="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
      <div class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm sm:p-8">
        <div v-if="result" class="space-y-4">
          <div class="rounded-lg border border-green-200 bg-green-50 p-4 text-sm text-green-800">
            <p class="font-semibold">{{ result.message }}</p>
            <p class="mt-1 text-xs text-green-700">Conta: {{ result.email }}</p>
            <p v-if="result.resetCode" class="mt-2 rounded bg-white/80 px-2.5 py-1.5 font-mono text-xs font-bold text-stone-900">
              Código de verificação: {{ result.resetCode }}
            </p>
          </div>

          <button
            type="button"
            class="w-full rounded-lg bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-700"
            @click="goToReset"
          >
            Definir nova senha
          </button>
        </div>

        <form v-else class="space-y-4" @submit.prevent="handleSubmit">
          <div
            v-if="error"
            role="alert"
            class="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700"
          >
            {{ error }}
          </div>

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

          <button
            type="submit"
            :disabled="loading"
            class="w-full rounded-lg bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
          >
            {{ loading ? 'Verificando...' : 'Continuar' }}
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

