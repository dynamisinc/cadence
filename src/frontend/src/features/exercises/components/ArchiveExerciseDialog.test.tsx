/**
 * ArchiveExerciseDialog Component Tests
 *
 * Tests for the archive exercise confirmation dialog.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { render } from '../../../test/test-utils'
import { ArchiveExerciseDialog } from './ArchiveExerciseDialog'
import type { ExerciseDto } from '../types'
import { ExerciseType, ExerciseStatus } from '../../../types'

// Helper to create mock exercise
const createMockExercise = (overrides: Partial<ExerciseDto> = {}): ExerciseDto => ({
  id: 'exercise-1',
  name: 'Test Exercise',
  description: 'Test description',
  exerciseType: ExerciseType.TTX,
  status: ExerciseStatus.Completed,
  isPracticeMode: false,
  scheduledDate: '2025-01-15',
  startTime: '09:00:00',
  endTime: '17:00:00',
  timeZoneId: 'America/New_York',
  location: 'Emergency Operations Center',
  organizationId: 'org-1',
  activeMselId: null,
  createdAt: '2025-01-01T00:00:00Z',
  updatedAt: '2025-01-01T00:00:00Z',
  createdBy: 'user-1',
  activatedAt: null,
  activatedBy: null,
  completedAt: null,
  completedBy: null,
  archivedAt: null,
  archivedBy: null,
  hasBeenPublished: true,
  previousStatus: null,
  ...overrides,
})

describe('ArchiveExerciseDialog', () => {
  const defaultProps = {
    open: true,
    exercise: createMockExercise(),
    onClose: vi.fn(),
    onConfirm: vi.fn().mockResolvedValue(undefined),
    isArchiving: false,
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('Basic Rendering', () => {
    it('renders dialog when open is true', () => {
      render(<ArchiveExerciseDialog {...defaultProps} />)

      expect(screen.getByRole('dialog')).toBeInTheDocument()
    })

    it('does not render dialog when open is false', () => {
      render(<ArchiveExerciseDialog {...defaultProps} open={false} />)

      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })

    it('does not render when exercise is null', () => {
      render(<ArchiveExerciseDialog {...defaultProps} exercise={null} />)

      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })

    it('renders dialog title with archive icon', () => {
      render(<ArchiveExerciseDialog {...defaultProps} />)

      // Both title and button contain "Archive Exercise" text
      const archiveTexts = screen.getAllByText('Archive Exercise')
      expect(archiveTexts.length).toBeGreaterThanOrEqual(1)
      expect(screen.getByRole('dialog').querySelector('svg[data-icon="box-archive"]')).toBeInTheDocument()
    })

    it('renders exercise name', () => {
      const exercise = createMockExercise({ name: 'Flood Response Exercise' })
      render(<ArchiveExerciseDialog {...defaultProps} exercise={exercise} />)

      expect(screen.getByText('Flood Response Exercise')).toBeInTheDocument()
    })

    it('renders confirmation message', () => {
      render(<ArchiveExerciseDialog {...defaultProps} />)

      expect(screen.getByText(/Are you sure you want to archive this exercise/)).toBeInTheDocument()
    })

    it('renders helpful description', () => {
      render(<ArchiveExerciseDialog {...defaultProps} />)

      expect(screen.getByText(/hidden from normal views/)).toBeInTheDocument()
      expect(screen.getByText(/restored by an administrator/)).toBeInTheDocument()
    })
  })

  describe('Status Display', () => {
    it('shows Draft status chip', () => {
      const exercise = createMockExercise({ status: ExerciseStatus.Draft })
      render(<ArchiveExerciseDialog {...defaultProps} exercise={exercise} />)

      expect(screen.getByText('Draft')).toBeInTheDocument()
    })

    it('shows Active status chip', () => {
      const exercise = createMockExercise({ status: ExerciseStatus.Active })
      render(<ArchiveExerciseDialog {...defaultProps} exercise={exercise} />)

      expect(screen.getByText('Active')).toBeInTheDocument()
    })

    it('shows Paused status chip', () => {
      const exercise = createMockExercise({ status: ExerciseStatus.Paused })
      render(<ArchiveExerciseDialog {...defaultProps} exercise={exercise} />)

      expect(screen.getByText('Paused')).toBeInTheDocument()
    })

    it('shows Completed status chip', () => {
      const exercise = createMockExercise({ status: ExerciseStatus.Completed })
      render(<ArchiveExerciseDialog {...defaultProps} exercise={exercise} />)

      expect(screen.getByText('Completed')).toBeInTheDocument()
    })
  })

  describe('Button Actions', () => {
    it('renders Cancel button', () => {
      render(<ArchiveExerciseDialog {...defaultProps} />)

      expect(screen.getByRole('button', { name: 'Cancel' })).toBeInTheDocument()
    })

    it('renders Archive Exercise button', () => {
      render(<ArchiveExerciseDialog {...defaultProps} />)

      expect(screen.getByRole('button', { name: /Archive Exercise/ })).toBeInTheDocument()
    })

    it('calls onClose when Cancel is clicked', async () => {
      const user = userEvent.setup()
      const onClose = vi.fn()
      render(<ArchiveExerciseDialog {...defaultProps} onClose={onClose} />)

      await user.click(screen.getByRole('button', { name: 'Cancel' }))

      expect(onClose).toHaveBeenCalledTimes(1)
    })

    it('calls onConfirm when Archive button is clicked', async () => {
      const user = userEvent.setup()
      const onConfirm = vi.fn().mockResolvedValue(undefined)
      render(<ArchiveExerciseDialog {...defaultProps} onConfirm={onConfirm} />)

      await user.click(screen.getByRole('button', { name: /Archive Exercise/ }))

      await waitFor(() => {
        expect(onConfirm).toHaveBeenCalledTimes(1)
      })
    })

    it('calls onClose after onConfirm completes', async () => {
      const user = userEvent.setup()
      const onConfirm = vi.fn().mockResolvedValue(undefined)
      const onClose = vi.fn()
      render(<ArchiveExerciseDialog {...defaultProps} onConfirm={onConfirm} onClose={onClose} />)

      await user.click(screen.getByRole('button', { name: /Archive Exercise/ }))

      await waitFor(() => {
        expect(onClose).toHaveBeenCalledTimes(1)
      })
    })
  })

  describe('Loading State', () => {
    it('shows Archiving... text when isArchiving is true', () => {
      render(<ArchiveExerciseDialog {...defaultProps} isArchiving />)

      expect(screen.getByRole('button', { name: /Archiving/ })).toBeInTheDocument()
    })

    it('disables buttons when isArchiving is true', () => {
      render(<ArchiveExerciseDialog {...defaultProps} isArchiving />)

      expect(screen.getByRole('button', { name: 'Cancel' })).toBeDisabled()
      expect(screen.getByRole('button', { name: /Archiving/ })).toBeDisabled()
    })

    it('enables buttons when isArchiving is false', () => {
      render(<ArchiveExerciseDialog {...defaultProps} isArchiving={false} />)

      expect(screen.getByRole('button', { name: 'Cancel' })).toBeEnabled()
      expect(screen.getByRole('button', { name: /Archive Exercise/ })).toBeEnabled()
    })
  })

  describe('Accessibility', () => {
    it('has proper aria-labelledby', () => {
      render(<ArchiveExerciseDialog {...defaultProps} />)

      const dialog = screen.getByRole('dialog')
      expect(dialog).toHaveAttribute('aria-labelledby', 'archive-dialog-title')
    })

    it('has proper aria-describedby', () => {
      render(<ArchiveExerciseDialog {...defaultProps} />)

      const dialog = screen.getByRole('dialog')
      expect(dialog).toHaveAttribute('aria-describedby', 'archive-dialog-description')
    })
  })
})
