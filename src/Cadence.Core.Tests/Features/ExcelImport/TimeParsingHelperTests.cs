using Cadence.Core.Features.ExcelImport.Services;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.ExcelImport;

public class TimeParsingHelperTests
{
    #region TryParseMilitaryDtg Tests

    [Theory]
    [InlineData("210900AUG2011", 2011, 8, 21, 9, 0)]
    [InlineData("150800JAN2013", 2013, 1, 15, 8, 0)]
    [InlineData("010000MAR2020", 2020, 3, 1, 0, 0)]
    [InlineData("311530DEC2009", 2009, 12, 31, 15, 30)]
    [InlineData("051200MAY2010", 2010, 5, 5, 12, 0)]
    public void TryParseMilitaryDtg_StandardFormat_ParsesCorrectly(
        string input, int year, int month, int day, int hour, int minute)
    {
        var result = TimeParsingHelper.TryParseMilitaryDtg(input, out var parsed);

        result.Should().BeTrue();
        parsed.Should().Be(new DateTime(year, month, day, hour, minute, 0));
    }

    [Theory]
    [InlineData("210900AUG11", 2011, 8, 21, 9, 0)]
    [InlineData("150800JAN13", 2013, 1, 15, 8, 0)]
    public void TryParseMilitaryDtg_TwoDigitYear_ParsesCorrectly(
        string input, int year, int month, int day, int hour, int minute)
    {
        var result = TimeParsingHelper.TryParseMilitaryDtg(input, out var parsed);

        result.Should().BeTrue();
        parsed.Should().Be(new DateTime(year, month, day, hour, minute, 0));
    }

    [Fact]
    public void TryParseMilitaryDtg_CaseInsensitive_ParsesCorrectly()
    {
        var result = TimeParsingHelper.TryParseMilitaryDtg("210900aug2011", out var parsed);

        result.Should().BeTrue();
        parsed.Year.Should().Be(2011);
        parsed.Month.Should().Be(8);
    }

    [Theory]
    [InlineData("not a dtg")]
    [InlineData("")]
    [InlineData("210900XYZ2011")]
    [InlineData("abc")]
    [InlineData("12345")]
    [InlineData("320900AUG2011")] // Day 32 invalid
    [InlineData("212500AUG2011")] // Hour 25 invalid
    public void TryParseMilitaryDtg_InvalidFormat_ReturnsFalse(string input)
    {
        var result = TimeParsingHelper.TryParseMilitaryDtg(input, out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void TryParseMilitaryDtg_Null_ReturnsFalse()
    {
        var result = TimeParsingHelper.TryParseMilitaryDtg(null!, out _);

        result.Should().BeFalse();
    }

    #endregion

    #region TryParseTime Tests

    [Theory]
    [InlineData("9:30 AM", 9, 30)]
    [InlineData("14:00", 14, 0)]
    [InlineData("2:15 PM", 14, 15)]
    [InlineData("08:00", 8, 0)]
    public void TryParseTime_ExistingFormats_StillWork(string input, int hour, int minute)
    {
        var result = TimeParsingHelper.TryParseTime(input, out var parsed);

        result.Should().BeTrue();
        parsed.Hour.Should().Be(hour);
        parsed.Minute.Should().Be(minute);
    }

    [Fact]
    public void TryParseTime_DateTime_ExtractsTime()
    {
        var dt = new DateTime(2010, 5, 3, 16, 30, 0);

        var result = TimeParsingHelper.TryParseTime(dt, out var parsed);

        result.Should().BeTrue();
        parsed.Should().Be(new TimeOnly(16, 30));
    }

    [Fact]
    public void TryParseTime_TimeSpan_ExtractsTime()
    {
        var ts = new TimeSpan(9, 15, 0);

        var result = TimeParsingHelper.TryParseTime(ts, out var parsed);

        result.Should().BeTrue();
        parsed.Should().Be(new TimeOnly(9, 15));
    }

    [Fact]
    public void TryParseTime_ExcelFraction_ExtractsTime()
    {
        // 0.5 = noon in Excel
        var result = TimeParsingHelper.TryParseTime(0.5d, out var parsed);

        result.Should().BeTrue();
        parsed.Should().Be(new TimeOnly(12, 0));
    }

    [Theory]
    [InlineData("5/08/2009  16:00:00 CDT", 16, 0)]
    [InlineData("3/15/2013  08:30:00 EST", 8, 30)]
    [InlineData("1/1/2020 09:00:00 PST", 9, 0)]
    [InlineData("5/08/2009 16:00:00", 16, 0)]
    public void TryParseTime_DateTimeWithTimezone_ExtractsTime(string input, int hour, int minute)
    {
        var result = TimeParsingHelper.TryParseTime(input, out var parsed);

        result.Should().BeTrue();
        parsed.Hour.Should().Be(hour);
        parsed.Minute.Should().Be(minute);
    }

    [Fact]
    public void TryParseTime_MilitaryDtgString_ExtractsTime()
    {
        var result = TimeParsingHelper.TryParseTime("210900AUG2011", out var parsed);

        result.Should().BeTrue();
        parsed.Should().Be(new TimeOnly(9, 0));
    }

    [Fact]
    public void TryParseTime_Null_ReturnsFalse()
    {
        var result = TimeParsingHelper.TryParseTime(null, out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void TryParseTime_EmptyString_ReturnsFalse()
    {
        var result = TimeParsingHelper.TryParseTime("", out _);

        result.Should().BeFalse();
    }

    #endregion

    #region TryParseDateTime Tests

    [Fact]
    public void TryParseDateTime_DateTime_ReturnsAsIs()
    {
        var dt = new DateTime(2010, 5, 15, 16, 0, 0);

        var result = TimeParsingHelper.TryParseDateTime(dt, out var parsed);

        result.Should().BeTrue();
        parsed.Should().Be(dt);
    }

    [Fact]
    public void TryParseDateTime_ExcelSerial_ParsesCorrectly()
    {
        // 40306 = 2010-05-10 in Excel serial date
        var result = TimeParsingHelper.TryParseDateTime(40306.0d, out var parsed);

        result.Should().BeTrue();
        parsed.Year.Should().Be(2010);
        parsed.Month.Should().Be(5);
    }

    [Fact]
    public void TryParseDateTime_MilitaryDtg_ParsesCorrectly()
    {
        var result = TimeParsingHelper.TryParseDateTime("210900AUG2011", out var parsed);

        result.Should().BeTrue();
        parsed.Should().Be(new DateTime(2011, 8, 21, 9, 0, 0));
    }

    [Theory]
    [InlineData("5/08/2009 16:00:00 CDT")]
    [InlineData("5/08/2009 16:00:00")]
    public void TryParseDateTime_DateTimeString_ParsesCorrectly(string input)
    {
        var result = TimeParsingHelper.TryParseDateTime(input, out var parsed);

        result.Should().BeTrue();
        parsed.Year.Should().Be(2009);
        parsed.Month.Should().Be(5);
        parsed.Day.Should().Be(8);
        parsed.Hour.Should().Be(16);
    }

    [Fact]
    public void TryParseDateTime_Null_ReturnsFalse()
    {
        var result = TimeParsingHelper.TryParseDateTime(null, out _);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a date")]
    [InlineData("xyz")]
    public void TryParseDateTime_InvalidString_ReturnsFalse(string input)
    {
        var result = TimeParsingHelper.TryParseDateTime(input, out _);

        result.Should().BeFalse();
    }

    #endregion
}
