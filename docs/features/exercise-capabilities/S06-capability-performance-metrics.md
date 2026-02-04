# Story: Capability Performance Metrics

**Feature:** Exercise Capabilities  
**Story ID:** S06  
**Priority:** P1 (Standard)  
**Phase:** Standard Implementation

---

## User Story

**As a** Director or Emergency Manager,  
**I want** to see performance metrics broken down by capability,  
**So that** I can identify which organizational capabilities need improvement and prioritize training.

---

## Context

This story wires capability data into the Metrics feature to enable capability-based performance analysis. Per HSEEP methodology, exercise evaluation assesses performance against capability-based objectives. This view shows:

1. **Which capabilities were evaluated** and their P/S/M/U ratings
2. **Capability gaps** - target capabilities with no observations
3. **Performance distribution** - how ratings are spread across capabilities
4. **Improvement priorities** - capabilities with lowest performance

This story integrates with the existing Metrics feature and depends on:
- Capabilities being defined in the organization library (S01-S03)
- Exercises having target capabilities (S04)
- Observations being tagged with capabilities (S05)
- **EEG Entries providing structured assessments (EEG Feature)**

### Data Sources: Observations vs. EEG Entries

Capability metrics now have **two data sources** with different characteristics:

| Source | Structure | Rating | Use Case |
|--------|-----------|--------|----------|
| **EEG Entries** | Structured by Critical Task | Required P/S/M/U | Formal HSEEP evaluation |
| **Observations** | Ad-hoc capability tagging | Optional P/S/M/U | General categorization |

**Metrics Priority:**
1. **EEG Entries are the primary source** for capability performance when available
2. **Observations provide supplemental data** for capabilities without EEG setup
3. Metrics clearly distinguish between EEG-based and observation-based assessments

This approach ensures:
- Formal exercises with EEG setup show structured, task-level evaluation
- Simple exercises (TTX) without EEG can still show observation-based metrics
- Users understand the data quality difference

---

## Acceptance Criteria

### Core Metrics Display

- [ ] **Given** I am viewing exercise metrics, **when** the exercise has EEG entries or capability-tagged observations, **then** I see "Capability Performance" section
- [ ] **Given** the Capability Performance section, **when** displayed, **then** I see each capability with evaluation count and aggregate rating
- [ ] **Given** the capability list, **when** displayed, **then** capabilities are sorted by rating (worst first) to highlight improvement areas
- [ ] **Given** a capability row, **when** hovered, **then** I see rating distribution breakdown (P/S/M/U counts)
- [ ] **Given** the exercise has target capabilities, **when** viewing, **then** I see coverage metric (X of Y target capabilities evaluated)
- [ ] **Given** target capabilities, **when** some have NO evaluations, **then** they are listed in a "Not Evaluated" section

### EEG Integration

- [ ] **Given** exercise has EEG Capability Targets, **when** viewing metrics, **then** I see EEG-based metrics prominently
- [ ] **Given** EEG-based capability row, **when** displayed, **then** I see "EEG" indicator and task coverage (e.g., "3/4 tasks evaluated")
- [ ] **Given** capability has only observation tags, **when** displayed, **then** I see "Observation" indicator
- [ ] **Given** capability has both EEG and observation data, **when** displayed, **then** EEG data takes priority with observation count noted
- [ ] **Given** EEG Critical Task coverage, **when** viewing capability detail, **then** I see task-by-task breakdown with ratings

### Drilldown and Navigation

- [ ] **Given** a capability row, **when** I click it, **then** I see detail panel with all evaluations (EEG entries and observations)
- [ ] **Given** the detail panel, **when** capability has EEG setup, **then** I see Critical Tasks with their EEG entries
- [ ] **Given** the detail panel, **when** I click an EEG entry or observation, **then** I navigate to its detail view

### Edge Cases

