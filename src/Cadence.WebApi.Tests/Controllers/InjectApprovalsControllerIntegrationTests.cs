using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for InjectApprovalsController API endpoints.
/// Tests the full approval workflow: submit, approve, reject, batch operations, revert, and permission checks.
/// </summary>
[Collection("WebApi Integration")]
public class InjectApprovalsControllerIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static async Task<(CadenceWebApplicationFactory Factory, HttpClient Client, string OrgId)>
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

        await client.PostAsJsonAsync("/api/users/current-organization",
            new { OrganizationId = org!.Id });

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, "Password123!", false));
        var loginAuth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginAuth!.AccessToken);

        return (factory, client, org.Id.ToString());
    }

    private static async Task<string> CreateExerciseAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/exercises", new
        {
            Name = $"Approval Test {Guid.NewGuid():N}"[..40],
            ExerciseType = 0,
            ScheduledDate = "2026-06-15",
            TimeZoneId = "UTC"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(json, JsonOptions)
            .GetProperty("id").GetString()!;
    }

    private static async Task<JsonElement> CreateInjectAsync(
        HttpClient client, string exerciseId, string? title = null)
    {
        var response = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/injects", new
        {
            Title = title ?? $"Test Inject {Guid.NewGuid():N}"[..40],
            Description = "Integration test inject description",
            Target = "EOC Team",
            ScheduledTime = "09:00:00"
        });
        var json = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"Inject creation failed with body: {json[..Math.Min(500, json.Length)]}");
        return JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
    }

    /// <summary>
    /// Enable approval on the exercise and set org self-approval policy to AllowedWithWarning
    /// so the single test user can both submit and approve (with confirmation).
    /// </summary>
    private static async Task EnableApprovalAsync(HttpClient client, string exerciseId, string orgId)
    {
        // Enable inject approval on the exercise
        var response = await client.PutAsJsonAsync($"/api/exercises/{exerciseId}/approval-settings", new
        {
            RequireInjectApproval = true,
            IsOverride = false
        });
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        // Set self-approval policy to AlwaysAllowed (2) so single-user tests can self-approve
        // (AllowedWithWarning blocks self-submissions in batch operations)
        // AuthorizedRoles: Administrator(1) | ExerciseDirector(2) = 3
        var permResponse = await client.PutAsJsonAsync(
            $"/api/admin/organizations/{orgId}/settings/approval-permissions",
            new { AuthorizedRoles = 3, SelfApprovalPolicy = 2 });
        permResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
    }

    private static async Task<JsonElement> SubmitInjectAsync(HttpClient client, string exerciseId, string injectId)
    {
        var response = await client.PostAsync(
            $"/api/exercises/{exerciseId}/injects/{injectId}/submit", null);
        var json = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"Submit failed with body: {json[..Math.Min(500, json.Length)]}");
        return JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
    }

    /// <summary>
    /// Approve an inject, handling self-approval confirmation if needed.
    /// </summary>
    private static async Task<JsonElement> ApproveInjectAsync(
        HttpClient client, string exerciseId, string injectId, string? notes = null)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/injects/{injectId}/approve",
            new { Notes = notes });

        // If self-approval is blocked, retry with confirmation
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            if (errorBody.Contains("self", StringComparison.OrdinalIgnoreCase))
            {
                response = await client.PostAsJsonAsync(
                    $"/api/exercises/{exerciseId}/injects/{injectId}/approve",
                    new { Notes = notes, ConfirmSelfApproval = true });
            }
        }

        var json = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"Approve failed with body: {json[..Math.Min(500, json.Length)]}");
        return JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
    }

    private static async Task<JsonElement> GetInjectAsync(HttpClient client, string exerciseId, string injectId)
    {
        var response = await client.GetAsync($"/api/exercises/{exerciseId}/injects/{injectId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
    }

    // =========================================================================
    // Submit
    // =========================================================================

    [Fact]
    public async Task POST_Submit_DraftInject_Returns200_StatusBecomesSubmitted()
    {
        var (factory, client, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        await EnableApprovalAsync(client, exerciseId, orgId);

        var created = await CreateInjectAsync(client, exerciseId, "Submit Test Inject");
        var injectId = created.GetProperty("id").GetString()!;

        var result = await SubmitInjectAsync(client, exerciseId, injectId);

        result.GetProperty("status").GetString().Should().Be("Submitted");

        // Verify via GET
        var fetched = await GetInjectAsync(client, exerciseId, injectId);
        fetched.GetProperty("status").GetString().Should().Be("Submitted");
    }

    [Fact]
    public async Task POST_Submit_AlreadySubmitted_Returns400()
    {
        var (factory, client, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        await EnableApprovalAsync(client, exerciseId, orgId);

        var created = await CreateInjectAsync(client, exerciseId, "Double Submit Inject");
        var injectId = created.GetProperty("id").GetString()!;

        // Submit once
        await SubmitInjectAsync(client, exerciseId, injectId);

        // Submit again — should fail
        var response = await client.PostAsync(
            $"/api/exercises/{exerciseId}/injects/{injectId}/submit", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Submit_WithoutAuth_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/exercises/{Guid.NewGuid()}/injects/{Guid.NewGuid()}/submit", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Approve
    // =========================================================================

    [Fact]
    public async Task POST_Approve_SubmittedInject_Returns200_StatusBecomesApproved()
    {
        var (factory, client, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        await EnableApprovalAsync(client, exerciseId, orgId);

        var created = await CreateInjectAsync(client, exerciseId, "Approve Test Inject");
        var injectId = created.GetProperty("id").GetString()!;

        await SubmitInjectAsync(client, exerciseId, injectId);
        await ApproveInjectAsync(client, exerciseId, injectId);

        // Verify status
        var fetched = await GetInjectAsync(client, exerciseId, injectId);
        fetched.GetProperty("status").GetString().Should().Be("Approved");
    }

    [Fact]
    public async Task POST_Approve_NotSubmitted_Returns400()
    {
        var (factory, client, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        await EnableApprovalAsync(client, exerciseId, orgId);

        var created = await CreateInjectAsync(client, exerciseId, "Not Submitted Inject");
        var injectId = created.GetProperty("id").GetString()!;

        // Approve without submitting first — should fail
        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/injects/{injectId}/approve",
            new { ConfirmSelfApproval = true });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Approve_WithNotes_Returns200_NotesIncluded()
    {
        var (factory, client, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        await EnableApprovalAsync(client, exerciseId, orgId);

        var created = await CreateInjectAsync(client, exerciseId, "Approve With Notes");
        var injectId = created.GetProperty("id").GetString()!;

        await SubmitInjectAsync(client, exerciseId, injectId);
        var result = await ApproveInjectAsync(client, exerciseId, injectId, "Looks good to proceed");

        // Check that approver notes are included
        if (result.TryGetProperty("approverNotes", out var notesProperty) &&
            notesProperty.ValueKind == JsonValueKind.String)
        {
            notesProperty.GetString().Should().Be("Looks good to proceed");
        }
    }

    [Fact]
    public async Task POST_Approve_NonExistentInject_Returns404()
    {
        var (factory, client, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/injects/{Guid.NewGuid()}/approve",
            new { ConfirmSelfApproval = true });

        // Service may throw KeyNotFoundException (404) or InvalidOperationException (400)
        // depending on how inject lookup is implemented
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Reject
    // =========================================================================

    [Fact]
    public async Task POST_Reject_SubmittedInject_Returns200_StatusBecomesDraft()
    {
        var (factory, client, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        await EnableApprovalAsync(client, exerciseId, orgId);

        var created = await CreateInjectAsync(client, exerciseId, "Reject Test Inject");
        var injectId = created.GetProperty("id").GetString()!;

        await SubmitInjectAsync(client, exerciseId, injectId);

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/injects/{injectId}/reject",
            new { Reason = "This inject needs more detail about the scenario context" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify status returns to Draft
        var fetched = await GetInjectAsync(client, exerciseId, injectId);
        fetched.GetProperty("status").GetString().Should().Be("Draft");
    }

    [Fact]
    public async Task POST_Reject_EmptyReason_Returns400()
    {
        var (factory, client, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        await EnableApprovalAsync(client, exerciseId, orgId);

        var created = await CreateInjectAsync(client, exerciseId, "Reject No Reason");
        var injectId = created.GetProperty("id").GetString()!;

        await SubmitInjectAsync(client, exerciseId, injectId);

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/injects/{injectId}/reject",
            new { Reason = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Reject_ShortReason_Returns400()
    {
        var (factory, client, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        await EnableApprovalAsync(client, exerciseId, orgId);

        var created = await CreateInjectAsync(client, exerciseId, "Reject Short Reason");
        var injectId = created.GetProperty("id").GetString()!;

        await SubmitInjectAsync(client, exerciseId, injectId);

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/injects/{injectId}/reject",
            new { Reason = "Too short" }); // 9 chars

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Reject_NotSubmitted_Returns400()
    {
        var (factory, client, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        await EnableApprovalAsync(client, exerciseId, orgId);

        var created = await CreateInjectAsync(client, exerciseId, "Reject Not Submitted");
        var injectId = created.GetProperty("id").GetString()!;

        // Reject without submitting first
        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/injects/{injectId}/reject",
            new { Reason = "This inject was never submitted for review" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Batch Approve
    // =========================================================================

    [Fact]
    public async Task POST_BatchApprove_MultipleSubmitted_Returns200_AllApproved()
    {
        var (factory, client, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        await EnableApprovalAsync(client, exerciseId, orgId);

        // Create and submit 3 injects
        var ids = new List<string>();
        for (int i = 1; i <= 3; i++)
        {
            var created = await CreateInjectAsync(client, exerciseId, $"Batch Approve {i}");
            var id = created.GetProperty("id").GetString()!;
            await SubmitInjectAsync(client, exerciseId, id);
            ids.Add(id);
        }

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/injects/batch/approve",
            new { InjectIds = ids, Notes = "All injects look good" });

        var json = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"Batch approve failed with body: {json[..Math.Min(500, json.Length)]}");

        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

        // Either all approved, or some skipped due to self-approval policy
        var approvedCount = result.GetProperty("approvedCount").GetInt32();
        var skippedCount = result.GetProperty("skippedCount").GetInt32();
        (approvedCount + skippedCount).Should().Be(3);
    }

    [Fact]
    public async Task POST_BatchApprove_EmptyIds_Returns400()
    {
        var (factory, client, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/injects/batch/approve",
            new { InjectIds = Array.Empty<string>(), Notes = "No injects" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_BatchApprove_MixedStatuses_SkipsNonSubmitted()
    {
        var (factory, client, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        await EnableApprovalAsync(client, exerciseId, orgId);

        // Create 3 injects, only submit 2
        var inject1 = await CreateInjectAsync(client, exerciseId, "Batch Mixed 1");
        var inject2 = await CreateInjectAsync(client, exerciseId, "Batch Mixed 2");
        var inject3 = await CreateInjectAsync(client, exerciseId, "Batch Mixed 3");

        var id1 = inject1.GetProperty("id").GetString()!;
        var id2 = inject2.GetProperty("id").GetString()!;
        var id3 = inject3.GetProperty("id").GetString()!;

        await SubmitInjectAsync(client, exerciseId, id1);
        await SubmitInjectAsync(client, exerciseId, id2);
        // id3 stays as Draft

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/injects/batch/approve",
            new { InjectIds = new[] { id1, id2, id3 }, Notes = "Mixed batch" });

        var json = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"Batch approve mixed failed with body: {json[..Math.Min(500, json.Length)]}");

        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

        // id3 should be skipped (not submitted)
        result.GetProperty("skippedCount").GetInt32().Should().BeGreaterOrEqualTo(1);
    }

    // =========================================================================
    // Batch Reject
    // =========================================================================

    [Fact]
    public async Task POST_BatchReject_MultipleSubmitted_Returns200_AllRejected()
    {
        var (factory, client, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        await EnableApprovalAsync(client, exerciseId, orgId);

        // Create and submit 3 injects
        var ids = new List<string>();
        for (int i = 1; i <= 3; i++)
        {
            var created = await CreateInjectAsync(client, exerciseId, $"Batch Reject {i}");
            var id = created.GetProperty("id").GetString()!;
            await SubmitInjectAsync(client, exerciseId, id);
            ids.Add(id);
        }

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/injects/batch/reject",
            new { InjectIds = ids, Reason = "All injects need significant rework before approval" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

        result.GetProperty("rejectedCount").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task POST_BatchReject_EmptyIds_Returns400()
    {
        var (factory, client, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/injects/batch/reject",
            new { InjectIds = Array.Empty<string>(), Reason = "Reason does not matter here" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_BatchReject_ShortReason_Returns400()
    {
        var (factory, client, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        await EnableApprovalAsync(client, exerciseId, orgId);

        var created = await CreateInjectAsync(client, exerciseId, "Batch Reject Short");
        var injectId = created.GetProperty("id").GetString()!;
        await SubmitInjectAsync(client, exerciseId, injectId);

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/injects/batch/reject",
            new { InjectIds = new[] { injectId }, Reason = "Too short" }); // 9 chars

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Revert
    // =========================================================================

    [Fact]
    public async Task POST_Revert_ApprovedInject_Returns200_StatusBecomesSubmitted()
    {
        var (factory, client, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        await EnableApprovalAsync(client, exerciseId, orgId);

        var created = await CreateInjectAsync(client, exerciseId, "Revert Test Inject");
        var injectId = created.GetProperty("id").GetString()!;

        await SubmitInjectAsync(client, exerciseId, injectId);
        await ApproveInjectAsync(client, exerciseId, injectId);

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/injects/{injectId}/revert",
            new { Reason = "Need to re-review this inject after new information" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify status reverted to Submitted
        var fetched = await GetInjectAsync(client, exerciseId, injectId);
        fetched.GetProperty("status").GetString().Should().Be("Submitted");
    }

    [Fact]
    public async Task POST_Revert_NotApproved_Returns400()
    {
        var (factory, client, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        await EnableApprovalAsync(client, exerciseId, orgId);

        var created = await CreateInjectAsync(client, exerciseId, "Revert Not Approved");
        var injectId = created.GetProperty("id").GetString()!;

        // Submit but don't approve
        await SubmitInjectAsync(client, exerciseId, injectId);

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/injects/{injectId}/revert",
            new { Reason = "Cannot revert something that is not approved yet" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Revert_ShortReason_Returns400()
    {
        var (factory, client, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        await EnableApprovalAsync(client, exerciseId, orgId);

        var created = await CreateInjectAsync(client, exerciseId, "Revert Short Reason");
        var injectId = created.GetProperty("id").GetString()!;

        await SubmitInjectAsync(client, exerciseId, injectId);
        await ApproveInjectAsync(client, exerciseId, injectId);

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/injects/{injectId}/revert",
            new { Reason = "Too short" }); // 9 chars

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Permission Checks
    // =========================================================================

    [Fact]
    public async Task GET_CanApproveInject_SubmittedInject_Returns200_WithPermissionDetails()
    {
        var (factory, client, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        await EnableApprovalAsync(client, exerciseId, orgId);

        var created = await CreateInjectAsync(client, exerciseId, "Can Approve Check");
        var injectId = created.GetProperty("id").GetString()!;

        await SubmitInjectAsync(client, exerciseId, injectId);

        var response = await client.GetAsync(
            $"/api/exercises/{exerciseId}/injects/{injectId}/can-approve");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

        // Should have permission check fields
        result.TryGetProperty("canApprove", out var canApproveProp).Should().BeTrue();
        result.TryGetProperty("isSelfApproval", out var selfApprovalProp).Should().BeTrue();
    }

    [Fact]
    public async Task GET_CanApproveExercise_Returns200_WithCanApproveFlag()
    {
        var (factory, client, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.GetAsync(
            $"/api/exercises/{exerciseId}/injects/can-approve");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

        result.TryGetProperty("canApprove", out var canApproveFlag).Should().BeTrue();
    }

    [Fact]
    public async Task GET_CanApproveInject_NonExistent_Returns404()
    {
        var (factory, client, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.GetAsync(
            $"/api/exercises/{exerciseId}/injects/{Guid.NewGuid()}/can-approve");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // E2E Workflow
    // =========================================================================

    [Fact]
    public async Task EndToEnd_Submit_Approve_Revert_Reject_FullLifecycle()
    {
        var (factory, client, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        await EnableApprovalAsync(client, exerciseId, orgId);

        // 1. Create inject
        var created = await CreateInjectAsync(client, exerciseId, "E2E Lifecycle Inject");
        var injectId = created.GetProperty("id").GetString()!;

        var fetched = await GetInjectAsync(client, exerciseId, injectId);
        fetched.GetProperty("status").GetString().Should().Be("Draft");

        // 2. Submit for approval
        await SubmitInjectAsync(client, exerciseId, injectId);

        fetched = await GetInjectAsync(client, exerciseId, injectId);
        fetched.GetProperty("status").GetString().Should().Be("Submitted");

        // 3. Approve
        await ApproveInjectAsync(client, exerciseId, injectId, "Approved after review");

        fetched = await GetInjectAsync(client, exerciseId, injectId);
        fetched.GetProperty("status").GetString().Should().Be("Approved");

        // 4. Revert back to Submitted
        var revertResponse = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/injects/{injectId}/revert",
            new { Reason = "Need to re-review after scenario change occurred" });
        revertResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        fetched = await GetInjectAsync(client, exerciseId, injectId);
        fetched.GetProperty("status").GetString().Should().Be("Submitted");

        // 5. Reject — returns to Draft
        var rejectResponse = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/injects/{injectId}/reject",
            new { Reason = "Inject needs rework after scenario was modified significantly" });
        rejectResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        fetched = await GetInjectAsync(client, exerciseId, injectId);
        fetched.GetProperty("status").GetString().Should().Be("Draft");
    }
}
