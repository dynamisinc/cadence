# CORS & Security Headers Guide

> **Status:** Reference Guide - Not Yet Implemented in Template
> **Priority:** High for Production

This guide explains how to implement proper CORS configuration and security headers.

---

## Table of Contents

1. [Overview](#overview)
2. [CORS Configuration](#cors-configuration)
3. [Security Headers](#security-headers)
4. [Implementation](#implementation)
5. [Testing](#testing)
6. [Environment-Specific Configuration](#environment-specific-configuration)

---

## Overview

### Current State

The template uses permissive settings for local development:

```json
// local.settings.json
{
  "Host": {
    "CORS": "*",
    "CORSCredentials": false
  }
}
```

**This is NOT suitable for production.** It allows any website to make requests to your API.

### Target State

Production-ready security configuration:
- Restrictive CORS with allowed origins whitelist
- Security headers on all responses
- Content Security Policy
- Protection against common web vulnerabilities

---

## CORS Configuration

### What is CORS?

Cross-Origin Resource Sharing (CORS) controls which websites can make requests to your API. Without proper CORS:
- Any malicious website could call your API on behalf of logged-in users
- Sensitive data could be exfiltrated to attacker-controlled servers

### CORS Headers Explained

| Header | Purpose |
|--------|---------|
| `Access-Control-Allow-Origin` | Which origins can access the API |
| `Access-Control-Allow-Methods` | Which HTTP methods are allowed |
| `Access-Control-Allow-Headers` | Which request headers are allowed |
| `Access-Control-Allow-Credentials` | Whether cookies/auth can be sent |
| `Access-Control-Max-Age` | How long to cache preflight response |

### Configuration Levels

#### Development (Permissive)

```json
{
  "Host": {
    "CORS": "*",
    "CORSCredentials": false
  }
}
```

#### Production (Restrictive)

```json
{
  "Host": {
    "CORS": "https://your-app.azurestaticapps.net,https://your-custom-domain.com",
    "CORSCredentials": true
  }
}
```

---

## Security Headers

### Essential Headers

| Header | Purpose | Example Value |
|--------|---------|---------------|
| `X-Content-Type-Options` | Prevent MIME sniffing | `nosniff` |
| `X-Frame-Options` | Prevent clickjacking | `DENY` |
| `X-XSS-Protection` | XSS filter (legacy) | `1; mode=block` |
| `Referrer-Policy` | Control referrer info | `strict-origin-when-cross-origin` |
| `Content-Security-Policy` | Control resource loading | See below |
| `Strict-Transport-Security` | Force HTTPS | `max-age=31536000; includeSubDomains` |
| `Permissions-Policy` | Control browser features | `camera=(), microphone=()` |

### Content Security Policy (CSP)

CSP controls what resources the browser can load:

```
Content-Security-Policy:
  default-src 'self';
  script-src 'self';
  style-src 'self' 'unsafe-inline';
  img-src 'self' data: https:;
  font-src 'self';
  connect-src 'self' https://your-api.azurewebsites.net;
  frame-ancestors 'none';
```

---

## Implementation

### Step 1: Create Security Headers Middleware

Create `src/api/Core/Middleware/SecurityHeadersMiddleware.cs`:

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Cadence.Api.Core.Middleware;

/// <summary>
/// Adds security headers to all HTTP responses.
/// </summary>
public class SecurityHeadersMiddleware : IFunctionsWorkerMiddleware
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;
    private readonly bool _isProduction;

    public SecurityHeadersMiddleware(
        IConfiguration configuration,
        ILogger<SecurityHeadersMiddleware> logger,
        IHostEnvironment environment)
    {
        _configuration = configuration;
        _logger = logger;
        _isProduction = environment.IsProduction();
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        await next(context);

        var httpContext = context.GetHttpContext();
        if (httpContext == null) return;

        var response = httpContext.Response;

        // Prevent MIME type sniffing
        response.Headers["X-Content-Type-Options"] = "nosniff";

        // Prevent clickjacking
        response.Headers["X-Frame-Options"] = "DENY";

        // XSS protection (legacy, but still useful)
        response.Headers["X-XSS-Protection"] = "1; mode=block";

        // Control referrer information
        response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Disable browser features we don't need
        response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        // Only add HSTS in production (requires HTTPS)
        if (_isProduction)
        {
            response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        // Cache control for API responses
        if (!response.Headers.ContainsKey("Cache-Control"))
        {
            response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        }
    }
}
```

### Step 2: Create CORS Middleware

Create `src/api/Core/Middleware/CorsMiddleware.cs`:

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Cadence.Api.Core.Middleware;

/// <summary>
/// Custom CORS middleware with more control than Azure Functions built-in CORS.
/// </summary>
public class CorsMiddleware : IFunctionsWorkerMiddleware
{
    private readonly HashSet<string> _allowedOrigins;
    private readonly ILogger<CorsMiddleware> _logger;
    private readonly bool _allowCredentials;

    public CorsMiddleware(IConfiguration configuration, ILogger<CorsMiddleware> logger)
    {
        _logger = logger;

        // Parse allowed origins from configuration
        var originsConfig = configuration["Cors:AllowedOrigins"] ?? "";
        _allowedOrigins = originsConfig
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(o => o.Trim().TrimEnd('/').ToLowerInvariant())
            .ToHashSet();

        _allowCredentials = configuration.GetValue<bool>("Cors:AllowCredentials");

        // Always allow localhost in development
        if (string.IsNullOrEmpty(configuration["WEBSITE_SITE_NAME"]))
        {
            _allowedOrigins.Add("http://localhost:5197");
            _allowedOrigins.Add("http://localhost:3000");
            _allowedOrigins.Add("http://127.0.0.1:5197");
        }

        _logger.LogInformation("CORS configured for origins: {Origins}", string.Join(", ", _allowedOrigins));
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
        var response = httpContext.Response;
        var origin = request.Headers["Origin"].FirstOrDefault();

        // Check if origin is allowed
        if (!string.IsNullOrEmpty(origin))
        {
            var normalizedOrigin = origin.TrimEnd('/').ToLowerInvariant();

            if (_allowedOrigins.Contains(normalizedOrigin) || _allowedOrigins.Contains("*"))
            {
                response.Headers["Access-Control-Allow-Origin"] = origin;

                if (_allowCredentials)
                {
                    response.Headers["Access-Control-Allow-Credentials"] = "true";
                }
            }
            else
            {
                _logger.LogWarning("Blocked CORS request from origin: {Origin}", origin);
            }
        }

        // Handle preflight requests
        if (HttpMethods.IsOptions(request.Method))
        {
            response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
            response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization, X-Correlation-Id, X-User-Id";
            response.Headers["Access-Control-Max-Age"] = "86400"; // 24 hours

            response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        await next(context);
    }
}
```

### Step 3: Create CORS Options Function

Handle OPTIONS requests explicitly for preflight:

```csharp
/// <summary>
/// Handles CORS preflight OPTIONS requests.
/// </summary>
public class CorsOptionsFunction
{
    [Function("CorsOptions")]
    public IActionResult HandleOptions(
        [HttpTrigger(AuthorizationLevel.Anonymous, "options", Route = "{*path}")]
        HttpRequest req)
    {
        // Headers are set by CorsMiddleware
        return new NoContentResult();
    }
}
```

### Step 4: Register Middleware

Update `src/api/Program.cs`:

```csharp
// Add middleware in order (CORS should be early)
builder.UseMiddleware<CorsMiddleware>();
builder.UseMiddleware<SecurityHeadersMiddleware>();
builder.UseMiddleware<CorrelationIdMiddleware>();
builder.UseMiddleware<ExceptionHandlingMiddleware>();
```

### Step 5: Add Configuration

Update `local.settings.json`:

```json
{
  "Values": {
    "Cors:AllowedOrigins": "http://localhost:5197,http://localhost:3000",
    "Cors:AllowCredentials": "true"
  }
}
```

For production (Azure App Settings):

```
Cors:AllowedOrigins = https://your-app.azurestaticapps.net,https://your-domain.com
Cors:AllowCredentials = true
```

### Step 6: Frontend CSP (Static Web App)

Create `src/frontend/staticwebapp.config.json`:

```json
{
  "globalHeaders": {
    "X-Content-Type-Options": "nosniff",
    "X-Frame-Options": "DENY",
    "X-XSS-Protection": "1; mode=block",
    "Referrer-Policy": "strict-origin-when-cross-origin",
    "Permissions-Policy": "camera=(), microphone=(), geolocation=()",
    "Content-Security-Policy": "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' data:; connect-src 'self' https://*.azurewebsites.net https://*.signalr.net wss://*.signalr.net; frame-ancestors 'none'; base-uri 'self'; form-action 'self'"
  },
  "routes": [
    {
      "route": "/api/*",
      "allowedRoles": ["anonymous"]
    }
  ],
  "navigationFallback": {
    "rewrite": "/index.html",
    "exclude": ["/api/*", "/_framework/*", "/assets/*"]
  }
}
```

---

## Testing

### Test CORS Configuration

```bash
# Test preflight request
curl -X OPTIONS \
  -H "Origin: https://your-app.azurestaticapps.net" \
  -H "Access-Control-Request-Method: POST" \
  -H "Access-Control-Request-Headers: Content-Type" \
  -i http://localhost:5071/api/notes

# Expected response headers:
# Access-Control-Allow-Origin: https://your-app.azurestaticapps.net
# Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS
# Access-Control-Allow-Headers: Content-Type, Authorization, X-Correlation-Id

# Test blocked origin
curl -X OPTIONS \
  -H "Origin: https://evil-site.com" \
  -i http://localhost:5071/api/notes

# Should NOT include Access-Control-Allow-Origin header
```

### Test Security Headers

```bash
curl -i http://localhost:5071/api/health

# Expected headers:
# X-Content-Type-Options: nosniff
# X-Frame-Options: DENY
# X-XSS-Protection: 1; mode=block
# Referrer-Policy: strict-origin-when-cross-origin
```

### Automated Tests

```csharp
public class SecurityHeadersTests
{
    [Fact]
    public async Task All_Responses_Include_Security_Headers()
    {
        var response = await _client.GetAsync("/api/health");

        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.GetValues("X-Content-Type-Options").First().Should().Be("nosniff");

        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.GetValues("X-Frame-Options").First().Should().Be("DENY");
    }

    [Fact]
    public async Task Cors_Allows_Configured_Origins()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/notes");
        request.Headers.Add("Origin", "https://allowed-origin.com");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await _client.SendAsync(request);

        response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
    }

    [Fact]
    public async Task Cors_Blocks_Unknown_Origins()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/notes");
        request.Headers.Add("Origin", "https://unknown-origin.com");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await _client.SendAsync(request);

        response.Headers.Should().NotContainKey("Access-Control-Allow-Origin");
    }
}
```

### Online Security Scanners

Test your production site with:
- [Security Headers](https://securityheaders.com/)
- [Mozilla Observatory](https://observatory.mozilla.org/)
- [SSL Labs](https://www.ssllabs.com/ssltest/)

---

## Environment-Specific Configuration

### Development

```json
{
  "Values": {
    "Cors:AllowedOrigins": "http://localhost:5197,http://localhost:3000",
    "Cors:AllowCredentials": "true"
  }
}
```

### Staging

```
Cors:AllowedOrigins = https://staging.your-app.com,https://your-app-staging.azurestaticapps.net
Cors:AllowCredentials = true
```

### Production

```
Cors:AllowedOrigins = https://your-app.com,https://www.your-app.com
Cors:AllowCredentials = true
```

---

## Common Issues

### "No 'Access-Control-Allow-Origin' header"

**Cause:** Origin not in allowed list
**Solution:** Add the origin to `Cors:AllowedOrigins`

### "The 'Access-Control-Allow-Origin' header contains multiple values"

**Cause:** Both Azure Functions CORS and custom middleware are setting headers
**Solution:** Disable Azure Functions built-in CORS when using custom middleware:

```json
{
  "Host": {
    "CORS": ""
  }
}
```

### "Credentials flag is true, but Access-Control-Allow-Credentials is not 'true'"

**Cause:** `credentials: 'include'` in fetch but server doesn't allow
**Solution:** Set `Cors:AllowCredentials` to `true`

### CSP Blocking Inline Scripts

**Cause:** CSP `script-src 'self'` blocks inline scripts
**Solution:** Either:
1. Move scripts to external files
2. Use nonces: `script-src 'self' 'nonce-abc123'`
3. Add hash: `script-src 'self' 'sha256-...'`

---

## Security Checklist

### CORS
- [ ] No wildcard (`*`) in production
- [ ] Only trusted origins allowed
- [ ] Credentials enabled only if needed
- [ ] Preflight requests handled

### Headers
- [ ] X-Content-Type-Options: nosniff
- [ ] X-Frame-Options: DENY (or SAMEORIGIN)
- [ ] Referrer-Policy set appropriately
- [ ] HSTS enabled in production
- [ ] CSP configured and tested

### General
- [ ] HTTPS enforced in production
- [ ] Sensitive headers not exposed
- [ ] Error messages don't leak info
- [ ] Security scanner shows A+ grade

---

## Related Documentation

- [MDN: CORS](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS)
- [MDN: Content-Security-Policy](https://developer.mozilla.org/en-US/docs/Web/HTTP/CSP)
- [OWASP Secure Headers](https://owasp.org/www-project-secure-headers/)
- [Azure Functions CORS](https://docs.microsoft.com/azure/azure-functions/functions-how-to-use-azure-function-app-settings#cors)
