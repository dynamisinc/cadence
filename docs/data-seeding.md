# Data Seeding

> **Last Updated:** January 2026  
> **Location:** `src/Cadence.Core/Data/`

## Overview

Cadence uses a two-stage data seeding architecture:

| Stage | Seeder | Environments | Failure Behavior |
|-------|--------|--------------|------------------|
| **1. Essential** | `EssentialDataSeeder` | ALL (including Production) | Throws - blocks startup |
| **2. Demo** | `DemoDataSeeder` + `DemoUserSeeder` | ALL except Production | Logs - continues startup |

This design ensures:
- Production gets only the minimum required data (default organization)
- UAT/Staging/Development get comprehensive demo data for testing and demonstrations
- Demo data is completely isolated in a separate organization

---

## Quick Start

### Program.cs Integration

```csharp
// =============================================================================
// Service Registration (before builder.Build())
// =============================================================================
if (!builder.Environment.IsProduction())
{
    builder.Services.AddDemoSeeder();
}

var app = builder.Build();

// =============================================================================
// Data Seeding (after builder.Build(), before app.Run())
// =============================================================================

// Stage 1: Essential data - ALL environments
// - Applies pending migrations
// - Creates default organization
await app.SeedEssentialDataAsync();

// Stage 2: Demo data - ALL except Production
// - Creates demo organization, users, exercises, MSELs, injects, observations
if (!app.Environment.IsProduction())
{
    await app.SeedDemoDataAsync();
}

app.Run();
```

---

## Demo Credentials

All demo users belong to **Metro County Emergency Management Agency** and use the same password for convenience.

| Email | Password | Display Name | System Role | HSEEP Roles |
|-------|----------|--------------|-------------|-------------|
| `admin@metrocounty.gov` | `Demo123!` | Maria Chen | Admin | Administrator |
| `jwashington@metrocounty.gov` | `Demo123!` | James Washington | Manager | Exercise Director |
| `tgarcia@metrocounty.gov` | `Demo123!` | Teresa Garcia | Manager | Exercise Director |
| `smartinez@metrocounty.gov` | `Demo123!` | Sarah Martinez | User | Controller |
| `mbrown@metrocounty.gov` | `Demo123!` | Michael Brown | User | Controller |
| `kpatel@metrocounty.gov` | `Demo123!` | Kiran Patel | User | Controller |
| `ldavis@metrocounty.gov` | `Demo123!` | Lisa Davis | User | Evaluator |
| `awilson@metrocounty.gov` | `Demo123!` | Angela Wilson | User | Evaluator |
| `rjohnson@metrocounty.gov` | `Demo123!` | Robert Johnson | User | Observer |

> **Security Note:** These credentials are for demo/UAT only. The `!IsProduction()` check prevents seeding in production.

---

## Seeded Exercises

The demo data includes exercises demonstrating the full lifecycle:

### 1. Hurricane Response TTX 2026 — `Active`
**Primary demonstration exercise** with comprehensive data:
- **Type:** Tabletop Exercise (TTX)
- **Status:** Active (clock running)
- **MSEL:** Hurricane Maria MSEL v2.1
- **Phases:** 4 (Warning & Preparation → Evacuation & Shelter → Response & Life Safety → Initial Recovery)
- **Objectives:** 5 (EOC Activation, Public Warning, Evacuation, Mass Care, Critical Infrastructure)
- **Injects:** 20 (mixed statuses: 5 Fired, 1 Ready, 13 Pending, 1 Skipped)
- **Observations:** 6 (demonstrating P, S, M ratings with inject/objective links)

### 2. Active Threat Response FSE — `Draft`
- **Type:** Full-Scale Exercise (FSE)
- **Status:** Draft (planning phase)
- **Purpose:** Shows exercise in planning stage before MSEL creation

### 3. Cybersecurity Incident Response TTX — `Completed`
- **Type:** Tabletop Exercise (TTX)
- **Status:** Completed (ready for AAR)
- **MSEL:** Ransomware Attack MSEL v1.0
- **Phases:** 3
- **Objectives:** 4
- **Injects:** 6 (all Fired)
- **Observations:** 3 (including Unsatisfactory rating example)

### 4. Earthquake Response FE 2025 — `Archived`
- **Type:** Functional Exercise (FE)
- **Status:** Archived (historical)
- **MSEL:** New Madrid Seismic Zone MSEL
- **Phases:** 2
- **Objectives:** 3
- **Injects:** 6 (all Fired)
- **Observations:** 3

