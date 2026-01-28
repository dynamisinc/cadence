# Story: Capability Library Admin UI

**Feature:** Exercise Capabilities  
**Story ID:** S02  
**Priority:** P0 (MVP)  
**Phase:** Standard Implementation

---

## User Story

**As an** Administrator,  
**I want** to view and manage my organization's capability library,  
**So that** exercises can be aligned to our specific evaluation capabilities.

---

## Context

The Capability Library is accessed through Organization Settings and allows administrators to view, create, edit, and deactivate capabilities. This is the primary interface for managing what capabilities are available for exercise targeting and observation tagging.

The UI should support organizations with no capabilities (empty state with guidance), organizations using predefined libraries (FEMA, NATO, etc.), and organizations with custom capabilities. Capabilities are grouped by category for easier navigation, especially when using FEMA's 32 capabilities across 5 mission areas.

---

## Acceptance Criteria

- [ ] **Given** I am logged in as Administrator, **when** I navigate to Settings, **then** I see "Capability Library" menu option
- [ ] **Given** I click "Capability Library", **when** the page loads, **then** I see a list of capabilities grouped by Category
- [ ] **Given** capabilities exist, **when** the page loads, **then** I see Name, Category, Status (Active/Inactive), and Source for each
- [ ] **Given** no capabilities exist, **when** the page loads, **then** I see an empty state with "Import Library" and "Add Capability" buttons
- [ ] **Given** the capability list, **when** I click filter toggle, **then** I can filter by All/Active/Inactive
- [ ] **Given** the capability list, **when** I type in search box, **then** capabilities are filtered by Name match
- [ ] **Given** the capability list, **when** I click "Add Capability", **then** a dialog/drawer opens with create form
- [ ] **Given** the create form, **when** I enter Name and optionally Description/Category and click Save, **then** capability is created and list refreshes
- [ ] **Given** the create form, **when** I submit without Name, **then** validation error is shown
- [ ] **Given** a capability row, **when** I click Edit, **then** a dialog/drawer opens with edit form pre-populated
- [ ] **Given** the edit form, **when** I modify fields and click Save, **then** capability is updated and list refreshes
- [ ] **Given** an active capability row, **when** I click Deactivate, **then** confirmation dialog appears
- [ ] **Given** confirmation dialog, **when** I confirm deactivation, **then** capability status changes to Inactive
- [ ] **Given** an inactive capability, **when** I click Reactivate, **then** capability status changes to Active
- [ ] **Given** the capability list, **when** I click "Import", **then** dropdown shows predefined library options (see S03)
- [ ] **Given** any capability action (create/edit/deactivate), **when** completed, **then** a toast notification confirms success
- [ ] **Given** I am not an Administrator, **when** I try to access Capability Library, **then** I am redirected or shown access denied

---

## Out of Scope

- Predefined library import logic (S03)
- Bulk import from CSV
- Capability reordering via drag-and-drop
- Delete (permanent removal) - only deactivation supported

---

## Dependencies

- S01: Capability Entity and API
- Settings feature navigation shell
- Toast notification component

---

## Open Questions

- [x] Should edit be inline or in a dialog? **Dialog for consistency with other Settings pages**
- [x] Should categories be collapsible? **Yes, for large libraries like FEMA 32**
- [ ] Should we show usage count (how many exercises/observations)? **Defer to enhancement**

---

## Domain Terms

| Term | Definition |
|------|------------|
| Capability Library | The collection of capabilities defined for an organization |
| Category | Grouping for capabilities (e.g., FEMA Mission Areas) |
| Source | Indicates if capability came from predefined library or is custom |

---

## UI/UX Notes

### Capability Library Page

```
┌─────────────────────────────────────────────────────────────────────────┐
│  ← Settings                                                             │
│                                                                         │
│  CAPABILITY LIBRARY                                                     │
│  Define the capabilities your organization evaluates during exercises   │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │ Filter: [● All] [○ Active] [○ Inactive]   🔍 [Search capabilities] ││
│  │                                                                     ││
│  │                                           [Import ▼] [+ Add]       ││
│  └─────────────────────────────────────────────────────────────────────┘│
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │  ▼ PREVENTION                                            7 items   ││
│  ├─────────────────────────────────────────────────────────────────────┤│
│  │  ┌───────────────────────────────────────────────────────────────┐ ││
│  │  │ Planning                                                      │ ││
│  │  │ Conduct systematic planning engaging whole community...       │ ││
│  │  │ 🏷️ FEMA    ● Active                              [Edit] [···]│ ││
│  │  └───────────────────────────────────────────────────────────────┘ ││
│  │  ┌───────────────────────────────────────────────────────────────┐ ││
│  │  │ Public Information and Warning                                │ ││
│  │  │ Deliver coordinated, prompt, reliable information...          │ ││
│  │  │ 🏷️ FEMA    ● Active                              [Edit] [···]│ ││
│  │  └───────────────────────────────────────────────────────────────┘ ││
│  │  ...                                                               ││
│  └─────────────────────────────────────────────────────────────────────┘│
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │  ▶ RESPONSE                                             15 items   ││
│  └─────────────────────────────────────────────────────────────────────┘│
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │  ▼ CUSTOM                                                3 items   ││
│  ├─────────────────────────────────────────────────────────────────────┤│
│  │  ┌───────────────────────────────────────────────────────────────┐ ││
│  │  │ Volunteer Coordination                                        │ ││
│  │  │ Manage volunteer check-in, assignments, and safety            │ ││
│  │  │ 🏷️ Custom  ● Active                              [Edit] [···]│ ││
│  │  └───────────────────────────────────────────────────────────────┘ ││
│  └─────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────┘
```

