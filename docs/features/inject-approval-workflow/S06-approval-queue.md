# S06: Approval Queue View

**Feature:** [Inject Approval Workflow](FEATURE.md)  
**Priority:** P1  
**Points:** 3  
**Dependencies:** S03 (Submit Inject for Approval)

## User Story

**As an** Exercise Director,  
**I want** to see all injects pending my approval,  
**So that** I can efficiently review submissions without hunting through the full MSEL.

## Context

Large MSELs can have 50+ injects across multiple phases. Directors need a focused view showing only items requiring their attention. The approval queue provides this filtered view with count badges for quick visibility.

## Acceptance Criteria

### Queue Tab/Filter
- [ ] **Given** I am Director or Admin viewing the MSEL, **when** approval is enabled, **then** I see a "Pending Approval" tab/filter option
- [ ] **Given** "Pending Approval" tab, **when** there are Submitted injects, **then** tab shows count badge (e.g., "Pending Approval (5)")
- [ ] **Given** I click "Pending Approval", **when** activated, **then** list shows only Submitted injects
- [ ] **Given** no injects are Submitted, **when** I view "Pending Approval", **then** I see empty state "No injects pending approval"

### Dashboard Alert
- [ ] **Given** I am Director or Admin on Exercise Dashboard, **when** there are Submitted injects, **then** I see alert "X injects pending approval"
- [ ] **Given** the dashboard alert, **when** I click it, **then** I navigate to MSEL with "Pending Approval" filter active
- [ ] **Given** no Submitted injects, **when** I view Dashboard, **then** the pending approval alert is not shown

### Queue Sorting
- [ ] **Given** I am viewing the approval queue, **when** displayed, **then** injects are sorted by SubmittedAt (oldest first)
- [ ] **Given** the queue, **when** I click a column header, **then** I can sort by other columns (Title, Scheduled Time, etc.)

### Self-Submission Indicator
- [ ] **Given** I am viewing the approval queue, **when** an inject was submitted by me, **then** it shows visual indicator (e.g., "Your submission" badge)
- [ ] **Given** my own submission in queue, **when** I view it, **then** Approve button is disabled with tooltip

### Quick Actions in Queue
- [ ] **Given** I am viewing the approval queue, **when** I see an inject row, **then** I see Approve/Reject quick action buttons
- [ ] **Given** I click Approve in the row, **when** clicked, **then** approval dialog opens (same as detail view)
- [ ] **Given** I approve/reject from queue, **when** action completes, **then** inject disappears from queue (status changed)

### Approval Summary
- [ ] **Given** approval is enabled on an exercise, **when** I view the MSEL header, **then** I see approval summary: "X of Y injects approved"
- [ ] **Given** all injects are approved, **when** I view summary, **then** it shows green checkmark "All injects approved"

### Controller View (Limited)
- [ ] **Given** I am Controller viewing MSEL, **when** I look for approval queue, **then** I can see "My Submissions" filter
- [ ] **Given** "My Submissions" filter, **when** activated, **then** shows injects I submitted with their current status

## UI Design

