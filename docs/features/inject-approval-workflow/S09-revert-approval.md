# S09: Revert Approval Status

**Feature:** [Inject Approval Workflow](FEATURE.md)  
**Priority:** P1  
**Points:** 2  
**Dependencies:** S04 (Approve or Reject Inject)

## User Story

**As an** Exercise Director,  
**I want** to revert an approved inject back to Submitted status,  
**So that** I can request changes discovered after initial approval without losing the approval trail.

## Context

Sometimes after approving an inject, the Director realizes changes are needed (perhaps after reviewing related injects or getting feedback from SMEs). Rather than having the author re-submit from scratch, reverting allows the inject to go back for another review cycle while maintaining the audit history of the original approval.

This is different from rejection which returns to Draft. Revert keeps the inject in Submitted status, ready for immediate re-review after edits.

## Acceptance Criteria

### Revert Action Visibility
- [ ] **Given** I am Director or Admin AND inject status is Approved, **when** I view the inject, **then** I see a "Revert to Submitted" action (in menu or secondary action)
- [ ] **Given** inject status is beyond Approved (Synchronized, Released, etc.), **when** I view inject, **then** I do NOT see revert option
- [ ] **Given** inject status is Submitted or Draft, **when** I view inject, **then** I do NOT see revert option

### Revert Execution
- [ ] **Given** I click "Revert to Submitted", **when** dialog opens, **then** I must provide a reason for reverting
- [ ] **Given** I confirm revert with reason, **when** saved, **then** inject status changes from Approved to Submitted
- [ ] **Given** revert succeeds, **when** I view inject, **then** I see "Reverted by [Name] on [Date]: [Reason]" in history
- [ ] **Given** revert succeeds, **when** saved, **then** original approval info (ApprovedBy, ApprovedAt, Notes) is cleared

### Status After Revert
- [ ] **Given** inject is reverted, **when** author views it, **then** they see it needs attention with revert reason
- [ ] **Given** reverted inject, **when** author edits and saves, **then** it remains Submitted (not returned to Draft)
- [ ] **Given** reverted inject, **when** Director approves again, **then** normal approval flow applies

### Audit Trail
- [ ] **Given** I revert an inject, **when** recorded, **then** status history shows: Approved → Submitted with reason
- [ ] **Given** multiple reverts, **when** viewing history, **then** all revert actions are visible with their reasons

### Notification
- [ ] **Given** inject is reverted, **when** action completes, **then** author receives notification: "Your inject [INJ-001] approval was reverted by [Director]"
- [ ] **Given** revert notification, **when** author views it, **then** they see the revert reason

## UI Design

### Inject Detail - Approved with Revert Option

```
┌─────────────────────────────────────────────────────────────────┐
│  INJ-005: Media Inquiry                                         │
│  ┌──────────┐                                                   │
│  │ Approved │  Approved by Jane Smith on Jan 16, 2026           │
│  └──────────┘                                                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  [... inject content ...]                                       │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  [View History]  [Edit]        [⋮ More ▼]                       │
│                                 ├─ Revert to Submitted          │
│                                 └─ Mark Obsolete                │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Revert Confirmation Dialog

```
┌─────────────────────────────────────────────────────────────────┐
│  Revert Approval?                                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  You are reverting: INJ-005 - Media Inquiry                     │
│                                                                 │
│  This will:                                                     │
│  • Change status from Approved back to Submitted                │
│  • Clear the current approval record                            │
│  • Require re-approval before the inject can be scheduled       │
│  • Notify the author about the required changes                 │
│                                                                 │
│  Reason for revert (required):                                  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ After reviewing related injects, this needs to include   │   │
│  │ the secondary contact phone number for media inquiries.  │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│                              [Cancel]  [Revert Approval]        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Inject Detail - After Revert (Author View)

