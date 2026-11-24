<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { Plus, Store, Tag, Trash2 } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppInput from '@/components/ui/AppInput.vue'
import AppDrawer from '@/components/ui/AppDrawer.vue'
import AppEmptyState from '@/components/ui/AppEmptyState.vue'
import AppConfirmDialog from '@/components/ui/AppConfirmDialog.vue'
import OrderStatusBadge from '@/components/ui/OrderStatusBadge.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import type { Category, Restaurant, User } from '@/types'

const props = defineProps<{
  restaurants: Restaurant[]
  categories: Category[]
  users: User[]
  focusedItemId?: string | null
}>()

const emit = defineEmits<{
  (e: 'refresh'): void
  (e: 'clear-focus'): void
}>()

const toast = useToastStore()

const createDrawerOpen = ref(false)
const inspectorRest = ref<Restaurant | null>(null)

const restName = ref('')
const restDocument = ref('')
const restDescription = ref('')
const restOwnerId = ref('')
const restZipCode = ref('')
const restStreet = ref('')
const restNumber = ref('')
const restNeighborhood = ref('')
const restCity = ref('Blumenau')
const restState = ref('SC')
const savingRest = ref(false)

const catName = ref('')
const savingCat = ref(false)

const deleteRestTarget = ref<Restaurant | null>(null)
const deleteCatTarget = ref<Category | null>(null)

watch(
  () => props.focusedItemId,
  (id) => {
    if (!id) return
    const match = props.restaurants.find((r) => r.id === id)
    if (match) {
      inspectorRest.value = match
      emit('clear-focus')
    }
  },
  { immediate: true },
)

const inspectorOwner = computed(() => {
  if (!inspectorRest.value) return ''
  const found = props.users.find((u) => u.id === inspectorRest.value?.ownerId)
  return found ? `${found.name} (${found.email})` : inspectorRest.value.ownerId
})

function openCreateDrawer() {
  restOwnerId.value = props.users[0]?.id || ''
  createDrawerOpen.value = true
}

