# Story: S06 - Inject Field Autocomplete

**Priority:** P0 (MVP - Quick Win)
**Feature:** Inject CRUD
**Created:** 2026-02-12

---

## User Story

**As a** Controller or Exercise Director authoring a MSEL,
**I want** previously used values to appear as suggestions when I type in Source, Target, Track, and other free-text fields,
**So that** I can quickly enter repeated values without retyping them for every inject.

---

## Context

When building a MSEL with 30-100+ injects, many free-text fields contain repeated values. For example, the same Source ("County Emergency Manager"), Target ("EOC Director"), or Track ("Fire") may appear across dozens of injects. Currently these are plain text inputs requiring manual entry each time.

An autocomplete backend and frontend hook infrastructure **already exists** but is not connected to the InjectForm:

| Layer | Status | Location |
|-------|--------|---------|
| Backend service | Built | `Core/Features/Autocomplete/Services/AutocompleteService.cs` |
| API controller | Built | `WebApi/Controllers/AutocompleteController.cs` |
| Frontend hooks | Built | `features/autocomplete/hooks/useAutocomplete.ts` |
| **InjectForm integration** | **NOT DONE** | `features/injects/components/InjectForm.tsx` |

The autocomplete service is **organization-scoped** and **frequency-ordered**: it queries distinct values from all injects across all exercises in the organization, ranked by usage count. This "learn as you go" approach means suggestions improve automatically as users create more injects.

This story connects the existing infrastructure to the UI.

---

## Acceptance Criteria

### Autocomplete on Primary Fields

- [ ] **Given** I am creating or editing an inject, **when** I focus the "From (Source)" field, **then** I see a dropdown of previously used Source values from my organization
- [ ] **Given** I am creating or editing an inject, **when** I focus the "To (Target)" field, **then** I see a dropdown of previously used Target values from my organization
- [ ] **Given** I am creating or editing an inject, **when** I focus the "Track" field, **then** I see a dropdown of previously used Track values from my organization

### Autocomplete on Advanced Fields

- [ ] **Given** I expand the Advanced section, **when** I focus "Responsible Controller", **then** I see suggestions from previously used values
- [ ] **Given** I expand the Advanced section, **when** I focus "Location Name", **then** I see suggestions from previously used values
- [ ] **Given** I expand the Advanced section, **when** I focus "Location Type", **then** I see suggestions from previously used values

### Typeahead Filtering

- [ ] **Given** I see autocomplete suggestions, **when** I type characters, **then** the suggestion list filters to match my input
- [ ] **Given** I type "Fire", **when** suggestions contain "Fire Department" and "Fire Marshal", **then** both appear
- [ ] **Given** I type text that matches no suggestions, **when** viewing the dropdown, **then** the dropdown shows no options (or is hidden) and I can continue typing a new value

### Free-Text Entry (freeSolo)

- [ ] **Given** I am typing in an autocomplete field, **when** I enter a value not in the suggestions, **then** I can submit the form with the new value
- [ ] **Given** I type a new value, **when** I press Tab or click away, **then** the new value is accepted without selecting from suggestions
- [ ] **Given** I select a suggestion, **when** the form submits, **then** the selected value is used exactly as displayed

### Suggestion Ordering and Limits

- [ ] **Given** suggestions are displayed, **when** I view the list, **then** they are ordered by frequency (most used first)
- [ ] **Given** there are many suggestions, **when** I view the list, **then** at most 20 suggestions are shown
- [ ] **Given** suggestions exist from other exercises in my organization, **when** I create a new exercise, **then** I see those suggestions in the new exercise too

### Loading and Error States

- [ ] **Given** suggestions are loading, **when** I view the field, **then** I see a subtle loading indicator (spinner in the field)
- [ ] **Given** the autocomplete API fails, **when** I type in the field, **then** the field still works as a normal text input (graceful degradation)

### Keyboard Navigation

- [ ] **Given** suggestions are visible, **when** I press ArrowDown/ArrowUp, **then** I can navigate through suggestions
- [ ] **Given** a suggestion is highlighted, **when** I press Enter, **then** the suggestion is selected
- [ ] **Given** suggestions are visible, **when** I press Escape, **then** the dropdown closes

---

## Out of Scope

- Agency entity integration (see OM-10 and OM-13)
- Managed/curated value lists (this is organic, user-driven suggestions)
- Admin ability to edit or remove suggestion values
- Cross-organization suggestions
- Minimum character threshold before showing suggestions (show on focus)

---

## Dependencies

- inject-crud/S01: Create Inject (form must exist)
- inject-crud/S02: Edit Inject (edit form must exist)
- Autocomplete backend service (already implemented)
- Autocomplete frontend hooks (already implemented)

---

## Domain Terms

| Term | Definition |
|------|------------|
| Source | The simulated sender of the inject (e.g., "County Emergency Manager") |
| Target | The player or role receiving the inject (e.g., "EOC Director") |
| Track | Agency grouping for multi-agency exercises (e.g., "Fire", "EMS") |
| freeSolo | MUI Autocomplete mode that allows typing values not in the suggestion list |

