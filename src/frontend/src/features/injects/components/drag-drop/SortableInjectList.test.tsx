/**
 * Tests for SortableInjectList component
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { SortableInjectList } from './SortableInjectList'
import type { InjectDto } from '../../types'
import { InjectStatus } from '../../../../types'

const mockInjects: InjectDto[] = [
  {
    id: 'inject-1',
    exerciseId: 'ex-1',
    mselId: 'msel-1',
    injectNumber: 1,
    title: 'Inject 1',
    description: 'Description 1',
    injectType: 'Information',
    deliveryMethod: 'Email',
    status: InjectStatus.Pending,
    scheduledTime: '2024-01-15T10:00:00Z',
    offsetMinutes: 0,
    scenarioDay: 1,
    scenarioTime: '08:00',
    createdAt: '2024-01-15T08:00:00Z',
    updatedAt: '2024-01-15T08:00:00Z',
  },
  {
    id: 'inject-2',
    exerciseId: 'ex-1',
    mselId: 'msel-1',
    injectNumber: 2,
    title: 'Inject 2',
    description: 'Description 2',
    injectType: 'Action',
    deliveryMethod: 'Phone',
    status: InjectStatus.Pending,
    scheduledTime: '2024-01-15T10:15:00Z',
    offsetMinutes: 15,
    scenarioDay: 1,
    scenarioTime: '08:15',
    createdAt: '2024-01-15T08:00:00Z',
    updatedAt: '2024-01-15T08:00:00Z',
  },
  {
    id: 'inject-3',
    exerciseId: 'ex-1',
    mselId: 'msel-1',
    injectNumber: 3,
    title: 'Inject 3',
    description: 'Description 3',
    injectType: 'Decision',
    deliveryMethod: 'Radio',
    status: InjectStatus.Pending,
    scheduledTime: '2024-01-15T10:30:00Z',
    offsetMinutes: 30,
    scenarioDay: 1,
    scenarioTime: '08:30',
    createdAt: '2024-01-15T08:00:00Z',
    updatedAt: '2024-01-15T08:00:00Z',
  },
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
