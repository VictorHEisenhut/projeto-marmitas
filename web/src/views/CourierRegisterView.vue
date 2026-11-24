<script setup lang="ts">
import { computed, ref } from 'vue'
import { ArrowLeft, ArrowRight, CheckCircle2, Search, ShieldCheck } from 'lucide-vue-next'
import AuthLayout from '@/layouts/AuthLayout.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppInput from '@/components/ui/AppInput.vue'
import { api, formatPhone, getErrorMessage } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import type { Courier } from '@/types'

const auth = useAuthStore()

const step = ref<1 | 2 | 3>(1)

const name = ref('')
const email = ref('')
const password = ref('')
const phone = ref('')

const cpf = ref('')
const cnh = ref('')
const vehicleType = ref('Moto')
const vehicleModel = ref('')
const vehiclePlate = ref('')

const billingZipCode = ref('')
const billingStreet = ref('')
const billingNumber = ref('')
const billingNeighborhood = ref('')
const billingCity = ref('Blumenau')
const billingState = ref('SC')

const loading = ref(false)
const lookingUpCep = ref(false)
const error = ref('')
const submitted = ref<Courier | null>(null)

const wizardSteps = [
  { id: 1 as const, label: 'Acesso & Dados' },
  { id: 2 as const, label: 'Docs & Veículo' },
  { id: 3 as const, label: 'Cobrança' },
]

function onPhoneInput(event: Event) {
  const target = event.target as HTMLInputElement
  phone.value = formatPhone(target.value)
}

function onPlateInput(value: string) {
  vehiclePlate.value = value.toUpperCase().replace(/[^A-Z0-9-]/g, '').slice(0, 8)
}

function onStateInput(value: string) {
  billingState.value = value.toUpperCase().replace(/[^A-Z]/g, '').slice(0, 2)
}

const requirementsChecklist = computed(() => [
  { key: 'cpf', label: 'CPF', done: cpf.value.replace(/\D/g, '').length >= 11 },
  { key: 'cnh', label: 'CNH', done: cnh.value.trim().length >= 5 },
  {
    key: 'vehicle',
    label: 'Veículo',
    done: Boolean(vehicleType.value && vehicleModel.value.trim() && vehiclePlate.value.trim().length >= 6),
  },
  {
    key: 'billing',
    label: 'End. Cobrança',
    done: Boolean(
      billingZipCode.value.trim() &&
      billingStreet.value.trim() &&
      billingNumber.value.trim() &&
      billingCity.value.trim() &&
      billingState.value.trim().length === 2,
    ),
  },
])

function goNextStep() {
  error.value = ''
  if (step.value === 1) {
    if (!name.value.trim() || !phone.value.trim()) {
      error.value = 'Informe seu nome completo e telefone.'
      return
    }
    if (!auth.isAuthenticated && (!email.value.trim() || password.value.length < 8)) {
      error.value = 'Informe um e-mail válido e uma senha com no mínimo 8 caracteres.'
      return
    }
    step.value = 2
    return
  }
  if (step.value === 2) {
    if (!cpf.value.trim() || !cnh.value.trim()) {
      error.value = 'CPF e CNH são obrigatórios.'
      return
    }
    if (!vehicleModel.value.trim() || !vehiclePlate.value.trim()) {
      error.value = 'Modelo e placa do veículo são obrigatórios.'
      return
    }
    step.value = 3
  }
}

async function handleLookupCep() {
  const clean = billingZipCode.value.replace(/\D/g, '')
  if (clean.length !== 8) return
  lookingUpCep.value = true
  try {
    const { data } = await api.get<{
      street: string
      neighborhood: string
      city: string
      state: string
    }>(`/geolocalizacao/cep/${clean}`)
    if (data.street) billingStreet.value = data.street
    if (data.neighborhood) billingNeighborhood.value = data.neighborhood
    if (data.city) billingCity.value = data.city
    if (data.state) billingState.value = data.state
  } catch {
    // Allow manual input
  } finally {
    lookingUpCep.value = false
  }
}

