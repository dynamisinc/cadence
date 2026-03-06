# Code Review Swarm Prompt

> Copy everything below the line into a new Claude Code session to launch the review.

---

## Prompt

You are an orchestrator directing a comprehensive code review of the Cadence codebase. Cadence is a HSEEP-compliant MSEL management platform built with .NET 10 / EF Core 10 backend and React 19 / TypeScript 5 / MUI 7 frontend. The codebase is at V1 (UAT phase) and needs to be hardened from MVP quality to production-grade.

This is **Pass 1** of an iterative review-fix cycle. The output of this pass will be consumed by a swarm of fix agents in the next session. Expect multiple iterations: Review → Fix → Re-review → Fix → ...

### Context Foundation

Before dispatching reviewers, each agent MUST read the relevant architecture docs as their map of the codebase. These were just created and accurately reflect the V1 state:

- `docs/architecture/OVERVIEW.md` — System architecture, deployment, middleware pipeline
- `docs/architecture/BACKEND_ARCHITECTURE.md` — 26 backend feature modules, DI, data access patterns
- `docs/architecture/FRONTEND_ARCHITECTURE.md` — 25 frontend feature modules, routing, state management
- `docs/architecture/FEATURE_INVENTORY.md` — Cross-reference of all features with file locations
- `docs/architecture/DATA_MODEL.md` — 45+ entities, relationships, enum reference
- `docs/architecture/API_DESIGN.md` — 36 controllers, all endpoints
- `docs/architecture/ROLE_ARCHITECTURE.md` — Three-tier role hierarchy
- `docs/architecture/SIGNALR_EVENTS.md` — Real-time event catalog

Also read `CLAUDE.md` for project conventions (COBRA styling, FontAwesome icons, notify wrapper, naming conventions).

### Review Objectives

Every reviewer focuses on the same quality goals, applied to their domain:

1. **DRY violations** — Duplicated logic, copy-pasted patterns, repeated boilerplate that should be extracted into shared utilities, base classes, or hooks
2. **File size** — Files exceeding ~500 lines are candidates for decomposition. Not an absolute rule — a 600-line file with clear sections is fine, but a 400-line file with mixed concerns is not
3. **Orphaned code** — Unused exports, dead imports, unreachable branches, commented-out code, TODO/FIXME/HACK comments that need resolution, unused variables or parameters
4. **Readability** — Unclear naming, deeply nested logic (>3 levels), missing or misleading comments, inconsistent patterns within a feature, magic numbers/strings
5. **Stability risks** — Missing error handling, unhandled promise rejections, potential null reference paths, race conditions, missing validation, silent failures
6. **Convention violations** — Deviations from patterns established in CLAUDE.md and CODING_STANDARDS.md (raw MUI imports, MUI icons, direct toast imports, wrong naming conventions)

### Output Format

Each reviewer MUST produce a single markdown file saved to `docs/reviews/` with the structure below. The header metadata is critical — it identifies the review type, pass number, and timestamp so that fix agents and re-review passes can reference it precisely.

```markdown
# {Review Type} Code Review

> **Review Type:** {Frontend Infrastructure | Frontend Features | API & Controllers | Core Domain & Data | Architecture}
> **Pass:** 1
> **Date:** {YYYY-MM-DD}
> **Reviewer:** {Specialist role description}
> **Scope:** {Brief description of directories/files covered}

---

## Reviewer Summary

- **Files reviewed:** X
- **Issues found:** X total — Critical: X, Major: X, Minor: X
- **Positive patterns identified:** X
- **Estimated remediation effort:** {Low | Medium | High}
- **Top 3 priorities for this domain:**
  1. {Most impactful issue or pattern — one sentence}
  2. {Second most impactful}
  3. {Third most impactful}

---

## Critical Issues

> Issues that affect correctness, security, or data integrity. Fix before production.

### C01: {Short title}
- **File(s):** `path/to/file.ts:123` (and X other files)
- **Category:** {DRY | FileSize | Orphaned | Readability | Stability | Convention}
- **Impact:** {What breaks or degrades if this is not fixed}
- **Description:** {What's wrong and why it matters}
- **Recommendation:** {Specific fix — name the extraction, the refactor, the deletion}
- **Affected files:** {List all files if pattern issue, or "see file above" if single file}

## Major Issues

> Issues that affect maintainability, readability, or developer experience. Fix during hardening.

### M01: {Short title}
- **File(s):** `path/to/file.ts:123`
- **Category:** {DRY | FileSize | Orphaned | Readability | Stability | Convention}
- **Impact:** {Why this matters for maintainability}
- **Description:** {What's wrong}
- **Recommendation:** {Specific fix}
- **Affected files:** {List if pattern issue}

## Minor Issues

> Style nits, small improvements, nice-to-haves. Batch these during a cleanup pass.

### N01: {Short title}
- **File(s):** `path/to/file.ts:123`
- **Category:** {DRY | FileSize | Orphaned | Readability | Stability | Convention}
- **Description:** {Brief}
- **Recommendation:** {Brief}

## Positive Patterns

> Things done well that should be preserved, documented, or replicated elsewhere.

### P01: {Short title}
- **Where:** `path/to/example.ts`
- **What:** {Describe the pattern and why it's good}
- **Replicate in:** {Other areas that would benefit from this pattern}
```

