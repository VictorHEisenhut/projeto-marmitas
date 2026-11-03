import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import type { Product, Restaurant } from '@/types'

const CART_STORAGE_KEY = 'marmitas_cart'

export interface CartItem {
  productId: string
  name: string
  price: number
  quantity: number
}

interface StoredCart {
  restaurantId: string | null
  restaurantName: string
  items: CartItem[]
}

export const useCartStore = defineStore('cart', () => {
  const restaurantId = ref<string | null>(null)
  const restaurantName = ref('')
  const items = ref<CartItem[]>([])

  function loadFromStorage() {
    const raw = localStorage.getItem(CART_STORAGE_KEY)
    if (!raw) return
    try {
      const parsed = JSON.parse(raw) as StoredCart
      restaurantId.value = parsed.restaurantId || null
      restaurantName.value = parsed.restaurantName || ''
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
      items: items.value,
    }
    localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(payload))
  }

  loadFromStorage()

  const totalItems = computed(() =>
    items.value.reduce((sum, item) => sum + item.quantity, 0),
  )

  const totalAmount = computed(() =>
    items.value.reduce((sum, item) => sum + item.price * item.quantity, 0),
  )

  function addItem(restaurant: Pick<Restaurant, 'id' | 'name'>, product: Pick<Product, 'id' | 'name' | 'price'>) {
    if (restaurantId.value && restaurantId.value !== restaurant.id) {
      items.value = []
    }

    restaurantId.value = restaurant.id
    restaurantName.value = restaurant.name

    const existing = items.value.find((i) => i.productId === product.id)
    if (existing) {
      existing.quantity += 1
    } else {
      items.value.push({
        productId: product.id,
        name: product.name,
        price: product.price,
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
    items.value = []
    localStorage.removeItem(CART_STORAGE_KEY)
  }

  return {
    restaurantId,
    restaurantName,
    items,
    totalItems,
    totalAmount,
    addItem,
    updateQuantity,
    removeItem,
    clear,
  }
})

