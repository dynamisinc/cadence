# S04: Approve or Reject Inject

**Feature:** [Inject Approval Workflow](FEATURE.md)  
**Priority:** P0  
**Points:** 5  
**Dependencies:** S03 (Submit Inject for Approval)

## User Story

**As an** Exercise Director,  
**I want** to approve or reject submitted injects,  
**So that** I can ensure MSEL quality before exercise conduct.

## Context

Directors review submitted injects and either approve them for use or reject them back to Draft with feedback. This implements the formal review gate required by HSEEP and organizational governance policies. Approvers can add review notes even when approving to provide guidance to Controllers.

## Acceptance Criteria

### Approve Action
- [ ] **Given** I am Director or Administrator AND inject is Submitted, **when** I view the inject, **then** I see "Approve" and "Reject" buttons
- [ ] **Given** I click "Approve", **when** confirmation dialog opens, **then** I see optional "Review Notes" field
- [ ] **Given** I confirm approval, **when** saved, **then** inject status changes to Approved
- [ ] **Given** I approve an inject, **when** saved, **then** `ApprovedById` is set to my user ID
- [ ] **Given** I approve an inject, **when** saved, **then** `ApprovedAt` is set to current UTC timestamp
- [ ] **Given** I entered review notes, **when** saved, **then** `ApproverNotes` is set to my notes
- [ ] **Given** approval succeeds, **when** complete, **then** I see success toast "Inject approved"

### Reject Action
- [ ] **Given** I click "Reject", **when** dialog opens, **then** I see required "Rejection Reason" field
- [ ] **Given** rejection reason is empty, **when** I try to confirm, **then** I see validation error
- [ ] **Given** I enter rejection reason and confirm, **when** saved, **then** inject status changes to Draft
- [ ] **Given** I reject an inject, **when** saved, **then** `RejectedById`, `RejectedAt`, and `RejectionReason` are set
- [ ] **Given** rejection succeeds, **when** complete, **then** I see success toast "Inject rejected - returned to author"

### Self-Approval Prevention
- [ ] **Given** I am the inject author (SubmittedById matches my user ID), **when** I try to approve, **then** I see error "Cannot approve your own submission"
- [ ] **Given** I am the inject author, **when** I view the inject, **then** I see "Approve" button disabled with tooltip explaining self-approval restriction
- [ ] **Given** I am the inject author, **when** I view the inject, **then** I CAN still reject my own inject (to withdraw it)

### Status Display
- [ ] **Given** inject is Approved, **when** I view it, **then** I see "Approved" status chip (green)
- [ ] **Given** inject is Approved, **when** I view detail, **then** I see "Approved by [Name] on [Date]"
- [ ] **Given** inject has approver notes, **when** I view detail, **then** I see the notes displayed

### Permission Enforcement
- [ ] **Given** I am Controller role only, **when** I view a Submitted inject, **then** I do NOT see Approve/Reject buttons
- [ ] **Given** I am Evaluator or Observer, **when** I view a Submitted inject, **then** I do NOT see Approve/Reject buttons
- [ ] **Given** I try to call approve endpoint without Director/Admin role, **when** request sent, **then** I get 403 Forbidden

### Audit Trail
- [ ] **Given** I approve or reject an inject, **when** action completes, **then** status change is recorded in InjectStatusHistory
- [ ] **Given** status history entry, **when** viewed, **then** it includes: from status, to status, user ID, timestamp, notes/reason

## UI Design

### Inject Detail - Submitted (Approver View)

```
┌─────────────────────────────────────────────────────────────────┐
│  INJ-001: Initial Weather Warning                               │
│  ┌───────────┐                                                  │
│  │ Submitted │  Submitted by John Doe on Jan 15, 2026           │
│  └───────────┘                                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Scheduled Time: 09:15      From: NWS                           │
│  Scenario Time: Day 1 08:00  To: EOC Director                   │
│                                                                 │
│  Message Script:                                                │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ The National Weather Service has issued a Hurricane      │   │
│  │ Warning for the Metro County area...                     │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  Expected Action:                                               │
│  Activate EOC to Level 2, notify department heads               │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  [View History]              [Reject]  [Approve]                │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Approval Confirmation Dialog

```
┌─────────────────────────────────────────────────────────────────┐
│  Approve Inject?                                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  You are approving: INJ-001 - Initial Weather Warning           │
│                                                                 │
│  Review Notes (optional):                                       │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ Consider adding specific radio frequencies for comms.    │   │
│  │                                                          │   │
│  └─────────────────────────────────────────────────────────┘   │
│  Add any feedback or guidance for the Controller                │
│                                                                 │
│                              [Cancel]  [Approve]                │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Rejection Dialog

