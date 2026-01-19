# Story: Admin Archive Management Page

## S07-admin-archive-management.md

**As an** Administrator,
**I want** a dedicated page to manage archived exercises,
**So that** I can efficiently review, restore, or permanently delete multiple archived exercises.

### Context

When many exercises accumulate in the archive over time, a dedicated management interface is more efficient than filtering the main exercise list. This admin page provides:

- Overview of all archived exercises with key metadata
- Sorting and filtering capabilities
- Bulk operations for efficient management
- Storage/usage insights

This is a P1 feature that can be deferred until after core archive/delete functionality is working.

### Acceptance Criteria

**Page Access:**
- [ ] **Given** I am an Administrator, **when** I navigate to Settings/Admin area, **then** I see an "Archived Exercises" menu option
- [ ] **Given** I am not an Administrator, **when** I view the Settings area, **then** I do NOT see "Archived Exercises" option
- [ ] **Given** I click "Archived Exercises", **when** the page loads, **then** I see a table of all archived exercises

**Table Display:**
- [ ] **Given** I view the archive table, **when** I see each row, **then** it shows: Exercise Name, Previous Status, Archived Date, Archived By, Inject Count
- [ ] **Given** multi-org is enabled, **when** I view the table, **then** I also see Organization column
- [ ] **Given** there are no archived exercises, **when** I view the page, **then** I see an appropriate empty state message

**Sorting & Filtering:**
- [ ] **Given** I view the archive table, **when** I click a column header, **then** the table sorts by that column
- [ ] **Given** I click the same column header again, **when** the sort updates, **then** it toggles between ascending and descending
- [ ] **Given** multi-org is enabled, **when** I use the organization filter, **then** the table shows only that organization's archived exercises
- [ ] **Given** I type in the search box, **when** I search, **then** the table filters by exercise name

**Row Actions:**
- [ ] **Given** I view a row, **when** I click the actions menu, **then** I see: View Details, Restore, Permanently Delete
- [ ] **Given** I click "View Details", **when** the action executes, **then** I navigate to the exercise detail page (read-only)
- [ ] **Given** I click "Restore", **when** the confirmation dialog appears, **then** I can restore the exercise (per S04)
- [ ] **Given** I click "Permanently Delete", **when** the confirmation dialog appears, **then** I can delete the exercise (per S06)

**Bulk Operations:**
- [ ] **Given** I view the table, **when** I see each row, **then** there is a checkbox for selection
- [ ] **Given** I check the "Select All" checkbox, **when** the selection updates, **then** all visible rows are selected
- [ ] **Given** I have selected multiple exercises, **when** I view bulk actions, **then** I see "Restore Selected" and "Delete Selected"
- [ ] **Given** I click "Restore Selected", **when** the confirmation dialog appears, **then** it shows the count of exercises to restore
- [ ] **Given** I confirm bulk restore, **when** the operation completes, **then** all selected exercises are restored
- [ ] **Given** I click "Delete Selected", **when** the confirmation dialog appears, **then** it shows total counts of all data to be deleted
- [ ] **Given** I confirm bulk delete, **when** the operation completes, **then** all selected exercises and their data are deleted

**Summary Stats:**
- [ ] **Given** I view the page header, **when** I see the summary, **then** it shows total archived count
- [ ] **Given** I view the page header, **when** I see the summary, **then** it shows total inject count across all archived

### Out of Scope

- Export archived exercise list
- Automatic retention policies
- Storage size calculations (future enhancement)
- Scheduled deletion jobs

### UI/UX Notes

**Page Layout:**

```
Settings > Archived Exercises

Archived Exercises                                              
───────────────────────────────────────────────────────────────────
Summary: 12 archived exercises • 847 total injects

[Organization: All ▼]  [Archived By: All ▼]  🔍 Search...

☐ Select All (0 selected)          [Restore Selected] [Delete Selected]

┌────┬────────────────────────┬───────────┬─────────────┬─────────────┬────────┬─────────┐
│ ☐  │ Exercise Name      ▲   │ Prev Sts  │ Archived    │ Archived By │ Injects│ Actions │
├────┼────────────────────────┼───────────┼─────────────┼─────────────┼────────┼─────────┤
│ ☐  │ Q4 2025 TTX            │ Completed │ Jan 5, 2026 │ J. Smith    │ 23     │   [⋮]   │
│ ☐  │ Test Exercise          │ Draft     │ Jan 3, 2026 │ A. Jones    │ 5      │   [⋮]   │
│ ☐  │ Cancelled FE           │ Published │ Dec 28, 2025│ J. Smith    │ 156    │   [⋮]   │
│ ☐  │ Old Tabletop           │ Completed │ Nov 15, 2025│ M. Wilson   │ 42     │   [⋮]   │
└────┴────────────────────────┴───────────┴─────────────┴─────────────┴────────┴─────────┘

Showing 1-10 of 12                                         [< 1  2 >]
```

