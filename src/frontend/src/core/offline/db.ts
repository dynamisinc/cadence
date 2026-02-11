/**
 * Cadence Offline Database
 *
 * IndexedDB database for caching exercise data and queueing offline actions.
 * Uses Dexie.js for a cleaner API over raw IndexedDB.
 *
 * Schema:
 * - exercises: Cached exercise data
 * - phases: Exercise phases
 * - injects: Cached injects with sync status
 * - observations: Cached observations with sync status
 * - pendingActions: Queue of actions to sync when online
 * - syncMetadata: Tracking last sync times
 */

import Dexie, { type Table } from 'dexie'

// ============================================================================
// Types
// ============================================================================

/** Cached exercise data */
export interface CachedExercise {
  id: string
  name: string
  description?: string | null
  exerciseType: string
  status: string
  startDate?: string | null
  endDate?: string | null
  clockState?: string | null
  clockStartedAt?: string | null
  clockElapsedMs?: number | null
  updatedAt: string
  cachedAt: Date
}

/** Cached exercise phase */
export interface CachedPhase {
  id: string
  exerciseId: string
  name: string
  description?: string | null
  startTime?: string | null
  endTime?: string | null
  order: number
  updatedAt: string
  cachedAt: Date
}

/** Cached inject with sync status */
export interface CachedInject {
  id: string
  exerciseId: string
  mselId?: string | null
  injectNumber: number
  title: string
  description?: string | null
  expectedAction?: string | null
  status: string
  scheduledTime?: string | null
  actualTime?: string | null
  firedById?: string | null
  firedByName?: string | null
  skippedById?: string | null
  skippedByName?: string | null
  skipReason?: string | null
  from?: string | null
  to?: string | null
  method?: string | null
  injectType?: string | null
  updatedAt: string
  cachedAt: Date
  /** Whether this has local changes pending sync */
  pendingSync?: boolean
}

/** Cached observation with sync status */
export interface CachedObservation {
  id: string
  exerciseId: string
  injectId?: string | null
  content: string
  rating?: string | null
  recommendation?: string | null
  createdById?: string | null
  createdByName?: string | null
  updatedAt: string
  cachedAt: Date
  /** Whether this has local changes pending sync */
  pendingSync?: boolean
  /** Temporary ID for optimistic creates (before server assigns real ID) */
  tempId?: string
}

/** Sync status for cached photo blobs */
export type PhotoSyncStatus = 'pending' | 'uploading' | 'synced' | 'failed'

/** Cached photo blob with sync tracking */
export interface CachedPhoto {
  /** Local UUID (matches tempId used in pending actions) */
  id: string
  /** Exercise this photo belongs to */
  exerciseId: string
  /** Full-size compressed photo blob */
  blob: Blob
  /** 300px thumbnail blob */
  thumbnailBlob: Blob
  /** Original file name */
  fileName: string
  /** File size in bytes */
  fileSizeBytes: number
  /** Upload sync status */
  syncStatus: PhotoSyncStatus
  /** Client-generated idempotency key for duplicate prevention */
  idempotencyKey: string
  /** Wall clock UTC when captured */
  capturedAt: string
  /** Scenario time when captured (if clock was running) */
  scenarioTime?: string | null
  /** GPS latitude */
  latitude?: number | null
  /** GPS longitude */
  longitude?: number | null
  /** GPS accuracy in meters */
  locationAccuracy?: number | null
  /** Observation ID if linked */
  observationId?: string | null
  /** Server-assigned photo ID after successful sync */
  serverPhotoId?: string | null
  /** Server blob URI after successful sync */
  serverBlobUri?: string | null
  /** Server thumbnail URI after successful sync */
  serverThumbnailUri?: string | null
  /** When this was cached locally */
  cachedAt: Date
  /** Error message if sync failed */
  error?: string | null
  /** Whether this was a quick photo (creates observation) */
  isQuickPhoto: boolean
  /** Temp observation ID for quick photos */
  tempObservationId?: string | null
}

/** Types of actions that can be queued */
export type PendingActionType =
  | 'FIRE_INJECT'
  | 'SKIP_INJECT'
  | 'RESET_INJECT'
  | 'CREATE_OBSERVATION'
  | 'UPDATE_OBSERVATION'
  | 'DELETE_OBSERVATION'
  | 'UPLOAD_PHOTO'
  | 'QUICK_PHOTO'
  | 'UPDATE_PHOTO'
  | 'DELETE_PHOTO'

/** Status of a pending action */
export type PendingActionStatus = 'pending' | 'syncing' | 'failed'

/** A queued action to be synced when online */
export interface PendingAction {
  id?: number // Auto-increment
  type: PendingActionType
  exerciseId: string
  payload: unknown
  timestamp: Date
  retryCount: number
  status: PendingActionStatus
  error?: string
}

/** Sync metadata for tracking last sync times */
export interface SyncMetadata {
  key: string // e.g., 'exercise-{id}' or 'lastGlobalSync'
  lastSyncAt: Date
  etag?: string
}

// ============================================================================
// Database Class
// ============================================================================

export class CadenceDatabase extends Dexie {
  // Table declarations
  exercises!: Table<CachedExercise, string>
  phases!: Table<CachedPhase, string>
  injects!: Table<CachedInject, string>
  observations!: Table<CachedObservation, string>
  pendingActions!: Table<PendingAction, number>
  syncMetadata!: Table<SyncMetadata, string>
  photos!: Table<CachedPhoto, string>

