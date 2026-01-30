using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Cadence.Core.Data;
using Cadence.Core.Extensions;
using Cadence.Core.Features.Capabilities.Services;
using Cadence.Core.Features.Authentication.Models;
using Cadence.Core.Features.Authentication.Services;
using Cadence.Core.Features.Notifications;
using Cadence.Core.Hubs;
using Cadence.Core.Logging;
using Cadence.Core.Models.Entities;
using Cadence.WebApi.Authorization;
using Cadence.WebApi.Hubs;
using Cadence.WebApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // Serialize enums as strings, preserving original case (TTX, FSE, Draft, etc.)
        // Frontend expects enum values to match exactly
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Add SignalR - conditionally use Azure SignalR Service for production
var useAzureSignalR = builder.Configuration.GetValue<bool>("Azure:SignalR:Enabled");
var azureSignalRConnectionString = builder.Configuration.GetConnectionString("AzureSignalR");

// Configure SignalR JSON options to match MVC controller serialization
// (use camelCase properties and string enums)
var signalRBuilder = builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
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

// Add SignalR Hub Context
builder.Services.AddScoped<IExerciseHubContext, ExerciseHubContext>();
builder.Services.AddScoped<INotificationBroadcaster, NotificationBroadcaster>();

// Add Organization Context (reads org claims from JWT)
builder.Services.AddScoped<ICurrentOrganizationContext, CurrentOrganizationContext>();

// Add Background Services
builder.Services.AddHostedService<InjectReadinessBackgroundService>();

// Add Logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    // Seed development data
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DevelopmentDataSeeder.SeedAsync(context);

    // Seed FEMA capabilities for demo organization
    var importService = scope.ServiceProvider.GetRequiredService<ICapabilityImportService>();
    var seedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DevelopmentDataSeeder.SeedCapabilitiesAsync(context, importService, seedLogger);
}

app.UseHttpsRedirection();

app.UseCors();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

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
