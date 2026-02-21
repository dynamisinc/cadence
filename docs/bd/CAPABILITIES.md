# Cadence Platform Capabilities

**HSEEP-Compliant Exercise Design, Conduct, and Evaluation Platform**

---

## 1. Executive Summary

Cadence is a purpose-built web platform for the design, conduct, and evaluation of emergency management exercises in full compliance with the Homeland Security Exercise and Evaluation Program (HSEEP) 2020 doctrine. The platform provides end-to-end management of Master Scenario Events Lists (MSELs), real-time exercise conduct with dual-time tracking, structured HSEEP evaluation through Exercise Evaluation Guides (EEGs), and comprehensive after-action review support.

Cadence was designed from the ground up to address the operational realities of exercise conduct: unreliable field connectivity, multi-hour sessions that cannot tolerate data loss, multi-agency participation requiring strict data isolation, and the need for real-time coordination across geographically distributed teams. The platform serves Exercise Directors, Controllers, Evaluators, Observers, Players, Simulators, Facilitators, Safety Officers, and Trusted Agents -- every HSEEP-defined participant role -- with role-appropriate interfaces and permissions.

The platform operates as a multi-tenant, cloud-hosted SaaS application with offline-first architecture, ensuring uninterrupted exercise conduct regardless of network conditions.

---

## 2. HSEEP Compliance

### 2.1 Doctrinal Alignment

Cadence implements the HSEEP 2020 exercise lifecycle with native support for all HSEEP-defined exercise types, participant roles, evaluation methodology, and documentation requirements.

**Supported Exercise Types:**

| Type | Abbreviation | Description |
|------|--------------|-------------|
| Tabletop Exercise | TTX | Discussion-based exercises with Facilitator-Paced timing mode |
| Functional Exercise | FE | Operations-based exercises with simulated response |
| Full-Scale Exercise | FSE | Operations-based exercises deploying real resources |
| Computer-Aided Exercise | CAX | Exercises integrated with simulation systems |

**HSEEP Participant Roles:**

Cadence implements all nine HSEEP-defined exercise roles with granular, exercise-scoped permissions:

| Role | Platform Capabilities |
|------|----------------------|
| **Exercise Director** | Full exercise authority: configuration, activation, participant management, Go/No-Go decisions, AAR review |
| **Controller** | MSEL delivery, inject firing, scenario flow management, field observation capture |
| **Evaluator** | Structured EEG assessment, ad-hoc observation capture, P/S/M/U rating, objective-linked evaluation |
| **Observer** | Read-only exercise monitoring without interference |
| **Player** | Inject receipt and acknowledgment in virtual/hybrid exercises |
| **Simulator** | Simulated communications and SimCell activity management |
| **Facilitator** | Discussion flow guidance and pace control for TTX exercises |
| **Safety Officer** | Safety oversight with exercise pause/stop authority |
| **Trusted Agent** | Embedded subject matter expertise with observation capabilities |

### 2.2 HSEEP Inject Status Workflow

Cadence implements the FEMA PrepToolkit inject status workflow as its default lifecycle:

```
Draft --> Submitted --> Approved --> Synchronized --> Released --> Complete
```

With parallel paths for Deferred (cancelled before delivery) and Obsolete (soft-removed but retained for audit). The platform supports a formal inject approval workflow with configurable policies at the organization, exercise, and inject levels, ensuring quality control and governance compliance before exercise conduct.

### 2.3 Exercise Evaluation Guide (EEG) Implementation

Cadence implements the full HSEEP evaluation chain:

**Objective --> Capability --> Capability Target --> Critical Task --> Inject --> EEG Entry**

Exercise planners define Capability Targets with measurable performance thresholds and Critical Tasks that specify the actions required to achieve each target. MSEL injects are linked to the Critical Tasks they are designed to test. During conduct, Evaluators record structured EEG entries against specific Critical Tasks using the HSEEP P/S/M/U rating scale:

- **P** -- Performed without Challenges
- **S** -- Performed with Some Challenges
- **M** -- Performed with Major Challenges
- **U** -- Unable to be Performed

An EEG Coverage Dashboard provides real-time visibility into which Critical Tasks have been evaluated, the performance distribution by Capability Target, and gaps requiring attention. All EEG data exports in AAR-ready format organized by Capability, Target, Task, and Observation.

### 2.4 Cross-Domain Framework Support

While HSEEP is the default framework, Cadence provides configurable status workflows and terminology to serve organizations operating under alternative frameworks:

| Framework | Audience |
|-----------|----------|
| DoD / Joint Training System (JTS) | Military exercises (STARTEX/ENDEX, JMSEL) |
| NATO | Allied military coordination (EXCON/DISTAFF, LIVEX/CPX) |
| UK Cabinet Office | UK government (Main Events List, Cell structure) |
| Australian AIIMS | Australia/NZ emergency management |
| NIST / MITRE ATT&CK | Cybersecurity exercises (Red/Blue/Purple teams) |
| CMS / Joint Commission | Healthcare exercises (HICS, surge levels) |
| FFIEC / FINRA | Financial sector exercises (RTO/RPO/MTD metrics) |
| ISO 22301 / BCI | Private sector business continuity |

---

## 3. Exercise Design and Planning

### 3.1 Exercise Lifecycle Management

Cadence manages exercises through a complete HSEEP-compliant lifecycle:

**Draft --> Active --> Paused --> Completed --> Archived**

Each status enforces appropriate data editability, access controls, and available actions. The platform supports pause and resume capabilities for administrative holds, safety events, and multi-day exercises. Exercise status and clock state operate independently, providing flexibility for scenarios such as administrative pauses while scenario time continues, or clock pauses for discussion while the exercise remains active.

Transition safeguards include validation rules (e.g., exercises cannot activate without at least one inject), confirmation dialogs for destructive transitions, and a full audit trail of who performed each transition and when.

### 3.2 MSEL Authoring

The Master Scenario Events List is the core artifact of exercise planning. Cadence provides:

**Inject Management:** Full create, read, update, and delete operations for injects with support for all standard MSEL fields including inject number (auto-sequential), title, description, scheduled time, scenario time, sender, recipient, delivery method, expected player action, and controller notes.

**Dual-Time Tracking:** Every inject supports two independent time concepts -- Scheduled Time (wall clock delivery) and Scenario Time (in-story time). This enables multi-day scenarios to be compressed into shorter exercise windows. A "Day 3" scenario event can be scheduled for delivery at 11:00 AM on a single exercise day.

**Phase Organization:** Exercises can be structured into named phases representing time periods (Morning, Afternoon), operational stages (Initial Response, Sustained Operations, Recovery), or scenario segments (Pre-Event, Escalation, Stabilization). Injects are assigned to phases and the MSEL can be grouped and collapsed by phase for large exercises.

**Objective Linkage:** HSEEP-compliant SMART objectives are defined per exercise and linked to injects in a many-to-many relationship, enabling coverage analysis to verify all objectives are adequately exercised.

**Inject Filtering and Search:** Multi-criteria filtering by status, phase, objective, delivery method, and controller assignment. Full-text search across inject fields for rapid location of specific content in large MSELs. Persistent filter state within sessions.

**Inject Organization:** Configurable sorting, grouping by phase/status/objective with collapsible sections, and drag-and-drop reordering that automatically updates sequence numbers.

### 3.3 Excel Import and Export

Cadence preserves existing organizational workflows by providing full round-trip Excel compatibility:

**Import:** A guided wizard supports uploading Excel (.xlsx, .xls) and CSV files, mapping spreadsheet columns to Cadence inject fields with intelligent synonym matching for delivery methods, validating data before finalization, and creating or updating injects by inject number. Organizations can continue authoring MSELs in Excel and import them into Cadence for conduct.

**Export:** MSEL data exports to formatted Excel workbooks with consistent column ordering that matches the import template for round-trip compatibility. Exports include optional metadata sheets with exercise information, objectives, and phases. Blank templates are available for download to standardize data entry.

### 3.4 Exercise Configuration

Before an exercise begins, Cadence guides setup through a structured configuration workflow with a visual progress dashboard:

- **Basic Information:** Exercise name, type (TTX/FE/FSE/CAX), description, dates
- **Participant Assignment:** Users assigned to HSEEP roles per exercise, with bulk import via CSV/Excel
- **Time Zone Configuration:** Geographic time zone for inject scheduling
- **Clock Mode Selection:** Clock-Driven (real-time delivery) or Facilitator-Paced (manual advancement for TTX)
- **Objective Definition:** SMART objectives with capability alignment
- **Phase Definition:** Exercise segmentation into operational stages
- **MSEL Population:** Inject entry or Excel import
- **Inject Approval:** Optional formal approval workflow before conduct

The progress dashboard displays completion status for each configuration area and prevents exercise activation until required items are complete.