```
┌─────────────────────────────────────────────────────────────────┐
│  INJ-005: Media Inquiry                                         │
│  ┌───────────┐                                                  │
│  │ Submitted │  Awaiting re-approval                            │
│  └───────────┘                                                  │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ ↩️  Approval Reverted                                     │   │
│  │                                                          │   │
│  │  Jane Smith reverted approval on Jan 17, 2026            │   │
│  │                                                          │   │
│  │  Reason: "After reviewing related injects, this needs    │   │
│  │  to include the secondary contact phone number for       │   │
│  │  media inquiries."                                       │   │
│  │                                                          │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  [... inject content ...]                                       │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│  [Edit]                                 Awaiting re-approval... │
└─────────────────────────────────────────────────────────────────┘
```

## Technical Implementation

### Backend: Inject Entity (New Fields)

```csharp
// File: src/Cadence.Core/Entities/Inject.cs (additions)

/// <summary>User who reverted the approval. Null if not reverted.</summary>
public Guid? RevertedById { get; set; }
public User? RevertedBy { get; set; }

/// <summary>When approval was reverted. Null if not reverted.</summary>
public DateTime? RevertedAt { get; set; }

/// <summary>Reason for reverting approval.</summary>
public string? RevertReason { get; set; }
```

### Backend: API Endpoint

```csharp
// File: src/Cadence.Core/Controllers/InjectsController.cs

public class RevertApprovalRequest
{
    [Required]
    [MinLength(10, ErrorMessage = "Please provide a detailed reason for reverting")]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Reverts an approved inject back to Submitted status.
/// </summary>
[HttpPost("{id}/revert")]
[Authorize(Roles = "Administrator,ExerciseDirector")]
[ProducesResponseType(typeof(InjectDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<ActionResult<InjectDto>> RevertApproval(
    Guid id,
    [FromBody] RevertApprovalRequest request)
{
    var inject = await _injectService.RevertApprovalAsync(id, request.Reason, User);
    return Ok(_mapper.Map<InjectDto>(inject));
}
```

### Backend: Service Implementation

```csharp
// File: src/Cadence.Core/Services/InjectService.cs

/// <summary>
/// Reverts an approved inject back to Submitted status for re-review.
/// </summary>
/// <param name="injectId">Inject ID</param>
/// <param name="reason">Required reason for reverting</param>
/// <param name="user">Current user (must be Director or Admin)</param>
/// <returns>Updated inject</returns>
public async Task<Inject> RevertApprovalAsync(
    Guid injectId, 
    string reason, 
    ClaimsPrincipal user)
{
    var inject = await GetInjectWithExerciseAsync(injectId);
    var userId = GetUserId(user);
    
    // Validate current status
    if (inject.Status != InjectStatus.Approved)
    {
        throw new ValidationException(
            $"Cannot revert inject with status '{inject.Status}'. " +
            "Only Approved injects can be reverted.");
    }
    
    // Validate reason
    if (string.IsNullOrWhiteSpace(reason) || reason.Length < 10)
    {
        throw new ValidationException(
            "Revert reason must be at least 10 characters");
    }
    
    // Record who reverted and why
    inject.RevertedById = userId;
    inject.RevertedAt = DateTime.UtcNow;
    inject.RevertReason = reason;
    
    // Clear approval info
    inject.ApprovedById = null;
    inject.ApprovedAt = null;
    inject.ApproverNotes = null;
    
    // Return to Submitted status
    var previousStatus = inject.Status;
    inject.Status = InjectStatus.Submitted;
    
    // Re-set submission info (since it was cleared when approved)
    inject.SubmittedById = inject.CreatedById;
    inject.SubmittedAt = DateTime.UtcNow;
    
    // Record history
    await RecordStatusChangeAsync(
        inject, 
        previousStatus, 
        InjectStatus.Submitted, 
        userId, 
        $"Approval reverted: {reason}");
    
    await _context.SaveChangesAsync();
    
    // Notify author
    await _notificationService.NotifyApprovalRevertedAsync(inject);
    
    return inject;
}
```

