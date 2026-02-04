using System.Text.Json;
using Cadence.Core.Models.Entities;
using FluentAssertions;

namespace Cadence.Core.Tests.Enums;

/// <summary>
/// Tests for InjectStatus enum ensuring HSEEP compliance per FEMA PrepToolkit.
/// </summary>
public class InjectStatusTests
{
    #region Enum Value Tests

    [Fact]
    public void InjectStatus_HasExactlyEightValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<InjectStatus>();

        // Assert - HSEEP defines exactly 8 statuses
        values.Should().HaveCount(8,
            "HSEEP standard defines exactly 8 inject statuses: Draft, Submitted, Approved, Synchronized, Released, Complete, Deferred, Obsolete");
    }

    [Theory]
    [InlineData(InjectStatus.Draft, 0)]
    [InlineData(InjectStatus.Submitted, 1)]
    [InlineData(InjectStatus.Approved, 2)]
    [InlineData(InjectStatus.Synchronized, 3)]
    [InlineData(InjectStatus.Released, 4)]
    [InlineData(InjectStatus.Complete, 5)]
    [InlineData(InjectStatus.Deferred, 6)]
    [InlineData(InjectStatus.Obsolete, 7)]
    public void InjectStatus_HasCorrectUnderlyingValues(InjectStatus status, int expectedValue)
    {
        // Assert
        ((int)status).Should().Be(expectedValue,
            $"InjectStatus.{status} should have underlying value {expectedValue} per HSEEP standard ordering");
    }

    [Fact]
    public void InjectStatus_ContainsAllHseepStatuses()
    {
        // Arrange - HSEEP standard status names
        var expectedStatuses = new[]
        {
            "Draft",
            "Submitted",
            "Approved",
            "Synchronized",
            "Released",
            "Complete",
            "Deferred",
            "Obsolete"
        };

        // Act
        var actualNames = Enum.GetNames<InjectStatus>();

        // Assert
        actualNames.Should().BeEquivalentTo(expectedStatuses,
            "enum should contain all HSEEP-defined status names");
    }

    #endregion

    #region JSON Serialization Tests

    [Theory]
    [InlineData(InjectStatus.Draft, "Draft")]
    [InlineData(InjectStatus.Submitted, "Submitted")]
    [InlineData(InjectStatus.Approved, "Approved")]
    [InlineData(InjectStatus.Synchronized, "Synchronized")]
    [InlineData(InjectStatus.Released, "Released")]
    [InlineData(InjectStatus.Complete, "Complete")]
    [InlineData(InjectStatus.Deferred, "Deferred")]
    [InlineData(InjectStatus.Obsolete, "Obsolete")]
    public void InjectStatus_SerializesToPascalCaseString(InjectStatus status, string expectedJson)
    {
        // Arrange
        var testObject = new { Status = status };

        // Act
        var json = JsonSerializer.Serialize(testObject);

        // Assert - Should serialize as string, not integer
        json.Should().Contain($"\"Status\":\"{expectedJson}\"",
            "inject status should serialize as PascalCase string per API contract");
    }

    [Theory]
    [InlineData("\"Draft\"", InjectStatus.Draft)]
    [InlineData("\"Submitted\"", InjectStatus.Submitted)]
    [InlineData("\"Approved\"", InjectStatus.Approved)]
    [InlineData("\"Synchronized\"", InjectStatus.Synchronized)]
    [InlineData("\"Released\"", InjectStatus.Released)]
    [InlineData("\"Complete\"", InjectStatus.Complete)]
    [InlineData("\"Deferred\"", InjectStatus.Deferred)]
    [InlineData("\"Obsolete\"", InjectStatus.Obsolete)]
    public void InjectStatus_DeserializesFromPascalCaseString(string jsonValue, InjectStatus expected)
    {
        // Arrange
        var json = $"{{\"Status\":{jsonValue}}}";

        // Act
        var result = JsonSerializer.Deserialize<StatusContainer>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(expected,
            "should deserialize PascalCase string to correct enum value");
    }

    [Fact]
    public void InjectStatus_ShouldNotSerializeAsInteger()
    {
        // Arrange
        var testObject = new { Status = InjectStatus.Released };

        // Act
        var json = JsonSerializer.Serialize(testObject);

        // Assert - Should NOT contain integer value
        json.Should().NotContain("\"Status\":4",
            "inject status should serialize as string, not integer");
    }

    private record StatusContainer(InjectStatus Status);

    #endregion

    #region Default Value Tests

    [Fact]
    public void InjectStatus_DefaultValueIsDraft()
    {
        // Arrange & Act
        var defaultStatus = default(InjectStatus);

        // Assert - Default should be Draft (0)
        defaultStatus.Should().Be(InjectStatus.Draft,
            "Draft should be the default status for new injects");
    }

    #endregion

    #region Status Category Tests

    [Fact]
    public void InjectStatus_ActiveStatuses_ShouldBeCorrect()
    {
        // Arrange - Statuses that indicate inject is still "in progress"
        var activeStatuses = new[]
        {
            InjectStatus.Draft,
            InjectStatus.Submitted,
            InjectStatus.Approved,
            InjectStatus.Synchronized
        };

        // Assert - These should all be less than Released
        foreach (var status in activeStatuses)
        {
            ((int)status).Should().BeLessThan((int)InjectStatus.Released,
                $"{status} should be an active (pre-release) status");
        }
    }

    [Fact]
    public void InjectStatus_TerminalStatuses_ShouldBeCorrect()
    {
        // Arrange - Statuses that indicate inject lifecycle is complete
        var terminalStatuses = new[]
        {
            InjectStatus.Released,
            InjectStatus.Complete,
            InjectStatus.Deferred,
            InjectStatus.Obsolete
        };

        // Assert - These should all be >= Released
        foreach (var status in terminalStatuses)
        {
            ((int)status).Should().BeGreaterThanOrEqualTo((int)InjectStatus.Released,
                $"{status} should be a terminal (post-release or cancelled) status");
        }
    }

    #endregion
}
