/**
 * Tests for SortableInjectRow component
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { SortableInjectRow } from './SortableInjectRow'
import type { InjectDto } from '../../types'
import { InjectStatus } from '../../../../types'

// Mock @dnd-kit/sortable
vi.mock('@dnd-kit/sortable', () => ({
  useSortable: () => ({
    attributes: {},
    listeners: {},
    setNodeRef: vi.fn(),
    transform: null,
    transition: undefined,
    isDragging: false,
  }),
}))

const mockInject: InjectDto = {
  id: 'inject-1',
  mselId: 'msel-1',
  injectNumber: 1,
  title: 'Test Inject',
  description: 'Test description',
  injectType: 'Standard',
  deliveryMethod: 'Email',
  deliveryMethodId: null,
  deliveryMethodName: null,
  deliveryMethodOther: null,
  status: InjectStatus.Pending,
  scheduledTime: '10:00:00',
  deliveryTime: null,
  scenarioDay: 1,
  scenarioTime: '08:00:00',
  target: 'Test Target',
  source: null,
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
  phaseId: null,
  phaseName: null,
  objectiveIds: [],
  sourceReference: null,
  priority: null,
  triggerType: 'Manual',
  responsibleController: null,
  locationName: null,
  locationType: null,
  track: null,
  createdAt: '2024-01-15T08:00:00Z',
  updatedAt: '2024-01-15T08:00:00Z',
}

describe('SortableInjectRow', () => {
  it('renders drag handle and children', () => {
    render(
      <table>
        <tbody>
          <SortableInjectRow inject={mockInject}>
            <td>Content</td>
          </SortableInjectRow>
        </tbody>
      </table>,
    )

    expect(screen.getByRole('button', { name: /drag to reorder/i })).toBeInTheDocument()
    expect(screen.getByText('Content')).toBeInTheDocument()
  })

  it('hides drag handle when disabled', () => {
    render(
      <table>
        <tbody>
          <SortableInjectRow inject={mockInject} disabled>
            <td>Content</td>
          </SortableInjectRow>
        </tbody>
      </table>,
    )

    expect(screen.queryByRole('button', { name: /drag to reorder/i })).not.toBeInTheDocument()
  })

  it('wraps children in TableRow', () => {
    const { container } = render(
      <table>
        <tbody>
          <SortableInjectRow inject={mockInject}>
            <td>Content</td>
          </SortableInjectRow>
        </tbody>
      </table>,
    )

    const row = container.querySelector('tr')
    expect(row).toBeInTheDocument()
  })
})
