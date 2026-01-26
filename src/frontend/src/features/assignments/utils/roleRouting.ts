/**
 * Role-Based Routing Utility
 *
 * Determines the default landing page for a user based on their role and exercise status.
 */

/**
 * Active exercise statuses that warrant role-based routing.
 */
const ACTIVE_STATUSES = ['Active', 'Paused']

/**
 * Check if an exercise status is considered active.
 */
export function isActiveStatus(status: string): boolean {
  return ACTIVE_STATUSES.includes(status)
}

/**
 * Get the default route for a user's role in an exercise.
 *
 * @param exerciseId - The exercise ID
 * @param role - The user's HSEEP role
 * @param exerciseStatus - The current exercise status
 * @returns The route path to navigate to
 */
export function getDefaultRouteForRole(
  exerciseId: string,
  role: string,
  exerciseStatus: string,
): string {
  // Draft exercises go to detail/setup page
  if (exerciseStatus === 'Draft') {
    return `/exercises/${exerciseId}`
  }

  // Non-active exercises go to hub (overview)
  if (!isActiveStatus(exerciseStatus)) {
    return `/exercises/${exerciseId}`
  }

  // Active exercises route by role
  switch (role) {
    case 'Controller':
      // Controllers go to inject queue/conduct page
      return `/exercises/${exerciseId}/conduct`

    case 'Evaluator':
      // Evaluators go to observations
      return `/exercises/${exerciseId}/observations`

    case 'ExerciseDirector':
    case 'Administrator':
    case 'Observer':
    default:
      // Directors, Admins, and Observers go to conduct hub
      return `/exercises/${exerciseId}/conduct`
  }
}

/**
 * Role display labels for UI.
 */
export const ROLE_LABELS: Record<string, string> = {
  Administrator: 'Administrator',
  ExerciseDirector: 'Exercise Director',
  Controller: 'Controller',
  Evaluator: 'Evaluator',
  Observer: 'Observer',
}

/**
 * Get user-friendly role label.
 */
export function getRoleLabel(role: string): string {
  return ROLE_LABELS[role] || role
}

/**
 * Role colors for badges.
 */
export const ROLE_COLORS: Record<
  string,
  'primary' | 'secondary' | 'success' | 'warning' | 'info' | 'error'
> = {
  Administrator: 'error',
  ExerciseDirector: 'primary',
  Controller: 'success',
  Evaluator: 'info',
  Observer: 'secondary',
}

/**
 * Get color for role badge.
 */
export function getRoleColor(
  role: string,
): 'primary' | 'secondary' | 'success' | 'warning' | 'info' | 'error' {
  return ROLE_COLORS[role] || 'secondary'
}
