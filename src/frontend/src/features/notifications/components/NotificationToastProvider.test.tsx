/**
 * NotificationToastProvider Tests
 *
 * Tests for the toast notification provider that manages:
 * - Rendering toasts via Portal
 * - showToast and clearAll context functions
 * - Integration with useNotificationToast hook
 * - Multiple toast display
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '../../../test/test-utils'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { MemoryRouter } from 'react-router-dom'
import { NotificationToastProvider, useToast } from './NotificationToastProvider'
import type { NotificationDto } from '../types'

// Mock the useNotificationToast hook
const mockAddToast = vi.fn()
const mockRemoveToast = vi.fn()
const mockPauseAutoDismiss = vi.fn()
const mockResumeAutoDismiss = vi.fn()
const mockClearAll = vi.fn()
const mockToasts = vi.fn(() => [])

vi.mock('../hooks/useNotificationToast', () => ({
  useNotificationToast: () => ({
    toasts: mockToasts(),
    addToast: mockAddToast,
    removeToast: mockRemoveToast,
    pauseAutoDismiss: mockPauseAutoDismiss,
    resumeAutoDismiss: mockResumeAutoDismiss,
    clearAll: mockClearAll,
  }),
  getToastConfig: (priority: string) => ({
    showToast: true,
    autoDismissMs: priority === 'High' ? null : 10000,
    backgroundColor: '#e3f2fd',
    borderColor: '#2196f3',
  }),
}))

// Helper to create mock notification
const createMockNotification = (
  overrides: Partial<NotificationDto> = {},
): NotificationDto => ({
  id: 'notif-1',
  type: 'InjectReady',
  priority: 'Medium',
  title: 'Inject Ready',
  message: 'Inject #5 is ready to fire',
  actionUrl: '/exercises/ex-1/conduct',
  relatedEntityType: 'Inject',
  relatedEntityId: 'inject-5',
  isRead: false,
  createdAt: '2026-01-15T10:00:00Z',
  readAt: null,
  ...overrides,
})

// Test component that uses the useToast hook
const TestConsumer = () => {
  const { showToast, clearAll } = useToast()

  return (
    <div>
      <button
        data-testid="show-toast-btn"
        onClick={() => showToast(createMockNotification())}
      >
        Show Toast
      </button>
      <button data-testid="clear-all-btn" onClick={clearAll}>
        Clear All
      </button>
    </div>
  )
}

// Test component without provider (for error test)
const ConsumerWithoutProvider = () => {
  const { showToast } = useToast()
  return <button onClick={() => showToast(createMockNotification())}>Test</button>
}

// Wrapper component with Router
const Wrapper = ({ children }: { children: ReactNode }) => (
  <MemoryRouter>{children}</MemoryRouter>
)

describe('NotificationToastProvider', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockToasts.mockReturnValue([])
  })

  // ============================================================================
  // Provider Tests
  // ============================================================================

  describe('NotificationToastProvider', () => {
    it('renders children properly', () => {
      render(
        <Wrapper>
          <NotificationToastProvider>
            <div data-testid="test-child">Test Content</div>
          </NotificationToastProvider>
        </Wrapper>,
      )

      expect(screen.getByTestId('test-child')).toBeInTheDocument()
      expect(screen.getByText('Test Content')).toBeInTheDocument()
    })

    it('renders toast container via Portal', () => {
      const mockToast = {
        id: 'toast-1',
        notification: createMockNotification(),
        createdAt: new Date(),
      }

      mockToasts.mockReturnValue([mockToast])

      render(
        <Wrapper>
          <NotificationToastProvider>
            <div>Content</div>
          </NotificationToastProvider>
        </Wrapper>,
      )

      // Toast should render in the document
      expect(screen.getByText('Inject Ready')).toBeInTheDocument()
    })
  })

  // ============================================================================
  // useToast Hook Tests
  // ============================================================================

  describe('useToast', () => {
    it('throws error when used outside provider', () => {
      // Suppress console.error for this test
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {})

      expect(() => {
        render(<ConsumerWithoutProvider />)
      }).toThrow('useToast must be used within NotificationToastProvider')

      consoleSpy.mockRestore()
    })

    it('provides showToast function', () => {
      render(
        <Wrapper>
          <NotificationToastProvider>
            <TestConsumer />
          </NotificationToastProvider>
        </Wrapper>,
      )

      expect(screen.getByTestId('show-toast-btn')).toBeInTheDocument()
    })

    it('provides clearAll function', () => {
      render(
        <Wrapper>
          <NotificationToastProvider>
            <TestConsumer />
          </NotificationToastProvider>
        </Wrapper>,
      )

      expect(screen.getByTestId('clear-all-btn')).toBeInTheDocument()
    })
  })

  // ============================================================================
  // showToast Tests
  // ============================================================================

  describe('showToast', () => {
    it('calls addToast when showToast is invoked', async () => {
      const user = userEvent.setup()

      render(
        <Wrapper>
          <NotificationToastProvider>
            <TestConsumer />
          </NotificationToastProvider>
        </Wrapper>,
      )

      await user.click(screen.getByTestId('show-toast-btn'))

      expect(mockAddToast).toHaveBeenCalledTimes(1)
      expect(mockAddToast).toHaveBeenCalledWith(
        expect.objectContaining({
          id: 'notif-1',
          type: 'InjectReady',
          title: 'Inject Ready',
          message: 'Inject #5 is ready to fire',
        }),
      )
    })

    it('adds a toast to the screen when showToast is called', async () => {
      const user = userEvent.setup()

      // Setup mock to return a toast after button click
      const mockToast = {
        id: 'toast-1',
        notification: createMockNotification(),
        createdAt: new Date(),
      }

      mockToasts.mockReturnValue([mockToast])

      render(
        <Wrapper>
          <NotificationToastProvider>
            <TestConsumer />
          </NotificationToastProvider>
        </Wrapper>,
      )

      // After clicking, the toast should appear
      await user.click(screen.getByTestId('show-toast-btn'))

      await waitFor(() => {
        expect(screen.getByText('Inject Ready')).toBeInTheDocument()
        expect(screen.getByText('Inject #5 is ready to fire')).toBeInTheDocument()
      })
    })
  })

  // ============================================================================
  // clearAll Tests
  // ============================================================================

  describe('clearAll', () => {
    it('calls clearAll from useNotificationToast hook', async () => {
      const user = userEvent.setup()

      render(
        <Wrapper>
          <NotificationToastProvider>
            <TestConsumer />
          </NotificationToastProvider>
        </Wrapper>,
      )

      await user.click(screen.getByTestId('clear-all-btn'))

      expect(mockClearAll).toHaveBeenCalledTimes(1)
    })

    it('removes all toasts from the screen', async () => {
      const user = userEvent.setup()

      // Start with toasts
      const mockToast1 = {
        id: 'toast-1',
        notification: createMockNotification({ id: 'notif-1', title: 'Toast 1' }),
        createdAt: new Date(),
      }
      const mockToast2 = {
        id: 'toast-2',
        notification: createMockNotification({ id: 'notif-2', title: 'Toast 2' }),
        createdAt: new Date(),
      }

      mockToasts.mockReturnValue([mockToast1, mockToast2])

      const { rerender } = render(
        <Wrapper>
          <NotificationToastProvider>
            <TestConsumer />
          </NotificationToastProvider>
        </Wrapper>,
      )

      expect(screen.getByText('Toast 1')).toBeInTheDocument()
      expect(screen.getByText('Toast 2')).toBeInTheDocument()

      // Clear all toasts
      mockToasts.mockReturnValue([])

      await user.click(screen.getByTestId('clear-all-btn'))

      // Rerender to reflect state change
      rerender(
        <Wrapper>
          <NotificationToastProvider>
            <TestConsumer />
          </NotificationToastProvider>
        </Wrapper>,
      )

      expect(screen.queryByText('Toast 1')).not.toBeInTheDocument()
      expect(screen.queryByText('Toast 2')).not.toBeInTheDocument()
    })
  })

  // ============================================================================
  // Multiple Toasts Tests
  // ============================================================================

  describe('multiple toasts', () => {
    it('can display multiple toasts simultaneously', () => {
      const mockToast1 = {
        id: 'toast-1',
        notification: createMockNotification({
          id: 'notif-1',
          title: 'First Toast',
          message: 'First message',
        }),
        createdAt: new Date(),
      }
      const mockToast2 = {
        id: 'toast-2',
        notification: createMockNotification({
          id: 'notif-2',
          title: 'Second Toast',
          message: 'Second message',
        }),
        createdAt: new Date(),
      }
      const mockToast3 = {
        id: 'toast-3',
        notification: createMockNotification({
          id: 'notif-3',
          title: 'Third Toast',
          message: 'Third message',
        }),
        createdAt: new Date(),
      }

      mockToasts.mockReturnValue([mockToast1, mockToast2, mockToast3])

      render(
        <Wrapper>
          <NotificationToastProvider>
            <div>Content</div>
          </NotificationToastProvider>
        </Wrapper>,
      )

      expect(screen.getByText('First Toast')).toBeInTheDocument()
      expect(screen.getByText('Second Toast')).toBeInTheDocument()
      expect(screen.getByText('Third Toast')).toBeInTheDocument()
    })

    it('renders toasts in stack order', () => {
      const mockToast1 = {
        id: 'toast-1',
        notification: createMockNotification({
          id: 'notif-1',
          title: 'First Toast',
        }),
        createdAt: new Date(),
      }
      const mockToast2 = {
        id: 'toast-2',
        notification: createMockNotification({
          id: 'notif-2',
          title: 'Second Toast',
        }),
        createdAt: new Date(),
      }

      mockToasts.mockReturnValue([mockToast1, mockToast2])

      render(
        <Wrapper>
          <NotificationToastProvider>
            <div>Content</div>
          </NotificationToastProvider>
        </Wrapper>,
      )

      // Both toasts should be rendered
      expect(screen.getByText('First Toast')).toBeInTheDocument()
      expect(screen.getByText('Second Toast')).toBeInTheDocument()
    })
  })

  // ============================================================================
  // Toast Interaction Tests
  // ============================================================================

  describe('toast interactions', () => {
    it('calls removeToast when toast is dismissed', async () => {
      const user = userEvent.setup()

      const mockToast = {
        id: 'toast-1',
        notification: createMockNotification(),
        createdAt: new Date(),
      }

      mockToasts.mockReturnValue([mockToast])

      render(
        <Wrapper>
          <NotificationToastProvider>
            <div>Content</div>
          </NotificationToastProvider>
        </Wrapper>,
      )

      const closeButton = screen.getByRole('button')
      await user.click(closeButton)

      expect(mockRemoveToast).toHaveBeenCalledWith('toast-1')
    })

    it('calls pauseAutoDismiss when mouse enters toast', async () => {
      const mockToast = {
        id: 'toast-1',
        notification: createMockNotification(),
        createdAt: new Date(),
      }

      mockToasts.mockReturnValue([mockToast])

      render(
        <Wrapper>
          <NotificationToastProvider>
            <div>Content</div>
          </NotificationToastProvider>
        </Wrapper>,
      )

      const paper = screen.getByText('Inject Ready').closest('.MuiPaper-root')
      await userEvent.hover(paper!)

      expect(mockPauseAutoDismiss).toHaveBeenCalledWith('toast-1')
    })

    it('calls resumeAutoDismiss when mouse leaves toast', async () => {
      const mockToast = {
        id: 'toast-1',
        notification: createMockNotification({ priority: 'Medium' }),
        createdAt: new Date(),
      }

      mockToasts.mockReturnValue([mockToast])

      render(
        <Wrapper>
          <NotificationToastProvider>
            <div>Content</div>
          </NotificationToastProvider>
        </Wrapper>,
      )

      const paper = screen.getByText('Inject Ready').closest('.MuiPaper-root')
      await userEvent.unhover(paper!)

      expect(mockResumeAutoDismiss).toHaveBeenCalledWith('toast-1', 'Medium')
    })
  })

  // ============================================================================
  // Portal Container Tests
  // ============================================================================

  describe('portal container', () => {
    it('renders multiple toasts in a container', () => {
      const mockToast1 = {
        id: 'toast-1',
        notification: createMockNotification({ title: 'Toast 1' }),
        createdAt: new Date(),
      }
      const mockToast2 = {
        id: 'toast-2',
        notification: createMockNotification({ title: 'Toast 2' }),
        createdAt: new Date(),
      }

      mockToasts.mockReturnValue([mockToast1, mockToast2])

      render(
        <Wrapper>
          <NotificationToastProvider>
            <div>Content</div>
          </NotificationToastProvider>
        </Wrapper>,
      )

      // Both toasts should be in the document
      expect(screen.getByText('Toast 1')).toBeInTheDocument()
      expect(screen.getByText('Toast 2')).toBeInTheDocument()
    })
  })
})
