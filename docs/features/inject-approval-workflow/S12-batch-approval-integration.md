# S12: Batch Approval Integration in MSEL View

**Feature:** [Inject Approval Workflow](FEATURE.md)
**Priority:** P1
**Points:** 3
**Dependencies:** S05 (Batch Approval Actions)

## User Story

**As an** Exercise Director,
**I want** to select multiple injects in the MSEL view and approve or reject them in bulk,
**So that** I can efficiently review large MSELs without processing each inject individually.

## Context

The BatchApprovalToolbar component already exists and provides batch approve/reject functionality. This story integrates it into the InjectListPage (MSEL view) by adding inject selection capabilities (checkboxes) and displaying the toolbar when injects are selected. This completes the batch approval user experience started in S05.

## Acceptance Criteria

### Selection UI in MSEL View
- [ ] **Given** I am on the MSEL view (InjectListPage) with approval enabled, **when** I view the inject table, **then** I see a checkbox column on the left of each row
- [ ] **Given** the checkbox column exists, **when** I click a row checkbox, **then** that inject is selected (checkbox checked)
- [ ] **Given** I click a selected checkbox, **when** clicked, **then** that inject is deselected (checkbox unchecked)
- [ ] **Given** there are multiple injects, **when** I click the header checkbox, **then** all visible injects are selected
- [ ] **Given** all injects are selected, **when** I click the header checkbox, **then** all are deselected
- [ ] **Given** some but not all injects are selected, **when** I view the header checkbox, **then** it shows an indeterminate state (partial selection)

### BatchApprovalToolbar Display
- [ ] **Given** no injects are selected, **when** I view the MSEL, **then** the BatchApprovalToolbar is not displayed
- [ ] **Given** I select one or more injects, **when** selection changes, **then** the BatchApprovalToolbar appears above the table
- [ ] **Given** the BatchApprovalToolbar is visible, **when** I view it, **then** it shows the count of selected injects
- [ ] **Given** the BatchApprovalToolbar is visible, **when** I view it, **then** it shows Approve and Reject buttons with counts of approvable injects

### Selection Persistence
- [ ] **Given** I select injects, **when** I navigate away from the MSEL page and return, **then** selection is cleared (no persistence across navigation)
- [ ] **Given** I select injects, **when** I apply filters or sorting, **then** selection is preserved for visible injects only
- [ ] **Given** I select injects, **when** I filter the list and selected injects are hidden, **then** those hidden injects remain selected but not visible

### Batch Actions Integration
- [ ] **Given** I select injects and click "Approve", **when** batch approval completes, **then** selection is cleared
- [ ] **Given** I select injects and click "Reject", **when** batch rejection completes, **then** selection is cleared
- [ ] **Given** batch action succeeds, **when** complete, **then** the inject list refreshes to show updated statuses
- [ ] **Given** batch action fails, **when** error occurs, **then** selection is preserved so user can retry

### Mixed Status Selection
- [ ] **Given** I select injects with mixed statuses (Draft, Submitted, Approved), **when** I view the toolbar, **then** it only counts Submitted injects as approvable
- [ ] **Given** I select only Draft or Approved injects, **when** I view the toolbar, **then** both Approve and Reject buttons are disabled with tooltip explaining no Submitted injects selected
- [ ] **Given** I select injects including some submitted by me, **when** I view the toolbar, **then** the approvable count excludes my submissions

### Approval Workflow Disabled
- [ ] **Given** approval workflow is disabled for the exercise, **when** I view the MSEL, **then** I do NOT see the checkbox column
- [ ] **Given** approval workflow is disabled, **when** I view the MSEL, **then** I do NOT see the BatchApprovalToolbar regardless of other conditions

## UI Design

### MSEL View with Selection (Desktop)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Hurricane Response TTX - MSEL                                              │
│                                                                             │
│  [Filter ▼]  [Sort ▼]  [Search...]                    [Import]  [Export]   │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ 3 injects selected (2 can be approved)                              │   │
│  │                                [Approve (2)]  [Reject (2)]  [Clear] │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────────────────┤
│ ☐ │ # │ Title                          │ Time  │ Status     │ Actions     │
├───┼───┼────────────────────────────────┼───────┼────────────┼─────────────┤
│ ☐ │ 1 │ Initial Weather Warning        │ 09:15 │ Approved   │ [...]       │
│ ☑ │ 2 │ EOC Activation Notice          │ 09:30 │ Submitted  │ [...]       │
│ ☑ │ 3 │ Shelter Capacity Report        │ 09:45 │ Submitted  │ [...]       │
│ ☐ │ 4 │ Traffic Control Request        │ 10:00 │ Draft      │ [...]       │
│ ☑ │ 5 │ Media Inquiry (your submission)│ 10:15 │ Submitted  │ [...]       │
│ ☐ │ 6 │ Resource Request               │ 10:30 │ Draft      │ [...]       │
└───┴───┴────────────────────────────────┴───────┴────────────┴─────────────┘