### 3.5 Inject Approval Workflow

Organizations can require formal approval of injects before exercise conduct. The workflow is configurable at three tiers:

- **Organization Level:** Policy set to Disabled, Optional, or Required
- **Exercise Level:** Directors enable/disable per exercise (constrained by org policy)
- **Inject Level:** Status workflow enforced per exercise setting

When enabled, injects follow a Draft --> Submitted --> Approved workflow. The platform provides an Approval Queue view for reviewers, batch approval and rejection capabilities, configurable approval permissions (including whether Controllers or Evaluators may approve), self-approval policies, and a Go-Live Gate that prevents exercise activation until all injects are approved. Every status change is recorded with full audit trail including user, timestamp, and notes.

### 3.6 MSEL Duplication and Reuse

Organizations running recurring exercises (annual hurricane drills, quarterly tabletops) can duplicate existing exercises including their complete MSEL, preserving inject content, phase structure, and objective definitions while creating a new exercise for modification.

---

## 4. Exercise Conduct

### 4.1 Real-Time Exercise Clock

Cadence provides two timing modes to support different exercise types:

**Clock-Driven Mode:** For operations-based exercises (FE, FSE), the exercise clock runs in real time. Injects automatically transition to "Ready" status when the clock reaches their scheduled delivery time. Controllers see injects organized into Now (ready to fire), Upcoming (within configurable window), and Completed sections. The clock supports start, pause, and resume operations with elapsed time preserved across pauses.

**Facilitator-Paced Mode:** For discussion-based exercises (TTX), the Facilitator manually advances through injects in sequence without time constraints. No clock runs; instead, the Facilitator controls the pace and can skip, reorder, or revisit injects as the discussion evolves.

Both modes support Story Time display when exercises use compressed timelines, showing participants where they are in the scenario narrative alongside wall clock time.

### 4.2 Inject Delivery (Firing)

Controllers deliver injects to exercise players through the platform's firing mechanism. When a Controller fires an inject:

- The inject status transitions from Ready/Pending to Released
- Both wall clock time and scenario time are recorded
- All connected users receive real-time notification via SignalR
- Evaluators are alerted to begin observation
- The exercise timeline updates across all clients

Fire confirmation dialogs are available for critical injects to prevent accidental delivery. Injects can also be Skipped (with required reason) or Deferred for later delivery.

### 4.3 Real-Time Synchronization

Cadence uses Azure SignalR Service for sub-second synchronization across all connected users:

- Inject status changes propagate to all clients instantly
- Clock state (start, pause, resume) synchronizes across all participants
- Observations appear in real-time for the Exercise Director
- Participant join/leave events are visible to all

### 4.4 Offline Capability and Field Resilience

Cadence was architected from day one for environments with unreliable connectivity, directly addressing the operational reality that exercises frequently occur in Emergency Operations Centers, field locations, and disaster sites with poor network access.

**Offline Detection:** The platform continuously monitors connectivity through both browser Navigator.onLine status and SignalR connection state, providing clear visual indicators of connection status.

**Local Data Cache:** All active exercise data is cached locally in IndexedDB, enabling full read access to the MSEL, inject details, and exercise state regardless of connectivity.

**Offline Action Queue:** When connectivity is lost, user actions (firing injects, recording observations, updating inject status) are queued in a persistent FIFO queue stored in IndexedDB. The platform provides optimistic UI updates so users see their actions reflected immediately.

**Sync on Reconnect:** When connectivity is restored, queued actions are processed sequentially against the server with conflict detection. The platform uses last-write-wins for most operations and first-write-wins for inject firing (an inject cannot be fired twice).

**Zero Data Loss Guarantee:** The architecture ensures that no user action is lost during offline periods. All queued operations are persisted to survive browser crashes and are reconciled on reconnect with user notification of any conflicts.

### 4.5 Notifications

A real-time notification system keeps all participants informed during exercise conduct:

- **Toast Alerts:** Immediate on-screen notifications for high-priority events (inject ready, clock started/paused)
- **Notification Bell:** Persistent header icon with unread count and dropdown of recent notifications
- **Priority-Based Display:** High (requires action), Medium (awareness), Low (informational) with configurable auto-dismiss
- **Database Persistence:** Notifications persist across sessions and devices
- **Role-Targeted Delivery:** Controllers receive inject-ready alerts; Evaluators receive inject-fired alerts; Directors receive all exercise events

### 4.6 Field Operations

For operations-based exercises where participants are deployed in the field, Cadence provides enhanced capabilities:

