# Cadence Development Roadmap

> **Three-phase development approach: MVP → Standard → Advanced**

## Phase Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CADENCE ROADMAP                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  MVP (18 features)          Standard (11 features)    Advanced (10 features)│
│  ──────────────────         ─────────────────────     ─────────────────────│
│  • Exercise CRUD            • Grid View Interface     • Auto AAR Generation │
│  • Basic Inject CRUD        • Advanced Filtering      • Multi-location Sync │
│  • Excel Import/Export      • Branching Injects       • Simulation Support  │
│  • Offline Capability       • Exercise Clock          • Channel Delivery    │
│  • Role-based Access        • Auto-fire Injects       • Approval Workflows  │
│  • Dual Time Tracking       • Basic Metrics           • Timeline Visual     │
│                             • Observer Dashboard       • Backchannel Comms   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## MVP Phase (Current Focus)

**Goal**: Deliver a functional MSEL management tool that replaces spreadsheet-based workflows while providing offline capability and dual time tracking.

### Epic 3: Exercise Setup

| Feature | Story | Priority | Status |
|---------|-------|----------|--------|
| exercise-crud/S01 | Create Exercise | P0 | 📋 Ready |
| exercise-crud/S02 | Edit Exercise | P0 | 📋 Ready |
| exercise-crud/S03 | View Exercise List | P0 | 📋 Ready |
| exercise-crud/S04 | Archive Exercise | P1 | 📋 Ready |
| exercise-crud/S05 | Practice Mode | P1 | 📋 Ready |
| exercise-config/S01 | Configure Roles | P1 | 📋 Ready |
| exercise-config/S02 | Assign Participants | P1 | 📋 Ready |
| exercise-config/S03 | Time Zone Configuration | P1 | 📋 Ready |
| exercise-objectives/S01 | Create Objective | P1 | 📋 Ready |
| exercise-objectives/S02 | Edit Objective | P1 | 📋 Ready |
| exercise-objectives/S03 | Link Objective to Inject | P1 | 📋 Ready |
| exercise-phases/S01 | Define Phases | P2 | 📋 Ready |
| exercise-phases/S02 | Assign Inject to Phase | P2 | 📋 Ready |
| msel-management/S01 | Select MSEL Version | P1 | 📋 Ready |
| msel-management/S02 | Duplicate MSEL | P2 | 📋 Ready |
| progress-dashboard/S01 | Setup Progress Dashboard | P2 | 📋 Ready |

### Epic 4: MSEL Authoring

| Feature | Story | Priority | Status |
|---------|-------|----------|--------|
| inject-crud/S01 | Create Inject | P0 | 📋 Ready |
| inject-crud/S02 | Edit Inject | P0 | 📋 Ready |
| inject-crud/S03 | View Inject Detail | P0 | 📋 Ready |
| inject-crud/S04 | Delete Inject | P1 | 📋 Ready |
| inject-crud/S05 | Dual Time Tracking | P0 | 📋 Ready |
| excel-import/S01 | Upload Excel | P0 | 📋 Ready |
| excel-import/S02 | Map Columns | P0 | 📋 Ready |
| excel-import/S03 | Validate Import | P0 | 📋 Ready |
| excel-export/S01 | Export MSEL | P0 | 📋 Ready |
| excel-export/S02 | Export Template | P1 | 📋 Ready |
| inject-filtering/S01 | Filter Injects | P1 | 📋 Ready |
| inject-filtering/S02 | Search Injects | P1 | 📋 Ready |
| inject-organization/S01 | Sort Injects | P1 | 📋 Ready |
| inject-organization/S02 | Group Injects | P2 | 📋 Ready |
| inject-organization/S03 | Reorder Injects | P1 | 📋 Ready |

### Cross-Cutting Concerns

| Feature | Story | Priority | Status |
|---------|-------|----------|--------|
| _cross-cutting/S01 | Session Management | P0 | 📋 Ready |
| _cross-cutting/S02 | Keyboard Navigation | P1 | 📋 Ready |
| _cross-cutting/S03 | Auto-save | P0 | 📋 Ready |
| _cross-cutting/S04 | Responsive Design | P1 | 📋 Ready |

### MVP Exit Criteria

- [ ] All P0 stories complete and tested
- [ ] Offline capability verified (IndexedDB + Service Workers)
- [ ] Excel import/export maintains formatting
- [ ] 5 roles properly enforced (Admin, Director, Controller, Evaluator, Observer)
- [ ] Dual time tracking operational
- [ ] Practice mode exercises excluded from production reports
- [ ] <3 second page load on 4G connection
- [ ] Works on latest Chrome, Firefox, Edge, Safari

---

## Standard Phase (Future)

**Goal**: Add advanced features for larger exercises and improved Controller efficiency.

