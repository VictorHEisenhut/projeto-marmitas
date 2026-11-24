<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import {
  AlertTriangle,
  CheckCircle2,
  Download,
  FileCheck2,
  KeyRound,
  ShieldCheck,
  Trash2,
} from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppModal from '@/components/ui/AppModal.vue'
import AppInput from '@/components/ui/AppInput.vue'
import { api, getErrorMessage } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'
import type { MfaEnableResponse, UserConsent } from '@/types'

const auth = useAuthStore()
const toast = useToastStore()
const router = useRouter()

const mfaEnabled = ref(Boolean(auth.user?.mfaEnabled))
const mfaBusy = ref(false)
const generatedMfaCode = ref('')

const consents = ref<UserConsent[]>([])
const loadingConsents = ref(false)
const recordingConsent = ref(false)
const exportingData = ref(false)

const deleteModalOpen = ref(false)
const deleteConfirmText = ref('')
const deletingAccount = ref(false)

const hasTermsConsent = computed(() =>
  consents.value.some((c) => c.purpose === 'TermsAndPrivacy' && c.isGranted),
)

const securityScore = computed(() => {
  let score = 40 // Base active authenticated account
  if (mfaEnabled.value) score += 35
  if (hasTermsConsent.value || consents.value.length > 0) score += 25
  return Math.min(100, score)
})

async function loadConsents() {
  loadingConsents.value = true
  try {
    const { data } = await api.get<UserConsent[]>('/usuarios/me/consentimentos')
    consents.value = data
  } catch {
    consents.value = []
  } finally {
    loadingConsents.value = false
  }
}

async function handleEnableMfa() {
  mfaBusy.value = true
  try {
    const { data } = await api.post<MfaEnableResponse>('/auth/mfa/ativar')
    mfaEnabled.value = true
    generatedMfaCode.value = data.verificationCode || ''
    if (auth.user) auth.user.mfaEnabled = true
    toast.success(data.message || 'MFA ativado com sucesso!')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível ativar o MFA.'))
  } finally {
    mfaBusy.value = false
  }
}

async function handleDisableMfa() {
  mfaBusy.value = true
  try {
    const { data } = await api.post<MfaEnableResponse>('/auth/mfa/desativar')
    mfaEnabled.value = false
    generatedMfaCode.value = ''
    if (auth.user) auth.user.mfaEnabled = false
    toast.info(data.message || 'MFA desativado.')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível desativar o MFA.'))
  } finally {
    mfaBusy.value = false
  }
}

async function handleRecordConsent(purpose: string, isGranted: boolean) {
  recordingConsent.value = true
  try {
    const { data } = await api.post<UserConsent>('/usuarios/me/consentimentos', {
      purpose,
      termsVersion: 'v1.0',
      isGranted,
    })
    consents.value.unshift(data)
    toast.success('Consentimento LGPD atualizado.')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível registrar o consentimento.'))
  } finally {
    recordingConsent.value = false
  }
}

async function handleExportData() {
  exportingData.value = true
  try {
    const { data } = await api.get('/usuarios/me/exportar-dados')
    const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `meus-dados-marmitas-${new Date().toISOString().slice(0, 10)}.json`
    document.body.appendChild(link)
    link.click()
    link.remove()
    URL.revokeObjectURL(url)
    toast.success('Arquivo JSON com seus dados exportado com sucesso!')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível exportar seus dados.'))
  } finally {
    exportingData.value = false
  }
}

async function handleDeleteAccount() {
  if (deleteConfirmText.value.trim().toUpperCase() !== 'EXCLUIR') return
  deletingAccount.value = true
  try {
    await api.delete('/usuarios/me')
    deleteModalOpen.value = false
    auth.clearSession()
    toast.info('Sua conta foi excluída e seus dados foram anonimizados conforme a LGPD.')
    await router.replace('/login')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível excluir sua conta.'))
  } finally {
    deletingAccount.value = false
  }
}

onMounted(() => {
  mfaEnabled.value = Boolean(auth.user?.mfaEnabled)
  void loadConsents()
})
</script>

