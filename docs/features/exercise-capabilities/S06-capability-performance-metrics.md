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

---

## Acceptance Criteria

- [ ] **Given** I am viewing exercise metrics, **when** the exercise has observations with capability tags, **then** I see "Capability Performance" section
- [ ] **Given** the Capability Performance section, **when** displayed, **then** I see each capability with observation count and average rating
- [ ] **Given** the capability list, **when** displayed, **then** capabilities are sorted by rating (worst first) to highlight improvement areas
- [ ] **Given** a capability row, **when** hovered, **then** I see rating distribution breakdown (P/S/M/U counts)
- [ ] **Given** the exercise has target capabilities, **when** viewing, **then** I see coverage metric (X of Y target capabilities evaluated)
- [ ] **Given** target capabilities, **when** some have NO observations, **then** they are listed in a "Not Evaluated" section
- [ ] **Given** a capability row, **when** I click it, **then** I see all observations tagged to that capability
- [ ] **Given** exercise objectives mapped to capabilities, **when** viewing, **then** I see objective alignment (future enhancement)
- [ ] **Given** no capability tags exist on any observations, **when** viewing metrics, **then** I see message encouraging capability tagging
- [ ] **Given** API, **when** `GET /api/exercises/{id}/metrics/capabilities` called, **then** returns capability performance data
- [ ] **Given** view toggle, **when** I select different sort orders, **then** list reorders (Rating/Alphabetical/Category)

---

## Out of Scope

- Capability trend analysis across multiple exercises (Organization-level metrics)
- FEMA capability definitions/descriptions display
- Capability-specific recommendations
- Automated capability inference from observation text
- Export to AAR/IP format (future enhancement)

---

## Dependencies

- S01: Capability Entity and API
- S04: Exercise Target Capabilities  
- S05: Observation Capability Tagging
- Metrics feature (existing dashboard)

---

## Open Questions

- [x] Should we show all capabilities or just those with observations? **Show both - evaluated and gaps**
- [x] How to handle capabilities not in target list but tagged? **Include in "Other Evaluated" section**
- [x] Should capability ratings weight by observation importance? **No, simple average for MVP**
- [ ] Do we need mission area grouping view option? **Defer to enhancement**

---

## Domain Terms

| Term | Definition |
|------|------------|
| Capability Performance | Aggregate P/S/M/U rating across all observations for a capability |
| Coverage | Percentage of target capabilities that have at least one observation |
| Capability Gap | A target capability that was not evaluated (no observations) |
| Average Rating | Mean of numeric rating values: P=1, S=2, M=3, U=4 |

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
│  │  Operational Communications                          ● Response   │ │
│  │  ████████████████████░░░░░░░░░░░░  Avg: Marginal (2.8)           │ │
│  │  Observations: 5                                                  │ │
│  │  [P: 0] [S: 1] [M: 3] [U: 1]                            [View →] │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  Public Information and Warning                      ● Prevention │ │
│  │  ████████████████░░░░░░░░░░░░░░░░  Avg: Marginal (2.5)           │ │
│  │  Observations: 4                                                  │ │
│  │  [P: 0] [S: 2] [M: 1] [U: 1]                            [View →] │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  ✓ SATISFACTORY                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  Mass Care Services                                  ● Response   │ │
│  │  ████████░░░░░░░░░░░░░░░░░░░░░░░░  Avg: Satisfactory (1.8)       │ │
│  │  Observations: 8                                        [View →] │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  Planning                                            ● All Areas  │ │
│  │  ████████░░░░░░░░░░░░░░░░░░░░░░░░  Avg: Performed (1.5)          │ │
│  │  Observations: 6                                        [View →] │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  ⊘ NOT EVALUATED (Target Capabilities)                                 │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  • Intelligence and Information Sharing                                │
│  • Logistics and Supply Chain Management                               │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  ○ OTHER EVALUATED (Non-Target)                                        │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  Critical Transportation                             ● Response   │ │
│  │  ████████████████░░░░░░░░░░░░░░░░  Avg: Satisfactory (2.0)       │ │
│  │  Observations: 2                                        [View →] │ │
│  └───────────────────────────────────────────────────────────────────┘ │
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

### Empty State (No Capability Tags)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  CAPABILITY PERFORMANCE                                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│                    📊                                                   │
│                                                                         │
│              No capability data available                               │
│                                                                         │
│    Observations haven't been tagged with capabilities yet.              │
│    Tag observations with capabilities to see performance metrics.       │
│                                                                         │
│    Target Capabilities: 5 defined                                       │
│    Observations: 23 (0 with capability tags)                           │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Capability Detail Drawer (on click "View →")

