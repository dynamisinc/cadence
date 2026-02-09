import apiClient from '@/core/services/api'
import type { SystemSettingsDto, UpdateSystemSettingsRequest } from '../types/systemSettings'

export const systemSettingsService = {
  getSettings: async (): Promise<SystemSettingsDto> => {
    const response = await apiClient.get<SystemSettingsDto>('/system-settings')
    return response.data
  },

  updateSettings: async (request: UpdateSystemSettingsRequest): Promise<SystemSettingsDto> => {
    const response = await apiClient.put<SystemSettingsDto>('/system-settings', request)
    return response.data
  },
}
