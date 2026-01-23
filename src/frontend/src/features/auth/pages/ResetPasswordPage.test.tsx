/**
 * ResetPasswordPage Component Tests
 *
 * @module features/auth
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '../../../test/testUtils'
import { ResetPasswordPage } from './ResetPasswordPage'
import { authService } from '../services/authService'
import { MemoryRouter, Routes, Route } from 'react-router-dom'

vi.mock('../services/authService', () => ({
  authService: {
    completePasswordReset: vi.fn(),
  },
}))

const renderWithRouter = (initialEntries: string[] = ['/reset-password?token=valid-token']) => {
  return render(
    <MemoryRouter initialEntries={initialEntries}>
      <Routes>
        <Route path="/reset-password" element={<ResetPasswordPage />} />
        <Route path="/login" element={<div>Login Page</div>} />
        <Route path="/forgot-password" element={<div>Forgot Password Page</div>} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('ResetPasswordPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders the new password form with valid token', () => {
    renderWithRouter()

    expect(screen.getByRole('heading', { name: /set new password/i })).toBeInTheDocument()
    expect(screen.getByLabelText(/new password/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/confirm password/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /set new password/i })).toBeInTheDocument()
  })

  it('shows error when no token provided', () => {
    renderWithRouter(['/reset-password'])

    expect(screen.getByRole('heading', { name: /reset link invalid/i })).toBeInTheDocument()
    expect(screen.getByText(/no reset token provided/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /request new reset link/i })).toBeInTheDocument()
  })

  it('toggles password visibility', async () => {
    renderWithRouter()

    const newPasswordInput = screen.getByLabelText(/new password/i) as HTMLInputElement
    const toggleButton = screen.getAllByLabelText(/show password/i)[0]

    expect(newPasswordInput.type).toBe('password')

    fireEvent.click(toggleButton)

    await waitFor(() => {
      expect(newPasswordInput.type).toBe('text')
    })
  })

  it('shows password requirements when typing', async () => {
    renderWithRouter()

    const newPasswordInput = screen.getByLabelText(/new password/i)

    fireEvent.change(newPasswordInput, { target: { value: 'weak' } })

    await waitFor(() => {
      expect(screen.getByText(/at least 8 characters/i)).toBeInTheDocument()
    })
  })

  it('validates passwords match', async () => {
    renderWithRouter()

    const newPasswordInput = screen.getByLabelText(/new password/i)
    const confirmPasswordInput = screen.getByLabelText(/confirm password/i)

    fireEvent.change(newPasswordInput, { target: { value: 'StrongPassword123!' } })
    fireEvent.change(confirmPasswordInput, { target: { value: 'DifferentPassword123!' } })
    fireEvent.blur(confirmPasswordInput)

    await waitFor(() => {
      expect(screen.getByText(/passwords do not match/i)).toBeInTheDocument()
    })
  })

  it('disables submit when password is invalid', () => {
    renderWithRouter()

    const submitButton = screen.getByRole('button', { name: /set new password/i })
    expect(submitButton).toBeDisabled()

    const newPasswordInput = screen.getByLabelText(/new password/i)
    fireEvent.change(newPasswordInput, { target: { value: 'weak' } })

    expect(submitButton).toBeDisabled()
  })

  it('shows success state after password reset', async () => {
    vi.mocked(authService.completePasswordReset).mockResolvedValueOnce(undefined)

    renderWithRouter()

    const newPasswordInput = screen.getByLabelText(/new password/i)
    const confirmPasswordInput = screen.getByLabelText(/confirm password/i)
    const submitButton = screen.getByRole('button', { name: /set new password/i })

    fireEvent.change(newPasswordInput, { target: { value: 'StrongPassword123!' } })
    fireEvent.change(confirmPasswordInput, { target: { value: 'StrongPassword123!' } })
    fireEvent.click(submitButton)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /password reset successful/i })).toBeInTheDocument()
    })

    expect(screen.getByText(/redirecting to login/i)).toBeInTheDocument()
  })

  it('shows error for invalid or expired token', async () => {
    vi.mocked(authService.completePasswordReset).mockRejectedValueOnce({
      response: { data: { code: 'invalid_token' } },
    })

    renderWithRouter()

    const newPasswordInput = screen.getByLabelText(/new password/i)
    const confirmPasswordInput = screen.getByLabelText(/confirm password/i)
    const submitButton = screen.getByRole('button', { name: /set new password/i })

    fireEvent.change(newPasswordInput, { target: { value: 'StrongPassword123!' } })
    fireEvent.change(confirmPasswordInput, { target: { value: 'StrongPassword123!' } })
    fireEvent.click(submitButton)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /reset link invalid/i })).toBeInTheDocument()
    })

    expect(screen.getByText(/invalid or has expired/i)).toBeInTheDocument()
  })

  it('shows generic error for server errors', async () => {
    vi.mocked(authService.completePasswordReset).mockRejectedValueOnce({
      response: { data: { message: 'Server error occurred' } },
    })

    renderWithRouter()

    const newPasswordInput = screen.getByLabelText(/new password/i)
    const confirmPasswordInput = screen.getByLabelText(/confirm password/i)
    const submitButton = screen.getByRole('button', { name: /set new password/i })

    fireEvent.change(newPasswordInput, { target: { value: 'StrongPassword123!' } })
    fireEvent.change(confirmPasswordInput, { target: { value: 'StrongPassword123!' } })
    fireEvent.click(submitButton)

    await waitFor(() => {
      expect(screen.getByText(/server error occurred/i)).toBeInTheDocument()
    })
  })
})
