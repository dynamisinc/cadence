import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { notify } from '@/shared/utils/notify'
import { deliveryMethodService } from '../services/deliveryMethodService'
import type { CreateDeliveryMethodRequest, UpdateDeliveryMethodRequest } from '../types'

const QUERY_KEY = 'deliveryMethods'
const ADMIN_QUERY_KEY = 'deliveryMethods-all'

/**
 * Fetch all delivery methods including inactive (admin).
 */
export const useAllDeliveryMethods = () => {
  return useQuery({
    queryKey: [ADMIN_QUERY_KEY],
    queryFn: () => deliveryMethodService.getAllDeliveryMethods(),
  })
}

/**
 * Create a delivery method.
 */
export const useCreateDeliveryMethod = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CreateDeliveryMethodRequest) =>
      deliveryMethodService.createDeliveryMethod(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [ADMIN_QUERY_KEY] })
      queryClient.invalidateQueries({ queryKey: [QUERY_KEY] })
      notify.success('Delivery method created')
    },
    onError: (error: Error & { response?: { data?: { message?: string } } }) => {
      const message =
        error.response?.data?.message || error.message || 'Failed to create delivery method'
      notify.error(message)
    },
  })
}

/**
 * Update a delivery method.
 */
export const useUpdateDeliveryMethod = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateDeliveryMethodRequest }) =>
      deliveryMethodService.updateDeliveryMethod(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [ADMIN_QUERY_KEY] })
      queryClient.invalidateQueries({ queryKey: [QUERY_KEY] })
      notify.success('Delivery method updated')
    },
    onError: (error: Error & { response?: { data?: { message?: string } } }) => {
      const message =
        error.response?.data?.message || error.message || 'Failed to update delivery method'
      notify.error(message)
    },
  })
}

/**
 * Delete a delivery method.
 */
export const useDeleteDeliveryMethod = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => deliveryMethodService.deleteDeliveryMethod(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [ADMIN_QUERY_KEY] })
      queryClient.invalidateQueries({ queryKey: [QUERY_KEY] })
      notify.success('Delivery method removed')
    },
    onError: () => {
      notify.error('Failed to remove delivery method')
    },
  })
}
