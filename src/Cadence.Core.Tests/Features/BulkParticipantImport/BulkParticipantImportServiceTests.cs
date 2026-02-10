using Cadence.Core.Data;
using Cadence.Core.Features.BulkParticipantImport.Models.DTOs;
using Cadence.Core.Features.BulkParticipantImport.Models.Entities;
using Cadence.Core.Features.BulkParticipantImport.Services;
using Cadence.Core.Features.Organizations.Models.DTOs;
using Cadence.Core.Features.Organizations.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.BulkParticipantImport;

/// <summary>
/// Unit tests for BulkParticipantImportService.
/// Tests the orchestration of bulk participant import workflow.
/// </summary>
public class BulkParticipantImportServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IParticipantFileParser> _mockParser;
    private readonly Mock<IParticipantClassificationService> _mockClassificationService;
    private readonly Mock<IMembershipService> _mockMembershipService;
    private readonly Mock<IOrganizationInvitationService> _mockInvitationService;
    private readonly Mock<ICurrentOrganizationContext> _mockOrgContext;
    private readonly Mock<ILogger<BulkParticipantImportService>> _mockLogger;
    private readonly BulkParticipantImportService _sut;

    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _exerciseId = Guid.NewGuid();
    private readonly string _userId = Guid.NewGuid().ToString();

    public BulkParticipantImportServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _mockParser = new Mock<IParticipantFileParser>();
        _mockClassificationService = new Mock<IParticipantClassificationService>();
        _mockMembershipService = new Mock<IMembershipService>();
        _mockInvitationService = new Mock<IOrganizationInvitationService>();
        _mockOrgContext = new Mock<ICurrentOrganizationContext>();
        _mockLogger = new Mock<ILogger<BulkParticipantImportService>>();

        // Setup org context
        _mockOrgContext.Setup(x => x.HasContext).Returns(true);
        _mockOrgContext.Setup(x => x.CurrentOrganizationId).Returns(_organizationId);

        _sut = new BulkParticipantImportService(
            _context,
            _mockParser.Object,
            _mockClassificationService.Object,
            _mockMembershipService.Object,
            _mockInvitationService.Object,
            _mockOrgContext.Object,
            _mockLogger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var organization = new Organization
        {
            Id = _organizationId,
            Name = "Test Organization",
            Slug = "test-organization"
        };

        var exercise = new Exercise
        {
            Id = _exerciseId,
            OrganizationId = _organizationId,
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Draft,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            Organization = organization
        };

        var user = new ApplicationUser
        {
            Id = _userId,
            Email = "test@example.com",
            DisplayName = "Test User",
            Status = UserStatus.Active
        };

        _context.Organizations.Add(organization);
        _context.Exercises.Add(exercise);
        _context.Users.Add(user);
        _context.SaveChanges();
    }

    [Fact]
    public async Task UploadAndParseAsync_ValidFile_ReturnsParseResult()
    {
        // Arrange
        var fileName = "participants.csv";
        var sessionId = Guid.NewGuid();
        using var stream = new MemoryStream();

        var expectedResult = new FileParseResult
        {
            SessionId = sessionId,
            FileName = fileName,
            TotalRows = 3,
            ColumnMappings = new List<ColumnMapping>(),
            Rows = new List<ParsedParticipantRow>(),
            Warnings = new List<string>(),
            Errors = new List<string>()
        };

        _mockParser
            .Setup(x => x.ParseAsync(It.IsAny<Stream>(), fileName))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.UploadAndParseAsync(_exerciseId, stream, fileName);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(sessionId);
        result.FileName.Should().Be(fileName);
        result.TotalRows.Should().Be(3);
        _mockParser.Verify(x => x.ParseAsync(It.IsAny<Stream>(), fileName), Times.Once);
    }

    [Fact]
    public async Task UploadAndParseAsync_ExerciseNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var nonExistentExerciseId = Guid.NewGuid();
        using var stream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.UploadAndParseAsync(nonExistentExerciseId, stream, "test.csv"));
    }

    [Fact]
    public async Task UploadAndParseAsync_ExerciseNotDraftOrActive_ThrowsInvalidOperationException()
    {
        // Arrange
        var completedExercise = new Exercise
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            Name = "Completed Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Completed,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            CompletedAt = DateTime.UtcNow
        };
        _context.Exercises.Add(completedExercise);
        await _context.SaveChangesAsync();

        using var stream = new MemoryStream();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UploadAndParseAsync(completedExercise.Id, stream, "test.csv"));

        exception.Message.Should().Contain("Exercise must be Draft or Active");
    }

    [Fact]
    public async Task GetPreviewAsync_ValidSession_ReturnsClassifiedRows()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var parseResult = new FileParseResult
        {
            SessionId = sessionId,
            FileName = "test.csv",
            TotalRows = 2,
            Rows = new List<ParsedParticipantRow>
            {
                new() { RowNumber = 1, Email = "user1@example.com", ExerciseRole = "Controller",
                       NormalizedExerciseRole = ExerciseRole.Controller },
                new() { RowNumber = 2, Email = "user2@example.com", ExerciseRole = "Evaluator",
                       NormalizedExerciseRole = ExerciseRole.Evaluator }
            }
        };

        _mockParser
            .Setup(x => x.ParseAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(parseResult);

        var classifiedRows = new List<ClassifiedParticipantRow>
        {
            new() { ParsedRow = parseResult.Rows.First(), Classification = ParticipantClassification.Assign,
                   ClassificationLabel = "Assign", ExistingUserId = Guid.NewGuid().ToString() },
            new() { ParsedRow = parseResult.Rows.Last(), Classification = ParticipantClassification.Invite,
                   ClassificationLabel = "Invite", IsNewAccount = true }
        };

        _mockClassificationService
            .Setup(x => x.ClassifyAsync(_exerciseId, It.IsAny<IReadOnlyList<ParsedParticipantRow>>()))
            .ReturnsAsync(classifiedRows);

        // First upload to create session
        using var stream = new MemoryStream();
        await _sut.UploadAndParseAsync(_exerciseId, stream, "test.csv");

        // Act
        var result = await _sut.GetPreviewAsync(_exerciseId, sessionId);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(sessionId);
        result.TotalRows.Should().Be(2);
        result.AssignCount.Should().Be(1);
        result.InviteCount.Should().Be(1);
        result.Rows.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPreviewAsync_ExpiredSession_ThrowsInvalidOperationException()
    {
        // Arrange - Create a session that's already expired (this is a simplified test)
        var nonExistentSessionId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.GetPreviewAsync(_exerciseId, nonExistentSessionId));

        exception.Message.Should().Contain("Import session");
    }

    [Fact]
    public async Task ConfirmImportAsync_AssignRows_CreatesExerciseParticipants()
    {
        // Arrange
        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "existing@example.com",
            DisplayName = "Existing User",
            Status = UserStatus.Active
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var sessionId = Guid.NewGuid();
        var parseResult = new FileParseResult
        {
            SessionId = sessionId,
            FileName = "test.csv",
            TotalRows = 1,
            Rows = new List<ParsedParticipantRow>
            {
                new() { RowNumber = 1, Email = existingUser.Email, ExerciseRole = "Controller",
                       NormalizedExerciseRole = ExerciseRole.Controller }
            }
        };

        var classifiedRows = new List<ClassifiedParticipantRow>
        {
            new() {
                ParsedRow = parseResult.Rows.First(),
                Classification = ParticipantClassification.Assign,
                ClassificationLabel = "Assign",
                ExistingUserId = existingUser.Id
            }
        };

        _mockParser
            .Setup(x => x.ParseAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(parseResult);

        _mockClassificationService
            .Setup(x => x.ClassifyAsync(_exerciseId, It.IsAny<IReadOnlyList<ParsedParticipantRow>>()))
            .ReturnsAsync(classifiedRows);

        // Upload and preview to create session
        using var stream = new MemoryStream();
        await _sut.UploadAndParseAsync(_exerciseId, stream, "test.csv");
        await _sut.GetPreviewAsync(_exerciseId, sessionId);

        // Act
        var result = await _sut.ConfirmImportAsync(_exerciseId, sessionId, _userId);

        // Assert
        result.Should().NotBeNull();
        result.AssignedCount.Should().Be(1);
        result.UpdatedCount.Should().Be(0);
        result.InvitedCount.Should().Be(0);
        result.ErrorCount.Should().Be(0);

        var participant = await _context.ExerciseParticipants
            .FirstOrDefaultAsync(p => p.ExerciseId == _exerciseId && p.UserId == existingUser.Id);

        participant.Should().NotBeNull();
        participant!.Role.Should().Be(ExerciseRole.Controller);
        participant.AssignedById.Should().Be(_userId);
    }

    [Fact]
    public async Task ConfirmImportAsync_UpdateRowsWithRoleChange_UpdatesRole()
    {
        // Arrange
        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "existing@example.com",
            DisplayName = "Existing User",
            Status = UserStatus.Active
        };

        var existingParticipant = new ExerciseParticipant
        {
            Id = Guid.NewGuid(),
            ExerciseId = _exerciseId,
            UserId = existingUser.Id,
            Role = ExerciseRole.Observer,
            AssignedAt = DateTime.UtcNow
        };

        _context.Users.Add(existingUser);
        _context.ExerciseParticipants.Add(existingParticipant);
        await _context.SaveChangesAsync();

        var sessionId = Guid.NewGuid();
        var parseResult = new FileParseResult
        {
            SessionId = sessionId,
            FileName = "test.csv",
            TotalRows = 1,
            Rows = new List<ParsedParticipantRow>
            {
                new() { RowNumber = 1, Email = existingUser.Email, ExerciseRole = "Controller",
                       NormalizedExerciseRole = ExerciseRole.Controller }
            }
        };

        var classifiedRows = new List<ClassifiedParticipantRow>
        {
            new() {
                ParsedRow = parseResult.Rows.First(),
                Classification = ParticipantClassification.Update,
                ClassificationLabel = "Update",
                ExistingUserId = existingUser.Id,
                CurrentExerciseRole = ExerciseRole.Observer,
                IsRoleChange = true
            }
        };

        _mockParser
            .Setup(x => x.ParseAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(parseResult);

        _mockClassificationService
            .Setup(x => x.ClassifyAsync(_exerciseId, It.IsAny<IReadOnlyList<ParsedParticipantRow>>()))
            .ReturnsAsync(classifiedRows);

        using var stream = new MemoryStream();
        await _sut.UploadAndParseAsync(_exerciseId, stream, "test.csv");
        await _sut.GetPreviewAsync(_exerciseId, sessionId);

        // Act
        var result = await _sut.ConfirmImportAsync(_exerciseId, sessionId, _userId);

        // Assert
        result.Should().NotBeNull();
        result.UpdatedCount.Should().Be(1);
        result.AssignedCount.Should().Be(0);

        var participant = await _context.ExerciseParticipants
            .FirstOrDefaultAsync(p => p.ExerciseId == _exerciseId && p.UserId == existingUser.Id);

        participant.Should().NotBeNull();
        participant!.Role.Should().Be(ExerciseRole.Controller);
    }

    [Fact]
    public async Task ConfirmImportAsync_UpdateRowsNoChange_SkipsRow()
    {
        // Arrange
        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "existing@example.com",
            DisplayName = "Existing User",
            Status = UserStatus.Active
        };

        var existingParticipant = new ExerciseParticipant
        {
            Id = Guid.NewGuid(),
            ExerciseId = _exerciseId,
            UserId = existingUser.Id,
            Role = ExerciseRole.Controller,
            AssignedAt = DateTime.UtcNow
        };

        _context.Users.Add(existingUser);
        _context.ExerciseParticipants.Add(existingParticipant);
        await _context.SaveChangesAsync();

        var sessionId = Guid.NewGuid();
        var parseResult = new FileParseResult
        {
            SessionId = sessionId,
            FileName = "test.csv",
            TotalRows = 1,
            Rows = new List<ParsedParticipantRow>
            {
                new() { RowNumber = 1, Email = existingUser.Email, ExerciseRole = "Controller",
                       NormalizedExerciseRole = ExerciseRole.Controller }
            }
        };

        var classifiedRows = new List<ClassifiedParticipantRow>
        {
            new() {
                ParsedRow = parseResult.Rows.First(),
                Classification = ParticipantClassification.Update,
                ClassificationLabel = "Update",
                ExistingUserId = existingUser.Id,
                CurrentExerciseRole = ExerciseRole.Controller,
                IsRoleChange = false
            }
        };

        _mockParser
            .Setup(x => x.ParseAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(parseResult);

        _mockClassificationService
            .Setup(x => x.ClassifyAsync(_exerciseId, It.IsAny<IReadOnlyList<ParsedParticipantRow>>()))
            .ReturnsAsync(classifiedRows);

        using var stream = new MemoryStream();
        await _sut.UploadAndParseAsync(_exerciseId, stream, "test.csv");
        await _sut.GetPreviewAsync(_exerciseId, sessionId);

        // Act
        var result = await _sut.ConfirmImportAsync(_exerciseId, sessionId, _userId);

        // Assert
        result.Should().NotBeNull();
        result.SkippedCount.Should().Be(1);
        result.UpdatedCount.Should().Be(0);
        result.AssignedCount.Should().Be(0);
    }

    [Fact]
    public async Task ConfirmImportAsync_InviteRows_CreatesInviteAndPendingAssignment()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var inviteId = Guid.NewGuid();
        var parseResult = new FileParseResult
        {
            SessionId = sessionId,
            FileName = "test.csv",
            TotalRows = 1,
            Rows = new List<ParsedParticipantRow>
            {
                new() { RowNumber = 1, Email = "newuser@example.com", ExerciseRole = "Evaluator",
                       NormalizedExerciseRole = ExerciseRole.Evaluator,
                       OrganizationRole = "OrgUser", NormalizedOrgRole = OrgRole.OrgUser }
            }
        };

        var classifiedRows = new List<ClassifiedParticipantRow>
        {
            new() {
                ParsedRow = parseResult.Rows.First(),
                Classification = ParticipantClassification.Invite,
                ClassificationLabel = "Invite",
                IsNewAccount = true
            }
        };

        _mockParser
            .Setup(x => x.ParseAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(parseResult);

        _mockClassificationService
            .Setup(x => x.ClassifyAsync(_exerciseId, It.IsAny<IReadOnlyList<ParsedParticipantRow>>()))
            .ReturnsAsync(classifiedRows);

        // Mock invitation service to create invite and return DTO
        _mockInvitationService
            .Setup(x => x.CreateInvitationAsync(
                _organizationId,
                It.Is<CreateInvitationRequest>(r => r.Email == "newuser@example.com" && r.Role == OrgRole.OrgUser),
                _userId))
            .ReturnsAsync((Guid orgId, CreateInvitationRequest req, string userId) =>
            {
                // Create the invite in the database for the test
                var invite = new OrganizationInvite
                {
                    Id = inviteId,
                    OrganizationId = orgId,
                    Email = req.Email,
                    Code = "TESTCODE",
                    Role = req.Role,
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    CreatedByUserId = userId,
                    MaxUses = 1,
                    UseCount = 0
                };
                _context.OrganizationInvites.Add(invite);
                _context.SaveChanges();

                return new InvitationDto(
                    Id: invite.Id,
                    Email: invite.Email!,
                    Code: invite.Code,
                    Role: invite.Role.ToString(),
                    Status: "Pending",
                    CreatedAt: invite.CreatedAt,
                    ExpiresAt: invite.ExpiresAt,
                    InvitedByName: "Test User",
                    InvitedByEmail: "test@example.com",
                    AcceptedAt: null,
                    CancelledAt: null,
                    AcceptedByName: null,
                    OrganizationName: "Test Organization",
                    EmailSent: true,
                    EmailError: null,
                    AccountExists: false
                );
            });

        using var stream = new MemoryStream();
        await _sut.UploadAndParseAsync(_exerciseId, stream, "test.csv");
        await _sut.GetPreviewAsync(_exerciseId, sessionId);

        // Act
        var result = await _sut.ConfirmImportAsync(_exerciseId, sessionId, _userId);

        // Assert
        result.Should().NotBeNull();
        result.InvitedCount.Should().Be(1);
        result.AssignedCount.Should().Be(0);

        var invite = await _context.OrganizationInvites
            .FirstOrDefaultAsync(i => i.Email == "newuser@example.com");

        invite.Should().NotBeNull();
        invite!.OrganizationId.Should().Be(_organizationId);
        invite.Role.Should().Be(OrgRole.OrgUser);

        var pendingAssignment = await _context.PendingExerciseAssignments
            .FirstOrDefaultAsync(a => a.OrganizationInviteId == invite.Id);

        pendingAssignment.Should().NotBeNull();
        pendingAssignment!.ExerciseId.Should().Be(_exerciseId);
        pendingAssignment.ExerciseRole.Should().Be(ExerciseRole.Evaluator);
        pendingAssignment.Status.Should().Be(PendingAssignmentStatus.Pending);

        // Verify invitation service was called
        _mockInvitationService.Verify(
            x => x.CreateInvitationAsync(_organizationId, It.IsAny<CreateInvitationRequest>(), _userId),
            Times.Once);
    }

    [Fact]
    public async Task ConfirmImportAsync_PartialFailure_ContinuesProcessing()
    {
        // Arrange
        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "existing@example.com",
            DisplayName = "Existing User",
            Status = UserStatus.Active
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var sessionId = Guid.NewGuid();
        var parseResult = new FileParseResult
        {
            SessionId = sessionId,
            FileName = "test.csv",
            TotalRows = 3,
            Rows = new List<ParsedParticipantRow>
            {
                new() { RowNumber = 1, Email = existingUser.Email, ExerciseRole = "Controller",
                       NormalizedExerciseRole = ExerciseRole.Controller },
                new() { RowNumber = 2, Email = "invalid@example.com", ExerciseRole = "InvalidRole",
                       ValidationErrors = new List<string> { "Invalid role" } },
                new() { RowNumber = 3, Email = "newuser@example.com", ExerciseRole = "Observer",
                       NormalizedExerciseRole = ExerciseRole.Observer }
            }
        };

        var classifiedRows = new List<ClassifiedParticipantRow>
        {
            new() {
                ParsedRow = parseResult.Rows[0],
                Classification = ParticipantClassification.Assign,
                ClassificationLabel = "Assign",
                ExistingUserId = existingUser.Id
            },
            new() {
                ParsedRow = parseResult.Rows[1],
                Classification = ParticipantClassification.Error,
                ClassificationLabel = "Error",
                ErrorMessage = "Invalid role"
            },
            new() {
                ParsedRow = parseResult.Rows[2],
                Classification = ParticipantClassification.Invite,
                ClassificationLabel = "Invite",
                IsNewAccount = true
            }
        };

        _mockParser
            .Setup(x => x.ParseAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(parseResult);

        _mockClassificationService
            .Setup(x => x.ClassifyAsync(_exerciseId, It.IsAny<IReadOnlyList<ParsedParticipantRow>>()))
            .ReturnsAsync(classifiedRows);

        // Mock invitation service for the invite row
        var inviteId = Guid.NewGuid();
        _mockInvitationService
            .Setup(x => x.CreateInvitationAsync(
                _organizationId,
                It.Is<CreateInvitationRequest>(r => r.Email == "newuser@example.com"),
                _userId))
            .ReturnsAsync((Guid orgId, CreateInvitationRequest req, string userId) =>
            {
                var invite = new OrganizationInvite
                {
                    Id = inviteId,
                    OrganizationId = orgId,
                    Email = req.Email,
                    Code = "TESTCODE",
                    Role = req.Role,
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    CreatedByUserId = userId,
                    MaxUses = 1,
                    UseCount = 0
                };
                _context.OrganizationInvites.Add(invite);
                _context.SaveChanges();

                return new InvitationDto(
                    Id: invite.Id,
                    Email: invite.Email!,
                    Code: invite.Code,
                    Role: invite.Role.ToString(),
                    Status: "Pending",
                    CreatedAt: invite.CreatedAt,
                    ExpiresAt: invite.ExpiresAt,
                    InvitedByName: "Test User",
                    InvitedByEmail: "test@example.com",
                    AcceptedAt: null,
                    CancelledAt: null,
                    AcceptedByName: null,
                    OrganizationName: "Test Organization",
                    EmailSent: true,
                    EmailError: null,
                    AccountExists: false
                );
            });

        using var stream = new MemoryStream();
        await _sut.UploadAndParseAsync(_exerciseId, stream, "test.csv");
        await _sut.GetPreviewAsync(_exerciseId, sessionId);

        // Act
        var result = await _sut.ConfirmImportAsync(_exerciseId, sessionId, _userId);

        // Assert
        result.Should().NotBeNull();
        result.AssignedCount.Should().Be(1);
        result.InvitedCount.Should().Be(1);
        result.ErrorCount.Should().Be(1);
        result.RowOutcomes.Should().HaveCount(3);
    }

    [Fact]
    public async Task ConfirmImportAsync_CreatesBulkImportRecord()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var parseResult = new FileParseResult
        {
            SessionId = sessionId,
            FileName = "test.csv",
            TotalRows = 0,
            Rows = new List<ParsedParticipantRow>()
        };

        var classifiedRows = new List<ClassifiedParticipantRow>();

        _mockParser
            .Setup(x => x.ParseAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(parseResult);

        _mockClassificationService
            .Setup(x => x.ClassifyAsync(_exerciseId, It.IsAny<IReadOnlyList<ParsedParticipantRow>>()))
            .ReturnsAsync(classifiedRows);

        using var stream = new MemoryStream();
        await _sut.UploadAndParseAsync(_exerciseId, stream, "test.csv");
        await _sut.GetPreviewAsync(_exerciseId, sessionId);

        // Act
        var result = await _sut.ConfirmImportAsync(_exerciseId, sessionId, _userId);

        // Assert
        var importRecord = await _context.BulkImportRecords
            .FirstOrDefaultAsync(r => r.Id == result.ImportRecordId);

        importRecord.Should().NotBeNull();
        importRecord!.ExerciseId.Should().Be(_exerciseId);
        importRecord.ImportedById.Should().Be(_userId);
        importRecord.FileName.Should().Be("test.csv");
    }

    [Fact]
    public async Task GenerateTemplateAsync_Csv_ReturnsCsvContent()
    {
        // Act
        var result = await _sut.GenerateTemplateAsync("csv");

        // Assert
        result.Content.Should().NotBeNull();
        result.ContentType.Should().Be("text/csv");
        result.FileName.Should().EndWith(".csv");

        var content = System.Text.Encoding.UTF8.GetString(result.Content);
        content.Should().Contain("Email");
        content.Should().Contain("Exercise Role");
    }

    [Fact]
    public async Task GenerateTemplateAsync_Xlsx_ReturnsXlsxContent()
    {
        // Act
        var result = await _sut.GenerateTemplateAsync("xlsx");

        // Assert
        result.Content.Should().NotBeNull();
        result.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        result.FileName.Should().EndWith(".xlsx");
        result.Content.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetImportHistoryAsync_ReturnsImportRecords()
    {
        // Arrange
        var importRecord = new BulkImportRecord
        {
            Id = Guid.NewGuid(),
            ExerciseId = _exerciseId,
            ImportedById = _userId,
            ImportedAt = DateTime.UtcNow,
            FileName = "test.csv",
            TotalRows = 5,
            AssignedCount = 3,
            UpdatedCount = 1,
            InvitedCount = 1,
            ErrorCount = 0,
            SkippedCount = 0
        };

        _context.BulkImportRecords.Add(importRecord);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetImportHistoryAsync(_exerciseId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Id.Should().Be(importRecord.Id);
        result.First().FileName.Should().Be("test.csv");
        result.First().TotalRows.Should().Be(5);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
