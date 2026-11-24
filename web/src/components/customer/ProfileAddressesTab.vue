<script setup lang="ts">
import { ref } from 'vue'
import { Edit3, MapPin, Plus, Search, Star, Trash2 } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppInput from '@/components/ui/AppInput.vue'
import AppModal from '@/components/ui/AppModal.vue'
import AppToggle from '@/components/ui/AppToggle.vue'
import AppEmptyState from '@/components/ui/AppEmptyState.vue'
import AppConfirmDialog from '@/components/ui/AppConfirmDialog.vue'
import { api, getErrorMessage } from '@/services/api'
import { useAddressStore } from '@/stores/address'
import { useToastStore } from '@/stores/toast'
import type { Address } from '@/types'

defineProps<{
  addresses: Address[]
}>()

const emit = defineEmits<{
  (e: 'refresh'): void
}>()

const addressStore = useAddressStore()
const toast = useToastStore()

const modalOpen = ref(false)
const editingId = ref<string | null>(null)
const deleteTarget = ref<Address | null>(null)
const saving = ref(false)
const lookingUpCep = ref(false)

const zipCode = ref('')
const street = ref('')
const number = ref('')
const complement = ref('')
const neighborhood = ref('')
const city = ref('')
const state = ref('SP')
const isMain = ref(false)

function openNewModal() {
  editingId.value = null
  zipCode.value = ''
  street.value = ''
  number.value = ''
  complement.value = ''
  neighborhood.value = ''
  city.value = ''
  state.value = 'SP'
  isMain.value = false
  modalOpen.value = true
}

function openEditModal(addr: Address) {
  editingId.value = addr.id
  zipCode.value = addr.zipCode
  street.value = addr.street
  number.value = addr.number
  complement.value = addr.complement || ''
  neighborhood.value = addr.neighborhood
  city.value = addr.city
  state.value = addr.state
  isMain.value = addr.isMain
  modalOpen.value = true
}

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
    toast.info('Endereço preenchido automaticamente pelo CEP.')
  } catch {
    // Allow manual completion
  } finally {
    lookingUpCep.value = false
  }
}

async function handleSaveAddress() {
  if (!street.value.trim() || !city.value.trim()) {
    toast.warning('Informe ao menos a rua e a cidade.')
    return
  }

  saving.value = true
  try {
    const payload = {
      street: street.value.trim(),
      number: number.value.trim() || 'S/N',
      complement: complement.value.trim() || null,
      neighborhood: neighborhood.value.trim(),
      city: city.value.trim(),
      state: state.value.trim().toUpperCase() || 'SP',
      zipCode: zipCode.value.trim(),
      isMain: isMain.value,
    }

    if (editingId.value) {
      await api.put(`/usuarios/me/enderecos/${editingId.value}`, payload)
      toast.success('Endereço atualizado!')
    } else {
      await api.post('/usuarios/me/enderecos', payload)
      toast.success('Novo endereço cadastrado!')
    }

    modalOpen.value = false
    await addressStore.fetchAddresses(true)
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Erro ao salvar endereço.'))
  } finally {
    saving.value = false
  }
}

async function handleSetMain(id: string) {
  try {
    await addressStore.setMainAddress(id)
    toast.success('Endereço principal atualizado!')
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Erro ao definir endereço principal.'))
  }
}

async function handleConfirmDelete() {
  if (!deleteTarget.value) return
  try {
    await api.delete(`/usuarios/me/enderecos/${deleteTarget.value.id}`)
    deleteTarget.value = null
    toast.info('Endereço removido.')
    await addressStore.fetchAddresses(true)
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Erro ao remover endereço.'))
  }
}
</script>

