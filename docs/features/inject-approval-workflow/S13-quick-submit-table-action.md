# S13: Quick Submit Action in MSEL Table Row

**Feature:** [Inject Approval Workflow](FEATURE.md)
**Priority:** P1
**Points:** 2
**Dependencies:** S03 (Submit Inject for Approval)

## User Story

**As a** Controller,
**I want** to submit a Draft inject for approval directly from the MSEL table row,
**So that** I can quickly submit injects without navigating to the detail page.

## Context

Currently, Controllers must click into the InjectDetailPage to access the SubmitForApprovalButton. This adds friction when preparing multiple injects for review. A quick-action "Submit" button in the table row's Actions column provides faster workflow, especially when preparing a large MSEL before exercise conduct.

The SubmitForApprovalButton component already exists and handles all submission logic. This story adds it to the table row actions, providing an alternative submission path while maintaining the existing detail page option.

## Acceptance Criteria

### Submit Button Visibility in Table Row
- [ ] **Given** approval workflow is enabled AND inject status is Draft, **when** I view the inject row in the MSEL table, **then** I see a "Submit" button in the Actions column
- [ ] **Given** approval workflow is enabled AND inject status is NOT Draft (e.g., Submitted, Approved), **when** I view the inject row, **then** I do NOT see "Submit" button in Actions
- [ ] **Given** approval workflow is DISABLED, **when** I view any inject row, **then** I do NOT see "Submit" button in Actions
- [ ] **Given** I am an Evaluator or Observer role, **when** I view a Draft inject row, **then** I do NOT see "Submit" button (permission check)

### Submit Action Execution from Row
- [ ] **Given** I see a "Submit" button in a Draft inject row, **when** I click it, **then** the inject status changes to Submitted without navigating away
- [ ] **Given** I click "Submit" in a row, **when** submission succeeds, **then** I see a success toast notification
- [ ] **Given** I click "Submit" in a row, **when** submission succeeds, **then** the row updates to show "Submitted" status and the Submit button is removed
- [ ] **Given** I click "Submit" in a row, **when** submission fails (validation error), **then** I see an error toast with specific reason

### Button Styling and UX
- [ ] **Given** a Submit button in a table row, **when** I view it, **then** it uses small size to fit in Actions column
- [ ] **Given** a Submit button in a table row, **when** I view it, **then** it shows a paper plane icon (FontAwesome `faPaperPlane`)
- [ ] **Given** I click Submit, **when** submission is in progress, **then** the button shows a spinner icon and is disabled
- [ ] **Given** multiple Draft injects exist, **when** I submit one, **then** other Submit buttons remain enabled (independent operations)

### Tooltip Guidance
- [ ] **Given** I hover over a Submit button in a row, **when** tooltip appears, **then** it shows "Submit this inject for director approval"
- [ ] **Given** inject has validation errors (missing required fields), **when** I hover over Submit button, **then** tooltip explains why submission will fail

### Responsive Design
- [ ] **Given** I view the MSEL on mobile, **when** screen is narrow, **then** Submit button shows icon-only (no text) to save space
- [ ] **Given** I view the MSEL on desktop, **when** screen is wide, **then** Submit button shows both icon and text "Submit"

## UI Design

### Desktop View - MSEL Table Row with Submit Button

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Hurricane Response TTX - MSEL                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│ # │ Title                          │ Time  │ Status     │ Actions           │
├───┼────────────────────────────────┼───────┼────────────┼───────────────────┤
│ 1 │ Initial Weather Warning        │ 09:15 │ Draft      │ [✎] [🗑] [Submit] │
│ 2 │ EOC Activation Notice          │ 09:30 │ Submitted  │ [✎] [🗑]          │
│ 3 │ Shelter Capacity Report        │ 09:45 │ Approved   │ [✎] [🗑]          │
│ 4 │ Traffic Control Request        │ 10:00 │ Draft      │ [✎] [🗑] [Submit] │
└───┴────────────────────────────────┴───────┴────────────┴───────────────────┘

