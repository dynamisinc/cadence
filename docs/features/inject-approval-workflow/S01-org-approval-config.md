# S01: Organization Approval Configuration

**Feature:** [Inject Approval Workflow](FEATURE.md)  
**Priority:** P1  
**Points:** 3  
**Dependencies:** S00 (HSEEP Status Enum)

## User Story

**As an** Administrator,  
**I want** to set a default inject approval policy for my organization,  
**So that** all exercises follow our governance standards by default.

## Context

Government agencies and regulated industries often require formal review of all exercise content. Other organizations prefer streamlined workflows for informal exercises. The approval policy should be configurable at the organization level to establish defaults, with the option for exercise-level overrides.

## Acceptance Criteria

### Policy Options
- [ ] **Given** I am an Administrator viewing Organization Settings, **when** I navigate to the Governance section, **then** I see an "Inject Approval Policy" setting
- [ ] **Given** the Inject Approval Policy setting, **when** I view options, **then** I see three choices:
  - "Disabled" - Approval workflow not available
  - "Optional" - Directors can enable per exercise (default)
  - "Required" - Required for all exercises

### Policy Descriptions
- [ ] **Given** each policy option, **when** displayed, **then** it includes a description explaining the behavior:
  - Disabled: "Inject approval workflow is not available. All injects move directly from Draft to Approved."
  - Optional: "Exercise Directors can choose to enable approval workflow per exercise. Approval is disabled by default."
  - Required: "All exercises require inject approval. Directors cannot disable. Administrators can override for specific exercises."

### Save Behavior
- [ ] **Given** I select a new policy, **when** I click Save, **then** the policy is persisted
- [ ] **Given** I save a policy change, **when** successful, **then** I see a success toast "Approval policy updated"
- [ ] **Given** I have unsaved changes, **when** I navigate away, **then** I see a confirmation dialog

### Existing Exercise Impact
- [ ] **Given** I change policy from Optional to Required, **when** saved, **then** existing exercises are NOT retroactively changed (they keep current setting)
- [ ] **Given** I change policy from Optional to Required, **when** creating NEW exercises, **then** approval is enabled by default
- [ ] **Given** I change policy to Disabled, **when** saved, **then** existing exercises with approval enabled continue to function (no data loss)

### Permission Enforcement
- [ ] **Given** I am an Exercise Director (not Admin), **when** I view Organization Settings, **then** I cannot see or modify the approval policy
- [ ] **Given** I am a Controller, **when** I try to access Organization Settings, **then** I receive 403 Forbidden

### Audit Trail
- [ ] **Given** I change the approval policy, **when** saved, **then** an audit log entry is created with old value, new value, user, and timestamp

## UI Design

### Organization Settings Page

```
┌─────────────────────────────────────────────────────────────────┐
│  Organization Settings                                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  General                                                        │
│  ├─ Organization Name: [Metro County EMA        ]               │
│  └─ Time Zone: [America/Chicago            ▼]                   │
│                                                                 │
│  ─────────────────────────────────────────────────────────────  │
│                                                                 │
│  Governance                                                     │
│                                                                 │
│  Inject Approval Policy                                         │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ ○ Disabled                                               │   │
│  │   Approval workflow not available. Injects move          │   │
│  │   directly from Draft to Approved.                       │   │
│  │                                                          │   │
│  │ ● Optional (Recommended)                                 │   │
│  │   Exercise Directors can enable approval per exercise.   │   │
│  │   Approval is disabled by default.                       │   │
│  │                                                          │   │
│  │ ○ Required                                               │   │
│  │   All exercises require approval. Administrators can     │   │
│  │   override for specific exercises.                       │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ─────────────────────────────────────────────────────────────  │
│                                                                 │
│                              [Cancel]  [Save Changes]           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Technical Implementation

### Backend: ApprovalPolicy Enum

```csharp
// File: src/Cadence.Core/Enums/ApprovalPolicy.cs

namespace Cadence.Core.Enums;

/// <summary>
/// Organization-level policy for inject approval workflow.
/// Determines default behavior and constraints for exercises.
/// </summary>
public enum ApprovalPolicy
{
    /// <summary>
    /// Approval workflow is not available.
    /// All injects move directly from Draft to Approved.
    /// Exercise-level toggle is hidden.
    /// </summary>
    Disabled = 0,
    
