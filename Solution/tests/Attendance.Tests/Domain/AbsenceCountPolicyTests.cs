using Attendance.Domain;
using Attendance.Domain.Absenteeism;

namespace Attendance.Tests.Domain;

public class AbsenceCountPolicyTests
{
    private static readonly SchoolYear Year = SchoolYear.StartingIn(2024);

    private static readonly AbsenceCountPolicy Policy = new(10);

    [Fact]
    public void AtTheThreshold_IsChronic()
    {
        var status = Policy.Evaluate(Year, 8, 8);

        Assert.True(status.IsChronic);
        Assert.Equal(8, status.Threshold);
    }

    [Fact]
    public void BelowTheThreshold_IsNotChronic()
    {
        Assert.False(Policy.Evaluate(Year, 7, 8).IsChronic);
    }

    [Fact]
    public void WithoutASchoolThreshold_TheConfiguredDefaultApplies()
    {
        var status = Policy.Evaluate(Year, 10, null);

        Assert.Equal(10, status.Threshold);
        Assert.True(status.IsChronic);
    }
}
