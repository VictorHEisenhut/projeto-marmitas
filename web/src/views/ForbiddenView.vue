<script setup lang="ts">
import { ArrowLeft, ShieldOff } from 'lucide-vue-next'
import CustomerLayout from '@/layouts/CustomerLayout.vue'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
</script>

<template>
  <CustomerLayout>
    <div class="mx-auto flex max-w-lg flex-col items-center justify-center px-4 py-20 text-center">
      <AppCard padding="lg" class="w-full space-y-5">
        <div class="mx-auto flex h-16 w-16 items-center justify-center rounded-3xl bg-red-50 text-red-600 dark:bg-red-950/60 dark:text-red-400">
          <ShieldOff class="h-8 w-8" />
        </div>

        <div class="space-y-2">
          <AppBadge variant="danger" size="sm">Erro 403 · Acesso Restrito</AppBadge>
          <h1 class="font-display text-2xl font-extrabold text-ink">
            Você não tem permissão para esta página
          </h1>
          <p class="text-sm leading-relaxed text-ink-secondary">
            Sua conta atual não possui o perfil RBAC necessário para acessar esta área operacional.
          </p>
        </div>

        <div class="pt-2">
          <RouterLink :to="auth.isAuthenticated ? auth.defaultRoute : '/'">
            <AppButton variant="primary" size="lg" block>
              <ArrowLeft class="h-4 w-4" />
              Voltar para minha área inicial
            </AppButton>
          </RouterLink>
        </div>
      </AppCard>
    </div>
  </CustomerLayout>
</template>
