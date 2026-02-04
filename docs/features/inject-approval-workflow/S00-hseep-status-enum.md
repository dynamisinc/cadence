# S00: HSEEP-Compliant Inject Status Enum

**Feature:** [Inject Approval Workflow](FEATURE.md)  
**Priority:** P0  
**Points:** 3  
**Dependencies:** None (foundational)

## User Story

**As a** system administrator,  
**I want** inject statuses to align with FEMA PrepToolkit terminology,  
**So that** users familiar with HSEEP tools have a consistent experience.

## Context

The current Cadence implementation uses a simplified status model (Pending, Fired, Skipped) that doesn't match HSEEP standards. FEMA's PrepToolkit defines 8 specific statuses that exercise practitioners expect. Aligning terminology reduces training burden and supports compliance requirements.

This is a foundational change that must be completed before other approval workflow stories.

## HSEEP Status Definitions

| Status | Definition | Cadence Usage |
|--------|------------|---------------|
| **Draft** | Initial status during design/development | Inject being authored, not ready for review |
| **Submitted** | Event sent for review | Awaiting Director approval (when approval enabled) |
| **Approved** | Event approved for use | Director has signed off, ready to schedule |
| **Synchronized** | Approved and scheduled for specific time | Has scheduled time, ready for conduct |
| **Released** | Event delivered to players in real time | Controller has fired the inject |
| **Complete** | Event has transpired | Delivery confirmed, moved past |
| **Deferred** | Synchronized event that was cancelled | Skipped before/during conduct |
| **Obsolete** | Event should be ignored, retained for audit | Soft-deleted, kept for history |

## Acceptance Criteria

### Enum Definition
- [ ] **Given** the codebase, **when** I view `InjectStatus` enum, **then** it contains exactly: Draft, Submitted, Approved, Synchronized, Released, Complete, Deferred, Obsolete
- [ ] **Given** each enum value, **when** I view it, **then** it has XML documentation matching HSEEP definitions
- [ ] **Given** the enum, **when** values are serialized to JSON, **then** they use PascalCase strings (not integers)

### Database Migration
- [ ] **Given** existing injects with old statuses, **when** migration runs, **then** statuses are mapped: Pending→Draft, Fired→Released, Skipped→Deferred
- [ ] **Given** the migration, **when** it completes, **then** no data loss occurs and all injects have valid new status
- [ ] **Given** the migration, **when** rolled back, **then** original statuses are restored

### API Changes
- [ ] **Given** inject API responses, **when** status is returned, **then** it uses new HSEEP status names
- [ ] **Given** inject create/update requests, **when** status is provided, **then** new status values are accepted
- [ ] **Given** invalid status value in request, **when** submitted, **then** 400 Bad Request with validation error

### UI Updates
- [ ] **Given** inject list view, **when** rendered, **then** status chips show new status names
- [ ] **Given** inject detail view, **when** rendered, **then** status displays with correct name
- [ ] **Given** inject forms, **when** status dropdown shown, **then** new values available (where applicable)

### Status Chip Styling
- [ ] **Given** Draft status, **when** chip rendered, **then** it shows gray background with pencil icon
- [ ] **Given** Submitted status, **when** chip rendered, **then** it shows amber/yellow background with clock icon
- [ ] **Given** Approved status, **when** chip rendered, **then** it shows green background with check icon
- [ ] **Given** Synchronized status, **when** chip rendered, **then** it shows blue background with calendar-check icon
- [ ] **Given** Released status, **when** chip rendered, **then** it shows purple background with paper-plane icon
- [ ] **Given** Complete status, **when** chip rendered, **then** it shows dark green background with circle-check icon
- [ ] **Given** Deferred status, **when** chip rendered, **then** it shows orange background with ban icon
- [ ] **Given** Obsolete status, **when** chip rendered, **then** it shows light gray background with archive icon

## Technical Implementation

### Backend: InjectStatus Enum