Note: Inject #5 is selected but cannot be approved (submitted by current user)
```

### Mobile View Behavior

On mobile (responsive):
- Checkbox column remains visible but smaller
- BatchApprovalToolbar stacks buttons vertically on narrow screens
- Consider sticky toolbar that stays visible when scrolling

## Technical Implementation

### Frontend: Selection State Hook

```typescript
// File: src/frontend/src/features/injects/hooks/useInjectSelection.ts

import { useState, useMemo, useCallback } from 'react'

interface UseInjectSelectionOptions {
  injects: InjectDto[]
  onSelectionChange?: (selectedIds: string[]) => void
}

export const useInjectSelection = ({
  injects,
  onSelectionChange,
}: UseInjectSelectionOptions) => {
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())

  const toggleSelection = useCallback((id: string) => {
    setSelectedIds((prev) => {
      const next = new Set(prev)
      if (next.has(id)) {
        next.delete(id)
      } else {
        next.add(id)
      }
      onSelectionChange?.(Array.from(next))
      return next
    })
  }, [onSelectionChange])

  const selectAll = useCallback(() => {
    const allIds = new Set(injects.map(i => i.id))
    setSelectedIds(allIds)
    onSelectionChange?.(Array.from(allIds))
  }, [injects, onSelectionChange])

  const clearSelection = useCallback(() => {
    setSelectedIds(new Set())
    onSelectionChange?.([])
  }, [onSelectionChange])

  const isSelected = useCallback((id: string) => selectedIds.has(id), [selectedIds])

  const selectionState = useMemo(() => {
    if (selectedIds.size === 0) return 'none'
    if (selectedIds.size === injects.length) return 'all'
    return 'some'
  }, [selectedIds.size, injects.length])

  return {
    selectedIds: Array.from(selectedIds),
    toggleSelection,
    selectAll,
    clearSelection,
    isSelected,
    selectionState,
  }
}
```

### Frontend: InjectListPage Integration

```typescript
// File: src/frontend/src/features/injects/pages/InjectListPage.tsx

export const InjectListPage = () => {
  const { exerciseId } = useParams()
  const { data: exercise } = useExercise(exerciseId)
  const { data: injects } = useInjects(exerciseId)
  const { currentUser } = useAuth()

  const approvalEnabled = exercise?.requireInjectApproval ?? false

  const {
    selectedIds,
    toggleSelection,
    selectAll,
    clearSelection,
    isSelected,
    selectionState,
  } = useInjectSelection({ injects })

  const handleActionComplete = () => {
    // Refresh inject list after batch action
    queryClient.invalidateQueries(['injects', exerciseId])
  }

  return (
    <Box>
      <InjectListToolbar
        exerciseId={exerciseId}
        approvalEnabled={approvalEnabled}
      />

      {/* Batch Approval Toolbar - only shown when selection exists */}
      {approvalEnabled && selectedIds.length > 0 && (
        <BatchApprovalToolbar
          selectedIds={selectedIds}
          injects={injects}
          exerciseId={exerciseId}
          currentUserId={currentUser.id}
          onClearSelection={clearSelection}
          onActionComplete={handleActionComplete}
        />
      )}

      <InjectTable
        injects={injects}
        approvalEnabled={approvalEnabled}
        selectedIds={selectedIds}
        onToggleSelection={toggleSelection}
        onSelectAll={selectionState === 'all' ? clearSelection : selectAll}
        selectionState={selectionState}
      />
    </Box>
  )
}
```

### Frontend: InjectTable with Checkboxes

```typescript
// File: src/frontend/src/features/injects/components/InjectTable.tsx

interface InjectTableProps {
  injects: InjectDto[]
  approvalEnabled: boolean
  selectedIds?: string[]
  onToggleSelection?: (id: string) => void
  onSelectAll?: () => void
  selectionState?: 'none' | 'some' | 'all'
}