**Photo Capture and Attachment:** Field participants capture photos during exercise conduct and attach them to observations with optional annotations. Photos are compressed client-side for bandwidth efficiency and queue for upload when offline.

**Enhanced Field Observations:** Fast, field-optimized observation capture with quick-add workflows, voice-to-text input via browser SpeechRecognition API, smart inject linking based on context, and a real-time observation feed for the Exercise Director.

**Location Tracking:** Opt-in GPS location sharing allows the Exercise Director to see a real-time map of team positions, identify coverage gaps (no evaluator near a fired inject's location), and redirect resources. Observations and inject firings are automatically geo-stamped. Location sharing auto-disables when the exercise clock stops.

**Safety Accountability:** Real-world safety concerns are flagged immediately with photo evidence and location data, providing the Safety Officer and Exercise Director with instant visibility into safety issues.

---

## 5. Evaluation and Observation

### 5.1 Ad-Hoc Observation Capture

Evaluators capture real-time observations during exercise conduct through a quick-entry interface optimized for speed during fast-paced exercises:

- **Observation Types:** Strength, Area for Improvement, or Neutral classification
- **P/S/M/U Rating:** Optional HSEEP performance rating on each observation
- **Inject Linkage:** Observations can be linked to one or more injects (many-to-many)
- **Objective Linkage:** Observations can be linked to exercise objectives (many-to-many)
- **Dual Timestamps:** Both exercise time (when observed) and wall clock time (when recorded)
- **Offline Support:** Observations persist through offline periods and sync when connected

### 5.2 Structured EEG Assessment

In addition to ad-hoc observations, Evaluators perform structured assessments against the Exercise Evaluation Guide:

- Select a Capability Target and Critical Task
- Record observation text describing what was observed
- Assign a mandatory P/S/M/U rating
- Optionally link to the triggering inject
- Timestamp is captured automatically

EEG entries are distinct from ad-hoc observations: they are structured assessments tied to specific Critical Tasks with mandatory ratings, feeding directly into HSEEP-compliant AAR documentation.

### 5.3 Evaluation Coverage Dashboard

The EEG Coverage Dashboard provides real-time visibility during and after conduct:

- **Overall Coverage:** Percentage of Critical Tasks with at least one EEG entry
- **Rating Distribution:** Visual breakdown of P/S/M/U ratings across the exercise
- **By Capability Target:** Per-target coverage showing evaluated vs. not-evaluated tasks with ratings
- **Gap Identification:** Explicit listing of Critical Tasks not yet evaluated, enabling Directors to redirect Evaluators

---

## 6. After-Action Review and Reporting

### 6.1 Review Mode

A dedicated post-conduct view optimized for After-Action Review (AAR) analysis rather than real-time action:

- **Phase-Grouped Timeline:** Injects organized by exercise phase with outcome summaries
- **Time Variance Analysis:** Scheduled vs. actual delivery times for each inject, highlighting early/late deliveries
- **Observation Review Panel:** All observations organized by linked inject and objective, with rating distributions
- **Exercise Statistics Dashboard:** Fire rate, timing accuracy, observation counts, evaluator coverage
- **Export Capabilities:** AAR-ready data export for report preparation

### 6.2 Metrics and Analytics

**Exercise-Level Metrics (Real-Time During Conduct):**

- Inject progress (fired, skipped, pending, deferred)
- On-time delivery rate and average timing variance
- Observation count and P/S/M/U distribution
- Evaluator coverage rate by objective
- Controller activity and workload distribution
- Exercise timeline with phase timing analysis
- Pause count and duration tracking

**Organization-Level Metrics (Cross-Exercise Analysis):**

- Total exercises conducted by type and status
- Performance trends (P/S/M/U distribution over time)
- Core Capability performance tracking across exercises
- Evaluator rating consistency analysis
- On-time inject rate trends
- Comparative analysis between exercises
- Benchmark comparison against sector standards
- Custom configurable metric dashboards

**Export:** All metrics support export to PDF, Excel, and image formats.

### 6.3 AAR Export

EEG-based AAR export organizes findings in the HSEEP-required structure:

**Capability --> Capability Target --> Critical Task --> Observations/EEG Entries**

Each entry includes the P/S/M/U rating, evaluator identity, timestamp, linked inject, and full observation text. This structure directly supports the HSEEP After-Action Report/Improvement Plan (AAR/IP) format.

Additionally, MSEL and observation data can be exported to Excel for stakeholders who need data outside the platform.

---

## 7. Multi-Tenancy and Organization Management

### 7.1 Organization as Security Boundary

Cadence implements a multi-tenant architecture with Organization as the primary security and data isolation boundary. All exercise data, user assignments, configurations, and audit trails are scoped to an organization. Data from one organization is never visible to or accessible by another organization.

### 7.2 Three-Tier Role Hierarchy

| Tier | Scope | Roles |
|------|-------|-------|
| **System** | Platform-wide | Admin, Manager, User |
| **Organization** | Per-organization | OrgAdmin, OrgManager, OrgUser |
| **Exercise** | Per-exercise | All 9 HSEEP roles |

A single user can belong to multiple organizations (supporting consultants and contractors who work across agencies) and hold different roles in each organization and each exercise. Users work within one organization context at a time, with a seamless organization switcher to change context.

### 7.3 Organization Features

- **Lifecycle Management:** Active, Archived, and Inactive states with appropriate access controls
- **User Management:** Invitation via email or shareable organization codes for self-service onboarding
- **Agency Lists:** Define participating agencies (Fire, EMS, Police, Public Health) and assign them to exercise participants, injects, and observations
- **Capability Libraries:** Select from pre-built capability frameworks (FEMA Core Capabilities, NATO, NIST CSF, ISO 22301) or define custom capabilities
- **Organization Settings:** Configurable defaults for session timeout, auto-save interval, exercise templates, and branding
- **Audit Trail:** Complete record of who performed what action in which organization

### 7.4 Data Isolation Architecture

- All domain entities (Exercises, Injects, Observations, EEG Entries) include an Organization ID foreign key
- Backend services automatically filter all queries by the current organization context
- A write-side validation interceptor ensures entities cannot be saved with an incorrect Organization ID
- JWT tokens include organization context (org_id, org_role) claims, validated on every API request

---

## 8. Authentication and Security

### 8.1 Authentication Architecture

Cadence implements a hybrid authentication architecture supporting multiple identity providers:

- **Local Credentials (Primary):** Email and password authentication with secure JWT token management
- **Azure Entra SSO:** Enterprise Single Sign-On integration for organizations using Microsoft identity
- **Account Linking:** Automatic association of external authentication accounts with local users by email

**Token Strategy:**

- 15-minute access tokens stored in memory (not localStorage) to prevent XSS attacks
- 4-hour refresh tokens in HttpOnly cookies to prevent JavaScript access
- Silent automatic token refresh without user intervention
- Session duration aligned with typical exercise conduct (4+ hours)

### 8.2 Authorization Model

- Server-side role validation on every API request
- Exercise-scoped permissions: a user's role can differ between exercises
- Role inheritance: users without an explicit exercise role inherit their global role
- Configurable permission matrices per organization for approval workflows

### 8.3 Security Features

- Failed login tracking and lockout after configurable attempts
- First-user bootstrap: the first registered user automatically becomes Administrator, eliminating deployment configuration
- Secure logout with token invalidation
- Database-level unique constraints preventing race conditions
- Soft-delete architecture preserving audit trails

---

## 9. Platform Experience

### 9.1 User Interface

Cadence provides a role-aware, context-adaptive user interface:

- **Role-Aware Dashboard:** Personalized landing page with quick actions and recent exercises tailored to user permissions
- **Context-Aware Navigation:** Sidebar transforms when entering an exercise, showing conduct-relevant options (MSEL, Inject Queue, Observations) with role-based visibility
- **Exercise Clock Integration:** Persistent clock display in the header during conduct showing elapsed time, status, and story time
- **Responsive Design:** Fully functional on desktop and tablet devices (1024px+ width) with touch-optimized interactions for field use

### 9.2 Session Resilience

The platform is designed for the extended, uninterrupted sessions required during exercise conduct:

- **Extended Timeout:** Configurable session timeout (default 4 hours) with warning before expiration
- **Auto-Save:** Automatic data persistence on field blur and configurable intervals, preventing data loss from accidental navigation or browser crashes
- **Keyboard Navigation:** Comprehensive keyboard shortcuts for rapid inject management, reducing the excessive clicking that characterizes many exercise management platforms
- **Multi-Tab Support:** Session sharing across browser tabs

### 9.3 Configurable Settings

A three-tier settings model mirrors organizational hierarchy:

- **User Preferences:** Time format, display density, notification preferences, keyboard shortcuts -- persistent across exercises
- **Exercise Settings:** Clock mode, auto-fire behavior, confirmation dialogs, skip reason requirements, observation required fields -- configured per exercise
- **Organization Defaults:** Session timeout, auto-save interval, default exercise templates, branding, core capability lists -- inherited by exercises unless overridden

Settings auto-save on change with reset-to-default options at every level.

### 9.4 Notifications and Alerts

The notification system ensures participants stay informed without constant page monitoring:

- Real-time toast messages for immediate alerts (inject ready, clock events)
- Persistent notification bell with unread count
- Priority-based display and auto-dismiss configuration
- Database-backed persistence across sessions
- Role-targeted delivery for relevant events

---

## 10. Technical Architecture

### 10.1 Platform Architecture

Cadence is built on a modern, scalable cloud-native architecture:

| Layer | Technology | Purpose |
|-------|------------|---------|
| Frontend | React 19, TypeScript 5, Material UI 7 | Responsive single-page application |
| Build System | Vite 7 | Fast development and production builds |
| Backend API | .NET 10, ASP.NET Core | RESTful API (always-warm, no cold starts) |
| ORM | Entity Framework Core 10 | Database access with migrations |
| Database | Azure SQL | Relational data storage |
| Real-Time | Azure SignalR Service | Sub-second synchronization |
| Background Jobs | Azure Functions | Timer-triggered maintenance tasks |
| Offline Storage | IndexedDB (Dexie.js) | Client-side data cache and action queue |

### 10.2 Architecture Decisions

**Always-Warm API:** The REST API runs on Azure App Service (not serverless Functions) to eliminate cold starts. Exercise conduct demands instant response times; Controllers cannot wait 5-10 seconds for a function to wake up when firing an inject during a live exercise. Azure Functions are used only for background timer tasks (data cleanup, sync retry) that run on schedule and can tolerate cold starts.

**Offline-First Design:** The entire offline architecture (IndexedDB cache, action queue, conflict resolution) was designed before the first feature was built, not bolted on as an afterthought. This ensures every feature works offline by default.

**Dual Time as Core Concept:** Dual-time tracking (wall clock + scenario time) is built into the data model, not layered on top. Every inject, observation, and EEG entry natively supports both time concepts.

### 10.3 Scalability and Multi-Tenancy

- Organization-scoped data isolation at the database query level
- Write-side interceptors ensuring data integrity across tenants
- JWT-based organization context on every request
- Designed for multiple concurrent organizations running independent exercises simultaneously

### 10.4 Cloud Deployment

Cadence deploys to Microsoft Azure with CI/CD pipelines:

- Automated deployment via GitHub Actions
- Separate frontend (Static Web App) and backend (App Service) deployment
- Infrastructure defined as code using Bicep templates
- Environment-specific configuration for development, staging, and production

---

## 11. Differentiating Capabilities Summary

| Capability | Description |
|------------|-------------|
| **Offline-First Architecture** | Full functionality during connectivity loss with zero data loss guarantee and automatic sync on reconnect. Designed for the reality of field exercises. |
| **Dual-Time Tracking** | Native support for both wall clock and scenario time on every inject, enabling multi-day scenario compression into shorter exercise windows. |
| **HSEEP EEG Implementation** | Structured evaluation chain from Objectives through Capability Targets, Critical Tasks, and EEG Entries with P/S/M/U ratings -- not just ad-hoc observation capture. |
| **Cross-Domain Framework Support** | Configurable status workflows for HSEEP, DoD/JTS, NATO, cybersecurity (NIST/MITRE), healthcare, financial, and international frameworks. |
| **Independent Clock and Status** | Exercise status and clock state operate independently, supporting complex operational patterns like administrative pauses while scenario time continues. |
| **Nine HSEEP Roles** | Complete implementation of all HSEEP-defined participant roles with exercise-scoped permissions, not just the basic management roles. |
| **Three-Tier Inject Approval** | Configurable approval workflows at organization, exercise, and inject levels with batch operations, approval queue, and Go-Live gate. |
| **Field Operations Suite** | Photo capture, voice-to-text observations, GPS location tracking, and real-time Director situational awareness for operations-based exercises. |
| **Multi-Tenant Data Isolation** | Organization-level security boundary with validated data isolation, supporting consultants and contractors across multiple agencies. |
| **Always-Warm API** | No cold starts during exercise conduct. The API is always responsive, critical for time-sensitive inject delivery. |

---

*Document prepared for business development use. All capabilities described represent the Cadence platform.*
