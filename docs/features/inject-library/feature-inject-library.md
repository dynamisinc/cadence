# Feature: Inject Library

**Parent Epic:** Collaborative MSEL Review / Platform Capability
**Priority:** P1 — Long-term moat; data model decisions required now
**Phase:** K (standalone phase, after J)

---

## Description

Every inject created in Cadence can be saved to a reusable library — either the organization's private library or, in a future capability, a curated shared library. When planning a new exercise, planners browse, search, and pull injects from the library as starting points, adapting them to the current exercise context. Over time, the library becomes the organization's institutional knowledge about exercise design.

The Inject Library is primarily a **planning accelerator** and **institutional memory tool**. It is not a template system (that is a separate feature) — it is a searchable repository of discrete inject building blocks.

---

## Domain Terms

| Term | Definition |
|------|------------|
| Library Inject | An inject saved to the library; it is a template/source, not an active exercise inject |
| Exercise Inject | An active inject within a specific MSEL; derived from a library inject or created from scratch |
| Library Source | The origin of a library inject: Cadence Built-in (starter packs), Organization (user-created), Shared (future — cross-org) |
| Starter Pack | A curated set of library injects for a specific exercise type (e.g., Hurricane Response FSE, Active Shooter TTX) |
| Instantiation | The act of pulling a library inject into an active MSEL; creates a copy, not a reference |

---

## User Stories

---

### Story IL-1: Save an Inject to the Library

**As a** Planner,
**I want** to save a well-designed inject to the organization's inject library,
**So that** future exercises can reuse it as a starting point without rebuilding from scratch.

#### Acceptance Criteria

- [ ] **Given** I am viewing an inject in a completed exercise, **when** I click "Save to Library", **then** a dialog opens prompting me to: confirm or edit the inject title, add library tags (exercise type, capability area, inject category), and add optional notes about when/how to use this inject
- [ ] **Given** I confirm the save, **when** the inject is stored in the library, **then** it is attributed to my organization and marked with the source exercise and date
- [ ] **Given** I save an inject that already exists in the library (matched by title + capability tags), **when** the duplicate is detected, **then** I am warned and offered the option to save as a new variant or update the existing entry
- [ ] **Given** I have the appropriate role (Director or Planner), **when** I view any exercise inject, **then** the "Save to Library" option is available on the inject detail panel

---

### Story IL-2: Browse and Search the Inject Library

**As a** Planner,
**I want** to browse and search the inject library when building a new MSEL,
**So that** I can find relevant injects quickly without scrolling through unrelated content.

#### Acceptance Criteria

- [ ] **Given** I am on the MSEL view or the library page, **when** I open the Inject Library panel, **then** I see a searchable, filterable list of all library injects available to my organization
- [ ] **Given** I view the library, **when** I apply filters, **then** I can filter by: source (Built-in / Organization / Shared), exercise type, capability area, inject category, keyword search of title and description
- [ ] **Given** I search by keyword, **when** results appear, **then** matching terms are highlighted in the inject title and description
- [ ] **Given** I view a library inject, **when** I click it, **then** I see the full inject detail including: description, suggested timing, expected actions, evaluation criteria, capability tags, usage history (how many times it has been used across exercises)
- [ ] **Given** I view the library, **when** I have no filters active, **then** injects are grouped by exercise type and sorted by usage frequency (most used first)

---

### Story IL-3: Add a Library Inject to the Current MSEL

**As a** Planner,
**I want** to pull a library inject into the current MSEL as a starting point,
**So that** I can adapt a proven inject to the current exercise context rather than writing from scratch.

#### Acceptance Criteria

- [ ] **Given** I am viewing a library inject, **when** I click "Add to MSEL", **then** a dialog opens where I can set: target phase, scheduled time, assigned controller, and any immediate adaptations to the description
- [ ] **Given** I confirm the addition, **when** the inject is created in the MSEL, **then** it is a full copy — changes to the MSEL inject do not affect the library entry
- [ ] **Given** the inject was added from the library, **when** I view it in the MSEL, **then** a "From Library" indicator shows its origin with a link back to the library entry
- [ ] **Given** I add a library inject to the MSEL, **when** I view the library entry, **then** its usage count increments and the current exercise is listed in its usage history

---

### Story IL-4: Cadence Starter Packs

**As a** Planner starting a new exercise,
**I want** to browse a curated set of Cadence-provided starter injects for my exercise type,
**So that** I have a structured starting point rather than a blank MSEL.

#### Context
Starter packs are the "out of the box" value proposition for new Cadence organizations. They represent the Cadence team's institutional knowledge about what makes a well-designed exercise for common scenarios. They are clearly marked as Built-in / Cadence-provided and are not editable (though they can be copied to the organization library and then edited).

