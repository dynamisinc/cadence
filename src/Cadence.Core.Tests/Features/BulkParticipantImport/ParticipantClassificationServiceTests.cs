using Cadence.Core.Data;
using Cadence.Core.Features.BulkParticipantImport.Models.DTOs;
using Cadence.Core.Features.BulkParticipantImport.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.BulkParticipantImport;

/// <summary>
/// Tests for ParticipantClassificationService.
/// Verifies classification logic for parsed participant rows (Assign/Update/Invite/Error).
/// </summary>
public class ParticipantClassificationServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ICurrentOrganizationContext> _orgContextMock;
    private readonly Mock<ILogger<ParticipantClassificationService>> _loggerMock;
    private readonly ParticipantClassificationService _sut;

    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _exerciseId = Guid.NewGuid();

    public ParticipantClassificationServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _orgContextMock = new Mock<ICurrentOrganizationContext>();
        _orgContextMock.Setup(x => x.CurrentOrganizationId).Returns(_organizationId);
        _loggerMock = new Mock<ILogger<ParticipantClassificationService>>();

        SeedOrganizationAndExercise();

        _sut = new ParticipantClassificationService(_context, _orgContextMock.Object, _loggerMock.Object);
    }

    private void SeedOrganizationAndExercise()
    {
        _context.Organizations.Add(new Organization
        {
            Id = _organizationId,
            Name = "Test Org",
            Slug = "test-org",
            Status = OrgStatus.Active
        });
        _context.Exercises.Add(new Exercise
        {
            Id = _exerciseId,
            OrganizationId = _organizationId,
            Name = "Test Exercise",
            Status = ExerciseStatus.Draft,
            TimeZoneId = "UTC"
        });
        _context.SaveChanges();
    }

    private ApplicationUser CreateUser(string email, SystemRole systemRole = SystemRole.User)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            DisplayName = email.Split('@')[0],
            SystemRole = systemRole,
            Status = UserStatus.Active
        };
        _context.ApplicationUsers.Add(user);
        _context.SaveChanges();
        return user;
    }

    private void AddOrgMembership(string userId, OrgRole role = OrgRole.OrgUser, MembershipStatus status = MembershipStatus.Active)
    {
        _context.OrganizationMemberships.Add(new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = _organizationId,
            Role = role,
            Status = status,
            JoinedAt = DateTime.UtcNow
        });
        _context.SaveChanges();
    }

    private void AddExerciseParticipant(string userId, ExerciseRole role, bool isDeleted = false)
    {
        var participant = new ExerciseParticipant
        {
            Id = Guid.NewGuid(),
            ExerciseId = _exerciseId,
            UserId = userId,
            Role = role,
            AssignedAt = DateTime.UtcNow,
            IsDeleted = isDeleted
        };

        if (isDeleted)
        {
            participant.DeletedAt = DateTime.UtcNow;
        }

        _context.ExerciseParticipants.Add(participant);
        _context.SaveChanges();
    }

    private void AddPendingInvite(string email)
    {
        _context.OrganizationInvites.Add(new OrganizationInvite
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            Email = email,
            Code = Guid.NewGuid().ToString("N")[..8],
            Role = OrgRole.OrgUser,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByUserId = Guid.NewGuid().ToString()
        });
        _context.SaveChanges();
    }

    private static ParsedParticipantRow CreateParsedRow(
        string email,
        ExerciseRole role = ExerciseRole.Controller,
        int rowNumber = 1,
        IReadOnlyList<string>? validationErrors = null)
    {
        return new ParsedParticipantRow
        {
            RowNumber = rowNumber,
            Email = email,
            ExerciseRole = role.ToString(),
            NormalizedExerciseRole = role,
            ValidationErrors = validationErrors ?? []
        };
    }

    [Fact]
    public async Task ClassifyAsync_ExistingOrgMemberNotInExercise_ReturnsAssign()
    {
        // Arrange
        var user = CreateUser("controller@test.com");
        AddOrgMembership(user.Id);

        var rows = new List<ParsedParticipantRow>
        {
            CreateParsedRow("controller@test.com", ExerciseRole.Controller)
        };

        // Act
        var result = await _sut.ClassifyAsync(_exerciseId, rows);

        // Assert
        result.Should().HaveCount(1);
        var classified = result[0];
        classified.Classification.Should().Be(ParticipantClassification.Assign);
        classified.ClassificationLabel.Should().Be("Assign");
        classified.ExistingUserId.Should().Be(user.Id);
        classified.ExistingDisplayName.Should().Be(user.DisplayName);
        classified.IsRoleChange.Should().BeFalse();
        classified.CurrentExerciseRole.Should().BeNull();
        classified.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ClassifyAsync_ExistingOrgMemberAlreadyInExerciseSameRole_ReturnsUpdateNoChange()
    {
        // Arrange
        var user = CreateUser("evaluator@test.com");
        AddOrgMembership(user.Id);
        AddExerciseParticipant(user.Id, ExerciseRole.Evaluator);

        var rows = new List<ParsedParticipantRow>
        {
            CreateParsedRow("evaluator@test.com", ExerciseRole.Evaluator)
        };

        // Act
        var result = await _sut.ClassifyAsync(_exerciseId, rows);

        // Assert
        result.Should().HaveCount(1);
        var classified = result[0];
        classified.Classification.Should().Be(ParticipantClassification.Update);
        classified.ClassificationLabel.Should().Be("Update - No Change");
        classified.ExistingUserId.Should().Be(user.Id);
        classified.IsRoleChange.Should().BeFalse();
        classified.CurrentExerciseRole.Should().Be(ExerciseRole.Evaluator);
    }

    [Fact]
    public async Task ClassifyAsync_ExistingOrgMemberAlreadyInExerciseDifferentRole_ReturnsUpdateRoleChange()
    {
        // Arrange
        var user = CreateUser("observer@test.com");
        AddOrgMembership(user.Id);
        AddExerciseParticipant(user.Id, ExerciseRole.Observer);

        var rows = new List<ParsedParticipantRow>
        {
            CreateParsedRow("observer@test.com", ExerciseRole.Evaluator)
        };

        // Act
        var result = await _sut.ClassifyAsync(_exerciseId, rows);

        // Assert
        result.Should().HaveCount(1);
        var classified = result[0];
        classified.Classification.Should().Be(ParticipantClassification.Update);
        classified.ClassificationLabel.Should().Be("Update - Role Change");
        classified.ExistingUserId.Should().Be(user.Id);
        classified.IsRoleChange.Should().BeTrue();
        classified.CurrentExerciseRole.Should().Be(ExerciseRole.Observer);
    }

    [Fact]
    public async Task ClassifyAsync_ExistingCadenceUserNotInOrg_ReturnsInvite()
    {
        // Arrange
        var user = CreateUser("external@other.com");
        // User exists in Cadence but is NOT in our organization

        var rows = new List<ParsedParticipantRow>
        {
            CreateParsedRow("external@other.com", ExerciseRole.Controller)
        };

        // Act
        var result = await _sut.ClassifyAsync(_exerciseId, rows);

        // Assert
        result.Should().HaveCount(1);
        var classified = result[0];
        classified.Classification.Should().Be(ParticipantClassification.Invite);
        classified.ClassificationLabel.Should().Be("Invite");
        classified.ExistingUserId.Should().Be(user.Id);
        classified.IsNewAccount.Should().BeFalse();
        classified.HasPendingInvitation.Should().BeFalse();
    }

    [Fact]
    public async Task ClassifyAsync_UnknownEmail_ReturnsInviteNewAccount()
    {
        // Arrange
        var rows = new List<ParsedParticipantRow>
        {
            CreateParsedRow("newuser@test.com", ExerciseRole.Observer)
        };

        // Act
        var result = await _sut.ClassifyAsync(_exerciseId, rows);

        // Assert
        result.Should().HaveCount(1);
        var classified = result[0];
        classified.Classification.Should().Be(ParticipantClassification.Invite);
        classified.ClassificationLabel.Should().Be("Invite (New Account)");
        classified.ExistingUserId.Should().BeNull();
        classified.IsNewAccount.Should().BeTrue();
        classified.HasPendingInvitation.Should().BeFalse();
    }

    [Fact]
    public async Task ClassifyAsync_EmailWithPendingOrgInvite_ReturnsInviteWithPendingFlag()
    {
        // Arrange
        AddPendingInvite("pending@test.com");

        var rows = new List<ParsedParticipantRow>
        {
            CreateParsedRow("pending@test.com", ExerciseRole.Controller)
        };

        // Act
        var result = await _sut.ClassifyAsync(_exerciseId, rows);

        // Assert
        result.Should().HaveCount(1);
        var classified = result[0];
        classified.Classification.Should().Be(ParticipantClassification.Invite);
        classified.HasPendingInvitation.Should().BeTrue();
        classified.Notes.Should().Contain("Existing pending invitation will be updated with exercise assignment");
    }

    [Fact]
    public async Task ClassifyAsync_RowWithValidationErrors_ReturnsError()
    {
        // Arrange
        var rows = new List<ParsedParticipantRow>
        {
            new ParsedParticipantRow
            {
                RowNumber = 1,
                Email = "invalid-email",
                ExerciseRole = "InvalidRole",
                NormalizedExerciseRole = null,
                ValidationErrors = new List<string> { "Invalid email format", "Invalid exercise role" }
            }
        };

        // Act
        var result = await _sut.ClassifyAsync(_exerciseId, rows);

        // Assert
        result.Should().HaveCount(1);
        var classified = result[0];
        classified.Classification.Should().Be(ParticipantClassification.Error);
        classified.ErrorMessage.Should().Contain("Invalid email format");
        classified.ErrorMessage.Should().Contain("Invalid exercise role");
    }

    [Fact]
    public async Task ClassifyAsync_ExerciseDirectorForUserRole_ReturnsError()
    {
        // Arrange
        var user = CreateUser("basicuser@test.com", SystemRole.User);
        AddOrgMembership(user.Id);

        var rows = new List<ParsedParticipantRow>
        {
            CreateParsedRow("basicuser@test.com", ExerciseRole.ExerciseDirector)
        };

        // Act
        var result = await _sut.ClassifyAsync(_exerciseId, rows);

        // Assert
        result.Should().HaveCount(1);
        var classified = result[0];
        classified.Classification.Should().Be(ParticipantClassification.Error);
        classified.ErrorMessage.Should().Contain("Exercise Director role requires Admin or Manager system role");
    }

    [Fact]
    public async Task ClassifyAsync_ExerciseDirectorForAdminRole_ReturnsAssign()
    {
        // Arrange
        var admin = CreateUser("admin@test.com", SystemRole.Admin);
        AddOrgMembership(admin.Id);

        var rows = new List<ParsedParticipantRow>
        {
            CreateParsedRow("admin@test.com", ExerciseRole.ExerciseDirector)
        };

        // Act
        var result = await _sut.ClassifyAsync(_exerciseId, rows);

        // Assert
        result.Should().HaveCount(1);
        var classified = result[0];
        classified.Classification.Should().Be(ParticipantClassification.Assign);
        classified.ExistingUserId.Should().Be(admin.Id);
    }

    [Fact]
    public async Task ClassifyAsync_ExerciseDirectorForManagerRole_ReturnsAssign()
    {
        // Arrange
        var manager = CreateUser("manager@test.com", SystemRole.Manager);
        AddOrgMembership(manager.Id);

        var rows = new List<ParsedParticipantRow>
        {
            CreateParsedRow("manager@test.com", ExerciseRole.ExerciseDirector)
        };

        // Act
        var result = await _sut.ClassifyAsync(_exerciseId, rows);

        // Assert
        result.Should().HaveCount(1);
        var classified = result[0];
        classified.Classification.Should().Be(ParticipantClassification.Assign);
        classified.ExistingUserId.Should().Be(manager.Id);
    }

    [Fact]
    public async Task ClassifyAsync_SoftDeletedParticipant_ReturnsAssign()
    {
        // Arrange
        var user = CreateUser("restored@test.com");
        AddOrgMembership(user.Id);
        AddExerciseParticipant(user.Id, ExerciseRole.Controller, isDeleted: true);

        var rows = new List<ParsedParticipantRow>
        {
            CreateParsedRow("restored@test.com", ExerciseRole.Controller)
        };

        // Act
        var result = await _sut.ClassifyAsync(_exerciseId, rows);

        // Assert
        result.Should().HaveCount(1);
        var classified = result[0];
        classified.Classification.Should().Be(ParticipantClassification.Assign);
        classified.ClassificationLabel.Should().Be("Assign");
        classified.Notes.Should().Contain("Will reactivate previous participation");
    }

    [Fact]
    public async Task ClassifyAsync_BatchProcesses_MultipleRows()
    {
        // Arrange - Create multiple users with different states
        var assignUser = CreateUser("assign@test.com");
        AddOrgMembership(assignUser.Id);

        var updateUser = CreateUser("update@test.com");
        AddOrgMembership(updateUser.Id);
        AddExerciseParticipant(updateUser.Id, ExerciseRole.Controller);

        var existingCadenceUser = CreateUser("invite-existing@test.com");
        // No org membership

        AddPendingInvite("pending@test.com");

        var rows = new List<ParsedParticipantRow>
        {
            CreateParsedRow("assign@test.com", ExerciseRole.Controller, 1),
            CreateParsedRow("update@test.com", ExerciseRole.Evaluator, 2),
            CreateParsedRow("invite-existing@test.com", ExerciseRole.Observer, 3),
            CreateParsedRow("newuser@test.com", ExerciseRole.Controller, 4),
            CreateParsedRow("pending@test.com", ExerciseRole.Evaluator, 5)
        };

        // Act
        var result = await _sut.ClassifyAsync(_exerciseId, rows);

        // Assert
        result.Should().HaveCount(5);
        result[0].Classification.Should().Be(ParticipantClassification.Assign);
        result[1].Classification.Should().Be(ParticipantClassification.Update);
        result[1].IsRoleChange.Should().BeTrue();
        result[2].Classification.Should().Be(ParticipantClassification.Invite);
        result[2].IsNewAccount.Should().BeFalse();
        result[3].Classification.Should().Be(ParticipantClassification.Invite);
        result[3].IsNewAccount.Should().BeTrue();
        result[4].Classification.Should().Be(ParticipantClassification.Invite);
        result[4].HasPendingInvitation.Should().BeTrue();
    }

    [Fact]
    public async Task ClassifyAsync_EmptyRowsList_ReturnsEmptyList()
    {
        // Arrange
        var rows = new List<ParsedParticipantRow>();

        // Act
        var result = await _sut.ClassifyAsync(_exerciseId, rows);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ClassifyAsync_CaseInsensitiveEmailMatching_Works()
    {
        // Arrange
        var user = CreateUser("MixedCase@Test.COM");
        AddOrgMembership(user.Id);

        var rows = new List<ParsedParticipantRow>
        {
            CreateParsedRow("mixedcase@test.com", ExerciseRole.Controller)
        };

        // Act
        var result = await _sut.ClassifyAsync(_exerciseId, rows);

        // Assert
        result.Should().HaveCount(1);
        var classified = result[0];
        classified.Classification.Should().Be(ParticipantClassification.Assign);
        classified.ExistingUserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task ClassifyAsync_InactiveMembership_ReturnsInvite()
    {
        // Arrange
        var user = CreateUser("inactive@test.com");
        AddOrgMembership(user.Id, OrgRole.OrgUser, MembershipStatus.Inactive);

        var rows = new List<ParsedParticipantRow>
        {
            CreateParsedRow("inactive@test.com", ExerciseRole.Controller)
        };

        // Act
        var result = await _sut.ClassifyAsync(_exerciseId, rows);

        // Assert
        result.Should().HaveCount(1);
        var classified = result[0];
        classified.Classification.Should().Be(ParticipantClassification.Invite);
        classified.IsNewAccount.Should().BeFalse();
    }

    [Fact]
    public async Task ClassifyAsync_DuplicateEmails_HandlesGracefully()
    {
        var user = CreateUser("dupe@test.com");
        AddOrgMembership(user.Id);

        var rows = new List<ParsedParticipantRow>
        {
            CreateParsedRow("dupe@test.com", ExerciseRole.Controller, 1),
            CreateParsedRow("dupe@test.com", ExerciseRole.Evaluator, 2)
        };

        var result = await _sut.ClassifyAsync(_exerciseId, rows);

        result.Should().HaveCount(2);
        // Both rows should be classified independently without error
        result[0].ParsedRow.Email.Should().Be("dupe@test.com");
        result[1].ParsedRow.Email.Should().Be("dupe@test.com");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
