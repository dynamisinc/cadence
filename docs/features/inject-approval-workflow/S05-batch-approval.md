# S05: Batch Approval Actions

**Feature:** [Inject Approval Workflow](FEATURE.md)  
**Priority:** P1  
**Points:** 5  
**Dependencies:** S04 (Approve or Reject Inject)

## User Story

**As an** Exercise Director,  
**I want** to approve or reject multiple injects at once,  
**So that** I can efficiently review large MSELs without clicking through each inject individually.

## Context

Large exercises may have 50+ injects requiring approval. Reviewing each individually is time-consuming. Batch actions allow Directors to select multiple injects and approve or reject them together. When batch rejecting, a single reason applies to all selected injects.

## Acceptance Criteria

### Selection UI
- [ ] **Given** I am on the MSEL view with approval enabled, **when** I view the inject list, **then** I see a checkbox column on the left
- [ ] **Given** the checkbox column, **when** I click a row checkbox, **then** that inject is selected (highlighted)
- [ ] **Given** there are Submitted injects, **when** I click the header checkbox, **then** all Submitted injects are selected
- [ ] **Given** some injects are selected, **when** I click the header checkbox, **then** all are deselected
- [ ] **Given** I select injects across multiple statuses, **when** viewed, **then** only Submitted injects show batch action options

### Batch Approval
- [ ] **Given** I have selected 1+ Submitted injects, **when** I view the toolbar, **then** I see "Approve Selected (N)" button
- [ ] **Given** I click "Approve Selected", **when** dialog opens, **then** I see count of injects to be approved
- [ ] **Given** approval dialog, **when** I view it, **then** I see optional "Review Notes" field (applies to all)
- [ ] **Given** I confirm batch approval, **when** complete, **then** all selected injects change to Approved status
- [ ] **Given** batch approval succeeds, **when** complete, **then** I see toast "N injects approved"

### Self-Submission Filtering in Batch
- [ ] **Given** some selected injects were submitted by me, **when** I click "Approve Selected", **then** dialog shows warning about skipped injects
- [ ] **Given** warning about self-submissions, **when** I confirm, **then** only non-self-submitted injects are approved
- [ ] **Given** ALL selected injects were submitted by me, **when** I click "Approve Selected", **then** I see error "Cannot approve - all selected injects were submitted by you"

### Batch Rejection
- [ ] **Given** I have selected 1+ Submitted injects, **when** I view the toolbar, **then** I see "Reject Selected (N)" button
- [ ] **Given** I click "Reject Selected", **when** dialog opens, **then** I see count and required rejection reason
- [ ] **Given** I enter reason and confirm, **when** complete, **then** all selected injects return to Draft with same rejection reason
- [ ] **Given** batch rejection succeeds, **when** complete, **then** I see toast "N injects rejected"

### Mixed Status Selection
- [ ] **Given** I select injects with mixed statuses (some Draft, some Submitted), **when** I view toolbar, **then** batch actions show count of only Submitted injects
- [ ] **Given** mixed selection with 0 Submitted injects, **when** I view toolbar, **then** batch actions are disabled with tooltip "No submitted injects selected"

### Toolbar Behavior
- [ ] **Given** no injects selected, **when** I view the toolbar, **then** batch actions are hidden or disabled
- [ ] **Given** injects selected, **when** I navigate away (e.g., open inject detail), **then** selection is preserved on return
- [ ] **Given** injects selected, **when** I apply a filter that hides selected injects, **then** selection is cleared for hidden injects

### Notifications for Batch Actions
- [ ] **Given** batch approval of 5 injects from 3 different authors, **when** complete, **then** each author receives ONE consolidated notification (not 5 individual ones)
- [ ] **Given** batch rejection, **when** complete, **then** each author receives ONE notification listing their rejected inject(s)

## UI Design

