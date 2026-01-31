# Changelog

All notable changes to Cadence will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-01-30

### Features

#### Exercise Management

- Exercise list, create, edit, and detail views with full CRUD operations
- Exercise status workflow (Draft → Active → Completed → Archived) with confirmation dialogs
- Exercise duplication with all related data (phases, objectives, injects)
- Setup progress sidebar for Draft exercises showing completion status
- Practice mode toggle with visual indicator across the application

#### MSEL & Inject Management

- Complete MSEL/Inject management with status tracking (Pending, Fired, Skipped, Deferred)
- Drag-and-drop inject reordering with optimistic updates
- Fire Confirmation Dialog with user preference persistence
- Inject organization features: sorting, filtering, grouping, and full-text search
- State persistence to sessionStorage per exercise
- Inject-to-Objective linking with many-to-many relationships
- Inject detail drawer with description preview and delivery method icons

#### Exercise Conduct

- Real-time exercise clock with start/pause/stop/reset controls
- Time-based inject sections (Ready to Fire, Upcoming, Later, Fired, Skipped)
- Clock-driven and facilitator-paced conduct view modes
- Narrative view for Observers showing "The Story So Far" and "What's Happening Now"
- Layout mode toggle (Classic, Sticky Header, Floating Chip)
- Progress indicator with current phase name and completion bar

#### Observations

- Observations panel for evaluators during active exercises
- Quick entry with keyboard shortcuts (Ctrl+O or O)
- Observation-to-inject linking with clickable references
- Location field for documenting where observations occurred

#### Capabilities & Metrics

- Organization-scoped capability library (FEMA, NATO, NIST CSF 2.0, ISO 22301)
- Admin UI for managing capabilities and importing predefined libraries
- Exercise metrics dashboard with progress tracking
- Inject summaries (P/S/M/U ratings) and observation summaries
- Timeline analysis and capability performance metrics

#### Settings & User Preferences

- User preferences (time format, theme)
- Exercise settings (confirmation dialogs, auto-fire, clock mode)
- Timing configuration (start time, scenario time, time ratio)
- Expanded timezone support (67 global timezones)

#### Authentication & Authorization

- JWT-based authentication with access tokens and refresh tokens
- Remember Me support with extended refresh token lifetime
- Cross-tab logout synchronization
- Role-based access control with three-tier hierarchy:
  - System Roles: Admin, Manager, User
  - Organization Roles: OrgAdmin, OrgManager, OrgUser
  - Exercise Roles: Observer, Evaluator, Controller, ExerciseDirector
- User management with search, role filtering, and deactivation
- Exercise participant management with HSEEP role assignment
- Inline user creation from Add Participant dialog

#### Navigation & UX

- Role-based sidebar and header navigation with collapsible menus
- In-exercise navigation with role-filtered menu items
- Breadcrumb navigation throughout the app
- My Assignments dashboard showing exercises grouped by status
- Profile menu with role display and exercise assignments

#### Reports & Export

- Excel export for MSEL data and observations
- Full exercise package export (ZIP with MSEL, Observations, Summary)
- Excel import wizard (upload → sheet selection → column mapping → validation → import)
- Intelligent column mapping with auto-suggestions
- Template download with Instructions and Lookups worksheets

#### Real-Time & Offline

- SignalR-based live updates for injects, observations, and clock state
- Offline detection with combined browser online + SignalR state monitoring
- IndexedDB caching via Dexie.js for injects and observations
- Action queue with FIFO processing on reconnect
- Conflict resolution with user notification dialog
- Force-push capability on reconnect for offline sync

#### PWA Support

- Progressive Web App support for installation
- Responsive design for tablet and desktop

### Bug Fixes

- Fixed auth handling when API is unreachable
- Fixed character counter showing `[object Object]` in exercise name field
- Fixed file picker opening twice when clicking Browse Files
- Fixed unsaved changes warning appearing after successful exercise creation
- Fixed InjectTypeChip null check error
- Fixed duplicate observation prevention from SignalR race conditions
- Fixed empty state flash on page load
- Fixed objective filter label display
- Fixed vite.config.ts and useInjects.ts indentation errors
- Added liveness endpoint (/api/health/live) for deployment validation

### Technical

- React 19 with TypeScript 5.x
- Material UI 7 component library with COBRA styling system
- Vite 7 build tooling
- .NET 10 backend with Entity Framework Core
- Azure App Service hosting (always warm, no cold starts)
- Azure SignalR Service for real-time communication
- Azure SQL Database
- FontAwesome icons
- Vitest + React Testing Library (2500+ tests)
