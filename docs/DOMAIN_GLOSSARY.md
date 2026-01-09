# Cadence Domain Glossary

> **HSEEP-aligned terminology for the Cadence MSEL Management Platform**

## Purpose

This glossary ensures consistent terminology across all requirements documentation, development, and user interfaces. Terms are aligned with the Homeland Security Exercise and Evaluation Program (HSEEP) 2020 doctrine where applicable.

---

## Core Exercise Terms

### Exercise
A planned event that tests and evaluates an organization's capabilities to respond to a real-world incident. In Cadence, an exercise is the top-level container for all related data including MSELs, objectives, participants, and observations.

**Attributes**: Name, Type, Date, Location, Status, Practice Flag, Time Zone

### Exercise Types

| Type | Abbreviation | Description | Cadence Support |
|------|--------------|-------------|-----------------|
| Table Top Exercise | TTX | Discussion-based exercise around a scenario | ✅ MVP |
| Functional Exercise | FE | Operations-based with simulated operations | ✅ MVP |
| Full-Scale Exercise | FSE | Operations-based with actual deployment | ✅ MVP |
| Computer-Aided Exercise | CAX | Technology-driven simulation support | ✅ Standard |

### MSEL (Master Scenario Events List)
The chronological timeline of events (injects) that drive an exercise. The MSEL details what happens, when, to whom, and expected outcomes.

**Attributes**: Version Number, Status (Draft/Active/Archived), Associated Exercise

**Important**: An exercise may have multiple MSEL versions (e.g., draft iterations before final), but only one can be active during conduct.

### Inject
A single event, message, or piece of information introduced into an exercise to drive player actions. Injects are the fundamental building blocks of a MSEL.

**Synonyms**: Event, input, stimulus