### Frontend: Revert Action Component

```tsx
// File: src/frontend/src/components/RevertApprovalAction.tsx

interface RevertApprovalActionProps {
  inject: Inject;
  onRevert: (reason: string) => Promise<void>;
}

export const RevertApprovalAction: React.FC<RevertApprovalActionProps> = ({
  inject,
  onRevert,
}) => {
  const [showDialog, setShowDialog] = useState(false);
  const [reason, setReason] = useState('');
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  
  // Only show for Approved status
  if (inject.status !== InjectStatus.Approved) {
    return null;
  }
  
  const handleConfirm = async () => {
    if (reason.length < 10) {
      setError('Please provide a detailed reason (at least 10 characters)');
      return;
    }
    
    setIsSubmitting(true);
    try {
      await onRevert(reason);
      setShowDialog(false);
    } catch (err) {
      setError(err.message || 'Failed to revert approval');
    } finally {
      setIsSubmitting(false);
    }
  };
  
  return (
    <>
      <MenuItem onClick={() => setShowDialog(true)}>
        <ListItemIcon>
          <UndoIcon fontSize="small" />
        </ListItemIcon>
        <ListItemText>Revert to Submitted</ListItemText>
      </MenuItem>
      
      <Dialog 
        open={showDialog} 
        onClose={() => setShowDialog(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Revert Approval?</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" gutterBottom>
            You are reverting: <strong>{inject.injectNumber} - {inject.title}</strong>
          </Typography>
          
          <Alert severity="info" sx={{ my: 2 }}>
            <Typography variant="body2">
              This will:
            </Typography>
            <ul style={{ margin: 0, paddingLeft: 20 }}>
              <li>Change status from Approved back to Submitted</li>
              <li>Clear the current approval record</li>
              <li>Require re-approval before scheduling</li>
              <li>Notify the author about required changes</li>
            </ul>
          </Alert>
          
          <TextField
            fullWidth
            multiline
            rows={3}
            required
            label="Reason for revert"
            value={reason}
            onChange={(e) => {
              setReason(e.target.value);
              setError('');
            }}
            error={!!error}
            helperText={error || "The author will see this when reviewing the inject"}
            placeholder="Explain what needs to be changed..."
            inputProps={{ maxLength: 1000 }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowDialog(false)} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button
            variant="contained"
            color="warning"
            onClick={handleConfirm}
            disabled={isSubmitting || reason.length < 10}
            startIcon={<UndoIcon />}
          >
            {isSubmitting ? 'Reverting...' : 'Revert Approval'}
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
};
```

### Frontend: Revert Alert Component

```tsx
// File: src/frontend/src/components/RevertedAlert.tsx

interface RevertedAlertProps {
  inject: Inject;
}

export const RevertedAlert: React.FC<RevertedAlertProps> = ({ inject }) => {
  // Only show if inject has revert info and is in Submitted status
  if (!inject.revertReason || inject.status !== InjectStatus.Submitted) {
    return null;
  }
  
  return (
    <Alert 
      severity="warning" 
      icon={<UndoIcon />}
      sx={{ mb: 2 }}
    >
      <AlertTitle>Approval Reverted</AlertTitle>
      <Typography variant="body2">
        {inject.revertedBy?.displayName || 'A director'} reverted approval on{' '}
        {formatDate(inject.revertedAt)}
      </Typography>
      <Typography variant="body2" sx={{ mt: 1, fontStyle: 'italic' }}>
        "{inject.revertReason}"
      </Typography>
    </Alert>
  );
};
```

### Database Migration

