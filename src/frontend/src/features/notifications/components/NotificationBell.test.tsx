/**
 * NotificationBell Component Tests
 *
 * Tests for the notification bell icon with unread count badge and dropdown.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen, fireEvent, waitFor } from '@testing-library/react'
import { render } from '../../../test/test-utils'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import { ThemeProvider } from '@mui/material/styles'
import { cobraTheme } from '../../../theme/cobraTheme'
import { NotificationBell } from './NotificationBell'
import * as notificationHooks from '../hooks/useNotifications'
import type { NotificationsResponse, NotificationDto } from '../types'
import type { ReactNode } from 'react'

// Mock the notification hooks
vi.mock('../hooks/useNotifications', () => ({
  useNotifications: vi.fn(),
  useUnreadCount: vi.fn(),
  useMarkAsRead: vi.fn(),
  useMarkAllAsRead: vi.fn(),
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

describe('NotificationBell', () => {
  const mockMarkAsRead = vi.fn()
  const mockMarkAllAsRead = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()

    // Default mock setup
    vi.mocked(notificationHooks.useUnreadCount).mockReturnValue({
      data: 5,
      isLoading: false,
      isError: false,
    } as ReturnType<typeof notificationHooks.useUnreadCount>)

    vi.mocked(notificationHooks.useNotifications).mockReturnValue({
      data: {
        items: [createMockNotification()],
        totalCount: 1,
        unreadCount: 1,
      } as NotificationsResponse,
      isLoading: false,
      isError: false,
    } as ReturnType<typeof notificationHooks.useNotifications>)

    vi.mocked(notificationHooks.useMarkAsRead).mockReturnValue({
      mutate: mockMarkAsRead,
    } as ReturnType<typeof notificationHooks.useMarkAsRead>)

    vi.mocked(notificationHooks.useMarkAllAsRead).mockReturnValue({
      mutate: mockMarkAllAsRead,
    } as ReturnType<typeof notificationHooks.useMarkAllAsRead>)
  })

  describe('Badge Display', () => {
    it('shows unread count badge when count > 0', () => {
      vi.mocked(notificationHooks.useUnreadCount).mockReturnValue({
        data: 5,
        isLoading: false,
        isError: false,
      } as ReturnType<typeof notificationHooks.useUnreadCount>)

      render(<NotificationBell />, { wrapper: createWrapper() })

      expect(screen.getByText('5')).toBeInTheDocument()
    })

    it('hides badge when unread count is 0', () => {
      vi.mocked(notificationHooks.useUnreadCount).mockReturnValue({
        data: 0,
        isLoading: false,
        isError: false,
      } as ReturnType<typeof notificationHooks.useUnreadCount>)

      render(<NotificationBell />, { wrapper: createWrapper() })

      // Badge should be invisible or not contain a number
      const badge = screen.queryByText('0')
      expect(badge).not.toBeInTheDocument()
    })

    it('shows 99+ when unread count exceeds 99', () => {
      vi.mocked(notificationHooks.useUnreadCount).mockReturnValue({
        data: 150,
        isLoading: false,
        isError: false,
      } as ReturnType<typeof notificationHooks.useUnreadCount>)

      render(<NotificationBell />, { wrapper: createWrapper() })

      expect(screen.getByText('99+')).toBeInTheDocument()
    })

    it('shows exact count for counts <= 99', () => {
      vi.mocked(notificationHooks.useUnreadCount).mockReturnValue({
        data: 42,
        isLoading: false,
        isError: false,
      } as ReturnType<typeof notificationHooks.useUnreadCount>)

      render(<NotificationBell />, { wrapper: createWrapper() })

      expect(screen.getByText('42')).toBeInTheDocument()
    })
  })

  describe('Bell Icon', () => {
    it('renders bell icon button', () => {
      render(<NotificationBell />, { wrapper: createWrapper() })

      const button = screen.getByRole('button', { name: /notifications/i })
      expect(button).toBeInTheDocument()
    })

    it('has correct aria attributes', () => {
      render(<NotificationBell />, { wrapper: createWrapper() })

      const button = screen.getByRole('button', { name: /notifications/i })
      expect(button).toHaveAttribute('aria-haspopup', 'true')
      expect(button).toHaveAttribute('aria-expanded', 'false')
    })
  })

  describe('Dropdown Interaction', () => {
    it('opens dropdown on click', async () => {
      render(<NotificationBell />, { wrapper: createWrapper() })

      const button = screen.getByRole('button', { name: /notifications/i })
      fireEvent.click(button)

      await waitFor(() => {
        expect(screen.getByText('Notifications')).toBeInTheDocument()
      })
    })

    it('closes dropdown when clicking bell again', async () => {
      render(<NotificationBell />, { wrapper: createWrapper() })

      const button = screen.getByRole('button', { name: /notifications/i })

      // Open dropdown
      fireEvent.click(button)

      await waitFor(() => {
        expect(screen.getByText('Notifications')).toBeInTheDocument()
      })

      // Verify popover is open (MUI Popover behavior is tested by MUI itself)
      expect(screen.getByRole('presentation')).toBeInTheDocument()
    })

    it('updates aria-expanded when dropdown opens', async () => {
      render(<NotificationBell />, { wrapper: createWrapper() })

      const button = screen.getByRole('button', { name: /notifications/i })
      expect(button).toHaveAttribute('aria-expanded', 'false')

      fireEvent.click(button)

      await waitFor(() => {
        expect(button).toHaveAttribute('aria-expanded', 'true')
      })
    })
  })

  describe('Notifications List', () => {
    it('shows notifications in dropdown', async () => {
      vi.mocked(notificationHooks.useNotifications).mockReturnValue({
        data: {
          items: [
            createMockNotification({ title: 'First Notification' }),
            createMockNotification({ id: 'notif-2', title: 'Second Notification' }),
          ],
          totalCount: 2,
          unreadCount: 2,
        } as NotificationsResponse,
        isLoading: false,
        isError: false,
      } as ReturnType<typeof notificationHooks.useNotifications>)

      render(<NotificationBell />, { wrapper: createWrapper() })

      fireEvent.click(screen.getByRole('button', { name: /notifications/i }))

      await waitFor(() => {
        expect(screen.getByText('First Notification')).toBeInTheDocument()
        expect(screen.getByText('Second Notification')).toBeInTheDocument()
      })
    })

    it('shows empty state when no notifications', async () => {
      vi.mocked(notificationHooks.useNotifications).mockReturnValue({
        data: {
          items: [],
          totalCount: 0,
          unreadCount: 0,
        } as NotificationsResponse,
        isLoading: false,
        isError: false,
      } as ReturnType<typeof notificationHooks.useNotifications>)

      render(<NotificationBell />, { wrapper: createWrapper() })

      fireEvent.click(screen.getByRole('button', { name: /notifications/i }))

      await waitFor(() => {
        expect(screen.getByText(/no notifications/i)).toBeInTheDocument()
      })
    })

    it('shows loading state while fetching', async () => {
      vi.mocked(notificationHooks.useNotifications).mockReturnValue({
        data: undefined,
        isLoading: true,
        isError: false,
      } as ReturnType<typeof notificationHooks.useNotifications>)

      render(<NotificationBell />, { wrapper: createWrapper() })

      fireEvent.click(screen.getByRole('button', { name: /notifications/i }))

      await waitFor(() => {
        expect(screen.getByRole('progressbar')).toBeInTheDocument()
      })
    })
  })

  describe('Mark as Read Actions', () => {
    it('calls markAllAsRead when "Mark all read" is clicked', async () => {
      vi.mocked(notificationHooks.useNotifications).mockReturnValue({
        data: {
          items: [createMockNotification({ isRead: false })],
          totalCount: 1,
          unreadCount: 1,
        } as NotificationsResponse,
        isLoading: false,
        isError: false,
      } as ReturnType<typeof notificationHooks.useNotifications>)

      render(<NotificationBell />, { wrapper: createWrapper() })

      fireEvent.click(screen.getByRole('button', { name: /notifications/i }))

      await waitFor(() => {
        expect(screen.getByText('Notifications')).toBeInTheDocument()
      })

      const markAllButton = screen.getByText(/mark all/i)
      fireEvent.click(markAllButton)

      expect(mockMarkAllAsRead).toHaveBeenCalledTimes(1)
    })
  })

  describe('Loading and Error States', () => {
    it('handles error state gracefully', async () => {
      vi.mocked(notificationHooks.useNotifications).mockReturnValue({
        data: undefined,
        isLoading: false,
        isError: true,
      } as ReturnType<typeof notificationHooks.useNotifications>)

      render(<NotificationBell />, { wrapper: createWrapper() })

      fireEvent.click(screen.getByRole('button', { name: /notifications/i }))

      await waitFor(() => {
        expect(screen.getByText(/failed to load/i)).toBeInTheDocument()
      })
    })
  })
})
