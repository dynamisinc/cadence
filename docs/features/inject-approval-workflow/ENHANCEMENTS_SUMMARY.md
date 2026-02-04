# Enhancement Stories Summary - Inject Approval Workflow

**Date:** 2026-02-04
**Feature:** Inject Approval Workflow
**Stories Added:** S12, S13, S14
**Author:** Business Analyst Agent

## Overview

Three optional enhancement stories have been added to the inject approval workflow feature. These stories improve the user experience by providing faster, more convenient ways to interact with the approval workflow without requiring page navigation.

## New User Stories

### S12: Batch Approval Integration in MSEL View
**File:** `docs/features/inject-approval-workflow/S12-batch-approval-integration.md`
**Priority:** P1
**Points:** 3
**Dependencies:** S05 (Batch Approval Actions)

**Summary:** Integrates the existing `BatchApprovalToolbar` component into the InjectListPage (MSEL view) by adding checkbox selection to the inject table. Exercise Directors can select multiple injects and approve/reject them in bulk directly from the MSEL view.

**Key Features:**
- Checkbox column in inject table (when approval enabled)
- Select all/none/indeterminate header checkbox
- BatchApprovalToolbar displays when selection exists
- Selection state management with `useInjectSelection` hook
- Filters approvable injects (excludes self-submissions, non-Submitted statuses)
- Responsive design (mobile-friendly)

**Components:**
- New: `useInjectSelection` hook (`features/injects/hooks/`)
- Updated: `InjectTable` component (adds checkboxes)
- Updated: `InjectListPage` (integrates selection + toolbar)
- Existing: `BatchApprovalToolbar` (already implemented in S05)

---

### S13: Quick Submit Action in MSEL Table Row
**File:** `docs/features/inject-approval-workflow/S13-quick-submit-table-action.md`
**Priority:** P1
**Points:** 2
**Dependencies:** S03 (Submit Inject for Approval)

**Summary:** Adds a "Submit for Approval" button directly in the inject table row's Actions column. Controllers can submit Draft injects without navigating to the InjectDetailPage, streamlining MSEL preparation workflow.

**Key Features:**
- Submit button in table row Actions column (Draft injects only)
- Shows when approval workflow enabled
- Icon-only on mobile, icon+text on desktop (responsive)
- Validation error tooltip when inject incomplete
- Success/error toast notifications
- Row updates in place (no navigation)

**Components:**
- Updated: `SubmitForApprovalButton` component (enhanced for responsive variants)
- Updated: `InjectTable` component (adds Submit button to row actions)
- Updated: `InjectListPage` (passes callbacks)

**Time Savings:** ~5-10 seconds per inject × 50 injects = **4-8 minutes saved** during MSEL preparation

---

### S14: Approval Actions in InjectDetailDrawer
**File:** `docs/features/inject-approval-workflow/S14-drawer-approval-actions.md`
**Priority:** P2
**Points:** 3
**Dependencies:** S03 (Submit for Approval), S04 (Approve/Reject Inject)

**Summary:** Extends the InjectDetailDrawer (used in conduct view) to support approval workflow actions. Users can submit, approve, or reject injects from the drawer without leaving the conduct view.

**Key Features:**
- Submit button for Draft injects in drawer footer
- Approve/Reject buttons for Submitted injects (Directors)
- Self-approval prevention (disabled button with tooltip)
- Approval status display in drawer header (Submitted/Approved metadata)
- Approver notes section (when present)
- Rejection alert for Draft injects with rejection reasons
- Drawer remains open after actions (shows updated status)

**Components:**
- Updated: `InjectDetailDrawer` component (approval props + content sections)
- New: `DrawerFooterActions` component (handles footer button logic)
- Updated: `ConductPage` (passes approval callbacks)
- Existing: `ApproveDialog`, `RejectDialog`, `RejectionAlert` (reused from S03/S04)

**Time Savings:** No navigation to detail page and back (5 seconds × 20 injects = **100 seconds saved**)

---

## Total Feature Scope Update

| Metric | Before | After | Change |
|--------|--------|-------|--------|
| Total Stories | 12 | **15** | +3 |
| Total Points | 48 | **56** | +8 |
| P0 Stories | 4 | 4 | - |
| P1 Stories | 6 | **8** | +2 |
| P2 Stories | 2 | **3** | +1 |

## Implementation Order

Recommended sequence based on dependencies:

1. **S13: Quick Submit Table Action** (2 pts)
   - No complex dependencies
   - Enhances existing `SubmitForApprovalButton`
   - Immediate value for Controllers

2. **S12: Batch Approval Integration** (3 pts)
   - Leverages existing `BatchApprovalToolbar` (S05)
   - Adds selection state management
   - High value for Directors reviewing MSELs

3. **S14: Drawer Approval Actions** (3 pts)
   - Requires conduct page context
   - Builds on S03 and S04 components
   - Nice-to-have for conduct view workflow

