# Cadence Feature Story Gap Analysis

**Generated**: January 21, 2026
**Updated**: January 21, 2026 (PO Decisions Incorporated)
**Analyst**: Business Analyst Agent
**Repository Branch**: feature/authentication

---

## Executive Summary

The Cadence codebase has **comprehensive feature story coverage** for most roadmap items, with several areas exceeding roadmap scope through thoughtful additions (exercise-lifecycle, review-mode, connectivity). **Authentication & Authorization (24 stories) is fully drafted and ready for implementation** - this is the primary production blocker.

**Key Findings:**
1. Authentication epic is complete with 24 stories including hybrid auth (MVP local + future Entra) and self-service password reset via ACS
2. Exercise Observations feature created with 8 stories for HSEEP-compliant evaluation capture
3. Exercise Status clarified: status and clock are **independent** (per PO decision)
4. PWA/offline capabilities confirmed as early MVP status - no additional stories needed
5. All cross-cutting concerns have story coverage

**Recommended Focus**: Implement Authentication (S01-S17, S24 for MVP), then proceed with Exercise Observations, Exercise Status, and Exercise Lifecycle workflows.

---

## PO Decisions (January 21, 2026)

| Question | Decision | Impact |
|----------|----------|--------|
| Password Reset | Self-service via email using Azure Communication Services (ACS) | Created S24-password-reset.md |
| Entra SSO Priority | Post-MVP (P2) | S18-S23 remain deferred |
| Status vs Clock Pause | **Separate/Independent** - can pause exercise while clock runs, or pause clock while exercise stays active | Updated exercise-status/FEATURE.md with detailed independence matrix |
| Observation Stories | New `exercise-observations/` folder needed | Created 8 stories (S01-S08) |
| PWA/Offline | Complete (early MVP status, not rigorously tested) | No additional stories needed |

---

## Feature Inventory

### Complete Features ✅

| Feature | Stories | Notes |
|---------|---------|-------|
| **authentication** | S01-S24 (24 stories) | Full epic including hybrid auth, Entra, account linking, password reset |
| **exercise-observations** | S01-S08 (8 stories) | NEW: HSEEP-compliant observation capture for evaluators |
| **exercise-crud** | S01-S05 (5 stories) | Complete per roadmap |
| **inject-crud** | S01-S05 (5 stories) | Complete per roadmap |
| **excel-import** | S01-S04 (4 stories) | Complete + S04 delivery method synonyms |
| **excel-export** | S01-S02 (2 stories) | Complete per roadmap |
| **exercise-objectives** | S01-S03 (3 stories) | Complete per roadmap |
| **exercise-phases** | S01-S02 (2 stories) | Complete per roadmap |
| **msel-management** | S01-S02 (2 stories) | Complete per roadmap |
| **progress-dashboard** | S01 (1 story) | Complete per roadmap |
| **inject-filtering** | S01-S02 (2 stories) | Complete per roadmap |
| **inject-organization** | S01-S03 (3 stories) | Complete per roadmap |
| **_cross-cutting** | S01-S04 (4 stories) | Complete per roadmap |
| **_core** | exercise-entity, inject-entity, user-roles | Reference docs complete |
| **homepage** | S01-S02 (2 stories) | Complete ✅ Implemented |
| **connectivity** | S01-S06 (6 stories) | Complete ✅ All stories marked complete |

### Features Exceeding Roadmap Scope ⬆️

| Feature | Roadmap Stories | Actual Stories | Additions |
|---------|-----------------|----------------|-----------|
| **exercise-config** | S01-S03 (3 stories) | S01-S10 + CLK-01 to CLK-10 (13+ stories) | Clock modes, timing config, fire confirmation |
| **exercise-status** | (not in roadmap) | S01-S06 (6 stories) | Full status workflow feature added |
| **exercise-lifecycle** | (not in roadmap) | S01-S07 (7 stories) | Archive/restore/delete management |
| **review-mode** | (not in roadmap) | E6-S20 to E6-S25 (6 stories) | Post-conduct AAR support |

### Features Not in Roadmap (Additions) 🆕

These features were added beyond the original roadmap to address gaps discovered during development:

| Feature | Stories | Purpose |
|---------|---------|---------|
| **exercise-status** | 6 stories | Status workflow (Draft → Active → Paused → Completed → Archived) |
| **exercise-lifecycle** | 7 stories | Archive/restore/delete with proper permissions |
| **review-mode** | 6 stories | Post-conduct AAR review functionality |
| **connectivity** | 6 stories | Real-time sync and offline capability |
| **authentication** | 24 stories | Full auth epic (was identified as production blocker) |
| **exercise-observations** | 8 stories | HSEEP evaluation capture with P/S/M/U ratings |

