<script setup lang="ts">
import { ref } from 'vue'
import { Plus, Store, Tag, Trash2 } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppInput from '@/components/ui/AppInput.vue'
import AppDrawer from '@/components/ui/AppDrawer.vue'
import AppEmptyState from '@/components/ui/AppEmptyState.vue'
import AppConfirmDialog from '@/components/ui/AppConfirmDialog.vue'
import OrderStatusBadge from '@/components/ui/OrderStatusBadge.vue'
import { api, getErrorMessage } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import type { Category, Restaurant, User } from '@/types'

const props = defineProps<{
  restaurants: Restaurant[]
  categories: Category[]
  users: User[]
}>()

const emit = defineEmits<{
  (e: 'refresh'): void
}>()

const toast = useToastStore()

const drawerOpen = ref(false)
const restName = ref('')
const restDocument = ref('')
const restDescription = ref('')
const restOwnerId = ref('')
const restZipCode = ref('')
const restStreet = ref('')
const restNumber = ref('')
const restNeighborhood = ref('')
const restCity = ref('São Paulo')
const restState = ref('SP')
const savingRest = ref(false)

const catName = ref('')
const savingCat = ref(false)

const deleteRestTarget = ref<Restaurant | null>(null)
const deleteCatTarget = ref<Category | null>(null)

function openCreateDrawer() {
  restOwnerId.value = props.users[0]?.id || ''
  drawerOpen.value = true
}

function getOwnerName(ownerId: string): string {
  const found = props.users.find((u) => u.id === ownerId)
  return found ? `${found.name} (${found.email})` : ownerId
}

async function handleCreateRestaurant() {
  if (!restName.value.trim() || !restDocument.value.trim()) {
    toast.warning('Informe nome e CNPJ do restaurante.')
    return
  }

  savingRest.value = true
  try {
    await api.post('/restaurantes', {
      name: restName.value.trim(),
      document: restDocument.value.trim(),
      description: restDescription.value.trim(),
      ownerId: restOwnerId.value || undefined,
      addressZipCode: restZipCode.value.trim() || '01001-000',
      addressStreet: restStreet.value.trim() || 'Av. Paulista',
      addressNumber: restNumber.value.trim() || '1000',
      addressNeighborhood: restNeighborhood.value.trim() || 'Bela Vista',
      addressCity: restCity.value.trim() || 'São Paulo',
      addressState: restState.value.trim().toUpperCase() || 'SP',
    })
    restName.value = ''
    restDocument.value = ''
    restDescription.value = ''
    drawerOpen.value = false
    toast.success('Restaurante cadastrado e aprovado!')
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível cadastrar o restaurante.'))
  } finally {
    savingRest.value = false
  }
}

async function handleChangeStatus(id: string, status: string) {
  try {
    await api.patch(`/restaurantes/${id}/status`, { status })
    toast.success(`Restaurante atualizado para ${status}.`)
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível atualizar o status.'))
  }
}

async function handleConfirmDeleteRest() {
  if (!deleteRestTarget.value) return
  try {
    await api.delete(`/restaurantes/${deleteRestTarget.value.id}`)
    deleteRestTarget.value = null
    toast.info('Restaurante removido.')
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Erro ao remover restaurante.'))
  }
}

async function handleCreateCategory() {
  if (!catName.value.trim()) return
  savingCat.value = true
  try {
    await api.post('/categorias', {
      name: catName.value.trim(),
      displayOrder: props.categories.length + 1,
    })
    catName.value = ''
    toast.success('Categoria criada!')
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível criar a categoria.'))
  } finally {
    savingCat.value = false
  }
}

async function handleConfirmDeleteCat() {
  if (!deleteCatTarget.value) return
  try {
    await api.delete(`/categorias/${deleteCatTarget.value.id}`)
    deleteCatTarget.value = null
    toast.info('Categoria removida.')
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Erro ao remover categoria.'))
  }
}
</script>

