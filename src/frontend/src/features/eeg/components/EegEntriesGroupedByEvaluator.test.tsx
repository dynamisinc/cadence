/**
 * Tests for EegEntriesGroupedByEvaluator component
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ThemeProvider } from '@mui/material'
import { cobraTheme } from '../../../theme/cobraTheme'
import { EegEntriesGroupedByEvaluator } from './EegEntriesGroupedByEvaluator'
import { PerformanceRating, type EegEntryDto } from '../types'

const renderWithTheme = (ui: React.ReactElement) => {
  return render(<ThemeProvider theme={cobraTheme}>{ui}</ThemeProvider>)
}

const mockEntries: EegEntryDto[] = [
  {
    id: '1',
    criticalTaskId: 'task1',
    criticalTask: {
      id: 'task1',
      taskDescription: 'Activate emergency communication plan',
      standard: 'Per SOP 5.2',
      capabilityTargetId: 'cap1',
      capabilityTargetDescription: 'Establish interoperable communications',
      capabilityTargetSources: null,
      capabilityName: 'Operational Communications',
    },
    observationText: 'EOC issued activation at 09:15',
    rating: PerformanceRating.SomeChallenges,
    ratingDisplay: 'S',
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
    criticalTaskId: 'task2',
    criticalTask: {
      id: 'task2',
      taskDescription: 'Establish radio net',
      standard: null,
      capabilityTargetId: 'cap1',
      capabilityTargetDescription: 'Establish interoperable communications',
      capabilityTargetSources: null,
      capabilityName: 'Operational Communications',
    },
    observationText: 'Radio net established with delays',
    rating: PerformanceRating.MajorChallenges,
    ratingDisplay: 'M',
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
    id: '3',
    criticalTaskId: 'task3',
    criticalTask: {
      id: 'task3',
      taskDescription: 'Staff EOC positions',
      standard: null,
      capabilityTargetId: 'cap1',
      capabilityTargetDescription: 'EOC activation',
      capabilityTargetSources: null,
      capabilityName: 'Operational Communications',
    },
    observationText: 'All positions filled on time',
    rating: PerformanceRating.Performed,
    ratingDisplay: 'P',
    observedAt: '2026-02-03T10:32:00',
    recordedAt: '2026-02-03T10:33:00',
    evaluatorId: 'eval2',
    evaluatorName: 'S. Kim',
    triggeringInjectId: null,
    triggeringInject: null,
    createdAt: '2026-02-03T10:33:00',
    updatedAt: '2026-02-03T10:33:00',
    wasEdited: false,
    updatedBy: null,
  },
]

describe('EegEntriesGroupedByEvaluator', () => {
  it('groups entries by evaluator', () => {
    render(
      <EegEntriesGroupedByEvaluator
        entries={mockEntries}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
        canEdit={false}
        canDelete={false}
      />,
    )

    expect(screen.getByText('R. Chen')).toBeInTheDocument()
    expect(screen.getByText('S. Kim')).toBeInTheDocument()
  })

  it('shows entry count for each evaluator', () => {
    render(
      <EegEntriesGroupedByEvaluator
        entries={mockEntries}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
        canEdit={false}
        canDelete={false}
      />,
    )

    // R. Chen has 2 entries
    expect(screen.getByText(/2 entries/i)).toBeInTheDocument()
    // S. Kim has 1 entry
    expect(screen.getByText(/1 entry/i)).toBeInTheDocument()
  })

  it('shows rating distribution for each evaluator', () => {
    render(
      <EegEntriesGroupedByEvaluator
        entries={mockEntries}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
        canEdit={false}
        canDelete={false}
      />,
    )

    // R. Chen: S:1, M:1
    const chenButton = screen.getByRole('button', { name: /Expand R\. Chen entries/i })
    expect(chenButton.textContent).toMatch(/S:1/)
    expect(chenButton.textContent).toMatch(/M:1/)

    // S. Kim: P:1
    const kimButton = screen.getByRole('button', { name: /Expand S\. Kim entries/i })
    expect(kimButton.textContent).toMatch(/P:1/)
  })

  it('expands to show evaluator entries', async () => {
    const user = userEvent.setup()

    render(
      <EegEntriesGroupedByEvaluator
        entries={mockEntries}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
        canEdit={false}
        canDelete={false}
      />,
    )

    // Entries should not be visible initially
    expect(screen.queryByText('EOC issued activation at 09:15')).not.toBeInTheDocument()

    // Expand R. Chen's section
    const chenAccordion = screen.getByText('R. Chen').closest('button')
    await user.click(chenAccordion!)

    // Should show entries
    await waitFor(() => {
      expect(screen.getByText(/EOC issued activation at 09:15/i)).toBeInTheDocument()
      expect(screen.getByText(/Radio net established with delays/i)).toBeInTheDocument()
    })
  })

  it('handles entries with null evaluator name', () => {
    const entriesWithNull: EegEntryDto[] = [
      {
        ...mockEntries[0],
        evaluatorId: 'unknown',
        evaluatorName: null,
      },
    ]

    render(
      <EegEntriesGroupedByEvaluator
        entries={entriesWithNull}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
        canEdit={false}
        canDelete={false}
      />,
    )

    expect(screen.getByText('Unknown Evaluator')).toBeInTheDocument()
  })

  it('shows empty state when no entries', () => {
    render(
      <EegEntriesGroupedByEvaluator
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

    renderWithTheme(
      <EegEntriesGroupedByEvaluator
        entries={mockEntries}
        onEdit={handleEdit}
        onDelete={vi.fn()}
        canEdit={true}
        canDelete={false}
        currentUserId="eval1"
      />,
    )

    // Expand R. Chen's section
    const chenAccordion = screen.getByText('R. Chen').closest('button')
    await user.click(chenAccordion!)

    await waitFor(() => {
      expect(screen.getByText(/EOC issued activation at 09:15/i)).toBeInTheDocument()
    })

    // Click entry to open detail
    const entryCard = screen.getByText(/EOC issued activation at 09:15/i).closest('div[role="button"]')
    await user.click(entryCard!)

    // Click edit in dialog
    await waitFor(() => {
      const editButton = screen.getByRole('button', { name: /edit/i })
      expect(editButton).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: /edit/i }))
    expect(handleEdit).toHaveBeenCalledWith(mockEntries[0])
  })

  it('shows loading state', () => {
    render(
      <EegEntriesGroupedByEvaluator
        entries={[]}
        loading={true}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
        canEdit={false}
        canDelete={false}
      />,
    )

    const skeletons = document.querySelectorAll('.MuiSkeleton-root')
    expect(skeletons.length).toBeGreaterThan(0)
  })

  it('shows error state', () => {
    render(
      <EegEntriesGroupedByEvaluator
        entries={[]}
        error="Failed to load"
        onEdit={vi.fn()}
        onDelete={vi.fn()}
        canEdit={false}
        canDelete={false}
      />,
    )

    expect(screen.getByText(/Failed to load/i)).toBeInTheDocument()
  })
})
