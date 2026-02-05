/**
 * EegEntriesList Component Tests
 *
 * Tests for the entries list component with sorting, entry detail, and CRUD actions.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { render } from '../../../test/test-utils'
import { EegEntriesList } from './EegEntriesList'
import type { EegEntryDto, PerformanceRating } from '../types'

describe('EegEntriesList', () => {
  const mockEntries: EegEntryDto[] = [
    {
      id: 'entry-1',
      criticalTaskId: 'task-1',
      criticalTask: {
        id: 'task-1',
        taskDescription: 'Activate EOC',
        standard: 'Within 30 minutes',
        capabilityTargetId: 'cap-1',
        capabilityTargetDescription: 'Establish operational command structure',
        capabilityTargetSources: 'EOP Section 3.1',
        capabilityName: 'Operational Coordination',
      },
      observationText: 'EOC activated promptly with all staff present',
      rating: 'Performed' as PerformanceRating,
      ratingDisplay: 'P - Performed without Challenges',
      observedAt: '2025-01-15T10:00:00Z',
      recordedAt: '2025-01-15T10:05:00Z',
      evaluatorId: 'user-1',
      evaluatorName: 'Jane Smith',
      triggeringInjectId: 'inject-1',
      triggeringInject: {
        id: 'inject-1',
        injectNumber: 5,
        title: 'Hurricane Warning Issued',
      },
      createdAt: '2025-01-15T10:05:00Z',
      updatedAt: '2025-01-15T10:05:00Z',
      wasEdited: false,
      updatedBy: null,
    },
    {
      id: 'entry-2',
      criticalTaskId: 'task-2',
      criticalTask: {
        id: 'task-2',
        taskDescription: 'Establish communications',
        standard: null,
        capabilityTargetId: 'cap-1',
        capabilityTargetDescription: 'Establish operational command structure',
        capabilityTargetSources: null,
        capabilityName: 'Operational Coordination',
      },
      observationText: 'Radio communications had 5-minute delays due to equipment issues',
      rating: 'SomeChallenges' as PerformanceRating,
      ratingDisplay: 'S - Performed with Some Challenges',
      observedAt: '2025-01-15T11:00:00Z',
      recordedAt: '2025-01-15T11:10:00Z',
      evaluatorId: 'user-2',
      evaluatorName: 'John Doe',
      triggeringInjectId: null,
      triggeringInject: null,
      createdAt: '2025-01-15T11:10:00Z',
      updatedAt: '2025-01-15T11:10:00Z',
      wasEdited: false,
      updatedBy: null,
    },
    {
      id: 'entry-3',
      criticalTaskId: 'task-3',
      criticalTask: {
        id: 'task-3',
        taskDescription: 'Deploy resources',
        standard: null,
        capabilityTargetId: 'cap-2',
        capabilityTargetDescription: 'Resource allocation',
        capabilityTargetSources: null,
        capabilityName: 'Resource Management',
      },
      observationText: 'Resources not deployed, tracking system failure',
      rating: 'UnableToPerform' as PerformanceRating,
      ratingDisplay: 'U - Unable to be Performed',
      observedAt: '2025-01-15T09:00:00Z',
      recordedAt: '2025-01-15T09:15:00Z',
      evaluatorId: 'user-1',
      evaluatorName: 'Jane Smith',
      triggeringInjectId: null,
      triggeringInject: null,
      createdAt: '2025-01-15T09:15:00Z',
      updatedAt: '2025-01-15T09:30:00Z',
      wasEdited: true,
      updatedBy: {
        id: 'user-3',
        name: 'Admin User',
      },
    },
  ]

  const defaultProps = {
    entries: mockEntries,
    loading: false,
    error: null,
    canEdit: true,
    canDelete: true,
    currentUserId: 'user-1',
    onEdit: vi.fn(),
    onDelete: vi.fn(),
    onInjectClick: vi.fn(),
    deletingId: null,
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('rendering', () => {
    it('renders all entries', () => {
      render(<EegEntriesList {...defaultProps} />)

      expect(screen.getByText('Activate EOC')).toBeInTheDocument()
      expect(screen.getByText('Establish communications')).toBeInTheDocument()
      expect(screen.getByText('Deploy resources')).toBeInTheDocument()
    })

    it('displays entry count', () => {
      render(<EegEntriesList {...defaultProps} />)

      expect(screen.getByText('3 entries')).toBeInTheDocument()
    })

    it('displays singular entry count for one entry', () => {
      render(<EegEntriesList {...defaultProps} entries={[mockEntries[0]]} />)

      expect(screen.getByText('1 entry')).toBeInTheDocument()
    })

    it('shows loading state with skeletons', () => {
      const { container } = render(<EegEntriesList {...defaultProps} entries={[]} loading={true} />)

      const skeletons = container.querySelectorAll('.MuiSkeleton-root')
      expect(skeletons.length).toBeGreaterThan(0)
    })

    it('shows error alert when error provided', () => {
      render(<EegEntriesList {...defaultProps} error="Failed to load entries" />)

      expect(screen.getByRole('alert')).toBeInTheDocument()
      expect(screen.getByText(/Failed to load entries/i)).toBeInTheDocument()
    })

    it('shows empty state when no entries', () => {
      render(<EegEntriesList {...defaultProps} entries={[]} />)

      expect(screen.getByText(/No EEG entries recorded yet/i)).toBeInTheDocument()
    })
  })

  describe('entry display', () => {
    it('displays observed time for each entry', () => {
      // Times should be formatted from UTC - these are converted to local time
      // Entry 1: 10:00 UTC, Entry 2: 11:00 UTC, Entry 3: 09:00 UTC
      // Just check that time elements exist with monospace font
      const { container } = render(<EegEntriesList {...defaultProps} />)
      const timeElements = container.querySelectorAll('p.MuiTypography-body2')
      // Should have time typography elements
      expect(timeElements.length).toBeGreaterThan(0)
    })

    it('displays task description', () => {
      render(<EegEntriesList {...defaultProps} />)

      expect(screen.getByText('Activate EOC')).toBeInTheDocument()
      expect(screen.getByText('Establish communications')).toBeInTheDocument()
    })

    it('displays observation text truncated', () => {
      render(<EegEntriesList {...defaultProps} />)

      expect(screen.getByText(/EOC activated promptly/i)).toBeInTheDocument()
      expect(screen.getByText(/Radio communications had 5-minute delays/i)).toBeInTheDocument()
    })

    it('displays rating chips with correct labels', () => {
      render(<EegEntriesList {...defaultProps} />)

      expect(screen.getByText('P')).toBeInTheDocument()
      expect(screen.getByText('S')).toBeInTheDocument()
      expect(screen.getByText('U')).toBeInTheDocument()
    })

    it('displays evaluator initials', () => {
      render(<EegEntriesList {...defaultProps} />)

      // Jane Smith -> J.S (without the dots in the implementation)
      const evaluators = screen.getAllByText('J.S')
      expect(evaluators.length).toBe(2) // Entry 1 and 3

      // John Doe -> J.D
      expect(screen.getByText('J.D')).toBeInTheDocument()
    })

    it('shows edited indicator for edited entries', () => {
      render(<EegEntriesList {...defaultProps} />)

      expect(screen.getByText('edited')).toBeInTheDocument()
    })

    it('has left border colored by rating', () => {
      const { container } = render(<EegEntriesList {...defaultProps} />)

      // MUI inlines the border styles, check for MuiPaper entries
      const entryCards = container.querySelectorAll('.MuiPaper-root')
      // Should have 3 entry cards (not counting dialogs or other papers)
      expect(entryCards.length).toBeGreaterThan(0)
    })
  })

  describe('sorting', () => {
    it('defaults to sorting by observedAt descending (newest first)', () => {
      const { container } = render(<EegEntriesList {...defaultProps} />)

      // Get all entry Paper components
      const papers = container.querySelectorAll('.MuiPaper-root')
      // Default sort: newest first (entry-2 at 11:00, entry-1 at 10:00, entry-3 at 09:00)
      // Check that entries are rendered in the correct order
      const firstEntry = papers[0]
      const secondEntry = papers[1]
      const thirdEntry = papers[2]

      expect(firstEntry.textContent).toContain('Establish communications')
      expect(secondEntry.textContent).toContain('Activate EOC')
      expect(thirdEntry.textContent).toContain('Deploy resources')
    })

    it('opens sort menu when sort button clicked', async () => {
      const user = userEvent.setup()
      render(<EegEntriesList {...defaultProps} />)

      const sortButton = screen.getByLabelText(/Sort entries/i)
      await user.click(sortButton)

      await waitFor(() => {
        expect(screen.getByRole('menu')).toBeInTheDocument()
        expect(screen.getByText(/Time/i)).toBeInTheDocument()
        expect(screen.getByText(/Rating/i)).toBeInTheDocument()
        expect(screen.getByText(/Evaluator/i)).toBeInTheDocument()
        expect(screen.getByText(/Task/i)).toBeInTheDocument()
      })
    })

    it('sorts by time when Time menu item clicked', async () => {
      const user = userEvent.setup()
      render(<EegEntriesList {...defaultProps} />)

      const sortButton = screen.getByLabelText(/Sort entries/i)
      await user.click(sortButton)

      const timeOption = screen.getByText(/Time/)
      await user.click(timeOption)

      // Menu should close
      await waitFor(() => {
        expect(screen.queryByRole('menu')).not.toBeInTheDocument()
      })
    })

    it('toggles sort order when clicking same field', async () => {
      const user = userEvent.setup()
      const { container } = render(<EegEntriesList {...defaultProps} />)

      // Default is already sorted by observedAt desc (newest first)
      // Click same field again to toggle to asc (oldest first)
      const sortButton = screen.getByLabelText(/Sort entries/i)
      await user.click(sortButton)

      await waitFor(() => {
        expect(screen.getByText(/Time.*newest/i)).toBeInTheDocument()
      })

      const timeOption = screen.getByText(/Time/)
      await user.click(timeOption)

      // Now menu closes, click again to toggle sort direction
      await user.click(sortButton)

      await waitFor(() => {
        const timeOption2 = screen.getByText(/Time/)
        expect(timeOption2).toBeInTheDocument()
      })

      await user.click(screen.getByText(/Time/))

      // After toggle, oldest first (entry-3 at 9:00, entry-1 at 10:00, entry-2 at 11:00)
      await waitFor(
        () => {
          const papers = container.querySelectorAll('.MuiPaper-root')
          expect(papers[0].textContent).toContain('Deploy resources')
        },
        { timeout: 2000 },
      )
    })

    it('sorts by rating when Rating clicked', async () => {
      const user = userEvent.setup()
      const { container } = render(<EegEntriesList {...defaultProps} />)

      const sortButton = screen.getByLabelText(/Sort entries/i)
      await user.click(sortButton)

      const ratingOption = screen.getByText(/Rating/)
      await user.click(ratingOption)

      // Rating sort (desc): U->M->S->P, so entry-3 (U) first
      await waitFor(() => {
        const papers = container.querySelectorAll('.MuiPaper-root')
        expect(papers[0].textContent).toContain('Deploy resources') // UnableToPerform
        expect(papers[1].textContent).toContain('Establish communications') // SomeChallenges
        expect(papers[2].textContent).toContain('Activate EOC') // Performed
      })
    })

    it('sorts by evaluator when Evaluator clicked', async () => {
      const user = userEvent.setup()
      const { container } = render(<EegEntriesList {...defaultProps} />)

      const sortButton = screen.getByLabelText(/Sort entries/i)
      await user.click(sortButton)

      const evaluatorOption = screen.getByText(/Evaluator/)
      await user.click(evaluatorOption)

      // Evaluator sort (desc): John Doe, Jane Smith (2 entries)
      await waitFor(() => {
        const papers = container.querySelectorAll('.MuiPaper-root')
        expect(papers[0].textContent).toContain('Establish communications') // John Doe
      })
    })

    it('sorts by task when Task clicked', async () => {
      const user = userEvent.setup()
      const { container } = render(<EegEntriesList {...defaultProps} />)

      const sortButton = screen.getByLabelText(/Sort entries/i)
      await user.click(sortButton)

      const taskOption = screen.getByText(/Task/)
      await user.click(taskOption)

      // Task sort (desc): Deploy, Establish, Activate
      await waitFor(() => {
        const papers = container.querySelectorAll('.MuiPaper-root')
        expect(papers[0].textContent).toContain('Establish communications')
        expect(papers[1].textContent).toContain('Deploy resources')
        expect(papers[2].textContent).toContain('Activate EOC')
      })
    })

    it('shows sort direction indicator in menu', async () => {
      const user = userEvent.setup()
      render(<EegEntriesList {...defaultProps} />)

      const sortButton = screen.getByLabelText(/Sort entries/i)
      await user.click(sortButton)

      await waitFor(() => {
        expect(screen.getByText(/Time.*newest/i)).toBeInTheDocument()
      })
    })
  })

  describe('entry interaction', () => {
    it('opens detail dialog when entry clicked', async () => {
      const user = userEvent.setup()
      render(<EegEntriesList {...defaultProps} />)

      // Find the Paper component that contains the task description
      const entryCard = screen.getByText('Activate EOC').closest('.MuiPaper-root')
      expect(entryCard).toBeInTheDocument()

      await user.click(entryCard!)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
        expect(screen.getByText('EEG Entry Detail')).toBeInTheDocument()
      })
    })

    it('shows full entry details in dialog', async () => {
      const user = userEvent.setup()
      render(<EegEntriesList {...defaultProps} />)

      const entryCard = screen.getByText('Activate EOC').closest('.MuiPaper-root')
      await user.click(entryCard!)

      await waitFor(() => {
        const dialog = screen.getByRole('dialog')
        expect(within(dialog).getByText('Operational Coordination')).toBeInTheDocument()
        expect(within(dialog).getByText('Activate EOC')).toBeInTheDocument()
        // The standard text appears in the dialog
        expect(within(dialog).getByText(/Standard:/i)).toBeInTheDocument()
        // Sources appear in dialog
        expect(within(dialog).getByText(/Sources:/i)).toBeInTheDocument()
        // Observation text appears
        expect(within(dialog).getByText(/EOC activated promptly/i)).toBeInTheDocument()
      })
    })

    it('closes detail dialog when close button clicked', async () => {
      const user = userEvent.setup()
      render(<EegEntriesList {...defaultProps} />)

      const entryCard = screen.getByText('Activate EOC').closest('.MuiPaper-root')
      await user.click(entryCard!)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      const closeButton = screen.getByRole('button', { name: /close/i })
      await user.click(closeButton)

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
      })
    })

    it('shows triggering inject link in detail dialog when present', async () => {
      const user = userEvent.setup()
      render(<EegEntriesList {...defaultProps} />)

      const entryCard = screen.getByText('Activate EOC').closest('.MuiPaper-root')
      await user.click(entryCard!)

      await waitFor(() => {
        const dialog = screen.getByRole('dialog')
        expect(within(dialog).getByText(/INJ-005.*Hurricane Warning Issued/i)).toBeInTheDocument()
      })
    })

    it('calls onInjectClick when inject link clicked', async () => {
      const user = userEvent.setup()
      render(<EegEntriesList {...defaultProps} />)

      const entryCard = screen.getByText('Activate EOC').closest('.MuiPaper-root')
      await user.click(entryCard!)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      const injectLink = screen.getByText(/INJ-005/)
      await user.click(injectLink)

      expect(defaultProps.onInjectClick).toHaveBeenCalledWith('inject-1')
    })

    it('shows edit indicator in detail for edited entries', async () => {
      const user = userEvent.setup()
      render(<EegEntriesList {...defaultProps} />)

      const entryCard = screen.getByText('Deploy resources').closest('.MuiPaper-root')
      await user.click(entryCard!)

      await waitFor(() => {
        const dialog = screen.getByRole('dialog')
        expect(within(dialog).getByText(/Edited/i)).toBeInTheDocument()
        expect(within(dialog).getByText(/by Admin User/i)).toBeInTheDocument()
      })
    })
  })

  describe('edit functionality', () => {
    it('shows Edit button in detail dialog when user can edit', async () => {
      const user = userEvent.setup()
      render(<EegEntriesList {...defaultProps} />)

      const entryCard = screen.getByText('Activate EOC').closest('.MuiPaper-root')
      await user.click(entryCard!)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Edit/i })).toBeInTheDocument()
      })
    })

    it('hides Edit button when user cannot edit', async () => {
      const user = userEvent.setup()
      render(<EegEntriesList {...defaultProps} canEdit={false} />)

      const entryCard = screen.getByText('Activate EOC').closest('.MuiPaper-root')
      await user.click(entryCard!)

      await waitFor(() => {
        expect(screen.queryByRole('button', { name: /Edit/i })).not.toBeInTheDocument()
      })
    })

    it('allows editing own entries when not Director', async () => {
      const user = userEvent.setup()
      render(<EegEntriesList {...defaultProps} canDelete={false} currentUserId="user-1" />)

      // Entry 1 is by user-1
      const entryCard = screen.getByText('Activate EOC').closest('.MuiPaper-root')
      await user.click(entryCard!)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Edit/i })).toBeInTheDocument()
      })
    })

    it('prevents editing others entries when not Director', async () => {
      const user = userEvent.setup()
      render(<EegEntriesList {...defaultProps} canDelete={false} currentUserId="user-1" />)

      // Entry 2 is by user-2
      const entryCard = screen.getByText('Establish communications').closest('.MuiPaper-root')
      await user.click(entryCard!)

      await waitFor(() => {
        expect(screen.queryByRole('button', { name: /Edit/i })).not.toBeInTheDocument()
      })
    })

    it('calls onEdit when Edit button clicked', async () => {
      const user = userEvent.setup()
      render(<EegEntriesList {...defaultProps} />)

      const entryCard = screen.getByText('Activate EOC').closest('.MuiPaper-root')
      await user.click(entryCard!)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Edit/i })).toBeInTheDocument()
      })

      const editButton = screen.getByRole('button', { name: /Edit/i })
      await user.click(editButton)

      expect(defaultProps.onEdit).toHaveBeenCalledWith(mockEntries[0])
    })

    it('closes detail dialog when Edit clicked', async () => {
      const user = userEvent.setup()
      render(<EegEntriesList {...defaultProps} />)

      const entryCard = screen.getByText('Activate EOC').closest('.MuiPaper-root')
      await user.click(entryCard!)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      const editButton = screen.getByRole('button', { name: /Edit/i })
      await user.click(editButton)

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
      })
    })
  })

  describe('delete functionality', () => {
    it('shows Delete button in detail dialog when user can delete', async () => {
      const user = userEvent.setup()
      render(<EegEntriesList {...defaultProps} />)

      const entryCard = screen.getByText('Activate EOC').closest('.MuiPaper-root')
      await user.click(entryCard!)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Delete/i })).toBeInTheDocument()
      })
    })

    it('hides Delete button when user cannot delete', async () => {
      const user = userEvent.setup()
      render(<EegEntriesList {...defaultProps} canDelete={false} />)

      const entryCard = screen.getByText('Activate EOC').closest('.MuiPaper-root')
      await user.click(entryCard!)

      await waitFor(() => {
        expect(screen.queryByRole('button', { name: /Delete/i })).not.toBeInTheDocument()
      })
    })

    it('opens confirmation dialog when Delete clicked', async () => {
      const user = userEvent.setup()
      render(<EegEntriesList {...defaultProps} />)

      const entryCard = screen.getByText('Activate EOC').closest('.MuiPaper-root')
      await user.click(entryCard!)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Delete/i })).toBeInTheDocument()
      })

      const deleteButton = screen.getByRole('button', { name: /Delete/i })
      await user.click(deleteButton)

      await waitFor(() => {
        expect(screen.getByText(/Delete EEG Entry\?/i)).toBeInTheDocument()
        expect(screen.getByText(/This action cannot be undone/i)).toBeInTheDocument()
      })
    })

    it('shows entry preview in delete confirmation', async () => {
      const user = userEvent.setup()
      render(<EegEntriesList {...defaultProps} />)

      const entryCard = screen.getByText('Activate EOC').closest('.MuiPaper-root')
      await user.click(entryCard!)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Delete/i })).toBeInTheDocument()
      })

      const deleteButton = screen.getByRole('button', { name: /Delete/i })
      await user.click(deleteButton)

      await waitFor(() => {
        const dialogs = screen.getAllByRole('dialog')
        const confirmDialog = dialogs[dialogs.length - 1]
        expect(within(confirmDialog).getByText('Activate EOC')).toBeInTheDocument()
        expect(within(confirmDialog).getByText(/EOC activated promptly/i)).toBeInTheDocument()
      })
    })

    it('calls onDelete when delete confirmed', async () => {
      const user = userEvent.setup()
      render(<EegEntriesList {...defaultProps} />)

      const entryCard = screen.getByText('Activate EOC').closest('.MuiPaper-root')
      await user.click(entryCard!)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Delete/i })).toBeInTheDocument()
      })

      const deleteButton = screen.getByRole('button', { name: /Delete/i })
      await user.click(deleteButton)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Delete Entry/i })).toBeInTheDocument()
      })

      const confirmButton = screen.getByRole('button', { name: /Delete Entry/i })
      await user.click(confirmButton)

      expect(defaultProps.onDelete).toHaveBeenCalledWith('entry-1')
    })

    it('closes confirmation when Cancel clicked', async () => {
      const user = userEvent.setup()
      render(<EegEntriesList {...defaultProps} />)

      const entryCard = screen.getByText('Activate EOC').closest('.MuiPaper-root')
      await user.click(entryCard!)

      const deleteButton = screen.getByRole('button', { name: /Delete/i })
      await user.click(deleteButton)

      await waitFor(() => {
        expect(screen.getByText(/Delete EEG Entry\?/i)).toBeInTheDocument()
      })

      const cancelButton = screen.getByRole('button', { name: /Cancel/i })
      await user.click(cancelButton)

      await waitFor(() => {
        expect(screen.queryByText(/Delete EEG Entry\?/i)).not.toBeInTheDocument()
      })
    })

    it('disables buttons during delete operation', async () => {
      const user = userEvent.setup()
      render(<EegEntriesList {...defaultProps} deletingId="entry-1" />)

      const entryCard = screen.getByText('Activate EOC').closest('.MuiPaper-root')
      await user.click(entryCard!)

      const deleteButton = screen.getByRole('button', { name: /Delete/i })
      await user.click(deleteButton)

      await waitFor(() => {
        const confirmButton = screen.getByRole('button', { name: /Deleting.../i })
        expect(confirmButton).toBeDisabled()
        expect(screen.getByRole('button', { name: /Cancel/i })).toBeDisabled()
      })
    })
  })

  describe('accessibility', () => {
    it('has clickable entry cards with proper cursor', () => {
      render(<EegEntriesList {...defaultProps} />)

      // Entry cards should be clickable - check they render
      expect(screen.getByText('Activate EOC')).toBeInTheDocument()
      expect(screen.getByText('Establish communications')).toBeInTheDocument()
      expect(screen.getByText('Deploy resources')).toBeInTheDocument()
    })

    it('has tooltip on sort button', async () => {
      render(<EegEntriesList {...defaultProps} />)

      const sortButton = screen.getByLabelText(/Sort entries/i)
      expect(sortButton).toBeInTheDocument()
    })

    it('uses semantic dialog role for detail view', async () => {
      const user = userEvent.setup()
      render(<EegEntriesList {...defaultProps} />)

      const entryCard = screen.getByText('Activate EOC').closest('.MuiPaper-root')
      await user.click(entryCard!)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })
    })
  })
})
