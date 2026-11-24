<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { CheckCircle2, Shield, Truck, XCircle } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import OrderStatusBadge from '@/components/ui/OrderStatusBadge.vue'
import { api, getErrorMessage } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import type { Courier, User } from '@/types'

const props = defineProps<{
  users: User[]
  couriers: Courier[]
}>()

const emit = defineEmits<{
  (e: 'refresh'): void
}>()

const toast = useToastStore()

const roleFilter = ref('')
const roleSelections = ref<Record<string, string>>({})

const allRoles = [
  { value: 'customer', label: 'customer (Cliente)' },
  { value: 'restaurant_owner', label: 'restaurant_owner (Dono de Restaurante)' },
  { value: 'restaurant_staff', label: 'restaurant_staff (Equipe Restaurante)' },
  { value: 'driver', label: 'driver (Entregador)' },
  { value: 'admin', label: 'admin (Administrador)' },
  { value: 'super_admin', label: 'super_admin (Super Admin)' },
]

watch(
  () => props.users,
  (list) => {
    const map: Record<string, string> = {}
    for (const u of list) {
      map[u.id] = roleSelections.value[u.id] || u.roles[0] || 'customer'
    }
    roleSelections.value = map
  },
  { immediate: true },
)

const filteredUsers = computed(() => {
  if (!roleFilter.value) return props.users
  return props.users.filter((u) => u.roles.includes(roleFilter.value))
})

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

async function handleAssignRole(user: User) {
  const selectedRole = roleSelections.value[user.id]
  if (!selectedRole) return
  try {
    const { data } = await api.patch<User>(`/usuarios/${user.id}/roles`, {
      roles: [selectedRole],
    })
    user.roles = data.roles
    toast.success(`Perfil de ${user.name} atualizado para ${selectedRole}.`)
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
  <div class="grid gap-6 lg:grid-cols-2 items-start">
    <!-- RBAC Users Management -->
    <AppCard padding="lg" class="space-y-4">
      <div class="flex flex-wrap items-center justify-between gap-2">
        <div class="flex items-center gap-2">
          <Shield class="h-5 w-5 text-brand-600" />
          <h2 class="font-display text-base font-extrabold text-ink">
            Usuários & RBAC ({{ filteredUsers.length }})
          </h2>
        </div>

        <select
          v-model="roleFilter"
          class="rounded-xl border border-line-strong bg-surface px-3 py-1.5 text-xs font-semibold text-ink"
        >
          <option value="">Todos os perfis</option>
          <option v-for="r in allRoles" :key="r.value" :value="r.value">{{ r.value }}</option>
        </select>
      </div>

      <div class="space-y-3 max-h-[620px] overflow-y-auto pr-1">
        <div
          v-for="u in filteredUsers"
          :key="u.id"
          class="rounded-2xl border border-line bg-surface-subtle p-3.5 space-y-3 text-xs"
        >
          <div class="flex items-start justify-between gap-2">
            <div class="min-w-0">
              <p class="font-display text-sm font-bold text-ink truncate">{{ u.name }}</p>
              <p class="text-ink-muted truncate">{{ u.email }}</p>
              <div class="mt-1.5 flex flex-wrap gap-1">
                <AppBadge v-for="role in u.roles" :key="role" variant="brand" size="xs">
                  {{ role }}
                </AppBadge>
                <AppBadge :variant="u.status === 'Active' ? 'fresh' : 'danger'" size="xs">
                  {{ u.status }}
                </AppBadge>
              </div>
            </div>

            <AppButton
              :variant="u.status === 'Active' ? 'outline' : 'fresh'"
              size="xs"
              @click="handleToggleUserStatus(u)"
            >
              {{ u.status === 'Active' ? 'Bloquear' : 'Ativar' }}
            </AppButton>
          </div>

          <div class="flex items-center gap-2 border-t border-line pt-2.5">
            <select
              v-model="roleSelections[u.id]"
              class="flex-1 rounded-xl border border-line-strong bg-surface px-2.5 py-1.5 text-xs text-ink"
            >
              <option v-for="r in allRoles" :key="r.value" :value="r.value">
                {{ r.label }}
              </option>
            </select>
            <AppButton variant="secondary" size="xs" @click="handleAssignRole(u)">
              Salvar perfil
            </AppButton>
          </div>
        </div>
      </div>
    </AppCard>

    <!-- Couriers Homologation Queue -->
    <AppCard padding="lg" class="space-y-4">
      <div class="flex items-center gap-2">
        <Truck class="h-5 w-5 text-brand-600" />
        <h2 class="font-display text-base font-extrabold text-ink">
          Homologação de Entregadores ({{ couriers.length }})
        </h2>
      </div>

      <div v-if="couriers.length > 0" class="space-y-3 max-h-[620px] overflow-y-auto pr-1">
        <div
          v-for="courier in couriers"
          :key="courier.id"
          class="flex flex-col justify-between gap-3 rounded-2xl border border-line bg-surface-subtle p-3.5 sm:flex-row sm:items-center"
        >
          <div class="min-w-0">
            <div class="flex flex-wrap items-center gap-2">
              <p class="font-display text-sm font-bold text-ink">{{ courier.name }}</p>
              <OrderStatusBadge :status="courier.status" />
            </div>
            <p class="mt-0.5 text-xs text-ink-muted">
              Doc: {{ courier.document || 'N/A' }} · Tel: {{ courier.phone || 'N/A' }}
            </p>
          </div>

          <div class="flex shrink-0 items-center gap-1.5">
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
  </div>
</template>
