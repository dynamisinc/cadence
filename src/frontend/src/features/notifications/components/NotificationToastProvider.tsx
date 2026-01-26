/**
 * NotificationToastProvider Component
 *
 * Provides toast notification functionality to the app.
 * Should be placed at the app root.
 */
import { createContext, useContext, useCallback, type ReactNode } from 'react'
import { Box, Portal } from '@mui/material'
import type { NotificationDto, NotificationPriority } from '../types'
import { useNotificationToast } from '../hooks/useNotificationToast'
import { NotificationToast } from './NotificationToast'

interface NotificationToastContextType {
  showToast: (notification: NotificationDto) => void
  clearAll: () => void
}

const NotificationToastContext = createContext<NotificationToastContextType | null>(null)

/**
 * Hook to access toast functions.
 */
export function useToast() {
  const context = useContext(NotificationToastContext)
  if (!context) {
    throw new Error('useToast must be used within NotificationToastProvider')
  }
  return context
}

interface NotificationToastProviderProps {
  children: ReactNode
}

export function NotificationToastProvider({ children }: NotificationToastProviderProps) {
  const {
    toasts,
    addToast,
    removeToast,
    pauseAutoDismiss,
    resumeAutoDismiss,
    clearAll,
  } = useNotificationToast()

  const showToast = useCallback(
    (notification: NotificationDto) => {
      addToast(notification)
    },
    [addToast],
  )

  const handleMouseEnter = useCallback(
    (toastId: string) => {
      pauseAutoDismiss(toastId)
    },
    [pauseAutoDismiss],
  )

  const handleMouseLeave = useCallback(
    (toastId: string, priority: NotificationPriority) => {
      resumeAutoDismiss(toastId, priority)
    },
    [resumeAutoDismiss],
  )

  return (
    <NotificationToastContext.Provider value={{ showToast, clearAll }}>
      {children}

      {/* Toast Container */}
      <Portal>
        <Box
          sx={{
            position: 'fixed',
            bottom: 24,
            right: 24,
            zIndex: 9999,
            display: 'flex',
            flexDirection: 'column-reverse',
            alignItems: 'flex-end',
          }}
        >
          {toasts.map((toast) => (
            <NotificationToast
              key={toast.id}
              toast={toast}
              onDismiss={removeToast}
              onMouseEnter={handleMouseEnter}
              onMouseLeave={handleMouseLeave}
            />
          ))}
        </Box>
      </Portal>
    </NotificationToastContext.Provider>
  )
}
