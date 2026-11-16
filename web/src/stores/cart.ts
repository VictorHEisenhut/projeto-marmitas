import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import type { Product, Restaurant } from '@/types'

const CART_STORAGE_KEY = 'marmitas_cart'

export interface SelectedAddon {
  name: string
  extraPrice: number
  quantity: number
}

export interface CartItem {
  productId: string
  name: string
  price: number
  basePrice?: number
  addonsPrice?: number
  cutleryPrice?: number
  wantsCutlery?: boolean
  selectedAddons?: SelectedAddon[]
  availableInPlans?: boolean
  quantity: number
}

interface StoredCart {
  restaurantId: string | null
  restaurantName: string
  isPlanRedemption?: boolean
  items: CartItem[]
}

export const useCartStore = defineStore('cart', () => {
  const restaurantId = ref<string | null>(null)
  const restaurantName = ref('')
  const isPlanRedemption = ref(false)
  const items = ref<CartItem[]>([])

  function loadFromStorage() {
    const raw = localStorage.getItem(CART_STORAGE_KEY)
    if (!raw) return
    try {
      const parsed = JSON.parse(raw) as StoredCart
      restaurantId.value = parsed.restaurantId || null
      restaurantName.value = parsed.restaurantName || ''
      isPlanRedemption.value = Boolean(parsed.isPlanRedemption)
      items.value = Array.isArray(parsed.items) ? parsed.items : []
    } catch {
      localStorage.removeItem(CART_STORAGE_KEY)
    }
  }

  function saveToStorage() {
    if (items.value.length === 0) {
      localStorage.removeItem(CART_STORAGE_KEY)
      return
    }
    const payload: StoredCart = {
      restaurantId: restaurantId.value,
      restaurantName: restaurantName.value,
      isPlanRedemption: isPlanRedemption.value,
      items: items.value,
    }
    localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(payload))
  }

  loadFromStorage()

  const totalItems = computed(() =>
    items.value.reduce((sum, item) => sum + item.quantity, 0),
  )

  function getItemEffectivePrice(item: CartItem): number {
    const base = item.basePrice ?? item.price
    const addons = item.addonsPrice ?? 0
    const cutlery = item.cutleryPrice ?? 0
    if (isPlanRedemption.value) {
      return addons + cutlery
    }
    return base + addons + cutlery
  }

  const totalAmount = computed(() =>
    items.value.reduce((sum, item) => sum + getItemEffectivePrice(item) * item.quantity, 0),
  )

  function setPlanMode(enabled: boolean) {
    if (enabled) {
      items.value = items.value.filter((i) => i.availableInPlans !== false)
    }
    isPlanRedemption.value = enabled
    saveToStorage()
  }

  function addItem(
    restaurant: Pick<Restaurant, 'id' | 'name'>,
    product: Pick<Product, 'id' | 'name' | 'price'> & Partial<Product>,
    options?: {
      selectedAddons?: SelectedAddon[]
      wantsCutlery?: boolean
      isPlanRedemption?: boolean
    },
  ) {
    if (restaurantId.value && restaurantId.value !== restaurant.id) {
      items.value = []
    }

    if (options?.isPlanRedemption !== undefined) {
      isPlanRedemption.value = options.isPlanRedemption
    }

    restaurantId.value = restaurant.id
    restaurantName.value = restaurant.name

    const selectedAddons = (options?.selectedAddons || []).filter((a) => a.quantity > 0)
    const addonsPrice = selectedAddons.reduce((sum, a) => sum + a.extraPrice * a.quantity, 0)
    const wantsCutlery = Boolean(options?.wantsCutlery && product.allowsCutlery)
    const cutleryPrice =
      wantsCutlery && product.cutleryHasExtraCost ? Number(product.cutleryPrice || 0) : 0
    const basePrice = product.price
    const fullUnitPrice = basePrice + addonsPrice + cutleryPrice

    const hasCustomOptions = selectedAddons.length > 0 || wantsCutlery
    const existing = !hasCustomOptions
      ? items.value.find(
          (i) =>
            i.productId === product.id &&
            (!i.selectedAddons || i.selectedAddons.length === 0) &&
            !i.wantsCutlery,
        )
      : undefined

    if (existing) {
      existing.quantity += 1
    } else {
      items.value.push({
        productId: product.id,
        name: product.name,
        price: fullUnitPrice,
        basePrice,
        addonsPrice,
        cutleryPrice,
        wantsCutlery,
        selectedAddons,
        availableInPlans: product.availableInPlans ?? true,
        quantity: 1,
      })
    }

    saveToStorage()
  }

  function updateQuantity(productId: string, quantity: number) {
    if (quantity <= 0) {
      removeItem(productId)
      return
    }
    const existing = items.value.find((i) => i.productId === productId)
    if (existing) {
      existing.quantity = quantity
      saveToStorage()
    }
  }

  function removeItem(productId: string) {
    items.value = items.value.filter((i) => i.productId !== productId)
    if (items.value.length === 0) {
      restaurantId.value = null
      restaurantName.value = ''
    }
    saveToStorage()
  }

  function clear() {
    restaurantId.value = null
    restaurantName.value = ''
    isPlanRedemption.value = false
    items.value = []
    localStorage.removeItem(CART_STORAGE_KEY)
  }

  return {
    restaurantId,
    restaurantName,
    isPlanRedemption,
    items,
    totalItems,
    totalAmount,
    getItemEffectivePrice,
    setPlanMode,
    addItem,
    updateQuantity,
    removeItem,
    clear,
  }
})

