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
import {
  getCachedPhoto,
  updateCachedPhotoSyncStatus,
  markPhotoSynced,
} from './photoCacheService'
import { injectService } from '../../features/injects/services/injectService'
import { observationService } from '../../features/observations/services/observationService'
import { photoService } from '../../features/photos/services/photoService'
import type { ObservationRating } from '../../types'
import type { UpdatePhotoRequest } from '../../features/photos/types'

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
  /** Photo-specific sync progress (only during blob upload phase) */
  photoSyncProgress?: { current: number; total: number }
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
  notes?: string | null
}

interface SkipInjectPayload {
  injectId: string
  reason: string
}

interface ResetInjectPayload {
  injectId: string
}

interface CreateObservationPayload {
  observation: {
    content: string
    rating?: ObservationRating | null
    recommendation?: string | null
    injectId?: string | null
  }
  tempId: string
}

interface UpdateObservationPayload {
  observationId: string
  changes: {
    content: string
    rating?: ObservationRating | null
    recommendation?: string | null
    injectId?: string | null
  }
}

interface DeleteObservationPayload {
  observationId: string
}

interface UploadPhotoPayload {
  /** Reference to cached photo in IndexedDB photos table */
  localPhotoId: string
  /** Idempotency key for duplicate prevention on retry */
  idempotencyKey: string
}

interface QuickPhotoPayload {
  /** Reference to cached photo in IndexedDB photos table */
  localPhotoId: string
  /** Idempotency key for duplicate prevention on retry */
  idempotencyKey: string
  /** Temp observation ID for the auto-created observation */
  tempObsId: string
}

interface UpdatePhotoPayload {
  photoId: string
  changes: UpdatePhotoRequest
}

interface DeletePhotoPayload {
  photoId: string
}

/**
 * Process a single pending action
 * Returns true if successful, throws on conflict/error
 */
