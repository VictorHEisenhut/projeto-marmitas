<script setup lang="ts">
import { ref, watch } from 'vue'
import { Check, ExternalLink, Plug, ShieldCheck, Unplug } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppInput from '@/components/ui/AppInput.vue'
import AppToggle from '@/components/ui/AppToggle.vue'
import AppConfirmDialog from '@/components/ui/AppConfirmDialog.vue'
import { api, getErrorMessage } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import type { RestaurantIntegrationStatus } from '@/types'

const props = defineProps<{
  restaurantId: string
  restaurantName: string
}>()

const toast = useToastStore()

const loading = ref(true)
const saving = ref(false)
const disconnecting = ref(false)
const confirmDisconnect = ref(false)
const status = ref<RestaurantIntegrationStatus | null>(null)
const accessToken = ref('')
const publicKey = ref('')
const enabled = ref(true)

async function loadIntegration(restaurantId: string) {
  loading.value = true
  try {
    const { data } = await api.get<RestaurantIntegrationStatus>(
      `/restaurantes/${restaurantId}/integracoes/mercadopago`,
    )
    status.value = data
    publicKey.value = data.publicKey || ''
    enabled.value = data.connected ? data.enabled : true
    accessToken.value = ''
  } catch (err) {
    status.value = null
    toast.error(getErrorMessage(err, 'Não foi possível carregar as integrações.'))
  } finally {
    loading.value = false
  }
}

watch(
  () => props.restaurantId,
  (id) => {
    if (id) void loadIntegration(id)
  },
  { immediate: true },
)

async function saveIntegration() {
  if (!publicKey.value.trim()) {
    toast.warning('Informe a Public Key do Mercado Pago.')
    return
  }
  if (!status.value?.connected && !accessToken.value.trim()) {
    toast.warning('Informe o Access Token do Mercado Pago.')
    return
  }

  saving.value = true
  try {
    const { data } = await api.put<RestaurantIntegrationStatus>(
      `/restaurantes/${props.restaurantId}/integracoes/mercadopago`,
      {
        accessToken: accessToken.value.trim(),
        publicKey: publicKey.value.trim(),
        enabled: enabled.value,
      },
    )
    status.value = data
    publicKey.value = data.publicKey || publicKey.value
    accessToken.value = ''
    toast.success(
      data.enabled
        ? 'Mercado Pago conectado. Os clientes já podem pagar com PIX ou cartão.'
        : 'Credenciais salvas. Ative a integração para liberar o pagamento.',
    )
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível validar as credenciais no Mercado Pago.'))
  } finally {
    saving.value = false
  }
}

async function disconnect() {
  disconnecting.value = true
  try {
    await api.delete(`/restaurantes/${props.restaurantId}/integracoes/mercadopago`)
    status.value = {
      provider: 'MercadoPago',
      enabled: false,
      connected: false,
    }
    accessToken.value = ''
    publicKey.value = ''
    enabled.value = true
    confirmDisconnect.value = false
    toast.success('Mercado Pago desconectado desta loja.')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível desconectar o Mercado Pago.'))
  } finally {
    disconnecting.value = false
  }
}
</script>

