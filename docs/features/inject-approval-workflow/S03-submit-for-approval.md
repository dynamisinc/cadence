# S03: Submit Inject for Approval

**Feature:** [Inject Approval Workflow](FEATURE.md)  
**Priority:** P0  
**Points:** 3  
**Dependencies:** S00 (HSEEP Status Enum)

## User Story

**As a** Controller,  
**I want** to submit my drafted inject for approval,  
**So that** the Exercise Director can review it before the exercise.

## Context

Controllers author injects but shouldn't be able to approve their own work. Submission moves the inject into a review queue visible to Directors and Admins. This implements separation of duties required by many compliance frameworks.

## Acceptance Criteria

### Submit Action Visibility
- [ ] **Given** approval workflow is enabled AND inject status is Draft, **when** I view the inject detail, **then** I see a "Submit for Approval" button
- [ ] **Given** approval workflow is enabled AND inject status is Draft, **when** I view the inject row in MSEL list, **then** I see a submit icon/button in the actions column
- [ ] **Given** approval workflow is DISABLED, **when** I view a Draft inject, **then** I do NOT see "Submit for Approval" button
- [ ] **Given** inject status is NOT Draft (e.g., Approved, Released), **when** I view the inject, **then** I do NOT see "Submit for Approval" button

### Submit Action Execution
- [ ] **Given** I am Controller+ role and inject is Draft, **when** I click "Submit for Approval", **then** inject status changes to "Submitted"
- [ ] **Given** I submit an inject, **when** submission succeeds, **then** `SubmittedById` is set to my user ID
- [ ] **Given** I submit an inject, **when** submission succeeds, **then** `SubmittedAt` is set to current UTC timestamp
- [ ] **Given** I submit an inject, **when** submission succeeds, **then** I see success toast "Inject submitted for approval"

### Status Display After Submit
- [ ] **Given** inject status is Submitted, **when** I view the inject, **then** I see "Submitted" status chip (amber/yellow)
- [ ] **Given** inject status is Submitted, **when** I view the inject detail, **then** I see "Submitted by [Name] on [Date]" info
- [ ] **Given** inject status is Submitted, **when** I view the MSEL list, **then** the row shows "Submitted" status

### Editing Submitted Injects
- [ ] **Given** inject is Submitted, **when** I click Edit, **then** I see warning: "Editing will return this inject to Draft status and require re-approval"
- [ ] **Given** I see the edit warning, **when** I click "Edit Anyway", **then** the edit form opens
- [ ] **Given** I edit a Submitted inject, **when** I save changes, **then** status returns to Draft
- [ ] **Given** I edit a Submitted inject, **when** status returns to Draft, **then** previous rejection reason (if any) is cleared

### Rejection Feedback Visibility
- [ ] **Given** inject was previously rejected (has RejectionReason), **when** I view the Draft inject, **then** I see the rejection reason in an alert
- [ ] **Given** I see rejection feedback, **when** I resubmit the inject, **then** rejection reason is cleared after new submission

### Permission Enforcement
- [ ] **Given** I am an Evaluator or Observer, **when** I view a Draft inject, **then** I do NOT see "Submit for Approval" button
- [ ] **Given** I am Controller, **when** I try to submit an inject I cannot edit, **then** submission is blocked with appropriate error

### Validation
- [ ] **Given** inject has validation errors (e.g., missing required fields), **when** I try to submit, **then** I see validation errors and submission is blocked
- [ ] **Given** inject passes validation, **when** I submit, **then** submission succeeds

## UI Design

### Inject Detail - Draft with Submit Button

