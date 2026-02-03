/**
 * Shared TypeScript types for Cadence
 */

// =============================================================================
// System Role Types (matches backend SystemRole enum)
// =============================================================================

/**
 * System-level access roles determining application permissions.
 * These are distinct from HSEEP exercise roles (HseepRole).
 * Values match the SystemRole enum in the backend.
 */
export const SystemRole = {
  /** Standard user - can only access exercises they are assigned to */
  User: 'User',
  /** Can create exercises and manage exercises they create/own */
  Manager: 'Manager',
  /** Full system access - user management, all exercises, system settings */
  Admin: 'Admin',
} as const

export type SystemRole = (typeof SystemRole)[keyof typeof SystemRole]

// =============================================================================
// HSEEP Role Types (matches backend ExerciseRole enum)
// =============================================================================

/**
 * HSEEP-aligned roles for exercise participation.
 * Values match the ExerciseRole enum in the backend.
 */
export const HseepRole = {
  /** System-wide configuration and user management (DEPRECATED - use SystemRole.Admin instead) */
  Administrator: 'Administrator',
  /** Full exercise management authority, Go/No-Go decisions */
  ExerciseDirector: 'ExerciseDirector',
  /** Delivers injects, manages scenario flow */
  Controller: 'Controller',
  /** Observes and documents player performance */
  Evaluator: 'Evaluator',
  /** Watches without interfering */
  Observer: 'Observer',
} as const

export type HseepRole = (typeof HseepRole)[keyof typeof HseepRole]

/**
 * HSEEP Role metadata (matches HseepRole entity from backend)
 */
export interface HseepRoleInfo {
  id: number;
  code: HseepRole;
  name: string;
  description: string;
  sortOrder: number;
  isSystemWide: boolean;
  canFireInjects: boolean;
  canRecordObservations: boolean;
  canManageExercise: boolean;
  isActive: boolean;
}

/**
 * Static HSEEP role definitions for client-side use.
 * Mirrors the seeded data in the database.
 */
export const HSEEP_ROLES: readonly HseepRoleInfo[] = [
  {
    id: 1,
    code: HseepRole.Administrator,
    name: 'Administrator',
    description: 'System-wide configuration and user management. Has full access to all exercises within their organization.',
    sortOrder: 1,
    isSystemWide: true,
    canFireInjects: true,
    canRecordObservations: true,
    canManageExercise: true,
    isActive: true,
  },
  {
    id: 2,
    code: HseepRole.ExerciseDirector,
    name: 'Exercise Director',
    description: 'Full exercise management authority. Responsible for Go/No-Go decisions and overall exercise conduct.',
    sortOrder: 2,
    isSystemWide: false,
    canFireInjects: true,
    canRecordObservations: true,
    canManageExercise: true,
    isActive: true,
  },
  {
    id: 3,
    code: HseepRole.Controller,
    name: 'Controller',
    description: 'Delivers injects to players and manages scenario flow during exercise conduct.',
    sortOrder: 3,
    isSystemWide: false,
    canFireInjects: true,
    canRecordObservations: false,
    canManageExercise: false,
    isActive: true,
  },
  {
    id: 4,
    code: HseepRole.Evaluator,
    name: 'Evaluator',
    description: 'Observes and documents player performance for the After-Action Report (AAR).',
    sortOrder: 4,
    isSystemWide: false,
    canFireInjects: false,
    canRecordObservations: true,
    canManageExercise: false,
    isActive: true,
  },
  {
    id: 5,
    code: HseepRole.Observer,
    name: 'Observer',
    description: 'Watches exercise conduct without interfering. Read-only access to exercise data.',
    sortOrder: 5,
    isSystemWide: false,
    canFireInjects: false,
    canRecordObservations: false,
    canManageExercise: false,
    isActive: true,
  },
] as const

/**
 * Helper function to get role info by code
 */
export const getHseepRoleInfo = (code: HseepRole): HseepRoleInfo | undefined => {
  return HSEEP_ROLES.find(role => role.code === code)
}

/**
 * Helper function to check if a role can fire injects
 */
export const canRoleFireInjects = (code: HseepRole): boolean => {
  return getHseepRoleInfo(code)?.canFireInjects ?? false
}

/**
 * Helper function to check if a role can record observations
 */