<template>
  <div class="space-y-6">
    <!-- Trust Center Health Score Banner -->
    <AppCard
      padding="lg"
      class="border-brand-200 bg-gradient-to-r from-surface via-brand-50/40 to-fresh-50/50 dark:border-brand-900/50 dark:from-surface dark:via-brand-950/20 dark:to-fresh-950/20"
    >
      <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <div class="space-y-1">
          <div class="flex items-center gap-2">
            <ShieldCheck class="h-5 w-5 text-brand-600" />
            <h2 class="font-display text-lg font-extrabold text-ink">Trust Center & Privacidade</h2>
          </div>
          <p class="text-xs text-ink-secondary">
            {{
              securityScore >= 75
                ? 'Excelente! Sua conta está protegida com verificação adicional e controles LGPD.'
                : 'Ative a autenticação em duas etapas (MFA) para alcançar proteção máxima.'
            }}
          </p>
        </div>

        <div class="flex items-center gap-3 shrink-0">
          <div class="text-right">
            <p class="text-[11px] font-bold uppercase tracking-wider text-ink-muted">Saúde da conta</p>
            <p class="font-display text-2xl font-extrabold text-ink tabular-nums">{{ securityScore }}%</p>
          </div>
          <div class="h-12 w-2 overflow-hidden rounded-full bg-surface-muted flex flex-col justify-end">
            <div
              class="w-full rounded-full transition-all duration-500"
              :class="securityScore >= 75 ? 'bg-fresh-600' : 'bg-amber-500'"
              :style="{ height: `${securityScore}%` }"
            />
          </div>
        </div>
      </div>
    </AppCard>

    <!-- 2x2 Trust Center Grid -->
    <div class="grid gap-5 md:grid-cols-2">
      <!-- Card 1: MFA -->
      <AppCard padding="lg" class="flex flex-col justify-between space-y-4">
        <div class="space-y-3">
          <div class="flex items-start justify-between gap-2">
            <div class="flex items-center gap-2.5">
              <span class="flex h-10 w-10 items-center justify-center rounded-2xl bg-brand-50 text-brand-600 dark:bg-brand-950/60 dark:text-brand-400">
                <KeyRound class="h-5 w-5" />
              </span>
              <div>
                <h3 class="font-display text-base font-extrabold text-ink">Duas Etapas (MFA)</h3>
                <AppBadge :variant="mfaEnabled ? 'fresh' : 'warning'" size="xs" dot>
                  {{ mfaEnabled ? 'Ativo' : 'Recomendado ativar' }}
                </AppBadge>
              </div>
            </div>
          </div>

          <p class="text-xs leading-relaxed text-ink-secondary">
            Exige um código de 6 dígitos durante o login para impedir acessos não autorizados.
          </p>

          <div
            v-if="generatedMfaCode"
            class="rounded-2xl border border-fresh-200 bg-fresh-50/80 p-3.5 dark:border-fresh-800/60 dark:bg-fresh-950/35"
          >
            <div class="flex items-center gap-1.5 text-[11px] font-bold text-fresh-800 dark:text-fresh-300">
              <CheckCircle2 class="h-3.5 w-3.5" />
              <span>Seu código MFA configurado:</span>
            </div>
            <p class="mt-1 font-mono text-2xl font-extrabold tracking-[0.3em] text-fresh-900 dark:text-fresh-200">
              {{ generatedMfaCode }}
            </p>
          </div>
        </div>

        <AppButton
          v-if="!mfaEnabled"
          variant="primary"
          size="sm"
          block
          :loading="mfaBusy"
          @click="handleEnableMfa"
        >
          <ShieldCheck class="h-4 w-4" />
          Ativar MFA agora
        </AppButton>
        <AppButton
          v-else
          variant="outline"
          size="sm"
          block
          :loading="mfaBusy"
          @click="handleDisableMfa"
        >
          Desativar MFA
        </AppButton>
      </AppCard>

      <!-- Card 2: Data Portability -->
      <AppCard padding="lg" class="flex flex-col justify-between space-y-4">
        <div class="space-y-3">
          <div class="flex items-center gap-2.5">
            <span class="flex h-10 w-10 items-center justify-center rounded-2xl bg-sky-50 text-sky-600 dark:bg-sky-950/60 dark:text-sky-400">
              <Download class="h-5 w-5" />
            </span>
            <div>
              <h3 class="font-display text-base font-extrabold text-ink">Portabilidade (Art. 18 LGPD)</h3>
              <AppBadge variant="info" size="xs">Formato JSON</AppBadge>
            </div>
          </div>

          <p class="text-xs leading-relaxed text-ink-secondary">
            Baixe uma cópia completa e estruturada dos seus dados cadastrais, endereços, favoritos, assinaturas e histórico de pedidos.
          </p>
        </div>

        <AppButton
          variant="outline"
          size="sm"
          block
          :loading="exportingData"
          @click="handleExportData"
        >
          <Download class="h-4 w-4" />
          Exportar meus dados (JSON)
        </AppButton>
      </AppCard>

      <!-- Card 3: Consents -->
      <AppCard padding="lg" class="space-y-4">
        <div class="flex items-center gap-2.5">
          <span class="flex h-10 w-10 items-center justify-center rounded-2xl bg-fresh-50 text-fresh-600 dark:bg-fresh-950/60 dark:text-fresh-400">
            <FileCheck2 class="h-5 w-5" />
          </span>
          <div>
            <h3 class="font-display text-base font-extrabold text-ink">Consentimentos LGPD</h3>
            <p class="text-xs text-ink-muted">{{ consents.length }} registro(s) auditável(is)</p>
          </div>
        </div>

        <div class="flex flex-wrap gap-2">
          <AppButton
            variant="outline"
            size="xs"
            :loading="recordingConsent"
            @click="handleRecordConsent('TermsAndPrivacy', true)"
          >
            Confirmar Termos & Privacidade
          </AppButton>
          <AppButton
            variant="outline"
            size="xs"
            :loading="recordingConsent"
            @click="handleRecordConsent('MarketingCommunications', true)"
          >
            Autorizar Comunicações
          </AppButton>
        </div>

        <ul v-if="consents.length > 0" class="max-h-36 divide-y divide-line overflow-y-auto text-xs">
          <li
            v-for="item in consents.slice(0, 6)"
            :key="item.id"
            class="flex items-center justify-between gap-2 py-2"
          >
            <div class="min-w-0">
              <p class="truncate font-semibold text-ink">{{ item.purpose }} ({{ item.termsVersion }})</p>
              <p class="text-[10px] text-ink-muted">{{ new Date(item.consentedAt).toLocaleString('pt-BR') }}</p>
            </div>
            <AppBadge :variant="item.isGranted ? 'fresh' : 'danger'" size="xs">
              {{ item.isGranted ? 'Concedido' : 'Revogado' }}
            </AppBadge>
          </li>
        </ul>
        <p v-else-if="!loadingConsents" class="text-xs text-ink-muted">
          Nenhum consentimento adicional registrado.
        </p>
      </AppCard>

      <!-- Card 4: Danger Zone -->
      <AppCard
        padding="lg"
        class="flex flex-col justify-between space-y-4 border-red-200 bg-red-50/40 dark:border-red-900/60 dark:bg-red-950/20"
      >
        <div class="space-y-3">
          <div class="flex items-center gap-2.5">
            <span class="flex h-10 w-10 items-center justify-center rounded-2xl bg-red-100 text-red-600 dark:bg-red-950/70 dark:text-red-400">
              <AlertTriangle class="h-5 w-5" />
            </span>
            <div>
              <h3 class="font-display text-base font-extrabold text-red-800 dark:text-red-300">
                Zona de Perigo
              </h3>
              <p class="text-xs text-red-700/80 dark:text-red-300/80">Exclusão e anonimização irreversível</p>
            </div>
          </div>

          <p class="text-xs leading-relaxed text-red-700/90 dark:text-red-300/80">
            Cancela assinaturas ativas, apaga endereços/favoritos e anonimiza sua identidade em conformidade com a LGPD.
          </p>
        </div>

        <AppButton
          variant="danger"
          size="sm"
          block
          @click="deleteConfirmText = ''; deleteModalOpen = true"
        >
          <Trash2 class="h-4 w-4" />
          Excluir e anonimizar conta
        </AppButton>
      </AppCard>
    </div>

    <!-- Typed Confirmation Modal for Account Deletion -->
    <AppModal
      :open="deleteModalOpen"
      title="Confirmar exclusão permanente"
      subtitle="Esta ação não pode ser desfeita."
      max-width="sm"
      :sheet-on-mobile="false"
      @close="deleteModalOpen = false"
    >
      <div class="space-y-4">
        <p class="text-xs leading-relaxed text-ink-secondary">
          Para confirmar a exclusão e anonimização definitiva da sua conta, digite
          <strong class="font-mono text-red-600 dark:text-red-400">EXCLUIR</strong> no campo abaixo:
        </p>

        <AppInput
          v-model="deleteConfirmText"
          placeholder="Digite EXCLUIR"
          input-class="font-mono uppercase"
        />
      </div>

      <template #footer>
        <div class="flex justify-end gap-2">
          <AppButton variant="outline" size="sm" @click="deleteModalOpen = false">Cancelar</AppButton>
          <AppButton
            variant="danger"
            size="sm"
            :disabled="deleteConfirmText.trim().toUpperCase() !== 'EXCLUIR'"
            :loading="deletingAccount"
            @click="handleDeleteAccount"
          >
            Excluir conta
          </AppButton>
        </div>
      </template>
    </AppModal>
  </div>
</template>
