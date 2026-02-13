# Story OM-13: Track-Agency Bridge

**Priority:** P2 (Standard - after OM-09 and OM-10)
**Feature:** Organization Management
**Created:** 2026-02-12

---

## User Story

**As an** Organization Administrator or Exercise Director,
**I want** the free-text Track field on injects to be informed by the organization's agency list,
**So that** Track values are consistent, agency-aware, and can feed into agency-based filtering and reporting.

---

## Context

Cadence has two parallel concepts for categorizing injects by responding organization:

| Concept | Type | Scope | Introduced |
|---------|------|-------|------------|
| **Track** | Free-text string (`Inject.Track`) | Per-inject | MVP |
| **Agency** | Structured entity (`Agency`, `Inject.TargetAgencyId`) | Per-organization | Standard (OM-09/OM-10) |

These serve similar purposes but are not connected:
- **Track** is lightweight and organic - users type whatever they want (e.g., "FD", "Fire", "Fire Dept", "Fire Department")
- **Agency** is structured with Name, Abbreviation, sort order, and active/inactive status

Without a bridge, users face friction:
1. The Track autocomplete (S06) suggests historical free-text values, but not agencies
2. Setting `TargetAgencyId` requires a separate dropdown, duplicating the Track concept
3. Imported MSELs have Track text but no agency linkage
4. Filtering by Track (text) and filtering by Agency (FK) are separate operations

This story defines how these two concepts interact when agencies are available.

---

## Acceptance Criteria

### Track Field Enhancement (When Agencies Exist)

- [ ] **Given** my organization has agencies defined, **when** I focus the Track field on the inject form, **then** I see organization agencies as the top suggestions (above historical free-text values)
- [ ] **Given** agencies are shown as suggestions, **when** I view an agency suggestion, **then** I see the agency name with its abbreviation (e.g., "Fire Department (FD)")
- [ ] **Given** I select an agency from the Track suggestions, **when** the value is set, **then** the Track field shows the agency name AND the `TargetAgencyId` is automatically set
- [ ] **Given** I type a free-text value that does not match an agency, **when** I save the inject, **then** Track is saved as free text and `TargetAgencyId` remains null
- [ ] **Given** my organization has NO agencies defined, **when** I use the Track field, **then** it behaves exactly as in S06 (autocomplete from historical values only)

### Automatic Agency Matching

- [ ] **Given** I type text in the Track field, **when** the text exactly matches an agency name or abbreviation (case-insensitive), **then** the system suggests linking to that agency
- [ ] **Given** I type "FD" in Track, **when** an agency with abbreviation "FD" exists, **then** "Fire Department (FD)" appears as the top suggestion
- [ ] **Given** I select a matched agency, **when** the inject is saved, **then** both `Track` = "Fire Department" and `TargetAgencyId` = {agency guid} are set

### Agency-to-Track Sync

- [ ] **Given** an inject has a `TargetAgencyId` set (from OM-10 dropdown), **when** the Track field is empty, **then** the Track field auto-fills with the agency name
- [ ] **Given** an inject has both Track and TargetAgencyId, **when** they refer to the same agency, **then** no special display is needed
- [ ] **Given** an inject has Track text that doesn't match the assigned agency, **when** viewing the inject, **then** both values are displayed (Track and Agency are independent)

### Bulk Track-to-Agency Mapping

- [ ] **Given** I am an OrgAdmin or Exercise Director, **when** I navigate to exercise settings, **then** I see a "Map Tracks to Agencies" action
- [ ] **Given** I click "Map Tracks to Agencies", **when** a dialog opens, **then** I see a list of distinct Track values used in this exercise with no corresponding TargetAgencyId
- [ ] **Given** I see unmatched Track values, **when** a Track value closely matches an agency name or abbreviation, **then** the agency is pre-suggested in a dropdown
- [ ] **Given** I review the mapping, **when** I confirm, **then** all injects with that Track value have their TargetAgencyId set to the selected agency
- [ ] **Given** some Track values don't map to any agency, **when** I leave them unmapped, **then** they remain as free-text Track values (no agency assigned)

### Import Integration