### Issue ID Convention

Issue IDs are namespaced by reviewer so fix agents can reference them unambiguously:

| Reviewer | Prefix | Example |
|----------|--------|---------|
| Frontend Infrastructure | `FI-` | `FI-C01`, `FI-M03` |
| Frontend Features | `FF-` | `FF-C01`, `FF-M05` |
| API & Controllers | `AC-` | `AC-C01`, `AC-M02` |
| Core Domain & Data | `CD-` | `CD-C01`, `CD-M04` |
| Architecture | `AR-` | `AR-C01`, `AR-M01` |

Use these prefixes in the issue IDs within each report (e.g., `### FI-C01: Missing error boundary in provider stack`).

---

### Reviewer Assignments

Launch these 5 reviewers in parallel. Each reviewer is a specialist who reads code deeply — not just scanning for patterns, but understanding intent and evaluating design decisions.

---

#### Reviewer 1: Frontend Infrastructure Specialist

**Agent type:** `feature-dev:code-reviewer`
**Expertise:** React 19, TypeScript 5, MUI 7, React Query, custom hooks, component patterns
**Output file:** `docs/reviews/frontend-infrastructure-review.md`
**Issue prefix:** `FI-`

**Scope:** All files under `src/frontend/src/` EXCEPT `features/` (those are covered by Reviewer 2).
Focus on:
- `contexts/` — Provider implementations, context value stability, unnecessary re-renders
- `shared/components/` — Component reusability, prop interfaces, missing memoization
- `shared/hooks/` — Hook composition, dependency arrays, cleanup functions
- `core/` — API client patterns, error interceptors, offline sync logic
- `theme/` — COBRA component completeness, styled component patterns
- `App.tsx` and routing — Guard components, lazy loading, route organization
- `types/` — Type definitions, any-casts, missing discriminated unions

Read `docs/architecture/FRONTEND_ARCHITECTURE.md` first as your map. Read `docs/COBRA_STYLING.md` for styling conventions. For each file you review, read it fully — do not skim.

Look specifically for:
- Raw MUI imports that should use COBRA wrappers
- MUI icon imports (should be FontAwesome)
- Direct `toast` imports (should use `notify`)
- `any` types or type assertions that mask real types
- useEffect with missing or incorrect dependency arrays
- Components that re-render unnecessarily (missing memo/useMemo/useCallback)
- Inconsistent error handling across API calls
- Files over 500 lines that should be split

---

#### Reviewer 2: Frontend Feature Module Specialist

**Agent type:** `feature-dev:code-reviewer`
**Expertise:** React feature architecture, hooks, services, page composition, test patterns
**Output file:** `docs/reviews/frontend-features-review.md`
**Issue prefix:** `FF-`

**Scope:** All files under `src/frontend/src/features/`. There are 25 feature modules.

Read `docs/architecture/FEATURE_INVENTORY.md` and `docs/architecture/FRONTEND_ARCHITECTURE.md` first. For each feature module, review:
- Pages — composition, data fetching patterns, loading/error states
- Hooks — query key consistency, mutation patterns, optimistic updates
- Services — API call patterns, URL construction, type safety
- Components — prop drilling vs context, component size, single responsibility

Look specifically for:
- Features that have divergent patterns from each other (one fetches data in the page, another in a hook — which is the standard?)
- Duplicated service call patterns across features
- Hooks that duplicate logic another hook already provides
- Missing loading states or error boundaries
- Test files that exist but have low-value tests (testing implementation details instead of behavior)
- Features with zero tests that handle complex logic (see FEATURE_INVENTORY.md test coverage section)
- Large page components that should extract sub-components

