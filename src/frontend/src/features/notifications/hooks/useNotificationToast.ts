/**
 * useNotificationToast Hook
 *
 * Manages toast notifications based on priority.
 */
import { useState, useCallback, useRef, useEffect } from 'react'
import type { NotificationDto, NotificationPriority, Toast, ToastConfig } from '../types'

/** Maximum number of visible toasts */
const MAX_VISIBLE_TOASTS = 3

/** Toast configuration by priority */
const TOAST_CONFIG: Record<NotificationPriority, ToastConfig> = {
  High: {
    showToast: true,
    autoDismissMs: null, // Never auto-dismiss
    backgroundColor: '#fff3e0',
    borderColor: '#ff9800',
  },
  Medium: {
    showToast: true,
    autoDismissMs: 10000, // 10 seconds
    backgroundColor: '#e3f2fd',
    borderColor: '#2196f3',
  },
  Low: {
    showToast: false, // Bell only
    autoDismissMs: 5000, // 5 seconds
    backgroundColor: '#f5f5f5',
    borderColor: '#9e9e9e',
  },
}

/**
 * Get toast configuration for a priority.
 */
export function getToastConfig(priority: NotificationPriority): ToastConfig {
  return TOAST_CONFIG[priority]
}

/**
 * Hook for managing toast notifications.
 */
export function useNotificationToast() {
  const [toasts, setToasts] = useState<Toast[]>([])
  const timerRefs = useRef<Map<string, NodeJS.Timeout>>(new Map())

  // Clean up timers on unmount
  useEffect(() => {
    return () => {
      timerRefs.current.forEach((timer) => clearTimeout(timer))
      timerRefs.current.clear()
    }
  }, [])

  // Add a toast
  const addToast = useCallback((notification: NotificationDto) => {
    const config = getToastConfig(notification.priority)

    // Skip Low priority notifications (bell only)
    if (!config.showToast) return

    const toast: Toast = {
      id: notification.id + '-' + Date.now(),
      notification,
      createdAt: new Date(),
    }

    setToasts((prev) => {
      // Limit to MAX_VISIBLE_TOASTS, removing oldest
      const newToasts = [toast, ...prev].slice(0, MAX_VISIBLE_TOASTS)
      return newToasts
    })

    // Set auto-dismiss timer if applicable
    if (config.autoDismissMs !== null) {
      const timer = setTimeout(() => {
        removeToast(toast.id)
      }, config.autoDismissMs)
      timerRefs.current.set(toast.id, timer)
    }
  }, [])

  // Remove a toast
  const removeToast = useCallback((toastId: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== toastId))

    // Clear timer if exists
    const timer = timerRefs.current.get(toastId)
    if (timer) {
      clearTimeout(timer)
      timerRefs.current.delete(toastId)
    }
  }, [])

  // Pause auto-dismiss (on hover)
  const pauseAutoDismiss = useCallback((toastId: string) => {
    const timer = timerRefs.current.get(toastId)
    if (timer) {
      clearTimeout(timer)
      timerRefs.current.delete(toastId)
    }
  }, [])

  // Resume auto-dismiss (on mouse leave)
  const resumeAutoDismiss = useCallback((toastId: string, priority: NotificationPriority) => {
    const config = getToastConfig(priority)
    if (config.autoDismissMs === null) return

    const timer = setTimeout(() => {
      removeToast(toastId)
    }, config.autoDismissMs)
    timerRefs.current.set(toastId, timer)
  }, [removeToast])

  // Clear all toasts
  const clearAll = useCallback(() => {
    timerRefs.current.forEach((timer) => clearTimeout(timer))
    timerRefs.current.clear()
    setToasts([])
  }, [])

  return {
    toasts,
    addToast,
    removeToast,
    pauseAutoDismiss,
    resumeAutoDismiss,
    clearAll,
  }
}
