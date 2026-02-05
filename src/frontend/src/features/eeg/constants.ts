/**
 * EEG Feature Constants
 *
 * Shared constants for EEG forms and validation.
 */

/** Field limits for capability target form */
export const CAPABILITY_TARGET_FIELD_LIMITS = {
  targetDescription: { min: 10, max: 500 },
}

/** Field limits for critical task form */
export const CRITICAL_TASK_FIELD_LIMITS = {
  taskDescription: { min: 5, max: 500 },
  standard: { max: 500 },
}
