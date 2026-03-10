using System.Security.Claims;
using Cadence.WebApi.Extensions;
using FluentAssertions;

namespace Cadence.WebApi.Tests.Extensions;

/// <summary>
/// Unit tests for <see cref="ClaimsPrincipalExtensions"/>.
/// Verifies consistent claim extraction behaviour for user ID and organization ID.
/// </summary>
public class ClaimsPrincipalExtensionsTests
{
    // =========================================================================
    // GetUserId Tests
    // =========================================================================

    #region GetUserId

    [Fact]
    public void GetUserId_ValidNameIdentifierClaim_ReturnsUserId()
    {
        // Arrange
        var expectedUserId = Guid.NewGuid().ToString();
        var principal = BuildPrincipal(new Claim(ClaimTypes.NameIdentifier, expectedUserId));

        // Act
        var result = principal.GetUserId();

        // Assert
        result.Should().Be(expectedUserId);
    }

    [Fact]
    public void GetUserId_MissingClaim_ThrowsUnauthorizedAccessException()
    {
        // Arrange — principal has no NameIdentifier claim
        var principal = BuildPrincipal(new Claim("email", "user@example.com"));

        // Act & Assert
        var act = () => principal.GetUserId();
        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void GetUserId_EmptyClaim_ThrowsUnauthorizedAccessException()
    {
        // Arrange — NameIdentifier claim exists but its value is an empty string
        var principal = BuildPrincipal(new Claim(ClaimTypes.NameIdentifier, string.Empty));

        // Act & Assert
        var act = () => principal.GetUserId();
        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void GetUserId_NullIdentity_ThrowsUnauthorizedAccessException()
    {
        // Arrange — ClaimsPrincipal with no identity at all
        var principal = new ClaimsPrincipal();

        // Act & Assert
        var act = () => principal.GetUserId();
        act.Should().Throw<UnauthorizedAccessException>();
    }

    #endregion

    // =========================================================================
    // TryGetUserId Tests
    // =========================================================================

    #region TryGetUserId

    [Fact]
    public void TryGetUserId_ValidClaim_ReturnsUserId()
    {
        // Arrange
        var expectedUserId = Guid.NewGuid().ToString();
        var principal = BuildPrincipal(new Claim(ClaimTypes.NameIdentifier, expectedUserId));

        // Act
        var result = principal.TryGetUserId();

        // Assert
        result.Should().Be(expectedUserId);
    }

    [Fact]
    public void TryGetUserId_MissingClaim_ReturnsNull()
    {
        // Arrange — no NameIdentifier claim present
        var principal = BuildPrincipal(new Claim("email", "user@example.com"));

        // Act
        var result = principal.TryGetUserId();

        // Assert
        // Note: When the claim is completely absent, FindFirstValue returns null.
        // When the claim value is an empty string, FindFirstValue returns "".
        // This method delegates directly to FindFirstValue, so the empty-string
        // case returns "" rather than null. Only a truly missing claim yields null.
        result.Should().BeNull();
    }

    #endregion

    // =========================================================================
    // GetOrganizationId Tests
    // =========================================================================

    #region GetOrganizationId

    [Fact]
    public void GetOrganizationId_ValidGuidClaim_ReturnsGuid()
    {
        // Arrange
        var expectedOrgId = Guid.NewGuid();
        var principal = BuildPrincipal(new Claim("org_id", expectedOrgId.ToString()));

        // Act
        var result = principal.GetOrganizationId();

        // Assert
        result.Should().Be(expectedOrgId);
    }

    [Fact]
    public void GetOrganizationId_MissingClaim_ReturnsNull()
    {
        // Arrange — no org_id claim present
        var principal = BuildPrincipal(new Claim("email", "user@example.com"));

        // Act
        var result = principal.GetOrganizationId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetOrganizationId_EmptyClaim_ReturnsNull()
    {
        // Arrange — org_id claim exists but value is empty
        var principal = BuildPrincipal(new Claim("org_id", string.Empty));

        // Act
        var result = principal.GetOrganizationId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetOrganizationId_MalformedGuid_ReturnsNull()
    {
        // Arrange — org_id contains a non-GUID string
        var principal = BuildPrincipal(new Claim("org_id", "not-a-valid-guid"));

        // Act
        var result = principal.GetOrganizationId();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    // =========================================================================
    // IsSystemAdmin Tests
    // =========================================================================

    #region IsSystemAdmin

    [Fact]
    public void IsSystemAdmin_AdminRole_ReturnsTrue()
    {
        var principal = BuildPrincipal(new Claim(ClaimTypes.Role, "Admin"));

        principal.IsSystemAdmin().Should().BeTrue();
    }

    [Fact]
    public void IsSystemAdmin_UserRole_ReturnsFalse()
    {
        var principal = BuildPrincipal(new Claim(ClaimTypes.Role, "User"));

        principal.IsSystemAdmin().Should().BeFalse();
    }

    [Fact]
    public void IsSystemAdmin_NoRoleClaim_ReturnsFalse()
    {
        var principal = BuildPrincipal(new Claim("email", "user@example.com"));

        principal.IsSystemAdmin().Should().BeFalse();
    }

    #endregion

    // =========================================================================
    // IsOrgAdmin Tests
    // =========================================================================

    #region IsOrgAdmin

    [Fact]
    public void IsOrgAdmin_OrgAdminRole_ReturnsTrue()
    {
        var principal = BuildPrincipal(new Claim("org_role", "OrgAdmin"));

        principal.IsOrgAdmin().Should().BeTrue();
    }

    [Fact]
    public void IsOrgAdmin_OrgUserRole_ReturnsFalse()
    {
        var principal = BuildPrincipal(new Claim("org_role", "OrgUser"));

        principal.IsOrgAdmin().Should().BeFalse();
    }

    [Fact]
    public void IsOrgAdmin_NoOrgRoleClaim_ReturnsFalse()
    {
        var principal = BuildPrincipal(new Claim("email", "user@example.com"));

        principal.IsOrgAdmin().Should().BeFalse();
    }

    [Fact]
    public void IsOrgAdmin_IsInRoleDoesNotWork_ExtensionDoes()
    {
        // Verify that IsInRole("OrgAdmin") returns false (the bug this fixes)
        // while IsOrgAdmin() returns true
        var principal = BuildPrincipal(new Claim("org_role", "OrgAdmin"));

        principal.IsInRole("OrgAdmin").Should().BeFalse("org_role is not in ClaimTypes.Role");
        principal.IsOrgAdmin().Should().BeTrue("extension checks org_role claim directly");
    }

    #endregion

    // =========================================================================
    // IsAdminOrOrgAdmin Tests
    // =========================================================================

    #region IsAdminOrOrgAdmin

    [Fact]
    public void IsAdminOrOrgAdmin_SystemAdmin_ReturnsTrue()
    {
        var principal = BuildPrincipal(new Claim(ClaimTypes.Role, "Admin"));

        principal.IsAdminOrOrgAdmin().Should().BeTrue();
    }

    [Fact]
    public void IsAdminOrOrgAdmin_OrgAdmin_ReturnsTrue()
    {
        var principal = BuildPrincipal(new Claim("org_role", "OrgAdmin"));

        principal.IsAdminOrOrgAdmin().Should().BeTrue();
    }

    [Fact]
    public void IsAdminOrOrgAdmin_RegularUser_ReturnsFalse()
    {
        var principal = BuildPrincipal(
            new Claim(ClaimTypes.Role, "User"),
            new Claim("org_role", "OrgUser"));

        principal.IsAdminOrOrgAdmin().Should().BeFalse();
    }

    #endregion

    // =========================================================================
    // Helpers
    // =========================================================================

    private static ClaimsPrincipal BuildPrincipal(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        return new ClaimsPrincipal(identity);
    }
}
