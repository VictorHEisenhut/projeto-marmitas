import { ref } from 'vue'
import { defineStore } from 'pinia'

export type ToastVariant = 'success' | 'error' | 'info' | 'warning'

export interface ToastMessage {
  id: string
  variant: ToastVariant
  title?: string
  message: string
  duration: number
}

export const useToastStore = defineStore('toast', () => {
  const toasts = ref<ToastMessage[]>([])

  function push(variant: ToastVariant, message: string, options?: { title?: string; duration?: number }) {
    const id = `${Date.now()}-${Math.random().toString(36).slice(2, 7)}`
    const duration = options?.duration ?? (variant === 'error' ? 5500 : 3800)

    toasts.value.push({
      id,
      variant,
      title: options?.title,
      message,
      duration,
    })

    if (duration > 0) {
      setTimeout(() => {
        dismiss(id)
      }, duration)
    }

    return id
  }

  function dismiss(id: string) {
    toasts.value = toasts.value.filter((t) => t.id !== id)
  }

  function success(message: string, title?: string) {
    return push('success', message, { title })
  }

  function error(message: string, title?: string) {
    return push('error', message, { title })
  }

  function info(message: string, title?: string) {
    return push('info', message, { title })
  }

  function warning(message: string, title?: string) {
    return push('warning', message, { title })
  }

  return {
    toasts,
    push,
    dismiss,
    success,
    error,
    info,
    warning,
  }
})
