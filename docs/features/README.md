# Cadence Requirements Documentation

> **HSEEP-Compliant MSEL Management Platform**

## Overview

Cadence is a Master Scenario Events List (MSEL) management platform designed for emergency management exercise conduct. Unlike full lifecycle planning tools, Cadence focuses specifically on the **operations phase** of exercises—where Controllers deliver injects, Evaluators record observations, and Exercise Directors maintain situational awareness.

### Key Differentiators

| Capability | Cadence | Typical Tools |
|------------|---------|---------------|
| **Offline Operation** | ✅ Full functionality | ❌ Cloud-dependent |
| **Dual Time Tracking** | ✅ Scheduled + Scenario time | ⚠️ Single timestamp |
| **Practice Mode** | ✅ Training exercises excluded from reports | ❌ Not available |
| **Excel Workflow** | ✅ Import/Export preserves formatting | ⚠️ Basic CSV only |
| **Mid-Market Pricing** | ✅ Affordable for regional agencies | ❌ Enterprise pricing |

## Target Users

Cadence serves five HSEEP-defined personas:

| Role | Primary Responsibilities | Key Needs |
|------|-------------------------|-----------|
| **Administrator** | System configuration, user management | Bulk operations, audit trails |
| **Exercise Director** | Overall exercise oversight | Real-time status, metrics dashboard |
| **Controller** | Inject delivery, player guidance | Quick inject firing, confirmation tracking |
| **Evaluator** | Performance observation, documentation | Observation capture, objective linking |
| **Observer** | Read-only monitoring | Timeline view, no edit access |

## Exercise Types Supported

- Table Top Exercises (TTX)
- Functional Exercises (FE)
- Full-Scale Exercises (FSE)
- Computer-Aided Exercises (CAX)
- Hybrid/Multi-domain exercises

## Documentation Structure

This repository uses a **feature-folder organization** optimized for AI-assisted development:

```
cadence-requirements/
├── README.md                    # This file
├── ROADMAP.md                   # Development phases and timeline
├── DOMAIN_GLOSSARY.md           # HSEEP terminology definitions
├── SME_QUESTIONS.md             # Questions for subject matter experts
├── setup-requirements.ps1       # PowerShell script to recreate structure
│
├── _core/                       # Core domain entities
│   ├── FEATURE.md
│   ├── exercise-entity.md
│   ├── inject-entity.md
│   └── user-roles.md
│
├── _cross-cutting/              # Cross-cutting concerns
│   ├── FEATURE.md
│   ├── S01-session-management.md
│   ├── S02-keyboard-navigation.md
│   ├── S03-auto-save.md
│   └── S04-responsive-design.md
│
├── exercise-crud/               # Exercise lifecycle management
│   ├── FEATURE.md
│   ├── S01-create-exercise.md
│   ├── S02-edit-exercise.md
│   ├── S03-view-exercise-list.md
│   ├── S04-archive-exercise.md
│   └── S05-practice-mode.md
│
├── exercise-config/             # Exercise configuration
│   ├── FEATURE.md
│   ├── S01-configure-roles.md
│   ├── S02-assign-participants.md
│   └── S03-timezone-configuration.md
│
├── exercise-objectives/         # Exercise objectives management
│   ├── FEATURE.md
│   ├── S01-create-objective.md
│   ├── S02-edit-objective.md
│   └── S03-link-objective-inject.md
│
├── exercise-phases/             # Exercise phase management
│   ├── FEATURE.md
│   ├── S01-define-phases.md
│   └── S02-assign-inject-phase.md
│
├── msel-management/             # MSEL version control
│   ├── FEATURE.md
│   ├── S01-select-msel-version.md
│   └── S02-duplicate-msel.md
│
├── inject-crud/                 # Inject lifecycle management
│   ├── FEATURE.md
│   ├── S01-create-inject.md
│   ├── S02-edit-inject.md
│   ├── S03-view-inject-detail.md
│   ├── S04-delete-inject.md
│   └── S05-dual-time-tracking.md
│
├── excel-import/                # Excel import functionality
│   ├── FEATURE.md
│   ├── S01-upload-excel.md
│   ├── S02-map-columns.md
│   └── S03-validate-import.md
│
├── excel-export/                # Excel export functionality
│   ├── FEATURE.md
│   ├── S01-export-msel.md
│   └── S02-export-template.md
│
├── inject-filtering/            # Inject search and filter
│   ├── FEATURE.md
│   ├── S01-filter-injects.md
│   └── S02-search-injects.md
│
├── inject-organization/         # Inject organization features
│   ├── FEATURE.md
│   ├── S01-sort-injects.md
│   ├── S02-group-injects.md
│   └── S03-reorder-injects.md
│
└── progress-dashboard/          # Setup progress tracking
    ├── FEATURE.md
    └── S01-setup-progress.md
```

## Story Numbering Convention

Stories use **feature-relative numbering**: `S##-descriptive-name.md`

- Stories are numbered sequentially within each feature folder (S01, S02, S03...)
- Folder context provides uniqueness: `exercise-crud/S01` ≠ `inject-crud/S01`
- Cross-references use folder/story format: `exercise-crud/S01: Create Exercise`

**Epic references** are preserved in each FEATURE.md file for planning purposes:

| Epic | Name | Features |
|------|------|----------|
| E2 | Infrastructure | _cross-cutting |
| E3 | Exercise Setup | exercise-crud, exercise-config, exercise-objectives, exercise-phases, msel-management, progress-dashboard |
| E4 | MSEL Authoring | inject-crud, excel-import, excel-export, inject-filtering, inject-organization |

## HSEEP Compliance

Cadence aligns with the [Homeland Security Exercise and Evaluation Program (HSEEP)](https://www.fema.gov/emergency-managers/national-preparedness/exercises/hseep) 2020 doctrine:

- **Exercise Types**: Supports operations-based exercises
- **Terminology**: Uses HSEEP-defined terms (inject, MSEL, Controller, etc.)
- **Objectives**: Links injects to exercise objectives
- **Evaluation**: Supports observation capture for After-Action Reports

## Getting Started

### For Product Owners
1. Start with `ROADMAP.md` for development phases
2. Review `DOMAIN_GLOSSARY.md` for terminology
3. Check `SME_QUESTIONS.md` for outstanding decisions

### For Developers
1. Each feature folder contains a `FEATURE.md` overview
2. Individual story files have acceptance criteria in Given/When/Then format
3. Stories follow INVEST principles and are sized for 1-3 day implementation

### For AI Coding Agents
1. Feature folders are self-contained with all context needed
2. Acceptance criteria are unambiguous and testable
3. Domain terms are defined in each story and in `DOMAIN_GLOSSARY.md`

## Related Resources

- **Cadence**: Provides technical foundation
- **EXIS Analysis**: TSA's Exercise Information System informed UX decisions
- **SME Validation**: 40+ years combined emergency management experience

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.1 | 2025-01-08 | Refactored to feature-relative story numbering |
| 1.0 | 2025-01-08 | Initial requirements documentation |

---

*This documentation was created using the Business Analyst Agent methodology for comprehensive, AI-friendly user story development.*
