/**
 * Exercise Capability API Service
 *
 * Handles API calls for exercise target capability management (S04).
 * Target capabilities define what will be evaluated in an exercise.
 */

import { apiClient } from '../../../core/services/api'
import type { CapabilityDto } from '../../capabilities/types'
import type { ExerciseCapabilitySummaryDto } from '../types'

export const exerciseCapabilityService = {
  /**
   * Get target capabilities for an exercise
   * @param exerciseId Exercise ID
   * @returns List of target capabilities
   */
  getTargetCapabilities: async (exerciseId: string): Promise<CapabilityDto[]> => {
    const response = await apiClient.get<CapabilityDto[]>(`/exercises/${exerciseId}/capabilities`)
    return response.data
  },

  /**
   * Set target capabilities for an exercise
   * @param exerciseId Exercise ID
   * @param capabilityIds Array of capability IDs to set as targets
   */
  setTargetCapabilities: async (exerciseId: string, capabilityIds: string[]): Promise<void> => {
    await apiClient.put(`/exercises/${exerciseId}/capabilities`, { capabilityIds })
  },

  /**
   * Get capability coverage summary for an exercise
   * Shows how many target capabilities have been evaluated
   * @param exerciseId Exercise ID
   * @returns Coverage summary with counts and percentage
   */
  getCapabilitySummary: async (exerciseId: string): Promise<ExerciseCapabilitySummaryDto> => {
    const response = await apiClient.get<ExerciseCapabilitySummaryDto>(
      `/exercises/${exerciseId}/capabilities/summary`,
    )
    return response.data
  },
}

export default exerciseCapabilityService
