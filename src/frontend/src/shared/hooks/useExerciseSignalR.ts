/**
 * useExerciseSignalR Hook
 *
 * Provides SignalR real-time connection management for exercise conduct.
 * Handles joining/leaving exercise groups and subscribing to exercise events.
 *
 * Usage:
 * ```tsx
 * const { connectionState, isJoined } = useExerciseSignalR({
 *   exerciseId,
 *   onInjectFired: (inject) => { ... },
 *   onClockChanged: (clock) => { ... },
 *   onObservationAdded: (observation) => { ... },
 * });
 * ```
 */

import { useEffect, useCallback, useState, useRef } from 'react'
import * as signalR from '@microsoft/signalr'
import type { InjectDto } from '../../features/injects/types'
import type { ObservationDto } from '../../features/observations/types'
import type { ExerciseClockDto } from '../../features/exercise-clock/types'

/** SignalR connection states */
export type ConnectionState =
  | 'disconnected'
  | 'connecting'
  | 'connected'
  | 'reconnecting'
  | 'error'

interface UseExerciseSignalROptions {
  /** Exercise ID to join */
  exerciseId: string
  /** Called when an inject is fired */
  onInjectFired?: (inject: InjectDto) => void
  /** Called when an inject status changes */
  onInjectStatusChanged?: (inject: InjectDto) => void
  /** Called when the exercise clock starts */
  onClockStarted?: (clock: ExerciseClockDto) => void
  /** Called when the exercise clock is paused */
  onClockPaused?: (clock: ExerciseClockDto) => void
  /** Called when the exercise clock is reset */
  onClockReset?: (clock: ExerciseClockDto) => void
  /** Called when clock state changes (generic) */
  onClockChanged?: (clock: ExerciseClockDto) => void
  /** Called when an observation is added */
  onObservationAdded?: (observation: ObservationDto) => void
  /** Called when an observation is updated */
  onObservationUpdated?: (observation: ObservationDto) => void
  /** Called when an observation is deleted */
  onObservationDeleted?: (observationId: string) => void
  /** Called when connection is restored after being disconnected */
  onReconnected?: () => void
  /** Whether to automatically connect (default: true) */
  enabled?: boolean
}

interface UseExerciseSignalRReturn {
  /** Current connection state */
  connectionState: ConnectionState
  /** Whether successfully joined the exercise group */
  isJoined: boolean
  /** Error message if connection failed */
  error: string | null
  /** Manually reconnect */
  reconnect: () => Promise<void>
}

/**
 * Get the SignalR hub URL
 */
const getHubUrl = (): string => {
  const baseUrl = import.meta.env.VITE_API_URL ?? ''
  return `${baseUrl}/hubs/exercise`
}

/**
 * Hook for managing SignalR real-time connections for exercise conduct
 */
