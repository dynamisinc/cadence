/**
 * useImportLibrary Hook
 *
 * React Query hooks for importing predefined capability libraries.
 * Provides list of available libraries and import functionality.
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'react-toastify'
import { capabilityService } from '../services/capabilityService'
import { capabilityKeys } from './useCapabilities'
import type { ImportLibraryResult } from '../types'

/**
 * Hook to fetch available predefined libraries
 *
 * @param organizationId Organization ID (optional)
 * @returns Query result with available libraries
 */
export const useAvailableLibraries = (organizationId?: string) => {
  return useQuery({
    queryKey: ['capabilities', 'libraries', organizationId],
    queryFn: () => capabilityService.getAvailableLibraries(organizationId),
    staleTime: 30 * 60 * 1000, // Cache for 30 minutes (rarely changes)
  })
}

/**
 * Hook to import a predefined capability library
 *
 * Features:
 * - Invalidates capability cache on success
 * - Shows success toast with import summary
 * - Shows error toast on failure
 *
 * @param organizationId Organization ID (optional)
 * @returns Mutation with import function
 */
export const useImportLibrary = (organizationId?: string) => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (libraryName: string) =>
      capabilityService.importLibrary(libraryName, organizationId),
    onSuccess: (result: ImportLibraryResult) => {
      // Invalidate all capability queries to refresh the list
      queryClient.invalidateQueries({ queryKey: capabilityKeys.all })

      // Show success message with import details
      if (result.skippedDuplicates > 0) {
        toast.success(
          `Imported ${result.imported} capabilities (${result.skippedDuplicates} skipped as duplicates)`,
          { autoClose: 5000 },
        )
      } else {
        toast.success(`Imported ${result.imported} capabilities`, { autoClose: 3000 })
      }
    },
    onError: err => {
      const message = err instanceof Error ? err.message : 'Failed to import library'
      toast.error(message)
    },
  })
}
