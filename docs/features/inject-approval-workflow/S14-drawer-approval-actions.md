# S14: Approval Actions in InjectDetailDrawer

**Feature:** [Inject Approval Workflow](FEATURE.md)
**Priority:** P2
**Points:** 3
**Dependencies:** S03 (Submit for Approval), S04 (Approve/Reject Inject)

## User Story

**As a** Controller or Exercise Director,
**I want** to submit, approve, or reject injects directly from the InjectDetailDrawer during exercise conduct,
**So that** I can manage approval workflow without leaving the conduct view.

## Context

The InjectDetailDrawer is used during exercise conduct to view inject details in a slide-out panel without navigating away from the conduct page. Currently, it only supports fire/skip/reset actions for conduct operations.

When approval workflow is enabled, users should also be able to perform approval actions (Submit/Approve/Reject) from the drawer for consistency with the InjectDetailPage. This allows Directors to review and approve injects without leaving the conduct view, and Controllers to submit last-minute injects during exercise setup.

## Acceptance Criteria

### Approval Button Visibility in Drawer
- [ ] **Given** approval workflow is enabled AND inject is Draft, **when** I open the drawer, **then** I see "Submit for Approval" button in the footer
- [ ] **Given** approval workflow is enabled AND inject is Submitted AND I am Director/Admin, **when** I open the drawer, **then** I see "Approve" and "Reject" buttons in the footer
- [ ] **Given** approval workflow is DISABLED, **when** I open the drawer, **then** I do NOT see any approval action buttons (only fire/skip/reset)
- [ ] **Given** inject is in Approved or Released status, **when** I open the drawer, **then** I do NOT see approval action buttons

### Submit Action from Drawer
- [ ] **Given** I see "Submit for Approval" button in the drawer, **when** I click it, **then** inject status changes to Submitted
- [ ] **Given** I submit an inject from drawer, **when** submission succeeds, **then** I see success toast and drawer content updates to show new status
- [ ] **Given** I submit an inject from drawer, **when** submission succeeds, **then** Submit button is replaced with approval status info (no need to close drawer)
- [ ] **Given** I submit an inject with validation errors, **when** I click Submit, **then** I see error toast with specific validation issues

### Approve/Reject Actions from Drawer
- [ ] **Given** inject is Submitted AND I am Director/Admin, **when** I click "Approve" in drawer, **then** approval dialog opens
- [ ] **Given** I confirm approval in dialog, **when** complete, **then** inject status changes to Approved and drawer shows updated status
- [ ] **Given** inject is Submitted AND I am Director/Admin, **when** I click "Reject" in drawer, **then** rejection dialog opens
- [ ] **Given** I confirm rejection in dialog, **when** complete, **then** inject status changes to Draft and drawer shows rejection reason

### Self-Approval Prevention (Consistent with S04)
- [ ] **Given** inject was submitted by me, **when** I view it in drawer as Director, **then** "Approve" button is disabled with tooltip "Cannot approve your own submission"
- [ ] **Given** inject was submitted by me, **when** I view it in drawer, **then** "Reject" button remains enabled (can reject own submission)

### Approval Status Display in Drawer
- [ ] **Given** inject is Submitted, **when** I view drawer, **then** I see "Submitted by [Name] on [Date]" in the header area
- [ ] **Given** inject is Approved, **when** I view drawer, **then** I see "Approved by [Name] on [Date]" in the header area
- [ ] **Given** inject is Approved with approver notes, **when** I view drawer content, **then** I see the notes displayed in a dedicated section
- [ ] **Given** inject is Draft with rejection reason, **when** I view drawer content, **then** I see the rejection alert prominently displayed

### Footer Layout (Multi-Action Support)
- [ ] **Given** I am a Controller viewing a Draft inject during conduct prep, **when** I open drawer, **then** I see both "Submit for Approval" and "Close" buttons
- [ ] **Given** I am a Director viewing a Submitted inject during conduct, **when** I open drawer, **then** I see "Close", "Approve", and "Reject" buttons
- [ ] **Given** I am a Controller during active conduct viewing an Approved inject, **when** I open drawer, **then** I see "Close", "Skip", and "Fire" buttons (conduct actions only)

