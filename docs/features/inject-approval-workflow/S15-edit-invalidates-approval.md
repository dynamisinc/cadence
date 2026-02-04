# S15: Edit Invalidates Approval

**Feature:** [Inject Approval Workflow](FEATURE.md)
**Priority:** P0
**Points:** 3
**Dependencies:** S03 (Submit for Approval), S04 (Approve or Reject Inject)

## User Story

**As a** Controller or Exercise Director,
**I want** injects to automatically revert to Draft status when edited after approval,
**So that** all content changes go through proper review and approval integrity is maintained.

## Context

When an inject has been submitted or approved, editing its content invalidates the approval because the review was for specific content. To maintain workflow integrity and ensure changes are reviewed, the system automatically reverts the status to Draft when content is edited.

This is different from S09 (manual revert by Director). This story covers automatic reversion triggered by the edit action itself, ensuring approval always reflects the current content.

**Rationale:**
- Approval was for specific content - editing invalidates that approval
- Prevents approved injects from containing unreviewed changes
- Maintains audit trail integrity
- Forces re-submission and re-approval workflow

## Acceptance Criteria

### Edit Detection and Status Reversion

- [ ] **Given** approval workflow is enabled (`RequireInjectApproval = true`) AND inject status is Submitted, **when** I edit and save the inject, **then** status automatically changes to Draft
- [ ] **Given** approval workflow is enabled AND inject status is Approved, **when** I edit and save the inject, **then** status automatically changes to Draft
- [ ] **Given** approval workflow is disabled (`RequireInjectApproval = false`), **when** I edit any inject, **then** status does NOT automatically change
- [ ] **Given** inject status is Draft, **when** I edit and save, **then** status remains Draft (no change)
- [ ] **Given** inject status is Deferred, **when** I edit and save, **then** status remains Deferred (no change)
- [ ] **Given** inject status is Released, **when** I edit only the notes field, **then** status remains Released (notes-only edits allowed per existing behavior)

### Approval Tracking Cleared

- [ ] **Given** I edit a Submitted inject, **when** saved, **then** all submission tracking is cleared: `SubmittedByUserId`, `SubmittedAt` → null
- [ ] **Given** I edit an Approved inject, **when** saved, **then** all approval tracking is cleared: `ApprovedByUserId`, `ApprovedAt`, `ApproverNotes` → null
- [ ] **Given** I edit an Approved inject that was previously rejected, **when** saved, **then** rejection tracking is also cleared: `RejectedByUserId`, `RejectedAt`, `RejectionReason` → null
- [ ] **Given** approval/submission tracking is cleared, **when** viewing inject, **then** previous approval info is not visible in main UI (only in history)

### Status History Recording

- [ ] **Given** I edit a Submitted inject, **when** saved, **then** status history records: Submitted → Draft with note "Content edited - reverted to Draft for re-approval"
- [ ] **Given** I edit an Approved inject, **when** saved, **then** status history records: Approved → Draft with note "Content edited - reverted to Draft for re-approval"
- [ ] **Given** multiple edits after submission, **when** viewing history, **then** all status changes are visible in chronological order

### Real-Time Notification

- [ ] **Given** I edit and save an inject that triggers status reversion, **when** saved, **then** SignalR broadcasts `NotifyInjectStatusChanged` event
- [ ] **Given** other users are viewing the MSEL, **when** status change is broadcast, **then** they see the inject status update to Draft in real-time
- [ ] **Given** approval queue is open, **when** inject reverts to Draft, **then** it is removed from the queue in real-time

### User Feedback

- [ ] **Given** I edit a Submitted/Approved inject, **when** I click Save, **then** I see a confirmation message: "Inject saved. Status reverted to Draft - re-submission required."
- [ ] **Given** I view an inject I previously edited, **when** status is Draft, **then** I see an indicator that it needs re-submission

### Edge Cases

