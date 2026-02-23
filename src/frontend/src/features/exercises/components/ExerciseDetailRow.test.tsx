/**
 * ExerciseDetailRow Component Tests
 *
 * Tests for the exercise detail row that displays expanded information
 * in the exercise table including location, organization, clock state,
 * ready injects, and progress bar.
 */

import { describe, it, expect } from 'vitest'
import { screen } from '@testing-library/react'
import { render } from '../../../test/test-utils'
import { ExerciseDetailRow } from './ExerciseDetailRow'
import type { ExerciseDto } from '../types'
import { ExerciseType, ExerciseStatus, DeliveryMode, TimelineMode } from '../../../types'

// Helper to create mock exercise
const createMockExercise = (overrides: Partial<ExerciseDto> = {}): ExerciseDto => ({
  id: 'exercise-1',
  name: 'Hurricane Maria TTX',
  description: 'Table-top exercise for hurricane response',
  exerciseType: ExerciseType.TTX,
  status: ExerciseStatus.Active,
  isPracticeMode: false,
  scheduledDate: '2026-02-23',
  startTime: '09:00:00',
  endTime: '17:00:00',
  timeZoneId: 'America/New_York',
  location: 'Emergency Operations Center',
  organizationId: 'org-1',
  activeMselId: 'msel-1',
  deliveryMode: DeliveryMode.ClockDriven,
  timelineMode: TimelineMode.WallClock,
  timeScale: null,
  createdAt: '2026-02-01T00:00:00Z',
  updatedAt: '2026-02-01T00:00:00Z',
  createdBy: 'user-1',
  activatedAt: null,
  activatedBy: null,
  completedAt: null,
  completedBy: null,
  archivedAt: null,
  archivedBy: null,
  hasBeenPublished: false,
  previousStatus: null,
  clockMultiplier: 1,
  autoFireEnabled: false,
  confirmFireInject: true,
  confirmSkipInject: true,
  confirmClockControl: true,
  maxDuration: null,
  injectCount: 10,
  firedInjectCount: 5,
  organizationName: 'FEMA Region 2',
  clockState: 'Running',
  elapsedSeconds: 3600,
  readyInjectCount: 2,
  ...overrides,
})

