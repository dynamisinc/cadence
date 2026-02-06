/**
 * RegisterPage Component Tests
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '../../../test/testUtils'
import { RegisterPage } from './RegisterPage'
import { authService } from '../services/authService'

// Re-mock authService for specific test behaviors
vi.mock('../services/authService', () => ({
  authService: {
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
    refreshToken: vi.fn(),
    getAvailableMethods: vi.fn(),
    requestPasswordReset: vi.fn(),
    completePasswordReset: vi.fn(),
  },
}))

// Mock navigator.onLine
Object.defineProperty(window.navigator, 'onLine', {
  writable: true,
  value: true,
})

describe('RegisterPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(authService.refreshToken).mockResolvedValue({
      isSuccess: false,
      expiresIn: 0,
      tokenType: 'Bearer',
      error: { code: 'invalid_token', message: 'No token' },
    })
  })

  it('renders registration form with all fields', async () => {
    render(<RegisterPage />)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /create account/i })).toBeInTheDocument()
    })

    expect(screen.getByLabelText(/display name/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/email address/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/^password/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/confirm password/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /create account/i })).toBeInTheDocument()
  })

  it('shows password requirements when typing password', async () => {
    render(<RegisterPage />)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /create account/i })).toBeInTheDocument()
    })

    // Password requirements only show when user starts typing
    const passwordInputs = screen.getAllByLabelText(/password/i) as HTMLInputElement[]
    const passwordInput = passwordInputs[0]

    // Type a password and blur to trigger requirements display
    fireEvent.change(passwordInput, { target: { value: 'test' } })
    fireEvent.blur(passwordInput)

    await waitFor(() => {
      // Check for requirement text from PasswordRequirements component
      expect(screen.getByText(/at least 8 characters/i)).toBeInTheDocument()
      expect(screen.getByText(/at least 1 uppercase letter/i)).toBeInTheDocument()
      expect(screen.getByText(/at least 1 number/i)).toBeInTheDocument()
    })
  })

  it('shows password when visibility toggle clicked', async () => {
    render(<RegisterPage />)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /create account/i })).toBeInTheDocument()
    })

    // Find the password input (not confirm password)
    const passwordInputs = screen.getAllByLabelText(/password/i) as HTMLInputElement[]
    const passwordInput = passwordInputs[0] // First one is the password field
    const toggleButtons = screen.getAllByLabelText(/show password/i)
    const toggleButton = toggleButtons[0]

    // Initially password should be hidden
    expect(passwordInput.type).toBe('password')

    // Click toggle to show password
    fireEvent.click(toggleButton)

    await waitFor(() => {
      expect(passwordInput.type).toBe('text')
    })
  })

  it('validates display name is required on blur', async () => {
    render(<RegisterPage />)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /create account/i })).toBeInTheDocument()
    })

    const displayNameInput = screen.getByLabelText(/display name/i)

    // Focus and blur without entering value
    fireEvent.focus(displayNameInput)
    fireEvent.blur(displayNameInput)

    await waitFor(() => {
      expect(screen.getByText(/display name is required/i)).toBeInTheDocument()
    })
  })

  it('validates email format on blur', async () => {
    render(<RegisterPage />)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /create account/i })).toBeInTheDocument()
    })

    const emailInput = screen.getByLabelText(/email address/i)

    // Enter invalid email
    fireEvent.change(emailInput, { target: { value: 'invalid-email' } })
    fireEvent.blur(emailInput)

    await waitFor(() => {
      expect(screen.getByText(/please enter a valid email address/i)).toBeInTheDocument()
    })
  })

  it('validates password confirmation matches', async () => {
    render(<RegisterPage />)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /create account/i })).toBeInTheDocument()
    })

    const passwordInputs = screen.getAllByLabelText(/password/i) as HTMLInputElement[]
    const passwordInput = passwordInputs[0]
    const confirmPasswordInput = screen.getByLabelText(/confirm password/i)

    // Enter mismatched passwords
    fireEvent.change(passwordInput, { target: { value: 'Password123!' } })
    fireEvent.change(confirmPasswordInput, { target: { value: 'DifferentPassword' } })
    fireEvent.blur(confirmPasswordInput)

    await waitFor(() => {
      expect(screen.getByText(/passwords do not match/i)).toBeInTheDocument()
    })
  })

  it('shows sign in link', async () => {
    render(<RegisterPage />)

    await waitFor(() => {
      expect(screen.getByText(/already have an account/i)).toBeInTheDocument()
    })

    expect(screen.getByRole('link', { name: /sign in/i })).toBeInTheDocument()
  })

  it('disables submit button when submitting', async () => {
    // Mock a slow register response
    // Never resolves - simulates slow response
    vi.mocked(authService.register).mockImplementation(() => new Promise(() => {}))

    render(<RegisterPage />)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /create account/i })).toBeInTheDocument()
    })

    const displayNameInput = screen.getByLabelText(/display name/i)
    const emailInput = screen.getByLabelText(/email address/i)
    const passwordInputs = screen.getAllByLabelText(/password/i) as HTMLInputElement[]
    const passwordInput = passwordInputs[0]
    const confirmPasswordInput = screen.getByLabelText(/confirm password/i)
    const submitButton = screen.getByRole('button', { name: /create account/i })

    // Fill in valid form data
    fireEvent.change(displayNameInput, { target: { value: 'Test User' } })
    fireEvent.change(emailInput, { target: { value: 'test@example.com' } })
    fireEvent.change(passwordInput, { target: { value: 'Password123!' } })
    fireEvent.change(confirmPasswordInput, { target: { value: 'Password123!' } })

    // Submit form
    fireEvent.click(submitButton)

    // Button should be disabled during submission
    await waitFor(() => {
      expect(submitButton).toBeDisabled()
    })
  })

  it('shows password requirements as user types', async () => {
    render(<RegisterPage />)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /create account/i })).toBeInTheDocument()
    })

    const passwordInputs = screen.getAllByLabelText(/password/i) as HTMLInputElement[]
    const passwordInput = passwordInputs[0]

    // Type a weak password and blur to trigger requirements display
    fireEvent.change(passwordInput, { target: { value: 'weak' } })
    fireEvent.blur(passwordInput)

    // Should show requirement indicators - check for the actual requirement text
    await waitFor(() => {
      expect(screen.getByText(/at least 8 characters/i)).toBeInTheDocument()
    })
  })

  it('shows error message when registration fails', async () => {
    vi.mocked(authService.register).mockResolvedValue({
      isSuccess: false,
      expiresIn: 0,
      tokenType: 'Bearer',
      error: {
        code: 'duplicate_email',
        message: 'Email is already registered',
      },
    })

    render(<RegisterPage />)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /create account/i })).toBeInTheDocument()
    })

    const displayNameInput = screen.getByLabelText(/display name/i)
    const emailInput = screen.getByLabelText(/email address/i)
    const passwordInputs = screen.getAllByLabelText(/password/i) as HTMLInputElement[]
    const passwordInput = passwordInputs[0]
    const confirmPasswordInput = screen.getByLabelText(/confirm password/i)
    const submitButton = screen.getByRole('button', { name: /create account/i })

    // Fill in valid form data
    fireEvent.change(displayNameInput, { target: { value: 'Test User' } })
    fireEvent.change(emailInput, { target: { value: 'existing@example.com' } })
    fireEvent.change(passwordInput, { target: { value: 'Password123!' } })
    fireEvent.change(confirmPasswordInput, { target: { value: 'Password123!' } })

    // Submit form
    fireEvent.click(submitButton)

    // Should show error message
    await waitFor(() => {
      expect(screen.getByText(/email is already registered/i)).toBeInTheDocument()
    })
  })
})
