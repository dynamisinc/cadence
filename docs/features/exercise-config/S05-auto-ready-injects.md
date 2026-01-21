# Story CLK-05: Auto-Ready Injects When Clock Reaches DeliveryTime

> **Story ID:** CLK-05
> **Feature:** Exercise Clock Modes
> **Phase:** D - Exercise Conduct
> **Status:** Ready for Development
> **Estimate:** Large (3-5 days)

---

## User Story

**As a** system,
**I want** pending injects to automatically transition to "Ready" when the exercise clock reaches their DeliveryTime,
**So that** Controllers are notified without manual intervention in clock-driven mode.

---

## Background

This is the core feature that enables clock-driven exercise conduct. When the exercise clock is running and reaches an inject's DeliveryTime, that inject should automatically transition from Pending to Ready, alerting Controllers that it needs attention.

**Key Considerations:**
- Must work across multiple connected clients
- Must be resilient to network interruptions
- Must handle clock pause/resume correctly
- Must not create race conditions with manual firing

---

## Scope

### In Scope
- Backend service to evaluate inject readiness
- Timer-based checking mechanism in App Service
- Transition Pending → Ready when DeliveryTime reached
- SignalR broadcast for Ready transitions
- Handle clock pause (no new Ready transitions while paused)
- Handle clock resume (evaluate all pending injects)

### Out of Scope
- Auto-fire (injects become Ready, not automatically Fired)
- Conditional triggers (future feature)
- Client-side calculation (server is authoritative)

---

## Acceptance Criteria

- [ ] **Given** a clock-driven exercise with clock running, **when** elapsed time ≥ inject's DeliveryTime, **then** inject status changes to Ready
- [ ] **Given** a clock-driven exercise with clock running, **when** inject becomes Ready, **then** SignalR broadcasts `InjectReadyToFire` event
- [ ] **Given** a facilitator-paced exercise, **when** time passes, **then** injects do NOT auto-Ready
- [ ] **Given** clock is paused, **when** DeliveryTime would be reached, **then** no Ready transitions occur
- [ ] **Given** clock is resumed, **when** any pending inject's DeliveryTime ≤ current elapsed, **then** those injects become Ready
- [ ] **Given** multiple injects reach their DeliveryTime simultaneously, **when** processed, **then** all become Ready with individual events
- [ ] **Given** an inject is already Ready/Fired/Skipped, **when** clock passes DeliveryTime, **then** no change occurs
- [ ] **Given** an inject without DeliveryTime set, **when** clock runs, **then** that inject is never auto-Ready

---

## Technical Design

### Architecture Options

**Option A: Background Timer in App Service (Recommended)**
- HostedService runs every N seconds
- Queries all active clock-driven exercises
- Evaluates pending injects against elapsed time
- Server-authoritative, consistent across clients

**Option B: Client-Side Calculation**
- Clients calculate Ready status locally
- Server validates on Fire action
- Potential inconsistency between clients

**Option C: Hybrid**
- Server authoritative for transitions
- Clients optimistically show Ready
- Best UX but more complex

**Recommendation:** Option A for MVP - simpler, consistent, authoritative.

### InjectReadinessService

Create `src/Cadence.Core/Features/Injects/Services/InjectReadinessService.cs`:

```csharp
public interface IInjectReadinessService
{
    /// <summary>
    /// Evaluates all active exercises and transitions ready injects.
    /// Called periodically by background timer.
    /// </summary>
    Task EvaluateAllExercisesAsync();

    /// <summary>
    /// Evaluates a specific exercise for ready injects.
    /// Called when clock starts or resumes.
    /// </summary>
    Task EvaluateExerciseAsync(Guid exerciseId);
}

public class InjectReadinessService : IInjectReadinessService
{
    private readonly AppDbContext _context;
    private readonly IExerciseHubContext _hubContext;
    private readonly ILogger<InjectReadinessService> _logger;

    public async Task EvaluateAllExercisesAsync()
    {
        // Find all active, clock-driven exercises with running clock
        var exercises = await _context.Exercises
            .Where(e => e.Status == ExerciseStatus.Active)
            .Where(e => e.DeliveryMode == DeliveryMode.ClockDriven)
            .Where(e => e.ClockState == ExerciseClockState.Running)
            .ToListAsync();

        foreach (var exercise in exercises)
        {
            await EvaluateExerciseAsync(exercise.Id);
        }
    }

    public async Task EvaluateExerciseAsync(Guid exerciseId)
    {
        var exercise = await _context.Exercises
            .Include(e => e.ActiveMsel)
                .ThenInclude(m => m.Injects)
            .FirstOrDefaultAsync(e => e.Id == exerciseId);

        if (exercise == null) return;
        if (exercise.DeliveryMode != DeliveryMode.ClockDriven) return;
        if (exercise.ClockState != ExerciseClockState.Running) return;

        var elapsedTime = CalculateElapsedTime(exercise);
        var readyInjects = new List<Inject>();

        foreach (var inject in exercise.ActiveMsel.Injects)
        {
            if (inject.Status != InjectStatus.Pending) continue;
            if (!inject.DeliveryTime.HasValue) continue;
            if (inject.DeliveryTime.Value > elapsedTime) continue;

            // Transition to Ready
            inject.Status = InjectStatus.Ready;
            inject.ReadyAt = DateTime.UtcNow;
            readyInjects.Add(inject);
        }

        if (readyInjects.Any())
        {
            await _context.SaveChangesAsync();

            // Broadcast each Ready transition
            foreach (var inject in readyInjects)
            {
                await _hubContext.NotifyInjectReadyToFire(exerciseId, inject.ToDto());
            }

            _logger.LogInformation(
                "Transitioned {Count} injects to Ready for exercise {ExerciseId}",
                readyInjects.Count, exerciseId);
        }
    }

    private TimeSpan CalculateElapsedTime(Exercise exercise)
    {
        var elapsed = exercise.ClockElapsedBeforePause ?? TimeSpan.Zero;
        if (exercise.ClockStartedAt.HasValue)
        {
            elapsed += DateTime.UtcNow - exercise.ClockStartedAt.Value;
        }
        return elapsed;
    }
}
```

### Background Timer

Create `src/Cadence.WebApi/Services/InjectReadinessBackgroundService.cs`:

```csharp
public class InjectReadinessBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InjectReadinessBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Inject readiness background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider
                    .GetRequiredService<IInjectReadinessService>();

                await service.EvaluateAllExercisesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating inject readiness");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}
```

### Register Services

In `Program.cs`:

```csharp
builder.Services.AddScoped<IInjectReadinessService, InjectReadinessService>();
builder.Services.AddHostedService<InjectReadinessBackgroundService>();
```

### Clock Event Integration

Update `ExerciseClockService` to trigger immediate evaluation:

```csharp
public async Task<ClockStateDto> StartClockAsync(Guid exerciseId, Guid startedBy)
{
    // ... existing start logic ...

    // Immediately evaluate for any past-due injects
    await _injectReadinessService.EvaluateExerciseAsync(exerciseId);

    return exercise.ToClockStateDto();
}

public async Task<ClockStateDto> ResumeClockAsync(Guid exerciseId, Guid resumedBy)
{
    // ... existing resume logic ...

    // Evaluate for any injects that became ready while paused
    await _injectReadinessService.EvaluateExerciseAsync(exerciseId);

    return exercise.ToClockStateDto();
}
```

### SignalR Event

Add to `IExerciseHubContext`:

```csharp
Task NotifyInjectReadyToFire(Guid exerciseId, InjectDto inject);
```

Implement in `ExerciseHubContext`:

```csharp
public async Task NotifyInjectReadyToFire(Guid exerciseId, InjectDto inject)
{
    await _hubContext.Clients
        .Group($"exercise-{exerciseId}")
        .SendAsync("InjectReadyToFire", inject);

    // Also send generic status change
    await NotifyInjectStatusChanged(exerciseId, inject);
}
```

