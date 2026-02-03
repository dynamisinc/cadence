# S11: Configurable Approval Permissions

**Feature:** [Inject Approval Workflow](FEATURE.md)  
**Priority:** P1  
**Points:** 5  
**Dependencies:** S01 (Organization Approval Configuration), S04 (Approve or Reject Inject)

## User Story

**As an** Administrator,  
**I want** to configure which exercise roles can approve injects,  
**So that** our approval workflow matches our organization's delegation of authority.

## Context

Different organizations have different governance structures:

| Organization Type | Typical Approval Authority |
|-------------------|---------------------------|
| Small agency | Director only |
| Large agency | Directors + Senior Controllers |
| Military/DoD | Hierarchical chain of command |
| Corporate | Department leads (may map to Controller role) |
| Healthcare | Incident Commander + designated alternates |

The current hardcoded permission (Admin + Director only) doesn't accommodate these variations. Organizations need to configure:

1. Which HSEEP roles can approve injects
2. Whether self-approval is permitted (separation of duties)
3. Optionally, per-exercise overrides for specific participants

## Acceptance Criteria

### Organization-Level Configuration

- [ ] **Given** I am an Admin in Organization Settings, **when** I navigate to Governance → Approval Permissions, **then** I see role checkboxes for approval authority
- [ ] **Given** the role checkboxes, **when** displayed, **then** I see:
  - ☑ Administrator (always checked, cannot uncheck)
  - ☑ Exercise Director (default checked)
  - ☐ Controller (default unchecked)
  - ☐ Evaluator (default unchecked)
- [ ] **Given** I check "Controller", **when** saved, **then** Controllers in all exercises can approve injects
- [ ] **Given** I uncheck "Exercise Director", **when** saved, **then** Directors can no longer approve (only Admins)

### Self-Approval Configuration

- [ ] **Given** the Approval Permissions settings, **when** I view options, **then** I see a "Self-Approval Policy" section
- [ ] **Given** self-approval options, **when** displayed, **then** I see:
  - ○ Never allowed (default) - Users cannot approve their own submissions
  - ○ Allowed with warning - Users can self-approve but see confirmation dialog
  - ○ Always allowed - No restrictions on self-approval
- [ ] **Given** "Never allowed" is selected, **when** a user tries to approve their own inject, **then** the approve button is disabled with tooltip "You cannot approve your own submission"
- [ ] **Given** "Allowed with warning" is selected, **when** a user approves their own inject, **then** they see a confirmation dialog explaining the audit implications

### Permission Enforcement

- [ ] **Given** Controller role is NOT enabled for approval, **when** a Controller views an inject, **then** they do not see Approve/Reject buttons
- [ ] **Given** Controller role IS enabled for approval, **when** a Controller views a Submitted inject, **then** they see Approve/Reject buttons
- [ ] **Given** a user without approval permission, **when** they call POST /injects/{id}/approve, **then** they receive 403 Forbidden
- [ ] **Given** self-approval is "Never allowed", **when** user calls approve on their own inject, **then** they receive 403 with message "Self-approval is not permitted"

### Exercise-Level Override (Optional Enhancement)

- [ ] **Given** I am a Director on an exercise, **when** I view Exercise Settings → Permissions, **then** I can grant approval rights to specific participants
- [ ] **Given** I grant approval rights to a Controller participant, **when** that Controller views Submitted injects, **then** they can approve (for this exercise only)
- [ ] **Given** a participant has exercise-level approval rights, **when** they view other exercises, **then** they do NOT have approval rights (unless org-level permits)

### Audit Trail

- [ ] **Given** approval permissions are changed, **when** saved, **then** an audit log entry records old settings, new settings, user, and timestamp
- [ ] **Given** a user approves via granted permission (not default role), **when** the approval is logged, **then** InjectStatusHistory notes the permission source

### Batch Approval Respect

- [ ] **Given** batch approval is used, **when** processing, **then** permission checks apply per-inject based on configured roles
- [ ] **Given** a Controller with approval permission uses batch approve, **when** their own submissions are in the batch, **then** those are skipped per self-approval policy

## UI Design