### Drawer Remains Open After Actions
- [ ] **Given** I submit an inject from drawer, **when** submission succeeds, **then** drawer remains open showing updated status
- [ ] **Given** I approve an inject from drawer, **when** approval succeeds, **then** drawer remains open showing "Approved" status
- [ ] **Given** I reject an inject from drawer, **when** rejection succeeds, **then** drawer remains open showing "Draft" status and rejection reason

## UI Design

### Drawer Footer - Draft Inject (Controller View)

```
┌─────────────────────────────────────────────────────────────────┐
│  [Conduct Actions Area - if exercise live]                      │
│  [Close]              [Skip]  [Submit for Approval]  [Fire]     │
└─────────────────────────────────────────────────────────────────┘
```

### Drawer Footer - Submitted Inject (Director View)

```
┌─────────────────────────────────────────────────────────────────┐
│  [Close]                              [Reject]  [Approve]        │
└─────────────────────────────────────────────────────────────────┘
```

### Drawer Header - Submitted Status Display

```
┌─────────────────────────────────────────────────────────────────┐
│  #5 Media Inquiry                                          [✕]   │
│  [Submitted]  Submitted by John Doe on Jan 15, 2026 at 14:32    │
├─────────────────────────────────────────────────────────────────┤
│  [... inject content ...]                                       │
└─────────────────────────────────────────────────────────────────┘
```

### Drawer Content - Approved with Notes

```
┌─────────────────────────────────────────────────────────────────┐
│  #5 Media Inquiry                                          [✕]   │
│  [Approved]  Approved by Jane Smith on Jan 16, 2026 at 09:15    │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ 📝 Approver Notes                                        │   │
│  │                                                          │   │
│  │ "Good scenario event. Consider adding radio frequency   │   │
│  │  details for realism."                                   │   │
│  │                                                          │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  [... rest of inject content ...]                               │
└─────────────────────────────────────────────────────────────────┘
```

### Drawer Content - Draft with Rejection Reason

```
┌─────────────────────────────────────────────────────────────────┐
│  #5 Media Inquiry                                          [✕]   │
│  [Draft]                                                         │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ ⚠️  Previously Rejected                                  │   │
│  │                                                          │   │
│  │  Rejected by Jane Smith on Jan 15, 2026                  │   │
│  │                                                          │   │
│  │  Reason: "Expected action needs more detail. What        │   │
│  │  specific response is required from PIO?"                │   │
│  │                                                          │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  [... rest of inject content ...]                               │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│  [Close]                                  [Submit for Approval] │
└─────────────────────────────────────────────────────────────────┘
```

## Technical Implementation

### Frontend: InjectDetailDrawer Props Extension

```typescript
// File: src/frontend/src/features/injects/components/InjectDetailDrawer.tsx

interface InjectDetailDrawerProps {
  inject: InjectDto | null
  open: boolean
  onClose: () => void

  // Existing conduct permissions
  canControl?: boolean
  canAddObservation?: boolean

  // NEW: Approval workflow permissions
  canSubmitForApproval?: boolean
  canApprove?: boolean
  canReject?: boolean

  // Existing conduct actions
  onFire?: (injectId: string) => void
  onSkip?: (injectId: string) => void
  onReset?: (injectId: string) => void

  // NEW: Approval actions
  onSubmit?: (injectId: string) => void
  onApprove?: (injectId: string, notes?: string) => void
  onReject?: (injectId: string, reason: string) => void

  // Context
  exerciseId: string
  approvalEnabled?: boolean
  currentUserId?: string

  // ... rest of existing props
}
```

### Frontend: Approval Status Display in Header

