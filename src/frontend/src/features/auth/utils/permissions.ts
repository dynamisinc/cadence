/**
 * Permission Utilities
 *
 * Helper functions for role-based permission checks and display
 *
 * @module features/auth
 */
import type { ExerciseRole, Permission } from '../constants/rolePermissions';
import { ROLE_PERMISSIONS } from '../constants/rolePermissions';

/**
 * Check if a role has a specific permission
 *
 * @param role - Exercise role to check
 * @param permission - Permission to check for
 * @returns True if role has permission
 */
export function hasPermission(role: ExerciseRole, permission: Permission): boolean {
  const permissions = ROLE_PERMISSIONS[role];
  if (!permissions) return false;
  return permissions.includes(permission);
}

/**
 * Get user-friendly display name for a role
 *
 * @param role - Exercise role
 * @returns Human-readable role name
 */
export function getRoleDisplayName(role: ExerciseRole): string {
  const displayNames: Record<ExerciseRole, string> = {
    Administrator: 'Administrator',
    ExerciseDirector: 'Exercise Director',
    Controller: 'Controller',
    Evaluator: 'Evaluator',
    Observer: 'Observer',
  };

  return displayNames[role] || 'Unknown';
}

/**
 * Get role description for tooltips
 *
 * @param role - Exercise role
 * @returns Description of role responsibilities
 */
export function getRoleDescription(role: ExerciseRole): string {
  const descriptions: Record<ExerciseRole, string> = {
    Administrator: 'Full system access - user management, all exercises, system settings',
    ExerciseDirector: 'Overall exercise authority - can manage all aspects of the exercise',
    Controller: 'Delivers injects and manages scenario flow during exercise conduct',
    Evaluator: 'Records observations and documents player performance',
    Observer: 'Can observe and watch the exercise without interfering or making changes',
  };

  return descriptions[role] || 'Unknown role';
}

/**
 * Get MUI color for role badge
 *
 * @param role - Exercise role
 * @returns MUI chip color
 */
export function getRoleColor(
  role: ExerciseRole
): 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning' {
  const colors: Record<ExerciseRole, 'default' | 'primary' | 'error' | 'success'> = {
    Administrator: 'error', // Red for highest authority
    ExerciseDirector: 'error', // Red for exercise authority
    Controller: 'primary', // Blue for active role
    Evaluator: 'success', // Green for observer role
    Observer: 'default', // Gray for read-only
  };

  return colors[role] || 'default';
}