**Bulk Delete Confirmation:**

```
┌─────────────────────────────────────────────────────────────┐
│  ⚠️  Permanently Delete 3 Exercises                     [×] │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  This action CANNOT be undone.                               │
│                                                              │
│  You are about to delete:                                    │
│    • 3 exercises                                             │
│    • 226 injects total                                       │
│    • 45 observations total                                   │
│    • 28 participants total                                   │
│                                                              │
│  Exercises to delete:                                        │
│    • Q4 2025 TTX (23 injects)                                │
│    • Test Exercise (5 injects)                               │
│    • Cancelled FE (156 injects)                              │
│                                                              │
│  Type "DELETE" to confirm:                                   │
│  ┌─────────────────────────────────────────────────────────┐│
│  │                                                         ││
│  └─────────────────────────────────────────────────────────┘│
│                                                              │
│  ☐ I understand this action is permanent and irreversible   │
│                                                              │
├─────────────────────────────────────────────────────────────┤
│                   [Cancel]    [Delete 3 Exercises] 🔴        │
└─────────────────────────────────────────────────────────────┘
```

**Bulk Restore Confirmation:**

```
┌─────────────────────────────────────────────────────────────┐
│  Restore 3 Exercises                                    [×] │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  The following exercises will be restored:                   │
│                                                              │
│    • Q4 2025 TTX → Completed                                 │
│    • Test Exercise → Draft                                   │
│    • Cancelled FE → Published                                │
│                                                              │
│  They will appear in the normal exercise list.               │
│                                                              │
├─────────────────────────────────────────────────────────────┤
│                    [Cancel]    [Restore 3 Exercises]         │
└─────────────────────────────────────────────────────────────┘
```

### API Specification

**Archived Exercises List (Enhanced):**
```
GET /api/admin/archived-exercises
```

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| page | int | Page number (1-indexed) |
| pageSize | int | Items per page (default 20) |
| sortBy | string | Column to sort by |
| sortOrder | string | "asc" or "desc" |
| organizationId | guid? | Filter by organization |
| archivedById | guid? | Filter by who archived |
| search | string | Search exercise name |

**Response:**
```json
{
  "items": [
    {
      "id": "guid",
      "name": "Q4 2025 TTX",
      "previousStatus": "Completed",
      "archivedAt": "2026-01-05T10:30:00Z",
      "archivedBy": { "id": "guid", "name": "J. Smith" },
      "organization": { "id": "guid", "name": "County EOC" },
      "injectCount": 23,
      "observationCount": 8
    }
  ],
  "totalCount": 12,
  "page": 1,
  "pageSize": 20,
  "summary": {
    "totalExercises": 12,
    "totalInjects": 847,
    "totalObservations": 156
  }
}
```

**Bulk Restore:**
```
POST /api/admin/archived-exercises/bulk-restore
```

**Request:**
```json
{
  "exerciseIds": ["guid1", "guid2", "guid3"]
}
```

**Bulk Delete:**
```
POST /api/admin/archived-exercises/bulk-delete
```

**Request:**
```json
{
  "exerciseIds": ["guid1", "guid2", "guid3"]
}
```

### Domain Terms

| Term | Definition |
|------|------------|
| Bulk Operation | Action applied to multiple selected items at once |
| Admin Area | Settings/configuration section accessible only to Administrators |

### Dependencies

- S02 - Archive Exercise
- S03 - View Archived Exercises
- S04 - Restore Exercise
- S06 - Delete Archived Exercise

### Technical Notes

**Pagination:**
- Server-side pagination for performance with large archives
- Default page size: 20
- Consider virtual scrolling for very large lists

**Bulk Operations:**
- Use transactions for bulk delete (all or nothing)
- Bulk restore can be sequential (partial success acceptable)
- Return detailed results showing success/failure per item

**Performance:**
- Include counts in list query (single query with aggregates)
- Cache summary stats if performance is an issue

### Deliverables

1. Create `ArchivedExercisesPage` React component
2. Add route `/admin/archived-exercises`
3. Add navigation link in Settings/Admin menu
4. Create data table component with sorting, filtering, pagination
5. Implement bulk selection with checkboxes
6. Create `BulkRestoreDialog` component
7. Create `BulkDeleteDialog` component
8. Add `GET /api/admin/archived-exercises` endpoint
9. Add `POST /api/admin/archived-exercises/bulk-restore` endpoint
10. Add `POST /api/admin/archived-exercises/bulk-delete` endpoint
11. Integration tests for bulk operations
12. Test pagination and filtering
