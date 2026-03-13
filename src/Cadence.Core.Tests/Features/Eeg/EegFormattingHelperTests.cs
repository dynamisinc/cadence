using Cadence.Core.Features.Eeg.Builders;
using Cadence.Core.Models.Entities;
using ClosedXML.Excel;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Eeg;

/// <summary>
/// Tests for EegFormattingHelper — shared rating display utilities and text formatting
/// used by all EEG Excel worksheet builders.
/// </summary>
public class EegFormattingHelperTests
{
    [Fact]
    public void GetRatingDisplay_AllRatings_ReturnNonEmptyStrings()
    {
        var ratings = new[]
        {
            PerformanceRating.Performed,
            PerformanceRating.SomeChallenges,
            PerformanceRating.MajorChallenges,
            PerformanceRating.UnableToPerform
        };

        foreach (var rating in ratings)
        {
            var display = EegFormattingHelper.GetRatingDisplay(rating);
            display.Should().NotBeNullOrWhiteSpace(
                because: $"rating {rating} should produce a non-empty display string");
        }
    }

    [Fact]
    public void GetRatingShortCode_AllRatings_ReturnNonEmptyStrings()
    {
        var expectedCodes = new Dictionary<PerformanceRating, string>
        {
            { PerformanceRating.Performed, "P" },
            { PerformanceRating.SomeChallenges, "S" },
            { PerformanceRating.MajorChallenges, "M" },
            { PerformanceRating.UnableToPerform, "U" }
        };

        foreach (var (rating, expectedCode) in expectedCodes)
        {
            var shortCode = EegFormattingHelper.GetRatingShortCode(rating);
            shortCode.Should().Be(expectedCode,
                because: $"rating {rating} should return the single-letter code '{expectedCode}'");
        }
    }

    [Fact]
    public void TruncateText_ShortText_ReturnsUnchanged()
    {
        var text = "Short text";
        var result = EegFormattingHelper.TruncateText(text, maxLength: 50);
        result.Should().Be("Short text");
    }

    [Fact]
    public void TruncateText_LongText_TruncatesWithEllipsis()
    {
        var longText = new string('A', 200);
        var result = EegFormattingHelper.TruncateText(longText, maxLength: 10);
        result.Should().HaveLength(10);
        result.Should().EndWith("...");
    }

    [Fact]
    public void TruncateText_ExactLength_ReturnsUnchanged()
    {
        var text = "Exactly20Characters!";
        var result = EegFormattingHelper.TruncateText(text, maxLength: 20);
        result.Should().Be(text);
        result.Should().HaveLength(20);
    }

    [Fact]
    public void RatingColors_AllRatingsHaveEntries()
    {
        var ratings = new[]
        {
            PerformanceRating.Performed,
            PerformanceRating.SomeChallenges,
            PerformanceRating.MajorChallenges,
            PerformanceRating.UnableToPerform
        };

        foreach (var rating in ratings)
        {
            EegFormattingHelper.RatingColors.Should().ContainKey(rating,
                because: $"RatingColors should have an entry for {rating}");
            EegFormattingHelper.RatingColors[rating].Should().NotBeNull(
                because: $"the color for {rating} should not be null");
        }
    }

    [Fact]
    public void RatingBackgroundColors_AllRatingsHaveEntries()
    {
        var ratings = new[]
        {
            PerformanceRating.Performed,
            PerformanceRating.SomeChallenges,
            PerformanceRating.MajorChallenges,
            PerformanceRating.UnableToPerform
        };

        foreach (var rating in ratings)
        {
            EegFormattingHelper.RatingBackgroundColors.Should().ContainKey(rating,
                because: $"RatingBackgroundColors should have an entry for {rating}");
            EegFormattingHelper.RatingBackgroundColors[rating].Should().NotBeNull(
                because: $"the background color for {rating} should not be null");
        }
    }
}
