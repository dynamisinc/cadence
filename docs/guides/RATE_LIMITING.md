# Rate Limiting Guide

> **Status:** Reference Guide - Not Yet Implemented in Template
> **Priority:** Medium for Production

This guide explains how to implement rate limiting to protect your API from abuse.

---

## Table of Contents

1. [Overview](#overview)
2. [Rate Limiting Strategies](#rate-limiting-strategies)
3. [Implementation](#implementation)
4. [Frontend Handling](#frontend-handling)
5. [Monitoring](#monitoring)
6. [Testing](#testing)

---

## Overview

### Why Rate Limit?

- **Prevent abuse** - Stop malicious actors from overwhelming your API
- **Fair usage** - Ensure all users get reasonable access
- **Cost control** - Limit Azure consumption costs
- **Stability** - Protect backend services from overload

### Current State

The template has no rate limiting. Any client can make unlimited requests.

### Target State

Rate limits based on:
- **Per-user limits** - Authenticated users get quotas
- **Per-IP limits** - Anonymous requests limited by IP
- **Per-endpoint limits** - Different limits for different operations

---

## Rate Limiting Strategies

### Fixed Window

Count requests in fixed time windows (e.g., 100 requests per minute).

```
Window: 12:00:00 - 12:01:00 → 100 requests allowed
Window: 12:01:00 - 12:02:00 → 100 requests allowed (reset)
```

**Pros:** Simple to implement
**Cons:** Burst at window boundaries

### Sliding Window

Rolling window that smooths out bursts.

```
At 12:00:30, looking back 60 seconds:
- Requests from 12:00:00-12:00:30: 50
- Requests from 11:59:30-12:00:00: 60 (weighted at 50%)
- Total: 50 + 30 = 80 of 100 allowed
```

**Pros:** Smoother limits
**Cons:** More complex

### Token Bucket

Tokens replenish over time. Each request consumes a token.

```
Bucket: 100 tokens max, refill 10/second
- Request costs 1 token
- Burst: Can use all 100 immediately
- Sustained: 10 requests/second
```

**Pros:** Allows controlled bursts
**Cons:** More complex state management

### Recommendation

Start with **Fixed Window** for simplicity, upgrade to **Sliding Window** if needed.

---

## Implementation

### Option 1: Azure API Management (Recommended for Production)

Azure API Management provides built-in rate limiting with minimal code:

```xml
<!-- APIM Policy -->
<inbound>
    <rate-limit-by-key
        calls="100"
        renewal-period="60"
        counter-key="@(context.Request.Headers.GetValueOrDefault("X-User-Id", context.Request.IpAddress))"
        increment-condition="@(context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)" />
</inbound>
```

### Option 2: Custom Middleware with Redis

For Azure Functions without APIM, use Redis for distributed rate limiting.

#### Step 1: Install Packages

```bash
cd src/api
dotnet add package StackExchange.Redis
```

#### Step 2: Create Rate Limiter Service

Create `src/api/Core/RateLimiting/RateLimiterService.cs`:

```csharp
using StackExchange.Redis;

namespace DynamisReferenceApp.Api.Core.RateLimiting;

public interface IRateLimiterService
{
    Task<RateLimitResult> CheckRateLimitAsync(string key, RateLimitPolicy policy);
}

public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public int Remaining { get; set; }
    public int Limit { get; set; }
    public DateTime ResetTime { get; set; }
    public TimeSpan RetryAfter { get; set; }
}

public class RateLimitPolicy
{
    public int RequestsPerWindow { get; set; } = 100;
    public TimeSpan WindowSize { get; set; } = TimeSpan.FromMinutes(1);
}

public class RedisRateLimiterService : IRateLimiterService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisRateLimiterService> _logger;

    public RedisRateLimiterService(
        IConnectionMultiplexer redis,
        ILogger<RedisRateLimiterService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<RateLimitResult> CheckRateLimitAsync(string key, RateLimitPolicy policy)
    {
        var db = _redis.GetDatabase();
        var now = DateTime.UtcNow;
        var windowKey = $"ratelimit:{key}:{GetWindowId(now, policy.WindowSize)}";

        try
        {
            // Increment counter and set expiry atomically
            var count = await db.StringIncrementAsync(windowKey);

            if (count == 1)
            {
                // First request in window - set expiry
                await db.KeyExpireAsync(windowKey, policy.WindowSize);
            }

            var isAllowed = count <= policy.RequestsPerWindow;
            var remaining = Math.Max(0, policy.RequestsPerWindow - (int)count);
            var resetTime = GetWindowResetTime(now, policy.WindowSize);

            return new RateLimitResult
            {
                IsAllowed = isAllowed,
                Remaining = remaining,
                Limit = policy.RequestsPerWindow,
                ResetTime = resetTime,
                RetryAfter = isAllowed ? TimeSpan.Zero : resetTime - now
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rate limiting check failed for key {Key}", key);

            // Fail open - allow request if Redis is down
            return new RateLimitResult
            {
                IsAllowed = true,
                Remaining = policy.RequestsPerWindow,
                Limit = policy.RequestsPerWindow,
                ResetTime = now.AddSeconds(60)
            };
        }
    }

    private static string GetWindowId(DateTime time, TimeSpan windowSize)
    {
        var ticks = time.Ticks / windowSize.Ticks;
        return ticks.ToString();
    }

    private static DateTime GetWindowResetTime(DateTime time, TimeSpan windowSize)
    {
        var ticks = time.Ticks / windowSize.Ticks;
        return new DateTime((ticks + 1) * windowSize.Ticks, DateTimeKind.Utc);
    }
}

/// <summary>
/// In-memory rate limiter for development/testing.
/// NOT suitable for production with multiple instances.
/// </summary>
public class InMemoryRateLimiterService : IRateLimiterService
{
    private readonly Dictionary<string, (int Count, DateTime ResetTime)> _counters = new();
    private readonly object _lock = new();

    public Task<RateLimitResult> CheckRateLimitAsync(string key, RateLimitPolicy policy)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var windowKey = $"{key}:{GetWindowId(now, policy.WindowSize)}";

            // Clean up old entries
            var expiredKeys = _counters
                .Where(kv => kv.Value.ResetTime < now)
                .Select(kv => kv.Key)
                .ToList();
            foreach (var expiredKey in expiredKeys)
            {
                _counters.Remove(expiredKey);
            }

            // Get or create counter
            if (!_counters.TryGetValue(windowKey, out var counter))
            {
                counter = (0, GetWindowResetTime(now, policy.WindowSize));
            }

            // Increment
            counter = (counter.Count + 1, counter.ResetTime);
            _counters[windowKey] = counter;

            var isAllowed = counter.Count <= policy.RequestsPerWindow;
            var remaining = Math.Max(0, policy.RequestsPerWindow - counter.Count);

            return Task.FromResult(new RateLimitResult
            {
                IsAllowed = isAllowed,
                Remaining = remaining,
                Limit = policy.RequestsPerWindow,
                ResetTime = counter.ResetTime,
                RetryAfter = isAllowed ? TimeSpan.Zero : counter.ResetTime - now
            });
        }
    }

    private static string GetWindowId(DateTime time, TimeSpan windowSize)
    {
        var ticks = time.Ticks / windowSize.Ticks;
        return ticks.ToString();
    }

    private static DateTime GetWindowResetTime(DateTime time, TimeSpan windowSize)
    {
        var ticks = time.Ticks / windowSize.Ticks;
        return new DateTime((ticks + 1) * windowSize.Ticks, DateTimeKind.Utc);
    }
}
```

#### Step 3: Create Rate Limiting Middleware

Create `src/api/Core/Middleware/RateLimitingMiddleware.cs`:

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using DynamisReferenceApp.Api.Core.RateLimiting;

namespace DynamisReferenceApp.Api.Core.Middleware;

public class RateLimitingMiddleware : IFunctionsWorkerMiddleware
{
    private readonly IRateLimiterService _rateLimiter;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitConfiguration _config;

    public RateLimitingMiddleware(
        IRateLimiterService rateLimiter,
        IConfiguration configuration,
        ILogger<RateLimitingMiddleware> logger)
    {
        _rateLimiter = rateLimiter;
        _logger = logger;
        _config = configuration.GetSection("RateLimiting").Get<RateLimitConfiguration>()
                  ?? new RateLimitConfiguration();
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext == null)
        {
            await next(context);
            return;
        }

        var request = httpContext.Request;

        // Skip rate limiting for health endpoints
        if (request.Path.StartsWithSegments("/api/health") ||
            request.Path.StartsWithSegments("/api/ping"))
        {
            await next(context);
            return;
        }

        // Determine rate limit key (user ID or IP)
        var userId = request.Headers["X-User-Id"].FirstOrDefault();
        var clientIp = GetClientIp(request);
        var rateLimitKey = !string.IsNullOrEmpty(userId) ? $"user:{userId}" : $"ip:{clientIp}";

        // Determine policy based on endpoint
        var policy = GetPolicyForEndpoint(request.Path, request.Method);

        // Check rate limit
        var result = await _rateLimiter.CheckRateLimitAsync(rateLimitKey, policy);

        // Add rate limit headers
        httpContext.Response.Headers["X-RateLimit-Limit"] = result.Limit.ToString();
        httpContext.Response.Headers["X-RateLimit-Remaining"] = result.Remaining.ToString();
        httpContext.Response.Headers["X-RateLimit-Reset"] = result.ResetTime.ToString("O");

        if (!result.IsAllowed)
        {
            _logger.LogWarning(
                "Rate limit exceeded for {Key}. Limit: {Limit}, Reset: {Reset}",
                rateLimitKey, result.Limit, result.ResetTime);

            httpContext.Response.Headers["Retry-After"] = ((int)result.RetryAfter.TotalSeconds).ToString();
            httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

            await httpContext.Response.WriteAsJsonAsync(new
            {
                error = "Too Many Requests",
                message = "Rate limit exceeded. Please try again later.",
                retryAfter = (int)result.RetryAfter.TotalSeconds
            });

            return;
        }

        await next(context);
    }

    private RateLimitPolicy GetPolicyForEndpoint(PathString path, string method)
    {
        // More restrictive limits for write operations
        if (method is "POST" or "PUT" or "DELETE")
        {
            return new RateLimitPolicy
            {
                RequestsPerWindow = _config.WriteRequestsPerMinute,
                WindowSize = TimeSpan.FromMinutes(1)
            };
        }

        // Standard limits for read operations
        return new RateLimitPolicy
        {
            RequestsPerWindow = _config.ReadRequestsPerMinute,
            WindowSize = TimeSpan.FromMinutes(1)
        };
    }

    private static string GetClientIp(HttpRequest request)
    {
        // Check X-Forwarded-For header (set by Azure)
        var forwardedFor = request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take the first IP (original client)
            return forwardedFor.Split(',')[0].Trim();
        }

        // Fall back to connection remote IP
        return request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

public class RateLimitConfiguration
{
    public int ReadRequestsPerMinute { get; set; } = 100;
    public int WriteRequestsPerMinute { get; set; } = 30;
    public bool Enabled { get; set; } = true;
}
```

#### Step 4: Register Services

Update `src/api/Program.cs`:

```csharp
// Rate limiting
var redisConnectionString = configuration["Redis:ConnectionString"];
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(
        ConnectionMultiplexer.Connect(redisConnectionString));
    builder.Services.AddSingleton<IRateLimiterService, RedisRateLimiterService>();
}
else
{
    // Use in-memory for development
    builder.Services.AddSingleton<IRateLimiterService, InMemoryRateLimiterService>();
}

// Add middleware
builder.UseMiddleware<RateLimitingMiddleware>();
```

#### Step 5: Add Configuration

Update `local.settings.json`:

```json
{
  "Values": {
    "RateLimiting:Enabled": "true",
    "RateLimiting:ReadRequestsPerMinute": "100",
    "RateLimiting:WriteRequestsPerMinute": "30",
    "Redis:ConnectionString": ""
  }
}
```

---

## Frontend Handling

### Handle 429 Responses

Update `src/frontend/src/core/services/api.ts`:

```typescript
import axios, { AxiosError } from "axios";
import { toast } from "react-toastify";

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
});

// Rate limit response interceptor
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    if (error.response?.status === 429) {
      const retryAfter = error.response.headers["retry-after"];
      const seconds = parseInt(retryAfter || "60", 10);

      toast.warning(`Too many requests. Please wait ${seconds} seconds.`, {
        autoClose: seconds * 1000,
      });

      // Optionally retry after delay
      if (error.config && seconds <= 10) {
        await new Promise((resolve) => setTimeout(resolve, seconds * 1000));
        return apiClient.request(error.config);
      }
    }

    return Promise.reject(error);
  }
);

export { apiClient };
```

### Display Rate Limit Info

```typescript
// Extract rate limit headers from response
interface RateLimitInfo {
  limit: number;
  remaining: number;
  resetTime: Date;
}

export function getRateLimitInfo(response: AxiosResponse): RateLimitInfo | null {
  const limit = response.headers["x-ratelimit-limit"];
  const remaining = response.headers["x-ratelimit-remaining"];
  const reset = response.headers["x-ratelimit-reset"];

  if (!limit || !remaining || !reset) return null;

  return {
    limit: parseInt(limit, 10),
    remaining: parseInt(remaining, 10),
    resetTime: new Date(reset),
  };
}
```

### Implement Retry with Backoff

```typescript
async function fetchWithRetry<T>(
  request: () => Promise<T>,
  maxRetries: number = 3
): Promise<T> {
  let lastError: Error | null = null;

  for (let attempt = 0; attempt < maxRetries; attempt++) {
    try {
      return await request();
    } catch (error) {
      lastError = error as Error;

      if (axios.isAxiosError(error) && error.response?.status === 429) {
        const retryAfter = parseInt(
          error.response.headers["retry-after"] || "5",
          10
        );
        const delay = Math.min(retryAfter * 1000, 30000);

        console.log(`Rate limited. Retrying in ${delay}ms...`);
        await new Promise((resolve) => setTimeout(resolve, delay));
      } else {
        throw error;
      }
    }
  }

  throw lastError;
}
```

---

## Monitoring

### Log Rate Limit Events

```csharp
// In RateLimitingMiddleware
if (!result.IsAllowed)
{
    _logger.LogWarning(
        "Rate limit exceeded. Key: {Key}, Limit: {Limit}, Endpoint: {Endpoint}",
        rateLimitKey,
        result.Limit,
        request.Path);
}
```

### Application Insights Custom Metrics

```csharp
private readonly TelemetryClient _telemetry;

// Track rate limit hits
_telemetry.TrackMetric("RateLimitHit", 1, new Dictionary<string, string>
{
    ["Endpoint"] = request.Path,
    ["ClientType"] = !string.IsNullOrEmpty(userId) ? "Authenticated" : "Anonymous"
});

// Track remaining quota
_telemetry.TrackMetric("RateLimitRemaining", result.Remaining, new Dictionary<string, string>
{
    ["Key"] = rateLimitKey
});
```

### Alert on High Rate Limit Hits

Create Azure Monitor alert when rate limits are frequently hit:

```
MetricName: RateLimitHit
Aggregation: Sum
Threshold: > 100 in 5 minutes
Severity: Warning
```

---

## Testing

### Unit Tests

```csharp
public class RateLimiterTests
{
    [Fact]
    public async Task Allows_Requests_Under_Limit()
    {
        var limiter = new InMemoryRateLimiterService();
        var policy = new RateLimitPolicy { RequestsPerWindow = 10, WindowSize = TimeSpan.FromMinutes(1) };

        for (int i = 0; i < 10; i++)
        {
            var result = await limiter.CheckRateLimitAsync("test-key", policy);
            result.IsAllowed.Should().BeTrue();
            result.Remaining.Should().Be(10 - i - 1);
        }
    }

    [Fact]
    public async Task Blocks_Requests_Over_Limit()
    {
        var limiter = new InMemoryRateLimiterService();
        var policy = new RateLimitPolicy { RequestsPerWindow = 5, WindowSize = TimeSpan.FromMinutes(1) };

        // Use up the limit
        for (int i = 0; i < 5; i++)
        {
            await limiter.CheckRateLimitAsync("test-key", policy);
        }

        // Next request should be blocked
        var result = await limiter.CheckRateLimitAsync("test-key", policy);
        result.IsAllowed.Should().BeFalse();
        result.Remaining.Should().Be(0);
        result.RetryAfter.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task Separate_Keys_Have_Separate_Limits()
    {
        var limiter = new InMemoryRateLimiterService();
        var policy = new RateLimitPolicy { RequestsPerWindow = 5, WindowSize = TimeSpan.FromMinutes(1) };

        // Use up limit for key1
        for (int i = 0; i < 5; i++)
        {
            await limiter.CheckRateLimitAsync("key1", policy);
        }

        // key2 should still have full limit
        var result = await limiter.CheckRateLimitAsync("key2", policy);
        result.IsAllowed.Should().BeTrue();
        result.Remaining.Should().Be(4);
    }
}
```

### Integration Tests

```csharp
[Fact]
public async Task Returns_429_When_Rate_Limited()
{
    // Make requests up to the limit
    for (int i = 0; i < 100; i++)
    {
        await _client.GetAsync("/api/notes");
    }

    // Next request should be rate limited
    var response = await _client.GetAsync("/api/notes");

    response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    response.Headers.Should().ContainKey("Retry-After");
    response.Headers.Should().ContainKey("X-RateLimit-Limit");
    response.Headers.Should().ContainKey("X-RateLimit-Remaining");
}

[Fact]
public async Task Includes_Rate_Limit_Headers()
{
    var response = await _client.GetAsync("/api/notes");

    response.Headers.GetValues("X-RateLimit-Limit").First().Should().Be("100");
    response.Headers.Should().ContainKey("X-RateLimit-Remaining");
    response.Headers.Should().ContainKey("X-RateLimit-Reset");
}
```

### Load Testing

Use a tool like k6 to test rate limiting:

```javascript
// k6 load test script
import http from "k6/http";
import { check, sleep } from "k6";

export const options = {
  vus: 10, // 10 virtual users
  duration: "1m",
};

export default function () {
  const res = http.get("http://localhost:7071/api/notes");

  check(res, {
    "status is 200 or 429": (r) => r.status === 200 || r.status === 429,
    "has rate limit headers": (r) => r.headers["X-Ratelimit-Limit"] !== undefined,
  });

  if (res.status === 429) {
    const retryAfter = parseInt(res.headers["Retry-After"] || "1", 10);
    sleep(retryAfter);
  } else {
    sleep(0.1);
  }
}
```

---

## Rate Limit Recommendations

| Endpoint Type | Limit | Rationale |
|---------------|-------|-----------|
| Read (GET) | 100/minute | Higher for browsing |
| Write (POST/PUT) | 30/minute | Lower to prevent spam |
| Delete | 10/minute | Very restrictive |
| Auth/Login | 5/minute | Prevent brute force |
| Search | 20/minute | Can be expensive |
| File Upload | 5/minute | Resource intensive |

---

## Related Documentation

- [Azure API Management Rate Limiting](https://docs.microsoft.com/azure/api-management/api-management-access-restriction-policies)
- [Redis Rate Limiting Patterns](https://redis.io/commands/incr#pattern-rate-limiter)
- [Token Bucket Algorithm](https://en.wikipedia.org/wiki/Token_bucket)
