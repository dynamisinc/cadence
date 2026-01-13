import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen, fireEvent, waitFor } from '@testing-library/react'
import { render } from '../../../test/test-utils'
import userEvent from '@testing-library/user-event'
import { PhaseFormDialog } from './PhaseFormDialog'
import type { PhaseDto } from '../types'

const mockPhase: PhaseDto = {
  id: 'phase-1',
  name: 'Initial Response',
  description: 'First phase of the exercise',
  sequence: 1,
  startTime: null,
  endTime: null,
  exerciseId: 'exercise-1',
  injectCount: 0,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
}

describe('PhaseFormDialog', () => {
  const defaultProps = {
    open: true,
    onClose: vi.fn(),
    onSubmit: vi.fn().mockResolvedValue(undefined),
    isSubmitting: false,
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('Create mode', () => {
    it('renders with "Add Phase" title when creating', () => {
      render(<PhaseFormDialog {...defaultProps} />)
      // Title is in the DialogTitle element
      expect(screen.getByRole('heading', { name: 'Add Phase' })).toBeInTheDocument()
    })

    it('starts with empty form fields', () => {
      render(<PhaseFormDialog {...defaultProps} />)
      const nameInput = screen.getByLabelText(/phase name/i)
      expect(nameInput).toHaveValue('')
    })

    it('shows "Add Phase" button text when creating', () => {
      render(<PhaseFormDialog {...defaultProps} />)
      expect(screen.getByRole('button', { name: /add phase/i })).toBeInTheDocument()
    })
  })

  describe('Edit mode', () => {
    it('renders with "Edit Phase" title when editing', () => {
      render(<PhaseFormDialog {...defaultProps} phase={mockPhase} />)
      expect(screen.getByText('Edit Phase')).toBeInTheDocument()
    })

    it('pre-populates form with phase data', () => {
      render(<PhaseFormDialog {...defaultProps} phase={mockPhase} />)
      const nameInput = screen.getByLabelText(/phase name/i)
      expect(nameInput).toHaveValue('Initial Response')
    })

    it('shows "Save" button text when editing', () => {
      render(<PhaseFormDialog {...defaultProps} phase={mockPhase} />)
      expect(screen.getByRole('button', { name: /^save$/i })).toBeInTheDocument()
    })
  })

  describe('Form validation', () => {
    it('shows error when name is empty on blur', async () => {
      render(<PhaseFormDialog {...defaultProps} />)
      const nameInput = screen.getByLabelText(/phase name/i)

      fireEvent.blur(nameInput)

      await waitFor(() => {
        expect(screen.getByText('Name is required')).toBeInTheDocument()
      })
    })

    it('shows error when name is too short', async () => {
      render(<PhaseFormDialog {...defaultProps} />)
      const nameInput = screen.getByLabelText(/phase name/i)

      await userEvent.type(nameInput, 'AB')
      fireEvent.blur(nameInput)

      await waitFor(() => {
        expect(screen.getByText(/must be at least 3 characters/i)).toBeInTheDocument()
      })
    })

    it('shows error when name is too long', async () => {
      render(<PhaseFormDialog {...defaultProps} />)
      const nameInput = screen.getByLabelText(/phase name/i)

      // Use fireEvent.change for faster test - 101 characters
      fireEvent.change(nameInput, { target: { value: 'A'.repeat(101) } })
      fireEvent.blur(nameInput)

      await waitFor(() => {
        expect(screen.getByText(/must be 100 characters or less/i)).toBeInTheDocument()
      })
    })

    it('shows error when description is too long', async () => {
      render(<PhaseFormDialog {...defaultProps} />)
      const descInput = screen.getByLabelText(/description/i)

      // Use fireEvent.change for faster test - 501 characters
      fireEvent.change(descInput, { target: { value: 'A'.repeat(501) } })
      fireEvent.blur(descInput)

      await waitFor(() => {
        expect(screen.getByText(/must be 500 characters or less/i)).toBeInTheDocument()
      })
    })

    it('does not submit when form is invalid', async () => {
      const onSubmit = vi.fn()
      render(<PhaseFormDialog {...defaultProps} onSubmit={onSubmit} />)

      const submitButton = screen.getByRole('button', { name: /add phase/i })
      fireEvent.click(submitButton)

      await waitFor(() => {
        expect(onSubmit).not.toHaveBeenCalled()
      })
    })
  })

  describe('Form submission', () => {
    // Note: Form submission tests are covered by integration testing
    // These unit tests verify the form UI renders correctly

    it('submit button is enabled when form has valid data', async () => {
      render(<PhaseFormDialog {...defaultProps} />)

      const nameInput = screen.getByLabelText(/phase name/i)
      await userEvent.type(nameInput, 'Test Phase')

      const submitButton = screen.getByRole('button', { name: /add phase/i })
      expect(submitButton).not.toBeDisabled()
    })
  })

  describe('Dialog behavior', () => {
    it('calls onClose when Cancel is clicked', () => {
      const onClose = vi.fn()
      render(<PhaseFormDialog {...defaultProps} onClose={onClose} />)

      const cancelButton = screen.getByRole('button', { name: /cancel/i })
      fireEvent.click(cancelButton)

      expect(onClose).toHaveBeenCalled()
    })

    it('disables buttons when isSubmitting is true', () => {
      render(<PhaseFormDialog {...defaultProps} isSubmitting={true} />)

      expect(screen.getByRole('button', { name: /cancel/i })).toBeDisabled()
      expect(screen.getByRole('button', { name: /creating/i })).toBeDisabled()
    })

    it('shows "Saving..." text when editing and submitting', () => {
      render(<PhaseFormDialog {...defaultProps} phase={mockPhase} isSubmitting={true} />)

      expect(screen.getByRole('button', { name: /saving/i })).toBeInTheDocument()
    })

    it('resets form when dialog opens', async () => {
      const { rerender } = render(<PhaseFormDialog {...defaultProps} open={false} />)

      // Open dialog
      rerender(<PhaseFormDialog {...defaultProps} open={true} />)

      const nameInput = screen.getByLabelText(/phase name/i)
      expect(nameInput).toHaveValue('')
    })

    it('loads phase data when phase prop changes', () => {
      const { rerender } = render(<PhaseFormDialog {...defaultProps} phase={null} />)

      rerender(<PhaseFormDialog {...defaultProps} phase={mockPhase} />)

      const nameInput = screen.getByLabelText(/phase name/i)
      expect(nameInput).toHaveValue('Initial Response')
    })
  })
})