### 5. Flash Flood Response Training — `Draft (Practice Mode)`
- **Type:** Tabletop Exercise (TTX)
- **Status:** Draft with Practice Mode enabled
- **Purpose:** Training exercise for new staff orientation
- **Injects:** 3 (simple training scenario)

---

## Inject Coverage

The Hurricane TTX demonstrates all inject types and statuses:

### Inject Types
| Type | Count | Example |
|------|-------|---------|
| Standard | 16 | NWS Hurricane Watch, Media Inquiry |
| Contingency | 2 | Evacuation Route Flooding |
| Adaptive | 1 | Tourist Hotel Complications |
| Complexity | 1 | Hospital Generator Failure |

### Inject Statuses
| Status | Count | Description |
|--------|-------|-------------|
| Fired | 5 | Delivered during exercise |
| Ready | 1 | Queued for delivery |
| Pending | 13 | Awaiting scheduled time |
| Skipped | 1 | Intentionally bypassed |

### Delivery Methods
All delivery methods demonstrated: Email, Phone, Radio, Verbal

---

## Observation Coverage

Sample observations demonstrate the HSEEP P/S/M/U rating system:

| Rating | Count | Example Scenario |
|--------|-------|------------------|
| **P** (Performed) | 3 | EOC activation within target time |
| **S** (Satisfactory) | 5 | PIO coordination with minor delays |
| **M** (Marginal) | 2 | School district callback exceeded target |
| **U** (Unsatisfactory) | 1 | Cyber incident public communication failure |

Observations also demonstrate:
- Linking to specific injects
- Linking to objectives
- General observations (no inject link)
- Different evaluator authors
- Various locations

---

## File Structure

```
src/Cadence.Core/Data/
├── EssentialDataSeeder.cs      # Stage 1: Default org (all environments)
├── DemoDataSeeder.cs           # Stage 2: Demo org, exercises, MSELs, injects
├── DemoUserSeeder.cs           # Stage 2: Users with password hashing
├── DataSeederExtensions.cs     # Extension methods + observations
└── AppDbContext.cs             # EF Core context
```

---

## Fixed GUIDs

Fixed GUIDs enable idempotent seeding and cross-entity references:

```csharp
// Organization
DemoDataSeeder.DemoOrganizationId  // 11111111-1111-1111-1111-111111111111

// Users (string IDs per ASP.NET Core Identity)
DemoDataSeeder.AdminUserId         // 22222222-2222-2222-2222-222222222222
DemoDataSeeder.Director1UserId     // 22222222-2222-2222-2222-222222222233
DemoDataSeeder.Director2UserId     // 22222222-2222-2222-2222-222222222234
DemoDataSeeder.Controller1UserId   // 22222222-2222-2222-2222-222222222244
// ... etc

// Exercises
DemoDataSeeder.HurricaneTtxId      // 33333333-3333-3333-3333-333333333333
DemoDataSeeder.CyberIncidentTtxId  // 33333333-3333-3333-3333-333333333355
// ... etc
```

---

## Customization

### Skip Observations
```csharp
await app.SeedDemoDataAsync(seedObservations: false);
```

### Add Custom Demo Data
Extend `DemoDataSeeder.SeedAsync()` or create additional seeders following the same pattern.

### Change Demo Password
Modify `DemoUserSeeder.DemoPassword` constant (requires rebuild).

---

## Troubleshooting

### Demo data not appearing
1. Verify environment is not `Production`
2. Check logs for seeding errors
3. Verify `AddDemoSeeder()` is called in service registration

### Users can't log in
1. Verify password is `Demo123!`
2. Check `EmailConfirmed = true` in database
3. Verify user `Status = Active`

### Duplicate key errors
Seeders are idempotent. If errors occur:
1. Delete the demo organization (cascades to related data)
2. Re-run application to re-seed

### Reset demo data
```sql
-- Delete demo organization (cascades to all related data)
DELETE FROM Organizations WHERE Id = '11111111-1111-1111-1111-111111111111';

-- Delete demo users
DELETE FROM AspNetUsers WHERE OrganizationId = '11111111-1111-1111-1111-111111111111';
```

---

## Multi-Tenant Isolation

Demo data is completely isolated:

| Organization | ID | Purpose |
|--------------|-----|---------|
| Default Organization | `SystemConstants.DefaultOrganizationId` | Production users |
| Metro County EMA | `11111111-1111-1111-1111-111111111111` | Demo/UAT data |

Query filters ensure users only see data from their organization.
