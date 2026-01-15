/**
 * ConnectivityContext
 *
 * Provides global connectivity state management including:
 * - Browser online/offline status
 * - SignalR connection state (when in exercise conduct)
 * - Pending sync count
 * - Toast notifications for connection changes
 *
 * Usage:
 * ```tsx
 * const { isOnline, connectionState, pendingCount } = useConnectivity();
 * ```
 */

import React, {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
  useMemo,
  type ReactNode,
} from 'react'
import { toast } from 'react-toastify'

/** Combined connectivity state */
export type ConnectivityState =
  | 'online'        // Browser online, SignalR connected (or not in exercise)
  | 'connecting'    // Attempting to connect
  | 'reconnecting'  // Lost connection, attempting to reconnect
  | 'offline'       // Browser offline or SignalR disconnected

/** SignalR connection state from useExerciseSignalR */
export type SignalRState =
  | 'disconnected'
  | 'connecting'
  | 'connected'
  | 'reconnecting'
  | 'error'

interface ConnectivityContextValue {
  /** Whether the browser reports being online */
  isOnline: boolean
  /** Combined connectivity state */
  connectivityState: ConnectivityState
  /** Current SignalR state (if in exercise) */
  signalRState: SignalRState | null
  /** Whether we're in an exercise context */
  isInExercise: boolean
  /** Number of pending offline actions */
  pendingCount: number
  /** Update SignalR state (called by useExerciseSignalR) */
  setSignalRState: (state: SignalRState | null) => void
  /** Set whether we're in an exercise context */
  setIsInExercise: (inExercise: boolean) => void
  /** Update pending count */
  setPendingCount: (count: number) => void
  /** Increment pending count */
  incrementPendingCount: () => void
  /** Decrement pending count */
  decrementPendingCount: () => void
}

const ConnectivityContext = createContext<ConnectivityContextValue | null>(null)

interface ConnectivityProviderProps {
  children: ReactNode
}

export const ConnectivityProvider: React.FC<ConnectivityProviderProps> = ({ children }) => {
  const [isOnline, setIsOnline] = useState<boolean>(
    typeof navigator !== 'undefined' ? navigator.onLine : true
  )
  const [signalRState, setSignalRStateInternal] = useState<SignalRState | null>(null)
  const [isInExercise, setIsInExercise] = useState(false)
  const [pendingCount, setPendingCountInternal] = useState(0)
  const [hasShownOfflineToast, setHasShownOfflineToast] = useState(false)

  // Calculate combined connectivity state
  const connectivityState = useMemo((): ConnectivityState => {
    // Browser offline takes precedence
    if (!isOnline) {
      return 'offline'
    }

    // If not in exercise, just check browser status
    if (!isInExercise || signalRState === null) {
      return 'online'
    }

    // Map SignalR state to connectivity state
    switch (signalRState) {
      case 'connected':
        return 'online'
      case 'connecting':
        return 'connecting'
      case 'reconnecting':
        return 'reconnecting'
      case 'disconnected':
      case 'error':
        return 'offline'
      default:
        return 'online'
    }
  }, [isOnline, isInExercise, signalRState])

  // Handle browser online/offline events
  useEffect(() => {
    const handleOnline = () => {
      setIsOnline(true)
      setHasShownOfflineToast(false)
      toast.success('🟢 Connection restored', {
        autoClose: 3000,
      })
    }

    const handleOffline = () => {
      setIsOnline(false)
      if (!hasShownOfflineToast) {
        toast.error('🔴 You are offline. Changes will sync when connection restores.', {
          autoClose: 5000,
        })
        setHasShownOfflineToast(true)
      }
    }

    window.addEventListener('online', handleOnline)
    window.addEventListener('offline', handleOffline)

    return () => {
      window.removeEventListener('online', handleOnline)
      window.removeEventListener('offline', handleOffline)
    }
  }, [hasShownOfflineToast])

  // Show toast when SignalR state changes (only when in exercise)
  const setSignalRState = useCallback((state: SignalRState | null) => {
    setSignalRStateInternal((prevState) => {
      // Only show toasts if state actually changed and we're in an exercise
      if (isInExercise && prevState !== state && prevState !== null) {
        if (state === 'connected' && (prevState === 'disconnected' || prevState === 'reconnecting' || prevState === 'error')) {
          toast.success('🟢 Real-time connection restored', {
            autoClose: 3000,
          })
        } else if ((state === 'disconnected' || state === 'error') && prevState === 'connected') {
          toast.warning('🟡 Real-time connection lost. Attempting to reconnect...', {
            autoClose: 4000,
          })
        }
      }
      return state
    })
  }, [isInExercise])

  const setPendingCount = useCallback((count: number) => {
    setPendingCountInternal(Math.max(0, count))
  }, [])

  const incrementPendingCount = useCallback(() => {
    setPendingCountInternal((prev) => prev + 1)
  }, [])

  const decrementPendingCount = useCallback(() => {
    setPendingCountInternal((prev) => Math.max(0, prev - 1))
  }, [])

  const value = useMemo(
    () => ({
      isOnline,
      connectivityState,
      signalRState,
      isInExercise,
      pendingCount,
      setSignalRState,
      setIsInExercise,
      setPendingCount,
      incrementPendingCount,
      decrementPendingCount,
    }),
    [
      isOnline,
      connectivityState,
      signalRState,
      isInExercise,
      pendingCount,
      setSignalRState,
      setPendingCount,
      incrementPendingCount,
      decrementPendingCount,
    ]
  )

  return (
    <ConnectivityContext.Provider value={value}>
      {children}
    </ConnectivityContext.Provider>
  )
}

/**
 * Hook to access connectivity state
 */
export const useConnectivity = (): ConnectivityContextValue => {
  const context = useContext(ConnectivityContext)
  if (!context) {
    throw new Error('useConnectivity must be used within a ConnectivityProvider')
  }
  return context
}

export default ConnectivityContext