Icons: ✎ = Edit, 🗑 = Delete, Submit = Paper Plane Icon
```

### Mobile View - Compact Actions

```
┌─────────────────────────────────────────┐
│  Hurricane Response TTX - MSEL          │
├─────────────────────────────────────────┤
│ #1 Initial Weather Warning              │
│ 09:15 | Draft                           │
│ [✎] [🗑] [✈]  ← Icon-only Submit        │
├─────────────────────────────────────────┤
│ #2 EOC Activation Notice                │
│ 09:30 | Submitted                       │
│ [✎] [🗑]      ← No Submit (not Draft)   │
└─────────────────────────────────────────┘
```

### Button States

**Default (Ready to Submit):**
```
[ ✈ Submit ]  ← Primary button style, small size
```

**Submitting:**
```
[ ⟳ Submitting... ]  ← Spinner icon, disabled
```

**After Success:**
```
(Button removed, status chip shows "Submitted")
```

## Technical Implementation

### Frontend: InjectTable Row Actions Update

```typescript
// File: src/frontend/src/features/injects/components/InjectTable.tsx

interface InjectTableRowProps {
  inject: InjectDto
  exerciseId: string
  approvalEnabled: boolean
  canEdit: boolean
  canDelete: boolean
  canSubmit: boolean
  onEdit: (inject: InjectDto) => void
  onDelete: (inject: InjectDto) => void
  onSubmitted?: (inject: InjectDto) => void
}

const InjectTableRow = ({
  inject,
  exerciseId,
  approvalEnabled,
  canEdit,
  canDelete,
  canSubmit,
  onEdit,
  onDelete,
  onSubmitted,
}: InjectTableRowProps) => {
  const isMobile = useMediaQuery((theme: Theme) => theme.breakpoints.down('sm'))

  return (
    <TableRow hover>
      <TableCell>{inject.injectNumber}</TableCell>
      <TableCell>{inject.title}</TableCell>
      <TableCell>{formatScheduledTime(inject.scheduledTime)}</TableCell>
      <TableCell>
        <InjectStatusChip status={inject.status} />
      </TableCell>
      <TableCell>
        <Stack direction="row" spacing={0.5}>
          {canEdit && (
            <Tooltip title="Edit inject">
              <IconButton size="small" onClick={() => onEdit(inject)}>
                <FontAwesomeIcon icon={faPen} size="sm" />
              </IconButton>
            </Tooltip>
          )}

          {canDelete && (
            <Tooltip title="Delete inject">
              <IconButton size="small" onClick={() => onDelete(inject)}>
                <FontAwesomeIcon icon={faTrash} size="sm" />
              </IconButton>
            </Tooltip>
          )}

          {/* Quick Submit Button - NEW */}
          {approvalEnabled && canSubmit && (
            <SubmitForApprovalButton
              inject={inject}
              exerciseId={exerciseId}
              approvalEnabled={approvalEnabled}
              canSubmit={canSubmit}
              size="small"
              onSubmitted={onSubmitted}
            />
          )}
        </Stack>
      </TableCell>
    </TableRow>
  )
}
```

### SubmitForApprovalButton Enhancement (Responsive)

```typescript
// File: src/frontend/src/features/injects/components/SubmitForApprovalButton.tsx

interface SubmitForApprovalButtonProps {
  inject: InjectDto
  exerciseId: string
  approvalEnabled: boolean
  canSubmit?: boolean
  size?: 'small' | 'medium'
  variant?: 'full' | 'icon-only' | 'auto'  // NEW: responsive control
  onSubmitted?: (inject: InjectDto) => void
}