### Frontend Handler

Update SignalR subscription in conduct page:

```typescript
useEffect(() => {
  if (!connection) return;

  connection.on('InjectReadyToFire', (inject: InjectDto) => {
    // Play notification sound (optional)
    playReadySound();

    // Show toast notification
    toast.info(`Inject #${inject.injectNumber} is ready to fire`);

    // Invalidate queries to refresh list
    queryClient.invalidateQueries(['injects', exerciseId]);
  });

  return () => connection.off('InjectReadyToFire');
}, [connection, exerciseId]);
```

---

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Clock paused at 29:59, inject due at 30:00 | No Ready transition while paused |
| Clock resumes at 29:59, inject due at 30:00 | Ready in ~1 second when check runs |
| Inject fired manually before auto-Ready | No Ready transition (already Fired) |
| Server restart while clock running | Background service resumes checking |
| Network disconnect during Ready | Client reconnects, receives current state |
| Multiple exercises active simultaneously | Each evaluated independently |
| 100 injects become ready at once | All transition, events batched reasonably |

---

## Performance Considerations

1. **Check Interval:** 5 seconds balances responsiveness vs load
2. **Database Queries:** Batch reads, minimize round trips
3. **SignalR Events:** Consider batching if many injects ready simultaneously
4. **Index:** Add index on `(MselId, Status, DeliveryTime)` for efficient queries

### Suggested Index

```sql
CREATE INDEX IX_Injects_ReadinessCheck
ON Injects (MselId, Status, DeliveryTime)
WHERE Status = 0 AND DeliveryTime IS NOT NULL;
```

---

## Test Cases

### Backend Unit Tests

```csharp
[Fact]
public async Task EvaluateExercise_ClockDriven_TransitionsPendingToReady()

[Fact]
public async Task EvaluateExercise_FacilitatorPaced_NoTransitions()

[Fact]
public async Task EvaluateExercise_ClockPaused_NoTransitions()

[Fact]
public async Task EvaluateExercise_MultipleInjectsReady_AllTransition()

[Fact]
public async Task EvaluateExercise_AlreadyFired_NoChange()

[Fact]
public async Task EvaluateExercise_NoDeliveryTime_SkipsInject()

[Fact]
public async Task EvaluateExercise_BroadcastsSignalREvent()
```

### Integration Tests

```csharp
[Fact]
public async Task BackgroundService_EvaluatesActiveExercises()

[Fact]
public async Task ClockStart_ImmediatelyEvaluatesReadiness()

[Fact]
public async Task ClockResume_EvaluatesPastDueInjects()
```

### Frontend Tests

```typescript
describe('InjectReadyToFire SignalR event', () => {
  it('updates inject list when received');
  it('shows notification toast');
  it('moves inject to Ready section');
});
```

---

## Dependencies

| Dependency | Status | Notes |
|------------|--------|-------|
| CLK-01: Timing fields | 🔲 Required | Need DeliveryMode |
| CLK-02: DeliveryTime field | 🔲 Required | Need DeliveryTime on inject |
| CLK-04: Ready status | 🔲 Required | Need Ready enum value |
| SignalR infrastructure | ✅ Complete | Hub and events exist |

---

## Blocked By

- CLK-01: Add timing configuration fields to Exercise entity
- CLK-02: Add DeliveryTime field to Inject entity
- CLK-04: Add "Ready" status to inject workflow

---

## Blocks

- CLK-06: Clock-driven conduct view (needs Ready injects to display)

---

## Notes

- Consider adding audio/visual notification when injects become Ready
- The 5-second check interval means injects may become Ready up to 5 seconds after their DeliveryTime
- For time-critical exercises, consider reducing interval to 1-2 seconds
- Future enhancement: WebSocket push for sub-second accuracy