describe('ExerciseDetailRow', () => {
  describe('Basic Rendering', () => {
    it('renders without crashing', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise()}
          isExpanded={true}
          showOrganization={false}
        />,
      )
    })

    it('renders location when present', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({ location: 'City Hall' })}
          isExpanded={true}
          showOrganization={false}
        />,
      )
      expect(screen.getByText('City Hall')).toBeInTheDocument()
    })

    it('does not render location when null', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({ location: null })}
          isExpanded={true}
          showOrganization={false}
        />,
      )
      expect(screen.queryByText(/City Hall/)).not.toBeInTheDocument()
    })

    it('renders progress bar when exercise has injects', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({ injectCount: 10, firedInjectCount: 5 })}
          isExpanded={true}
          showOrganization={false}
        />,
      )
      expect(screen.getByText('Progress')).toBeInTheDocument()
      expect(screen.getByText('5 / 10 injects')).toBeInTheDocument()
      expect(screen.getByRole('progressbar')).toBeInTheDocument()
    })

    it('does not render progress bar when exercise has no injects', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({ injectCount: 0, firedInjectCount: 0 })}
          isExpanded={true}
          showOrganization={false}
        />,
      )
      expect(screen.queryByRole('progressbar')).not.toBeInTheDocument()
    })
  })

  describe('Organization Name Display', () => {
    it('renders organization name when showOrganization is true and organizationName is present', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({ organizationName: 'FEMA Region 2' })}
          isExpanded={true}
          showOrganization={true}
        />,
      )
      expect(screen.getByText('FEMA Region 2')).toBeInTheDocument()
    })

    it('does not render organization name when showOrganization is false', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({ organizationName: 'FEMA Region 2' })}
          isExpanded={true}
          showOrganization={false}
        />,
      )
      expect(screen.queryByText('FEMA Region 2')).not.toBeInTheDocument()
    })

    it('does not render organization name when organizationName is null', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({ organizationName: null })}
          isExpanded={true}
          showOrganization={true}
        />,
      )
      // Should not have any building icon or org text
      const boxes = screen.queryAllByText(/FEMA|Organization/)
      expect(boxes).toHaveLength(0)
    })

    it('does not render organization name when organizationName is empty string', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({ organizationName: '' })}
          isExpanded={true}
          showOrganization={true}
        />,
      )
      // Empty string is falsy, so conditional should not render
      const boxes = screen.queryAllByText(/FEMA|Organization/)
      expect(boxes).toHaveLength(0)
    })
  })

  describe('Clock State Display', () => {
    it('shows clock info for active exercises with elapsed time', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            status: ExerciseStatus.Active,
            elapsedSeconds: 3661, // 1 hour, 1 minute, 1 second
            clockState: 'Running',
          })}
          isExpanded={true}
          showOrganization={false}
        />,
      )
      expect(screen.getByText('Elapsed: 01:01:01')).toBeInTheDocument()
      expect(screen.getByText('Running')).toBeInTheDocument()
    })

    it('does not show clock info for non-active exercises', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            status: ExerciseStatus.Draft,
            elapsedSeconds: 3600,
            clockState: 'Stopped',
          })}
          isExpanded={true}
          showOrganization={false}
        />,
      )
      expect(screen.queryByText(/Elapsed:/)).not.toBeInTheDocument()
      expect(screen.queryByText('Stopped')).not.toBeInTheDocument()
    })

    it('does not show clock info when elapsedSeconds is undefined', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            status: ExerciseStatus.Active,
            elapsedSeconds: undefined,
          })}
          isExpanded={true}
          showOrganization={false}
        />,
      )
      expect(screen.queryByText(/Elapsed:/)).not.toBeInTheDocument()
    })

    it('displays "Paused" clock state correctly', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            status: ExerciseStatus.Active,
            elapsedSeconds: 1800,
            clockState: 'Paused',
          })}
          isExpanded={true}
          showOrganization={false}
        />,
      )
      expect(screen.getByText('Paused')).toBeInTheDocument()
      expect(screen.getByText('Elapsed: 00:30:00')).toBeInTheDocument()
    })

    it('displays "Stopped" clock state correctly', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            status: ExerciseStatus.Active,
            elapsedSeconds: 0,
            clockState: 'Stopped',
          })}
          isExpanded={true}
          showOrganization={false}
        />,
      )
      expect(screen.getByText('Stopped')).toBeInTheDocument()
      expect(screen.getByText('Elapsed: 00:00:00')).toBeInTheDocument()
    })
  })

  describe('Elapsed Time Formatting', () => {
    it('formats seconds correctly', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            status: ExerciseStatus.Active,
            elapsedSeconds: 45,
          })}
          isExpanded={true}
          showOrganization={false}
        />,
      )
      expect(screen.getByText('Elapsed: 00:00:45')).toBeInTheDocument()
    })

    it('formats minutes correctly', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            status: ExerciseStatus.Active,
            elapsedSeconds: 754, // 12:34
          })}
          isExpanded={true}
          showOrganization={false}
        />,
      )
      expect(screen.getByText('Elapsed: 00:12:34')).toBeInTheDocument()
    })

    it('formats hours correctly', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            status: ExerciseStatus.Active,
            elapsedSeconds: 7384, // 2:03:04
          })}
          isExpanded={true}
          showOrganization={false}
        />,
      )
      expect(screen.getByText('Elapsed: 02:03:04')).toBeInTheDocument()
    })

    it('handles large elapsed times (over 24 hours)', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            status: ExerciseStatus.Active,
            elapsedSeconds: 90061, // 25:01:01
          })}
          isExpanded={true}
          showOrganization={false}
        />,
      )
      expect(screen.getByText('Elapsed: 25:01:01')).toBeInTheDocument()
    })

    it('formats zero time correctly', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            status: ExerciseStatus.Active,
            elapsedSeconds: 0,
          })}
          isExpanded={true}
          showOrganization={false}
        />,
      )
      expect(screen.getByText('Elapsed: 00:00:00')).toBeInTheDocument()
    })
  })

  describe('Ready Injects Display (Controller-Only)', () => {
    it('shows ready injects for Controller role in active exercise with ready injects', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            status: ExerciseStatus.Active,
            readyInjectCount: 3,
          })}
          isExpanded={true}
          userRole="Controller"
          showOrganization={false}
        />,
      )
      expect(screen.getByText('3 injects ready')).toBeInTheDocument()
    })

    it('shows correct singular form for 1 ready inject', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            status: ExerciseStatus.Active,
            readyInjectCount: 1,
          })}
          isExpanded={true}
          userRole="Controller"
          showOrganization={false}
        />,
      )
      expect(screen.getByText('1 inject ready')).toBeInTheDocument()
    })

    it('does not show ready injects for non-Controller roles', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            status: ExerciseStatus.Active,
            readyInjectCount: 3,
          })}
          isExpanded={true}
          userRole="Evaluator"
          showOrganization={false}
        />,
      )
      expect(screen.queryByText(/injects ready/)).not.toBeInTheDocument()
    })

    it('does not show ready injects for Observer role', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            status: ExerciseStatus.Active,
            readyInjectCount: 3,
          })}
          isExpanded={true}
          userRole="Observer"
          showOrganization={false}
        />,
      )
      expect(screen.queryByText(/injects ready/)).not.toBeInTheDocument()
    })

    it('does not show ready injects for Exercise Director role', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            status: ExerciseStatus.Active,
            readyInjectCount: 3,
          })}
          isExpanded={true}
          userRole="ExerciseDirector"
          showOrganization={false}
        />,
      )
      expect(screen.queryByText(/injects ready/)).not.toBeInTheDocument()
    })

    it('does not show ready injects for non-active exercises', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            status: ExerciseStatus.Draft,
            readyInjectCount: 3,
          })}
          isExpanded={true}
          userRole="Controller"
          showOrganization={false}
        />,
      )
      expect(screen.queryByText(/injects ready/)).not.toBeInTheDocument()
    })

    it('does not show ready injects when count is 0', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            status: ExerciseStatus.Active,
            readyInjectCount: 0,
          })}
          isExpanded={true}
          userRole="Controller"
          showOrganization={false}
        />,
      )
      expect(screen.queryByText(/injects ready/)).not.toBeInTheDocument()
    })

    it('does not show ready injects when readyInjectCount is null', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            status: ExerciseStatus.Active,
            readyInjectCount: null,
          })}
          isExpanded={true}
          userRole="Controller"
          showOrganization={false}
        />,
      )
      expect(screen.queryByText(/injects ready/)).not.toBeInTheDocument()
    })

    it('does not show ready injects when readyInjectCount is undefined', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            status: ExerciseStatus.Active,
            readyInjectCount: undefined,
          })}
          isExpanded={true}
          userRole="Controller"
          showOrganization={false}
        />,
      )
      expect(screen.queryByText(/injects ready/)).not.toBeInTheDocument()
    })
  })

  describe('Boolean Logic for Ready Injects (Bug Fix)', () => {
    it('does not render "0" when readyInjectCount is 0', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            status: ExerciseStatus.Active,
            readyInjectCount: 0,
          })}
          isExpanded={true}
          userRole="Controller"
          showOrganization={false}
        />,
      )
      // Should NOT render the ready injects section at all
      // This was the bug: without Boolean(), React would render "0"
      const text = screen.queryByText('0 injects ready')
      expect(text).not.toBeInTheDocument()
    })

    it('correctly uses Boolean() wrapper to prevent "0" rendering', () => {
      // This test verifies the specific fix from commit 502f895
      // The component uses: Boolean(userRole === 'Controller' && ... && (exercise.readyInjectCount ?? 0) > 0)

      const { container } = render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            status: ExerciseStatus.Active,
            readyInjectCount: 0,
          })}
          isExpanded={true}
          userRole="Controller"
          showOrganization={false}
        />,
      )

      // Check that "0" is not in the rendered output
      expect(container.textContent).not.toMatch(/^0$/)
    })
  })

  describe('Progress Bar', () => {
    it('calculates progress percentage correctly', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            injectCount: 10,
            firedInjectCount: 5,
          })}
          isExpanded={true}
          showOrganization={false}
        />,
      )
      const progressBar = screen.getByRole('progressbar')
      expect(progressBar).toHaveAttribute('aria-valuenow', '50')
    })

    it('shows 100% progress when all injects fired', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            injectCount: 10,
            firedInjectCount: 10,
          })}
          isExpanded={true}
          showOrganization={false}
        />,
      )
      const progressBar = screen.getByRole('progressbar')
      expect(progressBar).toHaveAttribute('aria-valuenow', '100')
    })

    it('shows 0% progress when no injects fired', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            injectCount: 10,
            firedInjectCount: 0,
          })}
          isExpanded={true}
          showOrganization={false}
        />,
      )
      const progressBar = screen.getByRole('progressbar')
      expect(progressBar).toHaveAttribute('aria-valuenow', '0')
    })

    it('handles partial progress correctly', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            injectCount: 3,
            firedInjectCount: 1,
          })}
          isExpanded={true}
          showOrganization={false}
        />,
      )
      const progressBar = screen.getByRole('progressbar')
      // 1/3 = 33.333...% (MUI rounds to integer)
      expect(progressBar).toHaveAttribute('aria-valuenow', '33')
    })
  })

  describe('Complex Scenarios', () => {
    it('renders all information for a fully-populated active exercise', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            location: 'Emergency Operations Center',
            organizationName: 'FEMA Region 2',
            status: ExerciseStatus.Active,
            clockState: 'Running',
            elapsedSeconds: 7200,
            readyInjectCount: 5,
            injectCount: 20,
            firedInjectCount: 10,
          })}
          isExpanded={true}
          userRole="Controller"
          showOrganization={true}
        />,
      )

      expect(screen.getByText('Emergency Operations Center')).toBeInTheDocument()
      expect(screen.getByText('FEMA Region 2')).toBeInTheDocument()
      expect(screen.getByText('Elapsed: 02:00:00')).toBeInTheDocument()
      expect(screen.getByText('Running')).toBeInTheDocument()
      expect(screen.getByText('5 injects ready')).toBeInTheDocument()
      expect(screen.getByText('10 / 20 injects')).toBeInTheDocument()
      expect(screen.getByRole('progressbar')).toBeInTheDocument()
    })

    it('renders minimal information for a draft exercise', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            location: null,
            organizationName: null,
            status: ExerciseStatus.Draft,
            clockState: null,
            elapsedSeconds: undefined,
            readyInjectCount: 0,
            injectCount: 0,
            firedInjectCount: 0,
          })}
          isExpanded={true}
          userRole="Controller"
          showOrganization={false}
        />,
      )

      // Should only render the container, no detail sections
      expect(screen.queryByText(/Emergency/)).not.toBeInTheDocument()
      expect(screen.queryByText(/Elapsed/)).not.toBeInTheDocument()
      expect(screen.queryByText(/ready/)).not.toBeInTheDocument()
      expect(screen.queryByRole('progressbar')).not.toBeInTheDocument()
    })

    it('handles exercise with location but no organization in multi-org context', () => {
      render(
        <ExerciseDetailRow
          exercise={createMockExercise({
            location: 'City Hall',
            organizationName: null,
          })}
          isExpanded={true}
          showOrganization={true}
        />,
      )

      expect(screen.getByText('City Hall')).toBeInTheDocument()
      expect(screen.queryByText(/FEMA|Organization/)).not.toBeInTheDocument()
    })
  })
})
