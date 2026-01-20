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
  exerciseId: 'ex-1',
  mselId: 'msel-1',
  injectNumber: 1,
  title: 'Test Inject',
  description: 'Test description',
  injectType: 'Information',
  deliveryMethod: 'Email',
  status: InjectStatus.Pending,
  scheduledTime: '2024-01-15T10:00:00Z',
  offsetMinutes: 0,
  scenarioDay: 1,
  scenarioTime: '08:00',
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