### Empty State

```
┌─────────────────────────────────────────────────────────────────────────┐
│  ← Settings                                                             │
│                                                                         │
│  CAPABILITY LIBRARY                                                     │
│  Define the capabilities your organization evaluates during exercises   │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │                                                                     ││
│  │                    📋                                               ││
│  │                                                                     ││
│  │              No capabilities defined                                ││
│  │                                                                     ││
│  │    Capabilities help you track which organizational functions      ││
│  │    are being evaluated during exercises.                           ││
│  │                                                                     ││
│  │    [Import Predefined Library]    [Create Custom Capability]       ││
│  │                                                                     ││
│  │    💡 Most US organizations use FEMA's 32 Core Capabilities        ││
│  │                                                                     ││
│  └─────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────┘
```

### Add/Edit Capability Dialog

```
┌─────────────────────────────────────────────────────────────┐
│  ADD CAPABILITY                                         [X] │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Name *                                                     │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Volunteer Coordination                              │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Description                                                │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Manage volunteer check-in, task assignments,        │   │
│  │ safety briefings, and coordination with response    │   │
│  │ teams during emergency operations.                  │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Category                                                   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Custom                                          [▼] │   │
│  └─────────────────────────────────────────────────────┘   │
│  💡 Use an existing category or type a new one             │
│                                                             │
│                              [Cancel]  [Save Capability]    │
└─────────────────────────────────────────────────────────────┘
```

### Deactivate Confirmation

```
┌─────────────────────────────────────────────────────────────┐
│  DEACTIVATE CAPABILITY                                  [X] │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ⚠️ Are you sure you want to deactivate                     │
│     "Mass Care Services"?                                   │
│                                                             │
│  This capability will no longer appear in exercise          │
│  setup or observation tagging. Historical data will         │
│  be preserved.                                              │
│                                                             │
│  You can reactivate this capability at any time.            │
│                                                             │
│                           [Cancel]  [Deactivate]            │
└─────────────────────────────────────────────────────────────┘
```

### More Actions Menu ([···])

```
┌──────────────────────┐
│ ✏️  Edit             │
├──────────────────────┤
│ ⏸️  Deactivate       │  (or "▶️ Reactivate" if inactive)
└──────────────────────┘
```

---

## Technical Notes

### Component Structure

```
src/frontend/
  features/
    settings/
      components/
        CapabilityLibrary/
          CapabilityLibraryPage.tsx      # Main page component
          CapabilityList.tsx             # List with category grouping
          CapabilityCard.tsx             # Individual capability display
          CapabilityDialog.tsx           # Add/Edit dialog
          CapabilityEmptyState.tsx       # Empty state with guidance
          ImportLibraryMenu.tsx          # Import dropdown (stub for S03)
          useCapabilities.ts             # Data fetching hook
```

### State Management

```typescript
interface CapabilityLibraryState {
  capabilities: Capability[];
  isLoading: boolean;
  error: string | null;
  filter: 'all' | 'active' | 'inactive';
  searchQuery: string;
  expandedCategories: Set<string>;
}
```

### API Integration

```typescript
// hooks/useCapabilities.ts
export function useCapabilities(organizationId: string) {
  // GET /api/organizations/{orgId}/capabilities?includeInactive=true
  // Returns all capabilities, UI handles filtering
}

export function useCreateCapability() {
  // POST /api/organizations/{orgId}/capabilities
}

export function useUpdateCapability() {
  // PUT /api/organizations/{orgId}/capabilities/{id}
}

export function useDeactivateCapability() {
  // DELETE /api/organizations/{orgId}/capabilities/{id}
}
```

---

## Estimation

**T-Shirt Size:** M  
**Story Points:** 5

---

## Testing Requirements

### Unit Tests
- [ ] CapabilityList renders grouped by category
- [ ] CapabilityCard displays all fields correctly
- [ ] Search filtering works correctly
- [ ] Active/Inactive filter works correctly

### Integration Tests
- [ ] Full CRUD flow through UI
- [ ] Empty state displays correctly
- [ ] Toast notifications appear on actions
- [ ] Authorization redirect for non-admins

### E2E Tests
- [ ] Navigate to Capability Library from Settings
- [ ] Add new capability
- [ ] Edit existing capability
- [ ] Deactivate and reactivate capability
