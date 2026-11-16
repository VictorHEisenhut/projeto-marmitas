import { ref } from 'vue'
import { defineStore } from 'pinia'

const THEME_STORAGE_KEY = 'marmitas_theme'

function readStoredDarkPreference(): boolean {
  try {
    if (typeof window === 'undefined' || typeof localStorage?.getItem !== 'function') {
      return false
    }
    const saved = localStorage.getItem(THEME_STORAGE_KEY)
    if (saved === 'dark') return true
    if (saved === 'light') return false
    return window.matchMedia?.('(prefers-color-scheme: dark)')?.matches ?? false
  } catch {
    return false
  }
}

function applyDocumentTheme(dark: boolean) {
  if (typeof document === 'undefined') return
  document.documentElement.classList.toggle('dark', dark)
}

export const useThemeStore = defineStore('theme', () => {
  const isDark = ref<boolean>(readStoredDarkPreference())

  function initTheme() {
    applyDocumentTheme(isDark.value)
  }

  function setDark(value: boolean) {
    isDark.value = value
    applyDocumentTheme(value)
    try {
      if (typeof localStorage?.setItem === 'function') {
        localStorage.setItem(THEME_STORAGE_KEY, value ? 'dark' : 'light')
      }
    } catch {
      // Ignore storage errors
    }
  }

  function toggleTheme() {
    setDark(!isDark.value)
  }

  return {
    isDark,
    initTheme,
    setDark,
    toggleTheme,
  }
})
