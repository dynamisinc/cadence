using Cadence.Core.Data;
using Cadence.Core.Features.Capabilities.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cadence.Core.Tests.Features.Capabilities;

/// <summary>
/// Tests for the CapabilityImportService.
/// </summary>
public class CapabilityImportServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ICapabilityImportService _sut;
    private readonly Guid _organizationId;

    public CapabilityImportServiceTests()
    {
        _context = TestDbContextFactory.Create();
        var libraryProvider = new PredefinedLibraryProvider();
        _sut = new CapabilityImportService(
            _context,
            libraryProvider,
            NullLogger<CapabilityImportService>.Instance);

        // Create test organization
        _organizationId = Guid.NewGuid();
        _context.Organizations.Add(new Organization
        {
            Id = _organizationId,
            Name = "Test Organization",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task ImportLibraryAsync_WithValidLibrary_ImportsAllCapabilities()
    {
        // Act
        var result = await _sut.ImportLibraryAsync(_organizationId, "NIST");

        // Assert
        result.Should().NotBeNull();
        result.TotalInLibrary.Should().Be(6);
        result.Imported.Should().Be(6);
        result.SkippedDuplicates.Should().Be(0);
        result.ImportedNames.Should().HaveCount(6);
        result.ImportedNames.Should().Contain("Govern");
        result.ImportedNames.Should().Contain("Recover");

        // Verify database
        var capabilities = await _context.Capabilities
            .Where(c => c.OrganizationId == _organizationId)
            .ToListAsync();

        capabilities.Should().HaveCount(6);
        capabilities.Should().OnlyContain(c => c.SourceLibrary == "NIST");
        capabilities.Should().OnlyContain(c => c.IsActive);
    }

    [Fact]
    public async Task ImportLibraryAsync_SetsCorrectSourceLibrary()
    {
        // Act
        await _sut.ImportLibraryAsync(_organizationId, "NATO");

        // Assert
        var capabilities = await _context.Capabilities
            .Where(c => c.OrganizationId == _organizationId)
            .ToListAsync();

        capabilities.Should().OnlyContain(c => c.SourceLibrary == "NATO");
    }

    [Fact]
    public async Task ImportLibraryAsync_PreservesCapabilityDetails()
    {
        // Act
        await _sut.ImportLibraryAsync(_organizationId, "FEMA");

        // Assert
        var planning = await _context.Capabilities
            .FirstOrDefaultAsync(c => c.OrganizationId == _organizationId && c.Name == "Planning");

        planning.Should().NotBeNull();
        planning!.Name.Should().Be("Planning");
        planning.Description.Should().NotBeNullOrEmpty();
        planning.Category.Should().Be("Prevention");
        planning.SourceLibrary.Should().Be("FEMA");
        planning.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ImportLibraryAsync_WithDuplicates_SkipsDuplicatesByName()
    {
        // Arrange - Create existing capability with same name (case-insensitive)
        _context.Capabilities.Add(new Capability
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            Name = "govern", // lowercase version of NIST capability "Govern"
            Description = "Existing capability",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.ImportLibraryAsync(_organizationId, "NIST");

        // Assert
        result.TotalInLibrary.Should().Be(6);
        result.Imported.Should().Be(5); // Should skip "Govern"
        result.SkippedDuplicates.Should().Be(1);
        result.ImportedNames.Should().HaveCount(5);
        result.ImportedNames.Should().NotContain("Govern");

        // Verify only 6 capabilities total (1 existing + 5 new)
        var capabilities = await _context.Capabilities
            .Where(c => c.OrganizationId == _organizationId)
            .ToListAsync();

        capabilities.Should().HaveCount(6);

        // Original should be unchanged
        var original = capabilities.First(c => c.Name == "govern");
        original.Description.Should().Be("Existing capability");
        original.SourceLibrary.Should().BeNull(); // Original didn't have source
    }

    [Fact]
    public async Task ImportLibraryAsync_WithInvalidLibrary_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ImportLibraryAsync(_organizationId, "INVALID_LIBRARY"));
    }

    [Fact]
    public async Task ImportLibraryAsync_WithInvalidOrganization_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidOrgId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ImportLibraryAsync(invalidOrgId, "FEMA"));
    }

    [Fact]
    public async Task ImportLibraryAsync_CanImportMultipleLibraries()
    {
        // Act
        await _sut.ImportLibraryAsync(_organizationId, "NIST");
        await _sut.ImportLibraryAsync(_organizationId, "NATO");

        // Assert
        var capabilities = await _context.Capabilities
            .Where(c => c.OrganizationId == _organizationId)
            .ToListAsync();

        capabilities.Should().HaveCount(13); // 6 NIST + 7 NATO
        capabilities.Count(c => c.SourceLibrary == "NIST").Should().Be(6);
        capabilities.Count(c => c.SourceLibrary == "NATO").Should().Be(7);
    }

    [Fact]
    public async Task ImportLibraryAsync_WithFEMALibrary_ImportsCorrectCount()
    {
        // Act
        var result = await _sut.ImportLibraryAsync(_organizationId, "FEMA");

        // Assert
        result.TotalInLibrary.Should().Be(31);
        result.Imported.Should().Be(31);
        result.SkippedDuplicates.Should().Be(0);

        var capabilities = await _context.Capabilities
            .Where(c => c.OrganizationId == _organizationId)
            .ToListAsync();

        capabilities.Should().HaveCount(31);
    }

    [Fact]
    public async Task ImportLibraryAsync_IsCaseInsensitive()
    {
        // Act
        var result = await _sut.ImportLibraryAsync(_organizationId, "nist"); // lowercase

        // Assert
        result.Imported.Should().Be(6);

        var capabilities = await _context.Capabilities
            .Where(c => c.OrganizationId == _organizationId)
            .ToListAsync();

        capabilities.Should().HaveCount(6);
        capabilities.Should().OnlyContain(c => c.SourceLibrary == "NIST"); // Should normalize to uppercase
    }

    [Fact]
    public async Task ImportLibraryAsync_ReturnsCorrectImportedNames()
    {
        // Act
        var result = await _sut.ImportLibraryAsync(_organizationId, "ISO");

        // Assert
        result.ImportedNames.Should().HaveCount(10);
        result.ImportedNames.Should().Contain("Business Impact Analysis");
        result.ImportedNames.Should().Contain("Testing and Exercising");
        result.ImportedNames.Should().Contain("Continuous Improvement");
    }

    public void Dispose()
    {
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
    }
}
