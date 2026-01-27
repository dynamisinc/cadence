/**
 * useNotificationSignalR Hook Tests
 *
 * Tests for notification-specific SignalR subscription including:
 * - Connection and userId validation
 * - User group joining/leaving
 * - NotificationCreated event subscription
 * - Cache updates via addNotificationToCache
 * - Callback invocation
 * - Cleanup behavior
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import type { HubConnection } from '@microsoft/signalr'
import { QueryClient } from '@tanstack/react-query'
import { useNotificationSignalR } from './useNotificationSignalR'
import type { NotificationDto } from '../types'

// Mock modules
vi.mock('@tanstack/react-query', () => ({
  useQueryClient: vi.fn(),
}))

vi.mock('./useNotifications', () => ({
  addNotificationToCache: vi.fn(),
}))

// Import mocked functions
import { useQueryClient } from '@tanstack/react-query'
import { addNotificationToCache } from './useNotifications'

describe('useNotificationSignalR', () => {
  // Test data
  const testUserId = 'user-123'
  const mockNotification: NotificationDto = {
    id: 'notif-1',
    type: 'InjectFired',
    priority: 'High',
    title: 'Test Notification',
    message: 'Test message',
    actionUrl: '/exercises/ex-1',
    relatedEntityType: 'Inject',
    relatedEntityId: 'inject-1',
    isRead: false,
    createdAt: '2026-01-26T10:00:00Z',
    readAt: null,
  }

  // Mock functions
  let mockConnection: HubConnection
  let mockQueryClient: QueryClient
  let mockInvoke: ReturnType<typeof vi.fn>
  let mockOn: ReturnType<typeof vi.fn>
  let mockOff: ReturnType<typeof vi.fn>

  beforeEach(() => {
    // Reset all mocks
    vi.clearAllMocks()

    // Create mock connection
    mockInvoke = vi.fn().mockResolvedValue(undefined)
    mockOn = vi.fn()
    mockOff = vi.fn()

    mockConnection = {
      invoke: mockInvoke,
      on: mockOn,
      off: mockOff,
    } as unknown as HubConnection

    // Create mock query client
    mockQueryClient = {} as QueryClient

    // Setup useQueryClient mock
    vi.mocked(useQueryClient).mockReturnValue(mockQueryClient)
  })

  describe('initialization', () => {
    it('does nothing when connection is null', () => {
      renderHook(() =>
        useNotificationSignalR({
          connection: null,
          userId: testUserId,
        }),
      )

      expect(mockInvoke).not.toHaveBeenCalled()
      expect(mockOn).not.toHaveBeenCalled()
    })

    it('does nothing when userId is null', () => {
      renderHook(() =>
        useNotificationSignalR({
          connection: mockConnection,
          userId: null,
        }),
      )

      expect(mockInvoke).not.toHaveBeenCalled()
      expect(mockOn).not.toHaveBeenCalled()
    })

    it('does nothing when both connection and userId are null', () => {
      renderHook(() =>
        useNotificationSignalR({
          connection: null,
          userId: null,
        }),
      )

      expect(mockInvoke).not.toHaveBeenCalled()
      expect(mockOn).not.toHaveBeenCalled()
    })
  })

  describe('user group', () => {
    it('joins user group when connection and userId are present', async () => {
      renderHook(() =>
        useNotificationSignalR({
          connection: mockConnection,
          userId: testUserId,
        }),
      )

      await waitFor(() => {
        expect(mockInvoke).toHaveBeenCalledWith('JoinUserGroup', testUserId)
      })
    })

    it('handles join error gracefully', async () => {
      const consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => {})
      mockInvoke.mockRejectedValueOnce(new Error('Join failed'))

      renderHook(() =>
        useNotificationSignalR({
          connection: mockConnection,
          userId: testUserId,
        }),
      )

      await waitFor(() => {
        expect(consoleErrorSpy).toHaveBeenCalledWith(
          'Failed to join user group:',
          expect.any(Error),
        )
      })

      consoleErrorSpy.mockRestore()
    })
  })

  describe('event subscription', () => {
    it('subscribes to NotificationCreated event', async () => {
      renderHook(() =>
        useNotificationSignalR({
          connection: mockConnection,
          userId: testUserId,
        }),
      )

      await waitFor(() => {
        expect(mockOn).toHaveBeenCalledWith('NotificationCreated', expect.any(Function))
      })
    })

    it('calls addNotificationToCache when notification received', async () => {
      renderHook(() =>
        useNotificationSignalR({
          connection: mockConnection,
          userId: testUserId,
        }),
      )

      // Get the handler that was registered
      await waitFor(() => {
        expect(mockOn).toHaveBeenCalledWith('NotificationCreated', expect.any(Function))
      })

      const handler = mockOn.mock.calls[0][1]

      // Simulate receiving a notification
      handler(mockNotification)

      expect(addNotificationToCache).toHaveBeenCalledWith(mockQueryClient, mockNotification)
    })

    it('calls onNotificationReceived callback when notification received', async () => {
      const onNotificationReceived = vi.fn()

      renderHook(() =>
        useNotificationSignalR({
          connection: mockConnection,
          userId: testUserId,
          onNotificationReceived,
        }),
      )

      // Get the handler that was registered
      await waitFor(() => {
        expect(mockOn).toHaveBeenCalledWith('NotificationCreated', expect.any(Function))
      })

      const handler = mockOn.mock.calls[0][1]

      // Simulate receiving a notification
      handler(mockNotification)

      expect(onNotificationReceived).toHaveBeenCalledWith(mockNotification)
    })

    it('does not throw when onNotificationReceived is not provided', async () => {
      renderHook(() =>
        useNotificationSignalR({
          connection: mockConnection,
          userId: testUserId,
        }),
      )

      // Get the handler that was registered
      await waitFor(() => {
        expect(mockOn).toHaveBeenCalledWith('NotificationCreated', expect.any(Function))
      })

      const handler = mockOn.mock.calls[0][1]

      // Should not throw
      expect(() => handler(mockNotification)).not.toThrow()
    })

    it('calls both addNotificationToCache and callback in correct order', async () => {
      const callOrder: string[] = []

      vi.mocked(addNotificationToCache).mockImplementation(() => {
        callOrder.push('cache')
      })

      const onNotificationReceived = vi.fn(() => {
        callOrder.push('callback')
      })

      renderHook(() =>
        useNotificationSignalR({
          connection: mockConnection,
          userId: testUserId,
          onNotificationReceived,
        }),
      )

      // Get the handler that was registered
      await waitFor(() => {
        expect(mockOn).toHaveBeenCalledWith('NotificationCreated', expect.any(Function))
      })

      const handler = mockOn.mock.calls[0][1]

      // Simulate receiving a notification
      handler(mockNotification)

      // Both should be called, cache first
      expect(callOrder).toEqual(['cache', 'callback'])
    })
  })

  describe('cleanup', () => {
    it('unsubscribes from NotificationCreated event on unmount', async () => {
      const { unmount } = renderHook(() =>
        useNotificationSignalR({
          connection: mockConnection,
          userId: testUserId,
        }),
      )

      // Wait for subscription
      await waitFor(() => {
        expect(mockOn).toHaveBeenCalled()
      })

      const handler = mockOn.mock.calls[0][1]

      // Unmount
      unmount()

      // Should unsubscribe
      expect(mockOff).toHaveBeenCalledWith('NotificationCreated', handler)
    })

    it('leaves user group on unmount', async () => {
      const { unmount } = renderHook(() =>
        useNotificationSignalR({
          connection: mockConnection,
          userId: testUserId,
        }),
      )

      // Wait for initial join
      await waitFor(() => {
        expect(mockInvoke).toHaveBeenCalledWith('JoinUserGroup', testUserId)
      })

      // Clear mocks to track cleanup behavior
      mockInvoke.mockClear()

      // Unmount
      unmount()

      // Wait a tick for async cleanup
      await new Promise(resolve => setTimeout(resolve, 10))

      // Should leave group
      expect(mockInvoke).toHaveBeenCalledWith('LeaveUserGroup', testUserId)
    })

    it('handles leave error gracefully', async () => {
      const consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => {})

      const { unmount } = renderHook(() =>
        useNotificationSignalR({
          connection: mockConnection,
          userId: testUserId,
        }),
      )

      // Wait for initial join
      await waitFor(() => {
        expect(mockInvoke).toHaveBeenCalledWith('JoinUserGroup', testUserId)
      })

      // Mock leave to fail
      mockInvoke.mockRejectedValueOnce(new Error('Leave failed'))

      // Unmount
      unmount()

      // Wait for error to be logged
      await new Promise(resolve => setTimeout(resolve, 10))

      expect(consoleErrorSpy).toHaveBeenCalledWith(
        'Failed to leave user group:',
        expect.any(Error),
      )

      consoleErrorSpy.mockRestore()
    })

    it('does not throw when unmounting', async () => {
      const { unmount } = renderHook(() =>
        useNotificationSignalR({
          connection: mockConnection,
          userId: testUserId,
          onNotificationReceived: vi.fn(),
        }),
      )

      // Wait for subscription
      await waitFor(() => {
        expect(mockOn).toHaveBeenCalled()
      })

      // Should not throw on unmount
      expect(() => unmount()).not.toThrow()
    })
  })

  describe('connection and userId changes', () => {
    it('resubscribes when connection changes', async () => {
      const { rerender } = renderHook(
        ({ connection, userId }) =>
          useNotificationSignalR({
            connection,
            userId,
          }),
        {
          initialProps: {
            connection: mockConnection,
            userId: testUserId,
          },
        },
      )

      // Wait for initial subscription
      await waitFor(() => {
        expect(mockInvoke).toHaveBeenCalledWith('JoinUserGroup', testUserId)
      })

      // Clear mocks
      mockInvoke.mockClear()
      mockOn.mockClear()

      // Create new connection
      const newMockConnection = {
        invoke: mockInvoke,
        on: mockOn,
        off: mockOff,
      } as unknown as HubConnection

      // Rerender with new connection
      rerender({
        connection: newMockConnection,
        userId: testUserId,
      })

      // Should rejoin with new connection
      await waitFor(() => {
        expect(mockInvoke).toHaveBeenCalledWith('JoinUserGroup', testUserId)
      })
    })

    it('resubscribes when userId changes', async () => {
      const { rerender } = renderHook(
        ({ connection, userId }) =>
          useNotificationSignalR({
            connection,
            userId,
          }),
        {
          initialProps: {
            connection: mockConnection,
            userId: testUserId,
          },
        },
      )

      // Wait for initial subscription
      await waitFor(() => {
        expect(mockInvoke).toHaveBeenCalledWith('JoinUserGroup', testUserId)
      })

      // Clear mocks
      mockInvoke.mockClear()

      const newUserId = 'user-456'

      // Rerender with new userId
      rerender({
        connection: mockConnection,
        userId: newUserId,
      })

      // Wait for cleanup of old subscription and new subscription
      await new Promise(resolve => setTimeout(resolve, 20))

      // Should leave old group and join new group
      expect(mockInvoke).toHaveBeenCalledWith('LeaveUserGroup', testUserId)
      expect(mockInvoke).toHaveBeenCalledWith('JoinUserGroup', newUserId)
    })

    it('unsubscribes when connection becomes null', async () => {
      const { rerender } = renderHook(
        ({ connection, userId }) =>
          useNotificationSignalR({
            connection,
            userId,
          }),
        {
          initialProps: {
            connection: mockConnection,
            userId: testUserId,
          },
        },
      )

      // Wait for initial subscription
      await waitFor(() => {
        expect(mockInvoke).toHaveBeenCalledWith('JoinUserGroup', testUserId)
      })

      // Clear mocks
      mockInvoke.mockClear()

      // Rerender with null connection
      rerender({
        connection: null,
        userId: testUserId,
      })

      // Wait for cleanup
      await new Promise(resolve => setTimeout(resolve, 10))

      // Should leave group
      expect(mockInvoke).toHaveBeenCalledWith('LeaveUserGroup', testUserId)
    })

    it('unsubscribes when userId becomes null', async () => {
      const { rerender } = renderHook(
        ({ connection, userId }) =>
          useNotificationSignalR({
            connection,
            userId,
          }),
        {
          initialProps: {
            connection: mockConnection,
            userId: testUserId,
          },
        },
      )

      // Wait for initial subscription
      await waitFor(() => {
        expect(mockInvoke).toHaveBeenCalledWith('JoinUserGroup', testUserId)
      })

      // Clear mocks
      mockInvoke.mockClear()

      // Rerender with null userId
      rerender({
        connection: mockConnection,
        userId: null,
      })

      // Wait for cleanup
      await new Promise(resolve => setTimeout(resolve, 10))

      // Should leave group
      expect(mockInvoke).toHaveBeenCalledWith('LeaveUserGroup', testUserId)
    })
  })

  describe('callback stability', () => {
    it('resubscribes when callback changes', async () => {
      const callback1 = vi.fn()

      const { rerender } = renderHook(
        ({ callback }) =>
          useNotificationSignalR({
            connection: mockConnection,
            userId: testUserId,
            onNotificationReceived: callback,
          }),
        {
          initialProps: { callback: callback1 },
        },
      )

      // Wait for initial subscription
      await waitFor(() => {
        expect(mockOn).toHaveBeenCalledWith('NotificationCreated', expect.any(Function))
      })

      const firstHandler = mockOn.mock.calls[0][1]

      // Clear mocks
      mockOn.mockClear()
      mockOff.mockClear()

      // Change callback
      const callback2 = vi.fn()
      rerender({ callback: callback2 })

      // Should unsubscribe old handler and subscribe with new handler
      await waitFor(() => {
        expect(mockOff).toHaveBeenCalledWith('NotificationCreated', firstHandler)
        expect(mockOn).toHaveBeenCalledWith('NotificationCreated', expect.any(Function))
      })

      // Get new handler
      const newHandler = mockOn.mock.calls[0][1]

      // New handler should call new callback
      newHandler(mockNotification)
      expect(callback2).toHaveBeenCalledWith(mockNotification)
      expect(callback1).not.toHaveBeenCalled()
    })
  })
})
