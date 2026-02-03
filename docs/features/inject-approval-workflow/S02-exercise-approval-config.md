# S02: Exercise Approval Configuration

**Feature:** [Inject Approval Workflow](FEATURE.md)  
**Priority:** P1  
**Points:** 3  
**Dependencies:** S01 (Organization Approval Configuration)

## User Story

**As an** Exercise Director,  
**I want** to enable or disable inject approval for a specific exercise,  
**So that** I can match the governance level to the exercise formality.

## Context

A formal Full-Scale Exercise needs approval workflow; an informal team TTX may not. Directors should be able to override organization defaults when the organization policy allows. Administrators can override even "Required" policies for specific exercises.

## Acceptance Criteria

### Policy: Optional (Director Can Toggle)
- [ ] **Given** org policy is "Optional", **when** I create a new exercise, **then** "Require inject approval" defaults to OFF
- [ ] **Given** org policy is "Optional", **when** I edit Exercise Settings, **then** I see "Require inject approval" toggle
- [ ] **Given** the toggle, **when** I turn it ON, **then** approval workflow is enabled for this exercise
- [ ] **Given** the toggle, **when** I turn it OFF, **then** approval workflow is disabled for this exercise

### Policy: Required (Director Cannot Disable)
- [ ] **Given** org policy is "Required", **when** I create a new exercise, **then** "Require inject approval" defaults to ON
- [ ] **Given** org policy is "Required", **when** I view Exercise Settings as Director, **then** the toggle is visible but locked ON
- [ ] **Given** the locked toggle, **when** I hover over it, **then** I see tooltip: "Organization policy requires inject approval for all exercises"
- [ ] **Given** org policy is "Required" and I am Admin, **when** I view Exercise Settings, **then** I see an "Override" button next to the locked toggle

### Admin Override for Required Policy
- [ ] **Given** org policy is "Required" and I am Admin, **when** I click "Override", **then** I see confirmation dialog explaining the override
- [ ] **Given** I confirm the override, **when** saved, **then** approval is disabled AND `ApprovalPolicyOverridden` flag is set to true
- [ ] **Given** approval was overridden, **when** viewing Exercise Settings, **then** I see warning: "Admin has overridden organization policy"
- [ ] **Given** approval was overridden, **when** I want to restore policy, **then** I can click "Restore Organization Policy" to re-enable approval

### Policy: Disabled (Toggle Hidden)
- [ ] **Given** org policy is "Disabled", **when** I edit Exercise Settings, **then** the approval toggle is NOT visible
- [ ] **Given** org policy is "Disabled", **when** I create a new exercise, **then** approval is disabled by default

### Enable Approval on Existing Exercise
- [ ] **Given** approval is disabled on an exercise with Draft injects, **when** I enable approval, **then** Draft injects remain Draft (must go through approval)
- [ ] **Given** approval is disabled on an exercise with Approved/Synchronized injects, **when** I enable approval, **then** those injects remain in their current status

### Disable Approval on Existing Exercise
- [ ] **Given** approval is enabled with some injects in Submitted status, **when** I try to disable approval, **then** I see warning: "X injects are pending approval. They will be returned to Draft."
- [ ] **Given** I confirm disabling approval with pending injects, **when** saved, **then** Submitted injects are changed to Approved (auto-approved)
- [ ] **Given** approval is disabled, **when** new injects are created, **then** they skip Submitted status and can go directly from Draft to Approved

### Permission Enforcement
- [ ] **Given** I am a Controller, **when** I view Exercise Settings, **then** I cannot see the approval toggle
- [ ] **Given** I am an Evaluator, **when** I try to access Exercise Settings, **then** I cannot modify any settings

## UI Design

### Exercise Settings - Approval Section

