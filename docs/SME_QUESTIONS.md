# SME Questions for Cadence

> **68 total questions | 15 pre-answered from EXIS analysis | 53 pending**

## Purpose

This document tracks questions requiring Subject Matter Expert (SME) input. Questions are organized by feature area and marked with their resolution status.

---

## Question Status Legend

| Status | Meaning |
|--------|---------|
| ✅ Answered | Resolution confirmed by SME or EXIS analysis |
| 🔶 Proposed | Claude has a recommendation pending SME validation |
| ❓ Open | Needs SME input before development |
| ⏸️ Deferred | Not needed for current phase |

---

## Core Entity Questions

### Exercise Entity

| # | Question | Status | Resolution |
|---|----------|--------|------------|
| 1 | Should exercises support multiple locations in MVP? | 🔶 Proposed | Recommend single location for MVP, multi-location in Advanced phase |
| 2 | What exercise metadata is required vs. optional? | ✅ Answered | Required: Name, Type, Date. Optional: Location, Description, Time Zone |
| 3 | Should archived exercises be permanently deletable? | 🔶 Proposed | Recommend no permanent deletion; archive is final state |
| 4 | How long should exercise data be retained? | ❓ Open | Need compliance guidance |

### Inject Entity

| # | Question | Status | Resolution |
|---|----------|--------|------------|
| 5 | What is the maximum inject description length? | 🔶 Proposed | Recommend 4000 characters (allows substantial content) |
| 6 | Should inject numbers auto-increment or allow manual entry? | ✅ Answered | EXIS uses auto-increment with manual override capability |
| 7 | Are inject timestamps required or optional? | ✅ Answered | Scheduled Time required; Scenario Time optional |
| 8 | Should deleted injects be hard or soft deleted? | 🔶 Proposed | Soft delete with archive flag; prevents audit trail gaps |
| 9 | What inject methods should be pre-defined? | ✅ Answered | Verbal, Phone, Email, Radio, Written, Simulation System |

### User Roles

| # | Question | Status | Resolution |
|---|----------|--------|------------|
| 10 | Can a user have multiple roles within an exercise? | 🔶 Proposed | Recommend no; single role per exercise for clarity |
| 11 | Should role permissions be configurable per exercise? | 🔶 Proposed | Fixed role definitions for MVP; configurable in Standard |
| 12 | Who can change user roles mid-exercise? | ✅ Answered | Administrator and Exercise Director only |
| 13 | Should there be a "Super Controller" role? | ❓ Open | Some exercises have senior Controllers with elevated access |

---

## Exercise Setup Questions

### Exercise CRUD

| # | Question | Status | Resolution |
|---|----------|--------|------------|
| 14 | Can exercises be duplicated? | ✅ Answered | Yes, duplicate exercise creates new exercise with copied MSEL |
| 15 | Should exercise templates be supported in MVP? | 🔶 Proposed | Defer to Advanced phase; duplicate covers basic need |
| 16 | What validation rules for exercise dates? | ❓ Open | Can exercises be backdated? Future-only? |
| 17 | Should exercise names be unique? | 🔶 Proposed | Unique within organization; allow duplicates across orgs |

### Practice Mode

| # | Question | Status | Resolution |
|---|----------|--------|------------|
| 18 | Can a practice exercise be converted to production? | 🔶 Proposed | No; must create new exercise to prevent data contamination |
| 19 | Should practice mode be visible in exercise list? | ✅ Answered | Yes, with clear visual indicator (badge/icon) |
| 20 | Are practice exercises counted in license limits? | ⏸️ Deferred | Licensing model TBD |

### Time Zone Configuration

| # | Question | Status | Resolution |
|---|----------|--------|------------|
| 21 | Should time zones be per-exercise or per-location? | ✅ Answered | Per-exercise for MVP; per-location in multi-location Advanced |
| 22 | How to handle Daylight Saving Time transitions? | 🔶 Proposed | Store all times in UTC; display in configured TZ |
| 23 | Default time zone for new exercises? | 🔶 Proposed | User's browser time zone as default |