<template>
  <section class="space-y-5">
    <div class="flex flex-wrap items-end justify-between gap-3">
      <div>
        <p class="text-xs font-bold uppercase tracking-wider text-ink-muted">Integrações</p>
        <h2 class="font-display text-2xl font-extrabold text-ink">{{ restaurantName }}</h2>
        <p class="mt-1 max-w-2xl text-sm text-ink-secondary">
          Conecte a conta Mercado Pago desta loja. O cliente paga no checkout com PIX ou cartão de crédito, e o valor entra na conta da empresa.
        </p>
      </div>
    </div>

    <AppCard padding="lg" class="space-y-6">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div class="flex items-start gap-3">
          <span class="flex h-12 w-12 items-center justify-center rounded-2xl bg-[#009ee3]/10 text-[#009ee3]">
            <Plug class="h-6 w-6" />
          </span>
          <div>
            <div class="flex flex-wrap items-center gap-2">
              <h3 class="font-display text-lg font-extrabold text-ink">Mercado Pago</h3>
              <AppBadge v-if="status?.connected && status.enabled" variant="fresh" dot>Conectado</AppBadge>
              <AppBadge v-else-if="status?.connected" variant="warning" dot>Salvo e desativado</AppBadge>
              <AppBadge v-else variant="neutral">Não conectado</AppBadge>
              <AppBadge v-if="status?.environment === 'test'" variant="info">Teste</AppBadge>
              <AppBadge v-else-if="status?.environment === 'production'" variant="brand">Produção</AppBadge>
            </div>
            <p class="mt-1 text-sm text-ink-secondary">
              PIX com QR Code e cartão de crédito no checkout, usando as credenciais desta empresa.
            </p>
          </div>
        </div>
        <a
          href="https://www.mercadopago.com.br/developers/panel/app"
          target="_blank"
          rel="noreferrer"
          class="inline-flex items-center gap-1.5 text-xs font-bold text-brand-600 hover:underline"
        >
          Abrir credenciais
          <ExternalLink class="h-3.5 w-3.5" />
        </a>
      </div>

      <ol class="grid gap-3 text-sm text-ink-secondary md:grid-cols-3">
        <li class="rounded-2xl border border-line bg-surface-subtle p-3">
          <span class="font-display text-xs font-extrabold text-brand-600">1</span>
          <p class="mt-1 font-semibold text-ink">Crie a aplicação</p>
          <p class="mt-1 text-xs">No painel de desenvolvedores do Mercado Pago, em Suas integrações.</p>
        </li>
        <li class="rounded-2xl border border-line bg-surface-subtle p-3">
          <span class="font-display text-xs font-extrabold text-brand-600">2</span>
          <p class="mt-1 font-semibold text-ink">Copie as duas chaves</p>
          <p class="mt-1 text-xs">Access Token e Public Key do mesmo ambiente, teste ou produção.</p>
        </li>
        <li class="rounded-2xl border border-line bg-surface-subtle p-3">
          <span class="font-display text-xs font-extrabold text-brand-600">3</span>
          <p class="mt-1 font-semibold text-ink">Salve e valide</p>
          <p class="mt-1 text-xs">Confirmamos o token na conta Mercado Pago antes de liberar o checkout.</p>
        </li>
      </ol>

      <div v-if="loading" class="text-sm text-ink-muted">Carregando integração...</div>

      <form v-else class="space-y-4" @submit.prevent="saveIntegration">
        <div
          v-if="status?.connected"
          class="rounded-2xl border border-fresh-200 bg-fresh-50/70 p-4 text-sm dark:border-fresh-800/60 dark:bg-fresh-950/30"
        >
          <p class="font-bold text-fresh-800 dark:text-fresh-300">
            {{ status.accountEmail || 'Conta Mercado Pago validada' }}
          </p>
          <p class="mt-1 text-xs text-fresh-800/80 dark:text-fresh-300/80">
            Token salvo: {{ status.accessTokenHint || '••••' }}
            <span v-if="status.accountId"> · Conta {{ status.accountId }}</span>
          </p>
        </div>

        <AppInput
          v-model="accessToken"
          type="password"
          autocomplete="off"
          :label="status?.connected ? 'Novo Access Token' : 'Access Token'"
          :required="!status?.connected"
          :placeholder="status?.connected ? 'Deixe em branco para manter o token atual' : 'TEST-... ou APP_USR-...'"
          hint="Credencial secreta. Ela é criptografada e não volta completa para esta tela."
        />

        <AppInput
          v-model="publicKey"
          autocomplete="off"
          label="Public Key"
          required
          placeholder="TEST-... ou APP_USR-..."
          hint="Usada só no navegador do cliente para tokenizar o cartão. O número do cartão não passa pelos nossos servidores."
        />

        <AppToggle
          v-model="enabled"
          label="Liberar pagamento nesta loja"
          description="Com a integração ativa, o checkout oferece PIX e cartão de crédito."
        />

        <div class="flex flex-wrap items-center gap-3">
          <AppButton type="submit" variant="primary" :loading="saving">
            <Check class="h-4 w-4" />
            Salvar e validar
          </AppButton>
          <AppButton
            v-if="status?.connected"
            type="button"
            variant="outline"
            :disabled="saving"
            @click="confirmDisconnect = true"
          >
            <Unplug class="h-4 w-4" />
            Desconectar
          </AppButton>
        </div>
      </form>

      <div class="flex items-start gap-2 rounded-2xl border border-line bg-surface-subtle p-3 text-xs text-ink-secondary">
        <ShieldCheck class="mt-0.5 h-4 w-4 shrink-0 text-fresh-600" />
        <p>
          No checkout, o PIX gera o QR Code na conta desta loja e a confirmação chega automaticamente.
          O cartão é tokenizado com a Public Key e cobrado com o Access Token no servidor.
          Enquanto esta loja não conectar o Mercado Pago, o pagamento continua em modo de demonstração.
        </p>
      </div>
    </AppCard>

    <AppConfirmDialog
      :open="confirmDisconnect"
      title="Desconectar Mercado Pago"
      message="Os clientes desta loja deixam de pagar com a conta Mercado Pago conectada. Você pode informar um novo token depois."
      confirm-label="Desconectar"
      variant="danger"
      :loading="disconnecting"
      @confirm="disconnect"
      @cancel="confirmDisconnect = false"
    />
  </section>
</template>
