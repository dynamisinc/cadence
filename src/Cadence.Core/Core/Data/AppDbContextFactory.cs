using Microsoft.EntityFrameworkCore.Design;

namespace Cadence.Core.Data;

/// <summary>
/// Design-time factory for AppDbContext.
/// Used by EF Core tools for migrations when the app isn't running.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Try to load from local.settings.json for Azure Functions style config
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("local.settings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Azure Functions stores connection strings in Values section
        var connectionString = configuration["Values:ConnectionStrings:DefaultConnection"]
            ?? configuration["ConnectionStrings:DefaultConnection"]
            ?? "Server=localhost;Database=Cadence;Trusted_Connection=True;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
