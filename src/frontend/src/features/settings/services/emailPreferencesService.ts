/**
 * Email Preferences Service
 *
 * API client for email notification preference operations.
 *
 * @module features/settings
 */

import { apiClient } from '@/core/services/api'
import type { EmailPreferencesResponse, UpdateEmailPreferenceRequest } from '../types'

export const emailPreferencesService = {
  /**
   * Get current user's email notification preferences
   */
  getPreferences: async (): Promise<EmailPreferencesResponse> => {
    const response = await apiClient.get<EmailPreferencesResponse>(
      '/users/me/email-preferences',
    )
    return response.data
  },

  /**
   * Update a single email preference category
   */
  updatePreference: async (
    request: UpdateEmailPreferenceRequest,
  ): Promise<EmailPreferencesResponse> => {
    const response = await apiClient.put<EmailPreferencesResponse>(
      '/users/me/email-preferences',
      request,
    )
    return response.data
  },
}