---

## Critical Path Analysis

### 1. Authentication & Authorization [PRODUCTION BLOCKER]

**Status**: ✅ **COMPLETE** - 24 stories fully drafted
**Priority**: P0 - Blocks all user-facing features

| Story Group | Stories | Status |
|-------------|---------|--------|
| User Registration | S01-S03 | 📋 Ready |
| User Login | S04-S06 | 📋 Ready |
| Token Management | S07-S09 | 📋 Ready |
| User Management | S10-S12 | 📋 Ready |
| Role Assignment | S13-S15 | 📋 Ready |
| Auth Service Interface | S16 | 📋 Ready |
| Identity Provider | S17 | 📋 Ready |
| Entra/External Auth (P2) | S18-S23 | 📋 Ready |
| **Password Reset (MVP)** | **S24** | 📋 **Ready** |

**Key Requirements Verified:**
- ✅ First user becomes Administrator automatically (S03)
- ✅ JWT strategy: 15-min access, 4-hr refresh, memory storage (S05, S07)
- ✅ Exercise-scoped role assignments (S14, S15)
- ✅ Hybrid authentication architecture (S16)
- ✅ Clean `IAuthenticationService` orchestrator pattern (S16)
- ✅ User account linking by email (S19)
- ✅ Complete OAuth 2.0 flow for Entra (S18, S20, S21)
- ✅ Domain restrictions and error handling (S22, S23)
- ✅ **Self-service password reset via Azure Communication Services (S24)**

**Recommendation**: Implement S01-S17 + S24 for MVP (local auth + password reset), defer S18-S23 (Entra) to post-MVP.

---

### 2. Exercise Observations [NEW]

**Status**: ✅ **COMPLETE** - 8 stories fully drafted
**Priority**: P0/P1 - Required for HSEEP evaluation
**Location**: `docs/features/exercise-observations/`

| Story | Title | Priority |
|-------|-------|----------|
| S01 | Create Observation | P0 |
| S02 | Edit Observation | P0 |
| S03 | Delete Observation | P1 |
| S04 | Link Observation to Inject | P1 |
| S05 | Link Observation to Objective | P1 |
| S06 | Apply P/S/M/U Rating | P1 |
| S07 | View Observations List | P0 |
| S08 | Filter Observations | P2 |

**Key Features:**
- HSEEP P/S/M/U performance rating scale
- Observation types: Strength, Area for Improvement, Neutral
- Link to injects and objectives for AAR context
- Role-based permissions (Evaluator+)

**Recommendation**: Implement after authentication and exercise-status. Critical for HSEEP compliance.

---

### 3. Exercise Status Workflow

**Status**: ✅ **COMPLETE** - 6 stories fully drafted
**Priority**: P0/P1 - Required for exercise conduct
**Location**: `docs/features/exercise-status/`

| Story | Title | Priority |
|-------|-------|----------|
| S01 | View Exercise Status | P0 |
| S02 | Activate Exercise (Draft → Active) | P0 |
| S03 | Pause Exercise (Active → Paused) | P1 |
| S04 | Complete Exercise | P0 |
| S05 | Revert to Draft | P1 |
| S06 | Archive Exercise | P0 |

**Key Design Decision (PO Confirmed):** Exercise Status and Clock State are **INDEPENDENT**:
- Pause Exercise ≠ Pause Clock (they can be controlled separately)
- Allows scenarios like: exercise paused for safety while clock continues, or clock paused for discussion while exercise stays active

**Recommendation**: Implement after authentication. Depends on auth for permission enforcement.

---

### 4. Exercise Lifecycle Management

**Status**: ✅ **COMPLETE** - 7 stories fully drafted
**Priority**: P0/P1 - Required for exercise management
**Location**: `docs/features/exercise-lifecycle/`

| Story | Title | Priority |
|-------|-------|----------|
| S01 | Lifecycle Tracking Fields | P0 |
| S02 | Archive Exercise | P0 |
| S03 | View Archived Exercises | P0 |
| S04 | Restore Exercise | P0 |
| S05 | Delete Draft Exercise | P0 |
| S06 | Delete Archived Exercise | P1 |
| S07 | Admin Archive Management Page | P1 |

