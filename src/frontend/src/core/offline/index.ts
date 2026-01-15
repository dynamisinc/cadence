/**
 * Offline Module
 *
 * Provides IndexedDB caching and offline action queuing for Cadence.
 *
 * @module core/offline
 */

// Database
export {
  db,
  CadenceDatabase,
  type CachedExercise,
  type CachedPhase,
  type CachedInject,
  type CachedObservation,
  type PendingAction,
  type PendingActionType,
  type PendingActionStatus,
  type SyncMetadata,
  clearExerciseCache,
  clearAllCache,
  getPendingActionCount,
  getPendingActions,
  addPendingAction,
  updatePendingActionStatus,
  incrementRetryCount,
  deletePendingAction,
  deleteFailedActions,
  pruneOldCache,
  getStorageEstimate,
} from './db'

// Cache Service
export {
  cacheExercise,
  getCachedExercise,
  cachedExerciseToDto,
  cacheInjects,
  getCachedInjects,
  updateCachedInject,
  cachedInjectToDto,
  cacheObservations,
  getCachedObservations,
  addLocalObservation,
  updateCachedObservation,
  deleteCachedObservation,
  cachedObservationToDto,
  updateSyncMetadata,
  getLastSyncTime,
  isCacheStale,
} from './cacheService'

// Sync Service
export {
  syncPendingActions,
  retryAction,
  discardAction,
  getSyncStatus,
  cancelSync,
  type SyncResult,
  type SyncProgress,
  type SyncStatus,
  type ConflictInfo,
} from './syncService'

// Hooks
export { useOfflineSync } from './useOfflineSync'

// Components
export { ConflictDialog } from '../components/ConflictDialog'
