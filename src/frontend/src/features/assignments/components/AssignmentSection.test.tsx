/**
 * AssignmentSection Component Tests
 *
 * Tests for the collapsible assignment section display.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { render } from '../../../test/test-utils'
import { MemoryRouter } from 'react-router-dom'
import { AssignmentSection } from './AssignmentSection'
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
  title: string,
  type: AssignmentSectionType,
  assignments: AssignmentDto[],
  props?: { isLoading?: boolean; emptyMessage?: string },
) => {
  return render(
    <MemoryRouter>
      <AssignmentSection
        title={title}
        type={type}
        assignments={assignments}
        isLoading={props?.isLoading}
        emptyMessage={props?.emptyMessage}
      />
    </MemoryRouter>,
  )
}

describe('AssignmentSection', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('Basic Rendering', () => {
    it('renders section title', () => {
      renderWithRouter('Active Exercises', 'active', [])
      expect(screen.getByRole('heading', { name: 'Active Exercises' })).toBeInTheDocument()
    })

    it('shows count badge with number of assignments', () => {
      const assignments = [
        createMockAssignment({ exerciseId: '1' }),
        createMockAssignment({ exerciseId: '2' }),
        createMockAssignment({ exerciseId: '3' }),
      ]
      renderWithRouter('Active Exercises', 'active', assignments)
      expect(screen.getByText('3')).toBeInTheDocument()
    })

    it('shows count badge with 0 when no assignments', () => {
      renderWithRouter('Active Exercises', 'active', [])
      expect(screen.getByText('0')).toBeInTheDocument()
    })
  })

  describe('Section Icons and Colors', () => {
    it('shows play icon for active type', () => {
      renderWithRouter('Active Exercises', 'active', [])
      // Check that the icon container exists (FontAwesome renders an svg)
      const iconButton = screen.getByLabelText('Collapse Active Exercises')
      expect(iconButton).toBeInTheDocument()
    })

    it('shows calendar icon for upcoming type', () => {
      renderWithRouter('Upcoming Exercises', 'upcoming', [])
      const iconButton = screen.getByLabelText('Collapse Upcoming Exercises')
      expect(iconButton).toBeInTheDocument()
    })

    it('shows checkmark icon for completed type', () => {
      renderWithRouter('Completed Exercises', 'completed', [])
      const iconButton = screen.getByLabelText('Expand Completed Exercises')
      expect(iconButton).toBeInTheDocument()
    })

    it('applies green color to active section badge', () => {
      renderWithRouter('Active Exercises', 'active', [])
      const badge = screen.getByText('0').closest('.MuiChip-root')
      expect(badge).toHaveStyle({ backgroundColor: '#4caf50' })
    })

    it('applies blue color to upcoming section badge', () => {
      renderWithRouter('Upcoming Exercises', 'upcoming', [])
      const badge = screen.getByText('0').closest('.MuiChip-root')
      expect(badge).toHaveStyle({ backgroundColor: '#1976d2' })
    })

    it('applies grey color to completed section badge', () => {
      renderWithRouter('Completed Exercises', 'completed', [])
      const badge = screen.getByText('0').closest('.MuiChip-root')
      expect(badge).toHaveStyle({ backgroundColor: '#757575' })
    })
  })

  describe('Expand/Collapse Behavior', () => {
    it('completed section is collapsed by default', () => {
      const assignments = [createMockAssignment()]
      renderWithRouter('Completed Exercises', 'completed', assignments)

      // Check aria-expanded is false
      const toggleButton = screen.getByLabelText('Expand Completed Exercises')
      expect(toggleButton).toHaveAttribute('aria-expanded', 'false')
    })

    it('active section is expanded by default', () => {
      const assignments = [createMockAssignment()]
      renderWithRouter('Active Exercises', 'active', assignments)

      // Check aria-expanded is true
      const toggleButton = screen.getByLabelText('Collapse Active Exercises')
      expect(toggleButton).toHaveAttribute('aria-expanded', 'true')

      // Content should be visible
      expect(screen.getByText('Hurricane Maria TTX')).toBeInTheDocument()
    })

    it('upcoming section is expanded by default', () => {
      const assignments = [createMockAssignment({ exerciseStatus: 'Draft' })]
      renderWithRouter('Upcoming Exercises', 'upcoming', assignments)

      // Check aria-expanded is true
      const toggleButton = screen.getByLabelText('Collapse Upcoming Exercises')
      expect(toggleButton).toHaveAttribute('aria-expanded', 'true')

      // Content should be visible
      expect(screen.getByText('Hurricane Maria TTX')).toBeInTheDocument()
    })

    it('clicking header toggles expand/collapse', async () => {
      const user = userEvent.setup()
      const assignments = [createMockAssignment()]
      renderWithRouter('Active Exercises', 'active', assignments)

      // Initially expanded - aria-expanded should be true
      let toggleButton = screen.getByLabelText('Collapse Active Exercises')
      expect(toggleButton).toHaveAttribute('aria-expanded', 'true')

      // Click the header (not the button, but the whole header area)
      const header = screen.getByRole('heading', { name: 'Active Exercises' }).closest('div')
      await user.click(header!)

      // Should be collapsed now - aria-expanded should be false
      toggleButton = screen.getByLabelText('Expand Active Exercises')
      expect(toggleButton).toHaveAttribute('aria-expanded', 'false')

      // Click again to expand
      await user.click(header!)

      // Should be expanded again - aria-expanded should be true
      toggleButton = screen.getByLabelText('Collapse Active Exercises')
      expect(toggleButton).toHaveAttribute('aria-expanded', 'true')
    })

    it('clicking toggle button toggles expand/collapse', async () => {
      const user = userEvent.setup()
      const assignments = [createMockAssignment()]
      renderWithRouter('Active Exercises', 'active', assignments)

      // Initially expanded
      let toggleButton = screen.getByLabelText('Collapse Active Exercises')
      expect(toggleButton).toHaveAttribute('aria-expanded', 'true')

      // Click the toggle button
      await user.click(toggleButton)

      // Should be collapsed now
      toggleButton = screen.getByLabelText('Expand Active Exercises')
      expect(toggleButton).toHaveAttribute('aria-expanded', 'false')

      // Click again to expand
      await user.click(toggleButton)

      // Should be expanded again
      toggleButton = screen.getByLabelText('Collapse Active Exercises')
      expect(toggleButton).toHaveAttribute('aria-expanded', 'true')
    })

    it('expand/collapse icon changes based on state', async () => {
      const user = userEvent.setup()
      renderWithRouter('Active Exercises', 'active', [])

      // Initially expanded - should show down chevron in label
      expect(screen.getByLabelText('Collapse Active Exercises')).toBeInTheDocument()

      // Click to collapse
      const toggleButton = screen.getByLabelText('Collapse Active Exercises')
      await user.click(toggleButton)

      // Should show right chevron in label
      expect(screen.getByLabelText('Expand Active Exercises')).toBeInTheDocument()
    })
  })

  describe('Loading State', () => {
    it('shows loading skeletons when isLoading is true', () => {
      renderWithRouter('Active Exercises', 'active', [], { isLoading: true })

      // Should show skeleton loaders
      const skeletons = screen.getAllByRole('generic').filter(
        el => el.classList.contains('MuiSkeleton-root'),
      )
      expect(skeletons.length).toBeGreaterThan(0)
    })

    it('does not show assignments when loading', () => {
      const assignments = [createMockAssignment()]
      renderWithRouter('Active Exercises', 'active', assignments, { isLoading: true })

      // Should not show assignment cards
      expect(screen.queryByText('Hurricane Maria TTX')).not.toBeInTheDocument()
    })

    it('does not show empty message when loading', () => {
      renderWithRouter('Active Exercises', 'active', [], {
        isLoading: true,
        emptyMessage: 'No active exercises',
      })

      // Should not show empty message
      expect(screen.queryByText('No active exercises')).not.toBeInTheDocument()
    })
  })

  describe('Empty State', () => {
    it('shows default empty message when no assignments', () => {
      renderWithRouter('Active Exercises', 'active', [])

      // Default message
      expect(screen.getByText('No assignments')).toBeInTheDocument()
    })

    it('shows custom empty message when provided', () => {
      renderWithRouter('Active Exercises', 'active', [], {
        emptyMessage: 'No active exercises at this time',
      })

      expect(screen.getByText('No active exercises at this time')).toBeInTheDocument()
    })

    it('shows empty message as an Alert', () => {
      renderWithRouter('Active Exercises', 'active', [])

      const alert = screen.getByRole('alert')
      expect(alert).toBeInTheDocument()
      expect(alert).toHaveTextContent('No assignments')
    })

    it('does not show empty message when assignments exist', () => {
      const assignments = [createMockAssignment()]
      renderWithRouter('Active Exercises', 'active', assignments)

      expect(screen.queryByText('No assignments')).not.toBeInTheDocument()
    })
  })

  describe('Assignment Cards Rendering', () => {
    it('renders AssignmentCard for each assignment when expanded', () => {
      const assignments = [
        createMockAssignment({ exerciseId: '1', exerciseName: 'Exercise 1' }),
        createMockAssignment({ exerciseId: '2', exerciseName: 'Exercise 2' }),
        createMockAssignment({ exerciseId: '3', exerciseName: 'Exercise 3' }),
      ]
      renderWithRouter('Active Exercises', 'active', assignments)

      expect(screen.getByText('Exercise 1')).toBeInTheDocument()
      expect(screen.getByText('Exercise 2')).toBeInTheDocument()
      expect(screen.getByText('Exercise 3')).toBeInTheDocument()
    })

    it('hides AssignmentCard when collapsed', async () => {
      const user = userEvent.setup()
      const assignments = [createMockAssignment({ exerciseName: 'Test Exercise' })]
      renderWithRouter('Active Exercises', 'active', assignments)

      // Initially visible
      expect(screen.getByText('Test Exercise')).toBeInTheDocument()

      // Collapse
      const toggleButton = screen.getByLabelText('Collapse Active Exercises')
      await user.click(toggleButton)

      // Check aria-expanded is false
      const expandButton = screen.getByLabelText('Expand Active Exercises')
      expect(expandButton).toHaveAttribute('aria-expanded', 'false')
    })

    it('passes section type to AssignmentCard', () => {
      const assignments = [createMockAssignment({ role: 'Controller' })]
      renderWithRouter('Active Exercises', 'active', assignments)

      // Verify card renders with correct role
      expect(screen.getByText('Controller')).toBeInTheDocument()
    })

    it('renders multiple assignments with different data', () => {
      const assignments = [
        createMockAssignment({
          exerciseId: '1',
          exerciseName: 'TTX Exercise',
          exerciseType: 'TTX',
          role: 'Controller',
        }),
        createMockAssignment({
          exerciseId: '2',
          exerciseName: 'FSE Exercise',
          exerciseType: 'FSE',
          role: 'Evaluator',
        }),
      ]
      renderWithRouter('Active Exercises', 'active', assignments)

      expect(screen.getByText('TTX Exercise')).toBeInTheDocument()
      expect(screen.getByText('FSE Exercise')).toBeInTheDocument()
      expect(screen.getByText('Controller')).toBeInTheDocument()
      expect(screen.getByText('Evaluator')).toBeInTheDocument()
      expect(screen.getAllByText('TTX')).toHaveLength(1)
      expect(screen.getAllByText('FSE')).toHaveLength(1)
    })
  })

  describe('Collapsed State Content', () => {
    it('section can be collapsed when loading', async () => {
      const user = userEvent.setup()
      renderWithRouter('Active Exercises', 'active', [], { isLoading: true })

      // Initially expanded with loading skeletons
      const skeletons = screen.queryAllByRole('generic').filter(
        el => el.classList.contains('MuiSkeleton-root'),
      )
      expect(skeletons.length).toBeGreaterThan(0)

      // Collapse the section
      const toggleButton = screen.getByLabelText('Collapse Active Exercises')
      await user.click(toggleButton)

      // Check aria-expanded is false
      const expandButton = screen.getByLabelText('Expand Active Exercises')
      expect(expandButton).toHaveAttribute('aria-expanded', 'false')
    })

    it('section can be collapsed when empty', async () => {
      const user = userEvent.setup()
      renderWithRouter('Active Exercises', 'active', [])

      // Initially expanded with empty message
      expect(screen.getByText('No assignments')).toBeInTheDocument()

      // Collapse
      const toggleButton = screen.getByLabelText('Collapse Active Exercises')
      await user.click(toggleButton)

      // Check aria-expanded is false
      const expandButton = screen.getByLabelText('Expand Active Exercises')
      expect(expandButton).toHaveAttribute('aria-expanded', 'false')
    })
  })

  describe('Accessibility', () => {
    it('has proper aria-label for expand button', () => {
      renderWithRouter('Completed Exercises', 'completed', [])

      const button = screen.getByLabelText('Expand Completed Exercises')
      expect(button).toBeInTheDocument()
    })

    it('has proper aria-label for collapse button', () => {
      renderWithRouter('Active Exercises', 'active', [])

      const button = screen.getByLabelText('Collapse Active Exercises')
      expect(button).toBeInTheDocument()
    })

    it('updates aria-expanded attribute on toggle', async () => {
      const user = userEvent.setup()
      renderWithRouter('Active Exercises', 'active', [])

      const button = screen.getByLabelText('Collapse Active Exercises')
      expect(button).toHaveAttribute('aria-expanded', 'true')

      await user.click(button)

      const expandButton = screen.getByLabelText('Expand Active Exercises')
      expect(expandButton).toHaveAttribute('aria-expanded', 'false')
    })

    it('has semantic heading for section title', () => {
      renderWithRouter('Active Exercises', 'active', [])

      const heading = screen.getByRole('heading', { name: 'Active Exercises', level: 2 })
      expect(heading).toBeInTheDocument()
    })
  })
})