```
┌─────────────────────────────────────────────────────────────────┐
│  Reject Inject?                                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  You are rejecting: INJ-001 - Initial Weather Warning           │
│                                                                 │
│  This will return the inject to Draft status for revision.      │
│                                                                 │
│  Rejection Reason (required):                                   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ Expected action needs more detail. Please specify:       │   │
│  │ - Which department heads should be notified?             │   │
│  │ - What is the notification method (phone, email)?        │   │
│  └─────────────────────────────────────────────────────────┘   │
│  ⚠️ Reason is required                                          │
│                                                                 │
│  The author will see this feedback when revising the inject.    │
│                                                                 │
│                              [Cancel]  [Reject]                 │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Inject Detail - Approved Status

```
┌─────────────────────────────────────────────────────────────────┐
│  INJ-001: Initial Weather Warning                               │
│  ┌──────────┐                                                   │
│  │ Approved │  Approved by Jane Smith on Jan 16, 2026           │
│  └──────────┘                                                   │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ 📝 Approver Notes                                        │   │
│  │                                                          │   │
│  │ "Consider adding specific radio frequencies for comms."  │   │
│  │                                                          │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  [... inject content ...]                                       │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Technical Implementation

### Backend: API Endpoints

```csharp
// File: src/Cadence.Core/Controllers/InjectsController.cs

public class ApproveInjectRequest
{
    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class RejectInjectRequest
{
    [Required]
    [MinLength(10, ErrorMessage = "Please provide a detailed rejection reason")]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Approves a submitted inject.
/// </summary>
[HttpPost("{id}/approve")]
[Authorize(Roles = "Administrator,ExerciseDirector")]
[ProducesResponseType(typeof(InjectDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<ActionResult<InjectDto>> Approve(
    Guid id, 
    [FromBody] ApproveInjectRequest request)
{
    var inject = await _injectService.ApproveAsync(id, request.Notes, User);
    return Ok(_mapper.Map<InjectDto>(inject));
}

/// <summary>
/// Rejects a submitted inject, returning it to Draft.
/// </summary>
[HttpPost("{id}/reject")]
[Authorize(Roles = "Administrator,ExerciseDirector")]
[ProducesResponseType(typeof(InjectDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<ActionResult<InjectDto>> Reject(
    Guid id, 
    [FromBody] RejectInjectRequest request)
{
    var inject = await _injectService.RejectAsync(id, request.Reason, User);
    return Ok(_mapper.Map<InjectDto>(inject));
}
```

### Backend: Service Implementation

```csharp
// File: src/Cadence.Core/Services/InjectService.cs

/// <summary>
/// Approves a submitted inject.
/// </summary>
/// <param name="injectId">Inject ID</param>
/// <param name="notes">Optional approver notes</param>
/// <param name="user">Current user (must be Director or Admin)</param>
/// <returns>Updated inject</returns>
public async Task<Inject> ApproveAsync(Guid injectId, string? notes, ClaimsPrincipal user)
{
    var inject = await GetInjectWithExerciseAsync(injectId);
    var userId = GetUserId(user);
    
    // Validate status
    if (inject.Status != InjectStatus.Submitted)
    {
        throw new ValidationException(
            $"Cannot approve inject with status '{inject.Status}'. Only Submitted injects can be approved.");
    }
    
    // Prevent self-approval
    if (inject.SubmittedById == userId)
    {
        throw new ValidationException(
            "Cannot approve your own submission. Another Director or Administrator must approve.");
    }
    
    // Update status
    var previousStatus = inject.Status;
    inject.Status = InjectStatus.Approved;
    inject.ApprovedById = userId;
    inject.ApprovedAt = DateTime.UtcNow;
    inject.ApproverNotes = notes;
    
    // Clear any previous rejection
    inject.RejectionReason = null;
    inject.RejectedById = null;
    inject.RejectedAt = null;
    
    // Record history
    await RecordStatusChangeAsync(inject, previousStatus, InjectStatus.Approved, userId, notes);
    
    await _context.SaveChangesAsync();
    
    // Notify author
    await _notificationService.NotifyInjectApprovedAsync(inject);
    
    return inject;
}

/// <summary>
/// Rejects a submitted inject, returning it to Draft.
/// </summary>
/// <param name="injectId">Inject ID</param>
/// <param name="reason">Required rejection reason</param>
/// <param name="user">Current user (must be Director or Admin)</param>
/// <returns>Updated inject</returns>
public async Task<Inject> RejectAsync(Guid injectId, string reason, ClaimsPrincipal user)
{
    var inject = await GetInjectWithExerciseAsync(injectId);
    var userId = GetUserId(user);
    
    // Validate status
    if (inject.Status != InjectStatus.Submitted)
    {
        throw new ValidationException(
            $"Cannot reject inject with status '{inject.Status}'. Only Submitted injects can be rejected.");
    }
    
    // Validate reason
    if (string.IsNullOrWhiteSpace(reason) || reason.Length < 10)
    {
        throw new ValidationException("Rejection reason must be at least 10 characters");
    }
    
    // Update status
    var previousStatus = inject.Status;
    inject.Status = InjectStatus.Draft;
    inject.RejectedById = userId;
    inject.RejectedAt = DateTime.UtcNow;
    inject.RejectionReason = reason;
    
    // Clear submission tracking (will be re-set on resubmit)
    inject.SubmittedById = null;
    inject.SubmittedAt = null;
    
    // Record history
    await RecordStatusChangeAsync(inject, previousStatus, InjectStatus.Draft, userId, reason);
    
    await _context.SaveChangesAsync();
    
    // Notify author
    await _notificationService.NotifyInjectRejectedAsync(inject);
    
    return inject;
}

/// <summary>
/// Records a status change in the audit history.
/// </summary>
private async Task RecordStatusChangeAsync(
    Inject inject, 
    InjectStatus fromStatus, 
    InjectStatus toStatus, 
    Guid userId,
    string? notes = null)
{
    var history = new InjectStatusHistory
    {
        Id = Guid.NewGuid(),
        InjectId = inject.Id,
        FromStatus = fromStatus,
        ToStatus = toStatus,
        ChangedById = userId,
        ChangedAt = DateTime.UtcNow,
        Notes = notes
    };
    
    _context.InjectStatusHistory.Add(history);
}
```

