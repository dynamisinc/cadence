import apiClient from '@/core/services/api'
import type { EulaStatusDto, AcceptEulaRequest } from '../types'

export const eulaService = {
  getStatus: async (): Promise<EulaStatusDto> => {
    const response = await apiClient.get<EulaStatusDto>('/eula/status')
    return response.data
  },

  accept: async (request: AcceptEulaRequest): Promise<void> => {
    await apiClient.post('/eula/accept', request)
  },
}
