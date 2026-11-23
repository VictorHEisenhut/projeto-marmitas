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
  mfaEnabled?: boolean
  roles: string[]
}

export interface LoginRequest {
  email: string
  password: string
  mfaCode?: string
}

export interface RegisterRequest {
  name: string
  email: string
  phone?: string | null
  password: string
  role?: string
  termsAccepted?: boolean
}

export interface LoginResponse {
  accessToken: string
  refreshToken?: string
  tokenType: string
  expiresAt: string
  user: User
}

export interface ForgotPasswordResponse {
  message: string
  email: string
  resetCode?: string
}

export interface ResetPasswordRequest {
  email: string
  newPassword: string
  resetCode?: string
}

export interface Restaurant {
  id: string
  name: string
  description: string
  document: string
  imageUrl?: string | null
  status: string
  ownerId: string
  addressStreet?: string
  addressNumber?: string
  addressComplement?: string | null
  addressNeighborhood?: string
  addressCity?: string
  addressState?: string
  addressZipCode?: string
  latitude?: number | null
  longitude?: number | null
  openingHours?: string
  deliveryFee?: number
  deliveryRadiusKm?: number
  bankName?: string | null
  bankAgency?: string | null
  bankAccount?: string | null
  pixKey?: string | null
}

export interface Category {
  id: string
  restaurantId?: string | null
  name: string
  displayOrder: number
}

export interface ProductAddon {
  name: string
  extraPrice: number
  maxQuantity: number
}

export interface Product {
  id: string
  restaurantId: string
  categoryId: string
  categoryName: string
  name: string
  description: string
  price: number
  promotionalPrice?: number | null
  dailyStock?: number | null
  isActive: boolean
  imageUrl?: string | null
  availableInPlans?: boolean
  allowsCutlery?: boolean
  cutleryHasExtraCost?: boolean
  cutleryPrice?: number
  addons?: ProductAddon[]
}

export interface Plan {
  id: string
  name: string
  description: string
  mealsPerWeek: number
  mealsPerMonth?: number
  monthlyPrice: number
  frequency?: string
  allowedDays?: string
  deliveryWindow?: string
  deliveryFee?: number
  cancellationPolicy?: string
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
  totalMealsInCycle?: number
  usedMealsInCycle?: number
  preferredRestaurantId?: string | null
  defaultAddressId?: string | null
}

export interface SubscriptionDelivery {
  id: string
  subscriptionId: string
  restaurantId?: string | null
  addressId?: string | null
  scheduledDate: string
  deliveryWindow: string
  mealsCount: number
  status: string
}

export interface DeliveryZone {
  id: string
  restaurantId: string
  neighborhood: string
  city: string
  radiusKm: number
  deliveryFee: number
  estimatedMinutes: number
  isActive: boolean
}

export interface RestaurantTeamMember {
  id: string
  restaurantId: string
  userId: string
  userName: string
  userEmail: string
  role: string
  isActive: boolean
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
  latitude?: number | null
  longitude?: number | null
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
  customerName?: string
  restaurantId: string
  restaurantName: string
  subscriptionId?: string | null
  subtotal?: number
  deliveryFee?: number
  totalAmount: number
  discountAmount?: number
  couponCode?: string | null
  deliveryAddress?: string | null
  confirmationPin?: string
  status: string
  createdAt: string
  updatedAt: string
  complaintMessage?: string | null
  complaintCreatedAt?: string | null
  complaintReply?: string | null
  complaintRepliedAt?: string | null
  items: OrderItem[]
}

export interface ReviewItem {
  id: string
  orderId?: string
  restaurantId?: string
  userId?: string
  rating: number
  comment: string
  targetType?: string
  authorName?: string
  restaurantName?: string
  createdAt?: string
}

export interface CreateOrderResponse {
  orderId: string
  subtotal?: number
  deliveryFee?: number
  totalAmount: number
  discountAmount?: number
  couponCode?: string | null
  deliveryAddress?: string | null
  confirmationPin?: string
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
  latitude?: number | null
  longitude?: number | null
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
  deliveryFee?: number
  distanceKm?: number
  confirmationPin?: string
  createdAt?: string
  updatedAt?: string
  restaurantName?: string
  restaurantAddress?: string
  deliveryNeighborhood?: string
  deliveryAddress?: string
}

export interface CourierBillingAddress {
  street: string
  number: string
  complement?: string | null
  neighborhood: string
  city: string
  state: string
  zipCode: string
  isComplete?: boolean
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

export interface RegionOrderSummary {
  region: string
  count: number
  revenue: number
}

export interface AdminDashboard {
  totalUsers: number
  totalCustomers?: number
  totalRestaurants: number
  totalCouriers?: number
  totalOrders: number
  totalRevenue: number
  averageTicket?: number
  totalCommissions?: number
  activeSubscriptions?: number
  monthlyRecurringRevenue?: number
  churnRatePercentage?: number
  cancelledOrders?: number
  averageDeliveryMinutes?: number
  ordersByRegion?: RegionOrderSummary[]
}

export interface Coupon {
  id: string
  code: string
  discountPercentage: number
  isActive: boolean
  expiresAt: string
}

export interface Payout {
  id: string
  targetId: string
  targetType: string
  grossAmount: number
  netAmount: number
  status: string
  processedAt?: string | null
}

export interface CommissionSummary {
  commissionRatePercentage: number
  grossVolume: number
  totalCommission: number
}

export interface SubscriptionsReport {
  totalActiveSubscriptions: number
  totalCancelledSubscriptions: number
  monthlyRecurringRevenue: number
  churnRatePercentage?: number
}

export interface DeliveriesReport {
  totalDeliveries: number
  inTransitDeliveries: number
  cancelledDeliveries: number
  averageDeliveryMinutes?: number
}

export interface AuditLog {
  id: string
  action: string
  details: string
  timestamp: string
  entityName?: string
  entityId?: string
}

export interface NotificationItem {
  id: string
  userId: string
  title: string
  message: string
  isRead: boolean
  createdAt: string
}

export interface NotificationPreferences {
  emailEnabled: boolean
  pushEnabled: boolean
  smsEnabled: boolean
}

export interface DeliveryTracking {
  deliveryId: string
  orderId: string
  status: string
  courierName?: string | null
  latitude?: number | null
  longitude?: number | null
  updatedAt: string
}

export interface NutritionalEstimate {
  calories: number
  protein: number
  carbs: number
  fat: number
}

export interface AiRecommendationRequest {
  restaurantId?: string | null
  query: string
  dietaryGoal?: string | null
  maxPrice?: number | null
}

export interface AiRecommendationItem {
  productId: string
  productName: string
  price: number
  restaurantName?: string | null
  whyRecommended: string
  nutritionalEstimate: NutritionalEstimate
  tags: string[]
}

export interface AiRecommendationResponse {
  generalAdvice: string
  recommendations: AiRecommendationItem[]
  isLiveAi: boolean
}

export interface AiGenerateMarmitaRequest {
  baseNameOrIngredients: string
  targetCategory?: string | null
  price?: number | null
}

export interface AiGenerateMarmitaResponse {
  suggestedName: string
  gourmetDescription: string
  suggestedCategory: string
  nutritionalEstimate: NutritionalEstimate
  highlightIngredients: string[]
  isLiveAi: boolean
}

export interface AiStatusResponse {
  isConfigured: boolean
  model: string
  provider: string
}