### MSEL View with Approval Tabs

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Hurricane Response TTX - MSEL                                              │
│                                                                             │
│  Approval Status: 8 of 14 injects approved  ▓▓▓▓▓▓▓▓░░░░░░ 57%             │
│                                                                             │
│  ┌──────────────┬─────────────────────┬────────────────┐                    │
│  │ All Injects  │ Pending Approval (6) │ My Submissions │                   │
│  │     (14)     │         ●           │      (3)       │                   │
│  └──────────────┴─────────────────────┴────────────────┘                    │
│                                                                             │
│  [Filter ▼]  [Sort ▼]  [Search...]                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│ ☐ │ # │ Title                    │ Submitted    │ By        │ Actions      │
├───┼───┼──────────────────────────┼──────────────┼───────────┼──────────────┤
│ ☐ │ 2 │ EOC Activation Notice    │ Jan 15 09:30 │ John D.   │ [✓] [✗]      │
│ ☐ │ 3 │ Shelter Capacity Report  │ Jan 15 10:15 │ You       │ [✓] [✗]      │
│ ☐ │ 5 │ Media Inquiry            │ Jan 15 11:00 │ Sarah M.  │ [✓] [✗]      │
│ ☐ │ 7 │ Evacuation Route Update  │ Jan 16 08:00 │ Mike R.   │ [✓] [✗]      │
│ ☐ │ 9 │ Hospital Status Report   │ Jan 16 09:30 │ John D.   │ [✓] [✗]      │
│ ☐ │11 │ Resource Staging Request │ Jan 16 10:00 │ Sarah M.  │ [✓] [✗]      │
└───┴───┴──────────────────────────┴──────────────┴───────────┴──────────────┘
```

### Exercise Dashboard Alert

```
┌─────────────────────────────────────────────────────────────────┐
│  Hurricane Response TTX                                         │
│  Exercise Dashboard                                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ ⚠️  6 injects pending approval                           │   │
│  │     Review and approve before exercise can begin.        │   │
│  │                                            [Review Now →] │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  Quick Stats                                                    │
│  ├─ Total Injects: 14                                           │
│  ├─ Approved: 8                                                 │
│  ├─ Pending: 6                                                  │
│  └─ Draft: 0                                                    │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Empty Queue State

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Hurricane Response TTX - MSEL                                              │
│                                                                             │
│  Approval Status: 14 of 14 injects approved  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓ 100% ✓        │
│                                                                             │
│  ┌──────────────┬─────────────────────┬────────────────┐                    │
│  │ All Injects  │ Pending Approval    │ My Submissions │                   │
│  │     (14)     │         ●           │      (3)       │                   │
│  └──────────────┴─────────────────────┴────────────────┘                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│                    ┌─────────────────────────────┐                          │
│                    │                             │                          │
│                    │      ✓ All caught up!       │                          │
│                    │                             │                          │
│                    │   No injects pending        │                          │
│                    │   approval.                 │                          │
│                    │                             │                          │
│                    │   [View All Injects]        │                          │
│                    │                             │                          │
│                    └─────────────────────────────┘                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Technical Implementation

### Backend: Approval Status Endpoint

```csharp
// File: src/Cadence.Core/Controllers/ExercisesController.cs

public class ApprovalStatusDto
{
    public int TotalInjects { get; set; }
    public int ApprovedCount { get; set; }
    public int PendingApprovalCount { get; set; }
    public int DraftCount { get; set; }
    public decimal ApprovalPercentage { get; set; }
    public bool AllApproved { get; set; }
}

/// <summary>
/// Gets approval status summary for an exercise.
/// </summary>
[HttpGet("{id}/approval-status")]
public async Task<ActionResult<ApprovalStatusDto>> GetApprovalStatus(Guid id)
{
    var exercise = await _context.Exercises
        .Include(e => e.Msels)
            .ThenInclude(m => m.Injects)
        .FirstOrDefaultAsync(e => e.Id == id)
        ?? throw new NotFoundException("Exercise not found");
    
    var injects = exercise.Msels.SelectMany(m => m.Injects).ToList();
    
    var status = new ApprovalStatusDto
    {
        TotalInjects = injects.Count,
        ApprovedCount = injects.Count(i => 
            i.Status >= InjectStatus.Approved && 
            i.Status != InjectStatus.Obsolete),
        PendingApprovalCount = injects.Count(i => i.Status == InjectStatus.Submitted),
        DraftCount = injects.Count(i => i.Status == InjectStatus.Draft),
    };
    
    status.ApprovalPercentage = status.TotalInjects > 0 
        ? (decimal)status.ApprovedCount / status.TotalInjects * 100 
        : 100;
    status.AllApproved = status.PendingApprovalCount == 0 && status.DraftCount == 0;
    
    return Ok(status);
}
```

### Backend: Inject Query with Status Filter