- [ ] **Given** inject is Released, **when** I attempt to edit content fields (not notes), **then** I receive error: "Cannot edit released inject. Only notes can be updated."
- [ ] **Given** inject is Complete, **when** I attempt to edit, **then** I receive error: "Cannot edit completed inject"
- [ ] **Given** inject is Obsolete, **when** I attempt to edit, **then** I receive error: "Cannot edit obsolete inject"

## UI Design

### Edit Confirmation for Submitted/Approved Injects

When editing an inject in Submitted or Approved status with approval workflow enabled:

```
┌─────────────────────────────────────────────────────────────────┐
│  Edit Inject: INJ-007 - Power Outage                           │
│  ┌───────────┐                                                  │
│  │ Submitted │  Submitted by John Doe on Jan 18, 2026           │
│  └───────────┘                                                  │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ ⚠️  Editing Will Reset Approval                          │   │
│  │                                                          │   │
│  │  This inject has been submitted for approval. Any       │   │
│  │  content changes will revert it to Draft status and     │   │
│  │  require re-submission.                                  │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  Inject Number: [INJ-007                         ]             │
│  Title:         [Power Outage Alert              ]             │
│  Description:   [─────────────────────────────────]             │
│                 [                                 ]             │
│                 [                                 ]             │
│                 [─────────────────────────────────]             │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│                                        [Cancel]  [Save Changes] │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Success Message After Save

```
┌─────────────────────────────────────────────────────────────────┐
│  ✅ Inject saved. Status reverted to Draft - re-submission      │
│     required.                                         [Dismiss] │
└─────────────────────────────────────────────────────────────────┘
```

### Inject Detail After Edit (Reverted Status)

```
┌─────────────────────────────────────────────────────────────────┐
│  INJ-007: Power Outage Alert                                    │
│  ┌───────┐                                                      │
│  │ Draft │  Last edited Jan 19, 2026                            │
│  └───────┘                                                      │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ 📝 Content Updated                                       │   │
│  │                                                          │   │
│  │  You edited this inject on Jan 19, 2026.                │   │
│  │  Previous approval has been cleared.                     │   │
│  │  Submit for approval when ready.                         │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  [... inject content ...]                                       │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│  [Submit for Approval]  [Edit]                                  │
└─────────────────────────────────────────────────────────────────┘
```

## Technical Implementation

### Backend: Service Method

```csharp
// File: src/Cadence.Core/Features/Injects/Services/InjectService.cs

/// <summary>
/// Updates an inject. If approval workflow is enabled and inject is in
/// Submitted/Approved status, automatically reverts to Draft.
/// </summary>
public async Task<InjectDto> UpdateInjectAsync(
    Guid id,
    UpdateInjectRequest request,
    ClaimsPrincipal user)
{
    var inject = await _context.Injects
        .Include(i => i.Msel)
            .ThenInclude(m => m.Exercise)
        .FirstOrDefaultAsync(i => i.Id == id);

    if (inject == null)
        throw new NotFoundException("Inject not found");

    // Authorization check
    await ValidateUpdatePermissionsAsync(inject, user);

    // Check if editing Released/Complete/Obsolete
    if (inject.Status == InjectStatus.Released && !IsNotesOnlyEdit(request))
    {
        throw new ValidationException(
            "Cannot edit released inject. Only notes can be updated.");
    }

    if (inject.Status == InjectStatus.Complete)
    {
        throw new ValidationException("Cannot edit completed inject");
    }

    if (inject.Status == InjectStatus.Obsolete)
    {
        throw new ValidationException("Cannot edit obsolete inject");
    }

    var previousStatus = inject.Status;
    var requiresApproval = inject.Msel.Exercise.RequireInjectApproval;

    // Apply updates
    _mapper.Map(request, inject);
    inject.UpdatedAt = DateTime.UtcNow;

    // Automatic status reversion if approval workflow enabled
    if (requiresApproval &&
        (previousStatus == InjectStatus.Submitted ||
         previousStatus == InjectStatus.Approved))
    {
        // Revert to Draft
        inject.Status = InjectStatus.Draft;

        // Clear all approval/submission tracking
        ClearApprovalTracking(inject);

        // Record status change in history
        await RecordStatusChangeAsync(
            inject,
            previousStatus,
            InjectStatus.Draft,
            GetUserId(user),
            "Content edited - reverted to Draft for re-approval");
    }

    await _context.SaveChangesAsync();

    // Broadcast status change if it occurred
    if (inject.Status != previousStatus)
    {
        var dto = _mapper.Map<InjectDto>(inject);
        await _hubContext.NotifyInjectStatusChanged(
            inject.Msel.ExerciseId,
            dto);
    }

    return _mapper.Map<InjectDto>(inject);
}

