/**
 * useStoryTime Hook Tests
 *
 * Tests for the story time calculation hook.
 */

import { describe, it, expect } from 'vitest'
import { renderHook } from '@testing-library/react'
import { useStoryTime } from './useStoryTime'
import { TimelineMode, ExerciseType, ExerciseStatus, DeliveryMode } from '../../../types'
import type { ExerciseDto } from '../../exercises/types'
import type { InjectDto } from '../../injects/types'

describe('useStoryTime', () => {
  const baseExercise: ExerciseDto = {
    id: 'ex1',
    name: 'Test Exercise',
    description: null,
    exerciseType: ExerciseType.TTX,
    status: ExerciseStatus.Active,
    isPracticeMode: false,
    scheduledDate: '2024-01-15',
    startTime: null,
    endTime: null,
    timeZoneId: 'America/New_York',
    location: null,
    organizationId: 'org1',
    activeMselId: 'msel1',
    deliveryMode: DeliveryMode.ClockDriven,
    timelineMode: TimelineMode.RealTime,
    timeScale: null,
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-01T00:00:00Z',
    createdBy: 'user1',
    activatedAt: null,
    activatedBy: null,
    completedAt: null,
    completedBy: null,
    archivedAt: null,
    archivedBy: null,
    hasBeenPublished: false,
    previousStatus: null,
  }

  describe('RealTime mode', () => {
    it('calculates story time from elapsed in RealTime mode', () => {
      const exercise: ExerciseDto = {
        ...baseExercise,
        timelineMode: TimelineMode.RealTime,
      }

      const elapsedTimeMs = 2 * 60 * 60 * 1000 + 30 * 60 * 1000 // 2:30:00

      const { result } = renderHook(() =>
        useStoryTime({
          exercise,
          elapsedTimeMs,
          currentInject: null,
        }),
      )

      expect(result.current.storyTime).toEqual({
        day: 1,
        hours: 2,
        minutes: 30,
      })
      expect(result.current.formattedStoryTime).toBe('Day 1 • 02:30')
      expect(result.current.isStoryOnly).toBe(false)
    })

    it('handles zero elapsed time', () => {
      const exercise: ExerciseDto = {
        ...baseExercise,
        timelineMode: TimelineMode.RealTime,
      }

      const { result } = renderHook(() =>
        useStoryTime({
          exercise,
          elapsedTimeMs: 0,
          currentInject: null,
        }),
      )

      expect(result.current.storyTime).toEqual({
        day: 1,
        hours: 0,
        minutes: 0,
      })
      expect(result.current.formattedStoryTime).toBe('Day 1 • 00:00')
    })
  })

  describe('Compressed mode', () => {
    it('applies compression in Compressed mode', () => {
      const exercise: ExerciseDto = {
        ...baseExercise,
        timelineMode: TimelineMode.Compressed,
        timeScale: 4,
      }

      const elapsedTimeMs = 15 * 60 * 1000 // 15 real minutes

      const { result } = renderHook(() =>
        useStoryTime({
          exercise,
          elapsedTimeMs,
          currentInject: null,
        }),
      )

      // 15 real minutes × 4 = 60 story minutes = 1 hour
      expect(result.current.storyTime).toEqual({
        day: 1,
        hours: 1,
        minutes: 0,
      })
      expect(result.current.formattedStoryTime).toBe('Day 1 • 01:00')
      expect(result.current.isStoryOnly).toBe(false)
    })

    it('handles day rollover in Compressed mode', () => {
      const exercise: ExerciseDto = {
        ...baseExercise,
        timelineMode: TimelineMode.Compressed,
        timeScale: 4,
      }

      const elapsedTimeMs = 6 * 60 * 60 * 1000 // 6 real hours

      const { result } = renderHook(() =>
        useStoryTime({
          exercise,
          elapsedTimeMs,
          currentInject: null,
        }),
      )

      // 6 real hours × 4 = 24 story hours = Day 2
      expect(result.current.storyTime).toEqual({
        day: 2,
        hours: 0,
        minutes: 0,
      })
      expect(result.current.formattedStoryTime).toBe('Day 2 • 00:00')
    })
  })

  describe('StoryOnly mode', () => {
    const mockInject: InjectDto = {
      id: 'inj1',
      injectNumber: 1,
      title: 'Test Inject',
      description: 'Test description',
      scheduledTime: '09:00:00',
      deliveryTime: null,
      scenarioDay: 1,
      scenarioTime: '18:00:00',
      target: 'Test target',
      source: null,
      deliveryMethod: null,
      deliveryMethodId: null,
      deliveryMethodName: null,
      deliveryMethodOther: null,
      injectType: 'Standard',
      status: 'Pending',
      sequence: 1,
      parentInjectId: null,
      triggerCondition: null,
      expectedAction: null,
      controllerNotes: null,
      readyAt: null,
      firedAt: null,
      firedBy: null,
      firedByName: null,
      skippedAt: null,
      skippedBy: null,
      skippedByName: null,
      skipReason: null,
      mselId: 'msel1',
      phaseId: null,
      phaseName: null,
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

    it('derives from current inject in StoryOnly mode', () => {
      const exercise: ExerciseDto = {
        ...baseExercise,
        timelineMode: TimelineMode.StoryOnly,
      }

      const { result } = renderHook(() =>
        useStoryTime({
          exercise,
          elapsedTimeMs: 0,
          currentInject: mockInject,
        }),
      )

      expect(result.current.storyTime).toEqual({
        day: 1,
        hours: 18,
        minutes: 0,
      })
      expect(result.current.formattedStoryTime).toBe('Day 1 • 18:00')
      expect(result.current.isStoryOnly).toBe(true)
    })

    it('returns null when no inject in StoryOnly mode', () => {
      const exercise: ExerciseDto = {
        ...baseExercise,
        timelineMode: TimelineMode.StoryOnly,
      }

      const { result } = renderHook(() =>
        useStoryTime({
          exercise,
          elapsedTimeMs: 0,
          currentInject: null,
        }),
      )

      expect(result.current.storyTime).toBeNull()
      expect(result.current.formattedStoryTime).toBe('—')
      expect(result.current.isStoryOnly).toBe(true)
    })

    it('handles inject with no scenario time', () => {
      const exercise: ExerciseDto = {
        ...baseExercise,
        timelineMode: TimelineMode.StoryOnly,
      }

      const injectWithoutScenario: InjectDto = {
        ...mockInject,
        scenarioDay: null,
        scenarioTime: null,
      }

      const { result } = renderHook(() =>
        useStoryTime({
          exercise,
          elapsedTimeMs: 0,
          currentInject: injectWithoutScenario,
        }),
      )

      expect(result.current.storyTime).toBeNull()
      expect(result.current.formattedStoryTime).toBe('—')
    })
  })

  describe('Edge cases', () => {
    it('handles undefined currentInject', () => {
      const exercise: ExerciseDto = {
        ...baseExercise,
        timelineMode: TimelineMode.RealTime,
      }

      const { result } = renderHook(() =>
        useStoryTime({
          exercise,
          elapsedTimeMs: 1000,
          currentInject: undefined,
        }),
      )

      expect(result.current.storyTime).toBeDefined()
      expect(result.current.formattedStoryTime).toBeTruthy()
    })

    it('updates when elapsed time changes', () => {
      const exercise: ExerciseDto = {
        ...baseExercise,
        timelineMode: TimelineMode.RealTime,
      }

      const { result, rerender } = renderHook(
        ({ elapsedTimeMs }) =>
          useStoryTime({
            exercise,
            elapsedTimeMs,
            currentInject: null,
          }),
        {
          initialProps: { elapsedTimeMs: 0 },
        },
      )

      expect(result.current.formattedStoryTime).toBe('Day 1 • 00:00')

      rerender({ elapsedTimeMs: 60 * 60 * 1000 }) // 1 hour

      expect(result.current.formattedStoryTime).toBe('Day 1 • 01:00')
    })
  })
})
