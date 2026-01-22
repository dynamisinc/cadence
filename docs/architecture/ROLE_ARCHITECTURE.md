# Cadence Role Architecture

## Two Separate Concerns

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              SYSTEM ROLES                                    │
│                    "What can you do in the APPLICATION?"                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ┌─────────────┐    ┌─────────────┐    ┌─────────────┐                    │
│   │    Admin    │    │   Manager   │    │    User     │                    │
│   ├─────────────┤    ├─────────────┤    ├─────────────┤                    │
│   │ • All users │    │ • Create    │    │ • Access    │                    │
│   │ • All exer- │    │   exercises │    │   assigned  │                    │
│   │   cises     │    │ • Manage    │    │   exercises │                    │
│   │ • Settings  │    │   own exer- │    │   only      │                    │
│   │             │    │   cises     │    │             │                    │
│   └─────────────┘    └─────────────┘    └─────────────┘                    │
│                                                                             │
│   Stored on: User.SystemRole                                                │
│   Scope: Application-wide                                                   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

                                    │
                                    │ User participates in exercises
                                    ▼

┌─────────────────────────────────────────────────────────────────────────────┐
│                           HSEEP EXERCISE ROLES                               │
│                  "What's your FUNCTION in THIS exercise?"                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ┌──────────┐   ┌────────────┐   ┌───────────┐   ┌──────────┐            │
│   │ Director │   │ Controller │   │ Evaluator │   │ Observer │            │
│   ├──────────┤   ├────────────┤   ├───────────┤   ├──────────┤            │
│   │ • Full   │   │ • Fire     │   │ • Capture │   │ • Read   │            │
│   │   control│   │   injects  │   │   observ- │   │   only   │            │
│   │ • Assign │   │ • Manage   │   │   ations  │   │          │            │
│   │   partic-│   │   clock    │   │ • Rate    │   │          │            │
│   │   ipants │   │ • Edit     │   │   perform-│   │          │            │
│   │ • All    │   │   MSEL     │   │   ance    │   │          │            │
│   │   below  │   │ • All      │   │           │   │          │            │
│   │          │   │   below    │   │           │   │          │            │
│   └──────────┘   └────────────┘   └───────────┘   └──────────┘            │
│                                                                             │
│   Stored on: ExerciseParticipant.Role                                       │
│   Scope: Per-exercise (same user can have different roles)                  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Example: Real-World Scenario

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  Sarah (System: Admin)                                                      │
│  ├── Hurricane Response TTX ....... Director    ← She created it           │
│  ├── Cyber Incident Exercise ...... Observer    ← Just watching            │
│  └── Regional Coordination ........ (none)      ← Can view as Admin        │
│                                                                             │
│  Bob (System: Manager)                                                      │
│  ├── Hurricane Response TTX ....... Controller  ← Helping Sarah            │
│  ├── Cyber Incident Exercise ...... Director    ← He created it            │
│  └── Regional Coordination ........ (none)      ← Can't see it             │
│                                                                             │
│  Carol (System: User)                                                       │
│  ├── Hurricane Response TTX ....... Evaluator   ← Assigned to evaluate     │
│  ├── Cyber Incident Exercise ...... (none)      ← Can't see it             │
│  └── Regional Coordination ........ Controller  ← Assigned as controller   │
│                                                                             │
│  Dave (System: User)                                                        │
│  ├── Hurricane Response TTX ....... Observer    ← Learning                 │
│  ├── Cyber Incident Exercise ...... (none)      ← Can't see it             │
│  └── Regional Coordination ........ (none)      ← Can't see it             │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Data Model

```
┌─────────────────────┐         ┌──────────────────────────┐
│   ApplicationUser   │         │    ExerciseParticipant   │
├─────────────────────┤         ├──────────────────────────┤
│ Id                  │────┐    │ Id                       │
│ Email               │    │    │ ExerciseId ──────────────┼──► Exercise
│ DisplayName         │    └───►│ UserId                   │
│ SystemRole ◄────────┼─┐       │ Role ◄───────────────────┼─── HSEEP Role
│ Status              │ │       │ AssignedAt               │
│ CreatedAt           │ │       │ AssignedById             │
│ LastLoginAt         │ │       └──────────────────────────┘
└─────────────────────┘ │
                        │
     ┌──────────────────┘
     │
     │  SystemRole enum:        ExerciseRole enum:
     │  ┌─────────────┐         ┌─────────────┐
     │  │ User    = 0 │         │ Observer  =0│
     │  │ Manager = 1 │         │ Evaluator =1│
     │  │ Admin   = 2 │         │ Controller=2│
     └─►└─────────────┘         │ Director  =3│
                                └─────────────┘
```

## Permission Resolution

```
User requests access to Exercise X
            │
            ▼
┌───────────────────────────────┐
│ Is user.SystemRole == Admin?  │
└───────────────┬───────────────┘
                │
        ┌───────┴───────┐
        │ Yes           │ No
        ▼               ▼
┌───────────────┐   ┌───────────────────────────────┐
│ ALLOW ACCESS  │   │ Is user in ExerciseParticipant│
│ (Admin sees   │   │ for this exercise?            │
│  all)         │   └───────────────┬───────────────┘
└───────────────┘                   │
                            ┌───────┴───────┐
                            │ Yes           │ No
                            ▼               ▼
                    ┌───────────────┐   ┌───────────────┐
                    │ ALLOW ACCESS  │   │ DENY ACCESS   │
                    │ Role = their  │   │               │
                    │ ExerciseRole  │   │               │
                    └───────────────┘   └───────────────┘
```

## Key Rules

| Rule | Description |
|------|-------------|
| **Admin Override** | Admins can see all exercises (for support) but don't get Director powers unless explicitly assigned |
| **Exercise Creation** | Manager creates exercise → auto-assigned as Director |
| **Last Admin Protection** | Cannot demote/deactivate the last Admin |
| **Last Director Protection** | Cannot remove/demote the last Director from an exercise |
| **Role Independence** | Changing SystemRole doesn't affect ExerciseRole assignments |
| **Ownership Transfer** | Director can assign another user as Director (transfers ownership) |
