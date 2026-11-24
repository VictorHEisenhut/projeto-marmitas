<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { AlertTriangle, CheckCircle2, Download, FileCheck2, KeyRound, ShieldCheck, Trash2 } from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppConfirmDialog from '@/components/ui/AppConfirmDialog.vue'
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

const deleteDialogOpen = ref(false)
const deletingAccount = ref(false)

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
    toast.success('Registro de consentimento LGPD atualizado.')
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
  deletingAccount.value = true
  try {
    await api.delete('/usuarios/me')
    deleteDialogOpen.value = false
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
    <!-- MFA Section -->
    <AppCard padding="lg" class="space-y-4">
      <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-start">
        <div class="flex items-start gap-3">
          <span class="flex h-10 w-10 shrink-0 items-center justify-center rounded-2xl bg-brand-50 text-brand-600 dark:bg-brand-950/60 dark:text-brand-400">
            <KeyRound class="h-5 w-5" />
          </span>
          <div>
            <div class="flex items-center gap-2">
              <h3 class="font-display text-base font-extrabold text-ink">
                Autenticação em Duas Etapas (MFA)
              </h3>
              <AppBadge :variant="mfaEnabled ? 'fresh' : 'neutral'" size="xs" dot>
                {{ mfaEnabled ? 'Ativo' : 'Desativado' }}
              </AppBadge>
            </div>
            <p class="mt-1 text-xs leading-relaxed text-ink-secondary">
              Adicione uma camada extra de segurança exigindo um código de 6 dígitos no login.
            </p>
          </div>
        </div>

        <AppButton
          v-if="!mfaEnabled"
          variant="primary"
          size="sm"
          :loading="mfaBusy"
          @click="handleEnableMfa"
        >
          <ShieldCheck class="h-4 w-4" />
          Ativar MFA
        </AppButton>
        <AppButton
          v-else
          variant="outline"
          size="sm"
          :loading="mfaBusy"
          @click="handleDisableMfa"
        >
          Desativar MFA
        </AppButton>
      </div>

      <div
        v-if="generatedMfaCode"
        class="rounded-2xl border border-fresh-200 bg-fresh-50/80 p-4 dark:border-fresh-800/60 dark:bg-fresh-950/35"
      >
        <div class="flex items-center gap-2 text-xs font-bold text-fresh-800 dark:text-fresh-300">
          <CheckCircle2 class="h-4 w-4" />
          <span>Guarde seu código de verificação MFA para os próximos logins:</span>
        </div>
        <p class="mt-2 font-mono text-2xl font-extrabold tracking-[0.3em] text-fresh-900 dark:text-fresh-200">
          {{ generatedMfaCode }}
        </p>
      </div>
    </AppCard>

    <!-- LGPD Privacy & Data Portability -->
    <AppCard padding="lg" class="space-y-5">
      <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-start">
        <div class="flex items-start gap-3">
          <span class="flex h-10 w-10 shrink-0 items-center justify-center rounded-2xl bg-sky-50 text-sky-600 dark:bg-sky-950/60 dark:text-sky-400">
            <FileCheck2 class="h-5 w-5" />
          </span>
          <div>
            <h3 class="font-display text-base font-extrabold text-ink">
              Privacidade & Portabilidade de Dados (LGPD)
            </h3>
            <p class="mt-1 text-xs leading-relaxed text-ink-secondary">
              Consulte seus consentimentos registrados e baixe uma cópia completa dos seus dados em formato JSON (Art. 18 LGPD).
            </p>
          </div>
        </div>

        <AppButton
          variant="outline"
          size="sm"
          :loading="exportingData"
          @click="handleExportData"
        >
          <Download class="h-4 w-4" />
          Exportar meus dados (JSON)
        </AppButton>
      </div>

      <div class="flex flex-wrap gap-2 border-t border-line pt-4">
        <AppButton
          variant="outline"
          size="xs"
          :loading="recordingConsent"
          @click="handleRecordConsent('TermsAndPrivacy', true)"
        >
          Renovar aceite dos Termos & Privacidade
        </AppButton>
        <AppButton
          variant="outline"
          size="xs"
          :loading="recordingConsent"
          @click="handleRecordConsent('MarketingCommunications', true)"
        >
          Autorizar comunicações promocionais
        </AppButton>
      </div>

      <div v-if="consents.length > 0" class="space-y-2">
        <p class="text-[11px] font-bold uppercase tracking-wider text-ink-muted">
          Histórico de consentimentos ({{ consents.length }})
        </p>
        <ul class="divide-y divide-line rounded-xl border border-line bg-surface-subtle px-4 text-xs">
          <li
            v-for="item in consents.slice(0, 6)"
            :key="item.id"
            class="flex items-center justify-between gap-2 py-2.5"
          >
            <div>
              <p class="font-semibold text-ink">{{ item.purpose }} ({{ item.termsVersion }})</p>
              <p class="text-[11px] text-ink-muted">
                {{ new Date(item.consentedAt).toLocaleString('pt-BR') }}
              </p>
            </div>
            <AppBadge :variant="item.isGranted ? 'fresh' : 'danger'" size="xs">
              {{ item.isGranted ? 'Concedido' : 'Revogado' }}
            </AppBadge>
          </li>
        </ul>
      </div>
      <p v-else-if="!loadingConsents" class="text-xs text-ink-muted">
        Nenhum consentimento explícito adicional registrado até o momento.
      </p>
    </AppCard>

    <!-- Danger Zone: Account Deletion & Anonymization -->
    <AppCard
      padding="lg"
      class="border-red-200 bg-red-50/40 dark:border-red-900/60 dark:bg-red-950/20"
    >
      <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <div class="flex items-start gap-3">
          <AlertTriangle class="mt-0.5 h-5 w-5 shrink-0 text-red-600 dark:text-red-400" />
          <div>
            <h3 class="font-display text-base font-extrabold text-red-800 dark:text-red-300">
              Zona de Perigo — Excluir e anonimizar conta
            </h3>
            <p class="mt-1 text-xs leading-relaxed text-red-700/90 dark:text-red-300/80">
              Esta ação cancela assinaturas ativas, remove endereços e favoritos e anonimiza seus dados pessoais permanentemente (irreversível).
            </p>
          </div>
        </div>

        <AppButton
          variant="danger"
          size="sm"
          class="shrink-0"
          @click="deleteDialogOpen = true"
        >
          <Trash2 class="h-4 w-4" />
          Excluir minha conta
        </AppButton>
      </div>
    </AppCard>

    <AppConfirmDialog
      :open="deleteDialogOpen"
      title="Excluir e anonimizar sua conta?"
      message="Esta ação é permanente e não poderá ser desfeita. Suas assinaturas ativas serão canceladas, seus tokens revogados e seus dados pessoais anonimizados."
      confirm-label="Sim, excluir permanentemente"
      variant="danger"
      :loading="deletingAccount"
      @confirm="handleDeleteAccount"
      @cancel="deleteDialogOpen = false"
    />
  </div>
</template>
