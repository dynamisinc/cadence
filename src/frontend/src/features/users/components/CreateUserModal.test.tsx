/**
 * CreateUserModal Component Tests
 *
 * @module features/users
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { ThemeProvider } from '@mui/material/styles'
import { CreateUserModal } from './CreateUserModal'
import { userService } from '../services/userService'
import { cobraTheme } from '../../../theme/cobraTheme'
import type { UserDto } from '../types'

vi.mock('../services/userService', () => ({
  userService: {
    createUser: vi.fn(),
  },
}))

const mockOnClose = vi.fn()
const mockOnUserCreated = vi.fn()

const renderModal = (open = true) => {
  return render(
    <ThemeProvider theme={cobraTheme}>
      <CreateUserModal
        open={open}
        onClose={mockOnClose}
        onUserCreated={mockOnUserCreated}
      />
    </ThemeProvider>,
  )
}

describe('CreateUserModal', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders the modal when open', () => {
    renderModal()

    expect(screen.getByRole('heading', { name: /create new user/i })).toBeInTheDocument()
    expect(screen.getByLabelText(/display name/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/email address/i)).toBeInTheDocument()
    expect(screen.getByLabelText('Password *')).toBeInTheDocument()
  })

  it('does not render when closed', () => {
    renderModal(false)

    expect(screen.queryByRole('heading', { name: /create new user/i })).not.toBeInTheDocument()
  })

  it('validates display name is required', () => {
    renderModal()

    const submitButton = screen.getByRole('button', { name: /create user/i })
    // Button should be disabled when displayName is empty
    expect(submitButton).toBeDisabled()
  })

  it('validates email is required', () => {
    renderModal()

    const displayNameInput = screen.getByLabelText(/display name/i)
    fireEvent.change(displayNameInput, { target: { value: 'John Doe' } })

    const submitButton = screen.getByRole('button', { name: /create user/i })
    // Button should be disabled when email is empty
    expect(submitButton).toBeDisabled()
  })

  it('validates email format', () => {
    renderModal()

    const displayNameInput = screen.getByLabelText(/display name/i)
    const emailInput = screen.getByLabelText(/email address/i)

    fireEvent.change(displayNameInput, { target: { value: 'John Doe' } })
    fireEvent.change(emailInput, { target: { value: 'invalid-email' } })

    const submitButton = screen.getByRole('button', { name: /create user/i })
    // Button should be disabled when email format is invalid
    expect(submitButton).toBeDisabled()
  })

  it('validates password is required', () => {
    renderModal()

    const displayNameInput = screen.getByLabelText(/display name/i)
    const emailInput = screen.getByLabelText(/email address/i)

    fireEvent.change(displayNameInput, { target: { value: 'John Doe' } })
    fireEvent.change(emailInput, { target: { value: 'john@example.com' } })

    const submitButton = screen.getByRole('button', { name: /create user/i })
    // Button should be disabled when password is empty
    expect(submitButton).toBeDisabled()
  })

  it('shows password requirements when typing', async () => {
    renderModal()

    const passwordInput = screen.getByLabelText('Password *')
    fireEvent.change(passwordInput, { target: { value: 'weak' } })

    await waitFor(() => {
      expect(screen.getByText(/at least 8 characters/i)).toBeInTheDocument()
    })
  })

  it('toggles password visibility', async () => {
    renderModal()

    const passwordInput = screen.getByLabelText('Password *') as HTMLInputElement
    const toggleButton = screen.getByLabelText(/show password/i)

    expect(passwordInput.type).toBe('password')

    fireEvent.click(toggleButton)

    await waitFor(() => {
      expect(passwordInput.type).toBe('text')
    })
  })

  it('calls onClose when cancel is clicked', () => {
    renderModal()

    const cancelButton = screen.getByRole('button', { name: /cancel/i })
    fireEvent.click(cancelButton)

    expect(mockOnClose).toHaveBeenCalled()
  })

  it('creates user and shows success state', async () => {
    const mockUser: UserDto = {
      id: '123',
      email: 'john@example.com',
      displayName: 'John Doe',
      systemRole: 'User',
      status: 'Active',
      lastLoginAt: null,
      createdAt: '2025-01-23T10:00:00Z',
    }
    vi.mocked(userService.createUser).mockResolvedValueOnce(mockUser)

    renderModal()

    const displayNameInput = screen.getByLabelText(/display name/i)
    const emailInput = screen.getByLabelText(/email address/i)
    const passwordInput = screen.getByLabelText('Password *')

    fireEvent.change(displayNameInput, { target: { value: 'John Doe' } })
    fireEvent.change(emailInput, { target: { value: 'john@example.com' } })
    fireEvent.change(passwordInput, { target: { value: 'StrongPassword123!' } })

    const submitButton = screen.getByRole('button', { name: /create user/i })
    fireEvent.click(submitButton)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /user created successfully/i })).toBeInTheDocument()
    })

    expect(screen.getByText(/john doe/i)).toBeInTheDocument()
    expect(screen.getByText(/john@example.com/i)).toBeInTheDocument()
  })

  it('shows error for duplicate email', async () => {
    vi.mocked(userService.createUser).mockRejectedValueOnce({
      response: { status: 409 },
    })

    renderModal()

    const displayNameInput = screen.getByLabelText(/display name/i)
    const emailInput = screen.getByLabelText(/email address/i)
    const passwordInput = screen.getByLabelText('Password *')

    fireEvent.change(displayNameInput, { target: { value: 'John Doe' } })
    fireEvent.change(emailInput, { target: { value: 'john@example.com' } })
    fireEvent.change(passwordInput, { target: { value: 'StrongPassword123!' } })

    const submitButton = screen.getByRole('button', { name: /create user/i })
    fireEvent.click(submitButton)

    await waitFor(() => {
      expect(screen.getByText(/user with this email already exists/i)).toBeInTheDocument()
    })
  })

  it('calls onUserCreated when done is clicked after success', async () => {
    const mockUser: UserDto = {
      id: '123',
      email: 'john@example.com',
      displayName: 'John Doe',
      systemRole: 'User',
      status: 'Active',
      lastLoginAt: null,
      createdAt: '2025-01-23T10:00:00Z',
    }
    vi.mocked(userService.createUser).mockResolvedValueOnce(mockUser)

    renderModal()

    const displayNameInput = screen.getByLabelText(/display name/i)
    const emailInput = screen.getByLabelText(/email address/i)
    const passwordInput = screen.getByLabelText('Password *')

    fireEvent.change(displayNameInput, { target: { value: 'John Doe' } })
    fireEvent.change(emailInput, { target: { value: 'john@example.com' } })
    fireEvent.change(passwordInput, { target: { value: 'StrongPassword123!' } })

    const submitButton = screen.getByRole('button', { name: /create user/i })
    fireEvent.click(submitButton)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /user created successfully/i })).toBeInTheDocument()
    })

    const doneButton = screen.getByRole('button', { name: /done/i })
    fireEvent.click(doneButton)

    expect(mockOnUserCreated).toHaveBeenCalledWith(mockUser)
    expect(mockOnClose).toHaveBeenCalled()
  })

  it('shows info about User role for new users', () => {
    renderModal()

    expect(screen.getByText(/will be created with/i)).toBeInTheDocument()
    expect(screen.getByText(/User.*system role/i)).toBeInTheDocument()
  })

  it('resets form when modal reopens', async () => {
    const { rerender } = renderModal()

    const displayNameInput = screen.getByLabelText(/display name/i)
    fireEvent.change(displayNameInput, { target: { value: 'John Doe' } })

    // Close modal
    rerender(
      <ThemeProvider theme={cobraTheme}>
        <CreateUserModal
          open={false}
          onClose={mockOnClose}
          onUserCreated={mockOnUserCreated}
        />
      </ThemeProvider>,
    )

    // Reopen modal
    rerender(
      <ThemeProvider theme={cobraTheme}>
        <CreateUserModal
          open={true}
          onClose={mockOnClose}
          onUserCreated={mockOnUserCreated}
        />
      </ThemeProvider>,
    )

    await waitFor(() => {
      const newDisplayNameInput = screen.getByLabelText(/display name/i) as HTMLInputElement
      expect(newDisplayNameInput.value).toBe('')
    })
  })
})
