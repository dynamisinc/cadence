/**
 * ConnectivityContext
 *
 * Provides global connectivity state management including:
 * - Browser online/offline status
 * - API server reachability (via health check)
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
  useRef,
  type ReactNode,
} from 'react'
import { toast } from 'react-toastify'
import { checkApiHealth } from '../services/api'

/** How often to check API health when browser reports online (ms) */
const HEALTH_CHECK_INTERVAL = 10000 // 10 seconds

/** How often to check API health when offline (faster retry) */
const HEALTH_CHECK_INTERVAL_OFFLINE = 5000 // 5 seconds

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
  /** Whether the API server is reachable */
  isApiReachable: boolean
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
  /** Manually trigger a health check */
  checkHealth: () => Promise<boolean>
}

const ConnectivityContext = createContext<ConnectivityContextValue | null>(null)

interface ConnectivityProviderProps {
  children: ReactNode
}

export const ConnectivityProvider: React.FC<ConnectivityProviderProps> = ({ children }) => {
  const [isOnline, setIsOnline] = useState<boolean>(
    typeof navigator !== 'undefined' ? navigator.onLine : true,
  )
  const [isApiReachable, setIsApiReachable] = useState<boolean>(true) // Assume online initially
  const [signalRState, setSignalRStateInternal] = useState<SignalRState | null>(null)
  const [isInExercise, setIsInExercise] = useState(false)
  const [pendingCount, setPendingCountInternal] = useState(0)
  const [hasShownOfflineToast, setHasShownOfflineToast] = useState(false)
  const healthCheckIntervalRef = useRef<ReturnType<typeof setInterval> | null>(null)
  const isMountedRef = useRef(true)

  // Perform health check
  const performHealthCheck = useCallback(async (): Promise<boolean> => {
    // Skip health check if browser reports offline
    if (!navigator.onLine) {
      if (isMountedRef.current) {
        setIsApiReachable(false)
      }
      return false
    }

    const isHealthy = await checkApiHealth()

    // Don't update state if component is unmounted
    if (!isMountedRef.current) {
      return isHealthy
    }

    const wasReachable = isApiReachable

    setIsApiReachable(isHealthy)

    // Show toast when API becomes unreachable (only if we thought we were online)
    if (!isHealthy && wasReachable && !hasShownOfflineToast) {
      toast.error('🔴 Cannot reach server. Changes will sync when connection restores.', {
        toastId: 'connectivity-api-offline',
        autoClose: 5000,
      })
      setHasShownOfflineToast(true)
    }

    // Show toast when API becomes reachable again
    if (isHealthy && !wasReachable) {
      toast.success('🟢 Server connection restored', {
        toastId: 'connectivity-api-online',
        autoClose: 3000,
      })
      setHasShownOfflineToast(false)
    }

    return isHealthy
  }, [isApiReachable, hasShownOfflineToast])

  // Start/restart health check interval
  const startHealthCheckInterval = useCallback((interval: number) => {
    if (healthCheckIntervalRef.current) {
      clearInterval(healthCheckIntervalRef.current)
    }
    healthCheckIntervalRef.current = setInterval(performHealthCheck, interval)
  }, [performHealthCheck])

  // Initial health check and setup interval
  useEffect(() => {
    isMountedRef.current = true

    // Perform initial health check
    performHealthCheck()

    // Start periodic health checks
    startHealthCheckInterval(HEALTH_CHECK_INTERVAL)

    return () => {
      isMountedRef.current = false
      if (healthCheckIntervalRef.current) {
        clearInterval(healthCheckIntervalRef.current)
      }
    }
  }, [performHealthCheck, startHealthCheckInterval])

  // Adjust health check interval based on connectivity state
  useEffect(() => {
    const interval = isApiReachable ? HEALTH_CHECK_INTERVAL : HEALTH_CHECK_INTERVAL_OFFLINE
    startHealthCheckInterval(interval)
  }, [isApiReachable, startHealthCheckInterval])

  // Calculate combined connectivity state
  const connectivityState = useMemo((): ConnectivityState => {
    // Browser offline takes precedence
    if (!isOnline) {
      return 'offline'
    }

    // API unreachable means offline
    if (!isApiReachable) {
      return 'offline'
    }

    // If not in exercise, just check browser + API status
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
  }, [isOnline, isApiReachable, isInExercise, signalRState])

  // Handle browser online/offline events
  useEffect(() => {
    const handleOnline = () => {
      setIsOnline(true)
      // When browser comes back online, immediately check API health
      performHealthCheck()
    }

    const handleOffline = () => {
      setIsOnline(false)
      setIsApiReachable(false)
      if (!hasShownOfflineToast) {
        toast.error('🔴 You are offline. Changes will sync when connection restores.', {
          toastId: 'connectivity-browser-offline',
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
  }, [hasShownOfflineToast, performHealthCheck])

  // Show toast when SignalR state changes (only when in exercise)
  const setSignalRState = useCallback((state: SignalRState | null) => {
    setSignalRStateInternal(prevState => {
      // Only show toasts if state actually changed and we're in an exercise
      if (isInExercise && prevState !== state && prevState !== null) {
        if (state === 'connected' && (prevState === 'disconnected' || prevState === 'reconnecting' || prevState === 'error')) {
          toast.success('🟢 Real-time connection restored', {
            toastId: 'connectivity-signalr-online',
            autoClose: 3000,
          })
        } else if ((state === 'disconnected' || state === 'error') && prevState === 'connected') {
          toast.warning('🟡 Real-time connection lost. Attempting to reconnect...', {
            toastId: 'connectivity-signalr-offline',
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
    setPendingCountInternal(prev => prev + 1)
  }, [])

  const decrementPendingCount = useCallback(() => {
    setPendingCountInternal(prev => Math.max(0, prev - 1))
  }, [])

  const value = useMemo(
    () => ({
      isOnline,
      isApiReachable,
      connectivityState,
      signalRState,
      isInExercise,
      pendingCount,
      setSignalRState,
      setIsInExercise,
      setPendingCount,
      incrementPendingCount,
      decrementPendingCount,
      checkHealth: performHealthCheck,
    }),
    [
      isOnline,
      isApiReachable,
      connectivityState,
      signalRState,
      isInExercise,
      pendingCount,
      setSignalRState,
      setPendingCount,
      incrementPendingCount,
      decrementPendingCount,
      performHealthCheck,
    ],
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
