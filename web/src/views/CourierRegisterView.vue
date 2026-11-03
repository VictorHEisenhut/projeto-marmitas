<script setup lang="ts">
import { ref } from 'vue'
import { Utensils } from 'lucide-vue-next'
import { api, formatPhone, getErrorMessage } from '@/services/api'
import type { Courier } from '@/types'

const name = ref('')
const document = ref('')
const phone = ref('')
const loading = ref(false)
const error = ref('')
const submitted = ref<Courier | null>(null)

function onPhoneInput(event: Event) {
  const target = event.target as HTMLInputElement
  phone.value = formatPhone(target.value)
}

async function handleSubmit() {
  error.value = ''

  if (!name.value.trim()) {
    error.value = 'O nome do entregador é obrigatório.'
    return
  }

  loading.value = true
  try {
    const { data } = await api.post<Courier>('/entregadores/registro', {
      name: name.value.trim(),
      document: document.value.trim(),
      phone: phone.value.trim(),
    })
    submitted.value = data
  } catch (err) {
    error.value = getErrorMessage(err, 'Erro ao enviar cadastro.')
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
        Cadastro de Entregador
      </h1>
      <p class="mt-1 text-center text-sm text-stone-600">
        Envie seus dados para aprovação da equipe.
      </p>
    </div>

    <div class="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
      <div class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm sm:p-8">
        <div v-if="submitted" class="space-y-4">
          <div class="rounded-lg border border-green-200 bg-green-50 p-4 text-sm text-green-800">
            <p class="font-semibold">Solicitação enviada com sucesso!</p>
            <p class="mt-1">
              O cadastro de <strong>{{ submitted.name }}</strong> foi registrado com status
              <strong>{{ submitted.status }}</strong> e aguarda aprovação.
            </p>
          </div>

          <RouterLink
            to="/login"
            class="block w-full rounded-lg bg-brand-600 px-4 py-2.5 text-center text-sm font-semibold text-white hover:bg-brand-700"
          >
            Voltar para o login
          </RouterLink>
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
            <label for="name" class="block text-sm font-medium text-stone-700">
              Nome completo
            </label>
            <input
              id="name"
              v-model="name"
              type="text"
              required
              placeholder="João da Silva"
              class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900 focus:border-brand-600 focus:outline-none focus:ring-1 focus:ring-brand-600"
            />
          </div>

          <div>
            <label for="document" class="block text-sm font-medium text-stone-700">
              CPF ou CNH
            </label>
            <input
              id="document"
              v-model="document"
              type="text"
              required
              placeholder="000.000.000-00"
              class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900 focus:border-brand-600 focus:outline-none focus:ring-1 focus:ring-brand-600"
            />
          </div>

          <div>
            <label for="phone" class="block text-sm font-medium text-stone-700">
              Telefone
            </label>
            <input
              id="phone"
              :value="phone"
              type="tel"
              required
              placeholder="(11) 99999-9999"
              class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900 focus:border-brand-600 focus:outline-none focus:ring-1 focus:ring-brand-600"
              @input="onPhoneInput"
            />
          </div>

          <button
            type="submit"
            :disabled="loading"
            class="w-full rounded-lg bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
          >
            {{ loading ? 'Enviando...' : 'Enviar cadastro' }}
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