```csharp
// File: src/Cadence.Core/Controllers/InjectsController.cs

/// <summary>
/// Gets injects for an MSEL with optional status filter.
/// </summary>
[HttpGet]
public async Task<ActionResult<List<InjectDto>>> GetInjects(
    Guid mselId,
    [FromQuery] InjectStatus? status = null,
    [FromQuery] bool mySubmissionsOnly = false,
    [FromQuery] string? sortBy = "scheduledTime",
    [FromQuery] string? sortOrder = "asc")
{
    var query = _context.Injects
        .Where(i => i.MselId == mselId);
    
    if (status.HasValue)
    {
        query = query.Where(i => i.Status == status.Value);
    }
    
    if (mySubmissionsOnly)
    {
        var userId = GetUserId(User);
        query = query.Where(i => i.SubmittedById == userId || i.CreatedById == userId);
    }
    
    // Apply sorting
    query = sortBy?.ToLower() switch
    {
        "title" => sortOrder == "desc" 
            ? query.OrderByDescending(i => i.Title) 
            : query.OrderBy(i => i.Title),
        "submittedat" => sortOrder == "desc" 
            ? query.OrderByDescending(i => i.SubmittedAt) 
            : query.OrderBy(i => i.SubmittedAt),
        _ => sortOrder == "desc" 
            ? query.OrderByDescending(i => i.ScheduledTime) 
            : query.OrderBy(i => i.ScheduledTime),
    };
    
    var injects = await query.ToListAsync();
    return Ok(_mapper.Map<List<InjectDto>>(injects));
}
```

### Frontend: Approval Status Header

```tsx
// File: src/frontend/src/components/ApprovalStatusHeader.tsx

interface ApprovalStatusHeaderProps {
  status: ApprovalStatus;
}

export const ApprovalStatusHeader: React.FC<ApprovalStatusHeaderProps> = ({ 
  status 
}) => {
  const progressColor = status.allApproved 
    ? 'success' 
    : status.approvalPercentage >= 75 
      ? 'primary' 
      : 'warning';
  
  return (
    <Box sx={{ mb: 2 }}>
      <Box display="flex" alignItems="center" gap={2}>
        <Typography variant="body2" color="text.secondary">
          Approval Status: {status.approvedCount} of {status.totalInjects} injects approved
        </Typography>
        
        {status.allApproved && (
          <Chip
            icon={<CheckCircleIcon />}
            label="All approved"
            color="success"
            size="small"
          />
        )}
      </Box>
      
      <LinearProgress
        variant="determinate"
        value={status.approvalPercentage}
        color={progressColor}
        sx={{ mt: 1, height: 8, borderRadius: 1 }}
      />
    </Box>
  );
};
```

### Frontend: Queue Tabs Component

```tsx
// File: src/frontend/src/components/MselTabs.tsx

interface MselTabsProps {
  activeTab: 'all' | 'pending' | 'mySubmissions';
  onTabChange: (tab: 'all' | 'pending' | 'mySubmissions') => void;
  totalCount: number;
  pendingCount: number;
  mySubmissionsCount: number;
  isApprovalEnabled: boolean;
}

export const MselTabs: React.FC<MselTabsProps> = ({
  activeTab,
  onTabChange,
  totalCount,
  pendingCount,
  mySubmissionsCount,
  isApprovalEnabled,
}) => {
  if (!isApprovalEnabled) {
    return null; // No tabs when approval is disabled
  }
  
  return (
    <Tabs
      value={activeTab}
      onChange={(_, tab) => onTabChange(tab)}
      sx={{ mb: 2 }}
    >
      <Tab 
        value="all" 
        label={`All Injects (${totalCount})`} 
      />
      <Tab
        value="pending"
        label={
          <Badge badgeContent={pendingCount} color="warning">
            Pending Approval
          </Badge>
        }
      />
      <Tab 
        value="mySubmissions" 
        label={`My Submissions (${mySubmissionsCount})`} 
      />
    </Tabs>
  );
};
```

### Frontend: Dashboard Alert

