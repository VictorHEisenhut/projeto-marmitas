<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { Bike, CheckCircle2, FileText, MapPin, Search, Upload } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppInput from '@/components/ui/AppInput.vue'
import { api, getErrorMessage, uploadFile } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import type { CourierBillingAddress, CourierDocument, CourierVehicle } from '@/types'

const props = defineProps<{
  documents: CourierDocument[]
  vehicles: CourierVehicle[]
  billing: CourierBillingAddress | null
  hasCpf: boolean
  hasCnh: boolean
  hasVehicle: boolean
  hasBillingAddress: boolean
}>()

const emit = defineEmits<{
  (e: 'refresh'): void
}>()

const toast = useToastStore()

const activeFormTab = ref<'docs' | 'vehicle' | 'billing'>('docs')

const cpfNumber = ref('')
const cnhNumber = ref('')
const cnhFileUrl = ref('')
const uploadingCnh = ref(false)
const savingDocs = ref(false)

const vehicleType = ref('Moto')
const vehicleModel = ref('')
const vehiclePlate = ref('')
const savingVehicle = ref(false)

const billingZipCode = ref('')
const billingStreet = ref('')
const billingNumber = ref('')
const billingComplement = ref('')
const billingNeighborhood = ref('')
const billingCity = ref('Blumenau')
const billingState = ref('SC')
const savingBilling = ref(false)
const lookingUpCep = ref(false)

watch(
  () => props.documents,
  (docs) => {
    const cpf = docs.find((d) => d.type.toUpperCase() === 'CPF')
    const cnh = docs.find((d) => d.type.toUpperCase() === 'CNH')
    if (cpf && !cpfNumber.value) cpfNumber.value = cpf.number
    if (cnh && !cnhNumber.value) cnhNumber.value = cnh.number
  },
  { immediate: true },
)

watch(
  () => props.billing,
  (b) => {
    if (!b) return
    billingZipCode.value = b.zipCode || ''
    billingStreet.value = b.street || ''
    billingNumber.value = b.number || ''
    billingComplement.value = b.complement || ''
    billingNeighborhood.value = b.neighborhood || ''
    billingCity.value = b.city || 'Blumenau'
    billingState.value = b.state || 'SC'
  },
  { immediate: true },
)

const completedSteps = computed(() => {
  let count = 0
  if (props.hasCpf) count++
  if (props.hasCnh) count++
  if (props.hasVehicle) count++
  if (props.hasBillingAddress) count++
  return count
})

const progressPercent = computed(() => Math.round((completedSteps.value / 4) * 100))

async function handleUploadCnh(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return
  uploadingCnh.value = true
  try {
    const res = await uploadFile(file)
    cnhFileUrl.value = res.url
    toast.success('Arquivo da CNH anexado!')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Erro ao enviar arquivo da CNH.'))
  } finally {
    uploadingCnh.value = false
  }
}

async function handleSaveDocs() {
  if (!cpfNumber.value.trim() || !cnhNumber.value.trim()) {
    toast.warning('Preencha tanto o CPF quanto o número da CNH.')
    return
  }
  savingDocs.value = true
  try {
    await api.post('/entregadores/me/documentos', { type: 'CPF', number: cpfNumber.value.trim() })
    await api.post('/entregadores/me/documentos', { type: 'CNH', number: cnhNumber.value.trim() })
    toast.success('CPF e CNH salvos com sucesso!')
    emit('refresh')
    if (!props.hasVehicle) activeFormTab.value = 'vehicle'
  } catch (err) {
    toast.error(getErrorMessage(err, 'Erro ao salvar documentos.'))
  } finally {
    savingDocs.value = false
  }
}