### MSEL View with Selection

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Hurricane Response TTX - MSEL                                              │
│                                                                             │
│  [Filter ▼]  [Sort ▼]  [Search...]                   3 selected             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ ☑ Select All Submitted    [Approve Selected (3)]  [Reject Selected] │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────────────────┤
│ ☐ │ # │ Title                          │ Time  │ Status     │ Actions     │
├───┼───┼────────────────────────────────┼───────┼────────────┼─────────────┤
│ ☐ │ 1 │ Initial Weather Warning        │ 09:15 │ Approved   │ [...]       │
│ ☑ │ 2 │ EOC Activation Notice          │ 09:30 │ Submitted  │ [...]       │
│ ☑ │ 3 │ Shelter Capacity Report        │ 09:45 │ Submitted  │ [...]       │
│ ☐ │ 4 │ Traffic Control Request        │ 10:00 │ Draft      │ [...]       │
│ ☑ │ 5 │ Media Inquiry                  │ 10:15 │ Submitted  │ [...]       │
│ ☐ │ 6 │ Resource Request               │ 10:30 │ Draft      │ [...]       │
└───┴───┴────────────────────────────────┴───────┴────────────┴─────────────┘
```

### Batch Approve Dialog

```
┌─────────────────────────────────────────────────────────────────┐
│  Approve 3 Injects?                                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  You are approving:                                             │
│  • INJ-002 - EOC Activation Notice                              │
│  • INJ-003 - Shelter Capacity Report                            │
│  • INJ-005 - Media Inquiry                                      │
│                                                                 │
│  Review Notes (optional):                                       │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ All injects reviewed and approved for exercise conduct.  │   │
│  └─────────────────────────────────────────────────────────┘   │
│  These notes will be added to all approved injects              │
│                                                                 │
│                              [Cancel]  [Approve All (3)]        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Batch Approve with Self-Submission Warning

```
┌─────────────────────────────────────────────────────────────────┐
│  Approve Injects                                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ⚠️  1 inject will be skipped (submitted by you)                │
│                                                                 │
│  Will be approved (2):                                          │
│  • INJ-002 - EOC Activation Notice                              │
│  • INJ-005 - Media Inquiry                                      │
│                                                                 │
│  Will be skipped (1):                                           │
│  • INJ-003 - Shelter Capacity Report (your submission)          │
│                                                                 │
│  Review Notes (optional):                                       │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                                                          │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│                              [Cancel]  [Approve 2 Injects]      │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Batch Reject Dialog

```
┌─────────────────────────────────────────────────────────────────┐
│  Reject 3 Injects?                                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  You are rejecting:                                             │
│  • INJ-002 - EOC Activation Notice                              │
│  • INJ-003 - Shelter Capacity Report                            │
│  • INJ-005 - Media Inquiry                                      │
│                                                                 │
│  All injects will be returned to Draft status.                  │
│                                                                 │
│  Rejection Reason (required):                                   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ These injects need timing adjustments - scheduled times  │   │
│  │ conflict with the evacuation phase. Please review and    │   │
│  │ adjust to fit the updated exercise timeline.             │   │
│  └─────────────────────────────────────────────────────────┘   │
│  This reason will be shown to all inject authors                │
│                                                                 │
│                              [Cancel]  [Reject All (3)]         │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Technical Implementation

### Backend: Batch Endpoints

```csharp
// File: src/Cadence.Core/Controllers/InjectsController.cs

public class BatchApproveRequest
{
    [Required]
    [MinLength(1)]
    public List<Guid> InjectIds { get; set; } = new();
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class BatchRejectRequest
{
    [Required]
    [MinLength(1)]
    public List<Guid> InjectIds { get; set; } = new();
    
    [Required]
    [MinLength(10)]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;
}

public class BatchApprovalResult
{
    public int ApprovedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> SkippedReasons { get; set; } = new();
    public List<InjectDto> ApprovedInjects { get; set; } = new();
}

/// <summary>
/// Batch approve multiple submitted injects.
/// Self-submissions are automatically skipped.
/// </summary>
[HttpPost("batch/approve")]
[Authorize(Roles = "Administrator,ExerciseDirector")]
public async Task<ActionResult<BatchApprovalResult>> BatchApprove(
    [FromBody] BatchApproveRequest request)
{
    var result = await _injectService.BatchApproveAsync(
        request.InjectIds, 
        request.Notes, 
        User);
    return Ok(result);
}

/// <summary>
/// Batch reject multiple submitted injects.
/// </summary>
[HttpPost("batch/reject")]
[Authorize(Roles = "Administrator,ExerciseDirector")]
public async Task<ActionResult<BatchApprovalResult>> BatchReject(
    [FromBody] BatchRejectRequest request)
{
    var result = await _injectService.BatchRejectAsync(
        request.InjectIds, 
        request.Reason, 
        User);
    return Ok(result);
}
```