- [ ] **Given** no capability tags or EEG entries exist, **when** viewing metrics, **then** I see message encouraging capability-based evaluation
- [ ] **Given** exercise has EEG setup but no entries yet, **when** viewing metrics, **then** I see EEG coverage at 0% with prompt
- [ ] **Given** view toggle, **when** I select different sort orders, **then** list reorders (Rating/Alphabetical/Category)

---

## Out of Scope

- Capability trend analysis across multiple exercises (Organization-level metrics)
- FEMA capability definitions/descriptions display
- Capability-specific recommendations
- Automated capability inference from observation text
- Export to AAR/IP format (see EEG Feature S10)

---

## Dependencies

- S01: Capability Entity and API
- S04: Exercise Target Capabilities  
- S05: Observation Capability Tagging
- **EEG Feature S01-S02**: Capability Targets and Critical Tasks (for EEG structure)
- **EEG Feature S06-S07**: EEG Entries (for structured ratings)
- Metrics feature (existing dashboard)

---

## Open Questions

- [x] Should we show all capabilities or just those with observations? **Show both - evaluated and gaps**
- [x] How to handle capabilities not in target list but tagged? **Include in "Other Evaluated" section**
- [x] Should capability ratings weight by observation importance? **No, simple average for MVP**
- [x] How to combine EEG and Observation data? **EEG takes priority; observations are supplemental**
- [ ] Do we need mission area grouping view option? **Defer to enhancement**

---

## Domain Terms

| Term | Definition |
|------|------------|
| Capability Performance | Aggregate P/S/M/U rating across all evaluations for a capability |
| Coverage | Percentage of target capabilities that have at least one evaluation |
| Capability Gap | A target capability that was not evaluated (no EEG entries or observations) |
| Average Rating | Mean of numeric rating values: P=1, S=2, M=3, U=4 |
| EEG-Based Metrics | Ratings derived from structured EEG Entries against Critical Tasks |
| Observation-Based Metrics | Ratings derived from ad-hoc observation capability tagging |
| Task Coverage | For EEG: percentage of Critical Tasks that have at least one EEG entry |

---

## UI/UX Notes

### Capability Performance Section in Metrics Dashboard