```csharp
// File: src/Cadence.Core/Enums/InjectStatus.cs

using System.Text.Json.Serialization;

namespace Cadence.Core.Enums;

/// <summary>
/// HSEEP-compliant inject status values per FEMA PrepToolkit.
/// These statuses align with standard exercise management terminology
/// to ensure consistency with federal guidance and training materials.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InjectStatus
{
    /// <summary>
    /// Initial status during design and development phase.
    /// Inject is being authored and is not ready for review or use.
    /// </summary>
    Draft = 0,
    
    /// <summary>
    /// Event has been sent for review by Exercise Director.
    /// Awaiting approval before it can be scheduled for delivery.
    /// Only used when approval workflow is enabled.
    /// </summary>
    Submitted = 1,
    
    /// <summary>
    /// Event has been approved for use in the exercise.
    /// Director has reviewed and signed off on the content.
    /// Ready to be scheduled with a specific delivery time.
    /// </summary>
    Approved = 2,
    
    /// <summary>
    /// Approved event is ready and scheduled for a specific time.
    /// The inject has a scheduled delivery time and will appear
    /// in the Controller's queue when that time approaches.
    /// </summary>
    Synchronized = 3,
    
    /// <summary>
    /// Event has been delivered to players in real time.
    /// Controller has "fired" the inject - delivered the message
    /// via the specified delivery method (phone, email, radio, etc.).
    /// </summary>
    Released = 4,
    
    /// <summary>
    /// Event delivery confirmed, exercise has moved past this inject.
    /// The inject has been delivered and any expected player actions
    /// have occurred or the time window has passed.
    /// </summary>
    Complete = 5,
    
    /// <summary>
    /// A synchronized event that was cancelled before delivery.
    /// The inject was scheduled but was skipped during conduct,
    /// typically due to time constraints or scenario changes.
    /// Requires a reason to be recorded for after-action review.
    /// </summary>
    Deferred = 6,
    
    /// <summary>
    /// Event should be ignored but remains in MSEL for audit trail.
    /// Used for injects that were removed during planning but need
    /// to be retained for historical record. Soft-delete pattern.
    /// </summary>
    Obsolete = 7
}
```

### Backend: Migration

```csharp
// File: src/Cadence.Core/Migrations/YYYYMMDDHHMMSS_UpdateInjectStatusToHseep.cs

public partial class UpdateInjectStatusToHseep : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Map old status values to new HSEEP values
        // Old: Pending (0), Fired (1), Skipped (2)
        // New: Draft (0), Submitted (1), Approved (2), Synchronized (3), 
        //      Released (4), Complete (5), Deferred (6), Obsolete (7)
        
        // Pending (0) → Draft (0) - no change needed
        // Fired (1) → Released (4)
        migrationBuilder.Sql(
            "UPDATE Injects SET Status = 4 WHERE Status = 1");
        
        // Skipped (2) → Deferred (6)
        migrationBuilder.Sql(
            "UPDATE Injects SET Status = 6 WHERE Status = 2");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Reverse the mapping
        // Released (4) → Fired (1)
        migrationBuilder.Sql(
            "UPDATE Injects SET Status = 1 WHERE Status = 4");
        
        // Deferred (6) → Skipped (2)
        migrationBuilder.Sql(
            "UPDATE Injects SET Status = 2 WHERE Status = 6");
        
        // Any other new statuses → Draft (0)
        migrationBuilder.Sql(
            "UPDATE Injects SET Status = 0 WHERE Status IN (1, 2, 3, 5, 7)");
    }
}
```

### Frontend: Status Chip Component

```tsx
// File: src/frontend/src/components/InjectStatusChip.tsx

import { Chip, ChipProps } from '@mui/material';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faPencil,
  faClock,
  faCheck,
  faCalendarCheck,
  faPaperPlane,
  faCircleCheck,
  faBan,
  faArchive,
} from '@fortawesome/free-solid-svg-icons';
import { InjectStatus } from '../types/enums';

interface InjectStatusChipProps {
  status: InjectStatus;
  size?: ChipProps['size'];
}

const statusConfig: Record<InjectStatus, { 
  label: string; 
  color: string; 
  bgColor: string;
  icon: typeof faPencil;
}> = {
  [InjectStatus.Draft]: {
    label: 'Draft',
    color: '#616161',
    bgColor: '#E0E0E0',
    icon: faPencil,
  },
  [InjectStatus.Submitted]: {
    label: 'Submitted',
    color: '#F57C00',
    bgColor: '#FFE0B2',
    icon: faClock,
  },
  [InjectStatus.Approved]: {
    label: 'Approved',
    color: '#388E3C',
    bgColor: '#C8E6C9',
    icon: faCheck,
  },
  [InjectStatus.Synchronized]: {
    label: 'Synchronized',
    color: '#1976D2',
    bgColor: '#BBDEFB',
    icon: faCalendarCheck,
  },
  [InjectStatus.Released]: {
    label: 'Released',
    color: '#7B1FA2',
    bgColor: '#E1BEE7',
    icon: faPaperPlane,
  },
  [InjectStatus.Complete]: {
    label: 'Complete',
    color: '#1B5E20',
    bgColor: '#A5D6A7',
    icon: faCircleCheck,
  },
  [InjectStatus.Deferred]: {
    label: 'Deferred',
    color: '#E65100',
    bgColor: '#FFCC80',
    icon: faBan,
  },
  [InjectStatus.Obsolete]: {
    label: 'Obsolete',
    color: '#9E9E9E',
    bgColor: '#F5F5F5',
    icon: faArchive,
  },
};

export const InjectStatusChip: React.FC<InjectStatusChipProps> = ({ 
  status, 
  size = 'small' 
}) => {
  const config = statusConfig[status];
  
  return (
    <Chip
      size={size}
      label={config.label}
      icon={<FontAwesomeIcon icon={config.icon} />}
      sx={{
        backgroundColor: config.bgColor,
        color: config.color,
        fontWeight: 500,
        '& .MuiChip-icon': {
          color: config.color,
        },
      }}
    />
  );
};
```

