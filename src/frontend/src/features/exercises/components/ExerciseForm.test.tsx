/**
 * ExerciseForm Component Tests
 *
 * Tests for the exercise creation and editing form including director selection.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import { render } from '../../../test/testUtils'
import userEvent from '@testing-library/user-event'
import { ExerciseForm } from './ExerciseForm'
import { ExerciseType, DeliveryMode, TimelineMode, ExerciseStatus } from '../../../types'
import type { ExerciseDto } from '../types'
import { userService } from '../../users/services/userService'
import type { UserDto } from '../../users/types'

// Mock the user service
vi.mock('../../users/services/userService', () => ({
  userService: {
    getUsers: vi.fn(),
  },
}))

const mockDirectorUsers: UserDto[] = [
  {
    id: 'admin-1',
    email: 'admin@test.com',
    displayName: 'Admin Director',
    systemRole: 'Admin',
    status: 'Active',
    lastLoginAt: '2026-01-20T10:00:00Z',
    createdAt: '2026-01-01T00:00:00Z',
  },
  {
    id: 'manager-1',
    email: 'manager@test.com',
    displayName: 'Manager Director',
    systemRole: 'Manager',
    status: 'Active',
    lastLoginAt: '2026-01-20T09:00:00Z',
    createdAt: '2026-01-02T00:00:00Z',
  },
]

const mockExercise: ExerciseDto = {
  id: 'ex-1',
  name: 'Test Exercise',
  description: 'Test Description',
  exerciseType: ExerciseType.TTX,
  status: ExerciseStatus.Draft,
  isPracticeMode: false,
  scheduledDate: '2026-06-01',
  startTime: '09:00:00',
  endTime: '17:00:00',
  timeZoneId: 'America/New_York',
  location: 'Test Location',
  organizationId: 'org-1',
  activeMselId: null,
  deliveryMode: DeliveryMode.ClockDriven,
  timelineMode: TimelineMode.RealTime,
  timeScale: null,
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-01T00:00:00Z',
  createdBy: 'user-1',
  activatedAt: null,
  activatedBy: null,
  completedAt: null,
  completedBy: null,
  archivedAt: null,
  archivedBy: null,
  hasBeenPublished: false,
  previousStatus: null,
}

describe('ExerciseForm - Director Selection', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(userService.getUsers).mockResolvedValue({
      users: mockDirectorUsers,
      pagination: { page: 1, pageSize: 100, totalCount: 2, totalPages: 1 },
    })
  })

  describe('Create Mode', () => {
    it('renders director field with optional label', async () => {
      render(
        <ExerciseForm
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      await waitFor(() => {
        expect(screen.getByLabelText(/exercise director \(optional\)/i)).toBeInTheDocument()
      }, { timeout: 15000 })
    }, 20000)

    it('shows helper text for create mode', () => {
      render(
        <ExerciseForm
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      expect(screen.getByText(/defaults to you if not specified/i)).toBeInTheDocument()
    })

    it('allows selecting a director from Admin/Manager users', async () => {
      const user = userEvent.setup()

      render(
        <ExerciseForm
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      const directorInput = screen.getByLabelText(/exercise director \(optional\)/i)
      await user.click(directorInput)

      await waitFor(() => {
        expect(screen.getByText('Admin Director')).toBeInTheDocument()
        expect(screen.getByText('Manager Director')).toBeInTheDocument()
      })
    })

    it('includes directorId in form submission when director is selected', { timeout: 30000 }, async () => {
      const user = userEvent.setup()
      const handleSubmit = vi.fn()

      render(
        <ExerciseForm
          onSubmit={handleSubmit}
          onCancel={vi.fn()}
        />,
      )

      // Fill required fields
      await user.type(screen.getByLabelText(/^name/i), 'New Exercise')

      const typeSelect = screen.getByLabelText(/exercise type/i)
      await user.click(typeSelect)
      await user.click(screen.getByText(/TTX - Tabletop Exercise/i))

      const dateInput = screen.getByLabelText(/scheduled date/i)
      await user.type(dateInput, '2026-06-01')

      // Select director
      const directorInput = screen.getByLabelText(/exercise director \(optional\)/i)
      await user.click(directorInput)

      await waitFor(() => {
        expect(screen.getByText('Admin Director')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Admin Director'))

      // Submit form
      const submitButton = screen.getByRole('button', { name: /create exercise/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(handleSubmit).toHaveBeenCalled()
      })

      const submittedData = handleSubmit.mock.calls[0][0]
      expect(submittedData.directorId).toBe('admin-1')
    })

    it('submits without directorId when no director is selected', { timeout: 30000 }, async () => {
      const user = userEvent.setup()
      const handleSubmit = vi.fn()

      render(
        <ExerciseForm
          onSubmit={handleSubmit}
          onCancel={vi.fn()}
        />,
      )

      // Fill required fields only
      await user.type(screen.getByLabelText(/^name/i), 'New Exercise')

      const typeSelect = screen.getByLabelText(/exercise type/i)
      await user.click(typeSelect)
      await user.click(screen.getByText(/TTX - Tabletop Exercise/i))

      const dateInput = screen.getByLabelText(/scheduled date/i)
      await user.type(dateInput, '2026-06-01')

      // Submit form without selecting director
      const submitButton = screen.getByRole('button', { name: /create exercise/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(handleSubmit).toHaveBeenCalled()
      })

      const submittedData = handleSubmit.mock.calls[0][0]
      expect(submittedData.directorId).toBe('')
    })

    it('allows clearing director selection', async () => {
      const user = userEvent.setup()

      render(
        <ExerciseForm
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      // Select director
      const directorInput = screen.getByLabelText(/exercise director \(optional\)/i)
      await user.click(directorInput)

      await waitFor(() => {
        expect(screen.getByText('Admin Director')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Admin Director'))

      // Verify selection
      await waitFor(() => {
        const input = screen.getByLabelText(/exercise director \(optional\)/i) as HTMLInputElement
        expect(input.value).toBe('Admin Director')
      })

      // Clear selection
      const clearButton = screen.getByTitle('Clear')
      await user.click(clearButton)

      await waitFor(() => {
        const input = screen.getByLabelText(/exercise director \(optional\)/i) as HTMLInputElement
        expect(input.value).toBe('')
      })
    })
  })

  describe('Edit Mode', () => {
    it('shows different helper text in edit mode', () => {
      render(
        <ExerciseForm
          exercise={mockExercise}
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      expect(screen.getByText(/change the assigned exercise director/i)).toBeInTheDocument()
    })

    it('includes directorId in update request when changed', async () => {
      const user = userEvent.setup()
      const handleSubmit = vi.fn()

      render(
        <ExerciseForm
          exercise={mockExercise}
          onSubmit={handleSubmit}
          onCancel={vi.fn()}
        />,
      )

      // Select a director
      const directorInput = screen.getByLabelText(/exercise director \(optional\)/i)
      await user.click(directorInput)

      await waitFor(() => {
        expect(screen.getByText('Manager Director')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Manager Director'))

      // Submit form
      const submitButton = screen.getByRole('button', { name: /save changes/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(handleSubmit).toHaveBeenCalled()
      })

      const submittedData = handleSubmit.mock.calls[0][0]
      expect(submittedData.directorId).toBe('manager-1')
    })

    it('can be disabled when field is in disabledFields', () => {
      render(
        <ExerciseForm
          exercise={mockExercise}
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
          disabledFields={['directorId']}
        />,
      )

      const directorInput = screen.getByLabelText(/exercise director \(optional\)/i)
      expect(directorInput).toBeDisabled()
    })
  })

  describe('UserAutocomplete Integration', () => {
    it('filters to Admin and Manager roles only', async () => {
      const user = userEvent.setup()

      render(
        <ExerciseForm
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      const directorInput = screen.getByLabelText(/exercise director \(optional\)/i)
      await user.click(directorInput)

      await waitFor(() => {
        expect(userService.getUsers).toHaveBeenCalledWith(
          expect.objectContaining({
            role: 'Admin,Manager',
          }),
        )
      })
    })

    it('displays user email and role in autocomplete options', async () => {
      const user = userEvent.setup()

      render(
        <ExerciseForm
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      const directorInput = screen.getByLabelText(/exercise director \(optional\)/i)
      await user.click(directorInput)

      await waitFor(() => {
        expect(screen.getByText(/admin@test\.com/i)).toBeInTheDocument()
        // Check for role in caption text that includes email
        const captionElement = screen.getByText((content, element) => {
          return element?.tagName.toLowerCase() === 'span' &&
                 element?.className.includes('MuiTypography-caption') &&
                 content.includes('admin@test.com') &&
                 content.includes('Admin')
        })
        expect(captionElement).toBeInTheDocument()
      })
    })

    it('shows loading state while fetching users', async () => {
      vi.mocked(userService.getUsers).mockImplementation(
        () => new Promise(() => {}), // Never resolves
      )

      render(
        <ExerciseForm
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      const directorInput = screen.getByLabelText(/exercise director \(optional\)/i)
      await userEvent.click(directorInput)

      // Should show loading indicator
      await waitFor(() => {
        expect(screen.getByRole('progressbar')).toBeInTheDocument()
      })
    })
  })
})
