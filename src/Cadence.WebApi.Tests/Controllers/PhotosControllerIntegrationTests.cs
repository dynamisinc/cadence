using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for PhotosController API endpoints.
/// Tests photo upload, listing, update, delete, and exercise-scoped access.
/// </summary>
[Collection("WebApi Integration")]
public class PhotosControllerIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static async Task<(CadenceWebApplicationFactory Factory, HttpClient Client, string AdminEmail)>
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

        return (factory, client, email);
    }

    private static async Task<JsonElement> CreateExerciseAsync(HttpClient client, string? name = null)
    {
        var exerciseName = name ?? $"Test Exercise {Guid.NewGuid():N}"[..40];
        var response = await client.PostAsJsonAsync("/api/exercises", new
        {
            Name = exerciseName,
            ExerciseType = 0,
            ScheduledDate = "2026-06-15",
            Description = "Integration test exercise",
            TimeZoneId = "UTC"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
    }

    /// <summary>
    /// Creates a minimal 1x1 PNG byte array for upload tests.
    /// </summary>
    private static byte[] CreateMinimalPng()
    {
        // Minimal valid 1x1 transparent PNG (67 bytes)
        return Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAC0lEQVQI12NgAAIABQAB" +
            "Nl7BcQAAAABJRU5ErkJggg==");
    }

    private static MultipartFormDataContent CreatePhotoUploadContent(byte[]? photoBytes = null, string contentType = "image/png")
    {
        var content = new MultipartFormDataContent();
        var imageBytes = photoBytes ?? CreateMinimalPng();
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(imageContent, "photo", "test-photo.png");
        content.Add(new StringContent(DateTime.UtcNow.ToString("o")), "CapturedAt");
        return content;
    }

    // =========================================================================
    // Auth Tests
    // =========================================================================

    [Fact]
    public async Task GET_Photos_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/photos");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_UploadPhoto_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var content = CreatePhotoUploadContent();
        var response = await client.PostAsync($"/api/exercises/{Guid.NewGuid()}/photos", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DELETE_Photo_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/exercises/{Guid.NewGuid()}/photos/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Upload Photo - Validation
    // =========================================================================

    [Fact]
    public async Task POST_UploadPhoto_NoFile_Returns400()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        // Send multipart without a photo file
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(DateTime.UtcNow.ToString("o")), "CapturedAt");

        var response = await client.PostAsync($"/api/exercises/{exerciseId}/photos", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_UploadPhoto_NonImageFile_Returns400()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x50, 0x4B, 0x03, 0x04 }); // ZIP header
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "photo", "test.zip");
        content.Add(new StringContent(DateTime.UtcNow.ToString("o")), "CapturedAt");

        var response = await client.PostAsync($"/api/exercises/{exerciseId}/photos", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Get Photos by Exercise
    // =========================================================================

    [Fact]
    public async Task GET_Photos_NewExercise_ReturnsEmptyList()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/photos");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("photos").GetArrayLength().Should().Be(0);
        result.GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    // =========================================================================
    // Get Single Photo
    // =========================================================================

    [Fact]
    public async Task GET_PhotoById_NonExistent_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/photos/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Delete Photo
    // =========================================================================

    [Fact]
    public async Task DELETE_Photo_NonExistent_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.DeleteAsync($"/api/exercises/{exerciseId}/photos/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Get Deleted Photos
    // =========================================================================

    [Fact]
    public async Task GET_DeletedPhotos_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/photos/deleted");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_DeletedPhotos_NewExercise_ReturnsEmptyList()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/photos/deleted");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetArrayLength().Should().Be(0);
    }

    // =========================================================================
    // Quick Photo
    // =========================================================================

    [Fact]
    public async Task POST_QuickPhoto_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var content = CreatePhotoUploadContent();
        var response = await client.PostAsync($"/api/exercises/{Guid.NewGuid()}/photos/quick", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_QuickPhoto_NoFile_Returns400()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var content = new MultipartFormDataContent();
        content.Add(new StringContent(DateTime.UtcNow.ToString("o")), "CapturedAt");

        var response = await client.PostAsync($"/api/exercises/{exerciseId}/photos/quick", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Permanent Delete
    // =========================================================================

    [Fact]
    public async Task DELETE_PermanentDelete_NonExistent_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.DeleteAsync($"/api/exercises/{exerciseId}/photos/{Guid.NewGuid()}/permanent");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Restore Photo
    // =========================================================================

    [Fact]
    public async Task POST_RestorePhoto_NonExistent_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.PostAsync($"/api/exercises/{exerciseId}/photos/{Guid.NewGuid()}/restore", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