```tsx
// File: src/frontend/src/components/PendingApprovalAlert.tsx

interface PendingApprovalAlertProps {
  exerciseId: string;
  pendingCount: number;
}

export const PendingApprovalAlert: React.FC<PendingApprovalAlertProps> = ({
  exerciseId,
  pendingCount,
}) => {
  const navigate = useNavigate();
  
  if (pendingCount === 0) {
    return null;
  }
  
  return (
    <Alert 
      severity="warning"
      action={
        <Button 
          color="inherit" 
          size="small"
          onClick={() => navigate(`/exercises/${exerciseId}/msel?tab=pending`)}
        >
          Review Now →
        </Button>
      }
    >
      <AlertTitle>{pendingCount} injects pending approval</AlertTitle>
      Review and approve before exercise can begin.
    </Alert>
  );
};
```

## Test Cases

### Unit Tests

```csharp
[Fact]
public async Task GetApprovalStatus_ReturnsCorrectCounts()
{
    // Arrange
    var exercise = await CreateExerciseWithMixedStatusInjects();
    // 2 Draft, 3 Submitted, 5 Approved
    
    // Act
    var result = await _controller.GetApprovalStatus(exercise.Id);
    
    // Assert
    var status = result.Value;
    Assert.Equal(10, status.TotalInjects);
    Assert.Equal(5, status.ApprovedCount);
    Assert.Equal(3, status.PendingApprovalCount);
    Assert.Equal(2, status.DraftCount);
    Assert.Equal(50, status.ApprovalPercentage);
    Assert.False(status.AllApproved);
}

[Fact]
public async Task GetInjects_PendingFilter_ReturnsOnlySubmitted()
{
    // Arrange
    var msel = await CreateMselWithMixedInjects();
    
    // Act
    var result = await _controller.GetInjects(
        msel.Id, 
        status: InjectStatus.Submitted);
    
    // Assert
    Assert.All(result.Value, inject => 
        Assert.Equal(InjectStatus.Submitted, inject.Status));
}

[Fact]
public async Task GetInjects_MySubmissionsFilter_ReturnsUserInjects()
{
    // Arrange
    var msel = await CreateMselWithMultipleAuthors();
    SetCurrentUser(_controller, _testUser);
    
    // Act
    var result = await _controller.GetInjects(
        msel.Id, 
        mySubmissionsOnly: true);
    
    // Assert
    Assert.All(result.Value, inject => 
        Assert.True(
            inject.SubmittedById == _testUser.Id || 
            inject.CreatedById == _testUser.Id));
}
```

### Frontend Tests

```typescript
describe('ApprovalStatusHeader', () => {
  it('shows all approved chip when complete', () => {
    render(
      <ApprovalStatusHeader
        status={{
          totalInjects: 10,
          approvedCount: 10,
          pendingApprovalCount: 0,
          draftCount: 0,
          approvalPercentage: 100,
          allApproved: true,
        }}
      />
    );
    
    expect(screen.getByText('All approved')).toBeInTheDocument();
  });
  
  it('shows correct progress percentage', () => {
    render(
      <ApprovalStatusHeader
        status={{
          totalInjects: 10,
          approvedCount: 5,
          pendingApprovalCount: 3,
          draftCount: 2,
          approvalPercentage: 50,
          allApproved: false,
        }}
      />
    );
    
    expect(screen.getByText(/5 of 10 injects approved/)).toBeInTheDocument();
  });
});
```

## Out of Scope

- Email reminders for pending approvals
- Approval queue on main dashboard (cross-exercise)
- Approval deadline tracking

## Definition of Done

- [ ] Approval status endpoint returns correct counts
- [ ] Inject query supports status filter parameter
- [ ] Inject query supports mySubmissionsOnly filter
- [ ] MSEL tabs component with badge counts
- [ ] Pending Approval tab shows only Submitted injects
- [ ] My Submissions tab shows user's injects
- [ ] Approval status progress bar in MSEL header
- [ ] Dashboard alert for pending approvals
- [ ] Empty state for queue when all approved
- [ ] Quick action buttons in queue rows
- [ ] Self-submission indicator in queue
- [ ] Sorting by SubmittedAt in queue
- [ ] Unit tests for endpoint filters
- [ ] Frontend component tests