```
┌─────────────────────────────────────────────────────────────────────────┐
│  CAPABILITY PERFORMANCE                                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  View: [● Rating] [○ Alphabetical] [○ Category]                        │
│                                                                         │
│  Target Capabilities Evaluated: 5 of 7                                  │
│  ████████████████████████████████████░░░░░░░░░░  71% coverage          │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  ⚠️ NEEDS IMPROVEMENT                                                   │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  Operational Communications               📋 EEG   ● Response     │ │
│  │  ████████████████████░░░░░░░░░░░░  Avg: Marginal (2.8)           │ │
│  │  Tasks: 3/4 evaluated • EEG Entries: 5                            │ │
│  │  [P: 0] [S: 1] [M: 3] [U: 1]                            [View →] │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  Public Information and Warning           📋 EEG   ● Prevention   │ │
│  │  ████████████████░░░░░░░░░░░░░░░░  Avg: Marginal (2.5)           │ │
│  │  Tasks: 2/2 evaluated • EEG Entries: 4                            │ │
│  │  [P: 0] [S: 2] [M: 1] [U: 1]                            [View →] │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  ✓ SATISFACTORY                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  Mass Care Services                       📋 EEG   ● Response     │ │
│  │  ████████░░░░░░░░░░░░░░░░░░░░░░░░  Avg: Satisfactory (1.8)       │ │
│  │  Tasks: 4/4 evaluated • EEG Entries: 8                [View →]   │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  Planning                                 📝 Obs   ● All Areas    │ │
│  │  ████████░░░░░░░░░░░░░░░░░░░░░░░░  Avg: Performed (1.5)          │ │
│  │  Observations: 6                                      [View →]   │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  ⊘ NOT EVALUATED (Target Capabilities)                                 │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  • Intelligence and Information Sharing (EEG: 0/3 tasks, Obs: 0)       │
│  • Logistics and Supply Chain Management (No EEG setup, Obs: 0)        │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  ○ OTHER EVALUATED (Non-Target)                                        │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  Critical Transportation                  📝 Obs   ● Response     │ │
│  │  ████████████████░░░░░░░░░░░░░░░░  Avg: Satisfactory (2.0)       │ │
│  │  Observations: 2                                      [View →]   │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Capability Detail Panel (EEG-Based)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Operational Communications                                     [X]    │
│  Target: "Establish interoperable communications within 30 minutes"    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Aggregate Rating: Marginal (2.8)            📋 EEG-Based Evaluation   │
│  Task Coverage: 3 of 4 tasks (75%)                                     │
│  Total EEG Entries: 5                                                   │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  CRITICAL TASK BREAKDOWN                                                │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │ Task: Activate emergency communication plan                       │ │
│  │ Entries: 2 • Avg Rating: S (2.0)                                  │ │
│  │ ├─ 10:45 │ S │ EOC issued activation at 09:15...        R. Chen  │ │
│  │ └─ 10:12 │ S │ Notification sent within 5 min...        S. Kim   │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │ Task: Establish radio net with field units                        │ │
│  │ Entries: 2 • Avg Rating: M (3.0)                                  │ │
│  │ ├─ 10:32 │ M │ Radio net established but Field...       R. Chen  │ │
│  │ └─ 09:55 │ M │ Significant delays in establishing...    M. Jones │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │ Task: Test backup communication systems                           │ │
│  │ Entries: 1 • Avg Rating: U (4.0)                                  │ │
│  │ └─ 11:05 │ U │ Backup systems were not tested...        S. Kim   │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │ Task: Verify interoperability with mutual aid partners            │ │
│  │ ⚠️ Not Evaluated                                                  │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  SUPPLEMENTAL OBSERVATIONS                                              │
│  (Ad-hoc observations tagged with this capability)                     │
│                                                                         │
│  • 09:30 │ M │ "Communication breakdown between EOC..."    J. Smith │ │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Rating Scale Reference

```
Rating Scale:
│  1.0 ──── 2.0 ──── 3.0 ──── 4.0  │
│   P        S        M        U   │
│  Performed  Satisfactory  Marginal  Unsatisfactory
```

### Data Source Indicators

```
📋 EEG  = Structured assessment via EEG Entries (recommended for formal evaluation)
📝 Obs  = Ad-hoc observation tagging (supplemental data)
```

### Empty State (No Capability Data)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  CAPABILITY PERFORMANCE                                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│                    📊                                                   │
│                                                                         │
│              No capability data available                               │
│                                                                         │
│    For structured evaluation, define Capability Targets in EEG Setup   │
│    and record EEG Entries during conduct.                              │
│                                                                         │
│    For basic metrics, tag observations with capabilities.              │
│                                                                         │
│    [Set Up EEG →]  [View Observations →]                               │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

### API Endpoint

```csharp
[HttpGet("{exerciseId}/metrics/capabilities")]
public async Task<ActionResult<CapabilityMetricsDto>> GetCapabilityMetrics(
    Guid exerciseId)
```

### Response DTOs

```csharp
public record CapabilityMetricsDto(
    int TargetCapabilityCount,
    int EvaluatedTargetCount,
    decimal CoveragePercentage,
    List<CapabilityPerformanceDto> EvaluatedCapabilities,
    List<CapabilityGapDto> NotEvaluatedTargets
);

public record CapabilityPerformanceDto(
    Guid CapabilityId,
    string Name,
    string? Category,
    bool IsTargetCapability,
    
    // Evaluation data
    EvaluationDataSource DataSource,  // EEG, Observation, or Both
    int TotalEvaluationCount,
    decimal AverageRating,           // 1.0-4.0
    string AverageRatingLabel,       // "Performed", "Satisfactory", etc.
    RatingDistributionDto Distribution,
    
    // EEG-specific (null if no EEG setup)
    EegMetricsDto? EegMetrics,
    
    // Observation-specific
    int ObservationCount
);

