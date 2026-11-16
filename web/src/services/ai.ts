import { api } from '@/services/api'
import type {
  AiGenerateMarmitaRequest,
  AiGenerateMarmitaResponse,
  AiRecommendationRequest,
  AiRecommendationResponse,
  AiStatusResponse,
} from '@/types'

export async function getAiStatus(): Promise<AiStatusResponse> {
  const { data } = await api.get<AiStatusResponse>('/ai/status')
  return data
}

export async function getAiMarmitaRecommendations(
  payload: AiRecommendationRequest,
): Promise<AiRecommendationResponse> {
  const { data } = await api.post<AiRecommendationResponse>('/ai/recommend-marmitas', payload)
  return data
}

export async function generateMarmitaWithAi(
  payload: AiGenerateMarmitaRequest,
): Promise<AiGenerateMarmitaResponse> {
  const { data } = await api.post<AiGenerateMarmitaResponse>('/ai/generate-marmita', payload)
  return data
}
