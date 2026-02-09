/**
 * Feedback Service
 *
 * API client for submitting feedback, bug reports, feature requests, and error reports.
 *
 * @module features/feedback
 */

import { apiClient } from '@/core/services/api'
import type {
  SubmitBugReportRequest,
  SubmitFeatureRequestRequest,
  SubmitGeneralFeedbackRequest,
  SubmitErrorReportRequest,
  FeedbackResponse,
} from '../types'

export const feedbackService = {
  submitBugReport: async (
    request: SubmitBugReportRequest,
  ): Promise<FeedbackResponse> => {
    const response = await apiClient.post<FeedbackResponse>(
      '/feedback/bug-report',
      request,
    )
    return response.data
  },

  submitFeatureRequest: async (
    request: SubmitFeatureRequestRequest,
  ): Promise<FeedbackResponse> => {
    const response = await apiClient.post<FeedbackResponse>(
      '/feedback/feature-request',
      request,
    )
    return response.data
  },

  submitGeneralFeedback: async (
    request: SubmitGeneralFeedbackRequest,
  ): Promise<FeedbackResponse> => {
    const response = await apiClient.post<FeedbackResponse>(
      '/feedback/general',
      request,
    )
    return response.data
  },

  submitErrorReport: async (
    request: SubmitErrorReportRequest,
  ): Promise<FeedbackResponse> => {
    const response = await apiClient.post<FeedbackResponse>(
      '/feedback/error-report',
      request,
    )
    return response.data
  },
}
