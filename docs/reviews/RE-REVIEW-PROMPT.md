# Code Re-Review Prompt — Pass 2

> **Input:** `docs/reviews/SUMMARY.md` (pass 1 findings), completed hardening fixes on `main`
> **Branch:** `maintenance/code-review-v2` (created from `main` after hardening-v1 merge)
> **Output:** Updated review reports in `docs/reviews/v2/`, resolution tracking, new issue discovery

---

## Context

Code review pass 1 identified 75 issues (15 critical, 35 major, 25 minor) across 5 specialist reviewers. The fix-agent swarm addressed these across 6 phases in `maintenance/code-hardening-v1`, now merged to `main`.

This is pass 2 — a verification and regression review. It is NOT a fresh review from scratch. Reviewers must compare the current codebase against pass 1 findings and evaluate whether fixes were correctly implemented, whether new issues were introduced, and whether deferred items should now be addressed.

---

## Pre-Flight

```
1. Ensure maintenance/code-hardening-v1 has been merged to main
2. Create branch: maintenance/code-review-v2 from main
3. Create directory: docs/reviews/v2/
4. Read docs/reviews/SUMMARY.md to load the pass 1 finding list
5. Read docs/reviews/FIX-AGENT-PROMPT.md to understand what fixes were planned
```

---

## Reviewer Assignments

Pass 2 uses **3 reviewers** instead of 5. The scope is narrower — focused on verification and regression detection rather than full codebase discovery.

### Reviewer 1: Backend Verification Specialist

**Agent type:** `feature-dev:code-reviewer`
**Output file:** `docs/reviews/v2/backend-verification-review.md`
**Issue prefix:** `BV-`

**Scope:** All backend files modified during hardening — `src/Cadence.WebApi/` and `src/Cadence.Core/`.

**Instructions:**

1. Read `docs/reviews/SUMMARY.md` — load all pass 1 backend issues (AC-*, CD-*, AR-C01, AR-M02, AR-M03, AR-M05)
2. Read `docs/reviews/api-controllers-review.md` and `docs/reviews/core-domain-review.md` for full issue descriptions
3. For EACH pass 1 issue assigned to backend:
   - Read the affected file(s) at the locations specified in the original report
   - Determine if the fix was implemented correctly
   - Classify as: **Resolved**, **Partially Resolved**, or **Unresolved**
   - If partially resolved or unresolved, explain what remains
4. Check for **regressions introduced by fixes:**
   - New services created during hardening — are they properly tested, registered in DI, following conventions?
   - `ClaimsPrincipalExtensions` (from AC-M01 fix) — is it used consistently across all controllers?
   - FluentValidation validators (from AR-M02/CD-M08 fix) — are they complete and registered?
   - Extracted services (from AC-C03 fix) — do they follow the Core service pattern? Are they properly org-scoped?
   - Exception handler reorder (from AR-M03 fix) — is the pipeline order now correct?
   - CORS fix (from AC-C05/AR-C01) — verify env-gating is correct
5. Check **deferred items** from pass 1 — should any now be prioritized?
   - AR-M05: InMemory → SQL Server test migration
   - CD-N04: Extract entity configs from DbContext
   - CD-N07: Composite indexes
   - AC-N06: JSON-based body sanitization
6. Look for any **new backend issues** not present in pass 1

**Report format:**

```markdown
# Backend Verification Review

> **Review Type:** Backend Verification
> **Pass:** 2
> **Date:** {YYYY-MM-DD}
> **Reviewer:** Backend Verification Specialist
> **Scope:** Backend files modified during hardening pass 1

---

## Pass 1 Resolution Tracking

| ID | Title | Status | Notes |
|----|-------|--------|-------|
| AC-C01 | Hardcoded SystemUserIdString | {Resolved/Partial/Unresolved} | {Brief explanation} |
| AC-C02 | Missing exercise-scoped auth | {status} | {notes} |
| ... | ... | ... | ... |

### Resolution Summary
- **Resolved:** X of Y backend issues
- **Partially Resolved:** X (details below)
- **Unresolved:** X (details below)

## Partially Resolved Issues
### {ID}: {Title}
- **What was fixed:** {description}
- **What remains:** {description}
- **Recommendation:** {specific next step}

## Unresolved Issues
### {ID}: {Title}
- **Expected fix:** {what FIX-AGENT-PROMPT specified}
- **Current state:** {what the code actually looks like}
- **Recommendation:** {specific next step}

## New Issues (Introduced by Fixes)
### BV-C01: {Title}
- **File(s):** `path/to/file.cs:line`
- **Category:** {category}
- **Introduced by:** {which pass 1 fix caused this}
- **Impact:** {description}
- **Recommendation:** {fix}

## New Issues (Previously Undetected)
### BV-M01: {Title}
(standard issue format)

## Deferred Item Re-evaluation
| ID | Title | Recommendation | Rationale |
|----|-------|---------------|-----------|
| AR-M05 | InMemory test migration | {Defer again / Prioritize now} | {why} |
| ... | ... | ... | ... |
```

