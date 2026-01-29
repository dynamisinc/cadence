# Story OM-01: Organization List

**Priority:** P0 (MVP)  
**Feature:** Organization Management  
**Created:** 2025-01-29

---

## User Story

**As a** System Administrator,  
**I want** to view a list of all organizations in the system,  
**So that** I can monitor the platform, find organizations that need attention, and navigate to manage them.

---

## Context

The SysAdmin needs visibility into all organizations on the platform. This is the primary navigation point for all organization management tasks. The list should support finding organizations quickly (search, filter, sort) and provide at-a-glance status information.

This view is **only accessible to SysAdmins** - regular users never see other organizations.

---

## Acceptance Criteria

### Display Requirements

- [ ] **Given** I am logged in as SysAdmin, **when** I navigate to Organization Management, **then** I see a list of all organizations
- [ ] **Given** organizations exist, **when** viewing the list, **then** I see for each: Name, Slug, Status, User Count, Exercise Count, Created Date
- [ ] **Given** I am viewing the list, **when** an organization has status "Archived", **then** it displays with a muted/grayed visual treatment
- [ ] **Given** I am viewing the list, **when** an organization has status "Inactive", **then** it displays with a warning indicator

### Search and Filter

- [ ] **Given** I am viewing the organization list, **when** I type in the search box, **then** the list filters to organizations matching name or slug (case-insensitive)
- [ ] **Given** I am viewing the organization list, **when** I select a status filter, **then** the list shows only organizations with that status
- [ ] **Given** filters are applied, **when** I clear filters, **then** all organizations are shown again

### Sorting

- [ ] **Given** I am viewing the organization list, **when** I click a column header, **then** the list sorts by that column
- [ ] **Given** I have sorted by a column, **when** I click the same header again, **then** the sort direction reverses
- [ ] **Given** I am viewing the list, **when** the page loads, **then** organizations are sorted by Name ascending by default

### Navigation

- [ ] **Given** I am viewing the organization list, **when** I click an organization row, **then** I navigate to that organization's detail/edit page
- [ ] **Given** I am viewing the organization list, **when** I click "Create Organization", **then** I navigate to the create organization form
- [ ] **Given** I am not a SysAdmin, **when** I try to access /admin/organizations, **then** I am redirected to my dashboard with an access denied message

### Empty State

- [ ] **Given** no organizations exist (fresh install), **when** I view the list, **then** I see an empty state with "Create your first organization" call to action

### Loading State

- [ ] **Given** I navigate to the organization list, **when** data is loading, **then** I see a skeleton loader (not a spinner)

---

## Out of Scope

- Bulk operations (delete multiple, export list)
- Pagination (defer until >100 organizations - use virtual scrolling if needed)
- Custom column selection
- Saved filter presets

---

## Dependencies

- Authentication system with SysAdmin role detection
- Organization entity and API endpoint
- Navigation/routing structure

---

## Domain Terms

| Term | Definition |
|------|------------|
| Organization | A tenant boundary containing users, exercises, and configurations |
| SysAdmin | System-wide administrator with access to all organizations |
| Slug | URL-friendly unique identifier (e.g., "cisa-region-4") |
| Status | Organization state: Active, Archived, or Inactive |

---

## UI/UX Notes

### Layout
```
┌─────────────────────────────────────────────────────────────────┐
│ Organization Management                      [+ Create Organization] │
├─────────────────────────────────────────────────────────────────┤
│ 🔍 Search organizations...          Status: [All ▼]             │
├─────────────────────────────────────────────────────────────────┤
│ Name ▲        │ Slug         │ Status  │ Users │ Exercises │ Created   │
├───────────────┼──────────────┼─────────┼───────┼───────────┼───────────┤
│ CISA Region 4 │ cisa-r4      │ 🟢 Active │  12  │     8     │ Jan 15    │
│ State EMA     │ state-ema    │ 🟢 Active │  45  │    23     │ Jan 10    │
│ Acme Corp     │ acme         │ 🟡 Archived│   3  │     2     │ Dec 20    │
│ Test Org      │ test         │ 🔴 Inactive│   0  │     0     │ Dec 01    │
└───────────────┴──────────────┴─────────┴───────┴───────────┴───────────┘
```

### Status Indicators
- **Active:** Green dot or chip
- **Archived:** Yellow/amber chip, row slightly muted
- **Inactive:** Red chip, row grayed out

### Responsive Behavior
- Desktop (≥1024px): Full table view
- Tablet (768-1023px): Hide Exercise Count column
- Mobile (<768px): Card view with key info only

---

## Technical Notes

### API Endpoint
```
GET /api/admin/organizations
Authorization: Bearer {token} (SysAdmin only)

Query Parameters:
  - search: string (optional)
  - status: Active|Archived|Inactive (optional)
  - sortBy: name|slug|status|userCount|createdAt (default: name)
  - sortDir: asc|desc (default: asc)

Response:
{
  "items": [
    {
      "id": "guid",
      "name": "CISA Region 4",
      "slug": "cisa-r4",
      "status": "Active",
      "userCount": 12,
      "exerciseCount": 8,
      "createdAt": "2025-01-15T10:00:00Z"
    }
  ],
  "totalCount": 4
}
```

### Authorization
```csharp
[Authorize(Policy = "SysAdminOnly")]
public class AdminOrganizationsController : ControllerBase
```

---

## Test Scenarios

| Scenario | Test Type | Priority |
|----------|-----------|----------|
| SysAdmin sees all organizations | Integration | P0 |
| Non-SysAdmin gets 403 | Integration | P0 |
| Search filters by name | Unit (frontend) | P0 |
| Search filters by slug | Unit (frontend) | P0 |
| Status filter works | Unit (frontend) | P0 |
| Sort by each column | Unit (frontend) | P1 |
| Empty state displays | Component | P1 |
| Loading skeleton displays | Component | P1 |
| Row click navigates | Component | P0 |

---

## Implementation Checklist

### Backend
- [ ] Create `GET /api/admin/organizations` endpoint
- [ ] Add SysAdmin authorization policy
- [ ] Include user count (subquery or computed)
- [ ] Include exercise count (subquery or computed)
- [ ] Add search, filter, sort query parameters
- [ ] Unit tests for repository/service
- [ ] Integration tests for authorization

### Frontend
- [ ] Create `OrganizationListPage` component
- [ ] Create `OrganizationTable` component
- [ ] Add search input with debounce
- [ ] Add status filter dropdown
- [ ] Add column sorting
- [ ] Add empty state
- [ ] Add loading skeleton
- [ ] Add route `/admin/organizations`
- [ ] Add navigation guard for SysAdmin
- [ ] Component tests

---

## Changelog

| Date | Change |
|------|--------|
| 2025-01-29 | Initial story creation |