```
┌─────────────────────────────────────────────────────────────────┐
│  Exercise Settings                                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  General                                                        │
│  ├─ Exercise Name: [Hurricane Response TTX     ]                │
│  ├─ Type: [Tabletop Exercise          ▼]                        │
│  └─ Description: [Annual hurricane...          ]                │
│                                                                 │
│  ─────────────────────────────────────────────────────────────  │
│                                                                 │
│  Governance                                                     │
│                                                                 │
│  Require Inject Approval                                        │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                                                          │   │
│  │  [====○    ] OFF                                         │   │
│  │                                                          │   │
│  │  When enabled, injects must be submitted and approved    │   │
│  │  by an Exercise Director before they can be scheduled.   │   │
│  │                                                          │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│                              [Cancel]  [Save Changes]           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Exercise Settings - Required Policy (Director View)

```
│  Governance                                                     │
│                                                                 │
│  Require Inject Approval                                        │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                                                          │   │
│  │  [●========] ON  🔒                                      │   │
│  │                                                          │   │
│  │  ⓘ Organization policy requires inject approval for     │   │
│  │    all exercises. Contact an administrator to override.  │   │
│  │                                                          │   │
│  └─────────────────────────────────────────────────────────┘   │
```

### Exercise Settings - Required Policy (Admin View with Override)

```
│  Governance                                                     │
│                                                                 │
│  Require Inject Approval                                        │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                                                          │   │
│  │  [●========] ON  🔒         [Override Policy]            │   │
│  │                                                          │   │
│  │  ⓘ Organization policy requires inject approval.        │   │
│  │    Administrators can override for this exercise.        │   │
│  │                                                          │   │
│  └─────────────────────────────────────────────────────────┘   │
```

### Override Confirmation Dialog

```
┌─────────────────────────────────────────────────────────────────┐
│  Override Approval Policy?                                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ⚠️  Your organization requires inject approval for all         │
│      exercises. Overriding this policy will:                    │
│                                                                 │
│      • Allow injects to bypass approval workflow                │
│      • Be visible to other administrators                       │
│      • Be recorded in the audit log                             │
│                                                                 │
│  This exercise will be marked as having an overridden policy.   │
│                                                                 │
│  Reason for override (optional):                                │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ Informal training exercise for new staff                 │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│                          [Cancel]  [Override and Disable]       │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Technical Implementation

### Backend: Exercise Entity Update

```csharp
// File: src/Cadence.Core/Entities/Exercise.cs (additions)

/// <summary>
/// Whether inject approval workflow is enabled for this exercise.
/// When true, injects must go through Draft → Submitted → Approved workflow.
/// Default value depends on organization's ApprovalPolicy setting.
/// </summary>
public bool RequireInjectApproval { get; set; } = false;

/// <summary>
/// If true, an Administrator has overridden the organization's "Required" policy
/// to disable approval for this specific exercise.
/// </summary>
public bool ApprovalPolicyOverridden { get; set; } = false;

/// <summary>
/// Optional reason provided when admin overrode the approval policy.
/// </summary>
public string? ApprovalOverrideReason { get; set; }

/// <summary>
/// User who overrode the approval policy. Null if not overridden.
/// </summary>
public Guid? ApprovalOverriddenById { get; set; }

/// <summary>
/// When the approval policy was overridden. Null if not overridden.
/// </summary>
public DateTime? ApprovalOverriddenAt { get; set; }
```

### Backend: Service Logic

```csharp
// File: src/Cadence.Core/Services/ExerciseService.cs

/// <summary>
/// Updates exercise approval settings, respecting organization policy constraints.
/// </summary>
public async Task<Exercise> UpdateApprovalSettingsAsync(
    Guid exerciseId,
    bool requireApproval,
    bool isOverride = false,
    string? overrideReason = null,
    ClaimsPrincipal user = null)
{
    var exercise = await _context.Exercises
        .Include(e => e.Organization)
        .FirstOrDefaultAsync(e => e.Id == exerciseId)
        ?? throw new NotFoundException("Exercise not found");
    
    var orgPolicy = exercise.Organization.InjectApprovalPolicy;
    var isAdmin = user.IsInRole("Administrator");
    
    // Validate against org policy
    if (orgPolicy == ApprovalPolicy.Disabled && requireApproval)
    {
        throw new ValidationException(
            "Cannot enable approval - organization policy has disabled approval workflow");
    }
    
    if (orgPolicy == ApprovalPolicy.Required && !requireApproval && !isAdmin)
    {
        throw new ValidationException(
            "Cannot disable approval - organization policy requires approval for all exercises");
    }
    
    // Handle override scenario
    if (orgPolicy == ApprovalPolicy.Required && !requireApproval && isAdmin)
    {
        exercise.ApprovalPolicyOverridden = true;
        exercise.ApprovalOverrideReason = overrideReason;
        exercise.ApprovalOverriddenById = GetUserId(user);
        exercise.ApprovalOverriddenAt = DateTime.UtcNow;
    }
    else if (requireApproval && exercise.ApprovalPolicyOverridden)
    {
        // Restoring org policy
        exercise.ApprovalPolicyOverridden = false;
        exercise.ApprovalOverrideReason = null;
        exercise.ApprovalOverriddenById = null;
        exercise.ApprovalOverriddenAt = null;
    }
    
    // Handle pending injects when disabling approval
    if (exercise.RequireInjectApproval && !requireApproval)
    {
        await AutoApproveSubmittedInjectsAsync(exerciseId);
    }
    
    exercise.RequireInjectApproval = requireApproval;
    await _context.SaveChangesAsync();
    
    return exercise;
}

private async Task AutoApproveSubmittedInjectsAsync(Guid exerciseId)
{
    var submittedInjects = await _context.Injects
        .Where(i => i.Msel.ExerciseId == exerciseId && i.Status == InjectStatus.Submitted)
        .ToListAsync();
    
    foreach (var inject in submittedInjects)
    {
        inject.Status = InjectStatus.Approved;
        inject.ApprovedAt = DateTime.UtcNow;
        // Note: ApprovedById left null to indicate auto-approval
    }
}
```

