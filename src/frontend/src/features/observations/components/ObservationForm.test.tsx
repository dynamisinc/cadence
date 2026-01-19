/**
 * ObservationForm Component Tests
 *
 * Tests for the observation form with inject selector dropdown.
 */

import { describe, it, expect, vi } from 'vitest'
import { screen, fireEvent, within } from '@testing-library/react'
import { render } from '../../../test/test-utils'
import { ObservationForm } from './ObservationForm'
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
  deliveryMethodId: null,
  deliveryMethodName: null,
  deliveryMethodOther: null,
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
  sourceReference: null,
  priority: null,
  triggerType: 'Manual',
  responsibleController: null,
  locationName: null,
  locationType: null,
  track: null,
  ...overrides,
})

describe('ObservationForm', () => {
  describe('Inject Dropdown Sorting', () => {
    it('sorts recently fired injects first', async () => {
      const injects = [
        createMockInject({
          id: 'pending-1',
          injectNumber: 1,
          title: 'Pending Inject',
          status: InjectStatus.Pending,
          firedAt: null,
        }),
        createMockInject({
          id: 'fired-early',
          injectNumber: 2,
          title: 'Early Fired',
          status: InjectStatus.Fired,
          firedAt: '2025-01-01T09:00:00Z',
        }),
        createMockInject({
          id: 'fired-late',
          injectNumber: 3,
          title: 'Recent Fired',
          status: InjectStatus.Fired,
          firedAt: '2025-01-01T10:00:00Z', // Most recent
        }),
        createMockInject({
          id: 'pending-2',
          injectNumber: 4,
          title: 'Another Pending',
          status: InjectStatus.Pending,
          firedAt: null,
        }),
      ]

      render(
        <ObservationForm
          injects={injects}
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      // Open the dropdown
      const select = screen.getByLabelText(/Related Inject/i)
      fireEvent.mouseDown(select)

      // Get all menu items (except "None")
      const listbox = await screen.findByRole('listbox')
      const options = within(listbox).getAllByRole('option')

      // First option should be "None", followed by recently fired injects, then pending
      expect(options[0]).toHaveTextContent('None')
      // Most recently fired first
      expect(options[1]).toHaveTextContent('#3')
      expect(options[1]).toHaveTextContent('(Fired)')
      // Earlier fired second
      expect(options[2]).toHaveTextContent('#2')
      expect(options[2]).toHaveTextContent('(Fired)')
      // Pending injects by sequence
      expect(options[3]).toHaveTextContent('#1')
      expect(options[4]).toHaveTextContent('#4')
    })

    it('shows inject status in dropdown', async () => {
      const injects = [
        createMockInject({
          id: 'fired-1',
          injectNumber: 1,
          title: 'Fired Inject',
          status: InjectStatus.Fired,
          firedAt: '2025-01-01T10:00:00Z',
        }),
        createMockInject({
          id: 'pending-1',
          injectNumber: 2,
          title: 'Pending Inject',
          status: InjectStatus.Pending,
        }),
        createMockInject({
          id: 'skipped-1',
          injectNumber: 3,
          title: 'Skipped Inject',
          status: InjectStatus.Skipped,
          skippedAt: '2025-01-01T10:05:00Z',
        }),
      ]

      render(
        <ObservationForm
          injects={injects}
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      // Open the dropdown
      const select = screen.getByLabelText(/Related Inject/i)
      fireEvent.mouseDown(select)

      const listbox = await screen.findByRole('listbox')

      // Check status indicators are shown
      expect(within(listbox).getByText(/\(Fired\)/)).toBeInTheDocument()
      expect(within(listbox).getByText(/\(Skipped\)/)).toBeInTheDocument()
    })

    it('shows title instead of truncated description', async () => {
      const injects = [
        createMockInject({
          id: 'inject-1',
          injectNumber: 1,
          title: 'Media Inquiry',
          description: 'This is a very long description that would normally be truncated',
          status: InjectStatus.Fired,
          firedAt: '2025-01-01T10:00:00Z',
        }),
      ]

      render(
        <ObservationForm
          injects={injects}
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      // Open the dropdown
      const select = screen.getByLabelText(/Related Inject/i)
      fireEvent.mouseDown(select)

      const listbox = await screen.findByRole('listbox')

      // Should show title, not description
      expect(within(listbox).getByText(/Media Inquiry/)).toBeInTheDocument()
    })
  })

  describe('Basic Form Behavior', () => {
    it('renders observation content field', () => {
      render(
        <ObservationForm
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      expect(screen.getByLabelText(/Observation/i)).toBeInTheDocument()
    })

    it('renders rating dropdown', () => {
      render(
        <ObservationForm
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      expect(screen.getByLabelText(/Rating/i)).toBeInTheDocument()
    })

    it('disables save button when content is empty', () => {
      render(
        <ObservationForm
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      expect(screen.getByRole('button', { name: /Save/i })).toBeDisabled()
    })

    it('enables save button when content has text', () => {
      render(
        <ObservationForm
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      const textarea = screen.getByLabelText(/Observation/i)
      fireEvent.change(textarea, { target: { value: 'Test observation' } })

      expect(screen.getByRole('button', { name: /Save/i })).not.toBeDisabled()
    })

    it('calls onCancel when cancel button clicked', () => {
      const onCancel = vi.fn()

      render(
        <ObservationForm
          onSubmit={vi.fn()}
          onCancel={onCancel}
        />,
      )

      fireEvent.click(screen.getByRole('button', { name: /Cancel/i }))

      expect(onCancel).toHaveBeenCalled()
    })

    it('does not show inject dropdown when no injects provided', () => {
      render(
        <ObservationForm
          injects={[]}
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      expect(screen.queryByLabelText(/Related Inject/i)).not.toBeInTheDocument()
    })
  })
})
