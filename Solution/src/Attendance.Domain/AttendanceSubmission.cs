namespace Attendance.Domain;

public sealed class AttendanceSubmission
{
    private AttendanceSubmission(int schoolId, DateOnly date, int recordCount, DateTimeOffset at, string by)
    {
        SchoolId = schoolId;
        Date = date;
        RecordCount = recordCount;
        SubmittedAt = at;
        SubmittedBy = by;
    }

    private AttendanceSubmission() => SubmittedBy = null!;

    public int Id { get; private set; }

    public int SchoolId { get; private set; }

    public DateOnly Date { get; private set; }

    public int RecordCount { get; private set; }

    public DateTimeOffset SubmittedAt { get; private set; }

    public string SubmittedBy { get; private set; }

    public static AttendanceSubmission Log(int schoolId, DateOnly date, int recordCount, DateTimeOffset at, string by)
    {
        if (recordCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(recordCount), recordCount, "Record count cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(by))
        {
            throw new ArgumentException("The acting user is required.", nameof(by));
        }

        return new AttendanceSubmission(schoolId, date, recordCount, at, by);
    }
}
