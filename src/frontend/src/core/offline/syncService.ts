/**
 * Sync Service
 *
 * Handles syncing offline actions with the server when connection is restored.
 * Processes the pending action queue in FIFO order.
 */

import {
  db,
  getPendingActions,
  updatePendingActionStatus,
  incrementRetryCount,
  deletePendingAction,
  type PendingAction,
  type PendingActionType,
} from './db'
import { updateCachedInject, updateCachedObservation, deleteCachedObservation } from './cacheService'
import { injectService } from '../../features/injects/services/injectService'
import { observationService } from '../../features/observations/services/observationService'

// ============================================================================
// Types
// ============================================================================

export interface SyncResult {
  totalActions: number
  succeeded: number
  failed: number
  failedActions: PendingAction[]
}

export interface ConflictInfo {
  actionId: number
  type: PendingActionType
  message: string
  conflictingUser?: string
  conflictingTimestamp?: Date
}

export type SyncStatus = 'idle' | 'syncing' | 'completed' | 'partial' | 'failed'

export interface SyncProgress {
  status: SyncStatus
  current: number
  total: number
  conflicts: ConflictInfo[]
}

// ============================================================================
// Sync State
// ============================================================================

let currentSyncStatus: SyncStatus = 'idle'
let syncAbortController: AbortController | null = null

export function getSyncStatus(): SyncStatus {
  return currentSyncStatus
}

export function cancelSync(): void {
  if (syncAbortController) {
    syncAbortController.abort()
    syncAbortController = null
  }
}

// ============================================================================
// Action Handlers
// ============================================================================

interface FireInjectPayload {
  injectId: string
  firedAt: string
}

interface SkipInjectPayload {
  injectId: string
  reason?: string
  skippedAt: string
}

interface ResetInjectPayload {
  injectId: string
}

interface CreateObservationPayload {
  observation: {
    content: string
    rating?: string
    recommendation?: string
    injectId?: string
  }
  tempId: string
}

interface UpdateObservationPayload {
  observationId: string
  changes: {
    content?: string
    rating?: string
    recommendation?: string
    injectId?: string
  }
}

interface DeleteObservationPayload {
  observationId: string
}

/**
 * Process a single pending action
 * Returns true if successful, throws on conflict/error
 */
async function processAction(action: PendingAction): Promise<void> {
  const { type, exerciseId, payload } = action

  switch (type) {
    case 'FIRE_INJECT': {
      const { injectId, firedAt } = payload as FireInjectPayload
      try {
        await injectService.fireInject(exerciseId, injectId, { firedAt })
        // Update local cache to remove pending flag
        await updateCachedInject(injectId, { pendingSync: false })
      } catch (error) {
        if (isConflictError(error)) {
          // Inject was already fired - this is expected in conflict scenarios
          await updateCachedInject(injectId, { pendingSync: false })
          throw error
        }
        throw error
      }
      break
    }

    case 'SKIP_INJECT': {
      const { injectId, reason } = payload as SkipInjectPayload
      try {
        await injectService.skipInject(exerciseId, injectId, { reason })
        await updateCachedInject(injectId, { pendingSync: false })
      } catch (error) {
        if (isConflictError(error)) {
          await updateCachedInject(injectId, { pendingSync: false })
          throw error
        }
        throw error
      }
      break
    }

    case 'RESET_INJECT': {
      const { injectId } = payload as ResetInjectPayload
      await injectService.resetInject(exerciseId, injectId)
      await updateCachedInject(injectId, { pendingSync: false })
      break
    }

    case 'CREATE_OBSERVATION': {
      const { observation, tempId } = payload as CreateObservationPayload
      const created = await observationService.createObservation(exerciseId, observation)
      // Replace temp observation with real one in cache
      await db.transaction('rw', db.observations, async () => {
        await db.observations.where('tempId').equals(tempId).delete()
        await db.observations.put({
          id: created.id,
          exerciseId,
          injectId: created.injectId ?? null,
          content: created.content,
          rating: created.rating ?? null,
          recommendation: created.recommendation ?? null,
          createdById: created.createdBy ?? null,
          createdByName: created.createdByName ?? null,
          updatedAt: created.updatedAt,
          cachedAt: new Date(),
          pendingSync: false,
        })
      })
      break
    }

    case 'UPDATE_OBSERVATION': {
      const { observationId, changes } = payload as UpdateObservationPayload
      try {
        await observationService.updateObservation(observationId, changes)
        await updateCachedObservation(observationId, { pendingSync: false })
      } catch (error) {
        if (isNotFoundError(error)) {
          // Observation was deleted - remove from local cache
          await deleteCachedObservation(observationId)
          throw error
        }
        throw error
      }
      break
    }

    case 'DELETE_OBSERVATION': {
      const { observationId } = payload as DeleteObservationPayload
      try {
        await observationService.deleteObservation(observationId)
      } catch (error) {
        // 404 is OK - observation was already deleted
        if (!isNotFoundError(error)) {
          throw error
        }
      }
      // Remove from local cache regardless
      await deleteCachedObservation(observationId)
      break
    }

    default:
      throw new Error(`Unknown action type: ${type}`)
  }
}

