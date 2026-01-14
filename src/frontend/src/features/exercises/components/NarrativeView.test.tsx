/**
 * NarrativeView Component Tests
 *
 * Tests for the Observer-friendly narrative view during exercise conduct.
 */

import { describe, it, expect } from 'vitest'
import { screen } from '@testing-library/react'
import { render } from '../../../test/test-utils'
import { NarrativeView } from './NarrativeView'
import { InjectStatus } from '../../../types'
import type { InjectDto } from '../../injects/types'
import type { ObservationDto } from '../../observations/types'
import type { ExerciseDto } from '../types'

// Helper to create mock inject
const createMockInject = (overrides: Partial<InjectDto> = {}): InjectDto => ({
  id: 'inject-1',
  injectNumber: 1,
  title: 'Test Inject',
  description: 'Test description for the inject.',
  scheduledTime: '09:00:00',
  scenarioDay: 1,
  scenarioTime: '08:00:00',
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
  phaseId: 'phase-1',
  phaseName: 'Initial Response',
  createdAt: '2025-01-01T00:00:00Z',
  updatedAt: '2025-01-01T00:00:00Z',
  ...overrides,
})

const createMockObservation = (overrides: Partial<ObservationDto> = {}): ObservationDto => ({
  id: 'obs-1',
  exerciseId: 'ex-1',
  injectId: null,
  objectiveId: null,
  content: 'Test observation',
  rating: 'Satisfactory',
  recommendation: null,
  observedAt: '2025-01-01T10:00:00Z',
  location: null,
  createdAt: '2025-01-01T10:00:00Z',
  updatedAt: '2025-01-01T10:00:00Z',
  createdBy: 'user-1',
  createdByName: 'Test User',
  injectTitle: null,
  ...overrides,
})

const createMockExercise = (overrides: Partial<ExerciseDto> = {}): ExerciseDto => ({
  id: 'ex-1',
  name: 'Hurricane Response TTX',
  description: 'Tabletop exercise for hurricane response',
  exerciseType: 'TTX',
  status: 'Active',
  plannedStartDate: '2025-01-15',
  plannedEndDate: '2025-01-15',
  actualStartDate: null,
  actualEndDate: null,
  objectives: null,
  scope: 'Metro County area',
  location: 'EOC Conference Room',
  organizationId: 'org-1',
  organizationName: 'Metro County EM',
  plannedStartTime: '09:00:00',
  createdAt: '2025-01-01T00:00:00Z',
  updatedAt: '2025-01-01T00:00:00Z',
  ...overrides,
})

