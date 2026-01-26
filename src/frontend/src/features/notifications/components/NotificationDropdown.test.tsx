/**
 * NotificationDropdown Component Tests
 *
 * Tests for the notification dropdown menu showing recent notifications.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen, fireEvent } from '@testing-library/react'
import { render } from '../../../test/test-utils'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import { ThemeProvider } from '@mui/material/styles'
import { cobraTheme } from '../../../theme/cobraTheme'
import { NotificationDropdown } from './NotificationDropdown'
import type { NotificationDto } from '../types'
import type { ReactNode } from 'react'

// Mock useNavigate
const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

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

// Create test query client
const createTestQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  })

// Wrapper component
const createWrapper = () => {
  const queryClient = createTestQueryClient()
  return ({ children }: { children: ReactNode }) => (
    <ThemeProvider theme={cobraTheme}>
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>{children}</MemoryRouter>
      </QueryClientProvider>
    </ThemeProvider>
  )
}

describe('NotificationDropdown', () => {
  const mockOnMarkAsRead = vi.fn()
  const mockOnMarkAllAsRead = vi.fn()
  const mockOnClose = vi.fn()

  const defaultProps = {
    notifications: [],
    isLoading: false,
    isError: false,
    onMarkAsRead: mockOnMarkAsRead,
    onMarkAllAsRead: mockOnMarkAllAsRead,
    onClose: mockOnClose,
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('Header', () => {
    it('shows "Notifications" header', () => {
      render(<NotificationDropdown {...defaultProps} />, { wrapper: createWrapper() })

      expect(screen.getByText('Notifications')).toBeInTheDocument()
    })

    it('shows "Mark all as read" button when there are unread notifications', () => {
      const notifications = [
        createMockNotification({ isRead: false }),
        createMockNotification({ id: 'notif-2', isRead: true }),
      ]

      render(<NotificationDropdown {...defaultProps} notifications={notifications} />, {
        wrapper: createWrapper(),
      })

      expect(screen.getByText('Mark all as read')).toBeInTheDocument()
    })

    it('hides "Mark all as read" button when all notifications are read', () => {
      const notifications = [
        createMockNotification({ isRead: true }),
        createMockNotification({ id: 'notif-2', isRead: true }),
      ]

      render(<NotificationDropdown {...defaultProps} notifications={notifications} />, {
        wrapper: createWrapper(),
      })

      expect(screen.queryByText('Mark all as read')).not.toBeInTheDocument()
    })

    it('hides "Mark all as read" button when there are no notifications', () => {
      render(<NotificationDropdown {...defaultProps} />, { wrapper: createWrapper() })

      expect(screen.queryByText('Mark all as read')).not.toBeInTheDocument()
    })
  })

  describe('Loading State', () => {
    it('shows loading state (CircularProgress) when isLoading is true', () => {
      render(<NotificationDropdown {...defaultProps} isLoading={true} />, {
        wrapper: createWrapper(),
      })

      expect(screen.getByRole('progressbar')).toBeInTheDocument()
    })

    it('does not show notification list when loading', () => {
      const notifications = [createMockNotification()]

      render(
        <NotificationDropdown
          {...defaultProps}
          notifications={notifications}
          isLoading={true}
        />,
        { wrapper: createWrapper() },
      )

      expect(screen.queryByText('Inject Ready')).not.toBeInTheDocument()
    })
  })

  describe('Error State', () => {
    it('shows error state (Alert) when isError is true', () => {
      render(<NotificationDropdown {...defaultProps} isError={true} />, {
        wrapper: createWrapper(),
      })

      expect(screen.getByText('Failed to load notifications')).toBeInTheDocument()
    })

    it('shows error alert with correct severity', () => {
      render(<NotificationDropdown {...defaultProps} isError={true} />, {
        wrapper: createWrapper(),
      })

      const alert = screen.getByRole('alert')
      expect(alert).toBeInTheDocument()
      expect(alert).toHaveClass('MuiAlert-standardError')
    })

    it('does not show notification list when error', () => {
      const notifications = [createMockNotification()]

      render(
        <NotificationDropdown
          {...defaultProps}
          notifications={notifications}
          isError={true}
        />,
        { wrapper: createWrapper() },
      )

      expect(screen.queryByText('Inject Ready')).not.toBeInTheDocument()
    })
  })

  describe('Empty State', () => {
    it('shows empty state with inbox icon when no notifications', () => {
      render(<NotificationDropdown {...defaultProps} />, { wrapper: createWrapper() })

      expect(screen.getByText('No notifications')).toBeInTheDocument()
    })

    it('shows inbox icon in empty state', () => {
      render(<NotificationDropdown {...defaultProps} />, { wrapper: createWrapper() })

      // FontAwesomeIcon renders as an svg
      const emptyStateBox = screen.getByText('No notifications').parentElement
      expect(emptyStateBox).toBeInTheDocument()
      expect(emptyStateBox?.querySelector('svg')).toBeInTheDocument()
    })

    it('does not show empty state when loading', () => {
      render(<NotificationDropdown {...defaultProps} isLoading={true} />, {
        wrapper: createWrapper(),
      })

      expect(screen.queryByText('No notifications')).not.toBeInTheDocument()
    })

    it('does not show empty state when error', () => {
      render(<NotificationDropdown {...defaultProps} isError={true} />, {
        wrapper: createWrapper(),
      })

      expect(screen.queryByText('No notifications')).not.toBeInTheDocument()
    })
  })

  describe('Notification List', () => {
    it('renders NotificationItem for each notification', () => {
      const notifications = [
        createMockNotification({ title: 'First Notification' }),
        createMockNotification({ id: 'notif-2', title: 'Second Notification' }),
        createMockNotification({ id: 'notif-3', title: 'Third Notification' }),
      ]

      render(<NotificationDropdown {...defaultProps} notifications={notifications} />, {
        wrapper: createWrapper(),
      })

      expect(screen.getByText('First Notification')).toBeInTheDocument()
      expect(screen.getByText('Second Notification')).toBeInTheDocument()
      expect(screen.getByText('Third Notification')).toBeInTheDocument()
    })

    it('shows notification messages', () => {
      const notifications = [
        createMockNotification({ message: 'This is a test message' }),
      ]

      render(<NotificationDropdown {...defaultProps} notifications={notifications} />, {
        wrapper: createWrapper(),
      })

      expect(screen.getByText('This is a test message')).toBeInTheDocument()
    })

    it('does not render list when loading', () => {
      const notifications = [createMockNotification()]

      render(
        <NotificationDropdown
          {...defaultProps}
          notifications={notifications}
          isLoading={true}
        />,
        { wrapper: createWrapper() },
      )

      expect(screen.queryByRole('list')).not.toBeInTheDocument()
    })

    it('does not render list when error', () => {
      const notifications = [createMockNotification()]

      render(
        <NotificationDropdown
          {...defaultProps}
          notifications={notifications}
          isError={true}
        />,
        { wrapper: createWrapper() },
      )

      expect(screen.queryByRole('list')).not.toBeInTheDocument()
    })
  })

  describe('Mark as Read Interaction', () => {
    it('calls onMarkAsRead when an unread notification is clicked', () => {
      const notifications = [createMockNotification({ isRead: false })]

      render(<NotificationDropdown {...defaultProps} notifications={notifications} />, {
        wrapper: createWrapper(),
      })

      const notificationItem = screen.getByText('Inject Ready')
      fireEvent.click(notificationItem)

      expect(mockOnMarkAsRead).toHaveBeenCalledWith('notif-1')
      expect(mockOnMarkAsRead).toHaveBeenCalledTimes(1)
    })

    it('does not call onMarkAsRead when a read notification is clicked', () => {
      const notifications = [createMockNotification({ isRead: true })]

      render(<NotificationDropdown {...defaultProps} notifications={notifications} />, {
        wrapper: createWrapper(),
      })

      const notificationItem = screen.getByText('Inject Ready')
      fireEvent.click(notificationItem)

      expect(mockOnMarkAsRead).not.toHaveBeenCalled()
    })

    it('calls onMarkAllAsRead when "Mark all as read" button is clicked', () => {
      const notifications = [createMockNotification({ isRead: false })]

      render(<NotificationDropdown {...defaultProps} notifications={notifications} />, {
        wrapper: createWrapper(),
      })

      const markAllButton = screen.getByText('Mark all as read')
      fireEvent.click(markAllButton)

      expect(mockOnMarkAllAsRead).toHaveBeenCalledTimes(1)
    })
  })

  describe('Navigation and Close', () => {
    it('navigates to actionUrl when notification with actionUrl is clicked', () => {
      const notifications = [
        createMockNotification({
          actionUrl: '/exercises/ex-123/conduct',
          isRead: true,
        }),
      ]

      render(<NotificationDropdown {...defaultProps} notifications={notifications} />, {
        wrapper: createWrapper(),
      })

      const notificationItem = screen.getByText('Inject Ready')
      fireEvent.click(notificationItem)

      expect(mockNavigate).toHaveBeenCalledWith('/exercises/ex-123/conduct')
    })

    it('calls onClose when navigating to an action URL', () => {
      const notifications = [
        createMockNotification({
          actionUrl: '/exercises/ex-123/conduct',
          isRead: true,
        }),
      ]

      render(<NotificationDropdown {...defaultProps} notifications={notifications} />, {
        wrapper: createWrapper(),
      })

      const notificationItem = screen.getByText('Inject Ready')
      fireEvent.click(notificationItem)

      expect(mockOnClose).toHaveBeenCalledTimes(1)
    })

    it('does not navigate when notification has no actionUrl', () => {
      const notifications = [
        createMockNotification({
          actionUrl: null,
          isRead: true,
        }),
      ]

      render(<NotificationDropdown {...defaultProps} notifications={notifications} />, {
        wrapper: createWrapper(),
      })

      const notificationItem = screen.getByText('Inject Ready')
      fireEvent.click(notificationItem)

      expect(mockNavigate).not.toHaveBeenCalled()
    })

    it('does not call onClose when notification has no actionUrl', () => {
      const notifications = [
        createMockNotification({
          actionUrl: null,
          isRead: true,
        }),
      ]

      render(<NotificationDropdown {...defaultProps} notifications={notifications} />, {
        wrapper: createWrapper(),
      })

      const notificationItem = screen.getByText('Inject Ready')
      fireEvent.click(notificationItem)

      expect(mockOnClose).not.toHaveBeenCalled()
    })

    it('marks as read AND navigates for unread notification with actionUrl', () => {
      const notifications = [
        createMockNotification({
          actionUrl: '/exercises/ex-123/conduct',
          isRead: false,
        }),
      ]

      render(<NotificationDropdown {...defaultProps} notifications={notifications} />, {
        wrapper: createWrapper(),
      })

      const notificationItem = screen.getByText('Inject Ready')
      fireEvent.click(notificationItem)

      expect(mockOnMarkAsRead).toHaveBeenCalledWith('notif-1')
      expect(mockNavigate).toHaveBeenCalledWith('/exercises/ex-123/conduct')
      expect(mockOnClose).toHaveBeenCalledTimes(1)
    })
  })

  describe('Visual States', () => {
    it('shows correct header structure', () => {
      render(<NotificationDropdown {...defaultProps} />, { wrapper: createWrapper() })

      const header = screen.getByText('Notifications').parentElement
      expect(header).toBeInTheDocument()
    })

    it('renders with correct width constraint', () => {
      const { container } = render(<NotificationDropdown {...defaultProps} />, {
        wrapper: createWrapper(),
      })

      const box = container.firstChild as HTMLElement
      expect(box).toHaveStyle({ width: '360px' })
    })
  })

  describe('Multiple Notifications', () => {
    it('handles multiple unread notifications correctly', () => {
      const notifications = [
        createMockNotification({ id: 'notif-1', isRead: false }),
        createMockNotification({ id: 'notif-2', isRead: false }),
        createMockNotification({ id: 'notif-3', isRead: false }),
      ]

      render(<NotificationDropdown {...defaultProps} notifications={notifications} />, {
        wrapper: createWrapper(),
      })

      expect(screen.getByText('Mark all as read')).toBeInTheDocument()
    })

    it('handles mixed read/unread notifications correctly', () => {
      const notifications = [
        createMockNotification({ id: 'notif-1', isRead: false }),
        createMockNotification({ id: 'notif-2', isRead: true }),
        createMockNotification({ id: 'notif-3', isRead: false }),
      ]

      render(<NotificationDropdown {...defaultProps} notifications={notifications} />, {
        wrapper: createWrapper(),
      })

      expect(screen.getByText('Mark all as read')).toBeInTheDocument()
    })

    it('can mark different notifications as read', () => {
      const notifications = [
        createMockNotification({ id: 'notif-1', title: 'First', isRead: false }),
        createMockNotification({ id: 'notif-2', title: 'Second', isRead: false }),
      ]

      render(<NotificationDropdown {...defaultProps} notifications={notifications} />, {
        wrapper: createWrapper(),
      })

      // Click first notification
      fireEvent.click(screen.getByText('First'))
      expect(mockOnMarkAsRead).toHaveBeenCalledWith('notif-1')

      // Click second notification
      fireEvent.click(screen.getByText('Second'))
      expect(mockOnMarkAsRead).toHaveBeenCalledWith('notif-2')

      expect(mockOnMarkAsRead).toHaveBeenCalledTimes(2)
    })
  })
})
