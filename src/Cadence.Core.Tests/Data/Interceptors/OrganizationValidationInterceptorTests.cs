using Cadence.Core.Data;
using Cadence.Core.Data.Interceptors;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Cadence.Core.Tests.Data.Interceptors;

/// <summary>
/// Tests for OrganizationValidationInterceptor write-side validation.
/// Verifies each guard branch in ValidateOrganizationScope.
/// </summary>
public class OrganizationValidationInterceptorTests : IDisposable
{
    private readonly Guid _orgAId = Guid.NewGuid();
    private readonly Guid _orgBId = Guid.NewGuid();
    private readonly string _dbName;

    public OrganizationValidationInterceptorTests()
    {
        _dbName = $"InterceptorTests_{Guid.NewGuid()}";
    }

    public void Dispose() { }

    private (AppDbContext context, OrganizationValidationInterceptor interceptor) CreateContextWithInterceptor(
        Mock<ICurrentOrganizationContext>? orgContextMock = null)
    {
        var services = new ServiceCollection();

        if (orgContextMock != null)
        {
            services.AddScoped(_ => orgContextMock.Object);
        }

        var serviceProvider = services.BuildServiceProvider();
        var interceptor = new OrganizationValidationInterceptor(serviceProvider);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_dbName)
            .AddInterceptors(interceptor)
            .Options;