```typescript
// File: src/frontend/src/features/injects/components/InjectDetailDrawer.tsx

export const InjectDetailDrawer = ({ inject, approvalEnabled, ... }) => {
  if (!inject) return null

  const isSubmitted = inject.status === InjectStatus.Submitted
  const isApproved = inject.status === InjectStatus.Approved
  const isDraft = inject.status === InjectStatus.Draft

  return (
    <Drawer anchor="right" open={open} onClose={onClose}>
      {/* Header */}
      <Box sx={{ p: 2, borderBottom: 1, borderColor: 'divider' }}>
        <Stack direction="row" justifyContent="space-between">
          <Box>
            <Stack direction="row" spacing={1} alignItems="center">
              <Typography variant="h6">#{inject.injectNumber}</Typography>
              <InjectTypeChip type={inject.injectType} />
              <InjectStatusChip status={inject.status} />
            </Stack>
            <Typography variant="h6" fontWeight={500}>
              {inject.title}
            </Typography>

            {/* NEW: Approval Workflow Status Info */}
            {approvalEnabled && (
              <>
                {isSubmitted && inject.submittedByName && (
                  <Typography variant="caption" color="text.secondary">
                    Submitted by {inject.submittedByName} on{' '}
                    {formatDate(inject.submittedAt)}
                  </Typography>
                )}
                {isApproved && inject.approvedByName && (
                  <Typography variant="caption" color="text.secondary">
                    Approved by {inject.approvedByName} on{' '}
                    {formatDate(inject.approvedAt)}
                  </Typography>
                )}
              </>
            )}
          </Box>
          <IconButton onClick={onClose}>
            <FontAwesomeIcon icon={faXmark} />
          </IconButton>
        </Stack>
      </Box>

      {/* Content - Scrollable */}
      <Box sx={{ flex: 1, overflow: 'auto', p: 2 }}>
        {/* NEW: Rejection Alert at Top */}
        {approvalEnabled && isDraft && inject.rejectionReason && (
          <RejectionAlert inject={inject} />
        )}

        {/* NEW: Approver Notes Section */}
        {approvalEnabled && isApproved && inject.approverNotes && (
          <Box sx={{ mb: 3 }}>
            <Typography variant="subtitle2" color="text.secondary" gutterBottom>
              <FontAwesomeIcon icon={faNoteSticky} style={{ marginRight: 8 }} />
              Approver Notes
            </Typography>
            <Typography
              variant="body2"
              sx={{
                pl: 3,
                whiteSpace: 'pre-wrap',
                fontStyle: 'italic',
                color: 'text.secondary',
                backgroundColor: 'action.hover',
                p: 1.5,
                borderRadius: 1,
              }}
            >
              {inject.approverNotes}
            </Typography>
          </Box>
        )}

        {/* Existing inject content sections */}
        {/* ... description, expected action, etc. ... */}
      </Box>

      {/* Footer - Actions */}
      <DrawerFooterActions
        inject={inject}
        approvalEnabled={approvalEnabled}
        canControl={canControl}
        canSubmitForApproval={canSubmitForApproval}
        canApprove={canApprove}
        canReject={canReject}
        currentUserId={currentUserId}
        onClose={onClose}
        onSubmit={onSubmit}
        onApprove={onApprove}
        onReject={onReject}
        onFire={onFire}
        onSkip={onSkip}
        onReset={onReset}
      />
    </Drawer>
  )
}
```

### Frontend: Drawer Footer Actions Component

