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
import type { ExerciseDto } from '../../features/exercises/types'
import type { InjectDto } from '../../features/injects/types'
import type { ObservationDto } from '../../features/observations/types'
import { DeliveryMode, TimelineMode } from '../../types'

// ============================================================================
// Exercise Caching
// ============================================================================

/**
 * Cache an exercise from the server
 */
export async function cacheExercise(exercise: ExerciseDto): Promise<void> {
  const cached: CachedExercise = {
    id: exercise.id,
    name: exercise.name,
    description: exercise.description ?? null,
    exerciseType: exercise.exerciseType,
    status: exercise.status,
    startDate: exercise.scheduledDate ?? null,
    endDate: null, // Not available in current DTO
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
    description: cached.description ?? null,
    exerciseType: cached.exerciseType as ExerciseDto['exerciseType'],
    status: cached.status as ExerciseDto['status'],
    isPracticeMode: false,
    scheduledDate: cached.startDate ?? new Date().toISOString().split('T')[0],
    startTime: null,
    endTime: null,
    timeZoneId: 'UTC',
    location: null,
    organizationId: '',
    activeMselId: null,
    // Clock mode properties (CLK-01)
    deliveryMode: DeliveryMode.FacilitatorPaced,
    timelineMode: TimelineMode.RealTime,
    timeScale: null,
    updatedAt: cached.updatedAt,
    createdAt: cached.updatedAt, // Use updatedAt as fallback
    createdBy: '', // Not available in cache
    activatedAt: null,
    activatedBy: null,
    completedAt: null,
    completedBy: null,
    archivedAt: null,
    archivedBy: null,
    hasBeenPublished: false, // Not tracked in cache
    previousStatus: null, // Not tracked in cache
    // Exercise settings - not tracked in cache, use defaults
    clockMultiplier: 1,
    autoFireEnabled: false,
    confirmFireInject: true,
    confirmSkipInject: true,
    confirmClockControl: false,
    maxDuration: null,
    // Summary counts - not tracked in cache, use defaults
    injectCount: 0,
    firedInjectCount: 0,
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
    mselId: inject.mselId ?? null,
    injectNumber: inject.injectNumber,
    title: inject.title,
    description: inject.description ?? null,
    expectedAction: inject.expectedAction ?? null,
    status: inject.status,
    scheduledTime: inject.scheduledTime ?? null,
    actualTime: inject.firedAt ?? null,
    firedById: inject.firedBy ?? null,
    firedByName: inject.firedByName ?? null,
    skippedById: inject.skippedBy ?? null,
    skippedByName: inject.skippedByName ?? null,
    skipReason: inject.skipReason ?? null,
    from: inject.source ?? null,
    to: inject.target ?? null,
    method: inject.deliveryMethod ?? null,
    injectType: inject.injectType ?? null,
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
    mselId: cached.mselId ?? '',
    injectNumber: cached.injectNumber,
    title: cached.title,
    description: cached.description ?? '',
    expectedAction: cached.expectedAction ?? null,
    status: cached.status as InjectDto['status'],
    scheduledTime: cached.scheduledTime ?? '00:00:00',
    deliveryTime: null, // Not tracked in cache
    scenarioDay: null,
    scenarioTime: null,
    target: cached.to ?? '',
    source: cached.from ?? null,
    deliveryMethod: cached.method as InjectDto['deliveryMethod'],
    deliveryMethodId: null,
    deliveryMethodName: null,
    deliveryMethodOther: null,
    injectType: (cached.injectType as InjectDto['injectType']) ?? 'Standard',
    sequence: cached.injectNumber,
    parentInjectId: null,
    triggerCondition: null,
    controllerNotes: null,
    readyAt: null, // Not tracked in cache
    firedAt: cached.actualTime ?? null,
    firedBy: cached.firedById ?? null,
    firedByName: cached.firedByName ?? null,
    skippedAt: null,
    skippedBy: cached.skippedById ?? null,
    skippedByName: cached.skippedByName ?? null,
    skipReason: cached.skipReason ?? null,
    phaseId: null,
    phaseName: null,
    objectiveIds: [],
    updatedAt: cached.updatedAt,
    createdAt: cached.updatedAt, // Use updatedAt as fallback
    // Phase G fields - not tracked in cache
    sourceReference: null,
    priority: null,
    triggerType: 'Manual',
    responsibleController: null,
    locationName: null,
    locationType: null,
    track: null,
    // Approval workflow fields - not tracked in cache
    submittedByUserId: null,
    submittedByName: null,
    submittedAt: null,
    approvedByUserId: null,
    approvedByName: null,
    approvedAt: null,
    approverNotes: null,
    rejectedByUserId: null,
    rejectedByName: null,
    rejectedAt: null,
    rejectionReason: null,
    revertedByUserId: null,
    revertedByName: null,
    revertedAt: null,
    revertReason: null,
    // Critical task linking - not tracked in cache
    linkedCriticalTaskCount: 0,
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
    injectId: obs.injectId ?? null,
    content: obs.content,
    rating: obs.rating ?? null,
    recommendation: obs.recommendation ?? null,
    createdById: obs.createdBy ?? null,
    createdByName: obs.createdByName ?? null,
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
    injectId: cached.injectId ?? null,
    objectiveId: null,
    content: cached.content,
    rating: cached.rating as ObservationDto['rating'],
    recommendation: cached.recommendation ?? null,
    observedAt: cached.updatedAt,
    location: null,
    createdBy: cached.createdById ?? '',
    createdByName: cached.createdByName ?? null,
    injectTitle: null,
    injectNumber: null,
    capabilities: [], // Not tracked in cache
    status: 'Draft',
    photos: [],
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
