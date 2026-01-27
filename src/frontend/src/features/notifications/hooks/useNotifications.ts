/**
 * useNotifications Hook
 *
 * React Query hook for fetching and managing notifications.
 */
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getNotifications,
  getUnreadCount,
  markAsRead,
  markAllAsRead,
} from '../services/notificationService'
import type { NotificationsResponse, NotificationDto } from '../types'

/** Query keys */
export const NOTIFICATIONS_QUERY_KEY = ['notifications']
export const UNREAD_COUNT_QUERY_KEY = ['notifications', 'unread-count']

/**
 * Hook to fetch notifications.
 */
export function useNotifications(limit = 10) {
  return useQuery<NotificationsResponse, Error>({
    queryKey: [...NOTIFICATIONS_QUERY_KEY, { limit }],
    queryFn: () => getNotifications(limit),
    staleTime: 1000 * 30, // 30 seconds
    refetchInterval: 1000 * 60, // Refetch every minute
  })
}

/**
 * Hook to fetch unread count.
 */
export function useUnreadCount() {
  return useQuery<number, Error>({
    queryKey: UNREAD_COUNT_QUERY_KEY,
    queryFn: getUnreadCount,
    staleTime: 1000 * 30, // 30 seconds
    refetchInterval: 1000 * 60, // Refetch every minute
  })
}

/**
 * Hook to mark a notification as read.
 */
export function useMarkAsRead() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: markAsRead,
    onSuccess: () => {
      // Invalidate both queries to refresh data
      queryClient.invalidateQueries({ queryKey: NOTIFICATIONS_QUERY_KEY })
      queryClient.invalidateQueries({ queryKey: UNREAD_COUNT_QUERY_KEY })
    },
    onMutate: async (notificationId: string) => {
      // Optimistic update
      await queryClient.cancelQueries({ queryKey: NOTIFICATIONS_QUERY_KEY })

      const previousNotifications = queryClient.getQueryData<NotificationsResponse>(
        [...NOTIFICATIONS_QUERY_KEY, { limit: 10 }],
      )

      if (previousNotifications) {
        queryClient.setQueryData<NotificationsResponse>(
          [...NOTIFICATIONS_QUERY_KEY, { limit: 10 }],
          {
            ...previousNotifications,
            items: previousNotifications.items.map(n =>
              n.id === notificationId
                ? { ...n, isRead: true, readAt: new Date().toISOString() }
                : n,
            ),
            unreadCount: Math.max(0, previousNotifications.unreadCount - 1),
          },
        )
      }

      // Update unread count
      const previousCount = queryClient.getQueryData<number>(UNREAD_COUNT_QUERY_KEY)
      if (previousCount !== undefined) {
        queryClient.setQueryData<number>(
          UNREAD_COUNT_QUERY_KEY,
          Math.max(0, previousCount - 1),
        )
      }

      return { previousNotifications, previousCount }
    },
    onError: (_err, _variables, context) => {
      // Rollback on error
      if (context?.previousNotifications) {
        queryClient.setQueryData(
          [...NOTIFICATIONS_QUERY_KEY, { limit: 10 }],
          context.previousNotifications,
        )
      }
      if (context?.previousCount !== undefined) {
        queryClient.setQueryData(UNREAD_COUNT_QUERY_KEY, context.previousCount)
      }
    },
  })
}

/**
 * Hook to mark all notifications as read.
 */
export function useMarkAllAsRead() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: markAllAsRead,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: NOTIFICATIONS_QUERY_KEY })
      queryClient.invalidateQueries({ queryKey: UNREAD_COUNT_QUERY_KEY })
    },
    onMutate: async () => {
      // Optimistic update
      await queryClient.cancelQueries({ queryKey: NOTIFICATIONS_QUERY_KEY })
      await queryClient.cancelQueries({ queryKey: UNREAD_COUNT_QUERY_KEY })

      const previousNotifications = queryClient.getQueryData<NotificationsResponse>(
        [...NOTIFICATIONS_QUERY_KEY, { limit: 10 }],
      )

      if (previousNotifications) {
        queryClient.setQueryData<NotificationsResponse>(
          [...NOTIFICATIONS_QUERY_KEY, { limit: 10 }],
          {
            ...previousNotifications,
            items: previousNotifications.items.map(n => ({
              ...n,
              isRead: true,
              readAt: n.readAt || new Date().toISOString(),
            })),
            unreadCount: 0,
          },
        )
      }

      queryClient.setQueryData<number>(UNREAD_COUNT_QUERY_KEY, 0)

      return { previousNotifications }
    },
    onError: (_err, _variables, context) => {
      if (context?.previousNotifications) {
        queryClient.setQueryData(
          [...NOTIFICATIONS_QUERY_KEY, { limit: 10 }],
          context.previousNotifications,
        )
        queryClient.setQueryData(
          UNREAD_COUNT_QUERY_KEY,
          context.previousNotifications.unreadCount,
        )
      }
    },
  })
}

/**
 * Add a new notification to the cache (for SignalR).
 */
export function addNotificationToCache(
  queryClient: ReturnType<typeof useQueryClient>,
  notification: NotificationDto,
) {
  // Update notifications list
  queryClient.setQueryData<NotificationsResponse>(
    [...NOTIFICATIONS_QUERY_KEY, { limit: 10 }],
    old => {
      if (!old) return old
      return {
        ...old,
        items: [notification, ...old.items.slice(0, 9)],
        totalCount: old.totalCount + 1,
        unreadCount: old.unreadCount + 1,
      }
    },
  )

  // Update unread count
  queryClient.setQueryData<number>(UNREAD_COUNT_QUERY_KEY, old =>
    old !== undefined ? old + 1 : 1,
  )
}
