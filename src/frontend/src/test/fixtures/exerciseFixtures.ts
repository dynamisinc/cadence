/**
 * Test fixtures for Exercise-related tests
 *
 * Provides mock data for ExerciseDto and related types with all required fields.
 */

import { ExerciseType, ExerciseStatus, DeliveryMode, TimelineMode } from '../../types'
import type { ExerciseDto } from '../../features/exercises/types'

/**
 * Create a mock ExerciseDto with default values
 * All required fields are populated with sensible defaults
 */
export const createMockExercise = (overrides?: Partial<ExerciseDto>): ExerciseDto => {
  return {
    id: 'ex-1',
    name: 'Test Exercise',
    description: 'Test description',
    exerciseType: ExerciseType.TTX,
    status: ExerciseStatus.Draft,
    isPracticeMode: false,
    scheduledDate: '2026-03-15',
    startTime: '09:00:00',
    endTime: '17:00:00',
    timeZoneId: 'America/New_York',
    location: 'Test Location',
    organizationId: 'org-1',
    activeMselId: null,
    // Timing configuration (CLK-01)
    deliveryMode: DeliveryMode.ClockDriven,
    timelineMode: TimelineMode.RealTime,
    timeScale: null,
    // Timestamps
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
    createdBy: 'user-1',
    // Status transitions
    activatedAt: null,
    activatedBy: null,
    completedAt: null,
    completedBy: null,
    archivedAt: null,
    archivedBy: null,
    // Lifecycle
    hasBeenPublished: false,
    previousStatus: null,
    ...overrides,
  }
}
