<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { AlertTriangle, MapPin, Plus, Store, Trash2, Upload, Users } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppInput from '@/components/ui/AppInput.vue'
import { api, formatCurrency, getErrorMessage, uploadFile } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'
import type { DeliveryZone, Restaurant, RestaurantTeamMember } from '@/types'

const props = defineProps<{
  restaurant: Restaurant
  deliveryZones: DeliveryZone[]
  teamMembers: RestaurantTeamMember[]
}>()

const emit = defineEmits<{
  (e: 'refresh'): void
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
const openingHours = ref('Seg a Sáb das 10:30 às 15:30')
const deliveryFee = ref(0)
const deliveryRadiusKm = ref(8)
const pixKey = ref('')
const imageUrl = ref('')

const savingInfo = ref(false)
const uploadingLogo = ref(false)

const newZoneNeighborhood = ref('')
const newZoneFee = ref(5.9)
const newZoneMinutes = ref(30)
const savingZone = ref(false)

const newStaffEmail = ref('')
const savingStaff = ref(false)

watch(
  () => props.restaurant,
  (r) => {
    if (!r) return
    name.value = r.name || ''
    document.value = r.document || ''
    description.value = r.description || ''
    zipCode.value = r.addressZipCode || ''
    street.value = r.addressStreet || ''
    number.value = r.addressNumber || ''
    complement.value = r.addressComplement || ''
    neighborhood.value = r.addressNeighborhood || ''
    city.value = r.addressCity || ''
    state.value = r.addressState || 'SP'
    openingHours.value = r.openingHours || 'Seg a Sáb das 10:30 às 15:30'
    deliveryFee.value = r.deliveryFee ?? 0
    deliveryRadiusKm.value = r.deliveryRadiusKm ?? 8
    pixKey.value = r.pixKey || ''
    imageUrl.value = r.imageUrl || ''
  },
  { immediate: true },
)

const hasIncompleteFiscalInfo = computed(
  () =>
    !document.value.trim() ||
    !street.value.trim() ||
    !number.value.trim() ||
    !neighborhood.value.trim() ||
    !city.value.trim() ||
    !state.value.trim() ||
    !zipCode.value.trim(),
)

async function handleUploadImage(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return
  uploadingLogo.value = true
  try {
    const res = await uploadFile(file)
    imageUrl.value = res.url
    toast.success('Imagem do restaurante enviada!')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Erro ao enviar imagem.'))
  } finally {
    uploadingLogo.value = false
  }
}

async function handleSaveInfo() {
  if (!name.value.trim() || !document.value.trim()) {
    toast.warning('Nome e CNPJ são obrigatórios.')
    return
  }
  if (hasIncompleteFiscalInfo.value) {
    toast.warning('Complete todos os campos obrigatórios de CNPJ e endereço.')
    return
  }

  savingInfo.value = true
  try {
    await api.put(`/restaurantes/${props.restaurant.id}`, {
      name: name.value.trim(),
      document: document.value.trim(),
      description: description.value.trim(),
      ownerId: props.restaurant.ownerId || auth.user?.id,
      addressZipCode: zipCode.value.trim(),
      addressStreet: street.value.trim(),
      addressNumber: number.value.trim(),
      addressComplement: complement.value.trim() || null,
      addressNeighborhood: neighborhood.value.trim(),
      addressCity: city.value.trim(),
      addressState: state.value.trim().toUpperCase(),
      openingHours: openingHours.value.trim() || 'Seg a Sáb das 10:30 às 15:30',
      deliveryFee: Number(deliveryFee.value) || 0,
      deliveryRadiusKm: Number(deliveryRadiusKm.value) || 8,
      pixKey: pixKey.value.trim() || null,
      imageUrl: imageUrl.value.trim() || null,
    })
    toast.success('Configurações da loja atualizadas!')
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível atualizar os dados da loja.'))
  } finally {
    savingInfo.value = false
  }
}

async function handleAddZone() {
  if (!newZoneNeighborhood.value.trim()) return
  savingZone.value = true
  try {
    await api.post(`/restaurantes/${props.restaurant.id}/zonas-entrega`, {
      neighborhood: newZoneNeighborhood.value.trim(),
      city: city.value || 'São Paulo',
      radiusKm: deliveryRadiusKm.value || 8,
      deliveryFee: Number(newZoneFee.value) || 0,
      estimatedMinutes: Number(newZoneMinutes.value) || 30,
    })
    newZoneNeighborhood.value = ''
    toast.success('Zona de entrega adicionada!')
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível adicionar a zona.'))
  } finally {
    savingZone.value = false
  }
}

async function handleDeleteZone(zoneId: string) {
  try {
    await api.delete(`/restaurantes/${props.restaurant.id}/zonas-entrega/${zoneId}`)
    toast.info('Zona de entrega removida.')
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Erro ao remover zona.'))
  }
}

async function handleAddStaff() {
  if (!newStaffEmail.value.trim()) return
  savingStaff.value = true
  try {
    await api.post(`/restaurantes/${props.restaurant.id}/equipe`, {
      email: newStaffEmail.value.trim(),
      role: 'restaurant_staff',
    })
    newStaffEmail.value = ''
    toast.success('Colaborador vinculado à equipe!')
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível vincular colaborador.'))
  } finally {
    savingStaff.value = false
  }
}

async function handleRemoveStaff(memberId: string) {
  try {
    await api.delete(`/restaurantes/${props.restaurant.id}/equipe/${memberId}`)
    toast.info('Colaborador removido da equipe.')
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Erro ao remover colaborador.'))
  }
}
</script>

<template>
  <div class="space-y-6">
    <!-- Missing Fiscal Info Alert -->
    <div
      v-if="hasIncompleteFiscalInfo"
      class="flex items-start gap-3 rounded-2xl border border-amber-300 bg-amber-50 p-4 text-sm text-amber-900 dark:border-amber-700/60 dark:bg-amber-950/35 dark:text-amber-300"
    >
      <AlertTriangle class="mt-0.5 h-5 w-5 shrink-0" />
      <div>
        <p class="font-bold">Dados obrigatórios pendentes</p>
        <p class="text-xs">
          Preencha o CNPJ e o endereço completo do restaurante abaixo para manter a operação e o despacho de entregas habilitados.
        </p>
      </div>
    </div>

    <!-- Store Info Form -->
    <AppCard padding="lg" class="space-y-5">
      <div class="flex items-center gap-2">
        <Store class="h-5 w-5 text-brand-600" />
        <h2 class="font-display text-base font-extrabold text-ink">
          Dados Cadastrais, Capa & Logística
        </h2>
      </div>

      <form class="space-y-4" @submit.prevent="handleSaveInfo">
        <!-- Logo / Cover Preview -->
        <div class="flex flex-col gap-3 rounded-2xl border border-line bg-surface-subtle p-4 sm:flex-row sm:items-center">
          <div class="h-20 w-32 shrink-0 overflow-hidden rounded-xl border border-line bg-surface flex items-center justify-center">
            <img v-if="imageUrl" :src="imageUrl" alt="Capa do restaurante" class="h-full w-full object-cover" />
            <Store v-else class="h-7 w-7 text-ink-muted" />
          </div>
          <div class="flex-1 space-y-2">
            <label class="block text-xs font-semibold text-ink-secondary">
              Logo / Capa da Vitrine
            </label>
            <div class="flex flex-wrap gap-2">
              <input
                v-model="imageUrl"
                type="text"
                placeholder="https://... ou faça upload"
                class="flex-1 min-w-[180px] rounded-xl border border-line-strong bg-surface px-3 py-2 text-xs text-ink"
              />
              <label class="inline-flex cursor-pointer items-center gap-1.5 rounded-xl border border-line-strong bg-surface px-3 py-2 text-xs font-bold text-ink hover:bg-surface-muted">
                <Upload class="h-3.5 w-3.5" />
                <span>{{ uploadingLogo ? 'Enviando...' : 'Upload' }}</span>
                <input type="file" accept=".jpg,.jpeg,.png,.webp" class="hidden" @change="handleUploadImage" />
              </label>
            </div>
          </div>
        </div>

        <div class="grid gap-4 sm:grid-cols-2">
          <AppInput v-model="name" label="Nome do restaurante" required />
          <AppInput v-model="document" label="CNPJ" required placeholder="00.000.000/0001-00" />
        </div>

        <div class="grid gap-3 sm:grid-cols-6">
          <AppInput v-model="zipCode" label="CEP" required class="sm:col-span-2" />
          <AppInput v-model="street" label="Rua / Logradouro" required class="sm:col-span-3" />
          <AppInput v-model="number" label="Número" required class="sm:col-span-1" />
          <AppInput v-model="neighborhood" label="Bairro" required class="sm:col-span-2" />
          <AppInput v-model="city" label="Cidade" required class="sm:col-span-2" />
          <AppInput v-model="state" label="UF" required maxlength="2" class="sm:col-span-1" input-class="uppercase" />
          <AppInput v-model="complement" label="Complemento" class="sm:col-span-1" />
        </div>

        <div class="grid gap-3 sm:grid-cols-4">
          <AppInput v-model="openingHours" label="Horário de funcionamento" class="sm:col-span-2" />
          <AppInput v-model.number="deliveryFee" label="Taxa base (R$)" type="number" step="0.1" min="0" />
          <AppInput v-model.number="deliveryRadiusKm" label="Raio máx. (km)" type="number" step="0.5" min="1" />
        </div>

        <div class="grid gap-4 sm:grid-cols-2">
          <AppInput v-model="pixKey" label="Chave PIX para repasses" placeholder="CNPJ, e-mail ou chave aleatória" />
          <AppInput v-model="description" label="Descrição curta na vitrine" placeholder="Culinária caseira saudável" />
        </div>

        <AppButton type="submit" variant="primary" size="md" :loading="savingInfo">
          Salvar configurações da loja
        </AppButton>
      </form>
    </AppCard>

    <!-- Delivery Zones + Internal Team -->
    <div class="grid gap-6 lg:grid-cols-2">
      <!-- Delivery Zones -->
      <AppCard padding="lg" class="space-y-4">
        <div class="flex items-center gap-2">
          <MapPin class="h-5 w-5 text-brand-600" />
          <h3 class="font-display text-base font-extrabold text-ink">Zonas de Entrega por Bairro</h3>
        </div>

        <form class="grid grid-cols-12 gap-2 items-end" @submit.prevent="handleAddZone">
          <div class="col-span-5">
            <AppInput v-model="newZoneNeighborhood" label="Bairro" required placeholder="Pinheiros" />
          </div>
          <div class="col-span-3">
            <AppInput v-model.number="newZoneFee" label="Taxa R$" type="number" step="0.1" min="0" />
          </div>
          <div class="col-span-2">
            <AppInput v-model.number="newZoneMinutes" label="Min" type="number" min="5" />
          </div>
          <div class="col-span-2">
            <AppButton type="submit" variant="secondary" size="md" block :loading="savingZone">
              <Plus class="h-4 w-4" />
            </AppButton>
          </div>
        </form>

        <ul v-if="deliveryZones.length > 0" class="divide-y divide-line text-xs">
          <li
            v-for="zone in deliveryZones"
            :key="zone.id"
            class="flex items-center justify-between py-2.5"
          >
            <span class="text-ink-secondary">
              <strong class="text-ink">{{ zone.neighborhood }}</strong>
              · {{ formatCurrency(zone.deliveryFee) }} · ~{{ zone.estimatedMinutes }} min
            </span>
            <button
              type="button"
              aria-label="Remover zona"
              class="rounded-lg p-1 text-red-600 hover:bg-red-50 dark:text-red-400"
              @click="handleDeleteZone(zone.id)"
            >
              <Trash2 class="h-3.5 w-3.5" />
            </button>
          </li>
        </ul>
        <p v-else class="text-xs text-ink-muted">Nenhuma zona personalizada cadastrada.</p>
      </AppCard>

      <!-- Internal Team -->
      <AppCard padding="lg" class="space-y-4">
        <div class="flex items-center gap-2">
          <Users class="h-5 w-5 text-brand-600" />
          <h3 class="font-display text-base font-extrabold text-ink">Equipe Interna (Staff)</h3>
        </div>

        <form class="flex items-end gap-2" @submit.prevent="handleAddStaff">
          <AppInput
            v-model="newStaffEmail"
            label="E-mail do colaborador cadastrado"
            type="email"
            required
            placeholder="cozinha@restaurante.com"
            class="flex-1"
          />
          <AppButton type="submit" variant="secondary" size="md" :loading="savingStaff">
            Vincular
          </AppButton>
        </form>

        <ul v-if="teamMembers.length > 0" class="divide-y divide-line text-xs">
          <li
            v-for="member in teamMembers"
            :key="member.id"
            class="flex items-center justify-between py-2.5"
          >
            <div>
              <p class="font-bold text-ink">{{ member.userName }}</p>
              <p class="text-ink-muted">{{ member.userEmail }}</p>
            </div>
            <button
              type="button"
              aria-label="Remover colaborador"
              class="rounded-lg p-1 text-red-600 hover:bg-red-50 dark:text-red-400"
              @click="handleRemoveStaff(member.id)"
            >
              <Trash2 class="h-3.5 w-3.5" />
            </button>
          </li>
        </ul>
        <p v-else class="text-xs text-ink-muted">Nenhum colaborador vinculado ainda.</p>
      </AppCard>
    </div>
  </div>
</template>