<template>
  <div class="space-y-5">
    <div class="flex flex-col justify-between gap-3 sm:flex-row sm:items-center">
      <div>
        <h2 class="font-display text-lg font-extrabold text-ink">Endereços de entrega</h2>
        <p class="text-xs text-ink-muted">Gerencie os locais onde você recebe suas marmitas.</p>
      </div>
      <AppButton variant="primary" size="sm" @click="openNewModal">
        <Plus class="h-4 w-4" />
        Novo endereço
      </AppButton>
    </div>

    <div v-if="addresses.length > 0" class="grid gap-4 sm:grid-cols-2">
      <AppCard
        v-for="addr in addresses"
        :key="addr.id"
        padding="md"
        class="flex flex-col justify-between"
        :class="addr.isMain ? 'border-brand-400/80 ring-1 ring-brand-500/20' : ''"
      >
        <div>
          <div class="flex items-start justify-between gap-2">
            <div class="flex items-start gap-2.5">
              <span
                class="mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-xl"
                :class="
                  addr.isMain
                    ? 'bg-brand-50 text-brand-600 dark:bg-brand-950/60 dark:text-brand-400'
                    : 'bg-surface-muted text-ink-muted'
                "
              >
                <MapPin class="h-4 w-4" />
              </span>
              <div>
                <p class="font-display text-sm font-extrabold text-ink">
                  {{ addr.street }}, {{ addr.number }}
                </p>
                <p v-if="addr.complement" class="text-xs text-ink-muted">{{ addr.complement }}</p>
              </div>
            </div>
            <AppBadge v-if="addr.isMain" variant="brand" size="xs">Principal</AppBadge>
          </div>

          <p class="mt-3 text-xs text-ink-secondary">
            {{ addr.neighborhood }} — {{ addr.city }}/{{ addr.state }}
          </p>
          <p class="text-[11px] text-ink-muted">CEP: {{ addr.zipCode }}</p>
        </div>

        <div class="mt-4 flex items-center justify-between gap-2 border-t border-line pt-3">
          <div class="flex items-center gap-2">
            <button
              v-if="!addr.isMain"
              type="button"
              class="inline-flex items-center gap-1 text-xs font-bold text-brand-600 hover:underline dark:text-brand-400"
              @click="handleSetMain(addr.id)"
            >
              <Star class="h-3.5 w-3.5" />
              Tornar principal
            </button>
            <button
              type="button"
              class="inline-flex items-center gap-1 text-xs font-semibold text-ink-secondary hover:text-ink"
              @click="openEditModal(addr)"
            >
              <Edit3 class="h-3.5 w-3.5" />
              Editar
            </button>
          </div>

          <button
            type="button"
            aria-label="Excluir endereço"
            class="rounded-lg p-1.5 text-ink-muted transition hover:bg-red-50 hover:text-red-600 dark:hover:bg-red-950/40"
            @click="deleteTarget = addr"
          >
            <Trash2 class="h-4 w-4" />
          </button>
        </div>
      </AppCard>
    </div>

    <AppEmptyState
      v-else
      :icon="MapPin"
      title="Nenhum endereço cadastrado"
      description="Adicione seu endereço com preenchimento automático por CEP para agilizar seus pedidos."
    >
      <template #action>
        <AppButton variant="primary" size="sm" @click="openNewModal">
          <Plus class="h-4 w-4" />
          Cadastrar endereço
        </AppButton>
      </template>
    </AppEmptyState>

    <!-- Create / Edit Address Modal -->
    <AppModal
      :open="modalOpen"
      :title="editingId ? 'Editar endereço' : 'Adicionar endereço'"
      subtitle="Digite o CEP para preencher automaticamente rua, bairro e cidade."
      max-width="lg"
      @close="modalOpen = false"
    >
      <form id="address-form" class="space-y-4" @submit.prevent="handleSaveAddress">
        <div class="flex items-end gap-2">
          <AppInput
            v-model="zipCode"
            label="CEP"
            placeholder="00000-000"
            class="flex-1"
            @blur="handleLookupCep"
          />
          <AppButton
            type="button"
            variant="outline"
            size="md"
            :loading="lookingUpCep"
            @click="handleLookupCep"
          >
            <Search class="h-4 w-4" />
            Buscar CEP
          </AppButton>
        </div>

        <div class="grid gap-3 sm:grid-cols-3">
          <AppInput
            v-model="street"
            label="Rua / Logradouro"
            required
            placeholder="Av. Paulista"
            class="sm:col-span-2"
          />
          <AppInput v-model="number" label="Número" required placeholder="1000" />
        </div>

        <div class="grid gap-3 sm:grid-cols-2">
          <AppInput v-model="complement" label="Complemento" placeholder="Apto 42, Bloco B" />
          <AppInput v-model="neighborhood" label="Bairro" placeholder="Bela Vista" />
        </div>

        <div class="grid gap-3 sm:grid-cols-3">
          <AppInput v-model="city" label="Cidade" required placeholder="São Paulo" class="sm:col-span-2" />
          <AppInput v-model="state" label="UF" maxlength="2" placeholder="SP" input-class="uppercase" />
        </div>

        <div class="rounded-xl border border-line bg-surface-subtle p-3.5">
          <AppToggle
            v-model="isMain"
            label="Definir como endereço principal"
            description="Usado como padrão no cabeçalho e no checkout."
          />
        </div>
      </form>

      <template #footer>
        <div class="flex justify-end gap-2">
          <AppButton variant="outline" size="sm" @click="modalOpen = false">Cancelar</AppButton>
          <AppButton type="submit" form="address-form" variant="primary" size="sm" :loading="saving">
            {{ editingId ? 'Salvar alterações' : 'Adicionar endereço' }}
          </AppButton>
        </div>
      </template>
    </AppModal>

    <AppConfirmDialog
      :open="Boolean(deleteTarget)"
      title="Remover endereço?"
      :message="`Deseja excluir o endereço '${deleteTarget?.street}, ${deleteTarget?.number}' da sua conta?`"
      confirm-label="Sim, excluir"
      variant="danger"
      @confirm="handleConfirmDelete"
      @cancel="deleteTarget = null"
    />
  </div>
</template>
