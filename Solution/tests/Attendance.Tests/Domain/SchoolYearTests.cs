using Attendance.Domain;

namespace Attendance.Tests.Domain;

public class SchoolYearTests
{
    [Fact]
    public void Aug31_BelongsToPriorSchoolYear_LegacyRolloverDefect()
    {
        var year = SchoolYear.ForDate(new DateOnly(2025, 8, 31));

        Assert.Equal(2024, year.StartYear);
        Assert.Equal("2024-2025", year.Label);
    }

    [Fact]
    public void Sep1_StartsTheNewSchoolYear_LegacyRolloverDefect()
    {
        var year = SchoolYear.ForDate(new DateOnly(2025, 9, 1));

        Assert.Equal(2025, year.StartYear);
        Assert.Equal("2025-2026", year.Label);
    }

    [Fact]
    public void RangeIsHalfOpen_SoADateFilterCanUseAnIndex()
    {
        var year = SchoolYear.StartingIn(2024);

        Assert.Equal(new DateOnly(2024, 9, 1), year.Start);
        Assert.Equal(new DateOnly(2025, 9, 1), year.EndExclusive);
        Assert.True(year.Contains(year.Start));
        Assert.True(year.Contains(new DateOnly(2025, 8, 31)));
        Assert.False(year.Contains(year.EndExclusive));
    }

    [Fact]
    public void Parse_RoundTripsTheLabel()
    {
        Assert.Equal(SchoolYear.StartingIn(2024), SchoolYear.Parse("2024-2025"));
    }

    [Theory]
    [InlineData("2024-2026")]
    [InlineData("2024")]
    [InlineData("24-25")]
    [InlineData("")]
    public void Parse_RejectsMalformedOrNonConsecutiveLabels(string label)
    {
        Assert.Throws<ArgumentException>(() => SchoolYear.Parse(label));
    }

    [Fact]
    public void EqualityIsByValue()
    {
        Assert.Equal(SchoolYear.ForDate(new DateOnly(2024, 10, 1)), SchoolYear.ForDate(new DateOnly(2025, 3, 4)));
    }
}
