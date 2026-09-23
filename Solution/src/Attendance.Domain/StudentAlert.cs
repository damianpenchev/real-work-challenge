namespace Attendance.Domain;

public sealed class StudentAlert
{
    public const string ChronicAbsence = "CHRONIC_ABSENCE";

    private StudentAlert(int studentId, int schoolId, string alertType, string schoolYear, string message, DateTimeOffset at)
    {
        StudentId = studentId;
        SchoolId = schoolId;
        AlertType = alertType;
        SchoolYear = schoolYear;
        Message = message;
        RaisedAt = at;
    }

    private StudentAlert()
    {
        AlertType = null!;
        SchoolYear = null!;
        Message = null!;
    }

    public int Id { get; private set; }

    public int StudentId { get; private set; }

    public int SchoolId { get; private set; }

    public string AlertType { get; private set; }

    public string SchoolYear { get; private set; }

    public string Message { get; private set; }

    public DateTimeOffset RaisedAt { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    public string? ResolvedBy { get; private set; }

    public static StudentAlert RaiseChronicAbsence(int studentId, int schoolId, SchoolYear schoolYear, int absences, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(schoolYear);

        if (studentId <= 0 || schoolId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(studentId), "Student and school ids must be positive.");
        }

        return new StudentAlert(
            studentId,
            schoolId,
            ChronicAbsence,
            schoolYear.Label,
            $"Student has reached {absences} absences in {schoolYear.Label}.",
            at);
    }

    public void Resolve(DateTimeOffset at, string by)
    {
        if (string.IsNullOrWhiteSpace(by))
        {
            throw new ArgumentException("The acting user is required.", nameof(by));
        }

        ResolvedAt = at;
        ResolvedBy = by;
    }
}
