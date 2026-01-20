/**
 * Test fixtures for Inject-related tests
 *
 * Provides mock data for InjectDto with all required fields.
 */

import { InjectType, InjectStatus, TriggerType, DeliveryMethod } from '../../types'
import type { InjectDto } from '../../features/injects/types'

/**
 * Create a mock InjectDto with default values
 * All required fields are populated with sensible defaults
 */
export const createMockInject = (overrides?: Partial<InjectDto>): InjectDto => {
  return {
    id: 'inject-1',
    injectNumber: 1,
    title: 'Test Inject',
    description: 'Test inject description',
    scheduledTime: '10:00:00',
    deliveryTime: null,
    scenarioDay: 1,
    scenarioTime: '10:00:00',
    target: 'Emergency Operations Center',
    source: 'Controller',
    deliveryMethod: DeliveryMethod.Verbal,
    deliveryMethodId: null,
    deliveryMethodName: null,
    deliveryMethodOther: null,
    injectType: InjectType.Standard,
    status: InjectStatus.Pending,
    sequence: 1,
    parentInjectId: null,
    triggerCondition: null,
    expectedAction: 'Expected player action',
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
    phaseId: null,
    phaseName: null,
    objectiveIds: [],
    createdAt: '2026-01-20T10:00:00Z',
    updatedAt: '2026-01-20T10:00:00Z',
    sourceReference: null,
    priority: null,
    triggerType: TriggerType.Manual,
    responsibleController: null,
    locationName: null,
    locationType: null,
    track: null,
    ...overrides,
  }
}