- [ ] **Given** I import a MSEL with Track/Agency column values, **when** a Track value matches an existing agency (name or abbreviation), **then** the `TargetAgencyId` is automatically linked
- [ ] **Given** I import a MSEL with Track values that don't match agencies, **when** import completes, **then** Track is stored as text and no TargetAgencyId is set
- [ ] **Given** import linked some Track values to agencies, **when** viewing the import summary, **then** I see how many injects were auto-linked to agencies

---

## Out of Scope

- Automatic creation of agencies from Track values (agencies are managed explicitly via OM-09)
- Merging or deduplicating existing Track text values
- Multi-agency per inject (single TargetAgencyId only, per OM-10)
- Changing the Track field to a strict dropdown (it remains freeSolo)
- Retroactive linking of historical exercises (only applies to active/future exercises)

---

## Dependencies

- OM-09: Agency List Management (agencies must exist)
- OM-10: Agency Assignment (TargetAgencyId field on Inject)
- inject-crud/S06: Inject Field Autocomplete (autocomplete infrastructure)
- excel-import-export (for import integration)

---

## Domain Terms

| Term | Definition |
|------|------------|
| Track | Free-text agency grouping field on an inject (lightweight categorization) |
| Agency | Structured organization entity managed at the org level (OM-09) |
| TargetAgencyId | Foreign key on Inject linking to a specific Agency entity (OM-10) |
| Bridge | The connection logic between free-text Track and structured Agency |

---

## UI/UX Notes

### Track Field with Agency Suggestions

When agencies exist, the Track autocomplete shows two sections:

```
Track
┌─────────────────────────────────────────┐
│ Fi|                                      │
└─────────────────────────────────────────┘
  ┌─────────────────────────────────────┐
  │ ── Organization Agencies ──         │
  │ 🏢 Fire Department (FD)            │  ← From agency list
  │                                     │
  │ ── Previously Used ──               │
  │ Fire Marshal's Office        (3x)   │  ← From autocomplete history
  │ Finance Section              (1x)   │
  └─────────────────────────────────────┘
```

### Bulk Mapping Dialog

```
┌─────────────────────────────────────────────────────────────────┐
│ Map Track Values to Agencies                               [X]  │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│ Link free-text Track values to organization agencies for         │
│ consistent filtering and reporting.                              │
│                                                                   │
│ Track Value          │ Injects │ Agency Match                    │
│ ─────────────────────┼─────────┼──────────────────────────────── │
│ "Fire Department"    │   12    │ [Fire Department (FD)      ▼]  │  ← Auto-suggested
│ "FD"                 │    5    │ [Fire Department (FD)      ▼]  │  ← Matched by abbrev
│ "EMS"                │    8    │ [Emergency Medical Svcs    ▼]  │  ← Matched by abbrev
│ "Hospital"           │    3    │ [Hospital Network          ▼]  │  ← Fuzzy match
│ "Red Team"           │    2    │ [-- No match --            ▼]  │  ← No agency match
│                                                                   │
│ ℹ️ 28 of 30 injects can be linked to agencies.                  │
│ 2 injects with unmatched tracks will keep their text values.    │
│                                                                   │
│                              [Cancel]  [Apply Mapping (28)]      │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

### Frontend: Enhanced Track Autocomplete

The Track field autocomplete (from S06) needs to be enhanced to also query agencies:

```typescript
// Combine agency list with autocomplete suggestions
const { data: agencies } = useAgencies(organizationId);
const { data: trackSuggestions } = useTrackSuggestions(exerciseId, filter);

const combinedOptions = [
  // Agency options (grouped)
  ...agencies.filter(a => a.isActive).map(a => ({
    label: `${a.name} (${a.abbreviation})`,
    value: a.name,
    agencyId: a.id,
    group: 'Organization Agencies',
  })),
  // Historical suggestions (grouped)
  ...trackSuggestions.filter(s => !agencyNames.includes(s)).map(s => ({
    label: s,
    value: s,
    agencyId: null,
    group: 'Previously Used',
  })),
];
```

### Backend: Bulk Mapping Endpoint

```
POST /api/exercises/{exerciseId}/injects/map-tracks-to-agencies
Authorization: Bearer {token} (ExerciseDirector or OrgAdmin)

Request:
{
  "mappings": [
    { "trackValue": "Fire Department", "agencyId": "guid-1" },
    { "trackValue": "FD", "agencyId": "guid-1" },
    { "trackValue": "EMS", "agencyId": "guid-2" }
  ]
}

