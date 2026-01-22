/**
 * User Management Types
 *
 * TypeScript definitions for user management data structures.
 *
 * @module features/users/types
 */

/**
 * User data transfer object
 */
export interface UserDto {
  /** Unique user identifier */
  id: string;
  /** User's email address (login identifier) */
  email: string;
  /** User's display name */
  displayName: string;
  /** System-level role assigned to user (Admin, Manager, User) */
  systemRole: string;
  /** Account status (Active/Deactivated) */
  status: string;
  /** Last login timestamp (ISO 8601) */
  lastLoginAt: string | null;
  /** Account creation timestamp (ISO 8601) */
  createdAt: string;
}

/**
 * Paginated list response
 */
export interface UserListResponse {
  /** Array of users */
  users: UserDto[];
  /** Pagination metadata */
  pagination: {
    /** Current page (1-indexed) */
    page: number;
    /** Number of items per page */
    pageSize: number;
    /** Total number of users */
    totalCount: number;
    /** Total number of pages */
    totalPages: number;
  };
}

/**
 * Update user request (display name and/or email)
 */
export interface UpdateUserRequest {
  /** New display name */
  displayName?: string;
  /** New email address */
  email?: string;
}

/**
 * Change user role request
 */
export interface ChangeRoleRequest {
  /** New system role to assign (Admin, Manager, User) */
  systemRole: string;
}

/**
 * Available system roles for user management
 * These are application-level roles, NOT exercise-specific HSEEP roles
 */
export const USER_ROLES = [
  'Admin',
  'Manager',
  'User',
] as const;

export type UserRole = typeof USER_ROLES[number];
