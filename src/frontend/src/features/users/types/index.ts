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
 * Create user request for inline user creation
 * Used when creating users from exercise participants dialog
 */
export interface CreateUserRequest {
  /** User's display name */
  displayName: string;
  /** User's email address (login identifier) */
  email: string;
  /** Initial password for the account */
  password: string;
}

/**
 * Available system roles for user management
 * These are application-level roles, NOT exercise-specific HSEEP roles
 */
export const USER_ROLES = [
  'Admin',
  'Manager',
  'User',
] as const

export type UserRole = typeof USER_ROLES[number]

/**
 * Current user's profile including contact information
 */
export interface CurrentUserProfileDto {
  /** Unique user identifier */
  id: string;
  /** User's display name */
  displayName: string;
  /** User's email address */
  email: string;
  /** Optional phone number for EEG document generation */
  phoneNumber: string | null;
  /** System-level role */
  systemRole: string;
  /** Account status */
  status: string;
  /** Last login timestamp */
  lastLoginAt: string | null;
  /** Account creation timestamp */
  createdAt: string;
}

/**
 * Request to update current user's contact information
 */
export interface UpdateContactRequest {
  /** Phone number to update (null to clear) */
  phoneNumber: string | null;
}

/**
 * Response after updating contact information
 */
export interface UserContactDto {
  /** User ID */
  id: string;
  /** User's display name */
  displayName: string;
  /** User's email address */
  email: string;
  /** Updated phone number */
  phoneNumber: string | null;
  /** When the contact info was last updated */
  updatedAt: string;
}

/**
 * User membership in an organization
 */
export interface UserMembershipDto {
  /** Membership ID */
  id: string;
  /** User ID */
  userId: string;
  /** Organization ID */
  organizationId: string;
  /** Organization name */
  organizationName: string;
  /** Organization slug */
  organizationSlug: string;
  /** User's role in the organization */
  role: string;
  /** When the user joined */
  joinedAt: string;
  /** Whether this is the user's current org */
  isCurrent: boolean;
}
