<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Eye, EyeOff, KeyRound } from 'lucide-vue-next'
import AuthLayout from '@/layouts/AuthLayout.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppInput from '@/components/ui/AppInput.vue'
import { api, getErrorMessage } from '@/services/api'
import { useToastStore } from '@/stores/toast'

const route = useRoute()
const router = useRouter()
const toast = useToastStore()

const email = ref(typeof route.query.email === 'string' ? route.query.email : '')
const resetCode = ref(typeof route.query.code === 'string' ? route.query.code : '')
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
  if (!resetCode.value.trim()) {
    error.value = 'Informe o código de verificação de 6 dígitos.'
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
      resetCode: resetCode.value.trim(),
    })

    toast.success('Senha atualizada com sucesso! Faça login com sua nova senha.')
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
  <AuthLayout
    title="Redefinir senha"
    subtitle="Digite o código de 6 dígitos e cadastre sua nova senha."
  >
    <div
      v-if="error"
      role="alert"
      class="mb-4 rounded-xl border border-red-200 bg-red-50 p-3.5 text-xs font-medium text-red-700 dark:border-red-900/60 dark:bg-red-950/40 dark:text-red-300"
    >
      {{ error }}
    </div>

    <form class="space-y-4" @submit.prevent="handleSubmit">
      <AppInput
        v-model="email"
        label="E-mail da conta"
        type="email"
        autocomplete="email"
        required
        placeholder="seu@email.com"
      />

      <AppInput
        v-model="resetCode"
        label="Código de verificação (6 dígitos)"
        maxlength="10"
        required
        placeholder="000000"
        input-class="font-mono text-base tracking-[0.25em] font-bold"
      >
        <template #prefix>
          <KeyRound class="h-4 w-4 text-brand-600" />
        </template>
      </AppInput>

      <AppInput
        v-model="newPassword"
        label="Nova senha"
        :type="showPassword ? 'text' : 'password'"
        autocomplete="new-password"
        minlength="8"
        required
        placeholder="Mínimo de 8 caracteres"
      >
        <template #suffix>
          <button
            type="button"
            aria-label="Mostrar ou ocultar senha"
            class="p-1 text-ink-muted hover:text-ink"
            @click="showPassword = !showPassword"
          >
            <EyeOff v-if="showPassword" class="h-4 w-4" />
            <Eye v-else class="h-4 w-4" />
          </button>
        </template>
      </AppInput>

      <AppInput
        v-model="confirmPassword"
        label="Confirmar nova senha"
        :type="showPassword ? 'text' : 'password'"
        autocomplete="new-password"
        required
        placeholder="Repita a nova senha"
      />

      <AppButton type="submit" variant="primary" size="lg" block :loading="loading">
        Atualizar senha
      </AppButton>
    </form>

    <template #footer>
      <RouterLink to="/login" class="font-bold text-brand-600 hover:underline dark:text-brand-400">
        Voltar para o login
      </RouterLink>
    </template>
  </AuthLayout>
</template>
