/**
 * useExerciseSignalR Hook Tests
 *
 * Tests for exercise-specific SignalR connection management including:
 * - Connection lifecycle
 * - Exercise group joining/leaving
 * - Event subscriptions
 * - Reconnection callback
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { renderHook, act, waitFor } from '@testing-library/react'

// Capture lifecycle callbacks for testing
let oncloseCallback: ((err?: Error) => void) | null = null
let onreconnectingCallback: ((err?: Error) => void) | null = null
let onreconnectedCallback: (() => void) | null = null

// Use vi.hoisted to define mocks that will be available during vi.mock hoisting
const {
  mockStart,
  mockStop,
  mockOn,
  mockOff,
  mockInvoke,
  mockWithUrl,
  mockState,
  setMockState,
} = vi.hoisted(() => {
  let _mockState = 'Disconnected'
  return {
    mockStart: vi.fn(),
    mockStop: vi.fn(),
    mockOn: vi.fn(),
    mockOff: vi.fn(),
    mockInvoke: vi.fn(),
    mockWithUrl: vi.fn(),
    mockState: { get current() { return _mockState } },
    setMockState: (state: string) => { _mockState = state },
  }
})

vi.mock('@microsoft/signalr', () => {
  // Create connection object inside factory
  const connection = {
    start: mockStart,
    stop: mockStop,
    on: mockOn,
    off: mockOff,
    invoke: mockInvoke,
    onclose: (callback: (err?: Error) => void) => {
      oncloseCallback = callback
    },
    onreconnecting: (callback: (err?: Error) => void) => {
      onreconnectingCallback = callback
    },
    onreconnected: (callback: () => void) => {
      onreconnectedCallback = callback
    },
    get state() {
      return mockState.current
    },
  }

  // Create builder class inside factory
  class HubConnectionBuilder {
    withUrl(...args: unknown[]) {
      mockWithUrl(...args)
      return this
    }
    withAutomaticReconnect() {
      return this
    }
    configureLogging() {
      return this
    }
    build() {
      return connection
    }
  }

  return {
    HubConnectionBuilder,
    HubConnectionState: {
      Disconnected: 'Disconnected',
      Connected: 'Connected',
      Connecting: 'Connecting',
      Reconnecting: 'Reconnecting',
    },
    LogLevel: {
      Information: 1,
      Warning: 3,
    },
  }
})

vi.mock('@/contexts/AuthContext', () => ({
  useAuth: () => ({ accessToken: 'mock-token' }),
}))

import { useExerciseSignalR } from './useExerciseSignalR'

describe('useExerciseSignalR', () => {
  const testExerciseId = 'test-exercise-123'

  beforeEach(() => {
    vi.clearAllMocks()
    mockStart.mockResolvedValue(undefined)
    mockStop.mockResolvedValue(undefined)
    mockInvoke.mockResolvedValue(undefined)
    setMockState('Disconnected')
    oncloseCallback = null
    onreconnectingCallback = null
    onreconnectedCallback = null
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  describe('initialization', () => {
    it('starts disconnected', () => {
      const { result } = renderHook(() =>
        useExerciseSignalR({ exerciseId: testExerciseId, enabled: false }),
      )

      expect(result.current.connectionState).toBe('disconnected')
      expect(result.current.isJoined).toBe(false)
      expect(result.current.error).toBeNull()
    })

    it('auto-connects by default when exerciseId is provided', async () => {
      renderHook(() => useExerciseSignalR({ exerciseId: testExerciseId }))

      await waitFor(() => {
        expect(mockStart).toHaveBeenCalled()
      })
    })

    it('does not connect when enabled is false', () => {
      renderHook(() =>
        useExerciseSignalR({ exerciseId: testExerciseId, enabled: false }),
      )

      expect(mockStart).not.toHaveBeenCalled()
    })
  })

  describe('exercise group', () => {
    it('joins exercise group after connecting', async () => {
      renderHook(() => useExerciseSignalR({ exerciseId: testExerciseId }))

      await waitFor(() => {
        expect(mockInvoke).toHaveBeenCalledWith('JoinExercise', testExerciseId)
      })
    })

    it('sets isJoined to true after joining', async () => {
      const { result } = renderHook(() =>
        useExerciseSignalR({ exerciseId: testExerciseId }),
      )

      await waitFor(() => {
        expect(result.current.isJoined).toBe(true)
      })
    })
  })

  describe('event subscriptions', () => {
    it('subscribes to inject events when callbacks are provided', async () => {
      const onInjectFired = vi.fn()
      const onInjectStatusChanged = vi.fn()

      renderHook(() =>
        useExerciseSignalR({
          exerciseId: testExerciseId,
          onInjectFired,
          onInjectStatusChanged,
        }),
      )

      await waitFor(() => {
        expect(mockOn).toHaveBeenCalledWith('InjectFired', onInjectFired)
        expect(mockOn).toHaveBeenCalledWith('InjectStatusChanged', onInjectStatusChanged)
      })
    })

    it('subscribes to clock events when callbacks are provided', async () => {
      const onClockStarted = vi.fn()
      const onClockPaused = vi.fn()
      const onClockReset = vi.fn()
      const onClockChanged = vi.fn()

      renderHook(() =>
        useExerciseSignalR({
          exerciseId: testExerciseId,
          onClockStarted,
          onClockPaused,
          onClockReset,
          onClockChanged,
        }),
      )

      await waitFor(() => {
        expect(mockOn).toHaveBeenCalledWith('ClockStarted', onClockStarted)
        expect(mockOn).toHaveBeenCalledWith('ClockPaused', onClockPaused)
        expect(mockOn).toHaveBeenCalledWith('ClockReset', onClockReset)
        expect(mockOn).toHaveBeenCalledWith('ClockChanged', onClockChanged)
      })
    })

    it('subscribes to observation events when callbacks are provided', async () => {
      const onObservationAdded = vi.fn()
      const onObservationUpdated = vi.fn()
      const onObservationDeleted = vi.fn()

      renderHook(() =>
        useExerciseSignalR({
          exerciseId: testExerciseId,
          onObservationAdded,
          onObservationUpdated,
          onObservationDeleted,
        }),
      )

      await waitFor(() => {
        expect(mockOn).toHaveBeenCalledWith('ObservationAdded', onObservationAdded)
        expect(mockOn).toHaveBeenCalledWith('ObservationUpdated', onObservationUpdated)
        expect(mockOn).toHaveBeenCalledWith('ObservationDeleted', onObservationDeleted)
      })
    })
  })

  describe('reconnection', () => {
    it('rejoins exercise group on reconnection', async () => {
      renderHook(() => useExerciseSignalR({ exerciseId: testExerciseId }))

      // Wait for initial connection and join
      await waitFor(() => {
        expect(mockInvoke).toHaveBeenCalledWith('JoinExercise', testExerciseId)
      })

      // Clear mocks to track reconnection behavior
      mockInvoke.mockClear()

      // Simulate reconnection
      await act(async () => {
        onreconnectedCallback?.()
      })

      // Should rejoin exercise group
      expect(mockInvoke).toHaveBeenCalledWith('JoinExercise', testExerciseId)
    })

    it('calls onReconnected callback after reconnection', async () => {
      const onReconnected = vi.fn()

      renderHook(() =>
        useExerciseSignalR({
          exerciseId: testExerciseId,
          onReconnected,
        }),
      )

      // Wait for initial connection
      await waitFor(() => {
        expect(mockStart).toHaveBeenCalled()
      })

      // Simulate reconnection
      await act(async () => {
        onreconnectedCallback?.()
      })

      // Should call onReconnected callback
      expect(onReconnected).toHaveBeenCalledTimes(1)
    })

    it('updates connection state to reconnecting during reconnection attempt', async () => {
      const { result } = renderHook(() =>
        useExerciseSignalR({ exerciseId: testExerciseId }),
      )

      // Wait for initial connection
      await waitFor(() => {
        expect(result.current.connectionState).toBe('connected')
      })

      // Simulate reconnecting
      act(() => {
        onreconnectingCallback?.()
      })

      expect(result.current.connectionState).toBe('reconnecting')
      expect(result.current.isJoined).toBe(false)
    })

    it('updates connection state to connected after successful reconnection', async () => {
      const { result } = renderHook(() =>
        useExerciseSignalR({ exerciseId: testExerciseId }),
      )

      // Wait for initial connection
      await waitFor(() => {
        expect(result.current.connectionState).toBe('connected')
      })

      // Simulate reconnecting then reconnected
      act(() => {
        onreconnectingCallback?.()
      })
      expect(result.current.connectionState).toBe('reconnecting')

      await act(async () => {
        onreconnectedCallback?.()
      })
      expect(result.current.connectionState).toBe('connected')
    })
  })

  describe('connection close', () => {
    it('updates state when connection closes', async () => {
      const { result } = renderHook(() =>
        useExerciseSignalR({ exerciseId: testExerciseId }),
      )

      // Wait for initial connection
      await waitFor(() => {
        expect(result.current.connectionState).toBe('connected')
      })

      // Simulate connection close
      act(() => {
        oncloseCallback?.()
      })

      expect(result.current.connectionState).toBe('disconnected')
      expect(result.current.isJoined).toBe(false)
    })

    it('captures error message when connection closes with error', async () => {
      const { result } = renderHook(() =>
        useExerciseSignalR({ exerciseId: testExerciseId }),
      )

      // Wait for initial connection
      await waitFor(() => {
        expect(result.current.connectionState).toBe('connected')
      })

      // Simulate connection close with error
      act(() => {
        oncloseCallback?.(new Error('Network failure'))
      })

      expect(result.current.connectionState).toBe('disconnected')
      expect(result.current.error).toBe('Connection closed: Network failure')
    })
  })

  describe('cleanup', () => {
    it('attempts to leave exercise group on unmount', async () => {
      const { unmount } = renderHook(() =>
        useExerciseSignalR({ exerciseId: testExerciseId }),
      )

      // Wait for initial connection
      await waitFor(() => {
        expect(mockInvoke).toHaveBeenCalledWith('JoinExercise', testExerciseId)
      })

      // Unmount triggers cleanup (async, fire and forget)
      unmount()

      // Wait a tick for async cleanup to start
      await new Promise(resolve => setTimeout(resolve, 10))

      // Should attempt to leave group
      expect(mockInvoke).toHaveBeenCalledWith('LeaveExercise', testExerciseId)
    })

    it('does not throw when unmounting', async () => {
      const { unmount } = renderHook(() =>
        useExerciseSignalR({
          exerciseId: testExerciseId,
          onInjectFired: vi.fn(),
          onClockChanged: vi.fn(),
        }),
      )

      // Wait for initial connection
      await waitFor(() => {
        expect(mockStart).toHaveBeenCalled()
      })

      // Should not throw on unmount
      expect(() => unmount()).not.toThrow()
    })
  })
})