```
┌─────────────────────────────────────────────────────────────────┐
│  INJ-001: Initial Weather Warning                               │
│  ┌─────────┐                                                    │
│  │  Draft  │                                                    │
│  └─────────┘                                                    │
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
│  [Edit]  [Delete]                    [Submit for Approval]      │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Inject Detail - Draft with Previous Rejection

```
┌─────────────────────────────────────────────────────────────────┐
│  INJ-001: Initial Weather Warning                               │
│  ┌─────────┐                                                    │
│  │  Draft  │                                                    │
│  └─────────┘                                                    │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ ⚠️  Previously Rejected                                  │   │
│  │                                                          │   │
│  │  Rejected by Jane Smith on Jan 15, 2026                  │   │
│  │                                                          │   │
│  │  Reason: "Expected action needs more detail. What        │   │
│  │  specific departments should be notified?"               │   │
│  │                                                          │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  [... inject content ...]                                       │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│  [Edit]  [Delete]                    [Submit for Approval]      │
└─────────────────────────────────────────────────────────────────┘
```

### Inject Detail - Submitted Status

```
┌─────────────────────────────────────────────────────────────────┐
│  INJ-001: Initial Weather Warning                               │
│  ┌───────────┐                                                  │
│  │ Submitted │  Submitted by John Doe on Jan 15, 2026           │
│  └───────────┘                                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  [... inject content ...]                                       │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│  [Edit]  [Delete]                    Awaiting approval...       │
└─────────────────────────────────────────────────────────────────┘
```

### Edit Warning Dialog

```
┌─────────────────────────────────────────────────────────────────┐
│  Edit Submitted Inject?                                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ⚠️  This inject has been submitted for approval.               │
│                                                                 │
│  Editing will:                                                  │
│  • Return the inject to Draft status                            │
│  • Require re-submission for approval                           │
│  • Clear any previous rejection feedback                        │
│                                                                 │
│                              [Cancel]  [Edit Anyway]            │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Technical Implementation

### Backend: Submit Endpoint

```csharp
// File: src/Cadence.Core/Controllers/InjectsController.cs

/// <summary>
/// Submits an inject for approval.
/// Changes status from Draft to Submitted.
/// </summary>
/// <param name="id">Inject ID</param>
/// <returns>Updated inject</returns>
[HttpPost("{id}/submit")]
[Authorize(Roles = "Administrator,ExerciseDirector,Controller")]
[ProducesResponseType(typeof(InjectDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<InjectDto>> SubmitForApproval(Guid id)
{
    var inject = await _injectService.SubmitForApprovalAsync(id, User);
    return Ok(_mapper.Map<InjectDto>(inject));
}
```

### Backend: Service Implementation

```csharp
// File: src/Cadence.Core/Services/InjectService.cs

/// <summary>
/// Submits an inject for approval, changing status from Draft to Submitted.
/// </summary>
/// <param name="injectId">Inject ID</param>
/// <param name="user">Current user</param>
/// <returns>Updated inject</returns>
/// <exception cref="NotFoundException">Inject not found</exception>
/// <exception cref="ValidationException">Invalid status transition or approval not enabled</exception>
public async Task<Inject> SubmitForApprovalAsync(Guid injectId, ClaimsPrincipal user)
{
    var inject = await _context.Injects
        .Include(i => i.Msel)
            .ThenInclude(m => m.Exercise)
        .FirstOrDefaultAsync(i => i.Id == injectId)
        ?? throw new NotFoundException("Inject not found");
    
    // Validate approval workflow is enabled
    if (!inject.Msel.Exercise.RequireInjectApproval)
    {
        throw new ValidationException(
            "Cannot submit for approval - approval workflow is not enabled for this exercise");
    }
    
    // Validate current status
    if (inject.Status != InjectStatus.Draft)
    {
        throw new ValidationException(
            $"Cannot submit inject with status '{inject.Status}'. Only Draft injects can be submitted.");
    }
    
    // Validate inject is complete (required fields)
    ValidateInjectForSubmission(inject);
    
    // Update status
    var userId = GetUserId(user);
    inject.Status = InjectStatus.Submitted;
    inject.SubmittedById = userId;
    inject.SubmittedAt = DateTime.UtcNow;
    
    // Clear any previous rejection
    inject.RejectionReason = null;
    inject.RejectedById = null;
    inject.RejectedAt = null;
    
    // Record status history
    await RecordStatusChangeAsync(inject, InjectStatus.Draft, InjectStatus.Submitted, userId);
    
    await _context.SaveChangesAsync();
    
    // Queue notification for approvers
    await _notificationService.NotifyInjectSubmittedAsync(inject);
    
    return inject;
}

private void ValidateInjectForSubmission(Inject inject)
{
    var errors = new List<string>();
    
    if (string.IsNullOrWhiteSpace(inject.Title))
        errors.Add("Title is required");
    
    if (string.IsNullOrWhiteSpace(inject.MessageScript))
        errors.Add("Message script is required");
    
    if (string.IsNullOrWhiteSpace(inject.From))
        errors.Add("From field is required");
    
    if (string.IsNullOrWhiteSpace(inject.To))
        errors.Add("To field is required");
    
    if (inject.ScheduledTime == default)
        errors.Add("Scheduled time is required");
    
    if (errors.Any())
    {
        throw new ValidationException(
            $"Cannot submit inject - missing required fields: {string.Join(", ", errors)}");
    }
}
```

