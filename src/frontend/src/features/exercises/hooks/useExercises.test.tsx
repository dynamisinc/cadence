import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useExercises } from './useExercises'
import { exerciseService } from '../services/exerciseService'
import { ExerciseType, ExerciseStatus } from '../../../types'
import type { ExerciseDto } from '../types'
import type { ReactNode } from 'react'

// Mock the exercise service
vi.mock('../services/exerciseService', () => ({
  exerciseService: {
    getExercises: vi.fn(),
    createExercise: vi.fn(),
    updateExercise: vi.fn(),
  },
}))

// Mock notify wrapper
vi.mock('@/shared/utils/notify', () => ({
  notify: {
    success: vi.fn(),
    error: vi.fn(),
  },
}))

const mockExercise: ExerciseDto = {
  id: '123',
  name: 'Test Exercise',
  description: 'Test description',
  exerciseType: ExerciseType.TTX,
  status: ExerciseStatus.Draft,
  isPracticeMode: false,
  scheduledDate: '2026-01-15',
  startTime: '09:00:00',
  endTime: '12:00:00',
  timeZoneId: 'America/New_York',
  location: 'Test Location',
  organizationId: 'org-123',
  activeMselId: null,
  deliveryMode: 'FacilitatorPaced',
  timelineMode: 'RealTime',
  timeScale: null,
  createdAt: '2026-01-10T00:00:00Z',
  updatedAt: '2026-01-10T00:00:00Z',
  createdBy: 'user-123',
  activatedAt: null,
  activatedBy: null,
  completedAt: null,
  completedBy: null,
  archivedAt: null,
  archivedBy: null,
  hasBeenPublished: false,
  previousStatus: null,
}

// Create a fresh QueryClient for each test
const createTestQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: {
        retry: false, // Don't retry in tests
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

describe('useExercises', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('fetches exercises on mount', async () => {
    vi.mocked(exerciseService.getExercises).mockResolvedValue([mockExercise])

    const { result } = renderHook(() => useExercises(), {
      wrapper: createWrapper(),
    })

    expect(result.current.loading).toBe(true)

    await waitFor(() => {
      expect(result.current.loading).toBe(false)
    })

    expect(result.current.exercises).toHaveLength(1)
    expect(result.current.exercises[0].name).toBe('Test Exercise')
    expect(exerciseService.getExercises).toHaveBeenCalledTimes(1)
  })

  it('handles fetch error', async () => {
    vi.mocked(exerciseService.getExercises).mockRejectedValue(
      new Error('Network error'),
    )

    const { result } = renderHook(() => useExercises(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.loading).toBe(false)
    })

    expect(result.current.error).toBe('Network error')
    expect(result.current.exercises).toHaveLength(0)
  })

  it('creates an exercise and adds to list', async () => {
    vi.mocked(exerciseService.getExercises).mockResolvedValue([])
    vi.mocked(exerciseService.createExercise).mockResolvedValue(mockExercise)

    const { result } = renderHook(() => useExercises(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.loading).toBe(false)
    })

    await result.current.createExercise({
      name: 'Test Exercise',
      exerciseType: ExerciseType.TTX,
      scheduledDate: '2026-01-15',
      deliveryMode: 'FacilitatorPaced',
      timelineMode: 'RealTime',
    })

    await waitFor(() => {
      expect(result.current.exercises).toHaveLength(1)
    })

    expect(result.current.exercises[0].name).toBe('Test Exercise')
  })

  it('updates an exercise in the list', async () => {
    const updatedExercise = { ...mockExercise, name: 'Updated Exercise' }

    vi.mocked(exerciseService.getExercises).mockResolvedValue([mockExercise])
    vi.mocked(exerciseService.updateExercise).mockResolvedValue(updatedExercise)

    const { result } = renderHook(() => useExercises(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.exercises).toHaveLength(1)
    })

    await result.current.updateExercise('123', {
      name: 'Updated Exercise',
      exerciseType: ExerciseType.TTX,
      scheduledDate: '2026-01-15',
      deliveryMode: 'FacilitatorPaced',
      timelineMode: 'RealTime',
    })

    await waitFor(() => {
      expect(result.current.exercises[0].name).toBe('Updated Exercise')
    })
  })
})