### Approval Permissions Settings

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Settings > Governance > Approval Permissions                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Which roles can approve injects?                                           │
│  ─────────────────────────────────────────────────────────────────────────  │
│  Select the exercise roles that have authority to approve or reject         │
│  injects submitted for review.                                              │
│                                                                             │
│  ☑ Administrator                                                            │
│    System administrators always have approval authority.                    │
│    ⓘ Cannot be disabled                                                     │
│                                                                             │
│  ☑ Exercise Director                                                        │
│    Exercise owners and leads can approve injects for their exercises.       │
│                                                                             │
│  ☐ Controller                                                               │
│    Allow Controllers to approve injects. Useful for large exercises         │
│    with distributed control teams.                                          │
│                                                                             │
│  ☐ Evaluator                                                                │
│    Allow Evaluators to approve injects. Uncommon - evaluators typically     │
│    observe rather than control exercise content.                            │
│                                                                             │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                             │
│  Self-Approval Policy                                                       │
│  ─────────────────────────────────────────────────────────────────────────  │
│  Can users approve injects they submitted themselves?                       │
│                                                                             │
│  ○ Never allowed (Recommended)                                              │
│    Enforces separation of duties. Users cannot approve their own work.      │
│                                                                             │
│  ○ Allowed with warning                                                     │
│    Users can self-approve but must confirm. Self-approvals are flagged      │
│    in audit logs.                                                           │
│                                                                             │
│  ○ Always allowed                                                           │
│    No restrictions. Not recommended for compliance-sensitive exercises.     │
│                                                                             │
│                                                 [Cancel]  [Save Changes]    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Self-Approval Warning Dialog

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  ⚠️  Self-Approval Confirmation                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  You are about to approve an inject that you submitted.                     │
│                                                                             │
│  Self-approvals are permitted by your organization but will be:             │
│  • Flagged in the audit trail                                               │
│  • Visible in approval reports                                              │
│  • Noted in HSEEP compliance documentation                                  │
│                                                                             │
│  Consider having another team member review this inject if possible.        │
│                                                                             │
│                                          [Cancel]  [Approve Anyway]         │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Exercise-Level Permission Grant (Optional)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Exercise Settings > Permissions                                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Additional Approval Authority                                              │
│  ─────────────────────────────────────────────────────────────────────────  │
│  Grant approval rights to specific participants beyond org defaults.        │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Participant              │ Role       │ Can Approve │                │   │
│  ├──────────────────────────┼────────────┼─────────────┤                │   │
│  │ Maria Santos             │ Controller │ ☑ Granted   │                │   │
│  │ James Chen               │ Controller │ ☐           │                │   │
│  │ Sarah Johnson            │ Evaluator  │ ☐           │                │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ⓘ Org-level settings allow: Administrator, Exercise Director              │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Technical Implementation

### Backend: New Enum for Self-Approval Policy

```csharp
// File: src/Cadence.Core/Entities/Enums/SelfApprovalPolicy.cs

/// <summary>
/// Organization policy for self-approval of injects.
/// </summary>
public enum SelfApprovalPolicy
{
    /// <summary>Users cannot approve injects they submitted.</summary>
    NeverAllowed = 0,
    
    /// <summary>Users can self-approve with confirmation dialog.</summary>
    AllowedWithWarning = 1,
    
    /// <summary>No restrictions on self-approval.</summary>
    AlwaysAllowed = 2
}
```

### Backend: Organization Entity Updates

```csharp
// File: src/Cadence.Core/Entities/Organization.cs (add fields)

/// <summary>Roles that can approve injects (flags enum).</summary>
public ApprovalRoles ApprovalAuthorizedRoles { get; set; } = 
    ApprovalRoles.Administrator | ApprovalRoles.ExerciseDirector;

/// <summary>Policy for self-approval of injects.</summary>
public SelfApprovalPolicy SelfApprovalPolicy { get; set; } = SelfApprovalPolicy.NeverAllowed;
```

### Backend: Approval Roles Flags Enum

```csharp
// File: src/Cadence.Core/Entities/Enums/ApprovalRoles.cs

/// <summary>
/// Flags enum for roles authorized to approve injects.
/// </summary>
[Flags]
public enum ApprovalRoles
{
    None = 0,
    Administrator = 1,
    ExerciseDirector = 2,
    Controller = 4,
    Evaluator = 8
}
```