// ============================================================================
// Error Helpers
// ============================================================================

/**
 * Extract error message from various error formats (axios, Error, etc.)
 */
function getErrorMessage(error: unknown): string {
  // Handle axios errors - the actual message is in response.data.message
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const axiosError = error as any
  if (axiosError?.response?.data?.message) {
    return axiosError.response.data.message
  }
  if (error instanceof Error) {
    return error.message
  }
  return 'Unknown error'
}

/**
 * Get HTTP status code from axios error
 */
function getErrorStatus(error: unknown): number | undefined {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const axiosError = error as any
  return axiosError?.response?.status
}

function isConflictError(error: unknown): boolean {
  const status = getErrorStatus(error)
  const msg = getErrorMessage(error).toLowerCase()

  // Check for 409 status code directly
  if (status === 409) return true

  // Check for 400 with conflict-related messages
  // Backend returns "Only pending injects can be fired/skipped. Current status: X"
  if (status === 400) {
    return (
      msg.includes('conflict') ||
      msg.includes('already fired') ||
      msg.includes('already skipped') ||
      msg.includes('only pending injects can be') ||
      msg.includes('current status:')
    )
  }

  return false
}

function isNotFoundError(error: unknown): boolean {
  const status = getErrorStatus(error)
  if (status === 404) return true

  const msg = getErrorMessage(error).toLowerCase()
  return msg.includes('not found')
}

function isServerError(error: unknown): boolean {
  const status = getErrorStatus(error)
  if (status && status >= 500) return true

  const msg = getErrorMessage(error).toLowerCase()
  return (
    msg.includes('network') ||
    msg.includes('timeout') ||
    msg.includes('econnrefused')
  )
}

function extractConflictInfo(action: PendingAction, error: unknown): ConflictInfo {
  return {
    actionId: action.id!,
    type: action.type,
    message: getErrorMessage(error),
  }
}

// ============================================================================
// Main Sync Function
// ============================================================================

/**
 * Sync all pending actions with the server
 *
 * @param exerciseId Optional - only sync actions for this exercise
 * @param onProgress Optional callback for progress updates
 * @returns SyncResult with counts and failed actions
 */