        // Use the parameterless constructor (no org context for query filters —
        // we're testing the interceptor, not query filters)
        var context = new AppDbContext(options);
        return (context, interceptor);
    }

    private Mock<ICurrentOrganizationContext> CreateOrgContextMock(
        Guid? orgId = null,
        bool hasContext = true,
        bool isAuthenticated = true,
        bool isSysAdmin = false)
    {
        var mock = new Mock<ICurrentOrganizationContext>();
        mock.Setup(x => x.CurrentOrganizationId).Returns(orgId);
        mock.Setup(x => x.CurrentOrgRole).Returns(orgId.HasValue ? OrgRole.OrgAdmin : null);
        mock.Setup(x => x.HasContext).Returns(hasContext);
        mock.Setup(x => x.IsAuthenticated).Returns(isAuthenticated);
        mock.Setup(x => x.IsSysAdmin).Returns(isSysAdmin);
        return mock;
    }

    private Exercise CreateExercise(Guid orgId) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = orgId,
        Name = "Test Exercise",
        ExerciseType = ExerciseType.TTX,
        Status = ExerciseStatus.Draft,
        CreatedBy = Guid.NewGuid().ToString(),
        ModifiedBy = Guid.NewGuid().ToString()
    };

    private Organization CreateOrganization(Guid id) => new()
    {
        Id = id,
        Name = $"Org {id}",
        Slug = $"org-{id}",
        Status = OrgStatus.Active,
        CreatedBy = Guid.NewGuid().ToString(),
        ModifiedBy = Guid.NewGuid().ToString()
    };

    // =========================================================================
    // Guard Branch Tests
    // =========================================================================

    [Fact]
    public async Task SavingChanges_BypassOrgValidationTrue_DoesNotThrow()
    {
        var mock = CreateOrgContextMock(orgId: _orgAId);
        var (context, _) = CreateContextWithInterceptor(mock);

        // Add an exercise belonging to a DIFFERENT org than the user's context
        context.BypassOrgValidation = true;
        context.Add(CreateExercise(_orgBId));

        var act = () => context.SaveChangesAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SavingChanges_NullOrgContext_DoesNotThrow()
    {
        // No ICurrentOrganizationContext registered → GetOrgContext returns null
        var (context, _) = CreateContextWithInterceptor(orgContextMock: null);

        context.Add(CreateExercise(_orgAId));

        var act = () => context.SaveChangesAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SavingChanges_NoHttpContext_DoesNotThrow()
    {
        var mock = CreateOrgContextMock(orgId: _orgAId, hasContext: false);
        var (context, _) = CreateContextWithInterceptor(mock);

        // Cross-org write should be allowed when there's no HTTP context (seeding/background jobs)
        context.Add(CreateExercise(_orgBId));

        var act = () => context.SaveChangesAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SavingChanges_UnauthenticatedRequest_DoesNotThrow()
    {
        var mock = CreateOrgContextMock(orgId: null, hasContext: true, isAuthenticated: false);
        var (context, _) = CreateContextWithInterceptor(mock);

        context.Add(CreateExercise(_orgAId));

        var act = () => context.SaveChangesAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SavingChanges_SysAdmin_AllowsCrossOrgWrite()
    {
        var mock = CreateOrgContextMock(orgId: _orgAId, isSysAdmin: true);
        var (context, _) = CreateContextWithInterceptor(mock);

        // SysAdmin writing to a different org should be allowed
        context.Add(CreateExercise(_orgBId));

        var act = () => context.SaveChangesAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SavingChanges_MatchingOrgId_DoesNotThrow()
    {
        var mock = CreateOrgContextMock(orgId: _orgAId);
        var (context, _) = CreateContextWithInterceptor(mock);

        context.Add(CreateExercise(_orgAId));

        var act = () => context.SaveChangesAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SavingChanges_NoCurrentOrgId_ThrowsOrganizationAccessException()
    {
        // Authenticated user with no org selected
        var mock = CreateOrgContextMock(orgId: null, hasContext: true, isAuthenticated: true);
        var (context, _) = CreateContextWithInterceptor(mock);

        context.Add(CreateExercise(_orgAId));

        var act = () => context.SaveChangesAsync();
        await act.Should().ThrowAsync<OrganizationAccessException>()
            .WithMessage("*no organization context*");
    }

    [Fact]
    public async Task SavingChanges_MismatchedOrgId_ThrowsOrganizationAccessException()
    {
        var mock = CreateOrgContextMock(orgId: _orgAId);
        var (context, _) = CreateContextWithInterceptor(mock);

        // Try to write an exercise belonging to org B while context is org A
        context.Add(CreateExercise(_orgBId));

        var act = () => context.SaveChangesAsync();
        await act.Should().ThrowAsync<OrganizationAccessException>()
            .WithMessage($"*Cannot modify Exercise*{_orgBId}*{_orgAId}*");
    }

    // =========================================================================
    // Async path tests (verify both sync and async hooks call validation)
    // =========================================================================

    [Fact]
    public void SavingChangesSync_MatchingOrgId_DoesNotThrow()
    {
        var mock = CreateOrgContextMock(orgId: _orgAId);
        var (context, _) = CreateContextWithInterceptor(mock);

        context.Add(CreateExercise(_orgAId));

        var act = () => context.SaveChanges();
        act.Should().NotThrow();
    }

    [Fact]
    public void SavingChangesSync_MismatchedOrgId_ThrowsOrganizationAccessException()
    {
        var mock = CreateOrgContextMock(orgId: _orgAId);
        var (context, _) = CreateContextWithInterceptor(mock);

        context.Add(CreateExercise(_orgBId));

        var act = () => context.SaveChanges();
        act.Should().Throw<OrganizationAccessException>();
    }

    [Fact]
    public async Task SavingChanges_NonOrgScopedEntity_Ignored()
    {
        var mock = CreateOrgContextMock(orgId: _orgAId);
        var (context, _) = CreateContextWithInterceptor(mock);

        // Organization is BaseEntity only — NOT IOrganizationScoped
        context.Add(CreateOrganization(Guid.NewGuid()));

        var act = () => context.SaveChangesAsync();
        await act.Should().NotThrowAsync("non-org-scoped entities are not subject to org validation");
    }

    [Fact]
    public async Task SavingChanges_NewOrgScopedEntity_SetsOrgId()
    {
        var mock = CreateOrgContextMock(orgId: _orgAId);
        var (context, _) = CreateContextWithInterceptor(mock);
        var exercise = CreateExercise(_orgAId);
        context.Add(exercise);

        await context.SaveChangesAsync();

        var persisted = await context.Exercises.FindAsync(exercise.Id);
        persisted.Should().NotBeNull();
        persisted!.OrganizationId.Should().Be(_orgAId);
    }

    [Fact]
    public async Task SavingChangesAsync_SameBehaviorAsSync()
    {
        // Async path throws for mismatched org
        var asyncMock = CreateOrgContextMock(orgId: _orgAId);
        var (asyncCtx, _) = CreateContextWithInterceptor(asyncMock);
        asyncCtx.Add(CreateExercise(_orgBId));
        var asyncAct = () => asyncCtx.SaveChangesAsync();
        await asyncAct.Should().ThrowAsync<OrganizationAccessException>(
            "async save path must enforce the same org validation as the sync path");

        // Sync path throws for same mismatch
        var syncMock = CreateOrgContextMock(orgId: _orgAId);
        var (syncCtx, _) = CreateContextWithInterceptor(syncMock);
        syncCtx.Add(CreateExercise(_orgBId));
        var syncAct = () => syncCtx.SaveChanges();
        syncAct.Should().Throw<OrganizationAccessException>(
            "sync save path must also enforce org validation");
    }
}
