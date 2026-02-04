# Cadence Capability Catalog

> **A comprehensive guide to Cadence platform capabilities for emergency management professionals**

## About This Document

This document describes the capabilities available in Cadence, a HSEEP-compliant Master Scenario Events List (MSEL) management platform. It is written for Exercise Directors, Controllers, Evaluators, and other emergency management professionals who need to understand what Cadence can do—not how it's built.

For each capability area, you'll find:
- **What it enables** — the outcomes and workflows supported
- **Key capabilities** — specific functions available
- **Business value** — why it matters for exercise conduct
- **Story references** — links to detailed requirements for those who need them

---

## Platform Overview

Cadence focuses specifically on the **conduct phase** of HSEEP exercises—the operational period where Controllers deliver injects, Evaluators capture observations, and Exercise Directors maintain situational awareness. Unlike full-lifecycle planning tools, Cadence optimizes for:

- **Real-time inject delivery** during exercise execution
- **Offline operation** for field locations with unreliable connectivity
- **Dual time tracking** (scheduled time vs. scenario time)
- **Multi-organization support** for consultants and shared services
- **HSEEP compliance** with standard terminology and workflows

### Supported Exercise Types

| Type | Description |
|------|-------------|
| **Tabletop Exercise (TTX)** | Discussion-based exercises around a conference table |
| **Functional Exercise (FE)** | Operations-based exercises testing specific functions |
| **Full-Scale Exercise (FSE)** | Multi-agency exercises with actual resource deployment |
| **Computer-Aided Exercise (CAX)** | Exercises utilizing simulation systems |
| **Hybrid Exercises** | Combinations of the above formats |

---

## Table of Contents