### Backend: Exercise Entity Updates (Optional Enhancement)

```csharp
// File: src/Cadence.Core/Entities/Exercise.cs (add field)

/// <summary>
/// Participant IDs granted approval authority for this exercise only.
/// </summary>
public List<Guid> AdditionalApprovers { get; set; } = new();
```

### Backend: Permission Check Service

```csharp
// File: src/Cadence.Core/Services/ApprovalPermissionService.cs

public class ApprovalPermissionService : IApprovalPermissionService
{
    /// <summary>
    /// Determines if a user can approve injects for an exercise.
    /// </summary>
    public async Task<bool> CanApproveAsync(Guid userId, Guid exerciseId)
    {
        var org = await GetOrganizationForExercise(exerciseId);
        var participant = await GetParticipant(userId, exerciseId);
        
        // Check org-level role permissions
        if (IsRoleAuthorized(participant.Role, org.ApprovalAuthorizedRoles))
            return true;
        
        // Check exercise-level grants (optional enhancement)
        var exercise = await _context.Exercises.FindAsync(exerciseId);
        if (exercise.AdditionalApprovers.Contains(userId))
            return true;
        
        return false;
    }
    
    /// <summary>
    /// Determines if a user can approve a specific inject (includes self-approval check).
    /// </summary>
    public async Task<ApprovalPermissionResult> CanApproveInjectAsync(
        Guid userId, 
        Guid injectId)
    {
        var inject = await _context.Injects
            .Include(i => i.Exercise)
            .ThenInclude(e => e.Organization)
            .FirstAsync(i => i.Id == injectId);
        
        // Check base permission
        if (!await CanApproveAsync(userId, inject.ExerciseId))
            return ApprovalPermissionResult.NotAuthorized;
        
        // Check self-approval
        if (inject.SubmittedById == userId)
        {
            return inject.Exercise.Organization.SelfApprovalPolicy switch
            {
                SelfApprovalPolicy.NeverAllowed => ApprovalPermissionResult.SelfApprovalDenied,
                SelfApprovalPolicy.AllowedWithWarning => ApprovalPermissionResult.SelfApprovalWithWarning,
                SelfApprovalPolicy.AlwaysAllowed => ApprovalPermissionResult.Allowed,
                _ => ApprovalPermissionResult.SelfApprovalDenied
            };
        }
        
        return ApprovalPermissionResult.Allowed;
    }
    
    private bool IsRoleAuthorized(ExerciseRole role, ApprovalRoles authorizedRoles)
    {
        return role switch
        {
            ExerciseRole.Administrator => authorizedRoles.HasFlag(ApprovalRoles.Administrator),
            ExerciseRole.ExerciseDirector => authorizedRoles.HasFlag(ApprovalRoles.ExerciseDirector),
            ExerciseRole.Controller => authorizedRoles.HasFlag(ApprovalRoles.Controller),
            ExerciseRole.Evaluator => authorizedRoles.HasFlag(ApprovalRoles.Evaluator),
            _ => false
        };
    }
}

public enum ApprovalPermissionResult
{
    Allowed,
    NotAuthorized,
    SelfApprovalDenied,
    SelfApprovalWithWarning
}
```

### Backend: Update Approval Endpoint

```csharp
// File: src/Cadence.Core/Controllers/InjectController.cs (modify)

[HttpPost("{id}/approve")]
public async Task<IActionResult> Approve(
    Guid id, 
    [FromBody] ApproveInjectRequest request)
{
    var userId = GetCurrentUserId();
    
    var permissionResult = await _permissionService.CanApproveInjectAsync(userId, id);
    
    return permissionResult switch
    {
        ApprovalPermissionResult.NotAuthorized => 
            Forbid("You do not have permission to approve injects"),
        
        ApprovalPermissionResult.SelfApprovalDenied => 
            Forbid("Self-approval is not permitted by your organization"),
        
        ApprovalPermissionResult.SelfApprovalWithWarning when !request.ConfirmSelfApproval =>
            BadRequest(new { 
                Code = "SELF_APPROVAL_WARNING",
                Message = "This is your own submission. Set confirmSelfApproval=true to proceed."
            }),
        
        _ => Ok(await _approvalService.ApproveAsync(id, userId, request))
    };
}
```

