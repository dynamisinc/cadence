using System.Text;
using Cadence.Core.Data;
using Cadence.Core.Features.Authentication.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Cadence.WebApi.Tests;

/// <summary>
/// Custom WebApplicationFactory for integration testing.
/// Replaces the database with an in-memory database for isolated tests.
/// </summary>
public class CadenceWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"CadenceTestDb-{Guid.NewGuid()}";

    // Test JWT configuration - must be consistent between token generation and validation
    private const string TestSecretKey = "TestSecretKeyThatIsAtLeast32CharactersLongForHmacSha256!";
    private const string TestIssuer = "CadenceTests";
    private const string TestAudience = "CadenceTests";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Add test configuration BEFORE services are configured
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add in-memory configuration for testing
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Identity:Enabled"] = "true",
                ["Authentication:Identity:AllowRegistration"] = "true",
                ["Authentication:Identity:PasswordMinLength"] = "8",
                ["Authentication:Identity:PasswordRequireDigit"] = "true",
                ["Authentication:Identity:PasswordRequireUppercase"] = "true",
                ["Authentication:Identity:PasswordRequireLowercase"] = "true",
                ["Authentication:Identity:PasswordRequireNonAlphanumeric"] = "false",
                ["Authentication:Identity:LockoutMaxAttempts"] = "5",
                ["Authentication:Identity:LockoutMinutes"] = "15",
                ["Authentication:Jwt:SecretKey"] = TestSecretKey,
                ["Authentication:Jwt:Issuer"] = TestIssuer,
                ["Authentication:Jwt:Audience"] = TestAudience,
                ["Authentication:Jwt:AccessTokenMinutes"] = "15",
                ["Authentication:Jwt:RefreshTokenHours"] = "4",
                ["Authentication:Jwt:RememberMeDays"] = "30",
                ["Azure:SignalR:Enabled"] = "false"
            });
        });

        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove all DbContext-related services
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            // Remove any existing DbContextOptions
            var descriptorsToRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                d.ServiceType.Name.Contains("DbContextOptions")).ToList();
            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Add InMemory database for testing
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            // Register JwtOptions directly (the JwtTokenService expects JwtOptions, not IOptions<JwtOptions>)
            var testJwtOptions = new JwtOptions
            {
                SecretKey = TestSecretKey,
                Issuer = TestIssuer,
                Audience = TestAudience,
                AccessTokenMinutes = 15,
                RefreshTokenHours = 4,
                RememberMeDays = 30
            };
            services.AddSingleton(testJwtOptions);

            // Re-configure JWT Bearer validation to use our test settings
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = TestIssuer,
                    ValidAudience = TestAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecretKey)),
                    ClockSkew = TimeSpan.FromSeconds(5)
                };
            });
        });
    }

    /// <summary>
    /// Ensures the database is created after the host is built.
    /// </summary>
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Create a scope to get the database context
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        return host;
    }
}
