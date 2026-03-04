# S03: Link Feedback Reports as Duplicates or Related

## User Story

**As an** Administrator,
**I want** to link feedback reports together as duplicates or as related issues,
**So that** I can consolidate the feedback queue, avoid working on the same problem twice, and surface connections between related submissions.

## Context

As the platform grows, users submit feedback that overlaps — two separate bug reports about the same inject-firing issue, or a feature request closely related to an open bug. Without a way to link these, administrators spend time triaging the same problem multiple times and lose visibility into recurring themes.

Two link types serve different purposes:

- **Duplicate** — one report is a re-submission of another. The newer report (the duplicate) points to the canonical original. Both reports remain in the system; no status is changed automatically.
- **Related** — two reports address connected but distinct issues. The relationship has no hierarchy; both reports surface the link equally.

Linking is a triage action performed by Administrators only. Reporters do not see link data.

## Acceptance Criteria

### Linking — Duplicate

- [ ] Given I am viewing a feedback report detail, when I click "Mark as Duplicate", then a report picker appears allowing me to search by reference number or title
- [ ] Given the report picker is open, when I type a reference number (e.g. "CAD-2026"), then matching reports are shown with their reference number, title, type, and status
- [ ] Given I select a target report from the picker, when I confirm, then the current report is linked as a duplicate of the selected report
- [ ] Given a duplicate link is saved, when I view the source report, then I see a "Duplicate of [CAD-YYYYMMDD-XXXX] — [Title]" notice
- [ ] Given a duplicate link is saved, when I view the target (canonical) report, then I see a "Duplicates" section listing all reports marked as duplicates of it
- [ ] Given I try to mark a report as a duplicate of itself, when I attempt to save, then I see a validation error "A report cannot be linked to itself"
- [ ] Given a report is already marked as a duplicate of another, when I try to add a second duplicate-of link, then I see a validation error "This report is already marked as a duplicate"

### Linking — Related

- [ ] Given I am viewing a feedback report detail, when I click "Add Related Report", then a report picker appears
- [ ] Given I select a report from the picker and confirm, when the link is saved, then both reports show each other in their "Related Reports" section
- [ ] Given a related link exists, when I view either report, then I see the other report listed with its reference number, title, type, and status badge
- [ ] Given I try to add a related link to a report already linked as related, then I see a validation error "These reports are already linked"
- [ ] Given I try to add a related link to a report already linked as a duplicate, then I see a validation error "These reports already have a duplicate link"

### Viewing Links

- [ ] Given I am viewing a report with no links, when I view the detail, then no link sections are shown
- [ ] Given I am viewing a report that is a duplicate of another, then I see the "Duplicate of" notice above the report body
- [ ] Given I am viewing a report that has duplicates filed against it, then I see a collapsible "Duplicates (N)" section
- [ ] Given I am viewing a report with related links, then I see a collapsible "Related Reports (N)" section
- [ ] Given I click a linked report's reference number, then I navigate to that report's detail view

### Unlinking

- [ ] Given I am viewing a report with a "Duplicate of" link, when I click the remove action, then a confirmation prompt appears
- [ ] Given I confirm removal of a duplicate link, then the link is removed from both reports
- [ ] Given I am viewing a related report link, when I remove it from either report, then the link is removed from both reports
- [ ] Given I cancel the removal confirmation, then the link remains unchanged

### Access Control

- [ ] Given I am logged in as Administrator, then I see the "Mark as Duplicate" and "Add Related Report" actions
- [ ] Given I am logged in as any non-Administrator role, when I access admin feedback endpoints, then I receive a 403 Forbidden response

## Out of Scope

- Automatically changing report status when a duplicate link is created
- Transitively merging content or comments across linked reports
- Bulk linking multiple reports in a single action
- Reporter-visible link data
- GitHub issue cross-linking between linked reports
- Link history or audit trail beyond standard `UpdatedAt` timestamps

## Dependencies

- feedback/S01: Submit Feedback (FeedbackReport entity must exist)
- feedback/S02: Admin Feedback Dashboard (detail view must exist for link UI)

## Open Questions

- [ ] Should creating a duplicate link offer an optional one-click "also close the duplicate" shortcut?
- [ ] Should the report picker exclude already-linked reports from results, or show them as disabled?
- [ ] Is there a maximum number of related links per report, or is it unbounded?
- [ ] Should duplicate chains be enforced (e.g. prevent marking a report as a duplicate of a report that is itself already a duplicate)?

## Technical Notes

### Entity Design

A `FeedbackReportLink` join entity with:

- `SourceReportId` (Guid FK)
- `TargetReportId` (Guid FK)
- `LinkType` enum: `Duplicate` | `Related`

For `Duplicate`: a report may have at most one outgoing duplicate link but a canonical report may have many incoming duplicates.

For `Related`: store a single row (A->B); query both directions to present the bidirectional view.

`FeedbackReportLink` inherits `BaseEntity`, is NOT org-scoped.

### API Endpoints

```
POST   /api/admin/feedback/{sourceId}/links     - Create link
GET    /api/admin/feedback/{id}/links            - Get links for a report
DELETE /api/admin/feedback/links/{linkId}         - Remove link
GET    /api/admin/feedback/search?q={term}       - Report picker search
```
