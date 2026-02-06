# Story: EM-10-S03 - Weekly Organization Report

**As an** OrgAdmin,  
**I want** to receive a weekly summary of organization activity,  
**So that** I can track usage and identify trends.

## Context

Organization administrators benefit from high-level visibility into how their team uses Cadence. Weekly reports provide metrics without overwhelming detail.

## Acceptance Criteria

- [ ] **Given** user is OrgAdmin, **when** weekly job runs (Monday 8 AM), **then** they receive org report
- [ ] **Given** report email, **when** received, **then** shows active exercises count
- [ ] **Given** report email, **when** received, **then** shows total injects created/delivered this week
- [ ] **Given** report email, **when** received, **then** shows active users count
- [ ] **Given** report email, **when** received, **then** shows upcoming exercises
- [ ] **Given** no activity in past week, **when** job runs, **then** still send report with zeros (confirms system working)

## Out of Scope

- Exportable reports
- Historical comparison
- Usage billing details

## Dependencies

- EM-01-S01: ACS Email Configuration
- Scheduled job infrastructure
- Organization metrics

## UI/UX Notes

### Email Preview

```
Subject: Weekly Report: Acme Emergency Management - Feb 3-9

---

[Organization Logo]

Weekly Organization Report
Acme Emergency Management
February 3-9, 2026

THIS WEEK'S ACTIVITY
━━━━━━━━━━━━━━━━━━━
Exercises Active: 2
Injects Created: 23
Injects Delivered: 47
Observations Captured: 31
Active Users: 18

UPCOMING EXERCISES
━━━━━━━━━━━━━━━━━━
• Operation Thunderstorm - March 15 (9 days)
• Quarterly Drill #2 - March 28 (22 days)

TEAM UPDATES
━━━━━━━━━━━━
• 2 new members joined
• 1 pending invitation

        [View Organization Dashboard]

---

Cadence - Exercise Management Platform
```

## Effort Estimate

**5 story points** - Organization-wide metrics, weekly scheduling

---

*Feature: EM-10 Digest & Summary Emails*  
*Priority: P2*
