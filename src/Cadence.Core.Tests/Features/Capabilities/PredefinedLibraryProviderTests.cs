using Cadence.Core.Features.Capabilities.Services;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Capabilities;

/// <summary>
/// Tests for the PredefinedLibraryProvider service.
/// </summary>
public class PredefinedLibraryProviderTests
{
    private readonly IPredefinedLibraryProvider _sut;

    public PredefinedLibraryProviderTests()
    {
        _sut = new PredefinedLibraryProvider();
    }

    [Fact]
    public void GetAvailableLibraries_ReturnsAllFourLibraries()
    {
        // Act
        var libraries = _sut.GetAvailableLibraries().ToList();

        // Assert
        libraries.Should().HaveCount(4);
        libraries.Select(l => l.Id).Should().BeEquivalentTo(new[] { "FEMA", "NATO", "NIST", "ISO" });
    }

    [Fact]
    public void GetAvailableLibraries_IncludesCorrectMetadata()
    {
        // Act
        var libraries = _sut.GetAvailableLibraries().ToList();

        // Assert
        var femaLibrary = libraries.First(l => l.Id == "FEMA");
        femaLibrary.Name.Should().Be("FEMA Core Capabilities");
        femaLibrary.Description.Should().Contain("32 FEMA Core Capabilities");
        femaLibrary.CapabilityCount.Should().Be(31); // 32 capabilities minus 1 duplicate (Planning appears in multiple mission areas)

        var natoLibrary = libraries.First(l => l.Id == "NATO");
        natoLibrary.CapabilityCount.Should().Be(7);

        var nistLibrary = libraries.First(l => l.Id == "NIST");
        nistLibrary.CapabilityCount.Should().Be(6);

        var isoLibrary = libraries.First(l => l.Id == "ISO");
        isoLibrary.CapabilityCount.Should().Be(10);
    }

    [Fact]
    public void GetLibrary_WithValidId_ReturnsLibraryWithCapabilities()
    {
        // Act
        var library = _sut.GetLibrary("FEMA");

        // Assert
        library.Should().NotBeNull();
        library!.Id.Should().Be("FEMA");
        library.Name.Should().Be("FEMA Core Capabilities");
        library.Capabilities.Should().NotBeEmpty();
        library.Capabilities.Should().HaveCount(31);
    }

    [Fact]
    public void GetLibrary_WithInvalidId_ReturnsNull()
    {
        // Act
        var library = _sut.GetLibrary("INVALID");

        // Assert
        library.Should().BeNull();
    }

    [Fact]
    public void GetLibrary_IsCaseInsensitive()
    {
        // Act
        var library = _sut.GetLibrary("fema");

        // Assert
        library.Should().NotBeNull();
        library!.Id.Should().Be("FEMA");
    }

    [Fact]
    public void GetLibrary_FEMA_ContainsExpectedCapabilities()
    {
        // Act
        var library = _sut.GetLibrary("FEMA");

        // Assert
        library.Should().NotBeNull();
        var capabilities = library!.Capabilities;

        // Check for a few key capabilities
        capabilities.Should().Contain(c => c.Name == "Planning");
        capabilities.Should().Contain(c => c.Name == "Mass Care Services");
        capabilities.Should().Contain(c => c.Name == "Cybersecurity");
        capabilities.Should().Contain(c => c.Name == "Economic Recovery");

        // Verify categories are present
        var planning = capabilities.First(c => c.Name == "Planning");
        planning.Category.Should().Be("Prevention");
        planning.Description.Should().NotBeNullOrEmpty();

        var massCare = capabilities.First(c => c.Name == "Mass Care Services");
        massCare.Category.Should().Be("Response");
    }

    [Fact]
    public void GetLibrary_NATO_ContainsExpectedCapabilities()
    {
        // Act
        var library = _sut.GetLibrary("NATO");

        // Assert
        library.Should().NotBeNull();
        library!.Capabilities.Should().HaveCount(7);
        library.Capabilities.Should().Contain(c => c.Name == "Command and Control");
        library.Capabilities.Should().Contain(c => c.Name == "Chemical, Biological, Radiological, and Nuclear (CBRN) Defence");
    }

    [Fact]
    public void GetLibrary_NIST_ContainsExpectedCapabilities()
    {
        // Act
        var library = _sut.GetLibrary("NIST");

        // Assert
        library.Should().NotBeNull();
        library!.Capabilities.Should().HaveCount(6);
        library.Capabilities.Select(c => c.Name).Should().BeEquivalentTo(new[]
        {
            "Govern", "Identify", "Protect", "Detect", "Respond", "Recover"
        });

        // All should have category "Core Functions"
        library.Capabilities.Should().OnlyContain(c => c.Category == "Core Functions");
    }

    [Fact]
    public void GetLibrary_ISO_ContainsExpectedCapabilities()
    {
        // Act
        var library = _sut.GetLibrary("ISO");

        // Assert
        library.Should().NotBeNull();
        library!.Capabilities.Should().HaveCount(10);
        library.Capabilities.Should().Contain(c => c.Name == "Business Impact Analysis");
        library.Capabilities.Should().Contain(c => c.Name == "Testing and Exercising");
        library.Capabilities.Should().Contain(c => c.Name == "Continuous Improvement");
    }
}
