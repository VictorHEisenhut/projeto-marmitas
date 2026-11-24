<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { CheckCircle2, Shield, Truck, XCircle } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppDrawer from '@/components/ui/AppDrawer.vue'
import OrderStatusBadge from '@/components/ui/OrderStatusBadge.vue'
import { api, getErrorMessage } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import type { Courier, User } from '@/types'

const props = defineProps<{
  users: User[]
  couriers: Courier[]
  focusedItemId?: string | null
}>()

const emit = defineEmits<{
  (e: 'refresh'): void
  (e: 'clear-focus'): void
}>()

const toast = useToastStore()

const roleFilter = ref('')
const inspectorUser = ref<User | null>(null)
const selectedRoleForInspector = ref('customer')

const allRoles = [
  { value: 'customer', label: 'customer (Cliente)' },
  { value: 'restaurant_owner', label: 'restaurant_owner (Dono de Restaurante)' },
  { value: 'restaurant_staff', label: 'restaurant_staff (Equipe Restaurante)' },
  { value: 'driver', label: 'driver (Entregador)' },
  { value: 'admin', label: 'admin (Administrador)' },
  { value: 'super_admin', label: 'super_admin (Super Admin)' },
]

watch(
  () => props.focusedItemId,
  (id) => {
    if (!id) return
    const u = props.users.find((x) => x.id === id)
    if (u) {
      openUserInspector(u)
      emit('clear-focus')
    }
  },
  { immediate: true },
)

const filteredUsers = computed(() => {
  if (!roleFilter.value) return props.users
  return props.users.filter((u) => u.roles.includes(roleFilter.value))
})

function openUserInspector(user: User) {
  inspectorUser.value = user
  selectedRoleForInspector.value = user.roles[0] || 'customer'
}

async function handleToggleUserStatus(user: User) {
  const nextStatus = user.status === 'Active' ? 'Blocked' : 'Active'
  try {
    const { data } = await api.patch<User>(`/usuarios/${user.id}/status`, { status: nextStatus })
    user.status = data.status
    toast.success(`Status de ${user.name} alterado para ${data.status}.`)
  } catch (err) {
    toast.error(getErrorMessage(err, 'Erro ao alterar status do usuário.'))
  }
}

async function handleAssignRole() {
  if (!inspectorUser.value || !selectedRoleForInspector.value) return
  try {
    const { data } = await api.patch<User>(`/usuarios/${inspectorUser.value.id}/roles`, {
      roles: [selectedRoleForInspector.value],
    })
    inspectorUser.value.roles = data.roles
    toast.success(`Perfil de ${inspectorUser.value.name} atualizado para ${selectedRoleForInspector.value}.`)
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível atualizar o perfil.'))
  }
}

async function handleCourierStatus(courier: Courier, status: 'Approved' | 'Rejected') {
  try {
    const { data } = await api.patch<Courier>(`/entregadores/${courier.id}/status`, { status })
    courier.status = data.status
    toast.success(`Entregador ${courier.name} atualizado para ${status}.`)
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível atualizar o entregador.'))
  }
}
</script>