1. [Access & Security](#1-access--security)
2. [Exercise Management](#2-exercise-management)
3. [MSEL & Inject Management](#3-msel--inject-management)
4. [Exercise Conduct](#4-exercise-conduct)
5. [User Experience](#5-user-experience)
6. [Reporting & Analysis](#6-reporting--analysis)
7. [Data Exchange](#7-data-exchange)
8. [Platform Features](#8-platform-features)

---

# 1. Access & Security

## 1.1 User Authentication

### Capability Overview

Cadence provides secure access control appropriate for emergency management exercises, which often involve sensitive scenario information and performance evaluations. Users authenticate with organization credentials and receive role-appropriate access to exercises and data.

The platform supports both local authentication (username/password) and organizational single sign-on through Azure Entra ID, allowing agencies to leverage existing identity infrastructure. Session management is optimized for exercise conduct—sessions remain active during long exercise days while maintaining security through automatic token refresh.

### Key Capabilities

- **Secure login** with username/password or organizational single sign-on
- **Extended sessions** (4 hours) designed for exercise day operations
- **Automatic session refresh** preventing mid-exercise logouts
- **Session timeout warnings** before automatic logout
- **Secure logout** clearing all cached credentials

### Business Value

Exercise conduct requires sustained access throughout exercise day, which can span 8-12 hours. Unlike enterprise applications with short session timeouts, Cadence maintains secure sessions throughout exercise operations while still protecting sensitive scenario and evaluation data.

### Related Stories
*See authentication stories S01-S25 in `docs/features/authentication/`*

---

## 1.2 Role-Based Access Control

### Capability Overview

Cadence implements HSEEP-defined roles with a three-tier permission model: platform-wide roles, organization roles, and exercise-specific roles. A user might be a Controller for one exercise but an Evaluator for another—permissions follow the exercise assignment, not a fixed global role.

This approach reflects how emergency management professionals actually work: consultants support multiple organizations, staff rotate through different exercise roles for training, and Exercise Directors need granular control over who can modify scenario content versus who can only observe.

### Key Capabilities

- **HSEEP exercise roles**: Exercise Director, Controller, Evaluator, Observer
- **Organization roles**: OrgAdmin, OrgManager, OrgUser for administrative functions
- **Per-exercise assignments**: Different roles for different exercises
- **Role inheritance**: Global defaults with exercise-specific overrides
- **Permission enforcement**: Read-only access for Observers, full control for Directors

### Business Value

Proper separation of duties is essential for exercise integrity. Evaluators should observe without ability to modify injects. Controllers need delivery authority but not administrative access. Cadence enforces these boundaries automatically based on role assignments.

### Related Stories
*See role stories S13-S15 in `docs/features/authentication/`*

---

## 1.3 Organization Management

### Capability Overview

Organizations serve as the primary security boundary in Cadence. All exercises, users, and configurations belong to an organization, ensuring complete data isolation between tenants. A consulting firm's exercises never mix with their client's data; federal agency scenarios remain separate from state/local exercises.

Users can belong to multiple organizations with different roles in each—essential for consultants, contractors, and mutual aid partners who support exercises across organizational boundaries. The organization switcher provides quick context changes without logging out.

### Key Capabilities

- **Complete data isolation** between organizations
- **Multi-organization membership** for consultants and contractors
- **Organization switching** without logout/login
- **Invitation system** for adding new members
- **Join codes** for streamlined onboarding
- **Agency management** for tracking participating organizations within exercises

### Business Value

Emergency management often involves complex organizational relationships. Cadence supports the reality that professionals work across organizational boundaries while maintaining strict data separation required for sensitive exercise content.

### Related Stories
*See organization stories OM-01 through OM-12 in `docs/features/organizations/`*

---

# 2. Exercise Management

## 2.1 Exercise Creation & Configuration

### Capability Overview

Exercise Directors create and configure exercises through a guided setup process. Each exercise includes essential metadata (name, type, dates, location), participant assignments, and timing configuration. Cadence automatically creates the initial MSEL when an exercise is created, eliminating manual setup steps.

Configuration options allow exercises to be tailored for different conduct modes—clock-driven exercises where injects fire automatically at scheduled times, or facilitator-paced exercises where Controllers manually advance the scenario. Time zone handling ensures distributed exercises display consistent times across locations.

### Key Capabilities

- **Create exercises** with type, dates, location, and description
- **Configure timing modes**: clock-driven or facilitator-paced
- **Assign participants** with appropriate exercise roles
- **Set time zones** for consistent display across locations
- **Enable practice mode** for training exercises excluded from metrics
- **Define exercise phases** (Initial Response, Sustained Ops, Demobilization)

### Business Value

Exercise setup often happens under time pressure with changing participant lists. Cadence streamlines configuration while enforcing HSEEP structure, ensuring exercises launch with proper role assignments and timing configuration.

### Related Stories
*See exercise stories S01-S05 in `docs/features/exercise-crud/` and configuration stories in `docs/features/exercise-config/`*

---

## 2.2 Exercise Lifecycle

### Capability Overview

Exercises progress through defined lifecycle states: Draft during planning, Active during conduct, Completed after execution, and Archived for historical records. Each state has appropriate edit restrictions—Active exercises allow inject delivery but prevent structural changes; Completed exercises become read-only for AAR documentation.

Exercise Directors control lifecycle transitions with built-in safeguards. An exercise cannot go Active with incomplete configuration. Completed exercises cannot accidentally revert to Active. Archived exercises can be restored if needed for reference or re-execution.

### Key Capabilities

- **Draft state** for full editing during planning
- **Active state** for exercise conduct with limited editing
- **Pause capability** for real-world interruptions (weather delays, safety issues)
- **Complete transition** locking exercise for AAR phase
- **Archive function** removing from active views while preserving history
- **Restore capability** bringing archived exercises back if needed

### Business Value

Exercise data represents significant planning investment. Lifecycle controls prevent accidental modification of historical records while supporting the reality that exercises sometimes need to pause, resume, or even restart.

### Related Stories
*See lifecycle stories S01-S07 in `docs/features/exercise-lifecycle/` and status stories in `docs/features/exercise-status/`*

---

## 2.3 Exercise Objectives

### Capability Overview

Exercises test specific capabilities defined through objectives. Cadence allows Exercise Directors to define objectives and link them to injects, creating traceability between scenario events and the capabilities being evaluated. This linkage supports HSEEP evaluation requirements and AAR analysis.

Objectives can reference standard capability frameworks (FEMA Core Capabilities, agency-specific frameworks) or custom definitions. During conduct, observations can be tagged to objectives, enabling capability-based performance analysis.

### Key Capabilities

- **Define exercise objectives** with descriptions and capability mappings
- **Link objectives to injects** showing which events test which capabilities
- **Tag observations to objectives** during evaluation
- **Analyze capability performance** based on linked observations
- **Support standard frameworks**: FEMA Core Capabilities, custom libraries

### Business Value

HSEEP requires exercises to test specific objectives with documented evaluation. The objective-inject-observation linkage provides the data structure needed for compliance while enabling meaningful AAR discussions about capability gaps.

### Related Stories
*See objective stories S01-S03 in `docs/features/exercise-objectives/`*

---

# 3. MSEL & Inject Management

## 3.1 Inject Authoring

### Capability Overview

The Master Scenario Events List (MSEL) contains all injects—the phone calls, emails, news reports, resource requests, and scenario developments that drive exercise play. Cadence provides comprehensive inject authoring with all HSEEP-required fields plus Cadence's signature dual-time tracking capability.

Dual time tracking separates "when to deliver" (scheduled time, wall clock) from "when it happens in the story" (scenario time). This allows multi-day disaster scenarios to be compressed into shorter exercise windows—Controllers deliver inject #47 at 10:30 AM exercise time, but the inject represents events occurring on "Day 3, 14:00" in the scenario narrative.

### Key Capabilities

- **Create injects** with title, description, delivery method, and recipient
- **Dual time tracking**: scheduled delivery time vs. scenario time
- **Delivery methods**: Phone, Email, Radio, In-Person, Video, Document
- **Assign Controllers** responsible for inject delivery
- **Link to objectives** for evaluation traceability
- **Assign to phases** for organizational grouping
- **Add expected actions** documenting anticipated player responses
- **Soft delete** preserving audit trail while removing from active views

### Business Value

MSELs often contain 100+ injects developed over months of planning. Cadence provides structured authoring that enforces HSEEP requirements while supporting the dual-time capability that enables realistic scenario compression.

### Related Stories
*See inject stories S01-S05 in `docs/features/inject-crud/`*

---

## 3.2 MSEL Organization

### Capability Overview

Large MSELs require organizational tools to manage complexity. Cadence provides filtering, searching, sorting, grouping, and reordering capabilities that help planners find specific injects and Controllers locate their assignments during conduct.

Views can be filtered by any combination of criteria—show only injects for a specific phase, delivery method, or assigned Controller. Grouping options organize the list by phase, time, or delivery method. Drag-and-drop reordering allows quick sequence adjustments during planning.

### Key Capabilities

- **Filter by criteria**: phase, status, delivery method, Controller, time range
- **Full-text search** across inject content
- **Sort options**: by time, sequence number, phase, status
- **Grouping views**: organize by phase, Controller, delivery method
- **Drag-and-drop reorder** for sequence adjustments
- **Save filter presets** for quick access to common views

### Business Value

Controllers managing 20+ injects during conduct need to quickly find "their" injects. Exercise Directors reviewing MSELs need to validate phase coverage. Filtering and organization tools transform unwieldy spreadsheets into manageable, role-appropriate views.

### Related Stories
*See filtering stories in `docs/features/inject-filtering/` and organization stories in `docs/features/inject-organization/`*

---

## 3.3 MSEL Version Control

### Capability Overview

MSEL development involves multiple revisions as scenarios evolve. Cadence supports MSEL versioning, allowing Exercise Directors to maintain a "Draft" version under development while a "Baseline" version remains locked for review. Only one version can be active during conduct.

Duplication enables creating new exercises from proven scenarios—duplicate last year's hurricane MSEL as a starting point, then modify for current year's objectives and participants.

### Key Capabilities

- **Multiple MSEL versions** per exercise
- **Version status**: Draft, Baseline, Active
- **Single active version** during conduct
- **Duplicate MSEL** for exercise reuse
- **Version comparison** showing changes between revisions

### Business Value

MSEL development is iterative, with stakeholder reviews driving revisions. Version control prevents "which version is current?" confusion while enabling exercise reuse that leverages previous planning investments.

### Related Stories
*See MSEL stories S01-S02 in `docs/features/msel-management/`*

---

## 3.4 Inject Approval Workflow

### Capability Overview

Organizations requiring formal quality control can enable inject approval workflow. When active, injects follow a structured path from Draft through Submitted to Approved before they can be delivered during conduct. This supports HSEEP compliance and separation of duties between inject authors and reviewers.

The workflow aligns with FEMA PrepToolkit's 8-status model (Draft, Submitted, Approved, Synchronized, Released, Complete, Deferred, Obsolete) while supporting simpler configurations for organizations with less formal requirements. A go-live gate prevents exercises from starting with unapproved injects.

### Key Capabilities

- **Organization-level policy**: Disabled, Optional, or Required approval
- **Exercise-level configuration**: Enable/disable per exercise
- **Submit for approval**: Authors request review of completed injects
- **Approve or reject**: Reviewers with appropriate roles process submissions
- **Rejection comments**: Feedback for authors on needed changes
- **Batch approval**: Process multiple injects efficiently
- **Approval queue**: Dedicated view for pending approvals
- **Go-live gate**: Block exercise activation until all injects approved
- **Separation of duties**: Users cannot approve their own submissions

### Business Value

Formal approval processes ensure scenario quality and appropriate oversight. For exercises involving sensitive scenarios or high-profile participants, approval workflow provides documented review chains that satisfy organizational governance requirements.

### Related Stories
*See approval stories S00-S10 in `docs/features/inject-approval-workflow/`*

---

# 4. Exercise Conduct

## 4.1 Inject Delivery

### Capability Overview

During exercise conduct, Controllers deliver injects to players according to the MSEL schedule. Cadence provides a conduct view optimized for real-time operations—showing upcoming injects, enabling quick delivery actions, and maintaining awareness of exercise progress.

Two conduct modes accommodate different exercise styles. Clock-driven mode automatically advances injects based on scheduled times, ideal for large exercises with strict timing. Facilitator-paced mode gives Controllers manual control over inject advancement, supporting adaptive exercises that respond to player actions.

### Key Capabilities

- **Conduct view** showing current and upcoming injects
- **Fire inject** action for delivery to players
- **Clock-driven mode**: automatic time-based advancement
- **Facilitator-paced mode**: manual Controller advancement
- **Inject status tracking**: Pending, Ready, Delivered, Skipped, Deferred
- **Skip with reason**: Document why inject was not delivered
- **Defer for later**: Postpone inject delivery with notes
- **Delivery confirmation**: Optional acknowledgment requirements

### Business Value

Exercise conduct moves fast. Controllers need instant access to their injects with one-click delivery actions. The conduct view eliminates spreadsheet scrolling and manual status tracking that slows traditional MSEL management.

### Related Stories
*See conduct stories in `docs/features/exercise-config/` (S04-S10)*

---

## 4.2 Observations & Evaluation

### Capability Overview

Evaluators capture observations during exercise conduct, documenting player performance against objectives. Cadence provides quick-capture tools designed for the fast pace of exercise operations—minimum required fields, optional linkages, and P/S/M/U rating selection.

Observations link to injects (what triggered the observation) and objectives (what capability was being tested), creating the data structure needed for After-Action Report analysis. The mobile-optimized interface supports Evaluators moving through exercise venues.

### Key Capabilities

- **Quick observation capture** with minimal required fields
- **P/S/M/U ratings**: Performed, Performed with Some difficulty, Performed with Major difficulty, Unable to perform
- **Link to injects**: Connect observation to triggering event
- **Link to objectives**: Connect observation to capability being tested
- **Free-text notes**: Detailed description of observed performance
- **Filter and search**: Find specific observations during AAR
- **Observation categories**: Strength, Area for Improvement, Neutral

### Business Value

Evaluator observations are the primary data source for After-Action Reports. Cadence's structured capture ensures observations include the linkages needed for capability-based analysis while keeping the interface fast enough for real-time use.

### Related Stories
*See observation stories S01-S08 in `docs/features/observations/`*

---

## 4.3 Real-Time Synchronization

### Capability Overview

Multi-user exercises require real-time data synchronization—when one Controller fires an inject, all users see the status change immediately. Cadence provides automatic synchronization across all connected devices, ensuring consistent exercise state without manual refresh.

Synchronization extends to all exercise data: inject status changes, new observations, participant updates, and exercise lifecycle transitions. Conflict resolution handles the rare cases where multiple users modify the same data simultaneously.

### Key Capabilities

- **Automatic sync** of all data changes across users
- **Instant status updates** when injects are fired
- **New observation notification** for Exercise Directors
- **Participant presence** showing who's online
- **Conflict resolution**: Last-write-wins for most data, first-write-wins for inject firing
- **Sync status indicators**: Visual confirmation of synchronization state

### Business Value

Exercise conduct requires shared situational awareness. When the lead Controller fires a critical inject, the Exercise Director and all other Controllers see the status change immediately—no refresh needed, no "did that go through?" uncertainty.

### Related Stories
*See connectivity stories S01, S05-S06 in `docs/features/connectivity/`*

---

## 4.4 Offline Capability

### Capability Overview

Emergency management exercises often occur in locations with poor or unreliable network connectivity—field locations, emergency operations centers with overloaded networks, or facilities without public internet. Cadence provides full offline operation, allowing users to continue working when connectivity is lost.

Offline changes queue locally and synchronize automatically when connectivity returns. The interface clearly indicates offline status and pending changes, preventing confusion about what data has been saved to the server.

### Key Capabilities

- **Full offline operation**: Continue working without network
- **Local data cache**: All exercise data available offline
- **Offline action queue**: Changes saved locally, synced on reconnect
- **Automatic reconnection**: Sync resumes without user action
- **Offline indicators**: Clear visual status of connectivity state
- **Pending change display**: See what hasn't synced yet
- **Conflict resolution**: Handle simultaneous offline edits

### Business Value

"The wifi in the EOC is terrible" is the #1 SME complaint about existing tools. Cadence's offline capability ensures exercise conduct continues regardless of connectivity, with automatic sync when networks recover.

### Related Stories
*See connectivity stories S02-S06 in `docs/features/connectivity/`*

---

# 5. User Experience

## 5.1 Role-Aware Interface

### Capability Overview

Cadence presents a role-appropriate interface based on user permissions. Exercise Directors see administrative functions and metrics; Controllers see delivery-focused views; Evaluators see observation capture tools; Observers see read-only displays. The interface adapts without overwhelming users with options they can't use.

The home page provides quick access to assigned exercises with role-appropriate actions. Recent exercises, upcoming responsibilities, and quick-action buttons reduce navigation overhead during busy exercise periods.

### Key Capabilities

- **Role-aware home page**: Content tailored to user's responsibilities
- **Quick actions**: One-click access to common tasks
- **Recent exercises**: Fast return to current work
- **Upcoming assignments**: See what's next on your schedule
- **Permission-appropriate menus**: Only show what user can access
- **Exercise context display**: Always know which exercise you're in

### Business Value

Users shouldn't wade through administrative options to find their assignments. Role-aware presentation reduces training requirements and speeds task completion by showing relevant options prominently.

### Related Stories
*See homepage stories S01-S02 in `docs/features/homepage/` and navigation stories in `docs/features/navigation/`*

---

## 5.2 My Assignments

### Capability Overview

The "My Assignments" view provides a consolidated list of all exercises where the user has an active role. Each assignment shows the exercise name, user's role, exercise status, and quick actions appropriate to that role. Users with multiple assignments can quickly navigate between exercises.

Assignment notifications alert users to new exercise invitations, upcoming conduct dates, and changes to their responsibilities.

### Key Capabilities

- **Assignment list**: All exercises with active user roles
- **Role display**: See your role for each exercise
- **Status indicators**: Draft, Active, Completed status visible
- **Quick navigation**: One-click to enter exercise context
- **New assignment alerts**: Notification of new invitations
- **Upcoming reminders**: Alerts for approaching exercise dates

### Business Value

Emergency management professionals often participate in multiple concurrent exercises. The assignment view provides a single place to see all responsibilities and quickly context-switch between exercises.

### Related Stories
*See assignment stories in `docs/features/my-assignments/`*

---

## 5.3 Notifications

### Capability Overview

Cadence provides contextual notifications for important events: new exercise assignments, inject assignments during conduct, observations requiring attention, and exercise status changes. Notifications appear as both in-app alerts and optional email summaries.

Notification preferences allow users to control what alerts they receive, reducing noise while ensuring critical updates aren't missed.

### Key Capabilities

- **In-app notification bell**: See pending notifications
- **Toast notifications**: Real-time alerts for important events
- **Email summaries**: Optional digest of notifications
- **Notification preferences**: Control what alerts you receive
- **Read/unread tracking**: See what you haven't reviewed
- **Notification history**: Review past alerts

### Business Value

Exercise coordination requires timely communication. Notifications ensure users learn about new assignments, inject responsibility changes, and exercise status updates without relying on external email or chat.

### Related Stories
*See notification stories in `docs/features/notifications/`*

---

## 5.4 Settings & Preferences

### Capability Overview

Users can customize their Cadence experience through personal preferences for display, time formats, and notification settings. Exercise Directors configure exercise-specific settings like timing mode and confirmation requirements. Organization administrators set defaults that apply across all organization exercises.

Settings cascade from organization defaults to exercise configuration to user preferences, providing consistent baselines with individual flexibility.

### Key Capabilities

**User Preferences:**
- Display preferences (theme, density)
- Time format (12/24 hour, timezone display)
- Notification preferences
- Keyboard shortcut customization
- Default MSEL view settings

**Exercise Settings:**
- Clock mode (clock-driven vs. facilitator-paced)
- Auto-fire configuration
- Confirmation dialog requirements
- Skip reason requirements
- Required observation fields

**Organization Defaults:**
- Default exercise templates
- Session timeout configuration
- Auto-save intervals
- Branding customization
- Core capability library selection

### Business Value

Different users have different preferences; different exercises have different requirements. Cascading settings provide appropriate defaults while allowing customization at each level.

### Related Stories
*See settings stories S01-S15 in `docs/features/settings/`*

---

# 6. Reporting & Analysis

## 6.1 Exercise Metrics

### Capability Overview

Cadence provides real-time metrics during conduct and comprehensive analysis after completion. Exercise Directors monitor progress through dashboards showing inject completion rates, observation counts, and timeline status. Post-exercise analysis supports AAR preparation with capability performance summaries.

Organization-level metrics track trends across exercises, demonstrating improvement over time and identifying persistent capability gaps requiring attention.

### Key Capabilities

**Exercise Metrics (Real-Time):**
- Inject progress (delivered / total)
- Phase completion status
- Observation count by category
- Timeline adherence
- Controller activity

**Exercise Metrics (Post-Conduct):**
- P/S/M/U distribution by objective
- Capability performance summary
- Evaluator coverage analysis
- Time variance analysis

**Organization Metrics:**
- Exercise history and trends
- Cross-exercise capability comparison
- Performance improvement tracking
- Benchmark comparisons (future)

### Business Value

Data-driven exercise programs require metrics. Real-time dashboards provide situational awareness during conduct; post-exercise analysis supports AAR discussions and improvement planning; organizational trends demonstrate program effectiveness.

### Related Stories
*See metrics stories S01-S14 in `docs/features/metrics/`*

---

## 6.2 Review Mode

### Capability Overview

After exercise completion, Review Mode provides an analysis-focused interface optimized for AAR preparation. Unlike the action-oriented conduct view, Review Mode groups events by phase, highlights outcomes, surfaces observation patterns, and supports collaborative review discussions.

Review Mode is read-only, preserving exercise data integrity while enabling detailed analysis of what happened and why.

### Key Capabilities

- **Phase-grouped timeline**: Events organized by exercise phase
- **Inject outcome summary**: Delivered, skipped, deferred with reasons
- **Observation review panel**: All observations with filtering
- **Statistics dashboard**: Exercise performance metrics
- **Export capabilities**: Generate reports for AAR distribution

### Business Value

The AAR is the primary deliverable from exercise conduct. Review Mode transforms raw exercise data into the organized views needed for productive AAR discussions and report preparation.

### Related Stories
*See review mode stories S20-S25 in `docs/features/review-mode/`*

---

## 6.3 Reports & Export

### Capability Overview

Cadence exports exercise data in formats suitable for documentation, sharing with stakeholders, and compliance requirements. Excel exports preserve formatting for those who need spreadsheet analysis. Report templates generate AAR-ready documents with exercise summaries, inject logs, and observation compilations.

Export options allow selective data inclusion based on audience—full technical exports for internal analysis, summary exports for stakeholder briefings.

### Key Capabilities

- **Excel MSEL export**: Full inject data with formatting preserved
- **Observation export**: All observations with linkages
- **Exercise summary report**: High-level overview document
- **Inject timeline export**: Chronological event log
- **Custom export templates** (future)

### Business Value

Exercise data must be shared with stakeholders who don't have Cadence access. Export capabilities ensure exercise documentation can be distributed, archived, and incorporated into broader AAR processes.

### Related Stories
*See export stories in `docs/features/reports/` and `docs/features/excel-export/`*

---

# 7. Data Exchange

## 7.1 Excel Import

### Capability Overview

Many organizations develop MSELs in Excel before transitioning to Cadence for conduct. The import capability maps spreadsheet columns to inject fields, validates data quality, and creates injects in bulk—eliminating manual re-entry of potentially hundreds of injects.

Import handles common Excel variations: different column names for the same data, varying date formats, and delivery method synonyms. Preview and validation steps ensure data quality before finalizing the import.

### Key Capabilities

- **Upload Excel files**: Standard .xlsx format support
- **Column mapping**: Match spreadsheet columns to inject fields
- **Auto-detect common mappings**: Recognize standard column names
- **Data validation**: Identify issues before import
- **Preview import**: Review what will be created
- **Delivery method matching**: Handle synonyms (e.g., "Phone Call" = "Phone")
- **Import log**: Document what was imported and any issues

### Business Value

MSEL development often starts in Excel—it's familiar, shareable, and doesn't require software procurement. Import capability bridges planning workflows to Cadence conduct without losing months of scenario development work.

### Related Stories
*See import stories S01-S04 in `docs/features/excel-import/`*

---

## 7.2 Excel Export

### Capability Overview

Cadence exports MSEL data back to Excel format for sharing with stakeholders, creating backups, or final formatting in familiar tools. Exports preserve data structure and can include a template export for organizations standardizing on specific Excel formats.

Export options allow filtering by criteria (phase, status, date range) and selecting which fields to include based on audience needs.

### Key Capabilities

- **Export full MSEL**: All injects with complete data
- **Filtered export**: Only selected injects based on criteria
- **Field selection**: Choose which columns to include
- **Template export**: Blank template for consistent MSEL authoring
- **Format preservation**: Maintain column widths, headers, formatting

### Business Value

Not everyone has Cadence access. Export capabilities ensure MSEL data can be shared with external stakeholders, archived to organizational document systems, and reformatted for specific distribution requirements.

### Related Stories
*See export stories S01-S02 in `docs/features/excel-export/`*

---

# 8. Platform Features

## 8.1 Session Management

### Capability Overview

Exercise conduct sessions can span 8-12 hours. Cadence session management balances security requirements with operational reality—sessions remain active during extended exercise days with automatic refresh, while still enforcing timeouts during periods of inactivity.

Session warnings alert users before automatic logout, preventing unexpected disconnections during critical exercise moments. Manual logout clears all cached data for security on shared devices.

### Key Capabilities

- **Extended session duration**: 4-hour timeout (vs. typical 30-minute enterprise default)
- **Automatic token refresh**: Sessions stay active during continuous use
- **Inactivity warning**: Alert before automatic logout
- **Manual logout**: Secure session termination
- **Multi-device support**: Sessions per device, not globally exclusive

### Business Value

Few things disrupt exercise conduct more than unexpected logouts forcing reauthentication during critical moments. Extended sessions with proper refresh eliminate this pain point while maintaining security.

### Related Stories
*See session story S01 in `docs/features/_cross-cutting/`*

---

## 8.2 Keyboard Navigation

### Capability Overview

Power users and accessibility requirements both benefit from comprehensive keyboard navigation. Cadence supports standard keyboard patterns for navigation, selection, and action execution. Keyboard shortcuts accelerate common operations for Controllers processing many injects.

Shortcut customization allows users to adjust bindings to match their preferences or accommodate assistive technology requirements.

### Key Capabilities

- **Full keyboard navigation**: Tab through all interactive elements
- **Keyboard shortcuts**: Quick access to common actions
- **Customizable bindings**: Adjust shortcuts to preferences
- **Accessibility compliance**: WCAG keyboard requirements met
- **Shortcut reference**: In-app documentation of available shortcuts

### Business Value

Controllers managing high-volume inject delivery need speed. Keyboard shortcuts eliminate mouse navigation overhead for common operations. Accessibility support ensures Cadence works with screen readers and alternative input devices.

### Related Stories
*See keyboard story S02 in `docs/features/_cross-cutting/`*

---

## 8.3 Auto-Save

### Capability Overview

Data loss from browser crashes, network interruptions, or accidental navigation destroys planning work. Cadence automatically saves changes as users work—on field blur, at regular intervals, and before navigation away from edited content.

Save status indicators show when changes are being saved and confirm successful persistence, eliminating uncertainty about data state.

### Key Capabilities

- **Auto-save on blur**: Save when leaving edited fields
- **Interval auto-save**: Regular saves during extended editing
- **Navigation protection**: Warn before leaving unsaved changes
- **Save status indicator**: Visual confirmation of save state
- **Offline save**: Queue changes when disconnected

### Business Value

MSEL authoring represents significant investment. Auto-save protects that investment from accidental loss, whether from browser issues, network problems, or simple user error.

### Related Stories
*See auto-save story S03 in `docs/features/_cross-cutting/`*

---

## 8.4 Responsive Design

### Capability Overview

Exercise conduct happens on various devices—desktop computers in the EOC, tablets carried by roving Evaluators, laptops at Controller stations. Cadence provides responsive interfaces that work across device sizes while maintaining usability for each context.

Core conduct operations are optimized for tablet use, supporting Evaluators who need to capture observations while moving through exercise venues.

### Key Capabilities

- **Desktop optimization**: Full-featured interface for planning and administration
- **Tablet support**: Touch-optimized conduct and observation interfaces
- **Minimum supported width**: 1024px (standard tablet landscape)
- **Adaptive layouts**: Interface adjusts to available screen space
- **Touch-friendly controls**: Appropriately sized targets for touch input

### Business Value

Evaluators can't be tethered to desktop computers during exercise conduct. Tablet support enables mobile observation capture while maintaining full functionality for desktop-based planning and administration.

### Related Stories
*See responsive story S04 in `docs/features/_cross-cutting/`*

---

## 8.5 Version Information

### Capability Overview

Cadence displays version information and release history, helping users understand what capabilities are available and what's changed in recent updates. "What's New" notifications highlight significant updates when users log in after a release.

Version information supports troubleshooting and ensures users and support staff can identify which version is running.

### Key Capabilities

- **Version display**: Current version visible in application
- **What's New notifications**: Highlight significant updates
- **Release history**: View past releases and changes
- **About page**: Detailed version and system information

### Business Value

Users need to know when new capabilities become available. Version notifications and release history keep users informed about platform evolution without requiring external communications.

### Related Stories
*See versioning stories S01-S02 in `docs/features/_cross-cutting/`*

---

# Appendix A: Capability Summary

| Category | Capabilities |
|----------|--------------|
| **Access & Security** | Authentication, RBAC, Organizations |
| **Exercise Management** | CRUD, Lifecycle, Objectives, Phases, Configuration |
| **MSEL & Injects** | Authoring, Organization, Versioning, Approval Workflow |
| **Conduct** | Delivery, Observations, Real-Time Sync, Offline |
| **User Experience** | Role-Aware UI, Assignments, Notifications, Settings |
| **Reporting** | Metrics, Review Mode, Reports |
| **Data Exchange** | Excel Import, Excel Export |
| **Platform** | Sessions, Keyboard, Auto-Save, Responsive, Versioning |

---

# Appendix B: Story Reference Index

For detailed requirements, user stories are organized by feature area in `docs/features/`:

| Feature Area | Story Location |
|--------------|----------------|
| Authentication | `docs/features/authentication/` |
| Organizations | `docs/features/organizations/` |
| Exercise CRUD | `docs/features/exercise-crud/` |
| Exercise Lifecycle | `docs/features/exercise-lifecycle/` |
| Exercise Status | `docs/features/exercise-status/` |
| Exercise Config | `docs/features/exercise-config/` |
| Exercise Phases | `docs/features/exercise-phases/` |
| Exercise Objectives | `docs/features/exercise-objectives/` |
| Inject CRUD | `docs/features/inject-crud/` |
| Inject Filtering | `docs/features/inject-filtering/` |
| Inject Organization | `docs/features/inject-organization/` |
| MSEL Management | `docs/features/msel-management/` |
| Inject Approval | `docs/features/inject-approval-workflow/` |
| Observations | `docs/features/observations/` |
| Capabilities | `docs/features/capabilities/` |
| Connectivity | `docs/features/connectivity/` |
| Homepage | `docs/features/homepage/` |
| Navigation | `docs/features/navigation/` |
| Notifications | `docs/features/notifications/` |
| Settings | `docs/features/settings/` |
| Metrics | `docs/features/metrics/` |
| Progress Dashboard | `docs/features/progress-dashboard/` |
| Reports | `docs/features/reports/` |
| Review Mode | `docs/features/review-mode/` |
| Excel Import | `docs/features/excel-import/` |
| Excel Export | `docs/features/excel-export/` |
| Cross-Cutting | `docs/features/_cross-cutting/` |

---

*Document generated: 2026-02-03*
*Based on Cadence Master_Features.md v1.0*
