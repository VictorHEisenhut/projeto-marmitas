<script setup lang="ts">
import { ref } from 'vue'
import { Utensils } from 'lucide-vue-next'
import { api, formatPhone, getErrorMessage } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import type { Courier } from '@/types'

const auth = useAuthStore()

const name = ref('')
const email = ref('')
const password = ref('')
const cpf = ref('')
const cnh = ref('')
const phone = ref('')
const vehicleType = ref('Moto')
const vehicleModel = ref('')
const vehiclePlate = ref('')
const billingZipCode = ref('')
const billingStreet = ref('')
const billingNumber = ref('')
const billingNeighborhood = ref('')
const billingCity = ref('')
const billingState = ref('SP')

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
  if (!cpf.value.trim() || !cnh.value.trim()) {
    error.value = 'CPF e CNH são obrigatórios.'
    return
  }
  if (!vehicleModel.value.trim() || !vehiclePlate.value.trim()) {
    error.value = 'Modelo e placa do veículo são obrigatórios.'
    return
  }
  if (
    !billingZipCode.value.trim() ||
    !billingStreet.value.trim() ||
    !billingNumber.value.trim() ||
    !billingNeighborhood.value.trim() ||
    !billingCity.value.trim() ||
    !billingState.value.trim()
  ) {
    error.value = 'O endereço de cobrança completo é obrigatório.'
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
  <div class="flex min-h-screen flex-col justify-center bg-stone-50 px-4 py-12 sm:px-6 lg:px-8">
    <div class="sm:mx-auto sm:w-full sm:max-w-lg">
      <RouterLink to="/" class="flex items-center justify-center gap-2 text-stone-900">
        <span class="flex h-10 w-10 items-center justify-center rounded-lg bg-brand-600 text-white">
          <Utensils class="h-5 w-5" />
        </span>
        <span class="text-xl font-bold">Marmitas</span>
      </RouterLink>

      <h1 class="mt-6 text-center text-2xl font-bold text-stone-900">
        Cadastro Completo de Entregador
      </h1>
      <p class="mt-1 text-center text-sm text-stone-600">
        Informe seus dados obrigatórios (CPF, CNH, Veículo e Endereço de Cobrança).
      </p>
    </div>

    <div class="mt-8 sm:mx-auto sm:w-full sm:max-w-lg">
      <div class="rounded-xl border border-stone-200 bg-white p-6 shadow-sm sm:p-8">
        <div v-if="submitted" class="space-y-4">
          <div class="rounded-lg border border-green-200 bg-green-50 p-4 text-sm text-green-800">
            <p class="font-semibold">Cadastro realizado com sucesso!</p>
            <p class="mt-1">
              O cadastro de <strong>{{ submitted.name }}</strong> foi registrado com seus documentos,
              veículo e endereço de cobrança.
            </p>
          </div>

          <RouterLink
            to="/login"
            class="block w-full rounded-lg bg-brand-600 px-4 py-2.5 text-center text-sm font-semibold text-white hover:bg-brand-700"
          >
            Ir para o login
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
              Nome completo <span class="text-red-500">*</span>
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

          <div v-if="!auth.isAuthenticated" class="grid gap-3 sm:grid-cols-2">
            <div>
              <label for="email" class="block text-sm font-medium text-stone-700">
                E-mail de acesso <span class="text-red-500">*</span>
              </label>
              <input
                id="email"
                v-model="email"
                type="email"
                required
                placeholder="entregador@email.com"
                class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900 focus:border-brand-600 focus:outline-none focus:ring-1 focus:ring-brand-600"
              />
            </div>

            <div>
              <label for="password" class="block text-sm font-medium text-stone-700">
                Senha (mín. 8 caracteres) <span class="text-red-500">*</span>
              </label>
              <input
                id="password"
                v-model="password"
                type="password"
                minlength="8"
                required
                placeholder="Mínimo 8 caracteres"
                class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900 focus:border-brand-600 focus:outline-none focus:ring-1 focus:ring-brand-600"
              />
            </div>
          </div>

          <div class="grid gap-3 sm:grid-cols-2">
            <div>
              <label for="cpf" class="block text-sm font-medium text-stone-700">
                CPF <span class="text-red-500">*</span>
              </label>
              <input
                id="cpf"
                v-model="cpf"
                type="text"
                required
                placeholder="000.000.000-00"
                class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900 focus:border-brand-600 focus:outline-none focus:ring-1 focus:ring-brand-600"
              />
            </div>

            <div>
              <label for="cnh" class="block text-sm font-medium text-stone-700">
                CNH <span class="text-red-500">*</span>
              </label>
              <input
                id="cnh"
                v-model="cnh"
                type="text"
                required
                placeholder="00000000000"
                class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm text-stone-900 focus:border-brand-600 focus:outline-none focus:ring-1 focus:ring-brand-600"
              />
            </div>
          </div>

          <div>
            <label for="phone" class="block text-sm font-medium text-stone-700">
              Telefone <span class="text-red-500">*</span>
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

          <div class="rounded-lg border border-stone-200 bg-stone-50 p-3 space-y-3">
            <h2 class="text-xs font-bold uppercase tracking-wide text-stone-700">
              Veículo Obrigatório
            </h2>
            <div class="grid gap-2 sm:grid-cols-3">
              <div>
                <label class="block text-xs font-medium text-stone-700">Tipo *</label>
                <select
                  v-model="vehicleType"
                  class="mt-1 block w-full rounded-lg border border-stone-300 bg-white px-2.5 py-2 text-sm"
                >
                  <option value="Moto">Moto</option>
                  <option value="Bicicleta">Bicicleta</option>
                  <option value="Carro">Carro</option>
                </select>
              </div>
              <div>
                <label class="block text-xs font-medium text-stone-700">Modelo *</label>
                <input
                  v-model="vehicleModel"
                  type="text"
                  required
                  placeholder="CG 160"
                  class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm"
                />
              </div>
              <div>
                <label class="block text-xs font-medium text-stone-700">Placa *</label>
                <input
                  v-model="vehiclePlate"
                  type="text"
                  required
                  placeholder="ABC1D23"
                  class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm uppercase"
                />
              </div>
            </div>
          </div>

          <div class="rounded-lg border border-stone-200 bg-stone-50 p-3 space-y-3">
            <div>
              <h2 class="text-xs font-bold uppercase tracking-wide text-stone-700">
                Endereço de Cobrança (Obrigatório)
              </h2>
              <p class="text-[11px] text-stone-500">
                Exclusivo para faturamento do entregador (não é endereço de entrega).
              </p>
            </div>
            <div class="grid gap-2 sm:grid-cols-3">
              <div>
                <label class="block text-xs font-medium text-stone-700">CEP *</label>
                <input
                  v-model="billingZipCode"
                  type="text"
                  required
                  placeholder="00000-000"
                  class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm"
                />
              </div>
              <div class="sm:col-span-2">
                <label class="block text-xs font-medium text-stone-700">Rua / Logradouro *</label>
                <input
                  v-model="billingStreet"
                  type="text"
                  required
                  placeholder="Rua Exemplo"
                  class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm"
                />
              </div>
              <div>
                <label class="block text-xs font-medium text-stone-700">Número *</label>
                <input
                  v-model="billingNumber"
                  type="text"
                  required
                  placeholder="100"
                  class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm"
                />
              </div>
              <div>
                <label class="block text-xs font-medium text-stone-700">Bairro *</label>
                <input
                  v-model="billingNeighborhood"
                  type="text"
                  required
                  placeholder="Centro"
                  class="mt-1 block w-full rounded-lg border border-stone-300 px-3 py-2 text-sm"
                />
              </div>
              <div>
                <label class="block text-xs font-medium text-stone-700">Cidade / UF *</label>
                <div class="mt-1 flex gap-1">
                  <input
                    v-model="billingCity"
                    type="text"
                    required
                    placeholder="São Paulo"
                    class="block w-full rounded-lg border border-stone-300 px-2.5 py-2 text-sm"
                  />
                  <input
                    v-model="billingState"
                    type="text"
                    maxlength="2"
                    required
                    placeholder="SP"
                    class="block w-14 rounded-lg border border-stone-300 px-2 py-2 text-center text-sm uppercase"
                  />
                </div>
              </div>
            </div>
          </div>

          <button
            type="submit"
            :disabled="loading"
            class="w-full rounded-lg bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
          >
            {{ loading ? 'Enviando...' : 'Concluir cadastro de entregador' }}
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