export const useExerciseSignalR = (
  options: UseExerciseSignalROptions,
): UseExerciseSignalRReturn => {
  const {
    exerciseId,
    onInjectFired,
    onInjectStatusChanged,
    onClockStarted,
    onClockPaused,
    onClockReset,
    onClockChanged,
    onObservationAdded,
    onObservationUpdated,
    onObservationDeleted,
    onReconnected,
    enabled = true,
  } = options

  const [connectionState, setConnectionState] = useState<ConnectionState>('disconnected')
  const [isJoined, setIsJoined] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const connectionRef = useRef<signalR.HubConnection | null>(null)
  const currentExerciseIdRef = useRef<string | null>(null)
  const onReconnectedRef = useRef(onReconnected)

  // Keep onReconnected ref up to date
  useEffect(() => {
    onReconnectedRef.current = onReconnected
  }, [onReconnected])

  /**
   * Create and configure the SignalR connection
   */
  const createConnection = useCallback(() => {
    const hubUrl = getHubUrl()

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext: signalR.RetryContext) => {
          // Exponential backoff: 0s, 2s, 4s, 8s, 16s, max 30s
          const delay = Math.min(
            Math.pow(2, retryContext.previousRetryCount) * 1000,
            30000,
          )
          return delay
        },
      })
      .configureLogging(
        import.meta.env.DEV ? signalR.LogLevel.Information : signalR.LogLevel.Warning,
      )
      .build()

    return connection
  }, [])

  /**
   * Join an exercise group
   */
  const joinExercise = useCallback(async (connection: signalR.HubConnection, id: string) => {
    try {
      await connection.invoke('JoinExercise', id)
      currentExerciseIdRef.current = id
      setIsJoined(true)
      console.log(`Joined exercise group: ${id}`)
    } catch (err) {
      console.error('Failed to join exercise group:', err)
      setError(`Failed to join exercise: ${err instanceof Error ? err.message : 'Unknown error'}`)
    }
  }, [])

  /**
   * Leave an exercise group
   */
  const leaveExercise = useCallback(async (connection: signalR.HubConnection, id: string) => {
    try {
      await connection.invoke('LeaveExercise', id)
      currentExerciseIdRef.current = null
      setIsJoined(false)
      console.log(`Left exercise group: ${id}`)
    } catch (err) {
      console.error('Failed to leave exercise group:', err)
    }
  }, [])

  /**
   * Set up event handlers
   */
  const setupEventHandlers = useCallback(
    (connection: signalR.HubConnection) => {
      // Inject events
      if (onInjectFired) {
        connection.on('InjectFired', onInjectFired)
      }
      if (onInjectStatusChanged) {
        connection.on('InjectStatusChanged', onInjectStatusChanged)
      }

      // Clock events
      if (onClockStarted) {
        connection.on('ClockStarted', onClockStarted)
      }
      if (onClockPaused) {
        connection.on('ClockPaused', onClockPaused)
      }
      if (onClockReset) {
        connection.on('ClockReset', onClockReset)
      }
      if (onClockChanged) {
        connection.on('ClockChanged', onClockChanged)
      }

      // Observation events
      if (onObservationAdded) {
        connection.on('ObservationAdded', onObservationAdded)
      }
      if (onObservationUpdated) {
        connection.on('ObservationUpdated', onObservationUpdated)
      }
      if (onObservationDeleted) {
        connection.on('ObservationDeleted', onObservationDeleted)
      }
    },
    [
      onInjectFired,
      onInjectStatusChanged,
      onClockStarted,
      onClockPaused,
      onClockReset,
      onClockChanged,
      onObservationAdded,
      onObservationUpdated,
      onObservationDeleted,
    ],
  )

  /**
   * Remove event handlers
   */
  const removeEventHandlers = useCallback((connection: signalR.HubConnection) => {
    connection.off('InjectFired')
    connection.off('InjectStatusChanged')
    connection.off('ClockStarted')
    connection.off('ClockPaused')
    connection.off('ClockReset')
    connection.off('ClockChanged')
    connection.off('ObservationAdded')
    connection.off('ObservationUpdated')
    connection.off('ObservationDeleted')
  }, [])

  /**
   * Connect to the SignalR hub and join the exercise
   */
  const connect = useCallback(async () => {
    if (!exerciseId) return

    // Don't reconnect if already connected to the same exercise
    if (
      connectionRef.current?.state === signalR.HubConnectionState.Connected &&
      currentExerciseIdRef.current === exerciseId
    ) {
      return
    }

    try {
      setError(null)
      setConnectionState('connecting')

      // Disconnect existing connection if switching exercises
      if (connectionRef.current) {
        if (currentExerciseIdRef.current) {
          await leaveExercise(connectionRef.current, currentExerciseIdRef.current)
        }
        removeEventHandlers(connectionRef.current)
        await connectionRef.current.stop()
      }

      const newConnection = createConnection()
      connectionRef.current = newConnection

      // Set up connection state handlers
      newConnection.onclose(err => {
        setConnectionState('disconnected')
        setIsJoined(false)
        if (err) {
          setError(`Connection closed: ${err.message}`)
        }
      })

      newConnection.onreconnecting(err => {
        setConnectionState('reconnecting')
        setIsJoined(false)
        if (err) {
          console.warn('SignalR reconnecting:', err.message)
        }
      })

      newConnection.onreconnected(async () => {
        setConnectionState('connected')
        setError(null)
        // Rejoin the exercise group after reconnection
        if (exerciseId && connectionRef.current) {
          await joinExercise(connectionRef.current, exerciseId)
        }
        // Notify caller that connection was restored so they can refresh state
        onReconnectedRef.current?.()
      })

      // Set up event handlers
      setupEventHandlers(newConnection)

      // Start connection
      await newConnection.start()
      setConnectionState('connected')

      // Join the exercise group
      await joinExercise(newConnection, exerciseId)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to connect to SignalR'
      setError(message)
      setConnectionState('error')
      console.error('SignalR connection error:', err)
    }
  }, [
    exerciseId,
    createConnection,
    joinExercise,
    leaveExercise,
    setupEventHandlers,
    removeEventHandlers,
  ])

  /**
   * Disconnect from the SignalR hub
   */
  const disconnect = useCallback(async () => {
    if (connectionRef.current) {
      try {
        if (currentExerciseIdRef.current) {
          await leaveExercise(connectionRef.current, currentExerciseIdRef.current)
        }
        removeEventHandlers(connectionRef.current)
        await connectionRef.current.stop()
        connectionRef.current = null
        setConnectionState('disconnected')
        setIsJoined(false)
      } catch (err) {
        console.error('SignalR disconnect error:', err)
      }
    }
  }, [leaveExercise, removeEventHandlers])

  /**
   * Reconnect to the hub
   */
  const reconnect = useCallback(async () => {
    await disconnect()
    await connect()
  }, [connect, disconnect])

  // Connect/disconnect based on enabled state and exerciseId
  useEffect(() => {
    if (enabled && exerciseId) {
      connect()
    }

    return () => {
      disconnect()
    }
  }, [enabled, exerciseId, connect, disconnect])

  return {
    connectionState,
    isJoined,
    error,
    reconnect,
  }
}

export default useExerciseSignalR
