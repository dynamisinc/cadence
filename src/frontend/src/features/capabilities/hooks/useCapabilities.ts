/**
 * useCapabilities Hook
 *
 * React Query hooks for capability CRUD operations.
 * Provides optimistic updates and cache management.
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'react-toastify'
import { capabilityService } from '../services/capabilityService'
import type { CapabilityDto, CreateCapabilityRequest, UpdateCapabilityRequest } from '../types'

/** Query key factory for capabilities */
export const capabilityKeys = {
  all: ['capabilities'] as const,
  list: (includeInactive: boolean, organizationId?: string) => [...capabilityKeys.all, { includeInactive, organizationId }] as const,
  detail: (id: string) => [...capabilityKeys.all, 'detail', id] as const,
}

interface UseCapabilitiesOptions {
  /** Include inactive capabilities (default: false) */
  includeInactive?: boolean
  /** Organization ID to filter capabilities (required for proper org scoping) */
  organizationId?: string
}

/**
 * Hook for managing capabilities list and CRUD operations
 *
 * Features:
 * - Automatic caching and background refetching
 * - Optimistic updates for create/update/delete
 * - Error handling with toast notifications
 *
 * @param optionsOrIncludeInactive Configuration options object, or boolean for backwards compatibility
 */
export const useCapabilities = (optionsOrIncludeInactive: UseCapabilitiesOptions | boolean = {}) => {
  // Support both old signature (boolean) and new signature (options object)
  const options: UseCapabilitiesOptions = typeof optionsOrIncludeInactive === 'boolean'
    ? { includeInactive: optionsOrIncludeInactive }
    : optionsOrIncludeInactive
  const { includeInactive = false, organizationId } = options
  const queryClient = useQueryClient()
  const queryKey = capabilityKeys.list(includeInactive, organizationId)

  // Query for fetching capabilities
  const {
    data: capabilities = [],
    isLoading: loading,
    error,
    refetch: fetchCapabilities,
  } = useQuery({
    queryKey,
    queryFn: () => capabilityService.getCapabilities(includeInactive, organizationId),
    staleTime: 5 * 60 * 1000, // Cache for 5 minutes (reference data)
  })

  // Mutation for creating capabilities
  const createMutation = useMutation({
    mutationFn: (request: CreateCapabilityRequest) =>
      capabilityService.createCapability(request, organizationId),
    onSuccess: newCapability => {
      queryClient.setQueryData<CapabilityDto[]>(queryKey, (old = []) => [
        ...old,
        newCapability,
      ])
      // Also invalidate the inactive list if we're viewing active only
      if (!includeInactive) {
        queryClient.invalidateQueries({ queryKey: capabilityKeys.list(true, organizationId) })
      }
      toast.success('Capability created')
    },
    onError: err => {
      const message =
        err instanceof Error ? err.message : 'Failed to create capability'
      toast.error(message)
    },
  })

  // Mutation for updating capabilities
  const updateMutation = useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateCapabilityRequest }) =>
      capabilityService.updateCapability(id, request, organizationId),
    onMutate: async ({ id, request }) => {
      await queryClient.cancelQueries({ queryKey })
      const previousCapabilities = queryClient.getQueryData<CapabilityDto[]>(queryKey)

      queryClient.setQueryData<CapabilityDto[]>(queryKey, (old = []) =>
        old.map(cap =>
          cap.id === id
            ? { ...cap, ...request, updatedAt: new Date().toISOString() }
            : cap,
        ),
      )

      return { previousCapabilities }
    },
    onSuccess: updatedCapability => {
      queryClient.setQueryData<CapabilityDto[]>(queryKey, (old = []) =>
        old.map(cap =>
          cap.id === updatedCapability.id ? updatedCapability : cap,
        ),
      )
      // Invalidate both lists to ensure consistency
      queryClient.invalidateQueries({ queryKey: capabilityKeys.all })
      toast.success('Capability updated')
    },
    onError: (err, _variables, context) => {
      if (context?.previousCapabilities) {
        queryClient.setQueryData(queryKey, context.previousCapabilities)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to update capability'
      toast.error(message)
    },
  })

  // Mutation for deleting (deactivating) capabilities
  const deleteMutation = useMutation({
    mutationFn: (id: string) => capabilityService.deleteCapability(id, organizationId),
    onMutate: async id => {
      await queryClient.cancelQueries({ queryKey })
      const previousCapabilities = queryClient.getQueryData<CapabilityDto[]>(queryKey)

      // If viewing active only, remove from list
      // If viewing all, mark as inactive
      queryClient.setQueryData<CapabilityDto[]>(queryKey, (old = []) =>
        includeInactive
          ? old.map(cap =>
            cap.id === id ? { ...cap, isActive: false } : cap,
          )
          : old.filter(cap => cap.id !== id),
      )

      return { previousCapabilities }
    },
    onSuccess: () => {
      // Invalidate both lists to ensure consistency
      queryClient.invalidateQueries({ queryKey: capabilityKeys.all })
      toast.success('Capability deactivated')
    },
    onError: (err, _variables, context) => {
      if (context?.previousCapabilities) {
        queryClient.setQueryData(queryKey, context.previousCapabilities)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to deactivate capability'
      toast.error(message)
    },
  })

  // Mutation for reactivating capabilities
  const reactivateMutation = useMutation({
    mutationFn: (id: string) => capabilityService.reactivateCapability(id, organizationId),
    onMutate: async id => {
      await queryClient.cancelQueries({ queryKey })
      const previousCapabilities = queryClient.getQueryData<CapabilityDto[]>(queryKey)

      // Mark as active in the list
      queryClient.setQueryData<CapabilityDto[]>(queryKey, (old = []) =>
        old.map(cap =>
          cap.id === id ? { ...cap, isActive: true } : cap,
        ),
      )

      return { previousCapabilities }
    },
    onSuccess: () => {
      // Invalidate both lists to ensure consistency
      queryClient.invalidateQueries({ queryKey: capabilityKeys.all })
      toast.success('Capability reactivated')
    },
    onError: (err, _variables, context) => {
      if (context?.previousCapabilities) {
        queryClient.setQueryData(queryKey, context.previousCapabilities)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to reactivate capability'
      toast.error(message)
    },
  })

  // Wrapper functions
  const createCapability = async (request: CreateCapabilityRequest) => {
    return createMutation.mutateAsync(request)
  }

  const updateCapability = async (id: string, request: UpdateCapabilityRequest) => {
    return updateMutation.mutateAsync({ id, request })
  }

  const deleteCapability = async (id: string) => {
    return deleteMutation.mutateAsync(id)
  }

  const reactivateCapability = async (id: string) => {
    return reactivateMutation.mutateAsync(id)
  }

  return {
    capabilities,
    loading,
    error: error
      ? error instanceof Error
        ? error.message
        : 'Failed to load capabilities'
      : null,
    fetchCapabilities,
    createCapability,
    updateCapability,
    deleteCapability,
    reactivateCapability,
    // Expose mutation states
    isCreating: createMutation.isPending,
    isUpdating: updateMutation.isPending,
    isDeleting: deleteMutation.isPending,
    isReactivating: reactivateMutation.isPending,
  }
}

/**
 * Hook to fetch a single capability by ID
 */
export const useCapability = (id: string | undefined) => {
  return useQuery({
    queryKey: capabilityKeys.detail(id!),
    queryFn: () => capabilityService.getCapability(id!),
    enabled: !!id,
    staleTime: 5 * 60 * 1000,
  })
}

/**
 * Hook to check capability name availability
 */
export const useCheckCapabilityName = () => {
  const mutation = useMutation({
    mutationFn: ({ name, excludeId }: { name: string; excludeId?: string }) =>
      capabilityService.checkNameAvailability(name, excludeId),
  })

  return {
    checkName: mutation.mutateAsync,
    isChecking: mutation.isPending,
  }
}

export default useCapabilities
