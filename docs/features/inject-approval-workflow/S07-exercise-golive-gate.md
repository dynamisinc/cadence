# S07: Exercise Go-Live Gate

**Feature:** [Inject Approval Workflow](FEATURE.md)  
**Priority:** P0  
**Points:** 3  
**Dependencies:** S04 (Approve or Reject Inject)

## User Story

**As an** Exercise Director,  
**I want** the system to prevent publishing an exercise with unapproved injects,  
**So that** we don't accidentally run an exercise with incomplete or unreviewed content.

## Context

This is a critical governance control. When inject approval is enabled, an exercise cannot transition from Draft to Published/Active status if any injects remain in Draft or Submitted status. This ensures all content has been reviewed before players interact with it.

## Acceptance Criteria

### Publish Validation
- [ ] **Given** approval is enabled AND some injects are Draft or Submitted, **when** I try to publish the exercise, **then** I see a blocking error
- [ ] **Given** the blocking error, **when** displayed, **then** it shows count of unapproved injects by status (e.g., "3 Draft, 2 Submitted")
- [ ] **Given** the blocking error, **when** I click "View unapproved injects", **then** I navigate to MSEL filtered to show Draft and Submitted injects
- [ ] **Given** all injects are Approved or beyond (Synchronized, Released, Complete, Deferred), **when** I publish, **then** it succeeds
- [ ] **Given** approval is DISABLED for the exercise, **when** I publish with Draft injects, **then** it succeeds (no validation)

### Edge Cases
- [ ] **Given** exercise has zero injects, **when** I try to publish, **then** I see warning "Exercise has no injects" but CAN proceed after confirmation
- [ ] **Given** all injects are Deferred or Obsolete (none active), **when** I try to publish, **then** I see warning but CAN proceed
- [ ] **Given** exercise was already Published and I'm re-publishing after adding new injects, **when** new injects are Draft, **then** validation still applies

### Publish Button State
- [ ] **Given** approval is enabled with unapproved injects, **when** I view exercise header, **then** Publish button shows warning state (orange) with tooltip
- [ ] **Given** all injects approved, **when** I view exercise header, **then** Publish button shows normal state (primary color)
- [ ] **Given** I hover over warning Publish button, **when** tooltip shows, **then** it says "X injects require approval before publishing"

### Dashboard Indicator
- [ ] **Given** approval enabled with unapproved injects, **when** I view exercise dashboard, **then** I see "Cannot publish" status with reason
- [ ] **Given** all injects approved, **when** I view exercise dashboard, **then** I see "Ready to publish" status (if not already published)

## UI Design

### Publish Blocked Dialog

```
┌─────────────────────────────────────────────────────────────────┐
│  ⛔ Cannot Publish Exercise                                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  This exercise has injects that require approval before         │
│  it can be published.                                           │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  📝 3 Draft injects                                      │   │
│  │  ⏳ 2 Submitted injects (awaiting approval)              │   │
│  │  ─────────────────────────────────────────────────────   │   │
│  │  5 total injects need attention                          │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  All injects must be Approved before the exercise can           │
│  go live.                                                       │
│                                                                 │
│                     [View Unapproved Injects]  [Close]          │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Exercise Header with Warning

```
┌─────────────────────────────────────────────────────────────────┐
│  Hurricane Response TTX                              [Draft ▼]  │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ ⚠️  5 injects require approval before publishing         │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  [Edit Settings]  [View MSEL]         [Publish ⚠️ ]            │
│                                      └── tooltip: "5 injects    │
│                                          require approval"      │
└─────────────────────────────────────────────────────────────────┘
```

### Exercise Header - Ready to Publish

```
┌─────────────────────────────────────────────────────────────────┐
│  Hurricane Response TTX                              [Draft ▼]  │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ ✓  All 14 injects approved - Ready to publish            │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  [Edit Settings]  [View MSEL]              [Publish]            │
└─────────────────────────────────────────────────────────────────┘
```

## Technical Implementation

### Backend: Publish Validation

```csharp
// File: src/Cadence.Core/Services/ExerciseService.cs

