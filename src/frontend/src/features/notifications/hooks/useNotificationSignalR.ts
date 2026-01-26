/**
 * useNotificationSignalR Hook
 *
 * SignalR subscription for real-time notifications.
 */
import { useEffect, useCallback } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import type { HubConnection } from '@microsoft/signalr'
import type { NotificationDto } from '../types'
import { addNotificationToCache } from './useNotifications'

interface UseNotificationSignalROptions {
  /** SignalR connection instance */
  connection: HubConnection | null
  /** User ID for subscribing to personal notifications */
  userId: string | null
  /** Callback when a notification is received (for toasts) */
  onNotificationReceived?: (notification: NotificationDto) => void
}

/**
 * Hook to subscribe to SignalR notifications.
 */
export function useNotificationSignalR({
  connection,
  userId,
  onNotificationReceived,
}: UseNotificationSignalROptions) {
  const queryClient = useQueryClient()

  // Handle incoming notification
  const handleNotification = useCallback(
    (notification: NotificationDto) => {
      // Add to cache
      addNotificationToCache(queryClient, notification)

      // Trigger callback (for toast display)
      onNotificationReceived?.(notification)
    },
    [queryClient, onNotificationReceived],
  )

  // Subscribe to user group and notification events
  useEffect(() => {
    if (!connection || !userId) return

    // Join user-specific group for notifications
    connection.invoke('JoinUserGroup', userId).catch(err => {
      console.error('Failed to join user group:', err)
    })

    // Subscribe to notification events
    connection.on('NotificationCreated', handleNotification)

    return () => {
      // Unsubscribe
      connection.off('NotificationCreated', handleNotification)

      // Leave user group
      connection.invoke('LeaveUserGroup', userId).catch(err => {
        console.error('Failed to leave user group:', err)
      })
    }
  }, [connection, userId, handleNotification])
}
