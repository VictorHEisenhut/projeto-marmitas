import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { api } from '@/services/api'
import type { Address } from '@/types'

export const useAddressStore = defineStore('address', () => {
  const addresses = ref<Address[]>([])
  const selectedAddressId = ref<string>('')
  const loading = ref(false)
  const loaded = ref(false)

  const selectedAddress = computed<Address | null>(() => {
    if (selectedAddressId.value) {
      const found = addresses.value.find((a) => a.id === selectedAddressId.value)
      if (found) return found
    }
    return addresses.value.find((a) => a.isMain) ?? addresses.value[0] ?? null
  })

  const shortLabel = computed(() => {
    const addr = selectedAddress.value
    if (!addr) return 'Selecionar endereço'
    return `${addr.street}, ${addr.number}`
  })

  async function fetchAddresses(force = false) {
    if (loading.value || (loaded.value && !force)) return
    loading.value = true
    try {
      const { data } = await api.get<Address[]>('/usuarios/me/enderecos')
      addresses.value = data
      loaded.value = true
      if (!selectedAddressId.value || !data.some((a) => a.id === selectedAddressId.value)) {
        const main = data.find((a) => a.isMain) ?? data[0]
        selectedAddressId.value = main?.id ?? ''
      }
    } catch {
      // Silent for unauthenticated or driver-only sessions
    } finally {
      loading.value = false
    }
  }

  function selectAddress(id: string) {
    selectedAddressId.value = id
  }

  async function setMainAddress(id: string) {
    await api.patch(`/usuarios/me/enderecos/${id}/principal`)
    selectedAddressId.value = id
    await fetchAddresses(true)
  }

  function reset() {
    addresses.value = []
    selectedAddressId.value = ''
    loaded.value = false
  }

  return {
    addresses,
    selectedAddressId,
    selectedAddress,
    shortLabel,
    loading,
    loaded,
    fetchAddresses,
    selectAddress,
    setMainAddress,
    reset,
  }
})
