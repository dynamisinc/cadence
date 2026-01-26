# S02: Notification Toasts

## Story

**As a** Cadence user during exercise conduct,
**I want** to see toast notifications for important events,
**So that** I am immediately alerted without having to check the bell icon.

## Context

During active exercise conduct, timing is critical. Toast notifications provide immediate, unobtrusive alerts that overlay the current view. High-priority events (like inject ready) require immediate attention, while lower-priority events auto-dismiss.

## Acceptance Criteria

### Toast Display
- [ ] **Given** a high-priority notification arrives, **when** displayed, **then** a toast appears in the corner of the screen
- [ ] **Given** a toast appears, **when** displayed, **then** it shows: icon, title, brief message
- [ ] **Given** multiple toasts arrive, **when** displayed, **then** maximum 3 are visible (stack vertically)
- [ ] **Given** more than 3 toasts pending, **when** displayed, **then** older ones are queued or collapsed

### Auto-Dismiss Behavior
- [ ] **Given** a HIGH priority toast, **when** displayed, **then** it does NOT auto-dismiss (requires manual close)
- [ ] **Given** a MEDIUM priority toast, **when** displayed, **then** it auto-dismisses after 10 seconds
- [ ] **Given** a LOW priority toast, **when** displayed, **then** it auto-dismisses after 5 seconds
- [ ] **Given** user hovers over a toast, **when** hovering, **then** auto-dismiss timer pauses

### Manual Dismiss
- [ ] **Given** a toast is displayed, **when** I click the X button, **then** it is dismissed
- [ ] **Given** a toast has an action URL, **when** I click the toast body, **then** I navigate and toast dismisses

### Visual Styling
- [ ] **Given** a high-priority toast, **when** displayed, **then** it has warning/alert styling (orange/red accent)
- [ ] **Given** a medium-priority toast, **when** displayed, **then** it has info styling (blue accent)
- [ ] **Given** a low-priority toast, **when** displayed, **then** it has subtle styling (gray accent)

### Toast Triggers
- [ ] **Given** SignalR receives NotificationCreated event, **when** notification is HIGH priority, **then** toast displays
- [ ] **Given** SignalR receives NotificationCreated event, **when** notification is MEDIUM priority, **then** toast displays
- [ ] **Given** SignalR receives NotificationCreated event, **when** notification is LOW priority, **then** NO toast (bell only)

## Out of Scope

- Sound/audio alerts
- Desktop/push notifications (browser API)
- Toast preferences (enable/disable by type)
- Toast grouping/batching

## Dependencies

- SignalR infrastructure (existing)
- Notification entity with Priority field
- React context or state management for toast queue

## Domain Terms

| Term | Definition |
|------|------------|
| Toast | Temporary notification overlay, typically in corner |
| Auto-Dismiss | Toast disappears after set time without user action |
| Priority | Urgency level determining display behavior |

## Notification Priority → Toast Behavior

| Priority | Shows Toast | Auto-Dismiss | Duration | Use Case |
|----------|-------------|--------------|----------|----------|
| High | ✅ | ❌ Manual only | ∞ | Inject ready, clock started |
| Medium | ✅ | ✅ | 10 sec | Inject fired, status changes |
| Low | ❌ | N/A | N/A | Assignments, passive updates |

## UI/UX Notes

### Toast Position and Stack
```
┌──────────────────────────────────────────────────┐
│                                                  │
│                     Main Content                 │
│                                                  │
│                                     ┌──────────┐ │
│                                     │ Toast 3  │ │
│                                     └──────────┘ │
│                                     ┌──────────┐ │
│                                     │ Toast 2  │ │
│                                     └──────────┘ │
│                                     ┌──────────┐ │
│                                     │ Toast 1  │ │ ← Newest at bottom
│                                     └──────────┘ │
└──────────────────────────────────────────────────┘
```

### Toast Anatomy
```
┌────────────────────────────────────────────────┐
│  ⚠️  Inject #12 Ready to Fire             ✕    │
│      Hurricane Response Exercise               │
│      Click to view inject queue                │
└────────────────────────────────────────────────┘
     │                                      │
     └── Icon indicates type                └── Close button
```

### Color Scheme by Priority
| Priority | Background | Border | Icon Color |
|----------|------------|--------|------------|
| High | Light orange | Orange | Orange |
| Medium | Light blue | Blue | Blue |
| Low | Light gray | Gray | Gray |

## Technical Notes

### Toast Context Provider
```typescript
// src/frontend/src/contexts/ToastContext.tsx

interface Toast {
  id: string;
  type: NotificationType;
  priority: Priority;
  title: string;
  message: string;
  actionUrl?: string;
  createdAt: Date;
}

interface ToastContextType {
  toasts: Toast[];
  addToast: (toast: Omit<Toast, 'id' | 'createdAt'>) => void;
  removeToast: (id: string) => void;
  clearAll: () => void;
}
```

### Component Structure
```
NotificationToast/
├── ToastProvider.tsx           # Context provider + queue management
├── ToastContainer.tsx          # Renders stack of toasts
├── Toast.tsx                   # Single toast component
├── useToast.ts                 # Hook to access toast context
└── toastConfig.ts              # Priority → timing configuration
```

### Auto-Dismiss Logic
```typescript
const DISMISS_TIMERS: Record<Priority, number | null> = {
  High: null,      // Never auto-dismiss
  Medium: 10000,   // 10 seconds
  Low: 5000        // 5 seconds
};

useEffect(() => {
  const timer = DISMISS_TIMERS[toast.priority];
  if (timer === null) return;
  
  const timeout = setTimeout(() => removeToast(toast.id), timer);
  return () => clearTimeout(timeout);
}, [toast.id, toast.priority]);
```

### SignalR Integration
```typescript
// In ToastProvider or hook
connection.on('NotificationCreated', (notification) => {
  // Only show toast for High and Medium priority
  if (notification.priority === 'Low') return;
  
  addToast({
    type: notification.type,
    priority: notification.priority,
    title: notification.title,
    message: notification.message,
    actionUrl: notification.actionUrl
  });
});
```

### Animation
- Slide in from right
- Fade out on dismiss
- Stack adjustment when toast removed

---

*Story created: 2026-01-23*
