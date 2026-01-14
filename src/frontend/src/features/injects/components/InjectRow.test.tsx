/**
 * InjectRow Component Tests
 *
 * Tests for the inject row display with description preview.
 */

import { describe, it, expect, vi } from 'vitest'
import { screen, fireEvent, within } from '@testing-library/react'
import { render } from '../../../test/test-utils'
import { InjectRow } from './InjectRow'
import { InjectStatus, DeliveryMethod } from '../../../types'
import type { InjectDto } from '../types'

// Wrapper component to provide Table context
const TableWrapper = ({ children }: { children: React.ReactNode }) => (
  <table>
    <tbody>{children}</tbody>
  </table>
)

// Helper to render InjectRow within table context
const renderInjectRow = (props: Parameters<typeof InjectRow>[0]) => {
  return render(
    <TableWrapper>
      <InjectRow {...props} />
    </TableWrapper>,
  )
}

// Helper to create mock inject
const createMockInject = (overrides: Partial<InjectDto> = {}): InjectDto => ({
  id: 'inject-1',
  injectNumber: 1,
  title: 'Test Inject Title',
  description: 'This is a test description for the inject.',
  scheduledTime: '09:00:00',
  scenarioDay: null,
  scenarioTime: null,
  target: 'EOC Director',
  source: 'NWS',
  deliveryMethod: DeliveryMethod.Email,
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
  createdAt: '2025-01-01T00:00:00Z',
  updatedAt: '2025-01-01T00:00:00Z',
  ...overrides,
})