/// <summary>
/// Clears all approval, submission, and rejection tracking fields.
/// </summary>
private void ClearApprovalTracking(Inject inject)
{
    // Clear submission tracking
    inject.SubmittedByUserId = null;
    inject.SubmittedAt = null;

    // Clear approval tracking
    inject.ApprovedByUserId = null;
    inject.ApprovedAt = null;
    inject.ApproverNotes = null;

    // Clear rejection tracking
    inject.RejectedByUserId = null;
    inject.RejectedAt = null;
    inject.RejectionReason = null;
}

/// <summary>
/// Checks if the edit only modifies notes (allowed for Released status).
/// </summary>
private bool IsNotesOnlyEdit(UpdateInjectRequest request)
{
    // Implementation: Compare only notes field changed
    // (This would need current inject state comparison)
    return false; // Placeholder
}
```

### Backend: Status History Recording

```csharp
// File: src/Cadence.Core/Features/Injects/Services/InjectService.cs

/// <summary>
/// Records a status change in the audit trail.
/// </summary>
private async Task RecordStatusChangeAsync(
    Inject inject,
    InjectStatus fromStatus,
    InjectStatus toStatus,
    Guid changedByUserId,
    string notes)
{
    var history = new InjectStatusHistory
    {
        Id = Guid.NewGuid(),
        InjectId = inject.Id,
        FromStatus = fromStatus,
        ToStatus = toStatus,
        ChangedByUserId = changedByUserId,
        ChangedAt = DateTime.UtcNow,
        Notes = notes
    };

    await _context.InjectStatusHistory.AddAsync(history);
}
```

### Frontend: Edit Warning Component

```tsx
// File: src/frontend/src/features/injects/components/EditApprovalWarning.tsx

interface EditApprovalWarningProps {
  inject: InjectDto;
  requiresApproval: boolean;
}

export const EditApprovalWarning: React.FC<EditApprovalWarningProps> = ({
  inject,
  requiresApproval,
}) => {
  // Only show warning for Submitted or Approved status when approval is required
  const shouldShowWarning =
    requiresApproval &&
    (inject.status === InjectStatus.Submitted ||
     inject.status === InjectStatus.Approved);

  if (!shouldShowWarning) {
    return null;
  }

  const statusLabel = inject.status === InjectStatus.Submitted
    ? 'submitted for approval'
    : 'approved';

  return (
    <Alert
      severity="warning"
      icon={<FontAwesomeIcon icon={faTriangleExclamation} />}
      sx={{ mb: 2 }}
    >
      <AlertTitle>Editing Will Reset Approval</AlertTitle>
      <Typography variant="body2">
        This inject has been {statusLabel}. Any content changes will revert
        it to Draft status and require re-submission.
      </Typography>
    </Alert>
  );
};
```

### Frontend: Success Notification

```tsx
// File: src/frontend/src/features/injects/hooks/useUpdateInject.ts

import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useSnackbar } from 'notistack';

