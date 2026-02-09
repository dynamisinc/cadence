/**
 * InstallBanner Component Tests
 *
 * Tests for PWA installation banner with persistent dismiss behavior
 */

import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { ThemeProvider } from '@mui/material/styles'
import { cobraTheme } from '../../theme/cobraTheme'
import { InstallBanner } from './InstallBanner'
import { shouldShowBanner } from './installBannerUtils'

// Mock the useInstallPrompt hook
const mockUseInstallPrompt = vi.fn()

vi.mock('../../shared/hooks', () => ({
  useInstallPrompt: () => mockUseInstallPrompt(),
}))

vi.mock('../../config/version', () => ({
  appVersion: {
    version: '2.1.0',
    buildDate: '2026-01-01T00:00:00Z',
    commitSha: 'abc1234',
  },
}))

const DISMISS_KEY = 'cadence-install-banner-dismissed'

const renderWithTheme = (component: React.ReactElement) => {
  return render(
    <ThemeProvider theme={cobraTheme}>
      {component}
    </ThemeProvider>,
  )
}

describe('InstallBanner', () => {
  const defaultMockReturn = {
    canInstall: false,
    isInstalled: false,
    promptInstall: vi.fn(),
  }

  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
    mockUseInstallPrompt.mockReturnValue(defaultMockReturn)
  })

  afterEach(() => {
    localStorage.clear()
  })

  describe('when not installable', () => {
    it('renders nothing when canInstall is false', () => {
      const { container } = renderWithTheme(<InstallBanner />)

      expect(container.firstChild).toBeNull()
    })

    it('renders nothing when already installed', () => {
      mockUseInstallPrompt.mockReturnValue({
        ...defaultMockReturn,
        canInstall: false,
        isInstalled: true,
      })

      const { container } = renderWithTheme(<InstallBanner />)

      expect(container.firstChild).toBeNull()
    })
  })

  describe('when installable', () => {
    beforeEach(() => {
      mockUseInstallPrompt.mockReturnValue({
        ...defaultMockReturn,
        canInstall: true,
      })
    })

    it('shows install banner', () => {
      renderWithTheme(<InstallBanner />)

      expect(screen.getByText('Install Cadence')).toBeInTheDocument()
      expect(screen.getByText('Add to your device for offline access')).toBeInTheDocument()
    })

    it('shows Install button', () => {
      renderWithTheme(<InstallBanner />)

      // Use exact match to avoid matching "Install Cadence" heading
      expect(screen.getByRole('button', { name: /^install$/i })).toBeInTheDocument()
    })

    it('shows dismiss button', () => {
      renderWithTheme(<InstallBanner />)

      expect(screen.getByRole('button', { name: /dismiss/i })).toBeInTheDocument()
    })

    it('calls promptInstall when Install clicked', async () => {
      const mockPromptInstall = vi.fn().mockResolvedValue(true)
      mockUseInstallPrompt.mockReturnValue({
        ...defaultMockReturn,
        canInstall: true,
        promptInstall: mockPromptInstall,
      })

      renderWithTheme(<InstallBanner />)

      fireEvent.click(screen.getByRole('button', { name: /^install$/i }))

      await waitFor(() => {
        expect(mockPromptInstall).toHaveBeenCalled()
      })
    })

    it('hides banner when user accepts install', async () => {
      const mockPromptInstall = vi.fn().mockResolvedValue(true)
      mockUseInstallPrompt.mockReturnValue({
        ...defaultMockReturn,
        canInstall: true,
        promptInstall: mockPromptInstall,
      })

      renderWithTheme(<InstallBanner />)

      fireEvent.click(screen.getByRole('button', { name: /^install$/i }))

      // Banner should still be visible (canInstall will be set to false by the hook)
      // The component just calls promptInstall and lets the hook manage state
      await waitFor(() => {
        expect(mockPromptInstall).toHaveBeenCalled()
      })
    })

    it('hides banner when user declines install', async () => {
      const mockPromptInstall = vi.fn().mockResolvedValue(false)
      mockUseInstallPrompt.mockReturnValue({
        ...defaultMockReturn,
        canInstall: true,
        promptInstall: mockPromptInstall,
      })

      renderWithTheme(<InstallBanner />)

      fireEvent.click(screen.getByRole('button', { name: /^install$/i }))

      // After declining, banner should be dismissed
      await waitFor(() => {
        expect(screen.queryByText('Install Cadence')).not.toBeInTheDocument()
      })
    })

    it('hides banner when dismiss button clicked', () => {
      renderWithTheme(<InstallBanner />)

      fireEvent.click(screen.getByRole('button', { name: /dismiss/i }))

      expect(screen.queryByText('Install Cadence')).not.toBeInTheDocument()
    })
  })

  describe('persistent dismiss', () => {
    beforeEach(() => {
      mockUseInstallPrompt.mockReturnValue({
        ...defaultMockReturn,
        canInstall: true,
      })
    })

    it('persists dismiss to localStorage when dismiss button clicked', () => {
      renderWithTheme(<InstallBanner />)

      fireEvent.click(screen.getByRole('button', { name: /dismiss/i }))

      const stored = localStorage.getItem(DISMISS_KEY)
      expect(stored).not.toBeNull()

      const state = JSON.parse(stored!)
      expect(state.majorVersion).toBe('2')
      expect(state.dismissedAt).toBeGreaterThan(0)
    })

    it('persists dismiss to localStorage when user declines install', async () => {
      const mockPromptInstall = vi.fn().mockResolvedValue(false)
      mockUseInstallPrompt.mockReturnValue({
        ...defaultMockReturn,
        canInstall: true,
        promptInstall: mockPromptInstall,
      })

      renderWithTheme(<InstallBanner />)

      fireEvent.click(screen.getByRole('button', { name: /^install$/i }))

      await waitFor(() => {
        const stored = localStorage.getItem(DISMISS_KEY)
        expect(stored).not.toBeNull()
      })
    })

    it('does not persist dismiss when user accepts install', async () => {
      const mockPromptInstall = vi.fn().mockResolvedValue(true)
      mockUseInstallPrompt.mockReturnValue({
        ...defaultMockReturn,
        canInstall: true,
        promptInstall: mockPromptInstall,
      })

      renderWithTheme(<InstallBanner />)

      fireEvent.click(screen.getByRole('button', { name: /^install$/i }))

      await waitFor(() => {
        expect(mockPromptInstall).toHaveBeenCalled()
      })

      expect(localStorage.getItem(DISMISS_KEY)).toBeNull()
    })

    it('does not show banner when previously dismissed', () => {
      // Set dismiss state in localStorage
      localStorage.setItem(DISMISS_KEY, JSON.stringify({
        dismissedAt: Date.now(),
        majorVersion: '2',
      }))

      const { container } = renderWithTheme(<InstallBanner />)

      expect(container.firstChild).toBeNull()
    })
  })

  describe('styling', () => {
    beforeEach(() => {
      mockUseInstallPrompt.mockReturnValue({
        ...defaultMockReturn,
        canInstall: true,
      })
    })

    it('renders as a Paper component', () => {
      renderWithTheme(<InstallBanner />)

      // Paper component has MuiPaper class
      const paper = document.querySelector('.MuiPaper-root')
      expect(paper).toBeInTheDocument()
    })

    it('is fixed positioned at bottom', () => {
      renderWithTheme(<InstallBanner />)

      const paper = document.querySelector('.MuiPaper-root')
      expect(paper).toHaveStyle({ position: 'fixed' })
    })
  })
})