export const InjectTable = ({
  injects,
  approvalEnabled,
  selectedIds = [],
  onToggleSelection,
  onSelectAll,
  selectionState = 'none',
}: InjectTableProps) => {
  return (
    <TableContainer>
      <Table>
        <TableHead>
          <TableRow>
            {approvalEnabled && onToggleSelection && (
              <TableCell padding="checkbox" sx={{ width: 48 }}>
                <Checkbox
                  indeterminate={selectionState === 'some'}
                  checked={selectionState === 'all'}
                  onChange={onSelectAll}
                  inputProps={{ 'aria-label': 'Select all injects' }}
                />
              </TableCell>
            )}
            <TableCell>#</TableCell>
            <TableCell>Title</TableCell>
            <TableCell>Scheduled</TableCell>
            <TableCell>Status</TableCell>
            <TableCell>Actions</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {injects.map((inject) => (
            <TableRow key={inject.id} hover>
              {approvalEnabled && onToggleSelection && (
                <TableCell padding="checkbox">
                  <Checkbox
                    checked={selectedIds.includes(inject.id)}
                    onChange={() => onToggleSelection(inject.id)}
                    inputProps={{ 'aria-label': `Select inject ${inject.injectNumber}` }}
                  />
                </TableCell>
              )}
              <TableCell>{inject.injectNumber}</TableCell>
              <TableCell>{inject.title}</TableCell>
              <TableCell>{formatScheduledTime(inject.scheduledTime)}</TableCell>
              <TableCell>
                <InjectStatusChip status={inject.status} />
              </TableCell>
              <TableCell>
                {/* Existing action buttons */}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  )
}
```

## Test Cases

### Unit Tests (Frontend)

```typescript
// File: src/frontend/src/features/injects/hooks/useInjectSelection.test.ts

describe('useInjectSelection', () => {
  it('starts with no selection', () => {
    const { result } = renderHook(() => useInjectSelection({ injects: mockInjects }))
    expect(result.current.selectedIds).toHaveLength(0)
    expect(result.current.selectionState).toBe('none')
  })

  it('toggles individual inject selection', () => {
    const { result } = renderHook(() => useInjectSelection({ injects: mockInjects }))

    act(() => result.current.toggleSelection('inject-1'))
    expect(result.current.selectedIds).toContain('inject-1')
    expect(result.current.selectionState).toBe('some')

    act(() => result.current.toggleSelection('inject-1'))
    expect(result.current.selectedIds).not.toContain('inject-1')
    expect(result.current.selectionState).toBe('none')
  })

  it('selects all injects', () => {
    const { result } = renderHook(() => useInjectSelection({ injects: mockInjects }))

    act(() => result.current.selectAll())
    expect(result.current.selectedIds).toHaveLength(mockInjects.length)
    expect(result.current.selectionState).toBe('all')
  })

  it('clears all selection', () => {
    const { result } = renderHook(() => useInjectSelection({ injects: mockInjects }))

    act(() => {
      result.current.selectAll()
      result.current.clearSelection()
    })
    expect(result.current.selectedIds).toHaveLength(0)
    expect(result.current.selectionState).toBe('none')
  })
})
```

### Component Tests

```typescript
// File: src/frontend/src/features/injects/pages/InjectListPage.test.tsx

describe('InjectListPage - Batch Selection', () => {
  it('shows checkbox column when approval is enabled', () => {
    render(<InjectListPage />, {
      initialEntries: [`/exercises/${mockExercise.id}/injects`],
    })

    expect(screen.getByLabelText('Select all injects')).toBeInTheDocument()
  })

  it('does not show checkbox column when approval is disabled', () => {
    mockUseExercise.mockReturnValue({
      data: { ...mockExercise, requireInjectApproval: false },
    })

    render(<InjectListPage />)

    expect(screen.queryByLabelText('Select all injects')).not.toBeInTheDocument()
  })

  it('shows BatchApprovalToolbar when injects are selected', async () => {
    render(<InjectListPage />)

    const checkbox = screen.getAllByRole('checkbox')[1] // First inject checkbox
    await userEvent.click(checkbox)

    expect(screen.getByText(/1 inject selected/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /approve/i })).toBeInTheDocument()
  })

  it('clears selection after batch approval', async () => {
    render(<InjectListPage />)

    // Select inject
    const checkbox = screen.getAllByRole('checkbox')[1]
    await userEvent.click(checkbox)

    // Approve
    const approveButton = screen.getByRole('button', { name: /approve/i })
    await userEvent.click(approveButton)

    // Confirm dialog
    const confirmButton = await screen.findByRole('button', { name: /approve/i })
    await userEvent.click(confirmButton)

    // Selection should be cleared
    await waitFor(() => {
      expect(screen.queryByText(/inject selected/i)).not.toBeInTheDocument()
    })
  })
})
```

## Out of Scope

- Selection persistence across page navigation (intentionally cleared)
- Keyboard shortcuts for selection (Shift+Click, Ctrl+Click)
- Export selected injects (separate feature)
- Bulk edit selected injects (separate feature)

## Definition of Done

- [ ] useInjectSelection hook created and tested
- [ ] Checkbox column added to InjectTable when approval enabled
- [ ] Header checkbox supports all/none/indeterminate states
- [ ] BatchApprovalToolbar displays when selection exists
- [ ] BatchApprovalToolbar hidden when approval disabled
- [ ] Selection cleared after successful batch action
- [ ] Selection preserved during filtering/sorting
- [ ] Responsive design for mobile (stacked buttons)
- [ ] All acceptance criteria tests passing
- [ ] Component tests for selection behavior
- [ ] Integration with existing BatchApprovalToolbar verified
- [ ] No visual regressions in MSEL view

## Related Components

| Component | Location | Purpose |
|-----------|----------|---------|
| `BatchApprovalToolbar` | `features/injects/components/` | Existing toolbar (S05) |
| `InjectListPage` | `features/injects/pages/` | MSEL view to be updated |
| `InjectTable` | `features/injects/components/` | Table to add checkboxes |
| `useInjectSelection` | `features/injects/hooks/` | New selection state hook |