**Recommendation**: Implement alongside or after exercise-status.

---

### 5. Exercise Configuration (Clock Modes)

**Status**: ✅ **COMPLETE** - 13+ stories fully drafted
**Priority**: P1 - Required for exercise conduct timing
**Location**: `docs/features/exercise-config/`

Expanded from original 3 roadmap stories to comprehensive clock mode support:
- S01-S03: Original config stories
- CLK-01 to CLK-10: Clock mode stories (timing configuration, clock-driven vs facilitator-paced)

**Implementation Phases Documented:**
1. Foundation (CLK-01, CLK-02, CLK-04)
2. Configuration UI (CLK-03)
3. Clock-Driven (CLK-05, CLK-06, CLK-08)
4. Facilitator Mode (CLK-07)
5. Polish (CLK-09, CLK-10)

---

### 6. Cross-Cutting Concerns

**Status**: ✅ **COMPLETE** - 4 stories
**Location**: `docs/features/_cross-cutting/`

| Story | Title | Priority |
|-------|-------|----------|
| S01 | Session Management | P0 |
| S02 | Keyboard Navigation | P1 |
| S03 | Auto-save | P0 |
| S04 | Responsive Design | P1 |

**Note**: Session Management (S01) is now handled by Authentication epic's token management.

---

### 7. Review Mode (Post-Conduct AAR)

**Status**: ✅ **COMPLETE** - 6 stories fully drafted
**Priority**: P1/P2 - Post-conduct analysis
**Location**: `docs/features/review-mode/`

| Story | Title | Priority |
|-------|-------|----------|
| E6-S20 | Access Review Mode | P1 |
| E6-S21 | Phase-Grouped Timeline | P1 |
| E6-S22 | Inject Outcome Summary | P1 |
| E6-S23 | Observation Review Panel | P2 |
| E6-S24 | Exercise Statistics Dashboard | P2 |
| E6-S25 | Export Review Data | P2 |

**Recommendation**: Implement after core conduct features are complete.

---

### 8. Connectivity (Real-Time & Offline)

**Status**: ✅ **COMPLETE & IMPLEMENTED** - 6 stories
**Location**: `docs/features/connectivity/`

All stories marked as **Complete**:
- S01: Real-Time Data Sync ✅
- S02: Offline Detection & Indicators ✅
- S03: Local Data Cache ✅
- S04: Offline Action Queue ✅
- S05: Sync on Reconnect ✅
- S06: Conflict Resolution ✅

---

## Roadmap vs Implementation Matrix

### MVP Phase Features

| Roadmap Item | Feature Folder | Stories Exist | Implemented |
|--------------|----------------|---------------|-------------|
| Exercise CRUD | exercise-crud/ | ✅ S01-S05 | ✅ Yes |
| Inject CRUD | inject-crud/ | ✅ S01-S05 | ✅ Yes |
| Excel Import | excel-import/ | ✅ S01-S04 | ✅ Yes |
| Excel Export | excel-export/ | ✅ S01-S02 | ✅ Yes |
| Configure Roles | exercise-config/ | ✅ S01 | ⏳ Partial |
| Assign Participants | exercise-config/ | ✅ S02 | ⏳ Partial |
| Time Zone Config | exercise-config/ | ✅ S03 | ⏳ Partial |
| Create Objective | exercise-objectives/ | ✅ S01 | 🔲 Ready |
| Edit Objective | exercise-objectives/ | ✅ S02 | 🔲 Ready |
| Link Objective-Inject | exercise-objectives/ | ✅ S03 | 🔲 Ready |
| Define Phases | exercise-phases/ | ✅ S01 | 🔲 Ready |
| Assign Inject to Phase | exercise-phases/ | ✅ S02 | 🔲 Ready |
| Select MSEL Version | msel-management/ | ✅ S01 | 🔲 Ready |
| Duplicate MSEL | msel-management/ | ✅ S02 | 🔲 Ready |
| Setup Progress | progress-dashboard/ | ✅ S01 | 🔲 Ready |
| Filter Injects | inject-filtering/ | ✅ S01 | 🔲 Ready |
| Search Injects | inject-filtering/ | ✅ S02 | 🔲 Ready |
| Sort Injects | inject-organization/ | ✅ S01 | 🔲 Ready |
| Group Injects | inject-organization/ | ✅ S02 | 🔲 Ready |
| Reorder Injects | inject-organization/ | ✅ S03 | 🔲 Ready |
| Session Management | _cross-cutting/ | ✅ S01 | 🔲 Ready |
| Keyboard Navigation | _cross-cutting/ | ✅ S02 | 🔲 Ready |
| Auto-save | _cross-cutting/ | ✅ S03 | 🔲 Ready |
| Responsive Design | _cross-cutting/ | ✅ S04 | 🔲 Ready |

