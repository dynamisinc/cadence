import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { feedbackAdminService } from '../services/feedbackAdminService'
import type { FeedbackQueryParams, UpdateFeedbackStatusRequest } from '../types/feedbackReport'
import { notify } from '@/shared/utils/notify'

export const feedbackAdminKeys = {
  all: ['feedback-admin'] as const,
  list: (params: FeedbackQueryParams) => [...feedbackAdminKeys.all, 'list', params] as const,
}

export function useFeedbackReports(params: FeedbackQueryParams) {
  return useQuery({
    queryKey: feedbackAdminKeys.list(params),
    queryFn: () => feedbackAdminService.getReports(params),
  })
}

export function useUpdateFeedbackStatus() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateFeedbackStatusRequest }) =>
      feedbackAdminService.updateStatus(id, request),
    onSuccess: async (data) => {
      console.log('[useFeedbackAdmin] Status update confirmed by server:', data)
      await queryClient.invalidateQueries({ queryKey: feedbackAdminKeys.all })
      notify.success('Report updated')
    },
    onError: () => {
      notify.error('Failed to update report')
    },
  })
}

export function useDeleteFeedbackReport() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => feedbackAdminService.deleteReport(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: feedbackAdminKeys.all })
      notify.success('Report deleted')
    },
    onError: () => {
      notify.error('Failed to delete report')
    },
  })
}
