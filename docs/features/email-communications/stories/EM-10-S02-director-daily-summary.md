# Story: EM-10-S02 - Exercise Director Daily Summary

**As an** Exercise Director,  
**I want** to receive a daily summary of my exercise's status,  
**So that** I can track planning progress and identify blockers.

## Context

Exercise Directors need visibility into overall exercise readiness. This summary provides metrics and highlights issues requiring attention.

## Acceptance Criteria

- [ ] **Given** user is Exercise Director, **when** digest job runs, **then** they receive Director-specific summary
- [ ] **Given** summary email, **when** received, **then** shows MSEL completion percentage
- [ ] **Given** summary email, **when** received, **then** shows pending approvals count
- [ ] **Given** summary email, **when** received, **then** shows participant confirmation status
- [ ] **Given** summary email, **when** received, **then** highlights blockers (overdue approvals, unassigned injects)
- [ ] **Given** no active exercises, **when** job runs, **then** no summary sent

## Out of Scope

- Dashboard analytics
- Trend analysis

## Dependencies

- EM-01-S01: ACS Email Configuration
- Scheduled job infrastructure
- Exercise metrics calculation

## UI/UX Notes

### Email Preview

```
Subject: Director Summary: Operation Thunderstorm - Feb 6

---

[Organization Logo]

Exercise Director Summary
Operation Thunderstorm | February 6, 2026

EXERCISE COUNTDOWN: 9 DAYS
━━━━━━━━━━━━━━━━━━━━━━━━━━

MSEL STATUS
━━━━━━━━━━━
Total Injects: 47
✓ Approved: 38 (81%)
⏳ Pending Review: 6
📝 In Draft: 3

⚠️ ATTENTION NEEDED
• 2 injects pending approval >3 days
• 5 injects unassigned to controllers

PARTICIPANT STATUS
━━━━━━━━━━━━━━━━━━
Invited: 24 | Confirmed: 19 | Pending: 5

        [Open Exercise Dashboard]

---

Cadence - Exercise Management Platform
```

## Effort Estimate

**3 story points** - Metrics aggregation, Director-specific content

---

*Feature: EM-10 Digest & Summary Emails*  
*Priority: P2*