  constructor() {
    super('CadenceDB')

    // Schema version 1
    this.version(1).stores({
      exercises: 'id, updatedAt, cachedAt',
      phases: 'id, exerciseId, updatedAt',
      injects: 'id, exerciseId, mselId, status, updatedAt, pendingSync',
      observations: 'id, exerciseId, injectId, updatedAt, pendingSync, tempId',
      pendingActions: '++id, exerciseId, status, type, timestamp',
      syncMetadata: 'key, lastSyncAt',
    })

    // Schema version 2 - Add dedicated photo blob storage
    this.version(2).stores({
      exercises: 'id, updatedAt, cachedAt',
      phases: 'id, exerciseId, updatedAt',
      injects: 'id, exerciseId, mselId, status, updatedAt, pendingSync',
      observations: 'id, exerciseId, injectId, updatedAt, pendingSync, tempId',
      pendingActions: '++id, exerciseId, status, type, timestamp',
      syncMetadata: 'key, lastSyncAt',
      photos: 'id, exerciseId, syncStatus, capturedAt, cachedAt, idempotencyKey',
    })
  }
}

// ============================================================================
// Singleton Instance
// ============================================================================

export const db = new CadenceDatabase()

// ============================================================================
// Helper Functions
// ============================================================================

/**
 * Clear all cached data for a specific exercise
 */
export async function clearExerciseCache(exerciseId: string): Promise<void> {
  await db.transaction(
    'rw',
    [db.exercises, db.phases, db.injects, db.observations, db.photos, db.syncMetadata],
    async () => {
      await db.exercises.delete(exerciseId)
      await db.phases.where('exerciseId').equals(exerciseId).delete()
      await db.injects.where('exerciseId').equals(exerciseId).delete()
      await db.observations.where('exerciseId').equals(exerciseId).delete()
      await db.photos.where('exerciseId').equals(exerciseId).delete()
      await db.syncMetadata.delete(`exercise-${exerciseId}`)
    },
  )
}

/**
 * Clear all cached data (e.g., on logout)
 */
export async function clearAllCache(): Promise<void> {
  await db.transaction(
    'rw',
    [db.exercises, db.phases, db.injects, db.observations, db.photos, db.pendingActions, db.syncMetadata],
    async () => {
      await db.exercises.clear()
      await db.phases.clear()
      await db.injects.clear()
      await db.observations.clear()
      await db.photos.clear()
      await db.pendingActions.clear()
      await db.syncMetadata.clear()
    },
  )
}

/**
 * Get pending action count for an exercise (excludes failed actions)
 */
export async function getPendingActionCount(exerciseId?: string): Promise<number> {
  if (exerciseId) {
    return db.pendingActions
      .where('exerciseId')
      .equals(exerciseId)
      .and(action => action.status !== 'failed')
      .count()
  }
  return db.pendingActions
    .where('status')
    .notEqual('failed')
    .count()
}

/**
 * Get all pending actions in FIFO order
 */
export async function getPendingActions(exerciseId?: string): Promise<PendingAction[]> {
  if (exerciseId) {
    return db.pendingActions.where('exerciseId').equals(exerciseId).sortBy('timestamp')
  }
  return db.pendingActions.orderBy('timestamp').toArray()
}

/**
 * Add a pending action to the queue
 */
export async function addPendingAction(
  action: Omit<PendingAction, 'id' | 'timestamp' | 'retryCount' | 'status'>,
): Promise<number> {
  return db.pendingActions.add({
    ...action,
    timestamp: new Date(),
    retryCount: 0,
    status: 'pending',
  })
}

/**
 * Update pending action status
 */
export async function updatePendingActionStatus(
  id: number,
  status: PendingActionStatus,
  error?: string,
): Promise<void> {
  await db.pendingActions.update(id, { status, error })
}

/**
 * Increment retry count for a pending action
 */
export async function incrementRetryCount(id: number): Promise<void> {
  await db.pendingActions
    .where('id')
    .equals(id)
    .modify(action => {
      action.retryCount++
    })
}

/**
 * Delete a pending action (after successful sync or manual discard)
 */
export async function deletePendingAction(id: number): Promise<void> {
  await db.pendingActions.delete(id)
}

/**
 * Delete all failed pending actions (after user acknowledges conflicts)
 */
export async function deleteFailedActions(): Promise<number> {
  return db.pendingActions.where('status').equals('failed').delete()
}

/**
 * Prune old cached data (older than specified days)
 */
export async function pruneOldCache(daysOld: number = 7): Promise<void> {
  const cutoff = new Date()
  cutoff.setDate(cutoff.getDate() - daysOld)

  await db.transaction(
    'rw',
    [db.exercises, db.phases, db.injects, db.observations, db.syncMetadata],
    async () => {
      // Get exercises older than cutoff
      const oldExercises = await db.exercises.where('cachedAt').below(cutoff).toArray()

      for (const exercise of oldExercises) {
        await clearExerciseCache(exercise.id)
      }
    },
  )
}

/**
 * Get storage estimate (how much space we're using)
 */
export async function getStorageEstimate(): Promise<{ usage: number; quota: number } | null> {
  if (navigator.storage && navigator.storage.estimate) {
    const estimate = await navigator.storage.estimate()
    return {
      usage: estimate.usage ?? 0,
      quota: estimate.quota ?? 0,
    }
  }
  return null
}

export default db
