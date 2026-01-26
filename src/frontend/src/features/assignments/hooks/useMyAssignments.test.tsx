/**
 * useMyAssignments Hook Tests
 *
 * Tests for the My Assignments React Query hook.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useMyAssignments, ASSIGNMENTS_QUERY_KEY } from './useMyAssignments'
import * as assignmentService from '../services/assignmentService'
import type { MyAssignmentsResponse, AssignmentDto } from '../types'
import type { ReactNode } from 'react'

// Mock the assignment service
vi.mock('../services/assignmentService', () => ({
  getMyAssignments: vi.fn(),
}))

// Helper to create mock assignment
const createMockAssignment = (overrides: Partial<AssignmentDto> = {}): AssignmentDto => ({
  exerciseId: 'exercise-1',
  exerciseName: 'Test Exercise',
  role: 'Controller',
  exerciseStatus: 'Active',
  exerciseType: 'TTX',
  scheduledDate: '2026-01-15',
  startTime: '09:00:00',
  clockState: 'Running',
  elapsedSeconds: 3600,
  completedAt: null,
  assignedAt: '2026-01-10T00:00:00Z',
  totalInjects: 10,
  firedInjects: 5,
  readyInjects: 2,
  location: 'Test Location',
  timeZoneId: 'America/New_York',
  ...overrides,
})

// Create a fresh QueryClient for each test
const createTestQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  })

// Wrapper component for React Query
const createWrapper = () => {
  const queryClient = createTestQueryClient()
  return ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  )
}

describe('useMyAssignments', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('fetches assignments on mount', async () => {
    const mockResponse: MyAssignmentsResponse = {
      active: [createMockAssignment()],
      upcoming: [],
      completed: [],
    }
    vi.mocked(assignmentService.getMyAssignments).mockResolvedValue(mockResponse)

    const { result } = renderHook(() => useMyAssignments(), {
      wrapper: createWrapper(),
    })

    expect(result.current.isLoading).toBe(true)

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false)
    })

    expect(result.current.data).toEqual(mockResponse)
    expect(result.current.data?.active).toHaveLength(1)
    expect(assignmentService.getMyAssignments).toHaveBeenCalledTimes(1)
  })

  it('handles fetch error', async () => {
    vi.mocked(assignmentService.getMyAssignments).mockRejectedValue(
      new Error('Network error'),
    )

    const { result } = renderHook(() => useMyAssignments(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false)
    })

    expect(result.current.isError).toBe(true)
    expect(result.current.error?.message).toBe('Network error')
  })

  it('returns grouped assignments correctly', async () => {
    const mockResponse: MyAssignmentsResponse = {
      active: [
        createMockAssignment({ exerciseId: 'ex-1', exerciseName: 'Active Exercise' }),
      ],
      upcoming: [
        createMockAssignment({
          exerciseId: 'ex-2',
          exerciseName: 'Upcoming Exercise',
          exerciseStatus: 'Draft',
        }),
      ],
      completed: [
        createMockAssignment({
          exerciseId: 'ex-3',
          exerciseName: 'Completed Exercise',
          exerciseStatus: 'Completed',
          completedAt: '2026-01-10T12:00:00Z',
        }),
      ],
    }
    vi.mocked(assignmentService.getMyAssignments).mockResolvedValue(mockResponse)

    const { result } = renderHook(() => useMyAssignments(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false)
    })

    expect(result.current.data?.active).toHaveLength(1)
    expect(result.current.data?.upcoming).toHaveLength(1)
    expect(result.current.data?.completed).toHaveLength(1)
    expect(result.current.data?.active[0].exerciseName).toBe('Active Exercise')
    expect(result.current.data?.upcoming[0].exerciseName).toBe('Upcoming Exercise')
    expect(result.current.data?.completed[0].exerciseName).toBe('Completed Exercise')
  })

  it('returns empty arrays when no assignments', async () => {
    const mockResponse: MyAssignmentsResponse = {
      active: [],
      upcoming: [],
      completed: [],
    }
    vi.mocked(assignmentService.getMyAssignments).mockResolvedValue(mockResponse)

    const { result } = renderHook(() => useMyAssignments(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false)
    })

    expect(result.current.data?.active).toHaveLength(0)
    expect(result.current.data?.upcoming).toHaveLength(0)
    expect(result.current.data?.completed).toHaveLength(0)
  })

  it('uses correct query key', async () => {
    const mockResponse: MyAssignmentsResponse = {
      active: [],
      upcoming: [],
      completed: [],
    }
    vi.mocked(assignmentService.getMyAssignments).mockResolvedValue(mockResponse)

    const queryClient = createTestQueryClient()
    const wrapper = ({ children }: { children: ReactNode }) => (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    )

    const { result } = renderHook(() => useMyAssignments(), { wrapper })

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false)
    })

    // Query should be cached with the correct key
    const cachedData = queryClient.getQueryData(ASSIGNMENTS_QUERY_KEY)
    expect(cachedData).toEqual(mockResponse)
  })
})
