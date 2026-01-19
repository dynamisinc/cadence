import { describe, it, expect, vi, beforeEach } from 'vitest'
import { exerciseService } from './exerciseService'
import { apiClient } from '../../../core/services/api'
import { ExerciseType, ExerciseStatus } from '../../../types'
import type { ExerciseDto } from '../types'

// Mock the API client
vi.mock('../../../core/services/api', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
  },
}))

const mockExercise: ExerciseDto = {
  id: '123',
  name: 'Test Exercise',
  description: 'Test description',
  exerciseType: ExerciseType.TTX,
  status: ExerciseStatus.Draft,
  isPracticeMode: false,
  scheduledDate: '2026-01-15',
  startTime: null,
  endTime: null,
  timeZoneId: 'UTC',
  location: null,
  organizationId: 'org-123',
  activeMselId: null,
  createdAt: '2026-01-10T00:00:00Z',
  updatedAt: '2026-01-10T00:00:00Z',
  createdBy: 'user-123',
  activatedAt: null,
  activatedBy: null,
  completedAt: null,
  completedBy: null,
  archivedAt: null,
  archivedBy: null,
  hasBeenPublished: false,
  previousStatus: null,
}

describe('exerciseService', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('getExercises', () => {
    it('fetches all exercises', async () => {
      vi.mocked(apiClient.get).mockResolvedValue({ data: [mockExercise] })

      const result = await exerciseService.getExercises()

      expect(apiClient.get).toHaveBeenCalledWith('/api/exercises')
      expect(result).toHaveLength(1)
      expect(result[0].name).toBe('Test Exercise')
    })
  })

  describe('getExercise', () => {
    it('fetches a single exercise by ID', async () => {
      vi.mocked(apiClient.get).mockResolvedValue({ data: mockExercise })

      const result = await exerciseService.getExercise('123')

      expect(apiClient.get).toHaveBeenCalledWith('/api/exercises/123')
      expect(result.name).toBe('Test Exercise')
    })
  })

  describe('createExercise', () => {
    it('creates a new exercise', async () => {
      vi.mocked(apiClient.post).mockResolvedValue({ data: mockExercise })

      const request = {
        name: 'Test Exercise',
        exerciseType: ExerciseType.TTX,
        scheduledDate: '2026-01-15',
      }

      const result = await exerciseService.createExercise(request)

      expect(apiClient.post).toHaveBeenCalledWith('/api/exercises', request)
      expect(result.name).toBe('Test Exercise')
      expect(result.status).toBe(ExerciseStatus.Draft)
    })
  })

  describe('updateExercise', () => {
    it('updates an existing exercise', async () => {
      const updatedExercise = { ...mockExercise, name: 'Updated Exercise' }
      vi.mocked(apiClient.put).mockResolvedValue({ data: updatedExercise })

      const request = {
        name: 'Updated Exercise',
        exerciseType: ExerciseType.TTX,
        scheduledDate: '2026-01-15',
      }

      const result = await exerciseService.updateExercise('123', request)

      expect(apiClient.put).toHaveBeenCalledWith('/api/exercises/123', request)
      expect(result.name).toBe('Updated Exercise')
    })
  })
})
