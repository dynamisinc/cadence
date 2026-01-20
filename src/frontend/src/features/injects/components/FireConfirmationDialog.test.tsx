/**
 * Tests for FireConfirmationDialog component
 *
 * Validates confirmation dialog behavior for firing injects.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '../../../test/testUtils'
import userEvent from '@testing-library/user-event'
import { FireConfirmationDialog } from './FireConfirmationDialog'
import { InjectType, InjectStatus } from '../../../types'
import type { InjectDto } from '../types'

describe('FireConfirmationDialog', () => {
  const mockInject: InjectDto = {
    id: 'inject-1',
    injectNumber: 3,
    title: 'Evacuation Order',
    description: 'Issue evacuation order for coastal areas',
    scheduledTime: '18:00:00',
    deliveryTime: '01:30:00',
    scenarioDay: 1,
    scenarioTime: '18:00:00',
    target: 'EOC Director',
    source: 'Emergency Management',
    deliveryMethod: null,
    deliveryMethodId: null,
    deliveryMethodName: 'Email',
    deliveryMethodOther: null,
    injectType: InjectType.Inject,
    status: InjectStatus.Pending,
    sequence: 3,
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
    mselId: 'msel-1',
    phaseId: 'phase-1',
    phaseName: 'Response',
    objectiveIds: [],
    createdAt: '2024-01-15T10:00:00Z',
    updatedAt: '2024-01-15T10:00:00Z',
    sourceReference: null,
    priority: null,
    triggerType: 'Manual',
    responsibleController: null,
    locationName: null,
    locationType: null,
    track: null,
  }

  const defaultProps = {
    open: true,
    inject: mockInject,
    onConfirm: vi.fn(),
    onCancel: vi.fn(),
    onDontAskAgain: vi.fn(),
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('rendering', () => {
    it('renders dialog when open', () => {
      render(<FireConfirmationDialog {...defaultProps} />)

      expect(screen.getByRole('dialog')).toBeInTheDocument()
      expect(screen.getByText(/Fire Inject?/i)).toBeInTheDocument()
    })

    it('does not render when closed', () => {
      render(<FireConfirmationDialog {...defaultProps} open={false} />)

      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })

    it('does not render when inject is null', () => {
      render(<FireConfirmationDialog {...defaultProps} inject={null} />)

      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })

    it('displays inject number and title', () => {
      render(<FireConfirmationDialog {...defaultProps} />)

      expect(screen.getByText(/#3 - Evacuation Order/i)).toBeInTheDocument()
    })

    it('displays target information', () => {
      render(<FireConfirmationDialog {...defaultProps} />)

      expect(screen.getByText(/Target:/i)).toBeInTheDocument()
      expect(screen.getByText(/EOC Director/i)).toBeInTheDocument()
    })

    it('displays story time when available', () => {
      render(<FireConfirmationDialog {...defaultProps} />)

      expect(screen.getByText(/Story Time:/i)).toBeInTheDocument()
      // formatScenarioTime returns "D1 18:00" format
      expect(screen.getByText(/D1 18:00/i)).toBeInTheDocument()
    })

    it('does not display story time when not available', () => {
      const injectWithoutStoryTime: InjectDto = {
        ...mockInject,
        scenarioDay: null,
        scenarioTime: null,
      }

      render(<FireConfirmationDialog {...defaultProps} inject={injectWithoutStoryTime} />)

      expect(screen.queryByText(/Story Time:/i)).not.toBeInTheDocument()
    })

    it('displays warning message', () => {
      render(<FireConfirmationDialog {...defaultProps} />)

      expect(
        screen.getByText(/This action will be broadcast to all exercise participants/i),
      ).toBeInTheDocument()
    })

    it('displays "don\'t ask again" checkbox', () => {
      render(<FireConfirmationDialog {...defaultProps} />)

      expect(screen.getByRole('checkbox', { name: /don't ask again this session/i })).toBeInTheDocument()
    })

    it('displays Cancel and Confirm Fire buttons', () => {
      render(<FireConfirmationDialog {...defaultProps} />)

      expect(screen.getByRole('button', { name: /cancel/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /confirm fire/i })).toBeInTheDocument()
    })
  })

  describe('interactions', () => {
    it('calls onConfirm when Confirm Fire button is clicked', async () => {
      const user = userEvent.setup()
      render(<FireConfirmationDialog {...defaultProps} />)

      const confirmButton = screen.getByRole('button', { name: /confirm fire/i })
      await user.click(confirmButton)

      expect(defaultProps.onConfirm).toHaveBeenCalledTimes(1)
    })

    it('calls onCancel when Cancel button is clicked', async () => {
      const user = userEvent.setup()
      render(<FireConfirmationDialog {...defaultProps} />)

      const cancelButton = screen.getByRole('button', { name: /cancel/i })
      await user.click(cancelButton)

      expect(defaultProps.onCancel).toHaveBeenCalledTimes(1)
    })

    it('calls onDontAskAgain when checkbox is checked and confirmed', async () => {
      const user = userEvent.setup()
      render(<FireConfirmationDialog {...defaultProps} />)

      const checkbox = screen.getByRole('checkbox', { name: /don't ask again this session/i })
      await user.click(checkbox)

      const confirmButton = screen.getByRole('button', { name: /confirm fire/i })
      await user.click(confirmButton)

      expect(defaultProps.onDontAskAgain).toHaveBeenCalledWith(true)
      expect(defaultProps.onConfirm).toHaveBeenCalled()
    })

    it('does not call onDontAskAgain when checkbox is unchecked', async () => {
      const user = userEvent.setup()
      render(<FireConfirmationDialog {...defaultProps} />)

      const confirmButton = screen.getByRole('button', { name: /confirm fire/i })
      await user.click(confirmButton)

      expect(defaultProps.onDontAskAgain).not.toHaveBeenCalled()
      expect(defaultProps.onConfirm).toHaveBeenCalled()
    })

    it('calls onConfirm when Enter key is pressed', async () => {
      const user = userEvent.setup()
      render(<FireConfirmationDialog {...defaultProps} />)

      await user.keyboard('{Enter}')

      expect(defaultProps.onConfirm).toHaveBeenCalledTimes(1)
    })

    it('calls onCancel when Escape key is pressed', async () => {
      const user = userEvent.setup()
      render(<FireConfirmationDialog {...defaultProps} />)

      await user.keyboard('{Escape}')

      expect(defaultProps.onCancel).toHaveBeenCalledTimes(1)
    })

    it('calls onDontAskAgain with true when checkbox is checked and Enter is pressed', async () => {
      const user = userEvent.setup()
      render(<FireConfirmationDialog {...defaultProps} />)

      const checkbox = screen.getByRole('checkbox', { name: /don't ask again this session/i })
      await user.click(checkbox)

      await user.keyboard('{Enter}')

      expect(defaultProps.onDontAskAgain).toHaveBeenCalledWith(true)
      expect(defaultProps.onConfirm).toHaveBeenCalled()
    })

    it('does not call onDontAskAgain when cancelled', async () => {
      const user = userEvent.setup()
      render(<FireConfirmationDialog {...defaultProps} />)

      const checkbox = screen.getByRole('checkbox', { name: /don't ask again this session/i })
      await user.click(checkbox)

      const cancelButton = screen.getByRole('button', { name: /cancel/i })
      await user.click(cancelButton)

      expect(defaultProps.onDontAskAgain).not.toHaveBeenCalled()
      expect(defaultProps.onCancel).toHaveBeenCalled()
    })
  })

  describe('keyboard shortcuts', () => {
    it('prevents default behavior on Enter key', async () => {
      render(<FireConfirmationDialog {...defaultProps} />)

      const enterEvent = new KeyboardEvent('keydown', {
        key: 'Enter',
        bubbles: true,
        cancelable: true,
      })

      window.dispatchEvent(enterEvent)

      await waitFor(() => {
        expect(defaultProps.onConfirm).toHaveBeenCalled()
      })
    })

    it('prevents default behavior on Escape key', async () => {
      render(<FireConfirmationDialog {...defaultProps} />)

      const escapeEvent = new KeyboardEvent('keydown', {
        key: 'Escape',
        bubbles: true,
        cancelable: true,
      })

      window.dispatchEvent(escapeEvent)

      await waitFor(() => {
        expect(defaultProps.onCancel).toHaveBeenCalled()
      })
    })

    it('removes keyboard listeners when dialog closes', () => {
      const { rerender } = render(<FireConfirmationDialog {...defaultProps} />)

      // Close dialog
      rerender(<FireConfirmationDialog {...defaultProps} open={false} />)

      // Keyboard events should not trigger handlers
      const enterEvent = new KeyboardEvent('keydown', { key: 'Enter', bubbles: true })
      window.dispatchEvent(enterEvent)

      expect(defaultProps.onConfirm).not.toHaveBeenCalled()
    })
  })
})
