using Cadence.Core.Data;
using Cadence.Core.Features.ExpectedOutcomes.Models.DTOs;
using Cadence.Core.Features.ExpectedOutcomes.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.ExpectedOutcomes;

/// <summary>
/// Tests for ExpectedOutcomeService — CRUD, evaluation, reordering, and validation
/// for expected outcomes on injects.
/// </summary>
public class ExpectedOutcomeServiceTests
{
    private const string UserId = "test-user-id";

    private (AppDbContext context, ExpectedOutcomeService service, Guid injectId) CreateTestContext(
        ExerciseStatus exerciseStatus = ExerciseStatus.Active)
    {
        var context = TestDbContextFactory.Create();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Org",
            Slug = "test-org"
        };
        context.Organizations.Add(org);

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            OrganizationId = org.Id,
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = exerciseStatus
        };
        context.Exercises.Add(exercise);

        var msel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "Test MSEL",
            ExerciseId = exercise.Id,
            OrganizationId = org.Id
        };
        context.Msels.Add(msel);

        var inject = new Inject
        {
            Id = Guid.NewGuid(),
            MselId = msel.Id,
            Title = "Test Inject",
            Description = "Test description",
            Target = "EOC",
            InjectNumber = 1
        };
        context.Injects.Add(inject);
        context.SaveChanges();

        var service = new ExpectedOutcomeService(context);
        return (context, service, inject.Id);
    }

    private async Task<ExpectedOutcomeDto> SeedOutcome(
        AppDbContext context,
        ExpectedOutcomeService service,
        Guid injectId,
        string description = "Test outcome",
        int? sortOrder = null)
    {
        var request = new CreateExpectedOutcomeRequest
        {
            Description = description,
            SortOrder = sortOrder
        };
        return await service.CreateAsync(injectId, request, UserId);
    }

    // =========================================================================
    // ValidateInjectAsync
    // =========================================================================

    [Fact]
    public async Task ValidateInjectAsync_NonexistentInject_ReturnsFalse()
    {
        var (_, service, _) = CreateTestContext();

        var result = await service.ValidateInjectAsync(Guid.NewGuid());

        result.InjectExists.Should().BeFalse();
        result.ExerciseIsArchived.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateInjectAsync_ActiveExercise_ReturnsNotArchived()
    {
        var (_, service, injectId) = CreateTestContext(ExerciseStatus.Active);

        var result = await service.ValidateInjectAsync(injectId);

        result.InjectExists.Should().BeTrue();
        result.ExerciseIsArchived.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateInjectAsync_ArchivedExercise_ReturnsArchived()
    {
        var (_, service, injectId) = CreateTestContext(ExerciseStatus.Archived);

        var result = await service.ValidateInjectAsync(injectId);

        result.InjectExists.Should().BeTrue();
        result.ExerciseIsArchived.Should().BeTrue();
    }

    // =========================================================================
    // GetByInjectIdAsync
    // =========================================================================

    [Fact]
    public async Task GetByInjectIdAsync_NoOutcomes_ReturnsEmptyList()
    {
        var (_, service, injectId) = CreateTestContext();

        var result = await service.GetByInjectIdAsync(injectId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByInjectIdAsync_MultipleOutcomes_ReturnsSortedBySortOrder()
    {
        var (context, service, injectId) = CreateTestContext();
        await SeedOutcome(context, service, injectId, "Third", sortOrder: 2);
        await SeedOutcome(context, service, injectId, "First", sortOrder: 0);
        await SeedOutcome(context, service, injectId, "Second", sortOrder: 1);

        var result = await service.GetByInjectIdAsync(injectId);

        result.Should().HaveCount(3);
        result[0].Description.Should().Be("First");
        result[1].Description.Should().Be("Second");
        result[2].Description.Should().Be("Third");
    }

    // =========================================================================
    // GetByIdAsync
    // =========================================================================

    [Fact]
    public async Task GetByIdAsync_NonexistentId_ReturnsNull()
    {
        var (_, service, _) = CreateTestContext();

        var result = await service.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingOutcome_ReturnsDto()
    {
        var (context, service, injectId) = CreateTestContext();
        var created = await SeedOutcome(context, service, injectId, "My outcome");

        var result = await service.GetByIdAsync(created.Id);

        result.Should().NotBeNull();
        result!.Description.Should().Be("My outcome");
        result.InjectId.Should().Be(injectId);
    }

    // =========================================================================
    // CreateAsync
    // =========================================================================

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsDto()
    {
        var (_, service, injectId) = CreateTestContext();
        var request = new CreateExpectedOutcomeRequest { Description = "New outcome" };

        var result = await service.CreateAsync(injectId, request, UserId);

        result.Should().NotBeNull();
        result.Description.Should().Be("New outcome");
        result.InjectId.Should().Be(injectId);
        result.WasAchieved.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_NonexistentInject_ThrowsKeyNotFoundException()
    {
        var (_, service, _) = CreateTestContext();
        var request = new CreateExpectedOutcomeRequest { Description = "Test" };

        var act = () => service.CreateAsync(Guid.NewGuid(), request, UserId);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_EmptyDescription_ThrowsArgumentException()
    {
        var (_, service, injectId) = CreateTestContext();
        var request = new CreateExpectedOutcomeRequest { Description = "   " };

        var act = () => service.CreateAsync(injectId, request, UserId);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Description*required*");
    }

    [Fact]
    public async Task CreateAsync_DescriptionExceeds1000Chars_ThrowsArgumentException()
    {
        var (_, service, injectId) = CreateTestContext();
        var request = new CreateExpectedOutcomeRequest { Description = new string('x', 1001) };

        var act = () => service.CreateAsync(injectId, request, UserId);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*1000*");
    }

    [Fact]
    public async Task CreateAsync_NoSortOrder_AutoAssignsNextSortOrder()
    {
        var (context, service, injectId) = CreateTestContext();
        await SeedOutcome(context, service, injectId, "First");

        var second = await service.CreateAsync(injectId,
            new CreateExpectedOutcomeRequest { Description = "Second" }, UserId);

        second.SortOrder.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_ExplicitSortOrder_UsesProvidedValue()
    {
        var (_, service, injectId) = CreateTestContext();
        var request = new CreateExpectedOutcomeRequest { Description = "Test", SortOrder = 42 };

        var result = await service.CreateAsync(injectId, request, UserId);

        result.SortOrder.Should().Be(42);
    }

    // =========================================================================
    // UpdateAsync
    // =========================================================================

    [Fact]
    public async Task UpdateAsync_NonexistentId_ReturnsNull()
    {
        var (_, service, _) = CreateTestContext();
        var request = new UpdateExpectedOutcomeRequest { Description = "Updated" };

        var result = await service.UpdateAsync(Guid.NewGuid(), request, UserId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesDescription()
    {
        var (context, service, injectId) = CreateTestContext();
        var created = await SeedOutcome(context, service, injectId, "Original");
        var request = new UpdateExpectedOutcomeRequest { Description = "Updated" };

        var result = await service.UpdateAsync(created.Id, request, UserId);

        result.Should().NotBeNull();
        result!.Description.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateAsync_EmptyDescription_ThrowsArgumentException()
    {
        var (context, service, injectId) = CreateTestContext();
        var created = await SeedOutcome(context, service, injectId, "Original");
        var request = new UpdateExpectedOutcomeRequest { Description = "" };

        var act = () => service.UpdateAsync(created.Id, request, UserId);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateAsync_DescriptionExceeds1000Chars_ThrowsArgumentException()
    {
        var (context, service, injectId) = CreateTestContext();
        var created = await SeedOutcome(context, service, injectId, "Original");
        var request = new UpdateExpectedOutcomeRequest { Description = new string('x', 1001) };

        var act = () => service.UpdateAsync(created.Id, request, UserId);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // =========================================================================
    // EvaluateAsync
    // =========================================================================

    [Fact]
    public async Task EvaluateAsync_NonexistentId_ReturnsNull()
    {
        var (_, service, _) = CreateTestContext();
        var request = new EvaluateExpectedOutcomeRequest { WasAchieved = true };

        var result = await service.EvaluateAsync(Guid.NewGuid(), request, UserId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateAsync_SetAchieved_UpdatesWasAchieved()
    {
        var (context, service, injectId) = CreateTestContext();
        var created = await SeedOutcome(context, service, injectId);
        var request = new EvaluateExpectedOutcomeRequest
        {
            WasAchieved = true,
            EvaluatorNotes = "Well done"
        };

        var result = await service.EvaluateAsync(created.Id, request, UserId);

        result.Should().NotBeNull();
        result!.WasAchieved.Should().BeTrue();
        result.EvaluatorNotes.Should().Be("Well done");
    }

    [Fact]
    public async Task EvaluateAsync_ClearEvaluation_SetsNull()
    {
        var (context, service, injectId) = CreateTestContext();
        var created = await SeedOutcome(context, service, injectId);
        // First evaluate
        await service.EvaluateAsync(created.Id,
            new EvaluateExpectedOutcomeRequest { WasAchieved = true }, UserId);
        // Then clear
        var result = await service.EvaluateAsync(created.Id,
            new EvaluateExpectedOutcomeRequest { WasAchieved = null }, UserId);

        result!.WasAchieved.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateAsync_NotesExceed2000Chars_ThrowsArgumentException()
    {
        var (context, service, injectId) = CreateTestContext();
        var created = await SeedOutcome(context, service, injectId);
        var request = new EvaluateExpectedOutcomeRequest
        {
            EvaluatorNotes = new string('x', 2001)
        };

        var act = () => service.EvaluateAsync(created.Id, request, UserId);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*2000*");
    }

    // =========================================================================
    // ReorderAsync
    // =========================================================================

    [Fact]
    public async Task ReorderAsync_NonexistentInject_ReturnsFalse()
    {
        var (_, service, _) = CreateTestContext();
        var request = new ReorderExpectedOutcomesRequest { OutcomeIds = new List<Guid>() };

        var result = await service.ReorderAsync(Guid.NewGuid(), request, UserId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ReorderAsync_ValidReorder_UpdatesSortOrders()
    {
        var (context, service, injectId) = CreateTestContext();
        var first = await SeedOutcome(context, service, injectId, "A", 0);
        var second = await SeedOutcome(context, service, injectId, "B", 1);
        var third = await SeedOutcome(context, service, injectId, "C", 2);

        // Reverse order
        var request = new ReorderExpectedOutcomesRequest
        {
            OutcomeIds = new List<Guid> { third.Id, second.Id, first.Id }
        };

        var result = await service.ReorderAsync(injectId, request, UserId);
        result.Should().BeTrue();

        var outcomes = await service.GetByInjectIdAsync(injectId);
        outcomes[0].Description.Should().Be("C");
        outcomes[1].Description.Should().Be("B");
        outcomes[2].Description.Should().Be("A");
    }

    [Fact]
    public async Task ReorderAsync_IdFromOtherInject_ThrowsArgumentException()
    {
        var (context, service, injectId) = CreateTestContext();
        var outcome = await SeedOutcome(context, service, injectId, "A", 0);

        var request = new ReorderExpectedOutcomesRequest
        {
            OutcomeIds = new List<Guid> { outcome.Id, Guid.NewGuid() }
        };

        var act = () => service.ReorderAsync(injectId, request, UserId);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*do not belong*");
    }

    [Fact]
    public async Task ReorderAsync_MissingIds_ThrowsArgumentException()
    {
        var (context, service, injectId) = CreateTestContext();
        await SeedOutcome(context, service, injectId, "A", 0);
        await SeedOutcome(context, service, injectId, "B", 1);

        // Only include one of two
        var outcomes = await service.GetByInjectIdAsync(injectId);
        var request = new ReorderExpectedOutcomesRequest
        {
            OutcomeIds = new List<Guid> { outcomes[0].Id }
        };

        var act = () => service.ReorderAsync(injectId, request, UserId);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*number*must match*");
    }

    // =========================================================================
    // DeleteAsync
    // =========================================================================

    [Fact]
    public async Task DeleteAsync_NonexistentId_ReturnsFalse()
    {
        var (_, service, _) = CreateTestContext();

        var result = await service.DeleteAsync(Guid.NewGuid(), UserId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ExistingOutcome_RemovesAndReturnsTrue()
    {
        var (context, service, injectId) = CreateTestContext();
        var created = await SeedOutcome(context, service, injectId);

        var result = await service.DeleteAsync(created.Id, UserId);

        result.Should().BeTrue();

        var remaining = await service.GetByInjectIdAsync(injectId);
        remaining.Should().BeEmpty();
    }
}