```typescript
// File: src/frontend/src/features/injects/components/DrawerFooterActions.tsx

interface DrawerFooterActionsProps {
  inject: InjectDto
  approvalEnabled: boolean
  canControl: boolean
  canSubmitForApproval: boolean
  canApprove: boolean
  canReject: boolean
  currentUserId: string
  onClose: () => void
  onSubmit?: (id: string) => void
  onApprove?: (id: string) => void
  onReject?: (id: string) => void
  onFire?: (id: string) => void
  onSkip?: (id: string) => void
  onReset?: (id: string) => void
}

export const DrawerFooterActions = ({
  inject,
  approvalEnabled,
  canControl,
  canSubmitForApproval,
  canApprove,
  canReject,
  currentUserId,
  onClose,
  onSubmit,
  onApprove,
  onReject,
  onFire,
  onSkip,
  onReset,
}: DrawerFooterActionsProps) => {
  const [showApproveDialog, setShowApproveDialog] = useState(false)
  const [showRejectDialog, setShowRejectDialog] = useState(false)

  const isDraft = inject.status === InjectStatus.Draft
  const isSubmitted = inject.status === InjectStatus.Submitted
  const isApproved = inject.status === InjectStatus.Approved
  const isFired = inject.status === InjectStatus.Released
  const isSkipped = inject.status === InjectStatus.Deferred

  const isSelfSubmission = inject.submittedByUserId === currentUserId

  return (
    <Box
      sx={{
        p: 2,
        borderTop: 1,
        borderColor: 'divider',
        backgroundColor: 'background.default',
      }}
    >
      <Stack direction="row" spacing={1} justifyContent="space-between">
        {/* Left Side - Close */}
        <CobraSecondaryButton
          size="small"
          startIcon={<FontAwesomeIcon icon={faXmark} />}
          onClick={onClose}
        >
          Close
        </CobraSecondaryButton>

        {/* Right Side - Contextual Actions */}
        <Stack direction="row" spacing={1}>
          {/* Approval Workflow Actions */}
          {approvalEnabled && (
            <>
              {/* Submit for Approval (Draft only) */}
              {isDraft && canSubmitForApproval && onSubmit && (
                <CobraPrimaryButton
                  size="small"
                  startIcon={<FontAwesomeIcon icon={faPaperPlane} />}
                  onClick={() => onSubmit(inject.id)}
                >
                  Submit for Approval
                </CobraPrimaryButton>
              )}

              {/* Approve/Reject (Submitted only, Director/Admin) */}
              {isSubmitted && (
                <>
                  {canReject && onReject && (
                    <CobraSecondaryButton
                      size="small"
                      variant="outlined"
                      color="error"
                      startIcon={<FontAwesomeIcon icon={faTimes} />}
                      onClick={() => setShowRejectDialog(true)}
                    >
                      Reject
                    </CobraSecondaryButton>
                  )}

                  {canApprove && onApprove && (
                    <Tooltip
                      title={
                        isSelfSubmission
                          ? 'Cannot approve your own submission'
                          : ''
                      }
                    >
                      <span>
                        <CobraPrimaryButton
                          size="small"
                          disabled={isSelfSubmission}
                          startIcon={<FontAwesomeIcon icon={faCheck} />}
                          onClick={() => setShowApproveDialog(true)}
                        >
                          Approve
                        </CobraPrimaryButton>
                      </span>
                    </Tooltip>
                  )}
                </>
              )}
            </>
          )}

          {/* Conduct Actions (if approved/draft and control permission) */}
          {canControl && (
            <>
              {(isDraft || isApproved) && (
                <>
                  <CobraSecondaryButton
                    size="small"
                    startIcon={<FontAwesomeIcon icon={faForwardStep} />}
                    onClick={() => onSkip?.(inject.id)}
                  >
                    Skip
                  </CobraSecondaryButton>
                  <CobraPrimaryButton
                    size="small"
                    color="success"
                    startIcon={<FontAwesomeIcon icon={faPlay} />}
                    onClick={() => onFire?.(inject.id)}
                  >
                    Fire
                  </CobraPrimaryButton>
                </>
              )}
              {(isFired || isSkipped) && (
                <CobraDeleteButton
                  size="small"
                  startIcon={<FontAwesomeIcon icon={faRotateLeft} />}
                  onClick={() => onReset?.(inject.id)}
                >
                  Reset
                </CobraDeleteButton>
              )}
            </>
          )}
        </Stack>
      </Stack>

      {/* Approval Dialogs */}
      {showApproveDialog && (
        <ApproveDialog
          open={showApproveDialog}
          inject={inject}
          onConfirm={async (notes) => {
            await onApprove?.(inject.id, notes)
            setShowApproveDialog(false)
          }}
          onCancel={() => setShowApproveDialog(false)}
        />
      )}

      {showRejectDialog && (
        <RejectDialog
          open={showRejectDialog}
          inject={inject}
          onConfirm={async (reason) => {
            await onReject?.(inject.id, reason)
            setShowRejectDialog(false)
          }}
          onCancel={() => setShowRejectDialog(false)}
        />
      )}
    </Box>
  )
}
```

