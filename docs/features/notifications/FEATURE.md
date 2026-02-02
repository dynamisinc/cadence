# Feature: Notifications

**Phase:** Post-MVP
**Status:** Ready

## Overview

The Notifications feature provides real-time alerts to users about important events across the platform. This includes a notification bell in the header with unread count, a dropdown showing recent notifications, and toast messages for immediate alerts.

## Problem Statement

During exercise conduct, users need immediate awareness of critical events without constantly monitoring multiple pages. Controllers need to know when injects are ready to fire, evaluators need alerts when injects are delivered, and directors need visibility into exercise milestones. Without real-time notifications, users risk missing time-sensitive actions that impact exercise quality.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-notification-bell.md) | Notification Bell & Dropdown | P0 | 📋 Ready |
| [S02](./S02-notification-toasts.md) | Notification Toasts | P0 | 📋 Ready |

## User Personas

| Persona | Interaction |
|---------|-------------|
| Controller | Inject ready alerts, clock started/paused, assignment updates |
| Evaluator | Inject fired alerts (to capture observations), exercise started |
| Exercise Director | All exercise events, participant joins, exercise completion |
| Observer | Exercise started, major milestones |

## Key Concepts

| Term | Definition |
|------|------------|
| Notification | A system-generated alert about an event relevant to a user |
| Toast Message | Temporary on-screen popup for immediate alerts |
| Notification Bell | Header icon showing unread notification count |
| Notification Priority | High (requires action), Medium (awareness), Low (informational) |
| Unread Count | Number of notifications not yet marked as read |

## Dependencies

- Navigation Shell (P0-01) for header integration
- SignalR infrastructure (already exists for clock/injects)
- User authentication context

## Acceptance Criteria (Feature-Level)

- [ ] Bell icon visible in header with unread count badge
- [ ] Dropdown shows recent notifications with timestamps
- [ ] Real-time toast messages for high-priority alerts
- [ ] Clicking notification navigates to relevant context
- [ ] Mark as read functionality (individual and bulk)
- [ ] Notifications persist across sessions (database-backed)

## Notes

### Business Value

- **Awareness**: Users stay informed of important events without constant page monitoring
- **Responsiveness**: Controllers see inject-ready alerts immediately
- **Collaboration**: Multi-user exercises benefit from real-time status updates
- **Engagement**: Persistent notifications prevent missed information

### Notification Types

| Type | Priority | Toast? | Description |
|------|----------|--------|-------------|
| `InjectReady` | High | Yes | Inject is ready to fire |
| `InjectFired` | Medium | Yes | Inject was fired (for evaluators) |
| `ClockStarted` | High | Yes | Exercise clock started |
| `ClockPaused` | Medium | Yes | Exercise clock paused |
| `ExerciseCompleted` | Medium | No | Exercise finished |
| `AssignmentCreated` | Low | No | User assigned to exercise |
| `ObservationCreated` | Low | No | New observation recorded |

### Data Requirements

#### Notification Entity
```
Notification:
  - Id (GUID)
  - UserId (FK → User, target recipient)
  - Type (NotificationType enum)
  - Priority (High, Medium, Low)
  - Title (string)
  - Message (string)
  - ActionUrl (string, optional - where to navigate)
  - RelatedEntityType (string, e.g., "Exercise", "Inject")
  - RelatedEntityId (GUID, optional)
  - IsRead (bool)
  - CreatedAt (DateTime)
  - ReadAt (DateTime, optional)
```

#### API Endpoints
```
GET /api/notifications              # List notifications (paginated)
GET /api/notifications/unread-count # Get unread count
POST /api/notifications/{id}/read   # Mark one as read
POST /api/notifications/read-all    # Mark all as read

# SignalR Events (real-time)
NotificationCreated(notification)   # Push new notification to client
```

### Technical Notes

- Leverage existing SignalR hub for real-time delivery
- Consider notification batching for high-frequency events
- Toast auto-dismiss: High=never (manual), Medium=10s, Low=5s
- Maximum 3 toasts visible at once
- Notifications older than 30 days can be archived/deleted

### Related Documentation

- [Navigation Shell Feature](../navigation-shell/FEATURE.md)
- [SignalR Implementation](../connectivity/FEATURE.md)
- [Exercise Clock Events](../exercise-clock/)

---

*Feature created: 2026-01-23*