### Features Added Beyond Roadmap

| Feature | Purpose | Stories |
|---------|---------|---------|
| authentication | Production blocker - user access control | 23 stories |
| exercise-status | Status workflow for conduct lifecycle | 6 stories |
| exercise-lifecycle | Archive/restore/delete management | 7 stories |
| review-mode | Post-conduct AAR analysis | 6 stories |
| connectivity | Real-time sync + offline capability | 6 stories |
| homepage | Landing page with role-aware content | 2 stories |

---

## Recommended Implementation Order

| Order | Feature | Stories | Rationale | Effort Est. |
|-------|---------|---------|-----------|-------------|
| **1** | **Authentication (MVP)** | S01-S17, S24 | Production blocker. No user can access system without auth. | Large (18 stories) |
| **2** | **Exercise Status** | S01-S06 | Required for exercise conduct. Depends on auth roles. | Medium (6 stories) |
| **3** | **Exercise Observations** | S01-S08 | HSEEP compliance. Evaluator workflow for AAR. | Medium (8 stories) |
| **4** | **Exercise Lifecycle** | S01-S07 | Complements status workflow. Archive/delete capabilities. | Medium (7 stories) |
| **5** | **Exercise Config (Clock)** | CLK-01 to CLK-10 | Required for conduct timing. Foundation for facilitator mode. | Large (10 stories) |
| **6** | **Exercise Objectives** | S01-S03 | HSEEP compliance. Links to injects for evaluation. | Small (3 stories) |
| **7** | **Exercise Phases** | S01-S02 | Organizational structure for large exercises. | Small (2 stories) |
| **8** | **Review Mode** | E6-S20 to E6-S25 | Post-conduct AAR support. Can be deferred. | Medium (6 stories) |

### MVP Critical Path (Minimum Viable)

```
Authentication (S01-S17, S24)  →  Exercise Status (S01-S06)  →  Exercise Observations (S01-S08)
         ↓                               ↓                              ↓
     User access                  Status workflow              HSEEP evaluation capture
```

---

## Stories Needing Creation

### High Priority (P0) - None Identified

All P0 features have comprehensive story coverage.

### Medium Priority (P1) - Minor Enhancements

- [ ] `inject-crud/S06-fire-inject.md` - Formalize the inject firing workflow (currently implied in exercise-config CLK stories)

### Low Priority (P2) - Future Considerations

- [ ] `authentication/S25-mfa-support.md` - Multi-factor authentication
- [ ] `exercise-config/S11-timezone-display.md` - Timezone handling for distributed teams

---

## Open Questions for Product Owner

### Resolved Questions ✅

| Question | Decision | Date |
|----------|----------|------|
| Password Reset | Self-service via ACS email | 2026-01-21 |
| Entra SSO Priority | Post-MVP (P2) | 2026-01-21 |
| Status vs Clock Pause | Independent/Separate | 2026-01-21 |
| Observation Stories | Created exercise-observations/ | 2026-01-21 |
| PWA/Offline | Complete (early MVP) | 2026-01-21 |

### Remaining Questions

1. **Inject Fire Story**: Should inject firing have its own dedicated story in inject-crud, or is it adequately covered in exercise-config CLK stories?
   - Current: Covered in CLK stories but not explicit inject-crud story
   - Consideration: May improve discoverability with explicit S06-fire-inject.md

2. **Observation Offline Sync**: How should observations sync when created offline? Same as injects (queue and sync)?
   - Assumption: Yes, use same connectivity patterns
   - Consideration: May need explicit offline story in exercise-observations/

---

## Appendix A: Feature Folder Inventory