### Backend: Service Implementation

```csharp
// File: src/Cadence.Core/Services/InjectService.cs

/// <summary>
/// Batch approve multiple injects.
/// </summary>
public async Task<BatchApprovalResult> BatchApproveAsync(
    List<Guid> injectIds, 
    string? notes, 
    ClaimsPrincipal user)
{
    var userId = GetUserId(user);
    var result = new BatchApprovalResult();
    
    var injects = await _context.Injects
        .Include(i => i.Msel).ThenInclude(m => m.Exercise)
        .Where(i => injectIds.Contains(i.Id))
        .ToListAsync();
    
    // Group by author for consolidated notifications
    var approvedByAuthor = new Dictionary<Guid, List<Inject>>();
    
    foreach (var inject in injects)
    {
        // Skip non-submitted
        if (inject.Status != InjectStatus.Submitted)
        {
            result.SkippedCount++;
            result.SkippedReasons.Add(
                $"{inject.InjectNumber}: Not in Submitted status");
            continue;
        }
        
        // Skip self-submissions
        if (inject.SubmittedById == userId)
        {
            result.SkippedCount++;
            result.SkippedReasons.Add(
                $"{inject.InjectNumber}: Cannot approve your own submission");
            continue;
        }
        
        // Approve
        inject.Status = InjectStatus.Approved;
        inject.ApprovedById = userId;
        inject.ApprovedAt = DateTime.UtcNow;
        inject.ApproverNotes = notes;
        inject.RejectionReason = null;
        inject.RejectedById = null;
        inject.RejectedAt = null;
        
        await RecordStatusChangeAsync(
            inject, InjectStatus.Submitted, InjectStatus.Approved, userId, notes);
        
        result.ApprovedCount++;
        result.ApprovedInjects.Add(_mapper.Map<InjectDto>(inject));
        
        // Track for notifications
        var authorId = inject.SubmittedById ?? inject.CreatedById;
        if (!approvedByAuthor.ContainsKey(authorId))
            approvedByAuthor[authorId] = new List<Inject>();
        approvedByAuthor[authorId].Add(inject);
    }
    
    await _context.SaveChangesAsync();
    
    // Send consolidated notifications
    foreach (var (authorId, authorInjects) in approvedByAuthor)
    {
        await _notificationService.NotifyBatchApprovedAsync(authorId, authorInjects);
    }
    
    return result;
}

/// <summary>
/// Batch reject multiple injects.
/// </summary>
public async Task<BatchApprovalResult> BatchRejectAsync(
    List<Guid> injectIds, 
    string reason, 
    ClaimsPrincipal user)
{
    var userId = GetUserId(user);
    var result = new BatchApprovalResult();
    
    var injects = await _context.Injects
        .Where(i => injectIds.Contains(i.Id))
        .ToListAsync();
    
    var rejectedByAuthor = new Dictionary<Guid, List<Inject>>();
    
    foreach (var inject in injects)
    {
        if (inject.Status != InjectStatus.Submitted)
        {
            result.SkippedCount++;
            result.SkippedReasons.Add(
                $"{inject.InjectNumber}: Not in Submitted status");
            continue;
        }
        
        inject.Status = InjectStatus.Draft;
        inject.RejectedById = userId;
        inject.RejectedAt = DateTime.UtcNow;
        inject.RejectionReason = reason;
        inject.SubmittedById = null;
        inject.SubmittedAt = null;
        
        await RecordStatusChangeAsync(
            inject, InjectStatus.Submitted, InjectStatus.Draft, userId, reason);
        
        result.ApprovedCount++; // Using same field for count
        
        var authorId = inject.CreatedById;
        if (!rejectedByAuthor.ContainsKey(authorId))
            rejectedByAuthor[authorId] = new List<Inject>();
        rejectedByAuthor[authorId].Add(inject);
    }
    
    await _context.SaveChangesAsync();
    
    // Send consolidated notifications
    foreach (var (authorId, authorInjects) in rejectedByAuthor)
    {
        await _notificationService.NotifyBatchRejectedAsync(authorId, authorInjects, reason);
    }
    
    return result;
}
```

