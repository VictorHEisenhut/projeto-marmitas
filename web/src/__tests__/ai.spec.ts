import { describe, expect, it, vi } from 'vitest'
import { api } from '@/services/api'
import { getAiMarmitaRecommendations, generateMarmitaWithAi, getAiStatus } from '@/services/ai'

describe('services/ai', () => {
  it('consulta o status da API Gemini', async () => {
    vi.spyOn(api, 'get').mockResolvedValueOnce({
      data: {
        isConfigured: true,
        model: 'gemini-3.8-flash',
        provider: 'Google Gemini',
      },
    })

    const status = await getAiStatus()
    expect(status.isConfigured).toBe(true)
    expect(status.model).toBe('gemini-3.8-flash')
    expect(status.provider).toBe('Google Gemini')
  })

  it('solicita recomendação de marmita ao Chef & Nutricionista IA', async () => {
    vi.spyOn(api, 'post').mockResolvedValueOnce({
      data: {
        generalAdvice: 'Opções ricas em proteína ideais para pós-treino.',
        recommendations: [
          {
            productId: 'prod-123',
            productName: 'Frango com Batata Doce',
            price: 26.9,
            whyRecommended: 'Excelente proporção proteica para regeneração muscular.',
            nutritionalEstimate: {
              calories: 490,
              protein: 42,
              carbs: 45,
              fat: 9,
            },
            tags: ['Alta Proteína', 'Pós-Treino'],
          },
        ],
        isLiveAi: true,
      },
    })

    const res = await getAiMarmitaRecommendations({
      restaurantId: 'rest-1',
      query: 'Quero marmita com bastante proteína pós-treino',
      dietaryGoal: 'hipertrofia',
    })

    expect(res.recommendations).toHaveLength(1)
    expect(res.recommendations[0]?.productName).toBe('Frango com Batata Doce')
    expect(res.recommendations[0]?.nutritionalEstimate.protein).toBe(42)
    expect(res.isLiveAi).toBe(true)
  })

  it('gera detalhes de prato para parceiros com IA', async () => {
    vi.spyOn(api, 'post').mockResolvedValueOnce({
      data: {
        suggestedName: 'Filé de Tilápia com Crosta de Ervas e Purê Rústico',
        gourmetDescription: 'Filé de tilápia grelhado com ervas finas aromáticas acompanhado de purê rústico.',
        suggestedCategory: 'Fitness & Low Carb',
        nutritionalEstimate: {
          calories: 420,
          protein: 36,
          carbs: 30,
          fat: 8,
        },
        highlightIngredients: ['Tilápia', 'Ervas Finas'],
        isLiveAi: true,
      },
    })

    const res = await generateMarmitaWithAi({
      baseNameOrIngredients: 'Tilapia com pure',
      price: 29.9,
    })

    expect(res.suggestedName).toContain('Tilápia')
    expect(res.suggestedCategory).toBe('Fitness & Low Carb')
    expect(res.nutritionalEstimate.calories).toBe(420)
  })
})
