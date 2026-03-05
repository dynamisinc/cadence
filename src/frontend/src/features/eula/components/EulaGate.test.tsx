/**
 * EulaGate Component Tests
 *
 * Tests for the EULA acceptance gate that blocks access until users
 * accept the current Terms of Use.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { ThemeProvider } from '@mui/material/styles'
import { cobraTheme } from '@/theme/cobraTheme'
import { EulaGate } from './EulaGate'

// Mock AuthContext
vi.mock('@/contexts/AuthContext', () => ({
  useAuth: vi.fn(() => ({ isAuthenticated: true, isLoading: false })),
}))

// Mock EULA hooks
vi.mock('../hooks/useEula', () => ({
  useEulaStatus: vi.fn(),
  useAcceptEula: vi.fn(() => ({ mutate: vi.fn(), isPending: false })),
}))

// Mock Loading component
vi.mock('@/shared/components/Loading', () => ({
  Loading: () => <div data-testid="loading">Loading</div>,
}))

// Mock react-markdown to avoid ESM issues in tests
vi.mock('react-markdown', () => ({
  default: ({ children }: { children: string }) => <div data-testid="markdown">{children}</div>,
}))

import * as AuthContext from '@/contexts/AuthContext'
import * as EulaHooks from '../hooks/useEula'

const renderWithTheme = (component: React.ReactElement) => {
  return render(
    <ThemeProvider theme={cobraTheme}>
      {component}
    </ThemeProvider>,
  )
}

describe('EulaGate', () => {
  const mockUseAuth = vi.mocked(AuthContext.useAuth)
  const mockUseEulaStatus = vi.mocked(EulaHooks.useEulaStatus)
  const mockUseAcceptEula = vi.mocked(EulaHooks.useAcceptEula)

  beforeEach(() => {
    vi.clearAllMocks()
    mockUseAuth.mockReturnValue({
      isAuthenticated: true,
      isLoading: false,
      user: null,
      accessToken: null,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      refreshAccessToken: vi.fn(),
    })
    mockUseEulaStatus.mockReturnValue({
      data: { required: false, version: null, content: null },
      isLoading: false,
    } as ReturnType<typeof EulaHooks.useEulaStatus>)
    mockUseAcceptEula.mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
    } as unknown as ReturnType<typeof EulaHooks.useAcceptEula>)
  })

  describe('when not authenticated', () => {
    it('renders children when not authenticated', () => {
      mockUseAuth.mockReturnValue({
        isAuthenticated: false,
        isLoading: false,
        user: null,
        accessToken: null,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshAccessToken: vi.fn(),
      })
      // When not authenticated, useEulaStatus is called with enabled=false
      mockUseEulaStatus.mockReturnValue({
        data: undefined,
        isLoading: false,
      } as ReturnType<typeof EulaHooks.useEulaStatus>)

      renderWithTheme(
        <EulaGate>
          <div data-testid="children">Protected Content</div>
        </EulaGate>,
      )

      expect(screen.getByTestId('children')).toBeInTheDocument()
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })

    it('renders children when auth is still loading', () => {
      mockUseAuth.mockReturnValue({
        isAuthenticated: false,
        isLoading: true,
        user: null,
        accessToken: null,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshAccessToken: vi.fn(),
      })
      mockUseEulaStatus.mockReturnValue({
        data: undefined,
        isLoading: false,
      } as ReturnType<typeof EulaHooks.useEulaStatus>)

      renderWithTheme(
        <EulaGate>
          <div data-testid="children">Protected Content</div>
        </EulaGate>,
      )

      expect(screen.getByTestId('children')).toBeInTheDocument()
    })
  })

  describe('when EULA status is loading', () => {
    it('renders Loading component while EULA status is fetching', () => {
      mockUseEulaStatus.mockReturnValue({
        data: undefined,
        isLoading: true,
      } as ReturnType<typeof EulaHooks.useEulaStatus>)

      renderWithTheme(
        <EulaGate>
          <div data-testid="children">Protected Content</div>
        </EulaGate>,
      )

      expect(screen.getByTestId('loading')).toBeInTheDocument()
      expect(screen.queryByTestId('children')).not.toBeInTheDocument()
    })
  })

  describe('when no EULA is required', () => {
    it('renders children when eulaStatus.required is false', () => {
      mockUseEulaStatus.mockReturnValue({
        data: { required: false, version: null, content: null },
        isLoading: false,
      } as ReturnType<typeof EulaHooks.useEulaStatus>)

      renderWithTheme(
        <EulaGate>
          <div data-testid="children">Protected Content</div>
        </EulaGate>,
      )

      expect(screen.getByTestId('children')).toBeInTheDocument()
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })

    it('renders children when eulaStatus data is undefined', () => {
      mockUseEulaStatus.mockReturnValue({
        data: undefined,
        isLoading: false,
      } as ReturnType<typeof EulaHooks.useEulaStatus>)

      renderWithTheme(
        <EulaGate>
          <div data-testid="children">Protected Content</div>
        </EulaGate>,
      )

      expect(screen.getByTestId('children')).toBeInTheDocument()
    })
  })

  describe('when EULA is required', () => {
    const eulaStatus = {
      required: true,
      version: '1.0',
      content: '# Terms of Use\n\nPlease read and accept these terms.',
    }

    beforeEach(() => {
      mockUseEulaStatus.mockReturnValue({
        data: eulaStatus,
        isLoading: false,
      } as ReturnType<typeof EulaHooks.useEulaStatus>)
    })

    it('does NOT render children when EULA acceptance is required', () => {
      renderWithTheme(
        <EulaGate>
          <div data-testid="children">Protected Content</div>
        </EulaGate>,
      )

      // Children must not be rendered — this was a bug we fixed
      expect(screen.queryByTestId('children')).not.toBeInTheDocument()
    })

    it('shows a blocking dialog when EULA acceptance is required', () => {
      renderWithTheme(
        <EulaGate>
          <div data-testid="children">Protected Content</div>
        </EulaGate>,
      )

      expect(screen.getByRole('dialog')).toBeInTheDocument()
    })

    it('shows "Terms of Use" title in the dialog', () => {
      renderWithTheme(
        <EulaGate>
          <div data-testid="children">Protected Content</div>
        </EulaGate>,
      )

      expect(screen.getByText('Terms of Use')).toBeInTheDocument()
    })

    it('shows the EULA version in the dialog', () => {
      renderWithTheme(
        <EulaGate>
          <div data-testid="children">Protected Content</div>
        </EulaGate>,
      )

      expect(screen.getByText('Version 1.0')).toBeInTheDocument()
    })

    it('renders the EULA content via markdown', () => {
      renderWithTheme(
        <EulaGate>
          <div data-testid="children">Protected Content</div>
        </EulaGate>,
      )

      // The mocked Markdown component renders the raw content
      expect(screen.getByTestId('markdown')).toBeInTheDocument()
    })

    it('shows scroll-to-bottom instruction before accepting', () => {
      renderWithTheme(
        <EulaGate>
          <div data-testid="children">Protected Content</div>
        </EulaGate>,
      )

      expect(screen.getByText('Please scroll to the bottom to continue')).toBeInTheDocument()
    })

    it('I Accept button is disabled before scrolling to bottom', () => {
      renderWithTheme(
        <EulaGate>
          <div data-testid="children">Protected Content</div>
        </EulaGate>,
      )

      const acceptButton = screen.getByRole('button', { name: /i accept/i })
      expect(acceptButton).toBeDisabled()
    })

    it('I Accept button becomes enabled after scrolling to bottom of dialog content', () => {
      renderWithTheme(
        <EulaGate>
          <div data-testid="children">Protected Content</div>
        </EulaGate>,
      )

      // Simulate scrolling to the bottom of the dialog content
      // The component checks: scrollHeight - scrollTop - clientHeight < 20
      const dialogContent = screen.getByRole('dialog').querySelector('.MuiDialogContent-root')
      expect(dialogContent).not.toBeNull()

      // Override scroll properties to simulate being at the bottom
      Object.defineProperty(dialogContent, 'scrollHeight', { value: 500, configurable: true })
      Object.defineProperty(dialogContent, 'scrollTop', { value: 490, configurable: true })
      Object.defineProperty(dialogContent, 'clientHeight', { value: 20, configurable: true })

      fireEvent.scroll(dialogContent!)

      const acceptButton = screen.getByRole('button', { name: /i accept/i })
      expect(acceptButton).not.toBeDisabled()
    })

    it('calls acceptMutation.mutate with EULA version when I Accept is clicked after scrolling', () => {
      const mockMutate = vi.fn()
      mockUseAcceptEula.mockReturnValue({
        mutate: mockMutate,
        isPending: false,
      } as unknown as ReturnType<typeof EulaHooks.useAcceptEula>)

      renderWithTheme(
        <EulaGate>
          <div data-testid="children">Protected Content</div>
        </EulaGate>,
      )

      // Scroll to bottom to enable the button
      const dialogContent = screen.getByRole('dialog').querySelector('.MuiDialogContent-root')
      Object.defineProperty(dialogContent, 'scrollHeight', { value: 500, configurable: true })
      Object.defineProperty(dialogContent, 'scrollTop', { value: 490, configurable: true })
      Object.defineProperty(dialogContent, 'clientHeight', { value: 20, configurable: true })
      fireEvent.scroll(dialogContent!)

      fireEvent.click(screen.getByRole('button', { name: /i accept/i }))

      expect(mockMutate).toHaveBeenCalledWith('1.0')
    })

    it('shows spinner and disables button while accept mutation is pending', () => {
      mockUseAcceptEula.mockReturnValue({
        mutate: vi.fn(),
        isPending: true,
      } as unknown as ReturnType<typeof EulaHooks.useAcceptEula>)

      renderWithTheme(
        <EulaGate>
          <div data-testid="children">Protected Content</div>
        </EulaGate>,
      )

      // Button is disabled (isPending even overrides scroll state)
      const acceptButton = screen.getByRole('button', { name: /i accept/i })
      expect(acceptButton).toBeDisabled()
    })
  })
})
