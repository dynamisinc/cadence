/**
 * ForgotPasswordPage Component Tests
 *
 * @module features/auth
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '../../../test/testUtils'
import { ForgotPasswordPage } from './ForgotPasswordPage'
import { authService } from '../services/authService'

vi.mock('../services/authService', () => ({
  authService: {
    requestPasswordReset: vi.fn(),
  },
}))

describe('ForgotPasswordPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders the reset password form', () => {
    render(<ForgotPasswordPage />)

    expect(screen.getByRole('heading', { name: 'Reset Password' })).toBeInTheDocument()
    expect(screen.getByLabelText(/email address/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /send reset link/i })).toBeInTheDocument()
  })

  it('shows back to sign in link', () => {
    render(<ForgotPasswordPage />)

    expect(screen.getByText(/back to sign in/i)).toBeInTheDocument()
  })

  it('validates email is required', async () => {
    render(<ForgotPasswordPage />)

    const emailInput = screen.getByLabelText(/email address/i)
    fireEvent.blur(emailInput)

    await waitFor(() => {
      expect(screen.getByText(/email is required/i)).toBeInTheDocument()
    })
  })

  it('validates email format', async () => {
    render(<ForgotPasswordPage />)

    const emailInput = screen.getByLabelText(/email address/i)
    fireEvent.change(emailInput, { target: { value: 'invalid-email' } })
    fireEvent.blur(emailInput)

    await waitFor(() => {
      expect(screen.getByText(/please enter a valid email address/i)).toBeInTheDocument()
    })
  })

  it('shows success state after submission', async () => {
    vi.mocked(authService.requestPasswordReset).mockResolvedValueOnce(undefined)

    render(<ForgotPasswordPage />)

    const emailInput = screen.getByLabelText(/email address/i)
    const submitButton = screen.getByRole('button', { name: /send reset link/i })

    fireEvent.change(emailInput, { target: { value: 'test@example.com' } })
    fireEvent.click(submitButton)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /check your email/i })).toBeInTheDocument()
    })

    expect(screen.getByText(/test@example.com/i)).toBeInTheDocument()
    expect(screen.getByText(/link will expire in 1 hour/i)).toBeInTheDocument()
  })

  it('shows success state even on error to prevent email enumeration', async () => {
    vi.mocked(authService.requestPasswordReset).mockRejectedValueOnce(new Error('Network error'))

    render(<ForgotPasswordPage />)

    const emailInput = screen.getByLabelText(/email address/i)
    const submitButton = screen.getByRole('button', { name: /send reset link/i })

    fireEvent.change(emailInput, { target: { value: 'test@example.com' } })
    fireEvent.click(submitButton)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /check your email/i })).toBeInTheDocument()
    })
  })

  it('allows requesting another link from success state', async () => {
    vi.mocked(authService.requestPasswordReset).mockResolvedValueOnce(undefined)

    render(<ForgotPasswordPage />)

    const emailInput = screen.getByLabelText(/email address/i)
    const submitButton = screen.getByRole('button', { name: /send reset link/i })

    fireEvent.change(emailInput, { target: { value: 'test@example.com' } })
    fireEvent.click(submitButton)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /check your email/i })).toBeInTheDocument()
    })

    const requestAnotherLink = screen.getByText(/request another link/i)
    fireEvent.click(requestAnotherLink)

    expect(screen.getByRole('heading', { name: 'Reset Password' })).toBeInTheDocument()
  })

  it('disables submit button during submission', async () => {
    vi.mocked(authService.requestPasswordReset).mockImplementation(
      () => new Promise(resolve => setTimeout(resolve, 100)),
    )

    render(<ForgotPasswordPage />)

    const emailInput = screen.getByLabelText(/email address/i)
    const submitButton = screen.getByRole('button', { name: /send reset link/i })

    fireEvent.change(emailInput, { target: { value: 'test@example.com' } })
    fireEvent.click(submitButton)

    await waitFor(() => {
      expect(submitButton).toBeDisabled()
    })
  })
})
