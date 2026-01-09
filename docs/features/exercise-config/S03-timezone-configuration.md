# Story: S03 - Configure Exercise Time Zone

## User Story

**As an** Administrator or Exercise Director,
**I want** to set the time zone for an exercise,
**So that** all participants see consistent times regardless of their physical location.

## Context

Emergency management exercises often involve participants across multiple time zones, especially for regional or federal exercises. Setting an authoritative exercise time zone ensures everyone interprets inject times consistently. This is critical for exercises where "10:00 AM" needs to mean the same moment for all participants.

This is particularly important for Cadence's dual-time tracking: Scheduled Time (wall clock) is displayed in the exercise time zone, while Scenario Time is independent of time zones.

## Acceptance Criteria

- [ ] **Given** I am creating or editing an exercise, **when** I access the Settings section, **then** I see a Time Zone dropdown
- [ ] **Given** I am viewing the Time Zone dropdown, **when** I expand it, **then** I see a list of IANA time zones grouped by region (America, Europe, Asia, etc.)
- [ ] **Given** I am selecting a time zone, **when** I type in the dropdown, **then** it filters to matching time zones
- [ ] **Given** I select a time zone, **when** I save the exercise, **then** the time zone is stored and used for all time displays
- [ ] **Given** an exercise has a time zone set, **when** any participant views Scheduled Times, **then** times are displayed in the exercise time zone with the zone abbreviation (e.g., "10:00 AM EST")
- [ ] **Given** I create a new exercise, **when** I don't select a time zone, **then** it defaults to America/New_York (Eastern Time)
- [ ] **Given** an exercise has injects with scheduled times, **when** I change the time zone, **then** I see a confirmation warning about the impact
- [ ] **Given** I confirm a time zone change, **when** the change is saved, **then** scheduled times are displayed in the new zone (underlying UTC values unchanged)
- [ ] **Given** I am viewing inject times, **when** the time zone observes DST, **then** the display adjusts appropriately (EST vs EDT)

## Out of Scope

- Per-user time zone preferences (all users see exercise time zone)
- Automatic time zone detection based on location
- Multiple time zone displays (e.g., showing both local and exercise time)
- Time zone conversion tools

## Dependencies

- exercise-crud/S01: Create Exercise (time zone is exercise property)
- exercise-crud/S02: Edit Exercise (time zone can be changed)
- inject-crud/S05: Dual Time Tracking (scheduled time uses exercise time zone)

## Open Questions

- [ ] Should exercises be able to span multiple time zones with different displays?
- [ ] Should we include UTC offset in the time zone display (e.g., "Eastern Time (UTC-5)")?
- [ ] Should time zone changes be logged for audit trail?

## Domain Terms

| Term | Definition |
|------|------------|
| Exercise Time Zone | The authoritative time zone for displaying Scheduled Times |
| Scheduled Time | Wall-clock time when an inject should be delivered |
| Scenario Time | In-story time (Day/Time) independent of real-world time zones |
| IANA Time Zone | Standard time zone identifier (e.g., "America/New_York") |
| DST | Daylight Saving Time - seasonal clock adjustment |

## UI/UX Notes

```
┌─────────────────────────────────────────────────────────────┐
│  Exercise Settings                                          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Time Zone                                                  │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ America/New_York (Eastern Time)                  ▼  │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  💡 All scheduled times will be displayed in this          │
│     time zone for all participants.                         │
│                                                             │
│  Current offset: UTC-5 (EST)                                │
│  DST transition: Mar 9, 2025 → EDT (UTC-4)                  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Time Zone Selector (Expanded)

```
┌─────────────────────────────────────────────────────────────┐
│  [Search time zones...                               ] 🔍   │
├─────────────────────────────────────────────────────────────┤
│  NORTH AMERICA                                              │
│    America/New_York (Eastern Time)                          │
│    America/Chicago (Central Time)                           │
│    America/Denver (Mountain Time)                           │
│    America/Los_Angeles (Pacific Time)                       │
│    America/Anchorage (Alaska Time)                          │
│    Pacific/Honolulu (Hawaii Time)                           │
│                                                             │
│  EUROPE                                                     │
│    Europe/London (GMT/BST)                                  │
│    Europe/Paris (Central European)                          │
│    Europe/Berlin (Central European)                         │
│                                                             │
│  UTC                                                        │
│    UTC (Coordinated Universal Time)                         │
└─────────────────────────────────────────────────────────────┘
```

### Time Zone Change Warning

```
┌─────────────────────────────────────────────────────────────┐
│  ⚠️ Change Time Zone?                                  ✕   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  This exercise has 47 injects with scheduled times.         │
│                                                             │
│  Changing from Eastern Time to Pacific Time will shift      │
│  how times are displayed:                                   │
│                                                             │
│  Example:                                                   │
│    "10:00 AM EST" → "7:00 AM PST"                          │
│                                                             │
│  The underlying times remain the same; only the display     │
│  changes.                                                   │
│                                                             │
│               [Cancel]  [Change Time Zone]                  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Technical Notes

- Store times in UTC internally; time zone is display preference only
- Use IANA time zone database (e.g., via NodaTime for .NET)
- Consider caching common time zones at top of list
- Handle DST transitions gracefully in calculations
