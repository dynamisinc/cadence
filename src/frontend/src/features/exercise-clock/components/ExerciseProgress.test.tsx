/**
 * ExerciseProgress Component Tests
 *
 * Tests for the exercise progress display showing current phase and inject completion.
 */

import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { ExerciseProgress } from './ExerciseProgress'
import { InjectStatus } from '../../../types'
import type { InjectDto } from '../../injects/types'

// Helper to create mock inject
const createMockInject = (
  overrides: Partial<InjectDto> = {},
): InjectDto => ({
  id: 'inject-1',
  injectNumber: 1,
  title: 'Test Inject',
  description: 'Test description',
  scheduledTime: '09:00:00',
  scenarioDay: null,
  scenarioTime: null,
  target: 'EOC Director',
  source: null,
  deliveryMethod: null,
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
  objectiveIds: [],
  createdAt: '2025-01-01T00:00:00Z',
  updatedAt: '2025-01-01T00:00:00Z',
  ...overrides,
})

describe('ExerciseProgress', () => {
  describe('Progress Calculation', () => {
    it('shows 0% progress when no injects are fired or skipped', () => {
      const injects = [
        createMockInject({ id: '1', status: InjectStatus.Pending }),
        createMockInject({ id: '2', status: InjectStatus.Pending }),
        createMockInject({ id: '3', status: InjectStatus.Pending }),
      ]

      render(<ExerciseProgress injects={injects} />)

      expect(screen.getByText('0 of 3 injects fired')).toBeInTheDocument()
    })

    it('counts fired injects toward progress', () => {
      const injects = [
        createMockInject({ id: '1', status: InjectStatus.Fired, firedAt: '2025-01-01T10:00:00Z' }),
        createMockInject({ id: '2', status: InjectStatus.Fired, firedAt: '2025-01-01T10:05:00Z' }),
        createMockInject({ id: '3', status: InjectStatus.Pending }),
      ]

      render(<ExerciseProgress injects={injects} />)

      expect(screen.getByText('2 of 3 injects fired')).toBeInTheDocument()
    })

    it('counts skipped injects toward progress', () => {
      const injects = [
        createMockInject({ id: '1', status: InjectStatus.Fired, firedAt: '2025-01-01T10:00:00Z' }),
        createMockInject({ id: '2', status: InjectStatus.Skipped, skippedAt: '2025-01-01T10:05:00Z' }),
        createMockInject({ id: '3', status: InjectStatus.Pending }),
      ]

      render(<ExerciseProgress injects={injects} />)

      expect(screen.getByText('2 of 3 injects fired')).toBeInTheDocument()
    })

    it('shows 100% progress when all injects are fired or skipped', () => {
      const injects = [
        createMockInject({ id: '1', status: InjectStatus.Fired, firedAt: '2025-01-01T10:00:00Z' }),
        createMockInject({ id: '2', status: InjectStatus.Skipped, skippedAt: '2025-01-01T10:05:00Z' }),
        createMockInject({ id: '3', status: InjectStatus.Fired, firedAt: '2025-01-01T10:10:00Z' }),
      ]

      render(<ExerciseProgress injects={injects} />)

      expect(screen.getByText('3 of 3 injects fired')).toBeInTheDocument()
    })

    it('handles empty inject list gracefully', () => {
      render(<ExerciseProgress injects={[]} />)

      expect(screen.getByText('0 of 0 injects fired')).toBeInTheDocument()
    })
  })

  describe('Phase Detection', () => {
    it('shows current phase name from most recently fired inject', () => {
      const injects = [
        createMockInject({
          id: '1',
          status: InjectStatus.Fired,
          firedAt: '2025-01-01T10:00:00Z',
          phaseId: 'phase-1',
          phaseName: 'Initial Response',
        }),
        createMockInject({
          id: '2',
          status: InjectStatus.Fired,
          firedAt: '2025-01-01T10:30:00Z',
          phaseId: 'phase-2',
          phaseName: 'Evacuation',
        }),
        createMockInject({
          id: '3',
          status: InjectStatus.Pending,
          phaseId: 'phase-2',
          phaseName: 'Evacuation',
        }),
      ]

      render(<ExerciseProgress injects={injects} />)

      expect(screen.getByText('Phase: Evacuation')).toBeInTheDocument()
    })

    it('shows phase name from first pending inject when no injects fired', () => {
      const injects = [
        createMockInject({
          id: '1',
          status: InjectStatus.Pending,
          sequence: 1,
          phaseId: 'phase-1',
          phaseName: 'Initial Response',
        }),
        createMockInject({
          id: '2',
          status: InjectStatus.Pending,
          sequence: 2,
          phaseId: 'phase-1',
          phaseName: 'Initial Response',
        }),
      ]

      render(<ExerciseProgress injects={injects} />)

      expect(screen.getByText('Phase: Initial Response')).toBeInTheDocument()
    })

    it('shows "No phase assigned" when injects have no phase', () => {
      const injects = [
        createMockInject({ id: '1', status: InjectStatus.Pending, phaseId: null, phaseName: null }),
        createMockInject({ id: '2', status: InjectStatus.Pending, phaseId: null, phaseName: null }),
      ]

      render(<ExerciseProgress injects={injects} />)

      expect(screen.getByText('No phase assigned')).toBeInTheDocument()
    })

    it('handles mixed phase assignment (some with, some without)', () => {
      const injects = [
        createMockInject({
          id: '1',
          status: InjectStatus.Fired,
          firedAt: '2025-01-01T10:00:00Z',
          phaseId: 'phase-1',
          phaseName: 'Initial Response',
        }),
        createMockInject({ id: '2', status: InjectStatus.Pending, phaseId: null, phaseName: null }),
      ]

      render(<ExerciseProgress injects={injects} />)

      // Should show the phase from the fired inject
      expect(screen.getByText('Phase: Initial Response')).toBeInTheDocument()
    })
  })

  describe('Progress Bar', () => {
    it('renders progress bar with correct value', () => {
      const injects = [
        createMockInject({ id: '1', status: InjectStatus.Fired, firedAt: '2025-01-01T10:00:00Z' }),
        createMockInject({ id: '2', status: InjectStatus.Pending }),
        createMockInject({ id: '3', status: InjectStatus.Pending }),
        createMockInject({ id: '4', status: InjectStatus.Pending }),
      ]

      const { container } = render(<ExerciseProgress injects={injects} />)

      // MUI LinearProgress uses aria-valuenow
      const progressBar = container.querySelector('[role="progressbar"]')
      expect(progressBar).toBeInTheDocument()
      expect(progressBar).toHaveAttribute('aria-valuenow', '25')
    })

    it('renders 0% progress bar when no injects completed', () => {
      const injects = [
        createMockInject({ id: '1', status: InjectStatus.Pending }),
        createMockInject({ id: '2', status: InjectStatus.Pending }),
      ]

      const { container } = render(<ExerciseProgress injects={injects} />)

      const progressBar = container.querySelector('[role="progressbar"]')
      expect(progressBar).toHaveAttribute('aria-valuenow', '0')
    })

    it('renders 100% progress bar when all injects completed', () => {
      const injects = [
        createMockInject({ id: '1', status: InjectStatus.Fired, firedAt: '2025-01-01T10:00:00Z' }),
        createMockInject({ id: '2', status: InjectStatus.Skipped, skippedAt: '2025-01-01T10:05:00Z' }),
      ]

      const { container } = render(<ExerciseProgress injects={injects} />)

      const progressBar = container.querySelector('[role="progressbar"]')
      expect(progressBar).toHaveAttribute('aria-valuenow', '100')
    })
  })
})