## Key Design Decisions

### 1. Component Reuse
All three stories maximize reuse of existing components:
- `BatchApprovalToolbar` (S05) - used as-is in S12
- `SubmitForApprovalButton` (S03) - enhanced for responsive variants in S13
- `ApproveDialog`, `RejectDialog`, `RejectionAlert` (S03/S04) - reused in S14

### 2. Selection State Management
S12 introduces `useInjectSelection` hook to manage checkbox selection state, following React best practices for shared state logic.

### 3. Responsive Design
Both S13 and S12 include mobile-first considerations:
- Icon-only buttons on mobile (space-saving)
- Stacked toolbar buttons on narrow screens
- Checkbox column remains usable on mobile

### 4. UX Consistency
All stories maintain consistency with existing approval workflow:
- Self-approval prevention (S04 pattern)
- Validation error handling (S03 pattern)
- Status display and metadata (existing InjectStatusChip, formatDate utilities)

## Testing Requirements

Each story includes comprehensive test coverage:

### S12 Tests
- `useInjectSelection` hook unit tests (selection state)
- InjectListPage integration tests (toolbar display, selection clearing)
- Responsive behavior tests

### S13 Tests
- Button visibility conditions (approval enabled, Draft status, permissions)
- Submission flow (success, validation errors)
- Responsive variants (mobile icon-only, desktop full)
- Tooltip behavior (validation errors)

### S14 Tests
- Button visibility in drawer footer (status-dependent)
- Self-approval prevention (disabled state)
- Approval status display (header metadata, notes, rejection alerts)
- Drawer persistence (remains open after actions)
- Integration with conduct page

## Files Modified/Created

### New Files
```
docs/features/inject-approval-workflow/
├── S12-batch-approval-integration.md
├── S13-quick-submit-table-action.md
└── S14-drawer-approval-actions.md

src/frontend/src/features/injects/hooks/
└── useInjectSelection.ts  (S12 - new hook)

src/frontend/src/features/injects/components/
└── DrawerFooterActions.tsx  (S14 - new component)
```

### Modified Files
```
docs/features/inject-approval-workflow/
└── FEATURE.md  (updated story table, file list, total points)

src/frontend/src/features/injects/components/
├── InjectTable.tsx          (S12: checkboxes, S13: Submit button in row)
├── SubmitForApprovalButton.tsx  (S13: responsive variants)
└── InjectDetailDrawer.tsx   (S14: approval props, status display, footer)

src/frontend/src/features/injects/pages/
└── InjectListPage.tsx       (S12: selection state, toolbar integration)

src/frontend/src/features/exercises/pages/
└── ConductPage.tsx          (S14: approval callbacks for drawer)
```

## User Value Proposition

### For Controllers (MSEL Preparation)
**Before:** Navigate to detail page → Submit → Back to list × 50 injects
**After:** Click Submit in row × 50 injects
**Time Saved:** 4-8 minutes per MSEL preparation session

### For Exercise Directors (MSEL Review)
**Before:** Review each inject detail page individually → Approve/Reject × 50 injects
**After:** Select multiple injects → Batch approve with one click
**Time Saved:** 10-15 minutes per MSEL review session

### For Conduct Team (Exercise Execution)
**Before:** Open detail page from conduct view → Approve inject → Back to conduct
**After:** Open drawer → Approve in drawer → Continue in conduct view
**Time Saved:** 100 seconds (20 injects) + reduced context switching

## HSEEP Compliance

All stories use HSEEP-compliant terminology:
- "Inject" (not "event" or "message")
- "Submit for Approval" (not "send for review")
- "Exercise Director" (not "admin" or "approver")
- "Controller" (not "author" or "creator")
- "MSEL" (not "scenario list" or "inject list")

## Next Steps

1. **Review Stories:** Validate acceptance criteria with stakeholders
2. **Prioritize:** Confirm P1/P2 priorities align with release goals
3. **Estimate:** Team estimates during sprint planning (using story points)
4. **Implement:** Follow TDD workflow per CLAUDE.md guidelines
5. **Test:** Comprehensive unit + integration tests before merge

## Questions for Product Owner

- [ ] Are these enhancements aligned with MVP release scope?
- [ ] Should S12/S13 (P1) be included in initial release, or deferred to Standard phase?
- [ ] Is S14 (P2 - conduct drawer) needed for v1.0, or can it wait?
- [ ] Any additional UX considerations for mobile approval workflows?

## Story Status

| Story | Status | Assignee | Sprint |
|-------|--------|----------|--------|
| S12   | Ready  | TBD      | TBD    |
| S13   | Ready  | TBD      | TBD    |
| S14   | Ready  | TBD      | TBD    |

---

**Summary:** Three well-defined enhancement stories that improve approval workflow UX by reducing navigation friction and enabling batch operations. Total feature scope increases from 48 to 56 points (+17% scope). All stories follow HSEEP terminology, INVEST criteria, and existing component patterns.
