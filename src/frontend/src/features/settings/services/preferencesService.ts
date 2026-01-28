/**
 * User Preferences Service
 *
 * API client for user preferences operations.
 *
 * @module features/settings
 */

import { apiClient } from '@/core/services/api'
import type { UserPreferencesDto, UpdateUserPreferencesRequest } from '../types'

export const preferencesService = {
  /**
   * Get current user's preferences
   * Creates default preferences if none exist
   */
  getPreferences: async (): Promise<UserPreferencesDto> => {
    const response = await apiClient.get<UserPreferencesDto>('/users/me/preferences')
    return response.data
  },

  /**
   * Update current user's preferences
   * Only provided fields will be updated
   */
  updatePreferences: async (
    request: UpdateUserPreferencesRequest,
  ): Promise<UserPreferencesDto> => {
    const response = await apiClient.put<UserPreferencesDto>(
      '/users/me/preferences',
      request,
    )
    return response.data
  },

  /**
   * Reset current user's preferences to defaults
   */
  resetPreferences: async (): Promise<UserPreferencesDto> => {
    const response = await apiClient.delete<UserPreferencesDto>('/users/me/preferences')
    return response.data
  },
}