describe('InjectRow', () => {
  describe('Description Preview', () => {
    it('shows description preview when showPreview is true', () => {
      const inject = createMockInject({
        description: 'NWS upgrades to Hurricane Warning. Maria now Category 3.',
      })

      renderInjectRow({ inject, offsetMs: 0, showPreview: true })

      expect(screen.getByText(/NWS upgrades to Hurricane Warning/)).toBeInTheDocument()
    })

    it('truncates long descriptions to ~150 characters', () => {
      const longDescription = 'A'.repeat(200)
      const inject = createMockInject({ description: longDescription })

      renderInjectRow({ inject, offsetMs: 0, showPreview: true })

      // Should show truncated text with ellipsis
      const descriptionText = screen.getByTestId('inject-description-preview')
      expect(descriptionText.textContent?.length).toBeLessThan(180)
      expect(descriptionText).toHaveTextContent('...')
    })

    it('does not show preview when showPreview is false', () => {
      const inject = createMockInject({
        description: 'This description should not appear.',
      })

      renderInjectRow({ inject, offsetMs: 0, showPreview: false })

      expect(screen.queryByText(/This description should not appear/)).not.toBeInTheDocument()
    })

    it('defaults to not showing preview', () => {
      const inject = createMockInject({
        description: 'This description should not appear by default.',
      })

      renderInjectRow({ inject, offsetMs: 0 })

      expect(screen.queryByText(/This description should not appear by default/)).not.toBeInTheDocument()
    })
  })

  describe('Delivery Context Display', () => {
    it('shows target (To) with icon', () => {
      const inject = createMockInject({ target: 'EOC Director' })

      renderInjectRow({ inject, offsetMs: 0, showPreview: true })

      expect(screen.getByText(/To:/)).toBeInTheDocument()
      expect(screen.getByText('EOC Director')).toBeInTheDocument()
    })

    it('shows source (From) with icon when present', () => {
      const inject = createMockInject({ source: 'National Weather Service' })

      renderInjectRow({ inject, offsetMs: 0, showPreview: true })

      expect(screen.getByText(/From:/)).toBeInTheDocument()
      expect(screen.getByText('National Weather Service')).toBeInTheDocument()
    })

    it('does not show source when null', () => {
      const inject = createMockInject({ source: null })

      renderInjectRow({ inject, offsetMs: 0, showPreview: true })

      expect(screen.queryByText(/From:/)).not.toBeInTheDocument()
    })

    it('shows delivery method icon for Email', () => {
      const inject = createMockInject({ deliveryMethod: DeliveryMethod.Email })

      renderInjectRow({ inject, offsetMs: 0, showPreview: true })

      // Should have email icon/label
      expect(screen.getByText('Email')).toBeInTheDocument()
    })

    it('shows delivery method icon for Phone', () => {
      const inject = createMockInject({ deliveryMethod: DeliveryMethod.Phone })

      renderInjectRow({ inject, offsetMs: 0, showPreview: true })

      expect(screen.getByText('Phone')).toBeInTheDocument()
    })

    it('shows delivery method icon for Radio', () => {
      const inject = createMockInject({ deliveryMethod: DeliveryMethod.Radio })

      renderInjectRow({ inject, offsetMs: 0, showPreview: true })

      expect(screen.getByText('Radio')).toBeInTheDocument()
    })

    it('shows delivery method icon for Verbal', () => {
      const inject = createMockInject({ deliveryMethod: DeliveryMethod.Verbal })

      renderInjectRow({ inject, offsetMs: 0, showPreview: true })

      expect(screen.getByText('Verbal')).toBeInTheDocument()
    })

    it('does not show delivery method when null', () => {
      const inject = createMockInject({ deliveryMethod: null })

      renderInjectRow({ inject, offsetMs: 0, showPreview: true })

      // Should not show any delivery method indicators
      expect(screen.queryByText('Email')).not.toBeInTheDocument()
      expect(screen.queryByText('Phone')).not.toBeInTheDocument()
      expect(screen.queryByText('Radio')).not.toBeInTheDocument()
    })
  })

  describe('Basic Rendering', () => {
    it('renders inject number', () => {
      const inject = createMockInject({ injectNumber: 5 })

      renderInjectRow({ inject, offsetMs: 0 })

      expect(screen.getByText('#5')).toBeInTheDocument()
    })

    it('renders inject title', () => {
      const inject = createMockInject({ title: 'Hurricane Warning Upgraded' })

      renderInjectRow({ inject, offsetMs: 0 })

      expect(screen.getByText('Hurricane Warning Upgraded')).toBeInTheDocument()
    })

    it('renders pending status chip', () => {
      const inject = createMockInject({ status: InjectStatus.Pending })

      renderInjectRow({ inject, offsetMs: 0 })

      expect(screen.getByText('Pending')).toBeInTheDocument()
    })

    it('renders fired status chip', () => {
      const inject = createMockInject({
        status: InjectStatus.Fired,
        firedAt: '2025-01-01T10:00:00Z',
      })

      renderInjectRow({ inject, offsetMs: 0 })

      expect(screen.getByText('Fired')).toBeInTheDocument()
    })
  })

  describe('Action Buttons', () => {
    it('shows fire button for pending inject when canControl is true', () => {
      const inject = createMockInject({ status: InjectStatus.Pending })

      renderInjectRow({ inject, offsetMs: 0, canControl: true, showFireButton: true })

      expect(screen.getByRole('button', { name: /Fire inject/i })).toBeInTheDocument()
    })

    it('shows skip button for pending inject when canControl is true', () => {
      const inject = createMockInject({ status: InjectStatus.Pending })

      renderInjectRow({ inject, offsetMs: 0, canControl: true })

      expect(screen.getByRole('button', { name: /Skip inject/i })).toBeInTheDocument()
    })

    it('hides action buttons when canControl is false', () => {
      const inject = createMockInject({ status: InjectStatus.Pending })

      renderInjectRow({ inject, offsetMs: 0, canControl: false })

      expect(screen.queryByRole('button', { name: /Fire inject/i })).not.toBeInTheDocument()
      expect(screen.queryByRole('button', { name: /Skip inject/i })).not.toBeInTheDocument()
    })

    it('calls onFire when fire button clicked', () => {
      const onFire = vi.fn()
      const inject = createMockInject({ id: 'inject-123', status: InjectStatus.Pending })

      renderInjectRow({ inject, offsetMs: 0, canControl: true, showFireButton: true, onFire })

      fireEvent.click(screen.getByRole('button', { name: /Fire inject/i }))

      expect(onFire).toHaveBeenCalledWith('inject-123')
    })
  })
})
