/**
 * Exercise Metrics API Service
 *
 * Handles all API calls for exercise metrics operations.
 * - S01: Real-time progress during conduct
 * - S02: Inject delivery summary for AAR
 * - S03: Observation summary for AAR
 * - S04: Timeline summary for AAR
 * - S06: Core capability performance for AAR
 * - S07: Controller activity metrics for AAR
 * - S08: Evaluator coverage metrics for AAR
 */

import { apiClient } from '../../../core/services/api'
import type {
  ExerciseProgressDto,
  InjectSummaryDto,
  ObservationSummaryDto,
  TimelineSummaryDto,
  ControllerActivitySummaryDto,
  EvaluatorCoverageSummaryDto,
  CapabilityPerformanceSummaryDto,
} from '../types'

export const metricsService = {
  /**
   * Get real-time exercise progress for conduct view (S01).
   * Provides situational awareness: inject counts, observation counts, clock status.
   */
  getExerciseProgress: async (exerciseId: string): Promise<ExerciseProgressDto> => {
    const response = await apiClient.get<ExerciseProgressDto>(
      `/exercises/${exerciseId}/progress`,
    )
    return response.data
  },

  /**
   * Get comprehensive inject delivery statistics for AAR (S02).
   * Shows timing performance, on-time rate, and breakdowns by phase/controller.
   */
  getInjectSummary: async (
    exerciseId: string,
    onTimeToleranceMinutes: number = 5,
  ): Promise<InjectSummaryDto> => {
    const response = await apiClient.get<InjectSummaryDto>(
      `/exercises/${exerciseId}/metrics/injects`,
      { params: { onTimeToleranceMinutes } },
    )
    return response.data
  },

  /**
   * Get comprehensive observation statistics for AAR (S03).
   * Shows P/S/M/U distribution, coverage rates, and breakdowns by evaluator/phase.
   */
  getObservationSummary: async (exerciseId: string): Promise<ObservationSummaryDto> => {
    const response = await apiClient.get<ObservationSummaryDto>(
      `/exercises/${exerciseId}/metrics/observations`,
    )
    return response.data
  },

  /**
   * Get comprehensive timeline and duration analysis for AAR (S04).
   * Shows pause history, phase timing, and inject pacing analysis.
   */
  getTimelineSummary: async (exerciseId: string): Promise<TimelineSummaryDto> => {
    const response = await apiClient.get<TimelineSummaryDto>(
      `/exercises/${exerciseId}/metrics/timeline`,
    )
    return response.data
  },

  /**
   * Get controller activity metrics for AAR (S07).
   * Shows workload distribution, timing performance, and phase activity per controller.
   */
  getControllerActivity: async (
    exerciseId: string,
    onTimeToleranceMinutes: number = 5,
  ): Promise<ControllerActivitySummaryDto> => {
    const response = await apiClient.get<ControllerActivitySummaryDto>(
      `/exercises/${exerciseId}/metrics/controllers`,
      { params: { onTimeToleranceMinutes } },
    )
    return response.data
  },

  /**
   * Get evaluator coverage metrics for AAR (S08).
   * Shows observation distribution, objective coverage, and rating consistency per evaluator.
   */
  getEvaluatorCoverage: async (exerciseId: string): Promise<EvaluatorCoverageSummaryDto> => {
    const response = await apiClient.get<EvaluatorCoverageSummaryDto>(
      `/exercises/${exerciseId}/metrics/evaluators`,
    )
    return response.data
  },

  /**
   * Get core capability performance metrics for AAR (S06).
   * Shows P/S/M/U ratings broken down by FEMA Core Capability.
   */
  getCapabilityPerformance: async (exerciseId: string): Promise<CapabilityPerformanceSummaryDto> => {
    const response = await apiClient.get<CapabilityPerformanceSummaryDto>(
      `/exercises/${exerciseId}/metrics/capabilities`,
    )
    return response.data
  },
}

export default metricsService