#### Initial Starter Pack Targets (v1)
- Hurricane/Flood Response FSE (FEMA framework)
- Active Shooter / Mass Casualty Incident FSE
- Cyber Incident Response Tabletop (NIST CSF framework)
- EOC Activation and Operations TTX
- Multi-Agency Coordination TTX

#### Acceptance Criteria

- [ ] **Given** I am browsing the inject library, **when** I filter by Source = Built-in, **then** I see Cadence-provided starter packs organized by exercise type
- [ ] **Given** I select a starter pack, **when** I view it, **then** I see: recommended exercise type, target capability framework, inject count, suggested phase structure, and the inject list
- [ ] **Given** I click "Import Starter Pack to MSEL", **when** I confirm, **then** all injects in the starter pack are added to the current MSEL with their default phase assignments, and I can edit any of them
- [ ] **Given** a starter pack inject exists in my organization library, **when** I import the pack, **then** my organization's version takes precedence over the Cadence default

---

### Story IL-5: Manage Library Injects

**As a** Planner or Exercise Director,
**I want** to edit, archive, and organize the organization's library injects,
**So that** the library stays accurate and useful over time.

#### Acceptance Criteria

- [ ] **Given** I have edit rights, **when** I view a library inject, **then** I can edit all fields: title, description, tags, notes
- [ ] **Given** I edit a library inject, **when** changes are saved, **then** existing MSEL injects that were previously instantiated from this library entry are NOT affected (copy-on-instantiation model)
- [ ] **Given** a library inject is outdated, **when** I click "Archive", **then** the inject is hidden from search results but its usage history is preserved; it can be restored
- [ ] **Given** I want to organize the library, **when** I manage library tags, **then** I can create, rename, and merge custom tags at the organization level
- [ ] **Given** I am an Administrator, **when** I view the library, **then** I can see usage analytics: most used injects, injects never used, injects used in the last 12 months

---

## Data Model Additions

### New Entities

```
LibraryInject
  Id                  GUID
  OrganizationId      GUID (FK → Organization; null for Cadence built-ins)
  Title               string
  Description         string
  ExpectedActions     string (what the player is expected to do)
  EvaluationCriteria  string
  SuggestedTiming     string (e.g., "H+1:00 to H+2:00")
  InjectCategory      string
  Source              enum: BuiltIn | Organization | Shared
  IsArchived          bool
  CreatedAt           datetime
  CreatedBy           GUID (FK → User; null for built-ins)
  UpdatedAt           datetime

LibraryInjectTag
  LibraryInjectId     GUID (FK → LibraryInject)
  TagType             enum: ExerciseType | CapabilityArea | InjectCategory | Custom
  TagValue            string

LibraryInjectUsage
  Id                  GUID
  LibraryInjectId     GUID (FK → LibraryInject)
  ExerciseId          GUID (FK → Exercise)
  InjectId            GUID (FK → Inject — the MSEL inject created from this library entry)
  UsedAt              datetime
  UsedBy              GUID (FK → User)

StarterPack
  Id                  GUID
  Name                string
  ExerciseType        string
  CapabilityFramework enum
  Description         string
  IsActive            bool

StarterPackInject
  StarterPackId       GUID (FK → StarterPack)
  LibraryInjectId     GUID (FK → LibraryInject)
  SuggestedPhase      string
  DisplayOrder        int
```

### Inject Entity Addition
```
Inject
  ...existing fields...
  LibrarySourceId     GUID? (FK → LibraryInject; null if not from library)
```

---

## API Endpoints Required

| Method | Route | Description |
|--------|-------|-------------|
| GET | /library/injects | Search/browse library injects |
| POST | /library/injects | Save inject to library |
| GET | /library/injects/{id} | Get library inject detail |
| PUT | /library/injects/{id} | Update library inject |
| POST | /library/injects/{id}/archive | Archive library inject |
| POST | /injects/{id}/save-to-library | Save MSEL inject to library (shortcut) |
| POST | /library/injects/{id}/add-to-msel | Instantiate library inject into a MSEL |
| GET | /library/starter-packs | List available starter packs |
| GET | /library/starter-packs/{id} | Get starter pack detail |
| POST | /library/starter-packs/{id}/import | Import starter pack into a MSEL |
| GET | /library/injects/{id}/usage | Get usage history for a library inject |

---

## Implementation Notes

**Copy-on-instantiation is non-negotiable.** Library injects are source material, not living references. Once an inject is pulled into a MSEL, it belongs to that exercise. This prevents a library edit from silently changing an in-progress MSEL.

**Data model must be established in Phase C or the next backend sprint** — even if the UI is deferred. The `LibrarySourceId` field on the Inject entity is a foreign key that is much cheaper to add now than to retrofit later. A null value means "not from library," so it is backward compatible.

**Starter packs are seeded data**, not user-created content. They are maintained by the Cadence team and deployed via migration seed scripts. Organizations cannot edit them, but can copy them to their org library and customize freely.