### Backend: Edit Handling (Return to Draft)

```csharp
// File: src/Cadence.Core/Services/InjectService.cs

/// <summary>
/// Updates an inject. If inject is Submitted, returns it to Draft.
/// </summary>
public async Task<Inject> UpdateInjectAsync(
    Guid injectId, 
    UpdateInjectRequest request, 
    ClaimsPrincipal user)
{
    var inject = await GetInjectWithExerciseAsync(injectId);
    var userId = GetUserId(user);
    
    // If submitted, return to draft
    if (inject.Status == InjectStatus.Submitted)
    {
        await RecordStatusChangeAsync(
            inject, 
            InjectStatus.Submitted, 
            InjectStatus.Draft, 
            userId,
            "Returned to draft due to edit");
        
        inject.Status = InjectStatus.Draft;
        inject.RejectionReason = null;
        inject.RejectedById = null;
        inject.RejectedAt = null;
    }
    
    // Apply updates
    _mapper.Map(request, inject);
    inject.UpdatedAt = DateTime.UtcNow;
    inject.UpdatedById = userId;
    
    await _context.SaveChangesAsync();
    
    return inject;
}
```

### Frontend: Submit Button Component

```tsx
// File: src/frontend/src/components/SubmitForApprovalButton.tsx

interface SubmitForApprovalButtonProps {
  inject: Inject;
  exerciseRequiresApproval: boolean;
  onSubmit: () => Promise<void>;
}

export const SubmitForApprovalButton: React.FC<SubmitForApprovalButtonProps> = ({
  inject,
  exerciseRequiresApproval,
  onSubmit,
}) => {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const { showToast } = useToast();
  
  // Only show for Draft injects when approval is enabled
  if (!exerciseRequiresApproval || inject.status !== InjectStatus.Draft) {
    return null;
  }
  
  const handleSubmit = async () => {
    setIsSubmitting(true);
    try {
      await onSubmit();
      showToast('Inject submitted for approval', 'success');
    } catch (error) {
      showToast(error.message || 'Failed to submit inject', 'error');
    } finally {
      setIsSubmitting(false);
    }
  };
  
  return (
    <Button
      variant="contained"
      color="primary"
      onClick={handleSubmit}
      disabled={isSubmitting}
      startIcon={isSubmitting ? <CircularProgress size={20} /> : <SendIcon />}
    >
      {isSubmitting ? 'Submitting...' : 'Submit for Approval'}
    </Button>
  );
};
```

### Frontend: Rejection Alert Component

```tsx
// File: src/frontend/src/components/RejectionAlert.tsx

interface RejectionAlertProps {
  inject: Inject;
}

export const RejectionAlert: React.FC<RejectionAlertProps> = ({ inject }) => {
  if (!inject.rejectionReason || inject.status !== InjectStatus.Draft) {
    return null;
  }
  
  return (
    <Alert severity="warning" sx={{ mb: 2 }}>
      <AlertTitle>Previously Rejected</AlertTitle>
      <Typography variant="body2">
        Rejected by {inject.rejectedBy?.displayName || 'Unknown'} on{' '}
        {formatDate(inject.rejectedAt)}
      </Typography>
      <Typography variant="body2" sx={{ mt: 1, fontStyle: 'italic' }}>
        "{inject.rejectionReason}"
      </Typography>
    </Alert>
  );
};
```

## Test Cases

### Unit Tests