### Frontend: Batch Selection Hook

```tsx
// File: src/frontend/src/hooks/useBatchSelection.ts

interface UseBatchSelectionOptions<T> {
  items: T[];
  getItemId: (item: T) => string;
  filterPredicate?: (item: T) => boolean;
}

export function useBatchSelection<T>({
  items,
  getItemId,
  filterPredicate = () => true,
}: UseBatchSelectionOptions<T>) {
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  
  const selectableItems = items.filter(filterPredicate);
  const selectedItems = items.filter(
    item => selectedIds.has(getItemId(item)) && filterPredicate(item)
  );
  
  const toggleItem = (item: T) => {
    const id = getItemId(item);
    setSelectedIds(prev => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  };
  
  const selectAll = () => {
    setSelectedIds(new Set(selectableItems.map(getItemId)));
  };
  
  const clearSelection = () => {
    setSelectedIds(new Set());
  };
  
  const isSelected = (item: T) => selectedIds.has(getItemId(item));
  const isAllSelected = selectedItems.length === selectableItems.length && selectableItems.length > 0;
  const isSomeSelected = selectedItems.length > 0 && !isAllSelected;
  
  return {
    selectedIds,
    selectedItems,
    selectableItems,
    toggleItem,
    selectAll,
    clearSelection,
    isSelected,
    isAllSelected,
    isSomeSelected,
  };
}
```

### Frontend: Batch Actions Toolbar

```tsx
// File: src/frontend/src/components/BatchApprovalToolbar.tsx

interface BatchApprovalToolbarProps {
  selectedInjects: Inject[];
  currentUserId: string;
  onBatchApprove: (injectIds: string[], notes?: string) => Promise<BatchApprovalResult>;
  onBatchReject: (injectIds: string[], reason: string) => Promise<BatchApprovalResult>;
  onClearSelection: () => void;
}

export const BatchApprovalToolbar: React.FC<BatchApprovalToolbarProps> = ({
  selectedInjects,
  currentUserId,
  onBatchApprove,
  onBatchReject,
  onClearSelection,
}) => {
  const [showApproveDialog, setShowApproveDialog] = useState(false);
  const [showRejectDialog, setShowRejectDialog] = useState(false);
  
  // Filter to only Submitted injects
  const submittedInjects = selectedInjects.filter(
    i => i.status === InjectStatus.Submitted
  );
  
  // Separate self-submissions
  const selfSubmissions = submittedInjects.filter(
    i => i.submittedById === currentUserId
  );
  const approvableInjects = submittedInjects.filter(
    i => i.submittedById !== currentUserId
  );
  
  if (selectedInjects.length === 0) {
    return null;
  }
  
  return (
    <Paper 
      elevation={2} 
      sx={{ p: 1, mb: 2, display: 'flex', alignItems: 'center', gap: 2 }}
    >
      <Typography variant="body2">
        {selectedInjects.length} selected
        {submittedInjects.length !== selectedInjects.length && (
          <span> ({submittedInjects.length} submitted)</span>
        )}
      </Typography>
      
      <Box flexGrow={1} />
      
      <Button
        variant="contained"
        color="success"
        onClick={() => setShowApproveDialog(true)}
        disabled={approvableInjects.length === 0}
        startIcon={<CheckIcon />}
      >
        Approve Selected ({approvableInjects.length})
      </Button>
      
      <Button
        variant="outlined"
        color="error"
        onClick={() => setShowRejectDialog(true)}
        disabled={submittedInjects.length === 0}
        startIcon={<CloseIcon />}
      >
        Reject Selected ({submittedInjects.length})
      </Button>
      
      <IconButton onClick={onClearSelection} size="small">
        <ClearIcon />
      </IconButton>
      
      <BatchApproveDialog
        open={showApproveDialog}
        approvableInjects={approvableInjects}
        selfSubmissions={selfSubmissions}
        onClose={() => setShowApproveDialog(false)}
        onConfirm={async (notes) => {
          const result = await onBatchApprove(
            approvableInjects.map(i => i.id),
            notes
          );
          setShowApproveDialog(false);
          onClearSelection();
          return result;
        }}
      />
      
      <BatchRejectDialog
        open={showRejectDialog}
        injects={submittedInjects}
        onClose={() => setShowRejectDialog(false)}
        onConfirm={async (reason) => {
          const result = await onBatchReject(
            submittedInjects.map(i => i.id),
            reason
          );
          setShowRejectDialog(false);
          onClearSelection();
          return result;
        }}
      />
    </Paper>
  );
};
```