```
┌─────────────────────────────────────────────────────────────┐
│  OPERATIONAL COMMUNICATIONS                             [X] │
│  Response • Target Capability                               │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  PERFORMANCE SUMMARY                                        │
│  ─────────────────────────────────────────────────────────  │
│  Average Rating: Marginal (2.8)                             │
│  Observations: 5                                            │
│                                                             │
│  Distribution:                                              │
│  P ████                           0 (0%)                   │
│  S ████████                       1 (20%)                  │
│  M ████████████████████████       3 (60%)                  │
│  U ████████                       1 (20%)                  │
│                                                             │
│  ─────────────────────────────────────────────────────────  │
│                                                             │
│  OBSERVATIONS                                               │
│  ─────────────────────────────────────────────────────────  │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ OBS-007  14:32                           [Marginal] │   │
│  │ Communication between EOC and field teams was       │   │
│  │ delayed by 15 minutes due to radio frequency...     │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ OBS-012  15:48                      [Unsatisfactory]│   │
│  │ Field team Alpha could not reach command post for   │   │
│  │ over 20 minutes during critical phase...            │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ OBS-015  16:22                           [Marginal] │   │
│  │ Backup radio channel was effective once activated   │   │
│  │ but took 10 minutes to coordinate switchover...     │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ...                                                        │
│                                                             │
└─────────────────────────────────────────────────────────────┘
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
    int ObservationCount,
    decimal AverageRating,        // 1.0-4.0
    string AverageRatingLabel,    // "Performed", "Satisfactory", etc.
    RatingDistributionDto Distribution
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
    string? Category
);
```

### Service Layer

```csharp
public class CapabilityMetricsService
{
    public async Task<CapabilityMetricsDto> GetCapabilityMetricsAsync(Guid exerciseId)
    {
        // 1. Get target capabilities for exercise
        var targetCapabilities = await _context.ExerciseCapabilities
            .Where(ec => ec.ExerciseId == exerciseId)
            .Select(ec => ec.Capability)
            .ToListAsync();
        
        // 2. Get all observations with capability tags
        var observations = await _context.Observations
            .Where(o => o.ExerciseId == exerciseId)
            .Include(o => o.ObservationCapabilities)
                .ThenInclude(oc => oc.Capability)
            .Where(o => o.Rating != null)  // Only rated observations
            .ToListAsync();
        
        // 3. Group by capability and calculate metrics
        var capabilityGroups = observations
            .SelectMany(o => o.ObservationCapabilities
                .Select(oc => new { Observation = o, Capability = oc.Capability }))
            .GroupBy(x => x.Capability.Id)
            .Select(g => new CapabilityPerformanceDto(
                CapabilityId: g.Key,
                Name: g.First().Capability.Name,
                Category: g.First().Capability.Category,
                IsTargetCapability: targetCapabilities.Any(tc => tc.Id == g.Key),
                ObservationCount: g.Count(),
                AverageRating: CalculateAverageRating(g.Select(x => x.Observation.Rating)),
                AverageRatingLabel: GetRatingLabel(averageRating),
                Distribution: CalculateDistribution(g.Select(x => x.Observation.Rating))
            ))
            .OrderByDescending(c => c.AverageRating)  // Worst first
            .ToList();
        
        // 4. Find gaps (target capabilities with no observations)
        var evaluatedCapabilityIds = capabilityGroups.Select(c => c.CapabilityId).ToHashSet();
        var gaps = targetCapabilities
            .Where(tc => !evaluatedCapabilityIds.Contains(tc.Id))
            .Select(tc => new CapabilityGapDto(tc.Id, tc.Name, tc.Category))
            .ToList();
        
        // 5. Calculate coverage
        var evaluatedTargetCount = targetCapabilities
            .Count(tc => evaluatedCapabilityIds.Contains(tc.Id));
        
        return new CapabilityMetricsDto(
            TargetCapabilityCount: targetCapabilities.Count,
            EvaluatedTargetCount: evaluatedTargetCount,
            CoveragePercentage: targetCapabilities.Count > 0 
                ? (decimal)evaluatedTargetCount / targetCapabilities.Count * 100 
                : 0,
            EvaluatedCapabilities: capabilityGroups,
            NotEvaluatedTargets: gaps
        );
    }
    
    private decimal CalculateAverageRating(IEnumerable<string?> ratings)
    {
        var values = ratings
            .Where(r => r != null)
            .Select(r => r switch
            {
                "P" or "Performed" => 1m,
                "S" or "Satisfactory" => 2m,
                "M" or "Marginal" => 3m,
                "U" or "Unsatisfactory" => 4m,
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
// - Gap list (not evaluated targets)
// - Empty state if no capability tags
```

---

## Estimation

**T-Shirt Size:** M  
**Story Points:** 5

---

## Testing Requirements

### Unit Tests
- [ ] Average rating calculation (P/S/M/U → numeric)
- [ ] Rating label thresholds
- [ ] Distribution counting
- [ ] Gap detection for target capabilities

### Integration Tests
- [ ] API returns correct metrics structure
- [ ] Coverage percentage calculation
- [ ] Sorting by worst rating first
- [ ] Empty state when no capability tags

### E2E Tests
- [ ] View capability metrics for exercise with tagged observations
- [ ] Click capability to see detail drawer
- [ ] Toggle sort order
- [ ] View exercise with no capability tags (empty state)