### Frontend: Conduct Page Integration

```typescript
// File: src/frontend/src/features/exercises/pages/ConductPage.tsx

export const ConductPage = () => {
  const { exerciseId } = useParams()
  const { data: exercise } = useExercise(exerciseId)
  const { currentUser } = useAuth()
  const { submitForApproval, approve, reject } = useInjectApproval(exerciseId)

  const [selectedInject, setSelectedInject] = useState<InjectDto | null>(null)

  const approvalEnabled = exercise?.requireInjectApproval ?? false
  const canSubmit = hasRole(currentUser, ['Controller', 'Director', 'Administrator'])
  const canApprove = hasRole(currentUser, ['Director', 'Administrator'])

  const handleSubmit = async (injectId: string) => {
    try {
      await submitForApproval(injectId)
      // Drawer stays open, content refreshes automatically
    } catch (error) {
      // Error handling in hook
    }
  }

  const handleApprove = async (injectId: string, notes?: string) => {
    try {
      await approve({ injectId, notes })
      // Drawer stays open, shows updated status
    } catch (error) {
      // Error handling in hook
    }
  }

  const handleReject = async (injectId: string, reason: string) => {
    try {
      await reject({ injectId, reason })
      // Drawer stays open, shows rejection
    } catch (error) {
      // Error handling in hook
    }
  }

  return (
    <Box>
      {/* Conduct view content */}

      <InjectDetailDrawer
        inject={selectedInject}
        open={selectedInject !== null}
        onClose={() => setSelectedInject(null)}
        exerciseId={exerciseId}
        approvalEnabled={approvalEnabled}
        currentUserId={currentUser.id}
        canControl={canFire}
        canSubmitForApproval={canSubmit}
        canApprove={canApprove}
        canReject={canApprove}  // Same permission as approve
        onSubmit={handleSubmit}
        onApprove={handleApprove}
        onReject={handleReject}
        onFire={handleFire}
        onSkip={handleSkip}
        onReset={handleReset}
      />
    </Box>
  )
}
```

## Test Cases

### Component Tests

