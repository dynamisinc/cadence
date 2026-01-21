/**
 * DeleteExerciseDialog Component Tests
 *
 * Tests for the permanent delete exercise confirmation dialog.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { render } from '../../../test/test-utils'
import { DeleteExerciseDialog } from './DeleteExerciseDialog'
import type { ExerciseDto, DeleteSummaryResponse } from '../types'
import { ExerciseType, ExerciseStatus } from '../../../types'
import { exerciseService } from '../services/exerciseService'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

// Mock the exercise service
vi.mock('../services/exerciseService', () => ({
  exerciseService: {
    getDeleteSummary: vi.fn(),
    deleteExercise: vi.fn(),
  },
}))

// Mock react-toastify
vi.mock('react-toastify', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  },
}))

// Helper to create mock exercise
const createMockExercise = (overrides: Partial<ExerciseDto> = {}): ExerciseDto => ({
  id: 'exercise-1',
  name: 'Test Exercise',
  description: 'Test description',
  exerciseType: ExerciseType.TTX,
  status: ExerciseStatus.Draft,
  isPracticeMode: false,
  scheduledDate: '2025-01-15',
  startTime: '09:00:00',
  endTime: '17:00:00',
  timeZoneId: 'America/New_York',
  location: 'Emergency Operations Center',
  organizationId: 'org-1',
  activeMselId: null,
  deliveryMode: 'FacilitatorPaced',
  timelineMode: 'RealTime',
  timeScale: null,
  createdAt: '2025-01-01T00:00:00Z',
  updatedAt: '2025-01-01T00:00:00Z',
  createdBy: 'user-1',
  activatedAt: null,
  activatedBy: null,
  completedAt: null,
  completedBy: null,
  archivedAt: null,
  archivedBy: null,
  hasBeenPublished: false,
  previousStatus: null,
  ...overrides,
})

const createMockDeleteSummary = (): DeleteSummaryResponse => ({
  exerciseId: 'exercise-1',
  exerciseName: 'Test Exercise',
  canDelete: true,
  deleteReason: 'NeverPublished',
  cannotDeleteReason: null,
  summary: {
    injectCount: 5,
    phaseCount: 2,
    observationCount: 3,
    participantCount: 4,
    expectedOutcomeCount: 10,
    objectiveCount: 6,
    mselCount: 1,
  },
})

// Custom render with QueryClient
const renderWithQueryClient = (ui: React.ReactElement) => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  })
  return render(
    <QueryClientProvider client={queryClient}>
      {ui}
    </QueryClientProvider>,
  )
}

describe('DeleteExerciseDialog', () => {
  const defaultProps = {
    open: true,
    exercise: createMockExercise(),
    onClose: vi.fn(),
    onDeleted: vi.fn(),
  }

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(exerciseService.getDeleteSummary).mockResolvedValue(createMockDeleteSummary())
    vi.mocked(exerciseService.deleteExercise).mockResolvedValue(undefined)
  })

  describe('Basic Rendering', () => {
    it('renders dialog when open is true', async () => {
      renderWithQueryClient(<DeleteExerciseDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })
    })

    it('does not render when exercise is null', () => {
      renderWithQueryClient(<DeleteExerciseDialog {...defaultProps} exercise={null} />)

      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })

    it('renders dialog title with warning icon', async () => {
      renderWithQueryClient(<DeleteExerciseDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByText('Permanently Delete Exercise')).toBeInTheDocument()
      })
    })

    it('renders warning message about permanent deletion', async () => {
      renderWithQueryClient(<DeleteExerciseDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByText(/CANNOT/)).toBeInTheDocument()
      })
      expect(screen.getByText(/be undone/i)).toBeInTheDocument()
    })
  })

  describe('Loading State', () => {
    it('shows loading indicator while fetching summary', async () => {
      vi.mocked(exerciseService.getDeleteSummary).mockImplementation(() =>
        new Promise(resolve => setTimeout(() => resolve(createMockDeleteSummary()), 100)),
      )

      renderWithQueryClient(<DeleteExerciseDialog {...defaultProps} />)

      expect(screen.getByRole('progressbar')).toBeInTheDocument()
    })

    it('hides loading indicator after summary loads', async () => {
      renderWithQueryClient(<DeleteExerciseDialog {...defaultProps} />)

      // Wait for data to load - check for something that only appears after loading
      await waitFor(
        () => {
          expect(screen.queryByRole('progressbar')).not.toBeInTheDocument()
        },
        { timeout: 10000 },
      )
    })
  })

  describe('Data Summary Display', () => {
    it('displays inject count', async () => {
      renderWithQueryClient(<DeleteExerciseDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByText(/5 injects/i)).toBeInTheDocument()
      })
    })

    it('displays phase count', async () => {
      renderWithQueryClient(<DeleteExerciseDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByText(/2 phases/i)).toBeInTheDocument()
      })
    })

    it('displays observation count', async () => {
      renderWithQueryClient(<DeleteExerciseDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByText(/3 observations/i)).toBeInTheDocument()
      })
    })
  })

  describe('Two-Step Confirmation', () => {
    it('renders name confirmation text field', async () => {
      renderWithQueryClient(<DeleteExerciseDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByLabelText(/Type exercise name to confirm/i)).toBeInTheDocument()
      })
    })

    it('renders confirmation checkbox', async () => {
      renderWithQueryClient(<DeleteExerciseDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByRole('checkbox')).toBeInTheDocument()
      })
    })

    it('delete button is disabled by default', async () => {
      renderWithQueryClient(<DeleteExerciseDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Delete/i })).toBeDisabled()
      })
    })

    it('delete button is disabled when only name is typed', async () => {
      const user = userEvent.setup()
      renderWithQueryClient(<DeleteExerciseDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByLabelText(/Type exercise name to confirm/i)).toBeInTheDocument()
      })

      await user.type(screen.getByLabelText(/Type exercise name to confirm/i), 'Test Exercise')

      expect(screen.getByRole('button', { name: /Delete/i })).toBeDisabled()
    })

    it('delete button is disabled when only checkbox is checked', async () => {
      const user = userEvent.setup()
      renderWithQueryClient(<DeleteExerciseDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByRole('checkbox')).toBeInTheDocument()
      })

      await user.click(screen.getByRole('checkbox'))

      expect(screen.getByRole('button', { name: /Delete/i })).toBeDisabled()
    })

    it('delete button is enabled when name matches and checkbox is checked', async () => {
      const user = userEvent.setup()
      renderWithQueryClient(<DeleteExerciseDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByLabelText(/Type exercise name to confirm/i)).toBeInTheDocument()
      })

      await user.type(screen.getByLabelText(/Type exercise name to confirm/i), 'Test Exercise')
      await user.click(screen.getByRole('checkbox'))

      expect(screen.getByRole('button', { name: /Delete/i })).toBeEnabled()
    })
  })

  describe('Button Actions', () => {
    it('renders Cancel button', async () => {
      renderWithQueryClient(<DeleteExerciseDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'Cancel' })).toBeInTheDocument()
      })
    })

    it('calls onClose when Cancel is clicked', async () => {
      const user = userEvent.setup()
      const onClose = vi.fn()
      renderWithQueryClient(<DeleteExerciseDialog {...defaultProps} onClose={onClose} />)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'Cancel' })).toBeInTheDocument()
      })

      await user.click(screen.getByRole('button', { name: 'Cancel' }))

      expect(onClose).toHaveBeenCalledTimes(1)
    })

    it(
      'calls exerciseService.deleteExercise when confirmed',
      async () => {
        const user = userEvent.setup({ delay: null }) // Faster typing
        renderWithQueryClient(<DeleteExerciseDialog {...defaultProps} />)

        await waitFor(() => {
          expect(screen.getByLabelText(/Type exercise name to confirm/i)).toBeInTheDocument()
        })

        await user.type(screen.getByLabelText(/Type exercise name to confirm/i), 'Test Exercise')
        await user.click(screen.getByRole('checkbox'))
        await user.click(screen.getByRole('button', { name: /Delete/i }))

        await waitFor(() => {
          expect(exerciseService.deleteExercise).toHaveBeenCalledWith('exercise-1')
        })
      },
      10000,
    )
  })

  describe('Error Handling', () => {
    it('shows error when summary fails to load', async () => {
      vi.mocked(exerciseService.getDeleteSummary).mockRejectedValue(new Error('Network error'))

      renderWithQueryClient(<DeleteExerciseDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByText(/Failed to load delete summary/i)).toBeInTheDocument()
      })
    })

    it('shows error when canDelete is false', async () => {
      vi.mocked(exerciseService.getDeleteSummary).mockResolvedValue({
        ...createMockDeleteSummary(),
        canDelete: false,
        cannotDeleteReason: 'MustArchiveFirst',
      })

      renderWithQueryClient(<DeleteExerciseDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByText(/must be archived before it can be deleted/i)).toBeInTheDocument()
      })
    })
  })

  describe('Accessibility', () => {
    it('has proper aria-labelledby', async () => {
      renderWithQueryClient(<DeleteExerciseDialog {...defaultProps} />)

      await waitFor(() => {
        const dialog = screen.getByRole('dialog')
        expect(dialog).toHaveAttribute('aria-labelledby', 'delete-dialog-title')
      })
    })
  })
})