export const canRoleRecordObservations = (code: HseepRole): boolean => {
  return getHseepRoleInfo(code)?.canRecordObservations ?? false
}

/**
 * Helper function to check if a role can manage exercises
 */
export const canRoleManageExercise = (code: HseepRole): boolean => {
  return getHseepRoleInfo(code)?.canManageExercise ?? false
}

// =============================================================================
// Legacy Permission Role Types (for demo/testing UI)
// =============================================================================

/**
 * Permission Role for access control
 * Used by ProfileMenu for client-side role switching (demo/testing)
 *
 * @deprecated Use HseepRole for HSEEP-compliant role checking.
 * This is retained for backwards compatibility with the demo ProfileMenu.
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

// =============================================================================
// Exercise Types (matches backend enums)
// =============================================================================

/**
 * Types of exercises per HSEEP classification
 */
export const ExerciseType = {
  /** Table Top Exercise - Discussion-based scenario walkthrough */
  TTX: 'TTX',
  /** Functional Exercise - Simulated operations in controlled environment */
  FE: 'FE',
  /** Full-Scale Exercise - Actual deployment of resources */
  FSE: 'FSE',
  /** Computer-Aided Exercise - Technology-driven simulation */
  CAX: 'CAX',
  /** Hybrid Exercise - Combination of multiple types */
  Hybrid: 'Hybrid',
} as const

export type ExerciseType = (typeof ExerciseType)[keyof typeof ExerciseType]

/**
 * Exercise lifecycle status
 */
export const ExerciseStatus = {
  /** Initial creation state. Setup phase - can edit everything. */
  Draft: 'Draft',
  /** Currently in conduct. Clock can run, injects can fire. */
  Active: 'Active',
  /** Temporarily stopped. Clock paused, can resume or revert to draft. */
  Paused: 'Paused',
  /** Conduct finished. Read-only except observations. */
  Completed: 'Completed',
  /** Read-only historical record. Fully read-only. */
  Archived: 'Archived',
} as const

export type ExerciseStatus = (typeof ExerciseStatus)[keyof typeof ExerciseStatus]

// =============================================================================
// Inject Types (matches backend enums)
// =============================================================================

/**
 * Types of injects based on their purpose
 */
export const InjectType = {
  /** Standard - Delivered at scheduled time */
  Standard: 'Standard',
  /** Contingency - Used if players deviate from expected path */
  Contingency: 'Contingency',
  /** Adaptive - Branch based on player decision */
  Adaptive: 'Adaptive',
  /** Complexity - Increase difficulty for advanced players */
  Complexity: 'Complexity',
} as const

export type InjectType = (typeof InjectType)[keyof typeof InjectType]

/**
 * HSEEP-compliant inject status values per FEMA PrepToolkit.
 * These statuses align with standard exercise management terminology
 * to ensure consistency with federal guidance and training materials.
 */
export const InjectStatus = {
  /** Initial status during design and development phase. Inject is being authored. */
  Draft: 'Draft',
  /** Event has been sent for review by Exercise Director. Awaiting approval. */
  Submitted: 'Submitted',
  /** Event has been approved for use. Director has signed off on the content. */
  Approved: 'Approved',
  /** Approved event is ready and scheduled for a specific time. */
  Synchronized: 'Synchronized',
  /** Event has been delivered to players in real time. Controller has "fired" the inject. */
  Released: 'Released',
  /** Event delivery confirmed, exercise has moved past this inject. */
  Complete: 'Complete',
  /** A synchronized event that was cancelled before delivery. */
  Deferred: 'Deferred',
  /** Event should be ignored but remains in MSEL for audit trail. */
  Obsolete: 'Obsolete',
} as const

export type InjectStatus = (typeof InjectStatus)[keyof typeof InjectStatus]

/**
 * Statuses that indicate an inject is "active" (not terminal).
 */
export const ACTIVE_INJECT_STATUSES: InjectStatus[] = [
  InjectStatus.Draft,
  InjectStatus.Submitted,
  InjectStatus.Approved,
  InjectStatus.Synchronized,
]

/**
 * Statuses that indicate an inject is "terminal" (conduct complete).
 */