### Exercise Configuration

| # | Question | Status | Resolution |
|---|----------|--------|------------|
| 24 | Should participant email be required? | 🔶 Proposed | Optional for MVP; required if notification features added |
| 25 | Can participants be added during exercise conduct? | ✅ Answered | Yes; late arrivals need to join exercises |
| 26 | Should there be a maximum participant count? | ❓ Open | Performance implications for large exercises |

---

## Objective Questions

| # | Question | Status | Resolution |
|---|----------|--------|------------|
| 27 | Should objectives have a priority hierarchy? | ✅ Answered | Yes: Core vs. Supporting (HSEEP standard) |
| 28 | Can an inject link to multiple objectives? | ✅ Answered | Yes; many injects address multiple objectives |
| 29 | Should objectives be reusable across exercises? | ⏸️ Deferred | Objective library planned for Advanced phase |
| 30 | How to handle objectives with no linked injects? | 🔶 Proposed | Warning indicator on progress dashboard |

---

## Phase Questions

| # | Question | Status | Resolution |
|---|----------|--------|------------|
| 31 | Are phases required for all exercises? | 🔶 Proposed | Optional; simple exercises may not need phases |
| 32 | Can phases overlap in time? | ❓ Open | Some exercises have parallel tracks |
| 33 | Should phase templates be provided? | 🔶 Proposed | Common defaults: Setup, Response, Recovery, Demobilization |
| 34 | Can injects belong to multiple phases? | 🔶 Proposed | No; single phase assignment for clarity |

---

## MSEL Management Questions

| # | Question | Status | Resolution |
|---|----------|--------|------------|
| 35 | How many MSEL versions should be retained? | 🔶 Proposed | Unlimited; storage is cheap |
| 36 | Can a MSEL be rolled back to a previous version? | ✅ Answered | EXIS supports this; valuable for recovery |
| 37 | Should version comparison be supported? | ⏸️ Deferred | Nice to have; not MVP |
| 38 | Can MSELs be shared between exercises? | 🔶 Proposed | No; duplicate exercise instead |

---

## Inject CRUD Questions

| # | Question | Status | Resolution |
|---|----------|--------|------------|
| 39 | Should inject creation have a quick-add mode? | ✅ Answered | EXIS lacks this; identified as pain point. Recommend yes |
| 40 | What happens to child injects when parent is deleted? | 🔶 Proposed | Orphan with warning; don't cascade delete |
| 41 | Should injects support file attachments? | ⏸️ Deferred | Planned for Standard phase |
| 42 | Maximum number of injects per MSEL? | 🔶 Proposed | Technical limit ~10,000; typical exercises 50-500 |

### Dual Time Tracking

| # | Question | Status | Resolution |
|---|----------|--------|------------|
| 43 | Should scenario time be required for any exercise type? | 🔶 Proposed | Optional for all types; some TTX don't need it |
| 44 | How to display scenario day in timeline views? | ✅ Answered | "Day X" prefix with time (e.g., "Day 2 14:30") |
| 45 | Can scenario time span multiple days within single session? | ✅ Answered | Yes; core use case for scenario time |
| 46 | Should there be a scenario start date/time configuration? | 🔶 Proposed | Yes; allows "scenario starts at Day 1 06:00" |

---

## Excel Import Questions

| # | Question | Status | Resolution |
|---|----------|--------|------------|
| 47 | Should custom column mappings be saved as templates? | 🔶 Proposed | Yes; reduces repeated configuration |
| 48 | How to handle Excel formulas in imported cells? | ✅ Answered | Import calculated value, not formula |
| 49 | Should import validate data types strictly? | 🔶 Proposed | Warn but allow; let users fix in app |
| 50 | Maximum file size for upload? | 🔶 Proposed | 10MB; covers very large MSELs |
| 51 | Should import support .xls (old format) or only .xlsx? | 🔶 Proposed | .xlsx only; .xls is legacy |