public enum EvaluationDataSource
{
    EEG,          // Has EEG entries (may also have observations)
    Observation,  // Only observations, no EEG
    Both,         // EEG is primary, observations supplemental
    None          // No evaluations
}

public record EegMetricsDto(
    Guid CapabilityTargetId,
    string TargetDescription,
    int TotalTasks,
    int EvaluatedTasks,
    decimal TaskCoveragePercentage,
    int TotalEegEntries,
    List<CriticalTaskMetricDto> TaskMetrics
);

public record CriticalTaskMetricDto(
    Guid TaskId,
    string TaskDescription,
    int EntryCount,
    decimal? AverageRating,
    string? AverageRatingLabel,
    bool IsEvaluated
);

public record RatingDistributionDto(
    int Performed,     // P count
    int Satisfactory,  // S count
    int Marginal,      // M count
    int Unsatisfactory // U count
);

public record CapabilityGapDto(
    Guid CapabilityId,
    string Name,
    string? Category,
    bool HasEegSetup,
    int EegTaskCount,    // If EEG setup exists, how many tasks defined
    int ObservationCount // Number of observations (even if all unrated)
);
```

### Service Layer

```csharp
public class CapabilityMetricsService
{
    public async Task<CapabilityMetricsDto> GetCapabilityMetricsAsync(Guid exerciseId)
    {
        // 1. Get target capabilities for exercise
        var targetCapabilities = await GetTargetCapabilitiesAsync(exerciseId);
        
        // 2. Get EEG-based metrics (Capability Targets → Critical Tasks → EEG Entries)
        var eegMetrics = await GetEegMetricsAsync(exerciseId);
        
        // 3. Get observation-based metrics
        var observationMetrics = await GetObservationMetricsAsync(exerciseId);
        
        // 4. Merge metrics, prioritizing EEG data
        var mergedCapabilities = MergeCapabilityMetrics(
            targetCapabilities, 
            eegMetrics, 
            observationMetrics);
        
        // 5. Identify gaps
        var gaps = IdentifyCapabilityGaps(targetCapabilities, mergedCapabilities);
        
        // 6. Calculate coverage
        var evaluatedTargetCount = mergedCapabilities
            .Count(c => c.IsTargetCapability && c.DataSource != EvaluationDataSource.None);
        
        return new CapabilityMetricsDto(
            TargetCapabilityCount: targetCapabilities.Count,
            EvaluatedTargetCount: evaluatedTargetCount,
            CoveragePercentage: targetCapabilities.Count > 0 
                ? (decimal)evaluatedTargetCount / targetCapabilities.Count * 100 
                : 0,
            EvaluatedCapabilities: mergedCapabilities
                .Where(c => c.DataSource != EvaluationDataSource.None)
                .OrderByDescending(c => c.AverageRating)  // Worst first
                .ToList(),
            NotEvaluatedTargets: gaps
        );
    }
    
    private async Task<List<EegCapabilityMetric>> GetEegMetricsAsync(Guid exerciseId)
    {
        // Query: CapabilityTargets → CriticalTasks → EegEntries
        // Group by Capability, aggregate ratings
        return await _context.CapabilityTargets
            .Where(ct => ct.ExerciseId == exerciseId)
            .Include(ct => ct.Capability)
            .Include(ct => ct.CriticalTasks)
                .ThenInclude(task => task.EegEntries)
            .Select(ct => new EegCapabilityMetric
            {
                CapabilityId = ct.CapabilityId,
                CapabilityName = ct.Capability.Name,
                Category = ct.Capability.Category,
                TargetId = ct.Id,
                TargetDescription = ct.TargetDescription,
                Tasks = ct.CriticalTasks.Select(task => new TaskMetric
                {
                    TaskId = task.Id,
                    TaskDescription = task.TaskDescription,
                    Entries = task.EegEntries.Select(e => e.Rating).ToList()
                }).ToList()
            })
            .ToListAsync();
    }
    