    /// <summary>
    /// Exercise Directors can choose to enable approval per exercise.
    /// Approval is disabled by default for new exercises.
    /// Recommended for most organizations.
    /// </summary>
    Optional = 1,
    
    /// <summary>
    /// All exercises require inject approval workflow.
    /// Directors cannot disable approval.
    /// Administrators can override for specific exercises.
    /// </summary>
    Required = 2
}
```

### Backend: Organization Entity Update

```csharp
// File: src/Cadence.Core/Entities/Organization.cs (additions)

/// <summary>
/// Organization-level inject approval policy.
/// Determines whether exercises require formal inject approval.
/// </summary>
public ApprovalPolicy InjectApprovalPolicy { get; set; } = ApprovalPolicy.Optional;
```

### Backend: API Endpoint

```csharp
// File: src/Cadence.Core/Controllers/OrganizationsController.cs

/// <summary>
/// Updates organization settings including approval policy.
/// </summary>
/// <param name="id">Organization ID</param>
/// <param name="request">Updated settings</param>
/// <returns>Updated organization settings</returns>
[HttpPut("{id}/settings")]
[Authorize(Roles = "Administrator")]
[ProducesResponseType(typeof(OrganizationSettingsDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<OrganizationSettingsDto>> UpdateSettings(
    Guid id, 
    [FromBody] UpdateOrganizationSettingsRequest request)
{
    // Implementation
}
```

### Backend: DTO

```csharp
// File: src/Cadence.Core/DTOs/OrganizationSettingsDto.cs

public class OrganizationSettingsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TimeZone { get; set; }
    public ApprovalPolicy InjectApprovalPolicy { get; set; }
}

public class UpdateOrganizationSettingsRequest
{
    public string? Name { get; set; }
    public string? TimeZone { get; set; }
    public ApprovalPolicy? InjectApprovalPolicy { get; set; }
}
```

### Frontend: Settings Component

```tsx
// File: src/frontend/src/pages/OrganizationSettings/ApprovalPolicySettings.tsx

import { useState } from 'react';
import { 
  FormControl, 
  FormControlLabel, 
  Radio, 
  RadioGroup,
  Typography,
  Box,
  Paper
} from '@mui/material';
import { ApprovalPolicy } from '../../types/enums';

interface ApprovalPolicySettingsProps {
  value: ApprovalPolicy;
  onChange: (policy: ApprovalPolicy) => void;
  disabled?: boolean;
}

const policyOptions = [
  {
    value: ApprovalPolicy.Disabled,
    label: 'Disabled',
    description: 'Approval workflow not available. Injects move directly from Draft to Approved.',
  },
  {
    value: ApprovalPolicy.Optional,
    label: 'Optional',
    recommended: true,
    description: 'Exercise Directors can enable approval per exercise. Approval is disabled by default.',
  },
  {
    value: ApprovalPolicy.Required,
    label: 'Required',
    description: 'All exercises require approval. Administrators can override for specific exercises.',
  },
];

export const ApprovalPolicySettings: React.FC<ApprovalPolicySettingsProps> = ({
  value,
  onChange,
  disabled = false,
}) => {
  return (
    <Paper variant="outlined" sx={{ p: 2 }}>
      <Typography variant="subtitle1" fontWeight={600} gutterBottom>
        Inject Approval Policy
      </Typography>
      <FormControl component="fieldset" disabled={disabled}>
        <RadioGroup
          value={value}
          onChange={(e) => onChange(e.target.value as ApprovalPolicy)}
        >
          {policyOptions.map((option) => (
            <Box key={option.value} sx={{ mb: 1 }}>
              <FormControlLabel
                value={option.value}
                control={<Radio />}
                label={
                  <Box>
                    <Typography variant="body1" component="span">
                      {option.label}
                      {option.recommended && (
                        <Typography 
                          component="span" 
                          variant="caption" 
                          sx={{ ml: 1, color: 'success.main' }}
                        >
                          (Recommended)
                        </Typography>
                      )}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {option.description}
                    </Typography>
                  </Box>
                }
              />
            </Box>
          ))}
        </RadioGroup>
      </FormControl>
    </Paper>
  );
};
```

### Database Migration

```csharp
// File: src/Cadence.Core/Migrations/YYYYMMDDHHMMSS_AddOrganizationApprovalPolicy.cs

public partial class AddOrganizationApprovalPolicy : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "InjectApprovalPolicy",
            table: "Organizations",
            type: "int",
            nullable: false,
            defaultValue: 1); // Optional
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "InjectApprovalPolicy",
            table: "Organizations");
    }
}
```

## Test Cases

### Unit Tests

```csharp
[Fact]
public async Task UpdateSettings_AsAdmin_UpdatesApprovalPolicy()
{
    // Arrange
    var org = await CreateTestOrganization();
    var request = new UpdateOrganizationSettingsRequest 
    { 
        InjectApprovalPolicy = ApprovalPolicy.Required 
    };
    
    // Act
    var result = await _controller.UpdateSettings(org.Id, request);
    
    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result.Result);
    var settings = Assert.IsType<OrganizationSettingsDto>(okResult.Value);
    Assert.Equal(ApprovalPolicy.Required, settings.InjectApprovalPolicy);
}