async function handleAddVehicle() {
  if (!vehicleModel.value.trim() || !vehiclePlate.value.trim()) {
    toast.warning('Informe modelo e placa do veículo.')
    return
  }
  savingVehicle.value = true
  try {
    await api.post('/entregadores/me/veiculos', {
      type: vehicleType.value,
      model: vehicleModel.value.trim(),
      licensePlate: vehiclePlate.value.trim().toUpperCase(),
    })
    vehicleModel.value = ''
    vehiclePlate.value = ''
    toast.success('Veículo cadastrado com sucesso!')
    emit('refresh')
    if (!props.hasBillingAddress) activeFormTab.value = 'billing'
  } catch (err) {
    toast.error(getErrorMessage(err, 'Erro ao cadastrar veículo.'))
  } finally {
    savingVehicle.value = false
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
    // Allow manual completion
  } finally {
    lookingUpCep.value = false
  }
}

async function handleSaveBilling() {
  if (
    !billingZipCode.value.trim() ||
    !billingStreet.value.trim() ||
    !billingNumber.value.trim() ||
    !billingNeighborhood.value.trim() ||
    !billingCity.value.trim() ||
    !billingState.value.trim()
  ) {
    toast.warning('Preencha todos os campos obrigatórios do endereço de cobrança.')
    return
  }
  savingBilling.value = true
  try {
    await api.put('/entregadores/me/endereco-cobranca', {
      zipCode: billingZipCode.value.trim(),
      street: billingStreet.value.trim(),
      number: billingNumber.value.trim(),
      complement: billingComplement.value.trim() || null,
      neighborhood: billingNeighborhood.value.trim(),
      city: billingCity.value.trim(),
      state: billingState.value.trim().toUpperCase(),
    })
    toast.success('Endereço de cobrança salvo!')
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Erro ao salvar endereço de cobrança.'))
  } finally {
    savingBilling.value = false
  }
}
</script>