### Backend: API Endpoint

```csharp
// File: src/Cadence.Core/Controllers/ExercisesController.cs

public class UpdateExerciseApprovalRequest
{
    public bool RequireInjectApproval { get; set; }
    public bool IsOverride { get; set; } = false;
    public string? OverrideReason { get; set; }
}

/// <summary>
/// Updates approval settings for an exercise.
/// </summary>
[HttpPut("{id}/approval-settings")]
[Authorize(Roles = "Administrator,ExerciseDirector")]
public async Task<ActionResult<ExerciseDto>> UpdateApprovalSettings(
    Guid id,
    [FromBody] UpdateExerciseApprovalRequest request)
{
    var exercise = await _exerciseService.UpdateApprovalSettingsAsync(
        id,
        request.RequireInjectApproval,
        request.IsOverride,
        request.OverrideReason,
        User);
    
    return Ok(_mapper.Map<ExerciseDto>(exercise));
}
```

### Frontend: Approval Toggle Component

```tsx
// File: src/frontend/src/components/ExerciseApprovalToggle.tsx

interface ExerciseApprovalToggleProps {
  exercise: Exercise;
  organizationPolicy: ApprovalPolicy;
  isAdmin: boolean;
  onChange: (enabled: boolean, isOverride?: boolean, reason?: string) => void;
}

export const ExerciseApprovalToggle: React.FC<ExerciseApprovalToggleProps> = ({
  exercise,
  organizationPolicy,
  isAdmin,
  onChange,
}) => {
  const [showOverrideDialog, setShowOverrideDialog] = useState(false);
  
  // Don't render if org policy is Disabled
  if (organizationPolicy === ApprovalPolicy.Disabled) {
    return null;
  }
  
  const isLocked = organizationPolicy === ApprovalPolicy.Required && !isAdmin;
  const canOverride = organizationPolicy === ApprovalPolicy.Required && isAdmin;
  
  return (
    <Paper variant="outlined" sx={{ p: 2 }}>
      <Box display="flex" alignItems="center" justifyContent="space-between">
        <Box>
          <Typography variant="subtitle1" fontWeight={600}>
            Require Inject Approval
            {isLocked && <LockIcon sx={{ ml: 1, fontSize: 16 }} />}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {exercise.requireInjectApproval 
              ? 'Injects must be approved before scheduling'
              : 'Injects can be scheduled without approval'}
          </Typography>
        </Box>
        
        <Box display="flex" alignItems="center" gap={1}>
          <Switch
            checked={exercise.requireInjectApproval}
            onChange={(e) => onChange(e.target.checked)}
            disabled={isLocked}
          />
          
          {canOverride && exercise.requireInjectApproval && (
            <Button
              size="small"
              variant="outlined"
              onClick={() => setShowOverrideDialog(true)}
            >
              Override Policy
            </Button>
          )}
        </Box>
      </Box>
      
      {exercise.approvalPolicyOverridden && (
        <Alert severity="warning" sx={{ mt: 2 }}>
          Admin has overridden organization policy. 
          <Button size="small" onClick={() => onChange(true)}>
            Restore Policy
          </Button>
        </Alert>
      )}
      
      {isLocked && (
        <Alert severity="info" sx={{ mt: 2 }}>
          Organization policy requires inject approval for all exercises.
          Contact an administrator to override.
        </Alert>
      )}
      
      <OverrideConfirmDialog
        open={showOverrideDialog}
        onClose={() => setShowOverrideDialog(false)}
        onConfirm={(reason) => {
          onChange(false, true, reason);
          setShowOverrideDialog(false);
        }}
      />
    </Paper>
  );
};
```

