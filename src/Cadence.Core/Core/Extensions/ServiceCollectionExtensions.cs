using Cadence.Core.Features.ExerciseClock.Services;
using Cadence.Core.Features.Injects.Services;
using Cadence.Core.Features.Observations.Services;
using FluentValidation;

namespace Cadence.Core.Extensions;

/// <summary>
/// Extension methods for configuring dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all application services to the DI container.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Add Validators from this assembly
        services.AddValidatorsFromAssemblyContaining<AppDbContext>();

        // Add Feature Services
        services.AddScoped<IObservationService, ObservationService>();
        services.AddScoped<IInjectService, InjectService>();
        services.AddScoped<IExerciseClockService, ExerciseClockService>();

        return services;
    }

    /// <summary>
    /// Adds database context with SQL Server configuration.
    /// </summary>
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["SqlConnectionString"]
            ?? throw new InvalidOperationException(
                "Database connection string not found. Set 'ConnectionStrings:DefaultConnection' or 'SqlConnectionString'.");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);

                sqlOptions.CommandTimeout(30);
            });

            // Enable detailed errors in development
#if DEBUG
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
#endif
        });

        return services;
    }
}
