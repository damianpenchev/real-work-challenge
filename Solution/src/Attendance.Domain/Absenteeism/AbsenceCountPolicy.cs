namespace Attendance.Domain.Absenteeism;

// Alternative implementation, selected from configuration: EnrolledDayPercentagePolicy
// (absences / instructional days in the year >= 10%), which needs an instructional calendar.
public sealed class AbsenceCountPolicy : IAbsenteeismPolicy
{
    private readonly int _defaultThreshold;

    public AbsenceCountPolicy(int defaultThreshold)
    {
        if (defaultThreshold <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(defaultThreshold), defaultThreshold, "Default threshold must be positive.");
        }

        _defaultThreshold = defaultThreshold;
    }

    public AbsenteeismStatus Evaluate(SchoolYear schoolYear, int absences, int? schoolThreshold)
    {
        ArgumentNullException.ThrowIfNull(schoolYear);

        if (absences < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(absences), absences, "Absences cannot be negative.");
        }

        var threshold = schoolThreshold ?? _defaultThreshold;
        return new AbsenteeismStatus(schoolYear, absences, threshold, absences >= threshold);
    }
}