---

### Reviewer 2: Frontend Verification Specialist

**Agent type:** `feature-dev:code-reviewer`
**Output file:** `docs/reviews/v2/frontend-verification-review.md`
**Issue prefix:** `FV-`

**Scope:** All frontend files modified during hardening — `src/frontend/src/`.

**Instructions:**

1. Read `docs/reviews/SUMMARY.md` — load all pass 1 frontend issues (FI-*, FF-*, AR-M01, AR-M04, AR-N01)
2. Read `docs/reviews/frontend-infrastructure-review.md` and `docs/reviews/frontend-features-review.md` for full issue descriptions
3. For EACH pass 1 issue assigned to frontend:
   - Read the affected file(s)
   - Classify as: **Resolved**, **Partially Resolved**, or **Unresolved**
4. Check for **regressions introduced by fixes:**
   - `devLog` utility (from AR-M04/FI-M01 fix) — is it used everywhere? Any `console.log` still in production paths?
   - COBRA compliance fixes — were ALL raw MUI imports replaced? Any new ones introduced?
   - `ConnectionState` type extraction (from FI-M03) — is it imported correctly in both hooks?
   - `isNetworkError` utility (from FI-M04) — is it used in both `AuthContext` and `api.ts`?
   - Query key fixes (from FF-M01, FF-M02) — are they correct now?
   - `useUsers` hook (from FF-C01) — does it follow the established React Query pattern?
