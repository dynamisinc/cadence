/**
 * Cache Service
 *
 * High-level service for caching and retrieving exercise data from IndexedDB.
 * Provides methods for:
 * - Caching data received from the server
 * - Retrieving cached data when offline
 * - Managing cache freshness
 */

import { db, type CachedExercise, type CachedInject, type CachedObservation } from './db'
import type { ExerciseDto, ExerciseDetailDto } from '../../features/exercises/types'
import type { InjectDto } from '../../features/injects/types'
import type { ObservationDto } from '../../features/observations/types'

// ============================================================================
// Exercise Caching
// ============================================================================

/**
 * Cache an exercise from the server
 */
export async function cacheExercise(exercise: ExerciseDto | ExerciseDetailDto): Promise<void> {
  const cached: CachedExercise = {
    id: exercise.id,
    name: exercise.name,
    description: exercise.description,
    exerciseType: exercise.exerciseType,
    status: exercise.status,
    startDate: exercise.startDate,
    endDate: exercise.endDate,
    updatedAt: exercise.updatedAt,
    cachedAt: new Date(),
  }

  await db.exercises.put(cached)
  await updateSyncMetadata(`exercise-${exercise.id}`)
}

/**
 * Get cached exercise
 */
export async function getCachedExercise(exerciseId: string): Promise<CachedExercise | undefined> {
  return db.exercises.get(exerciseId)
}

/**
 * Convert cached exercise to DTO format
 */
export function cachedExerciseToDto(cached: CachedExercise): ExerciseDto {
  return {
    id: cached.id,
    name: cached.name,
    description: cached.description,
    exerciseType: cached.exerciseType as ExerciseDto['exerciseType'],
    status: cached.status as ExerciseDto['status'],
    startDate: cached.startDate,
    endDate: cached.endDate,
    updatedAt: cached.updatedAt,
    createdAt: cached.updatedAt, // Use updatedAt as fallback
  }
}

// ============================================================================
// Inject Caching
// ============================================================================

/**
 * Cache injects from the server
 */
export async function cacheInjects(exerciseId: string, injects: InjectDto[]): Promise<void> {
  const now = new Date()
  const cached: CachedInject[] = injects.map(inject => ({
    id: inject.id,
    exerciseId,
    mselId: inject.mselId,
    injectNumber: inject.injectNumber,
    title: inject.title,
    description: inject.description,
    expectedAction: inject.expectedAction,
    status: inject.status,
    scheduledTime: inject.scheduledTime,
    actualTime: inject.actualTime,
    firedById: inject.firedById,
    firedByName: inject.firedByName,
    skippedById: inject.skippedById,
    skippedByName: inject.skippedByName,
    skipReason: inject.skipReason,
    from: inject.from,
    to: inject.to,
    method: inject.method,
    updatedAt: inject.updatedAt,
    cachedAt: now,
    pendingSync: false,
  }))

  await db.transaction('rw', db.injects, async () => {
    // Get IDs of items with pendingSync to preserve them
    const pendingSyncItems = await db.injects
      .where('exerciseId')
      .equals(exerciseId)
      .and(i => !!i.pendingSync)
      .toArray()
    const pendingSyncIds = new Set(pendingSyncItems.map(i => i.id))

    // Remove old cached injects that aren't pending sync
    await db.injects
      .where('exerciseId')
      .equals(exerciseId)
      .and(i => !i.pendingSync)
      .delete()

    // Filter out server items that would overwrite pendingSync items
    const safeToCache = cached.filter(c => !pendingSyncIds.has(c.id))

    // Add new cached injects (excluding those that would overwrite pendingSync)
    await db.injects.bulkPut(safeToCache)
  })

  await updateSyncMetadata(`injects-${exerciseId}`)
}

/**
 * Get cached injects for an exercise
 */
export async function getCachedInjects(exerciseId: string): Promise<CachedInject[]> {
  return db.injects.where('exerciseId').equals(exerciseId).toArray()
}

/**
 * Update a single cached inject (e.g., after optimistic update)
 */
export async function updateCachedInject(
  injectId: string,
  updates: Partial<CachedInject>,
): Promise<void> {
  await db.injects.update(injectId, {
    ...updates,
    cachedAt: new Date(),
  })
}

/**
 * Convert cached inject to DTO format
 */
export function cachedInjectToDto(cached: CachedInject): InjectDto & { pendingSync?: boolean } {
  return {
    id: cached.id,
    exerciseId: cached.exerciseId,
    mselId: cached.mselId,
    injectNumber: cached.injectNumber,
    title: cached.title,
    description: cached.description,
    expectedAction: cached.expectedAction,
    status: cached.status as InjectDto['status'],
    scheduledTime: cached.scheduledTime,
    actualTime: cached.actualTime,
    firedById: cached.firedById,
    firedByName: cached.firedByName,
    skippedById: cached.skippedById,
    skippedByName: cached.skippedByName,
    skipReason: cached.skipReason,
    from: cached.from,
    to: cached.to,
    method: cached.method,
    updatedAt: cached.updatedAt,
    createdAt: cached.updatedAt, // Use updatedAt as fallback
    pendingSync: cached.pendingSync,
  }
}

