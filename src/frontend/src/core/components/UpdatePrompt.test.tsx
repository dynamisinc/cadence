/**
 * UpdatePrompt Component Tests
 *
 * Tests for PWA update and offline-ready notifications
 */

import { render, screen, fireEvent } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { ThemeProvider } from '@mui/material/styles'
import { cobraTheme } from '../../theme/cobraTheme'
import { UpdatePrompt } from './UpdatePrompt'

// Mock the useServiceWorker hook
const mockUseServiceWorker = vi.fn()

vi.mock('../../shared/hooks', () => ({
  useServiceWorker: () => mockUseServiceWorker(),
}))

const renderWithTheme = (component: React.ReactElement) => {
  return render(
    <ThemeProvider theme={cobraTheme}>
      {component}
    </ThemeProvider>,
  )
}

describe('UpdatePrompt', () => {
  const defaultMockReturn = {
    needRefresh: false,
    offlineReady: false,
    updateServiceWorker: vi.fn(),
    dismissNotification: vi.fn(),
  }

  beforeEach(() => {
    vi.clearAllMocks()
    mockUseServiceWorker.mockReturnValue(defaultMockReturn)
  })

  describe('when neither offline ready nor refresh needed', () => {
    it('renders nothing', () => {
      const { container } = renderWithTheme(<UpdatePrompt />)

      expect(container.firstChild).toBeNull()
    })
  })

  describe('when offline ready', () => {
    beforeEach(() => {
      mockUseServiceWorker.mockReturnValue({
        ...defaultMockReturn,
        offlineReady: true,
      })
    })

    it('shows offline ready message', () => {
      renderWithTheme(<UpdatePrompt />)

      expect(screen.getByText('Cadence is ready to work offline')).toBeInTheDocument()
    })

    it('shows success alert', () => {
      renderWithTheme(<UpdatePrompt />)

      const alert = screen.getByRole('alert')
      expect(alert).toHaveClass('MuiAlert-standardSuccess')
    })

    it('calls dismissNotification when closed', () => {
      const mockDismiss = vi.fn()
      mockUseServiceWorker.mockReturnValue({
        ...defaultMockReturn,
        offlineReady: true,
        dismissNotification: mockDismiss,
      })

      renderWithTheme(<UpdatePrompt />)

      // Click the close button on the alert
      const closeButton = screen.getByRole('button', { name: /close/i })
      fireEvent.click(closeButton)

      expect(mockDismiss).toHaveBeenCalled()
    })
  })

  describe('when refresh needed', () => {
    beforeEach(() => {
      mockUseServiceWorker.mockReturnValue({
        ...defaultMockReturn,
        needRefresh: true,
      })
    })

    it('shows update available message', () => {
      renderWithTheme(<UpdatePrompt />)

      expect(screen.getByText('A new version of Cadence is available')).toBeInTheDocument()
    })

    it('shows info alert', () => {
      renderWithTheme(<UpdatePrompt />)

      const alert = screen.getByRole('alert')
      expect(alert).toHaveClass('MuiAlert-standardInfo')
    })

    it('shows Update button', () => {
      renderWithTheme(<UpdatePrompt />)

      expect(screen.getByRole('button', { name: /update/i })).toBeInTheDocument()
    })

    it('shows Later button', () => {
      renderWithTheme(<UpdatePrompt />)

      expect(screen.getByRole('button', { name: /later/i })).toBeInTheDocument()
    })

    it('calls updateServiceWorker when Update clicked', () => {
      const mockUpdate = vi.fn()
      mockUseServiceWorker.mockReturnValue({
        ...defaultMockReturn,
        needRefresh: true,
        updateServiceWorker: mockUpdate,
      })

      renderWithTheme(<UpdatePrompt />)

      fireEvent.click(screen.getByRole('button', { name: /update/i }))

      expect(mockUpdate).toHaveBeenCalled()
    })

    it('calls dismissNotification when Later clicked', () => {
      const mockDismiss = vi.fn()
      mockUseServiceWorker.mockReturnValue({
        ...defaultMockReturn,
        needRefresh: true,
        dismissNotification: mockDismiss,
      })

      renderWithTheme(<UpdatePrompt />)

      fireEvent.click(screen.getByRole('button', { name: /later/i }))

      expect(mockDismiss).toHaveBeenCalled()
    })
  })

  describe('priority', () => {
    it('shows offline ready over refresh needed when both are true', () => {
      mockUseServiceWorker.mockReturnValue({
        ...defaultMockReturn,
        offlineReady: true,
        needRefresh: true,
      })

      renderWithTheme(<UpdatePrompt />)

      // Should show offline ready message, not update available
      expect(screen.getByText('Cadence is ready to work offline')).toBeInTheDocument()
      expect(screen.queryByText('A new version of Cadence is available')).not.toBeInTheDocument()
    })
  })
})
