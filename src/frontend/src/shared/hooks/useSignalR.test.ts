/**
 * useSignalR Hook Tests
 *
 * Tests for SignalR connection management including:
 * - Connection lifecycle
 * - Event subscription/unsubscription
 * - Error handling
 * - Reconnection logic
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { renderHook, act, waitFor } from '@testing-library/react'

// Use vi.hoisted to define mocks that will be available during vi.mock hoisting
const {
  mockStart,
  mockStop,
  mockOn,
  mockOff,
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
    onclose: vi.fn(),
    onreconnecting: vi.fn(),
    onreconnected: vi.fn(),
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

import { useSignalR } from './useSignalR'

describe('useSignalR', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockStart.mockResolvedValue(undefined)
    mockStop.mockResolvedValue(undefined)
    setMockState('Disconnected')
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  describe('initialization', () => {
    it('starts disconnected', () => {
      const { result } = renderHook(() => useSignalR({ autoConnect: false }))

      expect(result.current.connectionState).toBe('disconnected')
      expect(result.current.connection).toBeNull()
      expect(result.current.error).toBeNull()
    })

    it('auto-connects by default', async () => {
      renderHook(() => useSignalR())

      await waitFor(() => {
        expect(mockStart).toHaveBeenCalled()
      })
    })

    it('does not auto-connect when autoConnect is false', () => {
      renderHook(() => useSignalR({ autoConnect: false }))

      expect(mockStart).not.toHaveBeenCalled()
    })
  })

  describe('connect', () => {
    it('connects successfully', async () => {
      const { result } = renderHook(() => useSignalR({ autoConnect: false }))

      await act(async () => {
        await result.current.connect()
      })

      expect(mockStart).toHaveBeenCalled()
      expect(result.current.connectionState).toBe('connected')
    })

    it('handles connection errors', async () => {
      mockStart.mockRejectedValueOnce(new Error('Connection failed'))

      const { result } = renderHook(() => useSignalR({ autoConnect: false }))

      await act(async () => {
        await result.current.connect()
      })

      expect(result.current.connectionState).toBe('error')
      expect(result.current.error).toBe('Connection failed')
    })

    it('passes userId header when provided', async () => {
      renderHook(() => useSignalR({ autoConnect: true, userId: 'test-user' }))

      await waitFor(() => {
        expect(mockWithUrl).toHaveBeenCalledWith(
          expect.any(String),
          expect.objectContaining({
            headers: { 'x-user-id': 'test-user' },
          }),
        )
      })
    })
  })

  describe('disconnect', () => {
    it('disconnects successfully', async () => {
      const { result } = renderHook(() => useSignalR({ autoConnect: false }))

      // First connect
      await act(async () => {
        await result.current.connect()
      })

      // Then disconnect
      await act(async () => {
        await result.current.disconnect()
      })

      expect(mockStop).toHaveBeenCalled()
      expect(result.current.connectionState).toBe('disconnected')
    })
  })

  describe('event handlers', () => {
    it('subscribes to events', async () => {
      const { result } = renderHook(() => useSignalR({ autoConnect: false }))
      const callback = vi.fn()

      await act(async () => {
        await result.current.connect()
      })

      act(() => {
        result.current.on('testEvent', callback)
      })

      expect(mockOn).toHaveBeenCalledWith('testEvent', callback)
    })

    it('unsubscribes from events', async () => {
      const { result } = renderHook(() => useSignalR({ autoConnect: false }))
      const callback = vi.fn()

      await act(async () => {
        await result.current.connect()
      })

      act(() => {
        result.current.off('testEvent', callback)
      })

      expect(mockOff).toHaveBeenCalledWith('testEvent', callback)
    })
  })

  describe('cleanup', () => {
    it('disconnects on unmount when connected', async () => {
      const { result, unmount } = renderHook(() => useSignalR({ autoConnect: false }))

      // First connect
      await act(async () => {
        await result.current.connect()
      })

      // Clear mocks to isolate unmount behavior
      vi.clearAllMocks()

      // Then unmount
      unmount()

      // Should attempt to stop connection on cleanup
      expect(mockStop).toHaveBeenCalled()
    })

    it('handles unmount when not connected', () => {
      const { unmount } = renderHook(() => useSignalR({ autoConnect: false }))

      // Should not throw when unmounting without connection
      expect(() => unmount()).not.toThrow()
    })
  })
})
