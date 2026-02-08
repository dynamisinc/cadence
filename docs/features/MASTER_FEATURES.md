# Cadence Feature Catalog

> **Master reference document for all Cadence platform features**
> **Generated:** 2026-02-03
> **Total Features:** 31

---

## Table of Contents

1. [Foundation](#1-foundation)
   - [1.1 Authentication & Authorization](#11-authentication--authorization)
   - [1.2 Organization Management](#12-organization-management)
   - [1.3 Bulk Participant Import](#13-bulk-participant-import)
   - [1.4 Core Domain Entities](#14-core-domain-entities)

2. [Exercise Management](#2-exercise-management)
   - [2.1 Exercise CRUD](#21-exercise-crud)
   - [2.2 Exercise Lifecycle](#22-exercise-lifecycle)
   - [2.3 Exercise Status Workflow](#23-exercise-status-workflow)
   - [2.4 Exercise Configuration](#24-exercise-configuration)
   - [2.5 Exercise Phases](#25-exercise-phases)
   - [2.6 Exercise Objectives](#26-exercise-objectives)

3. [MSEL & Inject Management](#3-msel--inject-management)
   - [3.1 Inject CRUD](#31-inject-crud)
   - [3.2 Inject Filtering](#32-inject-filtering)
   - [3.3 Inject Organization](#33-inject-organization)
   - [3.4 MSEL Management](#34-msel-management)
   - [3.5 Inject Approval Workflow](#35-inject-approval-workflow)

4. [Exercise Conduct](#4-exercise-conduct)
   - [4.1 Exercise Observations](#41-exercise-observations)
   - [4.2 Exercise Capabilities](#42-exercise-capabilities)
   - [4.3 Connectivity (Real-Time & Offline)](#43-connectivity-real-time--offline)

5. [User Experience](#5-user-experience)
   - [5.1 Homepage](#51-homepage)
   - [5.2 Navigation Shell](#52-navigation-shell)
   - [5.3 My Assignments](#53-my-assignments)
   - [5.4 Notifications](#54-notifications)
   - [5.5 Settings](#55-settings)

6. [Reporting & Analysis](#6-reporting--analysis)
   - [6.1 Metrics](#61-metrics)
   - [6.2 Progress Dashboard](#62-progress-dashboard)
   - [6.3 Reports](#63-reports)
   - [6.4 Review Mode](#64-review-mode)

7. [Data Exchange](#7-data-exchange)
   - [7.1 Excel Import](#71-excel-import)
   - [7.2 Excel Export](#72-excel-export)

8. [Cross-Cutting Concerns](#8-cross-cutting-concerns)
   - [8.1 Versioning](#81-versioning)
   - [8.2 Cross-Cutting Technical Features](#82-cross-cutting-technical-features)

---

# 1. Foundation

## 1.1 Authentication & Authorization

**Phase:** MVP | **Status:** In Progress | **Stories:** 25

### Overview
Cadence users can securely authenticate using local credentials (MVP) or their organization's Azure Entra identity (future), with role-based access control enforced at both the application and exercise level.

### Problem Statement
Emergency management exercises require secure access control to protect sensitive scenario data and evaluations. Users need different permissions for different exercises - a person might be a Controller for one exercise but an Evaluator for another.

### User Stories

| Story | Title | Priority |
|-------|-------|----------|
| S01 | Registration Form | P0 |
| S02 | Validate and Save User | P0 |
| S03 | First User Becomes Admin | P0 |
| S04 | Login Form | P0 |
| S05 | JWT Token Issuance | P0 |
| S06 | Failed Login Handling | P0 |
| S07 | Automatic Token Refresh | P0 |
| S08 | Token Expiration Handling | P0 |
| S09 | Secure Logout | P0 |
| S10 | View User List | P0 |
| S11 | Edit User Details | P0 |
| S12 | Deactivate User | P1 |
| S13 | Global Role Assignment | P0 |
| S14 | Exercise Role Assignment | P0 |
| S15 | Role Inheritance | P0 |
| S16 | Auth Service Interface | P1 |
| S17 | Identity Provider Implementation | P0 |
| S18 | Entra Provider Implementation | P2 |
| S19 | User Account Linking | P2 |
| S20 | Initiate External Login | P2 |
| S21 | OAuth Callback Handling | P2 |
| S22 | Entra Admin Configuration | P2 |
| S23 | External Auth Error Handling | P2 |
| S24 | Password Reset | P2 |
| S25 | Inline User Creation | P1 |

### Key Concepts

| Term | Definition |
|------|------------|
| **Access Token** | Short-lived JWT (15 minutes) for API authentication |
| **Refresh Token** | Long-lived token (4 hours) in HttpOnly cookie |
| **Global Role** | User's default role across the platform |
| **Exercise Role** | Role assigned for a specific exercise, can override global |
| **First User Bootstrap** | First registered user becomes Administrator |

---

## 1.2 Organization Management

**Phase:** MVP | **Status:** In Progress | **Stories:** 12

### Overview
Organizations are the primary security boundary in Cadence. All exercise data, users, and configurations are scoped to an organization. Users can belong to multiple organizations with different roles in each.

### Problem Statement
Emergency management platforms need robust multi-tenancy to ensure data isolation between organizations (e.g., CISA data never leaks to commercial clients), while supporting flexibility for consultants who work across multiple organizations.

### User Stories

| Story | Title | Priority |
|-------|-------|----------|
| OM-01 | Organization List | P0 |
| OM-02 | Create Organization | P0 |
| OM-03 | Edit Organization | P0 |
| OM-04 | Organization Lifecycle | P0 |
| OM-05 | User-Organization Assignment | P0 |
| OM-06 | Organization Switcher | P0 |
| OM-07 | Invite User to Organization | P1 |
| OM-08 | Join via Organization Code | P1 |
| OM-09 | Agency List Management | P1 |
| OM-10 | Agency Assignment | P1 |
| OM-11 | Capability Library Selection | P2 |
| OM-12 | Organization Settings | P2 |

### Key Concepts

| Term | Definition |
|------|------------|
| **Organization** | Tenant boundary containing users, exercises, and configurations |
| **Current Organization** | Active organization context for all user operations |
| **Organization Membership** | User's association with an organization and their OrgRole |
| **Three-Tier Roles** | System Level → Organization Level → Exercise Level |

---

## 1.3 Bulk Participant Import

**Phase:** Standard | **Status:** Not Started | **Stories:** 6

### Overview
Exercise Directors can upload CSV or Excel files containing participant information to add multiple participants to an exercise at once. The system classifies each row based on the participant's current relationship with the platform—assigning existing org members immediately, and sending organization invitations with pending exercise assignments for new participants.

### Problem Statement
Full-Scale Exercises (FSE) and multi-agency Functional Exercises (FE) often involve 50-500+ participants from multiple agencies. Exercise Directors typically receive participant lists as spreadsheets from partner agencies. Today, each participant must be individually invited to the organization and then separately assigned to the exercise with a specific HSEEP role. For large exercises, this manual process is prohibitively slow and error-prone.

### User Stories

| Story | Title | Priority |
|-------|-------|----------|
| S01 | Upload Participant File | P1 |
| S02 | Preview and Validate Import | P1 |
| S03 | Process Existing Organization Members | P1 |
| S04 | Invite Non-Members via Bulk Upload | P1 |
| S05 | View Upload Results and Status | P2 |
| S06 | Download Participant Template | P2 |

### Key Concepts

| Term | Definition |
|------|------------|
| **Participant Classification** | System determination of each row's scenario: Assign (existing member), Update (already in exercise), Invite (needs org invitation), or Error |
| **Pending Exercise Assignment** | A deferred exercise role that activates automatically when the participant accepts their organization invitation |
| **Preview** | Validation step showing what will happen for each row before any changes are committed |
| **Bulk Import Record** | Audit trail of each import operation with summary counts and per-row results |

### Participant Classification Scenarios

| Scenario | Condition | Action |
|----------|-----------|--------|
| **Assign** | Existing org member, not in exercise | Create ExerciseParticipant immediately |
| **Update** | Already assigned to exercise | Update role or skip if unchanged |
| **Invite** | Not an org member (or no account) | Create OrgInvite + pending exercise assignment |
| **Error** | Invalid data | Flag with reason, skip processing |

### Dependencies

- exercise-config/S02: Assign Participants (existing single-participant assignment)
- organization-management/OM-07: Organization Invitations (email invitation system)
- email-communications/EM-02: Invitation Emails (email delivery)
- excel-import/S01-S03: Excel Import (file parsing patterns)

---

## 1.4 Core Domain Entities

**Phase:** Foundation | **Status:** In Progress

### Overview
Core domain entities define the fundamental data structures and business rules that underpin the entire Cadence platform.

### Entity Documentation

| Entity | Description |
|--------|-------------|
| Exercise | Top-level container for MSEL, participants, and settings |
| Inject | Individual events that drive exercise scenarios |
| User Roles | Role definitions and permission matrices |

### Domain Rules

**Exercise Rules:**
- An exercise must have at least one MSEL version
- Only one MSEL version can be "Active" at a time
- Archived exercises are read-only

**Inject Rules:**
- Inject numbers are unique within a MSEL
- Scheduled Time is required; Scenario Time is optional
- Deleted injects are soft-deleted (archived, not removed)

---

# 2. Exercise Management

## 2.1 Exercise CRUD

**Phase:** MVP | **Status:** In Progress | **Stories:** 5

### Overview
Core exercise lifecycle management allowing users to create, view, edit, and archive exercises. This feature provides the foundation for all exercise-related functionality.

### Problem Statement
Emergency management professionals conduct multiple exercises throughout the year. They need a centralized system to create new exercises, view upcoming and past exercises, update exercise details as planning evolves, and archive completed exercises.

### User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| S01 | Create Exercise | P0 | ✅ Complete |
| S02 | Edit Exercise | P0 | ✅ Complete |
| S03 | View Exercise List | P0 | ✅ Complete |
| S04 | Archive Exercise | P1 | 📋 Ready |
| S05 | Practice Mode | P1 | 📋 Ready |

### Key Concepts

| Term | Definition |
|------|------------|
| **Exercise** | Planned event to test emergency response capabilities |
| **MSEL** | Master Scenario Events List - automatically created with exercise |
| **Archive** | Soft-delete operation making exercise read-only but viewable |
| **Practice Mode** | Exercise mode for training without affecting production metrics |

---

## 2.2 Exercise Lifecycle

**Phase:** MVP | **Status:** Not Started | **Stories:** 7

### Overview
Exercise Directors and Administrators can efficiently manage exercise lifecycle—archiving completed or abandoned exercises to declutter active views, and permanently deleting exercises when appropriate.

### Problem Statement
Without lifecycle management, users cannot quickly hide exercises from active views, remove draft exercises created by mistake, or permanently delete obsolete data after appropriate review.

### User Stories

| Story | Title | Priority |
|-------|-------|----------|
| S01 | Lifecycle Tracking Fields | P0 |
| S02 | Archive Exercise | P0 |
| S03 | View Archived Exercises | P0 |
| S04 | Restore Exercise | P0 |
| S05 | Delete Draft Exercise | P0 |
| S06 | Permanently Delete Archived Exercise | P1 |
| S07 | Admin Archive Management Page | P1 |

### Exercise Lifecycle States

```
Draft → Published → Active → Completed
  ↓         ↓         ↓         ↓
  └─────────────────────────────┘
              ↓
          Archived
              ↓
    Permanently Deleted
```

---

## 2.3 Exercise Status Workflow

**Phase:** MVP | **Status:** Not Started | **Stories:** 6

### Overview
HSEEP-compliant lifecycle management for exercises from initial planning through completion and archival, supporting both planned transitions and real-world scenarios requiring pause and resume.

### Problem Statement
Exercise Directors and Controllers need clear, enforceable status transitions that prevent accidental data loss and ensure exercises progress through proper phases.

### User Stories

| Story | Title | Priority |
|-------|-------|----------|
| S01 | View Exercise Status | P0 |
| S02 | Activate Exercise (Draft → Active) | P0 |
| S03 | Pause Exercise (Active → Paused) | P1 |
| S04 | Complete Exercise | P0 |
| S05 | Revert to Draft | P1 |
| S06 | Archive Exercise | P0 |

### Status Definitions

| Status | Description | Editable? |
|--------|-------------|-----------|
| **Draft** | Initial planning state | Yes (full edit) |
| **Active** | Exercise conduct in progress | Limited |
| **Paused** | Temporarily suspended | Limited |
| **Completed** | Conduct finished, AAR phase | Read-only |
| **Archived** | Historical record | Read-only |

---

## 2.4 Exercise Configuration

**Phase:** MVP | **Status:** In Progress | **Stories:** 13

### Overview
Exercise Configuration provides the settings and assignments needed to prepare an exercise for conduct, including role configuration, participant assignment, timing modes, and operational settings.

### Problem Statement
Before an exercise can begin, Exercise Directors need to configure critical settings like participant roles, time zones, and timing modes. Without structured configuration, exercises may launch with incomplete setup.

### User Stories

**Core Configuration:**

| Story | Title | Priority |
|-------|-------|----------|
| S01 | Configure Exercise Roles | P1 |
| S02 | Assign Participants | P1 |
| S03 | Configure Time Zone | P1 |

**Clock Modes & Timing:**

| Story | Title | Priority |
|-------|-------|----------|
| S01-timing | Timing Configuration Fields | P1 |
| S02-delivery | Inject DeliveryTime Field | P1 |
| S03-ui | Timing Configuration UI | P1 |
| S04-ready | Inject "Ready" Status | P1 |
| S05-auto | Auto-Ready Injects | P1 |
| S06-clock | Clock-Driven Conduct View | P1 |
| S07-facilitator | Facilitator-Paced Conduct View | P1 |
| S08-story | Story Time Display | P2 |
| S09-confirm | Fire Confirmation Dialog | P2 |
| S10-sequence | Sequence Drag-Drop Reorder | P2 |

---

## 2.5 Exercise Phases

**Phase:** MVP | **Status:** Not Started | **Stories:** 2

### Overview
Phases allow dividing a MSEL into logical segments (e.g., Initial Response, Sustained Operations, Demobilization) for organization and sequencing.

### User Stories

| Story | Title | Priority |
|-------|-------|----------|
| S01 | Define Phases | P1 |
| S02 | Assign Inject to Phase | P1 |

---

## 2.6 Exercise Objectives

**Phase:** Standard | **Status:** Not Started | **Stories:** 3

### Overview
Objectives define what capabilities or performance areas an exercise is designed to test. Injects can be linked to objectives to track objective coverage.

### User Stories

| Story | Title | Priority |
|-------|-------|----------|
| S01 | Create Objective | P1 |
| S02 | Edit Objective | P1 |
| S03 | Link Objective to Inject | P1 |

---

# 3. MSEL & Inject Management

## 3.1 Inject CRUD

**Phase:** MVP | **Status:** In Progress | **Stories:** 5

### Overview
Injects are the core content of a MSEL - the events, messages, and scenarios delivered during exercise conduct. This feature covers create, read, update, and delete operations including Cadence's dual-time tracking capability.

### Problem Statement
Exercise planners need to build MSELs containing dozens or hundreds of injects - phone calls, emails, news reports, and resource requests that drive the exercise scenario. Cadence's dual-time tracking allows multi-day scenarios to be compressed into shorter exercise windows.

### User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| S01 | Create Inject | P0 | ✅ Complete |
| S02 | Edit Inject | P0 | ✅ Complete |
| S03 | View Inject Detail | P0 | ✅ Complete |
| S04 | Delete Inject | P1 | 📋 Ready |
| S05 | Dual Time Tracking | P0 | ✅ Complete |

### Key Concepts: Dual Time Tracking

| Time Type | Purpose | Example |
|-----------|---------|---------|
| **Scheduled Time** | When to deliver (wall clock) | "10:30 AM EST" |
| **Scenario Time** | When it happens in the story | "Day 2, 14:00" |

---

## 3.2 Inject Filtering

**Phase:** Standard | **Status:** Not Started | **Stories:** 2

### Overview
Users can filter and search injects in the MSEL by various criteria to find specific items quickly.

### User Stories

| Story | Title | Priority |
|-------|-------|----------|
| S01 | Filter Injects | P1 |
| S02 | Search Injects | P1 |

---

## 3.3 Inject Organization

**Phase:** Standard | **Status:** Not Started | **Stories:** 3

### Overview
Users can sort, group, and reorder injects in the MSEL view for better organization and readability.

### User Stories

| Story | Title | Priority |
|-------|-------|----------|
| S01 | Sort Injects | P1 |
| S02 | Group Injects | P1 |
| S03 | Reorder Injects | P1 |

---

## 3.4 MSEL Management

**Phase:** MVP | **Status:** Ready | **Stories:** 2

### Overview
MSEL Management enables version control for Master Scenario Events Lists, allowing Exercise Directors to maintain multiple MSEL versions.

### User Stories

| Story | Title | Priority |
|-------|-------|----------|
| S01 | Select MSEL Version | P1 |
| S02 | Duplicate MSEL | P1 |

---

## 3.5 Inject Approval Workflow

**Phase:** MVP | **Status:** Not Started | **Stories:** 11 | **Points:** 43

### Overview
Organizations can require formal approval of injects before exercise conduct. When enabled, injects follow a Draft → Submitted → Approved workflow, and exercises cannot go live until all injects are approved. This supports HSEEP compliance and organizational governance requirements per FEMA PrepToolkit standards.

### Problem Statement
Emergency management exercises require quality control and oversight to ensure all scenario content is reviewed before delivery to players. FEMA PrepToolkit defines an 8-status inject workflow (Draft, Submitted, Approved, Synchronized, Released, Complete, Deferred, Obsolete) that organizations expect Cadence to support. Without formal approval workflow, organizations cannot enforce separation of duties between inject authors and reviewers.

### User Stories

| Story | Title | Priority | Points |
|-------|-------|----------|--------|
| S00 | HSEEP Inject Status Enum | P0 | 3 |
| S01 | Organization Approval Configuration | P1 | 3 |
| S02 | Exercise Approval Configuration | P1 | 3 |
| S03 | Submit Inject for Approval | P0 | 3 |
| S04 | Approve or Reject Inject | P0 | 5 |
| S05 | Batch Approval Actions | P1 | 5 |
| S06 | Approval Queue View | P1 | 3 |
| S07 | Exercise Go-Live Gate | P0 | 3 |
| S08 | Approval Notifications | P2 | 5 |
| S09 | Revert Approval Status | P1 | 2 |
| S10 | Configurable Status Workflow | P2 | 8 |

### Key Concepts

| Term | Definition |
|------|------------|
| **HSEEP Status Workflow** | FEMA PrepToolkit 8-status model: Draft → Submitted → Approved → Synchronized → Released → Complete (with Deferred/Obsolete branches) |
| **Approval Policy** | Organization-level setting: Disabled, Optional, or Required |
| **Go-Live Gate** | Validation that prevents publishing exercises with unapproved injects |
| **Separation of Duties** | Users cannot approve their own submissions |
| **Framework Templates** | Pre-built workflows for DoD/NATO, Cybersecurity, Healthcare, Financial sectors |

### Configuration Model

Three-tier configuration system:
- **Organization Level:** Sets default policy (Disabled/Optional/Required)
- **Exercise Level:** Directors enable/disable per exercise (constrained by org policy)
- **Inject Level:** Status workflow enforced based on exercise setting

### Cross-Domain Support

While HSEEP is the default for U.S. civilian emergency management, the feature supports configurable status workflows for:
- DoD/Joint Training System (JMSEL, Key/Enabling/Supporting injects)
- NATO (STARTEX/ENDEX, EXCON/DISTAFF roles)
- UK Cabinet Office (Main Events List, Blue/Red/Green/White Cells)
- Australian AIIMS (Special Ideas, DISCEX/Functional/Field)
- Cybersecurity (NIST/MITRE ATT&CK mapping, Red/Blue/Purple teams)
- Healthcare (CMS/Joint Commission, HICS roles, surge levels)
- Financial (FFIEC/FINRA, RTO/RPO/MTD metrics)
- ISO 22301/BCI (BCMS validation terminology)

### Dependencies

- **Requires:** Phase D (Exercise Conduct) for status workflow integration
- **Blocks:** Phase E (Observations) - evaluators need stable inject status
- **Related:** Exercise Status Workflow (Draft → Published gate)

---

# 4. Exercise Conduct

## 4.1 Exercise Observations

**Phase:** MVP | **Status:** Complete | **Stories:** 8

### Overview
Evaluators capture observations during exercise conduct, recording player performance against objectives using HSEEP P/S/M/U ratings.

### Problem Statement
Evaluators need a fast, mobile-friendly way to capture observations during fast-paced exercises. Observations must link to objectives and injects for AAR analysis.

### User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| S01 | Create Observation | P0 | ✅ Complete |
| S02 | Edit Observation | P0 | ✅ Complete |
| S03 | Delete Observation | P1 | ✅ Complete |
| S04 | Link Observation to Inject | P0 | ✅ Complete |
| S05 | Link Observation to Objective | P1 | ✅ Complete |
| S06 | P/S/M/U Rating | P0 | ✅ Complete |
| S07 | View Observations List | P0 | ✅ Complete |
| S08 | Filter Observations | P1 | ✅ Complete |

### Key Concepts: P/S/M/U Ratings

| Rating | Meaning |
|--------|---------|
| **P** | Performed without challenges |
| **S** | Performed with some difficulty |
| **M** | Performed with major difficulty |
| **U** | Unable to perform |

---

## 4.2 Exercise Capabilities

**Phase:** Standard | **Status:** Not Started | **Stories:** 6

### Overview
Organizations can maintain libraries of Core Capabilities (FEMA, NATO, NIST, or custom) and assign target capabilities to exercises for structured evaluation.

### User Stories

| Story | Title | Priority |
|-------|-------|----------|
| S01 | Capability Entity & API | P1 |
| S02 | Capability Library Admin UI | P1 |
| S03 | Import Predefined Libraries | P1 |
| S04 | Exercise Target Capabilities | P1 |
| S05 | Observation Capability Tagging | P1 |
| S06 | Capability Performance Metrics | P2 |

---

## 4.3 Connectivity (Real-Time & Offline)

**Phase:** MVP | **Status:** Complete | **Stories:** 6

### Overview
Enable multi-user real-time synchronization and offline capability for field use — addressing the #1 SME pain point: "The wifi in the EOC is terrible."

### Problem Statement
Emergency management exercises often occur in locations with poor or unreliable network connectivity. Users need real-time updates when connected and the ability to continue working offline.

### User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| S01 | Real-Time Data Sync | P0 | ✅ Complete |
| S02 | Offline Detection & Indicators | P0 | ✅ Complete |
| S03 | Local Data Cache | P0 | ✅ Complete |
| S04 | Offline Action Queue | P0 | ✅ Complete |
| S05 | Sync on Reconnect | P0 | ✅ Complete |
| S06 | Conflict Resolution | P1 | ✅ Complete |

### Key Concepts

| Term | Definition |
|------|------------|
| **Real-Time Sync** | Automatic data synchronization via SignalR |
| **Offline Mode** | Local-only operation when connectivity is lost |
| **Action Queue** | FIFO queue of offline operations, synced on reconnect |
| **Conflict Resolution** | Last-write-wins (most cases) or first-write-wins (inject firing) |

---

# 5. User Experience

## 5.1 Homepage

**Phase:** MVP | **Status:** Complete | **Stories:** 2

### Overview
The Home Page serves as the primary landing page providing a role-aware welcome experience, quick actions based on user permissions, and immediate access to recent exercises.

### User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| S01 | View Homepage with Role-Aware Content | P0 | ✅ Complete |
| S02 | Quick Actions Based on Permissions | P0 | ✅ Complete |

---

## 5.2 Navigation Shell

**Phase:** MVP | **Status:** Ready | **Stories:** 4

### Overview
The Navigation Shell provides consistent navigation throughout the application with role-aware menu visibility and exercise context display.

### User Stories

| Story | Title | Priority |
|-------|-------|----------|
| S01 | Updated Sidebar Menu | P0 |
| S02 | Role-Based Menu Visibility | P0 |
| S03 | In-Exercise Context Navigation | P1 |
| S04 | Exercise Header with Clock | P1 |

---

## 5.3 My Assignments

**Phase:** MVP | **Status:** Ready | **Stories:** 2

### Overview
Users can quickly see all exercises they're assigned to, their role in each, and upcoming responsibilities.

### User Stories

| Story | Title | Priority |
|-------|-------|----------|
| S01 | My Assignments View | P0 |
| S03 | Role-Based Landing | P1 |

---

## 5.4 Notifications

**Phase:** MVP | **Status:** Ready | **Stories:** 2

### Overview
Users receive notifications for important events like inject assignments, exercise status changes, and observations requiring attention.

### User Stories

| Story | Title | Priority |
|-------|-------|----------|
| S01 | Notification Bell | P1 |
| S02 | Notification Toasts | P1 |

---

## 5.5 Settings

**Phase:** MVP/Standard | **Status:** Not Started | **Stories:** 15

### Overview
Settings provides user preferences, exercise configuration, and organization defaults at three levels: User, Exercise, and Organization.

### User Stories

**User Settings:**

| Story | Title | Priority |
|-------|-------|----------|
| S01 | User Display Preferences | P1 |
| S02 | User Time Format | P1 |
| S06 | User Notification Preferences | P1 |
| S07 | User Keyboard Shortcuts | P2 |
| S15 | User Default MSEL View | P2 |

**Exercise Settings:**

| Story | Title | Priority |
|-------|-------|----------|
| S03 | Exercise Clock Mode | P1 |
| S04 | Exercise Auto-Fire | P2 |
| S05 | Exercise Confirmation Dialogs | P1 |
| S08 | Exercise Skip Reason Requirement | P2 |
| S09 | Exercise Observation Required Fields | P2 |

**Organization Settings:**

| Story | Title | Priority |
|-------|-------|----------|
| S10 | Org Default Exercise Template | P2 |
| S11 | Org Session Timeout | P1 |
| S12 | Org Auto-Save Interval | P2 |
| S13 | Org Branding | P2 |
| S14 | Org Core Capability List | P2 |

---

# 6. Reporting & Analysis

## 6.1 Metrics

**Phase:** Standard | **Status:** Not Started | **Stories:** 14

### Overview
Cadence provides metrics at two levels: exercise-level metrics for performance during/after a specific exercise, and organization-level metrics tracking trends across multiple exercises.

### Problem Statement
Directors need real-time situational awareness during conduct. After exercises, teams need comprehensive metrics for AAR. Organizations need trend analysis across exercises to demonstrate improvement.

### User Stories

**MVP (P0):**

| Story | Title |
|-------|-------|
| S01 | Exercise Progress Dashboard |
| S02 | Exercise Inject Summary |
| S03 | Exercise Observation Summary |
| S04 | Exercise Timeline Summary |

**Standard (P1):**

| Story | Title |
|-------|-------|
| S05 | P/S/M/U Distribution Chart |
| S06 | Core Capability Performance |
| S07 | Controller Activity Metrics |
| S08 | Evaluator Coverage Metrics |
| S09 | Organization Exercise History |
| S10 | Organization Performance Trends |
| S11 | Metrics Export |

**Advanced (P2):**

| Story | Title |
|-------|-------|
| S12 | Comparative Analysis |
| S13 | Benchmark Comparison |
| S14 | Custom Metrics Dashboard |

---

## 6.2 Progress Dashboard

**Phase:** MVP | **Status:** Ready | **Stories:** 1

### Overview
Visual progress dashboard that guides users through exercise setup and shows completion status for each configuration area.

### User Stories

| Story | Title | Priority |
|-------|-------|----------|
| S01 | View Setup Progress | P2 |

---

## 6.3 Reports

**Phase:** Post-MVP | **Status:** Planned | **Stories:** 1+

### Overview
Export exercise data for after-action review, documentation, and compliance purposes. Initial focus on Excel export for MSEL data and observations.

### User Stories

| Story | Title | Priority |
|-------|-------|----------|
| S05 | Excel Export | P0 |

---

## 6.4 Review Mode

**Phase:** Standard | **Status:** Not Started | **Stories:** 6

### Overview
Dedicated view for reviewing exercise execution after conduct, supporting After-Action Review (AAR) discussions and report preparation.

### Problem Statement
The real-time Conduct view is optimized for action, not analysis. Teams need a post-exercise view that groups events by phase, highlights outcomes, and surfaces observation patterns.

### User Stories

| Story | Title | Priority |
|-------|-------|----------|
| S20 | Access Review Mode | P1 |
| S21 | Phase-Grouped Timeline | P1 |
| S22 | Inject Outcome Summary | P1 |
| S23 | Observation Review Panel | P2 |
| S24 | Exercise Statistics Dashboard | P2 |
| S25 | Export Review Data | P2 |

---

# 7. Data Exchange

## 7.1 Excel Import

**Phase:** MVP | **Status:** Not Started | **Stories:** 4

### Overview
Users can import inject data from Excel files, mapping spreadsheet columns to inject fields to preserve existing MSEL authoring workflows.

### Problem Statement
Many organizations develop MSELs in Excel before exercise conduct. Without import capability, users would need to manually re-enter hundreds of injects.

### User Stories

| Story | Title | Priority |
|-------|-------|----------|
| S01 | Upload Excel File | P0 |
| S02 | Map Excel Columns | P0 |
| S03 | Validate Import Data | P0 |
| S04 | Delivery Method Synonym Matching | P2 |

---

## 7.2 Excel Export

**Phase:** MVP | **Status:** Not Started | **Stories:** 2

### Overview
Users can export MSEL data to Excel format for sharing with stakeholders, creating backups, or final formatting outside Cadence.

### User Stories

| Story | Title | Priority |
|-------|-------|----------|
| S01 | Export MSEL to Excel | P0 |
| S02 | Export Blank Template | P1 |

---

# 8. Cross-Cutting Concerns

## 8.1 Versioning

**Phase:** MVP | **Status:** Complete | **Stories:** 2

### Overview
Automated semantic versioning with Release Please, conventional commits enforcement, and user-facing version display with release notes.

### User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| S01 | What's New Notification | P1 | ✅ Complete |
| S02 | About Page with Release History | P1 | ✅ Complete |

---

## 8.2 Cross-Cutting Technical Features

**Phase:** MVP | **Status:** In Progress | **Stories:** 5

### Overview
Technical capabilities that span multiple features: session management, keyboard navigation, auto-save, and responsive design.

### Problem Statement
Exercise conduct requires long, uninterrupted sessions. Users lose work when sessions timeout unexpectedly, browsers crash without auto-save, or must use only a mouse for repetitive actions.

### User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| S01 | Session Management | P0 | 📋 Ready |
| S02 | Keyboard Navigation | P1 | 📋 Ready |
| S03 | Auto-save | P0 | 📋 Ready |
| S04 | Responsive Design | P1 | 📋 Ready |
| S05 | Deprecate Legacy User Table | P2 | ✅ Complete |

### EXIS Pain Points Addressed

| Pain Point | Resolution |
|------------|------------|
| Short session timeout (30 min) | Extended to 4 hours with warning |
| No keyboard shortcuts | Comprehensive keyboard navigation |
| No auto-save | Auto-save on blur + interval |
| Poor tablet experience | Responsive design for 1024px+ |

---

# Appendix: Feature Summary by Phase

## MVP Phase (19 features)

| Feature | Status |
|---------|--------|
| Authentication & Authorization | In Progress |
| Organization Management | In Progress |
| Core Domain Entities | In Progress |
| Exercise CRUD | In Progress |
| Exercise Lifecycle | Not Started |
| Exercise Status Workflow | Not Started |
| Exercise Configuration | In Progress |
| Exercise Phases | Not Started |
| Inject CRUD | In Progress |
| MSEL Management | Ready |
| Inject Approval Workflow | Not Started |
| Exercise Observations | Complete |
| Connectivity | Complete |
| Homepage | Complete |
| Navigation Shell | Ready |
| My Assignments | Ready |
| Notifications | Ready |
| Excel Import | Not Started |
| Excel Export | Not Started |
| Progress Dashboard | Ready |
| Versioning | Complete |
| Cross-Cutting | In Progress |

## Standard Phase (9 features)

| Feature | Status |
|---------|--------|
| Bulk Participant Import | Not Started |
| Exercise Objectives | Not Started |
| Inject Filtering | Not Started |
| Inject Organization | Not Started |
| Exercise Capabilities | Not Started |
| Settings | Not Started |
| Metrics | Not Started |
| Review Mode | Not Started |
| Reports | Planned |

---

*Generated from individual FEATURE.md files in docs/features/*
