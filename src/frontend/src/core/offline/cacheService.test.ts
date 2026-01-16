/**
 * Tests for Cache Service (cacheService.ts)
 *
 * Tests high-level caching and retrieval operations for exercise data.
 */

import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import { db, clearAllCache } from './db'
import {
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
import type { ExerciseDto } from '../../features/exercises/types'
import type { InjectDto } from '../../features/injects/types'
import type { ObservationDto } from '../../features/observations/types'

// Helper to create complete ExerciseDto
const createExerciseDto = (overrides: Partial<ExerciseDto> = {}): ExerciseDto => ({
  id: 'ex-123',
  name: 'Test Exercise',
  description: null,
  exerciseType: 'TTX',
  status: 'Draft',
  isPracticeMode: false,
  scheduledDate: '2025-01-15',
  startTime: null,
  endTime: null,
  timeZoneId: 'UTC',
  location: null,
  organizationId: 'org-1',
  activeMselId: null,
  updatedAt: '2025-01-14T12:00:00Z',
  createdAt: '2025-01-01T12:00:00Z',
  activatedAt: null,
  activatedBy: null,
  completedAt: null,
  completedBy: null,
  archivedAt: null,
  archivedBy: null,
  ...overrides,
})

// Helper to create complete InjectDto
const createInjectDto = (overrides: Partial<InjectDto> = {}): InjectDto => ({
  id: 'inj-1',
  injectNumber: 1,
  title: 'Test Inject',
  description: 'Test description',
  scheduledTime: '09:00:00',
  scenarioDay: null,
  scenarioTime: null,
  target: 'Target Team',
  source: null,
  deliveryMethod: null,
  injectType: 'Standard',
  status: 'Pending',
  sequence: 1,
  parentInjectId: null,
  triggerCondition: null,
  expectedAction: null,
  controllerNotes: null,
  firedAt: null,
  firedBy: null,
  firedByName: null,
  skippedAt: null,
  skippedBy: null,
  skippedByName: null,
  skipReason: null,
  mselId: 'msel-1',
  phaseId: null,
  phaseName: null,
  objectiveIds: [],
  updatedAt: '2025-01-14T12:00:00Z',
  createdAt: '2025-01-01T12:00:00Z',
  ...overrides,
})

// Helper to create complete ObservationDto
const createObservationDto = (overrides: Partial<ObservationDto> = {}): ObservationDto => ({
  id: 'obs-1',
  exerciseId: 'ex-123',
  injectId: null,
  objectiveId: null,
  content: 'Test observation',
  rating: null,
  recommendation: null,
  observedAt: '2025-01-14T12:00:00Z',
  location: null,
  createdBy: 'user-1',
  createdByName: 'John Doe',
  injectTitle: null,
  injectNumber: null,
  updatedAt: '2025-01-14T12:00:00Z',
  createdAt: '2025-01-01T12:00:00Z',
  ...overrides,
})

describe('cacheService', () => {
  beforeEach(async () => {
    await clearAllCache()
  })

  afterEach(async () => {
    await clearAllCache()
  })

  // ============================================================================
  // Exercise Caching Tests
  // ============================================================================

  describe('cacheExercise', () => {
    it('caches an exercise with all fields', async () => {
      const exercise = createExerciseDto({
        name: 'Test Exercise',
        description: 'A test exercise',
        status: 'Active',
      })

      await cacheExercise(exercise)

      const cached = await db.exercises.get('ex-123')
      expect(cached).toBeDefined()
      expect(cached?.name).toBe('Test Exercise')
      expect(cached?.exerciseType).toBe('TTX')
      expect(cached?.status).toBe('Active')
    })

    it('updates sync metadata when caching', async () => {
      const exercise = createExerciseDto()

      await cacheExercise(exercise)

      const metadata = await db.syncMetadata.get('exercise-ex-123')
      expect(metadata).toBeDefined()
      expect(metadata?.lastSyncAt).toBeInstanceOf(Date)
    })

    it('overwrites existing cached exercise', async () => {
      const exercise1 = createExerciseDto({ name: 'Original' })
      const exercise2 = createExerciseDto({
        name: 'Updated',
        exerciseType: 'FSE',
        status: 'Active',
      })

      await cacheExercise(exercise1)
      await cacheExercise(exercise2)

      const cached = await db.exercises.get('ex-123')
      expect(cached?.name).toBe('Updated')
      expect(cached?.exerciseType).toBe('FSE')
    })
  })

  describe('getCachedExercise', () => {
    it('returns cached exercise when exists', async () => {
      await db.exercises.put({
        id: 'ex-123',
        name: 'Test',
        exerciseType: 'TTX',
        status: 'Draft',
        updatedAt: '2025-01-14T12:00:00Z',
        cachedAt: new Date(),
      })

      const cached = await getCachedExercise('ex-123')
      expect(cached).toBeDefined()
      expect(cached?.name).toBe('Test')
    })

    it('returns undefined when not cached', async () => {
      const cached = await getCachedExercise('nonexistent')
      expect(cached).toBeUndefined()
    })
  })

  describe('cachedExerciseToDto', () => {
    it('converts cached exercise to DTO format', () => {
      const cached = {
        id: 'ex-123',
        name: 'Test Exercise',
        description: 'Description',
        exerciseType: 'TTX',
        status: 'Active',
        startDate: '2025-01-15',
        endDate: null,
        updatedAt: '2025-01-14T12:00:00Z',
        cachedAt: new Date(),
      }

      const dto = cachedExerciseToDto(cached)

      expect(dto.id).toBe('ex-123')
      expect(dto.name).toBe('Test Exercise')
      expect(dto.exerciseType).toBe('TTX')
      expect(dto.status).toBe('Active')
      expect(dto.createdAt).toBe(cached.updatedAt) // Uses updatedAt as fallback
    })
  })

  // ============================================================================
  // Inject Caching Tests
  // ============================================================================

  describe('cacheInjects', () => {
    it('caches multiple injects', async () => {
      const injects: InjectDto[] = [
        createInjectDto({ id: 'inj-1', injectNumber: 1, title: 'Inject 1' }),
        createInjectDto({
          id: 'inj-2',
          injectNumber: 2,
          title: 'Inject 2',
          status: 'Fired',
        }),
      ]

      await cacheInjects('ex-123', injects)

      const cached = await db.injects.where('exerciseId').equals('ex-123').toArray()
      expect(cached).toHaveLength(2)
    })

    it('preserves pendingSync items during cache update', async () => {
      // Add an inject with pendingSync = true
      await db.injects.put({
        id: 'inj-pending',
        exerciseId: 'ex-123',
        injectNumber: 1,
        title: 'Pending Inject',
        status: 'Fired',
        updatedAt: '2025-01-14T12:00:00Z',
        cachedAt: new Date(),
        pendingSync: true,
      })

      // Cache new injects from server
      const serverInjects: InjectDto[] = [
        createInjectDto({ id: 'inj-server', injectNumber: 2, title: 'Server Inject' }),
      ]

      await cacheInjects('ex-123', serverInjects)

      // Pending sync inject should still exist
      const pendingInject = await db.injects.get('inj-pending')
      expect(pendingInject).toBeDefined()
      expect(pendingInject?.pendingSync).toBe(true)

      // Server inject should also exist
      const serverInject = await db.injects.get('inj-server')
      expect(serverInject).toBeDefined()
    })

    it('removes old non-pendingSync injects', async () => {
      // Add an inject without pendingSync
      await db.injects.put({
        id: 'inj-old',
        exerciseId: 'ex-123',
        injectNumber: 1,
        title: 'Old Inject',
        status: 'Pending',
        updatedAt: '2025-01-14T12:00:00Z',
        cachedAt: new Date(),
        pendingSync: false,
      })

      // Cache new injects from server (which doesn't include the old one)
      const serverInjects: InjectDto[] = [
        createInjectDto({ id: 'inj-new', injectNumber: 2, title: 'New Inject' }),
      ]

      await cacheInjects('ex-123', serverInjects)

      // Old inject should be removed
      const oldInject = await db.injects.get('inj-old')
      expect(oldInject).toBeUndefined()

      // New inject should exist
      const newInject = await db.injects.get('inj-new')
      expect(newInject).toBeDefined()
    })

    it('updates sync metadata', async () => {
      await cacheInjects('ex-123', [])

      const metadata = await db.syncMetadata.get('injects-ex-123')
      expect(metadata).toBeDefined()
    })
  })

  describe('getCachedInjects', () => {
    it('returns all cached injects for exercise', async () => {
      await db.injects.bulkPut([
        {
          id: 'inj-1',
          exerciseId: 'ex-123',
          injectNumber: 1,
          title: 'Inject 1',
          status: 'Pending',
          updatedAt: '2025-01-14T12:00:00Z',
          cachedAt: new Date(),
        },
        {
          id: 'inj-2',
          exerciseId: 'ex-123',
          injectNumber: 2,
          title: 'Inject 2',
          status: 'Pending',
          updatedAt: '2025-01-14T12:00:00Z',
          cachedAt: new Date(),
        },
        {
          id: 'inj-3',
          exerciseId: 'ex-other',
          injectNumber: 1,
          title: 'Other Inject',
          status: 'Pending',
          updatedAt: '2025-01-14T12:00:00Z',
          cachedAt: new Date(),
        },
      ])

      const injects = await getCachedInjects('ex-123')
      expect(injects).toHaveLength(2)
    })
  })

  describe('updateCachedInject', () => {
    it('updates specific fields of cached inject', async () => {
      await db.injects.put({
        id: 'inj-1',
        exerciseId: 'ex-123',
        injectNumber: 1,
        title: 'Original',
        status: 'Pending',
        updatedAt: '2025-01-14T12:00:00Z',
        cachedAt: new Date(),
      })

      await updateCachedInject('inj-1', { status: 'Fired', pendingSync: true })

      const inject = await db.injects.get('inj-1')
      expect(inject?.status).toBe('Fired')
      expect(inject?.pendingSync).toBe(true)
      expect(inject?.title).toBe('Original') // Unchanged
    })

    it('updates cachedAt timestamp', async () => {
      const oldDate = new Date('2025-01-01T00:00:00Z')
      await db.injects.put({
        id: 'inj-1',
        exerciseId: 'ex-123',
        injectNumber: 1,
        title: 'Test',
        status: 'Pending',
        updatedAt: '2025-01-14T12:00:00Z',
        cachedAt: oldDate,
      })

      await updateCachedInject('inj-1', { status: 'Fired' })

      const inject = await db.injects.get('inj-1')
      expect(inject?.cachedAt.getTime()).toBeGreaterThan(oldDate.getTime())
    })
  })

  describe('cachedInjectToDto', () => {
    it('converts cached inject to DTO format with pendingSync', () => {
      const cached = {
        id: 'inj-1',
        exerciseId: 'ex-123',
        injectNumber: 1,
        title: 'Test Inject',
        status: 'Fired',
        updatedAt: '2025-01-14T12:00:00Z',
        cachedAt: new Date(),
        pendingSync: true,
      }

      const dto = cachedInjectToDto(cached)

      expect(dto.id).toBe('inj-1')
      expect(dto.title).toBe('Test Inject')
      expect(dto.status).toBe('Fired')
      expect(dto.pendingSync).toBe(true)
    })
  })

  // ============================================================================
  // Observation Caching Tests
  // ============================================================================

  describe('cacheObservations', () => {
    it('caches multiple observations', async () => {
      const observations: ObservationDto[] = [
        createObservationDto({ id: 'obs-1', content: 'Observation 1' }),
        createObservationDto({ id: 'obs-2', content: 'Observation 2', injectId: 'inj-1' }),
      ]

      await cacheObservations('ex-123', observations)

      const cached = await db.observations.where('exerciseId').equals('ex-123').toArray()
      expect(cached).toHaveLength(2)
    })

    it('preserves pendingSync observations', async () => {
      await db.observations.put({
        id: 'obs-pending',
        exerciseId: 'ex-123',
        content: 'Pending observation',
        updatedAt: '2025-01-14T12:00:00Z',
        cachedAt: new Date(),
        pendingSync: true,
      })

      const serverObservations: ObservationDto[] = [
        createObservationDto({ id: 'obs-server', content: 'Server observation' }),
      ]

      await cacheObservations('ex-123', serverObservations)

      const pending = await db.observations.get('obs-pending')
      expect(pending).toBeDefined()
      expect(pending?.pendingSync).toBe(true)
    })

    it('updates sync metadata', async () => {
      await cacheObservations('ex-123', [])

      const metadata = await db.syncMetadata.get('observations-ex-123')
      expect(metadata).toBeDefined()
    })
  })

  describe('getCachedObservations', () => {
    it('returns all cached observations for exercise', async () => {
      await db.observations.bulkPut([
        {
          id: 'obs-1',
          exerciseId: 'ex-123',
          content: 'Obs 1',
          updatedAt: '2025-01-14T12:00:00Z',
          cachedAt: new Date(),
        },
        {
          id: 'obs-2',
          exerciseId: 'ex-other',
          content: 'Obs 2',
          updatedAt: '2025-01-14T12:00:00Z',
          cachedAt: new Date(),
        },
      ])

      const observations = await getCachedObservations('ex-123')
      expect(observations).toHaveLength(1)
      expect(observations[0].id).toBe('obs-1')
    })
  })

  describe('addLocalObservation', () => {
    it('adds observation with tempId for optimistic creates', async () => {
      const observation = {
        id: 'temp-obs-123',
        exerciseId: 'ex-123',
        content: 'New observation',
        updatedAt: new Date().toISOString(),
        cachedAt: new Date(),
        pendingSync: true,
        tempId: 'temp-obs-123',
      }

      await addLocalObservation(observation)

      const cached = await db.observations.get('temp-obs-123')
      expect(cached).toBeDefined()
      expect(cached?.tempId).toBe('temp-obs-123')
      expect(cached?.pendingSync).toBe(true)
    })
  })

  describe('updateCachedObservation', () => {
    it('updates observation fields', async () => {
      await db.observations.put({
        id: 'obs-1',
        exerciseId: 'ex-123',
        content: 'Original',
        updatedAt: '2025-01-14T12:00:00Z',
        cachedAt: new Date(),
      })

      await updateCachedObservation('obs-1', { content: 'Updated', rating: 'Exceeded' })

      const observation = await db.observations.get('obs-1')
      expect(observation?.content).toBe('Updated')
      expect(observation?.rating).toBe('Exceeded')
    })
  })

  describe('deleteCachedObservation', () => {
    it('deletes observation from cache', async () => {
      await db.observations.put({
        id: 'obs-1',
        exerciseId: 'ex-123',
        content: 'To delete',
        updatedAt: '2025-01-14T12:00:00Z',
        cachedAt: new Date(),
      })

      await deleteCachedObservation('obs-1')

      const observation = await db.observations.get('obs-1')
      expect(observation).toBeUndefined()
    })
  })

  describe('cachedObservationToDto', () => {
    it('converts cached observation to DTO with pendingSync and tempId', () => {
      const cached = {
        id: 'obs-1',
        exerciseId: 'ex-123',
        content: 'Test observation',
        rating: 'Met',
        recommendation: 'Keep it up',
        createdById: 'user-1',
        createdByName: 'John Doe',
        updatedAt: '2025-01-14T12:00:00Z',
        cachedAt: new Date(),
        pendingSync: true,
        tempId: 'temp-123',
      }

      const dto = cachedObservationToDto(cached)

      expect(dto.id).toBe('obs-1')
      expect(dto.content).toBe('Test observation')
      expect(dto.rating).toBe('Met')
      expect(dto.pendingSync).toBe(true)
      expect(dto.tempId).toBe('temp-123')
    })
  })

  // ============================================================================
  // Sync Metadata Tests
  // ============================================================================

  describe('updateSyncMetadata', () => {
    it('creates metadata record with lastSyncAt', async () => {
      const before = new Date()
      await updateSyncMetadata('test-key')
      const after = new Date()

      const metadata = await db.syncMetadata.get('test-key')
      expect(metadata).toBeDefined()
      expect(metadata?.lastSyncAt.getTime()).toBeGreaterThanOrEqual(before.getTime())
      expect(metadata?.lastSyncAt.getTime()).toBeLessThanOrEqual(after.getTime())
    })

    it('stores optional etag', async () => {
      await updateSyncMetadata('test-key', 'etag-123')

      const metadata = await db.syncMetadata.get('test-key')
      expect(metadata?.etag).toBe('etag-123')
    })

    it('updates existing metadata', async () => {
      await updateSyncMetadata('test-key', 'old-etag')
      await updateSyncMetadata('test-key', 'new-etag')

      const metadata = await db.syncMetadata.get('test-key')
      expect(metadata?.etag).toBe('new-etag')
    })
  })

  describe('getLastSyncTime', () => {
    it('returns last sync time when exists', async () => {
      const syncTime = new Date('2025-01-15T12:00:00Z')
      await db.syncMetadata.put({
        key: 'test-key',
        lastSyncAt: syncTime,
      })

      const result = await getLastSyncTime('test-key')
      expect(result).toEqual(syncTime)
    })

    it('returns null when not found', async () => {
      const result = await getLastSyncTime('nonexistent')
      expect(result).toBeNull()
    })
  })

  describe('isCacheStale', () => {
    it('returns true when no sync metadata exists', async () => {
      const isStale = await isCacheStale('nonexistent')
      expect(isStale).toBe(true)
    })

    it('returns true when cache is older than maxAge', async () => {
      const oldTime = new Date()
      oldTime.setMinutes(oldTime.getMinutes() - 10)

      await db.syncMetadata.put({
        key: 'test-key',
        lastSyncAt: oldTime,
      })

      const isStale = await isCacheStale('test-key', 5)
      expect(isStale).toBe(true)
    })

    it('returns false when cache is fresher than maxAge', async () => {
      const recentTime = new Date()
      recentTime.setMinutes(recentTime.getMinutes() - 2)

      await db.syncMetadata.put({
        key: 'test-key',
        lastSyncAt: recentTime,
      })

      const isStale = await isCacheStale('test-key', 5)
      expect(isStale).toBe(false)
    })

    it('uses 5 minutes as default maxAge', async () => {
      const fourMinutesAgo = new Date()
      fourMinutesAgo.setMinutes(fourMinutesAgo.getMinutes() - 4)

      await db.syncMetadata.put({
        key: 'test-key',
        lastSyncAt: fourMinutesAgo,
      })

      const isStale = await isCacheStale('test-key')
      expect(isStale).toBe(false)
    })
  })
})