export const useUpdateInject = (exerciseId: string) => {
  const queryClient = useQueryClient();
  const { enqueueSnackbar } = useSnackbar();

  return useMutation({
    mutationFn: async (data: UpdateInjectRequest) => {
      const response = await api.put(`/api/injects/${data.id}`, data);
      return response.data;
    },

    onSuccess: (data, variables) => {
      // Invalidate queries
      queryClient.invalidateQueries(['injects', exerciseId]);
      queryClient.invalidateQueries(['inject', data.id]);

      // Check if status was reverted
      const wasReverted =
        variables.originalStatus === InjectStatus.Submitted ||
        variables.originalStatus === InjectStatus.Approved;

      const message = wasReverted
        ? 'Inject saved. Status reverted to Draft - re-submission required.'
        : 'Inject updated successfully';

      enqueueSnackbar(message, {
        variant: wasReverted ? 'warning' : 'success'
      });
    },

    onError: (error: any) => {
      enqueueSnackbar(
        error.response?.data?.message || 'Failed to update inject',
        { variant: 'error' }
      );
    },
  });
};
```

### SignalR Event Broadcasting

```csharp
// File: src/Cadence.Core/Hubs/IExerciseHubContext.cs

public interface IExerciseHubContext
{
    // ... existing methods

    /// <summary>
    /// Notifies clients that an inject's status has changed.
    /// Used for approval workflow transitions and edit-triggered reversions.
    /// </summary>
    Task NotifyInjectStatusChanged(Guid exerciseId, InjectDto inject);
}
```

```csharp
// File: src/Cadence.WebApi/Hubs/ExerciseHubContext.cs

public async Task NotifyInjectStatusChanged(Guid exerciseId, InjectDto inject)
{
    await _hubContext.Clients
        .Group(exerciseId.ToString())
        .SendAsync("InjectStatusChanged", inject);
}
```

### Frontend: SignalR Event Handler

```tsx
// File: src/frontend/src/features/injects/hooks/useInjectStatusSync.ts

export const useInjectStatusSync = (exerciseId: string) => {
  const queryClient = useQueryClient();
  const { connection } = useSignalR();

  useEffect(() => {
    if (!connection) return;

    const handleStatusChanged = (inject: InjectDto) => {
      // Update inject in cache
      queryClient.setQueryData(['inject', inject.id], inject);

      // Update in list
      queryClient.invalidateQueries(['injects', exerciseId]);

      // If inject was in approval queue and reverted to Draft, remove from queue
      if (inject.status === InjectStatus.Draft) {
        queryClient.invalidateQueries(['approval-queue', exerciseId]);
      }
    };

    connection.on('InjectStatusChanged', handleStatusChanged);

    return () => {
      connection.off('InjectStatusChanged', handleStatusChanged);
    };
  }, [connection, exerciseId, queryClient]);
};
```

## Test Cases

### Backend Unit Tests

```csharp
// File: src/Cadence.Core.Tests/Features/Injects/InjectServiceTests.cs

[Fact]
public async Task UpdateInject_SubmittedStatus_RevertsToDraft()
{
    // Arrange
    var exercise = await CreateExerciseAsync(requireApproval: true);
    var inject = await CreateInjectAsync(
        exerciseId: exercise.Id,
        status: InjectStatus.Submitted);

    var updateRequest = new UpdateInjectRequest
    {
        Id = inject.Id,
        Title = "Updated Title",
        Description = "Updated Description"
    };

    // Act
    var result = await _service.UpdateInjectAsync(
        inject.Id,
        updateRequest,
        _controllerUser);

    // Assert
    Assert.Equal(InjectStatus.Draft, result.Status);
    Assert.Null(result.SubmittedByUserId);
    Assert.Null(result.SubmittedAt);
}

[Fact]
public async Task UpdateInject_ApprovedStatus_ClearsApprovalTracking()
{
    // Arrange
    var exercise = await CreateExerciseAsync(requireApproval: true);
    var inject = await CreateApprovedInjectAsync(exercise.Id);

    var updateRequest = new UpdateInjectRequest
    {
        Id = inject.Id,
        Description = "Updated description"
    };

    // Act
    var result = await _service.UpdateInjectAsync(
        inject.Id,
        updateRequest,
        _controllerUser);

    // Assert
    Assert.Equal(InjectStatus.Draft, result.Status);
    Assert.Null(result.ApprovedByUserId);
    Assert.Null(result.ApprovedAt);
    Assert.Null(result.ApproverNotes);
}