async function processAction(action: PendingAction): Promise<void> {
  const { type, exerciseId, payload } = action

  switch (type) {
    case 'FIRE_INJECT': {
      const { injectId, notes } = payload as FireInjectPayload
      try {
        await injectService.fireInject(exerciseId, injectId, { notes })
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

    case 'UPLOAD_PHOTO': {
      const { localPhotoId, idempotencyKey } = payload as UploadPhotoPayload
      const cached = await getCachedPhoto(localPhotoId)
      if (!cached) {
        throw new Error(`Cached photo not found: ${localPhotoId}`)
      }
      await updateCachedPhotoSyncStatus(localPhotoId, 'uploading')
      const formData = buildPhotoFormDataFromBlob(cached)
      const serverPhoto = await photoService.uploadPhoto(exerciseId, formData, idempotencyKey)
      // Mark as synced and retain local cache
      await markPhotoSynced(localPhotoId, serverPhoto)
      break
    }

    case 'QUICK_PHOTO': {
      const { localPhotoId, idempotencyKey } = payload as QuickPhotoPayload
      const cached = await getCachedPhoto(localPhotoId)
      if (!cached) {
        throw new Error(`Cached photo not found: ${localPhotoId}`)
      }
      await updateCachedPhotoSyncStatus(localPhotoId, 'uploading')
      const formData = buildPhotoFormDataFromBlob(cached)
      const result = await photoService.quickPhoto(exerciseId, formData, idempotencyKey)
      // Mark as synced and retain local cache
      await markPhotoSynced(localPhotoId, result.photo)
      break
    }

    case 'UPDATE_PHOTO': {
      const { photoId, changes } = payload as UpdatePhotoPayload
      try {
        await photoService.updatePhoto(exerciseId, photoId, changes)
      } catch (error) {
        if (isNotFoundError(error)) {
          throw error
        }
        throw error
      }
      break
    }

    case 'DELETE_PHOTO': {
      const { photoId } = payload as DeletePhotoPayload
      try {
        await photoService.deletePhoto(exerciseId, photoId)
      } catch (error) {
        // 404 is OK - photo was already deleted
        if (!isNotFoundError(error)) {
          throw error
        }
      }
      break
    }

    default:
      throw new Error(`Unknown action type: ${type}`)
  }
}

// ============================================================================
// Photo Helpers
// ============================================================================

/**
 * Build FormData for upload from a CachedPhoto blob (no base64 conversion needed)
 */
function buildPhotoFormDataFromBlob(
  cached: import('./db').CachedPhoto,
): FormData {
  const file = new File([cached.blob], cached.fileName, { type: cached.blob.type || 'image/jpeg' })
  const formData = new FormData()
  formData.append('photo', file)

  // Add thumbnail if different from photo
  if (cached.thumbnailBlob !== cached.blob) {
    const thumbnail = new File([cached.thumbnailBlob], `thumb-${cached.fileName}`, { type: cached.thumbnailBlob.type || 'image/jpeg' })
    formData.append('thumbnail', thumbnail)
  }

  formData.append('capturedAt', cached.capturedAt)
  if (cached.scenarioTime) formData.append('scenarioTime', cached.scenarioTime)
  if (cached.latitude != null) formData.append('latitude', String(cached.latitude))
  if (cached.longitude != null) formData.append('longitude', String(cached.longitude))
  if (cached.locationAccuracy != null) formData.append('locationAccuracy', String(cached.locationAccuracy))
  if (cached.observationId) formData.append('observationId', cached.observationId)
  return formData
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
// Sync Ordering & Backoff
// ============================================================================

/** Action types that involve blob uploads (processed after data actions) */
const BLOB_ACTION_TYPES: PendingActionType[] = ['UPLOAD_PHOTO', 'QUICK_PHOTO']

/** Max retry count for photo uploads (more generous than data actions) */
const PHOTO_MAX_RETRIES = 8

/** Max retry count for data actions */
const DATA_MAX_RETRIES = 5

/**
 * Calculate exponential backoff delay: 1s, 2s, 4s, 8s, 16s, capped at 30s
 */
export function calculateBackoffDelay(retryCount: number, maxDelayMs: number = 30000): number {
  const delay = Math.min(1000 * Math.pow(2, retryCount), maxDelayMs)
  return delay
}

/**
 * Partition actions into data-first and blob-second phases
 * Data actions (observations, inject status) sync before blob actions (photos)
 * to guarantee referential integrity.
 */
function partitionActions(actions: PendingAction[]): {
  dataActions: PendingAction[]
  blobActions: PendingAction[]
} {
  const dataActions: PendingAction[] = []
  const blobActions: PendingAction[] = []

  for (const action of actions) {
    if (BLOB_ACTION_TYPES.includes(action.type)) {
      blobActions.push(action)
    } else {
      dataActions.push(action)
    }
  }

  return { dataActions, blobActions }
}

// ============================================================================
// Main Sync Function
// ============================================================================

/**
 * Sync all pending actions with the server.
 *
 * Processes in two phases:
 * 1. Data actions (observations, inject status) - ensures referential integrity
 * 2. Blob actions (photo uploads) - processed chronologically with backoff retry
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
  const { dataActions, blobActions } = partitionActions(actions)

  const result: SyncResult = {
    totalActions: actions.length,
    succeeded: 0,
    failed: 0,
    failedActions: [],
  }

  const conflicts: ConflictInfo[] = []
  let processedCount = 0

  // Phase 1: Process data actions first (observations, inject status, photo metadata)
  for (const action of dataActions) {
    if (syncAbortController.signal.aborted) break

    processedCount++
    if (onProgress) {
      onProgress({
        status: 'syncing',
        current: processedCount,
        total: actions.length,
        conflicts,
      })
    }

    await processActionWithRetry(action, DATA_MAX_RETRIES, result, conflicts)
  }

  // Phase 2: Process blob actions (photo uploads) chronologically
  for (let i = 0; i < blobActions.length; i++) {
    if (syncAbortController.signal.aborted) break

    const action = blobActions[i]
    processedCount++

    if (onProgress) {
      onProgress({
        status: 'syncing',
        current: processedCount,
        total: actions.length,
        conflicts,
        photoSyncProgress: { current: i + 1, total: blobActions.length },
      })
    }

    await processActionWithRetry(action, PHOTO_MAX_RETRIES, result, conflicts)
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
      photoSyncProgress: blobActions.length > 0
        ? { current: blobActions.length, total: blobActions.length }
        : undefined,
    })
  }

  syncAbortController = null
  return result
}

/**
 * Process a single action with error handling, backoff retry, and conflict detection
 */
async function processActionWithRetry(
  action: PendingAction,
  maxRetries: number,
  result: SyncResult,
  conflicts: ConflictInfo[],
): Promise<void> {
  try {
    await updatePendingActionStatus(action.id!, 'syncing')
    await processAction(action)
    await deletePendingAction(action.id!)
    result.succeeded++
  } catch (error) {
    const errorMessage = getErrorMessage(error)

    if (isConflictError(error)) {
      await updatePendingActionStatus(action.id!, 'failed', errorMessage)
      conflicts.push(extractConflictInfo(action, error))
      result.failed++
      result.failedActions.push({ ...action, status: 'failed', error: errorMessage })
    } else if (isServerError(error)) {
      await incrementRetryCount(action.id!)
      const newRetryCount = action.retryCount + 1

      if (newRetryCount >= maxRetries) {
        await updatePendingActionStatus(action.id!, 'failed', errorMessage)
        result.failed++
        result.failedActions.push({ ...action, status: 'failed', error: errorMessage })
        // Mark cached photo as failed if applicable
        if (action.type === 'UPLOAD_PHOTO' || action.type === 'QUICK_PHOTO') {
          const payload = action.payload as { localPhotoId: string }
          await updateCachedPhotoSyncStatus(payload.localPhotoId, 'failed', errorMessage)
        }
      } else {
        await updatePendingActionStatus(action.id!, 'pending', errorMessage)
        // Apply exponential backoff delay before next action
        const delay = calculateBackoffDelay(newRetryCount)
        await new Promise(resolve => setTimeout(resolve, delay))
      }
    } else {
      await updatePendingActionStatus(action.id!, 'failed', errorMessage)
      result.failed++
      result.failedActions.push({ ...action, status: 'failed', error: errorMessage })
      // Mark cached photo as failed if applicable
      if (action.type === 'UPLOAD_PHOTO' || action.type === 'QUICK_PHOTO') {
        const payload = action.payload as { localPhotoId: string }
        await updateCachedPhotoSyncStatus(payload.localPhotoId, 'failed', errorMessage)
      }
    }
  }
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
      case 'UPLOAD_PHOTO':
      case 'QUICK_PHOTO': {
        const payload = action.payload as { localPhotoId: string }
        // Remove the cached photo blob since we're discarding the action
        const { deleteCachedPhoto } = await import('./photoCacheService')
        await deleteCachedPhoto(payload.localPhotoId)
        break
      }
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