export async function syncPendingActions(
  exerciseId?: string,
  onProgress?: (progress: SyncProgress) => void,
): Promise<SyncResult> {
  // Don't start if already syncing
  if (currentSyncStatus === 'syncing') {
    return {
      totalActions: 0,
      succeeded: 0,
      failed: 0,
      failedActions: [],
    }
  }

  currentSyncStatus = 'syncing'
  syncAbortController = new AbortController()

  const actions = await getPendingActions(exerciseId)
  const result: SyncResult = {
    totalActions: actions.length,
    succeeded: 0,
    failed: 0,
    failedActions: [],
  }

  const conflicts: ConflictInfo[] = []

  for (let i = 0; i < actions.length; i++) {
    // Check for cancellation
    if (syncAbortController.signal.aborted) {
      break
    }

    const action = actions[i]

    // Report progress
    if (onProgress) {
      onProgress({
        status: 'syncing',
        current: i + 1,
        total: actions.length,
        conflicts,
      })
    }

    try {
      // Mark as syncing
      await updatePendingActionStatus(action.id!, 'syncing')

      // Process the action
      await processAction(action)

      // Success - remove from queue
      await deletePendingAction(action.id!)
      result.succeeded++
    } catch (error) {
      const errorMessage = getErrorMessage(error)

      if (isConflictError(error)) {
        // Conflict - mark as failed and record conflict info
        await updatePendingActionStatus(action.id!, 'failed', errorMessage)
        conflicts.push(extractConflictInfo(action, error))
        result.failed++
        result.failedActions.push({ ...action, status: 'failed', error: errorMessage })
      } else if (isServerError(error)) {
        // Server error - keep for retry with backoff
        await incrementRetryCount(action.id!)
        await updatePendingActionStatus(action.id!, 'pending', errorMessage)

        // If too many retries, mark as failed
        if (action.retryCount >= 5) {
          await updatePendingActionStatus(action.id!, 'failed', errorMessage)
          result.failed++
          result.failedActions.push({ ...action, status: 'failed', error: errorMessage })
        }
      } else {
        // Client error (4xx) - mark as failed, don't retry
        await updatePendingActionStatus(action.id!, 'failed', errorMessage)
        result.failed++
        result.failedActions.push({ ...action, status: 'failed', error: errorMessage })
      }
    }
  }

  // Determine final status
  if (result.failed === 0 && result.succeeded === result.totalActions) {
    currentSyncStatus = 'completed'
  } else if (result.succeeded === 0 && result.failed > 0) {
    currentSyncStatus = 'failed'
  } else if (result.succeeded > 0 && result.failed > 0) {
    currentSyncStatus = 'partial'
  } else {
    currentSyncStatus = 'idle'
  }

  // Final progress report
  if (onProgress) {
    onProgress({
      status: currentSyncStatus,
      current: actions.length,
      total: actions.length,
      conflicts,
    })
  }

  syncAbortController = null
  return result
}

/**
 * Retry a specific failed action
 */
export async function retryAction(actionId: number): Promise<boolean> {
  const action = await db.pendingActions.get(actionId)
  if (!action) return false

  try {
    await updatePendingActionStatus(actionId, 'syncing')
    await processAction(action)
    await deletePendingAction(actionId)
    return true
  } catch (error) {
    const errorMessage = error instanceof Error ? error.message : 'Unknown error'
    await updatePendingActionStatus(actionId, 'failed', errorMessage)
    return false
  }
}

/**
 * Discard a failed action (user chose not to retry)
 */
export async function discardAction(actionId: number): Promise<void> {
  const action = await db.pendingActions.get(actionId)
  if (action) {
    // Revert any optimistic updates
    switch (action.type) {
      case 'FIRE_INJECT':
      case 'SKIP_INJECT':
      case 'RESET_INJECT': {
        const payload = action.payload as { injectId: string }
        await updateCachedInject(payload.injectId, { pendingSync: false })
        break
      }
      case 'CREATE_OBSERVATION': {
        const payload = action.payload as CreateObservationPayload
        await db.observations.where('tempId').equals(payload.tempId).delete()
        break
      }
      case 'UPDATE_OBSERVATION': {
        const payload = action.payload as UpdateObservationPayload
        await updateCachedObservation(payload.observationId, { pendingSync: false })
        break
      }
      // DELETE_OBSERVATION - nothing to revert (already deleted locally)
    }

    await deletePendingAction(actionId)
  }
}

export default {
  syncPendingActions,
  retryAction,
  discardAction,
  getSyncStatus,
  cancelSync,
}
