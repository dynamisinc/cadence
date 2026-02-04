using Cadence.Core.Constants;
using Cadence.Core.Data;
using Cadence.Core.Features.Notifications.Models.DTOs;
using Cadence.Core.Features.Notifications.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.Notifications;

/// <summary>
/// Tests for ApprovalNotificationService (S08: Approval Notifications).
/// </summary>
public class ApprovalNotificationServiceTests
{
    private readonly Mock<ICurrentOrganizationContext> _orgContextMock;
    private readonly Mock<ILogger<ApprovalNotificationService>> _loggerMock;
    private readonly Mock<IExerciseHubContext> _hubContextMock;

    public ApprovalNotificationServiceTests()
    {
        _orgContextMock = new Mock<ICurrentOrganizationContext>();
        _loggerMock = new Mock<ILogger<ApprovalNotificationService>>();
        _hubContextMock = new Mock<IExerciseHubContext>();
    }

    private (AppDbContext context, Organization org) CreateTestContext()
    {
        var context = TestDbContextFactory.Create();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            Slug = "test-org",
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        context.Organizations.Add(org);
        context.SaveChanges();

        return (context, org);
    }

    private ApplicationUser CreateUser(AppDbContext context, Organization org, string displayName)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = $"{displayName.ToLower().Replace(" ", ".")}@test.com",
            UserName = $"{displayName.ToLower().Replace(" ", ".")}@test.com",
            DisplayName = displayName,
            OrganizationId = org.Id,
            Status = UserStatus.Active
        };
        context.ApplicationUsers.Add(user);
        context.SaveChanges();

        return user;
    }

    private Exercise CreateExercise(AppDbContext context, Organization org, string name = "Test Exercise")
    {
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = name,
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Draft,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            RequireInjectApproval = true,
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        context.Exercises.Add(exercise);
        context.SaveChanges();

        return exercise;
    }

    private void AssignParticipant(
        AppDbContext context,
        Exercise exercise,
        ApplicationUser user,
        ExerciseRole role)
    {
        var participant = new ExerciseParticipant
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            UserId = user.Id,
            Role = role,
            AssignedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.ExerciseParticipants.Add(participant);
        context.SaveChanges();
    }

    private Inject CreateInject(
        AppDbContext context,
        Exercise exercise,
        int injectNumber,
        InjectStatus status,
        string? submittedBy = null)
    {
        // Create MSEL if needed
        var msel = context.Msels.FirstOrDefault(m => m.ExerciseId == exercise.Id);
        if (msel == null)
        {
            msel = new Msel
            {
                Id = Guid.NewGuid(),
                Name = "Test MSEL",
                Version = 1,
                ExerciseId = exercise.Id,
                IsActive = true,
                OrganizationId = exercise.OrganizationId,
                CreatedBy = Guid.Empty.ToString(),
                ModifiedBy = Guid.Empty.ToString()
            };
            context.Msels.Add(msel);
            exercise.ActiveMselId = msel.Id;
            context.SaveChanges();
        }

        var inject = new Inject
        {
            Id = Guid.NewGuid(),
            InjectNumber = injectNumber,
            Title = $"Inject {injectNumber}",
            Description = $"Description for inject {injectNumber}",
            ScheduledTime = new TimeOnly(9, 0),
            Target = "Target",
            InjectType = InjectType.Standard,
            Status = status,
            Sequence = injectNumber,
            MselId = msel.Id,
            SubmittedByUserId = submittedBy,
            SubmittedAt = submittedBy != null ? DateTime.UtcNow : null,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Injects.Add(inject);
        context.SaveChanges();

        return inject;
    }

    private ApprovalNotificationService CreateService(AppDbContext context, Organization? testOrg = null)
    {
        // Use provided org or find the first non-default org created during test setup
        var org = testOrg ?? context.Organizations
            .FirstOrDefault(o => o.Id != Constants.SystemConstants.DefaultOrganizationId)
            ?? context.Organizations.First();

        _orgContextMock.Setup(x => x.CurrentOrganizationId).Returns(org.Id);
        _orgContextMock.Setup(x => x.HasContext).Returns(true);
        _orgContextMock.Setup(x => x.IsSysAdmin).Returns(false);

        return new ApprovalNotificationService(
            context,
            _orgContextMock.Object,
            _hubContextMock.Object,
            _loggerMock.Object);
    }

    #region NotifyInjectSubmittedAsync

    [Fact]
    public async Task NotifyInjectSubmittedAsync_CreatesNotificationForExerciseDirector()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var director = CreateUser(context, org, "John Director");
        var controller = CreateUser(context, org, "Jane Controller");
        var exercise = CreateExercise(context, org);
        AssignParticipant(context, exercise, director, ExerciseRole.ExerciseDirector);
        AssignParticipant(context, exercise, controller, ExerciseRole.Controller);

        var inject = CreateInject(context, exercise, 1, InjectStatus.Submitted, controller.Id);

        var service = CreateService(context);

        // Act
        await service.NotifyInjectSubmittedAsync(inject);

        // Assert
        var notification = await context.ApprovalNotifications
            .FirstOrDefaultAsync(n => n.InjectId == inject.Id);

        notification.Should().NotBeNull();
        notification!.UserId.Should().Be(director.Id);
        notification.ExerciseId.Should().Be(exercise.Id);
        notification.InjectId.Should().Be(inject.Id);
        notification.Type.Should().Be(ApprovalNotificationType.InjectSubmitted);
        notification.Title.Should().Contain("submitted for approval");
        notification.Message.Should().Contain(inject.Title);
        notification.TriggeredByUserId.Should().Be(controller.Id);
        notification.IsRead.Should().BeFalse();
        notification.ReadAt.Should().BeNull();
    }

    [Fact]
    public async Task NotifyInjectSubmittedAsync_SendsSignalRNotification()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var director = CreateUser(context, org, "John Director");
        var controller = CreateUser(context, org, "Jane Controller");
        var exercise = CreateExercise(context, org);
        AssignParticipant(context, exercise, director, ExerciseRole.ExerciseDirector);

        var inject = CreateInject(context, exercise, 1, InjectStatus.Submitted, controller.Id);

        var service = CreateService(context);

        // Act
        await service.NotifyInjectSubmittedAsync(inject);

        // Assert
        _hubContextMock.Verify(
            h => h.NotifyInjectSubmitted(exercise.Id, It.IsAny<Core.Features.Injects.Models.DTOs.InjectDto>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyInjectSubmittedAsync_NotifiesMultipleDirectors()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var director1 = CreateUser(context, org, "Director One");
        var director2 = CreateUser(context, org, "Director Two");
        var controller = CreateUser(context, org, "Jane Controller");
        var exercise = CreateExercise(context, org);
        AssignParticipant(context, exercise, director1, ExerciseRole.ExerciseDirector);
        AssignParticipant(context, exercise, director2, ExerciseRole.ExerciseDirector);

        var inject = CreateInject(context, exercise, 1, InjectStatus.Submitted, controller.Id);

        var service = CreateService(context);

        // Act
        await service.NotifyInjectSubmittedAsync(inject);

        // Assert
        var notifications = await context.ApprovalNotifications
            .Where(n => n.InjectId == inject.Id)
            .ToListAsync();

        notifications.Should().HaveCount(2);
        notifications.Should().Contain(n => n.UserId == director1.Id);
        notifications.Should().Contain(n => n.UserId == director2.Id);
    }

    #endregion

    #region NotifyInjectApprovedAsync

    [Fact]
    public async Task NotifyInjectApprovedAsync_NotifiesAuthor_NotSelf()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var director = CreateUser(context, org, "John Director");
        var controller = CreateUser(context, org, "Jane Controller");
        var exercise = CreateExercise(context, org);
        AssignParticipant(context, exercise, director, ExerciseRole.ExerciseDirector);

        var inject = CreateInject(context, exercise, 1, InjectStatus.Approved, controller.Id);
        inject.ApprovedByUserId = director.Id;
        inject.ApprovedAt = DateTime.UtcNow;
        inject.ApproverNotes = "Looks good!";
        context.SaveChanges();

        var service = CreateService(context);

        // Act
        await service.NotifyInjectApprovedAsync(inject);

        // Assert
        var notification = await context.ApprovalNotifications
            .FirstOrDefaultAsync(n => n.InjectId == inject.Id);

        notification.Should().NotBeNull();
        notification!.UserId.Should().Be(controller.Id);
        notification.Type.Should().Be(ApprovalNotificationType.InjectApproved);
        notification.Title.Should().Contain("approved");
        notification.TriggeredByUserId.Should().Be(director.Id);
    }

    [Fact]
    public async Task NotifyInjectApprovedAsync_DoesNotNotifySelf_WhenApproverIsAuthor()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var director = CreateUser(context, org, "John Director");
        var exercise = CreateExercise(context, org);
        AssignParticipant(context, exercise, director, ExerciseRole.ExerciseDirector);

        // Director submits and approves their own inject
        var inject = CreateInject(context, exercise, 1, InjectStatus.Approved, director.Id);
        inject.ApprovedByUserId = director.Id;
        inject.ApprovedAt = DateTime.UtcNow;
        context.SaveChanges();

        var service = CreateService(context);

        // Act
        await service.NotifyInjectApprovedAsync(inject);

        // Assert - should not create notification
        var notifications = await context.ApprovalNotifications
            .Where(n => n.InjectId == inject.Id)
            .ToListAsync();

        notifications.Should().BeEmpty();
    }

    #endregion

    #region NotifyInjectRejectedAsync

    [Fact]
    public async Task NotifyInjectRejectedAsync_NotifiesAuthorWithReason()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var director = CreateUser(context, org, "John Director");
        var controller = CreateUser(context, org, "Jane Controller");
        var exercise = CreateExercise(context, org);

        var inject = CreateInject(context, exercise, 1, InjectStatus.Draft, controller.Id);
        inject.RejectedByUserId = director.Id;
        inject.RejectedAt = DateTime.UtcNow;
        inject.RejectionReason = "Needs more detail on expected actions";
        context.SaveChanges();

        var service = CreateService(context);

        // Act
        await service.NotifyInjectRejectedAsync(inject);

        // Assert
        var notification = await context.ApprovalNotifications
            .FirstOrDefaultAsync(n => n.InjectId == inject.Id);

        notification.Should().NotBeNull();
        notification!.UserId.Should().Be(controller.Id);
        notification.Type.Should().Be(ApprovalNotificationType.InjectRejected);
        notification.Title.Should().Contain("rejected");
        notification.Message.Should().Contain(inject.RejectionReason);
    }

    #endregion

    #region NotifyInjectRevertedAsync

    [Fact]
    public async Task NotifyInjectRevertedAsync_NotifiesAuthorWithReason()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var director = CreateUser(context, org, "John Director");
        var controller = CreateUser(context, org, "Jane Controller");
        var exercise = CreateExercise(context, org);

        var inject = CreateInject(context, exercise, 1, InjectStatus.Submitted, controller.Id);
        inject.RevertedByUserId = director.Id;
        inject.RevertedAt = DateTime.UtcNow;
        inject.RevertReason = "Timeline changed, needs review";
        context.SaveChanges();

        var service = CreateService(context);

        // Act
        await service.NotifyInjectRevertedAsync(inject);

        // Assert
        var notification = await context.ApprovalNotifications
            .FirstOrDefaultAsync(n => n.InjectId == inject.Id);

        notification.Should().NotBeNull();
        notification!.UserId.Should().Be(controller.Id);
        notification.Type.Should().Be(ApprovalNotificationType.InjectReverted);
        notification.Title.Should().Contain("reverted");
        notification.Message.Should().Contain(inject.RevertReason);
    }

    #endregion

    #region Batch Notifications

    [Fact]
    public async Task NotifyBatchApprovedAsync_CreatesConsolidatedNotificationsByAuthor()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var director = CreateUser(context, org, "John Director");
        var controller1 = CreateUser(context, org, "Controller One");
        var controller2 = CreateUser(context, org, "Controller Two");
        var exercise = CreateExercise(context, org);

        var inject1 = CreateInject(context, exercise, 1, InjectStatus.Approved, controller1.Id);
        var inject2 = CreateInject(context, exercise, 2, InjectStatus.Approved, controller1.Id);
        var inject3 = CreateInject(context, exercise, 3, InjectStatus.Approved, controller2.Id);

        var injects = new List<Inject> { inject1, inject2, inject3 };

        var service = CreateService(context);

        // Act
        await service.NotifyBatchApprovedAsync(director.Id, injects, "Batch approved for timeline sync");

        // Assert
        var notifications = await context.ApprovalNotifications
            .Where(n => n.Type == ApprovalNotificationType.InjectApproved)
            .ToListAsync();

        // Should create 2 notifications - one per author
        notifications.Should().HaveCount(2);

        var notification1 = notifications.First(n => n.UserId == controller1.Id);
        notification1.InjectId.Should().BeNull(); // Batch notification
        notification1.Message.Should().Contain("2 injects");

        var notification2 = notifications.First(n => n.UserId == controller2.Id);
        notification2.InjectId.Should().BeNull();
        notification2.Message.Should().Contain("1 inject");
    }

    [Fact]
    public async Task NotifyBatchRejectedAsync_CreatesConsolidatedNotificationsByAuthor()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var director = CreateUser(context, org, "John Director");
        var controller1 = CreateUser(context, org, "Controller One");
        var controller2 = CreateUser(context, org, "Controller Two");
        var exercise = CreateExercise(context, org);

        var inject1 = CreateInject(context, exercise, 1, InjectStatus.Draft, controller1.Id);
        var inject2 = CreateInject(context, exercise, 2, InjectStatus.Draft, controller1.Id);
        var inject3 = CreateInject(context, exercise, 3, InjectStatus.Draft, controller2.Id);

        var injects = new List<Inject> { inject1, inject2, inject3 };

        var service = CreateService(context);

        // Act
        await service.NotifyBatchRejectedAsync(director.Id, injects, "Timeline needs revision");

        // Assert
        var notifications = await context.ApprovalNotifications
            .Where(n => n.Type == ApprovalNotificationType.InjectRejected)
            .ToListAsync();

        // Should create 2 notifications - one per author
        notifications.Should().HaveCount(2);

        var notification1 = notifications.First(n => n.UserId == controller1.Id);
        notification1.Message.Should().Contain("Timeline needs revision");
    }

    #endregion

    #region GetNotificationsAsync

    [Fact]
    public async Task GetNotificationsAsync_ReturnsNotificationsForCurrentUser()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var user1 = CreateUser(context, org, "User One");
        var user2 = CreateUser(context, org, "User Two");
        var exercise = CreateExercise(context, org);

        // Create notifications for both users
        var notification1 = new ApprovalNotification
        {
            Id = Guid.NewGuid(),
            UserId = user1.Id,
            ExerciseId = exercise.Id,
            InjectId = null,
            Type = ApprovalNotificationType.InjectSubmitted,
            Title = "Notification for User 1",
            Message = "Message 1",
            OrganizationId = org.Id,
            IsRead = false,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        var notification2 = new ApprovalNotification
        {
            Id = Guid.NewGuid(),
            UserId = user2.Id,
            ExerciseId = exercise.Id,
            InjectId = null,
            Type = ApprovalNotificationType.InjectApproved,
            Title = "Notification for User 2",
            Message = "Message 2",
            OrganizationId = org.Id,
            IsRead = false,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.ApprovalNotifications.AddRange(notification1, notification2);
        context.SaveChanges();

        var service = CreateService(context, org);

        // Act
        var results = await service.GetNotificationsAsync(user1.Id);

        // Assert
        results.Should().HaveCount(1);
        results.First().UserId.Should().Be(user1.Id);
        results.First().Title.Should().Be("Notification for User 1");
    }

    [Fact]
    public async Task GetNotificationsAsync_UnreadOnly_FiltersReadNotifications()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var user = CreateUser(context, org, "Test User");
        var exercise = CreateExercise(context, org);

        var unreadNotification = new ApprovalNotification
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ExerciseId = exercise.Id,
            Type = ApprovalNotificationType.InjectSubmitted,
            Title = "Unread",
            Message = "Message",
            OrganizationId = org.Id,
            IsRead = false,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        var readNotification = new ApprovalNotification
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ExerciseId = exercise.Id,
            Type = ApprovalNotificationType.InjectApproved,
            Title = "Read",
            Message = "Message",
            OrganizationId = org.Id,
            IsRead = true,
            ReadAt = DateTime.UtcNow.AddMinutes(-10),
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.ApprovalNotifications.AddRange(unreadNotification, readNotification);
        context.SaveChanges();

        var service = CreateService(context, org);

        // Act
        var results = await service.GetNotificationsAsync(user.Id, unreadOnly: true);

        // Assert
        results.Should().HaveCount(1);
        results.First().IsRead.Should().BeFalse();
        results.First().Title.Should().Be("Unread");
    }

    [Fact]
    public async Task GetNotificationsAsync_RespectsLimit()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var user = CreateUser(context, org, "Test User");
        var exercise = CreateExercise(context, org);

        // Create 25 notifications
        for (int i = 0; i < 25; i++)
        {
            var notification = new ApprovalNotification
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ExerciseId = exercise.Id,
                Type = ApprovalNotificationType.InjectSubmitted,
                Title = $"Notification {i}",
                Message = "Message",
                OrganizationId = org.Id,
                IsRead = false,
                CreatedBy = Guid.Empty.ToString(),
                ModifiedBy = Guid.Empty.ToString()
            };
            context.ApprovalNotifications.Add(notification);
        }
        context.SaveChanges();

        var service = CreateService(context, org);

        // Act
        var results = await service.GetNotificationsAsync(user.Id, limit: 10);

        // Assert
        results.Should().HaveCount(10);
    }

    #endregion

    #region GetUnreadCountAsync

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var user = CreateUser(context, org, "Test User");
        var exercise = CreateExercise(context, org);

        // Create 3 unread, 2 read
        for (int i = 0; i < 3; i++)
        {
            context.ApprovalNotifications.Add(new ApprovalNotification
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ExerciseId = exercise.Id,
                Type = ApprovalNotificationType.InjectSubmitted,
                Title = $"Unread {i}",
                Message = "Message",
                OrganizationId = org.Id,
                IsRead = false,
                CreatedBy = Guid.Empty.ToString(),
                ModifiedBy = Guid.Empty.ToString()
            });
        }
        for (int i = 0; i < 2; i++)
        {
            context.ApprovalNotifications.Add(new ApprovalNotification
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ExerciseId = exercise.Id,
                Type = ApprovalNotificationType.InjectApproved,
                Title = $"Read {i}",
                Message = "Message",
                OrganizationId = org.Id,
                IsRead = true,
                ReadAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty.ToString(),
                ModifiedBy = Guid.Empty.ToString()
            });
        }
        context.SaveChanges();

        var service = CreateService(context, org);

        // Act
        var count = await service.GetUnreadCountAsync(user.Id);

        // Assert
        count.Should().Be(3);
    }

    #endregion

    #region MarkAsReadAsync

    [Fact]
    public async Task MarkAsReadAsync_MarksNotificationAsRead()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var user = CreateUser(context, org, "Test User");
        var exercise = CreateExercise(context, org);

        var notification = new ApprovalNotification
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ExerciseId = exercise.Id,
            Type = ApprovalNotificationType.InjectSubmitted,
            Title = "Test",
            Message = "Message",
            OrganizationId = org.Id,
            IsRead = false,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.ApprovalNotifications.Add(notification);
        context.SaveChanges();

        var service = CreateService(context);

        // Act
        await service.MarkAsReadAsync(user.Id, notification.Id);

        // Assert
        var updated = await context.ApprovalNotifications.FindAsync(notification.Id);
        updated!.IsRead.Should().BeTrue();
        updated.ReadAt.Should().NotBeNull();
        updated.ReadAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region MarkAllAsReadAsync

    [Fact]
    public async Task MarkAllAsReadAsync_MarksAllUnreadNotificationsAsRead()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var user = CreateUser(context, org, "Test User");
        var exercise = CreateExercise(context, org);

        for (int i = 0; i < 3; i++)
        {
            context.ApprovalNotifications.Add(new ApprovalNotification
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ExerciseId = exercise.Id,
                Type = ApprovalNotificationType.InjectSubmitted,
                Title = $"Notification {i}",
                Message = "Message",
                OrganizationId = org.Id,
                IsRead = false,
                CreatedBy = Guid.Empty.ToString(),
                ModifiedBy = Guid.Empty.ToString()
            });
        }
        context.SaveChanges();

        var service = CreateService(context, org);

        // Act
        await service.MarkAllAsReadAsync(user.Id);

        // Assert - Clear tracked entities and re-query to get fresh data
        context.ChangeTracker.Clear();
        var notifications = await context.ApprovalNotifications
            .Where(n => n.UserId == user.Id)
            .ToListAsync();

        notifications.Should().AllSatisfy(n =>
        {
            n.IsRead.Should().BeTrue();
            n.ReadAt.Should().NotBeNull();
        });
    }

    #endregion
}