---

## Excel Export Questions

| # | Question | Status | Resolution |
|---|----------|--------|------------|
| 52 | Should export preserve original import formatting? | 🔶 Proposed | No; use standard Cadence template |
| 53 | What columns should be included in export? | ❓ Open | Need standard MSEL template specification |
| 54 | Should export include Controller notes? | 🔶 Proposed | Optional; separate worksheet or filtered |
| 55 | Should export include observations? | ⏸️ Deferred | AAR export is Advanced phase |

---

## Inject Organization Questions

| # | Question | Status | Resolution |
|---|----------|--------|------------|
| 56 | What grouping options beyond Phase? | ✅ Answered | Target, Source, Inject Type, Objective |
| 57 | Should custom groupings be saveable? | ⏸️ Deferred | Standard phase with saved views |
| 58 | How to handle reordering in grouped view? | 🔶 Proposed | Reorder within group only |
| 59 | Should drag-and-drop reorder be supported? | ✅ Answered | Yes; EXIS pain point was keyboard-only |

---

## UX Questions

### Keyboard Navigation

| # | Question | Status | Resolution |
|---|----------|--------|------------|
| 60 | What keyboard shortcuts are essential? | ✅ Answered | EXIS analysis: N(ew), E(dit), F(ire), S(ave), Esc(ape), Tab nav |
| 61 | Should shortcuts be customizable? | 🔶 Proposed | No; consistency more important |
| 62 | How to handle shortcut conflicts with browser? | 🔶 Proposed | Use Ctrl+ combinations; avoid F-keys |

### Auto-save

| # | Question | Status | Resolution |
|---|----------|--------|------------|
| 63 | What triggers auto-save? | ✅ Answered | Blur events (field exit) + 30-second interval |
| 64 | Should auto-save show confirmation? | ✅ Answered | Subtle indicator (checkmark/timestamp); no modal |
| 65 | How to handle auto-save conflicts? | 🔶 Proposed | Last-write-wins with conflict notification |

### Session Management

| # | Question | Status | Resolution |
|---|----------|--------|------------|
| 66 | What should session timeout be? | ✅ Answered | EXIS pain point was short timeout. Recommend 4 hours |
| 67 | Warning before timeout? | ✅ Answered | Yes; 5-minute warning with extend option |
| 68 | Should multiple browser tabs be supported? | 🔶 Proposed | Yes; sync across tabs |

---

## Summary Statistics

| Category | Total | Answered | Proposed | Open | Deferred |
|----------|-------|----------|----------|------|----------|
| Core Entities | 13 | 5 | 6 | 2 | 0 |
| Exercise Setup | 13 | 4 | 7 | 1 | 1 |
| Objectives | 4 | 2 | 1 | 0 | 1 |
| Phases | 4 | 0 | 3 | 1 | 0 |
| MSEL Management | 4 | 1 | 2 | 0 | 1 |
| Inject CRUD | 8 | 3 | 4 | 0 | 1 |
| Excel Import | 5 | 1 | 4 | 0 | 0 |
| Excel Export | 4 | 0 | 2 | 1 | 1 |
| Inject Organization | 4 | 2 | 1 | 0 | 1 |
| UX | 9 | 5 | 4 | 0 | 0 |
| **TOTAL** | **68** | **23** | **34** | **5** | **6** |

---

## SME Review Process

### How to Provide Feedback

1. Review questions in your area of expertise
2. For "Open" questions: Provide your recommendation
3. For "Proposed" questions: Confirm or counter-propose
4. Add any questions we've missed

### Feedback Format

```
Question #: [number]
Your Response: [answer or recommendation]
Rationale: [brief explanation]
Confidence: [High/Medium/Low]
```

### Contact

Questions can be submitted via:
- Direct response to this document
- Email to [project contact]
- Discussion in project repository

---

*Last updated: 2025-01-08*
