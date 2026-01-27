/**
 * useMyAssignments Hook
 *
 * React Query hook for fetching user's exercise assignments.
 */
import { useQuery } from '@tanstack/react-query'
import { getMyAssignments } from '../services/assignmentService'
import type { MyAssignmentsResponse } from '../types'

/** Query key for assignments */
export const ASSIGNMENTS_QUERY_KEY = ['assignments', 'my']

/**
 * Hook to fetch the current user's exercise assignments.
 */
export function useMyAssignments() {
  return useQuery<MyAssignmentsResponse, Error>({
    queryKey: ASSIGNMENTS_QUERY_KEY,
    queryFn: getMyAssignments,
    staleTime: 1000 * 60, // 1 minute
    refetchOnWindowFocus: true, // Refetch when user returns to tab
  })
}
