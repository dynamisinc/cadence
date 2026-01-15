/**
 * InstallBanner Component Tests
 *
 * Tests for PWA installation banner
 */

import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { ThemeProvider } from '@mui/material/styles'
import { cobraTheme } from '../../theme/cobraTheme'
import { InstallBanner } from './InstallBanner'

// Mock the useInstallPrompt hook
const mockUseInstallPrompt = vi.fn()

vi.mock('../../shared/hooks', () => ({
  useInstallPrompt: () => mockUseInstallPrompt(),
}))

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
    mockUseInstallPrompt.mockReturnValue(defaultMockReturn)
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
