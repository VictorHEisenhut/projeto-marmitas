<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { Bike, Check, Eye, EyeOff, Store, User as UserIcon } from 'lucide-vue-next'
import AuthLayout from '@/layouts/AuthLayout.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppInput from '@/components/ui/AppInput.vue'
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
const termsAccepted = ref(false)
const showPassword = ref(false)
const loading = ref(false)
const error = ref('')

const roleCards = [
  {
    id: 'customer' as const,
    label: 'Cliente',
    desc: 'Pedir marmitas e assinar planos',
    icon: UserIcon,
  },
  {
    id: 'restaurant_owner' as const,
    label: 'Restaurante',
    desc: 'Vender no marketplace',
    icon: Store,
  },
  {
    id: 'driver' as const,
    label: 'Entregador',
    desc: 'Realizar corridas e entregas',
    icon: Bike,
  },
]

const passwordChecks = computed(() => ({
  minLength: password.value.length >= 8,
  hasNumber: /\d/.test(password.value),
  hasLetter: /[a-zA-Z]/.test(password.value),
  matches: password.value.length > 0 && password.value === confirmPassword.value,
}))

const passwordStrengthScore = computed(() => {
  let score = 0
  if (passwordChecks.value.minLength) score++
  if (passwordChecks.value.hasLetter) score++
  if (passwordChecks.value.hasNumber) score++
  if (/[^a-zA-Z0-9]/.test(password.value) || password.value.length >= 12) score++
  return score
})

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
  if (!termsAccepted.value) {
    error.value = 'Você precisa aceitar os Termos de Uso e a Política de Privacidade (LGPD).'
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
      termsAccepted: termsAccepted.value,
    })

    if (accountRole.value === 'driver') {
      try {
        await api.post('/entregadores/registro', {
          name: name.value.trim(),
          document: '',
          phone: phone.value.trim(),
        })
      } catch {
        // Ignore if already registered
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
  <AuthLayout
    title="Criar sua conta"
    subtitle="Escolha seu perfil e comece em menos de um minuto."
    wide
  >
    <div
      v-if="error"
      role="alert"
      class="mb-4 rounded-xl border border-red-200 bg-red-50 p-3.5 text-xs font-medium text-red-700 dark:border-red-900/60 dark:bg-red-950/40 dark:text-red-300"
    >
      {{ error }}
    </div>

    <form class="space-y-4" @submit.prevent="handleSubmit">
      <!-- Visual Account Role Selector Cards -->
      <div class="space-y-1.5">
        <label class="block text-xs font-semibold text-ink-secondary">
          Como você vai usar a plataforma?
        </label>
        <div class="grid gap-2.5 sm:grid-cols-3">
          <button
            v-for="role in roleCards"
            :key="role.id"
            type="button"
            class="flex flex-col items-start gap-1.5 rounded-2xl border p-3.5 text-left transition"
            :class="
              accountRole === role.id
                ? 'border-brand-600 bg-brand-50/50 ring-1 ring-brand-600/30 dark:bg-brand-950/35'
                : 'border-line bg-surface-subtle hover:border-line-strong'
            "
            @click="accountRole = role.id"
          >
            <component
              :is="role.icon"
              class="h-5 w-5"
              :class="accountRole === role.id ? 'text-brand-600' : 'text-ink-muted'"
            />
            <span class="font-display text-sm font-extrabold text-ink">{{ role.label }}</span>
            <span class="text-[11px] leading-snug text-ink-muted">{{ role.desc }}</span>
          </button>
        </div>
      </div>

      <AppInput
        v-model="name"
        label="Nome completo"
        autocomplete="name"
        maxlength="150"
        required
        placeholder="Maria Silva"
      />

      <div class="grid gap-3 sm:grid-cols-2">
        <AppInput
          v-model="email"
          label="E-mail"
          type="email"
          autocomplete="email"
          maxlength="254"
          required
          placeholder="maria@email.com"
        />
        <AppInput
          :model-value="phone"
          label="Telefone (opcional)"
          type="tel"
          autocomplete="tel"
          maxlength="20"
          placeholder="(11) 99999-9999"
          @input="onPhoneInput"
        />
      </div>

      <div class="grid gap-3 sm:grid-cols-2">
        <AppInput
          v-model="password"
          label="Senha"
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
          label="Confirmar senha"
          :type="showPassword ? 'text' : 'password'"
          autocomplete="new-password"
          required
          placeholder="Repita a senha"
        />
      </div>

      <!-- Password Strength Bar & Checklist -->
      <div v-if="password.length > 0" class="rounded-xl border border-line bg-surface-subtle p-3 space-y-2">
        <div class="grid grid-cols-4 gap-1.5">
          <div
            v-for="bar in 4"
            :key="bar"
            class="h-1.5 rounded-full transition-colors"
            :class="
              bar <= passwordStrengthScore
                ? passwordStrengthScore >= 3
                  ? 'bg-fresh-500'
                  : 'bg-amber-500'
                : 'bg-line-strong'
            "
          />
        </div>
        <div class="flex flex-wrap gap-x-4 gap-y-1 text-[11px]">
          <span
            class="inline-flex items-center gap-1 font-semibold"
            :class="passwordChecks.minLength ? 'text-fresh-700 dark:text-fresh-400' : 'text-ink-muted'"
          >
            <Check class="h-3 w-3" /> Mín. 8 caracteres
          </span>
          <span
            class="inline-flex items-center gap-1 font-semibold"
            :class="passwordChecks.hasNumber ? 'text-fresh-700 dark:text-fresh-400' : 'text-ink-muted'"
          >
            <Check class="h-3 w-3" /> Contém número
          </span>
          <span
            v-if="confirmPassword.length > 0"
            class="inline-flex items-center gap-1 font-semibold"
            :class="passwordChecks.matches ? 'text-fresh-700 dark:text-fresh-400' : 'text-red-600'"
          >
            <Check class="h-3 w-3" /> {{ passwordChecks.matches ? 'Senhas conferem' : 'Senhas diferentes' }}
          </span>
        </div>
      </div>

      <!-- LGPD Terms Checkbox -->
      <label class="flex items-start gap-2.5 rounded-xl border border-line bg-surface-subtle p-3.5 text-xs text-ink-secondary cursor-pointer">
        <input
          v-model="termsAccepted"
          type="checkbox"
          required
          class="mt-0.5 h-4 w-4 rounded border-line-strong accent-brand-600"
        />
        <span class="leading-relaxed">
          Li e aceito os <strong class="text-ink">Termos de Uso</strong> e a
          <strong class="text-ink">Política de Privacidade (LGPD)</strong>, autorizando o tratamento dos meus dados para operação dos pedidos e entregas.
        </span>
      </label>

      <AppButton type="submit" variant="primary" size="lg" block :loading="loading">
        Criar minha conta
      </AppButton>
    </form>

    <template #footer>
      Já possui cadastro?
      <RouterLink to="/login" class="font-bold text-brand-600 hover:underline dark:text-brand-400">
        Fazer login
      </RouterLink>
    </template>
  </AuthLayout>
</template>