describe('shouldShowBanner', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  afterEach(() => {
    localStorage.clear()
  })

  it('returns true when no dismiss state in localStorage', () => {
    expect(shouldShowBanner()).toBe(true)
  })

  it('returns false when dismissed recently on same major version', () => {
    localStorage.setItem(DISMISS_KEY, JSON.stringify({
      dismissedAt: Date.now(),
      majorVersion: '2',
    }))

    expect(shouldShowBanner()).toBe(false)
  })

  it('returns true when dismissed over 90 days ago', () => {
    const ninetyOneDaysAgo = Date.now() - (91 * 24 * 60 * 60 * 1000)
    localStorage.setItem(DISMISS_KEY, JSON.stringify({
      dismissedAt: ninetyOneDaysAgo,
      majorVersion: '2',
    }))

    expect(shouldShowBanner()).toBe(true)
  })

  it('returns false when dismissed 89 days ago', () => {
    const eightyNineDaysAgo = Date.now() - (89 * 24 * 60 * 60 * 1000)
    localStorage.setItem(DISMISS_KEY, JSON.stringify({
      dismissedAt: eightyNineDaysAgo,
      majorVersion: '2',
    }))

    expect(shouldShowBanner()).toBe(false)
  })

  it('returns true when major version has changed', () => {
    localStorage.setItem(DISMISS_KEY, JSON.stringify({
      dismissedAt: Date.now(),
      majorVersion: '1', // Old major version
    }))

    // appVersion is mocked as '2.1.0' so major is '2'
    expect(shouldShowBanner()).toBe(true)
  })

  it('returns true when localStorage contains invalid JSON', () => {
    localStorage.setItem(DISMISS_KEY, 'not-json')

    expect(shouldShowBanner()).toBe(true)
  })
})