<template>
  <div class="grid gap-6 lg:grid-cols-3 items-start">
    <!-- Restaurants List -->
    <AppCard padding="lg" class="lg:col-span-2 space-y-4">
      <div class="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 class="font-display text-base font-extrabold text-ink">
            Restaurantes Cadastrados ({{ restaurants.length }})
          </h2>
          <p class="text-xs text-ink-muted">Aprove, pause ou cadastre novos parceiros.</p>
        </div>
        <AppButton variant="primary" size="sm" @click="openCreateDrawer">
          <Plus class="h-4 w-4" />
          Novo restaurante
        </AppButton>
      </div>

      <div v-if="restaurants.length > 0" class="space-y-3">
        <div
          v-for="rest in restaurants"
          :key="rest.id"
          class="flex flex-col justify-between gap-3 rounded-2xl border border-line bg-surface-subtle p-4 sm:flex-row sm:items-center"
        >
          <div class="min-w-0 space-y-1">
            <div class="flex flex-wrap items-center gap-2">
              <h3 class="font-display text-sm font-extrabold text-ink">{{ rest.name }}</h3>
              <OrderStatusBadge :status="rest.status" />
            </div>
            <p class="text-xs text-ink-muted truncate">
              CNPJ: {{ rest.document }} · Dono: {{ getOwnerName(rest.ownerId) }}
            </p>
            <p v-if="rest.description" class="text-xs text-ink-secondary line-clamp-1">
              {{ rest.description }}
            </p>
          </div>

          <div class="flex shrink-0 items-center gap-2">
            <AppButton
              v-if="rest.status !== 'Approved'"
              variant="fresh"
              size="xs"
              @click="handleChangeStatus(rest.id, 'Approved')"
            >
              Aprovar
            </AppButton>
            <AppButton
              v-if="rest.status === 'Approved'"
              variant="outline"
              size="xs"
              @click="handleChangeStatus(rest.id, 'Rejected')"
            >
              Pausar
            </AppButton>
            <button
              type="button"
              aria-label="Remover restaurante"
              class="rounded-lg border border-red-200 p-1.5 text-red-600 hover:bg-red-50 dark:border-red-900/60 dark:text-red-400"
              @click="deleteRestTarget = rest"
            >
              <Trash2 class="h-4 w-4" />
            </button>
          </div>
        </div>
      </div>

      <AppEmptyState v-else :icon="Store" title="Nenhum restaurante encontrado" />
    </AppCard>

    <!-- Menu Categories -->
    <AppCard padding="lg" class="space-y-4">
      <div class="flex items-center gap-2">
        <Tag class="h-5 w-5 text-brand-600" />
        <h2 class="font-display text-base font-extrabold text-ink">Categorias do Cardápio</h2>
      </div>

      <form class="flex gap-2" @submit.prevent="handleCreateCategory">
        <input
          v-model="catName"
          type="text"
          required
          placeholder="Ex: Fitness, Tradicional"
          class="flex-1 rounded-xl border border-line-strong bg-surface px-3 py-2 text-xs text-ink"
        />
        <AppButton type="submit" variant="secondary" size="sm" :loading="savingCat">
          <Plus class="h-4 w-4" />
        </AppButton>
      </form>

      <ul class="divide-y divide-line text-sm">
        <li
          v-for="cat in categories"
          :key="cat.id"
          class="flex items-center justify-between py-2.5"
        >
          <span class="font-semibold text-ink">{{ cat.name }}</span>
          <button
            type="button"
            aria-label="Remover categoria"
            class="rounded-lg p-1 text-ink-muted hover:bg-red-50 hover:text-red-600"
            @click="deleteCatTarget = cat"
          >
            <Trash2 class="h-4 w-4" />
          </button>
        </li>
      </ul>
    </AppCard>

    <!-- Create Restaurant Drawer -->
    <AppDrawer
      :open="drawerOpen"
      title="Cadastrar Restaurante"
      subtitle="Novo parceiro já entra aprovado na vitrine."
      width-class="max-w-md"
      @close="drawerOpen = false"
    >
      <form id="admin-rest-form" class="space-y-4" @submit.prevent="handleCreateRestaurant">
        <AppInput v-model="restName" label="Nome do restaurante" required placeholder="Marmitaria Central" />
        <AppInput v-model="restDocument" label="CNPJ" required placeholder="00.000.000/0001-00" />
        <div class="space-y-1.5">
          <label class="block text-xs font-semibold text-ink-secondary">Proprietário</label>
          <select
            v-model="restOwnerId"
            class="w-full rounded-xl border border-line-strong bg-surface px-3 py-2.5 text-sm text-ink"
          >
            <option v-for="u in users" :key="u.id" :value="u.id">{{ u.name }} ({{ u.email }})</option>
          </select>
        </div>
        <AppInput v-model="restDescription" label="Descrição" placeholder="Especialidade da cozinha" />
        <div class="grid grid-cols-2 gap-3">
          <AppInput v-model="restZipCode" label="CEP" placeholder="01001-000" />
          <AppInput v-model="restNumber" label="Número" placeholder="100" />
        </div>
        <AppInput v-model="restStreet" label="Rua / Logradouro" placeholder="Av. Paulista" />
        <div class="grid grid-cols-3 gap-2">
          <AppInput v-model="restNeighborhood" label="Bairro" placeholder="Centro" />
          <AppInput v-model="restCity" label="Cidade" />
          <AppInput v-model="restState" label="UF" maxlength="2" input-class="uppercase" />
        </div>
      </form>

      <template #footer>
        <div class="flex justify-end gap-2">
          <AppButton variant="outline" size="sm" @click="drawerOpen = false">Cancelar</AppButton>
          <AppButton type="submit" form="admin-rest-form" variant="primary" size="sm" :loading="savingRest">
            Cadastrar restaurante
          </AppButton>
        </div>
      </template>
    </AppDrawer>

    <AppConfirmDialog
      :open="Boolean(deleteRestTarget)"
      title="Remover restaurante?"
      :message="`Confirma a remoção do restaurante '${deleteRestTarget?.name}'?`"
      confirm-label="Sim, remover"
      variant="danger"
      @confirm="handleConfirmDeleteRest"
      @cancel="deleteRestTarget = null"
    />

    <AppConfirmDialog
      :open="Boolean(deleteCatTarget)"
      title="Remover categoria?"
      :message="`Deseja remover a categoria '${deleteCatTarget?.name}'?`"
      confirm-label="Sim, remover"
      variant="danger"
      @confirm="handleConfirmDeleteCat"
      @cancel="deleteCatTarget = null"
    />
  </div>
</template>
