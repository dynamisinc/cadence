# S01: Notification Bell & Dropdown

## Story

**As a** Cadence user,
**I want** to see a notification bell with unread count in the header,
**So that** I know when I have new alerts and can review them without leaving my current page.

## Context

During exercise conduct, users need awareness of events happening across the system. The notification bell provides a persistent, non-intrusive indicator of pending notifications with quick access to details.

## Acceptance Criteria

### Bell Icon
- [ ] **Given** I am logged in, **when** the header renders, **then** I see a bell icon
- [ ] **Given** I have unread notifications, **when** displayed, **then** a badge shows the unread count
- [ ] **Given** I have 0 unread notifications, **when** displayed, **then** no badge is shown
- [ ] **Given** I have 100+ unread notifications, **when** displayed, **then** badge shows "99+"
- [ ] **Given** a new notification arrives, **when** the bell updates, **then** badge count increases without page refresh

### Dropdown Display
- [ ] **Given** I click the bell, **when** dropdown opens, **then** I see up to 10 recent notifications
- [ ] **Given** the dropdown is open, **when** I click outside it, **then** it closes
- [ ] **Given** a notification in the dropdown, **when** displayed, **then** I see: icon, title, time ago, read/unread indicator
- [ ] **Given** an unread notification, **when** displayed, **then** it has a visual distinction (bold, dot, background)

### Notification Items
- [ ] **Given** a notification has an ActionUrl, **when** I click it, **then** I navigate to that URL
- [ ] **Given** I click a notification, **when** navigating, **then** it is marked as read
- [ ] **Given** a notification has no ActionUrl, **when** I click it, **then** it is marked as read (no navigation)

### Mark as Read
- [ ] **Given** the dropdown is open, **when** I click "Mark all as read", **then** all notifications are marked read
- [ ] **Given** I mark all as read, **when** the action completes, **then** the unread badge disappears

### Empty State
- [ ] **Given** I have no notifications, **when** I open the dropdown, **then** I see "No notifications" message

### Real-Time Updates
- [ ] **Given** a new notification is created, **when** SignalR delivers it, **then** bell badge updates immediately
- [ ] **Given** dropdown is open, **when** new notification arrives, **then** it appears at the top of the list

## Out of Scope

- Full notifications page/history
- Delete notifications
- Notification preferences/settings
- Filtering by type

## Dependencies

- Header/AppBar component
- SignalR connection (existing)
- GET /api/notifications endpoint
- POST /api/notifications/read-all endpoint

## Domain Terms

| Term | Definition |
|------|------------|
| Notification | A system message alerting user to an event |
| Unread | Notification that hasn't been acknowledged |
| Badge | Small count indicator overlaying the bell icon |
| ActionUrl | Link to navigate when notification clicked |

## UI/UX Notes

### Bell Icon States
```
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│      🔔         │  │      🔔³        │  │      🔔⁹⁹⁺      │
│   No unread     │  │   3 unread      │  │   99+ unread    │
└─────────────────┘  └─────────────────┘  └─────────────────┘
```

### Dropdown Layout
```
┌────────────────────────────────────────────┐
│  Notifications            Mark all as read │
├────────────────────────────────────────────┤
│  ● Inject #12 ready to fire                │
│    Hurricane Response  •  2m ago           │
├────────────────────────────────────────────┤
│  ○ Exercise clock started                  │
│    Cyber TTX  •  15m ago                   │
├────────────────────────────────────────────┤
│  ○ You've been assigned to Flood Drill     │
│    As Controller  •  1h ago                │
├────────────────────────────────────────────┤
│  ... more notifications ...                │
│                                            │
│  [See all notifications] (future)          │
└────────────────────────────────────────────┘

● = unread (filled dot, bolder text)
○ = read (open dot, normal text)
```

### Icon by Notification Type
| Type | Icon |
|------|------|
| InjectReady | `faExclamationCircle` (warning color) |
| InjectFired | `faPlay` |
| ClockStarted | `faClock` |
| ClockPaused | `faPause` |
| ExerciseCompleted | `faCheckCircle` |
| AssignmentCreated | `faUserPlus` |

## Technical Notes

### Component Structure
```
NotificationBell/
├── NotificationBell.tsx        # Bell icon + badge
├── NotificationDropdown.tsx    # Dropdown menu
├── NotificationItem.tsx        # Single notification row
├── useNotifications.ts         # React Query hook
├── useNotificationSignalR.ts   # Real-time subscription
└── notificationApi.ts          # API functions
```

### SignalR Integration
```typescript
// Subscribe to new notifications
useEffect(() => {
  const connection = getSignalRConnection();
  
  connection.on('NotificationCreated', (notification) => {
    // Add to local state
    queryClient.setQueryData(['notifications'], (old) => ({
      ...old,
      items: [notification, ...old.items.slice(0, 9)],
      unreadCount: old.unreadCount + 1
    }));
  });
  
  return () => connection.off('NotificationCreated');
}, []);
```

### Badge Styling (MUI)
```tsx
<Badge 
  badgeContent={unreadCount > 99 ? '99+' : unreadCount} 
  color="error"
  invisible={unreadCount === 0}
>
  <NotificationsIcon />
</Badge>
```

---

*Story created: 2026-01-23*
