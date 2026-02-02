# Feature: Versioning & Release Notes

**Phase:** MVP
**Status:** Complete

## Overview

Automated semantic versioning with Release Please, conventional commits enforcement, and user-facing version display with release notes. Users are notified of updates when returning to the app, and can view full release history in the About page.

## Problem Statement

Users and support staff need to know what version they're running and what has changed. Without automated versioning and release notes, users are unaware of new features, and troubleshooting is difficult ("what version are you on?"). Manual versioning is error-prone and changelog updates are often forgotten.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-whats-new-notification.md) | What's New Notification on Version Change | P1 | ✅ Complete |
| [S02](./S02-about-page-release-history.md) | About Page with Version Info and Release History | P1 | ✅ Complete |

## User Personas

| Persona | Interaction |
|---------|-------------|
| **All Users** | See "What's New" modal on version change, dismisses to continue workflow |
| **All Users** | View current version and release history in About page |
| **Support Staff** | Ask users "what version are you on?" to troubleshoot issues |
| **Exercise Director** | Understands what features/fixes are available in current version |
| **Administrators** | Track version history for compliance and change management |

## Key Concepts

| Term | Definition |
|------|------------|
| **Semantic Versioning** | MAJOR.MINOR.PATCH format (e.g., 1.3.0) |
| **Release Please** | Google's automated release management tool |
| **Conventional Commits** | Standardized commit message format (type(scope): description) |
| **Changelog** | Chronological list of changes (CHANGELOG.md) |
| **Release Notes** | User-facing description of changes in a version |
| **What's New Modal** | Notification shown when app version changes |
| **About Page** | Full version information and release history |

## Architecture Overview

### Components

1. **Release Please** - Automated release management via GitHub Action
2. **Commitlint** - Conventional commit message enforcement
3. **Version Endpoint** - Backend API for version information
4. **What's New Modal** - Frontend notification on version changes
5. **About Page** - Full release history display

### Version Flow

```
Conventional   ───▶  Release Please  ───▶  GitHub
Commits             Action                Releases
                       │
                       ▼
                  CHANGELOG.md
                  package.json
                  version updated
```

## Conventional Commits

All commits must follow the conventional commits format:

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

### Types

| Type | Purpose | Version Bump |
|------|---------|--------------|
| `feat` | New feature | Minor |
| `fix` | Bug fix | Patch |
| `feat!` | Breaking change | Major |
| `docs` | Documentation only | None |
| `style` | Code style (no logic changes) | None |
| `refactor` | Code refactoring | None |
| `perf` | Performance improvement | Patch |
| `test` | Adding/updating tests | None |
| `chore` | Maintenance tasks | None |
| `ci` | CI/CD changes | None |

### Scopes

| Scope | Area |
|-------|------|
| `api` | Backend changes |
| `ui` | Frontend changes |
| `offline` | Offline/PWA features |
| `auth` | Authentication |
| `docs` | Documentation |
| `ci` | CI/CD |
| `deps` | Dependencies |

## Configuration Files

| File | Purpose |
|------|---------|
| `release-please-config.json` | Monorepo package configuration |
| `.release-please-manifest.json` | Current version tracking |
| `commitlint.config.js` | Commit message rules |
| `.github/workflows/release-please.yml` | Release automation |
| `.github/workflows/commitlint.yml` | PR commit validation |

## Version Storage

- **Frontend version**: Injected at build time via Vite `define`
- **API version**: `InformationalVersion` in csproj
- **Last seen version**: `localStorage.cadence_last_seen_version`

## Dependencies

- None (foundational feature)

## Acceptance Criteria (Feature-Level)

- [x] Conventional commits are enforced on all PRs via commitlint
- [x] Release Please automatically creates release PRs with version bumps
- [x] CHANGELOG.md is automatically updated with commit messages
- [x] Frontend version is injected at build time
- [x] Backend `/api/version` endpoint returns version, commit SHA, build date
- [x] Users see "What's New" modal when app version changes
- [x] Users can view full release history in About page
- [x] Version info is accessible from Settings page
- [x] Offline mode shows cached version information

## Integration Points

1. **Profile Menu** - "About" link added
2. **Settings Page** - VersionInfoCard at bottom
3. **App.tsx** - WhatsNewProvider wrapper
4. **Routes** - `/about` route added
5. **CI/CD** - Release Please workflow, commitlint workflow

## Notes

### Frontend Components

- `useVersionCheck` - Hook to detect version changes
- `WhatsNewModal` - Modal displayed on version change
- `WhatsNewProvider` - Context wrapper to manage modal state
- `AboutPage` - Full page with version info and release history
- `VersionInfoCard` - Compact card for embedding in Settings

### Backend Endpoint

**GET /api/version** - Returns current API version information

```json
{
  "version": "1.0.0",
  "commitSha": "abc1234",
  "buildDate": "2026-01-30T12:00:00Z",
  "environment": "Production"
}
```

### Files Created/Modified

**New Files:**
- `release-please-config.json`
- `.release-please-manifest.json`
- `commitlint.config.js`
- `.github/workflows/release-please.yml`
- `.github/workflows/commitlint.yml`
- `src/Cadence.Core/CHANGELOG.md`
- `src/frontend/CHANGELOG.md`
- `src/Cadence.WebApi/Controllers/VersionController.cs`
- `src/frontend/src/config/version.ts`
- `src/frontend/src/types/env.d.ts`
- `src/frontend/src/features/version/` (entire feature)

**Modified Files:**
- `src/frontend/vite.config.ts` - Version injection
- `src/frontend/package.json` - Version bump to 1.0.0
- `src/Cadence.WebApi/Cadence.WebApi.csproj` - InformationalVersion
- `src/frontend/src/App.tsx` - WhatsNewProvider, AboutPage route
- `src/frontend/src/core/components/ProfileMenu.tsx` - About link
- `src/frontend/src/features/settings/pages/UserSettingsPage.tsx` - VersionInfoCard