    private async Task<List<ObservationCapabilityMetric>> GetObservationMetricsAsync(Guid exerciseId)
    {
        // Query: Observations with capability tags, grouped by capability
        return await _context.Observations
            .Where(o => o.ExerciseId == exerciseId)
            .SelectMany(o => o.ObservationCapabilities
                .Select(oc => new { Observation = o, Capability = oc.Capability }))
            .GroupBy(x => x.Capability.Id)
            .Select(g => new ObservationCapabilityMetric
            {
                CapabilityId = g.Key,
                CapabilityName = g.First().Capability.Name,
                Category = g.First().Capability.Category,
                Ratings = g.Where(x => x.Observation.Rating != null)
                    .Select(x => x.Observation.Rating!)
                    .ToList(),
                ObservationCount = g.Count()
            })
            .ToListAsync();
    }
    
    private decimal CalculateAverageRating(IEnumerable<string> ratings)
    {
        // Convert P/S/M/U to numeric and average
        var values = ratings
            .Select(r => r switch
            {
                "P" => 1m,
                "S" => 2m,
                "M" => 3m,
                "U" => 4m,
                _ => 0m
            })
            .Where(v => v > 0)
            .ToList();
        
        return values.Any() ? values.Average() : 0;
    }
    
    private string GetRatingLabel(decimal avgRating)
    {
        return avgRating switch
        {
            <= 1.5m => "Performed",
            <= 2.5m => "Satisfactory",
            <= 3.5m => "Marginal",
            _ => "Unsatisfactory"
        };
    }
}
```

### Frontend Component

```typescript
// CapabilityPerformanceMetrics.tsx
interface CapabilityPerformanceMetricsProps {
  exerciseId: string;
}

// Fetches GET /api/exercises/{id}/metrics/capabilities
// Renders:
// - Coverage progress bar
// - Sorted capability list with performance bars
// - Data source indicators (EEG vs Observation)
// - Gap list (not evaluated targets)
// - Detail panel with task breakdown for EEG
// - Empty state if no capability data
```

---

## Estimation

**T-Shirt Size:** L  
**Story Points:** 8 (increased from 5 due to EEG integration)

---

## Testing Requirements

### Unit Tests
- [ ] Average rating calculation (P/S/M/U → numeric)
- [ ] Rating label thresholds
- [ ] Distribution counting
- [ ] Gap detection for target capabilities
- [ ] EEG metrics aggregation
- [ ] Observation metrics aggregation
- [ ] Metrics merging (EEG priority)

### Integration Tests
- [ ] API returns correct metrics structure
- [ ] Coverage percentage calculation
- [ ] Sorting by worst rating first
- [ ] Empty state when no capability data
- [ ] EEG-based capability shows task breakdown
- [ ] Observation-only capability shows observation count
- [ ] Mixed data source handling

### E2E Tests
- [ ] View capability metrics for exercise with EEG entries
- [ ] View capability metrics for exercise with observations only
- [ ] Click capability to see detail panel with task breakdown
- [ ] Toggle sort order
- [ ] View exercise with no capability data (empty state)

---

## Related Features

| Feature | Relationship |
|---------|--------------|
| S04 Target Capabilities | Provides target capability list for coverage calculation |
| S05 Observation Tagging | Provides observation-based capability data |
| **EEG Feature S01-S04** | Provides Capability Target and Critical Task structure |
| **EEG Feature S06-S07** | Provides EEG Entry data for structured ratings |
| **EEG Feature S09** | EEG Coverage Dashboard provides task-level metrics (complementary view) |
| **EEG Feature S10** | AAR Export uses these metrics for capability performance section |
