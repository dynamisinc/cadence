using System.Net;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for HealthController API endpoints.
/// Health endpoints require no authentication.
/// </summary>
[Collection("WebApi Integration")]
public class HealthControllerIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task GetHealth_NoAuth_Returns200WithStatus()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var health = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

        health.GetProperty("status").GetString().Should().Be("Healthy");
        health.TryGetProperty("timestamp", out var ts).Should().BeTrue();
        health.GetProperty("database").GetString().Should().Be("Connected");
    }

    [Fact]
    public async Task GetLiveness_NoAuth_Returns200WithAlive()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var liveness = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

        liveness.GetProperty("status").GetString().Should().Be("Alive");
        liveness.TryGetProperty("timestamp", out var ts).Should().BeTrue();
    }
}
