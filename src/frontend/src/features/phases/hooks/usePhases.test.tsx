import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor, act } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { usePhases, phaseKeys } from './usePhases'
import { phaseService } from '../services/phaseService'
import type { PhaseDto, CreatePhaseRequest, UpdatePhaseRequest } from '../types'

// Mock the phase service
vi.mock('../services/phaseService', () => ({
  phaseService: {
    getPhases: vi.fn(),
    getPhase: vi.fn(),
    createPhase: vi.fn(),
    updatePhase: vi.fn(),
    deletePhase: vi.fn(),
    reorderPhases: vi.fn(),
  },
}))

// Mock notify
vi.mock('@/shared/utils/notify', () => ({
  notify: {
    success: vi.fn(),
    error: vi.fn(),
  },
}))

const mockPhases: PhaseDto[] = [
  {
    id: 'phase-1',
    name: 'Warning Phase',
    description: 'Initial warning period',
    sequence: 1,
    startTime: '08:00:00',
    endTime: '09:00:00',
    exerciseId: 'exercise-1',
    injectCount: 5,
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-01T00:00:00Z',
  },
  {
    id: 'phase-2',
    name: 'Response Phase',
    description: 'Active response period',
    sequence: 2,
    startTime: '09:00:00',
    endTime: '12:00:00',
    exerciseId: 'exercise-1',
    injectCount: 10,
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-01T00:00:00Z',
  },
  {
    id: 'phase-3',
    name: 'Recovery Phase',
    description: 'Recovery operations',
    sequence: 3,
    startTime: '12:00:00',
    endTime: '14:00:00',
    exerciseId: 'exercise-1',
    injectCount: 3,
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-01T00:00:00Z',
  },
]

// Helper to create a wrapper with React Query provider
const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  })

  const Wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  )

  return { Wrapper, queryClient }
}