### Frontend: Approval Actions Component

```tsx
// File: src/frontend/src/components/ApprovalActions.tsx

interface ApprovalActionsProps {
  inject: Inject;
  currentUserId: string;
  onApprove: (notes?: string) => Promise<void>;
  onReject: (reason: string) => Promise<void>;
}

export const ApprovalActions: React.FC<ApprovalActionsProps> = ({
  inject,
  currentUserId,
  onApprove,
  onReject,
}) => {
  const [showApproveDialog, setShowApproveDialog] = useState(false);
  const [showRejectDialog, setShowRejectDialog] = useState(false);
  
  // Only show for Submitted status
  if (inject.status !== InjectStatus.Submitted) {
    return null;
  }
  
  const isSelfSubmission = inject.submittedById === currentUserId;
  
  return (
    <Box display="flex" gap={1}>
      <Tooltip 
        title={isSelfSubmission ? "Cannot approve your own submission" : ""}
      >
        <span>
          <Button
            variant="contained"
            color="success"
            onClick={() => setShowApproveDialog(true)}
            disabled={isSelfSubmission}
            startIcon={<CheckIcon />}
          >
            Approve
          </Button>
        </span>
      </Tooltip>
      
      <Button
        variant="outlined"
        color="error"
        onClick={() => setShowRejectDialog(true)}
        startIcon={<CloseIcon />}
      >
        Reject
      </Button>
      
      <ApproveDialog
        open={showApproveDialog}
        inject={inject}
        onClose={() => setShowApproveDialog(false)}
        onConfirm={async (notes) => {
          await onApprove(notes);
          setShowApproveDialog(false);
        }}
      />
      
      <RejectDialog
        open={showRejectDialog}
        inject={inject}
        onClose={() => setShowRejectDialog(false)}
        onConfirm={async (reason) => {
          await onReject(reason);
          setShowRejectDialog(false);
        }}
      />
    </Box>
  );
};
```

### Frontend: Approve Dialog

```tsx
// File: src/frontend/src/components/ApproveDialog.tsx

interface ApproveDialogProps {
  open: boolean;
  inject: Inject;
  onClose: () => void;
  onConfirm: (notes?: string) => Promise<void>;
}

export const ApproveDialog: React.FC<ApproveDialogProps> = ({
  open,
  inject,
  onClose,
  onConfirm,
}) => {
  const [notes, setNotes] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  
  const handleConfirm = async () => {
    setIsSubmitting(true);
    try {
      await onConfirm(notes || undefined);
    } finally {
      setIsSubmitting(false);
    }
  };
  
  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Approve Inject?</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" gutterBottom>
          You are approving: <strong>{inject.injectNumber} - {inject.title}</strong>
        </Typography>
        
        <TextField
          fullWidth
          multiline
          rows={3}
          label="Review Notes (optional)"
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          placeholder="Add any feedback or guidance for the Controller"
          helperText="These notes will be visible to the inject author"
          sx={{ mt: 2 }}
          inputProps={{ maxLength: 1000 }}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={isSubmitting}>
          Cancel
        </Button>
        <Button
          variant="contained"
          color="success"
          onClick={handleConfirm}
          disabled={isSubmitting}
        >
          {isSubmitting ? 'Approving...' : 'Approve'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};
```

### Frontend: Reject Dialog