| Folder | Type | Stories | Status |
|--------|------|---------|--------|
| `_core/` | Reference | 3 docs | Complete |
| `_cross-cutting/` | Cross-cutting | S01-S04 | Complete |
| `authentication/` | Epic | S01-S24 | Complete |
| `exercise-observations/` | Feature | S01-S08 | Complete (NEW) |
| `connectivity/` | Feature | S01-S06 | Complete & Implemented |
| `excel-export/` | Feature | S01-S02 | Complete |
| `excel-import/` | Feature | S01-S04 | Complete |
| `exercise-config/` | Feature | S01-S10, CLK-01-CLK-10 | Complete |
| `exercise-crud/` | Feature | S01-S05 | Complete & Implemented |
| `exercise-lifecycle/` | Feature | S01-S07 | Complete |
| `exercise-objectives/` | Feature | S01-S03 | Complete |
| `exercise-phases/` | Feature | S01-S02 | Complete |
| `exercise-status/` | Feature | S01-S06 | Complete |
| `homepage/` | Feature | S01-S02 | Complete & Implemented |
| `inject-crud/` | Feature | S01-S05 | Complete & Implemented |
| `inject-filtering/` | Feature | S01-S02 | Complete |
| `inject-organization/` | Feature | S01-S03 | Complete |
| `msel-management/` | Feature | S01-S02 | Complete |
| `progress-dashboard/` | Feature | S01 | Complete |
| `review-mode/` | Feature | E6-S20-S25 | Complete |

---

## Appendix B: Authentication Epic Story Summary

| ID | Title | Priority | Key Functionality |
|----|-------|----------|-------------------|
| S01 | Registration Form | P0 | Email/password registration UI |
| S02 | Validate and Save User | P0 | Backend registration validation |
| S03 | First User Becomes Admin | P0 | Bootstrap without config |
| S04 | Login Form | P0 | Hybrid login UI (local + SSO) |
| S05 | JWT Token Issuance | P0 | 15-min access, 4-hr refresh |
| S06 | Failed Login Handling | P0 | Lockout after 5 attempts |
| S07 | Automatic Token Refresh | P0 | Silent refresh before expiry |
| S08 | Token Expiration Handling | P0 | Redirect to login on expiry |
| S09 | Secure Logout | P0 | Token revocation |
| S10 | View User List | P0 | Admin user management |
| S11 | Edit User Details | P0 | Update user profile |
| S12 | Deactivate User | P1 | Soft deactivation |
| S13 | Global Role Assignment | P0 | System-wide role |
| S14 | Exercise Role Assignment | P0 | Per-exercise roles |
| S15 | Role Inheritance | P0 | Global → exercise override |
| S16 | Auth Service Interface | P1 | Orchestrator pattern |
| S17 | Identity Provider | P0 | ASP.NET Core Identity impl |
| S18 | Entra Provider | P2 | Azure AD integration |
| S19 | User Account Linking | P2 | Link external to local |
| S20 | Initiate External Login | P2 | OAuth redirect |
| S21 | OAuth Callback Handling | P2 | Token exchange |
| S22 | Entra Admin Configuration | P2 | Tenant/client setup |
| S23 | External Auth Error Handling | P2 | SSO error scenarios |
| **S24** | **Password Reset** | **P0** | **Self-service via ACS email** |

---

## Appendix C: Exercise Observations Story Summary

| ID | Title | Priority | Key Functionality |
|----|-------|----------|-------------------|
| S01 | Create Observation | P0 | Quick entry during conduct |
| S02 | Edit Observation | P0 | Update content, type, rating, links |
| S03 | Delete Observation | P1 | Director/Admin can remove |
| S04 | Link to Inject | P1 | Associate observation with inject(s) |
| S05 | Link to Objective | P1 | Associate observation with objective(s) |
| S06 | P/S/M/U Rating | P1 | HSEEP performance rating scale |
| S07 | View Observations List | P0 | Chronological list with metadata |
| S08 | Filter Observations | P2 | By type, rating, objective, author |

---

## Conclusion

The Cadence feature story coverage is **comprehensive and well-organized**. All PO questions have been resolved and documentation updated accordingly.

**Summary of Changes (2026-01-21):**
- ✅ Created `authentication/S24-password-reset.md` - Self-service password reset via ACS
- ✅ Created `exercise-observations/` feature folder with 8 stories (S01-S08)
- ✅ Updated `exercise-status/FEATURE.md` - Clarified status vs clock independence
- ✅ Updated this gap analysis report with all PO decisions

**Next Steps:**
1. Begin implementation of Authentication stories S01-S17 + S24
2. Implement Exercise Status workflow (S01-S06)
3. Implement Exercise Observations (S01-S08) for HSEEP compliance
4. Proceed with Exercise Lifecycle and Clock Mode features

---

*Report generated by Business Analyst Agent*
*Last updated: 2026-01-21 (PO decisions incorporated)*
