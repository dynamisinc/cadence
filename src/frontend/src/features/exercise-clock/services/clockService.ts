/**
 * Exercise Clock API Service
 *
 * Handles all API calls for exercise clock operations.
 */

import { apiClient } from '../../../core/services/api'
import type { ClockStateDto } from '../types'

export const clockService = {
  /**
   * Get the current clock state for an exercise
   */
  getClockState: async (exerciseId: string): Promise<ClockStateDto> => {
    const response = await apiClient.get<ClockStateDto>(
      `/api/exercises/${exerciseId}/clock`,
    )
    return response.data
  },

  /**
   * Start the exercise clock
   * Also transitions the exercise from Draft to Active status
   */
  startClock: async (exerciseId: string): Promise<ClockStateDto> => {
    const response = await apiClient.post<ClockStateDto>(
      `/api/exercises/${exerciseId}/clock/start`,
    )
    return response.data
  },

  /**
   * Pause the exercise clock
   * Preserves elapsed time for later resumption
   */
  pauseClock: async (exerciseId: string): Promise<ClockStateDto> => {
    const response = await apiClient.post<ClockStateDto>(
      `/api/exercises/${exerciseId}/clock/pause`,
    )
    return response.data
  },

  /**
   * Stop the exercise clock and complete the exercise
   * Transitions the exercise to Completed status
   */
  stopClock: async (exerciseId: string): Promise<ClockStateDto> => {
    const response = await apiClient.post<ClockStateDto>(
      `/api/exercises/${exerciseId}/clock/stop`,
    )
    return response.data
  },

  /**
   * Reset the exercise clock to zero
   * Only allowed for Draft exercises or when clock is Stopped
   */
  resetClock: async (exerciseId: string): Promise<ClockStateDto> => {
    const response = await apiClient.post<ClockStateDto>(
      `/api/exercises/${exerciseId}/clock/reset`,
    )
    return response.data
  },
}

export default clockService
