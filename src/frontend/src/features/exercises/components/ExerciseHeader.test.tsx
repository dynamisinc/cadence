/**
 * ExerciseHeader Component Tests
 *
 * Tests for the reusable exercise header component displaying:
 * - Exercise name
 * - Exercise type chip
 * - Exercise status chip
 * - Practice mode indicator (optional)
 * - Custom action buttons
 */

import { describe, it, expect } from 'vitest'
import { screen } from '@testing-library/react'
import { render } from '../../../test/test-utils'
import { ExerciseHeader } from './ExerciseHeader'
import { ExerciseType, ExerciseStatus } from '../../../types'
import type { ExerciseDto } from '../types'

// Helper to create mock exercise
const createMockExercise = (overrides: Partial<ExerciseDto> = {}): ExerciseDto => ({
  id: 'exercise-1',
  name: 'Test Exercise',
  description: 'This is a test exercise',
  exerciseType: ExerciseType.TTX,
  status: ExerciseStatus.Draft,
  isPracticeMode: false,
  scheduledDate: '2025-01-15',
  startTime: '09:00:00',
  endTime: '17:00:00',
  timeZoneId: 'America/New_York',
  location: 'Emergency Operations Center',
  organizationId: 'org-1',
  activeMselId: null,
  createdAt: '2025-01-01T00:00:00Z',
  updatedAt: '2025-01-01T00:00:00Z',
  createdBy: 'user-1',
  activatedAt: null,
  activatedBy: null,
  completedAt: null,
  completedBy: null,
  archivedAt: null,
  archivedBy: null,
  hasBeenPublished: false,
  previousStatus: null,
  ...overrides,
})

