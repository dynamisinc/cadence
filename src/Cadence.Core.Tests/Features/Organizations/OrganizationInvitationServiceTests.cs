using Cadence.Core.Data;
using Cadence.Core.Exceptions;
using Cadence.Core.Features.Authentication.Models;
using Cadence.Core.Features.Email.Models;
using Cadence.Core.Features.Email.Services;
using Cadence.Core.Features.Organizations.Models.DTOs;
using Cadence.Core.Features.Organizations.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Cadence.Core.Tests.Features.Organizations;

/// <summary>
/// Tests for OrganizationInvitationService - invitation lifecycle management.
/// </summary>
public class OrganizationInvitationServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly OrganizationInvitationService _sut;
    private readonly Organization _testOrg;
    private readonly ApplicationUser _adminUser;
    private readonly Mock<IEmailService> _mockEmailService;

    public OrganizationInvitationServiceTests()
    {
        _context = TestDbContextFactory.Create();
        var logger = new Mock<ILogger<OrganizationInvitationService>>();
        _mockEmailService = new Mock<IEmailService>();

        // Setup email service to return successful result by default
        _mockEmailService
            .Setup(x => x.SendTemplatedAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<EmailRecipient>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailSendResult("test-message-id", EmailSendStatus.Sent));

        var authOptions = Options.Create(new AuthenticationOptions
        {
            FrontendBaseUrl = "http://localhost:5173"
        });

        _sut = new OrganizationInvitationService(_context, logger.Object, _mockEmailService.Object, authOptions);

        // Seed test data
        _testOrg = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            Slug = "test-org",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Organizations.Add(_testOrg);

        _adminUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "admin@test.com",
            UserName = "admin@test.com",
            DisplayName = "Test Admin",
            SystemRole = SystemRole.User,
            Status = UserStatus.Active
        };
        _context.ApplicationUsers.Add(_adminUser);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    // =========================================================================
    // CreateInvitationAsync Tests
    // =========================================================================

    [Fact]
    public async Task CreateInvitationAsync_ValidRequest_CreatesInvitation()
    {
        var request = new CreateInvitationRequest("new@example.com");

        var result = await _sut.CreateInvitationAsync(_testOrg.Id, request, _adminUser.Id);

        Assert.Equal("new@example.com", result.Email);
        Assert.Equal("Pending", result.Status);
        Assert.NotEmpty(result.Code);
        Assert.Equal("OrgUser", result.Role);
    }

    [Fact]
    public async Task CreateInvitationAsync_WithRole_AssignsSpecifiedRole()
    {
        var request = new CreateInvitationRequest("manager@example.com", OrgRole.OrgManager);

        var result = await _sut.CreateInvitationAsync(_testOrg.Id, request, _adminUser.Id);

        Assert.Equal("OrgManager", result.Role);
    }

    [Fact]
    public async Task CreateInvitationAsync_SetsExpirationTo7Days()
    {
        var request = new CreateInvitationRequest("user@example.com");
        var before = DateTime.UtcNow;

        var result = await _sut.CreateInvitationAsync(_testOrg.Id, request, _adminUser.Id);

        var expectedMin = before.AddDays(7);
        var expectedMax = DateTime.UtcNow.AddDays(7).AddSeconds(5);
        Assert.InRange(result.ExpiresAt, expectedMin, expectedMax);
    }

    [Fact]
    public async Task CreateInvitationAsync_GeneratesUniqueCode()
    {
        var r1 = new CreateInvitationRequest("a@example.com");
        var r2 = new CreateInvitationRequest("b@example.com");

        var inv1 = await _sut.CreateInvitationAsync(_testOrg.Id, r1, _adminUser.Id);
        var inv2 = await _sut.CreateInvitationAsync(_testOrg.Id, r2, _adminUser.Id);

        Assert.NotEqual(inv1.Code, inv2.Code);
    }

    [Fact]
    public async Task CreateInvitationAsync_InvalidOrg_ThrowsNotFoundException()
    {
        var request = new CreateInvitationRequest("user@example.com");

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.CreateInvitationAsync(Guid.NewGuid(), request, _adminUser.Id));
    }

    [Fact]
    public async Task CreateInvitationAsync_AlreadyMember_ThrowsConflictException()
    {
        // Add existing member
        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "existing@example.com",
            UserName = "existing@example.com",
            DisplayName = "Existing User",
            SystemRole = SystemRole.User,
            Status = UserStatus.Active
        };
        _context.ApplicationUsers.Add(existingUser);

        _context.OrganizationMemberships.Add(new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = existingUser.Id,
            OrganizationId = _testOrg.Id,
            Role = OrgRole.OrgUser,
            Status = MembershipStatus.Active,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var request = new CreateInvitationRequest("existing@example.com");

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.CreateInvitationAsync(_testOrg.Id, request, _adminUser.Id));
    }

    [Fact]
    public async Task CreateInvitationAsync_PendingInviteExists_ThrowsConflictException()
    {
        // Create first invitation
        var request = new CreateInvitationRequest("dupe@example.com");
        await _sut.CreateInvitationAsync(_testOrg.Id, request, _adminUser.Id);

        // Try to create duplicate
        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.CreateInvitationAsync(_testOrg.Id, request, _adminUser.Id));
    }

    [Fact]
    public async Task CreateInvitationAsync_RecordsInviterInfo()
    {
        var request = new CreateInvitationRequest("new@example.com");

        var result = await _sut.CreateInvitationAsync(_testOrg.Id, request, _adminUser.Id);

        Assert.Equal("Test Admin", result.InvitedByName);
        Assert.Equal("admin@test.com", result.InvitedByEmail);
    }

    [Fact]
    public async Task CreateInvitationAsync_SendsInvitationEmail()
    {
        var request = new CreateInvitationRequest("new@example.com");

        await _sut.CreateInvitationAsync(_testOrg.Id, request, _adminUser.Id);

        // Verify email was sent with correct template and recipient
        _mockEmailService.Verify(
            x => x.SendTemplatedAsync(
                "OrganizationInvite",
                It.Is<OrganizationInviteEmailModel>(m =>
                    m.OrganizationName == "Test Organization" &&
                    m.InviterName == "Test Admin" &&
                    m.Role == "OrgUser" &&
                    m.InviteUrl.Contains("/invite/")),
                It.Is<EmailRecipient>(r => r.Email == "new@example.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateInvitationAsync_EmailFailure_DoesNotThrow()
    {
        // Setup email service to fail
        _mockEmailService
            .Setup(x => x.SendTemplatedAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<EmailRecipient>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailSendResult(null, EmailSendStatus.Failed, "SMTP error"));

        var request = new CreateInvitationRequest("fail@example.com");

        // Should not throw even if email fails
        var result = await _sut.CreateInvitationAsync(_testOrg.Id, request, _adminUser.Id);

        Assert.NotNull(result);
        Assert.Equal("fail@example.com", result.Email);
    }

    // =========================================================================
    // ResendInvitationAsync Tests
    // =========================================================================

    [Fact]
    public async Task ResendInvitationAsync_PendingInvite_RefreshesExpiration()
    {
        var request = new CreateInvitationRequest("user@example.com");
        var invite = await _sut.CreateInvitationAsync(_testOrg.Id, request, _adminUser.Id);
        var originalExpiry = invite.ExpiresAt;

        // Small delay to ensure time difference
        await Task.Delay(10);

        var result = await _sut.ResendInvitationAsync(invite.Id, _adminUser.Id);

        Assert.True(result.ExpiresAt >= originalExpiry);
    }

    [Fact]
    public async Task ResendInvitationAsync_GeneratesNewCode()
    {
        var request = new CreateInvitationRequest("user@example.com");
        var invite = await _sut.CreateInvitationAsync(_testOrg.Id, request, _adminUser.Id);
        var originalCode = invite.Code;

        var result = await _sut.ResendInvitationAsync(invite.Id, _adminUser.Id);

        Assert.NotEqual(originalCode, result.Code);
    }

    [Fact]
    public async Task ResendInvitationAsync_AcceptedInvite_ThrowsBusinessRuleException()
    {
        var invite = new OrganizationInvite
        {
            Id = Guid.NewGuid(),
            OrganizationId = _testOrg.Id,
            Email = "accepted@example.com",
            Code = "ACCEPTED",
            Role = OrgRole.OrgUser,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByUserId = _adminUser.Id,
            UsedAt = DateTime.UtcNow,
            UsedById = _adminUser.Id,
            UseCount = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.OrganizationInvites.Add(invite);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.ResendInvitationAsync(invite.Id, _adminUser.Id));
    }

    [Fact]
    public async Task ResendInvitationAsync_NotFound_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.ResendInvitationAsync(Guid.NewGuid(), _adminUser.Id));
    }

    [Fact]
    public async Task ResendInvitationAsync_SendsInvitationEmail()
    {
        var request = new CreateInvitationRequest("user@example.com");
        var invite = await _sut.CreateInvitationAsync(_testOrg.Id, request, _adminUser.Id);

        // Reset mock to clear previous call
        _mockEmailService.Reset();
        _mockEmailService
            .Setup(x => x.SendTemplatedAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<EmailRecipient>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailSendResult("test-message-id", EmailSendStatus.Sent));

        await _sut.ResendInvitationAsync(invite.Id, _adminUser.Id);

        // Verify email was sent with correct template and new code
        _mockEmailService.Verify(
            x => x.SendTemplatedAsync(
                "OrganizationInvite",
                It.Is<OrganizationInviteEmailModel>(m =>
                    m.OrganizationName == "Test Organization" &&
                    m.InviterName == "Test Admin" &&
                    m.InviteUrl.Contains("/invite/")),
                It.Is<EmailRecipient>(r => r.Email == "user@example.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================================
    // CancelInvitationAsync Tests
    // =========================================================================

    [Fact]
    public async Task CancelInvitationAsync_PendingInvite_SoftDeletesInvitation()
    {
        var request = new CreateInvitationRequest("cancel@example.com");
        var invite = await _sut.CreateInvitationAsync(_testOrg.Id, request, _adminUser.Id);

        await _sut.CancelInvitationAsync(invite.Id, _adminUser.Id);

        // Should not appear in active invitations
        var invitations = await _sut.GetInvitationsAsync(_testOrg.Id);
        Assert.DoesNotContain(invitations, i => i.Id == invite.Id);
    }

    [Fact]
    public async Task CancelInvitationAsync_AcceptedInvite_ThrowsBusinessRuleException()
    {
        var invite = new OrganizationInvite
        {
            Id = Guid.NewGuid(),
            OrganizationId = _testOrg.Id,
            Email = "accepted@example.com",
            Code = "ACCEPTED2",
            Role = OrgRole.OrgUser,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByUserId = _adminUser.Id,
            UsedAt = DateTime.UtcNow,
            UsedById = _adminUser.Id,
            UseCount = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.OrganizationInvites.Add(invite);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.CancelInvitationAsync(invite.Id, _adminUser.Id));
    }

    [Fact]
    public async Task CancelInvitationAsync_NotFound_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.CancelInvitationAsync(Guid.NewGuid(), _adminUser.Id));
    }

    // =========================================================================
    // GetInvitationsAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetInvitationsAsync_ReturnsAllInvitations()
    {
        await _sut.CreateInvitationAsync(_testOrg.Id, new("a@example.com"), _adminUser.Id);
        await _sut.CreateInvitationAsync(_testOrg.Id, new("b@example.com"), _adminUser.Id);

        var invitations = await _sut.GetInvitationsAsync(_testOrg.Id);

        Assert.Equal(2, invitations.Count());
    }

    [Fact]
    public async Task GetInvitationsAsync_FilterPending_ReturnsOnlyPending()
    {
        await _sut.CreateInvitationAsync(_testOrg.Id, new("pending@example.com"), _adminUser.Id);

        // Create an expired one manually
        _context.OrganizationInvites.Add(new OrganizationInvite
        {
            Id = Guid.NewGuid(),
            OrganizationId = _testOrg.Id,
            Email = "expired@example.com",
            Code = "EXPIRED1",
            Role = OrgRole.OrgUser,
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedByUserId = _adminUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var invitations = await _sut.GetInvitationsAsync(_testOrg.Id, "pending");

        Assert.Single(invitations);
        Assert.Equal("pending@example.com", invitations.First().Email);
    }

    [Fact]
    public async Task GetInvitationsAsync_FilterExpired_ReturnsOnlyExpired()
    {
        await _sut.CreateInvitationAsync(_testOrg.Id, new("pending@example.com"), _adminUser.Id);

        _context.OrganizationInvites.Add(new OrganizationInvite
        {
            Id = Guid.NewGuid(),
            OrganizationId = _testOrg.Id,
            Email = "expired@example.com",
            Code = "EXPIRED2",
            Role = OrgRole.OrgUser,
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedByUserId = _adminUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var invitations = await _sut.GetInvitationsAsync(_testOrg.Id, "expired");

        Assert.Single(invitations);
        Assert.Equal("expired@example.com", invitations.First().Email);
    }

    [Fact]
    public async Task GetInvitationsAsync_OrderedByDateDescending()
    {
        await _sut.CreateInvitationAsync(_testOrg.Id, new("first@example.com"), _adminUser.Id);
        await Task.Delay(50);
        await _sut.CreateInvitationAsync(_testOrg.Id, new("second@example.com"), _adminUser.Id);

        var invitations = (await _sut.GetInvitationsAsync(_testOrg.Id)).ToList();

        Assert.Equal("second@example.com", invitations[0].Email);
        Assert.Equal("first@example.com", invitations[1].Email);
    }

    // =========================================================================
    // ValidateCodeAsync Tests
    // =========================================================================

    [Fact]
    public async Task ValidateCodeAsync_ValidCode_ReturnsInvitation()
    {
        var invite = await _sut.CreateInvitationAsync(
            _testOrg.Id, new("valid@example.com"), _adminUser.Id);

        var result = await _sut.ValidateCodeAsync(invite.Code);

        Assert.NotNull(result);
        Assert.Equal("valid@example.com", result!.Email);
    }

    [Fact]
    public async Task ValidateCodeAsync_ExpiredCode_ReturnsNull()
    {
        _context.OrganizationInvites.Add(new OrganizationInvite
        {
            Id = Guid.NewGuid(),
            OrganizationId = _testOrg.Id,
            Email = "expired@example.com",
            Code = "EXPCODE1",
            Role = OrgRole.OrgUser,
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedByUserId = _adminUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _sut.ValidateCodeAsync("EXPCODE1");

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateCodeAsync_UsedCode_ReturnsNull()
    {
        _context.OrganizationInvites.Add(new OrganizationInvite
        {
            Id = Guid.NewGuid(),
            OrganizationId = _testOrg.Id,
            Email = "used@example.com",
            Code = "USEDCOD1",
            Role = OrgRole.OrgUser,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByUserId = _adminUser.Id,
            UsedAt = DateTime.UtcNow,
            UsedById = _adminUser.Id,
            UseCount = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _sut.ValidateCodeAsync("USEDCOD1");

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateCodeAsync_NonexistentCode_ReturnsNull()
    {
        var result = await _sut.ValidateCodeAsync("NONEXIST");

        Assert.Null(result);
    }

    // =========================================================================
    // AcceptInvitationAsync Tests
    // =========================================================================

    [Fact]
    public async Task AcceptInvitationAsync_ValidCode_CreatesMembership()
    {
        var invite = await _sut.CreateInvitationAsync(
            _testOrg.Id, new("joiner@example.com", OrgRole.OrgManager), _adminUser.Id);

        var newUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "joiner@example.com",
            UserName = "joiner@example.com",
            DisplayName = "New Joiner",
            SystemRole = SystemRole.User,
            Status = UserStatus.Active
        };
        _context.ApplicationUsers.Add(newUser);
        await _context.SaveChangesAsync();

        await _sut.AcceptInvitationAsync(invite.Code, newUser.Id);

        var membership = _context.OrganizationMemberships
            .FirstOrDefault(m => m.UserId == newUser.Id && m.OrganizationId == _testOrg.Id);

        Assert.NotNull(membership);
        Assert.Equal(OrgRole.OrgManager, membership!.Role);
        Assert.Equal(MembershipStatus.Active, membership.Status);
    }

    [Fact]
    public async Task AcceptInvitationAsync_ValidCode_MarksInviteAsUsed()
    {
        var invite = await _sut.CreateInvitationAsync(
            _testOrg.Id, new("joiner2@example.com"), _adminUser.Id);

        var newUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "joiner2@example.com",
            UserName = "joiner2@example.com",
            DisplayName = "New Joiner 2",
            SystemRole = SystemRole.User,
            Status = UserStatus.Active
        };
        _context.ApplicationUsers.Add(newUser);
        await _context.SaveChangesAsync();

        await _sut.AcceptInvitationAsync(invite.Code, newUser.Id);

        var dbInvite = _context.OrganizationInvites.First(i => i.Id == invite.Id);
        Assert.NotNull(dbInvite.UsedAt);
        Assert.Equal(newUser.Id, dbInvite.UsedById);
        Assert.Equal(1, dbInvite.UseCount);
    }

    [Fact]
    public async Task AcceptInvitationAsync_AlreadyMember_ThrowsConflictException()
    {
        var invite = await _sut.CreateInvitationAsync(
            _testOrg.Id, new("member@example.com"), _adminUser.Id);

        // Admin is already a member in some orgs; create explicit membership
        _context.OrganizationMemberships.Add(new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = _adminUser.Id,
            OrganizationId = _testOrg.Id,
            Role = OrgRole.OrgAdmin,
            Status = MembershipStatus.Active,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.AcceptInvitationAsync(invite.Code, _adminUser.Id));
    }

    [Fact]
    public async Task AcceptInvitationAsync_ExpiredCode_ThrowsNotFoundException()
    {
        _context.OrganizationInvites.Add(new OrganizationInvite
        {
            Id = Guid.NewGuid(),
            OrganizationId = _testOrg.Id,
            Email = "expired@example.com",
            Code = "EXPACPT1",
            Role = OrgRole.OrgUser,
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedByUserId = _adminUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.AcceptInvitationAsync("EXPACPT1", _adminUser.Id));
    }
}