[Fact]
public async Task UpdateInject_ApprovedWithRejectionHistory_ClearsRejectionTracking()
{
    // Arrange
    var exercise = await CreateExerciseAsync(requireApproval: true);
    var inject = await CreateInjectAsync(exercise.Id);

    // Simulate previous rejection
    inject.RejectedByUserId = Guid.NewGuid();
    inject.RejectedAt = DateTime.UtcNow.AddDays(-1);
    inject.RejectionReason = "Previous rejection";
    inject.Status = InjectStatus.Approved;
    await _context.SaveChangesAsync();

    var updateRequest = new UpdateInjectRequest
    {
        Id = inject.Id,
        Description = "Updated"
    };

    // Act
    var result = await _service.UpdateInjectAsync(
        inject.Id,
        updateRequest,
        _controllerUser);

    // Assert
    Assert.Null(result.RejectedByUserId);
    Assert.Null(result.RejectedAt);
    Assert.Null(result.RejectionReason);
}

[Fact]
public async Task UpdateInject_ApprovalDisabled_DoesNotRevertStatus()
{
    // Arrange
    var exercise = await CreateExerciseAsync(requireApproval: false);
    var inject = await CreateInjectAsync(
        exerciseId: exercise.Id,
        status: InjectStatus.Approved);

    var updateRequest = new UpdateInjectRequest
    {
        Id = inject.Id,
        Description = "Updated"
    };

    // Act
    var result = await _service.UpdateInjectAsync(
        inject.Id,
        updateRequest,
        _controllerUser);

    // Assert - Status should remain Approved since workflow disabled
    Assert.Equal(InjectStatus.Approved, result.Status);
}

[Fact]
public async Task UpdateInject_DraftStatus_RemainsInDraft()
{
    // Arrange
    var exercise = await CreateExerciseAsync(requireApproval: true);
    var inject = await CreateInjectAsync(
        exerciseId: exercise.Id,
        status: InjectStatus.Draft);

    var updateRequest = new UpdateInjectRequest
    {
        Id = inject.Id,
        Description = "Updated"
    };

    // Act
    var result = await _service.UpdateInjectAsync(
        inject.Id,
        updateRequest,
        _controllerUser);

    // Assert - Draft remains Draft
    Assert.Equal(InjectStatus.Draft, result.Status);
}

[Fact]
public async Task UpdateInject_ReleasedStatus_ThrowsValidationException()
{
    // Arrange
    var inject = await CreateInjectAsync(status: InjectStatus.Released);

    var updateRequest = new UpdateInjectRequest
    {
        Id = inject.Id,
        Title = "Cannot change title", // Content edit, not notes
        Description = "Cannot change description"
    };

    // Act & Assert
    var ex = await Assert.ThrowsAsync<ValidationException>(() =>
        _service.UpdateInjectAsync(inject.Id, updateRequest, _controllerUser));

    Assert.Contains("Cannot edit released inject", ex.Message);
}

[Fact]
public async Task UpdateInject_StatusReverted_RecordsStatusHistory()
{
    // Arrange
    var exercise = await CreateExerciseAsync(requireApproval: true);
    var inject = await CreateInjectAsync(
        exerciseId: exercise.Id,
        status: InjectStatus.Approved);

    var updateRequest = new UpdateInjectRequest
    {
        Id = inject.Id,
        Description = "Updated"
    };

    // Act
    await _service.UpdateInjectAsync(inject.Id, updateRequest, _controllerUser);

    // Assert
    var history = await _context.InjectStatusHistory
        .Where(h => h.InjectId == inject.Id)
        .OrderByDescending(h => h.ChangedAt)
        .FirstAsync();

    Assert.Equal(InjectStatus.Approved, history.FromStatus);
    Assert.Equal(InjectStatus.Draft, history.ToStatus);
    Assert.Contains("Content edited - reverted to Draft", history.Notes);
}

