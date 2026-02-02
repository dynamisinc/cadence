/**
 * User Management Service
 *
 * API client for user management operations.
 * Handles all HTTP communication with the user management backend.
 *
 * @module features/users/services
 */

import { apiClient } from '../../../core/services/api'
import type {
  UserDto,
  UserListResponse,
  UpdateUserRequest,
  ChangeRoleRequest,
  CreateUserRequest,
  UserMembershipDto,
} from '../types'

/**
 * Query parameters for user list
 */
export interface UserListParams {
  /** Page number (1-indexed) */
  page?: number;
  /** Number of items per page */
  pageSize?: number;
  /** Search query (name or email) */
  search?: string;
  /** Filter by system role */
  role?: string;
  /** Filter by status (Active, Inactive, Pending) */
  status?: string;
  /** Filter by organization membership (Admin only) */
  organizationId?: string;
}

/**
 * User management API client
 */
export const userService = {
  /**
   * Get paginated list of users with optional filters
   * @param params Query parameters for filtering and pagination
   * @returns Paginated user list
   */
  async getUsers(params: UserListParams): Promise<UserListResponse> {
    const response = await apiClient.get<UserListResponse>('/users', { params })
    return response.data
  },

  /**
   * Get a single user by ID
   * @param id User ID
   * @returns User details
   */
  async getUser(id: string): Promise<UserDto> {
    const response = await apiClient.get<UserDto>(`/users/${id}`)
    return response.data
  },

  /**
   * Create a new user account
   * Used for inline user creation from exercise participants dialog
   * @param request User creation request with display name, email, and password
   * @returns Created user
   * @throws Error with status 409 if email already exists
   */
  async createUser(request: CreateUserRequest): Promise<UserDto> {
    const response = await apiClient.post<UserDto>('/users', request)
    return response.data
  },

  /**
   * Update user details (display name and/or email)
   * @param id User ID
   * @param request Update request with changed fields
   * @returns Updated user
   */
  async updateUser(id: string, request: UpdateUserRequest): Promise<UserDto> {
    const response = await apiClient.put<UserDto>(`/users/${id}`, request)
    return response.data
  },

  /**
   * Change user's global role
   * @param id User ID
   * @param request Role change request
   * @returns Updated user
   */
  async changeRole(id: string, request: ChangeRoleRequest): Promise<UserDto> {
    const response = await apiClient.patch<UserDto>(`/users/${id}/role`, request)
    return response.data
  },

  /**
   * Deactivate user account
   * @param id User ID
   * @param reason Optional deactivation reason
   * @returns Updated user with Deactivated status
   */
  async deactivateUser(id: string, reason?: string): Promise<UserDto> {
    const response = await apiClient.post<UserDto>(`/users/${id}/deactivate`, { reason })
    return response.data
  },

  /**
   * Reactivate deactivated user account
   * @param id User ID
   * @returns Updated user with Active status
   */
  async reactivateUser(id: string): Promise<UserDto> {
    const response = await apiClient.post<UserDto>(`/users/${id}/reactivate`)
    return response.data
  },

  /**
   * Get a user's organization memberships (admin only)
   * @param id User ID
   * @returns List of organization memberships
   */
  async getUserMemberships(id: string): Promise<UserMembershipDto[]> {
    const response = await apiClient.get<UserMembershipDto[]>(`/users/${id}/memberships`)
    return response.data
  },
}