5. Run a fresh scan for COBRA violations:
   - `grep` for raw MUI component imports (`from '@mui/material'` that aren't in theme files)
   - `grep` for MUI icon imports (`from '@mui/icons-material'`)
   - `grep` for direct toast imports (`from 'react-toastify'`)
   - `grep` for remaining `console.log` in non-test files
   - `grep` for hardcoded hex color strings in component files
6. Check **deferred items** — should any frontend deferred items be prioritized?
7. Look for any **new frontend issues**

**Report format:** Same structure as Reviewer 1, with `FV-` prefix for new issues.

---

### Reviewer 3: Cross-Cutting Verification Specialist

**Agent type:** `feature-dev:code-reviewer`
**Output file:** `docs/reviews/v2/cross-cutting-verification-review.md`
**Issue prefix:** `XV-`

**Scope:** Architecture-level patterns, test coverage, cross-layer consistency.

**Instructions:**

1. Read `docs/reviews/architecture-review.md` for pass 1 architecture findings
2. Read `docs/reviews/SUMMARY.md` — focus on the "Positive Patterns to Preserve" section
3. **Verify positive patterns were NOT regressed:**
   - Core/WebApi separation — did any business logic leak back into controllers?
   - React Query cache invalidation via SignalR — still using invalidation, not direct mutation?
   - Organization validation interceptor — still intact, not bypassed?
   - Feature module structure — do new files follow the established layout?
   - Static pure-function mappers — any new mappers with side effects?
   - Secure token strategy — unchanged?
   - `notify` wrapper — no new direct `toast` imports?
   - FontAwesome icons — no new MUI icon imports?
   - COBRA styled components — no new raw MUI imports?
   - `IgnoreQueryFilters()` comments — any new uncommented usages?
4. **Test coverage audit:**
   - How many new tests were added during hardening? (count test files/methods added)
   - Do the new tests follow the project's naming conventions?
   - Are there any fixes that shipped WITHOUT corresponding tests? (compare FIX-AGENT-PROMPT test requirements against actual test files)
   - Run `dotnet test --list-tests` and `npm run test -- --run` to get current counts
5. **Cross-layer consistency:**
   - Any new frontend types that don't match backend DTOs?
   - Any new API routes that don't match frontend service URLs?
   - Any new SignalR events that aren't subscribed to on the frontend?
6. **Build and test verification:**
   - `dotnet build src/Cadence.WebApi` — clean?
   - `dotnet test src/Cadence.Core.Tests` — all pass?
   - `cd src/frontend && npm run type-check` — clean?
   - `cd src/frontend && npm run test -- --run` — all pass?

**Report format:** Same structure as other reviewers, with `XV-` prefix. Include a dedicated "Positive Pattern Verification" section with pass/fail for each pattern.

---

## Orchestration Instructions

1. Create branch `maintenance/code-review-v2` from `main`
2. Create directory `docs/reviews/v2/`
3. Launch all 3 reviewers in parallel
4. Each reviewer reads pass 1 reports FIRST, then verifies against current code
5. Each reviewer writes findings to their assigned output file
6. After all 3 complete, create `docs/reviews/v2/SUMMARY.md` (format below)
7. Commit with message: `docs(reviews): add V2 code review verification (pass 2)`
8. Merge to main

---

## Summary Format

```markdown
# Code Review Summary — Pass 2 (Verification)

> **Date:** {YYYY-MM-DD}
> **Branch:** maintenance/code-review-v2
> **Reviewers:** 3 (Backend Verification, Frontend Verification, Cross-Cutting Verification)

---

## Pass 1 Resolution Status

| Category | Resolved | Partially Resolved | Unresolved | Total from Pass 1 |
|----------|----------|--------------------|------------|-------------------|
| Critical | X | X | X | 15 |
| Major | X | X | X | 35 |
| Minor | X | X | X | 25 |
| **Total** | **X** | **X** | **X** | **75** |

## Regressions Introduced by Fixes

| ID | Title | Severity | Introduced By | Source Report |
|----|-------|----------|--------------|--------------|
| {BV-C01} | {title} | {Critical/Major/Minor} | {pass 1 fix ID} | backend-verification-review.md |
| ... | ... | ... | ... | ... |

## New Issues (Previously Undetected)

| ID | Title | Severity | Source Report |
|----|-------|----------|--------------|
| {FV-M01} | {title} | {severity} | frontend-verification-review.md |
| ... | ... | ... | ... |

## Positive Pattern Verification

| Pattern | Status | Notes |
|---------|--------|-------|
| Core/WebApi separation | {Pass/Fail} | {notes} |
| React Query invalidation via SignalR | {Pass/Fail} | {notes} |
| ... | ... | ... |

## Test Coverage Delta

| Metric | Before Hardening | After Hardening | Delta |
|--------|-----------------|-----------------|-------|
| Backend test count | X | X | +X |
| Frontend test count | X | X | +X |
| Fixes with required tests | X | X | X missing |

## Deferred Items Re-evaluation

| ID | Title | Pass 1 Decision | Pass 2 Recommendation | Rationale |
|----|-------|-----------------|----------------------|-----------|
| AR-M05 | InMemory test migration | Deferred | {Defer/Prioritize} | {reason} |
| ... | ... | ... | ... | ... |

## Recommended Next Steps

### If Unresolved/Regressed Issues Remain:
1. Create `maintenance/code-hardening-v2` from `main`
2. Fix unresolved pass 1 issues and regressions
3. Run pass 3 review (likely only 1-2 reviewers needed)

### If All Issues Resolved:
1. Codebase is production-hardened
2. Address deferred items in future maintenance cycles
3. Archive review docs for reference
```

---

## Decision Tree: Is Another Pass Needed?

```
After pass 2 results:
├── All pass 1 issues resolved AND no regressions → DONE. Codebase hardened.
├── <5 unresolved/regressed issues → Fix directly (no full swarm needed), then DONE.
├── 5-15 unresolved/regressed issues → Run hardening-v2 + review-v3 (2 reviewers).
└── >15 unresolved/regressed issues → Something went wrong. Audit the fix process.
```
