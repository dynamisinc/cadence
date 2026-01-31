# Story: About Page with Version Info and Release History

**As a** Cadence user,
**I want** to view the current version and historical release notes,
**So that** I can understand what version I'm running and review past changes.

## Context

Users and support staff need a reliable place to find version information. During troubleshooting ("what version are you on?") this is essential. Historical release notes help users understand the evolution of features and find when specific changes were introduced.

## Acceptance Criteria

- [ ] **Given** I am logged in, **when** I click my profile menu, **then** I see an "About" option
- [ ] **Given** I click "About" from profile menu, **when** the page loads, **then** I see the current frontend version prominently displayed
- [ ] **Given** I am on the About page, **when** the API is reachable, **then** I see the API version with a "Connected" indicator
- [ ] **Given** I am on the About page, **when** the API is not reachable, **then** I see "API Unavailable" with appropriate styling (not an error, just informational)
- [ ] **Given** I am on the About page, **when** I scroll down, **then** I see a chronological list of release notes (newest first)
- [ ] **Given** I am viewing release history, **when** there are more than 5 releases, **then** I see the 5 most recent with a "Show older" option
- [ ] **Given** I am on the About page, **when** I am offline, **then** I see cached version info and release notes (if previously loaded)
- [ ] **Given** I am on the Settings page, **when** I view the page, **then** I see a version info card with link to full About page

## UI/UX Notes

### About Page Layout

```
┌─────────────────────────────────────────────────────┐
│ ← Back                                              │
├─────────────────────────────────────────────────────┤
│                                                     │
│ About Cadence                                       │
│                                                     │
│ ┌─────────────────────────────────────────────────┐ │
│ │ App Version    1.5.0                            │ │
│ │ API Version    1.2.0  ● Connected               │ │
│ │ Build Date     January 30, 2026                 │ │
│ │ Build          a1b2c3d                          │ │
│ └─────────────────────────────────────────────────┘ │
│                                                     │
│ ─────────────────────────────────────────────────── │
│                                                     │
│ Release History                                     │
│                                                     │
│ ┌─ v1.5.0 · January 30, 2026 ───────────────────┐  │
│ │ Features                                       │  │
│ │ • Exercise progress dashboard                  │  │
│ │ • Observation quick-entry improvements         │  │
│ │                                                │  │
│ │ Fixes                                          │  │
│ │ • Offline sync reliability improved            │  │
│ └────────────────────────────────────────────────┘  │
│                                                     │
│ ┌─ v1.4.0 · January 22, 2026 ───────────────────┐  │
│ │ Features                                       │  │
│ │ • Real-time multi-user sync                    │  │
│ │ • Offline mode with local caching              │  │
│ └────────────────────────────────────────────────┘  │
│                                                     │
│ [Show older releases]                               │
│                                                     │
│ ─────────────────────────────────────────────────── │
│                                                     │
│ Cadence is a HSEEP-compliant MSEL management       │
│ platform for emergency management exercises.        │
│                                                     │
│ © 2026                                             │
│                                                     │
└─────────────────────────────────────────────────────┘
```

### Settings Page Version Card

```
┌─────────────────────────────────────────────────────┐
│ Version Information                                 │
│                                                     │
│ App Version    1.5.0                               │
│ API Version    1.2.0  ● Connected                  │
│ Build          a1b2c3d                             │
│                                                     │
│ [View release notes →]                             │
└─────────────────────────────────────────────────────┘
```

### Profile Menu Addition

```
┌──────────────────┐
│ Tom Smith        │
│ tom@example.com  │
├──────────────────┤
│ Settings         │
│ About            │  ← New
├──────────────────┤
│ Sign out         │
└──────────────────┘
```

### Design Notes

- Clean, minimal layout - this is informational, not interactive
- Version info card uses subtle background differentiation
- API status indicator: green dot = connected, gray dot = offline/unavailable
- Release entries are collapsible cards (expanded by default for recent)
- Responsive: single column on mobile, comfortable reading width on desktop
- Route: `/about`

## Out of Scope

- Feedback/support contact form (separate feature)
- System health/status dashboard
- License information display
- Exportable version report
- Footer version display (keeping UI minimal)

## Dependencies

- Version injection into frontend build
- `/api/version` endpoint for API version
- Release notes content (CHANGELOG.md or API)
- Profile menu component (should exist from auth work)
- Settings page (should exist)

## Technical Notes

- Frontend version: Injected at build time via Vite define
- API version: Fetch from `/api/version` endpoint
- Release notes: Bundle CHANGELOG.md at build, parse markdown to structured data
- Consider caching release notes in IndexedDB for offline access

## Open Questions

- [x] Include build SHA for debugging? **Decision: Yes, show abbreviated SHA (7 chars)**
- [x] Link to GitHub releases? **Decision: Not initially - keep self-contained**
- [x] Add footer with version? **Decision: No - keep UI minimal, use Settings card instead**

## Story Points

**Estimate:** 5 points

## Definition of Done

- [ ] About page implemented at `/about` route
- [ ] Profile menu updated with "About" link
- [ ] Settings page includes VersionInfoCard component
- [ ] Version info displays frontend version, API version, build date, commit SHA
- [ ] API connection status shown with visual indicator
- [ ] Release history displays with show more/less functionality
- [ ] Offline mode shows cached data gracefully
- [ ] Unit tests for hooks (useApiVersion, useReleaseNotes)
- [ ] Component tests for AboutPage and VersionInfoCard