### Database Migration

```csharp
public partial class AddExerciseApprovalSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "RequireInjectApproval",
            table: "Exercises",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "ApprovalPolicyOverridden",
            table: "Exercises",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "ApprovalOverrideReason",
            table: "Exercises",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "ApprovalOverriddenById",
            table: "Exercises",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ApprovalOverriddenAt",
            table: "Exercises",
            type: "datetime2",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "RequireInjectApproval", table: "Exercises");
        migrationBuilder.DropColumn(name: "ApprovalPolicyOverridden", table: "Exercises");
        migrationBuilder.DropColumn(name: "ApprovalOverrideReason", table: "Exercises");
        migrationBuilder.DropColumn(name: "ApprovalOverriddenById", table: "Exercises");
        migrationBuilder.DropColumn(name: "ApprovalOverriddenAt", table: "Exercises");
    }
}
```

## Test Cases

### Unit Tests

```csharp
[Fact]
public async Task UpdateApprovalSettings_OptionalPolicy_DirectorCanToggle()
{
    // Arrange
    var org = await CreateOrgWithPolicy(ApprovalPolicy.Optional);
    var exercise = await CreateExercise(org.Id);
    SetCurrentUser("Director", exercise.Id);
    
    // Act
    var result = await _service.UpdateApprovalSettingsAsync(
        exercise.Id, requireApproval: true);
    
    // Assert
    Assert.True(result.RequireInjectApproval);
}

[Fact]
public async Task UpdateApprovalSettings_RequiredPolicy_DirectorCannotDisable()
{
    // Arrange
    var org = await CreateOrgWithPolicy(ApprovalPolicy.Required);
    var exercise = await CreateExercise(org.Id);
    exercise.RequireInjectApproval = true;
    SetCurrentUser("Director", exercise.Id);
    
    // Act & Assert
    await Assert.ThrowsAsync<ValidationException>(() =>
        _service.UpdateApprovalSettingsAsync(exercise.Id, requireApproval: false));
}

[Fact]
public async Task UpdateApprovalSettings_RequiredPolicy_AdminCanOverride()
{
    // Arrange
    var org = await CreateOrgWithPolicy(ApprovalPolicy.Required);
    var exercise = await CreateExercise(org.Id);
    exercise.RequireInjectApproval = true;
    SetCurrentUser("Administrator");
    
    // Act
    var result = await _service.UpdateApprovalSettingsAsync(
        exercise.Id, 
        requireApproval: false, 
        isOverride: true,
        overrideReason: "Training exercise");
    
    // Assert
    Assert.False(result.RequireInjectApproval);
    Assert.True(result.ApprovalPolicyOverridden);
    Assert.Equal("Training exercise", result.ApprovalOverrideReason);
}

[Fact]
public async Task UpdateApprovalSettings_DisablingApproval_AutoApprovesSubmittedInjects()
{
    // Arrange
    var exercise = await CreateExerciseWithInjects();
    var submittedInject = exercise.Msel.Injects.First(i => i.Status == InjectStatus.Submitted);
    
    // Act
    await _service.UpdateApprovalSettingsAsync(exercise.Id, requireApproval: false);
    
    // Assert
    var inject = await _context.Injects.FindAsync(submittedInject.Id);
    Assert.Equal(InjectStatus.Approved, inject.Status);
}
```

## Out of Scope

- Submit/Approve workflow mechanics (S03, S04)
- Notifications when approval settings change
- Bulk exercise configuration changes

## Definition of Done

- [ ] Exercise entity has new approval fields
- [ ] Database migration created and tested
- [ ] Service logic respects org policy constraints
- [ ] Admin override functionality working
- [ ] UI toggle component with locked state
- [ ] Override confirmation dialog
- [ ] Warning when disabling with pending injects
- [ ] Auto-approve submitted injects when disabling
- [ ] Unit tests for all policy scenarios
- [ ] Integration tests for override flow
- [ ] Audit logging for changes
