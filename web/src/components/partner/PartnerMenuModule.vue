<script setup lang="ts">
import { computed, ref } from 'vue'
import {
  Edit3,
  Loader2,
  Plus,
  Search,
  Sparkles,
  Trash2,
  Upload,
  UtensilsCrossed,
} from 'lucide-vue-next'
import AppCard from '@/components/ui/AppCard.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppInput from '@/components/ui/AppInput.vue'
import AppToggle from '@/components/ui/AppToggle.vue'
import AppDrawer from '@/components/ui/AppDrawer.vue'
import AppEmptyState from '@/components/ui/AppEmptyState.vue'
import AppConfirmDialog from '@/components/ui/AppConfirmDialog.vue'
import { api, formatCurrency, getErrorMessage, uploadFile } from '@/services/api'
import { generateMarmitaWithAi } from '@/services/ai'
import { useToastStore } from '@/stores/toast'
import type { Category, NutritionalEstimate, Product } from '@/types'

const props = defineProps<{
  restaurantId: string
  products: Product[]
  categories: Category[]
}>()

const emit = defineEmits<{
  (e: 'refresh'): void
}>()

const toast = useToastStore()

const searchQuery = ref('')
const categoryFilter = ref('')
const statusFilter = ref<'all' | 'active' | 'paused'>('all')

// Drawer Form State
const drawerOpen = ref(false)
const editingProductId = ref<string | null>(null)
const productName = ref('')
const productDescription = ref('')
const productPrice = ref<number | null>(null)
const productCategoryId = ref('')
const productImageUrl = ref('')
const availableInPlans = ref(true)
const allowsCutlery = ref(false)
const cutleryHasExtraCost = ref(false)
const cutleryPrice = ref<number | null>(null)
const productAddons = ref<Array<{ name: string; extraPrice: number; maxQuantity: number }>>([])

const uploadingImage = ref(false)
const generatingWithAi = ref(false)
const aiNutrition = ref<NutritionalEstimate | null>(null)
const savingProduct = ref(false)
const deleteTarget = ref<Product | null>(null)

const filteredProducts = computed(() => {
  let list = [...props.products]
  if (categoryFilter.value) {
    list = list.filter((p) => p.categoryId === categoryFilter.value)
  }
  if (statusFilter.value === 'active') {
    list = list.filter((p) => p.isActive)
  } else if (statusFilter.value === 'paused') {
    list = list.filter((p) => !p.isActive)
  }
  const q = searchQuery.value.trim().toLowerCase()
  if (q) {
    list = list.filter(
      (p) =>
        p.name.toLowerCase().includes(q) ||
        (p.description && p.description.toLowerCase().includes(q)),
    )
  }
  return list
})

function openCreateDrawer() {
  editingProductId.value = null
  productName.value = ''
  productDescription.value = ''
  productPrice.value = null
  productCategoryId.value = props.categories[0]?.id || ''
  productImageUrl.value = ''
  availableInPlans.value = true
  allowsCutlery.value = false
  cutleryHasExtraCost.value = false
  cutleryPrice.value = null
  productAddons.value = []
  aiNutrition.value = null
  drawerOpen.value = true
}

function openEditDrawer(product: Product) {
  editingProductId.value = product.id
  productName.value = product.name
  productDescription.value = product.description || ''
  productPrice.value = product.price
  productCategoryId.value = product.categoryId || props.categories[0]?.id || ''
  productImageUrl.value = product.imageUrl || ''
  availableInPlans.value = product.availableInPlans ?? true
  allowsCutlery.value = product.allowsCutlery ?? false
  cutleryHasExtraCost.value = product.cutleryHasExtraCost ?? false
  cutleryPrice.value = product.cutleryPrice ?? null
  productAddons.value = (product.addons || []).map((a) => ({
    name: a.name,
    extraPrice: a.extraPrice,
    maxQuantity: a.maxQuantity,
  }))
  aiNutrition.value = null
  drawerOpen.value = true
}

function addAddonRow() {
  productAddons.value.push({ name: '', extraPrice: 0, maxQuantity: 1 })
}

function removeAddonRow(idx: number) {
  productAddons.value.splice(idx, 1)
}

async function handleUploadImage(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return
  uploadingImage.value = true
  try {
    const res = await uploadFile(file)
    productImageUrl.value = res.url
    toast.success('Foto da marmita enviada!')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível enviar a foto.'))
  } finally {
    uploadingImage.value = false
  }
}

