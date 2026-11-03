import axios from 'axios'

const STORAGE_KEY = 'marmitas_auth'

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
  headers: {
    'Content-Type': 'application/json',
  },
})

api.interceptors.request.use((config) => {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (raw) {
    try {
      const session = JSON.parse(raw)
      if (session?.accessToken) {
        config.headers.Authorization = `Bearer ${session.accessToken}`
      }
    } catch {
      localStorage.removeItem(STORAGE_KEY)
    }
  }
  return config
})

export function getErrorMessage(error: unknown, fallback = 'Ocorreu um erro inesperado.'): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as
      | { errors?: Array<{ message?: string }> | Record<string, string[]>; message?: string }
      | undefined

    if (Array.isArray(data?.errors) && data.errors[0]?.message) {
      return data.errors[0].message
    }

    if (data?.errors && !Array.isArray(data.errors)) {
      const firstKey = Object.keys(data.errors)[0]
      if (firstKey && data.errors[firstKey]?.[0]) {
        return data.errors[firstKey][0]
      }
    }

    if (typeof data?.message === 'string' && data.message) {
      return data.message
    }

    if (error.response?.status === 401) {
      return 'Sessão expirada ou não autorizada. Faça login novamente.'
    }

    if (error.response?.status === 403) {
      return 'Você não tem permissão para realizar esta ação.'
    }

    if (error.response && error.response.status >= 500) {
      return 'Erro ao comunicar com o servidor da API. Verifique se o backend está rodando.'
    }

    if (!error.response) {
      return 'Não foi possível conectar ao servidor.'
    }
  }

  if (error instanceof Error && error.message) {
    return error.message
  }

  return fallback
}

export function formatCurrency(value: number): string {
  return new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: 'BRL',
  }).format(value || 0)
}

export function formatPhone(value: string): string {
  const digits = value.replace(/\D/g, '').slice(0, 11)
  if (!digits) return ''
  if (digits.length <= 2) return `(${digits}`
  if (digits.length <= 6) return `(${digits.slice(0, 2)}) ${digits.slice(2)}`
  if (digits.length <= 10) {
    return `(${digits.slice(0, 2)}) ${digits.slice(2, 6)}-${digits.slice(6)}`
  }
  return `(${digits.slice(0, 2)}) ${digits.slice(2, 7)}-${digits.slice(7)}`
}

