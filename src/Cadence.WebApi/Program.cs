using Cadence.Core.Extensions;
using Cadence.Core.Logging;
using Cadence.WebApi.Hubs;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

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
app.MapHub<NotificationHub>("/api");

app.Run();
