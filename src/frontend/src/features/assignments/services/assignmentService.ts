/**
 * Assignment API Service
 *
 * API calls for the My Assignments feature.
 */
import { apiClient } from '@/core/services/api'
import type { MyAssignmentsResponse, AssignmentDto } from '../types'

/**
 * Get all assignments for the current user grouped by status.
 */
export async function getMyAssignments(): Promise<MyAssignmentsResponse> {
  const response = await apiClient.get<MyAssignmentsResponse>('/assignments/my')
  return response.data
}

/**
 * Get a single assignment for a specific exercise.
 */
export async function getAssignment(exerciseId: string): Promise<AssignmentDto> {
  const response = await apiClient.get<AssignmentDto>(`/assignments/my/${exerciseId}`)
  return response.data
}