async function handleOptimizeWithAi() {
  if (!productName.value.trim()) {
    toast.warning('Digite o nome base ou ingredientes do prato para otimizar com IA.')
    return
  }

  generatingWithAi.value = true
  aiNutrition.value = null
  try {
    const selectedCat = props.categories.find((c) => c.id === productCategoryId.value)?.name
    const res = await generateMarmitaWithAi({
      baseNameOrIngredients: productName.value.trim(),
      targetCategory: selectedCat || null,
      price: productPrice.value || null,
    })

    if (res.suggestedName) productName.value = res.suggestedName
    if (res.gourmetDescription) productDescription.value = res.gourmetDescription
    aiNutrition.value = res.nutritionalEstimate

    if (res.suggestedCategory && props.categories.length > 0) {
      const match = props.categories.find(
        (c) =>
          c.name.toLowerCase().includes(res.suggestedCategory.toLowerCase()) ||
          res.suggestedCategory.toLowerCase().includes(c.name.toLowerCase()),
      )
      if (match) productCategoryId.value = match.id
    }

    toast.success('Nome, descrição gourmet e macros otimizados pela IA!')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível otimizar com IA no momento.'))
  } finally {
    generatingWithAi.value = false
  }
}

async function handleSaveProduct() {
  if (!productName.value.trim() || !productDescription.value.trim()) {
    toast.warning('Informe o nome e a descrição da marmita.')
    return
  }
  if (!productCategoryId.value) {
    toast.warning('Selecione uma categoria.')
    return
  }
  if (!productPrice.value || productPrice.value <= 0) {
    toast.warning('Informe um preço avulso válido maior que zero.')
    return
  }
  if (allowsCutlery.value && cutleryHasExtraCost.value && (!cutleryPrice.value || cutleryPrice.value <= 0)) {
    toast.warning('Informe o custo extra dos talheres.')
    return
  }

  savingProduct.value = true
  try {
    const payload = {
      categoryId: productCategoryId.value,
      name: productName.value.trim(),
      description: productDescription.value.trim(),
      price: productPrice.value,
      imageUrl: productImageUrl.value.trim() || null,
      availableInPlans: availableInPlans.value,
      allowsCutlery: allowsCutlery.value,
      cutleryHasExtraCost: allowsCutlery.value && cutleryHasExtraCost.value,
      cutleryPrice: allowsCutlery.value && cutleryHasExtraCost.value ? cutleryPrice.value : 0,
      addons: productAddons.value
        .filter((a) => a.name.trim())
        .map((a) => ({
          name: a.name.trim(),
          extraPrice: Number(a.extraPrice) || 0,
          maxQuantity: Number(a.maxQuantity) || 1,
        })),
    }

    if (editingProductId.value) {
      await api.put(`/marmitas/${editingProductId.value}`, payload)
      toast.success('Marmita atualizada no cardápio!')
    } else {
      await api.post(`/restaurantes/${props.restaurantId}/marmitas`, payload)
      toast.success('Nova marmita adicionada ao cardápio!')
    }

    drawerOpen.value = false
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Não foi possível salvar a marmita.'))
  } finally {
    savingProduct.value = false
  }
}

async function handleToggleProduct(product: Product, nextState: boolean) {
  try {
    const { data } = await api.patch<Product>(`/marmitas/${product.id}/disponibilidade`, {
      isActive: nextState,
    })
    product.isActive = data.isActive
    toast.info(data.isActive ? `"${product.name}" ativada no cardápio.` : `"${product.name}" pausada.`)
  } catch (err) {
    toast.error(getErrorMessage(err, 'Erro ao alterar disponibilidade.'))
  }
}

async function handleConfirmDelete() {
  if (!deleteTarget.value) return
  try {
    await api.delete(`/marmitas/${deleteTarget.value.id}`)
    toast.info(`"${deleteTarget.value.name}" removida do cardápio.`)
    deleteTarget.value = null
    emit('refresh')
  } catch (err) {
    toast.error(getErrorMessage(err, 'Erro ao remover marmita.'))
  }
}
</script>

