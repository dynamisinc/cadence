using Cadence.Core.Data;
using Cadence.Core.Features.BulkParticipantImport.Services;
using Cadence.Core.Features.Organizations.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.BulkParticipantImport;

/// <summary>
/// Tests for the import session lifecycle in <see cref="BulkParticipantImportService"/>.
///
/// The session store is a private static <c>ConcurrentDictionary</c> with a 30-minute expiry.
/// The expiry is enforced by a private sealed <c>ImportSession</c> class with a hardcoded timeout,
/// so true time-based expiry is not testable without either clock injection or a long sleep.
/// These tests cover the observable contract for the common failure path: accessing a session
/// that was never created (non-existent session ID).
/// </summary>
public class BulkParticipantImportSessionTests : IDisposable
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

    public BulkParticipantImportSessionTests()
    {
        _context = TestDbContextFactory.Create();
        _mockParser = new Mock<IParticipantFileParser>();
        _mockClassificationService = new Mock<IParticipantClassificationService>();
        _mockMembershipService = new Mock<IMembershipService>();
        _mockInvitationService = new Mock<IOrganizationInvitationService>();
        _mockOrgContext = new Mock<ICurrentOrganizationContext>();
        _mockLogger = new Mock<ILogger<BulkParticipantImportService>>();

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

        _context.Organizations.Add(organization);
        _context.Exercises.Add(exercise);
        _context.SaveChanges();
    }

    // =========================================================================
    // GetPreviewAsync — session lifecycle
    // =========================================================================

    [Fact]
    public async Task GetPreviewAsync_NonExistentSession_ThrowsKeyNotFoundException()
    {
        // Arrange — a session ID that was never registered
        var nonExistentSessionId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.GetPreviewAsync(_exerciseId, nonExistentSessionId));

        exception.Message.Should().Contain("Import session");
    }

    // =========================================================================
    // ConfirmImportAsync — session lifecycle
    // =========================================================================

    [Fact]
    public async Task ConfirmImportAsync_NonExistentSession_ThrowsKeyNotFoundException()
    {
        // Arrange — a session ID that was never registered
        var nonExistentSessionId = Guid.NewGuid();
        var importingUserId = Guid.NewGuid().ToString();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.ConfirmImportAsync(_exerciseId, nonExistentSessionId, importingUserId));

        exception.Message.Should().Contain("Import session");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
