/**
 * UpNextList Tests
 *
 * Tests for up next injects list in facilitator-paced conduct view.
 *
 * @module features/injects
 * @see exercise-config/S07-facilitator-paced-conduct-view
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@/test/testUtils'
import { InjectStatus, InjectType, TriggerType } from '../../../../types'
import type { InjectDto } from '../../types'
import { UpNextList } from './UpNextList'

// Test helper to create inject with minimal required fields
const createInject = (overrides: Partial<InjectDto> = {}): InjectDto => ({
  id: overrides.id || 'test-id',
  injectNumber: overrides.injectNumber || 1,
  title: overrides.title || 'Test Inject',
  description: overrides.description || 'Test description',
  scheduledTime: '08:00:00',
  deliveryTime: null,
  scenarioDay: overrides.scenarioDay || null,
  scenarioTime: overrides.scenarioTime || null,
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

describe('UpNextList', () => {
  it('renders section header', () => {
    render(<UpNextList injects={[]} onJumpTo={vi.fn()} />)

    expect(screen.getByText(/UP NEXT/i)).toBeInTheDocument()
  })

  it('renders list of upcoming injects', () => {
    const injects = [
      createInject({ id: 'inject-1', injectNumber: 4, title: 'Shelter Operations Begin' }),
      createInject({ id: 'inject-2', injectNumber: 5, title: 'Medical Emergency' }),
    ]

    render(<UpNextList injects={injects} onJumpTo={vi.fn()} />)

    expect(screen.getByText(/#4/)).toBeInTheDocument()
    expect(screen.getByText(/Shelter Operations Begin/)).toBeInTheDocument()
    expect(screen.getByText(/#5/)).toBeInTheDocument()
    expect(screen.getByText(/Medical Emergency/)).toBeInTheDocument()
  })

  it('shows truncated description for each inject', () => {
    const longDescription = 'A'.repeat(200)
    const injects = [
      createInject({ id: 'inject-1', description: longDescription }),
    ]

    render(<UpNextList injects={injects} onJumpTo={vi.fn()} />)

    const description = screen.getByText(/A+\.\.\./)
    expect(description.textContent?.length).toBeLessThan(longDescription.length)
  })

  it('displays story time when available', () => {
    const injects = [
      createInject({ id: 'inject-1', scenarioDay: 2, scenarioTime: '06:00:00' }),
    ]

    render(<UpNextList injects={injects} onJumpTo={vi.fn()} />)

    expect(screen.getByText(/D2 06:00/)).toBeInTheDocument()
  })

  it('shows Jump button for each inject', () => {
    const injects = [
      createInject({ id: 'inject-1', injectNumber: 4 }),
      createInject({ id: 'inject-2', injectNumber: 5 }),
    ]

    render(<UpNextList injects={injects} onJumpTo={vi.fn()} />)

    const jumpButtons = screen.getAllByRole('button', { name: /jump/i })
    expect(jumpButtons).toHaveLength(2)
  })

  it('calls onJumpTo with inject when Jump button clicked', () => {
    const onJumpTo = vi.fn()
    const inject = createInject({ id: 'inject-1', injectNumber: 4 })
    const injects = [inject]

    render(<UpNextList injects={injects} onJumpTo={onJumpTo} />)

    fireEvent.click(screen.getByRole('button', { name: /jump/i }))

    expect(onJumpTo).toHaveBeenCalledTimes(1)
    expect(onJumpTo).toHaveBeenCalledWith(inject)
  })

  it('shows empty state when no upcoming injects', () => {
    render(<UpNextList injects={[]} onJumpTo={vi.fn()} />)

    expect(screen.getByText(/no upcoming injects/i)).toBeInTheDocument()
  })

  it('hides Jump buttons when canControl is false', () => {
    const injects = [
      createInject({ id: 'inject-1', injectNumber: 4 }),
    ]

    render(<UpNextList injects={injects} onJumpTo={vi.fn()} canControl={false} />)

    expect(screen.queryByRole('button', { name: /jump/i })).not.toBeInTheDocument()
  })

  it('disables Jump buttons when isSubmitting is true', () => {
    const injects = [
      createInject({ id: 'inject-1', injectNumber: 4 }),
    ]

    render(<UpNextList injects={injects} onJumpTo={vi.fn()} isSubmitting={true} />)

    expect(screen.getByRole('button', { name: /jump/i })).toBeDisabled()
  })
})
