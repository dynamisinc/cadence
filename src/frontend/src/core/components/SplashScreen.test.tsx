/**
 * SplashScreen Component Tests
 *
 * Tests for the startup splash screen shown once per app version.
 * Covers branding content, auto-close timer, hover pause, and manual close.
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, act } from '@testing-library/react'
import { ThemeProvider } from '@mui/material/styles'
import { cobraTheme } from '@/theme/cobraTheme'
import { SplashScreen } from './SplashScreen'

// Mock version config
vi.mock('@/config/version', () => ({
  appVersion: { version: '1.2.3', commitSha: 'abc123' },
}))

const renderWithTheme = (component: React.ReactElement) => {
  return render(
    <ThemeProvider theme={cobraTheme}>
      {component}
    </ThemeProvider>,
  )
}

describe('SplashScreen', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.runOnlyPendingTimers()
    vi.useRealTimers()
  })

  describe('branding content', () => {
    it('renders the CADENCE heading', () => {
      const onComplete = vi.fn()
      renderWithTheme(<SplashScreen onComplete={onComplete} />)

      expect(screen.getByText('CADENCE')).toBeInTheDocument()
    })

    it('renders the platform subtitle', () => {
      const onComplete = vi.fn()
      renderWithTheme(<SplashScreen onComplete={onComplete} />)

      expect(screen.getByText('HSEEP MSEL Management Platform')).toBeInTheDocument()
    })

    it('renders the version as major.minor only', () => {
      const onComplete = vi.fn()
      renderWithTheme(<SplashScreen onComplete={onComplete} />)

      // Version 1.2.3 should display as "v1.2"
      expect(screen.getByText(/v1\.2/)).toBeInTheDocument()
      // Patch version should NOT be shown
      expect(screen.queryByText(/v1\.2\.3/)).not.toBeInTheDocument()
    })

    it('renders the copyright notice', () => {
      const onComplete = vi.fn()
      renderWithTheme(<SplashScreen onComplete={onComplete} />)

      const currentYear = new Date().getFullYear()
      expect(screen.getByText(new RegExp(String(currentYear)))).toBeInTheDocument()
      expect(screen.getByText(/Dynamis, Inc/)).toBeInTheDocument()
    })

    it('renders the logo image', () => {
      const onComplete = vi.fn()
      renderWithTheme(<SplashScreen onComplete={onComplete} />)

      expect(screen.getByAltText('Cadence Logo')).toBeInTheDocument()
    })

    it('renders the accessible loading status landmark', () => {
      const onComplete = vi.fn()
      renderWithTheme(<SplashScreen onComplete={onComplete} />)

      expect(screen.getByRole('status', { name: /loading cadence/i })).toBeInTheDocument()
    })
  })

  describe('close button', () => {
    it('renders a close button', () => {
      const onComplete = vi.fn()
      renderWithTheme(<SplashScreen onComplete={onComplete} />)

      expect(screen.getByRole('button', { name: /close splash screen/i })).toBeInTheDocument()
    })

    it('clicking close button starts fade and calls onComplete after fade duration', () => {
      const onComplete = vi.fn()
      renderWithTheme(<SplashScreen onComplete={onComplete} />)

      const closeButton = screen.getByRole('button', { name: /close splash screen/i })
      // Advance timer in same act as click so the timeout fires before React cleanup
      act(() => {
        fireEvent.click(closeButton)
        vi.advanceTimersByTime(500)
      })
      expect(onComplete).toHaveBeenCalledTimes(1)
    })

    it('clicking close button a second time does not call onComplete twice', () => {
      const onComplete = vi.fn()
      renderWithTheme(<SplashScreen onComplete={onComplete} />)

      const closeButton = screen.getByRole('button', { name: /close splash screen/i })
      act(() => {
        fireEvent.click(closeButton)
        vi.advanceTimersByTime(500)
      })
      // Second click after fade has started — guard `if (!fading)` prevents double-fire
      act(() => {
        fireEvent.click(closeButton)
        vi.advanceTimersByTime(500)
      })
      expect(onComplete).toHaveBeenCalledTimes(1)
    })
  })

  describe('auto-close timer', () => {
    it('calls onComplete after 4500ms (4000ms display + 500ms fade)', () => {
      const onComplete = vi.fn()
      renderWithTheme(<SplashScreen onComplete={onComplete} />)

      // Not called before auto-close
      vi.advanceTimersByTime(4000)
      expect(onComplete).not.toHaveBeenCalled()

      // Fade begins at 4000ms, completes 500ms later
      vi.advanceTimersByTime(500)
      expect(onComplete).toHaveBeenCalledTimes(1)
    })

    it('does not call onComplete before the display duration elapses', () => {
      const onComplete = vi.fn()
      renderWithTheme(<SplashScreen onComplete={onComplete} />)

      vi.advanceTimersByTime(3999)
      expect(onComplete).not.toHaveBeenCalled()
    })
  })

  describe('hover behavior', () => {
    it('pauses auto-close timer when card is hovered', () => {
      const onComplete = vi.fn()
      renderWithTheme(<SplashScreen onComplete={onComplete} />)

      const card = screen.getByRole('status', { name: /loading cadence/i })

      // Hover the card after 2 seconds — timer should pause
      vi.advanceTimersByTime(2000)
      fireEvent.mouseEnter(card)

      // Advance past what would have been the close time
      vi.advanceTimersByTime(3000)
      expect(onComplete).not.toHaveBeenCalled()
    })

    it('resumes auto-close timer when mouse leaves after hover', () => {
      const onComplete = vi.fn()
      renderWithTheme(<SplashScreen onComplete={onComplete} />)

      const card = screen.getByRole('status', { name: /loading cadence/i })

      // Hover, then leave — a new 4000ms timer should start
      fireEvent.mouseEnter(card)
      vi.advanceTimersByTime(2000)
      fireEvent.mouseLeave(card)

      // Should not complete immediately after mouse leave
      vi.advanceTimersByTime(3999)
      expect(onComplete).not.toHaveBeenCalled()

      // Should complete after full 4000ms display + 500ms fade from resume
      vi.advanceTimersByTime(501)
      expect(onComplete).toHaveBeenCalledTimes(1)
    })
  })
})
