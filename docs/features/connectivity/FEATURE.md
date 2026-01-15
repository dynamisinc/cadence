# Connectivity Feature

## Overview

Enable multi-user real-time synchronization and offline capability for field use — addressing the #1 SME pain point: "The wifi in the EOC is terrible."

## Problem Statement

Emergency management exercises often occur in locations with poor or unreliable network connectivity (emergency operations centers, field locations, etc.). Users need to:

1. See real-time updates from other users when connected
2. Continue working when connectivity is lost
3. Have their changes synchronized when connectivity is restored
4. Understand what happened if conflicts occurred during offline work

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| S01 | Real-Time Data Sync | P0 | Not Started |
| S02 | Offline Detection & Indicators | P0 | Not Started |
| S03 | Local Data Cache | P0 | Not Started |
| S04 | Offline Action Queue | P0 | Not Started |
| S05 | Sync on Reconnect | P0 | Not Started |
| S06 | Conflict Resolution | P1 | Not Started |

## Technical Architecture

### Real-Time Stack

- **Backend**: ASP.NET Core SignalR Hub
- **Azure Service**: Azure SignalR Service (for production scaling)
- **Frontend**: @microsoft/signalr client library
- **State Management**: React Query with SignalR event invalidation

### Offline Stack

- **Local Storage**: IndexedDB via Dexie.js
- **Offline Detection**: Navigator.onLine + SignalR connection state
- **Action Queue**: IndexedDB-persisted operation queue
- **Sync Strategy**: FIFO queue processing with conflict detection

## Data Flow

```
Online Mode:
User Action → API Call → Database → SignalR Broadcast → All Clients Update

Offline Mode:
User Action → IndexedDB Queue → Optimistic UI Update
                    ↓
              (on reconnect)
                    ↓
           Queue Processing → API Calls → Conflict Check → UI Update
```

## Key Decisions

1. **Last-write-wins** for most conflicts (timestamps determine winner)
2. **First-write-wins** for inject firing (can't fire twice)
3. **Optimistic UI** for immediate feedback during offline
4. **IndexedDB** over localStorage for structured data and larger storage
5. **Dexie.js** for cleaner IndexedDB API

## Dependencies

- Phase D (Exercise Conduct) - Complete
- Phase E (Observations) - Complete
- SignalR infrastructure - Exists, needs clock events added

## Success Metrics

- Real-time updates within 1 second across connected users
- Zero data loss during offline periods
- Clear user feedback on connection state
- Graceful conflict resolution with user notification
