/**
 * Role Permission Matrix
 *
 * Defines which exercise roles can perform which actions.
 * Implements HSEEP role hierarchy:
 * Observer < Evaluator < Controller < ExerciseDirector < Administrator
 *
 * @module features/auth
 */

/**
 * Available actions in the system
 */
export type Permission =
  | 'view_exercise'
  | 'add_observation'
  | 'delete_observation'
  | 'fire_inject'
  | 'edit_inject'
  | 'approve_inject'
  | 'manage_participants'
  | 'edit_exercise'
  | 'delete_exercise'
  | 'start_clock'
  | 'pause_clock'
  | 'set_clock_time'

/**
 * Exercise roles matching backend ExerciseRole enum
 */
export type ExerciseRole =
  | 'Administrator'
  | 'ExerciseDirector'
  | 'Controller'
  | 'Evaluator'
  | 'Observer'

/**
 * System roles matching backend SystemRole enum
 */
export type SystemRole = 'Admin' | 'Manager' | 'User'

/**
 * Role permission mapping
 * Higher roles inherit all permissions from lower roles
 */
export const ROLE_PERMISSIONS: Record<ExerciseRole, Permission[]> = {
  Observer: ['view_exercise'],

  Evaluator: ['view_exercise', 'add_observation'],

  Controller: ['view_exercise', 'add_observation', 'fire_inject', 'edit_inject'],

  ExerciseDirector: [
    'view_exercise',
    'add_observation',
    'delete_observation',
    'fire_inject',
    'edit_inject',
    'approve_inject',
    'manage_participants',
    'edit_exercise',
    'start_clock',
    'pause_clock',
    'set_clock_time',
  ],

  Administrator: [
    'view_exercise',
    'add_observation',
    'delete_observation',
    'fire_inject',
    'edit_inject',
    'approve_inject',
    'manage_participants',
    'edit_exercise',
    'delete_exercise',
    'start_clock',
    'pause_clock',
    'set_clock_time',
  ],
}

/**
 * Role hierarchy values for comparison
 * Higher number = more permissions
 */
export const ROLE_HIERARCHY: Record<ExerciseRole, number> = {
  Observer: 1,
  Evaluator: 2,
  Controller: 3,
  ExerciseDirector: 4,
  Administrator: 5,
}