```csharp
public partial class AddInjectRevertFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "RevertedById",
            table: "Injects",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "RevertedAt",
            table: "Injects",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "RevertReason",
            table: "Injects",
            type: "nvarchar(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Injects_RevertedById",
            table: "Injects",
            column: "RevertedById");

        migrationBuilder.AddForeignKey(
            name: "FK_Injects_Users_RevertedById",
            table: "Injects",
            column: "RevertedById",
            principalTable: "Users",
            principalColumn: "Id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Injects_Users_RevertedById",
            table: "Injects");

        migrationBuilder.DropIndex(
            name: "IX_Injects_RevertedById",
            table: "Injects");

        migrationBuilder.DropColumn(name: "RevertedById", table: "Injects");
        migrationBuilder.DropColumn(name: "RevertedAt", table: "Injects");
        migrationBuilder.DropColumn(name: "RevertReason", table: "Injects");
    }
}
```

## Test Cases

### Unit Tests

```csharp
[Fact]
public async Task RevertApproval_ApprovedInject_ReturnsToSubmitted()
{
    // Arrange
    var inject = await CreateApprovedInject();
    
    // Act
    var result = await _service.RevertApprovalAsync(
        inject.Id, 
        "Needs secondary contact info added", 
        _director);
    
    // Assert
    Assert.Equal(InjectStatus.Submitted, result.Status);
    Assert.NotNull(result.RevertedById);
    Assert.NotNull(result.RevertedAt);
    Assert.Equal("Needs secondary contact info added", result.RevertReason);
    Assert.Null(result.ApprovedById); // Cleared
}

[Fact]
public async Task RevertApproval_NotApproved_ThrowsValidationException()
{
    // Arrange
    var inject = await CreateSubmittedInject(); // Not Approved
    
    // Act & Assert
    var ex = await Assert.ThrowsAsync<ValidationException>(() =>
        _service.RevertApprovalAsync(inject.Id, "Some reason", _director));
    
    Assert.Contains("Only Approved injects can be reverted", ex.Message);
}

[Fact]
public async Task RevertApproval_ShortReason_ThrowsValidationException()
{
    // Arrange
    var inject = await CreateApprovedInject();
    
    // Act & Assert
    var ex = await Assert.ThrowsAsync<ValidationException>(() =>
        _service.RevertApprovalAsync(inject.Id, "Too short", _director));
    
    Assert.Contains("at least 10 characters", ex.Message);
}

[Fact]
public async Task RevertApproval_RecordsStatusHistory()
{
    // Arrange
    var inject = await CreateApprovedInject();
    
    // Act
    await _service.RevertApprovalAsync(inject.Id, "Need to add contact info", _director);
    
    // Assert
    var history = await _context.InjectStatusHistory
        .Where(h => h.InjectId == inject.Id)
        .OrderByDescending(h => h.ChangedAt)
        .FirstAsync();
    
    Assert.Equal(InjectStatus.Approved, history.FromStatus);
    Assert.Equal(InjectStatus.Submitted, history.ToStatus);
    Assert.Contains("Approval reverted", history.Notes);
}

[Fact]
public async Task RevertApproval_SendsNotificationToAuthor()
{
    // Arrange
    var author = await CreateUser("Controller");
    var inject = await CreateApprovedInject(createdBy: author.Id);
    
    // Act
    await _service.RevertApprovalAsync(inject.Id, "Add phone number", _director);
    
    // Assert
    _notificationService.Verify(
        x => x.NotifyApprovalRevertedAsync(It.Is<Inject>(i => i.Id == inject.Id)),
        Times.Once);
}
```

## Out of Scope

- Batch revert (reverting multiple approvals at once)
- Automatic revert after time period
- Revert from Synchronized or later statuses

## Definition of Done

- [ ] Revert endpoint implemented with authorization
- [ ] Validates only Approved injects can be reverted
- [ ] Requires reason (min 10 chars)
- [ ] Clears approval info on revert
- [ ] Records revert info (by, at, reason)
- [ ] Status history records the revert
- [ ] Notification sent to author
- [ ] Revert action in inject menu (for Approved only)
- [ ] Confirmation dialog with reason field
- [ ] Alert shows revert info to author
- [ ] Database migration for new fields
- [ ] Unit tests for all scenarios
- [ ] Frontend component tests
