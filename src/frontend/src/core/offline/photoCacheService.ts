/**
 * Photo Cache Service
 *
 * CRUD operations for cached photo blobs in IndexedDB.
 * Provides dedicated photo blob storage separate from the pending actions queue.
 * Stores photos as Blob objects (not base64) for efficient binary storage.
 */

import { db, type CachedPhoto, type PhotoSyncStatus } from './db'
import type { PhotoDto } from '../../features/photos/types'

/**
 * Cache a new photo blob in IndexedDB
 */
export async function cachePhotoBlob(photo: CachedPhoto): Promise<void> {
  await db.photos.put(photo)
}

/**
 * Get a cached photo by local ID
 */
export async function getCachedPhoto(id: string): Promise<CachedPhoto | undefined> {
  return db.photos.get(id)
}

/**
 * Get all cached photos for an exercise
 */
export async function getCachedPhotosByExercise(exerciseId: string): Promise<CachedPhoto[]> {
  return db.photos.where('exerciseId').equals(exerciseId).sortBy('capturedAt')
}

/**
 * Get all pending (unsynced) photos in chronological order
 */
export async function getPendingPhotos(exerciseId?: string): Promise<CachedPhoto[]> {
  if (exerciseId) {
    return db.photos
      .where('exerciseId')
      .equals(exerciseId)
      .and(p => p.syncStatus === 'pending' || p.syncStatus === 'failed')
      .sortBy('capturedAt')
  }
  return db.photos
    .where('syncStatus')
    .anyOf('pending', 'failed')
    .sortBy('capturedAt')
}

/**
 * Get count of pending photos
 */
export async function getPendingPhotoCount(exerciseId?: string): Promise<number> {
  if (exerciseId) {
    return db.photos
      .where('exerciseId')
      .equals(exerciseId)
      .and(p => p.syncStatus === 'pending' || p.syncStatus === 'failed')
      .count()
  }
  return db.photos
    .where('syncStatus')
    .anyOf('pending', 'failed')
    .count()
}

/**
 * Update the sync status of a cached photo
 */
export async function updateCachedPhotoSyncStatus(
  id: string,
  syncStatus: PhotoSyncStatus,
  error?: string | null,
): Promise<void> {
  await db.photos.update(id, { syncStatus, error: error ?? null })
}

/**
 * Mark a photo as successfully synced and store server-assigned data.
 * Retains the local blob for offline viewing (cache retention).
 */
export async function markPhotoSynced(
  localPhotoId: string,
  serverPhoto: PhotoDto,
): Promise<void> {
  await db.photos.update(localPhotoId, {
    syncStatus: 'synced' as PhotoSyncStatus,
    serverPhotoId: serverPhoto.id,
    serverBlobUri: serverPhoto.blobUri,
    serverThumbnailUri: serverPhoto.thumbnailUri,
    error: null,
  })
}

/**
 * Delete a cached photo (remove blob data from IndexedDB)
 */
export async function deleteCachedPhoto(id: string): Promise<void> {
  await db.photos.delete(id)
}

/**
 * Delete all synced photos for an exercise (free up storage).
 * Only removes photos that have been successfully uploaded.
 */
export async function clearSyncedPhotos(exerciseId: string): Promise<number> {
  return db.photos
    .where('exerciseId')
    .equals(exerciseId)
    .and(p => p.syncStatus === 'synced')
    .delete()
}

/**
 * Convert a CachedPhoto to a PhotoDto for display purposes.
 * Uses object URLs for the blob/thumbnail URIs.
 * IMPORTANT: Callers must revoke the object URLs when done via _localObjectUrls.
 */
export function cachedPhotoToDto(cached: CachedPhoto): PhotoDto & { _localObjectUrls: string[] } {
  const blobUrl = URL.createObjectURL(cached.blob)
  const thumbnailUrl = URL.createObjectURL(cached.thumbnailBlob)

  return {
    id: cached.id,
    exerciseId: cached.exerciseId,
    observationId: cached.observationId ?? null,
    capturedById: 'offline-user',
    capturedByName: 'You (offline)',
    fileName: cached.fileName,
    blobUri: cached.serverBlobUri ?? blobUrl,
    thumbnailUri: cached.serverThumbnailUri ?? thumbnailUrl,
    fileSizeBytes: cached.fileSizeBytes,
    capturedAt: cached.capturedAt,
    scenarioTime: cached.scenarioTime ?? null,
    latitude: cached.latitude ?? null,
    longitude: cached.longitude ?? null,
    locationAccuracy: cached.locationAccuracy ?? null,
    displayOrder: 0,
    status: 'Draft',
    annotationsJson: null,
    createdAt: cached.cachedAt.toISOString(),
    updatedAt: cached.cachedAt.toISOString(),
    _localObjectUrls: [blobUrl, thumbnailUrl],
  }
}

/**
 * Get total storage used by cached photos (approximate)
 */
export async function getPhotoStorageUsage(exerciseId?: string): Promise<{
  totalBytes: number
  photoCount: number
  pendingCount: number
  syncedCount: number
}> {
  const photos = exerciseId
    ? await db.photos.where('exerciseId').equals(exerciseId).toArray()
    : await db.photos.toArray()

  let totalBytes = 0
  let pendingCount = 0
  let syncedCount = 0

  for (const photo of photos) {
    totalBytes += photo.fileSizeBytes
    if (photo.syncStatus === 'pending' || photo.syncStatus === 'uploading' || photo.syncStatus === 'failed') {
      pendingCount++
    } else {
      syncedCount++
    }
  }

  return {
    totalBytes,
    photoCount: photos.length,
    pendingCount,
    syncedCount,
  }
}