---

## UI/UX Notes

### Before (Current)

Plain text inputs with no suggestions:
```
From (Source)
┌─────────────────────────────────────────┐
│                                          │  ← User types everything manually
└─────────────────────────────────────────┘
```

### After (With Autocomplete)

Autocomplete with suggestion dropdown:
```
From (Source)
┌─────────────────────────────────────────┐
│ County E|                                │  ← User starts typing
└─────────────────────────────────────────┘
  ┌─────────────────────────────────────┐
  │ County Emergency Manager     (15x)  │  ← Most frequently used
  │ County Executive Office       (8x)  │
  │ County EOC                    (3x)  │
  └─────────────────────────────────────┘
```

### Field Behavior

- **On focus**: Show top suggestions immediately (no typing required)
- **On type**: Filter suggestions as user types
- **On select**: Fill field with selected value
- **On blur with new value**: Accept custom text (freeSolo)
- **Loading**: Show small spinner inside the field's end adornment
- **Error**: Silently fall back to plain text input

### Visual Treatment

- Use MUI `Autocomplete` with `freeSolo` prop
- Wrap with COBRA styling (consistent with existing form fields)
- Suggestion items should show the value text; optionally show usage count in muted text
- No explicit "create new" option needed - freeSolo handles this naturally

---

## Technical Notes

### Frontend Implementation

The existing hooks in `features/autocomplete/hooks/useAutocomplete.ts` provide:

```typescript
// Already built - just need to import and use
useTrackSuggestions(exerciseId, filter, limit)
useTargetSuggestions(exerciseId, filter, limit)
useSourceSuggestions(exerciseId, filter, limit)
useLocationNameSuggestions(exerciseId, filter, limit)
useLocationTypeSuggestions(exerciseId, filter, limit)
useResponsibleControllerSuggestions(exerciseId, filter, limit)
```

### InjectForm Changes

Replace `CobraTextField` with MUI `Autocomplete` (freeSolo) for the six fields:

```typescript
import { Autocomplete } from '@mui/material';
import { useSourceSuggestions } from '@/features/autocomplete';

// In the form component:
const [sourceInput, setSourceInput] = useState('');
const { data: sourceSuggestions = [] } = useSourceSuggestions(exerciseId, sourceInput);

<Autocomplete
  freeSolo
  options={sourceSuggestions}
  inputValue={sourceInput}
  onInputChange={(_, value) => setSourceInput(value)}
  value={formValues.source}
  onChange={(_, value) => setFieldValue('source', value || '')}
  renderInput={(params) => (
    <CobraTextField {...params} label="From (Source)" placeholder="e.g., County Emergency Manager" />
  )}
/>
```

### Debouncing

The autocomplete hooks should debounce the filter parameter to avoid excessive API calls. A 300ms debounce on the input value before triggering the query is recommended.

### Caching

The existing hooks use React Query with 1-minute stale time. This means:
- Suggestions are cached and reused across form opens
- The cache is shared across create/edit forms
- Fresh data is fetched after 1 minute of staleness

---

## Test Scenarios

| Scenario | Test Type | Priority |
|----------|-----------|----------|
| Source field shows autocomplete suggestions | Component | P0 |
| Target field shows autocomplete suggestions | Component | P0 |
| Track field shows autocomplete suggestions | Component | P0 |
| Typing filters suggestions | Component | P0 |
| Selecting a suggestion fills the field | Component | P0 |
| New values (not in suggestions) can be entered | Component | P0 |
| Suggestions ordered by frequency | Component | P1 |
| Loading state shown while fetching | Component | P1 |
| Graceful degradation on API failure | Component | P1 |
| Keyboard navigation (ArrowDown, Enter, Escape) | Component | P1 |
| Advanced fields (Location, Controller) show suggestions | Component | P1 |

---

## Implementation Checklist

### Frontend
- [ ] Create reusable `AutocompleteTextField` component (wraps MUI Autocomplete + CobraTextField)
- [ ] Add debounced input state management (300ms)
- [ ] Replace Source `CobraTextField` with autocomplete
- [ ] Replace Target `CobraTextField` with autocomplete
- [ ] Replace Track `CobraTextField` with autocomplete
- [ ] Replace ResponsibleController `CobraTextField` with autocomplete
- [ ] Replace LocationName `CobraTextField` with autocomplete
- [ ] Replace LocationType `CobraTextField` with autocomplete
- [ ] Handle loading states (spinner in field)
- [ ] Handle error states (graceful degradation)
- [ ] Component tests for autocomplete behavior
- [ ] Test for freeSolo (new value entry)
- [ ] Test for suggestion selection

### Backend
- [ ] Verify autocomplete endpoints are functional (already implemented)
- [ ] Add tests for autocomplete service if missing

---

## Changelog

| Date | Change |
|------|--------|
| 2026-02-12 | Initial story creation |
