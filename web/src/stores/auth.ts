import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { api } from '@/services/api'
import type { LoginRequest, LoginResponse, RegisterRequest, User } from '@/types'

const STORAGE_KEY = 'marmitas_auth'

interface StoredSession {
  accessToken: string
  refreshToken?: string
  expiresAt: string
  user: User
}

function parseJwtRoles(token: string): string[] {
  try {
    const payloadPart = token.split('.')[1]
    if (!payloadPart) return []
    const base64 = payloadPart.replace(/-/g, '+').replace(/_/g, '/')
    const json = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join(''),
    )
    const payload = JSON.parse(json)
    if (Array.isArray(payload.role)) return payload.role
    if (typeof payload.role === 'string') return [payload.role]
    return []
  } catch {
    return []
  }
}

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)
  const accessToken = ref<string | null>(null)
  const refreshTokenValue = ref<string | null>(null)
  const expiresAt = ref<string | null>(null)
  const initialized = ref(false)

  let refreshTimer: ReturnType<typeof setTimeout> | null = null
  let refreshPromise: Promise<boolean> | null = null
  let visibilityBound = false

  const isAuthenticated = computed(() => {
    if (!accessToken.value || !user.value || !expiresAt.value) return false
    return new Date(expiresAt.value).getTime() > Date.now()
  })

  const roles = computed<string[]>(() => {
    const tokenRoles = accessToken.value ? parseJwtRoles(accessToken.value) : []
    const userRoles = user.value?.roles ?? []
    return Array.from(new Set([...userRoles, ...tokenRoles]))
  })

  const isDriverOnly = computed(
    () =>
      hasRole(['driver']) &&
      !hasRole(['customer', 'restaurant_owner', 'restaurant_staff', 'admin', 'super_admin']),
  )

  const defaultRoute = computed(() => {
    if (hasRole(['admin', 'super_admin'])) return '/admin'
    if (hasRole(['restaurant_owner', 'restaurant_staff'])) return '/parceiro'
    if (hasRole(['driver'])) return '/entregador'
    return '/'
  })

  function hasRole(allowed: string | string[]): boolean {
    const list = Array.isArray(allowed) ? allowed : [allowed]
    return list.some((r) => roles.value.includes(r))
  }

  function clearRefreshTimer() {
    if (refreshTimer) {
      clearTimeout(refreshTimer)
      refreshTimer = null
    }
  }

  function scheduleRefresh() {
    clearRefreshTimer()
    if (!expiresAt.value || !accessToken.value) return

    const expiresMs = new Date(expiresAt.value).getTime()
    const delay = expiresMs - Date.now() - 45_000

    if (delay <= 0) return

    refreshTimer = setTimeout(() => {
      void refreshToken()
    }, delay)
  }

  function setSession(data: LoginResponse) {
    accessToken.value = data.accessToken
    if (data.refreshToken) {
      refreshTokenValue.value = data.refreshToken
    }
    expiresAt.value = data.expiresAt
    user.value = data.user

    const payload: StoredSession = {
      accessToken: data.accessToken,
      refreshToken: refreshTokenValue.value || undefined,
      expiresAt: data.expiresAt,
      user: data.user,
    }
    localStorage.setItem(STORAGE_KEY, JSON.stringify(payload))
    scheduleRefresh()
  }

  function clearSession() {
    clearRefreshTimer()
    accessToken.value = null
    refreshTokenValue.value = null
    expiresAt.value = null
    user.value = null
    localStorage.removeItem(STORAGE_KEY)
  }

  async function refreshToken(): Promise<boolean> {
    if (!accessToken.value && !refreshTokenValue.value) return false
    if (expiresAt.value && new Date(expiresAt.value).getTime() <= Date.now() && !refreshTokenValue.value) {
      clearSession()
      return false
    }

    if (refreshPromise) return refreshPromise

    refreshPromise = (async () => {
      try {
        const { data } = await api.post<LoginResponse>('/auth/refresh-token', {
          email: user.value?.email,
          refreshToken: refreshTokenValue.value || undefined,
        })
        setSession(data)
        return true
      } catch {
        clearSession()
        return false
      } finally {
        refreshPromise = null
      }
    })()

    return refreshPromise
  }

  function bindVisibilityListener() {
    if (visibilityBound || typeof document === 'undefined') return
    visibilityBound = true

    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState !== 'visible') return
      if (!accessToken.value || !expiresAt.value) return

      const remainingMs = new Date(expiresAt.value).getTime() - Date.now()
      if (remainingMs <= 0) {
        clearSession()
      } else if (remainingMs <= 90_000) {
        void refreshToken()
      }
    })
  }

  async function init() {
    if (initialized.value) return
    initialized.value = true
    bindVisibilityListener()

    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return

    try {
      const stored = JSON.parse(raw) as StoredSession
      if (!stored.accessToken || !stored.expiresAt) {
        clearSession()
        return
      }

      if (new Date(stored.expiresAt).getTime() <= Date.now()) {
        clearSession()
        return
      }

      accessToken.value = stored.accessToken
      refreshTokenValue.value = stored.refreshToken || null
      expiresAt.value = stored.expiresAt
      user.value = stored.user
      scheduleRefresh()

      const { data } = await api.get<User>('/usuarios/me')
      user.value = data
    } catch {
      clearSession()
    }
  }

  async function login(credentials: LoginRequest): Promise<LoginResponse> {
    const { data } = await api.post<LoginResponse>('/auth/login', {
      email: credentials.email.trim().toLowerCase(),
      password: credentials.password,
    })
    setSession(data)
    return data
  }

  async function register(payload: RegisterRequest): Promise<User> {
    const { data } = await api.post<User>('/auth/registro', {
      name: payload.name.trim(),
      email: payload.email.trim().toLowerCase(),
      phone: payload.phone?.trim() || null,
      password: payload.password,
      role: payload.role,
      termsAccepted: payload.termsAccepted ?? true,
    })
    return data
  }

  async function logout() {
    if (isAuthenticated.value) {
      try {
        await api.post('/auth/logout')
      } catch {
        // Ignora erro caso a sessão já tenha expirado no servidor
      }
    }
    clearSession()
  }

  return {
    user,
    accessToken,
    expiresAt,
    initialized,
    isAuthenticated,
    isDriverOnly,
    roles,
    defaultRoute,
    hasRole,
    init,
    login,
    register,
    refreshToken,
    logout,
    clearSession,
  }
})

