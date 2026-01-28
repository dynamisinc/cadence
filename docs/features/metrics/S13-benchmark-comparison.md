# Story: Benchmark Comparison (Organization vs Industry)

**Feature**: Metrics  
**Story ID**: S13  
**Priority**: P2 (Future)  
**Phase**: Future Enhancement

---

## User Story

**As an** Emergency Manager or Administrator,  
**I want** to see how our organization's performance compares to industry benchmarks,  
**So that** I can understand our relative preparedness and set realistic improvement targets.

---

## Context

Organizations often ask "How do we compare to others?" This is challenging in emergency management due to:

- Varied exercise types and complexity
- Different organizational sizes and missions
- Limited data sharing between organizations

However, anonymized aggregate data could provide valuable benchmarks if enough Cadence users participate.

**Note**: This feature requires careful privacy consideration and opt-in participation.

---

## Acceptance Criteria

- [ ] **Given** I am an Admin, **when** I view organization metrics, **then** I see option to enable benchmark comparison
- [ ] **Given** benchmarks are enabled, **when** viewing metrics, **then** I see industry average comparison
- [ ] **Given** benchmark view, **when** displayed, **then** I see our on-time rate vs. industry average
- [ ] **Given** benchmark view, **when** displayed, **then** I see our P/S/M/U distribution vs. average
- [ ] **Given** benchmark view, **when** displayed, **then** I see percentile ranking (e.g., "Top 25%")
- [ ] **Given** I want to opt out, **when** I disable benchmarks, **then** our data is excluded from aggregates
- [ ] **Given** insufficient benchmark data, **when** viewing, **then** I see appropriate messaging

---

## Out of Scope

- Organization-to-organization direct comparison (privacy)
- Benchmark by specific organization type
- Real-time benchmark updates
- Benchmark targets/goals

---

## Dependencies

- Organization metrics (S09-S10)
- Aggregated anonymous data collection
- Privacy/consent framework

---

## Open Questions

- [ ] How do we ensure data privacy while enabling benchmarks?
- [ ] Minimum organizations needed for valid benchmark?
- [ ] How to normalize across different exercise types/sizes?
- [ ] Is this feature tied to subscription tier?

---

## UI/UX Notes

### Benchmark Enable/Disable

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Organization Settings > Analytics                                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Industry Benchmarks                                                    │
│  ───────────────────                                                    │
│                                                                         │
│  Participate in anonymous benchmarking                                  │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                                                      [  OFF ]   │   │
│  │  When enabled, your anonymized exercise metrics contribute      │   │
│  │  to industry benchmarks, and you can compare your performance   │   │
│  │  against aggregate data from other organizations.               │   │
│  │                                                                 │   │
│  │  No identifying information is shared. You can opt out          │   │
│  │  at any time.                                                   │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Benchmark Comparison View

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Your Organization vs. Industry Benchmark                               │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Based on 150+ organizations │ Updated monthly                         │
│                                                                         │
│  Metric                  │ Your Org │ Industry Avg │ Percentile        │
│  ────────────────────────┼──────────┼──────────────┼──────────         │
│  On-Time Inject Rate     │    85%   │     78%      │  Top 30%          │
│  P/S Rating Percentage   │    75%   │     68%      │  Top 35%          │
│  Observation Coverage    │    82%   │     71%      │  Top 25%          │
│  Exercises/Year          │    18    │     12       │  Top 20%          │
│                                                                         │
│  ✓ You're performing above average in all measured areas               │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

- Requires backend aggregation service
- Data anonymization before aggregation
- Consider: differential privacy techniques
- Segment benchmarks by org size/type if sufficient data
- Update benchmarks periodically (monthly/quarterly)

---

## Estimation

**T-Shirt Size**: XL  
**Story Points**: 13
