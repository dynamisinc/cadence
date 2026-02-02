# Feature: Cross-Cutting Concerns

**Phase:** MVP
**Status:** In Progress

## Overview

Cross-cutting concerns are technical capabilities that span multiple features and affect the entire application. These are not user-facing features in the traditional sense, but rather foundational behaviors that users expect throughout the application.

## Problem Statement

Exercise conduct requires long, uninterrupted sessions where Controllers manage multiple injects over several hours. Users lose work when sessions timeout unexpectedly, when browsers crash without auto-save, or when forced to use only a mouse for repetitive actions. The application must provide session resilience, data protection, keyboard efficiency, and responsive layouts to support professional emergency management workflows without frustration or data loss.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-session-management.md) | Session Management | P0 | 📋 Ready |
| [S02](./S02-keyboard-navigation.md) | Keyboard Navigation | P1 | 📋 Ready |
| [S03](./S03-auto-save.md) | Auto-save | P0 | 📋 Ready |
| [S04](./S04-responsive-design.md) | Responsive Design | P1 | 📋 Ready |
| [S05](./S05-deprecate-user-table.md) | Deprecate Legacy User Table | P2 | ✅ Complete |

## User Personas

| Persona | Interaction |
|---------|-------------|
| **Controller** | Needs long session support during multi-hour exercise conduct |
| **Evaluator** | Requires auto-save for observations during fast-paced exercises |
| **Exercise Director** | Uses keyboard shortcuts for rapid inject management |
| **All Users** | Expect responsive layouts on desktop and tablet devices |

## Key Concepts

### EXIS Pain Points Addressed

These stories were informed by pain points identified in the TSA EXIS walkthrough:

| Pain Point | Resolution |
|------------|------------|
| Short session timeout (30 min) causing data loss | Extended timeout (4 hours) with warning (S01) |
| No keyboard shortcuts, excessive clicking | Comprehensive keyboard navigation (S02) |
| No auto-save, lost work on navigation | Auto-save on blur + interval (S03) |
| Poor mobile/tablet experience | Responsive design for desktop/tablet (S04) |

## Dependencies

- User authentication system (for session tokens)
- Backend API endpoints (for auto-save operations)
- Frontend routing (for navigation detection)

## Acceptance Criteria (Feature-Level)

- [ ] Application remains functional for 4+ hour sessions without timeout
- [ ] All primary actions are accessible via keyboard shortcuts
- [ ] No data loss on accidental navigation or browser crash
- [ ] Application is usable on tablet devices (1024px+ width)
- [ ] Legacy User table is fully deprecated with audit fields migrated

## Notes

### Implementation Considerations

**Session Management:**
- Token refresh strategy (silent refresh vs. explicit)
- Multi-tab session sharing
- Offline session handling

**Keyboard Navigation:**
- Focus management patterns
- ARIA compliance
- Shortcut collision avoidance

**Auto-save:**
- Debounce strategy
- Conflict detection
- Offline queue

**Responsive Design:**
- Breakpoint definitions
- Component adaptation patterns
- Touch target sizing

**Deprecate Legacy User Table:**
- Multi-phase migration to avoid data loss
- Dual-write period during transition
- System user handling for audit fields
- Type change from Guid to string for audit columns

### Development Priority

These features should be implemented early as they affect all subsequent development. Consider creating shared hooks/components for reuse across features.
