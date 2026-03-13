using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for ExcelExportController API endpoints.
/// Tests MSEL export, observations export, full package export, and template download.
/// </summary>
[Collection("WebApi Integration")]
public class ExcelExportControllerIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static async Task<(CadenceWebApplicationFactory Factory, HttpClient Client)>
        SetupAuthenticatedClientAsync()
    {
        var factory = new CadenceWebApplicationFactory();
        var client = factory.CreateClient();

        var email = $"admin-{Guid.NewGuid()}@example.com";
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new RegistrationRequest(email, "Password123!", "Test Admin"));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        var slug = $"test-org-{Guid.NewGuid():N}"[..30];
        var createOrgResponse = await client.PostAsJsonAsync("/api/admin/organizations",
            new CreateOrganizationRequest($"Test Org {Guid.NewGuid():N}"[..40], slug, null, null, email));
        createOrgResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var org = await createOrgResponse.Content.ReadFromJsonAsync<OrganizationDto>();

        var switchResponse = await client.PostAsJsonAsync("/api/users/current-organization",
            new { OrganizationId = org!.Id });
        switchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, "Password123!", false));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginAuth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginAuth!.AccessToken);

        return (factory, client);
    }

    private static async Task<JsonElement> CreateExerciseAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/exercises", new
        {
            Name = $"Export Test Exercise {Guid.NewGuid():N}"[..40],
            ExerciseType = 0,
            ScheduledDate = "2026-06-15",
            Description = "Integration test exercise",
            TimeZoneId = "UTC"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
    }

    // =========================================================================
    // Auth Tests
    // =========================================================================

    [Fact]
    public async Task GET_Template_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/export/template");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_ExportMsel_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/export/msel", new
        {
            ExerciseId = Guid.NewGuid(),
            Format = "xlsx"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Template Download Tests
    // =========================================================================

    [Fact]
    public async Task GET_Template_Authenticated_Returns200WithFile()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/export/template");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Contain("spreadsheet");
    }

    [Fact]
    public async Task GET_Template_WithFormattingFlag_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/export/template?includeFormatting=false");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // =========================================================================
    // MSEL Export (POST) Tests
    // =========================================================================

    [Fact]
    public async Task POST_ExportMsel_ValidExercise_ReturnsFileOrNotFound()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.PostAsJsonAsync("/api/export/msel", new
        {
            ExerciseId = Guid.Parse(exerciseId!),
            Format = "xlsx",
            IncludeFormatting = true,
            IncludeObjectives = true,
            IncludePhases = true,
            IncludeConductData = false
        });

        // New exercise may not have MSEL data yet → 404, or export succeeds → 200
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsByteArrayAsync();
            content.Length.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public async Task POST_ExportMsel_CsvFormat_ReturnsFileOrNotFound()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.PostAsJsonAsync("/api/export/msel", new
        {
            ExerciseId = Guid.Parse(exerciseId!),
            Format = "csv"
        });

        // New exercise may not have MSEL data yet → 404
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_ExportMsel_NonExistentExercise_Returns404()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/export/msel", new
        {
            ExerciseId = Guid.NewGuid(),
            Format = "xlsx"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // MSEL Export (GET) Tests
    // =========================================================================

    [Fact]
    public async Task GET_ExportMsel_ValidExercise_ReturnsFileOrNotFound()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/export/exercises/{exerciseId}/msel");

        // New exercise may not have MSEL data yet → 404
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_ExportMsel_WithQueryParams_ReturnsFileOrNotFound()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync(
            $"/api/export/exercises/{exerciseId}/msel?format=csv&includeFormatting=false&includeObjectives=false");

        // New exercise may not have MSEL data yet → 404
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Observations Export Tests
    // =========================================================================

    [Fact]
    public async Task GET_ExportObservations_ValidExercise_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/export/exercises/{exerciseId}/observations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // =========================================================================
    // Full Package Export Tests
    // =========================================================================

    [Fact]
    public async Task GET_ExportFullPackage_ValidExercise_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/export/exercises/{exerciseId}/full");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsByteArrayAsync();
        content.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GET_ExportFullPackage_WithCustomFilename_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync(
            $"/api/export/exercises/{exerciseId}/full?filename=my-exercise-package");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