export const SubmitForApprovalButton = ({
  inject,
  exerciseId,
  approvalEnabled,
  canSubmit = true,
  size = 'small',
  variant = 'auto',  // NEW: defaults to responsive
  onSubmitted,
}: SubmitForApprovalButtonProps) => {
  const { submitForApproval, isSubmitting } = useInjectApproval(exerciseId)
  const isMobile = useMediaQuery((theme: Theme) => theme.breakpoints.down('sm'))

  // Don't render if conditions not met
  if (!approvalEnabled || inject.status !== InjectStatus.Draft || !canSubmit) {
    return null
  }

  const handleSubmit = async () => {
    try {
      const submittedInject = await submitForApproval(inject.id)
      onSubmitted?.(submittedInject)
    } catch {
      // Error handling is done in the hook
    }
  }

  // Determine display mode
  const showIconOnly = variant === 'icon-only' || (variant === 'auto' && isMobile)

  return (
    <Tooltip title="Submit this inject for director approval">
      <span>
        <CobraPrimaryButton
          size={size}
          onClick={handleSubmit}
          disabled={isSubmitting}
          startIcon={
            <FontAwesomeIcon
              icon={isSubmitting ? faSpinner : faPaperPlane}
              spin={isSubmitting}
            />
          }
          sx={{
            minWidth: showIconOnly ? 'auto' : undefined,
            whiteSpace: 'nowrap',
            px: showIconOnly ? 1 : undefined,
          }}
        >
          {showIconOnly ? null : (isSubmitting ? 'Submitting...' : 'Submit')}
        </CobraPrimaryButton>
      </span>
    </Tooltip>
  )
}
```

### Validation Error Tooltip Enhancement

```typescript
// File: src/frontend/src/features/injects/components/SubmitForApprovalButton.tsx

export const SubmitForApprovalButton = ({ ... }) => {
  const { submitForApproval, isSubmitting } = useInjectApproval(exerciseId)

  // Validate inject completeness
  const validationErrors = useMemo(() => {
    const errors: string[] = []
    if (!inject.title) errors.push('Title is required')
    if (!inject.description) errors.push('Description is required')
    if (!inject.target) errors.push('Target is required')
    if (!inject.scheduledTime) errors.push('Scheduled time is required')
    return errors
  }, [inject])

  const hasValidationErrors = validationErrors.length > 0
  const tooltipTitle = hasValidationErrors
    ? `Cannot submit: ${validationErrors.join(', ')}`
    : 'Submit this inject for director approval'

  return (
    <Tooltip title={tooltipTitle}>
      <span>
        <CobraPrimaryButton
          size={size}
          onClick={handleSubmit}
          disabled={isSubmitting || hasValidationErrors}
          // ... rest of button props
        >
          {showIconOnly ? null : 'Submit'}
        </CobraPrimaryButton>
      </span>
    </Tooltip>
  )
}
```

## Test Cases

### Component Tests

```typescript
// File: src/frontend/src/features/injects/components/InjectTable.test.tsx

describe('InjectTable - Quick Submit', () => {
  it('shows Submit button for Draft inject when approval enabled', () => {
    render(
      <InjectTable
        injects={[{ ...mockInject, status: InjectStatus.Draft }]}
        approvalEnabled={true}
        canSubmit={true}
      />
    )

    expect(screen.getByRole('button', { name: /submit/i })).toBeInTheDocument()
  })

  it('does not show Submit button when approval disabled', () => {
    render(
      <InjectTable
        injects={[{ ...mockInject, status: InjectStatus.Draft }]}
        approvalEnabled={false}
        canSubmit={true}
      />
    )

    expect(screen.queryByRole('button', { name: /submit/i })).not.toBeInTheDocument()
  })

  it('does not show Submit button for non-Draft status', () => {
    render(
      <InjectTable
        injects={[{ ...mockInject, status: InjectStatus.Submitted }]}
        approvalEnabled={true}
        canSubmit={true}
      />
    )

    expect(screen.queryByRole('button', { name: /submit/i })).not.toBeInTheDocument()
  })

  it('calls onSubmitted callback after successful submission', async () => {
    const onSubmitted = jest.fn()

    render(
      <InjectTable
        injects={[{ ...mockInject, status: InjectStatus.Draft }]}
        approvalEnabled={true}
        canSubmit={true}
        onSubmitted={onSubmitted}
      />
    )

    const submitButton = screen.getByRole('button', { name: /submit/i })
    await userEvent.click(submitButton)

    await waitFor(() => {
      expect(onSubmitted).toHaveBeenCalledWith(
        expect.objectContaining({ status: InjectStatus.Submitted })
      )
    })
  })

  it('shows validation error tooltip when inject incomplete', () => {
    render(
      <InjectTable
        injects={[{
          ...mockInject,
          status: InjectStatus.Draft,
          title: '',  // Missing required field
        }]}
        approvalEnabled={true}
        canSubmit={true}
      />
    )

    const submitButton = screen.getByRole('button', { name: /submit/i })
    expect(submitButton).toBeDisabled()

    fireEvent.mouseEnter(submitButton)
    expect(screen.getByText(/cannot submit.*title is required/i)).toBeInTheDocument()
  })
})
```

### Responsive Tests

```typescript
// File: src/frontend/src/features/injects/components/SubmitForApprovalButton.test.tsx