```csharp
[Fact]
public async Task SubmitForApproval_DraftInject_ChangesToSubmitted()
{
    // Arrange
    var inject = await CreateDraftInjectWithApprovalEnabled();
    
    // Act
    var result = await _service.SubmitForApprovalAsync(inject.Id, _testUser);
    
    // Assert
    Assert.Equal(InjectStatus.Submitted, result.Status);
    Assert.NotNull(result.SubmittedById);
    Assert.NotNull(result.SubmittedAt);
}

[Fact]
public async Task SubmitForApproval_ApprovalDisabled_ThrowsValidationException()
{
    // Arrange
    var inject = await CreateDraftInjectWithApprovalDisabled();
    
    // Act & Assert
    var ex = await Assert.ThrowsAsync<ValidationException>(() =>
        _service.SubmitForApprovalAsync(inject.Id, _testUser));
    
    Assert.Contains("approval workflow is not enabled", ex.Message);
}

[Fact]
public async Task SubmitForApproval_AlreadySubmitted_ThrowsValidationException()
{
    // Arrange
    var inject = await CreateSubmittedInject();
    
    // Act & Assert
    var ex = await Assert.ThrowsAsync<ValidationException>(() =>
        _service.SubmitForApprovalAsync(inject.Id, _testUser));
    
    Assert.Contains("Only Draft injects can be submitted", ex.Message);
}

[Fact]
public async Task SubmitForApproval_MissingRequiredFields_ThrowsValidationException()
{
    // Arrange
    var inject = await CreateDraftInjectWithApprovalEnabled();
    inject.MessageScript = null; // Missing required field
    await _context.SaveChangesAsync();
    
    // Act & Assert
    var ex = await Assert.ThrowsAsync<ValidationException>(() =>
        _service.SubmitForApprovalAsync(inject.Id, _testUser));
    
    Assert.Contains("Message script is required", ex.Message);
}

[Fact]
public async Task UpdateInject_SubmittedInject_ReturnsToDraft()
{
    // Arrange
    var inject = await CreateSubmittedInject();
    var request = new UpdateInjectRequest { Title = "Updated Title" };
    
    // Act
    var result = await _service.UpdateInjectAsync(inject.Id, request, _testUser);
    
    // Assert
    Assert.Equal(InjectStatus.Draft, result.Status);
}
```

### Frontend Tests

```typescript
describe('SubmitForApprovalButton', () => {
  it('renders when inject is Draft and approval is enabled', () => {
    render(
      <SubmitForApprovalButton
        inject={{ ...mockInject, status: InjectStatus.Draft }}
        exerciseRequiresApproval={true}
        onSubmit={jest.fn()}
      />
    );
    
    expect(screen.getByText('Submit for Approval')).toBeInTheDocument();
  });
  
  it('does not render when approval is disabled', () => {
    render(
      <SubmitForApprovalButton
        inject={{ ...mockInject, status: InjectStatus.Draft }}
        exerciseRequiresApproval={false}
        onSubmit={jest.fn()}
      />
    );
    
    expect(screen.queryByText('Submit for Approval')).not.toBeInTheDocument();
  });
  
  it('does not render for non-Draft status', () => {
    render(
      <SubmitForApprovalButton
        inject={{ ...mockInject, status: InjectStatus.Approved }}
        exerciseRequiresApproval={true}
        onSubmit={jest.fn()}
      />
    );
    
    expect(screen.queryByText('Submit for Approval')).not.toBeInTheDocument();
  });
});
```

## Out of Scope

- Batch submit multiple injects (could be future enhancement)
- Approval workflow notifications (S08)
- Approval/rejection actions (S04)

## Definition of Done

- [ ] Submit endpoint created with proper authorization
- [ ] Service validates approval is enabled
- [ ] Service validates inject is in Draft status
- [ ] Service validates required fields
- [ ] Status changes to Submitted with user/timestamp
- [ ] Previous rejection cleared on new submission
- [ ] Edit warning dialog implemented
- [ ] Editing Submitted inject returns to Draft
- [ ] Status history recorded for audit
- [ ] Rejection alert component shows previous feedback
- [ ] Submit button only visible when appropriate
- [ ] Success/error toasts implemented
- [ ] Unit tests for all scenarios
- [ ] Frontend component tests
