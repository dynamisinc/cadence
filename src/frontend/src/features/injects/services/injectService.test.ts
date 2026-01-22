import { describe, it, expect, vi, beforeEach } from 'vitest'
import { injectService } from './injectService'
import { apiClient } from '../../../core/services/api'
import { InjectType, InjectStatus, DeliveryMethod } from '../../../types'
import type { InjectDto, CreateInjectRequest, SkipInjectRequest } from '../types'

// Mock the API client
vi.mock('../../../core/services/api', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}))

const mockInject: InjectDto = {
  id: '123',
  injectNumber: 1,
  title: 'Test Inject',
  description: 'Test description',
  scheduledTime: '09:00:00',
  deliveryTime: null,
  scenarioDay: 1,
  scenarioTime: '08:00:00',
  target: 'EOC Director',
  source: 'County Manager',
  deliveryMethod: DeliveryMethod.Phone,
  deliveryMethodId: null,
  deliveryMethodName: null,
  deliveryMethodOther: null,
  injectType: InjectType.Standard,
  status: InjectStatus.Pending,
  sequence: 1,
  parentInjectId: null,
  triggerCondition: null,
  expectedAction: 'Acknowledge and respond',
  controllerNotes: null,
  readyAt: null,
  firedAt: null,
  firedBy: null,
  firedByName: null,
  skippedAt: null,
  skippedBy: null,
  skippedByName: null,
  skipReason: null,
  mselId: 'msel-1',
  phaseId: 'phase-1',
  phaseName: 'Warning Phase',
  objectiveIds: [],
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
  sourceReference: null,
  priority: null,
  triggerType: 'Manual',
  responsibleController: null,
  locationName: null,
  locationType: null,
  track: null,
}

describe('injectService', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('getInjects', () => {
    it('fetches injects for an exercise', async () => {
      vi.mocked(apiClient.get).mockResolvedValue({ data: [mockInject] })

      const result = await injectService.getInjects('exercise-1')

      expect(apiClient.get).toHaveBeenCalledWith('/exercises/exercise-1/injects')
      expect(result).toEqual([mockInject])
    })
  })

  describe('getInject', () => {
    it('fetches a single inject by ID', async () => {
      vi.mocked(apiClient.get).mockResolvedValue({ data: mockInject })

      const result = await injectService.getInject('exercise-1', '123')

      expect(apiClient.get).toHaveBeenCalledWith('/exercises/exercise-1/injects/123')
      expect(result).toEqual(mockInject)
    })
  })

  describe('createInject', () => {
    it('creates a new inject', async () => {
      const request: CreateInjectRequest = {
        title: 'New Inject',
        description: 'New description',
        scheduledTime: '10:00:00',
        target: 'Fire Chief',
      }
      vi.mocked(apiClient.post).mockResolvedValue({ data: mockInject })

      const result = await injectService.createInject('exercise-1', request)

      expect(apiClient.post).toHaveBeenCalledWith('/exercises/exercise-1/injects', request)
      expect(result).toEqual(mockInject)
    })
  })

  describe('fireInject', () => {
    it('fires an inject', async () => {
      const firedInject = { ...mockInject, status: InjectStatus.Fired }
      vi.mocked(apiClient.post).mockResolvedValue({ data: firedInject })

      const result = await injectService.fireInject('exercise-1', '123')

      expect(apiClient.post).toHaveBeenCalledWith('/exercises/exercise-1/injects/123/fire', {})
      expect(result.status).toBe(InjectStatus.Fired)
    })

    it('fires an inject with notes', async () => {
      const firedInject = { ...mockInject, status: InjectStatus.Fired }
      vi.mocked(apiClient.post).mockResolvedValue({ data: firedInject })

      await injectService.fireInject('exercise-1', '123', { notes: 'Early delivery' })

      expect(apiClient.post).toHaveBeenCalledWith(
        '/exercises/exercise-1/injects/123/fire',
        { notes: 'Early delivery' },
      )
    })
  })

  describe('skipInject', () => {
    it('skips an inject with reason', async () => {
      const skippedInject = { ...mockInject, status: InjectStatus.Skipped, skipReason: 'Time constraints' }
      vi.mocked(apiClient.post).mockResolvedValue({ data: skippedInject })

      const request: SkipInjectRequest = { reason: 'Time constraints' }
      const result = await injectService.skipInject('exercise-1', '123', request)

      expect(apiClient.post).toHaveBeenCalledWith('/exercises/exercise-1/injects/123/skip', request)
      expect(result.status).toBe(InjectStatus.Skipped)
      expect(result.skipReason).toBe('Time constraints')
    })
  })

  describe('deleteInject', () => {
    it('deletes an inject', async () => {
      vi.mocked(apiClient.delete).mockResolvedValue({})

      await injectService.deleteInject('exercise-1', '123')

      expect(apiClient.delete).toHaveBeenCalledWith('/exercises/exercise-1/injects/123')
    })
  })
})
