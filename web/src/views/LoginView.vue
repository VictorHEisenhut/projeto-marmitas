<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Eye, EyeOff, ShieldAlert } from 'lucide-vue-next'
import AuthLayout from '@/layouts/AuthLayout.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppInput from '@/components/ui/AppInput.vue'
import { getErrorMessage } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const toast = useToastStore()

const email = ref(typeof route.query.email === 'string' ? route.query.email : '')
const password = ref('')
const showPassword = ref(false)
const loading = ref(false)
const error = ref('')

const registeredSuccess = route.query.registered === 'true'
const isLockoutError = computed(() => /bloquead|lockout|15 min|tentativas/i.test(error.value))

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

    toast.success('Login realizado com sucesso!')
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
  <AuthLayout
    title="Entrar na sua conta"
    subtitle="Acesse seus pedidos, assinaturas ou painel operacional."
  >
    <div
      v-if="registeredSuccess && !error"
      class="mb-4 rounded-xl border border-fresh-200 bg-fresh-50 p-3.5 text-xs font-medium text-fresh-800 dark:border-fresh-800/60 dark:bg-fresh-950/40 dark:text-fresh-300"
    >
      Conta criada com sucesso! Faça login para continuar.
    </div>

    <div
      v-if="error"
      role="alert"
      class="mb-4 flex items-start gap-2.5 rounded-xl border p-3.5 text-xs font-medium"
      :class="
        isLockoutError
          ? 'border-amber-300 bg-amber-50 text-amber-900 dark:border-amber-700/60 dark:bg-amber-950/40 dark:text-amber-200'
          : 'border-red-200 bg-red-50 text-red-700 dark:border-red-900/60 dark:bg-red-950/40 dark:text-red-300'
      "
    >
      <ShieldAlert v-if="isLockoutError" class="mt-0.5 h-4 w-4 shrink-0" />
      <div>
        <p class="font-bold" v-if="isLockoutError">Conta temporariamente bloqueada</p>
        <p>{{ error }}</p>
      </div>
    </div>

    <form class="space-y-4" @submit.prevent="handleSubmit">
      <AppInput
        v-model="email"
        label="E-mail"
        type="email"
        autocomplete="email"
        required
        placeholder="seu@email.com"
      />

      <AppInput
        v-model="password"
        label="Senha"
        :type="showPassword ? 'text' : 'password'"
        autocomplete="current-password"
        required
        placeholder="••••••••"
      >
        <template #labelRight>
          <RouterLink
            to="/esqueci-senha"
            class="text-xs font-semibold text-brand-600 hover:underline dark:text-brand-400"
          >
            Esqueceu a senha?
          </RouterLink>
        </template>
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

      <AppButton type="submit" variant="primary" size="lg" block :loading="loading">
        Entrar
      </AppButton>
    </form>

    <template #footer>
      Ainda não tem conta?
      <RouterLink to="/cadastro" class="font-bold text-brand-600 hover:underline dark:text-brand-400">
        Criar conta gratuitamente
      </RouterLink>
    </template>
  </AuthLayout>
</template>
