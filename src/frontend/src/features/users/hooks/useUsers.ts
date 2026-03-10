/**
 * User Management React Query Hooks
 *
 * Provides React Query hooks for user management operations with
 * automatic cache invalidation and optimistic updates.
 *
 * @module features/users/hooks
 * @see authentication/S10 View User List
 * @see authentication/S11 Edit User Details
 * @see authentication/S12 Deactivate User Account
 * @see authentication/S13 Global Role Assignment
 */
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { notify } from '@/shared/utils/notify'
import { userService } from '../services/userService'
import type {
  UserDto,
  UserListResponse,
  UpdateUserRequest,
  ChangeRoleRequest,
  UserMembershipDto,
} from '../types'
import type { UserListParams } from '../services/userService'

/**
 * Query key factory for users
 */
export const userKeys = {
  all: ['users'] as const,
  lists: () => [...userKeys.all, 'list'] as const,
  list: (params: UserListParams) => [...userKeys.lists(), params] as const,
  details: () => [...userKeys.all, 'detail'] as const,
  detail: (id: string) => [...userKeys.details(), id] as const,
  memberships: (id: string) => [...userKeys.all, 'memberships', id] as const,
}

/**
 * Hook to fetch a paginated list of users with optional filters.
 *
 * @param params Filter and pagination parameters
 */
export function useUserList(params: UserListParams) {
  return useQuery<UserListResponse, Error>({
    queryKey: userKeys.list(params),
    queryFn: () => userService.getUsers(params),
    // Keep previous data while fetching new page to prevent layout shifts
    placeholderData: previousData => previousData,
  })
}

/**
 * Hook to fetch organization memberships for a specific user.
 *
 * @param userId The user ID to fetch memberships for
 */
export function useUserMemberships(userId: string) {
  return useQuery<UserMembershipDto[], Error>({
    queryKey: userKeys.memberships(userId),
    queryFn: () => userService.getUserMemberships(userId),
    enabled: !!userId,
  })
}

/**
 * Hook to update a user's display name or email.
 */
export function useUpdateUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateUserRequest }) =>
      userService.updateUser(id, request),
    onSuccess: (updatedUser: UserDto) => {
      // Invalidate all user lists (filters/pages may vary)
      queryClient.invalidateQueries({ queryKey: userKeys.lists() })
      // Eager cache update from trusted mutation response (distinct from AR-P02).
      // SignalR event handlers must use invalidateQueries, but mutation onSuccess
      // callbacks may use setQueryData with the server-returned data.
      queryClient.setQueryData(userKeys.detail(updatedUser.id), updatedUser)
      notify.success('User updated')
    },
    onError: (err: unknown) => {
      const message = err instanceof Error ? err.message : 'Failed to update user'
      notify.error(message)
    },
  })
}

/**
 * Hook to change a user's global system role.
 */
export function useChangeUserRole() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: ChangeRoleRequest }) =>
      userService.changeRole(id, request),
    onSuccess: () => {
      // Invalidate all user lists to reflect role change
      queryClient.invalidateQueries({ queryKey: userKeys.lists() })
    },
    onError: (err: unknown) => {
      const axiosError = err as { response?: { data?: { error?: string } } }
      if (axiosError.response?.data?.error === 'last_administrator') {
        notify.error('Cannot remove the last Administrator. Assign another Administrator first.')
      } else {
        const message = err instanceof Error ? err.message : 'Failed to change user role'
        notify.error(message)
      }
    },
  })
}

/**
 * Hook to deactivate a user account.
 */
export function useDeactivateUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => userService.deactivateUser(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: userKeys.lists() })
      notify.success('User deactivated')
    },
    onError: (err: unknown) => {
      const message = err instanceof Error ? err.message : 'Failed to deactivate user'
      notify.error(message)
    },
  })
}

/**
 * Hook to reactivate a deactivated user account.
 */
export function useReactivateUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => userService.reactivateUser(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: userKeys.lists() })
      notify.success('User reactivated')
    },
    onError: (err: unknown) => {
      const message = err instanceof Error ? err.message : 'Failed to reactivate user'
      notify.error(message)
    },
  })
}
