using Microsoft.EntityFrameworkCore;
using Cadence.Core.Data;

namespace Cadence.Core.Tests.Helpers;

/// <summary>
/// Factory for creating in-memory database contexts for testing.
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Creates a new AppDbContext using an in-memory database.
    /// Each call creates a fresh database instance.
    /// </summary>
    /// <param name="databaseName">Optional name for the database. If not provided, a unique name is generated.</param>
    /// <returns>A new AppDbContext instance.</returns>
    public static AppDbContext Create(string? databaseName = null)
    {
        databaseName ??= Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var context = new AppDbContext(options);

        // Ensure the database is created
        context.Database.EnsureCreated();

        return context;
    }

    /// <summary>
    /// Creates an AppDbContext with pre-seeded test data.
    /// </summary>
    /// <param name="seedAction">Action to seed test data.</param>
    /// <param name="databaseName">Optional name for the database.</param>
    /// <returns>A new AppDbContext instance with seeded data.</returns>
    public static AppDbContext CreateWithData(Action<AppDbContext> seedAction, string? databaseName = null)
    {
        var context = Create(databaseName);
        seedAction(context);
        context.SaveChanges();
        return context;
    }
}
