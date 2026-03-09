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
 * Narrow a potentially unknown cache value to NotificationsResponse.
 * Notification list queries store { items, totalCount, unreadCount }.
 * Unread-count query stores a plain number — we must not confuse them.
 */
function isNotificationsResponse(value: unknown): value is NotificationsResponse {
  return (
    typeof value === 'object' &&
    value !== null &&
    'items' in value &&
    Array.isArray((value as NotificationsResponse).items)
  )
}

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
      // Optimistic update - cancel any in-flight notification queries
      await queryClient.cancelQueries({ queryKey: NOTIFICATIONS_QUERY_KEY })

      // Snapshot ALL cache entries whose key starts with NOTIFICATIONS_QUERY_KEY
      // (this includes both list queries at various limits AND the unread-count query)
      const previousQueriesData = queryClient.getQueriesData<NotificationsResponse>({
        queryKey: NOTIFICATIONS_QUERY_KEY,
      })

      // Update all cached notification LIST queries regardless of their pagination params.
      // We guard with isNotificationsResponse() to skip the unread-count cache entry,
      // which shares the same key prefix but stores a plain number, not a response object.
      queryClient.setQueriesData<NotificationsResponse>(
        { queryKey: NOTIFICATIONS_QUERY_KEY },
        old => {
          if (!isNotificationsResponse(old)) return old
          return {
            ...old,
            items: old.items.map(n =>
              n.id === notificationId
                ? { ...n, isRead: true, readAt: new Date().toISOString() }
                : n,
            ),
            unreadCount: Math.max(0, old.unreadCount - 1),
          }
        },
      )

      // Update unread count separately
      const previousCount = queryClient.getQueryData<number>(UNREAD_COUNT_QUERY_KEY)
      if (previousCount !== undefined) {
        queryClient.setQueryData<number>(
          UNREAD_COUNT_QUERY_KEY,
          Math.max(0, previousCount - 1),
        )
      }

      return { previousQueriesData, previousCount }
    },
    onError: (_err, _variables, context) => {
      // Rollback on error - restore all notification query cache entries
      if (context?.previousQueriesData) {
        for (const [queryKey, data] of context.previousQueriesData) {
          queryClient.setQueryData(queryKey, data)
        }
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
      // Optimistic update - cancel in-flight queries
      await queryClient.cancelQueries({ queryKey: NOTIFICATIONS_QUERY_KEY })
      await queryClient.cancelQueries({ queryKey: UNREAD_COUNT_QUERY_KEY })

      // Snapshot ALL cache entries whose key starts with NOTIFICATIONS_QUERY_KEY
      const previousQueriesData = queryClient.getQueriesData<NotificationsResponse>({
        queryKey: NOTIFICATIONS_QUERY_KEY,
      })
      const previousCount = queryClient.getQueryData<number>(UNREAD_COUNT_QUERY_KEY)

      // Update all cached notification LIST queries regardless of their pagination params.
      // Guard with isNotificationsResponse() to skip the unread-count cache entry.
      queryClient.setQueriesData<NotificationsResponse>(
        { queryKey: NOTIFICATIONS_QUERY_KEY },
        old => {
          if (!isNotificationsResponse(old)) return old
          return {
            ...old,
            items: old.items.map(n => ({
              ...n,
              isRead: true,
              readAt: n.readAt || new Date().toISOString(),
            })),
            unreadCount: 0,
          }
        },
      )

      queryClient.setQueryData<number>(UNREAD_COUNT_QUERY_KEY, 0)

      return { previousQueriesData, previousCount }
    },
    onError: (_err, _variables, context) => {
      // Rollback on error - restore all notification query cache entries
      if (context?.previousQueriesData) {
        for (const [queryKey, data] of context.previousQueriesData) {
          queryClient.setQueryData(queryKey, data)
        }
      }
      if (context?.previousCount !== undefined) {
        queryClient.setQueryData(UNREAD_COUNT_QUERY_KEY, context.previousCount)
      }
    },
  })
}

/**
 * Add a new notification to the cache (for SignalR).
 * Updates all cached notification LIST queries regardless of their pagination params.
 */
export function addNotificationToCache(
  queryClient: ReturnType<typeof useQueryClient>,
  notification: NotificationDto,
) {
  // Update all cached notification list queries (any limit variant) using the base key.
  // Guard with isNotificationsResponse() to skip the unread-count cache entry.
  queryClient.setQueriesData<NotificationsResponse>(
    { queryKey: NOTIFICATIONS_QUERY_KEY },
    old => {
      if (!isNotificationsResponse(old)) return old
      // Prepend the new notification and cap each cached page at 10 items total.
      // We slice to 9 existing items so that new + 9 old = max 10 displayed.
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
