<script setup lang="ts">
import { CheckCircle2, Clock, Moon, Sparkles, Sun, Utensils } from 'lucide-vue-next'
import { useThemeStore } from '@/stores/theme'

withDefaults(
  defineProps<{
    title: string
    subtitle?: string
    wide?: boolean
  }>(),
  {
    wide: false,
  },
)

const theme = useThemeStore()
</script>

<template>
  <div class="relative flex min-h-screen bg-canvas text-ink">
    <!-- Left Editorial Showcase (Desktop Split-Screen) -->
    <div
      class="relative hidden lg:flex lg:w-[44%] xl:w-[46%] flex-col justify-between overflow-hidden bg-gradient-to-br from-brand-700 via-brand-600 to-amber-600 p-12 text-white"
    >
      <div
        class="pointer-events-none absolute -right-24 -top-24 h-80 w-80 rounded-full bg-white/10 blur-3xl"
      />
      <div
        class="pointer-events-none absolute -bottom-32 -left-20 h-96 w-96 rounded-full bg-amber-300/20 blur-3xl"
      />

      <RouterLink to="/" class="relative z-10 inline-flex items-center gap-2.5">
        <span class="flex h-11 w-11 items-center justify-center rounded-2xl bg-white/15 backdrop-blur-md">
          <Utensils class="h-6 w-6 text-white" />
        </span>
        <div>
          <span class="font-display text-xl font-extrabold tracking-tight">Marmitas</span>
          <p class="text-[11px] font-medium text-white/80">Marketplace & Assinaturas</p>
        </div>
      </RouterLink>

      <div class="relative z-10 max-w-md space-y-6">
        <div class="inline-flex items-center gap-2 rounded-full bg-white/15 px-3.5 py-1.5 text-xs font-semibold backdrop-blur-md">
          <Sparkles class="h-3.5 w-3.5" />
          Gastronomia fresca todos os dias
        </div>

        <h2 class="font-display text-4xl font-extrabold leading-tight tracking-tight">
          Comida de verdade, quente e no horário certo.
        </h2>

        <p class="text-sm leading-relaxed text-white/85">
          Peça marmitas caseiras avulsas ou assine um plano semanal com economia inteligente,
          rastreamento GPS ao vivo e recomendação nutricional por IA.
        </p>

        <div class="grid grid-cols-2 gap-3 pt-2">
          <div class="rounded-2xl bg-white/10 p-4 backdrop-blur-md">
            <Clock class="h-5 w-5 text-amber-200" />
            <p class="mt-2 font-display text-lg font-extrabold">Entrega ao vivo</p>
            <p class="text-xs text-white/80">Rastreio GPS + PIN de 4 dígitos</p>
          </div>
          <div class="rounded-2xl bg-white/10 p-4 backdrop-blur-md">
            <CheckCircle2 class="h-5 w-5 text-fresh-300" />
            <p class="mt-2 font-display text-lg font-extrabold">Clube de Planos</p>
            <p class="text-xs text-white/80">Resgate semanal sem burocracia</p>
          </div>
        </div>
      </div>

      <p class="relative z-10 text-xs text-white/70">
        © {{ new Date().getFullYear() }} Projeto Marmita · Segurança LGPD & Pagamentos Instantâneos
      </p>
    </div>

    <!-- Right Form Area -->
    <div class="relative flex flex-1 flex-col justify-center px-4 py-10 sm:px-8 lg:px-12">
      <!-- Discreet Theme Toggle -->
      <button
        type="button"
        :title="theme.isDark ? 'Ativar modo claro' : 'Ativar modo escuro'"
        :aria-label="theme.isDark ? 'Ativar modo claro' : 'Ativar modo escuro'"
        class="fixed right-4 top-4 z-30 inline-flex h-10 items-center gap-1.5 rounded-xl border border-line bg-surface px-3 text-xs font-semibold text-ink-secondary shadow-card transition hover:bg-surface-muted hover:text-ink"
        @click="theme.toggleTheme()"
      >
        <Sun v-if="theme.isDark" class="h-4 w-4 text-amber-400" />
        <Moon v-else class="h-4 w-4" />
        <span class="hidden sm:inline">{{ theme.isDark ? 'Claro' : 'Escuro' }}</span>
      </button>

      <div class="mx-auto w-full" :class="wide ? 'max-w-xl' : 'max-w-md'">
        <!-- Mobile Brand Header -->
        <RouterLink to="/" class="mb-6 flex items-center justify-center gap-2.5 lg:hidden">
          <span class="flex h-10 w-10 items-center justify-center rounded-xl bg-brand-600 text-white shadow-sm">
            <Utensils class="h-5 w-5" />
          </span>
          <span class="font-display text-xl font-extrabold text-ink">Marmitas</span>
        </RouterLink>

        <div class="mb-6 text-center lg:text-left">
          <h1 class="font-display text-2xl font-extrabold tracking-tight text-ink sm:text-3xl">
            {{ title }}
          </h1>
          <p v-if="subtitle" class="mt-1.5 text-sm text-ink-secondary">
            {{ subtitle }}
          </p>
        </div>

        <div class="rounded-2xl border border-line bg-surface p-6 shadow-card sm:p-8">
          <slot />
        </div>

        <div v-if="$slots.footer" class="mt-5 text-center text-sm text-ink-secondary">
          <slot name="footer" />
        </div>
      </div>
    </div>
  </div>
</template>
