/**
 * useDeliveryMethods Hook
 *
 * React Query hook for fetching delivery methods.
 */

import { useQuery } from '@tanstack/react-query'
import { deliveryMethodService } from '../services/deliveryMethodService'

const QUERY_KEY = 'deliveryMethods'

/**
 * Hook to fetch all active delivery methods
 */
export const useDeliveryMethods = () => {
  return useQuery({
    queryKey: [QUERY_KEY],
    queryFn: () => deliveryMethodService.getDeliveryMethods(),
    staleTime: 5 * 60 * 1000, // Cache for 5 minutes (reference data rarely changes)
  })
}

/**
 * Hook to fetch a single delivery method by ID
 */
export const useDeliveryMethod = (id: string | undefined) => {
  return useQuery({
    queryKey: [QUERY_KEY, id],
    queryFn: () => deliveryMethodService.getDeliveryMethod(id!),
    enabled: !!id,
    staleTime: 5 * 60 * 1000,
  })
}
