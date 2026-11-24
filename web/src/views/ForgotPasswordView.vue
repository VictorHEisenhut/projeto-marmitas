<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { ArrowRight, CheckCircle2, Mail } from 'lucide-vue-next'
import AuthLayout from '@/layouts/AuthLayout.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppInput from '@/components/ui/AppInput.vue'
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
  <AuthLayout
    title="Recuperar senha"
    subtitle="Informe seu e-mail cadastrado para gerar um código de verificação."
  >
    <div v-if="result" class="space-y-5">
      <div class="rounded-2xl border border-fresh-200 bg-fresh-50/80 p-4 text-center space-y-2 dark:border-fresh-800/60 dark:bg-fresh-950/35">
        <CheckCircle2 class="mx-auto h-8 w-8 text-fresh-600" />
        <p class="text-sm font-bold text-ink">{{ result.message }}</p>
        <p class="text-xs text-ink-secondary">Conta: {{ result.email }}</p>

        <div
          v-if="result.resetCode"
          class="mt-3 rounded-xl border border-fresh-300 bg-surface p-3 dark:border-fresh-800"
        >
          <p class="text-[11px] font-semibold uppercase tracking-wider text-ink-muted">
            Código de verificação (6 dígitos)
          </p>
          <p class="mt-1 font-mono text-2xl font-extrabold tracking-[0.35em] text-brand-600 dark:text-brand-400">
            {{ result.resetCode }}
          </p>
        </div>
      </div>

      <AppButton variant="primary" size="lg" block @click="goToReset">
        <span>Definir nova senha</span>
        <ArrowRight class="h-4 w-4" />
      </AppButton>
    </div>

    <form v-else class="space-y-4" @submit.prevent="handleSubmit">
      <div
        v-if="error"
        role="alert"
        class="rounded-xl border border-red-200 bg-red-50 p-3.5 text-xs font-medium text-red-700 dark:border-red-900/60 dark:bg-red-950/40 dark:text-red-300"
      >
        {{ error }}
      </div>

      <AppInput
        v-model="email"
        label="E-mail da conta"
        type="email"
        autocomplete="email"
        required
        placeholder="seu@email.com"
      >
        <template #prefix>
          <Mail class="h-4 w-4" />
        </template>
      </AppInput>

      <AppButton type="submit" variant="primary" size="lg" block :loading="loading">
        Enviar código de verificação
      </AppButton>
    </form>

    <template #footer>
      Lembrou sua senha?
      <RouterLink to="/login" class="font-bold text-brand-600 hover:underline dark:text-brand-400">
        Voltar para o login
      </RouterLink>
    </template>
  </AuthLayout>
</template>
