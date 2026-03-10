/**
 * useUsers Hook Tests
 *
 * Tests for user management React Query hooks.
 * FF-C01: Verifies hooks use React Query properly with correct cache invalidation.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor, act } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { createElement, type ReactNode } from 'react'
import {
  useUserList,
  useUpdateUser,
  useChangeUserRole,
  useDeactivateUser,
  useReactivateUser,
  userKeys,
} from './useUsers'
import { userService } from '../services/userService'
import type { UserDto, UserListResponse } from '../types'

// Mock the user service
vi.mock('../services/userService', () => ({
  userService: {
    getUsers: vi.fn(),
    updateUser: vi.fn(),
    changeRole: vi.fn(),
    deactivateUser: vi.fn(),
    reactivateUser: vi.fn(),
  },
}))

// Mock notify to prevent toast errors in tests
vi.mock('@/shared/utils/notify', () => ({
  notify: {
    success: vi.fn(),
    error: vi.fn(),
    warning: vi.fn(),
    info: vi.fn(),
    dismiss: vi.fn(),
  },
}))

const mockUser: UserDto = {
  id: 'user-1',
  email: 'test@example.com',
  displayName: 'Test User',
  systemRole: 'User',
  status: 'Active',
  lastLoginAt: null,
  createdAt: '2025-01-01T00:00:00Z',
}

const mockUserListResponse: UserListResponse = {
  users: [mockUser],
  pagination: {
    page: 1,
    pageSize: 10,
    totalCount: 1,
    totalPages: 1,
  },
}

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  const Wrapper = ({ children }: { children: ReactNode }) =>
    createElement(QueryClientProvider, { client: queryClient }, children)

  return { Wrapper, queryClient }
}

describe('useUserList', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(userService.getUsers).mockResolvedValue(mockUserListResponse)
  })

  it('fetches users via React Query', async () => {
    const { Wrapper } = createWrapper()
    const params = { page: 1, pageSize: 10 }

    const { result } = renderHook(() => useUserList(params), { wrapper: Wrapper })

    await waitFor(() => {
      expect(result.current.data).toBeDefined()
    })

    expect(userService.getUsers).toHaveBeenCalledWith(params)
    expect(result.current.data?.users).toEqual([mockUser])
  })

  it('uses correct query key with params', async () => {
    const { Wrapper, queryClient } = createWrapper()
    const params = { page: 2, pageSize: 25, search: 'test' }

    const { result } = renderHook(() => useUserList(params), { wrapper: Wrapper })

    await waitFor(() => {
      expect(result.current.data).toBeDefined()
    })

    const cachedData = queryClient.getQueryData(userKeys.list(params))
    expect(cachedData).toBeDefined()
  })
})

describe('useUpdateUser', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(userService.updateUser).mockResolvedValue({
      ...mockUser,
      displayName: 'Updated Name',
    })
  })

  it('invalidates user lists on success', async () => {
    const { Wrapper, queryClient } = createWrapper()
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries')

    const { result } = renderHook(() => useUpdateUser(), { wrapper: Wrapper })

    await act(async () => {
      await result.current.mutateAsync({
        id: 'user-1',
        request: { displayName: 'Updated Name' },
      })
    })

    expect(invalidateSpy).toHaveBeenCalledWith({
      queryKey: userKeys.lists(),
    })
  })

  it('updates detail cache entry on success', async () => {
    const { Wrapper, queryClient } = createWrapper()
    const setDataSpy = vi.spyOn(queryClient, 'setQueryData')

    const { result } = renderHook(() => useUpdateUser(), { wrapper: Wrapper })

    await act(async () => {
      await result.current.mutateAsync({
        id: 'user-1',
        request: { displayName: 'Updated Name' },
      })
    })

    expect(setDataSpy).toHaveBeenCalledWith(
      userKeys.detail('user-1'),
      expect.objectContaining({ displayName: 'Updated Name' }),
    )
  })
})

describe('useChangeUserRole', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(userService.changeRole).mockResolvedValue({
      ...mockUser,
      systemRole: 'Admin',
    })
  })

  it('invalidates user lists on success', async () => {
    const { Wrapper, queryClient } = createWrapper()
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries')

    const { result } = renderHook(() => useChangeUserRole(), { wrapper: Wrapper })

    await act(async () => {
      await result.current.mutateAsync({
        id: 'user-1',
        request: { systemRole: 'Admin' },
      })
    })

    expect(invalidateSpy).toHaveBeenCalledWith({
      queryKey: userKeys.lists(),
    })
  })
})

describe('useDeactivateUser', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(userService.deactivateUser).mockResolvedValue({
      ...mockUser,
      status: 'Deactivated',
    })
  })

  it('invalidates user lists on success', async () => {
    const { Wrapper, queryClient } = createWrapper()
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries')

    const { result } = renderHook(() => useDeactivateUser(), { wrapper: Wrapper })

    await act(async () => {
      await result.current.mutateAsync('user-1')
    })

    expect(invalidateSpy).toHaveBeenCalledWith({
      queryKey: userKeys.lists(),
    })
  })
})

describe('useReactivateUser', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(userService.reactivateUser).mockResolvedValue({
      ...mockUser,
      status: 'Active',
    })
  })

  it('invalidates user lists on success', async () => {
    const { Wrapper, queryClient } = createWrapper()
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries')

    const { result } = renderHook(() => useReactivateUser(), { wrapper: Wrapper })

    await act(async () => {
      await result.current.mutateAsync('user-1')
    })

    expect(invalidateSpy).toHaveBeenCalledWith({
      queryKey: userKeys.lists(),
    })
  })
})

describe('userKeys factory', () => {
  it('produces correct key structure', () => {
    expect(userKeys.all).toEqual(['users'])
    expect(userKeys.lists()).toEqual(['users', 'list'])
    expect(userKeys.list({ page: 1 })).toEqual(['users', 'list', { page: 1 }])
    expect(userKeys.details()).toEqual(['users', 'detail'])
    expect(userKeys.detail('abc')).toEqual(['users', 'detail', 'abc'])
    expect(userKeys.memberships('xyz')).toEqual(['users', 'memberships', 'xyz'])
  })
})
