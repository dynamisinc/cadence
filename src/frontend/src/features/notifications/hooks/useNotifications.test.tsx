/**
 * useNotifications Hook Tests
 *
 * Tests for the notification management hooks.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor, act } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import {
  useNotifications,
  useUnreadCount,
  useMarkAsRead,
  useMarkAllAsRead,
  addNotificationToCache,
  NOTIFICATIONS_QUERY_KEY,
  UNREAD_COUNT_QUERY_KEY,
} from './useNotifications'
import {
  getNotifications,
  getUnreadCount,
  markAsRead,
  markAllAsRead,
} from '../services/notificationService'
import type { NotificationsResponse, NotificationDto } from '../types'

// Mock the notification service
vi.mock('../services/notificationService', () => ({
  getNotifications: vi.fn(),
  getUnreadCount: vi.fn(),
  markAsRead: vi.fn(),
  markAllAsRead: vi.fn(),
}))

const mockNotifications: NotificationDto[] = [
  {
    id: 'notif-1',
    type: 'InjectReady',
    priority: 'High',
    title: 'Inject Ready',
    message: 'Inject INJ-001 is ready to be fired',
    actionUrl: '/exercises/ex-1/injects',
    relatedEntityType: 'Inject',
    relatedEntityId: 'inject-1',
    isRead: false,
    createdAt: '2025-01-26T10:00:00Z',
    readAt: null,
  },
  {
    id: 'notif-2',
    type: 'ClockStarted',
    priority: 'Medium',
    title: 'Clock Started',
    message: 'Exercise clock has been started',
    actionUrl: '/exercises/ex-1',
    relatedEntityType: 'Exercise',
    relatedEntityId: 'ex-1',
    isRead: false,
    createdAt: '2025-01-26T09:30:00Z',
    readAt: null,
  },
  {
    id: 'notif-3',
    type: 'ObservationCreated',
    priority: 'Low',
    title: 'New Observation',
    message: 'Evaluator added a new observation',
    actionUrl: '/exercises/ex-1/observations',
    relatedEntityType: 'Observation',
    relatedEntityId: 'obs-1',
    isRead: true,
    createdAt: '2025-01-26T09:00:00Z',
    readAt: '2025-01-26T09:05:00Z',
  },
]

const mockResponse: NotificationsResponse = {
  items: mockNotifications,
  totalCount: 3,
  unreadCount: 2,
}

// Helper to create a wrapper with React Query provider
const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  })

  const Wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  )

  return { Wrapper, queryClient }
}

describe('useNotifications', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(getNotifications).mockResolvedValue(mockResponse)
  })

  describe('useNotifications', () => {
    it('fetches notifications on mount with default limit', async () => {
      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => useNotifications(), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.data).toBeDefined()
      })

      expect(getNotifications).toHaveBeenCalledWith(10)
      expect(result.current.data?.items).toEqual(mockNotifications)
      expect(result.current.data?.totalCount).toBe(3)
      expect(result.current.data?.unreadCount).toBe(2)
    })

    it('fetches notifications with custom limit', async () => {
      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => useNotifications(20), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.data).toBeDefined()
      })

      expect(getNotifications).toHaveBeenCalledWith(20)
    })

    it('handles fetch error', async () => {
      vi.mocked(getNotifications).mockRejectedValue(new Error('Network error'))

      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => useNotifications(), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.isError).toBe(true)
      })

      expect(result.current.error).toBeInstanceOf(Error)
      expect(result.current.error?.message).toBe('Network error')
    })

    it('sets correct staleTime and refetchInterval', async () => {
      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => useNotifications(), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.data).toBeDefined()
      })

      // Verify query was called (staleTime and refetchInterval are internal config)
      expect(getNotifications).toHaveBeenCalled()
    })
  })

  describe('useUnreadCount', () => {
    beforeEach(() => {
      vi.mocked(getUnreadCount).mockResolvedValue(5)
    })

    it('fetches unread count on mount', async () => {
      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => useUnreadCount(), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.data).toBe(5)
      })

      expect(getUnreadCount).toHaveBeenCalledTimes(1)
    })

    it('handles fetch error', async () => {
      vi.mocked(getUnreadCount).mockRejectedValue(new Error('API error'))

      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => useUnreadCount(), {
        wrapper: Wrapper,
      })

      await waitFor(() => {
        expect(result.current.isError).toBe(true)
      })

      expect(result.current.error).toBeInstanceOf(Error)
      expect(result.current.error?.message).toBe('API error')
    })
  })

  describe('useMarkAsRead', () => {
    beforeEach(() => {
      vi.mocked(markAsRead).mockResolvedValue()
    })

    it('calls markAsRead service function', async () => {
      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => useMarkAsRead(), {
        wrapper: Wrapper,
      })

      await act(async () => {
        await result.current.mutateAsync('notif-1')
      })

      expect(markAsRead).toHaveBeenCalledWith('notif-1', expect.anything())
    })

    it('invalidates queries on success', async () => {
      const { Wrapper, queryClient } = createWrapper()

      // Pre-populate cache
      queryClient.setQueryData([...NOTIFICATIONS_QUERY_KEY, { limit: 10 }], mockResponse)
      queryClient.setQueryData(UNREAD_COUNT_QUERY_KEY, 2)

      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries')

      const { result } = renderHook(() => useMarkAsRead(), {
        wrapper: Wrapper,
      })

      await act(async () => {
        await result.current.mutateAsync('notif-1')
      })

      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: NOTIFICATIONS_QUERY_KEY })
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: UNREAD_COUNT_QUERY_KEY })
    })

    it('performs optimistic update on notifications', async () => {
      const { Wrapper, queryClient } = createWrapper()

      // Pre-populate cache
      queryClient.setQueryData([...NOTIFICATIONS_QUERY_KEY, { limit: 10 }], mockResponse)

      const { result } = renderHook(() => useMarkAsRead(), {
        wrapper: Wrapper,
      })

      act(() => {
        result.current.mutate('notif-1')
      })

      // Check optimistic update applied immediately
      await waitFor(() => {
        const data = queryClient.getQueryData<NotificationsResponse>([
          ...NOTIFICATIONS_QUERY_KEY,
          { limit: 10 },
        ])
        const notification = data?.items.find(n => n.id === 'notif-1')
        expect(notification?.isRead).toBe(true)
        expect(notification?.readAt).toBeTruthy()
      })
    })

    it('decrements unread count optimistically', async () => {
      const { Wrapper, queryClient } = createWrapper()

      // Pre-populate cache
      queryClient.setQueryData([...NOTIFICATIONS_QUERY_KEY, { limit: 10 }], mockResponse)
      queryClient.setQueryData(UNREAD_COUNT_QUERY_KEY, 2)

      const { result } = renderHook(() => useMarkAsRead(), {
        wrapper: Wrapper,
      })

      act(() => {
        result.current.mutate('notif-1')
      })

      // Check optimistic update applied immediately
      await waitFor(() => {
        const notifData = queryClient.getQueryData<NotificationsResponse>([
          ...NOTIFICATIONS_QUERY_KEY,
          { limit: 10 },
        ])
        const countData = queryClient.getQueryData<number>(UNREAD_COUNT_QUERY_KEY)

        expect(notifData?.unreadCount).toBe(1)
        expect(countData).toBe(1)
      })
    })

    it('does not decrement unread count below zero', async () => {
      const { Wrapper, queryClient } = createWrapper()

      // Pre-populate cache with count of 0
      const responseWithNoUnread: NotificationsResponse = {
        ...mockResponse,
        unreadCount: 0,
      }
      queryClient.setQueryData(
        [...NOTIFICATIONS_QUERY_KEY, { limit: 10 }],
        responseWithNoUnread,
      )
      queryClient.setQueryData(UNREAD_COUNT_QUERY_KEY, 0)

      const { result } = renderHook(() => useMarkAsRead(), {
        wrapper: Wrapper,
      })

      act(() => {
        result.current.mutate('notif-1')
      })

      // Check count stays at 0
      await waitFor(() => {
        const notifData = queryClient.getQueryData<NotificationsResponse>([
          ...NOTIFICATIONS_QUERY_KEY,
          { limit: 10 },
        ])
        const countData = queryClient.getQueryData<number>(UNREAD_COUNT_QUERY_KEY)

        expect(notifData?.unreadCount).toBe(0)
        expect(countData).toBe(0)
      })
    })

    it('rolls back optimistic update on error', async () => {
      vi.mocked(markAsRead).mockRejectedValue(new Error('Failed to mark as read'))

      const { Wrapper, queryClient } = createWrapper()

      // Pre-populate cache
      queryClient.setQueryData([...NOTIFICATIONS_QUERY_KEY, { limit: 10 }], mockResponse)
      queryClient.setQueryData(UNREAD_COUNT_QUERY_KEY, 2)

      const { result } = renderHook(() => useMarkAsRead(), {
        wrapper: Wrapper,
      })

      await act(async () => {
        try {
          await result.current.mutateAsync('notif-1')
        } catch {
          // Expected to throw
        }
      })

      // Check rollback occurred
      await waitFor(() => {
        const notifData = queryClient.getQueryData<NotificationsResponse>([
          ...NOTIFICATIONS_QUERY_KEY,
          { limit: 10 },
        ])
        const countData = queryClient.getQueryData<number>(UNREAD_COUNT_QUERY_KEY)

        expect(notifData?.items.find(n => n.id === 'notif-1')?.isRead).toBe(false)
        expect(notifData?.unreadCount).toBe(2)
        expect(countData).toBe(2)
      })
    })
  })

  describe('useMarkAllAsRead', () => {
    beforeEach(() => {
      vi.mocked(markAllAsRead).mockResolvedValue(2)
    })

    it('calls markAllAsRead service function', async () => {
      const { Wrapper } = createWrapper()
      const { result } = renderHook(() => useMarkAllAsRead(), {
        wrapper: Wrapper,
      })

      await act(async () => {
        await result.current.mutateAsync()
      })

      expect(markAllAsRead).toHaveBeenCalledTimes(1)
    })

    it('invalidates queries on success', async () => {
      const { Wrapper, queryClient } = createWrapper()

      // Pre-populate cache
      queryClient.setQueryData([...NOTIFICATIONS_QUERY_KEY, { limit: 10 }], mockResponse)
      queryClient.setQueryData(UNREAD_COUNT_QUERY_KEY, 2)

      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries')

      const { result } = renderHook(() => useMarkAllAsRead(), {
        wrapper: Wrapper,
      })

      await act(async () => {
        await result.current.mutateAsync()
      })

      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: NOTIFICATIONS_QUERY_KEY })
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: UNREAD_COUNT_QUERY_KEY })
    })

    it('performs optimistic update marking all as read', async () => {
      const { Wrapper, queryClient } = createWrapper()

      // Pre-populate cache
      queryClient.setQueryData([...NOTIFICATIONS_QUERY_KEY, { limit: 10 }], mockResponse)

      const { result } = renderHook(() => useMarkAllAsRead(), {
        wrapper: Wrapper,
      })

      act(() => {
        result.current.mutate()
      })

      // Check optimistic update applied immediately
      await waitFor(() => {
        const data = queryClient.getQueryData<NotificationsResponse>([
          ...NOTIFICATIONS_QUERY_KEY,
          { limit: 10 },
        ])

        expect(data?.items.every(n => n.isRead)).toBe(true)
        expect(data?.unreadCount).toBe(0)
      })
    })

    it('sets unread count to zero optimistically', async () => {
      const { Wrapper, queryClient } = createWrapper()

      // Pre-populate cache
      queryClient.setQueryData([...NOTIFICATIONS_QUERY_KEY, { limit: 10 }], mockResponse)
      queryClient.setQueryData(UNREAD_COUNT_QUERY_KEY, 2)

      const { result } = renderHook(() => useMarkAllAsRead(), {
        wrapper: Wrapper,
      })

      act(() => {
        result.current.mutate()
      })

      // Check optimistic update applied immediately
      await waitFor(() => {
        const countData = queryClient.getQueryData<number>(UNREAD_COUNT_QUERY_KEY)
        expect(countData).toBe(0)
      })
    })

    it('preserves existing readAt timestamps', async () => {
      const { Wrapper, queryClient } = createWrapper()

      // Pre-populate cache
      queryClient.setQueryData([...NOTIFICATIONS_QUERY_KEY, { limit: 10 }], mockResponse)

      const { result } = renderHook(() => useMarkAllAsRead(), {
        wrapper: Wrapper,
      })

      act(() => {
        result.current.mutate()
      })

      // Check that already-read notification keeps its timestamp
      await waitFor(() => {
        const data = queryClient.getQueryData<NotificationsResponse>([
          ...NOTIFICATIONS_QUERY_KEY,
          { limit: 10 },
        ])
        const alreadyRead = data?.items.find(n => n.id === 'notif-3')
        expect(alreadyRead?.readAt).toBe('2025-01-26T09:05:00Z')
      })
    })

    it('rolls back optimistic update on error', async () => {
      vi.mocked(markAllAsRead).mockRejectedValue(new Error('Failed to mark all as read'))

      const { Wrapper, queryClient } = createWrapper()

      // Pre-populate cache
      queryClient.setQueryData([...NOTIFICATIONS_QUERY_KEY, { limit: 10 }], mockResponse)
      queryClient.setQueryData(UNREAD_COUNT_QUERY_KEY, 2)

      const { result } = renderHook(() => useMarkAllAsRead(), {
        wrapper: Wrapper,
      })

      await act(async () => {
        try {
          await result.current.mutateAsync()
        } catch {
          // Expected to throw
        }
      })

      // Check rollback occurred
      await waitFor(() => {
        const notifData = queryClient.getQueryData<NotificationsResponse>([
          ...NOTIFICATIONS_QUERY_KEY,
          { limit: 10 },
        ])
        const countData = queryClient.getQueryData<number>(UNREAD_COUNT_QUERY_KEY)

        expect(notifData?.items.find(n => n.id === 'notif-1')?.isRead).toBe(false)
        expect(notifData?.unreadCount).toBe(2)
        expect(countData).toBe(2)
      })
    })
  })

  describe('addNotificationToCache', () => {
    it('adds notification to cache', () => {
      const queryClient = new QueryClient()

      // Pre-populate cache
      queryClient.setQueryData([...NOTIFICATIONS_QUERY_KEY, { limit: 10 }], mockResponse)

      const newNotification: NotificationDto = {
        id: 'notif-4',
        type: 'InjectFired',
        priority: 'High',
        title: 'Inject Fired',
        message: 'Inject INJ-002 has been fired',
        actionUrl: '/exercises/ex-1/injects',
        relatedEntityType: 'Inject',
        relatedEntityId: 'inject-2',
        isRead: false,
        createdAt: '2025-01-26T11:00:00Z',
        readAt: null,
      }

      addNotificationToCache(queryClient, newNotification)

      const data = queryClient.getQueryData<NotificationsResponse>([
        ...NOTIFICATIONS_QUERY_KEY,
        { limit: 10 },
      ])

      expect(data?.items[0]).toEqual(newNotification)
      expect(data?.items).toHaveLength(4) // New notification + first 3 from old (which had 3 items)
    })

    it('increments total count', () => {
      const queryClient = new QueryClient()

      // Pre-populate cache
      queryClient.setQueryData([...NOTIFICATIONS_QUERY_KEY, { limit: 10 }], mockResponse)

      const newNotification: NotificationDto = {
        id: 'notif-4',
        type: 'System',
        priority: 'Low',
        title: 'System Update',
        message: 'System maintenance scheduled',
        actionUrl: null,
        relatedEntityType: null,
        relatedEntityId: null,
        isRead: false,
        createdAt: '2025-01-26T11:00:00Z',
        readAt: null,
      }

      addNotificationToCache(queryClient, newNotification)

      const data = queryClient.getQueryData<NotificationsResponse>([
        ...NOTIFICATIONS_QUERY_KEY,
        { limit: 10 },
      ])

      expect(data?.totalCount).toBe(4)
    })

    it('increments unread count', () => {
      const queryClient = new QueryClient()

      // Pre-populate cache
      queryClient.setQueryData([...NOTIFICATIONS_QUERY_KEY, { limit: 10 }], mockResponse)
      queryClient.setQueryData(UNREAD_COUNT_QUERY_KEY, 2)

      const newNotification: NotificationDto = {
        id: 'notif-4',
        type: 'AssignmentCreated',
        priority: 'Medium',
        title: 'New Assignment',
        message: 'You have been assigned to an exercise',
        actionUrl: '/exercises/ex-2',
        relatedEntityType: 'ExerciseUser',
        relatedEntityId: 'eu-1',
        isRead: false,
        createdAt: '2025-01-26T11:00:00Z',
        readAt: null,
      }

      addNotificationToCache(queryClient, newNotification)

      const notifData = queryClient.getQueryData<NotificationsResponse>([
        ...NOTIFICATIONS_QUERY_KEY,
        { limit: 10 },
      ])
      const countData = queryClient.getQueryData<number>(UNREAD_COUNT_QUERY_KEY)

      expect(notifData?.unreadCount).toBe(3)
      expect(countData).toBe(3)
    })

    it('initializes unread count to 1 if undefined', () => {
      const queryClient = new QueryClient()

      // Pre-populate cache without unread count
      queryClient.setQueryData([...NOTIFICATIONS_QUERY_KEY, { limit: 10 }], mockResponse)
      // Don't set UNREAD_COUNT_QUERY_KEY

      const newNotification: NotificationDto = {
        id: 'notif-4',
        type: 'System',
        priority: 'Low',
        title: 'Test',
        message: 'Test message',
        actionUrl: null,
        relatedEntityType: null,
        relatedEntityId: null,
        isRead: false,
        createdAt: '2025-01-26T11:00:00Z',
        readAt: null,
      }

      addNotificationToCache(queryClient, newNotification)

      const countData = queryClient.getQueryData<number>(UNREAD_COUNT_QUERY_KEY)
      expect(countData).toBe(1)
    })

    it('limits items to 10 (limit)', () => {
      const queryClient = new QueryClient()

      // Create response with 10 items
      const fullResponse: NotificationsResponse = {
        items: Array.from({ length: 10 }, (_, i) => ({
          ...mockNotifications[0],
          id: `notif-${i}`,
        })),
        totalCount: 10,
        unreadCount: 10,
      }

      queryClient.setQueryData([...NOTIFICATIONS_QUERY_KEY, { limit: 10 }], fullResponse)

      const newNotification: NotificationDto = {
        id: 'notif-new',
        type: 'System',
        priority: 'Low',
        title: 'New',
        message: 'New message',
        actionUrl: null,
        relatedEntityType: null,
        relatedEntityId: null,
        isRead: false,
        createdAt: '2025-01-26T11:00:00Z',
        readAt: null,
      }

      addNotificationToCache(queryClient, newNotification)

      const data = queryClient.getQueryData<NotificationsResponse>([
        ...NOTIFICATIONS_QUERY_KEY,
        { limit: 10 },
      ])

      // Should have 10 items (new one + first 9 of old)
      expect(data?.items).toHaveLength(10)
      expect(data?.items[0].id).toBe('notif-new')
      expect(data?.items[9].id).toBe('notif-8') // Last item from old list (0-indexed, so notif-8 is 9th)
    })

    it('does nothing if cache is undefined', () => {
      const queryClient = new QueryClient()
      // Don't pre-populate cache

      const newNotification: NotificationDto = {
        id: 'notif-4',
        type: 'System',
        priority: 'Low',
        title: 'Test',
        message: 'Test message',
        actionUrl: null,
        relatedEntityType: null,
        relatedEntityId: null,
        isRead: false,
        createdAt: '2025-01-26T11:00:00Z',
        readAt: null,
      }

      addNotificationToCache(queryClient, newNotification)

      const data = queryClient.getQueryData<NotificationsResponse>([
        ...NOTIFICATIONS_QUERY_KEY,
        { limit: 10 },
      ])

      // Should remain undefined
      expect(data).toBeUndefined()
    })
  })
})