describe('SubmitForApprovalButton - Responsive', () => {
  it('shows icon-only on mobile', () => {
    // Mock mobile viewport
    window.matchMedia = jest.fn().mockImplementation(query => ({
      matches: query === '(max-width: 600px)',
      media: query,
      addListener: jest.fn(),
      removeListener: jest.fn(),
    }))

    render(
      <SubmitForApprovalButton
        inject={{ ...mockInject, status: InjectStatus.Draft }}
        exerciseId="test-exercise"
        approvalEnabled={true}
        variant="auto"
      />
    )

    const button = screen.getByRole('button')
    expect(button.textContent).toBe('')  // Icon only, no text
    expect(button.querySelector('svg')).toBeInTheDocument()  // Icon present
  })

  it('shows icon and text on desktop', () => {
    // Mock desktop viewport
    window.matchMedia = jest.fn().mockImplementation(query => ({
      matches: false,
      media: query,
      addListener: jest.fn(),
      removeListener: jest.fn(),
    }))

    render(
      <SubmitForApprovalButton
        inject={{ ...mockInject, status: InjectStatus.Draft }}
        exerciseId="test-exercise"
        approvalEnabled={true}
        variant="auto"
      />
    )

    const button = screen.getByRole('button', { name: /submit/i })
    expect(button.textContent).toContain('Submit')
    expect(button.querySelector('svg')).toBeInTheDocument()
  })
})
```

## Out of Scope

- Bulk submit multiple Draft injects (use batch selection instead - S12)
- Inline validation feedback in the row (use tooltip)
- Undo submission from the table row (requires separate revert story)
- Custom submission notes from table row (only available in detail page)

## Definition of Done

- [ ] SubmitForApprovalButton integrated into InjectTable row actions
- [ ] Button only visible for Draft injects when approval enabled
- [ ] Button respects user permissions (Controller+ role)
- [ ] Submission updates row status without page navigation
- [ ] Success toast notification on submission
- [ ] Error toast on validation failure
- [ ] Responsive design: icon-only on mobile, icon+text on desktop
- [ ] Validation error tooltip when inject incomplete
- [ ] Loading state during submission (spinner icon)
- [ ] Component tests for visibility conditions
- [ ] Component tests for submission flow
- [ ] Responsive behavior tests
- [ ] Visual regression testing on MSEL page
- [ ] No conflicts with existing Edit/Delete actions

## User Flow Example

**Before (Current):**
1. Controller views MSEL list
2. Clicks on inject row to open detail page
3. Finds "Submit for Approval" button
4. Clicks submit
5. Navigates back to MSEL list
6. Repeats for next inject

**After (This Story):**
1. Controller views MSEL list
2. Clicks "Submit" button directly in row
3. Row updates to show "Submitted" status
4. Continues to next inject in same view

**Time Saved:** ~5-10 seconds per inject × 50 injects = 4-8 minutes saved during MSEL preparation

## Related Components

| Component | Location | Usage |
|-----------|----------|-------|
| `SubmitForApprovalButton` | `features/injects/components/` | Existing component, enhanced for responsive |
| `InjectTable` | `features/injects/components/` | Row actions updated |
| `InjectListPage` | `features/injects/pages/` | Parent page, passes props |
| `useInjectApproval` | `features/injects/hooks/` | Existing hook for submission logic |
