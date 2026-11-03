import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

declare module 'vue-router' {
  interface RouteMeta {
    title?: string
    requiresAuth?: boolean
    guestOnly?: boolean
    roles?: string[]
  }
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: () => import('@/views/HomeView.vue'),
      meta: { title: 'Início' },
    },
    {
      path: '/restaurantes/:id',
      name: 'restaurant-menu',
      component: () => import('@/views/RestaurantMenuView.vue'),
      meta: { title: 'Cardápio' },
    },
    {
      path: '/pedidos',
      name: 'orders',
      component: () => import('@/views/OrdersView.vue'),
      meta: {
        title: 'Meus Pedidos',
        requiresAuth: true,
        roles: ['customer', 'admin', 'super_admin'],
      },
    },
    {
      path: '/conta',
      name: 'profile',
      component: () => import('@/views/ProfileView.vue'),
      meta: {
        title: 'Minha Conta',
        requiresAuth: true,
      },
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/LoginView.vue'),
      meta: { title: 'Entrar', guestOnly: true },
    },
    {
      path: '/cadastro',
      name: 'register',
      component: () => import('@/views/RegisterView.vue'),
      meta: { title: 'Criar Conta', guestOnly: true },
    },
    {
      path: '/esqueci-senha',
      name: 'forgot-password',
      component: () => import('@/views/ForgotPasswordView.vue'),
      meta: { title: 'Recuperar Senha', guestOnly: true },
    },
    {
      path: '/redefinir-senha',
      name: 'reset-password',
      component: () => import('@/views/ResetPasswordView.vue'),
      meta: { title: 'Redefinir Senha' },
    },
    {
      path: '/entregador/cadastro',
      name: 'courier-register',
      component: () => import('@/views/CourierRegisterView.vue'),
      meta: { title: 'Cadastro de Entregador' },
    },
    {
      path: '/parceiro',
      name: 'partner',
      component: () => import('@/views/PartnerView.vue'),
      meta: {
        title: 'Área do Restaurante',
        requiresAuth: true,
        roles: ['restaurant_owner', 'restaurant_staff', 'admin', 'super_admin'],
      },
    },
    {
      path: '/entregador',
      name: 'courier',
      component: () => import('@/views/CourierView.vue'),
      meta: {
        title: 'Área do Entregador',
        requiresAuth: true,
        roles: ['driver', 'admin', 'super_admin'],
      },
    },
    {
      path: '/admin',
      name: 'admin',
      component: () => import('@/views/AdminView.vue'),
      meta: {
        title: 'Administração',
        requiresAuth: true,
        roles: ['admin', 'super_admin'],
      },
    },
    {
      path: '/acesso-negado',
      name: 'forbidden',
      component: () => import('@/views/ForbiddenView.vue'),
      meta: { title: 'Acesso Negado' },
    },
    {
      path: '/:pathMatch(.*)*',
      redirect: '/',
    },
  ],
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()
  if (!auth.initialized) {
    await auth.init()
  }

  document.title = to.meta.title ? `${to.meta.title} | Marmitas` : 'Marmitas'

  if (to.meta.guestOnly && auth.isAuthenticated) {
    return auth.defaultRoute
  }

  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return {
      path: '/login',
      query: { redirect: to.fullPath },
    }
  }

  if (to.meta.roles && to.meta.roles.length > 0 && !auth.hasRole(to.meta.roles)) {
    return '/acesso-negado'
  }

  return true
})

export default router
