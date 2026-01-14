/**
 * Narrative Generator Tests
 *
 * Tests for generating narrative text from inject data.
 */

import { describe, it, expect } from 'vitest'
import {
  generateStorySoFar,
  generateCurrentEvent,
  generateUpcomingPreview,
} from './narrativeGenerator'
import { InjectStatus } from '../../../types'
import type { InjectDto } from '../../injects/types'

// Helper to create mock inject
const createMockInject = (overrides: Partial<InjectDto> = {}): InjectDto => ({
  id: 'inject-1',
  injectNumber: 1,
  title: 'Test Inject',
  description: 'Test description for the inject.',
  scheduledTime: '09:00:00',
  scenarioDay: null,
  scenarioTime: null,
  target: 'EOC Director',
  source: 'NWS',
  deliveryMethod: 'Email',
  injectType: 'Standard',
  status: InjectStatus.Pending,
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
  createdAt: '2025-01-01T00:00:00Z',
  updatedAt: '2025-01-01T00:00:00Z',
  ...overrides,
})

describe('narrativeGenerator', () => {
  describe('generateStorySoFar', () => {
    it('returns empty array when no fired injects', () => {
      const injects = [
        createMockInject({ status: InjectStatus.Pending }),
        createMockInject({ status: InjectStatus.Pending }),
      ]

      const result = generateStorySoFar(injects)

      expect(result).toEqual([])
    })

    it('returns descriptions of fired injects', () => {
      const injects = [
        createMockInject({
          id: '1',
          status: InjectStatus.Fired,
          firedAt: '2025-01-01T09:00:00Z',
          description: 'Hurricane watch issued.',
        }),
        createMockInject({
          id: '2',
          status: InjectStatus.Pending,
          description: 'This should not appear.',
        }),
      ]

      const result = generateStorySoFar(injects)

      expect(result).toHaveLength(1)
      expect(result[0]).toContain('Hurricane watch issued.')
    })

    it('sorts fired injects by firedAt ascending (oldest first)', () => {
      const injects = [
        createMockInject({
          id: '1',
          status: InjectStatus.Fired,
          firedAt: '2025-01-01T10:00:00Z',
          description: 'Second event.',
        }),
        createMockInject({
          id: '2',
          status: InjectStatus.Fired,
          firedAt: '2025-01-01T09:00:00Z',
          description: 'First event.',
        }),
      ]

      const result = generateStorySoFar(injects)

      expect(result[0]).toContain('First event.')
      expect(result[1]).toContain('Second event.')
    })

    it('excludes skipped injects', () => {
      const injects = [
        createMockInject({
          id: '1',
          status: InjectStatus.Fired,
          firedAt: '2025-01-01T09:00:00Z',
          description: 'Fired inject.',
        }),
        createMockInject({
          id: '2',
          status: InjectStatus.Skipped,
          skippedAt: '2025-01-01T09:30:00Z',
          description: 'This was skipped.',
        }),
      ]

      const result = generateStorySoFar(injects)

      expect(result).toHaveLength(1)
      expect(result.join('')).not.toContain('skipped')
    })
  })

  describe('generateCurrentEvent', () => {
    it('returns null when no next inject', () => {
      const result = generateCurrentEvent(null)

      expect(result).toBeNull()
    })

    it('generates narrative for next inject with target and source', () => {
      const inject = createMockInject({
        target: 'EOC Director',
        source: 'National Weather Service',
        description: 'Hurricane warning upgraded to Category 3.',
      })

      const result = generateCurrentEvent(inject)

      expect(result).not.toBeNull()
      expect(result).toContain('EOC Director')
      expect(result).toContain('National Weather Service')
      expect(result).toContain('Hurricane warning upgraded to Category 3.')
    })

    it('handles inject without source', () => {
      const inject = createMockInject({
        target: 'Emergency Manager',
        source: null,
        description: 'Make critical decision.',
      })

      const result = generateCurrentEvent(inject)

      expect(result).not.toBeNull()
      expect(result).toContain('Emergency Manager')
      expect(result).toContain('Make critical decision.')
    })
  })

  describe('generateUpcomingPreview', () => {
    it('returns empty array when no upcoming injects', () => {
      const result = generateUpcomingPreview([])

      expect(result).toEqual([])
    })

    it('returns preview items for upcoming injects', () => {
      const injects = [
        createMockInject({
          id: '1',
          title: 'Media Inquiry',
          target: 'PIO',
        }),
        createMockInject({
          id: '2',
          title: 'Shelter Capacity',
          target: 'Shelter Manager',
        }),
      ]

      const result = generateUpcomingPreview(injects)

      expect(result).toHaveLength(2)
      expect(result[0]).toContain('Media Inquiry')
      expect(result[1]).toContain('Shelter Capacity')
    })

    it('limits to 5 items', () => {
      const injects = Array.from({ length: 10 }, (_, i) =>
        createMockInject({
          id: `inject-${i}`,
          title: `Inject ${i + 1}`,
        }),
      )

      const result = generateUpcomingPreview(injects)

      expect(result).toHaveLength(5)
    })
  })
})
