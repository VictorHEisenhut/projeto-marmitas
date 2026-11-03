export type UserRole =
  | 'customer'
  | 'restaurant_owner'
  | 'restaurant_staff'
  | 'driver'
  | 'admin'
  | 'super_admin'

export interface User {
  id: string
  name: string
  email: string
  phone?: string | null
  status: string
  roles: string[]
}

export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  name: string
  email: string
  phone?: string | null
  password: string
  role?: string
}

export interface LoginResponse {
  accessToken: string
  tokenType: string
  expiresAt: string
  user: User
}

export interface ForgotPasswordResponse {
  message: string
  email: string
}

export interface ResetPasswordRequest {
  email: string
  newPassword: string
}

export interface Restaurant {
  id: string
  name: string
  description: string
  document: string
  imageUrl?: string | null
  status: string
  ownerId: string
}

export interface Category {
  id: string
  restaurantId?: string | null
  name: string
  displayOrder: number
}

export interface Product {
  id: string
  restaurantId: string
  categoryId: string
  categoryName: string
  name: string
  description: string
  price: number
  isActive: boolean
  imageUrl?: string | null
}

export interface Plan {
  id: string
  name: string
  description: string
  mealsPerWeek: number
  monthlyPrice: number
  isActive: boolean
}

export interface Subscription {
  id: string
  userId: string
  planId: string
  planName: string
  monthlyPrice: number
  status: string
  startDate: string
  nextBillingDate: string
}

export interface Address {
  id: string
  userId: string
  street: string
  number: string
  complement?: string | null
  neighborhood: string
  city: string
  state: string
  zipCode: string
  isMain: boolean
}

export interface Favorite {
  id: string
  userId: string
  restaurantId?: string | null
  productId?: string | null
}

export interface OrderItem {
  id: string
  productId: string
  productName: string
  unitPrice: number
  quantity: number
  totalPrice: number
}

export interface Order {
  id: string
  customerId: string
  restaurantId: string
  restaurantName: string
  totalAmount: number
  status: string
  createdAt: string
  updatedAt: string
  items: OrderItem[]
}

export interface CreateOrderResponse {
  orderId: string
  totalAmount: number
  status: string
}

export interface CouponValidationResponse {
  valid: boolean
  couponId: string
  code: string
  discountPercentage: number
  discountAmount: number
  finalAmount: number
}

export interface RestaurantFinancials {
  restaurantId: string
  totalOrders: number
  grossRevenue: number
  platformCommission: number
  netRevenue: number
}

export interface Courier {
  id: string
  userId?: string | null
  name: string
  document: string
  phone: string
  status: string
  isAvailable: boolean
}

export interface CourierEarnings {
  courierId: string
  completedDeliveries: number
  totalEarnings: number
}

export interface Delivery {
  id: string
  orderId: string
  courierId?: string | null
  status: string
  createdAt?: string
  updatedAt?: string
}

export interface CourierDocument {
  id: string
  courierId: string
  type: string
  number: string
  status: string
}

export interface CourierVehicle {
  id: string
  courierId: string
  type: string
  model: string
  licensePlate: string
}

export interface AdminDashboard {
  totalUsers: number
  totalRestaurants: number
  totalOrders: number
  totalRevenue: number
}