<template>
  <div class="grid grid-cols-1 gap-6 lg:grid-cols-12 items-start">
    <!-- RBAC Users Dense Table (7 cols) -->
    <AppCard padding="lg" class="lg:col-span-7 space-y-4">
      <div class="flex flex-wrap items-center justify-between gap-2">
        <div class="flex items-center gap-2">
          <Shield class="h-5 w-5 text-brand-600" />
          <div>
            <h2 class="font-display text-base font-extrabold text-ink">
              Usuários & RBAC ({{ filteredUsers.length }})
            </h2>
            <p class="text-xs text-ink-muted">Clique em uma linha para gerenciar papéis e bloqueio.</p>
          </div>
        </div>

        <select
          v-model="roleFilter"
          class="rounded-xl border border-line-strong bg-surface px-3 py-1.5 text-xs font-semibold text-ink"
        >
          <option value="">Todos os perfis</option>
          <option v-for="r in allRoles" :key="r.value" :value="r.value">{{ r.value }}</option>
        </select>
      </div>

      <div class="overflow-x-auto rounded-2xl border border-line">
        <table class="w-full text-left text-xs">
          <thead class="border-b border-line bg-surface-subtle text-[11px] font-bold uppercase tracking-wider text-ink-muted">
            <tr>
              <th class="px-4 py-3">Usuário</th>
              <th class="px-4 py-3">Perfis</th>
              <th class="px-4 py-3">Status</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-line">
            <tr
              v-for="u in filteredUsers"
              :key="u.id"
              class="cursor-pointer transition hover:bg-surface-muted/60"
              @click="openUserInspector(u)"
            >
              <td class="px-4 py-3">
                <p class="font-bold text-ink">{{ u.name }}</p>
                <p class="text-[11px] text-ink-muted">{{ u.email }}</p>
              </td>
              <td class="px-4 py-3">
                <div class="flex flex-wrap gap-1">
                  <AppBadge v-for="role in u.roles" :key="role" variant="brand" size="xs">
                    {{ role }}
                  </AppBadge>
                </div>
              </td>
              <td class="px-4 py-3">
                <AppBadge :variant="u.status === 'Active' ? 'fresh' : 'danger'" size="xs">
                  {{ u.status }}
                </AppBadge>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </AppCard>

    <!-- Couriers Homologation Queue (5 cols) -->
    <AppCard padding="lg" class="lg:col-span-5 space-y-4">
      <div class="flex items-center gap-2">
        <Truck class="h-5 w-5 text-brand-600" />
        <h2 class="font-display text-base font-extrabold text-ink">
          Homologação de Entregadores ({{ couriers.length }})
        </h2>
      </div>

      <div v-if="couriers.length > 0" class="space-y-3 max-h-[600px] overflow-y-auto pr-1">
        <div
          v-for="courier in couriers"
          :key="courier.id"
          class="flex flex-col justify-between gap-3 rounded-2xl border border-line bg-surface-subtle p-3.5"
        >
          <div class="flex items-start justify-between gap-2">
            <div class="min-w-0">
              <p class="font-display text-sm font-bold text-ink truncate">{{ courier.name }}</p>
              <p class="mt-0.5 text-xs text-ink-muted">
                Doc: {{ courier.document || 'N/A' }} · Tel: {{ courier.phone || 'N/A' }}
              </p>
            </div>
            <OrderStatusBadge :status="courier.status" />
          </div>

          <div class="flex items-center gap-2">
            <AppButton
              v-if="courier.status !== 'Approved'"
              variant="fresh"
              size="xs"
              @click="handleCourierStatus(courier, 'Approved')"
            >
              <CheckCircle2 class="h-3.5 w-3.5" />
              Aprovar
            </AppButton>
            <AppButton
              v-if="courier.status !== 'Rejected'"
              variant="outline"
              size="xs"
              class="text-red-600 dark:text-red-400"
              @click="handleCourierStatus(courier, 'Rejected')"
            >
              <XCircle class="h-3.5 w-3.5" />
              Rejeitar
            </AppButton>
          </div>
        </div>
      </div>
      <p v-else class="py-8 text-center text-xs text-ink-muted">
        Nenhum entregador cadastrado no momento.
      </p>
    </AppCard>

    <!-- User RBAC Inspector Drawer -->
    <AppDrawer
      :open="Boolean(inspectorUser)"
      :title="inspectorUser?.name"
      :subtitle="inspectorUser?.email"
      width-class="max-w-md"
      @close="inspectorUser = null"
    >
      <div v-if="inspectorUser" class="space-y-5 text-xs">
        <div class="flex items-center justify-between rounded-2xl border border-line bg-surface-subtle p-4">
          <span class="font-bold text-ink-secondary">Status da Conta</span>
          <AppBadge :variant="inspectorUser.status === 'Active' ? 'fresh' : 'danger'" size="sm">
            {{ inspectorUser.status }}
          </AppBadge>
        </div>

        <div class="space-y-3 rounded-2xl border border-line bg-surface-subtle p-4">
          <label class="block text-xs font-bold text-ink">Alterar Perfil RBAC</label>
          <select
            v-model="selectedRoleForInspector"
            class="w-full rounded-xl border border-line-strong bg-surface px-3 py-2.5 text-xs font-semibold text-ink"
          >
            <option v-for="r in allRoles" :key="r.value" :value="r.value">
              {{ r.label }}
            </option>
          </select>
          <AppButton variant="primary" size="md" block @click="handleAssignRole">
            Salvar novo perfil
          </AppButton>
        </div>

        <AppButton
          :variant="inspectorUser.status === 'Active' ? 'danger' : 'fresh'"
          size="md"
          block
          @click="handleToggleUserStatus(inspectorUser)"
        >
          {{ inspectorUser.status === 'Active' ? 'Bloquear usuário' : 'Reativar usuário' }}
        </AppButton>
      </div>
    </AppDrawer>
  </div>
</template>