[Fact]
public async Task UpdateInject_StatusReverted_BroadcastsSignalREvent()
{
    // Arrange
    var exercise = await CreateExerciseAsync(requireApproval: true);
    var inject = await CreateInjectAsync(
        exerciseId: exercise.Id,
        status: InjectStatus.Submitted);

    var updateRequest = new UpdateInjectRequest
    {
        Id = inject.Id,
        Description = "Updated"
    };

    // Act
    await _service.UpdateInjectAsync(inject.Id, updateRequest, _controllerUser);

    // Assert
    _mockHubContext.Verify(
        x => x.NotifyInjectStatusChanged(
            exercise.Id,
            It.Is<InjectDto>(i =>
                i.Id == inject.Id &&
                i.Status == InjectStatus.Draft)),
        Times.Once);
}
```

### Frontend Component Tests

```tsx
// File: src/frontend/src/features/injects/components/EditApprovalWarning.test.tsx

describe('EditApprovalWarning', () => {
  it('shows warning for Submitted inject when approval required', () => {
    const inject = createMockInject({ status: InjectStatus.Submitted });

    render(
      <EditApprovalWarning inject={inject} requiresApproval={true} />
    );

    expect(screen.getByText(/Editing Will Reset Approval/i)).toBeInTheDocument();
    expect(screen.getByText(/submitted for approval/i)).toBeInTheDocument();
  });

  it('shows warning for Approved inject when approval required', () => {
    const inject = createMockInject({ status: InjectStatus.Approved });

    render(
      <EditApprovalWarning inject={inject} requiresApproval={true} />
    );

    expect(screen.getByText(/Editing Will Reset Approval/i)).toBeInTheDocument();
    expect(screen.getByText(/approved/i)).toBeInTheDocument();
  });

  it('does not show warning when approval disabled', () => {
    const inject = createMockInject({ status: InjectStatus.Submitted });

    render(
      <EditApprovalWarning inject={inject} requiresApproval={false} />
    );

    expect(screen.queryByText(/Editing Will Reset Approval/i)).not.toBeInTheDocument();
  });

  it('does not show warning for Draft status', () => {
    const inject = createMockInject({ status: InjectStatus.Draft });

    render(
      <EditApprovalWarning inject={inject} requiresApproval={true} />
    );

    expect(screen.queryByText(/Editing Will Reset Approval/i)).not.toBeInTheDocument();
  });
});
```

## Out of Scope

- Selective field change detection (which fields trigger reversion)
- Grace period or undo mechanism for accidental edits
- Configurable reversion policy (e.g., allow minor edits without reversion)
- Notification to approvers when previously approved inject is edited

## Related Stories

- **S03: Submit Inject for Approval** - Sets the Submitted status that this story reverts from
- **S04: Approve or Reject Inject** - Sets the Approved status that this story reverts from
- **S09: Revert Approval Status** - Manual revert by Director (complementary, not duplicate)

## Definition of Done

- [ ] Backend: Auto-revert logic implemented in `UpdateInjectAsync`
- [ ] Backend: Clears submission, approval, and rejection tracking on revert
- [ ] Backend: Records status history with "Content edited - reverted to Draft" note
- [ ] Backend: Validates Released/Complete/Obsolete cannot be edited (content)
- [ ] Backend: SignalR broadcasts `InjectStatusChanged` event on revert
- [ ] Frontend: Warning component shows on edit form for Submitted/Approved injects
- [ ] Frontend: Success notification mentions status reversion when applicable
- [ ] Frontend: SignalR handler updates inject lists and approval queue in real-time
- [ ] Unit tests: All status transition scenarios covered
- [ ] Unit tests: Approval tracking cleared correctly
- [ ] Unit tests: Status history recorded
- [ ] Unit tests: SignalR broadcast verified
- [ ] Component tests: Warning displays correctly
- [ ] Integration test: Full edit → revert → re-submit → re-approve flow
- [ ] Documentation: Updated FEATURE.md to reference S15
