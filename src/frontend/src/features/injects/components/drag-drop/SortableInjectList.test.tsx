/**
 * Tests for SortableInjectList component
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { SortableInjectList } from './SortableInjectList'
import type { InjectDto } from '../../types'
import { InjectStatus, InjectType, TriggerType, DeliveryMethod } from '../../../../types'

// Helper to create mock inject with all required fields
const createMockInject = (overrides: Partial<InjectDto> = {}): InjectDto => ({
  id: 'inject-1',
  mselId: 'msel-1',
  injectNumber: 1,
  title: 'Test Inject',
  description: 'Test Description',
  injectType: InjectType.Standard,
  deliveryMethod: DeliveryMethod.Email,
  deliveryMethodId: null,
  deliveryMethodName: null,
  deliveryMethodOther: null,
  status: InjectStatus.Draft,
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
  createdAt: '2024-01-15T08:00:00Z',
  updatedAt: '2024-01-15T08:00:00Z',
  sourceReference: null,
  priority: null,
  triggerType: TriggerType.Manual,
  responsibleController: null,
  locationName: null,
  locationType: null,
  track: null,
  ...overrides,
})

const mockInjects: InjectDto[] = [
  createMockInject({
    id: 'inject-1',
    injectNumber: 1,
    title: 'Inject 1',
    description: 'Description 1',
    scheduledTime: '10:00:00',
    scenarioDay: 1,
    scenarioTime: '08:00:00',
  }),
  createMockInject({
    id: 'inject-2',
    injectNumber: 2,
    title: 'Inject 2',
    description: 'Description 2',
    deliveryMethod: DeliveryMethod.Phone,
    scheduledTime: '10:15:00',
    scenarioDay: 1,
    scenarioTime: '08:15:00',
    sequence: 2,
  }),
  createMockInject({
    id: 'inject-3',
    injectNumber: 3,
    title: 'Inject 3',
    description: 'Description 3',
    deliveryMethod: DeliveryMethod.Radio,
    scheduledTime: '10:30:00',
    scenarioDay: 1,
    scenarioTime: '08:30:00',
    sequence: 3,
  }),
]

describe('SortableInjectList', () => {
  const mockOnReorder = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders children with inject list', () => {
    render(
      <SortableInjectList injects={mockInjects} onReorder={mockOnReorder}>
        {injects => (
          <div>
            {injects.map(inject => (
              <div key={inject.id}>{inject.title}</div>
            ))}
          </div>
        )}
      </SortableInjectList>,
    )

    expect(screen.getByText('Inject 1')).toBeInTheDocument()
    expect(screen.getByText('Inject 2')).toBeInTheDocument()
    expect(screen.getByText('Inject 3')).toBeInTheDocument()
  })

  it('renders children when disabled', () => {
    render(
      <SortableInjectList injects={mockInjects} onReorder={mockOnReorder} disabled>
        {injects => (
          <div>
            {injects.map(inject => (
              <div key={inject.id}>{inject.title}</div>
            ))}
          </div>
        )}
      </SortableInjectList>,
    )

    expect(screen.getByText('Inject 1')).toBeInTheDocument()
    expect(screen.getByText('Inject 2')).toBeInTheDocument()
    expect(screen.getByText('Inject 3')).toBeInTheDocument()
  })

  it('provides injects in order to children', () => {
    const childRenderFn = vi.fn(() => <div>Content</div>)

    render(<SortableInjectList injects={mockInjects} onReorder={mockOnReorder}>
      {childRenderFn}
    </SortableInjectList>)

    expect(childRenderFn).toHaveBeenCalledWith(mockInjects)
  })

  it('updates when injects prop changes', () => {
    const { rerender } = render(
      <SortableInjectList injects={mockInjects} onReorder={mockOnReorder}>
        {injects => (
          <div>
            {injects.map(inject => (
              <div key={inject.id}>{inject.title}</div>
            ))}
          </div>
        )}
      </SortableInjectList>,
    )

    const updatedInjects = [mockInjects[0]]
    rerender(
      <SortableInjectList injects={updatedInjects} onReorder={mockOnReorder}>
        {injects => (
          <div>
            {injects.map(inject => (
              <div key={inject.id}>{inject.title}</div>
            ))}
          </div>
        )}
      </SortableInjectList>,
    )

    expect(screen.getByText('Inject 1')).toBeInTheDocument()
    expect(screen.queryByText('Inject 2')).not.toBeInTheDocument()
  })
})