describe('usePhases', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(phaseService.getPhases).mockResolvedValue(mockPhases)
  })

  describe('phaseKeys', () => {
    it('generates correct query key for all phases', () => {
      const key = phaseKeys.all('exercise-1')
      expect(key).toEqual(['exercises', 'exercise-1', 'phases'])
    })

    it('generates correct query key for phase detail', () => {
      const key = phaseKeys.detail('exercise-1', 'phase-1')
      expect(key).toEqual(['exercises', 'exercise-1', 'phases', 'phase-1'])
    })
  })

  describe('initial state', () => {
    it('starts with empty phases array', () => {
      vi.mocked(phaseService.getPhases).mockImplementation(
        () => new Promise(() => {}), // Never resolves
      )

      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      expect(result.current.phases).toEqual([])
    })

    it('starts with loading state', () => {
      vi.mocked(phaseService.getPhases).mockImplementation(
        () => new Promise(() => {}),
      )

      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      expect(result.current.loading).toBe(true)
    })

    it('starts with no error', () => {
      vi.mocked(phaseService.getPhases).mockImplementation(
        () => new Promise(() => {}),
      )

      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      expect(result.current.error).toBeNull()
    })

    it('does not fetch when exerciseId is empty', () => {
      const { Wrapper } = createWrapper()
      renderHook(() => usePhases(''), { wrapper: Wrapper })

      expect(phaseService.getPhases).not.toHaveBeenCalled()
    })
  })

  describe('fetching phases', () => {
    it('fetches phases on mount', async () => {
      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.phases).toEqual(mockPhases)
      })

      expect(phaseService.getPhases).toHaveBeenCalledWith('exercise-1')
      expect(phaseService.getPhases).toHaveBeenCalledTimes(1)
    })

    it('sets loading to false after fetch', async () => {
      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.loading).toBe(false)
      })
    })

    it('sets error on fetch failure', async () => {
      vi.mocked(phaseService.getPhases).mockRejectedValue(
        new Error('Failed to fetch phases'),
      )

      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.error).toBe('Failed to fetch phases')
      })
    })

    it('returns generic error message for non-Error objects', async () => {
      vi.mocked(phaseService.getPhases).mockRejectedValue('Unknown error')

      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.error).toBe('Failed to load phases')
      })
    })
  })

  describe('createPhase', () => {
    it('creates a phase and adds it to the list', async () => {
      const newPhase: PhaseDto = {
        id: 'phase-4',
        name: 'New Phase',
        description: 'New phase description',
        sequence: 4,
        startTime: null,
        endTime: null,
        exerciseId: 'exercise-1',
        injectCount: 0,
        createdAt: '2024-01-02T00:00:00Z',
        updatedAt: '2024-01-02T00:00:00Z',
      }

      vi.mocked(phaseService.createPhase).mockResolvedValue(newPhase)

      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.phases).toEqual(mockPhases)
      })

      const request: CreatePhaseRequest = {
        name: 'New Phase',
        description: 'New phase description',
      }

      await act(async () => {
        await result.current.createPhase(request)
      })

      expect(phaseService.createPhase).toHaveBeenCalledWith('exercise-1', request)
      await waitFor(() => {
        expect(result.current.phases).toContainEqual(newPhase)
      })
    })

    it('returns the created phase', async () => {
      const newPhase: PhaseDto = {
        id: 'phase-5',
        name: 'Created Phase',
        description: null,
        sequence: 5,
        startTime: null,
        endTime: null,
        exerciseId: 'exercise-1',
        injectCount: 0,
        createdAt: '2024-01-02T00:00:00Z',
        updatedAt: '2024-01-02T00:00:00Z',
      }

      vi.mocked(phaseService.createPhase).mockResolvedValue(newPhase)

      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.loading).toBe(false)
      })

      let createdPhase: PhaseDto | undefined
      await act(async () => {
        createdPhase = await result.current.createPhase({ name: 'Created Phase' })
      })

      expect(createdPhase).toEqual(newPhase)
    })

    it('sets isCreating during mutation', async () => {
      let resolveCreate: (value: PhaseDto) => void = () => {}
      vi.mocked(phaseService.createPhase).mockImplementation(
        () =>
          new Promise(resolve => {
            resolveCreate = resolve
          }),
      )

      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.loading).toBe(false)
      })

      expect(result.current.isCreating).toBe(false)

      act(() => {
        result.current.createPhase({ name: 'Test' })
      })

      await waitFor(() => {
        expect(result.current.isCreating).toBe(true)
      })

      await act(async () => {
        resolveCreate({
          ...mockPhases[0],
          id: 'new-phase',
          name: 'Test',
        })
      })

      await waitFor(() => {
        expect(result.current.isCreating).toBe(false)
      })
    })
  })

  describe('updatePhase', () => {
    it('updates a phase in the list', async () => {
      const updatedPhase: PhaseDto = {
        ...mockPhases[0],
        name: 'Updated Phase Name',
        updatedAt: '2024-01-02T00:00:00Z',
      }

      vi.mocked(phaseService.updatePhase).mockResolvedValue(updatedPhase)

      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.phases).toEqual(mockPhases)
      })

      const request: UpdatePhaseRequest = {
        name: 'Updated Phase Name',
        description: mockPhases[0].description,
      }

      await act(async () => {
        await result.current.updatePhase('phase-1', request)
      })

      expect(phaseService.updatePhase).toHaveBeenCalledWith(
        'exercise-1',
        'phase-1',
        request,
      )
      await waitFor(() => {
        expect(result.current.phases.find(p => p.id === 'phase-1')?.name).toBe(
          'Updated Phase Name',
        )
      })
    })

    it('applies optimistic update immediately', async () => {
      let resolveUpdate: (value: PhaseDto) => void = () => {}
      vi.mocked(phaseService.updatePhase).mockImplementation(
        () =>
          new Promise(resolve => {
            resolveUpdate = resolve
          }),
      )

      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.phases).toEqual(mockPhases)
      })

      act(() => {
        result.current.updatePhase('phase-1', { name: 'Optimistic Name' })
      })

      // Check optimistic update applied before API resolves
      await waitFor(() => {
        expect(result.current.phases.find(p => p.id === 'phase-1')?.name).toBe(
          'Optimistic Name',
        )
      })

      // Resolve the API call
      await act(async () => {
        resolveUpdate({
          ...mockPhases[0],
          name: 'Optimistic Name',
        })
      })
    })

    it('rolls back on error', async () => {
      vi.mocked(phaseService.updatePhase).mockRejectedValue(
        new Error('Update failed'),
      )

      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.phases).toEqual(mockPhases)
      })

      await act(async () => {
        try {
          await result.current.updatePhase('phase-1', { name: 'Will Fail' })
        } catch {
          // Expected to throw
        }
      })

      // Should roll back to original name
      await waitFor(() => {
        expect(result.current.phases.find(p => p.id === 'phase-1')?.name).toBe(
          'Warning Phase',
        )
      })
    })
  })

  describe('deletePhase', () => {
    it('removes a phase from the list', async () => {
      vi.mocked(phaseService.deletePhase).mockResolvedValue()

      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.phases).toEqual(mockPhases)
      })

      await act(async () => {
        await result.current.deletePhase('phase-1')
      })

      expect(phaseService.deletePhase).toHaveBeenCalledWith('exercise-1', 'phase-1')
      await waitFor(() => {
        expect(result.current.phases.find(p => p.id === 'phase-1')).toBeUndefined()
        expect(result.current.phases).toHaveLength(2)
      })
    })

    it('applies optimistic delete immediately', async () => {
      let resolveDelete: () => void = () => {}
      vi.mocked(phaseService.deletePhase).mockImplementation(
        () =>
          new Promise(resolve => {
            resolveDelete = resolve
          }),
      )

      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.phases).toHaveLength(3)
      })

      act(() => {
        result.current.deletePhase('phase-1')
      })

      // Check optimistic delete applied before API resolves
      await waitFor(() => {
        expect(result.current.phases).toHaveLength(2)
      })

      await act(async () => {
        resolveDelete()
      })
    })

    it('rolls back on error', async () => {
      vi.mocked(phaseService.deletePhase).mockRejectedValue(
        new Error('Cannot delete phase with injects'),
      )

      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.phases).toHaveLength(3)
      })

      await act(async () => {
        try {
          await result.current.deletePhase('phase-1')
        } catch {
          // Expected to throw
        }
      })

      // Should roll back
      await waitFor(() => {
        expect(result.current.phases).toHaveLength(3)
        expect(result.current.phases.find(p => p.id === 'phase-1')).toBeDefined()
      })
    })
  })

  describe('reorderPhases', () => {
    it('reorders phases', async () => {
      const reorderedPhases: PhaseDto[] = [
        { ...mockPhases[2], sequence: 1 },
        { ...mockPhases[0], sequence: 2 },
        { ...mockPhases[1], sequence: 3 },
      ]

      vi.mocked(phaseService.reorderPhases).mockResolvedValue(reorderedPhases)

      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.phases).toEqual(mockPhases)
      })

      await act(async () => {
        await result.current.reorderPhases(['phase-3', 'phase-1', 'phase-2'])
      })

      expect(phaseService.reorderPhases).toHaveBeenCalledWith('exercise-1', {
        phaseIds: ['phase-3', 'phase-1', 'phase-2'],
      })
      await waitFor(() => {
        expect(result.current.phases[0].id).toBe('phase-3')
        expect(result.current.phases[0].sequence).toBe(1)
      })
    })

    it('applies optimistic reorder immediately', async () => {
      let resolveReorder: (value: PhaseDto[]) => void = () => {}
      vi.mocked(phaseService.reorderPhases).mockImplementation(
        () =>
          new Promise(resolve => {
            resolveReorder = resolve
          }),
      )

      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.phases[0].id).toBe('phase-1')
      })

      act(() => {
        result.current.reorderPhases(['phase-2', 'phase-1', 'phase-3'])
      })

      // Check optimistic reorder applied before API resolves
      await waitFor(() => {
        expect(result.current.phases[0].id).toBe('phase-2')
        expect(result.current.phases[0].sequence).toBe(1)
      })

      await act(async () => {
        resolveReorder([
          { ...mockPhases[1], sequence: 1 },
          { ...mockPhases[0], sequence: 2 },
          { ...mockPhases[2], sequence: 3 },
        ])
      })
    })
  })

  describe('movePhaseUp', () => {
    it('moves a phase up in order', async () => {
      const reorderedPhases: PhaseDto[] = [
        { ...mockPhases[1], sequence: 1 },
        { ...mockPhases[0], sequence: 2 },
        { ...mockPhases[2], sequence: 3 },
      ]

      vi.mocked(phaseService.reorderPhases).mockResolvedValue(reorderedPhases)

      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.phases).toEqual(mockPhases)
      })

      await act(async () => {
        await result.current.movePhaseUp('phase-2')
      })

      expect(phaseService.reorderPhases).toHaveBeenCalledWith('exercise-1', {
        phaseIds: ['phase-2', 'phase-1', 'phase-3'],
      })
    })

    it('does nothing when phase is already at top', async () => {
      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.phases).toEqual(mockPhases)
      })

      await act(async () => {
        await result.current.movePhaseUp('phase-1')
      })

      expect(phaseService.reorderPhases).not.toHaveBeenCalled()
    })

    it('does nothing for non-existent phase', async () => {
      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.phases).toEqual(mockPhases)
      })

      await act(async () => {
        await result.current.movePhaseUp('nonexistent')
      })

      expect(phaseService.reorderPhases).not.toHaveBeenCalled()
    })
  })

  describe('movePhaseDown', () => {
    it('moves a phase down in order', async () => {
      const reorderedPhases: PhaseDto[] = [
        { ...mockPhases[1], sequence: 1 },
        { ...mockPhases[0], sequence: 2 },
        { ...mockPhases[2], sequence: 3 },
      ]

      vi.mocked(phaseService.reorderPhases).mockResolvedValue(reorderedPhases)

      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.phases).toEqual(mockPhases)
      })

      await act(async () => {
        await result.current.movePhaseDown('phase-1')
      })

      expect(phaseService.reorderPhases).toHaveBeenCalledWith('exercise-1', {
        phaseIds: ['phase-2', 'phase-1', 'phase-3'],
      })
    })

    it('does nothing when phase is already at bottom', async () => {
      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.phases).toEqual(mockPhases)
      })

      await act(async () => {
        await result.current.movePhaseDown('phase-3')
      })

      expect(phaseService.reorderPhases).not.toHaveBeenCalled()
    })

    it('does nothing for non-existent phase', async () => {
      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.phases).toEqual(mockPhases)
      })

      await act(async () => {
        await result.current.movePhaseDown('nonexistent')
      })

      expect(phaseService.reorderPhases).not.toHaveBeenCalled()
    })
  })

  describe('mutation states', () => {
    it('exposes isDeleting state', async () => {
      let resolveDelete: () => void = () => {}
      vi.mocked(phaseService.deletePhase).mockImplementation(
        () =>
          new Promise(resolve => {
            resolveDelete = resolve
          }),
      )

      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.loading).toBe(false)
      })

      expect(result.current.isDeleting).toBe(false)

      act(() => {
        result.current.deletePhase('phase-1')
      })

      await waitFor(() => {
        expect(result.current.isDeleting).toBe(true)
      })

      await act(async () => {
        resolveDelete()
      })

      await waitFor(() => {
        expect(result.current.isDeleting).toBe(false)
      })
    })

    it('exposes isReordering state', async () => {
      let resolveReorder: (value: PhaseDto[]) => void = () => {}
      vi.mocked(phaseService.reorderPhases).mockImplementation(
        () =>
          new Promise(resolve => {
            resolveReorder = resolve
          }),
      )

      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => usePhases('exercise-1'), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.loading).toBe(false)
      })

      expect(result.current.isReordering).toBe(false)

      act(() => {
        result.current.reorderPhases(['phase-1', 'phase-2', 'phase-3'])
      })

      await waitFor(() => {
        expect(result.current.isReordering).toBe(true)
      })

      await act(async () => {
        resolveReorder(mockPhases)
      })

      await waitFor(() => {
        expect(result.current.isReordering).toBe(false)
      })
    })
  })
})