describe('NarrativeView', () => {
  describe('Basic Rendering', () => {
    it('renders exercise title', () => {
      render(
        <NarrativeView
          exercise={createMockExercise({ name: 'Hurricane Maria Response TTX' })}
          injects={[]}
          observations={[]}
          displayTime="00:15:20"
          elapsedTimeMs={920000}
        />,
      )

      expect(screen.getByText('Hurricane Maria Response TTX')).toBeInTheDocument()
    })

    it('renders clock display', () => {
      render(
        <NarrativeView
          exercise={createMockExercise()}
          injects={[]}
          observations={[]}
          displayTime="00:15:20"
          elapsedTimeMs={920000}
        />,
      )

      expect(screen.getByText('00:15:20')).toBeInTheDocument()
    })

    it('renders current phase name', () => {
      const injects = [
        createMockInject({
          status: InjectStatus.Fired,
          firedAt: '2025-01-01T10:00:00Z',
          phaseName: 'Evacuation Phase',
        }),
      ]

      render(
        <NarrativeView
          exercise={createMockExercise()}
          injects={injects}
          observations={[]}
          displayTime="00:15:20"
          elapsedTimeMs={920000}
        />,
      )

      expect(screen.getByText(/Evacuation Phase/)).toBeInTheDocument()
    })
  })

  describe('Story So Far Section', () => {
    it('shows "The Story So Far" heading', () => {
      render(
        <NarrativeView
          exercise={createMockExercise()}
          injects={[]}
          observations={[]}
          displayTime="00:15:20"
          elapsedTimeMs={920000}
        />,
      )

      expect(screen.getByText('The Story So Far')).toBeInTheDocument()
    })

    it('shows fired inject descriptions in narrative', () => {
      const injects = [
        createMockInject({
          id: '1',
          status: InjectStatus.Fired,
          firedAt: '2025-01-01T09:00:00Z',
          description: 'Hurricane Maria strengthened to Category 2.',
        }),
      ]

      render(
        <NarrativeView
          exercise={createMockExercise()}
          injects={injects}
          observations={[]}
          displayTime="00:15:20"
          elapsedTimeMs={920000}
        />,
      )

      expect(screen.getByText(/Hurricane Maria strengthened to Category 2/)).toBeInTheDocument()
    })

    it('shows empty state when no injects fired', () => {
      render(
        <NarrativeView
          exercise={createMockExercise()}
          injects={[createMockInject({ status: InjectStatus.Pending })]}
          observations={[]}
          displayTime="00:15:20"
          elapsedTimeMs={920000}
        />,
      )

      expect(screen.getByText(/exercise has just begun/i)).toBeInTheDocument()
    })
  })

  describe("What's Happening Now Section", () => {
    it('shows "What\'s Happening Now" heading', () => {
      render(
        <NarrativeView
          exercise={createMockExercise()}
          injects={[]}
          observations={[]}
          displayTime="00:15:20"
          elapsedTimeMs={920000}
        />,
      )

      expect(screen.getByText("What's Happening Now")).toBeInTheDocument()
    })

    it('shows next pending inject narrative', () => {
      const injects = [
        createMockInject({
          id: '1',
          status: InjectStatus.Pending,
          sequence: 1,
          target: 'EOC Director',
          source: 'NWS',
          description: 'Hurricane warning upgraded.',
        }),
      ]

      render(
        <NarrativeView
          exercise={createMockExercise()}
          injects={injects}
          observations={[]}
          displayTime="00:15:20"
          elapsedTimeMs={920000}
        />,
      )

      expect(screen.getByText(/Hurricane warning upgraded/)).toBeInTheDocument()
    })
  })

  describe('Coming Up Section', () => {
    it('shows "Coming Up" heading', () => {
      render(
        <NarrativeView
          exercise={createMockExercise()}
          injects={[]}
          observations={[]}
          displayTime="00:15:20"
          elapsedTimeMs={920000}
        />,
      )

      expect(screen.getByText('Coming Up')).toBeInTheDocument()
    })

    it('shows upcoming inject previews', () => {
      const injects = [
        createMockInject({
          id: '1',
          status: InjectStatus.Pending,
          sequence: 1,
          title: 'Media Inquiry',
          target: 'PIO',
        }),
        createMockInject({
          id: '2',
          status: InjectStatus.Pending,
          sequence: 2,
          title: 'Shelter Capacity',
          target: 'Shelter Manager',
        }),
      ]

      render(
        <NarrativeView
          exercise={createMockExercise()}
          injects={injects}
          observations={[]}
          displayTime="00:15:20"
          elapsedTimeMs={920000}
        />,
      )

      expect(screen.getByText(/Media Inquiry/)).toBeInTheDocument()
      expect(screen.getByText(/Shelter Capacity/)).toBeInTheDocument()
    })
  })

  describe('Observations Section', () => {
    it('shows observations section', () => {
      render(
        <NarrativeView
          exercise={createMockExercise()}
          injects={[]}
          observations={[]}
          displayTime="00:15:20"
          elapsedTimeMs={920000}
        />,
      )

      expect(screen.getByText('Evaluator Observations')).toBeInTheDocument()
    })

    it('displays observation content', () => {
      const observations = [
        createMockObservation({
          content: 'EOC activation was slower than expected.',
          rating: 'Marginal',
        }),
      ]

      render(
        <NarrativeView
          exercise={createMockExercise()}
          injects={[]}
          observations={observations}
          displayTime="00:15:20"
          elapsedTimeMs={920000}
        />,
      )

      expect(screen.getByText(/EOC activation was slower than expected/)).toBeInTheDocument()
    })
  })

  describe('Read-Only Nature', () => {
    it('does not render fire buttons', () => {
      const injects = [createMockInject({ status: InjectStatus.Pending })]

      render(
        <NarrativeView
          exercise={createMockExercise()}
          injects={injects}
          observations={[]}
          displayTime="00:15:20"
          elapsedTimeMs={920000}
        />,
      )

      expect(screen.queryByRole('button', { name: /fire/i })).not.toBeInTheDocument()
    })

    it('does not render skip buttons', () => {
      const injects = [createMockInject({ status: InjectStatus.Pending })]

      render(
        <NarrativeView
          exercise={createMockExercise()}
          injects={injects}
          observations={[]}
          displayTime="00:15:20"
          elapsedTimeMs={920000}
        />,
      )

      expect(screen.queryByRole('button', { name: /skip/i })).not.toBeInTheDocument()
    })

    it('does not render clock control buttons', () => {
      render(
        <NarrativeView
          exercise={createMockExercise()}
          injects={[]}
          observations={[]}
          displayTime="00:15:20"
          elapsedTimeMs={920000}
        />,
      )

      expect(screen.queryByRole('button', { name: /start/i })).not.toBeInTheDocument()
      expect(screen.queryByRole('button', { name: /pause/i })).not.toBeInTheDocument()
      expect(screen.queryByRole('button', { name: /stop/i })).not.toBeInTheDocument()
    })
  })
})
