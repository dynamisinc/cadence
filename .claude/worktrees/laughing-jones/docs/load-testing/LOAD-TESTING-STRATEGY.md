# Load & Stress Testing Strategy for Cadence

## Context

Cadence is at V1 (UAT phase) heading toward production. The critical path is **real-time exercise conduct** — Controllers fire injects, triggering database writes and SignalR broadcasts to all connected participants. The app runs on Azure App Service B1 (1.75GB RAM) with Azure SQL and Azure SignalR Service.

No load testing has been done. We need to validate that the B1 tier can handle realistic exercise sizes (50-500 concurrent users) before production. Additionally, Azure Load Testing is a strategic capability the engineering team needs to develop across all products.

---

## Two-Track Approach

This strategy uses two complementary tools. Each covers scenarios the other cannot.

### Track A: NBomber + SignalR Client (Local/Custom)

**NBomber** (C# load testing framework):
- Reuses existing DTOs from `Cadence.Core` — no parallel type system
- Built-in HTML reports with latency percentiles, throughput, error rates
- Runs as a console app — no infrastructure needed
- **Required for:** SignalR WebSocket testing, custom broadcast latency measurement, clock start stress test

**`Microsoft.AspNetCore.SignalR.Client`** for WebSocket simulation:
- Official .NET SignalR client — exact same protocol as the React frontend
- Can join exercise groups and measure broadcast latency
- Azure Load Testing cannot do this

### Track B: Azure Load Testing (CI/Cloud)

Azure's managed load testing service — strategic investment for the engineering team across all products.

**What Azure Load Testing provides:**
- Native GitHub Actions integration (upload JMeter `.jmx`, trigger on demand or post-deploy)
- Auto-correlates with Application Insights (CPU, memory, DTU alongside HTTP latency)
- Geo-distributed load generation from multiple Azure regions
- Shareable dashboards and trend lines in Azure Portal
- Server-side metrics without custom instrumentation
- Reusable patterns across all Dynamis products

**Pricing (~$0.15/VUH for first 10K, $0.06/VUH after 10K):**
| Scenario | VUH | Est. Cost |
|----------|-----|-----------|
| Post-deploy smoke (50 users, 5 min) | 4 | ~$1 |
| Full exercise (200 users, 10 min) | 33 | ~$5 |
| Stress test (avg 275, 15 min) | 69 | ~$10 |
| Full suite, single run | ~125 | ~$19 |
| Weekly UAT cadence (4x/month) | ~500 | ~$75/month |

No monthly resource fee — pay only for VUH consumed.

### What Each Track Covers

| Scenario | NBomber | Azure Load Testing |
|----------|---------|-------------------|
| Baseline smoke test | Yes | Yes |
| MSEL read load | Yes | Yes |
| Inject fire burst | Yes | Yes |
| Full exercise conduct | Yes | Yes (HTTP only) |
| **SignalR fan-out** | **Yes (only)** | No |
| **Clock start stress** | **Yes (only)** | No |
| Stress test (find ceiling) | Yes | Yes |
| CI post-deploy gate | Manual | **Native** |
| App Insights correlation | Manual | **Automatic** |
| Geo-distributed load | No | **Yes** |

---

## Project Structure

New project: `src/Cadence.LoadTests/`

```
Cadence.LoadTests/
  Cadence.LoadTests.csproj          # References Cadence.Core for DTOs
  Config/
    LoadTestSettings.cs             # API URLs per environment (local/staging)
    TestUserPool.cs                 # Pre-seeded user credentials
  Infrastructure/
    AuthHelper.cs                   # Login + JWT token caching
    SignalRClientPool.cs            # Manages N concurrent hub connections
    ExerciseSeeder.cs               # Creates test exercise + injects via API
    MetricsCollector.cs             # Custom SignalR latency metrics
  Scenarios/
    BaselineScenario.cs             # 1 user, verify infrastructure works
    MselReadScenario.cs             # Concurrent GET /injects reads
    InjectFireScenario.cs           # Concurrent fire operations
    ExerciseConductScenario.cs      # Full mixed-workload (the key test)
    SignalRFanOutScenario.cs        # Broadcast latency at scale (NBomber only)
    ClockStartScenario.cs           # Heaviest single operation (NBomber only)
    StressTestScenario.cs           # Ramp to find the ceiling
  Azure/
    cadence-load-test.jmx           # JMeter test plan for HTTP scenarios
    load-test-config.yaml           # Azure Load Testing resource config
    csv/
      test-users.csv                # User credentials for JMeter (parameterized)
  README.md
```

GitHub Actions workflows:
```
.github/workflows/
  load-test-nbomber.yml             # Manual: run NBomber scenarios against staging
  load-test-azure.yml               # Manual: run Azure Load Testing against staging
```

Azure infrastructure (Bicep):
```
infra/
  load-testing.bicep                # Azure Load Testing resource provisioning
```

Dependencies (Cadence.LoadTests.csproj):
```xml
<PackageReference Include="NBomber" Version="6.*" />
<PackageReference Include="NBomber.Http" Version="6.*" />
<PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="10.*" />
<ProjectReference Include="..\Cadence.Core\Cadence.Core.csproj" />
```

Neither project runs in CI automatically — load tests take minutes and target live endpoints. Both are manual-trigger only.

---

## Test Scenarios

### Scenario 1: Baseline (Smoke Test)
- **Users:** 1
- **Duration:** 30 seconds
- **Actions:** Login -> GET exercise -> GET injects -> POST fire -> GET clock
- **Purpose:** Establish baseline latencies, verify test infrastructure
- **Pass:** All succeed, p50 < 200ms

### Scenario 2: MSEL Read Load
- **Users:** 50 -> 100 -> 200 (tiered)
- **Duration:** 2 min per tier
- **Actions:** Each user polls `GET /exercises/{id}/injects` every 5 seconds
- **Purpose:** Test read scaling — this is the heaviest query (5 Includes)
- **Pass:** p95 < 500ms at 50 users, p95 < 1000ms at 100 users

### Scenario 3: Inject Fire Burst
- **Users:** 5 Controllers
- **Duration:** 3 minutes
- **Actions:** Each fires 1 inject every 6 seconds (~50 fires/min total)
- **Purpose:** Test write path — `SaveChangesAsync()` + SignalR broadcast
- **Pass:** p95 < 300ms, zero 5xx errors

### Scenario 4: Full Exercise Conduct (THE KEY TEST)
- **Users:** 200 total — 3 Directors, 10 Controllers, 20 Evaluators, 167 Observers
- **Duration:** 10 minutes
- **Actions:** Mixed workload per role:
  - Observers: GET /injects every 10s
  - Controllers: GET /injects every 5s + POST /fire every 30s
  - Evaluators: GET /injects every 10s + POST /observations every 60s
  - Directors: GET /injects every 10s + GET /clock every 15s
- All 200 maintain SignalR connections
- **Purpose:** Simulate a realistic Full-Scale Exercise
- **Pass:** p95 < 500ms reads, p95 < 1000ms writes, zero 5xx, SignalR delivery < 2s

### Scenario 5: SignalR Fan-Out
- **Users:** 100 -> 200 -> 300 -> 500 SignalR connections
- **Actions:** 1 Controller fires at 10/min. Measure time from API return to last client receiving the event.
- **Purpose:** Isolate broadcast performance from API performance
- **Pass:** Message delivery p95 < 500ms at 200 connections

### Scenario 6: Clock Start Stress
- **Setup:** Exercise with 200 injects, all past-due, clock-driven mode, 50 connected users
- **Actions:** Start clock -> triggers full MSEL readiness evaluation -> 200 transition broadcasts
- **Purpose:** Test the single heaviest operation in the system
- **Pass:** Clock start completes within 10s, all broadcasts received within 15s

### Scenario 7: Stress Test (Find the Ceiling)
- **Users:** Ramp 50 -> 500 over 15 minutes (+50 every 2 min)
- **Actions:** Mixed workload (Scenario 4 proportions)
- **Purpose:** Find the user count where p95 > 2s or error rate > 1%
- **Output:** "B1 supports X concurrent users before degradation"

---

## Authentication Strategy

1. Pre-seed N test users in a dedicated "Load Test" organization (via `ExerciseSeeder`)
2. Login each via `POST /api/auth/login`, cache JWT tokens
3. Assign exercise roles (Controllers, Evaluators, etc.) via participants API
4. For tests >15 min, implement token refresh via `POST /api/auth/refresh`
5. For staging: use dedicated load test accounts, never real user accounts

---

## SignalR Connection Simulation

`SignalRClientPool` manages N `HubConnection` instances:

1. Create connection with JWT: `.WithUrl(hubUrl, opts => opts.AccessTokenProvider = () => token)`
2. `await connection.StartAsync()` — establishes WebSocket
3. `await connection.InvokeAsync("JoinExercise", exerciseId)` — joins group
4. Register handlers for `InjectFired`, `InjectStatusChanged`, `ClockStarted`, etc.
5. Each handler records `(eventName, receivedAtUtc)` for latency measurement
6. Broadcast latency = `max(receivedAtUtc across all clients) - firedAtUtc`

**Note:** Azure SignalR free tier supports only 20 connections. Local testing (SignalR disabled) allows unlimited. Staging needs Standard tier.

---

## Metrics & Thresholds

### HTTP API (captured by NBomber + Azure Load Testing)
| Endpoint | 50 users p95 | 200 users p95 | 500 users p95 |
|----------|-------------|---------------|---------------|
| GET /injects | < 200ms | < 500ms | < 1000ms |
| POST /fire | < 300ms | < 500ms | < 1000ms |
| Error rate | 0% | < 0.5% | < 2% |

### SignalR (NBomber custom metrics)
| Metric | Threshold |
|--------|-----------|
| Connection time p95 | < 2s |
| Message delivery p95 | < 500ms |
| Message loss | 0% |
| Connection drop rate | < 1% |

### Infrastructure (Azure Monitor / App Insights — auto-correlated by Azure Load Testing)
| Metric | Warning | Critical |
|--------|---------|----------|
| CPU | > 70% sustained | > 90% |
| Memory | > 75% (1.3GB) | > 90% (1.57GB) |
| SQL connection pool | > 60/100 | > 85/100 |
| SQL DTU | > 70% | > 90% |

---

## How to Run

```bash
# Local NBomber (backend must be running)
cd src/Cadence.LoadTests
dotnet run -- --scenario baseline --target local

# Specific scenario with user count
dotnet run -- --scenario full-exercise --users 200 --duration 600 --target local

# Stress test
dotnet run -- --scenario stress --target staging
```

### CI: NBomber (GitHub Actions — manual trigger)
```yaml
# .github/workflows/load-test-nbomber.yml
on:
  workflow_dispatch:
    inputs:
      scenario: { type: choice, options: [baseline, msel-reads, inject-fire, full-exercise, stress] }
      target: { type: choice, options: [staging] }
      users: { default: '50' }
```

### CI: Azure Load Testing (GitHub Actions — manual trigger)
```yaml
# .github/workflows/load-test-azure.yml
on:
  workflow_dispatch:
    inputs:
      scenario: { type: choice, options: [smoke-50, full-200, stress-500] }
jobs:
  load-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}
      - uses: azure/load-testing@v1
        with:
          loadTestConfigFile: src/Cadence.LoadTests/Azure/load-test-config.yaml
          resourceGroup: cadence-rg
          loadTestResource: cadence-load-test
          env: |
            [
              { "name": "TARGET_URL", "value": "${{ vars.STAGING_URL }}" },
              { "name": "SCENARIO", "value": "${{ inputs.scenario }}" }
            ]
```

Never auto-run against production. Both workflows target staging only.

---

## Interpreting Results

| Pattern | Cause | Fix |
|---------|-------|-----|
| Linear latency increase with users | Normal CPU-bound scaling | Expected on B1 |
| Sudden latency spike at N users | Resource cliff | Check SQL pool (add `Max Pool Size=200`), thread pool (`SetMinThreads`), or memory |
| 500 errors under load | Concurrency bug | Check logs — likely optimistic concurrency or timeout |
| SignalR message loss | Connection drops or throttling | Check Azure SignalR tier limits, reduce payload size |
| Clock start >10s with 200 injects | Readiness evaluation not batched | Batch `SaveChangesAsync()` in readiness service |
| Memory >1.5GB at 200 users | Large object heap from DTO serialization | Profile with `dotnet-counters`, reduce InjectDto broadcast payload |

### Decision after stress test:
| Result | Action |
|--------|--------|
| B1 handles 200 users | Sufficient for FSE customers |
| B1 fails at 150 users | Scale to S1 for FSE, keep B1 for TTX/FE |
| SQL pool exhaustion | Add `Max Pool Size=200` to connection string |
| SignalR delivery >2s | Trim InjectDto broadcast payload (strip null fields) |

---

## Implementation Sequence

### Phase 1: NBomber Foundation (Week 1-2)
1. Project setup (`Cadence.LoadTests.csproj`, config, dependencies)
2. `AuthHelper` — login + JWT caching
3. `ExerciseSeeder` — create test exercise + injects via API
4. Scenario 1: Baseline (smoke test, 1 user)
5. Scenario 2: MSEL Reads (50-200 users)
6. Scenario 3: Inject Fire Burst (5 controllers)

### Phase 2: Azure Load Testing Setup (Week 2-3)
7. Provision Azure Load Testing resource (Bicep template `infra/load-testing.bicep`)
8. Create JMeter test plan (`cadence-load-test.jmx`) covering HTTP-only scenarios
9. Create `load-test-config.yaml` with parameterized environment variables
10. Create `csv/test-users.csv` for JMeter credential parameterization
11. Create GitHub Actions workflow (`load-test-azure.yml`)
12. Validate: run smoke test against staging, verify App Insights correlation

### Phase 3: SignalR Testing (Week 3-4)
13. `SignalRClientPool` — manage N concurrent hub connections
14. `MetricsCollector` — custom broadcast latency measurement
15. Scenario 5: SignalR Fan-Out (100-500 connections) — NBomber only
16. Scenario 6: Clock Start Stress — NBomber only

### Phase 4: Full Workload + Stress (Week 4-5)
17. Scenario 4: Full Exercise Conduct (NBomber, 200 users, mixed roles + SignalR)
18. Scenario 7: Stress Test / Find the Ceiling (NBomber ramp to 500)
19. Run same HTTP scenarios via Azure Load Testing for comparison + App Insights dashboards
20. Create NBomber CI workflow (`load-test-nbomber.yml`)

### Phase 5: Documentation + Cross-Product Template (Week 5)
21. README with usage guide, scenario descriptions, threshold reference
22. Document Azure Load Testing setup as a reusable template for other Dynamis products
23. Archive baseline results for future comparison

---

## Azure Infrastructure

### Bicep Template (`infra/load-testing.bicep`)

Provisions:
- `Microsoft.LoadTestService/loadTests` resource
- Role assignment for GitHub Actions service principal (`Load Test Contributor`)

### JMeter Test Plan Structure

The `.jmx` file contains Thread Groups for each HTTP scenario, parameterized via CSV and environment variables:

| Thread Group | Users | Duration | Endpoints |
|-------------|-------|----------|-----------|
| Smoke | 1 | 30s | Login -> GET exercise -> GET injects -> POST fire |
| MSEL Reads | ${USERS} | ${DURATION} | GET /exercises/{id}/injects every 5s |
| Inject Fire | ${CONTROLLERS} | 3min | POST /fire every 6s |
| Mixed Workload | ${USERS} | 10min | Role-proportioned reads + writes |
| Stress Ramp | 50->${MAX_USERS} | 15min | Mixed workload, ramping |

User credentials come from `csv/test-users.csv` (same pool as NBomber).

---

## Verification

### NBomber Verification
- Run Baseline scenario against local dev — should produce HTML report with all metrics
- Run MSEL Read at 50 users — verify p95 < 200ms locally
- Run Inject Fire at 5 users — verify zero errors
- Check SignalR connections establish and receive events
- Verify test cleanup removes seeded data

### Azure Load Testing Verification
- Verify Bicep template deploys Azure Load Testing resource successfully
- Run smoke test via GitHub Actions against staging
- Confirm App Insights shows correlated metrics (HTTP latency + CPU + memory + DTU)
- Verify JMeter parameterization works (user count, duration, target URL)
- Compare Azure Load Testing results with NBomber results for same HTTP scenarios (should match within 10%)
