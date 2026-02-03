/**
 * JumpConfirmationDialog Tests
 *
 * Tests for jump confirmation dialog in facilitator-paced conduct view.
 *
 * @module features/injects
 * @see exercise-config/S07-facilitator-paced-conduct-view
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@/test/testUtils'
import { InjectStatus, InjectType, TriggerType } from '../../../../types'
import type { InjectDto } from '../../types'
import { JumpConfirmationDialog } from './JumpConfirmationDialog'

// Test helper to create inject with minimal required fields
const createInject = (overrides: Partial<InjectDto> = {}): InjectDto => ({
  id: overrides.id || 'test-id',
  injectNumber: overrides.injectNumber || 1,
  title: overrides.title || 'Test Inject',
  description: overrides.description || 'Test description',
  scheduledTime: '08:00:00',
  deliveryTime: null,
  scenarioDay: null,
  scenarioTime: null,
  target: 'Test Target',
  source: null,
  deliveryMethod: null,
  deliveryMethodId: null,
  deliveryMethodName: null,
  deliveryMethodOther: null,
  injectType: InjectType.Standard,
  status: InjectStatus.Draft,
  sequence: overrides.sequence || 1,
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
  mselId: 'test-msel-id',
  phaseId: null,
  phaseName: null,
  objectiveIds: [],
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  sourceReference: null,
  priority: null,
  triggerType: TriggerType.Manual,
  responsibleController: null,
  locationName: null,
  locationType: null,
  track: null,
  ...overrides,
})

describe('JumpConfirmationDialog', () => {
  it('does not render when open is false', () => {
    render(
      <JumpConfirmationDialog
        open={false}
        targetInject={null}
        skippedInjects={[]}
        onConfirm={vi.fn()}
        onCancel={vi.fn()}
      />,
    )

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('renders when open is true', () => {
    const target = createInject({ id: 'target', injectNumber: 5, title: 'Target Inject' })

    render(
      <JumpConfirmationDialog
        open={true}
        targetInject={target}
        skippedInjects={[]}
        onConfirm={vi.fn()}
        onCancel={vi.fn()}
      />,
    )

    expect(screen.getByRole('dialog')).toBeInTheDocument()
    expect(screen.getByText(/jump to inject #5/i)).toBeInTheDocument()
  })

  it('lists injects that will be skipped', () => {
    const target = createInject({ id: 'target', injectNumber: 5 })
    const skippedInjects = [
      createInject({ id: 'skip-1', injectNumber: 2, title: 'Inject 2' }),
      createInject({ id: 'skip-2', injectNumber: 3, title: 'Inject 3' }),
      createInject({ id: 'skip-3', injectNumber: 4, title: 'Inject 4' }),
    ]

    render(
      <JumpConfirmationDialog
        open={true}
        targetInject={target}
        skippedInjects={skippedInjects}
        onConfirm={vi.fn()}
        onCancel={vi.fn()}
      />,
    )

    expect(screen.getByText(/this will skip the following/i)).toBeInTheDocument()
    expect(screen.getByText(/#2 - Inject 2/i)).toBeInTheDocument()
    expect(screen.getByText(/#3 - Inject 3/i)).toBeInTheDocument()
    expect(screen.getByText(/#4 - Inject 4/i)).toBeInTheDocument()
  })

  it('shows warning about skipped injects being marked', () => {
    const target = createInject({ id: 'target', injectNumber: 5 })
    const skippedInjects = [
      createInject({ id: 'skip-1', injectNumber: 2 }),
    ]

    render(
      <JumpConfirmationDialog
        open={true}
        targetInject={target}
        skippedInjects={skippedInjects}
        onConfirm={vi.fn()}
        onCancel={vi.fn()}
      />,
    )

    expect(screen.getByText(/marked as "skipped"/i)).toBeInTheDocument()
  })

  it('calls onCancel when cancel button clicked', () => {
    const onCancel = vi.fn()
    const target = createInject({ id: 'target', injectNumber: 5 })

    render(
      <JumpConfirmationDialog
        open={true}
        targetInject={target}
        skippedInjects={[]}
        onConfirm={vi.fn()}
        onCancel={onCancel}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: /cancel/i }))

    expect(onCancel).toHaveBeenCalledTimes(1)
  })

  it('calls onConfirm when Skip & Jump button clicked', () => {
    const onConfirm = vi.fn()
    const target = createInject({ id: 'target', injectNumber: 5 })

    render(
      <JumpConfirmationDialog
        open={true}
        targetInject={target}
        skippedInjects={[]}
        onConfirm={onConfirm}
        onCancel={vi.fn()}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: /skip & jump/i }))

    expect(onConfirm).toHaveBeenCalledTimes(1)
  })

  // Note: Testing backdrop click is handled by MUI Dialog component itself,
  // not part of our component logic

  it('handles null targetInject gracefully', () => {
    render(
      <JumpConfirmationDialog
        open={true}
        targetInject={null}
        skippedInjects={[]}
        onConfirm={vi.fn()}
        onCancel={vi.fn()}
      />,
    )

    // Should still render but with placeholder text
    expect(screen.getByRole('dialog')).toBeInTheDocument()
  })

  it('shows inject count when multiple injects will be skipped', () => {
    const target = createInject({ id: 'target', injectNumber: 10 })
    const skippedInjects = [
      createInject({ id: 'skip-1', injectNumber: 2 }),
      createInject({ id: 'skip-2', injectNumber: 3 }),
      createInject({ id: 'skip-3', injectNumber: 4 }),
      createInject({ id: 'skip-4', injectNumber: 5 }),
      createInject({ id: 'skip-5', injectNumber: 6 }),
    ]

    render(
      <JumpConfirmationDialog
        open={true}
        targetInject={target}
        skippedInjects={skippedInjects}
        onConfirm={vi.fn()}
        onCancel={vi.fn()}
      />,
    )

    expect(screen.getByText(/5 injects/i)).toBeInTheDocument()
  })
})
