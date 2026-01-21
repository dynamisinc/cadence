using System.Text.Json.Serialization;
using Cadence.Core.Data;
using Cadence.Core.Extensions;
using Cadence.Core.Hubs;
using Cadence.Core.Logging;
using Cadence.WebApi.Hubs;
using Cadence.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
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

// Add Background Services
builder.Services.AddHostedService<InjectReadinessBackgroundService>();

// Add Logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
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
}

app.UseHttpsRedirection();

app.UseCors();

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
