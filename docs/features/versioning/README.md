# Versioning & Release Notes

This feature provides automated versioning with Release Please, conventional commits enforcement, and user-facing version display with release notes.

## Architecture Overview

### Components

1. **Release Please** - Automated release management via GitHub Action
2. **Commitlint** - Conventional commit message enforcement
3. **Version Endpoint** - Backend API for version information
4. **What's New Modal** - Frontend notification on version changes
5. **About Page** - Full release history display

### Version Flow

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  Conventional   │────▶│  Release Please  │────▶│    GitHub       │
│  Commits        │     │  Action          │     │    Releases     │
└─────────────────┘     └──────────────────┘     └─────────────────┘
                                │
                                ▼
                        ┌──────────────────┐
                        │  CHANGELOG.md    │
                        │  package.json    │
                        │  version updated │
                        └──────────────────┘
```

## Configuration Files

| File | Purpose |
|------|---------|
| `release-please-config.json` | Monorepo package configuration |
| `.release-please-manifest.json` | Current version tracking |
| `commitlint.config.js` | Commit message rules |
| `.github/workflows/release-please.yml` | Release automation |
| `.github/workflows/commitlint.yml` | PR commit validation |

## Conventional Commits

All commits must follow the conventional commits format:

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

### Types

| Type | Purpose |
|------|---------|
| `feat` | New feature (bumps minor) |
| `fix` | Bug fix (bumps patch) |
| `docs` | Documentation only |
| `style` | Code style (no logic changes) |
| `refactor` | Code refactoring |
| `perf` | Performance improvement |
| `test` | Adding/updating tests |
| `chore` | Maintenance tasks |
| `ci` | CI/CD changes |

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

### Examples

```bash
# Feature
feat(ui): add What's New modal on version change

# Bug fix
fix(api): handle null response in version endpoint

# Breaking change (adds ! after type)
feat(api)!: change version response format
```

## Frontend Components

### useVersionCheck Hook

Detects version changes and manages What's New modal visibility.

```typescript
const { showWhatsNew, dismissWhatsNew } = useVersionCheck();
```

### WhatsNewModal

Modal displayed when app version changes.

```typescript
<WhatsNewModal open={showWhatsNew} onDismiss={dismissWhatsNew} />
```

### WhatsNewProvider

Wrap your app to automatically show What's New on version changes.

```typescript
<WhatsNewProvider>
  <App />
</WhatsNewProvider>
```

### AboutPage

Full page with version info and release history at `/about`.

### VersionInfoCard

Compact version card for embedding in Settings page.

## Backend Endpoint

### GET /api/version

Returns current API version information.

**Response:**

```json
{
  "version": "1.0.0",
  "commitSha": "abc1234",
  "buildDate": "2026-01-30T12:00:00Z",
  "environment": "Production"
}
```

## Version Storage

- **Frontend version**: Injected at build time via Vite `define`
- **API version**: `InformationalVersion` in csproj
- **Last seen version**: `localStorage.cadence_last_seen_version`

## Integration Points

1. **Profile Menu** - "About" link added
2. **Settings Page** - VersionInfoCard at bottom
3. **App.tsx** - WhatsNewProvider wrapper
4. **Routes** - `/about` route added

## Testing

Run the version feature tests:

```bash
# Frontend tests
cd src/frontend
npm run test -- --grep version

# All tests
npm run test
```

## Files Created/Modified

### New Files

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

### Modified Files

- `src/frontend/vite.config.ts` - Version injection
- `src/frontend/package.json` - Version bump to 1.0.0
- `src/Cadence.WebApi/Cadence.WebApi.csproj` - InformationalVersion
- `src/frontend/src/App.tsx` - WhatsNewProvider, AboutPage route
- `src/frontend/src/core/components/ProfileMenu.tsx` - About link
- `src/frontend/src/features/settings/pages/UserSettingsPage.tsx` - VersionInfoCard