<template>
  <div class="space-y-5">
    <!-- 4-Step Progress Summary -->
    <AppCard padding="md" class="space-y-4">
      <div class="flex flex-wrap items-center justify-between gap-2">
        <div>
          <h2 class="font-display text-base font-extrabold text-ink">
            Homologação Obrigatória ({{ progressPercent }}%)
          </h2>
          <p class="text-xs text-ink-muted">
            Complete os 4 requisitos abaixo para liberar a fila de corridas em tempo real.
          </p>
        </div>
        <AppBadge :variant="completedSteps === 4 ? 'fresh' : 'warning'" size="sm" dot>
          {{ completedSteps === 4 ? 'Liberado para Entregas' : `${completedSteps} de 4 concluídos` }}
        </AppBadge>
      </div>

      <div class="h-2.5 w-full overflow-hidden rounded-full bg-surface-muted">
        <div
          class="h-full rounded-full transition-all duration-500"
          :class="completedSteps === 4 ? 'bg-fresh-600' : 'bg-brand-600'"
          :style="{ width: `${progressPercent}%` }"
        />
      </div>

      <div class="grid grid-cols-2 gap-2.5 sm:grid-cols-4">
        <button
          type="button"
          class="flex items-center justify-between rounded-xl border px-3 py-2.5 text-left text-xs font-bold transition"
          :class="
            hasCpf
              ? 'border-fresh-200 bg-fresh-50/70 text-fresh-800 dark:border-fresh-800/60 dark:bg-fresh-950/35 dark:text-fresh-300'
              : 'border-amber-200 bg-amber-50/70 text-amber-900 dark:border-amber-800/60 dark:bg-amber-950/35 dark:text-amber-300'
          "
          @click="activeFormTab = 'docs'"
        >
          <span>1. CPF</span>
          <CheckCircle2 v-if="hasCpf" class="h-4 w-4 text-fresh-600" />
          <span v-else class="text-[10px]">Pendente</span>
        </button>

        <button
          type="button"
          class="flex items-center justify-between rounded-xl border px-3 py-2.5 text-left text-xs font-bold transition"
          :class="
            hasCnh
              ? 'border-fresh-200 bg-fresh-50/70 text-fresh-800 dark:border-fresh-800/60 dark:bg-fresh-950/35 dark:text-fresh-300'
              : 'border-amber-200 bg-amber-50/70 text-amber-900 dark:border-amber-800/60 dark:bg-amber-950/35 dark:text-amber-300'
          "
          @click="activeFormTab = 'docs'"
        >
          <span>2. CNH</span>
          <CheckCircle2 v-if="hasCnh" class="h-4 w-4 text-fresh-600" />
          <span v-else class="text-[10px]">Pendente</span>
        </button>

        <button
          type="button"
          class="flex items-center justify-between rounded-xl border px-3 py-2.5 text-left text-xs font-bold transition"
          :class="
            hasVehicle
              ? 'border-fresh-200 bg-fresh-50/70 text-fresh-800 dark:border-fresh-800/60 dark:bg-fresh-950/35 dark:text-fresh-300'
              : 'border-amber-200 bg-amber-50/70 text-amber-900 dark:border-amber-800/60 dark:bg-amber-950/35 dark:text-amber-300'
          "
          @click="activeFormTab = 'vehicle'"
        >
          <span>3. Veículo</span>
          <CheckCircle2 v-if="hasVehicle" class="h-4 w-4 text-fresh-600" />
          <span v-else class="text-[10px]">Pendente</span>
        </button>

        <button
          type="button"
          class="flex items-center justify-between rounded-xl border px-3 py-2.5 text-left text-xs font-bold transition"
          :class="
            hasBillingAddress
              ? 'border-fresh-200 bg-fresh-50/70 text-fresh-800 dark:border-fresh-800/60 dark:bg-fresh-950/35 dark:text-fresh-300'
              : 'border-amber-200 bg-amber-50/70 text-amber-900 dark:border-amber-800/60 dark:bg-amber-950/35 dark:text-amber-300'
          "
          @click="activeFormTab = 'billing'"
        >
          <span>4. Cobrança</span>
          <CheckCircle2 v-if="hasBillingAddress" class="h-4 w-4 text-fresh-600" />
          <span v-else class="text-[10px]">Pendente</span>
        </button>
      </div>
    </AppCard>

    <!-- Tab Selector -->
    <div class="grid grid-cols-3 gap-2 rounded-2xl bg-surface-muted p-1.5">
      <button
        type="button"
        class="inline-flex h-11 items-center justify-center gap-1.5 rounded-xl text-xs font-bold transition"
        :class="activeFormTab === 'docs' ? 'bg-surface text-ink shadow-sm' : 'text-ink-muted hover:text-ink'"
        @click="activeFormTab = 'docs'"
      >
        <FileText class="h-4 w-4" />
        <span>CPF & CNH</span>
      </button>
      <button
        type="button"
        class="inline-flex h-11 items-center justify-center gap-1.5 rounded-xl text-xs font-bold transition"
        :class="activeFormTab === 'vehicle' ? 'bg-surface text-ink shadow-sm' : 'text-ink-muted hover:text-ink'"
        @click="activeFormTab = 'vehicle'"
      >
        <Bike class="h-4 w-4" />
        <span>Veículo</span>
      </button>
      <button
        type="button"
        class="inline-flex h-11 items-center justify-center gap-1.5 rounded-xl text-xs font-bold transition"
        :class="activeFormTab === 'billing' ? 'bg-surface text-ink shadow-sm' : 'text-ink-muted hover:text-ink'"
        @click="activeFormTab = 'billing'"
      >
        <MapPin class="h-4 w-4" />
        <span>Cobrança</span>
      </button>
    </div>

    <!-- Form 1: CPF & CNH -->
    <AppCard v-if="activeFormTab === 'docs'" padding="lg" class="space-y-4">
      <h3 class="font-display text-base font-extrabold text-ink">Documentos Obrigatórios</h3>

      <ul v-if="documents.length > 0" class="space-y-2 text-xs">
        <li
          v-for="doc in documents"
          :key="doc.id"
          class="flex items-center justify-between rounded-xl border border-line bg-surface-subtle px-3.5 py-2.5"
        >
          <span class="font-bold text-ink">{{ doc.type }}: {{ doc.number }}</span>
          <AppBadge variant="fresh" size="xs">Registrado</AppBadge>
        </li>
      </ul>

      <form class="space-y-4" @submit.prevent="handleSaveDocs">
        <div class="grid gap-4 sm:grid-cols-2">
          <AppInput v-model="cpfNumber" label="CPF" required placeholder="000.000.000-00" />
          <AppInput v-model="cnhNumber" label="Número da CNH" required placeholder="00000000000" />
        </div>

        <div class="space-y-1.5">
          <label class="block text-xs font-semibold text-ink-secondary">Anexo da CNH (Foto ou PDF)</label>
          <div class="flex flex-wrap items-center gap-2">
            <input
              v-model="cnhFileUrl"
              type="text"
              readonly
              placeholder="Nenhum arquivo anexado"
              class="flex-1 rounded-xl border border-line-strong bg-surface-subtle px-3.5 py-2.5 text-xs text-ink-secondary"
            />
            <label class="inline-flex h-10 cursor-pointer items-center gap-1.5 rounded-xl border border-line-strong bg-surface px-4 text-xs font-bold text-ink hover:bg-surface-muted">
              <Upload class="h-4 w-4" />
              <span>{{ uploadingCnh ? 'Enviando...' : 'Anexar CNH' }}</span>
              <input type="file" accept=".jpg,.jpeg,.png,.webp,.pdf" class="hidden" @change="handleUploadCnh" />
            </label>
          </div>
        </div>

        <AppButton type="submit" variant="primary" size="lg" block :loading="savingDocs">
          Salvar CPF e CNH
        </AppButton>
      </form>
    </AppCard>

    <!-- Form 2: Vehicle -->
    <AppCard v-else-if="activeFormTab === 'vehicle'" padding="lg" class="space-y-4">
      <h3 class="font-display text-base font-extrabold text-ink">Meu Veículo de Entrega</h3>

      <ul v-if="vehicles.length > 0" class="space-y-2 text-xs">
        <li
          v-for="v in vehicles"
          :key="v.id"
          class="flex items-center justify-between rounded-xl border border-line bg-surface-subtle px-3.5 py-2.5"
        >
          <span class="font-bold text-ink">{{ v.type }} — {{ v.model }}</span>
          <AppBadge variant="neutral" size="xs">{{ v.licensePlate }}</AppBadge>
        </li>
      </ul>

      <form class="space-y-4" @submit.prevent="handleAddVehicle">
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
          <AppInput v-model="vehicleModel" label="Modelo" required placeholder="Honda CG 160" />
          <AppInput v-model="vehiclePlate" label="Placa" required placeholder="ABC1D23" input-class="uppercase" />
        </div>

        <AppButton type="submit" variant="primary" size="lg" block :loading="savingVehicle">
          Cadastrar veículo
        </AppButton>
      </form>
    </AppCard>

    <!-- Form 3: Billing Address -->
    <AppCard v-else padding="lg" class="space-y-4">
      <div>
        <h3 class="font-display text-base font-extrabold text-ink">
          Endereço de Cobrança / Faturamento
        </h3>
        <p class="text-xs text-ink-muted">
          Exclusivo para cadastro fiscal e repasse financeiro do entregador.
        </p>
      </div>

      <form class="space-y-4" @submit.prevent="handleSaveBilling">
        <div class="flex items-end gap-2">
          <AppInput v-model="billingZipCode" label="CEP" required placeholder="00000-000" class="flex-1" @blur="handleLookupCep" />
          <AppButton type="button" variant="outline" size="md" :loading="lookingUpCep" @click="handleLookupCep">
            <Search class="h-4 w-4" />
            CEP
          </AppButton>
        </div>

        <div class="grid gap-3 sm:grid-cols-4">
          <AppInput v-model="billingStreet" label="Rua / Logradouro" required class="sm:col-span-3" />
          <AppInput v-model="billingNumber" label="Número" required />
        </div>

        <div class="grid gap-3 sm:grid-cols-4">
          <AppInput v-model="billingNeighborhood" label="Bairro" required />
          <AppInput v-model="billingCity" label="Cidade" required />
          <AppInput v-model="billingState" label="UF" required maxlength="2" input-class="uppercase" />
          <AppInput v-model="billingComplement" label="Complemento" />
        </div>

        <AppButton type="submit" variant="primary" size="lg" block :loading="savingBilling">
          Salvar endereço de cobrança
        </AppButton>
      </form>
    </AppCard>
  </div>
</template>
