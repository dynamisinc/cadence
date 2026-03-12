using System.Net;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for VersionController API endpoints.
/// Version endpoint is AllowAnonymous - no authentication required.
/// </summary>
[Collection("WebApi Integration")]
public class VersionControllerIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task GetVersion_NoAuth_Returns200WithVersionInfo()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/version");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var version = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

        version.TryGetProperty("version", out var versionProp).Should().BeTrue();
        versionProp.GetString().Should().NotBeNullOrWhiteSpace();

        version.TryGetProperty("environment", out var envProp).Should().BeTrue();
        envProp.GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetVersion_Returns_ValidVersionFormat()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/version");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var version = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

        // Version should not contain '+' (commit hash is split out)
        var versionString = version.GetProperty("version").GetString()!;
        versionString.Should().NotContain("+");

        // BuildDate can be null but the property should exist
        version.TryGetProperty("buildDate", out var bd).Should().BeTrue();

        // CommitSha can be null but the property should exist
        version.TryGetProperty("commitSha", out var cs).Should().BeTrue();
    }
}
