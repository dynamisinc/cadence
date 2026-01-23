/**
 * ResetPasswordPage Component Tests
 *
 * @module features/auth
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen, fireEvent, waitFor } from '@testing-library/react'
import { render } from '../../../test/testUtils'
import { ResetPasswordPage } from './ResetPasswordPage'
import { authService } from '../services/authService'
import { useSearchParams, useNavigate } from 'react-router-dom'

// Mock the router hooks since we can't use nested routers
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useSearchParams: vi.fn(),
    useNavigate: vi.fn(),
  }
})

vi.mock('../services/authService', () => ({
  authService: {
    completePasswordReset: vi.fn(),
  },
}))

const renderPage = (token = 'valid-token') => {
  vi.mocked(useSearchParams).mockReturnValue([
    new URLSearchParams(token ? `token=${token}` : ''),
    vi.fn() as any,
  ])
  vi.mocked(useNavigate).mockReturnValue(vi.fn())
  return render(<ResetPasswordPage />)
}

describe('ResetPasswordPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders the new password form with valid token', () => {
    renderPage()

    expect(screen.getByRole('heading', { name: /set new password/i })).toBeInTheDocument()
    expect(screen.getByLabelText(/new password/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/confirm password/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /set new password/i })).toBeInTheDocument()
  })

  it('shows error when no token provided', () => {
    renderPage('')

    expect(screen.getByRole('heading', { name: /reset link invalid/i })).toBeInTheDocument()
    expect(screen.getByText(/no reset token provided/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /request new reset link/i })).toBeInTheDocument()
  })

  it('toggles password visibility', async () => {
    renderPage()

    const newPasswordInput = screen.getByLabelText(/new password/i) as HTMLInputElement
    const toggleButton = screen.getAllByLabelText(/show password/i)[0]

    expect(newPasswordInput.type).toBe('password')

    fireEvent.click(toggleButton)

    await waitFor(() => {
      expect(newPasswordInput.type).toBe('text')
    })
  })

  it('shows password requirements when typing', async () => {
    renderPage()

    const newPasswordInput = screen.getByLabelText(/new password/i)

    fireEvent.change(newPasswordInput, { target: { value: 'weak' } })

    await waitFor(() => {
      expect(screen.getByText(/at least 8 characters/i)).toBeInTheDocument()
    })
  })

  it('validates passwords match', async () => {
    renderPage()

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
    renderPage()

    const submitButton = screen.getByRole('button', { name: /set new password/i })
    expect(submitButton).toBeDisabled()

    const newPasswordInput = screen.getByLabelText(/new password/i)
    fireEvent.change(newPasswordInput, { target: { value: 'weak' } })

    expect(submitButton).toBeDisabled()
  })

  it('shows success state after password reset', async () => {
    vi.mocked(authService.completePasswordReset).mockResolvedValueOnce(undefined)

    renderPage()

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

    renderPage()

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

    renderPage()

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
