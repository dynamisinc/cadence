/**
 * AddMemberDialog Tests
 *
 * Tests the Add Member dialog component for organization member management.
 *
 * @module features/organizations/components
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/testUtils'
import userEvent from '@testing-library/user-event'
import { AddMemberDialog } from './AddMemberDialog'
import type { OrgRole } from '../types'

describe('AddMemberDialog', () => {
  const mockOnClose = vi.fn()
  const mockOnAdd = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('Rendering', () => {
    it('renders when open is true', () => {
      render(
        <AddMemberDialog
          open={true}
          onClose={mockOnClose}
          onAdd={mockOnAdd}
        />
      )

      expect(screen.getByText('Add Member to Organization')).toBeInTheDocument()
      expect(screen.getByLabelText(/email address/i)).toBeInTheDocument()
      // Check for role dropdown
      expect(screen.getByRole('combobox')).toBeInTheDocument()
    })

    it('does not render when open is false', () => {
      render(
        <AddMemberDialog
          open={false}
          onClose={mockOnClose}
          onAdd={mockOnAdd}
        />
      )

      expect(screen.queryByText('Add Member to Organization')).not.toBeInTheDocument()
    })

    it('displays all role options', async () => {
      const user = userEvent.setup()
      render(
        <AddMemberDialog
          open={true}
          onClose={mockOnClose}
          onAdd={mockOnAdd}
        />
      )

      // Click the role select (use combobox role)
      const roleSelect = screen.getByRole('combobox')
      await user.click(roleSelect)

      // Verify all roles are available - get all options
      const options = await screen.findAllByRole('option')
      expect(options.length).toBe(3)

      // Check that option text includes role names
      const optionTexts = options.map(opt => opt.textContent)
      expect(optionTexts.some(text => text?.includes('Admin'))).toBe(true)
      expect(optionTexts.some(text => text?.includes('Manager'))).toBe(true)
      expect(optionTexts.some(text => text?.includes('User'))).toBe(true)
    })

    it('shows helper text for email field', () => {
      render(
        <AddMemberDialog
          open={true}
          onClose={mockOnClose}
          onAdd={mockOnAdd}
        />
      )

      expect(screen.getByText(/user must already have an account/i)).toBeInTheDocument()
    })
  })

  describe('Form Validation', () => {
    it('disables button when email is empty', () => {
      render(
        <AddMemberDialog
          open={true}
          onClose={mockOnClose}
          onAdd={mockOnAdd}
        />
      )

      const addButton = screen.getByRole('button', { name: /add member/i })
      // Button should be disabled when email is empty
      expect(addButton).toBeDisabled()
    })

    it('shows error when email is invalid', async () => {
      const user = userEvent.setup()
      render(
        <AddMemberDialog
          open={true}
          onClose={mockOnClose}
          onAdd={mockOnAdd}
        />
      )

      const emailInput = screen.getByLabelText(/email address/i)
      await user.type(emailInput, 'invalidemail')

      const addButton = screen.getByRole('button', { name: /add member/i })
      await user.click(addButton)

      expect(await screen.findByText(/please enter a valid email address/i)).toBeInTheDocument()
      expect(mockOnAdd).not.toHaveBeenCalled()
    })

    it('disables add button when email is empty', () => {
      render(
        <AddMemberDialog
          open={true}
          onClose={mockOnClose}
          onAdd={mockOnAdd}
        />
      )

      const addButton = screen.getByRole('button', { name: /add member/i })
      expect(addButton).toBeDisabled()
    })

    it('enables add button when email has value', async () => {
      const user = userEvent.setup()
      render(
        <AddMemberDialog
          open={true}
          onClose={mockOnClose}
          onAdd={mockOnAdd}
        />
      )

      const emailInput = screen.getByLabelText(/email address/i)
      await user.type(emailInput, 'test@example.com')

      const addButton = screen.getByRole('button', { name: /add member/i })
      expect(addButton).not.toBeDisabled()
    })

    it('allows closing error alert', async () => {
      const user = userEvent.setup()
      render(
        <AddMemberDialog
          open={true}
          onClose={mockOnClose}
          onAdd={mockOnAdd}
        />
      )

      // Trigger validation error with invalid email format
      const emailInput = screen.getByLabelText(/email address/i)
      await user.type(emailInput, 'invalidemail')

      const addButton = screen.getByRole('button', { name: /add member/i })
      await user.click(addButton)

      const errorAlert = await screen.findByText(/please enter a valid email address/i)
      expect(errorAlert).toBeInTheDocument()

      // Close the alert
      const closeButton = screen.getByRole('button', { name: /close/i })
      await user.click(closeButton)

      await waitFor(() => {
        expect(screen.queryByText(/please enter a valid email address/i)).not.toBeInTheDocument()
      })
    })
  })

  describe('Form Submission', () => {
    it('calls onAdd with email and role when form is valid', async () => {
      const user = userEvent.setup()
      mockOnAdd.mockResolvedValue(undefined)

      render(
        <AddMemberDialog
          open={true}
          onClose={mockOnClose}
          onAdd={mockOnAdd}
        />
      )

      const emailInput = screen.getByLabelText(/email address/i)
      await user.type(emailInput, 'newuser@example.com')

      const addButton = screen.getByRole('button', { name: /add member/i })
      await user.click(addButton)

      await waitFor(() => {
        expect(mockOnAdd).toHaveBeenCalledWith('newuser@example.com', 'OrgUser')
      })
    })

    it('calls onAdd with selected role', async () => {
      const user = userEvent.setup()
      mockOnAdd.mockResolvedValue(undefined)

      render(
        <AddMemberDialog
          open={true}
          onClose={mockOnClose}
          onAdd={mockOnAdd}
        />
      )

      const emailInput = screen.getByLabelText(/email address/i)
      await user.type(emailInput, 'admin@example.com')

      // Change role to OrgAdmin
      const roleSelect = screen.getByRole('combobox')
      await user.click(roleSelect)
      const adminOption = await screen.findByRole('option', { name: /admin/i })
      await user.click(adminOption)

      const addButton = screen.getByRole('button', { name: /add member/i })
      await user.click(addButton)

      await waitFor(() => {
        expect(mockOnAdd).toHaveBeenCalledWith('admin@example.com', 'OrgAdmin')
      })
    })

    it('trims whitespace from email', async () => {
      const user = userEvent.setup()
      mockOnAdd.mockResolvedValue(undefined)

      render(
        <AddMemberDialog
          open={true}
          onClose={mockOnClose}
          onAdd={mockOnAdd}
        />
      )

      const emailInput = screen.getByLabelText(/email address/i)
      await user.type(emailInput, '  user@example.com  ')

      const addButton = screen.getByRole('button', { name: /add member/i })
      await user.click(addButton)

      await waitFor(() => {
        expect(mockOnAdd).toHaveBeenCalledWith('user@example.com', 'OrgUser')
      })
    })

    it('closes dialog and resets form on successful submission', async () => {
      const user = userEvent.setup()
      mockOnAdd.mockResolvedValue(undefined)

      render(
        <AddMemberDialog
          open={true}
          onClose={mockOnClose}
          onAdd={mockOnAdd}
        />
      )

      const emailInput = screen.getByLabelText(/email address/i)
      await user.type(emailInput, 'user@example.com')

      const addButton = screen.getByRole('button', { name: /add member/i })
      await user.click(addButton)

      await waitFor(() => {
        expect(mockOnClose).toHaveBeenCalled()
      })
    })

    it('shows error message when onAdd fails', async () => {
      const user = userEvent.setup()
      const error = {
        response: {
          data: {
            message: 'User not found',
          },
        },
      }
      mockOnAdd.mockRejectedValue(error)

      render(
        <AddMemberDialog
          open={true}
          onClose={mockOnClose}
          onAdd={mockOnAdd}
        />
      )

      const emailInput = screen.getByLabelText(/email address/i)
      await user.type(emailInput, 'notfound@example.com')

      const addButton = screen.getByRole('button', { name: /add member/i })
      await user.click(addButton)

      expect(await screen.findByText('User not found')).toBeInTheDocument()
      expect(mockOnClose).not.toHaveBeenCalled()
    })

    it('shows generic error message when onAdd fails without specific message', async () => {
      const user = userEvent.setup()
      mockOnAdd.mockRejectedValue(new Error('Network error'))

      render(
        <AddMemberDialog
          open={true}
          onClose={mockOnClose}
          onAdd={mockOnAdd}
        />
      )

      const emailInput = screen.getByLabelText(/email address/i)
      await user.type(emailInput, 'user@example.com')

      const addButton = screen.getByRole('button', { name: /add member/i })
      await user.click(addButton)

      expect(await screen.findByText(/failed to add member/i)).toBeInTheDocument()
    })
  })

  describe('Loading State', () => {
    it('disables buttons when isLoading is true', () => {
      render(
        <AddMemberDialog
          open={true}
          onClose={mockOnClose}
          onAdd={mockOnAdd}
          isLoading={true}
        />
      )

      expect(screen.getByRole('button', { name: /cancel/i })).toBeDisabled()
      expect(screen.getByRole('button', { name: /adding/i })).toBeDisabled()
    })

    it('shows "Adding..." text when isLoading is true', () => {
      render(
        <AddMemberDialog
          open={true}
          onClose={mockOnClose}
          onAdd={mockOnAdd}
          isLoading={true}
        />
      )

      expect(screen.getByRole('button', { name: /adding/i })).toBeInTheDocument()
    })
  })

  describe('Dialog Close', () => {
    it('calls onClose when cancel button is clicked', async () => {
      const user = userEvent.setup()
      render(
        <AddMemberDialog
          open={true}
          onClose={mockOnClose}
          onAdd={mockOnAdd}
        />
      )

      const cancelButton = screen.getByRole('button', { name: /cancel/i })
      await user.click(cancelButton)

      expect(mockOnClose).toHaveBeenCalled()
    })

    it('resets form when dialog is closed', async () => {
      const user = userEvent.setup()
      const { rerender } = render(
        <AddMemberDialog
          open={true}
          onClose={mockOnClose}
          onAdd={mockOnAdd}
        />
      )

      // Fill in form
      const emailInput = screen.getByLabelText(/email address/i)
      await user.type(emailInput, 'test@example.com')

      // Close dialog
      const cancelButton = screen.getByRole('button', { name: /cancel/i })
      await user.click(cancelButton)

      // Reopen dialog
      rerender(
        <AddMemberDialog
          open={true}
          onClose={mockOnClose}
          onAdd={mockOnAdd}
        />
      )

      // Form should be reset
      const emailInputAfterReopen = screen.getByLabelText(/email address/i)
      expect(emailInputAfterReopen).toHaveValue('')
    })

    it('clears error when dialog is closed', async () => {
      const user = userEvent.setup()
      render(
        <AddMemberDialog
          open={true}
          onClose={mockOnClose}
          onAdd={mockOnAdd}
        />
      )

      // Trigger validation error with invalid email
      const emailInput = screen.getByLabelText(/email address/i)
      await user.type(emailInput, 'invalidemail')

      const addButton = screen.getByRole('button', { name: /add member/i })
      await user.click(addButton)

      expect(await screen.findByText(/please enter a valid email address/i)).toBeInTheDocument()

      // Close dialog
      const cancelButton = screen.getByRole('button', { name: /cancel/i })
      await user.click(cancelButton)

      // Error should not persist
      expect(screen.queryByText(/please enter a valid email address/i)).not.toBeInTheDocument()
    })
  })

  describe('Accessibility', () => {
    it('auto-focuses email input when dialog opens', async () => {
      render(
        <AddMemberDialog
          open={true}
          onClose={mockOnClose}
          onAdd={mockOnAdd}
        />
      )

      const emailInput = screen.getByLabelText(/email address/i)
      // Wait for autofocus to take effect
      await waitFor(() => {
        expect(emailInput).toHaveFocus()
      })
    })

    it('has proper labels for form fields', () => {
      render(
        <AddMemberDialog
          open={true}
          onClose={mockOnClose}
          onAdd={mockOnAdd}
        />
      )

      expect(screen.getByLabelText(/email address/i)).toBeInTheDocument()
      expect(screen.getByRole('combobox')).toBeInTheDocument()
    })
  })
})