### Frontend: Permission Check Hook

```tsx
// File: src/frontend/src/hooks/useApprovalPermissions.ts

interface ApprovalPermissions {
  canApprove: boolean;
  isSelfApproval: boolean;
  selfApprovalPolicy: SelfApprovalPolicy;
  requiresConfirmation: boolean;
}

export const useApprovalPermissions = (
  injectId: string
): ApprovalPermissions => {
  const { user } = useAuth();
  const { data: inject } = useInject(injectId);
  const { data: orgSettings } = useOrganizationSettings();
  const { data: participant } = useCurrentParticipant(inject?.exerciseId);
  
  const isSelfApproval = inject?.submittedById === user?.id;
  
  const canApprove = useMemo(() => {
    if (!participant || !orgSettings) return false;
    
    const authorizedRoles = orgSettings.approvalAuthorizedRoles;
    const roleFlag = getRoleFlag(participant.role);
    
    // Check role permission
    if (!(authorizedRoles & roleFlag)) return false;
    
    // Check self-approval
    if (isSelfApproval && 
        orgSettings.selfApprovalPolicy === SelfApprovalPolicy.NeverAllowed) {
      return false;
    }
    
    return true;
  }, [participant, orgSettings, isSelfApproval]);
  
  return {
    canApprove,
    isSelfApproval,
    selfApprovalPolicy: orgSettings?.selfApprovalPolicy ?? SelfApprovalPolicy.NeverAllowed,
    requiresConfirmation: isSelfApproval && 
      orgSettings?.selfApprovalPolicy === SelfApprovalPolicy.AllowedWithWarning
  };
};
```

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/organizations/{id}/approval-permissions` | Get approval permission settings |
| PUT | `/api/organizations/{id}/approval-permissions` | Update approval permission settings |
| GET | `/api/exercises/{id}/additional-approvers` | Get exercise-level approvers |
| PUT | `/api/exercises/{id}/additional-approvers` | Update exercise-level approvers |

### DTOs

```csharp
public record ApprovalPermissionsDto
{
    public ApprovalRoles AuthorizedRoles { get; init; }
    public SelfApprovalPolicy SelfApprovalPolicy { get; init; }
}

public record UpdateApprovalPermissionsRequest
{
    public ApprovalRoles AuthorizedRoles { get; init; }
    public SelfApprovalPolicy SelfApprovalPolicy { get; init; }
}

public record ApproveInjectRequest
{
    public string? Notes { get; init; }
    public bool ConfirmSelfApproval { get; init; } = false;
}
```

## Test Cases

### Unit Tests

```csharp
[Fact]
public async Task CanApprove_DirectorRole_WhenAuthorized_ReturnsTrue()
{
    // Arrange
    var org = new Organization { 
        ApprovalAuthorizedRoles = ApprovalRoles.Administrator | ApprovalRoles.ExerciseDirector 
    };
    var participant = new ExerciseParticipant { Role = ExerciseRole.ExerciseDirector };
    
    // Act
    var result = await _service.CanApproveAsync(participant.UserId, exerciseId);
    
    // Assert
    Assert.True(result);
}

[Fact]
public async Task CanApprove_ControllerRole_WhenNotAuthorized_ReturnsFalse()
{
    // Arrange
    var org = new Organization { 
        ApprovalAuthorizedRoles = ApprovalRoles.Administrator | ApprovalRoles.ExerciseDirector 
    };
    var participant = new ExerciseParticipant { Role = ExerciseRole.Controller };
    
    // Act
    var result = await _service.CanApproveAsync(participant.UserId, exerciseId);
    
    // Assert
    Assert.False(result);
}

[Fact]
public async Task CanApproveInject_SelfApproval_WhenNeverAllowed_ReturnsDenied()
{
    // Arrange
    var org = new Organization { SelfApprovalPolicy = SelfApprovalPolicy.NeverAllowed };
    var inject = new Inject { SubmittedById = userId };
    
    // Act
    var result = await _service.CanApproveInjectAsync(userId, inject.Id);
    
    // Assert
    Assert.Equal(ApprovalPermissionResult.SelfApprovalDenied, result);
}