| Feature | Description | Depends On |
|---------|-------------|------------|
| Grid View | Spreadsheet-like inject editing interface | MVP complete |
| Advanced Filtering | Multi-criteria, saved filters | inject-filtering/S01, S02 |
| Branching Injects | Level 1-2 contingency and adaptive injects | inject-crud/S01 |
| Exercise Clock | Unified start/pause/fast-forward | exercise-config/S03 |
| Auto-fire Injects | Automatic delivery at scheduled time | Exercise Clock |
| Basic Metrics | Inject counts, completion rates | MVP complete |
| Observer Dashboard | Read-only exercise overview | exercise-crud/S03 |
| Bulk Operations | Multi-select inject actions | inject-organization/S01 |
| Bulk Participant Import | CSV/Excel upload for exercise participants | exercise-config/S02, OM-07 |
| Controller Assignment | Assign Controllers to specific injects | exercise-config/S02 |
| Confirmation Tracking | Track player acknowledgment of injects | inject-crud/S01 |
| Quick Actions | Right-click context menus | _cross-cutting/S02 |

### Standard Exit Criteria

- [ ] All Standard features complete and tested
- [ ] Grid view matches Excel familiarity expectations
- [ ] Branching logic supports contingency + adaptive patterns
- [ ] Exercise clock syncs across all connected users
- [ ] Metrics dashboard shows real-time exercise status

---

## Advanced Phase (Future)

**Goal**: Enterprise-grade features for complex, multi-location exercises.

| Feature | Description | Depends On |
|---------|-------------|------------|
| Auto AAR Generation | Generate After-Action Report from observations | Standard complete |
| Multi-location Sync | Real-time sync across distributed exercise sites | Offline capability |
| Simulation Support | Exercise simulation integration | Exercise Clock |
| Channel Delivery | Multi-channel inject distribution (email, SMS, radio) | inject-crud/S01 |
| Approval Workflows | Inject release approval process | Controller Assignment |
| Timeline Visualization | Interactive exercise timeline view | Basic Metrics |
| Backchannel Comms | Exercise control team chat | Session Management |
| Objective Library | Reusable objective templates | exercise-objectives/S01 |
| Exercise Templates | Full exercise templates for common scenarios | msel-management/S02 |
| API Integration | External system integration capabilities | All MVP |

---

## Technical Milestones

### Infrastructure (Parallel Track)

| Milestone | Description | Target |
|-----------|-------------|--------|
| **UAT Environment** | Azure deployment with CI/CD | Week 2 |
| **GitHub → DevOps** | Repository migration | Post-MVP |
| **Offline Architecture** | IndexedDB + Service Workers | MVP |
| **Real-time Sync** | Azure SignalR integration | MVP |
| **Conflict Resolution** | Last-write-wins + merge strategies | MVP |

### Technology Stack

| Layer | Technology | Notes |
|-------|------------|-------|
| **Frontend** | React + TypeScript + Vite | Material UI, responsive |
| **Backend** | .NET 10 + EF Core | Azure App Services |
| **Database** | SQL Server | Azure SQL or local |
| **Offline** | IndexedDB + Service Workers | Dexie.js wrapper |
| **Real-time** | Azure SignalR | Selective sync |
| **Background** | Azure Functions | Time-based tasks only |

---

## Priority Definitions

| Priority | Definition | SLA |
|----------|------------|-----|
| **P0** | Core functionality, blocks MVP | Must complete |
| **P1** | Important for user experience | Should complete |
| **P2** | Nice to have, can defer | May defer |

## Status Legend

| Status | Meaning |
|--------|---------|
| 📋 Ready | Requirements complete, ready for development |
| 🔨 In Progress | Currently being developed |
| 🔍 Review | In code review or QA |
| ✅ Complete | Deployed and verified |
| ⏸️ Blocked | Waiting on dependency or decision |

---

## Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2025-01 | Focus on conduct, not planning | Complement existing tools like EXIS |
| 2025-01 | Offline-first architecture | Critical for field exercises |
| 2025-01 | Excel import/export over grid UI | Familiar workflow, lower complexity |
| 2025-01 | Simple RBAC for MVP | Avoid over-engineering permissions |
| 2025-01 | Dual time tracking required | Scenario time distinct from delivery time |
| 2025-01 | Practice mode for training | Keep test data out of production reports |
| 2025-01 | .NET 10 LTS | Latest long-term support version |

---

## Risk Register

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Offline sync conflicts | High | Medium | Clear conflict resolution rules |
| Excel format complexity | Medium | High | Use EPPlus, extensive testing |
| Performance with large MSELs | Medium | Medium | Pagination, virtual scrolling |
| Scope creep to planning features | High | Medium | Strict scope governance |
| SME availability for validation | Medium | Low | Async question format |

---

*Last updated: 2025-01-08*