function getOwnerShort(ownerId: string): string {
  const found = props.users.find((u) => u.id === ownerId)
  return found ? found.name : ownerId.slice(0, 8)
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
      addressZipCode: restZipCode.value.trim() || '89010-001',
      addressStreet: restStreet.value.trim() || 'Rua XV de Novembro',
      addressNumber: restNumber.value.trim() || '450',
      addressNeighborhood: restNeighborhood.value.trim() || 'Centro',
      addressCity: restCity.value.trim() || 'Blumenau',
      addressState: restState.value.trim().toUpperCase() || 'SC',
    })
    restName.value = ''
    restDocument.value = ''
    restDescription.value = ''
    createDrawerOpen.value = false
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
    if (inspectorRest.value && inspectorRest.value.id === id) {
      inspectorRest.value.status = status
    }
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
    if (inspectorRest.value?.id === deleteRestTarget.value.id) {
      inspectorRest.value = null
    }
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
  <div class="grid grid-cols-1 gap-6 lg:grid-cols-3 items-start">
    <!-- Master Table of Restaurants -->
    <AppCard padding="lg" class="lg:col-span-2 space-y-4">
      <div class="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 class="font-display text-base font-extrabold text-ink">
            Restaurantes Cadastrados ({{ restaurants.length }})
          </h2>
          <p class="text-xs text-ink-muted">Clique em qualquer linha para abrir o Inspector de detalhes e ações.</p>
        </div>
        <AppButton variant="primary" size="sm" @click="openCreateDrawer">
          <Plus class="h-4 w-4" />
          Novo restaurante
        </AppButton>
      </div>

      <div v-if="restaurants.length > 0" class="overflow-x-auto rounded-2xl border border-line">
        <table class="w-full text-left text-xs">
          <thead class="border-b border-line bg-surface-subtle text-[11px] font-bold uppercase tracking-wider text-ink-muted">
            <tr>
              <th class="px-4 py-3">Restaurante</th>
              <th class="px-4 py-3">CNPJ</th>
              <th class="px-4 py-3 hidden sm:table-cell">Proprietário</th>
              <th class="px-4 py-3">Status</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-line">
            <tr
              v-for="rest in restaurants"
              :key="rest.id"
              class="cursor-pointer transition hover:bg-surface-muted/60"
              @click="inspectorRest = rest"
            >
              <td class="px-4 py-3 font-bold text-ink">{{ rest.name }}</td>
              <td class="px-4 py-3 font-mono text-ink-secondary">{{ rest.document }}</td>
              <td class="px-4 py-3 hidden sm:table-cell text-ink-secondary">{{ getOwnerShort(rest.ownerId) }}</td>
              <td class="px-4 py-3">
                <OrderStatusBadge :status="rest.status" />
              </td>
            </tr>
          </tbody>
        </table>
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

    <!-- Restaurant Detail Inspector Drawer -->
    <AppDrawer
      :open="Boolean(inspectorRest)"
      :title="inspectorRest?.name"
      :subtitle="`CNPJ: ${inspectorRest?.document}`"
      width-class="max-w-md"
      @close="inspectorRest = null"
    >
      <div v-if="inspectorRest" class="space-y-5 text-xs">
        <div class="flex items-center justify-between rounded-2xl border border-line bg-surface-subtle p-4">
          <span class="font-bold text-ink-secondary">Status na Vitrine</span>
          <OrderStatusBadge :status="inspectorRest.status" />
        </div>

        <div class="space-y-2 rounded-2xl border border-line bg-surface-subtle p-4">
          <p class="font-bold uppercase tracking-wider text-[10px] text-ink-muted">Dados & Endereço</p>
          <p class="text-ink-secondary"><strong class="text-ink">Proprietário:</strong> {{ inspectorOwner }}</p>
          <p class="text-ink-secondary"><strong class="text-ink">Descrição:</strong> {{ inspectorRest.description || 'N/A' }}</p>
          <p class="text-ink-secondary">
            <strong class="text-ink">Endereço:</strong>
            {{ inspectorRest.addressStreet || 'N/A' }}, {{ inspectorRest.addressNumber }} —
            {{ inspectorRest.addressNeighborhood }} ({{ inspectorRest.addressCity }}/{{ inspectorRest.addressState }})
          </p>
          <p class="text-ink-secondary">
            <strong class="text-ink">Taxa base:</strong> {{ formatCurrency(inspectorRest.deliveryFee ?? 0) }}
            · <strong class="text-ink">Raio:</strong> {{ inspectorRest.deliveryRadiusKm ?? 8 }} km
          </p>
        </div>

        <div class="space-y-2 pt-2">
          <AppButton
            v-if="inspectorRest.status !== 'Approved'"
            variant="fresh"
            size="md"
            block
            @click="handleChangeStatus(inspectorRest.id, 'Approved')"
          >
            Aprovar restaurante
          </AppButton>
          <AppButton
            v-if="inspectorRest.status === 'Approved'"
            variant="outline"
            size="md"
            block
            @click="handleChangeStatus(inspectorRest.id, 'Rejected')"
          >
            Pausar restaurante
          </AppButton>
          <AppButton
            variant="danger"
            size="md"
            block
            @click="deleteRestTarget = inspectorRest"
          >
            <Trash2 class="h-4 w-4" />
            Remover restaurante
          </AppButton>
        </div>
      </div>
    </AppDrawer>

    <!-- Create Restaurant Drawer -->
    <AppDrawer
      :open="createDrawerOpen"
      title="Cadastrar Restaurante"
      subtitle="Novo parceiro já entra aprovado na vitrine."
      width-class="max-w-md"
      @close="createDrawerOpen = false"
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
          <AppInput v-model="restZipCode" label="CEP" placeholder="89010-001" />
          <AppInput v-model="restNumber" label="Número" placeholder="450" />
        </div>
        <AppInput v-model="restStreet" label="Rua / Logradouro" placeholder="Rua XV de Novembro" />
        <div class="grid grid-cols-3 gap-2">
          <AppInput v-model="restNeighborhood" label="Bairro" placeholder="Centro" />
          <AppInput v-model="restCity" label="Cidade" />
          <AppInput v-model="restState" label="UF" maxlength="2" input-class="uppercase" />
        </div>
      </form>

      <template #footer>
        <div class="flex justify-end gap-2">
          <AppButton variant="outline" size="sm" @click="createDrawerOpen = false">Cancelar</AppButton>
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
