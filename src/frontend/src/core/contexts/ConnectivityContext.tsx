/**
 * ConnectivityContext
 *
 * Provides global connectivity state management including:
 * - Browser online/offline status
 * - API server reachability (via health check)
 * - SignalR connection state (when in exercise conduct)
 * - SignalR joined state (whether joined exercise group)
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
import { notify } from '@/shared/utils/notify'
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
  /** Whether joined the exercise SignalR group */
  isSignalRJoined: boolean
  /** Number of pending offline actions */
  pendingCount: number
  /** Update SignalR state (called by useExerciseSignalR) */
  setSignalRState: (state: SignalRState | null) => void
  /** Set whether we're in an exercise context */
  setIsInExercise: (inExercise: boolean) => void
  /** Set whether joined the exercise SignalR group */
  setIsSignalRJoined: (joined: boolean) => void
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
  const [isSignalRJoined, setIsSignalRJoined] = useState(false)
  const [pendingCount, setPendingCountInternal] = useState(0)
  const healthCheckIntervalRef = useRef<ReturnType<typeof setInterval> | null>(null)
  const isMountedRef = useRef(true)

  // Use ref for isInExercise inside setSignalRState to avoid dependency cycle
  const isInExerciseRef = useRef(isInExercise)
  useEffect(() => {
    isInExerciseRef.current = isInExercise
  }, [isInExercise])

  // Use ref for isApiReachable inside performHealthCheck to avoid stale closure
  const isApiReachableRef = useRef(isApiReachable)
  useEffect(() => {
    isApiReachableRef.current = isApiReachable
  }, [isApiReachable])

  // Perform health check — stable callback (no state in deps)
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

    const wasReachable = isApiReachableRef.current

    setIsApiReachable(isHealthy)

    // Show persistent warning when API becomes unreachable
    if (!isHealthy && wasReachable) {
      notify.error('Cannot reach server. Changes will sync when connection restores.', {
        toastId: 'connection-status',
        autoClose: false,
      })
    }

    // Dismiss warning and show brief success when API becomes reachable again
    if (isHealthy && !wasReachable) {
      notify.dismiss('connection-status')
      notify.success('Server connection restored', {
        toastId: 'connection-restored',
        autoClose: 2000,
      })
    }

    return isHealthy
  }, []) // No deps — uses refs for mutable state

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
    const interval = isApiReachable ? HEALTH_CHECK_INTERVAL : HEALTH_CHECK_INTERVAL_OFFLINE
    startHealthCheckInterval(interval)

    return () => {
      isMountedRef.current = false
      if (healthCheckIntervalRef.current) {
        clearInterval(healthCheckIntervalRef.current)
      }
    }
  // performHealthCheck and startHealthCheckInterval are now stable (no state deps)
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

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
      notify.error('You are offline. Changes will sync when connection restores.', {
        toastId: 'connection-status',
        autoClose: false,
      })
    }

    window.addEventListener('online', handleOnline)
    window.addEventListener('offline', handleOffline)

    return () => {
      window.removeEventListener('online', handleOnline)
      window.removeEventListener('offline', handleOffline)
    }
  }, [performHealthCheck])

  // Stable setSignalRState — uses ref for isInExercise to avoid dependency cycle
  const setSignalRState = useCallback((state: SignalRState | null) => {
    setSignalRStateInternal(prevState => {
      // Only show toasts if state actually changed and we're in an exercise
      if (isInExerciseRef.current && prevState !== state && prevState !== null) {
        if (state === 'connected' && (prevState === 'disconnected' || prevState === 'reconnecting' || prevState === 'error')) {
          notify.dismiss('signalr-status')
          notify.success('Real-time connection restored', {
            toastId: 'signalr-restored',
            autoClose: 2000,
          })
        } else if ((state === 'disconnected' || state === 'error') && prevState === 'connected') {
          notify.warning('Real-time connection lost. Attempting to reconnect...', {
            toastId: 'signalr-status',
            autoClose: false,
          })
        }
      }
      return state
    })
  }, []) // No deps — uses ref for isInExercise

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
      isSignalRJoined,
      pendingCount,
      setSignalRState,
      setIsInExercise,
      setIsSignalRJoined,
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
      isSignalRJoined,
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