### Frontend: Enum Type

```typescript
// File: src/frontend/src/types/enums.ts

/**
 * HSEEP-compliant inject status values per FEMA PrepToolkit.
 */
export enum InjectStatus {
  Draft = 'Draft',
  Submitted = 'Submitted',
  Approved = 'Approved',
  Synchronized = 'Synchronized',
  Released = 'Released',
  Complete = 'Complete',
  Deferred = 'Deferred',
  Obsolete = 'Obsolete',
}

/**
 * Helper to get display-friendly status label.
 */
export const getInjectStatusLabel = (status: InjectStatus): string => {
  return status; // Already display-friendly
};

/**
 * Statuses that indicate an inject is "active" (not terminal).
 */
export const ACTIVE_INJECT_STATUSES: InjectStatus[] = [
  InjectStatus.Draft,
  InjectStatus.Submitted,
  InjectStatus.Approved,
  InjectStatus.Synchronized,
];

/**
 * Statuses that indicate an inject is "terminal" (conduct complete).
 */
export const TERMINAL_INJECT_STATUSES: InjectStatus[] = [
  InjectStatus.Released,
  InjectStatus.Complete,
  InjectStatus.Deferred,
  InjectStatus.Obsolete,
];
```

## Test Cases

### Unit Tests

```csharp
// File: src/Cadence.Core.Tests/Enums/InjectStatusTests.cs

[Fact]
public void InjectStatus_HasCorrectNumberOfValues()
{
    var values = Enum.GetValues<InjectStatus>();
    Assert.Equal(8, values.Length);
}

[Theory]
[InlineData(InjectStatus.Draft, 0)]
[InlineData(InjectStatus.Submitted, 1)]
[InlineData(InjectStatus.Approved, 2)]
[InlineData(InjectStatus.Synchronized, 3)]
[InlineData(InjectStatus.Released, 4)]
[InlineData(InjectStatus.Complete, 5)]
[InlineData(InjectStatus.Deferred, 6)]
[InlineData(InjectStatus.Obsolete, 7)]
public void InjectStatus_HasCorrectUnderlyingValues(InjectStatus status, int expected)
{
    Assert.Equal(expected, (int)status);
}

[Fact]
public void InjectStatus_SerializesToString()
{
    var inject = new Inject { Status = InjectStatus.Synchronized };
    var json = JsonSerializer.Serialize(inject);
    Assert.Contains("\"Status\":\"Synchronized\"", json);
}
```

### Frontend Tests

```typescript
// File: src/frontend/src/components/InjectStatusChip.test.tsx

describe('InjectStatusChip', () => {
  it.each([
    [InjectStatus.Draft, 'Draft'],
    [InjectStatus.Submitted, 'Submitted'],
    [InjectStatus.Approved, 'Approved'],
    [InjectStatus.Synchronized, 'Synchronized'],
    [InjectStatus.Released, 'Released'],
    [InjectStatus.Complete, 'Complete'],
    [InjectStatus.Deferred, 'Deferred'],
    [InjectStatus.Obsolete, 'Obsolete'],
  ])('renders %s status correctly', (status, label) => {
    render(<InjectStatusChip status={status} />);
    expect(screen.getByText(label)).toBeInTheDocument();
  });
});
```

## Out of Scope

- Status transition validation rules (covered in S04)
- Approval workflow logic (covered in S03, S04)
- Batch status updates (covered in S05)
- **Configurable status workflows** (covered in S10) - HSEEP is the default; other frameworks (DoD, NATO, Cybersecurity, Healthcare) addressed via configuration

## Future Consideration: Configurable Statuses

This story implements HSEEP as the default framework. [Cross-domain research](../../research/inject-status-cross-domain-analysis.md) identified significant terminology variation across frameworks. Story S10 implements configurable status workflows to support:

- DoD/JTS (Key, Enabling, Supporting inject types)
- NATO (STARTEX/ENDEX phases)
- Cybersecurity (simplified 4-state workflow)
- Healthcare (HICS integration)
- Financial (RTO/MTD tracking)

The enum defined here serves as the **default template** and **export mapping target** for HSEEP compliance.

## Definition of Done

- [ ] Enum defined with all 8 HSEEP statuses
- [ ] XML documentation on each enum value
- [ ] JSON serialization uses string names
- [ ] Database migration maps old values to new
- [ ] Migration is reversible
- [ ] API returns new status values
- [ ] Frontend enum type updated
- [ ] Status chip component updated with new colors/icons
- [ ] All existing tests pass
- [ ] New unit tests for enum values
- [ ] Seed data updated to use new statuses
- [ ] No regressions in inject list/detail views
