<script setup lang="ts">
import Navbar from '@/components/Navbar.vue'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
</script>

<template>
  <div class="min-h-screen bg-stone-50">
    <Navbar />

    <main class="mx-auto max-w-lg px-4 py-16 text-center">
      <div class="rounded-xl border border-stone-200 bg-white p-8 shadow-sm">
        <h1 class="text-2xl font-bold text-stone-900">Acesso negado</h1>
        <p class="mt-2 text-sm text-stone-600">
          Sua conta não possui permissão para visualizar esta página.
        </p>

        <div class="mt-6">
          <RouterLink
            :to="auth.isAuthenticated ? auth.defaultRoute : '/'"
            class="inline-block rounded-lg bg-brand-600 px-5 py-2.5 text-sm font-semibold text-white hover:bg-brand-700"
          >
            Voltar para o início
          </RouterLink>
        </div>
      </div>
    </main>
  </div>
</template>

