<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { Bell, CheckCheck } from 'lucide-vue-next'
import { api } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import type { NotificationItem } from '@/types'

const auth = useAuthStore()
const open = ref(false)
const notifications = ref<NotificationItem[]>([])
const containerRef = ref<HTMLElement | null>(null)

const unreadCount = computed(() => notifications.value.filter((n) => !n.isRead).length)

async function loadNotifications() {
  if (!auth.isAuthenticated) return
  try {
    const { data } = await api.get<NotificationItem[]>('/notificacoes')
    notifications.value = data
  } catch {
    // Silent fallback
  }
}

async function toggle() {
  open.value = !open.value
  if (open.value) {
    await loadNotifications()
  }
}

async function markAsRead(item: NotificationItem) {
  if (item.isRead) return
  try {
    await api.patch(`/notificacoes/${item.id}/lida`)
    item.isRead = true
  } catch {
    // Ignore transient failure
  }
}

async function markAllAsRead() {
  try {
    await api.patch('/notificacoes/marcar-todas-lidas')
    notifications.value.forEach((n) => {
      n.isRead = true
    })
  } catch {
    // Ignore transient failure
  }
}

function handleClickOutside(event: MouseEvent) {
  if (containerRef.value && !containerRef.value.contains(event.target as Node)) {
    open.value = false
  }
}

onMounted(() => {
  if (auth.isAuthenticated) {
    void loadNotifications()
  }
  document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside)
})
</script>

<template>
  <div v-if="auth.isAuthenticated" ref="containerRef" class="relative">
    <button
      type="button"
      aria-label="Notificações"
      class="relative inline-flex h-10 w-10 items-center justify-center rounded-xl border border-line bg-surface text-ink-secondary transition hover:border-line-strong hover:bg-surface-muted hover:text-ink"
      @click="toggle"
    >
      <Bell class="h-[18px] w-[18px]" />
      <span
        v-if="unreadCount > 0"
        class="absolute -right-1 -top-1 flex h-4 min-w-[1rem] items-center justify-center rounded-full bg-brand-600 px-1 text-[10px] font-bold text-white tabular-nums shadow-sm"
      >
        {{ unreadCount > 9 ? '9+' : unreadCount }}
      </span>
    </button>

    <div
      v-if="open"
      class="absolute right-0 z-40 mt-2 w-80 max-w-[calc(100vw-2rem)] overflow-hidden rounded-2xl border border-line bg-surface shadow-elevated animate-slide-up"
    >
      <div class="flex items-center justify-between border-b border-line px-4 py-3">
        <div class="flex items-center gap-2">
          <span class="font-display text-sm font-bold text-ink">Notificações</span>
          <span
            v-if="unreadCount > 0"
            class="rounded-full bg-brand-50 px-2 py-0.5 text-[10px] font-bold text-brand-700 dark:bg-brand-950/60 dark:text-brand-300"
          >
            {{ unreadCount }} nova(s)
          </span>
        </div>
        <button
          v-if="unreadCount > 0"
          type="button"
          class="inline-flex items-center gap-1 text-[11px] font-semibold text-brand-600 hover:underline dark:text-brand-400"
          @click="markAllAsRead"
        >
          <CheckCheck class="h-3.5 w-3.5" />
          Marcar todas
        </button>
      </div>

      <div
        v-if="notifications.length === 0"
        class="px-4 py-8 text-center text-xs text-ink-muted"
      >
        Nenhuma notificação no momento.
      </div>

      <ul v-else class="max-h-72 divide-y divide-line overflow-y-auto">
        <li
          v-for="item in notifications.slice(0, 12)"
          :key="item.id"
          class="cursor-pointer px-4 py-3 text-xs transition hover:bg-surface-muted/70"
          :class="item.isRead ? 'opacity-65' : 'bg-brand-50/40 dark:bg-brand-950/20'"
          @click="markAsRead(item)"
        >
          <div class="flex items-start justify-between gap-2">
            <span class="font-semibold text-ink">{{ item.title }}</span>
            <span
              v-if="!item.isRead"
              class="mt-1 h-2 w-2 shrink-0 rounded-full bg-brand-600"
            />
          </div>
          <p class="mt-0.5 leading-relaxed text-ink-secondary">{{ item.message }}</p>
          <p class="mt-1 text-[10px] text-ink-muted">
            {{ new Date(item.createdAt).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' }) }}
          </p>
        </li>
      </ul>
    </div>
  </div>
</template>