```tsx
// File: src/frontend/src/components/RejectDialog.tsx

interface RejectDialogProps {
  open: boolean;
  inject: Inject;
  onClose: () => void;
  onConfirm: (reason: string) => Promise<void>;
}

export const RejectDialog: React.FC<RejectDialogProps> = ({
  open,
  inject,
  onClose,
  onConfirm,
}) => {
  const [reason, setReason] = useState('');
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  
  const handleConfirm = async () => {
    if (reason.length < 10) {
      setError('Please provide a detailed rejection reason (at least 10 characters)');
      return;
    }
    
    setIsSubmitting(true);
    try {
      await onConfirm(reason);
    } finally {
      setIsSubmitting(false);
    }
  };
  
  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Reject Inject?</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" gutterBottom>
          You are rejecting: <strong>{inject.injectNumber} - {inject.title}</strong>
        </Typography>
        
        <Alert severity="info" sx={{ my: 2 }}>
          This will return the inject to Draft status for revision.
        </Alert>
        
        <TextField
          fullWidth
          multiline
          rows={4}
          required
          label="Rejection Reason"
          value={reason}
          onChange={(e) => {
            setReason(e.target.value);
            setError('');
          }}
          error={!!error}
          helperText={error || "The author will see this feedback when revising the inject"}
          placeholder="Explain what needs to be changed or improved..."
          inputProps={{ maxLength: 1000 }}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={isSubmitting}>
          Cancel
        </Button>
        <Button
          variant="contained"
          color="error"
          onClick={handleConfirm}
          disabled={isSubmitting || reason.length < 10}
        >
          {isSubmitting ? 'Rejecting...' : 'Reject'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};
```

## Test Cases

### Unit Tests

```csharp
[Fact]
public async Task Approve_SubmittedInject_ChangesToApproved()
{
    // Arrange
    var inject = await CreateSubmittedInject();
    var approver = await CreateUser("Director");
    
    // Act
    var result = await _service.ApproveAsync(inject.Id, "Looks good!", approver);
    
    // Assert
    Assert.Equal(InjectStatus.Approved, result.Status);
    Assert.Equal(approver.Id, result.ApprovedById);
    Assert.Equal("Looks good!", result.ApproverNotes);
    Assert.NotNull(result.ApprovedAt);
}

[Fact]
public async Task Approve_SelfSubmission_ThrowsValidationException()
{
    // Arrange
    var user = await CreateUser("Director");
    var inject = await CreateSubmittedInject(submittedBy: user);
    
    // Act & Assert
    var ex = await Assert.ThrowsAsync<ValidationException>(() =>
        _service.ApproveAsync(inject.Id, null, user));
    
    Assert.Contains("Cannot approve your own submission", ex.Message);
}

[Fact]
public async Task Reject_SubmittedInject_ReturnsToDraft()
{
    // Arrange
    var inject = await CreateSubmittedInject();
    var director = await CreateUser("Director");
    
    // Act
    var result = await _service.RejectAsync(inject.Id, "Needs more detail on expected actions", director);
    
    // Assert
    Assert.Equal(InjectStatus.Draft, result.Status);
    Assert.Equal("Needs more detail on expected actions", result.RejectionReason);
    Assert.Equal(director.Id, result.RejectedById);
}

[Fact]
public async Task Reject_ShortReason_ThrowsValidationException()
{
    // Arrange
    var inject = await CreateSubmittedInject();
    
    // Act & Assert
    var ex = await Assert.ThrowsAsync<ValidationException>(() =>
        _service.RejectAsync(inject.Id, "No", _testUser));
    
    Assert.Contains("at least 10 characters", ex.Message);
}

[Fact]
public async Task Approve_RecordsStatusHistory()
{
    // Arrange
    var inject = await CreateSubmittedInject();
    
    // Act
    await _service.ApproveAsync(inject.Id, "Approved!", _testUser);
    
    // Assert
    var history = await _context.InjectStatusHistory
        .Where(h => h.InjectId == inject.Id)
        .OrderByDescending(h => h.ChangedAt)
        .FirstAsync();
    
    Assert.Equal(InjectStatus.Submitted, history.FromStatus);
    Assert.Equal(InjectStatus.Approved, history.ToStatus);
}
```

## Out of Scope

- Batch approval (S05)
- Notifications (S08)
- Revert approval (S09)

## Definition of Done

- [ ] Approve endpoint implemented with authorization
- [ ] Reject endpoint implemented with authorization
- [ ] Self-approval prevention logic working
- [ ] Approver notes field supported
- [ ] Rejection reason required and validated
- [ ] Status history recorded for both actions
- [ ] Frontend approval actions component
- [ ] Approve dialog with optional notes
- [ ] Reject dialog with required reason
- [ ] Self-approval button disabled with tooltip
- [ ] Success/error toasts
- [ ] Unit tests for all scenarios
- [ ] Frontend component tests