**Attributes**: See [Inject Entity](#inject-entity-attributes)

---

## Inject Types

### Standard Inject
A planned inject that is delivered at a specific scheduled time regardless of player actions.

### Contingency Inject
An inject prepared for use if players deviate from expected responses. Used to guide players back on track.

**Example**: "If players do not evacuate within 15 minutes, deliver: 'Fire alarm activates automatically'"

### Adaptive Inject
An inject that creates meaningful branches based on player decisions. Used for realistic decision-consequence flows.

**Example**: "If players choose to shelter in place, deliver Branch A. If players choose to evacuate, deliver Branch B."

### Complexity Inject
An inject delivered based on Controller assessment that increases exercise difficulty.

**Example**: "If players are handling the scenario well, Controller may introduce additional casualties."

---

## Time Concepts

### Scheduled Time
The planned wall-clock time when an inject should be delivered during exercise conduct. This is the time Controllers use to sequence inject delivery.

**Format**: HH:MM (local to exercise time zone)

### Scenario Time
The fictional in-story time within the exercise narrative. Allows scenarios to span multiple simulated days within a single exercise session.

**Format**: Day + HH:MM (e.g., "Day 2 14:30")

**Example**: An exercise running from 0900-1200 real time might simulate events from "Day 1 06:00" through "Day 3 18:00" in scenario time.

### Dual Time Tracking
Cadence's capability to maintain both Scheduled Time and Scenario Time for each inject, preserving the distinction between delivery logistics and narrative timeline.

### Exercise Clock
The unified time control that allows Exercise Directors to start, pause, and fast-forward exercise time. Affects auto-fire timing for scheduled injects.

**Status**: Standard Phase feature

---

## Roles and Responsibilities

### Administrator
System-level user responsible for Cadence configuration, user management, and organizational settings.

**Permissions**: All permissions including user management, system configuration

### Exercise Director
Senior exercise leadership responsible for overall exercise management and real-time decision making during conduct.

**Permissions**: Full exercise access, participant assignment, MSEL activation

### Controller
Exercise staff member responsible for delivering injects and guiding player actions during conduct.

**Permissions**: Fire injects, update inject status, view all injects

### Evaluator
Observer responsible for documenting player performance against exercise objectives for the After-Action Report.

**Permissions**: Record observations, view injects and objectives, cannot fire injects

### Observer
Read-only participant who monitors exercise progress without active involvement.

**Permissions**: View-only access to exercise data

### Player
The trainees or responders being exercised. Players are NOT Cadence users—they are the recipients of injects during exercise conduct.

**Note**: Players do not have Cadence accounts; they interact through the exercise scenario itself.

---

## Exercise Structure

### Phase
A logical division of an exercise (e.g., "Initial Response", "Recovery", "Demobilization"). Phases help organize injects and track exercise progression.

**Attributes**: Name, Sequence Order, Start Time (optional)

### Objective
A specific, measurable outcome the exercise is designed to test. Objectives are linked to injects that provide opportunities for players to demonstrate capability.

**HSEEP Alignment**: Objectives should be SMART (Specific, Measurable, Achievable, Relevant, Time-bound)

**Attributes**: Description, Priority (Core/Supporting), Linked Injects

### Observation
A documented instance of player performance during exercise conduct, typically recorded by Evaluators against specific objectives.

**Attributes**: Timestamp, Observer, Objective Link, Description, Strength/Area for Improvement flag

---

## Exercise Lifecycle

### Draft
An exercise or MSEL that is being prepared and is not yet ready for conduct. Draft items can be freely edited.

### Active
An exercise currently in conduct or a MSEL selected as the current version for exercise conduct.

### Archived
An exercise or MSEL that has been completed and preserved for historical reference. Archived items are read-only.

### Practice Mode
An exercise flagged for training or testing purposes. Practice exercises are excluded from production reports and metrics.

**Use Cases**: Controller training, system testing, scenario validation

---

## Inject Entity Attributes

| Attribute | Description | Required |
|-----------|-------------|----------|
| Inject Number | Unique sequential identifier within MSEL | System-generated |
| Title | Brief descriptive name | Yes |
| Description | Full inject content delivered to players | Yes |
| Scheduled Time | Planned delivery time (wall clock) | Yes |
| Scenario Time | In-story time (Day + Time) | No |
| Target | Player role or organization receiving inject | Yes |
| Source | Simulated origin of inject (e.g., "911 Dispatch") | No |
| Method | Delivery mechanism (verbal, phone, email, radio) | No |
| Expected Action | Anticipated player response | No |
| Controller Notes | Private guidance for Controller delivering inject | No |
| Phase | Exercise phase this inject belongs to | No |
| Objectives | Linked exercise objectives | No |
| Status | Pending/Fired/Skipped | System-managed |
| Inject Type | Standard/Contingency/Adaptive/Complexity | Standard default |
| Parent Inject | For branching: the inject that triggers this one | No |

---

## Technical Terms

### Offline Mode
Cadence's ability to function without network connectivity, storing data locally and synchronizing when connection is restored.

### Sync Conflict
A situation where the same data has been modified in multiple places while offline. Cadence uses a last-write-wins strategy with conflict logging.

### Auto-save
Automatic preservation of in-progress edits without explicit save action. Protects against data loss from browser crashes or accidental navigation.

### Session
A user's authenticated interaction period with Cadence. Sessions have configurable timeout with warning prompts.

---

## Abbreviations

| Abbreviation | Full Term |
|--------------|-----------|
| AAR | After-Action Report |
| CAX | Computer-Aided Exercise |
| FE | Functional Exercise |
| FSE | Full-Scale Exercise |
| HSEEP | Homeland Security Exercise and Evaluation Program |
| MSEL | Master Scenario Events List |
| TTX | Table Top Exercise |

---

## Terms NOT Used in Cadence

To avoid confusion, these terms are intentionally avoided:

| Avoided Term | Use Instead | Reason |
|--------------|-------------|--------|
| Scenario | Exercise | "Scenario" is ambiguous; could mean exercise type or narrative |
| Event | Inject | "Event" is too generic |
| Task | Inject or Expected Action | "Task" implies assignment, not stimulus |
| Trigger | Parent Inject | "Trigger" has technical connotations |

---

## References

- [HSEEP 2020 Doctrine](https://www.fema.gov/emergency-managers/national-preparedness/exercises/hseep)
- [FEMA Exercise Design Resources](https://www.fema.gov/emergency-managers/national-preparedness/exercises)

---

*Last updated: 2025-01-08*