[Fact]
public async Task CanApproveInject_SelfApproval_WhenAllowedWithWarning_ReturnsWarning()
{
    // Arrange
    var org = new Organization { SelfApprovalPolicy = SelfApprovalPolicy.AllowedWithWarning };
    var inject = new Inject { SubmittedById = userId };
    
    // Act
    var result = await _service.CanApproveInjectAsync(userId, inject.Id);
    
    // Assert
    Assert.Equal(ApprovalPermissionResult.SelfApprovalWithWarning, result);
}

[Fact]
public async Task CanApprove_ExerciseLevelGrant_OverridesOrgDefault()
{
    // Arrange
    var org = new Organization { 
        ApprovalAuthorizedRoles = ApprovalRoles.Administrator // Controller not authorized
    };
    var exercise = new Exercise { AdditionalApprovers = new List<Guid> { userId } };
    var participant = new ExerciseParticipant { Role = ExerciseRole.Controller };
    
    // Act
    var result = await _service.CanApproveAsync(userId, exercise.Id);
    
    // Assert
    Assert.True(result); // Granted at exercise level
}
```

### Integration Tests

```csharp
[Fact]
public async Task ApproveEndpoint_ControllerWithoutPermission_Returns403()
{
    // Arrange
    await SetOrgApprovalRoles(ApprovalRoles.Administrator | ApprovalRoles.ExerciseDirector);
    var inject = await CreateSubmittedInject();
    
    // Act
    var response = await _client.PostAsync(
        $"/api/injects/{inject.Id}/approve",
        JsonContent.Create(new { }),
        asRole: ExerciseRole.Controller);
    
    // Assert
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}

[Fact]
public async Task ApproveEndpoint_SelfApprovalDenied_Returns403WithMessage()
{
    // Arrange
    await SetSelfApprovalPolicy(SelfApprovalPolicy.NeverAllowed);
    var inject = await CreateSubmittedInject(submittedBy: currentUserId);
    
    // Act
    var response = await _client.PostAsync(
        $"/api/injects/{inject.Id}/approve",
        JsonContent.Create(new { }));
    
    // Assert
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    var content = await response.Content.ReadAsStringAsync();
    Assert.Contains("Self-approval is not permitted", content);
}
```

### Frontend Tests

```tsx
describe('useApprovalPermissions', () => {
  it('returns canApprove=false when role not authorized', () => {
    // Arrange
    mockOrgSettings({ approvalAuthorizedRoles: ApprovalRoles.Administrator });
    mockParticipant({ role: ExerciseRole.Controller });
    
    // Act
    const { result } = renderHook(() => useApprovalPermissions(injectId));
    
    // Assert
    expect(result.current.canApprove).toBe(false);
  });
  
  it('returns requiresConfirmation=true for self-approval with warning policy', () => {
    // Arrange
    mockOrgSettings({ selfApprovalPolicy: SelfApprovalPolicy.AllowedWithWarning });
    mockInject({ submittedById: currentUserId });
    
    // Act
    const { result } = renderHook(() => useApprovalPermissions(injectId));
    
    // Assert
    expect(result.current.requiresConfirmation).toBe(true);
  });
});
```

## Out of Scope

- Multi-level approval chains (e.g., Controller approves → Director final approves)
- Time-based approval delegation (temporary authority)
- Approval quotas (e.g., must have 2 approvers)
- Role-based approval limits (e.g., Controller can only approve low-priority injects)

## Dependencies

- S01: Organization approval policy must exist
- S04: Approve/reject functionality must be implemented
- S05: Batch approval must respect these permissions

## Definition of Done

- [ ] ApprovalRoles flags enum created
- [ ] SelfApprovalPolicy enum created
- [ ] Organization entity updated with new fields
- [ ] Database migration for new columns
- [ ] ApprovalPermissionService implemented
- [ ] Approval endpoint updated with permission checks
- [ ] Batch approval respects configured permissions
- [ ] Frontend settings UI for approval permissions
- [ ] Frontend self-approval confirmation dialog
- [ ] useApprovalPermissions hook implemented
- [ ] Approve/reject buttons conditionally rendered
- [ ] Unit tests for permission service
- [ ] Integration tests for API permission enforcement
- [ ] Frontend component tests
- [ ] Audit logging for permission changes
- [ ] Seed data includes varied permission configurations