Response (200 OK):
{
  "updatedCount": 25,
  "mappings": [
    { "trackValue": "Fire Department", "agencyName": "Fire Department", "injectsUpdated": 12 },
    { "trackValue": "FD", "agencyName": "Fire Department", "injectsUpdated": 5 },
    { "trackValue": "EMS", "agencyName": "Emergency Medical Services", "injectsUpdated": 8 }
  ]
}
```

### Backend: Track-Agency Auto-Link on Save

When saving an inject, if Track text matches an agency name or abbreviation and no TargetAgencyId is set, auto-link:

```csharp
// In InjectService.CreateAsync or UpdateAsync
if (inject.TargetAgencyId == null && !string.IsNullOrEmpty(inject.Track))
{
    var matchedAgency = await _context.Agencies
        .Where(a => a.OrganizationId == _orgContext.OrganizationId && a.IsActive)
        .FirstOrDefaultAsync(a =>
            a.Name.ToLower() == inject.Track.ToLower() ||
            a.Abbreviation.ToLower() == inject.Track.ToLower());

    if (matchedAgency != null)
    {
        inject.TargetAgencyId = matchedAgency.Id;
    }
}
```

### Import Enhancement

The Excel import pipeline should attempt agency matching after Track values are mapped:

```csharp
// After column mapping resolves Track value
var track = row["Track"];
var agency = agencies.FirstOrDefault(a =>
    string.Equals(a.Name, track, StringComparison.OrdinalIgnoreCase) ||
    string.Equals(a.Abbreviation, track, StringComparison.OrdinalIgnoreCase));

inject.Track = track;
inject.TargetAgencyId = agency?.Id;
```

---

## Test Scenarios

| Scenario | Test Type | Priority |
|----------|-----------|----------|
| Track field shows agencies when org has agencies | Component | P0 |
| Selecting agency sets both Track and TargetAgencyId | Component | P0 |
| Free-text Track without agency match saves normally | Component | P0 |
| Track field works normally when org has no agencies | Component | P0 |
| Bulk mapping dialog shows unmatched Track values | Component | P1 |
| Bulk mapping auto-suggests agencies by name/abbreviation | Component | P1 |
| Bulk mapping updates TargetAgencyId on affected injects | Integration | P0 |
| Auto-link on save matches agency name (case-insensitive) | Unit | P0 |
| Auto-link on save matches abbreviation | Unit | P0 |
| Auto-link does not overwrite existing TargetAgencyId | Unit | P0 |
| Import auto-links Track to agencies | Integration | P1 |
| Import summary shows agency link count | Component | P1 |

---

## Implementation Checklist

### Backend
- [ ] Add auto-link logic to InjectService (match Track → Agency on save)
- [ ] Create `POST /api/exercises/{id}/injects/map-tracks-to-agencies` endpoint
- [ ] Create `GET /api/exercises/{id}/injects/unmatched-tracks` endpoint (distinct Track values without TargetAgencyId)
- [ ] Add agency matching to Excel import pipeline
- [ ] Unit tests for auto-link logic
- [ ] Integration tests for bulk mapping endpoint

### Frontend
- [ ] Enhance Track autocomplete to show agency suggestions (grouped)
- [ ] Handle agency selection → set both Track and TargetAgencyId
- [ ] Create `MapTracksDialog` component
- [ ] Add "Map Tracks to Agencies" action in exercise settings
- [ ] Show agency link count in import summary
- [ ] Component tests for enhanced Track field
- [ ] Component tests for bulk mapping dialog

---

## Design Decisions

### Why Not Replace Track with Agency?

The Track field serves a different purpose than TargetAgencyId:

1. **Track is flexible**: Not every exercise needs formal agency management. Small TTX exercises may just need quick text labels.
2. **Track survives import**: Excel MSELs always have text columns, not foreign keys. Track preserves imported text.
3. **Track is backward compatible**: Existing data uses Track. Replacing it would require migration.
4. **Agency adds structure**: When an organization invests in defining agencies, the bridge gives them structured benefits (filtering, reporting) without breaking the simple workflow.

The bridge approach lets Track and Agency coexist: simple exercises use Track as text, structured exercises use Track linked to agencies.

---

## Changelog

| Date | Change |
|------|--------|
| 2026-02-12 | Initial story creation |