public class PublishValidationResult
{
    public bool CanPublish { get; set; }
    public int DraftCount { get; set; }
    public int SubmittedCount { get; set; }
    public int TotalUnapprovedCount => DraftCount + SubmittedCount;
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Validates whether an exercise can be published.
/// </summary>
public async Task<PublishValidationResult> ValidatePublishAsync(Guid exerciseId)
{
    var exercise = await _context.Exercises
        .Include(e => e.Msels)
            .ThenInclude(m => m.Injects)
        .FirstOrDefaultAsync(e => e.Id == exerciseId)
        ?? throw new NotFoundException("Exercise not found");
    
    var result = new PublishValidationResult { CanPublish = true };
    
    var injects = exercise.Msels.SelectMany(m => m.Injects).ToList();
    
    // Check for no injects
    if (!injects.Any())
    {
        result.Warnings.Add("Exercise has no injects");
        // Can still publish, just warning
    }
    
    // Check for all deferred/obsolete
    var activeInjects = injects.Where(i => 
        i.Status != InjectStatus.Deferred && 
        i.Status != InjectStatus.Obsolete).ToList();
    
    if (injects.Any() && !activeInjects.Any())
    {
        result.Warnings.Add("All injects are Deferred or Obsolete");
    }
    
    // Check approval status (only if approval enabled)
    if (exercise.RequireInjectApproval)
    {
        result.DraftCount = injects.Count(i => i.Status == InjectStatus.Draft);
        result.SubmittedCount = injects.Count(i => i.Status == InjectStatus.Submitted);
        
        if (result.TotalUnapprovedCount > 0)
        {
            result.CanPublish = false;
            result.Errors.Add(
                $"{result.TotalUnapprovedCount} injects require approval before publishing");
        }
    }
    
    return result;
}

/// <summary>
/// Publishes an exercise if validation passes.
/// </summary>
public async Task<Exercise> PublishAsync(Guid exerciseId, ClaimsPrincipal user)
{
    var validation = await ValidatePublishAsync(exerciseId);
    
    if (!validation.CanPublish)
    {
        throw new ValidationException(
            $"Cannot publish exercise: {string.Join("; ", validation.Errors)}");
    }
    
    var exercise = await _context.Exercises.FindAsync(exerciseId);
    exercise.Status = ExerciseStatus.Published;
    exercise.PublishedAt = DateTime.UtcNow;
    exercise.PublishedById = GetUserId(user);
    
    await _context.SaveChangesAsync();
    
    return exercise;
}
```

### Backend: API Endpoints

```csharp
// File: src/Cadence.Core/Controllers/ExercisesController.cs

/// <summary>
/// Validates whether an exercise can be published.
/// Returns detailed status including unapproved inject counts.
/// </summary>
[HttpGet("{id}/publish-validation")]
[Authorize(Roles = "Administrator,ExerciseDirector")]
public async Task<ActionResult<PublishValidationResult>> ValidatePublish(Guid id)
{
    var result = await _exerciseService.ValidatePublishAsync(id);
    return Ok(result);
}

/// <summary>
/// Publishes an exercise, making it active for conduct.
/// Fails if approval is enabled and injects are unapproved.
/// </summary>
[HttpPost("{id}/publish")]
[Authorize(Roles = "Administrator,ExerciseDirector")]
public async Task<ActionResult<ExerciseDto>> Publish(Guid id)
{
    var exercise = await _exerciseService.PublishAsync(id, User);
    return Ok(_mapper.Map<ExerciseDto>(exercise));
}
```

### Frontend: Publish Validation Hook

```tsx
// File: src/frontend/src/hooks/usePublishValidation.ts

interface UsePublishValidationResult {
  validation: PublishValidationResult | null;
  isLoading: boolean;
  refetch: () => void;
}

export function usePublishValidation(exerciseId: string): UsePublishValidationResult {
  const [validation, setValidation] = useState<PublishValidationResult | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  
  const fetch = useCallback(async () => {
    setIsLoading(true);
    try {
      const result = await exerciseApi.validatePublish(exerciseId);
      setValidation(result);
    } finally {
      setIsLoading(false);
    }
  }, [exerciseId]);
  
  useEffect(() => {
    fetch();
  }, [fetch]);
  
  return { validation, isLoading, refetch: fetch };
}
```

### Frontend: Publish Button Component

```tsx
// File: src/frontend/src/components/PublishButton.tsx

interface PublishButtonProps {
  exerciseId: string;
  exerciseStatus: ExerciseStatus;
  requiresApproval: boolean;
  onPublish: () => Promise<void>;
}

export const PublishButton: React.FC<PublishButtonProps> = ({
  exerciseId,
  exerciseStatus,
  requiresApproval,
  onPublish,
}) => {
  const { validation, isLoading } = usePublishValidation(exerciseId);
  const [showBlockedDialog, setShowBlockedDialog] = useState(false);
  const [isPublishing, setIsPublishing] = useState(false);
  const navigate = useNavigate();
  
  if (exerciseStatus !== ExerciseStatus.Draft) {
    return null; // Already published
  }
  
  const hasUnapproved = validation && validation.totalUnapprovedCount > 0;
  
  const handleClick = async () => {
    if (hasUnapproved) {
      setShowBlockedDialog(true);
      return;
    }
    
    setIsPublishing(true);
    try {
      await onPublish();
    } finally {
      setIsPublishing(false);
    }
  };
  
  return (
    <>
      <Tooltip
        title={hasUnapproved 
          ? `${validation.totalUnapprovedCount} injects require approval`
          : ''}
      >
        <span>
          <Button
            variant="contained"
            color={hasUnapproved ? 'warning' : 'primary'}
            onClick={handleClick}
            disabled={isLoading || isPublishing}
            startIcon={hasUnapproved ? <WarningIcon /> : <PublishIcon />}
          >
            {isPublishing ? 'Publishing...' : 'Publish'}
          </Button>
        </span>
      </Tooltip>
      
      <PublishBlockedDialog
        open={showBlockedDialog}
        validation={validation}
        onClose={() => setShowBlockedDialog(false)}
        onViewUnapproved={() => {
          setShowBlockedDialog(false);
          navigate(`/exercises/${exerciseId}/msel?filter=unapproved`);
        }}
      />
    </>
  );
};
```

### Frontend: Publish Blocked Dialog

```tsx
// File: src/frontend/src/components/PublishBlockedDialog.tsx

interface PublishBlockedDialogProps {
  open: boolean;
  validation: PublishValidationResult | null;
  onClose: () => void;
  onViewUnapproved: () => void;
}

export const PublishBlockedDialog: React.FC<PublishBlockedDialogProps> = ({
  open,
  validation,
  onClose,
  onViewUnapproved,
}) => {
  if (!validation) return null;
  
  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        <BlockIcon color="error" />
        Cannot Publish Exercise
      </DialogTitle>
      <DialogContent>
        <Typography variant="body1" gutterBottom>
          This exercise has injects that require approval before it can be published.
        </Typography>
        
        <Paper variant="outlined" sx={{ p: 2, mt: 2 }}>
          {validation.draftCount > 0 && (
            <Box display="flex" alignItems="center" gap={1} mb={1}>
              <EditIcon fontSize="small" color="action" />
              <Typography>
                <strong>{validation.draftCount}</strong> Draft injects
              </Typography>
            </Box>
          )}
          
          {validation.submittedCount > 0 && (
            <Box display="flex" alignItems="center" gap={1} mb={1}>
              <HourglassIcon fontSize="small" color="warning" />
              <Typography>
                <strong>{validation.submittedCount}</strong> Submitted injects (awaiting approval)
              </Typography>
            </Box>
          )}
          
          <Divider sx={{ my: 1 }} />
          
          <Typography variant="body2" color="text.secondary">
            <strong>{validation.totalUnapprovedCount}</strong> total injects need attention
          </Typography>
        </Paper>
        
        <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
          All injects must be Approved before the exercise can go live.
        </Typography>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>
          Close
        </Button>
        <Button 
          variant="contained" 
          onClick={onViewUnapproved}
          startIcon={<ListIcon />}
        >
          View Unapproved Injects
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
public async Task ValidatePublish_WithUnapprovedInjects_ReturnsCannotPublish()
{
    // Arrange
    var exercise = await CreateExerciseWithApprovalEnabled();
    await CreateInjects(exercise.Id, draft: 2, submitted: 3, approved: 5);
    
    // Act
    var result = await _service.ValidatePublishAsync(exercise.Id);
    
    // Assert
    Assert.False(result.CanPublish);
    Assert.Equal(2, result.DraftCount);
    Assert.Equal(3, result.SubmittedCount);
    Assert.Equal(5, result.TotalUnapprovedCount);
}

[Fact]
public async Task ValidatePublish_AllApproved_ReturnsCanPublish()
{
    // Arrange
    var exercise = await CreateExerciseWithApprovalEnabled();
    await CreateInjects(exercise.Id, approved: 10);
    
    // Act
    var result = await _service.ValidatePublishAsync(exercise.Id);
    
    // Assert
    Assert.True(result.CanPublish);
    Assert.Equal(0, result.TotalUnapprovedCount);
}

[Fact]
public async Task ValidatePublish_ApprovalDisabled_IgnoresUnapproved()
{
    // Arrange
    var exercise = await CreateExerciseWithApprovalDisabled();
    await CreateInjects(exercise.Id, draft: 5); // Draft injects
    
    // Act
    var result = await _service.ValidatePublishAsync(exercise.Id);
    
    // Assert
    Assert.True(result.CanPublish); // Can publish despite Draft injects
}

[Fact]
public async Task Publish_WithUnapprovedInjects_ThrowsValidationException()
{
    // Arrange
    var exercise = await CreateExerciseWithApprovalEnabled();
    await CreateInjects(exercise.Id, submitted: 3);
    
    // Act & Assert
    var ex = await Assert.ThrowsAsync<ValidationException>(() =>
        _service.PublishAsync(exercise.Id, _testUser));
    
    Assert.Contains("injects require approval", ex.Message);
}

[Fact]
public async Task ValidatePublish_NoInjects_ReturnsWarningButCanPublish()
{
    // Arrange
    var exercise = await CreateExerciseWithApprovalEnabled();
    // No injects created
    
    // Act
    var result = await _service.ValidatePublishAsync(exercise.Id);
    
    // Assert
    Assert.True(result.CanPublish);
    Assert.Contains("Exercise has no injects", result.Warnings);
}
```

## Out of Scope

- Unpublishing an exercise
- Scheduled publish (publish at specific time)
- Partial publish (publish subset of injects)

## Definition of Done

- [ ] Validation endpoint returns correct counts
- [ ] Publish endpoint enforces validation
- [ ] Publish blocked when unapproved injects exist
- [ ] Publish succeeds when all approved
- [ ] Publish button shows warning state
- [ ] Tooltip shows unapproved count
- [ ] Blocked dialog with breakdown by status
- [ ] "View Unapproved Injects" navigates to filtered MSEL
- [ ] Warning for no injects (but can proceed)
- [ ] Approval-disabled exercises skip validation
- [ ] Unit tests for all scenarios
- [ ] Frontend component tests
