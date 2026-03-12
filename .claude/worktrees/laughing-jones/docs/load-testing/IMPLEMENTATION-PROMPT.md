# Load Testing Implementation Prompt

> Copy everything below the line into a new Claude Code session to implement the load testing strategy.

---

## Task

Implement the load & stress testing strategy for Cadence as defined in `docs/load-testing/LOAD-TESTING-STRATEGY.md`. This creates a two-track testing system: **NBomber** (C# load testing with SignalR support) and **Azure Load Testing** (managed cloud service with App Insights correlation).

Read `docs/load-testing/LOAD-TESTING-STRATEGY.md` and `CLAUDE.md` before starting.

## Branch

Create branch `feature/load-stress-testing` from `main`.

## Implementation Phases

Work through these phases sequentially. Each phase must compile before moving to the next. Commit after each phase.

---

### Phase 1: NBomber Project Setup + Baseline Scenario

**Create `src/Cadence.LoadTests/` console app project.**

1. Create `Cadence.LoadTests.csproj`:
   - Target `net10.0`, `OutputType: Exe`
   - Dependencies: `NBomber 6.*`, `NBomber.Http 6.*`, `Microsoft.AspNetCore.SignalR.Client 10.*`
   - Project reference: `../Cadence.Core/Cadence.Core.csproj`

2. Create `Config/LoadTestSettings.cs`:
   - Settings class with `BaseUrl`, `HubUrl`, `DefaultTimeout`
   - Environment presets: `local` (`https://localhost:7001`), `staging` (from env var `CADENCE_STAGING_URL`)
   - Load from `appsettings.loadtest.json` or command-line args

3. Create `Config/TestUserPool.cs`:
   - Holds pre-configured test user credentials (email/password pairs)
   - At least 10 users for local, 200+ for staging
   - User pool assignment by role: Controllers, Evaluators, Directors, Observers

4. Create `Infrastructure/AuthHelper.cs`:
   - `LoginAsync(email, password)` -> calls `POST /api/auth/login`
   - Returns and caches JWT access token
   - Request body: `{ "email": "...", "password": "...", "rememberMe": true }`
   - Response includes `accessToken`, `expiresIn` (900s default)
   - Token refresh via `POST /api/auth/refresh` (uses HttpOnly cookie — must preserve cookies between requests)
   - Thread-safe token cache keyed by email
   - Auto-refresh tokens before expiry (at 80% of `expiresIn`)

5. Create `Infrastructure/ExerciseSeeder.cs`:
   - Creates a test organization (or uses existing "Load Test" org)
   - Creates test exercise via `POST /api/exercises`
   - Creates N test injects via `POST /api/exercises/{id}/injects` (bulk)
   - Assigns participants with roles via `POST /api/exercises/{id}/participants`
   - Cleanup method to delete seeded data after tests
   - All API calls use admin user token

6. Create `Scenarios/BaselineScenario.cs` (Scenario 1):
   - 1 virtual user, 30-second duration
   - Steps: Login -> GET `/api/exercises` -> GET `/api/exercises/{id}/injects` -> POST `/api/exercises/{id}/injects/{injectId}/fire` (with body `{ "notes": "Load test fire" }`) -> GET `/api/exercises/{id}/clock`
   - Pass criteria: All succeed, p50 < 200ms

7. Create `Program.cs`:
   - Parse command-line args: `--scenario`, `--target`, `--users`, `--duration`
   - Route to scenario classes
   - Example: `dotnet run -- --scenario baseline --target local`

**Verify:** `dotnet build` succeeds. Run baseline against local dev server if running.

**Commit:** `feat(load-tests): add NBomber project with baseline scenario`

---

### Phase 2: HTTP Load Scenarios (NBomber)

1. Create `Scenarios/MselReadScenario.cs` (Scenario 2):
   - Tiered load: 50 -> 100 -> 200 users (2 min each tier)
   - Each user: GET `/api/exercises/{id}/injects` every 5 seconds
   - This is the heaviest query (multiple Include joins)
   - Pass: p95 < 500ms at 50 users, p95 < 1000ms at 100

2. Create `Scenarios/InjectFireScenario.cs` (Scenario 3):
   - 5 Controller users, 3-minute duration
   - Each fires 1 inject every 6 seconds (~50 fires/min total)
   - POST `/api/exercises/{id}/injects/{injectId}/fire` with body `{ "notes": "Load test" }`
   - Only users with Controller role can fire
   - Pass: p95 < 300ms, zero 5xx errors

**Verify:** Both scenarios compile. Run MSEL reads at 10 users locally to validate.

**Commit:** `feat(load-tests): add MSEL read and inject fire scenarios`

---

### Phase 3: Azure Load Testing Infrastructure

1. Create `infra/load-testing.bicep`:
   - Resource: `Microsoft.LoadTestService/loadTests` (Standard tier)
   - Location: same region as App Service
   - Role assignment: `Load Test Contributor` for GitHub Actions service principal
   - Parameters: `location`, `loadTestName`, `principalId`

2. Create `src/Cadence.LoadTests/Azure/load-test-config.yaml`:
   ```yaml
   testName: cadence-load-test
   testPlan: cadence-load-test.jmx
   description: Cadence exercise conduct load tests
   engineInstances: 1
   configurationFiles:
     - csv/test-users.csv
   failureCriteria:
     - avg(response_time_ms) > 1000
     - percentage(error) > 5
   autoStop:
     errorPercentage: 90
     timeWindow: 60
   ```

3. Create `src/Cadence.LoadTests/Azure/cadence-load-test.jmx`:
   - JMeter test plan with these Thread Groups (each can be enabled/disabled via property):
   - **Auth Setup:** POST `/api/auth/login`, extract `accessToken` from JSON response, store in JMeter variable
   - **Smoke Test:** 1 user, 30s — sequential: login, GET exercises, GET injects, POST fire, GET clock
   - **MSEL Reads:** ${USERS} threads, ${DURATION}s — GET `/api/exercises/${EXERCISE_ID}/injects` with 5s constant timer
   - **Inject Fire:** ${CONTROLLERS} threads, 180s — POST fire with 6s constant timer
   - **Mixed Workload:** ${USERS} threads split by role proportion (Observer 83%, Controller 5%, Evaluator 10%, Director 2%)
   - **Stress Ramp:** Stepping thread group 50 -> ${MAX_USERS} over 15min
   - All requests include `Authorization: Bearer ${accessToken}` header
   - Use CSV Data Set Config for `csv/test-users.csv` (columns: email, password, role)

4. Create `src/Cadence.LoadTests/Azure/csv/test-users.csv`:
   - Template with columns: `email,password,role`
   - Placeholder rows (actual credentials populated per environment)

5. Create `.github/workflows/load-test-azure.yml`:
   ```yaml
   name: Azure Load Test
   on:
     workflow_dispatch:
       inputs:
         scenario:
           type: choice
           options: [smoke-50, full-200, stress-500]
         target:
           type: choice
           options: [staging]
           default: staging
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
             resourceGroup: ${{ vars.AZURE_RESOURCE_GROUP }}
             loadTestResource: cadence-load-test
             env: |
               [
                 { "name": "TARGET_URL", "value": "${{ vars.STAGING_API_URL }}" },
                 { "name": "EXERCISE_ID", "value": "${{ vars.LOAD_TEST_EXERCISE_ID }}" },
                 { "name": "USERS", "value": "50" },
                 { "name": "DURATION", "value": "300" },
                 { "name": "CONTROLLERS", "value": "5" },
                 { "name": "MAX_USERS", "value": "500" }
               ]
   ```

6. Create `.github/workflows/load-test-nbomber.yml`:
   - Manual trigger with scenario/target/users inputs
   - Runs on self-hosted runner (or ubuntu-latest with .NET 10)
   - Builds and runs `src/Cadence.LoadTests` with passed args
   - Uploads NBomber HTML report as artifact

**Verify:** Bicep template validates (`az bicep build`). JMeter file is valid XML. GitHub Actions workflows have correct syntax.

**Commit:** `feat(load-tests): add Azure Load Testing infrastructure and JMeter test plan`

---

### Phase 4: SignalR Testing (NBomber)

1. Create `Infrastructure/SignalRClientPool.cs`:
   - Manages N concurrent `HubConnection` instances
   - `CreateConnectionAsync(token)`:
     - `new HubConnectionBuilder().WithUrl(hubUrl, opts => opts.AccessTokenProvider = () => Task.FromResult(token))`
     - `await connection.StartAsync()`
   - `JoinExerciseAsync(connection, exerciseId)`:
     - `await connection.InvokeAsync("JoinExercise", exerciseId.ToString())`
   - Register event handlers for all SignalR events:
     - `InjectFired`, `InjectStatusChanged`, `InjectSkipped`
     - `ClockStarted`, `ClockPaused`, `ClockStopped`, `ClockReset`
     - `ObservationAdded`, `ObservationUpdated`, `ObservationDeleted`
   - Track connection state (Connected, Reconnecting, Disconnected)
   - Graceful cleanup: LeaveExercise + StopAsync + DisposeAsync

2. Create `Infrastructure/MetricsCollector.cs`:
   - Records `(eventName, sentAtUtc, receivedAtUtc)` for each SignalR event
   - Thread-safe collection (ConcurrentBag or Channel)
   - Broadcast latency = `max(receivedAtUtc across all clients) - sentAtUtc`
   - Message loss tracking: expected vs received count per event
   - Connection drop counter
   - Export metrics to NBomber custom reporting

3. Create `Scenarios/SignalRFanOutScenario.cs` (Scenario 5):
   - Tiered: 100 -> 200 -> 300 -> 500 SignalR connections
   - 1 Controller user fires injects at 10/min via HTTP
   - All other connections are passive listeners measuring broadcast latency
   - Measure: time from POST /fire HTTP response to last client receiving `InjectFired` event
   - Pass: Message delivery p95 < 500ms at 200 connections

4. Create `Scenarios/ClockStartScenario.cs` (Scenario 6):
   - Setup: Exercise with 200 injects (all with past-due scheduled times)
   - 50 connected SignalR clients
   - Action: POST `/api/exercises/{id}/clock/start`
   - Measure: time from start to all clients receiving clock + inject status events
   - Pass: Clock start completes within 10s, all broadcasts received within 15s

**Important Note:** Azure SignalR free tier only supports 20 concurrent connections. For local testing without Azure SignalR (direct SignalR), there is no connection limit. For staging tests with >20 connections, Azure SignalR Standard tier is required.

**Verify:** Compile succeeds. Run fan-out with 5 connections locally to validate SignalR handshake works.

**Commit:** `feat(load-tests): add SignalR client pool and broadcast latency scenarios`

---

### Phase 5: Full Workload + Stress Scenarios

1. Create `Scenarios/ExerciseConductScenario.cs` (Scenario 4 — THE KEY TEST):
   - 200 total users with role distribution:
     - 3 Directors: GET /injects every 10s + GET /clock every 15s
     - 10 Controllers: GET /injects every 5s + POST /fire every 30s
     - 20 Evaluators: GET /injects every 10s + POST /observations every 60s
     - 167 Observers: GET /injects every 10s
   - All 200 maintain SignalR connections (joined to exercise group)
   - Duration: 10 minutes
   - Combines HTTP load (NBomber scenarios) + SignalR monitoring (MetricsCollector)
   - Pass: p95 < 500ms reads, p95 < 1000ms writes, zero 5xx, SignalR delivery < 2s

2. Create `Scenarios/StressTestScenario.cs` (Scenario 7):
   - Ramp: 50 -> 500 users over 15 minutes (+50 every 2 min)
   - Mixed workload using Scenario 4 proportions
   - Find the user count where p95 > 2s or error rate > 1%
   - Output the breaking point: "B1 supports X concurrent users before degradation"

**Verify:** Compile succeeds. Run exercise conduct with 10 users locally.

**Commit:** `feat(load-tests): add full exercise conduct and stress test scenarios`

---

### Phase 6: Documentation + Polish

1. Create `src/Cadence.LoadTests/README.md`:
   - Quick start guide (prerequisites, setup, first run)
   - Scenario descriptions with expected results
   - How to run locally vs staging
   - How to trigger Azure Load Testing via GitHub Actions
   - Metrics reference and threshold table
   - Troubleshooting common issues
   - Azure Load Testing setup guide (Bicep deployment, secrets configuration)

2. Review and polish:
   - Ensure all scenarios handle auth errors gracefully (re-login on 401)
   - Ensure cleanup runs even on test failure (try/finally in seeder)
   - Add `--dry-run` flag to validate config without actually running load
   - Verify JMeter test plan matches NBomber scenarios for HTTP-only tests

**Commit:** `docs(load-tests): add README and usage documentation`

---

## Key API Reference

### Authentication
- `POST /api/auth/login` — Body: `{ "email", "password", "rememberMe" }` — Returns: `{ "accessToken", "expiresIn": 900 }`
- `POST /api/auth/refresh` — Uses HttpOnly cookie, returns new access token
- Rate limit: 10 req/min per IP on auth endpoints

### Exercise Conduct (all require `Authorization: Bearer {token}`)
- `GET /api/exercises` — List exercises
- `GET /api/exercises/{id}/injects` — Get all injects (heaviest query)
- `POST /api/exercises/{id}/injects/{injectId}/fire` — Fire inject (Controller+ role required). Body: `{ "notes": "..." }`
- `POST /api/exercises/{id}/injects/{injectId}/skip` — Skip inject. Body: `{ "reason": "..." }`
- `GET /api/exercises/{id}/clock` — Get clock state
- `POST /api/exercises/{id}/clock/start` — Start clock (Controller+ role)
- `POST /api/exercises/{id}/clock/pause` — Pause clock
- `POST /api/exercises/{id}/clock/stop` — Stop clock
- `POST /api/exercises/{id}/observations` — Create observation (Evaluator+ role). Body: `{ "content", "rating", "recommendation", "location", "injectId", "observedAt" }`
- `POST /api/exercises/{id}/participants` — Add participant. Body: `{ "userId", "exerciseRole" }`

### SignalR Hub
- URL: `/hubs/exercise`
- Client methods: `JoinExercise(exerciseId)`, `LeaveExercise(exerciseId)`
- Server events: `InjectFired`, `InjectStatusChanged`, `InjectSkipped`, `ClockStarted`, `ClockPaused`, `ClockStopped`, `ClockReset`, `ObservationAdded`, `ObservationUpdated`, `ObservationDeleted`

### Exercise Roles (per-exercise assignment)
- `ExerciseDirector` — Full control
- `Controller` — Fire/skip injects, manage clock
- `Evaluator` — Create observations
- `Observer` — View only

## Important Notes

- **Organization context:** All domain queries are scoped by organization. Test users must belong to an organization and have the org context set in their JWT (`org_id` claim). The seeder must handle this.
- **Access token expiry:** 15 minutes. For tests >10 min, implement proactive refresh at 80% expiry.
- **Rate limiting:** Auth endpoints are rate-limited (10/min/IP). Spread login calls over time or use pre-authenticated token pool.
- **Azure SignalR free tier:** Only 20 concurrent connections. Local dev (no Azure SignalR) has no limit. Staging needs Standard tier for >20 connections.
- **Inject state:** Injects can only be fired once (Pending -> Delivered). The seeder must create enough injects for the test duration, or reset inject status between fires.
- **Do not run against production.** All CI workflows target staging only.