<template>
  <div class="space-y-5">
    <!-- Top Controls Bar -->
    <div class="flex flex-col justify-between gap-3 sm:flex-row sm:items-center">
      <div class="flex flex-1 flex-wrap items-center gap-2">
        <div class="relative flex-1 min-w-[200px] max-w-xs">
          <Search class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-muted" />
          <input
            v-model="searchQuery"
            type="search"
            placeholder="Buscar marmita..."
            class="w-full rounded-xl border border-line-strong bg-surface py-2 pl-9 pr-3 text-xs text-ink focus:border-brand-600 focus:outline-none"
          />
        </div>

        <select
          v-model="categoryFilter"
          class="rounded-xl border border-line-strong bg-surface px-3 py-2 text-xs font-medium text-ink"
        >
          <option value="">Todas categorias</option>
          <option v-for="cat in categories" :key="cat.id" :value="cat.id">{{ cat.name }}</option>
        </select>

        <select
          v-model="statusFilter"
          class="rounded-xl border border-line-strong bg-surface px-3 py-2 text-xs font-medium text-ink"
        >
          <option value="all">Todos os status</option>
          <option value="active">Somente ativos</option>
          <option value="paused">Somente pausados</option>
        </select>
      </div>

      <AppButton variant="primary" size="md" @click="openCreateDrawer">
        <Plus class="h-4 w-4" />
        Nova marmita
      </AppButton>
    </div>

    <!-- Dish List -->
    <div v-if="filteredProducts.length > 0" class="grid gap-4 md:grid-cols-2">
      <AppCard
        v-for="product in filteredProducts"
        :key="product.id"
        padding="md"
        class="flex gap-4"
        :class="!product.isActive ? 'opacity-70' : ''"
      >
        <div class="h-24 w-24 shrink-0 overflow-hidden rounded-2xl border border-line bg-surface-muted flex items-center justify-center">
          <img
            v-if="product.imageUrl"
            :src="product.imageUrl"
            :alt="product.name"
            class="h-full w-full object-cover"
          />
          <UtensilsCrossed v-else class="h-7 w-7 text-ink-muted/60" />
        </div>

        <div class="flex min-w-0 flex-1 flex-col justify-between">
          <div>
            <div class="flex items-start justify-between gap-2">
              <h3 class="font-display text-sm font-extrabold text-ink truncate">
                {{ product.name }}
              </h3>
              <AppToggle
                :model-value="product.isActive"
                active-color="fresh"
                size="sm"
                @update:model-value="(val) => handleToggleProduct(product, val)"
              />
            </div>

            <div class="mt-1 flex flex-wrap items-center gap-1.5">
              <AppBadge variant="neutral" size="xs">{{ product.categoryName || 'Sem categoria' }}</AppBadge>
              <AppBadge v-if="product.availableInPlans !== false" variant="fresh" size="xs">
                No Plano
              </AppBadge>
              <AppBadge :variant="product.isActive ? 'fresh' : 'neutral'" size="xs">
                {{ product.isActive ? 'Ativo' : 'Pausado' }}
              </AppBadge>
            </div>

            <p class="mt-1.5 line-clamp-2 text-xs text-ink-secondary">
              {{ product.description }}
            </p>
          </div>

          <div class="mt-3 flex items-center justify-between gap-2 border-t border-line pt-2.5">
            <span class="font-display text-base font-extrabold text-ink tabular-nums">
              {{ formatCurrency(product.price) }}
            </span>

            <div class="flex items-center gap-1.5">
              <AppButton variant="outline" size="xs" @click="openEditDrawer(product)">
                <Edit3 class="h-3.5 w-3.5" />
                Editar
              </AppButton>
              <button
                type="button"
                aria-label="Excluir marmita"
                class="rounded-lg border border-red-200 p-1.5 text-red-600 transition hover:bg-red-50 dark:border-red-900/60 dark:text-red-400 dark:hover:bg-red-950/40"
                @click="deleteTarget = product"
              >
                <Trash2 class="h-3.5 w-3.5" />
              </button>
            </div>
          </div>
        </div>
      </AppCard>
    </div>

    <AppEmptyState
      v-else
      :icon="UtensilsCrossed"
      title="Nenhuma marmita encontrada"
      description="Cadastre pratos com fotos apetitosas e use a IA do Gemini para gerar descrições irresistíveis."
    >
      <template #action>
        <AppButton variant="primary" size="sm" @click="openCreateDrawer">
          <Plus class="h-4 w-4" />
          Cadastrar primeira marmita
        </AppButton>
      </template>
    </AppEmptyState>

    <!-- Create / Edit Dish Slide-Over Drawer -->
    <AppDrawer
      :open="drawerOpen"
      :title="editingProductId ? 'Editar Marmita' : 'Nova Marmita'"
      subtitle="Configure foto, preço, elegibilidade em planos, extras e otimize com IA."
      width-class="max-w-lg"
      @close="drawerOpen = false"
    >
      <form id="dish-drawer-form" class="space-y-4" @submit.prevent="handleSaveProduct">
        <!-- Image Upload + Preview -->
        <div class="space-y-2">
          <label class="block text-xs font-semibold text-ink-secondary">Foto da marmita</label>
          <div class="flex items-center gap-3">
            <div class="h-20 w-20 shrink-0 overflow-hidden rounded-2xl border border-line bg-surface-muted flex items-center justify-center">
              <img v-if="productImageUrl" :src="productImageUrl" alt="Preview" class="h-full w-full object-cover" />
              <UtensilsCrossed v-else class="h-6 w-6 text-ink-muted" />
            </div>
            <div class="flex-1 space-y-2">
              <input
                v-model="productImageUrl"
                type="text"
                placeholder="URL da foto ou envie um arquivo abaixo"
                class="w-full rounded-xl border border-line-strong bg-surface px-3 py-2 text-xs text-ink"
              />
              <label class="inline-flex cursor-pointer items-center gap-1.5 rounded-xl border border-line-strong bg-surface-subtle px-3 py-1.5 text-xs font-bold text-ink-secondary hover:bg-surface-muted">
                <Upload class="h-3.5 w-3.5" />
                <span>{{ uploadingImage ? 'Enviando foto...' : 'Enviar imagem' }}</span>
                <input type="file" accept=".jpg,.jpeg,.png,.webp" class="hidden" @change="handleUploadImage" />
              </label>
            </div>
          </div>
        </div>

        <div class="grid gap-3 sm:grid-cols-2">
          <div class="space-y-1.5">
            <label class="block text-xs font-semibold text-ink-secondary">Categoria *</label>
            <select
              v-model="productCategoryId"
              required
              class="w-full rounded-xl border border-line-strong bg-surface px-3 py-2.5 text-sm text-ink"
            >
              <option value="" disabled>Selecione...</option>
              <option v-for="cat in categories" :key="cat.id" :value="cat.id">{{ cat.name }}</option>
            </select>
          </div>

          <AppInput
            v-model.number="productPrice"
            label="Valor avulso (R$)"
            type="number"
            step="0.01"
            min="0.01"
            required
            placeholder="24.90"
          />
        </div>

        <!-- Dish Name + Gemini AI Optimizer -->
        <div class="space-y-1.5">
          <div class="flex items-center justify-between gap-2">
            <label class="block text-xs font-semibold text-ink-secondary">
              Nome do prato ou ingredientes *
            </label>
            <button
              type="button"
              :disabled="generatingWithAi || !productName.trim()"
              class="inline-flex items-center gap-1.5 rounded-lg bg-gradient-to-r from-brand-600 to-amber-500 px-2.5 py-1 text-[11px] font-bold text-white shadow-xs transition hover:brightness-110 disabled:opacity-45"
              @click="handleOptimizeWithAi"
            >
              <Loader2 v-if="generatingWithAi" class="h-3.5 w-3.5 animate-spin" />
              <Sparkles v-else class="h-3.5 w-3.5" />
              <span>{{ generatingWithAi ? 'Otimizando...' : 'Otimizar com IA (Gemini)' }}</span>
            </button>
          </div>
          <input
            v-model="productName"
            type="text"
            required
            placeholder="Ex: Frango grelhado com batata doce e brócolis"
            class="w-full rounded-xl border border-line-strong bg-surface px-3.5 py-2.5 text-sm text-ink focus:border-brand-600 focus:outline-none"
          />
        </div>

        <div class="space-y-1.5">
          <label class="block text-xs font-semibold text-ink-secondary">Descrição *</label>
          <textarea
            v-model="productDescription"
            rows="3"
            required
            placeholder="Ingredientes, acompanhamentos e temperos..."
            class="w-full rounded-xl border border-line-strong bg-surface px-3.5 py-2.5 text-sm text-ink focus:border-brand-600 focus:outline-none"
          />
        </div>

        <!-- AI Nutritional Estimate Preview -->
        <div
          v-if="aiNutrition"
          class="rounded-2xl border border-amber-200 bg-amber-50/70 p-3.5 text-xs dark:border-amber-800/60 dark:bg-amber-950/30"
        >
          <div class="flex items-center gap-1.5 font-bold text-amber-900 dark:text-amber-300">
            <Sparkles class="h-4 w-4 text-brand-600" />
            <span>Estimativa Nutricional (Gemini IA)</span>
          </div>
          <div class="mt-2 grid grid-cols-4 gap-2 text-center">
            <div class="rounded-lg bg-surface p-1.5 border border-line">
              <p class="text-[10px] text-ink-muted">Calorias</p>
              <p class="font-bold text-amber-700 dark:text-amber-400">~{{ aiNutrition.calories }}</p>
            </div>
            <div class="rounded-lg bg-surface p-1.5 border border-line">
              <p class="text-[10px] text-ink-muted">Proteína</p>
              <p class="font-bold text-fresh-700 dark:text-fresh-400">{{ aiNutrition.protein }}g</p>
            </div>
            <div class="rounded-lg bg-surface p-1.5 border border-line">
              <p class="text-[10px] text-ink-muted">Carbs</p>
              <p class="font-bold text-ink">{{ aiNutrition.carbs }}g</p>
            </div>
            <div class="rounded-lg bg-surface p-1.5 border border-line">
              <p class="text-[10px] text-ink-muted">Gordura</p>
              <p class="font-bold text-ink">{{ aiNutrition.fat }}g</p>
            </div>
          </div>
        </div>

        <!-- Plan Eligibility -->
        <div class="rounded-2xl border border-line bg-surface-subtle p-3.5">
          <AppToggle
            v-model="availableInPlans"
            active-color="fresh"
            label="Disponível em Planos Mensais"
            description="Permite que assinantes resgatem este prato sem custo base."
          />
        </div>

        <!-- Cutlery Config -->
        <div class="rounded-2xl border border-line bg-surface-subtle p-3.5 space-y-3">
          <AppToggle
            v-model="allowsCutlery"
            label="Oferecer opção de talheres?"
            description="O cliente poderá escolher se deseja talheres no pedido."
          />
          <div v-if="allowsCutlery" class="space-y-2.5 border-t border-line pt-3">
            <div class="flex gap-4 text-xs font-semibold text-ink">
              <label class="inline-flex items-center gap-1.5 cursor-pointer">
                <input v-model="cutleryHasExtraCost" type="radio" :value="false" class="accent-brand-600" />
                Gratuito
              </label>
              <label class="inline-flex items-center gap-1.5 cursor-pointer">
                <input v-model="cutleryHasExtraCost" type="radio" :value="true" class="accent-brand-600" />
                Com custo extra
              </label>
            </div>
            <AppInput
              v-if="cutleryHasExtraCost"
              v-model.number="cutleryPrice"
              label="Valor do talher (R$)"
              type="number"
              step="0.01"
              min="0.01"
              required
              placeholder="1.50"
            />
          </div>
        </div>

        <!-- Addons Builder -->
        <div class="rounded-2xl border border-line bg-surface-subtle p-3.5 space-y-3">
          <div class="flex items-center justify-between">
            <span class="text-xs font-bold text-ink">Adicionais / Extras</span>
            <AppButton type="button" variant="outline" size="xs" @click="addAddonRow">
              <Plus class="h-3.5 w-3.5" />
              Adicionar extra
            </AppButton>
          </div>

          <div
            v-for="(addon, idx) in productAddons"
            :key="idx"
            class="rounded-xl border border-line bg-surface p-3 space-y-2"
          >
            <div class="flex items-center gap-2">
              <input
                v-model="addon.name"
                type="text"
                required
                placeholder="Ex: Ovo frito, Porção extra de frango"
                class="flex-1 rounded-lg border border-line-strong bg-surface px-2.5 py-1.5 text-xs text-ink"
              />
              <button
                type="button"
                aria-label="Remover adicional"
                class="p-1 text-red-500 hover:text-red-700"
                @click="removeAddonRow(idx)"
              >
                <Trash2 class="h-4 w-4" />
              </button>
            </div>
            <div class="grid grid-cols-2 gap-2">
              <AppInput
                v-model.number="addon.extraPrice"
                label="Valor extra (R$)"
                type="number"
                step="0.01"
                min="0"
                required
              />
              <AppInput
                v-model.number="addon.maxQuantity"
                label="Limite máx."
                type="number"
                min="1"
                step="1"
                required
              />
            </div>
          </div>
        </div>
      </form>

      <template #footer>
        <div class="flex justify-end gap-2">
          <AppButton variant="outline" size="md" @click="drawerOpen = false">Cancelar</AppButton>
          <AppButton
            type="submit"
            form="dish-drawer-form"
            variant="primary"
            size="md"
            :loading="savingProduct"
          >
            {{ editingProductId ? 'Salvar alterações' : 'Adicionar ao cardápio' }}
          </AppButton>
        </div>
      </template>
    </AppDrawer>

    <AppConfirmDialog
      :open="Boolean(deleteTarget)"
      title="Remover marmita do cardápio?"
      :message="`Deseja excluir '${deleteTarget?.name}' permanentemente?`"
      confirm-label="Sim, remover"
      variant="danger"
      @confirm="handleConfirmDelete"
      @cancel="deleteTarget = null"
    />
  </div>
</template>
