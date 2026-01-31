# Story: What's New Notification on Version Change

**As a** Cadence user,
**I want** to see what's changed when I open the app after an update,
**So that** I'm aware of new features and fixes without hunting for release notes.

## Context

Users returning to Cadence after an update should be informed of changes relevant to them. This builds trust (transparency about what changed) and helps adoption of new features. The notification should be unobtrusive - informative but not blocking workflow.

## Acceptance Criteria

- [ ] **Given** the app version has changed since my last visit, **when** I open Cadence, **then** I see a "What's New" modal displaying changes for the current version
- [ ] **Given** the "What's New" modal is displayed, **when** I click "Got it" or click outside the modal, **then** the modal closes and I can use the app normally
- [ ] **Given** I have dismissed the "What's New" modal, **when** I refresh or return to the app (same version), **then** the modal does not appear again
- [ ] **Given** the "What's New" modal is displayed, **when** I click "View all release notes", **then** I am navigated to the About page
- [ ] **Given** this is my first time using Cadence (no stored version), **when** I open the app, **then** the "What's New" modal does NOT appear (avoid overwhelming new users)
- [ ] **Given** the app cannot reach the API to fetch release notes, **when** I open the app after an update, **then** the modal shows version number with a simple "Updated to version X.Y.Z" message

## UI/UX Notes

### Modal Content Structure

```
┌─────────────────────────────────────────┐
│ ✨ What's New in Cadence               │
│ Version 1.3.0                           │
├─────────────────────────────────────────┤
│                                         │
│ Features                                │
│ • Exercise progress dashboard           │
│ • Observation quick-entry improvements  │
│                                         │
│ Fixes                                   │
│ • Offline sync reliability improved     │
│                                         │
├─────────────────────────────────────────┤
│ [View all release notes]    [Got it ➜] │
└─────────────────────────────────────────┘
```

### Design Notes

- Modal width: 480px max, responsive on smaller screens
- "Got it" is primary action (filled button)
- "View all release notes" is secondary (text link)
- Use existing MUI Dialog component
- Subtle entrance animation (fade + slight scale)

### Version Storage

- Store last-seen version in localStorage: `cadence_last_seen_version`
- Compare against current app version on mount

## Out of Scope

- Per-feature "tour" or walkthrough (future enhancement)
- Notification badge/indicator when modal is dismissed (v2)
- Admin ability to customize release notes content
- Push notifications about updates

## Dependencies

- Version injection into frontend build (implementation prompt covers this)
- Release notes content source (CHANGELOG.md or API endpoint)

## Technical Notes

- Release notes content: Parse from bundled CHANGELOG.md at build time OR fetch from `/api/release-notes`
- Recommend: Bundle at build time for offline support, with API fallback for dynamic updates
- localStorage key: `cadence_last_seen_version`

## Open Questions

- [x] Should modal auto-dismiss after a timeout? **Decision: No - user should explicitly acknowledge**
- [x] Show modal on every minor version or only selected "notable" releases? **Decision: Every version change - keep it simple**

## Story Points

**Estimate:** 3 points

## Definition of Done

- [ ] What's New modal component implemented with MUI Dialog
- [ ] useVersionCheck hook detects version changes
- [ ] Modal only appears for returning users (not first visit)
- [ ] Dismissal persists across page refreshes
- [ ] Navigation to About page works
- [ ] Unit tests for hook logic
- [ ] Component test for modal interactions
