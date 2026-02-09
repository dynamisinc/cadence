// Application entry point
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Cadence.Core.Data;
using Cadence.Core.Extensions;
using Cadence.Core.Features.Authentication.Models;
using Cadence.Core.Features.Authentication.Services;
using Cadence.Core.Features.Capabilities.Services;
using Cadence.Core.Features.Email.Services;
using Cadence.Core.Features.Notifications;
using Cadence.Core.Hubs;
using Cadence.Core.Logging;
using Cadence.Core.Models.Entities;
using Cadence.WebApi.Authorization;
using Cadence.WebApi.Extensions;
using Cadence.WebApi.Hubs;
using Cadence.WebApi.Middleware;
using Cadence.WebApi.Services;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

// Bootstrap logger for capturing startup errors before config is loaded
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Replace default logging with Serilog, reading config from appsettings
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithThreadId()
        .Enrich.WithProperty("Application", "Cadence.WebApi"));

    // Load optional local config (git-ignored, for secrets like ACS connection strings)
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

    // Add services to the container.
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            // ApprovalRoles must serialize as integer for frontend bitwise operations
            // Add this BEFORE the global string enum converter so it takes precedence
            options.JsonSerializerOptions.Converters.Add(new Cadence.Core.Models.Entities.ApprovalRolesJsonConverter());
            // Serialize other enums as strings, preserving original case (TTX, FSE, Draft, etc.)
            // Frontend expects enum values to match exactly
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();

    // Add Application Insights telemetry
    // Check both custom config and Azure's standard environment variable
    var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    if (string.IsNullOrEmpty(appInsightsConnectionString))
    {
        // Azure App Service sets this env var automatically when App Insights is enabled
        appInsightsConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
    }

    if (!string.IsNullOrEmpty(appInsightsConnectionString))
    {
        builder.Services.AddApplicationInsightsTelemetry(options =>
        {
            options.ConnectionString = appInsightsConnectionString;
        });

        // Configure adaptive sampling to reduce telemetry volume while keeping errors
        builder.Services.Configure<TelemetryConfiguration>(config =>
        {
            var telemetryBuilder = config.DefaultTelemetrySink.TelemetryProcessorChainBuilder;
            // Keep all failed requests (4xx, 5xx)
            telemetryBuilder.UseAdaptiveSampling(excludedTypes: "Exception;Request");
            telemetryBuilder.Build();
        });
    }

    // Add SignalR - conditionally use Azure SignalR Service for production
    var useAzureSignalR = builder.Configuration.GetValue<bool>("Azure:SignalR:Enabled");
    var azureSignalRConnectionString = builder.Configuration.GetConnectionString("AzureSignalR");

    // Configure SignalR JSON options to match MVC controller serialization
    // (use camelCase properties and string enums)
    var signalRBuilder = builder.Services.AddSignalR()
        .AddJsonProtocol(options =>
        {
            options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            // ApprovalRoles must serialize as integer for frontend bitwise operations
            options.PayloadSerializerOptions.Converters.Add(new Cadence.Core.Models.Entities.ApprovalRolesJsonConverter());
            options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

    if (useAzureSignalR && !string.IsNullOrEmpty(azureSignalRConnectionString))
    {
        signalRBuilder.AddAzureSignalR(azureSignalRConnectionString);
    }

    // Add ASP.NET Core Identity
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        var authConfig = builder.Configuration.GetSection("Authentication:Identity");
        options.Password.RequiredLength = authConfig.GetValue<int>("PasswordMinLength", 8);
        options.Password.RequireDigit = authConfig.GetValue<bool>("PasswordRequireDigit", true);
        options.Password.RequireUppercase = authConfig.GetValue<bool>("PasswordRequireUppercase", true);
        options.Password.RequireLowercase = authConfig.GetValue<bool>("PasswordRequireLowercase", true);
        options.Password.RequireNonAlphanumeric = authConfig.GetValue<bool>("PasswordRequireNonAlphanumeric", false);
        options.Lockout.MaxFailedAccessAttempts = authConfig.GetValue<int>("LockoutMaxAttempts", 5);
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(authConfig.GetValue<int>("LockoutMinutes", 15));
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

    // Add JWT Authentication
    var jwtOptions = builder.Configuration.GetSection("Authentication:Jwt").Get<JwtOptions>() ?? new JwtOptions();
    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Authentication:Jwt"));
    builder.Services.Configure<AuthenticationOptions>(builder.Configuration.GetSection("Authentication"));

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ClockSkew = TimeSpan.FromSeconds(5),
            NameClaimType = JwtRegisteredClaimNames.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

    // Register Authentication Services
    builder.Services.AddScoped<ITokenService, JwtTokenService>();
    builder.Services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
    builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

    // Add Authorization (Cadence policies and handlers)
    builder.Services.AddHttpContextAccessor(); // Required for authorization handlers
    builder.Services.AddCadenceAuthorization();

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.SetIsOriginAllowed(_ => true) // Allow any origin for development
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // Add Core Services (Database, Services, Validators)
    if (builder.Environment.IsEnvironment("Testing"))
    {
        // Skip database registration for tests that use InMemory database
    }
    else
    {
        builder.Services.AddDatabase(builder.Configuration);
    }
    builder.Services.AddApplicationServices();

    // Add Email Services (templates registered in AddApplicationServices, delivery provider selected here)
    builder.Services.Configure<EmailServiceOptions>(builder.Configuration.GetSection(EmailServiceOptions.SectionName));

    // Register email delivery provider (config-driven: set Email:Provider in appsettings)
    var emailProvider = builder.Configuration.GetSection(EmailServiceOptions.SectionName)["Provider"] ?? "Logging";
    if (emailProvider.Equals("AzureCommunicationServices", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddScoped<Cadence.Core.Features.Email.Services.IEmailService, AzureCommunicationEmailService>();
        Log.Information("Email provider: AzureCommunicationServices");
    }
    else
    {
        builder.Services.AddScoped<Cadence.Core.Features.Email.Services.IEmailService, LoggingEmailService>();
        Log.Information("Email provider: Logging (emails logged to console, not delivered)");
    }

    // Add SignalR Hub Context
    builder.Services.AddScoped<IExerciseHubContext, ExerciseHubContext>();
    builder.Services.AddScoped<INotificationBroadcaster, NotificationBroadcaster>();

    // Add Organization Context (reads org claims from JWT)
    builder.Services.AddScoped<ICurrentOrganizationContext, CurrentOrganizationContext>();

    // Add Background Services
    builder.Services.AddHostedService<InjectReadinessBackgroundService>();

    // Add Demo Data Seeder for non-production, non-testing environments
    if (!builder.Environment.IsProduction() && !builder.Environment.IsEnvironment("Testing"))
    {
        builder.Services.AddDemoSeeder();
    }

    // Add Rate Limiting for authentication endpoints
    builder.Services.AddRateLimiter(options =>
    {
        // Rate limit for auth endpoints (login, register)
        // 10 requests per minute per IP address
        options.AddPolicy("auth", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    PermitLimit = 10,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }));

        // Stricter rate limit for password reset (3 requests per 15 minutes)
        options.AddPolicy("password-reset", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(15),
                    PermitLimit = 3,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }));

        // Custom rejection response
        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "application/json";

            var retryAfterSeconds = 60; // Default retry after 1 minute
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                retryAfterSeconds = (int)retryAfter.TotalSeconds;
            }

            context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();

            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                error = "rate_limit_exceeded",
                message = "Too many requests. Please try again later.",
                retryAfter = retryAfterSeconds
            }, cancellationToken: token);
        };
    });

    var app = builder.Build();

    // =============================================================================
    // Data Seeding
    // =============================================================================

    // Stage 1: Essential data - ALL environments (applies migrations, creates default org)
    await app.SeedEssentialDataAsync();

    // Stage 2: Demo data - ALL except Production and Testing (demo org, users, exercises, observations)
    if (!app.Environment.IsProduction() && !app.Environment.IsEnvironment("Testing"))
    {
        await app.SeedDemoDataAsync();
    }

    // =============================================================================
    // Configure HTTP Pipeline
    // =============================================================================

    app.MapOpenApi();
    app.MapScalarApiReference();

    app.UseHttpsRedirection();

    app.UseCors();

    app.UseRateLimiter();

    // Add request/response logging for failed requests (captures 4xx/5xx with body details)
    app.UseRequestResponseLogging();

    app.UseAuthentication();
    app.UseAuthorization();

    // Enrich Serilog LogContext with UserId, OrganizationId, ExerciseId from JWT claims
    app.UseMiddleware<SerilogContextMiddleware>();

    // Serilog request logging (structured HTTP request summaries)
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
            var exerciseId = httpContext.Request.RouteValues["exerciseId"]?.ToString();
            if (!string.IsNullOrEmpty(exerciseId))
                diagnosticContext.Set("ExerciseId", exerciseId);
        };
    });

    // Global Exception Handler (Simple version for now)
    app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = 500;

            if (app.Environment.IsDevelopment())
            {
                await context.Response.WriteAsJsonAsync(new { message = ex.Message, stackTrace = ex.StackTrace });
            }
            else
            {
                await context.Response.WriteAsJsonAsync(new { message = "An internal error occurred." });
            }
        }
    });

    app.MapControllers();
    app.MapHub<ExerciseHub>("/hubs/exercise");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