## Test Cases

### Unit Tests

```csharp
[Fact]
public async Task BatchApprove_MultipleInjects_ApprovesAll()
{
    // Arrange
    var injects = await Create3SubmittedInjects();
    var ids = injects.Select(i => i.Id).ToList();
    
    // Act
    var result = await _service.BatchApproveAsync(ids, "Batch approved", _director);
    
    // Assert
    Assert.Equal(3, result.ApprovedCount);
    Assert.Equal(0, result.SkippedCount);
}

[Fact]
public async Task BatchApprove_WithSelfSubmissions_SkipsSelf()
{
    // Arrange
    var inject1 = await CreateSubmittedInject(submittedBy: _otherUser);
    var inject2 = await CreateSubmittedInject(submittedBy: _director); // Self
    var inject3 = await CreateSubmittedInject(submittedBy: _otherUser);
    var ids = new List<Guid> { inject1.Id, inject2.Id, inject3.Id };
    
    // Act
    var result = await _service.BatchApproveAsync(ids, null, _director);
    
    // Assert
    Assert.Equal(2, result.ApprovedCount);
    Assert.Equal(1, result.SkippedCount);
    Assert.Contains("Cannot approve your own submission", result.SkippedReasons[0]);
}

[Fact]
public async Task BatchReject_MultipleInjects_RejectsAll()
{
    // Arrange
    var injects = await Create3SubmittedInjects();
    var ids = injects.Select(i => i.Id).ToList();
    
    // Act
    var result = await _service.BatchRejectAsync(ids, "Timing needs adjustment", _director);
    
    // Assert
    foreach (var inject in injects)
    {
        var updated = await _context.Injects.FindAsync(inject.Id);
        Assert.Equal(InjectStatus.Draft, updated.Status);
        Assert.Equal("Timing needs adjustment", updated.RejectionReason);
    }
}

[Fact]
public async Task BatchApprove_SendsConsolidatedNotifications()
{
    // Arrange
    var author1 = await CreateUser("Controller");
    var author2 = await CreateUser("Controller");
    var inject1 = await CreateSubmittedInject(submittedBy: author1);
    var inject2 = await CreateSubmittedInject(submittedBy: author1);
    var inject3 = await CreateSubmittedInject(submittedBy: author2);
    var ids = new List<Guid> { inject1.Id, inject2.Id, inject3.Id };
    
    // Act
    await _service.BatchApproveAsync(ids, null, _director);
    
    // Assert - should be 2 notifications, not 3
    _notificationService.Verify(
        x => x.NotifyBatchApprovedAsync(author1.Id, It.Is<List<Inject>>(l => l.Count == 2)),
        Times.Once);
    _notificationService.Verify(
        x => x.NotifyBatchApprovedAsync(author2.Id, It.Is<List<Inject>>(l => l.Count == 1)),
        Times.Once);
}
```

## Out of Scope

- Batch submission (authors submitting multiple drafts)
- Batch scheduling (moving to Synchronized)
- Partial approval with individual notes per inject

## Definition of Done

- [ ] Batch approve endpoint implemented
- [ ] Batch reject endpoint implemented
- [ ] Self-submission filtering in batch approve
- [ ] Consolidated notifications by author
- [ ] Selection UI with checkboxes
- [ ] Select all/none functionality
- [ ] Batch toolbar with action buttons
- [ ] Batch approve dialog showing list and warning
- [ ] Batch reject dialog with shared reason
- [ ] Selection state persists during navigation
- [ ] Toast notifications for batch results
- [ ] Unit tests for batch operations
- [ ] Frontend component tests