---

#### Reviewer 3: .NET API & Controller Specialist

**Agent type:** `feature-dev:code-reviewer`
**Expertise:** ASP.NET Core 10, REST API design, controller patterns, authorization, middleware
**Output file:** `docs/reviews/api-controllers-review.md`
**Issue prefix:** `AC-`

**Scope:** All files under `src/Cadence.WebApi/` — controllers, hubs, middleware, authorization, services, Program.cs.

Read `docs/architecture/API_DESIGN.md`, `docs/architecture/BACKEND_ARCHITECTURE.md`, and `docs/architecture/ROLE_ARCHITECTURE.md` first.

Review all 36 controllers for:
- Consistent action method patterns (async/await, return types, status codes)
- Authorization attribute usage — are all endpoints protected? Are exercise-scoped endpoints using `[ExerciseAccess]` or `[ExerciseRole]`?
- Input validation — are DTOs validated? Are route parameters checked?
- Error responses — consistent ProblemDetails format? Proper status codes?
- Controller size — controllers over 500 lines should delegate to services
- Business logic leaking into controllers (should be in Core services)

Also review:
- `Program.cs` — middleware ordering, configuration patterns, potential startup issues
- `Authorization/` — handler correctness, policy coverage gaps
- `Hubs/` — connection management, group lifecycle, error handling
- `Services/` — InjectReadinessBackgroundService robustness, blob storage implementations

---

#### Reviewer 4: Core Domain & Data Specialist

**Agent type:** `feature-dev:code-reviewer`
**Expertise:** EF Core 10, domain modeling, service layer patterns, data access, migrations
**Output file:** `docs/reviews/core-domain-review.md`
**Issue prefix:** `CD-`

**Scope:** All files under `src/Cadence.Core/` — entities, services, data access, DbContext, validators, mappers.

Read `docs/architecture/DATA_MODEL.md`, `docs/architecture/BACKEND_ARCHITECTURE.md`, and `docs/architecture/FEATURE_INVENTORY.md` first.

Review:
- `Models/Entities/` — Entity design, navigation properties, missing indexes, nullable reference type usage
- `Data/AppDbContext.cs` — Configuration completeness, query filter correctness, relationship mapping
- `Data/Interceptors/` — OrganizationValidationInterceptor correctness
- `Data/Seeders/` — Idempotency, ordering dependencies
- `Features/*/Services/` — Service method size, query efficiency (N+1 queries, missing includes, over-fetching)
- `Features/*/Mappers/` — Mapping completeness, null handling
- `Features/*/Validators/` — Validation coverage, rule correctness
- `Core/Extensions/ServiceCollectionExtensions.cs` — Registration completeness, lifetime correctness

Look specifically for:
- Services that query the database in loops (N+1)
- Missing `.AsNoTracking()` on read-only queries
- Inconsistent org-scoping (some queries filter by OrgId, some rely on global filters — which is the pattern?)
- Large service classes (>500 lines) that should be decomposed
- Entities with navigation properties that are never used (over-includes)
- Missing validation for business rule invariants
- Services that catch and swallow exceptions

---

#### Reviewer 5: Holistic Architecture Reviewer

**Agent type:** `feature-dev:code-reviewer`
**Expertise:** Software architecture, system design, cross-cutting concerns, consistency patterns
**Output file:** `docs/reviews/architecture-review.md`
**Issue prefix:** `AR-`

**Scope:** The entire codebase at a structural level. This reviewer does NOT read every file line-by-line. Instead, they sample representative files from each layer and focus on cross-cutting patterns.

Read ALL 8 architecture docs first. Then examine:

1. **Cross-layer consistency** — Do frontend service types match backend DTOs? Are API routes consistent with frontend service URLs? Are SignalR event names consistent between hub context and frontend subscriptions?

2. **Feature module consistency** — Pick 3 well-developed features (Exercises, Injects, Organizations) and 3 thinner features (Autocomplete, Delivery Methods, Feedback). Compare their internal structure. Document the "golden path" pattern and note which features deviate.

3. **Error handling strategy** — Trace an error from the frontend API call through the controller, service, and database. Is there a consistent strategy? Are errors properly categorized (validation vs authorization vs server)?

4. **Test architecture** — Review `src/Cadence.Core.Tests/Helpers/` for test infrastructure. Are test helpers consistent? Is there a test base class? Sample 3 backend test files and 3 frontend test files — are they testing behavior or implementation details?

