using Cadence.Core.Data;
using Cadence.Core.Features.DeliveryMethods.Models.DTOs;
using Cadence.Core.Features.DeliveryMethods.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Tests.Features.DeliveryMethods;

/// <summary>
/// Unit tests for <see cref="DeliveryMethodService"/>.
/// Note: The in-memory test database is pre-seeded with 7 system delivery methods via
/// <see cref="Cadence.Core.Data.Configurations.DeliveryMethodLookupConfiguration"/>:
///   Verbal (1), Phone (2), Email (3), Radio (4), Written (5), Simulation (6), Other/IsOther (99).
/// Tests that check counts or query the full set are written to account for this baseline.
/// </summary>
public class DeliveryMethodServiceTests
{
    private const int SeedCount = 7;
    private const int SeedActiveCount = 7;

    private static readonly Guid SeededVerbalId  = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid SeededPhoneId   = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid SeededEmailId   = Guid.Parse("10000000-0000-0000-0000-000000000003");
    private static readonly Guid SeededRadioId   = Guid.Parse("10000000-0000-0000-0000-000000000004");
    private static readonly Guid SeededOtherId   = Guid.Parse("10000000-0000-0000-0000-000000000007");

    // =========================================================================
    // Test Setup Helpers
    // =========================================================================

    private DeliveryMethodLookup AddTestMethod(
        AppDbContext context,
        string name,
        int sortOrder = 50,
        bool isActive = true,
        bool isOther = false)
    {
        var method = new DeliveryMethodLookup
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = isActive,
            SortOrder = sortOrder,
            IsOther = isOther,
            CreatedBy = "test",
            ModifiedBy = "test"
        };
        context.DeliveryMethods.Add(method);
        context.SaveChanges();
        return method;
    }

    private DeliveryMethodService CreateService(AppDbContext context) =>
        new(context);

    // =========================================================================
    // GetAllAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyActiveMethods()
    {
        // Arrange — add an inactive method on top of seeded data
        var context = TestDbContextFactory.Create();
        var inactive = AddTestMethod(context, "Test-Inactive-Method", isActive: false);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetAllAsync();

        // Assert — inactive method must not appear
        result.Should().NotContain(m => m.Id == inactive.Id);
        result.Should().OnlyContain(m => m.IsActive);
    }

    [Fact]
    public async Task GetAllAsync_AddsNewActiveMethod_AppearsInResults()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var extra = AddTestMethod(context, "Test-Custom-Active-Method", isActive: true);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetAllAsync();

        // Assert
        result.Should().Contain(m => m.Id == extra.Id);
        result.Should().HaveCount(SeedActiveCount + 1);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMethodsOrderedBySortOrder()
    {
        // Arrange — add a method with sort order 0 (lower than all seeded methods)
        var context = TestDbContextFactory.Create();
        var firstMethod = AddTestMethod(context, "Test-First-In-Sort", sortOrder: 0);
        var sut = CreateService(context);

        // Act
        var result = (await sut.GetAllAsync()).ToList();

        // Assert — our method should be first
        result[0].Id.Should().Be(firstMethod.Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        // Arrange — retrieve a known seeded method
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act
        var result = await sut.GetAllAsync();

        // Assert — "Radio" is seeded and active
        var dto = result.Single(m => m.Id == SeededRadioId);
        dto.Should().BeOfType<DeliveryMethodDto>();
        dto.Name.Should().Be("Radio");
        dto.IsActive.Should().BeTrue();
    }

    // =========================================================================
    // GetAllIncludingInactiveAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetAllIncludingInactiveAsync_ReturnsActiveAndInactiveMethods()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var inactive = AddTestMethod(context, "Test-Inactive-Included", isActive: false);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetAllIncludingInactiveAsync();

        // Assert — seed rows (all active) + our inactive row
        result.Should().Contain(m => m.Id == inactive.Id);
        result.Should().HaveCount(SeedCount + 1);
    }

    [Fact]
    public async Task GetAllIncludingInactiveAsync_OrdersBySortOrderThenName()
    {
        // Arrange — add two methods with the same high sort order; alphabetical tie-break
        var context = TestDbContextFactory.Create();
        var zebra = AddTestMethod(context, "ZZZ-Test-Last",  sortOrder: 200);
        var alpha = AddTestMethod(context, "AAA-Test-First", sortOrder: 200);
        var sut = CreateService(context);

        // Act
        var result = (await sut.GetAllIncludingInactiveAsync()).ToList();

        // Assert — AAA should appear before ZZZ at the same sort order
        var alphaIdx = result.FindIndex(m => m.Id == alpha.Id);
        var zebraIdx = result.FindIndex(m => m.Id == zebra.Id);
        alphaIdx.Should().BeLessThan(zebraIdx);
    }

    // =========================================================================
    // GetByIdAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetByIdAsync_MethodExists_ReturnsDto()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var method = AddTestMethod(context, "Test-GetById-Target");
        var sut = CreateService(context);

        // Act
        var result = await sut.GetByIdAsync(method.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(method.Id);
        result.Name.Should().Be("Test-GetById-Target");
    }

    [Fact]
    public async Task GetByIdAsync_MethodNotFound_ReturnsNull()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act
        var result = await sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_CanRetrieveSeededMethod()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act
        var result = await sut.GetByIdAsync(SeededRadioId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Radio");
    }

    // =========================================================================
    // CreateAsync Tests
    // =========================================================================

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedDto()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var request = new CreateDeliveryMethodRequest
        {
            Name = "Test-Satellite-Phone",
            Description = "Satellite phone communication",
            SortOrder = 10
        };
        var sut = CreateService(context);

        // Act
        var result = await sut.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test-Satellite-Phone");
        result.Description.Should().Be("Satellite phone communication");
        result.SortOrder.Should().Be(10);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_PersistsToDatabase()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act
        var result = await sut.CreateAsync(new CreateDeliveryMethodRequest { Name = "Test-Persist-Method" });

        // Assert
        var persisted = await context.DeliveryMethods.FindAsync(result.Id);
        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be("Test-Persist-Method");
    }

    [Fact]
    public async Task CreateAsync_TrimsNameWhitespace()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act
        var result = await sut.CreateAsync(new CreateDeliveryMethodRequest { Name = "  Test-Trimmed-Name  " });

        // Assert
        result.Name.Should().Be("Test-Trimmed-Name");
    }

    [Fact]
    public async Task CreateAsync_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act
        var act = () => sut.CreateAsync(new CreateDeliveryMethodRequest { Name = "   " });

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public async Task CreateAsync_NameExceeds100Characters_ThrowsArgumentException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act
        var act = () => sut.CreateAsync(new CreateDeliveryMethodRequest { Name = new string('A', 101) });

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*exceed 100 characters*");
    }

    [Fact]
    public async Task CreateAsync_DescriptionExceeds500Characters_ThrowsArgumentException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act
        var act = () => sut.CreateAsync(new CreateDeliveryMethodRequest
        {
            Name = "Test-Valid-Name-Long-Desc",
            Description = new string('D', 501)
        });

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*exceed 500 characters*");
    }

    [Fact]
    public async Task CreateAsync_DuplicateSeededName_ThrowsInvalidOperationException()
    {
        // Arrange — "Radio" is seeded
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act
        var act = () => sut.CreateAsync(new CreateDeliveryMethodRequest { Name = "Radio" });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task CreateAsync_DuplicateNameCaseInsensitive_ThrowsInvalidOperationException()
    {
        // Arrange — "Radio" is seeded; try uppercase variant
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act
        var act = () => sut.CreateAsync(new CreateDeliveryMethodRequest { Name = "RADIO" });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task CreateAsync_IsOther_WhenSeededOtherAlreadyExists_ThrowsInvalidOperationException()
    {
        // Arrange — "Other" with IsOther=true is seeded
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act
        var act = () => sut.CreateAsync(new CreateDeliveryMethodRequest
        {
            Name = "Test-Another-Other",
            IsOther = true
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Only one delivery method can be marked as 'Other'*");
    }

    [Fact]
    public async Task CreateAsync_UniqueNonOtherName_Succeeds()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act
        var result = await sut.CreateAsync(new CreateDeliveryMethodRequest
        {
            Name = "Test-Digital-Whiteboard",
            IsOther = false
        });

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test-Digital-Whiteboard");
        result.IsOther.Should().BeFalse();
    }

    // =========================================================================
    // UpdateAsync Tests
    // =========================================================================

    [Fact]
    public async Task UpdateAsync_ValidRequest_ReturnsUpdatedDto()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var method = AddTestMethod(context, "Test-Update-Source", sortOrder: 10);
        var request = new UpdateDeliveryMethodRequest
        {
            Name = "Test-Update-Target",
            Description = "Updated description",
            SortOrder = 55,
            IsActive = true,
            IsOther = false
        };
        var sut = CreateService(context);

        // Act
        var result = await sut.UpdateAsync(method.Id, request);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test-Update-Target");
        result.Description.Should().Be("Updated description");
        result.SortOrder.Should().Be(55);
    }

    [Fact]
    public async Task UpdateAsync_MethodNotFound_ReturnsNull()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act
        var result = await sut.UpdateAsync(Guid.NewGuid(), new UpdateDeliveryMethodRequest { Name = "Update" });

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var method = AddTestMethod(context, "Test-EmptyName-Source");
        var sut = CreateService(context);

        // Act
        var act = () => sut.UpdateAsync(method.Id, new UpdateDeliveryMethodRequest { Name = "" });

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public async Task UpdateAsync_DuplicateNameOnDifferentRecord_ThrowsInvalidOperationException()
    {
        // Arrange — create a custom method, then try to rename it to "Email" (seeded)
        var context = TestDbContextFactory.Create();
        var method = AddTestMethod(context, "Test-Original-Name");
        var sut = CreateService(context);

        // Act
        var act = () => sut.UpdateAsync(method.Id, new UpdateDeliveryMethodRequest { Name = "Email" });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_SameNameOnSameRecord_DoesNotThrowDuplicateError()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var method = AddTestMethod(context, "Test-Same-Name-Method");
        var sut = CreateService(context);

        // Act — self-update: same name is permitted
        var result = await sut.UpdateAsync(method.Id, new UpdateDeliveryMethodRequest
        {
            Name = "Test-Same-Name-Method",
            IsActive = false
        });

        // Assert
        result.Should().NotBeNull();
        result!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_SetIsOtherTrue_WhenAnotherRecordAlreadyIsOther_ThrowsInvalidOperationException()
    {
        // Arrange — seeded "Other" record exists; add a new method and try to mark it IsOther
        var context = TestDbContextFactory.Create();
        var method = AddTestMethod(context, "Test-NotOther-Method", isOther: false);
        var sut = CreateService(context);

        // Act
        var act = () => sut.UpdateAsync(method.Id, new UpdateDeliveryMethodRequest
        {
            Name = "Test-NotOther-Method",
            IsOther = true
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Only one delivery method can be marked as 'Other'*");
    }

    [Fact]
    public async Task UpdateAsync_SetIsOtherTrueOnExistingOtherRecord_DoesNotThrow()
    {
        // Arrange — updating the seeded "Other" record to keep IsOther=true is a no-op and is allowed
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act
        var result = await sut.UpdateAsync(SeededOtherId, new UpdateDeliveryMethodRequest
        {
            Name = "Other",
            IsOther = true,
            IsActive = true
        });

        // Assert
        result.Should().NotBeNull();
        result!.IsOther.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_CanDeactivateMethod()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var method = AddTestMethod(context, "Test-Deactivate-Me", isActive: true);
        var sut = CreateService(context);

        // Act
        var result = await sut.UpdateAsync(method.Id, new UpdateDeliveryMethodRequest
        {
            Name = "Test-Deactivate-Me",
            IsActive = false
        });

        // Assert
        result!.IsActive.Should().BeFalse();

        // Verify it no longer appears in active-only results
        var activeOnly = await sut.GetAllAsync();
        activeOnly.Should().NotContain(m => m.Id == method.Id);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChangesToDatabase()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var method = AddTestMethod(context, "Test-Before-Persist");
        var sut = CreateService(context);

        // Act
        await sut.UpdateAsync(method.Id, new UpdateDeliveryMethodRequest
        {
            Name = "Test-After-Persist",
            SortOrder = 77
        });

        // Assert
        var persisted = await context.DeliveryMethods.FindAsync(method.Id);
        persisted!.Name.Should().Be("Test-After-Persist");
        persisted.SortOrder.Should().Be(77);
    }

    // =========================================================================
    // DeleteAsync Tests
    // =========================================================================

    [Fact]
    public async Task DeleteAsync_MethodExists_ReturnsTrue()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var method = AddTestMethod(context, "Test-Delete-Me");
        var sut = CreateService(context);

        // Act
        var result = await sut.DeleteAsync(method.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_MethodNotFound_ReturnsFalse()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act
        var result = await sut.DeleteAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesRecord()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var method = AddTestMethod(context, "Test-Soft-Delete-Target");
        var sut = CreateService(context);

        // Act
        await sut.DeleteAsync(method.Id);

        // Assert — use IgnoreQueryFilters to bypass the IsDeleted soft-delete filter
        var persisted = context.DeliveryMethods
            .IgnoreQueryFilters()
            .Single(dm => dm.Id == method.Id);
        persisted.IsDeleted.Should().BeTrue();
        persisted.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletedMethodNoLongerReturnedByGetAllIncludingInactive()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var method = AddTestMethod(context, "Test-Hidden-After-Delete");
        var sut = CreateService(context);

        // Act
        await sut.DeleteAsync(method.Id);
        var result = await sut.GetAllIncludingInactiveAsync();

        // Assert — back to seed count
        result.Should().NotContain(m => m.Id == method.Id);
        result.Should().HaveCount(SeedCount);
    }

    // =========================================================================
    // ReorderAsync Tests
    // =========================================================================

    [Fact]
    public async Task ReorderAsync_ValidIds_UpdatesSortOrderByPosition()
    {
        // Arrange — use three seeded methods to verify reordering
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Reverse the first three seeded methods
        var newOrder = new List<Guid> { SeededEmailId, SeededPhoneId, SeededVerbalId };

        // Act
        await sut.ReorderAsync(newOrder);

        // Assert — positions are 0-indexed
        var verbal = await context.DeliveryMethods.FindAsync(SeededVerbalId);
        var phone  = await context.DeliveryMethods.FindAsync(SeededPhoneId);
        var email  = await context.DeliveryMethods.FindAsync(SeededEmailId);
        email!.SortOrder.Should().Be(0);
        phone!.SortOrder.Should().Be(1);
        verbal!.SortOrder.Should().Be(2);
    }

    [Fact]
    public async Task ReorderAsync_UnrecognizedIdsAreIgnored()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var method = AddTestMethod(context, "Test-Reorder-Known", sortOrder: 50);
        var sut = CreateService(context);

        // Include a bogus ID alongside the known one
        var newOrder = new List<Guid> { Guid.NewGuid(), method.Id };

        // Act — must not throw
        await sut.ReorderAsync(newOrder);

        // Assert — known method gets position 1 (0-indexed)
        var persisted = await context.DeliveryMethods.FindAsync(method.Id);
        persisted!.SortOrder.Should().Be(1);
    }
}