[Fact]
public async Task UpdateSettings_AsDirector_Returns403()
{
    // Arrange
    SetCurrentUserRole("ExerciseDirector");
    var request = new UpdateOrganizationSettingsRequest 
    { 
        InjectApprovalPolicy = ApprovalPolicy.Required 
    };
    
    // Act
    var result = await _controller.UpdateSettings(_orgId, request);
    
    // Assert
    Assert.IsType<ForbidResult>(result.Result);
}
```

### Frontend Tests

```typescript
describe('ApprovalPolicySettings', () => {
  it('renders all three policy options', () => {
    render(
      <ApprovalPolicySettings 
        value={ApprovalPolicy.Optional} 
        onChange={jest.fn()} 
      />
    );
    
    expect(screen.getByText('Disabled')).toBeInTheDocument();
    expect(screen.getByText('Optional')).toBeInTheDocument();
    expect(screen.getByText('Required')).toBeInTheDocument();
  });
  
  it('shows recommended badge on Optional', () => {
    render(
      <ApprovalPolicySettings 
        value={ApprovalPolicy.Optional} 
        onChange={jest.fn()} 
      />
    );
    
    expect(screen.getByText('(Recommended)')).toBeInTheDocument();
  });
  
  it('calls onChange when selection changes', async () => {
    const onChange = jest.fn();
    render(
      <ApprovalPolicySettings 
        value={ApprovalPolicy.Optional} 
        onChange={onChange} 
      />
    );
    
    await userEvent.click(screen.getByLabelText(/Required/));
    
    expect(onChange).toHaveBeenCalledWith(ApprovalPolicy.Required);
  });
});
```

## Out of Scope

- Exercise-level configuration (S02)
- Email notification settings
- Custom approval workflows (e.g., multi-level approval)
- **Framework template selection** (S10) - Organizations can select HSEEP, DoD, NATO, etc. frameworks that configure status workflows, inject categories, and other dropdowns

## Related: Framework Templates (S10)

This story establishes the approval policy setting. Organizations using non-HSEEP frameworks (DoD, NATO, Cybersecurity, etc.) may have different approval workflow expectations. S10 implements framework template selection that works alongside this approval policy configuration. The approval policy (Disabled/Optional/Required) applies regardless of which framework template is active.

## Domain Terms

| Term | Definition |
|------|------------|
| Approval Policy | Organization-wide setting controlling whether inject approval is available, optional, or required |
| Governance | Rules and processes ensuring exercise quality and compliance |

## Definition of Done

- [ ] ApprovalPolicy enum created with documentation
- [ ] Organization entity updated with new field
- [ ] Database migration created and tested
- [ ] API endpoint for updating settings
- [ ] Permission check for Administrator role
- [ ] Frontend settings component
- [ ] Radio button group with descriptions
- [ ] Success/error toast notifications
- [ ] Unsaved changes warning
- [ ] Unit tests for backend
- [ ] Component tests for frontend
- [ ] Audit logging for policy changes
- [ ] Seed data includes organization with policy set
