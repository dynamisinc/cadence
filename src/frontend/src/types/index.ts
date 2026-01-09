/**
 * Shared TypeScript types for Cadence
 */

/**
 * Permission Role for access control
 * Used by ProfileMenu for client-side role switching (demo/testing)
 */
export const PermissionRole = {
  READONLY: 'Readonly',
  CONTRIBUTOR: 'Contributor',
  MANAGE: 'Manage',
} as const

export type PermissionRole = (typeof PermissionRole)[keyof typeof PermissionRole]

/**
 * Mock user profile stored in localStorage
 */
export interface MockUserProfile {
  role: PermissionRole;
  email: string;
  fullName: string;
}
