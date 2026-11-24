<script setup lang="ts">
import { ref } from 'vue'
import { Search, Store } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppInput from '@/components/ui/AppInput.vue'
import { api, getErrorMessage } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'
import type { Restaurant } from '@/types'

const emit = defineEmits<{
  (e: 'created', restaurant: Restaurant): void
}>()

const auth = useAuthStore()
const toast = useToastStore()

const name = ref('')
const document = ref('')
const description = ref('')
const zipCode = ref('')
const street = ref('')
const number = ref('')
const complement = ref('')
const neighborhood = ref('')
const city = ref('')
const state = ref('SP')
const saving = ref(false)
const lookingUpCep = ref(false)

async function handleLookupCep() {
  const clean = zipCode.value.replace(/\D/g, '')
  if (clean.length !== 8) return
  lookingUpCep.value = true
  try {
    const { data } = await api.get<{
      street: string
      neighborhood: string
      city: string
      state: string
    }>(`/geolocalizacao/cep/${clean}`)
    if (data.street) street.value = data.street
    if (data.neighborhood) neighborhood.value = data.neighborhood
    if (data.city) city.value = data.city
    if (data.state) state.value = data.state
  } catch {
    // Allow manual input
  } finally {
    lookingUpCep.value = false
  }
}

async function handleCreate() {
  if (!name.value.trim() || !document.value.trim()) {
    toast.warning('Informe o nome e o CNPJ do restaurante.')
    return
  }
  if (
    !zipCode.value.trim() ||
    !street.value.trim() ||
    !number.value.trim() ||
    !neighborhood.value.trim() ||
    !city.value.trim() ||
    !state.value.trim()
  ) {
    toast.warning('Preencha todos os campos obrigatórios do endereço.')
    return
  }

  saving.value = true
  try {
    const { data } = await api.post<Restaurant>('/restaurantes', {
      name: name.value.trim(),
      document: document.value.trim(),
      description: description.value.trim(),
      ownerId: auth.user?.id,
      addressZipCode: zipCode.value.trim(),
      addressStreet: street.value.trim(),
      addressNumber: number.value.trim(),
      addressComplement: complement.value.trim() || null,
      addressNeighborhood: neighborhood.value.trim(),
      addressCity: city.value.trim(),
      addressState: state.value.trim().toUpperCase(),
    })
    toast.success('Restaurante cadastrado e pronto para operação!')
    emit('created', data)
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível cadastrar o restaurante.'))
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="mx-auto max-w-2xl py-4">
    <AppCard padding="lg" class="space-y-6">
      <div class="text-center space-y-2">
        <div class="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-brand-50 text-brand-600 dark:bg-brand-950/60 dark:text-brand-400">
          <Store class="h-7 w-7" />
        </div>
        <h2 class="font-display text-2xl font-extrabold text-ink">
          Bem-vindo ao Gestor do Restaurante
        </h2>
        <p class="mx-auto max-w-md text-sm text-ink-secondary">
          Cadastre os dados fiscais e o endereço de coleta da sua cozinha para começar a montar o cardápio e receber pedidos.
        </p>
      </div>

      <form class="space-y-5" @submit.prevent="handleCreate">
        <div class="grid gap-4 sm:grid-cols-2">
          <AppInput v-model="name" label="Nome do restaurante" required placeholder="Ex: Cozinha Caseira da Maria" />
          <AppInput v-model="document" label="CNPJ" required placeholder="00.000.000/0001-00" />
        </div>

        <div class="rounded-2xl border border-line bg-surface-subtle p-4 space-y-4">
          <p class="text-xs font-bold uppercase tracking-wider text-ink-secondary">
            Endereço de Coleta (Obrigatório)
          </p>

          <div class="flex items-end gap-2">
            <AppInput v-model="zipCode" label="CEP" required placeholder="00000-000" class="flex-1" @blur="handleLookupCep" />
            <AppButton type="button" variant="outline" size="md" :loading="lookingUpCep" @click="handleLookupCep">
              <Search class="h-4 w-4" />
              Buscar CEP
            </AppButton>
          </div>

          <div class="grid gap-3 sm:grid-cols-4">
            <AppInput v-model="street" label="Rua / Logradouro" required placeholder="Rua Harmonia" class="sm:col-span-3" />
            <AppInput v-model="number" label="Número" required placeholder="450" />
          </div>

          <div class="grid gap-3 sm:grid-cols-3">
            <AppInput v-model="neighborhood" label="Bairro" required placeholder="Vila Madalena" />
            <AppInput v-model="city" label="Cidade" required placeholder="São Paulo" />
            <AppInput v-model="state" label="UF" required maxlength="2" placeholder="SP" input-class="uppercase" />
          </div>

          <AppInput v-model="complement" label="Complemento (opcional)" placeholder="Loja 1, Fundos" />
        </div>

        <div class="space-y-1.5">
          <label class="block text-xs font-semibold text-ink-secondary">Descrição da cozinha</label>
          <textarea
            v-model="description"
            rows="2"
            placeholder="Especialidades, estilo culinário e horário de atendimento..."
            class="w-full rounded-xl border border-line-strong bg-surface px-3.5 py-2.5 text-sm text-ink focus:border-brand-600 focus:outline-none"
          />
        </div>

        <AppButton type="submit" variant="primary" size="lg" block :loading="saving">
          Criar e ativar minha loja
        </AppButton>
      </form>
    </AppCard>
  </div>
</template>