describe('ExerciseHeader', () => {
  describe('Basic Rendering', () => {
    it('renders exercise name', () => {
      const exercise = createMockExercise({ name: 'Hurricane Response Exercise' })

      render(<ExerciseHeader exercise={exercise} />)

      expect(screen.getByText('Hurricane Response Exercise')).toBeInTheDocument()
    })

    it('renders exercise name as h1 heading', () => {
      const exercise = createMockExercise({ name: 'Test Exercise' })

      render(<ExerciseHeader exercise={exercise} />)

      const heading = screen.getByRole('heading', { level: 1, name: 'Test Exercise' })
      expect(heading).toBeInTheDocument()
    })
  })

  describe('Exercise Type Chip', () => {
    it('renders exercise type chip with TTX type', () => {
      const exercise = createMockExercise({ exerciseType: ExerciseType.TTX })

      render(<ExerciseHeader exercise={exercise} />)

      // ExerciseTypeChip shows abbreviation
      expect(screen.getByText('TTX')).toBeInTheDocument()
    })

    it('renders exercise type chip with FE type', () => {
      const exercise = createMockExercise({ exerciseType: ExerciseType.FE })

      render(<ExerciseHeader exercise={exercise} />)

      expect(screen.getByText('FE')).toBeInTheDocument()
    })

    it('renders exercise type chip with FSE type', () => {
      const exercise = createMockExercise({ exerciseType: ExerciseType.FSE })

      render(<ExerciseHeader exercise={exercise} />)

      expect(screen.getByText('FSE')).toBeInTheDocument()
    })

    it('renders exercise type chip with CAX type', () => {
      const exercise = createMockExercise({ exerciseType: ExerciseType.CAX })

      render(<ExerciseHeader exercise={exercise} />)

      expect(screen.getByText('CAX')).toBeInTheDocument()
    })

    it('renders exercise type chip with Hybrid type', () => {
      const exercise = createMockExercise({ exerciseType: ExerciseType.Hybrid })

      render(<ExerciseHeader exercise={exercise} />)

      expect(screen.getByText('Hybrid')).toBeInTheDocument()
    })
  })

  describe('Exercise Status Chip', () => {
    it('renders exercise status chip with Draft status', () => {
      const exercise = createMockExercise({ status: ExerciseStatus.Draft })

      render(<ExerciseHeader exercise={exercise} />)

      expect(screen.getByText('Draft')).toBeInTheDocument()
    })

    it('renders exercise status chip with Active status', () => {
      const exercise = createMockExercise({ status: ExerciseStatus.Active })

      render(<ExerciseHeader exercise={exercise} />)

      expect(screen.getByText('Active')).toBeInTheDocument()
    })

    it('renders exercise status chip with Completed status', () => {
      const exercise = createMockExercise({ status: ExerciseStatus.Completed })

      render(<ExerciseHeader exercise={exercise} />)

      expect(screen.getByText('Completed')).toBeInTheDocument()
    })

    it('renders exercise status chip with Archived status', () => {
      const exercise = createMockExercise({ status: ExerciseStatus.Archived })

      render(<ExerciseHeader exercise={exercise} />)

      expect(screen.getByText('Archived')).toBeInTheDocument()
    })
  })

  describe('Practice Mode Indicator', () => {
    it('shows practice mode icon when isPracticeMode is true', () => {
      const exercise = createMockExercise({ isPracticeMode: true })

      render(<ExerciseHeader exercise={exercise} />)

      // Check for tooltip element by aria-label
      const tooltip = screen.getByLabelText(/Practice Mode - excluded from production reports/)
      expect(tooltip).toBeInTheDocument()

      // Verify icon is present within the tooltip wrapper
      const icon = tooltip.querySelector('svg[data-icon="screwdriver-wrench"]')
      expect(icon).toBeInTheDocument()
    })

    it('does not show practice mode icon when isPracticeMode is false', () => {
      const exercise = createMockExercise({ isPracticeMode: false })

      render(<ExerciseHeader exercise={exercise} />)

      // Should not have practice mode tooltip
      expect(screen.queryByLabelText(/Practice Mode - excluded from production reports/)).not.toBeInTheDocument()
    })
  })

  describe('Custom Actions', () => {
    it('renders actions when provided', () => {
      const exercise = createMockExercise()
      const actions = (
        <button type="button">Edit</button>
      )

      render(<ExerciseHeader exercise={exercise} actions={actions} />)

      expect(screen.getByRole('button', { name: 'Edit' })).toBeInTheDocument()
    })

    it('renders multiple actions when provided', () => {
      const exercise = createMockExercise()
      const actions = (
        <>
          <button type="button">Edit</button>
          <button type="button">Delete</button>
        </>
      )

      render(<ExerciseHeader exercise={exercise} actions={actions} />)

      expect(screen.getByRole('button', { name: 'Edit' })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Delete' })).toBeInTheDocument()
    })

    it('does not render actions section when no actions provided', () => {
      const exercise = createMockExercise()

      const { container } = render(<ExerciseHeader exercise={exercise} />)

      // Should only have one Stack (the main container), not the actions Stack
      const stacks = container.querySelectorAll('.MuiStack-root')
      // Main Stack + inner Stack for exercise info = 2 Stacks
      expect(stacks.length).toBe(2)
    })

    it('does not render actions section when actions is undefined', () => {
      const exercise = createMockExercise()

      const { container } = render(<ExerciseHeader exercise={exercise} actions={undefined} />)

      const stacks = container.querySelectorAll('.MuiStack-root')
      expect(stacks.length).toBe(2)
    })
  })

  describe('Margin Bottom', () => {
    it('uses default marginBottom of 2 when not specified', () => {
      const exercise = createMockExercise()

      const { container } = render(<ExerciseHeader exercise={exercise} />)

      const mainStack = container.firstChild as HTMLElement
      // MUI converts marginBottom={2} to margin-bottom: 16px (2 * 8px spacing)
      expect(mainStack).toHaveStyle({ marginBottom: '16px' })
    })

    it('applies custom marginBottom when provided', () => {
      const exercise = createMockExercise()

      const { container } = render(<ExerciseHeader exercise={exercise} marginBottom={4} />)

      const mainStack = container.firstChild as HTMLElement
      // MUI converts marginBottom={4} to margin-bottom: 32px (4 * 8px spacing)
      expect(mainStack).toHaveStyle({ marginBottom: '32px' })
    })

    it('applies marginBottom of 0 when specified', () => {
      const exercise = createMockExercise()

      const { container } = render(<ExerciseHeader exercise={exercise} marginBottom={0} />)

      const mainStack = container.firstChild as HTMLElement
      expect(mainStack).toHaveStyle({ marginBottom: '0px' })
    })

    it('applies marginBottom of 6 when specified', () => {
      const exercise = createMockExercise()

      const { container } = render(<ExerciseHeader exercise={exercise} marginBottom={6} />)

      const mainStack = container.firstChild as HTMLElement
      // MUI converts marginBottom={6} to margin-bottom: 48px (6 * 8px spacing)
      expect(mainStack).toHaveStyle({ marginBottom: '48px' })
    })
  })

  describe('Complete Exercise Header', () => {
    it('renders all elements together for active exercise with practice mode and actions', () => {
      const exercise = createMockExercise({
        name: 'Full-Scale Flood Response',
        exerciseType: ExerciseType.FSE,
        status: ExerciseStatus.Active,
        isPracticeMode: true,
      })
      const actions = <button type="button">Stop Exercise</button>

      render(<ExerciseHeader exercise={exercise} actions={actions} />)

      expect(screen.getByRole('heading', { level: 1, name: 'Full-Scale Flood Response' })).toBeInTheDocument()
      expect(screen.getByText('FSE')).toBeInTheDocument()
      expect(screen.getByText('Active')).toBeInTheDocument()
      expect(screen.getByLabelText(/Practice Mode - excluded from production reports/)).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Stop Exercise' })).toBeInTheDocument()
    })

    it('renders correctly for draft exercise without practice mode or actions', () => {
      const exercise = createMockExercise({
        name: 'Tabletop Cybersecurity Exercise',
        exerciseType: ExerciseType.TTX,
        status: ExerciseStatus.Draft,
        isPracticeMode: false,
      })

      render(<ExerciseHeader exercise={exercise} />)

      expect(screen.getByRole('heading', { level: 1, name: 'Tabletop Cybersecurity Exercise' })).toBeInTheDocument()
      expect(screen.getByText('TTX')).toBeInTheDocument()
      expect(screen.getByText('Draft')).toBeInTheDocument()
      expect(screen.queryByText(/Practice Mode/)).not.toBeInTheDocument()
    })
  })
})
