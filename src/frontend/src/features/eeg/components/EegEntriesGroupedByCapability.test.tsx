/**
 * Tests for EegEntriesGroupedByCapability component
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { EegEntriesGroupedByCapability } from './EegEntriesGroupedByCapability'
import { PerformanceRating, type EegEntryDto } from '../types'

const mockEntries: EegEntryDto[] = [
  {
    id: '1',
    criticalTaskId: 'task1',
    criticalTask: {
      id: 'task1',
      taskDescription: 'Activate emergency communication plan',
      standard: 'Per SOP 5.2',
      capabilityTargetId: 'cap1',
      capabilityTargetDescription: 'Establish interoperable communications within 30 minutes',
      capabilityTargetSources: 'Metro County EOP, Annex F',
      capabilityName: 'Operational Communications',
    },
    observationText: 'EOC issued activation at 09:15',
    rating: PerformanceRating.SomeChallenges,
    ratingDisplay: 'S - Performed with Some Challenges',
    observedAt: '2026-02-03T10:45:00',
    recordedAt: '2026-02-03T10:47:00',
    evaluatorId: 'eval1',
    evaluatorName: 'R. Chen',
    triggeringInjectId: null,
    triggeringInject: null,
    createdAt: '2026-02-03T10:47:00',
    updatedAt: '2026-02-03T10:47:00',
    wasEdited: false,
    updatedBy: null,
  },
  {
    id: '2',
    criticalTaskId: 'task1',
    criticalTask: {
      id: 'task1',
      taskDescription: 'Activate emergency communication plan',
      standard: 'Per SOP 5.2',
      capabilityTargetId: 'cap1',
      capabilityTargetDescription: 'Establish interoperable communications within 30 minutes',
      capabilityTargetSources: 'Metro County EOP, Annex F',
      capabilityName: 'Operational Communications',
    },
    observationText: 'Notification sent within 5 minutes',
    rating: PerformanceRating.Performed,
    ratingDisplay: 'P - Performed without Challenges',
    observedAt: '2026-02-03T10:12:00',
    recordedAt: '2026-02-03T10:13:00',
    evaluatorId: 'eval2',
    evaluatorName: 'S. Kim',
    triggeringInjectId: null,
    triggeringInject: null,
    createdAt: '2026-02-03T10:13:00',
    updatedAt: '2026-02-03T10:13:00',
    wasEdited: false,
    updatedBy: null,
  },
  {
    id: '3',
    criticalTaskId: 'task2',
    criticalTask: {
      id: 'task2',
      taskDescription: 'Establish radio net with field units',
      standard: null,
      capabilityTargetId: 'cap1',
      capabilityTargetDescription: 'Establish interoperable communications within 30 minutes',
      capabilityTargetSources: 'Metro County EOP, Annex F',
      capabilityName: 'Operational Communications',
    },
    observationText: 'Radio net established but field delays',
    rating: PerformanceRating.MajorChallenges,
    ratingDisplay: 'M - Performed with Major Challenges',
    observedAt: '2026-02-03T10:18:00',
    recordedAt: '2026-02-03T10:20:00',
    evaluatorId: 'eval1',
    evaluatorName: 'R. Chen',
    triggeringInjectId: null,
    triggeringInject: null,
    createdAt: '2026-02-03T10:20:00',
    updatedAt: '2026-02-03T10:20:00',
    wasEdited: false,
    updatedBy: null,
  },
  {
    id: '4',
    criticalTaskId: 'task3',
    criticalTask: {
      id: 'task3',
      taskDescription: 'Open and staff shelter',
      standard: 'Within 2 hours',
      capabilityTargetId: 'cap2',
      capabilityTargetDescription: 'Open shelter facility within 2 hours',
      capabilityTargetSources: null,
      capabilityName: 'Mass Care Services',
    },
    observationText: 'Shelter opened on time',
    rating: PerformanceRating.Performed,
    ratingDisplay: 'P - Performed without Challenges',
    observedAt: '2026-02-03T11:00:00',
    recordedAt: '2026-02-03T11:05:00',
    evaluatorId: 'eval3',
    evaluatorName: 'M. Jones',
    triggeringInjectId: null,
    triggeringInject: null,
    createdAt: '2026-02-03T11:05:00',
    updatedAt: '2026-02-03T11:05:00',
    wasEdited: false,
    updatedBy: null,
  },
]

describe('EegEntriesGroupedByCapability', () => {
  it('groups entries by capability target', () => {
    render(
      <EegEntriesGroupedByCapability
        entries={mockEntries}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
        canEdit={false}
        canDelete={false}
      />,
    )

    // Should show two capability targets
    expect(screen.getByText('Operational Communications')).toBeInTheDocument()
    expect(screen.getByText('Mass Care Services')).toBeInTheDocument()
  })

  it('shows entry count for each capability target', () => {
    render(
      <EegEntriesGroupedByCapability
        entries={mockEntries}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
        canEdit={false}
        canDelete={false}
      />,
    )

    // Operational Communications has 3 entries
    expect(screen.getByText(/3 entries/i)).toBeInTheDocument()
    // Mass Care Services has 1 entry
    expect(screen.getByText(/1 entry/i)).toBeInTheDocument()
  })

  it('shows rating distribution for each capability target', () => {
    render(
      <EegEntriesGroupedByCapability
        entries={mockEntries}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
        canEdit={false}
        canDelete={false}
      />,
    )

    // Should show P:1 S:1 M:1 U:0 for Operational Communications
    expect(screen.getByText(/P:1/)).toBeInTheDocument()
    expect(screen.getByText(/S:1/)).toBeInTheDocument()
    expect(screen.getByText(/M:1/)).toBeInTheDocument()
  })

  it('expands and collapses capability targets', async () => {
    const user = userEvent.setup()

    render(
      <EegEntriesGroupedByCapability
        entries={mockEntries}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
        canEdit={false}
        canDelete={false}
      />,
    )

    // Critical tasks should not be visible initially (accordion collapsed by default)
    expect(screen.queryByText('Activate emergency communication plan')).not.toBeInTheDocument()

    // Click to expand Operational Communications
    const opCommAccordion = screen.getByText('Operational Communications').closest('button')
    expect(opCommAccordion).toBeInTheDocument()
    await user.click(opCommAccordion!)

    // Now critical tasks should be visible
    await waitFor(() => {
      expect(screen.getByText('Activate emergency communication plan')).toBeInTheDocument()
      expect(screen.getByText('Establish radio net with field units')).toBeInTheDocument()
    })
  })

  it('groups entries by critical task within capability target', async () => {
    const user = userEvent.setup()

    render(
      <EegEntriesGroupedByCapability
        entries={mockEntries}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
        canEdit={false}
        canDelete={false}
      />,
    )

    // Expand Operational Communications
    const opCommAccordion = screen.getByText('Operational Communications').closest('button')
    await user.click(opCommAccordion!)

    await waitFor(() => {
      // Should show 2 entries for "Activate emergency communication plan"
      const taskText = screen.getByText('Activate emergency communication plan')
      const taskSection = taskText.closest('div')
      expect(taskSection).toBeInTheDocument()
      expect(taskSection?.textContent).toMatch(/2 entries/i)
    })
  })

  it('expands and shows individual entries under a task', async () => {
    const user = userEvent.setup()

    render(
      <EegEntriesGroupedByCapability
        entries={mockEntries}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
        canEdit={false}
        canDelete={false}
      />,
    )

    // Expand Operational Communications
    const opCommAccordion = screen.getByText('Operational Communications').closest('button')
    await user.click(opCommAccordion!)

    await waitFor(() => {
      expect(screen.getByText('Activate emergency communication plan')).toBeInTheDocument()
    })

    // Expand the critical task
    const taskAccordion = screen.getByText('Activate emergency communication plan').closest('button')
    await user.click(taskAccordion!)

    // Should show individual entries
    await waitFor(() => {
      expect(screen.getByText(/EOC issued activation at 09:15/i)).toBeInTheDocument()
      expect(screen.getByText(/Notification sent within 5 minutes/i)).toBeInTheDocument()
    })
  })

  it('shows empty state when no entries', () => {
    render(
      <EegEntriesGroupedByCapability
        entries={[]}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
        canEdit={false}
        canDelete={false}
      />,
    )

    expect(screen.getByText(/No EEG entries/i)).toBeInTheDocument()
  })

  it('calls onEdit when edit is clicked', async () => {
    const handleEdit = vi.fn()
    const user = userEvent.setup()

    render(
      <EegEntriesGroupedByCapability
        entries={mockEntries}
        onEdit={handleEdit}
        onDelete={vi.fn()}
        canEdit={true}
        canDelete={false}
        currentUserId="eval1"
      />,
    )

    // Expand to entries
    const opCommAccordion = screen.getByText('Operational Communications').closest('button')
    await user.click(opCommAccordion!)

    await waitFor(() => {
      expect(screen.getByText('Activate emergency communication plan')).toBeInTheDocument()
    })

    const taskAccordion = screen.getByText('Activate emergency communication plan').closest('button')
    await user.click(taskAccordion!)

    await waitFor(() => {
      expect(screen.getByText(/EOC issued activation at 09:15/i)).toBeInTheDocument()
    })

    // Click on an entry to open detail dialog
    const entryCard = screen.getByText(/EOC issued activation at 09:15/i).closest('div[role="button"]')
    await user.click(entryCard!)

    // Wait for dialog and click edit
    await waitFor(() => {
      const editButton = screen.getByRole('button', { name: /edit/i })
      expect(editButton).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: /edit/i }))
    expect(handleEdit).toHaveBeenCalledWith(mockEntries[0])
  })

  it('shows loading state', () => {
    render(
      <EegEntriesGroupedByCapability
        entries={[]}
        loading={true}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
        canEdit={false}
        canDelete={false}
      />,
    )

    // Should show skeleton loaders
    const skeletons = document.querySelectorAll('.MuiSkeleton-root')
    expect(skeletons.length).toBeGreaterThan(0)
  })

  it('shows error state', () => {
    render(
      <EegEntriesGroupedByCapability
        entries={[]}
        error="Failed to load entries"
        onEdit={vi.fn()}
        onDelete={vi.fn()}
        canEdit={false}
        canDelete={false}
      />,
    )

    expect(screen.getByText(/Failed to load entries/i)).toBeInTheDocument()
  })
})