// ============================================================================
// Observation Caching
// ============================================================================

/**
 * Cache observations from the server
 */
export async function cacheObservations(
  exerciseId: string,
  observations: ObservationDto[],
): Promise<void> {
  const now = new Date()
  const cached: CachedObservation[] = observations.map(obs => ({
    id: obs.id,
    exerciseId,
    injectId: obs.injectId,
    content: obs.content,
    rating: obs.rating,
    recommendation: obs.recommendation,
    createdById: obs.createdById,
    createdByName: obs.createdByName,
    updatedAt: obs.updatedAt,
    cachedAt: now,
    pendingSync: false,
  }))

  await db.transaction('rw', db.observations, async () => {
    // Get IDs of items with pendingSync to preserve them
    const pendingSyncItems = await db.observations
      .where('exerciseId')
      .equals(exerciseId)
      .and(o => !!o.pendingSync)
      .toArray()
    const pendingSyncIds = new Set(pendingSyncItems.map(o => o.id))

    // Remove old cached observations that aren't pending sync
    await db.observations
      .where('exerciseId')
      .equals(exerciseId)
      .and(o => !o.pendingSync)
      .delete()

    // Filter out server items that would overwrite pendingSync items
    const safeToCache = cached.filter(c => !pendingSyncIds.has(c.id))

    // Add new cached observations (excluding those that would overwrite pendingSync)
    await db.observations.bulkPut(safeToCache)
  })

  await updateSyncMetadata(`observations-${exerciseId}`)
}

/**
 * Get cached observations for an exercise
 */
export async function getCachedObservations(exerciseId: string): Promise<CachedObservation[]> {
  return db.observations.where('exerciseId').equals(exerciseId).toArray()
}

/**
 * Add a locally created observation (optimistic create)
 */
export async function addLocalObservation(observation: CachedObservation): Promise<void> {
  await db.observations.put(observation)
}

/**
 * Update a cached observation
 */
export async function updateCachedObservation(
  observationId: string,
  updates: Partial<CachedObservation>,
): Promise<void> {
  await db.observations.update(observationId, {
    ...updates,
    cachedAt: new Date(),
  })
}

/**
 * Delete a cached observation
 */
export async function deleteCachedObservation(observationId: string): Promise<void> {
  await db.observations.delete(observationId)
}

/**
 * Convert cached observation to DTO format
 */
export function cachedObservationToDto(
  cached: CachedObservation,
): ObservationDto & { pendingSync?: boolean; tempId?: string } {
  return {
    id: cached.id,
    exerciseId: cached.exerciseId,
    injectId: cached.injectId,
    content: cached.content,
    rating: cached.rating as ObservationDto['rating'],
    recommendation: cached.recommendation,
    createdById: cached.createdById,
    createdByName: cached.createdByName,
    updatedAt: cached.updatedAt,
    createdAt: cached.updatedAt, // Use updatedAt as fallback
    pendingSync: cached.pendingSync,
    tempId: cached.tempId,
  }
}

// ============================================================================
// Sync Metadata
// ============================================================================

/**
 * Update sync metadata for a resource
 */
export async function updateSyncMetadata(key: string, etag?: string): Promise<void> {
  await db.syncMetadata.put({
    key,
    lastSyncAt: new Date(),
    etag,
  })
}

/**
 * Get last sync time for a resource
 */
export async function getLastSyncTime(key: string): Promise<Date | null> {
  const meta = await db.syncMetadata.get(key)
  return meta?.lastSyncAt ?? null
}

/**
 * Check if cache is stale (older than specified minutes)
 */
export async function isCacheStale(key: string, maxAgeMinutes: number = 5): Promise<boolean> {
  const lastSync = await getLastSyncTime(key)
  if (!lastSync) return true

  const staleThreshold = new Date()
  staleThreshold.setMinutes(staleThreshold.getMinutes() - maxAgeMinutes)

  return lastSync < staleThreshold
}

// ============================================================================
// Exports
// ============================================================================

export default {
  // Exercise
  cacheExercise,
  getCachedExercise,
  cachedExerciseToDto,
  // Injects
  cacheInjects,
  getCachedInjects,
  updateCachedInject,
  cachedInjectToDto,
  // Observations
  cacheObservations,
  getCachedObservations,
  addLocalObservation,
  updateCachedObservation,
  deleteCachedObservation,
  cachedObservationToDto,
  // Sync metadata
  updateSyncMetadata,
  getLastSyncTime,
  isCacheStale,
}