async function handleSubmit() {
  error.value = ''

  if (
    !billingZipCode.value.trim() ||
    !billingStreet.value.trim() ||
    !billingNumber.value.trim() ||
    !billingNeighborhood.value.trim() ||
    !billingCity.value.trim() ||
    !billingState.value.trim()
  ) {
    error.value = 'Preencha todos os campos do endereço de cobrança.'
    return
  }

  loading.value = true
  try {
    if (!auth.isAuthenticated && email.value.trim() && password.value) {
      await auth.register({
        name: name.value.trim(),
        email: email.value.trim().toLowerCase(),
        phone: phone.value.trim() || null,
        password: password.value,
        role: 'driver',
        termsAccepted: true,
      })
    }

    const { data } = await api.post<Courier>('/entregadores/registro', {
      name: name.value.trim(),
      document: cpf.value.trim(),
      cpf: cpf.value.trim(),
      cnh: cnh.value.trim(),
      phone: phone.value.trim(),
      vehicleType: vehicleType.value,
      vehicleModel: vehicleModel.value.trim(),
      vehiclePlate: vehiclePlate.value.trim().toUpperCase(),
      billingZipCode: billingZipCode.value.trim(),
      billingStreet: billingStreet.value.trim(),
      billingNumber: billingNumber.value.trim(),
      billingNeighborhood: billingNeighborhood.value.trim(),
      billingCity: billingCity.value.trim(),
      billingState: billingState.value.trim().toUpperCase(),
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
  <AuthLayout
    title="Cadastro de Entregador"
    subtitle="Complete as 3 etapas obrigatórias para entrar na fila de corridas."
    wide
  >
    <!-- Success Screen -->
    <div v-if="submitted" class="space-y-5 text-center py-4">
      <div class="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-fresh-50 text-fresh-600 dark:bg-fresh-950/60 dark:text-fresh-400">
        <CheckCircle2 class="h-8 w-8" />
      </div>
      <div class="space-y-1.5">
        <h2 class="font-display text-xl font-extrabold text-ink">Cadastro concluído!</h2>
        <p class="text-sm text-ink-secondary">
          O cadastro de <strong class="text-ink">{{ submitted.name }}</strong> foi registrado com CPF, CNH, veículo e endereço de cobrança.
        </p>
      </div>
      <RouterLink to="/login" class="block">
        <AppButton variant="primary" size="lg" block>Ir para o login</AppButton>
      </RouterLink>
    </div>

    <!-- 3-Step Wizard -->
    <div v-else class="space-y-6">
      <!-- Visual Stepper -->
      <div class="grid grid-cols-3 gap-2">
        <div
          v-for="s in wizardSteps"
          :key="s.id"
          class="space-y-1.5"
        >
          <div
            class="h-1.5 rounded-full transition-colors"
            :class="step >= s.id ? 'bg-brand-600' : 'bg-surface-muted'"
          />
          <p
            class="text-center text-[11px] font-bold"
            :class="step >= s.id ? 'text-ink' : 'text-ink-muted'"
          >
            {{ s.id }}. {{ s.label }}
          </p>
        </div>
      </div>

      <!-- Live 4-Item Mandatory Queue Checklist -->
      <div class="rounded-2xl border border-line bg-surface-subtle p-3">
        <div class="mb-2 flex items-center gap-1.5 text-[11px] font-bold text-ink-secondary">
          <ShieldCheck class="h-3.5 w-3.5 text-brand-600" />
          <span>Requisitos obrigatórios para liberar a fila de corridas</span>
        </div>
        <div class="grid grid-cols-2 gap-2 sm:grid-cols-4">
          <div
            v-for="req in requirementsChecklist"
            :key="req.key"
            class="flex items-center gap-1.5 rounded-lg border px-2 py-1 text-[11px] font-semibold transition-colors"
            :class="req.done
              ? 'border-fresh-200 bg-fresh-50 text-fresh-700 dark:border-fresh-900/60 dark:bg-fresh-950/40 dark:text-fresh-300'
              : 'border-line bg-surface text-ink-muted'"
          >
            <CheckCircle2 class="h-3.5 w-3.5 shrink-0" :class="req.done ? 'text-fresh-600' : 'text-ink-muted opacity-50'" />
            <span class="truncate">{{ req.label }}</span>
          </div>
        </div>
      </div>

      <div
        v-if="error"
        role="alert"
        class="rounded-xl border border-red-200 bg-red-50 p-3.5 text-xs font-medium text-red-700 dark:border-red-900/60 dark:bg-red-950/40 dark:text-red-300"
      >
        {{ error }}
      </div>

      <form class="space-y-4" @submit.prevent="step < 3 ? goNextStep() : handleSubmit()">
        <!-- Step 1: Access & Personal Info -->
        <template v-if="step === 1">
          <AppInput v-model="name" label="Nome completo" required placeholder="João da Silva" />

          <div v-if="!auth.isAuthenticated" class="grid gap-3 sm:grid-cols-2">
            <AppInput v-model="email" label="E-mail de acesso" type="email" required placeholder="entregador@email.com" />
            <AppInput v-model="password" label="Senha (mín. 8 caracteres)" type="password" minlength="8" required placeholder="••••••••" />
          </div>

          <AppInput
            :model-value="phone"
            label="Telefone / WhatsApp"
            type="tel"
            required
            placeholder="(11) 99999-9999"
            @input="onPhoneInput"
          />
        </template>

        <!-- Step 2: Documents & Vehicle -->
        <template v-else-if="step === 2">
          <div class="grid gap-3 sm:grid-cols-2">
            <AppInput v-model="cpf" label="CPF" required placeholder="000.000.000-00" />
            <AppInput v-model="cnh" label="Número da CNH" required placeholder="00000000000" />
          </div>

          <div class="rounded-2xl border border-line bg-surface-subtle p-4 space-y-3">
            <p class="text-xs font-bold uppercase tracking-wider text-ink-secondary">Veículo Obrigatório</p>
            <div class="grid gap-3 sm:grid-cols-3">
              <div class="space-y-1.5">
                <label class="block text-xs font-semibold text-ink-secondary">Tipo *</label>
                <select
                  v-model="vehicleType"
                  class="w-full rounded-xl border border-line-strong bg-surface px-3 py-2.5 text-sm text-ink"
                >
                  <option value="Moto">Moto</option>
                  <option value="Bicicleta">Bicicleta</option>
                  <option value="Carro">Carro</option>
                </select>
              </div>
              <AppInput v-model="vehicleModel" label="Modelo" required placeholder="CG 160" />
              <AppInput
                :model-value="vehiclePlate"
                label="Placa"
                required
                placeholder="ABC1D23"
                input-class="uppercase font-mono"
                @update:model-value="onPlateInput"
              />
            </div>
          </div>
        </template>

        <!-- Step 3: Billing Address + Review Summary -->
        <template v-else>
          <div class="space-y-3">
            <div class="flex items-end gap-2">
              <AppInput v-model="billingZipCode" label="CEP (Cobrança)" required placeholder="00000-000" class="flex-1" @blur="handleLookupCep" />
              <AppButton type="button" variant="outline" size="md" :loading="lookingUpCep" @click="handleLookupCep">
                <Search class="h-4 w-4" />
                CEP
              </AppButton>
            </div>

            <div class="grid gap-3 sm:grid-cols-4">
              <AppInput v-model="billingStreet" label="Rua / Logradouro" required class="sm:col-span-3" />
              <AppInput v-model="billingNumber" label="Número" required />
            </div>

            <div class="grid gap-3 sm:grid-cols-3">
              <AppInput v-model="billingNeighborhood" label="Bairro" required />
              <AppInput v-model="billingCity" label="Cidade" required />
              <AppInput
                :model-value="billingState"
                label="UF"
                required
                maxlength="2"
                input-class="uppercase font-mono"
                @update:model-value="onStateInput"
              />
            </div>
          </div>

          <!-- Quick Review Box -->
          <div class="rounded-2xl border border-line bg-surface-subtle p-3.5 text-xs text-ink-secondary space-y-1">
            <p class="font-bold text-ink">Resumo do cadastro</p>
            <p>{{ name }} · {{ phone }}</p>
            <p>CPF {{ cpf }} · CNH {{ cnh }} · {{ vehicleType }} {{ vehicleModel }} ({{ vehiclePlate.toUpperCase() }})</p>
          </div>
        </template>

        <!-- Navigation Controls -->
        <div class="flex items-center justify-between gap-3 pt-2">
          <AppButton v-if="step > 1" type="button" variant="outline" size="md" @click="step = (step - 1) as 1 | 2">
            <ArrowLeft class="h-4 w-4" />
            Voltar
          </AppButton>
          <div v-else />

          <AppButton v-if="step < 3" type="submit" variant="primary" size="md">
            <span>Próxima etapa</span>
            <ArrowRight class="h-4 w-4" />
          </AppButton>

          <AppButton v-else type="submit" variant="fresh" size="lg" :loading="loading">
            Concluir cadastro de entregador
          </AppButton>
        </div>
      </form>
    </div>

    <template #footer>
      Já tem cadastro?
      <RouterLink to="/login" class="font-bold text-brand-600 hover:underline dark:text-brand-400">
        Voltar para o login
      </RouterLink>
    </template>
  </AuthLayout>
</template>
