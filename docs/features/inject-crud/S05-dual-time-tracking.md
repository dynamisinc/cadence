# Story: S05 - Dual Time Tracking

## User Story

**As a** Controller or Exercise Director,
**I want** to track both scheduled delivery time and scenario time for each inject,
**So that** I can manage multi-day scenarios compressed into shorter exercise windows.

## Context

Emergency management exercises often simulate scenarios spanning multiple days (hurricane making landfall over 72 hours, multi-day cyber incident, etc.), but the actual exercise may only run for 4-8 hours. Dual time tracking allows:

- **Scheduled Time**: When the Controller should actually deliver the inject (wall clock)
- **Scenario Time**: When the event occurs in the story (Day 2, 14:00)

This separation enables exercises to compress or expand time while maintaining scenario realism. Players understand "it's Day 3 in the scenario" even though the exercise just started.

## Acceptance Criteria

### Scheduled Time
- [ ] **Given** I am creating an inject, **when** I enter Scheduled Time, **then** I can select date and time using a datetime picker
- [ ] **Given** I set Scheduled Time, **when** I save, **then** the time is stored in UTC and displayed in exercise time zone
- [ ] **Given** an exercise time zone is set, **when** I view Scheduled Time, **then** I see the time with zone abbreviation (e.g., "09:30 AM EST")
- [ ] **Given** I am viewing the MSEL list, **when** I look at the Scheduled column, **then** I see times in exercise time zone

### Scenario Time (Optional)
- [ ] **Given** I am creating an inject, **when** I view Scenario Time fields, **then** I see: Scenario Day (number) and Scenario Time (HH:MM)
- [ ] **Given** I enter a Scenario Day, **when** I save, **then** the value is stored (integer 1-99)
- [ ] **Given** I enter a Scenario Time, **when** I save, **then** the value is stored (24-hour format)
- [ ] **Given** I enter Scenario Time without Scenario Day, **when** I try to save, **then** I see validation error "Day required when Time is set"
- [ ] **Given** I enter Scenario Day without Scenario Time, **when** I try to save, **then** save succeeds (Time is optional when Day is set)
- [ ] **Given** I don't enter Scenario Day or Time, **when** I save, **then** both fields are null (valid state)

### Display
- [ ] **Given** I am viewing the MSEL list, **when** I look at the Scenario column, **then** I see "D1 08:00" format (or "—" if not set)
- [ ] **Given** I am viewing inject detail, **when** I look at Time section, **then** I see both Scheduled and Scenario times clearly labeled
- [ ] **Given** I am sorting the MSEL, **when** I sort by Scheduled Time, **then** injects order by actual delivery time
- [ ] **Given** I am sorting the MSEL, **when** I sort by Scenario Time, **then** injects order by Day then Time (nulls last)

### Conduct Time Tracking
- [ ] **Given** I fire an inject, **when** the fire is recorded, **then** the system captures: FiredAt (actual timestamp), variance from Scheduled
- [ ] **Given** I view a fired inject, **when** I look at timing, **then** I see: Scheduled, Actual (FiredAt), and Variance
- [ ] **Given** variance is calculated, **when** inject was fired 5 minutes late, **then** I see "+5 min" variance

### Excel Round-Trip
- [ ] **Given** I import from Excel, **when** Scenario Day and Time columns exist, **then** values are imported correctly
- [ ] **Given** I export to Excel, **when** the export completes, **then** Scenario Day and Time are separate columns

## Out of Scope

- Exercise clock simulation (automatic time advancement)
- Time scaling (1 minute = 1 hour scenario time)
- Automatic inject firing based on time
- Multiple scenario timelines

## Dependencies

- inject-crud/S01: Create Inject (time fields are inject properties)
- exercise-config/S03: Time Zone Configuration (scheduled time display)
- excel-import/S01: Excel Import (time column mapping)
- excel-export/S01: Excel Export (time column output)

## Open Questions

- [ ] Should scenario time support hours beyond 24 (e.g., "Day 1, 36:00" = Day 2, 12:00)?
- [ ] Should there be validation that scenario times progress logically?
- [ ] Should we support scenario dates (specific dates vs. Day 1, Day 2)?

## Domain Terms

| Term | Definition |
|------|------------|
| Scheduled Time | Wall-clock time when the inject should be delivered |
| Scenario Time | In-story time (Day and Time) for the event |
| Scenario Day | The day number in the scenario narrative (1, 2, 3...) |
| FiredAt | Actual timestamp when inject was delivered |
| Variance | Difference between Scheduled and FiredAt times |
| Time Compression | Running a multi-day scenario in shorter real time |

## UI/UX Notes

### Time Entry in Create/Edit Form

```
┌─────────────────────────────────────────────────────────────────────────┐
│  TIME                                                                   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Scheduled Time * (when to deliver)                                    │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ 📅 January 15, 2025        🕐 09:30 AM                          │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│  Time zone: Eastern Time (EST)                                         │
│                                                                         │
│  Scenario Time (when it happens in the story)              [Optional]  │
│  ┌────────────────┐  ┌────────────────┐                               │
│  │ Day    1       │  │ Time   14:00   │                               │
│  └────────────────┘  └────────────────┘                               │
│  💡 Use scenario time for multi-day scenarios compressed into         │
│     shorter exercise windows.                                          │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### MSEL List with Both Times

```
┌──────────────────────────────────────────────────────────────────────────────┐
│  #  │ Scheduled  │ Scenario   │ Title                      │ Status │       │
│ ────┼────────────┼────────────┼────────────────────────────┼────────┼────── │
│  1  │ 09:00 AM   │ D1 08:00   │ Hurricane warning issued   │ Pending│ •••  │
│  2  │ 09:15 AM   │ D1 10:00   │ EOC activation ordered     │ Pending│ •••  │
│  3  │ 09:30 AM   │ D1 14:00   │ Evacuation order issued    │ Pending│ •••  │
│  4  │ 09:45 AM   │ D2 08:00   │ Landfall + 6 hours         │ Pending│ •••  │
│  5  │ 10:00 AM   │ D2 14:00   │ Shelter capacity exceeded  │ Pending│ •••  │
│  6  │ 10:15 AM   │ —          │ Administrative break       │ Pending│ •••  │
└──────────────────────────────────────────────────────────────────────────────┘

D1 = Day 1, D2 = Day 2
— = No scenario time set
```

### Inject Detail - Time Section

```
┌─────────────────────────────────────────────────────────────────────────┐
│  TIME                                                                   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌──────────────────────────────┐  ┌──────────────────────────────┐   │
│  │  SCHEDULED                   │  │  SCENARIO                    │   │
│  │  (When to deliver)           │  │  (Story time)                │   │
│  │                              │  │                              │   │
│  │  📅 Jan 15, 2025             │  │  📖 Day 1                    │   │
│  │  🕐 09:30 AM EST             │  │  🕐 14:00                    │   │
│  └──────────────────────────────┘  └──────────────────────────────┘   │
│                                                                         │
│  After firing:                                                         │
│  ┌──────────────────────────────┐                                     │
│  │  ACTUAL                      │                                     │
│  │  🕐 09:32 AM EST             │                                     │
│  │  Variance: +2 min            │                                     │
│  └──────────────────────────────┘                                     │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Technical Notes

- Store Scheduled Time as UTC DateTime
- Store Scenario Day as nullable int (1-99)
- Store Scenario Time as nullable TimeSpan
- Calculate variance as TimeSpan (FiredAt - ScheduledTime)
- Indexing strategy for both time columns for efficient sorting
