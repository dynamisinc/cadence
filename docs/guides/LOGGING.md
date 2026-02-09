# Logging Guide

## Overview

Cadence uses **Serilog** as its structured logging framework, replacing the default `Microsoft.Extensions.Logging` pipeline. Serilog integrates as a provider behind the standard `ILogger<T>` interface, so all existing logging code works unchanged.

### Architecture

```
ILogger<T> (Microsoft.Extensions.Logging)
    └── Serilog Provider
         ├── Console Sink (all environments)
         ├── File Sink (Development only)
         └── Application Insights Sink (Azure environments)
```

- **Cadence.Core** uses `ILogger<T>` only (no Serilog dependency)
- **Cadence.WebApi** configures Serilog as the logging pipeline
- **Cadence.Functions** configures Serilog independently

## Sink Configuration by Environment

| Environment | Console | File | Application Insights |
|-------------|---------|------|---------------------|
| Development | Yes | Yes (`logs/cadence-{date}.log`) | No |
| UAT | Yes | No | Yes |
| Production | Yes | No | Yes |

## Configuration

### appsettings.json (Base)

The `Serilog` section in `appsettings.json` defines the base configuration. Environment-specific overrides are in `appsettings.{Environment}.json`.

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "Cadence": "Information"
      }
    },
    "WriteTo": [
      { "Name": "Console", "Args": { ... } }
    ]
  }
}
```

### Adjusting Log Levels

To change log levels for a specific namespace, update the `Override` section:

```json
"Override": {
  "Cadence.Core.Features.Injects": "Debug",
  "Microsoft.EntityFrameworkCore.Database.Command": "Information"
}
```

### Adding Application Insights in Azure

In Azure App Service Configuration, add these app settings:

```
Serilog__WriteTo__1__Name = ApplicationInsights
Serilog__WriteTo__1__Args__connectionString = <your-connection-string>
Serilog__WriteTo__1__Args__telemetryConverter = Serilog.Sinks.ApplicationInsights.TelemetryConverters.TraceTelemetryConverter, Serilog.Sinks.ApplicationInsights
```

## Structured Logging Best Practices

### Use Named Parameters (NOT String Interpolation)

```csharp
// GOOD - structured logging (properties are indexed)
_logger.LogInformation("Exercise {ExerciseId} fired inject {InjectId}", exerciseId, injectId);

// BAD - string interpolation (loses structure)
_logger.LogInformation($"Exercise {exerciseId} fired inject {injectId}");
```

### Property Naming

Use PascalCase for log property names:

| Property | Usage |
|----------|-------|
| `ExerciseId` | Exercise identifier |
| `InjectId` | Inject identifier |
| `UserId` | Authenticated user |
| `OrganizationId` | Current organization |
| `Duration` | Operation timing (ms) |

### Log Levels

| Level | When to Use |
|-------|-------------|
| `Debug` | Detailed diagnostic info (dev only) |
| `Information` | Normal operations, state changes, key business events |
| `Warning` | Unexpected but recoverable situations (e.g., validation failures) |
| `Error` | Failures that prevent an operation from completing |
| `Fatal` | Application-level failures requiring immediate attention |

### Use LoggingExtensions

The project provides extension methods in `Cadence.Core/Core/Logging/LoggingExtensions.cs`:

```csharp
_logger.LogOperationStart("FireInject", injectId);
_logger.LogOperationSuccess("FireInject", injectId, stopwatch.ElapsedMilliseconds);
_logger.LogEntityCreated<Inject>(inject.Id);
_logger.LogSlowOperation("DatabaseQuery", elapsed);
```

## Request Context Enrichment

The `SerilogContextMiddleware` automatically adds these properties to every log entry within an HTTP request:

- **UserId** - from JWT `sub` claim
- **OrganizationId** - from JWT `org_id` claim
- **ExerciseId** - from route values (when available)

Additionally, `UseSerilogRequestLogging` enriches each request summary with:

- **RequestHost** - the request host header
- **UserAgent** - the client user agent
- **ExerciseId** - from route values

## Adding New Enrichment Properties

To add a new property to all log entries in a request scope:

1. Edit `SerilogContextMiddleware.cs`
2. Add a new `LogContext.PushProperty()` call:

```csharp
using (LogContext.PushProperty("MyProperty", value ?? "default"))
{
    await _next(context);
}
```

## File Logging (Development)

- **Location:** `src/Cadence.WebApi/logs/cadence-{date}.log`
- **Rolling:** Daily, 7 files retained
- **Size limit:** 10 MB per file
- **Format:** `{Timestamp} [{Level}] ({SourceContext}) {Message}`

The `logs/` directory is in `.gitignore`.

## Querying Logs in Application Insights

### Kusto Query Examples

**All errors in the last hour:**
```kusto
traces
| where timestamp > ago(1h)
| where severityLevel >= 3
| order by timestamp desc
```

**Logs for a specific exercise:**
```kusto
traces
| where customDimensions.ExerciseId == "your-exercise-id"
| order by timestamp desc
```

**Logs for a specific user:**
```kusto
traces
| where customDimensions.UserId == "user-guid"
| order by timestamp desc
```

**Slow operations:**
```kusto
traces
| where message contains "slow"
| order by timestamp desc
```

**Failed HTTP requests with details:**
```kusto
traces
| where customDimensions.StatusCode >= 400
| project timestamp, message, customDimensions.StatusCode, customDimensions.RequestPath
| order by timestamp desc
```

## Two-Stage Bootstrap

Serilog uses a two-stage initialization pattern in `Program.cs`:

1. **Bootstrap logger** - Created before configuration is loaded, writes to Console only. Captures startup failures.
2. **Full logger** - Created after `WebApplicationBuilder` loads config, uses all configured sinks.

If the application fails to start (e.g., bad config, missing DB), the bootstrap logger ensures the fatal error is captured.
