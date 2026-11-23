export interface OrderStatusUpdatedEvent {
  orderId: string
  customerId: string
  restaurantId: string
  status: string
  updatedAt: string
}

export interface DriverLocationUpdatedEvent {
  orderId: string
  courierId: string
  latitude: number
  longitude: number
  updatedAt: string
}

export interface DeliveryUpdatedEvent {
  deliveryId: string
  orderId: string
  status: string
  updatedAt: string
}

export interface RealtimeListeners {
  onOrderStatusUpdated?: (payload: OrderStatusUpdatedEvent) => void
  onDriverLocationUpdated?: (payload: DriverLocationUpdatedEvent) => void
  onDeliveryUpdated?: (payload: DeliveryUpdatedEvent) => void
}

const RECORD_SEPARATOR = '\x1e'

export class OrderTrackingClient {
  private socket: WebSocket | null = null
  private connected = false
  private pendingInvocations: Array<{ target: string; arguments: string[] }> = []
  private listeners: RealtimeListeners

  constructor(listeners: RealtimeListeners = {}) {
    this.listeners = listeners
  }

  async connect(): Promise<void> {
    if (typeof window === 'undefined' || typeof WebSocket === 'undefined') {
      return
    }
    if (this.socket && (this.socket.readyState === WebSocket.OPEN || this.socket.readyState === WebSocket.CONNECTING)) {
      return
    }

    try {
      let connectionToken = ''
      try {
        const res = await fetch('/hubs/tracking/negotiate?negotiateVersion=1', {
          method: 'POST',
        })
        if (res.ok) {
          const data = (await res.json()) as { connectionToken?: string; connectionId?: string }
          connectionToken = data.connectionToken || data.connectionId || ''
        }
      } catch {
        // Fallback para conexão WebSocket direta se negotiate não estiver disponível
      }

      const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:'
      const query = connectionToken ? `?id=${encodeURIComponent(connectionToken)}` : ''
      const wsUrl = `${protocol}//${window.location.host}/hubs/tracking${query}`

      this.socket = new WebSocket(wsUrl)

      this.socket.onopen = () => {
        this.socket?.send(`{"protocol":"json","version":1}${RECORD_SEPARATOR}`)
        this.connected = true
        for (const item of this.pendingInvocations) {
          this.sendInvocation(item.target, item.arguments)
        }
        this.pendingInvocations = []
      }

      this.socket.onmessage = (event) => {
        if (typeof event.data !== 'string') return
        const frames = event.data.split(RECORD_SEPARATOR).filter(Boolean)
        for (const frame of frames) {
          try {
            const msg = JSON.parse(frame) as {
              type?: number
              target?: string
              arguments?: unknown[]
            }

            if (msg.type === 6) {
              this.socket?.send(`{"type":6}${RECORD_SEPARATOR}`)
              continue
            }

            if (msg.type === 1 && msg.target && Array.isArray(msg.arguments)) {
              const payload = msg.arguments[0]
              if (msg.target === 'OrderStatusUpdated' && payload && this.listeners.onOrderStatusUpdated) {
                this.listeners.onOrderStatusUpdated(payload as OrderStatusUpdatedEvent)
              } else if (
                msg.target === 'DriverLocationUpdated' &&
                payload &&
                this.listeners.onDriverLocationUpdated
              ) {
                this.listeners.onDriverLocationUpdated(payload as DriverLocationUpdatedEvent)
              } else if (msg.target === 'DeliveryUpdated' && payload && this.listeners.onDeliveryUpdated) {
                this.listeners.onDeliveryUpdated(payload as DeliveryUpdatedEvent)
              }
            }
          } catch {
            // Ignora frames malformados
          }
        }
      }

      this.socket.onclose = () => {
        this.connected = false
        this.socket = null
      }
    } catch {
      this.connected = false
    }
  }

  joinOrderGroup(orderId: string): void {
    if (!orderId) return
    this.invoke('JoinOrderGroup', [orderId])
  }

  joinRestaurantGroup(restaurantId: string): void {
    if (!restaurantId) return
    this.invoke('JoinRestaurantGroup', [restaurantId])
  }

  joinUserGroup(userId: string): void {
    if (!userId) return
    this.invoke('JoinUserGroup', [userId])
  }

  disconnect(): void {
    this.connected = false
    this.pendingInvocations = []
    if (this.socket) {
      try {
        this.socket.close()
      } catch {
        // Ignora erro ao encerrar socket
      }
      this.socket = null
    }
  }

  private invoke(target: string, args: string[]): void {
    if (this.connected && this.socket?.readyState === WebSocket.OPEN) {
      this.sendInvocation(target, args)
    } else {
      this.pendingInvocations.push({ target, arguments: args })
    }
  }

  private sendInvocation(target: string, args: string[]): void {
    if (!this.socket || this.socket.readyState !== WebSocket.OPEN) return
    const payload = JSON.stringify({
      type: 1,
      target,
      arguments: args,
    })
    this.socket.send(`${payload}${RECORD_SEPARATOR}`)
  }
}
