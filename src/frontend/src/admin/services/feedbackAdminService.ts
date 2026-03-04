import apiClient from '@/core/services/api'
import type {
  FeedbackListResponse,
  FeedbackQueryParams,
  UpdateFeedbackStatusRequest,
} from '../types/feedbackReport'

export const feedbackAdminService = {
  getReports: async (params: FeedbackQueryParams = {}): Promise<FeedbackListResponse> => {
    const response = await apiClient.get<FeedbackListResponse>('/feedback', { params })
    return response.data
  },

  updateStatus: async (id: string, request: UpdateFeedbackStatusRequest): Promise<{ status: string; adminNotes: string | null }> => {
    const response = await apiClient.patch<{ status: string; adminNotes: string | null }>(`/feedback/${id}/status`, request)
    return response.data
  },

  deleteReport: async (id: string): Promise<void> => {
    await apiClient.delete(`/feedback/${id}`)
  },
}