```typescript
// File: src/frontend/src/features/injects/components/InjectDetailDrawer.test.tsx

describe('InjectDetailDrawer - Approval Actions', () => {
  it('shows Submit button for Draft inject when approval enabled', () => {
    render(
      <InjectDetailDrawer
        inject={{ ...mockInject, status: InjectStatus.Draft }}
        open={true}
        exerciseId="test"
        approvalEnabled={true}
        canSubmitForApproval={true}
        onSubmit={jest.fn()}
      />
    )

    expect(screen.getByRole('button', { name: /submit for approval/i })).toBeInTheDocument()
  })

  it('shows Approve and Reject buttons for Submitted inject', () => {
    render(
      <InjectDetailDrawer
        inject={{ ...mockInject, status: InjectStatus.Submitted }}
        open={true}
        exerciseId="test"
        approvalEnabled={true}
        canApprove={true}
        canReject={true}
        onApprove={jest.fn()}
        onReject={jest.fn()}
      />
    )

    expect(screen.getByRole('button', { name: /approve/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /reject/i })).toBeInTheDocument()
  })

  it('disables Approve button for self-submission', () => {
    render(
      <InjectDetailDrawer
        inject={{
          ...mockInject,
          status: InjectStatus.Submitted,
          submittedByUserId: 'user-123',
        }}
        open={true}
        exerciseId="test"
        approvalEnabled={true}
        currentUserId="user-123"
        canApprove={true}
        onApprove={jest.fn()}
      />
    )

    const approveButton = screen.getByRole('button', { name: /approve/i })
    expect(approveButton).toBeDisabled()
  })

  it('displays approver notes when inject is approved', () => {
    render(
      <InjectDetailDrawer
        inject={{
          ...mockInject,
          status: InjectStatus.Approved,
          approverNotes: 'Good scenario event',
        }}
        open={true}
        exerciseId="test"
        approvalEnabled={true}
      />
    )

    expect(screen.getByText(/approver notes/i)).toBeInTheDocument()
    expect(screen.getByText(/good scenario event/i)).toBeInTheDocument()
  })

  it('displays rejection alert when inject is Draft with rejection reason', () => {
    render(
      <InjectDetailDrawer
        inject={{
          ...mockInject,
          status: InjectStatus.Draft,
          rejectionReason: 'Needs more detail',
        }}
        open={true}
        exerciseId="test"
        approvalEnabled={true}
      />
    )

    expect(screen.getByText(/previously rejected/i)).toBeInTheDocument()
    expect(screen.getByText(/needs more detail/i)).toBeInTheDocument()
  })

  it('drawer remains open after successful submission', async () => {
    const onSubmit = jest.fn().mockResolvedValue(undefined)

    render(
      <InjectDetailDrawer
        inject={{ ...mockInject, status: InjectStatus.Draft }}
        open={true}
        exerciseId="test"
        approvalEnabled={true}
        canSubmitForApproval={true}
        onSubmit={onSubmit}
      />
    )

    const submitButton = screen.getByRole('button', { name: /submit for approval/i })
    await userEvent.click(submitButton)

    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalled()
    })

    // Drawer should still be open (not closed)
    expect(screen.getByRole('dialog')).toBeInTheDocument()
  })
})
```

## Out of Scope

- Batch approval from conduct view (use MSEL page instead)
- Inline editing from drawer (use detail page)
- Approval workflow configuration from drawer
- Revert approval from drawer (S09 covers this as separate action)

## Definition of Done

- [ ] InjectDetailDrawer accepts approval workflow props
- [ ] Submit button shown for Draft injects when approval enabled
- [ ] Approve/Reject buttons shown for Submitted injects
- [ ] Self-approval prevention (disabled button with tooltip)
- [ ] Approval status displayed in drawer header
- [ ] Approver notes section shown when present
- [ ] Rejection alert shown for Draft with rejection reason
- [ ] Drawer remains open after approval actions
- [ ] ApproveDialog and RejectDialog integrated
- [ ] Footer layout handles multiple action types gracefully
- [ ] Component tests for approval visibility
- [ ] Component tests for approval actions
- [ ] Component tests for status display
- [ ] Integration tests with conduct page
- [ ] Visual consistency with InjectDetailPage approval UI

## User Flow Example

**Exercise Director reviewing injects during exercise setup:**

1. Opens conduct page before going live
2. Sees MSEL with mix of Draft/Submitted injects
3. Clicks on Submitted inject in timeline
4. Drawer slides out showing inject details
5. Reviews content, clicks "Approve" button in drawer footer
6. Enters optional notes in dialog
7. Confirms approval
8. Drawer shows updated "Approved" status immediately
9. Closes drawer and moves to next inject
10. Repeats without leaving conduct view

**Time Saved:** No navigation to detail page and back (5 seconds × 20 injects = 100 seconds saved)

## Related Components

| Component | Location | Usage |
|-----------|----------|-------|
| `InjectDetailDrawer` | `features/injects/components/` | Main component to be updated |
| `DrawerFooterActions` | `features/injects/components/` | New component for footer logic |
| `ApproveDialog` | `features/injects/components/` | Existing dialog (S04) |
| `RejectDialog` | `features/injects/components/` | Existing dialog (S04) |
| `RejectionAlert` | `features/injects/components/` | Existing alert (S03) |
| `ConductPage` | `features/exercises/pages/` | Parent page integration |
| `useInjectApproval` | `features/injects/hooks/` | Existing hook for actions |
