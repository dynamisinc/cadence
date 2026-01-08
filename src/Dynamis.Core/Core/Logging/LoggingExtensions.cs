namespace DynamisReferenceApp.Api.Core.Logging;

/// <summary>
/// Extension methods for structured logging with consistent patterns.
/// </summary>
public static class LoggingExtensions
{
    // =========================================================================
    // Operation Logging
    // =========================================================================

    /// <summary>
    /// Logs the start of an operation with correlation data.
    /// </summary>
    public static void LogOperationStart(
        this ILogger logger,
        string operationName,
        string? entityId = null,
        string? userId = null)
    {
        logger.LogInformation(
            "Starting operation {OperationName} for entity {EntityId} by user {UserId}",
            operationName,
            entityId ?? "N/A",
            userId ?? "Anonymous");
    }

    /// <summary>
    /// Logs the successful completion of an operation.
    /// </summary>
    public static void LogOperationSuccess(
        this ILogger logger,
        string operationName,
        string? entityId = null,
        long? durationMs = null)
    {
        logger.LogInformation(
            "Completed operation {OperationName} for entity {EntityId} in {DurationMs}ms",
            operationName,
            entityId ?? "N/A",
            durationMs ?? 0);
    }

    /// <summary>
    /// Logs a failed operation.
    /// </summary>
    public static void LogOperationError(
        this ILogger logger,
        string operationName,
        Exception ex,
        string? entityId = null)
    {
        logger.LogError(
            ex,
            "Failed operation {OperationName} for entity {EntityId}: {ErrorMessage}",
            operationName,
            entityId ?? "N/A",
            ex.Message);
    }

    // =========================================================================
    // Entity Logging
    // =========================================================================

    /// <summary>
    /// Logs entity creation.
    /// </summary>
    public static void LogEntityCreated<T>(
        this ILogger logger,
        string entityId,
        string? userId = null)
    {
        logger.LogInformation(
            "Created {EntityType} with ID {EntityId} by user {UserId}",
            typeof(T).Name,
            entityId,
            userId ?? "Anonymous");
    }

    /// <summary>
    /// Logs entity update.
    /// </summary>
    public static void LogEntityUpdated<T>(
        this ILogger logger,
        string entityId,
        string? userId = null)
    {
        logger.LogInformation(
            "Updated {EntityType} with ID {EntityId} by user {UserId}",
            typeof(T).Name,
            entityId,
            userId ?? "Anonymous");
    }

    /// <summary>
    /// Logs entity deletion.
    /// </summary>
    public static void LogEntityDeleted<T>(
        this ILogger logger,
        string entityId,
        string? userId = null)
    {
        logger.LogInformation(
            "Deleted {EntityType} with ID {EntityId} by user {UserId}",
            typeof(T).Name,
            entityId,
            userId ?? "Anonymous");
    }

    /// <summary>
    /// Logs entity not found.
    /// </summary>
    public static void LogEntityNotFound<T>(
        this ILogger logger,
        string entityId)
    {
        logger.LogWarning(
            "{EntityType} with ID {EntityId} not found",
            typeof(T).Name,
            entityId);
    }

    // =========================================================================
    // Performance Logging
    // =========================================================================

    /// <summary>
    /// Logs a slow operation warning.
    /// </summary>
    public static void LogSlowOperation(
        this ILogger logger,
        string operationName,
        long durationMs,
        long thresholdMs = 1000)
    {
        if (durationMs > thresholdMs)
        {
            logger.LogWarning(
                "Slow operation detected: {OperationName} took {DurationMs}ms (threshold: {ThresholdMs}ms)",
                operationName,
                durationMs,
                thresholdMs);
        }
    }

    // =========================================================================
    // Database Logging
    // =========================================================================

    /// <summary>
    /// Logs database connection issues.
    /// </summary>
    public static void LogDatabaseConnectionError(
        this ILogger logger,
        Exception ex)
    {
        logger.LogError(
            ex,
            "Database connection failed: {ErrorMessage}",
            ex.Message);
    }

    /// <summary>
    /// Logs successful database connection.
    /// </summary>
    public static void LogDatabaseConnected(
        this ILogger logger,
        string databaseName)
    {
        logger.LogInformation(
            "Successfully connected to database: {DatabaseName}",
            databaseName);
    }
}
