/**
 * useSignalR Hook
 *
 * Provides SignalR real-time connection management for the application.
 * Connects to Azure SignalR Service via the negotiate endpoint.
 *
 * Usage:
 * ```tsx
 * const { connection, connectionState, error } = useSignalR();
 *
 * // Subscribe to events
 * useEffect(() => {
 *   if (connection) {
 *     connection.on('noteCreated', (data) => {
 *       console.log('Note created:', data);
 *     });
 *   }
 * }, [connection]);
 * ```
 */

import { useState, useEffect, useCallback, useRef } from 'react'
import * as signalR from '@microsoft/signalr'

/** SignalR connection states */
export type ConnectionState =
  | 'disconnected'
  | 'connecting'
  | 'connected'
  | 'reconnecting'
  | 'error'

interface UseSignalROptions {
  /** Whether to automatically connect on mount (default: true) */
  autoConnect?: boolean;
  /** User ID to send with negotiate request */
  userId?: string;
  /** Enable automatic reconnection (default: true) */
  autoReconnect?: boolean;
  /** Custom hub URL (default: uses VITE_API_URL + /api/negotiate) */
  hubUrl?: string;
}

interface UseSignalRReturn {
  /** The SignalR connection instance */
  connection: signalR.HubConnection | null;
  /** Current connection state */
  connectionState: ConnectionState;
  /** Error message if connection failed */
  error: string | null;
  /** Manually connect to the hub */
  connect: () => Promise<void>;
  /** Manually disconnect from the hub */
  disconnect: () => Promise<void>;
  /** Subscribe to a hub event */
  on: <T = unknown>(eventName: string, callback: (data: T) => void) => void;
  /** Unsubscribe from a hub event */
  off: (eventName: string, callback: (...args: unknown[]) => void) => void;
}

/**
 * Get the SignalR negotiate URL
 */
const getNegotiateUrl = (customUrl?: string): string => {
  if (customUrl) return customUrl
  const baseUrl = import.meta.env.VITE_API_URL ?? ''
  return `${baseUrl}/api`
}

/**
 * Hook for managing SignalR real-time connections
 */
export const useSignalR = (options: UseSignalROptions = {}): UseSignalRReturn => {
  const {
    autoConnect = true,
    userId,
    autoReconnect = true,
    hubUrl,
  } = options

  const [connection, setConnection] = useState<signalR.HubConnection | null>(null)
  const [connectionState, setConnectionState] = useState<ConnectionState>('disconnected')
  const [error, setError] = useState<string | null>(null)
  const connectionRef = useRef<signalR.HubConnection | null>(null)

  /**
   * Create and configure the SignalR connection
   */
  const createConnection = useCallback(() => {
    const negotiateUrl = getNegotiateUrl(hubUrl)

    const connectionBuilder = new signalR.HubConnectionBuilder()
      .withUrl(negotiateUrl, {
        headers: userId ? { 'x-user-id': userId } : undefined,
      })
      .withAutomaticReconnect(
        autoReconnect
          ? {
            nextRetryDelayInMilliseconds: retryContext => {
              // Exponential backoff: 0s, 2s, 4s, 8s, 16s, max 30s
              const delay = Math.min(
                Math.pow(2, retryContext.previousRetryCount) * 1000,
                30000,
              )
              return delay
            },
          }
          : undefined,
      )
      .configureLogging(
        import.meta.env.DEV ? signalR.LogLevel.Information : signalR.LogLevel.Warning,
      )
      .build()

    return connectionBuilder
  }, [userId, autoReconnect, hubUrl])

  /**
   * Connect to the SignalR hub
   */
  const connect = useCallback(async () => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      return
    }

    try {
      setError(null)
      setConnectionState('connecting')

      const newConnection = createConnection()
      connectionRef.current = newConnection

      // Set up connection state handlers
      newConnection.onclose(err => {
        setConnectionState('disconnected')
        if (err) {
          setError(`Connection closed: ${err.message}`)
        }
      })

      newConnection.onreconnecting(err => {
        setConnectionState('reconnecting')
        if (err) {
          console.warn('SignalR reconnecting:', err.message)
        }
      })

      newConnection.onreconnected(() => {
        setConnectionState('connected')
        setError(null)
      })

      await newConnection.start()
      setConnection(newConnection)
      setConnectionState('connected')
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to connect to SignalR'
      setError(message)
      setConnectionState('error')
      console.error('SignalR connection error:', err)
    }
  }, [createConnection])

  /**
   * Disconnect from the SignalR hub
   */
  const disconnect = useCallback(async () => {
    if (connectionRef.current) {
      try {
        await connectionRef.current.stop()
        setConnection(null)
        connectionRef.current = null
        setConnectionState('disconnected')
      } catch (err) {
        console.error('SignalR disconnect error:', err)
      }
    }
  }, [])

  /**
   * Subscribe to a hub event
   */
  const on = useCallback(
    <T = unknown>(eventName: string, callback: (data: T) => void) => {
      if (connectionRef.current) {
        connectionRef.current.on(eventName, callback)
      }
    },
    [],
  )

  /**
   * Unsubscribe from a hub event
   */
  const off = useCallback(
    (eventName: string, callback: (...args: unknown[]) => void) => {
      if (connectionRef.current) {
        connectionRef.current.off(eventName, callback)
      }
    },
    [],
  )

  // Auto-connect on mount if enabled
  useEffect(() => {
    if (autoConnect) {
      connect()
    }

    return () => {
      disconnect()
    }
  }, [autoConnect, connect, disconnect])

  return {
    connection,
    connectionState,
    error,
    connect,
    disconnect,
    on,
    off,
  }
}

export default useSignalR
