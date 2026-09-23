namespace Attendance.Domain;

public sealed class UniqueInvariantViolationException : Exception
{
    public UniqueInvariantViolationException(string invariant, Exception? inner = null)
        : base($"A unique invariant was violated: {invariant}.", inner) => Invariant = invariant;

    public string Invariant { get; }

    public const string OneRecordPerStudentPerDay = "one attendance record per student per day";

    public const string OneOpenAlertPerStudentPerYear = "one open alert per student per school year";
}