export const TERMINAL_INJECT_STATUSES: InjectStatus[] = [
  InjectStatus.Released,
  InjectStatus.Complete,
  InjectStatus.Deferred,
  InjectStatus.Obsolete,
]

/**
 * Methods for delivering injects to players
 * @deprecated Use DeliveryMethodLookup instead. Kept for backward compatibility.
 */
export const DeliveryMethod = {
  /** Spoken directly to player */
  Verbal: 'Verbal',
  /** Simulated phone call */
  Phone: 'Phone',
  /** Simulated email */
  Email: 'Email',
  /** Radio communication */
  Radio: 'Radio',
  /** Paper document */
  Written: 'Written',
  /** CAX/simulation input */
  Simulation: 'Simulation',
  /** Custom method */
  Other: 'Other',
} as const

export type DeliveryMethod = (typeof DeliveryMethod)[keyof typeof DeliveryMethod]

/**
 * How an inject is triggered during the exercise
 */
export const TriggerType = {
  /** Controller manually fires */
  Manual: 'Manual',
  /** Auto-fire at scheduled time (future feature) */
  Scheduled: 'Scheduled',
  /** Fire when conditions are met (future feature) */
  Conditional: 'Conditional',
} as const

export type TriggerType = (typeof TriggerType)[keyof typeof TriggerType]

// =============================================================================
// Exercise Clock Types (matches backend enums)
// =============================================================================

/**
 * State of the exercise clock during conduct
 */
export const ExerciseClockState = {
  /** Clock not started - exercise not yet in conduct */
  Stopped: 'Stopped',
  /** Clock actively running - exercise in progress */
  Running: 'Running',
  /** Clock temporarily paused - exercise on hold */
  Paused: 'Paused',
} as const

export type ExerciseClockState = (typeof ExerciseClockState)[keyof typeof ExerciseClockState]

/**
 * Delivery mode determines how injects transition to Ready status
 */
export const DeliveryMode = {
  /** Injects become Ready when exercise clock reaches DeliveryTime */
  ClockDriven: 'ClockDriven',
  /** Injects are fired manually by Controller in Sequence order */
  FacilitatorPaced: 'FacilitatorPaced',
} as const

export type DeliveryMode = (typeof DeliveryMode)[keyof typeof DeliveryMode]

/**
 * Timeline mode determines how exercise time relates to story time
 */
export const TimelineMode = {
  /** 1:1 ratio - exercise time matches wall clock */
  RealTime: 'RealTime',
  /** Story time advances faster than real time per TimeScale */
  Compressed: 'Compressed',
  /** No real-time clock; only Story Time is used */
  StoryOnly: 'StoryOnly',
} as const

export type TimelineMode = (typeof TimelineMode)[keyof typeof TimelineMode]

// =============================================================================
// Observation Types (matches backend enums)
// =============================================================================

/**
 * HSEEP performance rating scale (P/S/M/U) for evaluator observations
 */
export const ObservationRating = {
  /** P - Performed without challenges */
  Performed: 'Performed',
  /** S - Performed with some difficulty */
  Satisfactory: 'Satisfactory',
  /** M - Performed with major difficulty */
  Marginal: 'Marginal',
  /** U - Unable to be performed */
  Unsatisfactory: 'Unsatisfactory',
} as const

export type ObservationRating = (typeof ObservationRating)[keyof typeof ObservationRating]

/**
 * Human-readable labels for observation ratings
 */
export const ObservationRatingLabels: Record<ObservationRating, string> = {
  [ObservationRating.Performed]: 'P - Performed',
  [ObservationRating.Satisfactory]: 'S - Satisfactory',
  [ObservationRating.Marginal]: 'M - Marginal',
  [ObservationRating.Unsatisfactory]: 'U - Unsatisfactory',
}

/**
 * Short labels for observation ratings (for badges)
 */
export const ObservationRatingShortLabels: Record<ObservationRating, string> = {
  [ObservationRating.Performed]: 'P',
  [ObservationRating.Satisfactory]: 'S',
  [ObservationRating.Marginal]: 'M',
  [ObservationRating.Unsatisfactory]: 'U',
}

/**
 * Helper function to get human-readable label for an observation rating
 */
export const getObservationRatingLabel = (rating: ObservationRating): string => {
  return ObservationRatingLabels[rating] ?? rating
}