5. **Configuration sprawl** — Review `appsettings.json` and `appsettings.Development.json`. Are config sections well-organized? Are there magic strings for config keys?

6. **Dependency health** — Check `package.json` and `.csproj` files for outdated or unnecessary dependencies.

7. **Missing abstractions** — Are there patterns that repeat across 3+ features that should be extracted into shared infrastructure?

---

### Orchestration Instructions

1. Create branch `maintenance/code-review-v1` from `main`
2. Create directory `docs/reviews/`
3. Launch all 5 reviewers in parallel using the Task tool with `subagent_type: "feature-dev:code-reviewer"`
4. Each reviewer reads their assigned architecture docs FIRST, then systematically reviews their scope
5. Each reviewer writes their findings to their assigned output file using the format above, including the header metadata block and their namespaced issue IDs
6. After all 5 reviewers complete, read all 5 review files and create `docs/reviews/SUMMARY.md` (see format below)
7. Commit all 6 review files with message: `docs(reviews): add V1 code review findings (pass 1)`

### Orchestrator Summary Format

The orchestrator (you) produces the SUMMARY.md after reading all 5 reviewer reports. This is the primary input for the fix-agent swarm in the next session.

```markdown
# Code Review Summary — Pass 1

> **Date:** {YYYY-MM-DD}
> **Branch:** maintenance/code-review-v1
> **Reviewers:** 5 (Frontend Infrastructure, Frontend Features, API & Controllers, Core Domain & Data, Architecture)

---

## Aggregate Metrics

| Reviewer | Files Reviewed | Critical | Major | Minor | Positive |
|----------|---------------|----------|-------|-------|----------|
| Frontend Infrastructure | X | X | X | X | X |
| Frontend Features | X | X | X | X | X |
| API & Controllers | X | X | X | X | X |
| Core Domain & Data | X | X | X | X | X |
| Architecture | X | X | X | X | X |
| **Total** | **X** | **X** | **X** | **X** | **X** |

## Issues by Category

| Category | Critical | Major | Minor | Total |
|----------|----------|-------|-------|-------|
| DRY | X | X | X | X |
| FileSize | X | X | X | X |
| Orphaned | X | X | X | X |
| Readability | X | X | X | X |
| Stability | X | X | X | X |
| Convention | X | X | X | X |

## Top 10 Most Impactful Issues

> Ranked by blast radius (how many files/features affected) and severity.
> These are the issues that fix agents should tackle first.

| Rank | ID | Title | Category | Blast Radius | Source Report |
|------|-----|-------|----------|-------------|--------------|
| 1 | {FF-C01} | {title} | {cat} | {X files / X features} | frontend-features-review.md |
| 2 | ... | ... | ... | ... | ... |
| ... | | | | | |

## Cross-Reviewer Patterns

> Issues that appeared independently in multiple reviewers' findings.

### Pattern 1: {Name}
- **Found by:** {Reviewer A (ID), Reviewer B (ID)}
- **Description:** {The shared pattern}
- **Recommended unified fix:** {Single approach that addresses it everywhere}

### Pattern 2: {Name}
...

## Recommended Remediation Order

> For consumption by fix-agent swarm. Ordered by: (1) critical first, (2) shared patterns before isolated issues, (3) foundation fixes before feature fixes.

### Phase 1: Critical & Shared Patterns
{List issue IDs with brief description}

### Phase 2: Major Stability & DRY
{List issue IDs}

### Phase 3: Major Readability & FileSize
{List issue IDs}

### Phase 4: Minor Cleanup
{List issue IDs}

## Positive Patterns to Preserve

> Consolidated list of things done well. Fix agents must NOT regress these.

| ID | Pattern | Where | Replicate In |
|----|---------|-------|-------------|
| {FI-P01} | {description} | {file} | {other areas} |
| ... | | | |
```

### Important Notes

- Reviewers should be constructive, not pedantic. The goal is a prioritized improvement roadmap, not a list of style nits.
- When a reviewer finds a pattern issue (same problem in 10 files), document it once with the full list of affected files, don't write 10 separate issues.
- Reviewers should note positive patterns too — these are guardrails for fix agents so they don't regress good patterns.
- Each reviewer should aim to read at least 30-50 files in their domain. Quick scans are not acceptable — read each file fully.
- Issues should have specific file paths and line numbers where possible.
- Recommendations should be specific and actionable ("extract X into a shared hook called useY" not "improve code reuse").
- The SUMMARY.md remediation order is what the next session's fix agents will consume. Make it precise.
