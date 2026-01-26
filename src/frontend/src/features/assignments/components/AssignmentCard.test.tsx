/**
 * AssignmentCard Component Tests
 *
 * Tests for the assignment card display.
 */

import { describe, it, expect, vi } from 'vitest'
import { screen, fireEvent } from '@testing-library/react'
import { render } from '../../../test/test-utils'
import { MemoryRouter } from 'react-router-dom'
import { AssignmentCard } from './AssignmentCard'
import type { AssignmentDto, AssignmentSectionType } from '../types'

// Mock useNavigate
const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

// Helper to create mock assignment
const createMockAssignment = (overrides: Partial<AssignmentDto> = {}): AssignmentDto => ({
  exerciseId: 'exercise-1',
  exerciseName: 'Hurricane Maria TTX',
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
  location: 'Emergency Operations Center',
  timeZoneId: 'America/New_York',
  ...overrides,
})

// Helper to render with router
const renderWithRouter = (
  assignment: AssignmentDto,
  sectionType: AssignmentSectionType = 'active',
) => {
  return render(
    <MemoryRouter>
      <AssignmentCard assignment={assignment} sectionType={sectionType} />
    </MemoryRouter>,
  )
}

describe('AssignmentCard', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('Basic Rendering', () => {
    it('renders exercise name', () => {
      renderWithRouter(createMockAssignment())
      expect(screen.getByText('Hurricane Maria TTX')).toBeInTheDocument()
    })

    it('renders role badge', () => {
      renderWithRouter(createMockAssignment({ role: 'Controller' }))
      expect(screen.getByText('Controller')).toBeInTheDocument()
    })

    it('renders exercise type chip', () => {
      renderWithRouter(createMockAssignment({ exerciseType: 'TTX' }))
      expect(screen.getByText('TTX')).toBeInTheDocument()
    })

    it('renders location when present', () => {
      renderWithRouter(createMockAssignment({ location: 'Emergency Operations Center' }))
      expect(screen.getByText('Emergency Operations Center')).toBeInTheDocument()
    })

    it('does not render location when null', () => {
      renderWithRouter(createMockAssignment({ location: null }))
      expect(screen.queryByText('Emergency Operations Center')).not.toBeInTheDocument()
    })

    it('renders scheduled date', () => {
      renderWithRouter(createMockAssignment({ scheduledDate: '2026-01-15' }))
      // Date is formatted by toLocaleDateString, so check for year at minimum
      // The exact format depends on locale, so use flexible matching
      expect(screen.getByText(/2026/)).toBeInTheDocument()
    })
  })

  describe('Active Section Display', () => {
    it('shows clock state for active exercises', () => {
      renderWithRouter(
        createMockAssignment({ clockState: 'Running' }),
        'active',
      )
      // Clock time is displayed as HH:MM:SS for elapsedSeconds: 3600 = 01:00:00
      expect(screen.getByText('01:00:00')).toBeInTheDocument()
    })

    it('shows "Not Started" when elapsed time is 0', () => {
      renderWithRouter(
        createMockAssignment({ elapsedSeconds: 0 }),
        'active',
      )
      expect(screen.getByText('Not Started')).toBeInTheDocument()
    })

    it('shows progress bar for active exercises with injects', () => {
      renderWithRouter(
        createMockAssignment({ totalInjects: 10, firedInjects: 5 }),
        'active',
      )
      expect(screen.getByText('5 / 10 injects')).toBeInTheDocument()
      expect(screen.getByRole('progressbar')).toBeInTheDocument()
    })

    it('does not show progress bar when no injects', () => {
      renderWithRouter(
        createMockAssignment({ totalInjects: 0, firedInjects: 0 }),
        'active',
      )
      expect(screen.queryByRole('progressbar')).not.toBeInTheDocument()
    })

    it('shows ready injects count for Controllers', () => {
      renderWithRouter(
        createMockAssignment({ role: 'Controller', readyInjects: 3 }),
        'active',
      )
      expect(screen.getByText('3 injects ready')).toBeInTheDocument()
    })

    it('does not show ready injects for non-Controllers', () => {
      renderWithRouter(
        createMockAssignment({ role: 'Evaluator', readyInjects: 3 }),
        'active',
      )
      expect(screen.queryByText('3 injects ready')).not.toBeInTheDocument()
    })
  })

  describe('Completed Section Display', () => {
    it('shows completed indicator for completed exercises', () => {
      renderWithRouter(
        createMockAssignment({
          exerciseStatus: 'Completed',
          completedAt: '2026-01-15T12:00:00Z',
        }),
        'completed',
      )
      expect(screen.getByText('Completed')).toBeInTheDocument()
    })

    it('does not show clock time for completed exercises', () => {
      renderWithRouter(
        createMockAssignment({
          exerciseStatus: 'Completed',
          elapsedSeconds: 3600,
        }),
        'completed',
      )
      // Should not show the formatted time
      expect(screen.queryByText('01:00:00')).not.toBeInTheDocument()
    })
  })

  describe('Role Badge Colors', () => {
    it('renders Controller badge with success color', () => {
      renderWithRouter(createMockAssignment({ role: 'Controller' }))
      const chip = screen.getByText('Controller').closest('.MuiChip-root')
      expect(chip).toHaveClass('MuiChip-colorSuccess')
    })

    it('renders Evaluator badge with info color', () => {
      renderWithRouter(createMockAssignment({ role: 'Evaluator' }))
      const chip = screen.getByText('Evaluator').closest('.MuiChip-root')
      expect(chip).toHaveClass('MuiChip-colorInfo')
    })

    it('renders Exercise Director badge with primary color', () => {
      renderWithRouter(createMockAssignment({ role: 'ExerciseDirector' }))
      const chip = screen.getByText('Exercise Director').closest('.MuiChip-root')
      expect(chip).toHaveClass('MuiChip-colorPrimary')
    })

    it('renders Observer badge with secondary color', () => {
      renderWithRouter(createMockAssignment({ role: 'Observer' }))
      const chip = screen.getByText('Observer').closest('.MuiChip-root')
      expect(chip).toHaveClass('MuiChip-colorSecondary')
    })
  })

  describe('Navigation', () => {
    it('navigates to conduct page for Controller in active exercise', () => {
      renderWithRouter(
        createMockAssignment({
          exerciseId: 'ex-123',
          role: 'Controller',
          exerciseStatus: 'Active',
        }),
        'active',
      )

      const card = screen.getByRole('button')
      fireEvent.click(card)

      expect(mockNavigate).toHaveBeenCalledWith('/exercises/ex-123/conduct')
    })

    it('navigates to observations page for Evaluator in active exercise', () => {
      renderWithRouter(
        createMockAssignment({
          exerciseId: 'ex-123',
          role: 'Evaluator',
          exerciseStatus: 'Active',
        }),
        'active',
      )

      const card = screen.getByRole('button')
      fireEvent.click(card)

      expect(mockNavigate).toHaveBeenCalledWith('/exercises/ex-123/observations')
    })

    it('navigates to exercise detail for draft exercises', () => {
      renderWithRouter(
        createMockAssignment({
          exerciseId: 'ex-123',
          role: 'Controller',
          exerciseStatus: 'Draft',
        }),
        'upcoming',
      )

      const card = screen.getByRole('button')
      fireEvent.click(card)

      expect(mockNavigate).toHaveBeenCalledWith('/exercises/ex-123')
    })

    it('navigates to conduct page for Observer in active exercise', () => {
      renderWithRouter(
        createMockAssignment({
          exerciseId: 'ex-123',
          role: 'Observer',
          exerciseStatus: 'Active',
        }),
        'active',
      )

      const card = screen.getByRole('button')
      fireEvent.click(card)

      expect(mockNavigate).toHaveBeenCalledWith('/exercises/ex-123/conduct')
    })
  })

  describe('Elapsed Time Formatting', () => {
    it('formats time less than an hour correctly', () => {
      renderWithRouter(
        createMockAssignment({ elapsedSeconds: 754 }), // 12:34
        'active',
      )
      expect(screen.getByText('00:12:34')).toBeInTheDocument()
    })

    it('formats time with hours correctly', () => {
      renderWithRouter(
        createMockAssignment({ elapsedSeconds: 7384 }), // 2:03:04
        'active',
      )
      expect(screen.getByText('02:03:04')).toBeInTheDocument()
    })

    it('handles null elapsed time', () => {
      renderWithRouter(
        createMockAssignment({ elapsedSeconds: null }),
        'active',
      )
      expect(screen.getByText('Not Started')).toBeInTheDocument()
    })
  })
})
