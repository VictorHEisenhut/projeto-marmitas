<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { DollarSign, RefreshCw, ShoppingBag, Store, Users } from 'lucide-vue-next'
import Navbar from '@/components/Navbar.vue'
import { api, formatCurrency, getErrorMessage } from '@/services/api'
import type { AdminDashboard } from '@/types'

const stats = ref<AdminDashboard | null>(null)
const loading = ref(true)
const errorMessage = ref('')

async function loadDashboard() {
  loading.value = true
  errorMessage.value = ''

  try {
    const { data } = await api.get<AdminDashboard>('/admin/dashboard')
    stats.value = data
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Não foi possível carregar os indicadores.')
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  void loadDashboard()
})
</script>

<template>
  <div class="min-h-screen bg-stone-50">
    <Navbar />

    <main class="mx-auto max-w-6xl px-4 py-8 sm:px-6">
      <div class="mb-8 flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <div>
          <h1 class="text-2xl font-bold text-stone-900">Painel Administrativo</h1>
          <p class="mt-1 text-sm text-stone-600">
            Visão geral de usuários, restaurantes, pedidos e faturamento da plataforma.
          </p>
        </div>

        <button
          type="button"
          :disabled="loading"
          class="inline-flex items-center gap-2 self-start rounded-lg border border-stone-200 bg-white px-4 py-2 text-sm font-medium text-stone-700 hover:bg-stone-50 disabled:opacity-50"
          @click="loadDashboard"
        >
          <RefreshCw class="h-4 w-4" :class="{ 'animate-spin': loading }" />
          Atualizar
        </button>
      </div>

      <div
        v-if="errorMessage"
        class="mb-6 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700"
      >
        {{ errorMessage }}
      </div>

      <div v-if="loading" class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <div
          v-for="n in 4"
          :key="n"
          class="h-28 animate-pulse rounded-xl border border-stone-200 bg-white p-5"
        />
      </div>

      <div v-else-if="stats" class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <div class="rounded-xl border border-stone-200 bg-white p-5 shadow-sm">
          <div class="flex items-center justify-between text-stone-500">
            <span class="text-sm font-medium">Usuários</span>
            <Users class="h-5 w-5 text-stone-400" />
          </div>
          <p class="mt-3 text-2xl font-bold text-stone-900">{{ stats.totalUsers }}</p>
        </div>

        <div class="rounded-xl border border-stone-200 bg-white p-5 shadow-sm">
          <div class="flex items-center justify-between text-stone-500">
            <span class="text-sm font-medium">Restaurantes</span>
            <Store class="h-5 w-5 text-orange-600" />
          </div>
          <p class="mt-3 text-2xl font-bold text-stone-900">{{ stats.totalRestaurants }}</p>
        </div>

        <div class="rounded-xl border border-stone-200 bg-white p-5 shadow-sm">
          <div class="flex items-center justify-between text-stone-500">
            <span class="text-sm font-medium">Pedidos</span>
            <ShoppingBag class="h-5 w-5 text-blue-600" />
          </div>
          <p class="mt-3 text-2xl font-bold text-stone-900">{{ stats.totalOrders }}</p>
        </div>

        <div class="rounded-xl border border-stone-200 bg-white p-5 shadow-sm">
          <div class="flex items-center justify-between text-stone-500">
            <span class="text-sm font-medium">Receita Total</span>
            <DollarSign class="h-5 w-5 text-green-600" />
          </div>
          <p class="mt-3 text-2xl font-bold text-stone-900">
            {{ formatCurrency(stats.totalRevenue) }}
          </p>
        </div>
      </div>
    </main>
  </div>
</template>

